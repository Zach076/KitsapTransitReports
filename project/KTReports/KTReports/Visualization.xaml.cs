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


namespace KTReports
{
    public partial class Visualization : Page
    {

        public Visualization()
        {
            InitializeComponent();

            DatabaseManager dbManager = DatabaseManager.GetDBManager();
            string[] routes = dbManager.getRoutes().ToArray();
            int[] sortedRoutes = Array.ConvertAll(routes, s => int.Parse(s));
            Array.Sort(sortedRoutes);
            int[] boardings = new int[sortedRoutes.Length];
            var range = dbManager.getRange();

            for (int i =0; i < sortedRoutes.Length; i++)
            {
                int x = Int32.Parse(routes[i]);
                boardings[i] = dbManager.getBoardings(x); 
            }

            SeriesCollection = new LiveCharts.SeriesCollection
            {
                new ColumnSeries
                {
                    Title = range[0],
                    Values = new ChartValues<int> { } //Boardings
                }
            };
            int numRoutes = sortedRoutes.Length;
            int j = 0;
            while(numRoutes != 0)
            {
                SeriesCollection[0].Values.Add(boardings[j]);
                j++;
                numRoutes--;
            }

            string[] backString = sortedRoutes.Select(x => x.ToString()).ToArray();
            Labels = backString;
            Formatter = value => value.ToString("N");

            DataContext = this;
        }

        public LiveCharts.SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> Formatter { get; set; }


    }
}

