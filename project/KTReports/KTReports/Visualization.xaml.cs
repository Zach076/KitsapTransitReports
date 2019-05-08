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
            int[] sortedRoutes = dbManager.getRoutes().ToArray();
            Array.Sort(sortedRoutes);
            int[] boardings = new int[sortedRoutes.Length];
            var range = dbManager.getRange();

            SeriesCollection = new LiveCharts.SeriesCollection{};

            for (int y = 0; y < range.Count-1; y++)
            {
                var reportRange = range[y];
                int month = (((int) reportRange[5] - 48) * 10) + ((int)reportRange[6] - 48);
                var endDate = reportRange.Remove(8,2);
                switch(month)
                {
                    case 2:
                        endDate = endDate + "28";
                        break;
                    case 4:
                        endDate = endDate + "30";
                        break;
                    case 6:
                        endDate = endDate + "30";
                        break;
                    case 9:
                        endDate = endDate + "30";
                        break;
                    case 11:
                        endDate = endDate + "30";
                        break;
                    default:
                        endDate = endDate + "31";
                        break;
                }

                List<DateTime> newRange = new List<DateTime>();
                newRange.Add(DateTime.ParseExact(reportRange, "yyyy-MM-dd", null));
                newRange.Add(DateTime.ParseExact(endDate, "yyyy-MM-dd", null));

                for (int i = 0; i < sortedRoutes.Length; i++)
                {
                    boardings[i] = dbManager.GetRouteRidership(sortedRoutes[i], newRange, true)["total"] + dbManager.GetRouteRidership(sortedRoutes[i], newRange, false)["total"];
                }

                SeriesCollection.Add(new StackedColumnSeries
                {
                    Title = range[y],
                    Values = new ChartValues<int> { } //Boardings
                });

                int numRoutes = sortedRoutes.Length;
                int j = 0;
                while (numRoutes != 0)
                {
                    SeriesCollection[y].Values.Add(boardings[j]);
                    j++;
                    numRoutes--;
                }

                string[] backString = sortedRoutes.Select(x => x.ToString()).ToArray();
                Labels = backString;
                Formatter = value => value.ToString("N");
            }

            DataContext = this;
        }

        public LiveCharts.SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> Formatter { get; set; }


    }
}

