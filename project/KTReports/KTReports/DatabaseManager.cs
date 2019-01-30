using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
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
        private static DatabaseManager dbManagerInstance = null;
        private SQLiteConnection sqliteConnection;

        private DatabaseManager()
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
        }

        public static DatabaseManager GetDBManager()
        {
            if (dbManagerInstance == null)
            {
                dbManagerInstance = new DatabaseManager();
            }
            return dbManagerInstance;
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

        public enum FileType { NFC, FC, RSD };
        // returns nullable long
        public long? InsertNewFile(string fileName, string fileLocation, FileType fileType, string[] dateRange)
        {
            try
            {

                string insertSQL =
                    @"INSERT INTO ImportedFiles 
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
                return null;
            }
            catch (IndexOutOfRangeException ie)
            {
                Console.WriteLine(ie.StackTrace);
                return null;
            }
            // return file id here
            return sqliteConnection.LastInsertRowId;
        }

        public Boolean InsertFCD(Dictionary<string, string> keyValuePairs)
        {
            try
            {

                string insertSQL =
                    @"INSERT INTO FareCardData 
                        (route_id, is_weekday, transit_operator, source_participant, service_participant, mode, route_direction, trip_start, boardings, file_id)
                    VALUES (@route_id, @is_weekday, @transit_operator, @source_participant, @service_participant, @mode, @route_direction, @trip_start, @boardings, @file_id)";
                using (SQLiteCommand command = new SQLiteCommand())
                {
                    command.CommandText = insertSQL;
                    command.Connection = sqliteConnection;
                    command.Parameters.Add(new SQLiteParameter("@route_id", keyValuePairs["route_id"]));
                    command.Parameters.Add(new SQLiteParameter("@is_weekday", keyValuePairs["is_weekday"]));
                    command.Parameters.Add(new SQLiteParameter("@transit_operator", keyValuePairs["transit_operator"]));
                    command.Parameters.Add(new SQLiteParameter("@source_participant", keyValuePairs["source_participant"]));
                    command.Parameters.Add(new SQLiteParameter("@service_participant", keyValuePairs["service_participant"]));
                    command.Parameters.Add(new SQLiteParameter("@mode", keyValuePairs["mode"]));
                    command.Parameters.Add(new SQLiteParameter("@route_direction", keyValuePairs["route_direction"]));
                    command.Parameters.Add(new SQLiteParameter("@trip_start", keyValuePairs["trip_start"]));
                    command.Parameters.Add(new SQLiteParameter("@boardings", keyValuePairs["boardings"]));
                    command.Parameters.Add(new SQLiteParameter("@file_id", keyValuePairs["file_id"]));
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

        public Boolean InsertNFC(Dictionary<string, string> keyValuePairs)
        {
            try
            {

                string insertSQL =
                    @"INSERT INTO NonFareCardData 
                        (route_id, is_weekday, route_direction, total_ridership, total_non_ridership, adult_cash_fare, youth_cash_fare, reduced_cash_fare, paper_transfer,
                        free_ride, personal_care_attendant, passenger_headcount, cash_fare_underpmnt, cash_upgrade, special_survey, wheelchair, bicycle, ferry_passenger_headcount, file_id) 
                    VALUES (@route_id, @is_weekday, @route_direction, @total_ridership, @total_non_ridership, @adult_cash_fare, @youth_cash_fare, 
                        @reduced_cash_fare, @paper_transfer, @free_ride, @personal_care_attendant, @passenger_headcount, @cash_fare_underpmnt, @cash_upgrade, @special_survey,
                        @wheelchair, @bicycle, @ferry_passenger_headcount, @file_id)";
                using (SQLiteCommand command = new SQLiteCommand())
                {
                    command.CommandText = insertSQL;
                    command.Connection = sqliteConnection;
                    command.Parameters.Add(new SQLiteParameter("@route_id", keyValuePairs["route_id"]));
                    command.Parameters.Add(new SQLiteParameter("@is_weekday", keyValuePairs["is_weekday"]));
                    command.Parameters.Add(new SQLiteParameter("@route_direction", keyValuePairs["route_direction"]));
                    command.Parameters.Add(new SQLiteParameter("@total_ridership", keyValuePairs["total_ridership"]));
                    command.Parameters.Add(new SQLiteParameter("@total_non_ridership", keyValuePairs["total_non_ridership"]));
                    command.Parameters.Add(new SQLiteParameter("@adult_cash_fare", keyValuePairs["adult_cash_fare"]));
                    command.Parameters.Add(new SQLiteParameter("@youth_cash_fare", keyValuePairs["youth_cash_fare"]));
                    command.Parameters.Add(new SQLiteParameter("@reduced_cash_fare", keyValuePairs["paper_transfer"]));
                    command.Parameters.Add(new SQLiteParameter("@free_ride", keyValuePairs["free_ride"]));
                    command.Parameters.Add(new SQLiteParameter("@personal_care_attendant", keyValuePairs["personal_care_attendant"]));
                    command.Parameters.Add(new SQLiteParameter("@passenger_headcount", keyValuePairs["passenger_headcount"]));
                    command.Parameters.Add(new SQLiteParameter("@cash_fare_underpmnt", keyValuePairs["cash_fare_underpmnt"]));
                    command.Parameters.Add(new SQLiteParameter("@cash_upgrade", keyValuePairs["cash_upgrade"]));
                    command.Parameters.Add(new SQLiteParameter("@special_survey", keyValuePairs["special_survey"]));
                    command.Parameters.Add(new SQLiteParameter("@wheelchair", keyValuePairs["wheelchair"]));
                    command.Parameters.Add(new SQLiteParameter("@bicycle", keyValuePairs["bicycle"]));
                    command.Parameters.Add(new SQLiteParameter("@ferry_passenger_headcount", keyValuePairs["ferry_passenger_headcount"]));
                    command.Parameters.Add(new SQLiteParameter("@file_id", keyValuePairs["file_id"]));
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

        public Boolean InsertRSD(Dictionary<string, string> keyValuePairs)
        {
            try
            {

                string insertSQL =
                    @"INSERT INTO RouteStopData 
                        (route_stop_id, route_name, minus_door_1_person, minus_door_2_person, door_1_person, door_2_person, file_id) 
                    VALUES (@route_stop_id, @route_name, @minus_door_1_person, @minus_door_2_person, @door_1_person, @door_2_person, @file_id)";
                using (SQLiteCommand command = new SQLiteCommand())
                {
                    command.CommandText = insertSQL;
                    command.Connection = sqliteConnection;
                    command.Parameters.Add(new SQLiteParameter("@route_stop_id", keyValuePairs["route_stop_id"]));
                    command.Parameters.Add(new SQLiteParameter("@route_name", keyValuePairs["route_name"]));
                    command.Parameters.Add(new SQLiteParameter("@minus_door_1_person", keyValuePairs["minus_door_1_person"]));
                    command.Parameters.Add(new SQLiteParameter("@minus_door_2_person", keyValuePairs["minus_door_2_person"]));
                    command.Parameters.Add(new SQLiteParameter("@door_1_person", keyValuePairs["door_1_person"]));
                    command.Parameters.Add(new SQLiteParameter("@door_2_person", keyValuePairs["door_2_person"]));
                    command.Parameters.Add(new SQLiteParameter("@file_id", keyValuePairs["file_id"]));
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

        public Boolean InsertReportHistory(Dictionary<string, string> keyValuePairs)
        {
            try
            {

                string insertSQL =
                    @"INSERT INTO ReportHistory 
                        (report_location, datetime_created, report_range) 
                    VALUES (@report_location, @datetime_created, @report_range)";
                using (SQLiteCommand command = new SQLiteCommand())
                {
                    command.CommandText = insertSQL;
                    command.Connection = sqliteConnection;
                    command.Parameters.Add(new SQLiteParameter("@report_location", keyValuePairs["report_location"]));
                    command.Parameters.Add(new SQLiteParameter("@datetime_created", keyValuePairs["datetime_created"]));
                    command.Parameters.Add(new SQLiteParameter("@report_range", keyValuePairs["report_range"]));
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

        public Boolean InsertRoutes(Dictionary<string, string> keyValuePairs)
        {
            try
            {

                string insertSQL =
                    @"INSERT INTO Routes 
                        (master_route_id, assigned_route_id, start_date, end_date, route_name, district, distance, num_trips_week, 
                        num_trips_sat, num_trips_hol, weekday_hours, saturday_hours, holiday_hours) 
                    VALUES (@master_route_id, @assigned_route_id, @start_date, @end_date, @route_name, @district, @distance, @num_trips_week,
                         @num_trips_sat, @num_trips_hol, @weekday_hours, @saturday_hours, @holiday_hours)";
                using (SQLiteCommand command = new SQLiteCommand())
                {
                    command.CommandText = insertSQL;
                    command.Connection = sqliteConnection;
                    command.Parameters.Add(new SQLiteParameter("@master_route_id", keyValuePairs["master_route_id"]));
                    command.Parameters.Add(new SQLiteParameter("@assigned_route_id", keyValuePairs["assigned_route_id"]));
                    command.Parameters.Add(new SQLiteParameter("@start_date", keyValuePairs["start_date"]));
                    keyValuePairs.TryGetValue("end_date", out string end_date);
                    command.Parameters.Add(new SQLiteParameter("@end_date", end_date));
                    command.Parameters.Add(new SQLiteParameter("@route_name", keyValuePairs["route_name"]));
                    command.Parameters.Add(new SQLiteParameter("@district", keyValuePairs["district"]));
                    command.Parameters.Add(new SQLiteParameter("@distance", keyValuePairs["distance"]));
                    command.Parameters.Add(new SQLiteParameter("@num_trips_week", keyValuePairs["num_trips_week"]));
                    command.Parameters.Add(new SQLiteParameter("@num_trips_sat", keyValuePairs["num_trips_sat"]));
                    command.Parameters.Add(new SQLiteParameter("@num_trips_hol", keyValuePairs["num_trips_hol"]));
                    command.Parameters.Add(new SQLiteParameter("@weekday_hours", keyValuePairs["weekday_hours"]));
                    command.Parameters.Add(new SQLiteParameter("@saturday_hours", keyValuePairs["saturday_hours"]));
                    command.Parameters.Add(new SQLiteParameter("@holiday_hours", keyValuePairs["holiday_hours"]));
                    command.ExecuteNonQuery();
                }
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle.StackTrace);
                throw sqle;
                // Throw for now, handle in future
                //return false;
            }
            return true;
        }

        public Boolean InsertRouteStops(Dictionary<string, string> keyValuePairs)
        {
            try
            {

                string insertSQL =
                    @"INSERT INTO RouteStops 
                        (master_rs_id, assigned_rs_id, rs_name, route_id, start_date, end_date) 
                    VALUES (@master_rs_id, @assigned_rs_id, @rs_name, @route_id, @start_date, @end_date)";
                using (SQLiteCommand command = new SQLiteCommand())
                {
                    command.CommandText = insertSQL;
                    command.Connection = sqliteConnection;
                    command.Parameters.Add(new SQLiteParameter("@master_rs_id", keyValuePairs["master_rs_id"]));
                    command.Parameters.Add(new SQLiteParameter("@assigned_rs_id", keyValuePairs["assigned_rs_id"]));
                    command.Parameters.Add(new SQLiteParameter("@rs_name", keyValuePairs["rs_name"]));
                    command.Parameters.Add(new SQLiteParameter("@route_id", keyValuePairs["route_id"]));
                    command.Parameters.Add(new SQLiteParameter("@route_name", keyValuePairs["route_name"]));
                    command.Parameters.Add(new SQLiteParameter("@start_date", keyValuePairs["start_date"]));
                    command.Parameters.Add(new SQLiteParameter("@end_date", keyValuePairs["end_date"]));
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

        public Boolean InsertNewRoute(Dictionary<string, string> keyValuePairs)
        {
            try
            {
                string addToMaster = @"INSERT INTO MasterRoutes (master_route_id) VALUES (null)";
                using (SQLiteCommand masterCommand = new SQLiteCommand(addToMaster, sqliteConnection))
                {
                    masterCommand.ExecuteNonQuery();
                }
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle.StackTrace);
                return false;
            }
            long master_route_id = sqliteConnection.LastInsertRowId;
            keyValuePairs.Add("master_route_id", master_route_id.ToString());
            InsertRoutes(keyValuePairs);
            return true;
        }

        public Boolean InsertNewRouteStop(Dictionary<string, string> keyValuePairs)
        {
            try
            {
                string addToMaster = @"INSERT INTO MasterRouteStops (master_rs_id) VALUES (null)";
                using (SQLiteCommand masterCommand = new SQLiteCommand(addToMaster, sqliteConnection))
                {
                    masterCommand.ExecuteNonQuery();
                }
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle.StackTrace);
                return false;
            }
            long master_rs_id = sqliteConnection.LastInsertRowId;
            keyValuePairs.Add("master_rs_id", master_rs_id.ToString());
            InsertRoutes(keyValuePairs);
            return true;
        }

        // TODO: Methods to update information, rather than just inserting

        public List<NameValueCollection> Query(string[] selection, string[] tables, string expressions)
        {
            string query = "SELECT " + string.Join(", ", selection) + " FROM " + string.Join(", ", tables) + " WHERE " + expressions;
            var results = new List<NameValueCollection>();
            using (var command = new SQLiteCommand(query, sqliteConnection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        NameValueCollection row = reader.GetValues();
                        results.Add(row);
                    }
                }
            }
            
            return results;
        }

        public static void TestDB()
        {
            TestDB tests = new TestDB();
            tests.TestInsertions();
            tests.TestQueries();
        }


        /* Only use for testing purposes
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
        } */
    }
}
