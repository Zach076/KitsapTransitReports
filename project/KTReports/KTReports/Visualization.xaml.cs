using Microsoft.Office.Core;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LiveCharts;
using LiveCharts.Wpf;
using System.Globalization;

namespace KTReports
{
    public partial class Visualization : Page
    {
        private static Visualization visualizationInstance = null;
        private DatabaseManager databaseManager = DatabaseManager.GetDBManager();
        private int numVisualizations = 0;
        private Brush brush = null;

        private Visualization()
        {
            InitializeComponent();
            SeriesCollection = new LiveCharts.SeriesCollection();
            DataContext = this;
            monthYearPicker.Value = DateTime.Now;
            var converter = new System.Windows.Media.BrushConverter();
            brush = (Brush)converter.ConvertFromString("#f27024");
        }

        public static Visualization GetVisualizationInstance()
        {
            if (visualizationInstance == null)
            {
                visualizationInstance = new Visualization();
            }
            return visualizationInstance;
        }

        public void RefreshVisualization()
        {
            OnDateChange(null, null);
        } 

        private void OnDateChange(object sender, RoutedEventArgs e)
        {
            Interlocked.Increment(ref numVisualizations);
            MainWindow.progressBar.IsIndeterminate = true;
            MainWindow.statusTextBlock.Text = "Generating Visualization...";
            if (SeriesCollection != null) {
                SeriesCollection.Clear();
            }
            var date = (DateTime)monthYearPicker.Value;
            if (date == null)
            {
                return;
            }
            var startDate = new DateTime(date.Year, date.Month, 1);
            var endDate = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
            var range = new List<DateTime>() { startDate, endDate };
            List<NameValueCollection> sortedRoutes = databaseManager.GetRoutesInRange(range);

            var boardings = new int[sortedRoutes.Count];

            for (int i = 0; i < sortedRoutes.Count; i++)
            {
                int routeId = int.Parse(sortedRoutes[i]["assigned_route_id"]);
                boardings[i] = databaseManager.GetRouteRidership(routeId, range, true)["total"] + databaseManager.GetRouteRidership(routeId, range, false)["total"];
            }
            CultureInfo cultureInfo = new CultureInfo("en-US");
            var month = range[0].ToString("MMM", cultureInfo);
            SeriesCollection.Add(new StackedColumnSeries
            {
                Title = $"{month}, {range[0].Year}",
                Values = new ChartValues<int>(), //Boardings
                Fill = brush
            });
            var labels = new List<string>();
            foreach (var boardingCount in boardings)
            {
                SeriesCollection[0].Values.Add(boardingCount);
            }

            string[] backString = sortedRoutes.Select(x => x["assigned_route_id"].ToString()).ToArray();
            Labels = backString;
            Formatter = value => value.ToString("N");

            Interlocked.Decrement(ref numVisualizations);
            if (numVisualizations == 0)
            {
                MainWindow.progressBar.IsIndeterminate = false;
                MainWindow.statusTextBlock.Text = string.Empty;
            }
        }

        public LiveCharts.SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> Formatter { get; set; }


    }
}

