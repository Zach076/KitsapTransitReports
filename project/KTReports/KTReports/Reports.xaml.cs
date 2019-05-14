using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KTReports
{

    /// <summary>
    /// Interaction logic for Reports.xaml
    /// </summary>
    public partial class Reports : Page
    {
        DatabaseManager databaseManager;
        private List<NameValueCollection> latestReports = null;
        private NameValueCollection selectedReport = null;
        private Button lastButtonClicked = null;
        private int numReportsGenerating = 0;
        Stopwatch stopWatch = new Stopwatch();

        public Reports()
        {
            InitializeComponent();
            databaseManager = DatabaseManager.GetDBManager();
            RefreshReportsPanel();
        }

        public void OnDataPointClick(object sender, RoutedEventArgs e)
        {
            CheckBox senderCheckBox = (CheckBox) sender;
            if (senderCheckBox == SelectAllDataPoints && senderCheckBox.IsChecked == true)
            {
                // Enable all checkboxes for data points
                foreach (var uiElem in DataPointCheckBoxes.Children)
                {
                    if (uiElem.GetType() != typeof(StackPanel)) continue;

                    foreach (CheckBox c in ((StackPanel)uiElem).Children)
                    {
                        c.IsChecked = true;
                    }
                }
            }
            else if (senderCheckBox.IsChecked == false)
            {
                // A checkbox was unchecked
                SelectAllDataPoints.IsChecked = false;
            }
        }

        public void OnDistrictClick(object sender, RoutedEventArgs e)
        {
            CheckBox senderCheckBox = (CheckBox) sender;
            if (senderCheckBox == SelectAllDistricts && senderCheckBox.IsChecked == true)
            {
                // Enable all checkboxes for data points
                foreach (var uiElem in DistrictCheckBoxes.Children)
                {
                    if (uiElem.GetType() != typeof(CheckBox)) continue;
                    CheckBox c = (CheckBox)uiElem;
                    c.IsChecked = true;
                }
            }
            else if (senderCheckBox.IsChecked == false)
            {
                // A checkbox was unchecked
                SelectAllDistricts.IsChecked = false;
            }
        }

        public void OnGenerateReportClick(object sender, RoutedEventArgs e)
        {

            Console.WriteLine("Generate Report Clicked");
            // Get a list of Datapoints to include
            List<string> dataPoints = GetSelectedDataPoints();
            if (dataPoints.Count == 0)
            {
                MessageBox.Show("Must select at least one data point.", "Report Generation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // Get a list of districts to include
            List<string> districts = GetSelectedDistricts();
            if (districts.Count == 0) {
                MessageBox.Show("Must select at least one district.", "Report Generation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Get the start and end dates and validate
            List<DateTime> reportRange = GetReportRange();
            // Validate the start and end dates
            if (!IsValidRange(reportRange))
            {
                // Display error message and do not generate report
                Console.WriteLine("Enter a valid report range.");
                MessageBox.Show("Enter a valid report range.", "Report Generation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;

            }
            var startDateStr = reportRange[0].ToString("MM-dd-yyyy");
            var endDateStr = reportRange[1].ToString("MM-dd-yyyy");
            var rangeStr = startDateStr + " to " + endDateStr;

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.FileName = "Kitsap Transit Report " + rangeStr;
            saveFileDialog.DefaultExt = ".xlsx";
            saveFileDialog.Filter = "Excel Files | *.xlsx";
            bool? dialogResult = saveFileDialog.ShowDialog();
            if (dialogResult != true)
            {
                return;
            }
            stopWatch.Start();

            // Insert new report information into database
            DateTime dateTime = DateTime.Now;
            Dictionary<string, string> dict = new Dictionary<string, string>();
            DatabaseManager databaseManager = DatabaseManager.GetDBManager();
            dict.Add("report_location", saveFileDialog.FileName);
            dict.Add("datetime_created", dateTime.ToString("yyyy-MM-dd hh:mm:ss tt"));
            dict.Add("report_range", reportRange[0].ToString("yyyy-MM-dd") + " to " + reportRange[1].ToString("yyyy-MM-dd"));
            databaseManager.InsertReportHistory(dict);

            Interlocked.Increment(ref numReportsGenerating);
            MainWindow.progressBar.IsIndeterminate = true;
            MainWindow.statusTextBlock.Text = "Generating Report...";
            var thread = new Thread(()=>CreateReportThread(reportRange, districts, dataPoints, saveFileDialog.FileName));
            thread.Start();
        }

        private void CreateReportThread(List<DateTime> reportRange, List<string> districts, List<string> dataPoints, string saveLocation)
        {
            try
            {
                CreateReport(reportRange, districts, dataPoints, saveLocation);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Unable to save report {saveLocation}", "Report Generation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine(e.StackTrace);
            
            }
            finally
            {
                stopWatch.Stop();
                // Get the elapsed time as a TimeSpan value.
                TimeSpan ts = stopWatch.Elapsed;
                // Format and display the TimeSpan value.
                string elapsedTime = String.Format("{0} seconds, {1} milliseconds", ts.Seconds, ts.Milliseconds);
                Console.WriteLine("RunTime " + elapsedTime);
                // Refresh the report history panel on the UI thread
                Dispatcher.Invoke(() =>
                {
                    RefreshReportsPanel();
                    Interlocked.Decrement(ref numReportsGenerating);
                    if (numReportsGenerating == 0)
                    {
                        MainWindow.progressBar.IsIndeterminate = false;
                        MainWindow.statusTextBlock.Text = string.Empty;
                    }
                });
            }
        }

        private void CreateReport(List<DateTime> reportRange, List<string> districts, List<string> dataPoints, string saveLocation)
        {

            var startDateStr = reportRange[0].ToString("MM-dd-yyyy");
            var endDateStr = reportRange[1].ToString("MM-dd-yyyy");
            var rangeStr = startDateStr + " TO " + endDateStr;

            //creating excel file
            var workbook = new XLWorkbook();
            var xlWEndsheet = workbook.Worksheets.Add("FR SAT");
            var xlWeeksheet = workbook.Worksheets.Add("FR WEEK");
            int rowSat = 1;
            int rowWeek = 1;

            // Center and bold the first 4 lines of each sheet
            for (int i = 1; i < 4; i++)
            {
                xlWEndsheet.Range(xlWEndsheet.Cell(i, 1).Address, xlWEndsheet.Cell(i, 8).Address).Row(1).Merge();
                xlWEndsheet.Cell(i, 1).Style.Font.Bold = true;
                xlWEndsheet.Cell(i, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                xlWeeksheet.Range(xlWeeksheet.Cell(i, 1).Address, xlWeeksheet.Cell(i, 8).Address).Row(1).Merge();
                xlWeeksheet.Cell(i, 1).Style.Font.Bold = true;
                xlWeeksheet.Cell(i, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            }
            xlWEndsheet.Cell(1, 1).Value = "KITSAP TRANSIT ROUTE PERFORMANCE REPORT";
            xlWEndsheet.Cell(2, 1).Value = "SATURDAY REPORT";
            xlWEndsheet.Cell(3, 1).Value = rangeStr;

            xlWeeksheet.Cell(1, 1).Value = "KITSAP TRANSIT ROUTE PERFORMANCE REPORT";
            xlWeeksheet.Cell(2, 1).Value = "WEEKDAY REPORT";
            xlWeeksheet.Cell(3, 1).Value = rangeStr;

            rowSat = 4;
            rowWeek = 4;


            // Get num trips from date range for weekdays and saturdays separately
            // This requires counting (number of weekdays in date range - number of holidays on weekdays in day range) * num trips made per weekday
            int weekdayCount = GetNumWeekdays(reportRange);
            int weekdayHolidayCount = GetNumHolidays(reportRange, true, new List<int> { 1 });
            int saturdayCount = GetNumSaturdays(reportRange);
            int saturdayHolidayCount = GetNumHolidays(reportRange, false, new List<int> { 1 });
            Console.WriteLine("Weekday count: " + weekdayCount);
            Console.WriteLine("Weekday holiday count: " + weekdayHolidayCount);
            Console.WriteLine("Saturday count: " + saturdayCount);
            Console.WriteLine("Saturday holiday count: " + saturdayHolidayCount);

            //write days
            xlWEndsheet.Range(xlWEndsheet.Cell(rowSat, 7).Address, xlWEndsheet.Cell(rowSat, 7).Address).Row(1).Merge();
            xlWEndsheet.Cell(rowSat, 7).Value = "SATURDAY";
            xlWEndsheet.Cell(rowSat, 8).Value = saturdayCount;
            xlWEndsheet.Cell(rowSat++, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
            xlWEndsheet.Range(xlWEndsheet.Cell(rowSat, 7).Address, xlWEndsheet.Cell(rowSat, 7).Address).Row(1).Merge();
            xlWEndsheet.Cell(rowSat, 7).Value = "HOLIDAY";
            xlWEndsheet.Cell(rowSat, 8).Value = saturdayHolidayCount;
            xlWEndsheet.Cell(rowSat++, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
            xlWEndsheet.Range(xlWEndsheet.Cell(rowSat, 7).Address, xlWEndsheet.Cell(rowSat, 7).Address).Row(1).Merge();
            xlWEndsheet.Cell(rowSat, 7).Value = "TOTAL SATURDAYS";
            xlWEndsheet.Cell(rowSat, 8).Value = saturdayCount + saturdayHolidayCount;
            xlWEndsheet.Cell(rowSat++, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

            xlWeeksheet.Range(xlWeeksheet.Cell(rowWeek, 7).Address, xlWeeksheet.Cell(rowWeek, 7).Address).Row(1).Merge();
            xlWeeksheet.Cell(rowWeek, 7).Value = "WEEKDAY";
            xlWeeksheet.Cell(rowWeek, 8).Value = weekdayCount;
            xlWeeksheet.Cell(rowWeek++, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
            xlWeeksheet.Range(xlWeeksheet.Cell(rowWeek, 7).Address, xlWeeksheet.Cell(rowWeek, 7).Address).Row(1).Merge();
            xlWeeksheet.Cell(rowWeek, 7).Value = "HOLIDAY";
            xlWeeksheet.Cell(rowWeek, 8).Value = weekdayHolidayCount;
            xlWeeksheet.Cell(rowWeek++, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
            xlWeeksheet.Range(xlWeeksheet.Cell(rowWeek, 7).Address, xlWeeksheet.Cell(rowWeek, 7).Address).Row(1).Merge();
            xlWeeksheet.Cell(rowWeek, 7).Value = "TOTAL WEEKDAYS";
            xlWeeksheet.Cell(rowWeek, 8).Value = weekdayCount + weekdayHolidayCount;
            xlWeeksheet.Cell(rowWeek++, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

            xlWeeksheet.Range("G4", "G6").Style.Font.Bold = true;
            xlWEndsheet.Range("G4", "G6").Style.Font.Bold = true;

            foreach (var worksheet in workbook.Worksheets)
            {
                worksheet.SheetView.Freeze(rowWeek + 1, dataPoints.Count);
            }

            // Get all routes per district
            var districtToRoutes = new Dictionary<string, List<NameValueCollection>>();
            var weekRoutes = new Dictionary<int, Dictionary<string, int>>();
            var satRoutes = new Dictionary<int, Dictionary<string, int>>();
            foreach (var district in districts)
            {
                // Need to distinguish between weekday and non-weekday routes
                List<NameValueCollection> routes = databaseManager.GetDistrictRoutes(district, reportRange);
                districtToRoutes.Add(district, routes);
                Console.WriteLine($"Printing routes in district: {district}");

                // Write district name
                xlWEndsheet.Range(xlWEndsheet.Cell(rowSat, 1).Address, xlWEndsheet.Cell(rowSat, dataPoints.Count).Address).Row(1).Merge();
                xlWEndsheet.Cell(rowSat, 1).Value= district;
                xlWEndsheet.Cell(rowSat, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                xlWEndsheet.Cell(rowSat, 1).Style.Font.Bold = true;
                rowSat++;

                xlWeeksheet.Range(xlWeeksheet.Cell(rowWeek, 1).Address, xlWeeksheet.Cell(rowWeek, dataPoints.Count).Address).Row(1).Merge();
                xlWeeksheet.Cell(rowWeek, 1).Value = district;
                xlWeeksheet.Cell(rowWeek, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                xlWeeksheet.Cell(rowWeek, 1).Style.Font.Bold = true;
                rowWeek++;
                // Write column titles
                for (int i = 0; i < dataPoints.Count; i++)
                {
                    xlWEndsheet.Cell(rowSat, i + 1).Value = dataPoints[i];
                    xlWEndsheet.Cell(rowSat, i + 1).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                    xlWEndsheet.Cell(rowSat, i + 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    xlWEndsheet.Cell(rowSat, i + 1).Style.Font.Bold = true;
                    xlWEndsheet.Cell(rowSat, i + 1).Style.Alignment.WrapText = false;

                    xlWeeksheet.Cell(rowWeek, i + 1).Value = dataPoints[i];
                    xlWeeksheet.Cell(rowWeek, i + 1).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                    xlWeeksheet.Cell(rowWeek, i + 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    xlWeeksheet.Cell(rowWeek, i + 1).Style.Font.Bold = true;
                    xlWeeksheet.Cell(rowWeek, i + 1).Style.Alignment.WrapText = false;
                }
                rowSat++;
                rowWeek++;
                int rowSatStart = rowSat;
                int rowWeekStart = rowWeek;
                var routeWeekCalculations = new List<Dictionary<string, object>>();
                var routeSatCalculations = new List<Dictionary<string, object>>();
                foreach (var route in routes)
                {
                    int routeId = Convert.ToInt32(route["assigned_route_id"]);
                    //Console.WriteLine($"\tRoute id: {routeId}");

                    // Get sum of ridership for each route between reportRange for weekdays
                    // routeTotal contains nfc.total_ridership, nfc.total_nonridership, fc.boardings and total
                    Dictionary<string, int> routeTotalWeek = databaseManager.GetRouteRidership(routeId, reportRange, true);

                    // Get sum of ridership for each route between reportRange for saturdays
                    Dictionary<string, int> routeTotalSat = databaseManager.GetRouteRidership(routeId, reportRange, false);

                    weekRoutes.Add(routeId, routeTotalWeek);
                    satRoutes.Add(routeId, routeTotalSat);
                    var calculatedWeek = new Dictionary<string, object>();
                    routeWeekCalculations.Add(calculatedWeek);
                    var calculatedSat = new Dictionary<string, object>();
                    routeSatCalculations.Add(calculatedSat);
                    calculatedWeek.Add("ROUTE NAME", route["route_name"].ToUpper());
                    calculatedWeek.Add("ROUTE NO.", routeId);
                    calculatedSat.Add("ROUTE NAME", route["route_name"].ToUpper());
                    calculatedSat.Add("ROUTE NO.", routeId);
                    // Num trips on normal weekdays
                    //Console.WriteLine("NUM TRIPS WEEK: " + route["num_trips_week"]);
                    double numTripsWeek = Convert.ToDouble(route["num_trips_week"]) * weekdayCount;

                    // Num trips on serviced holiday weekdays
                    double numTripsHolidaysW = Convert.ToDouble(route["num_trips_hol"]) * weekdayHolidayCount;
                    calculatedWeek.Add("NO. OF TRIPS", numTripsWeek + numTripsHolidaysW);

                    // Num trips on normal saturdays
                    double numTripsSat = Convert.ToDouble(route["num_trips_sat"]) * saturdayCount;

                    // Num trips on serviced holiday saturdays
                    double numTripsHolidaysS = Convert.ToDouble(route["num_trips_hol"]) * saturdayHolidayCount;
                    calculatedSat.Add("NO. OF TRIPS", numTripsSat + numTripsHolidaysS);

                    // Get revenue miles for a route (distance of trip * num trips during week (regardless of holiday or not))
                    double routeDistanceWeek = Convert.ToDouble(route["distance_week"]);
                    double routeDistanceSat = Convert.ToDouble(route["distance_sat"]);
                    double revenueMilesWeek = routeDistanceWeek * (weekdayCount + weekdayHolidayCount);
                    calculatedWeek.Add("REVENUE MILES", revenueMilesWeek);
                    double revenueMilesSat = routeDistanceSat * (saturdayCount + saturdayHolidayCount);
                    calculatedSat.Add("REVENUE MILES", revenueMilesSat);

                    // Get revenue hours (num hours on weekday * number of weekdays excluding holidays)
                    double revenueHoursWeek = Convert.ToDouble(route["weekday_hours"]) * weekdayCount;
                    double revenueHoursHolidaysW = Convert.ToDouble(route["holiday_hours"]) * weekdayHolidayCount;
                    calculatedWeek.Add("REVENUE HOURS", revenueHoursWeek + revenueHoursHolidaysW);
                    double revenueHoursSat = Convert.ToDouble(route["saturday_hours"]) * saturdayCount;
                    double revenueHoursHolidaysS = Convert.ToDouble(route["holiday_hours"]) * saturdayHolidayCount;
                    calculatedSat.Add("REVENUE HOURS", revenueHoursSat + revenueHoursHolidaysS);

                    // Get total ridership during weekdays and saturdays
                    int totalRidesWeek = routeTotalWeek["total"];
                    calculatedWeek.Add("TOTAL PASSENGERS", totalRidesWeek);
                    int totalRidesSat = routeTotalSat["total"];
                    calculatedSat.Add("TOTAL PASSENGERS", totalRidesSat);

                    // Get passengers per mile (total passengers on weekdays / revenue miles)
                    double passPerMileW = totalRidesWeek / revenueMilesWeek;
                    calculatedWeek.Add("PASSENGERS PER MILE", passPerMileW);
                    double passPerMileS = totalRidesSat / revenueMilesSat;
                    calculatedSat.Add("PASSENGERS PER MILE", passPerMileS);

                    // Get passengers per hour (using total passengers / revenue hours)
                    double passPerHourW = totalRidesWeek / (revenueHoursWeek + revenueHoursHolidaysW);
                    calculatedWeek.Add("PASSENGERS PER HOUR", passPerHourW);
                    double passPerHourS = totalRidesSat / (revenueHoursSat + revenueHoursHolidaysS);
                    calculatedSat.Add("PASSENGERS PER HOUR", passPerHourS);

                    // Write values
                    bool includeSat = routeDistanceSat > 0;
                    for (int i = 0; i < dataPoints.Count; i++)
                    {
                        var column = dataPoints[i];
                        if (!calculatedWeek.ContainsKey(column))
                        {
                            continue;
                        }
                        if (calculatedWeek[column] is double)
                        {
                            xlWeeksheet.Cell(rowWeek, i + 1).Value = Math.Round((double)calculatedWeek[column], 1);
                            if (includeSat)
                            {
                                xlWEndsheet.Cell(rowSat, i + 1).Value = Math.Round((double)calculatedSat[column], 1);
                            }
                        }
                        else
                        {
                            xlWeeksheet.Cell(rowWeek, i + 1).Value = calculatedWeek[column];
                            if (includeSat)
                            {
                                xlWEndsheet.Cell(rowSat, i + 1).Value = calculatedSat[column];
                            }
                        }
                    }
                    if (includeSat)
                    {
                        rowSat++;
                    }
                    rowWeek++;
                }
                xlWeeksheet.Cell(rowWeek, 2).Value = $"TOTAL {district.ToUpper()}";
                xlWEndsheet.Cell(rowSat, 2).Value = $"TOTAL {district.ToUpper()}";
                // Insert totals for district
                for (int i = 2; i < dataPoints.Count; i++)
                {
                    var column = dataPoints[i];
                    if (column.Equals("ROUTE NO.") || column.Equals("ROUTE NAME"))
                    {
                        continue;
                    }
                    else if (column.Equals("PASSENGERS PER MILE"))
                    {
                        WritePassengersPerMile(xlWeeksheet, xlWEndsheet, rowWeek, rowSat, i + 1, routeWeekCalculations, routeSatCalculations);
                        
                    }
                    else if (column.Equals("PASSENGERS PER HOUR"))
                    {
                        WritePassengersPerHour(xlWeeksheet, xlWEndsheet, rowWeek, rowSat, i + 1, routeWeekCalculations, routeSatCalculations);
                    }
                    else
                    {
                        xlWeeksheet.Cell(rowWeek, i + 1).FormulaA1 = "=Sum(" + xlWeeksheet.Cell(rowWeekStart, i + 1).Address
                                                                    + ":" + xlWeeksheet.Cell(rowWeek - 1, i + 1).Address + ")";
                        xlWEndsheet.Cell(rowSat, i + 1).FormulaA1 = "=Sum(" + xlWEndsheet.Cell(rowSatStart, i + 1).Address
                                                                        + ":" + xlWEndsheet.Cell(rowSat - 1, i + 1).Address + ")";
                    }
                }
                xlWeeksheet.Row(rowWeek).Style.Font.Bold = true;
                xlWEndsheet.Row(rowSat).Style.Font.Bold = true;
                rowSat += 2;
                rowWeek += 2;
            }
            xlWeeksheet.Columns().AdjustToContents();
            xlWEndsheet.Columns().AdjustToContents();

            workbook.SaveAs(saveLocation);
        }


        private void WritePassengersPerHour(IXLWorksheet xlWeeksheet, IXLWorksheet xlWEndsheet,
            int rowWeek, int rowSat, int col, List<Dictionary<string, object>> routeWeekCalculations, List<Dictionary<string, object>> routeSatCalculations)
        {
            int totalPassengersW = 0;
            double totalHoursW = 0;
            foreach (var route in routeWeekCalculations)
            {
                totalPassengersW += Convert.ToInt32(route["TOTAL PASSENGERS"]);
                totalHoursW += Convert.ToDouble(route["REVENUE HOURS"]);
            }
            xlWeeksheet.Cell(rowWeek, col).Value = Math.Round(totalPassengersW / totalHoursW, 1);
            int totalPassengersS = 0;
            double totalHoursS = 0;
            foreach (var route in routeSatCalculations)
            {
                totalPassengersS += Convert.ToInt32(route["TOTAL PASSENGERS"]);
                totalHoursS += Convert.ToDouble(route["REVENUE HOURS"]);
            }
            xlWEndsheet.Cell(rowSat, col).Value = Math.Round(totalPassengersS / totalHoursS, 1);
        }

        private void WritePassengersPerMile(IXLWorksheet xlWeeksheet, IXLWorksheet xlWEndsheet, 
            int rowWeek, int rowSat, int col, List<Dictionary<string, object>> routeWeekCalculations, List<Dictionary<string, object>>routeSatCalculations)
        {
            int totalPassengersW = 0;
            double totalRevenueMilesW = 0;
            foreach (var route in routeWeekCalculations)
            {
                totalPassengersW += Convert.ToInt32(route["TOTAL PASSENGERS"]);
                totalRevenueMilesW += Convert.ToDouble(route["REVENUE MILES"]);
            }
            xlWeeksheet.Cell(rowWeek, col).Value = Math.Round(totalPassengersW / totalRevenueMilesW, 1);
            int totalPassengersS = 0;
            double totalRevenueMilesS = 0;
            foreach (var route in routeSatCalculations)
            {
                totalPassengersS += Convert.ToInt32(route["TOTAL PASSENGERS"]);
                totalRevenueMilesS += Convert.ToDouble(route["REVENUE MILES"]);
            }
            xlWEndsheet.Cell(rowSat, col).Value = Math.Round(totalPassengersS / totalRevenueMilesS, 1);
        }

        private void RefreshReportsPanel()
        {
            latestReports = databaseManager.GetLatestReports();
            // Reverse so that most recent report is at bottom of list
            latestReports.Reverse();
            RemoveReportButtons();
            AddReportButtons();
            selectedReport = null;
            lastButtonClicked = null;
        }

        private void RemoveReportButtons()
        {
            PastReportsList.Items.Clear();
        }

        private void AddReportButtons()
        {
            foreach (var report in latestReports)
            {
                var stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Horizontal;
                stackPanel.VerticalAlignment = VerticalAlignment.Center;
                stackPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                var button = new Button();
                button.Width = 300;
                button.Content = System.IO.Path.GetFileName(report["report_location"]);
                button.Tag = report["report_location"];
                button.Margin = new Thickness(4);
                button.Padding = new Thickness(4);
                button.Click += new RoutedEventHandler(this.ReportButtonClick);
                var description = new TextBlock();
                description.Text = report["report_range"];
                description.Margin = new Thickness(50, 0, 0, 0);
                description.VerticalAlignment = VerticalAlignment.Center;
                var dateCreated = new TextBlock();
                dateCreated.Text = "Created on " + report["datetime_created"];
                dateCreated.Margin = new Thickness(50, 0, 0, 0);
                dateCreated.VerticalAlignment = VerticalAlignment.Center;
                stackPanel.Children.Add(button);
                stackPanel.Children.Add(description);
                stackPanel.Children.Add(dateCreated);
                PastReportsList.Items.Add(stackPanel);
            }
        }

        private void OnStartDateChanged(object sender, RoutedEventArgs e)
        {
            if (EndDatePicker.SelectedDate == null)
            {
                var startDate = (DateTime) StartDatePicker.SelectedDate;

                EndDatePicker.SelectedDate = new DateTime(startDate.Year, startDate.Month, DateTime.DaysInMonth(startDate.Year, startDate.Month));
            }
        }

        private void ReportButtonClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (lastButtonClicked != null)
            {
                lastButtonClicked.Background = Brushes.Gainsboro;
            }
            button.Background = Brushes.SkyBlue;
            lastButtonClicked = button;
        }

        private void OpenReportClick(object sender, RoutedEventArgs e)
        {
            if (lastButtonClicked == null)
            {
                return;
            }
            var path = lastButtonClicked.Tag.ToString();
            try
            {
                Process.Start(path);
            } catch (Exception fileException)
            {
                Console.WriteLine(fileException.StackTrace);
                MessageBox.Show($"Could not open file: {path}", "Open Report Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            RefreshReportsPanel();
        }

        private int GetNumWeekdays(List<DateTime> reportRange)
        {
            int weekdayCount = 0;
            DateTime startDate = reportRange[0];
            DateTime endDate = reportRange[1];
            // Add one so that we include the start date as a day
            int totalDays = (int) (endDate - startDate).TotalDays; 
            double numWeekends = totalDays / 7.0;
            int numFullWeeks = (int) Math.Floor(numWeekends);
            DateTime lastAccountedDay = startDate.AddDays(numFullWeeks * 7);
            int additionalWeekdays = 0;
            if (DateTime.Compare(lastAccountedDay, endDate) <= 0)
            {
                // Calculate the number of remaining weekdays after accounting for full weeks
                // First calculate how many days to reach the weekend
                int numTilWeekend = lastAccountedDay.DayOfWeek > DayOfWeek.Sunday ? 
                        (DayOfWeek.Saturday - lastAccountedDay.DayOfWeek) : 0;
                // Next calculate how many days to reach the end date if the end date is during the weekdays
                int numTilEndDate = 0;
                if (endDate.DayOfWeek < DayOfWeek.Saturday && endDate.DayOfWeek > DayOfWeek.Sunday)
                {
                    numTilEndDate = endDate.DayOfWeek - DayOfWeek.Sunday;
                }
                // A possibility is that the last accounted day and the end date both take place during the week
                int dateDiff = (int) (endDate - lastAccountedDay).TotalDays + 1;
                // The min of the two possibilities is the number of weekdays we did not account for with full weeks
                additionalWeekdays = Math.Min(numTilWeekend + numTilEndDate, dateDiff);
            }
            weekdayCount = (numFullWeeks * 5) + additionalWeekdays;
            int weekdayHolidayCount = GetNumHolidays(reportRange, true, new List<int> { 1, 2 });
            // Subtract number of holidays occurring on weekdays within range
            weekdayCount -= weekdayHolidayCount;
            return weekdayCount;
        }

        private int GetNumSaturdays(List<DateTime> reportRange)
        {
            DateTime startDate = reportRange[0];
            DateTime endDate = reportRange[1];
            int totalDays = (int)(endDate - startDate).TotalDays; 
            int numFullWeeks = (int) Math.Floor(totalDays / 7.0);
            DateTime lastAccountedDay = startDate.AddDays(numFullWeeks * 7);
            int additionalSaturday = 0;
            int dateDiff = (int) (endDate - lastAccountedDay).TotalDays;
            if (lastAccountedDay.DayOfWeek + dateDiff >= DayOfWeek.Saturday)
            {
                // If adding the difference between the lastAccountedDay and the endDate
                // equals or goes past saturday, that means that we must account for an additional saturday
                additionalSaturday++;
            }

            int numSaturdays = numFullWeeks + additionalSaturday;
            int saturdayHolidayCount = GetNumHolidays(reportRange, false, new List<int> { 1, 2 });
            // Subtract number of holidays occurring on saturdays within range
            numSaturdays -= saturdayHolidayCount;
            return numSaturdays;
        }

        // Gets number of holidays occurring within a range on weekdays or on saturdays (true/false respectively)
        // serviceType: 1 == HOLIDAY SERVICE, 2 == NO SERVICE
        private int GetNumHolidays(List<DateTime> reportRange, bool forWeekdays, List<int> serviceTypes)
        {
            List<NameValueCollection> holidays = databaseManager.GetHolidaysInRange(reportRange);
            int holidayCount = 0;
            foreach (var holiday in holidays)
            {
                int serviceType = Convert.ToInt32(holiday["service_type"]);
                // Only examine holidays with the specified service type
                if (!serviceTypes.Contains(serviceType))
                {
                    continue;
                }
                DayOfWeek holidayDOW = Convert.ToDateTime(holiday["date"]).DayOfWeek;
                bool onWeekday = holidayDOW > DayOfWeek.Sunday && holidayDOW < DayOfWeek.Saturday;
                if (forWeekdays)
                {
                    if (onWeekday)
                    {
                        holidayCount++;
                    }
                }
                else
                {
                    if (!onWeekday && holidayDOW == DayOfWeek.Saturday)
                    {
                        holidayCount++;
                    }
                }
            }
            return holidayCount;
        }

        private bool IsValidRange(List<DateTime> reportRange)
        {
            if (reportRange.Count != 2 || reportRange[0].CompareTo(reportRange[1]) > 0)
            {
                // reportRange contains null values or start date is later than end date, which is invalid
                return false;
            }
            return true;
        }

        private List<DateTime> GetReportRange()
        {
            // Get the DateTimes from each of the date pickers and return them in a list
            List<DateTime> reportRange = new List<DateTime>();
            if (StartDatePicker.SelectedDate != null)
            {
                DateTime startDate = StartDatePicker.SelectedDate.Value;
                reportRange.Add(startDate);
            }
            if (EndDatePicker.SelectedDate != null)
            {
                DateTime endDate = EndDatePicker.SelectedDate.Value;
                reportRange.Add(endDate);
            }
            return reportRange;
        }

        private List<string> GetSelectedDataPoints()
        {
            // Iterate through each of the Data Point checkboxes and return the selected data points
            var dataPoints = new List<string>();
            foreach (var uiElem in DataPointCheckBoxes.Children)
            {
                if (uiElem.GetType() != typeof(StackPanel)) continue;

                foreach (CheckBox c in ((StackPanel)uiElem).Children)
                {
                    if (c != SelectAllDataPoints && c.IsChecked == true)
                    {
                        var dataPointStr = c.Content.ToString().ToUpper().Replace("NUMBER", "NO.");
                        /*if (dataPointStr.Length > 10)
                        {
                            int firstSpaceIdx = dataPointStr.IndexOf(' ');
                            if (firstSpaceIdx > 0)
                            {
                                dataPointStr = dataPointStr.Substring(0, firstSpaceIdx) + '\n' + dataPointStr.Substring(firstSpaceIdx+1);
                            }
                        }*/
                        dataPoints.Add(dataPointStr);
                    }
                }
            }
            return dataPoints;
        }

        private List<string> GetSelectedDistricts()
        {
            // Iterate through each of the District checkboxes and return the selected districts
            var districts = new List<string>();
            foreach (var uiElem in DistrictCheckBoxes.Children)
            {
                if (uiElem.GetType() != typeof(CheckBox)) continue;
                CheckBox c = (CheckBox)uiElem;
                if (c != SelectAllDistricts && c.IsChecked == true)
                {
                    districts.Add(c.Content.ToString());
                }
            }
            return districts;
        }
    }
}

