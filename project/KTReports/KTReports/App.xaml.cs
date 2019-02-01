using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace KTReports
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public DatabaseManager databaseManager;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Gets a singleton for database manager
            // databaseManager = DatabaseManager.GetDBManager();
            // Run tests on insertions and queries for a test database
            TestDB test = new TestDB();
            test.TestInsertions();
            test.TestQueries();
            test.RemoveDB();
        }

    }
}
