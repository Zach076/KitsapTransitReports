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
    /// Interaction logic for UpdateRoutes.xaml
    /// </summary>
    public partial class UpdateRoutes : Page
    {
        private DatabaseManager databaseManager = GetDBManager();
        private List<NameValueCollection> routes = null;
        private DataTable dataTable = new DataTable();
        private List<string> routeColumns;
        private Stack<DataTable> undoStack = new Stack<DataTable>();
        private Stack<DataTable> redoStack = new Stack<DataTable>();
        public UpdateRoutes()
        {
            InitializeComponent();
            routeColumns = databaseManager.GetRouteTableInfo();
            TextInfo ti = new CultureInfo("en-US", false).TextInfo;
            var deleteCol = new DataColumn("Delete?", typeof(bool));
            deleteCol.DefaultValue = false;
            dataTable.Columns.Add(deleteCol);
            foreach (string col in routeColumns)
            {
                var titleCol = ti.ToTitleCase(col.Replace('_', ' '));
                if (titleCol.Equals("Assigned Route Id"))
                {
                    titleCol = "Route ID";
                }
                dataTable.Columns.Add(new DataColumn(titleCol, typeof(string)));
            }
            //dataTable.Columns.Add(new DataColumn("DELETE", typeof(bool)));
            updateDatePicker.SelectedDate = DateTime.Today;
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
            dataGrid.Columns[1].Visibility = Visibility.Collapsed;
            dataGrid.Columns[2].Visibility = Visibility.Collapsed;
        }


        private void OnDateChange(object sender, RoutedEventArgs e)
        {
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
            // Get all route data valid on selected date
            routes = databaseManager.GetValidRoutes((DateTime)updateDatePicker.SelectedDate);
            foreach (NameValueCollection route in routes)
            {
                var dataRow = dataTable.NewRow();
                for (int i = 0; i < routeColumns.Count; i++)
                {
                    string col = routeColumns[i];
                    dataRow[i + 1] = route[col];
                }
                dataTable.Rows.Add(dataRow);
                dataRow.AcceptChanges();
            }
        }

        private void SaveChanges(object sender, RoutedEventArgs e)
        {

            MessageBoxResult result = MessageBox.Show("Save your changes?", "Save Changes", MessageBoxButton.OKCancel);
            if (result != MessageBoxResult.OK)
            {
                return;
            }
            var addedRoutes = new List<Dictionary<string, string>>();
            var deletedRoutes = new List<Dictionary<string, string>>();
            var modifiedRoutes = new List<Dictionary<string, string>>();
            foreach (DataRow row in dataTable.Rows)
            {
                if (row.HasVersion(DataRowVersion.Proposed))
                {
                    row.EndEdit();
                }
                if (row.RowState == DataRowState.Modified)
                {
                    Console.Write("Modified: ");
                    var modifiedRoute = new Dictionary<string, string>();
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        string databaseColName = col.ColumnName.ToLower().Replace(' ', '_');
                        modifiedRoute.Add(databaseColName, row[col] as string);
                        Console.Write(databaseColName + ": " + row[col] + ", ");
                    }
                    //modifiedRoute.Add("start_date", ((DateTime) updateDatePicker.SelectedDate).ToString("yyyy-MM-dd")); 
                    if ((bool)row["Delete?"])
                    {
                        deletedRoutes.Add(modifiedRoute);
                    }
                    else
                    {
                        modifiedRoutes.Add(modifiedRoute);
                    }
                    Console.WriteLine();
                }
                else if (row.RowState == DataRowState.Added)
                {
                    Console.Write("Added: ");
                    var addedRoute = new Dictionary<string, string>();
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        string databaseColName = col.ColumnName.ToLower().Replace(' ', '_');
                        addedRoute.Add(databaseColName, row[col] as string);
                        Console.Write(databaseColName + ": " + row[col] + ", ");
                    }
                    addedRoutes.Add(addedRoute);
                    Console.WriteLine();
                }
                row.AcceptChanges();
            }
            foreach (var route in modifiedRoutes)
            {
                databaseManager.UpdateRoute(route);
            }
            foreach (var route in addedRoutes)
            {
                route.Remove("path_id");
                route.Remove("db_route_id");
                databaseManager.InsertPath(route);
            }
            foreach (var route in deletedRoutes)
            {
                databaseManager.DeleteRoute(route);
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
                dataGrid.Columns[1].Visibility = Visibility.Collapsed;
                dataGrid.Columns[2].Visibility = Visibility.Collapsed;
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
                /*dataTable.Clear();
                foreach (DataRow dataRow in undoStack.Pop().Rows)
                {
                    Console.WriteLine(dataRow.ItemArray);
                    dataTable.Rows.Add(dataRow.ItemArray);
                }*/
                dataTable = undoStack.Pop();
                dataGrid.DataContext = dataTable.DefaultView;
                dataGrid.ItemsSource = dataTable.DefaultView;
                dataGrid.Items.Refresh();
                dataGrid.Columns[1].Visibility = Visibility.Collapsed;
                dataGrid.Columns[2].Visibility = Visibility.Collapsed;
                Console.WriteLine("Undo Complete");
            }
            else
            {
                SystemSounds.Beep.Play();
            }
        }
    }
}
