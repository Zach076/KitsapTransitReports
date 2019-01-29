﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace KTReports
{
    public class DatabaseManager
    {
        public SQLiteConnection sqliteConnection;

        public DatabaseManager()
        {
            sqliteConnection = new SQLiteConnection("Data Source=ktdatabase.sqlite3");
            if (File.Exists("./ktdatabase.sqlite3"))
            {
                SQLiteConnection.CreateFile("ktdatabase.sqlite3");
                Console.WriteLine("Database file created");
            }
            else
            {
                Console.WriteLine("Already created database");
            }
            sqliteConnection.Open();
            CreateTables();
            TestInsertions();
            TestQueries();

        }

        private void CreateTables()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                List<string> commands = new List<string>();
                // When we add a brand new route, add a new master route entry and detail routes entry.
                // When we update route information, like name or assigned route id, update the old detail route with an end_date,
                // then add a new detail routes entry with the same master route id.
                // If we update detailed historical route information, user picks a date to begin the change and a date to end the change.
                // We will find the detailed 
                string routes = @"CREATE TABLE IF NOT EXISTS Routes (
                    id integer PRIMARY KEY AUTOINCREMENT,
	                master_route_id integer,
                    assigned_route_id integer,
	                start_date text,
                    end_date text,
	                route_name text,
                    district text,
	                distance float,
                    num_trips_week float,
                    num_trips_sat float,
                    num_trips_hol float,
                    weekday_hours float,
                    saturday_hours float,
                    holiday_hours float
                )";
                commands.Add(routes);
                string routeStops = @"CREATE TABLE IF NOT EXISTS RouteStops (
	                id integer PRIMARY KEY AUTOINCREMENT,
	                master_rs_id integer,
	                assigned_rs_id integer,
	                rs_name text,
	                route_id integer,
	                start_date text,
	                end_date text
                )";
                commands.Add(routeStops);
                string dropRSD = @"DROP TABLE IF EXISTS RouteStopData";
                commands.Add(dropRSD);
                string routeStopsData = @"CREATE TABLE RouteStopsData (
	                sd_id integer PRIMARY KEY AUTOINCREMENT,
	                route_stop_id integer,
	                route_name text,
	                minus_door_1_person integer,
	                minus_door_2_person integer,
	                door_1_person integer,
	                door_2_person integer,
	                file_id integer
                )";
                commands.Add(routeStopsData);
                string dropNFCD = @"DROP TABLE IF EXISTS nonFareCardData";
                commands.Add(dropNFCD);
                string nonFareCardData = @"CREATE TABLE NonFareCardData (
	                nfc_id integer PRIMARY KEY AUTOINCREMENT,
	                route_id integer,
	                is_weekday boolean,
	                route_direction text,
	                total_ridership integer,
	                total_non_ridership integer,
	                adult_cash_fare integer,
	                youth_cash_fare integer,
	                reduced_cash_fare integer,
	                paper_transfer integer,
	                free_ride integer,
	                personal_care_attendant integer,
	                passenger_headcount integer,
	                cash_fare_underpmnt integer,
	                cash_upgrade integer,
	                special_survey integer,
	                wheelchair integer,
	                bicycle integer,
	                ferry_passenger_headcount integer,
	                file_id integer
                )";
                commands.Add(nonFareCardData);
                string dropFCD = @"DROP TABLE IF EXISTS FareCardData";
                commands.Add(dropFCD);
                string fareCardData = @"CREATE TABLE FareCardData (
	                fc_id integer PRIMARY KEY AUTOINCREMENT,
	                route_id integer,
	                is_weekday boolean,
	                transit_operator text,
	                source_participant text,
	                service_participant text,
	                mode text,
	                route_direction text,
	                trip_start text,
	                boardings text,
	                file_id integer
                )";
                commands.Add(fareCardData);
                string reportHistory = @"CREATE TABLE IF NOT EXISTS ReportHistory (
	                report_id integer PRIMARY KEY AUTOINCREMENT,
	                report_location string,
	                datetime_created text,
	                report_range string
                )";
                commands.Add(reportHistory);
                string masterRoutes = @"CREATE TABLE IF NOT EXISTS MasterRoutes (
	                master_route_id integer PRIMARY KEY AUTOINCREMENT
                )";
                commands.Add(masterRoutes);
                string masterRouteStops = @"CREATE TABLE IF NOT EXISTS MasterRouteStops (
	                master_stop_id integer PRIMARY KEY AUTOINCREMENT
                )";
                commands.Add(masterRouteStops);
                string importedFiles = @"CREATE TABLE IF NOT EXISTS ImportedFiles (
	                file_id integer PRIMARY KEY AUTOINCREMENT,
	                name text,
	                dir_location text,
	                file_type text,
	                start_date text,
	                end_date text
                )";
                commands.Add(importedFiles);
                foreach (string commandStr in commands)
                {
                    SQLiteCommand command = new SQLiteCommand(commandStr, sqliteConnection);
                    command.ExecuteNonQuery();
                    command.Dispose();
                }
                transaction.Complete();
            }
        }

        public Boolean Insert(string table, string[] keys, string[] values)
        {
            try
            {
                string insertSQL = "INSERT INTO @table (" + 
                    string.Join(", ", keys) + 
                    ") VALUES (" + 
                    string.Join(", ", values) + ")";
                using (SQLiteCommand command = new SQLiteCommand(insertSQL, sqliteConnection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle.StackTrace);
                return false;
            }
            return true;
        }

        public enum FileType {NFC, FC, RSD};
        public Boolean InsertNewFile(string fileName, string fileLocation, FileType fileType, string[] dateRange)
        {
            try
            {
               
                string insertSQL = @"INSERT INTO ImportedFiles 
                    (name, dir_location, file_type, start_date, end_date) 
                    VALUES (@fileName, @fileLocation, @fileType, @startDate, @endDate)";
                using (SQLiteCommand command = new SQLiteCommand())
                {
                    command.CommandText = insertSQL;
                    command.Connection = sqliteConnection;
                    command.Parameters.Add(new SQLiteParameter("@fileName", fileName));
                    command.Parameters.Add(new SQLiteParameter("@fileLocation", fileLocation));
                    command.Parameters.Add(new SQLiteParameter("@fileType", fileType.ToString()));
                    command.Parameters.Add(new SQLiteParameter("@startDate", dateRange[0]));
                    command.Parameters.Add(new SQLiteParameter("@endDate", dateRange[1]));
                    command.ExecuteNonQuery();
                }
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle.StackTrace);
                return false;
            }
            catch (IndexOutOfRangeException ie)
            {
                Console.WriteLine(ie.StackTrace);
                return false;
            }
            return true;
        }

        public Boolean Query(string[] selection, string[] tables, string expressions)
        {
            SQLiteCommand command = null;
            try
            {
                string query = "SELECT " + string.Join(", ", selection) + "FROM " + string.Join(", ", tables) + " WHERE " + expressions;
                command = new SQLiteCommand(query, sqliteConnection);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var result = reader.GetValues();
                        foreach (string col in result.AllKeys)
                        {
                            Console.Write(col + " ");
                            Console.WriteLine(result[col]);
                        }
                    }
                }
                command.Dispose();
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle.StackTrace);
                return false;
            }
            finally
            {
                if (command != null)
                {
                    command.Dispose();
                }
            }
            Console.WriteLine("Done with query");
            return true;
        }

        // Write tests for insertion and queries
        public void TestInsertions()
        {
            InsertNewFile("test_file_name.csv", "C:\\folder\\kt", FileType.FC, new string[] { "1980-01-01", "1980-01-31" });

        }

        public void TestQueries()
        {
            Query(new string[] { "*" }, new string[] { "ImportedFiles" }, "date(\"1980-02-01\") > date(start_date)");
            Query(new string[] { "*" }, new string[] { "ImportedFiles" }, "date(\"1980-01-20\") > end_date");
        }
    }
}
