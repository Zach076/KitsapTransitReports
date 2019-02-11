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
                    file.IsEnabled = true;
                    file.Visibility = Visibility.Visible;
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
                                textFile.AppendText(xlRange.Cells[i, j].Value2.ToString());
                                //dataGridView1.Rows[i - 1].Cells[j - 1].Value = xlRange.Cells[i, j].Value2.ToString();
                            }

                            /*
                            if (xlRange.Cells[i, j] != null && xlRange.Cells[i, j].Value2 != null)
                            {
                                //append to a string?
                            }
                            */
        }
        textFile.AppendText("\n");
        //send list to Database?
        }
        /*
        //cleanup  
        GC.Collect();
        GC.WaitForPendingFinalizers();
        //release com objects to fully kill excel process from running in the background
        X.ReleaseComObject(xlRange);
        X.ReleaseComObject(xlWorksheet);
        //close and release  
        xlWorkbook.Close();
        X.ReleaseComObject(xlWorkbook);
        //quit and release  
        xlApp.Quit();
        X.ReleaseComObject(xlApp);
        */
        }
        }
        }

       /* private void Import_File(object sender, RoutedEventArgs e)
        {
            String path;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel/CSV Files|*.xls;*.xlsx;*.xlsm;*.csv";
            openFileDialog.Title = "Select a file to import";
            openFileDialog.ShowDialog();
            path = openFileDialog.FileName;

            if(path.Length > 2)
            {
                file.IsEnabled = true;
                file.Visibility = Visibility.Visible;
                string text = System.IO.File.ReadAllText(@path);
                textFile.AppendText(text);
            }

            //string text = System.IO.File.ReadAllText(@path);
            //System.Console.WriteLine("Contents of file = {0}", text);
        }*/
        

        private void Report_Options(object sender, RoutedEventArgs e)
        {
            rOptions.IsEnabled = true;
            rOptions.Visibility = Visibility.Visible;
        } /*

        private void Generate_Report(object sender, RoutedEventArgs e)
        {
            rOptions.IsEnabled = false;
            rOptions.Visibility = Visibility.Hidden;
        }
        */
        private void Done_File(object sender, RoutedEventArgs e)
        {
            file.IsEnabled = false;
            file.Visibility = Visibility.Hidden;
        }
    }
}
