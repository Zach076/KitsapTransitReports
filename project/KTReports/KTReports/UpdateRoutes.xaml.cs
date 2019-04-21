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
    /// Interaction logic for UpdateRoutes.xaml
    /// </summary>
    public partial class UpdateRoutes : Page
    {
        private DatabaseManager databaseManager = GetDBManager();

        public UpdateRoutes()
        {
            InitializeComponent();
        }

        private void SaveChanges(object sender, RoutedEventArgs e)
        {
        }

        private void CancelChanges(object sender, RoutedEventArgs e)
        {
        }

    }
}
