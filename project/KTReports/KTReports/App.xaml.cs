using MahApps.Metro;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
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
            ThemeManager.AddAccent("AppAccent", new Uri("pack://application:,,,/AppAccent.xaml"));

            // get the current app style (theme and accent) from the application
            Tuple<AppTheme, Accent> theme = ThemeManager.DetectAppStyle(Application.Current);

            // now change app style to the custom accent and current theme
            ThemeManager.ChangeAppStyle(Application.Current,
                                        ThemeManager.GetAccent("AppAccent"),
                                        ThemeManager.GetAppTheme("BaseLight"));
            // Gets a singleton for database manager
            //databaseManager = DatabaseManager.GetDBManager();
            // Run tests on insertions and queries for a test database
            //TestDB test = new TestDB();
            //test.TestInsertions();
            //test.RemoveDB();
        }

    }
}

