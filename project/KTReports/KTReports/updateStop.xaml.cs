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
    public partial class updateStop: Page
    {
        public updateStop()
        {
            InitializeComponent();

            DatabaseManager dbManager = DatabaseManager.GetDBManager();
            dbManager.viewRouteStops();

            var stopList = dbManager.getStops();

            listStops.Items.Clear();
            foreach (String all in stopList)
            {
                listStops.Items.Add(all);
            }

            listStopInfo.Items.Clear();
            listStopInfo.Items.Add("stop name");
            listStopInfo.Items.Add("location name");
            listStopInfo.Items.Add("location id");
            listStopInfo.Items.Add("stop id");
            listStopInfo.Items.Add("path id");
            listStopInfo.Items.Add("start date");
            listStopInfo.Items.Add("(-)door 1 person");
            listStopInfo.Items.Add("(-)door 2 person");
            listStopInfo.Items.Add("door 1 person");
            listStopInfo.Items.Add("door 2 person");
        }

        private void updateStopButton(object sender, RoutedEventArgs e)
        {
            if (listStops.SelectedItem != null && listStopInfo.SelectedItem != null && change.Text != null)
            {
                string selectedStop = listStops.SelectedItem.ToString();
                string selectedInfo = listStopInfo.SelectedItem.ToString();
                string input = change.Text;

                DatabaseManager dbManager = DatabaseManager.GetDBManager();
                dbManager.modifyStop(selectedStop, selectedInfo, input);
                dbManager.viewRouteStops();

                listStops.SelectedItem = -1;
                listStopInfo.SelectedItem = -1;
                change.Text = "";
            }

        }


    }
}

