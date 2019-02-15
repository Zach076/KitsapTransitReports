using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Text.RegularExpressions;
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
using System.Diagnostics;

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
            FileInfo fileInfo = new FileInfo(fileName);
            //TODO: use PST unstead of UTC?
            DateTime dateTime = DateTime.UtcNow.Date;
            Dictionary<string, string> dict = new Dictionary<string, string>();
            LinkedList<string> colNames = new LinkedList<string>();
            DatabaseManager databaseManager = DatabaseManager.GetDBManager();
            long? file_id = 0;
            bool isORCA = false;
            string isWeekday = "false";
            string[] reportPeriod;

            if (fileName.Length > 2)
            {
                Microsoft.Office.Interop.Excel.Application xlApp = new Microsoft.Office.Interop.Excel.Application();
                Microsoft.Office.Interop.Excel.Workbook xlWorkbook = xlApp.Workbooks.Open(fileName);
                if (Regex.Match(xlWorkbook.Name, @".*ORCA.*") != null)
                {
                    isORCA = true;
                }
                int sheetCount = xlWorkbook.Sheets.Count;
                //Loop through each sheet in the file
                for (int sheetNum = 1; sheetNum <= sheetCount; sheetNum++)
                {
                    Microsoft.Office.Interop.Excel._Worksheet xlWorksheet = xlWorkbook.Sheets[sheetNum];
                    //ignore sheet if it's a summary sheet
                    if (!Regex.Match(xlWorksheet.Name, @".*TOTAL.*").Success)
                    {
                        if (!Regex.Match(xlWorksheet.Name, @".*SAT.*").Success)
                        {
                            isWeekday = "true";
                        }
                        Microsoft.Office.Interop.Excel.Range xlRange = xlWorksheet.UsedRange;

                        int rowCount = xlRange.Rows.Count;
                        int colCount = xlRange.Columns.Count;
                        int colLim = 2;
                        int rowLim = rowCount;
                        int i = 1;
                        int j = 1;
                        LinkedListNode<string> key = colNames.First;
                        string reportVal = xlRange.Cells[1, 6].Value2;
                        reportPeriod = reportVal.Split(' ');

                        //0 means hasnt hit table, 1 means reading columns, 2 means hit end of columns, 3 means just found column names
                        int inTable = 0;

                        while (i < rowLim)
                        {
                            while (j < colLim)
                            {
                                switch (inTable)
                                {
                                    case 2:
                                        if (xlRange.Cells[i, j] != null && xlRange.Cells[i, j].Value2 != null && !Regex.Match(xlRange.Cells[i, j].Value2.ToString(), @".*Subtotal.*").Success && !Regex.Match(xlRange.Cells[i, j].Value2.ToString(), @".*Page.*").Success)
                                        {
                                            dict.Add(key.Value, xlRange.Cells[i, j].Value2.ToString());
                                            key = key.Next;
                                        }
                                        else if (xlRange.Cells[i, j] != null && xlRange.Cells[i, j].Value2 != null && Regex.Match(xlRange.Cells[i, j].Value2.ToString(), @".*Subtotal.*").Success)
                                        {
                                            // look x rows ahead for more then end while Loop if empty
                                            i = i + 2;
                                            j = colLim;
                                        }
                                        else
                                        {
                                            rowLim = i;
                                        }
                                        break;
                                    case 1:
                                        if (xlRange.Cells[i, j] == null || xlRange.Cells[i, j].Value2 == null)
                                        {
                                            colLim = j;
                                            inTable = 3;
                                        }
                                        else
                                        {
                                            string value = xlRange.Cells[i, j].Value2.ToString();
                                            value = value.ToLower();
                                            value = value.Replace(' ', '_');
                                            colNames.AddLast(value);
                                        }
                                        break;
                                    case 0:
                                        if (xlRange.Cells[i, j] != null && xlRange.Cells[i, j].Value2 != null)
                                        {
                                            if (Regex.Match(xlRange.Cells[i, j].Value2.ToString(), @".*Transit.*Operator.*").Success || Regex.Match(xlRange.Cells[i, j].Value2.ToString(), @".*Route.*ID.*").Success)
                                            {
                                                inTable = 1;
                                                colLim = colCount;
                                                string value = xlRange.Cells[i, j].Value2.ToString();
                                                value = value.ToLower();
                                                value = value.Replace(' ', '_');
                                                colNames.AddLast(value);
                                            }
                                        }
                                        break;
                                }
                                j++;
                            }

                            if (inTable == 2)
                            {
                                dict.Add("start_date", reportPeriod[0]);
                                dict.Add("end_date", reportPeriod[2]);
                                dict.Add("is_weekday", isWeekday);

                                Debug.WriteLine("insert");
                                if (isORCA)
                                {
                                    file_id = databaseManager.InsertNewFile(fileName, fileInfo.FullName, DatabaseManager.FileType.FC, dateTime.ToString("MM/dd/yyyy"));
                                    dict.Add("file_id", file_id.ToString());
                                    databaseManager.InsertFCD(dict);
                                }
                                else
                                {
                                    file_id = databaseManager.InsertNewFile(fileName, fileInfo.FullName, DatabaseManager.FileType.NFC, dateTime.ToString("MM/dd/yyyy"));
                                    dict.Add("file_id", file_id.ToString());
                                    databaseManager.InsertNFC(dict);
                                }

                                dict.Clear();
                                key = colNames.First;
                            }
                            else if (inTable == 3)
                            {
                                inTable = 2;
                                key = colNames.First;
                            }
                            j = 1;
                            i++;
                        }

                        /*
                        //cleanup
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        //release com objects to fully kill excel process from running in the background
                        Marshal.ReleaseComObject(xlRange);
                        Marshal.ReleaseComObject(xlWorksheet);
                        //close and release
                        xlWorkbook.Close();
                        Marshal.ReleaseComObject(xlWorkbook);
                        //quit and release
                        xlApp.Quit();
                        Marshal.ReleaseComObject(xlApp);
                        */
                    }
                }
            }
        }

        private void CreateReport(object sender, RoutedEventArgs e)
        {
            var excel = new Microsoft.Office.Interop.Excel.Application();
            excel.Visible = false;
            excel.DisplayAlerts = false;
            var xlWorkbook = excel.Workbooks.Add(Type.Missing);

            var xlWorksheet = (Microsoft.Office.Interop.Excel.Worksheet)xlWorkbook.ActiveSheet;
            xlWorksheet.Name = "Report";

            /*
            xlWorksheet.Range[xlWorksheet.Cells[1, 1], xlWorksheet.Cells[1, 8]].Merge();
            xlWorksheet.Cells[1, 1] = "Report Name";
            xlWorksheet.Cells.Font.Size = 15;
            */

            int rowcount = 2;
            bool done = false;
            while (!done)
            {
                //Add a line with a query
                rowcount = rowcount + 1;
            }

            xlWorkbook.SaveAs();
            xlWorkbook.Close();
            excel.Quit();
        }
    }
}
