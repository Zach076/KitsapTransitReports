using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
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
using static KTReports.DatabaseManager;

namespace KTReports
{
    /// <summary>
    /// Interaction logic for ManualDataEntry.xaml
    /// </summary>
    public partial class ManualDataEntry : Page
    {
        private DataTable dataTable;
        private DatabaseManager databaseManager = GetDBManager();

        public ManualDataEntry()
        {
            InitializeComponent();
            InitializeDataGrid();
        }

        private FileType GetDataType()
        {
            string option = (DataTypeSelector.SelectedItem as ComboBoxItem).Content.ToString().ToLower();
            switch (option)
            {
                case "fare card":
                    return FileType.FC;
                case "non-fare card":
                    return FileType.NFC;
                case "boardings":
                    return FileType.RSD;
                default:
                    return FileType.FC;
            }
        }

        private void DataTypeChanged(object sender, RoutedEventArgs e)
        {
            if (dataGrid != null)
            {
                InitializeDataGrid();
            }
        }

        private void InitializeDataGrid()
        {
            dataGrid.Columns.Clear();
            // Get the fare card data type selected
            var dataType = GetDataType();
            List<string> columnNames = databaseManager.GetTableInfo(dataType);
            dataTable = new DataTable();
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            foreach (string columnName in columnNames)
            {
                string mColumnName = textInfo.ToTitleCase(columnName.Replace("_", " "));
                if (mColumnName.Equals("Start Date") || mColumnName.Equals("End Date"))
                {
                    continue;
                } else if (mColumnName.Equals("Assigned Route Id"))
                {
                    mColumnName = "Route ID";
                }
                dataTable.Columns.Add(mColumnName, typeof(string));
                
            }
            if (dataType == FileType.FC)
            {
                dataTable.Columns["Route ID"].SetOrdinal(0);
                dataTable.Columns["Boardings"].SetOrdinal(1);
            } else if (dataType == FileType.NFC)
            {
                dataTable.Columns["Route ID"].SetOrdinal(0);
                dataTable.Columns["Total Ridership"].SetOrdinal(1);
                dataTable.Columns["Total Non Ridership"].SetOrdinal(2);
            }
            dataGrid.ItemsSource = dataTable.DefaultView;
            dataGrid.AutoGenerateColumns = true;
        }

        private bool IsValidRange(List<DateTime> dateRange)
        {
            if (dateRange.Count != 2 || dateRange[0].CompareTo(dateRange[1]) > 0)
            {
                // dateRange contains null values or start date is later than end date, which is invalid
                return false;
            }
            return true;
        }

        private List<DateTime> GetDateRange()
        {
            // Get the DateTimes from each of the date pickers and return them in a list
            List<DateTime> dateRange = new List<DateTime>();
            if (StartDatePicker.SelectedDate != null)
            {
                DateTime startDate = StartDatePicker.SelectedDate.Value;
                dateRange.Add(startDate);
            }
            if (EndDatePicker.SelectedDate != null)
            {
                DateTime endDate = EndDatePicker.SelectedDate.Value;
                dateRange.Add(endDate);
            }
            return dateRange;
        }

        private void AddToDatabase(object sender, RoutedEventArgs e)
        {
            // Get the date range, and check if valid
            List<DateTime> dateRange = GetDateRange();
            if (!IsValidRange(dateRange))
            {
                // Display error message and do not generate add to database
                MessageBox.Show("Enter a valid date range.", "Add to Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // Ask user if they are sure they want to continue?

            var dataType = GetDataType();

            foreach (DataRow row in dataTable.Rows)
            {
                var keyValuePairs = new Dictionary<string, string>();
                keyValuePairs.Add("start_date", dateRange[0].ToString("yyyy-MM-dd"));
                keyValuePairs.Add("end_date", dateRange[1].ToString("yyyy-MM-dd"));
                foreach (DataColumn column in dataTable.Columns)
                {
                    string columnName = column.ColumnName;
                    string dbColumnName = columnName.ToLower().Replace(" ", "_");
                    string enteredData = row[columnName].ToString();
                    if (columnName.Equals("Is Weekday") && String.IsNullOrEmpty(enteredData))
                    {
                        enteredData = true.ToString();
                    }
                    keyValuePairs.Add(dbColumnName, enteredData);
                   // Console.Write(row[columnName].ToString() + " ");
                    Console.Write(dbColumnName + " ");
                }
                switch (dataType)
                {
                    case FileType.FC:
                        databaseManager.InsertFCD(keyValuePairs);
                        break;
                    case FileType.NFC:
                        databaseManager.InsertNFC(keyValuePairs);
                        break;
                    case FileType.RSD:
                        databaseManager.InsertRSD(keyValuePairs);
                        break;
                }
                Console.WriteLine("");
            }
            InitializeDataGrid();
        }
    }
}
