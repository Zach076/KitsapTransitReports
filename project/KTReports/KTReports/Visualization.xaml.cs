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
            LiveCharts.SeriesCollection sc = new LiveCharts.SeriesCollection { };
            string[] Labels = dbManager.getRoutes().ToArray();



        }

    }
}

