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
            // Set the Reports page as content by default
            //Main.Content = new Reports();
        }

        private void CloseClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OpenReportsPage(object sender, RoutedEventArgs e)
        {
            // Set the content of the MainWindow to be the Reports page
            Main.Content = new Reports();
        }

        private void editPage(object sender, RoutedEventArgs e)
        {
            int num = TestDB.routes;
            listRoutes.Visibility = Visibility.Visible;
            
            DatabaseManager dbManager = DatabaseManager.GetDBManager();
            var routeList = dbManager.getRoutes();

            foreach (String all in routeList)
            {
                listRoutes.Items.Add(all);
            }

            listAttributes.Visibility = Visibility.Visible;
            listAttributes.Items.Add("route id");
            listAttributes.Items.Add("start date");
            listAttributes.Items.Add("end date");
            listAttributes.Items.Add("route name");
            listAttributes.Items.Add("district");
            listAttributes.Items.Add("distance");
            listAttributes.Items.Add("number of trips per week");
            listAttributes.Items.Add("number of saturday trips");
            listAttributes.Items.Add("number of holiday trips");
            listAttributes.Items.Add("weekday hours");
            listAttributes.Items.Add("saturday hours");
            listAttributes.Items.Add("holilday hours");

            updateButton.Visibility = Visibility.Visible;
            newField.Visibility = Visibility.Visible;

        }

        private void update(object sender, RoutedEventArgs e)
        {
            string selectedRoute = listRoutes.SelectedItem.ToString();
            string selectedAttribute = listAttributes.SelectedItem.ToString();
            string input = newField.Text;

            Console.WriteLine(selectedRoute);
            Console.WriteLine(selectedAttribute);
            Console.WriteLine(input);

            DatabaseManager dbManager = DatabaseManager.GetDBManager();
            dbManager.viewRoutes();

            dbManager.modifyRoute(selectedRoute, selectedAttribute, input);

            dbManager.viewRoutes();

            listRoutes.Visibility = Visibility.Hidden;
            listAttributes.Visibility = Visibility.Hidden;
            newField.Visibility = Visibility.Hidden;
            updateButton.Visibility = Visibility.Hidden;
        }

        [STAThread]
        private void ImportFile(object sender, RoutedEventArgs e)
        {
            string fileName = "";
            OpenFileDialog fileDia = new OpenFileDialog();
            fileDia.Filter = "Excel/CSV Files|*.xls;*.xlsx;*.xlsm;*.csv";
            fileDia.Title = "Select a file to import";
            fileDia.FilterIndex = 2;
            fileDia.ShowDialog();
            //fileDia.RestoreDirectory = true;
            fileName = fileDia.FileName;

            if (fileName.Length > 2)
            {

                Microsoft.Office.Interop.Excel.Application xlApp = new Microsoft.Office.Interop.Excel.Application();
                Microsoft.Office.Interop.Excel.Workbook xlWorkbook = xlApp.Workbooks.Open(fileName);
                int sheetCount = xlWorkbook.Sheets.Count;
                //Loop through each sheet in the file
                for (int sheetNum = 1; sheetNum <= sheetCount; sheetNum++)
                {

                    Microsoft.Office.Interop.Excel._Worksheet xlWorksheet = xlWorkbook.Sheets[sheetNum];
                    Microsoft.Office.Interop.Excel.Range xlRange = xlWorksheet.UsedRange;

                    int rowCount = xlRange.Rows.Count;
                    int colCount = xlRange.Columns.Count;

                    //dataGridView1.ColumnCount = colCount;
                    //dataGridView1.RowCount = rowCount;

                    for (int i = 1; i <= rowCount; i++)
                    {
                        for (int j = 1; j <= colCount; j++)
                        {
                            //write to a grid
                            if (xlRange.Cells[i, j] != null && xlRange.Cells[i, j].Value2 != null)
                            {
                                //string text = System.IO.File.ReadAllText(@fileName);

                                //dataGridView1.Rows[i - 1].Cells[j - 1].Value = xlRange.Cells[i, j].Value2.ToString();
                            }

                            /*
                            if (xlRange.Cells[i, j] != null && xlRange.Cells[i, j].Value2 != null)
                            {
                                //append to a string?
                            }
                            */

                        }
                        //send list to Database?
                    }
                }
            }
        }

        [STAThread]
        private void ImportRoutes(object sender, RoutedEventArgs e)
        {
            string fileName = "";
            OpenFileDialog fileDia = new OpenFileDialog();
            fileDia.Title = "Select a file to import";
            fileDia.FilterIndex = 2;
            fileDia.ShowDialog();
            //fileDia.RestoreDirectory = true;
            fileName = fileDia.FileName;

            //if (fileName.Length > 2)
            //{
             
            //}
        }

    }
}

