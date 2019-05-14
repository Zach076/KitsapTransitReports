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
        private Brush brush = null;
        string[] labelStrs = new string[1000];

        private Visualization()
        {
            InitializeComponent();
            SeriesCollection = new LiveCharts.SeriesCollection();
            PieChartCollection = new LiveCharts.SeriesCollection();
            var converter = new System.Windows.Media.BrushConverter();
            brush = (Brush)converter.ConvertFromString("#f27024");
            monthYearPicker.Value = DateTime.Now;
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
            InitializeChart(null, null);
        }

        private string GetVisType()
        {
            return (VisualizationType.SelectedItem as ComboBoxItem).Content.ToString().ToLower();
        }



        private void InitializeChart(object sender, RoutedEventArgs e)
        {
            if (SeriesCollection != null)
            {
                LVBarGraph.Visibility = Visibility.Collapsed;
                SeriesCollection.Clear();
            }
            if (PieChartCollection != null)
            {
                LVPieChart.Visibility = Visibility.Collapsed;
                PieChartCollection.Clear();
            }

            if (monthYearPicker.Value == null)
            {
                return;
            }
            var date = (DateTime)monthYearPicker.Value;
            if (date == null)
            {
                return;
            }
            var startDate = new DateTime(date.Year, date.Month, 1);
            var endDate = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
            var range = new List<DateTime>() { startDate, endDate };
            // Get a list of districts to include
            List<string> districts = GetSelectedDistricts();

            var sortedRoutes = new List<int>();
            foreach (var district in districts)
            {
                List<NameValueCollection> routes = databaseManager.GetDistrictRoutes(district, range);
                foreach (var route in routes)
                {
                    sortedRoutes.Add(int.Parse(route["assigned_route_id"]));
                }
            }
            sortedRoutes = sortedRoutes.Distinct().ToList();
            sortedRoutes.Sort();
            var boardings = new int[sortedRoutes.Count];

            for (int i = 0; i < sortedRoutes.Count; i++)
            {
                int routeId = sortedRoutes[i];
                boardings[i] = databaseManager.GetRouteRidership(routeId, range, true)["total"] + databaseManager.GetRouteRidership(routeId, range, false)["total"];
            }

            string visType = GetVisType();
            switch (visType)
            {
                case "bar graph":
                    SetBarGraph(range, boardings, sortedRoutes);
                    break;
                case "pie chart":
                    SetPieChart(range, boardings, sortedRoutes);
                    break;
                default:
                    SetBarGraph(range, boardings, sortedRoutes);
                    break;
            }
        }

        public void OnDistrictClick(object sender, RoutedEventArgs e)
        {
            CheckBox senderCheckBox = (CheckBox)sender;
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
            InitializeChart(null, null);
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

        public Func<LiveCharts.ChartPoint, string> PointLabel { get; set; }

        private void SetPieChart(List<DateTime> range, int[] boardings, List<int> sortedRoutes)
        {
            LVPieChart.Visibility = Visibility.Visible;
            CultureInfo cultureInfo = new CultureInfo("en-US");
            var month = range[0].ToString("MMM", cultureInfo);
            PointLabel = chartPoint =>
                string.Format("{0} ({1:P})", Title, chartPoint.Participation);
            for (int i = 0; i < boardings.Length; i++)
            {
                var boardingCount = boardings[i];
                string label = sortedRoutes[i].ToString();
                PieChartCollection.Add(new PieSeries
                {
                    Values = new ChartValues<int> { boardingCount },
                    Title = label,
                    LabelPoint = chartPoint =>
                string.Format("{0} ({1:P})", label, chartPoint.Participation),
                    DataLabels = true
                });
            }
        }

        private void SetBarGraph(List<DateTime> range, int[] boardings, List<int> sortedRoutes)
        {
            LVBarGraph.Visibility = Visibility.Visible;
            CultureInfo cultureInfo = new CultureInfo("en-US");
            var month = range[0].ToString("MMM", cultureInfo);
            SeriesCollection.Add(new StackedColumnSeries
            {
                Title = $"{month}, {range[0].Year}",
                Values = new ChartValues<int>(), //Boardings
                Fill = brush
            });
            foreach (var boardingCount in boardings)
            {
                SeriesCollection[0].Values.Add(boardingCount);
            }
            for (int i = 0; i < sortedRoutes.Count; i++)
            {
                labelStrs[i] = sortedRoutes[i].ToString();
            }
            //labelStrs = sortedRoutes.Select(x => x.ToString()).ToArray();
            foreach (var str in labelStrs)
            {
                Console.WriteLine(str);
            }
            Labels = labelStrs; 
            Formatter = value => value.ToString("N");
            DataContext = this;
        }

        public LiveCharts.SeriesCollection SeriesCollection { get; set; }
        public LiveCharts.SeriesCollection PieChartCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> Formatter { get; set; }


    }
}

