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
    public partial class deleteRoute: Page
    {
        public deleteRoute()
        {
            InitializeComponent();
            DatabaseManager dbManager = DatabaseManager.GetDBManager();
            dbManager.viewRoutes();

            var routeList = dbManager.getRoutes();


            routes.Items.Clear();
            foreach (string all in routeList)
            {
                routes.Items.Add(all);
            }
        }

        private void deleteRoutebutton(object sender, RoutedEventArgs e)
        {
            if (routes.SelectedItem != null)
            {
                DatabaseManager dbManager = DatabaseManager.GetDBManager();
                string selectedRoute = routes.SelectedItem.ToString();
                dbManager.deleteRouteinfo(selectedRoute);
                dbManager.viewRoutes();

                var routeList = dbManager.getRoutes();

                routes.Items.Clear();
                foreach (string all in routeList)
                {
                    routes.Items.Add(all);
                }
            }

        }

        private void deleteAllRoutes(object sender, RoutedEventArgs e)
        {
            DatabaseManager dbManager = DatabaseManager.GetDBManager();
            dbManager.deleteAllRouteinfo();

            var routeList = dbManager.getRoutes();

            routes.Items.Clear();
            foreach (string all in routeList)
            {
                routes.Items.Add(all);
            }
        }


    }
}

