﻿using System;
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
            var weekRoutes = new Dictionary<int, Dictionary<string, int>>();
            var satRoutes = new Dictionary<int, Dictionary<string, int>>();
            foreach (var district in districts)
            {
                // Need to distinguish between weekday and non-weekday routes
                List<NameValueCollection> routes = databaseManager.GetDistrictRoutes(district, reportRange);
                districtToRoutes.Add(district, routes);
                Console.WriteLine($"Printing routes in district: {district}");
                foreach (var route in routes)
                {
                    int routeId = Convert.ToInt32(route["assigned_route_id"]);
                    Console.WriteLine($"\tRoute id: {routeId}");
                    // Get sum of ridership for each route between reportRange for weekdays
                    // routeTotal contains nfc.total_ridership, nfc.total_nonridership, fc.boardings and total
                    Dictionary<string, int> routeTotalWeek = databaseManager.GetRouteRidership(routeId, reportRange, true);

                    // Get sum of ridership for each route between reportRange for saturdays
                    Dictionary<string, int> routeTotalSat = databaseManager.GetRouteRidership(routeId, reportRange, false);

                    weekRoutes.Add(routeId, routeTotalWeek);
                    satRoutes.Add(routeId, routeTotalSat);

                    // Num trips on normal weekdays
                    double numTripsWeek = Convert.ToDouble(route["num_trips_week"]) * weekdayCount;
                    Console.WriteLine($"\t\tNum trips on normal weekdays: {numTripsWeek}");

                    // Num trips on serviced holiday weekdays
                    double numTripsHolidaysW = Convert.ToDouble(route["num_trips_hol"]) * weekdayHolidayCount;
                    Console.WriteLine($"\t\tNum trips on serviced holiday weekdays: {numTripsHolidaysW}");

                    // Num trips on normal saturdays
                    double numTripsSat = Convert.ToDouble(route["num_trips_sat"]) * saturdayCount;
                    Console.WriteLine($"\t\tNum trips on normal saturdays: {numTripsSat}");

                    // Num trips on serviced holiday saturdays
                    double numTripsHolidaysS = Convert.ToDouble(route["num_trips_hol"]) * saturdayHolidayCount;
                    Console.WriteLine($"\t\tNum trips on holiday saturdays: {numTripsHolidaysS}");

                    // Get revenue miles for a route (distance of trip * num trips during week (regardless of holiday or not))
                    double routeDistance = Convert.ToDouble(route["distance"]);
                    double revenueMilesWeek = routeDistance * (numTripsWeek + numTripsHolidaysW);
                    Console.WriteLine($"\t\tRevenue miles weekdays: {revenueMilesWeek}");
                    double revenueMilesSat = routeDistance * (numTripsSat + numTripsHolidaysS);
                    Console.WriteLine($"\t\tRevenue miles saturdays: {revenueMilesSat}");

                    // Get revenue hours (num hours on weekday * number of weekdays excluding holidays)
                    double revenueHoursWeek = Convert.ToDouble(route["weekday_hours"]) * weekdayCount;
                    Console.WriteLine($"\t\tRevenue hours normal weekdays: {revenueHoursWeek}");
                    double revenueHoursHolidaysW = Convert.ToDouble(route["holiday_hours"]) * weekdayHolidayCount;
                    Console.WriteLine($"\t\tRevenue hours holiday weekdays: {revenueHoursHolidaysW}");
                    double revenueHoursSat = Convert.ToDouble(route["saturday_hours"]) * saturdayCount;
                    Console.WriteLine($"\t\tRevenue hours normal saturdays: {revenueHoursSat}");
                    double revenueHoursHolidaysS = Convert.ToDouble(route["holiday_hours"]) * saturdayHolidayCount;
                    Console.WriteLine($"\t\tRevenue hours holiday saturdays: {revenueHoursHolidaysS}");

                    // Get total ridership during weekdays and saturdays
                    int totalRidesWeek = routeTotalWeek["total"];
                    Console.WriteLine($"\t\tTotal ridership weekdays: {totalRidesWeek}");
                    int totalRidesSat = routeTotalSat["total"];
                    Console.WriteLine($"\t\tTotal ridership saturdays: {totalRidesSat}");

                    // Get passengers per mile (total passengers on weekdays / revenue miles)
                    double passPerMileW = totalRidesWeek / revenueMilesWeek;
                    Console.WriteLine($"\t\tPassengers per mile weekdays: {passPerMileW}");
                    double passPerMileS = totalRidesSat / revenueMilesSat;
                    Console.WriteLine($"\t\tPassengers per mile saturdays: {passPerMileS}");

                    // Get passengers per hour (using total passengers / revenue hours)
                    double passPerHourW = totalRidesWeek / (revenueHoursWeek + revenueHoursHolidaysW);
                    Console.WriteLine($"\t\tPassengers per hour weekdays: {passPerHourW}");
                    double passPerHourS = totalRidesSat / (revenueHoursSat + revenueHoursHolidaysS);
                    Console.WriteLine($"\t\tPassengers per hour saturdays: {passPerHourS}");
                }
            }
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
                        dataPoints.Add(c.Content.ToString());
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