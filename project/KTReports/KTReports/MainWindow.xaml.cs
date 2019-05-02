
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
using System.Threading;
using System.Windows.Controls.Primitives;
using System.Web.Script.Serialization;

namespace KTReports
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int numFilesImporting = 0;
        public static ProgressBar progressBar = null;
        public static TextBlock statusTextBlock = null;
        public MainWindow()
        {
            InitializeComponent();
            // Set the Delete Imports page as default
            Main.Content = DeleteImports.GetDeleteImports();
            progressBar = KTProgressBar;
            statusTextBlock = StatusBarText;
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

        private void OpenDeleteFiles(object sender, RoutedEventArgs e)
        {
            // Set the content of the MainWindow to be the Reports page
            Main.Content = DeleteImports.GetDeleteImports();
        }

        private void OpenManualAddData(object sender, RoutedEventArgs e)
        {
            Main.Content = new ManualDataEntry();
        }

        private void OpenUpdateRoutes(object sender, RoutedEventArgs e)
        {
            Main.Content = new UpdateRoutes();
        }

        private void updateStop(object sender, RoutedEventArgs e)
        {
            Main.Content = new updateStop();
        }

        private void addStop(object sender, RoutedEventArgs e)
        {
            Main.Content = new AddStop();
        }
        private void visualizeData(object sender, RoutedEventArgs e)
        {
            Main.Content = new Visualization();
        }

        private void OnSizeChanged(object sender, RoutedEventArgs e)
        {
            if (Main.Content is UpdateRoutes)
            {
                var updateRoutesPage = Main.Content as UpdateRoutes;
                updateRoutesPage.dataGrid.MaxHeight = ActualHeight - 180;
            }
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
            var thread = new System.Threading.Thread(()=>ThreadParseData(fileName));
            thread.Start();
        }

        private void ThreadParseData(string fileName) 
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }
            Interlocked.Increment(ref numFilesImporting);
            Dispatcher.Invoke(() => {
                KTProgressBar.IsIndeterminate = true;
                StatusBarText.Text = "Importing...";
            });
            try
            {
                if (ParseFileData(fileName))
                {
                    DeleteImports deleteImports = DeleteImports.GetDeleteImports();
                    deleteImports.SetupPage();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                MessageBox.Show($"Unable to import {fileName}", "Import File Error", MessageBoxButton.OK, MessageBoxImage.Error);
            } finally
            {
                Interlocked.Decrement(ref numFilesImporting);
            }
            if (numFilesImporting == 0)
            {
                Dispatcher.Invoke(() => {
                    KTProgressBar.IsIndeterminate = false;
                    StatusBarText.Text = string.Empty;
                });
            } 
        }

        private bool ParseFileData(string fileName)
        {
            DateTime dateTime = DateTime.Now;
            LinkedList<string> colNames = new LinkedList<string>();
            DatabaseManager databaseManager = DatabaseManager.GetDBManager();
            long? file_id = 0;
            bool isORCA = false;
            bool isWeekday = false;
            Microsoft.Office.Interop.Excel.Application xlApp = new Microsoft.Office.Interop.Excel.Application();
            Microsoft.Office.Interop.Excel.Workbook xlWorkbook = xlApp.Workbooks.Open(fileName);
            Console.WriteLine("FILE NAME: " + fileName);
            if (Regex.Match(xlWorkbook.Name, @".*ORCA.*").Success)
            {
                isORCA = true;
                file_id = databaseManager.InsertNewFile(System.IO.Path.GetFileNameWithoutExtension(fileName),
                    fileName, DatabaseManager.FileType.FC, dateTime.ToString("yyyy-MM-dd"));
            }
            else
            {
                file_id = databaseManager.InsertNewFile(System.IO.Path.GetFileNameWithoutExtension(fileName),
                    fileName, DatabaseManager.FileType.NFC, dateTime.ToString("yyyy-MM-dd"));
            }
            int sheetCount = xlWorkbook.Sheets.Count;
            //Loop through each sheet in the file
            var bulkData = new List<Dictionary<string, string>>();
            for (int sheetNum = 1; sheetNum <= sheetCount; sheetNum++)
            {
                Microsoft.Office.Interop.Excel._Worksheet xlWorksheet = xlWorkbook.Sheets[sheetNum];
                //ignore sheet if it's a summary sheet
                if (!Regex.Match(xlWorksheet.Name, @".*TOTAL.*").Success)
                {
                    if (!Regex.Match(xlWorksheet.Name, @".*SAT.*").Success)
                    {
                        isWeekday = true;
                    } else
                    {
                        isWeekday = false;
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
                    string[] reportPeriod = reportVal.Split(' ');

                    //0 means hasnt hit table, 1 means reading columns, 2 means hit end of columns
                    int inTable = 0;

                    while (i <= rowLim)
                    {
                        Dictionary<string, string> dict = new Dictionary<string, string>();
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
                            dict.Add("start_date", DateTime.Parse(reportPeriod[0]).ToString("yyyy-MM-dd"));
                            dict.Add("end_date", DateTime.Parse(reportPeriod[2]).ToString("yyyy-MM-dd"));
                            dict.Add("is_weekday", isWeekday.ToString());

                            dict.Add("file_id", file_id.ToString());
                            //Debug.WriteLine("insert");
                            bulkData.Add(dict);
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
            if (isORCA)
            {
                databaseManager.InsertBulkFCD(bulkData);
                ImportKnownRoutes();
            }
            else
            {
                databaseManager.InsertBulkNFC(bulkData);
                ImportKnownRoutesNFC();
            }
            //cleanup workbook
            //close and release
            xlWorkbook.Close(false);
            Marshal.ReleaseComObject(xlWorkbook);
            //quit and release
            xlApp.Quit();
            Marshal.ReleaseComObject(xlApp);
            return true;
        }

        private void ImportRoutes(object sender, RoutedEventArgs e)
        {
            var importRoutesDialog = new OpenFileDialog();
            importRoutesDialog.Filter = "Json files | *.json";
            importRoutesDialog.ShowDialog();
            if (string.IsNullOrEmpty(importRoutesDialog.FileName))
            {
                return;
            }
            var databaseManager = DatabaseManager.GetDBManager();
            KTProgressBar.IsIndeterminate = true;
            StatusBarText.Text = "Importing Routes...";
            try
            {
                var thread = new System.Threading.Thread(delegate() {
                    var bulkPaths = new List<Dictionary<string, string>>();
                    var reader = new StreamReader(importRoutesDialog.FileName);
                    string json = reader.ReadToEnd();
                    var jss = new JavaScriptSerializer();
                    var routes = jss.Deserialize<List<Dictionary<string, string>>>(json);
                    foreach (var route in routes)
                    {
                        Console.WriteLine($"Route name: {route["route_name"]}");
                        bulkPaths.Add(route);
                    }
                    reader.Dispose();
                    databaseManager.InsertBulkPaths(bulkPaths);
                    Dispatcher.Invoke(() =>
                    {
                        KTProgressBar.IsIndeterminate = false;
                        StatusBarText.Text = string.Empty;
                        MessageBox.Show($"Successfully imported routes from {importRoutesDialog.FileName}", "Import Routes Successful", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    });
                });
                thread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open {importRoutesDialog.FileName}", "Import Routes Error", MessageBoxButton.OK, MessageBoxImage.Error);
                KTProgressBar.IsIndeterminate = false;
                StatusBarText.Text = string.Empty;
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
