using Microsoft.Office.Core;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        public void OnDistictClick(object sender, RoutedEventArgs e)
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

            // Insert new report information into database
            DateTime dateTime = DateTime.Now;
            Dictionary<string, string> dict = new Dictionary<string, string>();
            DatabaseManager databaseManager = DatabaseManager.GetDBManager();
            dict.Add("report_location", saveFileDialog.FileName);
            dict.Add("datetime_created", dateTime.ToString("yyyy-MM-dd hh:mm tt"));
            dict.Add("report_range", reportRange[0].ToString("yyyy-MM-dd") + " to " + reportRange[1].ToString("yyyy-MM-dd"));
            databaseManager.InsertReportHistory(dict);

            Interlocked.Increment(ref numReportsGenerating);
            MainWindow.progressBar.IsIndeterminate = true;
            MainWindow.statusTextBlock.Text = "Generating Report...";
            var thread = new Thread(()=>CreateReport(reportRange, districts, dataPoints, saveFileDialog.FileName));
            thread.Start();
        }

        private void CreateReport(List<DateTime> reportRange, List<string> districts, List<string> dataPoints, string saveLocation)
        {

            var startDateStr = reportRange[0].ToString("MM-dd-yyyy");
            var endDateStr = reportRange[1].ToString("MM-dd-yyyy");
            var rangeStr = startDateStr + " TO " + endDateStr;

            //creating excel file
            var excel = new Microsoft.Office.Interop.Excel.Application();
            excel.Visible = false;
            excel.DisplayAlerts = false;
            var xlWorkbook = excel.Workbooks.Add(Type.Missing);
            int rowSat = 1;
            int rowWeek = 1;

            var xlWeeksheet = (Microsoft.Office.Interop.Excel.Worksheet)xlWorkbook.ActiveSheet;
            xlWeeksheet.Name = "FR WEEK";

            var xlWEndsheet = (Microsoft.Office.Interop.Excel.Worksheet)xlWorkbook.Sheets.Add();
            xlWEndsheet.Name = "FR SAT";

            // Center and bold the first 4 lines of each sheet
            for (int i = 1; i < 4; i++)
            {
                xlWEndsheet.Range[xlWEndsheet.Cells[i, 1], xlWEndsheet.Cells[i, 8]].Merge();
                xlWEndsheet.Cells[i, 1].Font.Bold = true;
                xlWEndsheet.Cells[i, 1].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                xlWeeksheet.Range[xlWeeksheet.Cells[i, 1], xlWeeksheet.Cells[i, 8]].Merge();
                xlWeeksheet.Cells[i, 1].Font.Bold = true;
                xlWeeksheet.Cells[i, 1].HorizontalAlignment = XlHAlign.xlHAlignCenter;

            }
            xlWEndsheet.Cells[1, 1] = "KITSAP TRANSIT ROUTE PERFORMANCE REPORT";
            xlWEndsheet.Cells[2, 1] = "SATURDAY REPORT";
            xlWEndsheet.Cells[3, 1] = rangeStr;

            xlWeeksheet.Cells[1, 1] = "KITSAP TRANSIT ROUTE PERFORMANCE REPORT";
            xlWeeksheet.Cells[2, 1] = "WEEKDAY REPORT";
            xlWeeksheet.Cells[3, 1] = rangeStr;


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
            xlWEndsheet.Range[xlWEndsheet.Cells[rowSat, 7], xlWEndsheet.Cells[rowSat, 7]].Merge();
            xlWEndsheet.Cells[rowSat, 7] = "SATURDAY";
            xlWEndsheet.Cells[rowSat, 8] = saturdayCount;
            xlWEndsheet.Cells[rowSat++, 8].HorizontalAlignment = XlHAlign.xlHAlignLeft;
            xlWEndsheet.Range[xlWEndsheet.Cells[rowSat, 7], xlWEndsheet.Cells[rowSat, 7]].Merge();
            xlWEndsheet.Cells[rowSat, 7] = "HOLIDAY";
            xlWEndsheet.Cells[rowSat, 8] = saturdayHolidayCount;
            xlWEndsheet.Cells[rowSat++, 8].HorizontalAlignment = XlHAlign.xlHAlignLeft;
            xlWEndsheet.Range[xlWEndsheet.Cells[rowSat, 7], xlWEndsheet.Cells[rowSat, 7]].Merge();
            xlWEndsheet.Cells[rowSat, 7] = "TOTAL SATURDAYS";
            xlWEndsheet.Cells[rowSat, 8] = saturdayCount + saturdayHolidayCount;
            xlWEndsheet.Cells[rowSat++, 8].HorizontalAlignment = XlHAlign.xlHAlignLeft;

            xlWeeksheet.Range[xlWeeksheet.Cells[rowWeek, 7], xlWeeksheet.Cells[rowWeek, 7]].Merge();
            xlWeeksheet.Cells[rowWeek, 7] = "WEEKDAY";
            xlWeeksheet.Cells[rowWeek, 8] = weekdayCount;
            xlWeeksheet.Cells[rowWeek++, 8].HorizontalAlignment = XlHAlign.xlHAlignLeft;
            xlWeeksheet.Range[xlWeeksheet.Cells[rowWeek, 7], xlWeeksheet.Cells[rowWeek, 7]].Merge();
            xlWeeksheet.Cells[rowWeek, 7] = "HOLIDAY";
            xlWeeksheet.Cells[rowWeek, 8] = weekdayHolidayCount;
            xlWeeksheet.Cells[rowWeek++, 8].HorizontalAlignment = XlHAlign.xlHAlignLeft;
            xlWeeksheet.Range[xlWeeksheet.Cells[rowWeek, 7], xlWeeksheet.Cells[rowWeek, 7]].Merge();
            xlWeeksheet.Cells[rowWeek, 7] = "TOTAL WEEKDAYS";
            xlWeeksheet.Cells[rowWeek, 8] = weekdayCount + weekdayHolidayCount;
            xlWeeksheet.Cells[rowWeek++, 8].HorizontalAlignment = XlHAlign.xlHAlignLeft;

            xlWeeksheet.Range["G4", "G6"].EntireRow.Font.Bold = true;
            xlWEndsheet.Range["G4", "G6"].EntireRow.Font.Bold = true;

            foreach (Microsoft.Office.Interop.Excel.Worksheet worksheet in xlWorkbook.Worksheets)
            {
                worksheet.Activate();
                worksheet.Application.ActiveWindow.SplitColumn = dataPoints.Count;
                worksheet.Application.ActiveWindow.SplitRow = rowWeek + 1;
                worksheet.Application.ActiveWindow.FreezePanes = true;
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
                xlWEndsheet.Range[xlWEndsheet.Cells[rowSat, 1], xlWEndsheet.Cells[rowSat, dataPoints.Count]].Merge();
                xlWEndsheet.Cells[rowSat, 1] = district;
                xlWEndsheet.Cells[rowSat, 1].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                xlWEndsheet.Cells[rowSat, 1].Font.Bold = true;
                rowSat++;

                xlWeeksheet.Range[xlWeeksheet.Cells[rowWeek, 1], xlWeeksheet.Cells[rowWeek, 8]].Merge();
                xlWeeksheet.Cells[rowWeek, 1] = district;
                xlWeeksheet.Cells[rowWeek, 1].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                xlWeeksheet.Cells[rowWeek, 1].Font.Bold = true;
                rowWeek++;
                // Write column titles
                for (int i = 0; i < dataPoints.Count; i++)
                {
                    xlWEndsheet.Cells[rowSat, i + 1] = dataPoints[i];
                    xlWEndsheet.Cells[rowSat, i + 1].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                    xlWEndsheet.Cells[rowSat, i + 1].VerticalAlignment = XlVAlign.xlVAlignCenter;
                    xlWEndsheet.Cells[rowSat, i + 1].Font.Bold = true;
                    xlWEndsheet.Cells[rowSat, i + 1].WrapText = false;

                    xlWeeksheet.Cells[rowWeek, i + 1] = dataPoints[i];
                    xlWeeksheet.Cells[rowSat, i + 1].VerticalAlignment = XlVAlign.xlVAlignCenter;
                    xlWeeksheet.Cells[rowWeek, i + 1].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                    xlWeeksheet.Cells[rowWeek, i + 1].Font.Bold = true;
                    xlWeeksheet.Cells[rowSat, i + 1].WrapText = false;
                }
                rowSat++;
                rowWeek++;

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
                    var calculatedSat = new Dictionary<string, object>();
                    calculatedWeek.Add("ROUTE NAME", route["route_name"]);
                    calculatedWeek.Add("ROUTE NO.", routeId);
                    calculatedSat.Add("ROUTE NAME", route["route_name"]);
                    calculatedSat.Add("ROUTE NO.", routeId);
                    // Num trips on normal weekdays
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
                    double routeDistance = Convert.ToDouble(route["distance"]);
                    double revenueMilesWeek = routeDistance * (numTripsWeek + numTripsHolidaysW);
                    calculatedWeek.Add("REVENUE MILES", revenueMilesWeek);
                    double revenueMilesSat = routeDistance * (numTripsSat + numTripsHolidaysS);
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
                    for (int i = 0; i < dataPoints.Count; i++)
                    {
                        var column = dataPoints[i];
                        if (!calculatedWeek.ContainsKey(column))
                        {
                            continue;
                        }
                        if (calculatedWeek[column] is double)
                        {
                            xlWeeksheet.Cells[rowWeek, i + 1] = Math.Round((double)calculatedWeek[column], 1);
                            xlWEndsheet.Cells[rowSat, i + 1] = Math.Round((double)calculatedSat[column], 1);
                        }
                        else
                        {
                            xlWeeksheet.Cells[rowWeek, i + 1] = calculatedWeek[column];
                            xlWEndsheet.Cells[rowSat, i + 1] = calculatedSat[column];
                        }
                    }
                    rowSat++;
                    rowWeek++;
                }
                rowSat++;
                rowWeek++;
            }

            xlWeeksheet.Columns.AutoFit();
            xlWEndsheet.Columns.AutoFit();
            xlWorkbook.SaveAs(saveLocation);
            xlWorkbook.Close();
            excel.Quit();
            // Refresh the report history panel on the UI thread
            this.Dispatcher.Invoke(()=>
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

