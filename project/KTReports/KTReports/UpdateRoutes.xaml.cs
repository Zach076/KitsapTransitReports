using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
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
    /// Interaction logic for UpdateRoutes.xaml
    /// </summary>
    public partial class UpdateRoutes : Page
    {
        private DatabaseManager databaseManager = GetDBManager();
        private List<NameValueCollection> routes = null;
        private DataTable dataTable = new DataTable();
        private List<string> routeColumns;

        public UpdateRoutes()
        {
            InitializeComponent();
            routeColumns = databaseManager.GetRouteTableInfo();
            TextInfo ti = new CultureInfo("en-US", false).TextInfo;
            foreach (string col in routeColumns)
            {
                var titleCol = ti.ToTitleCase(col.Replace('_', ' '));
                if (titleCol.Equals("Db Route Id") || titleCol.Equals("Path Id"))
                {
                    continue;
                } else if (titleCol.Equals("Assigned Route Id"))
                {
                    titleCol = "Route ID";
                }
                dataTable.Columns.Add(new DataColumn(titleCol, typeof(string)));
            }
            updateDatePicker.SelectedDate = DateTime.Today;
            dataGrid.DataContext = dataTable.DefaultView;
            var checkBoxColumn = new DataGridCheckBoxColumn
            {
                Header = "Delete?"
            };
            dataGrid.Columns.Add(checkBoxColumn);
        }

        private void OnDateChange(object sender, RoutedEventArgs e)
        {
            // If any unsaved changes, ask user if they want to save or cancel

            PopulateDataGrid();
        }

        private void OnCellChanged(object sender, EventArgs e)
        {
            // If any unsaved changes, ask user if they want to save or cancel
            DataGrid cell = (DataGrid) sender;
            Console.WriteLine(cell);
        }

        private void PopulateDataGrid()
        {
            dataTable.Clear();
            // Get all route data valid on selected date
            routes = databaseManager.GetValidRoutes((DateTime) updateDatePicker.SelectedDate);
            foreach(NameValueCollection route in routes)
            {
                var dataRow = dataTable.NewRow();
                for (int i = 2; i < routeColumns.Count; i++)
                {
                    string col = routeColumns[i];
                    dataRow[i-2] = route[col];
                }
                dataTable.Rows.Add(dataRow);
                dataRow.AcceptChanges();
            }
        }

        private void SaveChanges(object sender, RoutedEventArgs e)
        {
            // Add new routes to database
            // Update existing route info
            foreach (DataRow row in dataTable.Rows)
            {
                if (row.HasVersion(DataRowVersion.Proposed))
                {
                    row.EndEdit();
                }
                if (row.RowState == DataRowState.Modified)
                {
                    Console.Write("Modified: ");
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        Console.Write(row[col] + " ");
                    }
                    Console.WriteLine();
                } else if (row.RowState == DataRowState.Added)
                {
                    Console.Write("Added: ");
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        Console.Write(row[col] + " ");
                    }
                    Console.WriteLine();
                }
            }
        }

        private void CancelChanges(object sender, RoutedEventArgs e)
        {
            PopulateDataGrid();
        }

    }
}
