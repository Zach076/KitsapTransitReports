using System;
using System.Collections.Generic;
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
using System.IO;
using Microsoft.Win32;

namespace KTReports
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Close_Clicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        [STAThread]
        private void Import_File(object sender, RoutedEventArgs e)
        {
            String path;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel Files|*.xls;*.xlsx;*.xlsm|CSV files|*.csv";
            openFileDialog.Title = "Select a file to import";
            openFileDialog.ShowDialog();
            path = openFileDialog.FileName;
        }
        private void Report_Options(object sender, RoutedEventArgs e)
        {
            rOptions.IsEnabled = true;
            rOptions.Visibility = Visibility.Visible;
        }

        private void Generate_Report(object sender, RoutedEventArgs e)
        {
            rOptions.IsEnabled = false;
            rOptions.Visibility = Visibility.Hidden;
        }
    }
}
