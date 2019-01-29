﻿using System;
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
            databaseManager = new DatabaseManager();
        }

    }
}
