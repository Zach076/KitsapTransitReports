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
        // Can make Reports a singleton
        public Reports()
        {
            InitializeComponent();
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
            DatabaseManager databaseManager = DatabaseManager.GetDBManager();
            // Get a list of Datapoints to include
            List<string> dataPoints = GetSelectedDataPoints();
            // Get a list of districts to include
            List<string> districts = GetSelectedDistricts();
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
                return;

            }
            // Get all routes per district
            var districtToRoutes = new Dictionary<string, List<NameValueCollection>>();
            foreach (var district in districts)
            {
                // Need to distinguish between weekday and non-weekday routes
                List<NameValueCollection> routes = databaseManager.GetDistrictRoutes(district, reportRange);
                districtToRoutes.Add(district, routes);
                /*foreach (var row in routes)
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
                }*/
                foreach (var route in routes)
                {
                    int routeId = Convert.ToInt32(route["route_id"]);
                    // Get sum of ridership for each route between reportRange for weekdays
                    // routeTotal contains nfc.total_ridership, nfc.total_nonridership, fc.boardings and total
                    List<NameValueCollection> routeTotalWeek = databaseManager.GetRouteRidership(routeId, reportRange, true);

                    // Get sum of ridership for each route between reportRange for saturdays
                    List<NameValueCollection> routeTotalSat = databaseManager.GetRouteRidership(routeId, reportRange, false);
                    // TODO: Store these lists or totals in association with their routes so that we can use them
                    // Could use a Dictionary that maps route id to these lists
                }

            }


            // When making queries for FC data and NFC data, modify startDate to be the first day of that month
            // and modify endDate to be the last date of that month because FC and NFC data are accumulated in months, not days


            // Make queries

            // Get total ridership from NFC and FC

            // Get num trips from date range

            // Get revenue miles for a route

            // Get revenue hours from db info and calendar

            // Get passengers per mile

            // Get passengers per hour (using total passengers / revenue hours)
        }

        private Boolean IsValidRange(List<DateTime> reportRange)
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

        // DateTime(Int32, Int32, Int32) Initializes a new instance of the DateTime structure to the specified year, month, and day.
        // Use DaysInMonth() for constructing the end DateTime
        // Get day of the week using DataTime property .DayOfWeek
        // Get a list of all holidays in a month
        // If holiday is on a weekday then decrement weekday count
        // If holiday is on a saturday then decrement saturday count


        // Get all districts

        // Get all routes in district

        // Get selected routes (all except what's unchecked)

        // Get total ridership from NFC and FC

        // Get num trips from date range and calendar

        // Get revenue miles for a route

        // Get revenue hours from db info and calendar

        // Get passengers per mile

        // Get passengers per hour (using total passengers / revenue hours)
    }
}
