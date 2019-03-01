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
using static KTReports.DatabaseManager;

namespace KTReports
{
    /// <summary>
    /// Interaction logic for ManualDataEntry.xaml
    /// </summary>
    public partial class ManualDataEntry : Page
    {
        List<TextBox> myItems;
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
            DatabaseManager databaseManager = DatabaseManager.GetDBManager();
            List<string> columnNames = databaseManager.GetTableInfo(dataType);
            foreach (string columnName in columnNames)
            {
                DataGridTextColumn textColumn = new DataGridTextColumn();
                // Console.WriteLine(columnName);
                textColumn.Header = columnName.Replace("_", "__");
                //textColumn.IsFrozen = true;
                dataGrid.Columns.Add(textColumn);
            }
            var textBox = new TextBox();
            textBox.IsReadOnly = false;
            myItems = new List<TextBox>();
            dataGrid.ItemsSource = myItems;
            myItems.Add(textBox);
        }
    }
}
