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
    public partial class updateRouteInfo: Page
    {
        string selectedRoute = null;

        public updateRouteInfo()
        {
            InitializeComponent();
            
            DatabaseManager dbManager = DatabaseManager.GetDBManager();
            dbManager.viewRoutes();
            var routeList = dbManager.getRoutes();
            List<int> list = new List<int>();

            foreach (String all in routeList)
            {
                list.Add(Convert.ToInt32(all));
            }
            list.Sort();
            listRoutes.Items.Clear();
            foreach (int all in list)
            {
                listRoutes.Items.Add(all);
            }

            listAttributes.Items.Clear();
            listAttributes.Items.Add("path id");
            listAttributes.Items.Add("start date");
            listAttributes.Items.Add("assigned route id");
            listAttributes.Items.Add("route name");
            listAttributes.Items.Add("district");
            listAttributes.Items.Add("distance week");
            listAttributes.Items.Add("distance sat");
            listAttributes.Items.Add("number of trips per weekday");
            listAttributes.Items.Add("number of trips per saturday");
            listAttributes.Items.Add("number of trips per holiday");
            listAttributes.Items.Add("weekday hours");
            listAttributes.Items.Add("saturday hours");
            listAttributes.Items.Add("holiday hours");

        }

        private void update(object sender, RoutedEventArgs e)
        {
            if ((selectedRoute != null || listRoutes.SelectedItem != null) 
                && listAttributes.SelectedItem != null 
                && newField.Text != null)
            {
                if (listRoutes.SelectedItem != null)
                {
                    selectedRoute = listRoutes.SelectedItem.ToString();
                }
                string selectedAttribute = listAttributes.SelectedItem.ToString();
                string input = newField.Text;

                Console.WriteLine(selectedRoute);
                Console.WriteLine(selectedAttribute);
                Console.WriteLine(input);

                DatabaseManager dbManager = DatabaseManager.GetDBManager();
                dbManager.viewRoutes();

                dbManager.modifyRoute(selectedRoute, selectedAttribute, input);

                dbManager.viewRoutes();

                newField.Text = "";

                var routeList = dbManager.getRoutes();
                List<int> list = new List<int>();

                foreach (String all in routeList)
                {
                    list.Add(Convert.ToInt32(all));
                }
                list.Sort();
                listRoutes.Items.Clear();
                foreach (int all in list)
                {
                    listRoutes.Items.Add(all);
                }

            }

        }
    }
}

