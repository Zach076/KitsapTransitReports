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
    public partial class addStop: Page
    {
        public addStop()
        {
            InitializeComponent();
            
        }

        private void addStopButton(object sender, RoutedEventArgs e)
        {

            String stopName = stopNameTextBox.Text;
            String locationName = locationNameTextBox.Text;
            String locationId = locationIdTextBox.Text;
            String stopId = stopIdTextBox.Text;
            String pathId = pathIdTextBox.Text;
            String startDate = startDateTextBox.Text;
            String minusDoor1 = minusdoor1TextBox.Text;
            String minusDoor2 = minusdoor2personTextBox.Text;
            String door1 = door1TextBox.Text;
            String door2 = door2TextBox.Text;

            DatabaseManager dbManager = DatabaseManager.GetDBManager();
            dbManager.viewRouteStops();
            if(locationId.Length > 0)
            {
                dbManager.addStop(stopName, locationName, locationId, stopId, pathId, startDate, minusDoor1, minusDoor2,
                door1, door2);
                dbManager.viewRouteStops();

                stopNameTextBox.Text = "";
                locationNameTextBox.Text = "";
                locationIdTextBox.Text = "";
                stopIdTextBox.Text = "";
                pathIdTextBox.Text = "";
                startDateTextBox.Text = "";
                minusdoor1TextBox.Text = "";
                minusdoor2personTextBox.Text = "";
                door1TextBox.Text = "";
                door2TextBox.Text = "";
            }
        }
    }
}

