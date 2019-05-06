using PublicHoliday;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Media;
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
    /// Interaction logic for Holidays.xaml
    /// </summary>
    public partial class Holidays : Page
    {
        private DatabaseManager databaseManager = GetDBManager();
        private List<NameValueCollection> routes = null;
        private DataTable dataTable = new DataTable();
        private List<string> routeColumns;
        private Stack<DataTable> undoStack = new Stack<DataTable>();
        private Stack<DataTable> redoStack = new Stack<DataTable>();

        public Holidays()
        {
            InitializeComponent();
            TextInfo ti = new CultureInfo("en-US", false).TextInfo;
            var deleteCol = new DataColumn("Delete?", typeof(bool));
            deleteCol.DefaultValue = false;
            dataTable.Columns.Add(deleteCol);
            var nameCol = new DataColumn("Name", typeof(string));
            dataTable.Columns.Add(nameCol);
            var dateCol = new DataColumn("Date", typeof(string));
            dataTable.Columns.Add(dateCol);
            var serviceTypeCol = new DataColumn("Service Type", typeof(int));
            serviceTypeCol.DefaultValue = 0;
            dataTable.Columns.Add(serviceTypeCol);
            var holidayIdCol = new DataColumn("Holiday Id", typeof(int));
            holidayIdCol.DefaultValue = -1;
            dataTable.Columns.Add(holidayIdCol);

            yearPicker.Value = DateTime.Today;
            dataGrid.DataContext = dataTable.DefaultView;
            dataGrid.ItemsSource = dataTable.DefaultView;
            RoutedCommand undoCommand = new RoutedCommand();
            undoCommand.InputGestures.Add(new KeyGesture(Key.Z, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(undoCommand, OnUndoClicked));
            RoutedCommand redoCommand = new RoutedCommand();
            redoCommand.InputGestures.Add(new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift));
            CommandBindings.Add(new CommandBinding(redoCommand, OnRedoClicked));
        }

        private void LoadedDataGrid(object sender, EventArgs e)
        {
            dataGrid.MaxHeight = ActualHeight - 90;
            dataGrid.Columns[4].Visibility = Visibility.Collapsed;
        }


        private void OnDateChange(object sender, RoutedEventArgs e)
        {
            var curDate = (DateTime) yearPicker.Value;
            Console.WriteLine(curDate);
            if (curDate == null)
            {
                return;
            }
            int year = curDate.Year;
            Console.WriteLine(year);
            if (dataTable == null)
            {
                return;
            }
            // If any unsaved changes, ask user if they want to save or cancel
            if (dataTable.GetChanges() != null)
            {
                MessageBoxResult result = MessageBox.Show("Save your changes?", "Save Changes", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    SaveChanges(null, null);
                }
            }
            undoStack.Clear();
            redoStack.Clear();
            PopulateDataGrid();
        }

        private void PopulateDataGrid()
        {

            //dataGrid.Columns[1].Visibility = Visibility.Collapsed;
            dataTable.Clear();
            int year = ((DateTime)yearPicker.Value).Year;
            IDictionary<DateTime, string> holidays = new USAPublicHoliday().PublicHolidayNames(year);
            var yearStartAndEnd = new List<DateTime>() {
                new DateTime(year, 1, 1),
                new DateTime(year, 12, 31)
            };
            List<NameValueCollection> dbHolidays = databaseManager.GetHolidaysInRange(yearStartAndEnd);
            var dbDates = new Dictionary<string, NameValueCollection>();
            foreach (var holiday in dbHolidays)
            {
                dbDates.Add(DateTime.Parse(holiday["date"]).ToShortDateString(), holiday);
                Console.WriteLine(DateTime.Parse(holiday["date"]).ToShortDateString());
            }

            foreach (var holiday in holidays.Keys)
            {
                var dataRow = dataTable.NewRow();
                var date = holiday.ToShortDateString();
                dataRow[2] = date;
                if (dbDates.ContainsKey(date))
                {
                    dataRow[1] = dbDates[date]["name"];
                    Console.WriteLine((dbDates[date]));
                    dataRow[3] = dbDates[date]["service_type"];
                    dataRow[4] = dbDates[date]["holiday_id"];
                    dbDates.Remove(date);
                }
                else
                {
                    dataRow[1] = holidays[holiday];
                    dataRow[3] = 0;
                    dataRow[4] = -1;
                }
                dataTable.Rows.Add(dataRow);
                dataRow.AcceptChanges();
                Console.WriteLine(holiday + " " + holidays[holiday]);
                
            }
            foreach (var holiday in dbDates.Values)
            {
                var dataRow = dataTable.NewRow();
                dataRow[1] = holiday["name"];
                var date = DateTime.Parse(holiday["date"]).ToShortDateString();
                dataRow[2] = date;
                dataRow[3] = holiday["service_type"];
                dataRow[4] = holiday["holiday_id"];
                dataTable.Rows.Add(dataRow);
                dataRow.AcceptChanges();
                Console.WriteLine(date);
            }
            Console.WriteLine("Holidays printed");
        }

        private void SaveChanges(object sender, RoutedEventArgs e)
        {

            MessageBoxResult result = MessageBox.Show("Save your changes?", "Save Changes", MessageBoxButton.OKCancel);
            if (result != MessageBoxResult.OK)
            {
                return;
            }
            var addedHolidays = new List<Dictionary<string, string>>();
            var deletedHolidays = new List<Dictionary<string, string>>();
            var modifiedHolidays = new List<Dictionary<string, string>>();
            foreach (DataRow row in dataTable.Rows)
            {
                if (row.HasVersion(DataRowVersion.Proposed))
                {
                    row.EndEdit();
                }
                if (row.RowState == DataRowState.Modified)
                {
                    Console.Write("Modified: ");
                    var modifiedHoliday = new Dictionary<string, string>();
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        string databaseColName = col.ColumnName.ToLower().Replace(' ', '_');
                        modifiedHoliday.Add(databaseColName, row[col].ToString());
                        Console.Write(databaseColName + ": " + row[col] + ", ");
                    }
                    if ((bool)row["Delete?"])
                    {
                        deletedHolidays.Add(modifiedHoliday);
                    }
                    else
                    {
                        modifiedHolidays.Add(modifiedHoliday);
                    }
                    Console.WriteLine();
                }
                else if (row.RowState == DataRowState.Added)
                {
                    Console.Write("Added: ");
                    var addedHoliday = new Dictionary<string, string>();
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        string databaseColName = col.ColumnName.ToLower().Replace(' ', '_');
                        addedHoliday.Add(databaseColName, row[col].ToString());
                        Console.Write(databaseColName + ": " + row[col] + ", ");
                    }
                    addedHolidays.Add(addedHoliday);
                    Console.WriteLine();
                }
                row.AcceptChanges();
            }
            foreach (var holiday in modifiedHolidays)
            {
                try
                {
                    holiday["date"] = DateTime.Parse(holiday["date"]).ToString("yyyy-MM-dd");
                    Console.WriteLine(holiday["holiday_id"]);
                    string holiday_id_str = holiday["holiday_id"];
                    if (!holiday_id_str.Equals("-1"))
                    {
                        databaseManager.UpdateHoliday(holiday);
                        Console.WriteLine("Modified holiday");
                    }
                    else
                    {
                        databaseManager.AddHoliday(holiday);
                        Console.WriteLine("Added a modified holiday");
                    }
                }
                catch (Exception) { }
            }
            foreach (var holiday in addedHolidays)
            {
                try
                {
                    holiday["date"] = DateTime.Parse(holiday["date"]).ToString("yyyy-MM-dd");
                    databaseManager.AddHoliday(holiday);
                }
                catch (Exception) { }
            }
            foreach (var holiday in deletedHolidays)
            {
                try
                {
                    holiday["date"] = DateTime.Parse(holiday["date"]).ToString("yyyy-MM-dd");
                    databaseManager.DeleteHoliday(holiday);
                }
                catch (Exception) { }
        }
            PopulateDataGrid();
        }

        private void OnPageSizeChanged(object sender, RoutedEventArgs e)
        {
            if (!double.IsNaN(Height))
            {
                dataGrid.MaxHeight = 50;

            }
        }

        private void CancelChanges(object sender, RoutedEventArgs e)
        {
            if (dataTable.GetChanges() == null)
            {
                return;
            }
            MessageBoxResult result = MessageBox.Show("Cancel your changes?", "Cancel Changes", MessageBoxButton.OKCancel);
            if (result != MessageBoxResult.OK)
            {
                return;
            }
            PopulateDataGrid();
        }

        private void UpdatedDataGrid(object sender, EventArgs e)
        {
            Console.WriteLine("Cell Updated");
            undoStack.Push(dataTable.Copy());
            redoStack.Clear();
        }

        private void OnRedoClicked(object sender, RoutedEventArgs e)
        {
            if (redoStack.Count() > 0)
            {
                undoStack.Push(dataTable.Copy());
                dataTable = redoStack.Pop();
                dataGrid.DataContext = dataTable.DefaultView;
                dataGrid.ItemsSource = dataTable.DefaultView;
                dataGrid.Items.Refresh();
                dataGrid.Columns[4].Visibility = Visibility.Collapsed;
                Console.WriteLine("Redo Complete");
            }
            else
            {
                SystemSounds.Beep.Play();
            }
        }

        private void OnUndoClicked(object sender, RoutedEventArgs e)
        {
            if (undoStack.Count() > 0)
            {
                redoStack.Push(dataTable.Copy());
                dataTable = undoStack.Pop();
                dataGrid.DataContext = dataTable.DefaultView;
                dataGrid.ItemsSource = dataTable.DefaultView;
                dataGrid.Items.Refresh();
                dataGrid.Columns[4].Visibility = Visibility.Collapsed;
                Console.WriteLine("Undo Complete");
            }
            else
            {
                SystemSounds.Beep.Play();
            }
        }
    }
}
