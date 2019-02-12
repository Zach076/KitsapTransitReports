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
        public Reports()
        {
            InitializeComponent();
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
