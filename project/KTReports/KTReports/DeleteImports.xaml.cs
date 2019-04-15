using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
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

namespace KTReports
{
    /// <summary>
    /// Interaction logic for DeleteImports.xaml
    /// </summary>
    public partial class DeleteImports : Page
    {
        private static DeleteImports deleteImportsInstance = null;
        private List<NameValueCollection> importedFiles = null;
        private NameValueCollection selectedFile;
        private DataTable dataTable;
        private DeleteImports()
        {
            InitializeComponent();
            dataTable = new DataTable();
            dataTable.Columns.Add(new DataColumn("File Name", typeof(string)));
            dataTable.Columns.Add(new DataColumn("Date Imported", typeof(string)));
            dataTable.Columns.Add(new DataColumn("File Type", typeof(string)));
            dataTable.Columns.Add(new DataColumn("File ID", typeof(string)));
            dataTable.Columns.Add(new DataColumn("Directory Location", typeof(string)));
            ImportedInfoGrid.ItemsSource = dataTable.DefaultView;
            SetupPage();
        }

        public static DeleteImports GetDeleteImports()
        {
            if (deleteImportsInstance == null)
            {
                deleteImportsInstance = new DeleteImports();
            }
            return deleteImportsInstance;
        }

        // Call this when updating database with new file and after deleting an imported file
        public void SetupPage()
        {
            // Query the database for a list of imported files
            DatabaseManager databaseManager = DatabaseManager.GetDBManager();
            importedFiles = databaseManager.GetImportedFiles();
            Dispatcher.Invoke(() =>
            {
                // Remove existing radio buttons and add new radio buttons to the ListOfImports
                RemoveRadioButtons();
                AddRadioButtons();
                // Set/Reset selected file to null so that we don't try to delete an already deleted file
                selectedFile = null;
                FileInfoTitle.Text = "No File Selected";
            });
            dataTable.Clear();
        }

        private void RemoveRadioButtons()
        {
            Dispatcher.Invoke(() => ListOfImports.Items.Clear());
        }

        private void AddRadioButtons()
        {
            foreach (var file in importedFiles)
            {
                RadioButton radioButton = new RadioButton();
                radioButton.Content = file["name"];
                radioButton.Tag = file["file_id"];
                radioButton.Margin = new Thickness(4);
                radioButton.Click += new RoutedEventHandler(this.RadioButtonClicked);
                ListOfImports.Items.Add(radioButton);
            }
        }

        private void RadioButtonClicked(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = (RadioButton)sender;
            NameValueCollection fileSelected = null;
            foreach (var file in importedFiles)
            {
                if (file["file_id"].Equals(radioButton.Tag))
                {
                    fileSelected = file;
                }
            }
            selectedFile = fileSelected;
            DisplayFileInfo(fileSelected);
        }

        private void DisplayFileInfo(NameValueCollection file)
        {
            dataTable.Clear();
            FileInfoTitle.Text = $"File selected:";
            var dataRow = dataTable.NewRow();
            dataRow[0] = file["name"];
            dataRow[1] = file["import_date"];
            dataRow[2] = file["file_type"].ToString();
            dataRow[3] = file["file_id"];
            dataRow[4] = file["dir_location"];
            dataTable.Rows.Add(dataRow);
        }

        private void DeleteImportedFile(object sender, RoutedEventArgs e)
        {
            // Make sure a file is selected
            if (selectedFile == null) return;
            DatabaseManager databaseManager = DatabaseManager.GetDBManager();
            Enum.TryParse(selectedFile["file_type"], out DatabaseManager.FileType fileType);
            databaseManager.DeleteImportedFile(Convert.ToInt64(selectedFile["file_id"]), fileType);
            // Refresh the page
            SetupPage();
        }
    }
}
