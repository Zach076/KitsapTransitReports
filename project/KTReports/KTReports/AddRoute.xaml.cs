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
    public partial class AddRoute: Page
    {
        public AddRoute()
        {
            InitializeComponent();
        }

        private void AddRouteButton(object sender, RoutedEventArgs e)
        {
            string routeID = routeIDtextbox.Text;
            string start = startTextBox.Text;
            string name = routeNameTextBox.Text;
            string district = districtTextBox.Text;
            string distanceWeek = distanceWeekTextBox.Text;
            string distanceSat = distanceSatTextBox.Text;
            string tripsWeek = tripsWeekTextBox.Text;
            string tripsSat = tripsSaturdayTextBox.Text;
            string tripsHol = tripsHolidayTextBox.Text;
            string weekdayHours = weekdayHoursTextBox.Text;
            string satHours = saturdayHoursTextBox.Text;
            string holHours = holidayHoursTextBox.Text;

            DatabaseManager dbManager = DatabaseManager.GetDBManager();
            dbManager.addRouteinfo(routeID, start, name, district, distanceWeek, distanceSat, tripsWeek,
                tripsSat, tripsHol, weekdayHours, satHours, holHours);
            dbManager.viewRoutes();

            routeIDtextbox.Text = "";
            startTextBox.Text = "";
            routeNameTextBox.Text = "";
            districtTextBox.Text = "";
            distanceWeekTextBox.Text = "";
            distanceSatTextBox.Text = "";
            tripsWeekTextBox.Text = "";
            tripsSaturdayTextBox.Text = "";
            tripsHolidayTextBox.Text = "";
            weekdayHoursTextBox.Text = "";
            saturdayHoursTextBox.Text = "";
            holidayHoursTextBox.Text = "";
        }
    }
}

