using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        private DeleteImports()
        {
            InitializeComponent();
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
            // Remove existing radio buttons and add new radio buttons to the ListOfImports
            RemoveRadioButtons();
            AddRadioButtons();
            // Set/Reset selected file to null so that we don't try to delete an already deleted file
            selectedFile = null;
            FileInfoTitle.Text = "No File Selected";
            ImportedFileInfo.Text = string.Empty;
            ImportedFileInfo.Visibility = Visibility.Hidden;
        }

        private void RemoveRadioButtons()
        {
            ListOfImports.Items.Clear();
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
            FileInfoTitle.Text = $"File selected:";
            string nameStr = $"{ file["name"] }\n\n";
            string importDateStr = $"Date imported: {file["import_date"]}\n\n";
            string dirLocationStr = $"Directory location: {file["dir_location"]}\n\n";
            string fileTypeStr = $"File type: ${file["file_type"].ToString()}\n\n";
            string fileIdStr = $"File ID: {file["file_id"]}";
            ImportedFileInfo.Text = nameStr + fileTypeStr + dirLocationStr + importDateStr + fileIdStr;
            ImportedFileInfo.Visibility = Visibility.Visible;
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
