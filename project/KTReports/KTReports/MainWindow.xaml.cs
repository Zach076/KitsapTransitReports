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
            //Main.Content = new Reports();
        }

        private void CloseClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OpenReportsPage(object sender, RoutedEventArgs e)
        {
            Main.Content = new Reports();
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
    
    }
}
