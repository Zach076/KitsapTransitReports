using System;
using System.Collections.Generic;
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
        DateTime startDate;
        DateTime endDate;

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
            // Get a list of Datapoints to include

            // Get a list of districts to include

            // Get the start and end dates
            // Validate the start and end dates

            // Make queries
        }

        // Get date range
        DateTime[] GetDateRange()
        {
            return new DateTime[] { startDate, endDate };
        }

        // Set date range (check if range is accurate)
        void SetDateRange()
        {
            
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
