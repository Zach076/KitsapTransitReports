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
using System.Runtime.InteropServices;


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
            Main.Content = new updateRouteInfo();
        }

        private void addRoute(object sender, RoutedEventArgs e)
        {
            Main.Content = new addRoute();
        }

        private void deleteRoute(object sender, RoutedEventArgs e)
        {
            Main.Content = new deleteRoute();
        }

        private void updateStop(object sender, RoutedEventArgs e)
        {
            Main.Content = new updateStop();
        }

        private void addStop(object sender, RoutedEventArgs e)
        {
            Main.Content = new addStop();
        }

        [STAThread]
        private void ImportRoutes(object sender, RoutedEventArgs e)
        {
            string fileName = "";
            OpenFileDialog fileDia = new OpenFileDialog();
            fileDia.Title = "Select a file to import";
            fileDia.FilterIndex = 2;
            fileDia.ShowDialog();
            fileName = fileDia.FileName;
            
        }

        private void ImportKnownRoutes()
        {
            DatabaseManager databaseManager = DatabaseManager.GetDBManager();
            databaseManager.viewFCD();
            databaseManager.getFCDRoutes();
        }

        private void ImportKnownRoutesNFC()
        {
            DatabaseManager databaseManager = DatabaseManager.GetDBManager();
            databaseManager.viewNFC();
            databaseManager.getNFCRoutes();
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
            //Console.WriteLine(fileName);
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
                if (Regex.Match(xlWorkbook.Name, @".*ORCA.*").Success)
                {
                    isORCA = true;
                    file_id = databaseManager.InsertNewFile(fileName, fileInfo.FullName, DatabaseManager.FileType.FC, dateTime.ToString("yyyy-MM-dd"));
                }
                else
                {
                    file_id = databaseManager.InsertNewFile(fileName, fileInfo.FullName, DatabaseManager.FileType.NFC, dateTime.ToString("yyyy-MM-dd"));
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
                        object[,] values = (object[,])xlRange.Value2;

                        //cleanup worksheet
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        //release com objects to fully kill excel process from running in the background
                        Marshal.ReleaseComObject(xlRange);
                        Marshal.ReleaseComObject(xlWorksheet);

                        //Debug.WriteLine(rowCount);
                        //Debug.WriteLine(colCount);

                        int colLim = 1;
                        int rowLim = rowCount;
                        int i = 1;
                        int j = 1;
                        LinkedListNode<string> key = colNames.First;
                        string reportVal = values[1, 6].ToString();
                        reportPeriod = reportVal.Split(' ');

                        //0 means hasnt hit table, 1 means reading columns, 2 means hit end of columns
                        int inTable = 0;

                        while (i <= rowLim)
                        {
                            while (j <= colLim)
                            {
                                //Debug.WriteLine("i:" + i + " j:" + j);
                                switch (inTable)
                                {
                                    case 2:
                                        //Debug.WriteLine("case 2:");
                                        if (values[i, j] != null && !Regex.Match(values[i, j].ToString(), @".*Subtotal.*").Success && !Regex.Match(values[i, j].ToString(), @".*Page.*").Success)
                                        {
                                            //Debug.WriteLine("Key: " + key.Value);
                                            dict.Add(key.Value, values[i, j].ToString());
                                            key = key.Next;
                                        }
                                        else if (values[i, j] != null && Regex.Match(values[i, j].ToString(), @".*Subtotal.*").Success)
                                        {
                                            //Debug.WriteLine("subtotal");
                                            // look x rows ahead for more then end while Loop if empty
                                            i = i + 2;
                                            j = colLim;
                                            inTable = 1;
                                        }
                                        else
                                        {
                                            j = colLim;
                                            inTable = 1;
                                        }
                                        break;
                                    case 1:
                                        //Debug.WriteLine("case 1:");
                                        if (values[i, j] == null)
                                        {
                                            colLim = j - 1;
                                            inTable = 1;
                                        }
                                        else
                                        {
                                            string value = values[i, j].ToString();
                                            value = value.ToLower();
                                            value = value.Replace(' ', '_');
                                            value = value.Replace('\n', '_');
                                            value = value.TrimStart('_');
                                            //Debug.WriteLine("newKey: " + value);
                                            colNames.AddLast(value);
                                        }
                                        break;
                                    case 0:
                                        //Debug.WriteLine("case 0:");
                                        if (values[i, j] != null)
                                        {
                                            if (Regex.Match(values[i, j].ToString(), @".*Transit.*Operator.*").Success || Regex.Match(values[i, j].ToString(), @".*Route.*ID.*").Success)
                                            {
                                                inTable = 1;
                                                colLim = colCount;
                                                string value = values[i, j].ToString();
                                                value = value.ToLower();
                                                value = value.Replace(' ', '_');
                                                value = value.Replace('\n', '_');
                                                value = value.TrimStart('_');
                                                //Debug.WriteLine("newKey: " + value);
                                                colNames.AddLast(value);
                                            }
                                        }
                                        break;
                                }
                                j++;
                            }

                            //Debug.WriteLine("inTable = " + inTable);
                            if (inTable == 2)
                            {
                                dict.Add("start_date", reportPeriod[0]);
                                dict.Add("end_date", reportPeriod[2]);
                                dict.Add("is_weekday", isWeekday);
                                dict.Add("file_id", file_id.ToString());
                                //Debug.WriteLine("insert");
                                if (isORCA)
                                {
                                    databaseManager.InsertFCD(dict);
                                }
                                else
                                {
                                    databaseManager.InsertNFC(dict);
                                }

                                dict.Clear();
                                key = colNames.First;
                            }
                            else if (inTable == 1)
                            {
                                inTable = 2;
                                key = colNames.First;
                            }
                            j = 1;
                            i++;
                        }
                    }
                }

                //cleanup workbook
                //close and release
                xlWorkbook.Close(false);
                Marshal.ReleaseComObject(xlWorkbook);
                //quit and release
                xlApp.Quit();
                Marshal.ReleaseComObject(xlApp);
                if (isORCA)
                {
                    ImportKnownRoutes();
                }
                else
                {
                    ImportKnownRoutesNFC();
                }
                

            }
        }
    }
}

