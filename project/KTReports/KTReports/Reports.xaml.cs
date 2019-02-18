using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        // Can make Reports a singleton
        public Reports()
        {
            InitializeComponent();
            databaseManager = DatabaseManager.GetDBManager();
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
            /*foreach (string district in districts)
            {
                Console.WriteLine(district);
            }*/
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
            // Get all routes per district
            var districtToRoutes = new Dictionary<string, List<NameValueCollection>>();
            var weekRoutes = new Dictionary<int, NameValueCollection>();
            var satRoutes = new Dictionary<int, NameValueCollection>();
            foreach (var district in districts)
            {
                // Need to distinguish between weekday and non-weekday routes
                List<NameValueCollection> routes = databaseManager.GetDistrictRoutes(district, reportRange);
                districtToRoutes.Add(district, routes);
                Console.WriteLine($"Printing routes in district: {district}");
               /* foreach (var row in routes)
                {
                    string rowStr = "";
                    foreach (string colName in row.AllKeys)
                    {
                        if (rowStr.Length != 0)
                        {
                            rowStr += ", ";
                        }
                        rowStr += colName.ToString() + ": " + row[colName].ToString();
                    }
                    Console.WriteLine(rowStr);
                }
                Console.WriteLine($"Printing routes in district: {district}");*/
                foreach (var route in routes)
                {
                    int routeId = Convert.ToInt32(route["assigned_route_id"]);
                    Console.WriteLine($"Route id: {routeId}");
                    // Get sum of ridership for each route between reportRange for weekdays
                    // routeTotal contains nfc.total_ridership, nfc.total_nonridership, fc.boardings and total
                    NameValueCollection routeTotalWeek = databaseManager.GetRouteRidership(routeId, reportRange, true);

                    // Get sum of ridership for each route between reportRange for saturdays
                    NameValueCollection routeTotalSat = databaseManager.GetRouteRidership(routeId, reportRange, false);
                        /*string rowStr = "";
                        foreach (string colName in routeTotalSat.AllKeys)
                        {
                            if (rowStr.Length != 0)
                            {
                                rowStr += ", ";
                            }
                            rowStr += colName.ToString() + ": " + routeTotalSat[colName].ToString();
                        }
                        Console.WriteLine(rowStr);*/
                    weekRoutes.Add(routeId, routeTotalWeek);
                    satRoutes.Add(routeId, routeTotalSat);

                    // Num trips on normal weekdays
                    double numTripsWeek = Convert.ToDouble(route["num_trips_week"]) * weekdayCount;
                    Console.WriteLine($"\tNum trips on normal weekdays: {numTripsWeek}");
                    // Num trips on serviced holiday weekdays
                    double numTripsHolidaysW = Convert.ToDouble(route["num_trips_hol"]) * weekdayHolidayCount;
                    Console.WriteLine($"\tNum trips on serviced holiday weekdays: {numTripsHolidaysW}");
                    // Num trips on normal saturdays
                    double numTripsSat = Convert.ToDouble(route["num_trips_sat"]) * saturdayCount;
                    Console.WriteLine($"\tNum trips on normal saturdays: {numTripsSat}");
                    // Num trips on serviced holiday saturdays
                    double numTripsHolidaysS = Convert.ToDouble(route["num_trips_hol"]) * saturdayHolidayCount;
                    Console.WriteLine($"\tNum trips on holiday saturdays: {numTripsHolidaysS}");

                    // Get revenue miles for a route (distance of trip * num trips during week (regardless of holiday or not))
                    double routeDistance = Convert.ToDouble(route["distance"]);
                    double revenueMilesWeek = routeDistance * (numTripsWeek + numTripsHolidaysW);
                    Console.WriteLine($"\tRevenue miles weekdays: {revenueMilesWeek}");
                    double revenueMilesSat = routeDistance * (numTripsSat + numTripsHolidaysS);
                    Console.WriteLine($"\tRevenue miles saturdays: {revenueMilesSat}");
                    // Get revenue hours (num hours on weekday * number of weekdays excluding holidays)
                    double revenueHoursWeek = Convert.ToDouble(route["weekday_hours"]) * weekdayCount;
                    Console.WriteLine($"\tRevenue hours normal weekdays: {revenueHoursWeek}");
                    double revenueHoursHolidaysW = Convert.ToDouble(route["holiday_hours"]) * weekdayHolidayCount;
                    Console.WriteLine($"\tRevenue hours holiday weekdays: {revenueHoursHolidaysW}");
                    double revenueHoursSat = Convert.ToDouble(route["saturday_hours"]) * saturdayCount;
                    Console.WriteLine($"\tRevenue hours normal saturdays: {revenueHoursSat}");
                    double revenueHoursHolidaysS = Convert.ToDouble(route["holiday_hours"]) * saturdayHolidayCount;
                    Console.WriteLine($"\tRevenue hours holiday saturdays: {revenueHoursHolidaysS}");

                    // Get total ridership during weekdays and saturdays
                    string routeTotalWeekStr = routeTotalWeek["fc_boardings"];
                    int totalRidesWeek = 0;
                    if (!string.IsNullOrEmpty(routeTotalWeekStr))
                    {
                        totalRidesWeek = Convert.ToInt32(routeTotalWeekStr);
                    }
                    Console.WriteLine($"\tTotal ridership weekdays: {totalRidesWeek}");
                    string routeTotalSatStr = routeTotalSat["fc_boardings"];
                    int totalRidesSat = 0;
                    if (!string.IsNullOrEmpty(routeTotalSatStr))
                    {
                        totalRidesSat = Convert.ToInt32(routeTotalSatStr);
                    }
                    Console.WriteLine($"\tTotal ridership saturdays: {totalRidesSat}");

                    // Get passengers per mile (total passengers on weekdays / revenue miles)
                    double passPerMileW = totalRidesWeek / revenueMilesWeek;
                    Console.WriteLine($"\tPassengers per mile weekdays: {passPerMileW}");
                    double passPerMileS = totalRidesSat / revenueMilesSat;
                    Console.WriteLine($"\tPassengers per mile saturdays: {passPerMileS}");
                    // Get passengers per hour (using total passengers / revenue hours)
                    double passPerHourW = totalRidesWeek / (revenueHoursWeek + revenueHoursHolidaysW);
                    Console.WriteLine($"\tPassengers per hour weekdays: {passPerHourW}");
                    double passPerHourS = totalRidesSat / (revenueHoursSat + revenueHoursHolidaysS);
                    Console.WriteLine($"\tPassengers per hour saturdays: {passPerHourS}");


                }

            }
            
        }

        private int GetNumWeekdays(List<DateTime> reportRange)
        {
            int weekdayCount = 0;
            DateTime startDate = reportRange[0];
            DateTime endDate = reportRange[1];
            int totalDays = (int) (endDate - startDate).TotalDays + 1; // Add one so that we include the start date as a day
            int weekendStart = 0;
            double numWeekends = totalDays / 7.0;
            int numFullWeekends = (int) Math.Floor(numWeekends);
            if (numWeekends != numFullWeekends)
            {
                // If we haven't accounted for every pair of weekend days,
                // we need to set weekendStart appropriately
                if (startDate.DayOfWeek == DayOfWeek.Saturday)
                {
                    weekendStart = 2;
                }
                else if (startDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    weekendStart = 1;
                }
            }
            int numWeekendDays = (numFullWeekends * 2) + weekendStart;
            weekdayCount = totalDays - numWeekendDays;
            int weekdayHolidayCount = GetNumHolidays(reportRange, true, new List<int> { 1, 2 });
            // Subtract number of holidays occurring on weekdays within range
            weekdayCount -= weekdayHolidayCount;
            return weekdayCount;
        }

        private int GetNumSaturdays(List<DateTime> reportRange)
        {
            DateTime startDate = reportRange[0];
            DateTime endDate = reportRange[1];
            int totalDays = (int)(endDate - startDate).TotalDays + 1; // Add one so that we include the start date as a day
            int numFullWeekends = (int) Math.Floor(totalDays / 7.0);
            DayOfWeek lastAccountedDOW = endDate.AddDays(-numFullWeekends).DayOfWeek;

            int numSaturdays = numFullWeekends + (startDate.DayOfWeek <= DayOfWeek.Saturday && lastAccountedDOW >= DayOfWeek.Saturday ? 1 : 0);

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
            var dataPoints = new List<string>();
            foreach (var uiElem in DataPointCheckBoxes.Children)
            {
                if (uiElem.GetType() != typeof(StackPanel)) continue;

                foreach (CheckBox c in ((StackPanel)uiElem).Children)
                {
                    if (c != SelectAllDataPoints && c.IsChecked == true)
                    {
                        dataPoints.Add(c.Content.ToString());
                    }
                }
            }
            return dataPoints;
        }

        private List<string> GetSelectedDistricts()
        {
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
