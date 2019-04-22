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
        // Singleton instance of the DatabaseManager
        private static DatabaseManager dbManagerInstance = null;
        private SQLiteConnection sqliteConnection;

        private DatabaseManager()
        {
            ConnectToDB("ktdatabase");
        }

        private DatabaseManager(string fileName)
        {
            ConnectToDB(fileName);
        }

        private void ConnectToDB(string fileName)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            sqliteConnection = new SQLiteConnection("Data Source=" + appDataPath + "\\" + fileName + ".sqlite3");
            // Create the database if it doesn't exist
            if (!File.Exists(appDataPath + "\\" + fileName + ".sqlite3"))
            {
                SQLiteConnection.CreateFile(appDataPath + "\\" + fileName + ".sqlite3");
            }
            sqliteConnection.Open();
            CreateTables();
        }

        // Gets a single instance of the DatabaseManager (singleton)
        public static DatabaseManager GetDBManager(string optionalName = "ktdatabase")
        {
            if (dbManagerInstance == null)
            {
                dbManagerInstance = new DatabaseManager(optionalName);
            }
            return dbManagerInstance;
        }

        // Current schema (NEED TO UPDATE w/ distance_week and distance_sat in Routes table): https://i.imgur.com/4eerugA.png
        private void CreateTables()
        {
            // Complete all commands or none at all
            using (TransactionScope transaction = new TransactionScope())
            {
                List<string> commands = new List<string>();
                // Note: When we add a brand new route, add a new master route entry and detail routes entry.
                // When we update route information, like name or assigned route id, update the old detail route with an end_date,
                // then add a new detail routes entry with the same master route id.
                // If we update detailed historical route information, user picks a date to begin the change and a date to end the change.
                string routes = @"CREATE TABLE IF NOT EXISTS Routes (
                    db_route_id integer PRIMARY KEY AUTOINCREMENT,
                    path_id integer,
                    assigned_route_id integer,
	                start_date text,
	                route_name text,
                    district text,
                    distance_week float,
	                distance_sat float,
                    num_trips_week float,
                    num_trips_sat float,
                    num_trips_hol float,
                    weekday_hours float,
                    saturday_hours float,
                    holiday_hours float
                )";
                commands.Add(routes);
                string routeStops = @"CREATE TABLE IF NOT EXISTS Stops (
	                stop_id integer PRIMARY KEY,
	                location_id integer,
	                path_id integer,
	                start_date text,
	                stop_name text,
	                assigned_stop_id integer
                )";
                commands.Add(routeStops);
                string routeStopsData = @"CREATE TABLE IF NOT EXISTS RouteStopData (
	                sd_id integer PRIMARY KEY AUTOINCREMENT,
	                location_id integer,
	                assigned_stop_id integer,
	                start_date text,
	                end_date text,
	                minus_door_1_person integer,
	                minus_door_2_person integer,
	                door_1_person integer,
	                door_2_person integer,
	                file_id integer
                )";
                commands.Add(routeStopsData);
                string nonFareCardData = @"CREATE TABLE IF NOT EXISTS NonFareCardData (
	                nfc_id integer PRIMARY KEY AUTOINCREMENT,
	                path_id integer,
	                start_date text,
	                end_date text,
	                is_weekday boolean,
	                assigned_route_id text,
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
                string fareCardData = @"CREATE TABLE IF NOT EXISTS FareCardData (
	                fc_id integer PRIMARY KEY AUTOINCREMENT,
	                path_id integer,
	                start_date text,
	                end_date text,
	                is_weekday boolean,
	                assigned_route_id integer,
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
	                report_location string PRIMARY KEY,
	                datetime_created text,
	                report_range string
                )";
                commands.Add(reportHistory);
                string paths = @"CREATE TABLE IF NOT EXISTS Paths (
	                path_id integer PRIMARY KEY AUTOINCREMENT,
	                path_name text
                )";
                commands.Add(paths);
                string masterRouteStops = @"CREATE TABLE IF NOT EXISTS StopLocations (
	                location_id integer PRIMARY KEY AUTOINCREMENT,
	                location_name text
                )";
                commands.Add(masterRouteStops);
                string importedFiles = @"CREATE TABLE IF NOT EXISTS ImportedFiles (
	                file_id integer PRIMARY KEY AUTOINCREMENT,
	                name text,
	                dir_location text,
	                file_type text,
	                import_date text
                )";
                commands.Add(importedFiles);

                string holidays = @"CREATE TABLE IF NOT EXISTS Holidays (
	                holiday_id integer PRIMARY KEY AUTOINCREMENT,
                    date text,
                    service_type int
                )";

                commands.Add(holidays);
                // Execute each command
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
        // Insert brand new file information into the db (returns the file_id)
        public long InsertNewFile(string fileName, string fileLocation, FileType fileType, string importDate)
        {
            string insertSQL =
                @"INSERT INTO ImportedFiles 
                    (name, dir_location, file_type, import_date) 
                VALUES (@fileName, @fileLocation, @fileType, @import_date)";
            using (SQLiteCommand command = new SQLiteCommand(insertSQL, sqliteConnection))
            {
                command.Parameters.Add(new SQLiteParameter("@fileName", fileName));
                command.Parameters.Add(new SQLiteParameter("@fileLocation", fileLocation));
                command.Parameters.Add(new SQLiteParameter("@fileType", fileType.ToString()));
                command.Parameters.Add(new SQLiteParameter("@import_date", importDate));
                command.ExecuteNonQuery();
            }
            // Return file id here
            return sqliteConnection.LastInsertRowId;
        }

        private long? GetPathId(Dictionary<string, string> keyValuePairs)
        {
            long? pathId = null;
            var results = new List<NameValueCollection>();
            string query = @"SELECT path_id 
                                FROM Routes as r 
                                WHERE @start_date >= r.start_date AND r.assigned_route_id == @route_id
                                ORDER BY r.start_date";
            using (SQLiteCommand command = new SQLiteCommand(query, sqliteConnection))
            {
                command.Parameters.Add(new SQLiteParameter("@start_date", keyValuePairs["start_date"]));
                command.Parameters.Add(new SQLiteParameter("@route_id", keyValuePairs["route_id"]));
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        NameValueCollection row = reader.GetValues();
                        pathId = Convert.ToInt64(row["path_id"]);
                    }
                }
            }
            return pathId;
        }

        private long? GetLocationId(Dictionary<string, string> keyValuePairs)
        {
            long? locationId = null;
            var results = new List<NameValueCollection>();
            string query = @"SELECT location_id 
                                FROM Stops as s
                                WHERE @start_date >= s.start_date AND s.assigned_stop_id == @stop_id
                                ORDER BY s.start_date";
            using (SQLiteCommand command = new SQLiteCommand(query, sqliteConnection))
            {
                command.Parameters.Add(new SQLiteParameter("@start_date", keyValuePairs["start_date"]));
                command.Parameters.Add(new SQLiteParameter("@stop_id", keyValuePairs["stop_id"]));
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        NameValueCollection row = reader.GetValues();
                        locationId = Convert.ToInt64(row["location_id"]);
                    }
                }
            }
            if (locationId == null)
            {
                // If no result, then create entries in StopLocations and Stops and return the created location_id
                locationId = InsertStopLocation(keyValuePairs);
            }
            return locationId;
        }

        // Insert new fare card data (ORCA)
        // Returns bool based on success of insertion
        public bool InsertFCD(Dictionary<string, string> keyValuePairs)
        {
            // Get the path_id associated with this FareCardData using the file's start date and the assigned_route_id
            long? pathId = GetPathId(keyValuePairs);
            if (pathId == null)
            {
                // If no result, then create entries in Paths and Routes and return the created path_id
                pathId = InsertPath(keyValuePairs);
                Console.WriteLine($"Created path ID: {pathId}");
            }
            else
            {
                Console.WriteLine($"Path ID match: {pathId}");
            }
            string insertSQL =
                    @"INSERT INTO FareCardData 
                    (path_id, assigned_route_id, start_date, end_date, is_weekday, transit_operator, source_participant, service_participant, mode, route_direction, trip_start, boardings, file_id)
                VALUES (@path_id, @route_id, @start_date, @end_date, @is_weekday, @transit_operator, @source_participant, @service_participant, @mode, @route_direction, @trip_start, @boardings, @file_id)";
            using (SQLiteCommand command = new SQLiteCommand(insertSQL, sqliteConnection))
            {
                command.Parameters.Add(new SQLiteParameter("@path_id", pathId));
                command.Parameters.Add(new SQLiteParameter("@route_id", keyValuePairs["route_id"]));
                command.Parameters.Add(new SQLiteParameter("@is_weekday", keyValuePairs["is_weekday"]));
                command.Parameters.Add(new SQLiteParameter("@start_date", keyValuePairs["start_date"]));
                command.Parameters.Add(new SQLiteParameter("@end_date", keyValuePairs["end_date"]));
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
            return true;
        }

        public bool InsertBulkFCD(List<Dictionary<string, string>> bulkFCD)
        {
            var command = new SQLiteCommand("begin", sqliteConnection);
            command.ExecuteNonQuery();
            foreach (var keyValuePair in bulkFCD)
            {
                InsertFCD(keyValuePair);
            }
            command = new SQLiteCommand("end", sqliteConnection);
            command.ExecuteNonQuery();
            command.Dispose();
            return true;
        }

        public bool InsertBulkNFC(List<Dictionary<string, string>> bulkNFC) {
            var command = new SQLiteCommand("begin", sqliteConnection);
            command.ExecuteNonQuery();
            foreach (var keyValuePair in bulkNFC)
            {
                InsertNFC(keyValuePair);
            }
            command = new SQLiteCommand("end", sqliteConnection);
            command.ExecuteNonQuery();
            command.Dispose();
            return true;
        }

        // Insert non-fare card data into the database
        // Returns bool based on the success of the operation
        public bool InsertNFC(Dictionary<string, string> keyValuePairs)
        {
            // Get the path_id associated with this NonFareCard data using the file's start date and the assigned_route_id
            long? pathId = GetPathId(keyValuePairs);
            if (pathId == null)
            {
                // If no result, then create entries in Paths and Routes and return the created path_id
                pathId = InsertPath(keyValuePairs);
                Console.WriteLine($"Created path ID: {pathId}");
            }
            else
            {
                Console.WriteLine($"Path ID match: {pathId}");
            }
            string insertSQL =
                @"INSERT INTO NonFareCardData 
                    (path_id, assigned_route_id, start_date, end_date, is_weekday, route_direction, total_ridership, total_non_ridership, adult_cash_fare, youth_cash_fare, reduced_cash_fare, paper_transfer,
                    free_ride, personal_care_attendant, passenger_headcount, cash_fare_underpmnt, cash_upgrade, special_survey, wheelchair, bicycle, ferry_passenger_headcount, file_id) 
                VALUES (@path_id, @route_id, @start_date, @end_date, @is_weekday, @route_direction, @total_ridership, @total_non_ridership, @adult_cash_fare, @youth_cash_fare, 
                    @reduced_cash_fare, @paper_transfer, @free_ride, @personal_care_attendant, @passenger_headcount, @cash_fare_underpmnt, @cash_upgrade, @special_survey,
                    @wheelchair, @bicycle, @ferry_passenger_headcount, @file_id)";
            using (SQLiteCommand command = new SQLiteCommand(insertSQL, sqliteConnection))
            {
                command.Parameters.Add(new SQLiteParameter("@path_id", pathId));
                command.Parameters.Add(new SQLiteParameter("@route_id", keyValuePairs["route_id"]));
                command.Parameters.Add(new SQLiteParameter("@start_date", keyValuePairs["start_date"]));
                command.Parameters.Add(new SQLiteParameter("@end_date", keyValuePairs["end_date"]));
                command.Parameters.Add(new SQLiteParameter("@is_weekday", keyValuePairs["is_weekday"]));
                command.Parameters.Add(new SQLiteParameter("@route_direction", keyValuePairs["route_direction"]));
                command.Parameters.Add(new SQLiteParameter("@total_ridership", keyValuePairs["total_ridership"]));
                command.Parameters.Add(new SQLiteParameter("@total_non_ridership", keyValuePairs["total_non_ridership"]));
                command.Parameters.Add(new SQLiteParameter("@adult_cash_fare", keyValuePairs["adult_cash_fare"]));
                command.Parameters.Add(new SQLiteParameter("@youth_cash_fare", keyValuePairs["youth_cash_fare"]));
                command.Parameters.Add(new SQLiteParameter("@reduced_cash_fare", keyValuePairs["reduced_cash_fare"]));
                command.Parameters.Add(new SQLiteParameter("@paper_transfer", keyValuePairs["paper_transfer"]));
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
            return true;
        }


        // Insert Route Stop Data (from the CSV file)
        public bool InsertRSD(Dictionary<string, string> keyValuePairs)
        {
            try
            {
                string insertSQL =
                    @"INSERT INTO RouteStopData 
                        (location_id, assigned_stop_id, minus_door_1_person, minus_door_2_person, door_1_person, door_2_person, file_id, start_date, end_date) 
                    VALUES (@location_id, @stop_id, @minus_door_1_person, @minus_door_2_person, @door_1_person, @door_2_person, @file_id, @start_date, @end_date)";
                using (SQLiteCommand command = new SQLiteCommand(insertSQL, sqliteConnection))
                {
                    command.Parameters.Add(new SQLiteParameter("@location_id", keyValuePairs["location_id"]));
                    command.Parameters.Add(new SQLiteParameter("@stop_id", keyValuePairs["assigned_stop_id"]));
                    command.Parameters.Add(new SQLiteParameter("@start_date", keyValuePairs["start_date"]));
                    command.Parameters.Add(new SQLiteParameter("@end_date", keyValuePairs["end_date"]));
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

        // After creating a new report, we want to insert details about the creation in the DB
        public bool InsertReportHistory(Dictionary<string, string> keyValuePairs)
        {
            try
            {
                string insertSQL =
                    @"INSERT OR REPLACE INTO ReportHistory 
                        (report_location, datetime_created, report_range) 
                    VALUES (@report_location, @datetime_created, @report_range)";
                using (SQLiteCommand command = new SQLiteCommand(insertSQL, sqliteConnection))
                {
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

        // Insert a route (either new or a change to a route using an existing path)
        public bool InsertRoute(Dictionary<string, string> keyValuePairs)
        {
            string insertSQL =
                @"INSERT INTO Routes 
                    (path_id, assigned_route_id, start_date, route_name, district, distance_week, distance_sat, num_trips_week, 
                    num_trips_sat, num_trips_hol, weekday_hours, saturday_hours, holiday_hours) 
                VALUES (@path_id, @assigned_route_id, @start_date, @route_name, @district, @distance_week, @distance_sat, @num_trips_week,
                        @num_trips_sat, @num_trips_hol, @weekday_hours, @saturday_hours, @holiday_hours)";
            using (SQLiteCommand command = new SQLiteCommand(insertSQL, sqliteConnection))
            {
                command.Parameters.Add(new SQLiteParameter("@path_id", keyValuePairs["path_id"]));
                command.Parameters.Add(new SQLiteParameter("@assigned_route_id", keyValuePairs["route_id"]));
                command.Parameters.Add(new SQLiteParameter("@start_date", keyValuePairs["start_date"]));
                keyValuePairs.TryGetValue("route_name", out string route_name);
                command.Parameters.Add(new SQLiteParameter("@route_name", route_name));
                keyValuePairs.TryGetValue("district", out string district);
                command.Parameters.Add(new SQLiteParameter("@district", district));
                keyValuePairs.TryGetValue("distance_week", out string distance_week);
                command.Parameters.Add(new SQLiteParameter("@distance_week", distance_week));
                keyValuePairs.TryGetValue("distance_sat", out string distance_sat);
                command.Parameters.Add(new SQLiteParameter("@distance_sat", distance_sat));
                keyValuePairs.TryGetValue("num_trips_week", out string num_trips_week);
                command.Parameters.Add(new SQLiteParameter("@num_trips_week", num_trips_week));
                keyValuePairs.TryGetValue("num_trips_sat", out string num_trips_sat);
                command.Parameters.Add(new SQLiteParameter("@num_trips_sat", num_trips_sat));
                keyValuePairs.TryGetValue("num_trips_hol", out string num_trips_hol);
                command.Parameters.Add(new SQLiteParameter("@num_trips_hol", num_trips_hol));
                keyValuePairs.TryGetValue("weekday_hours", out string weekday_hours);
                command.Parameters.Add(new SQLiteParameter("@weekday_hours", weekday_hours));
                keyValuePairs.TryGetValue("saturday_hours", out string saturday_hours);
                command.Parameters.Add(new SQLiteParameter("@saturday_hours", saturday_hours));
                keyValuePairs.TryGetValue("holiday_hours", out string holiday_hours);
                command.Parameters.Add(new SQLiteParameter("@holiday_hours", holiday_hours));
                command.ExecuteNonQuery();
            }
            return true;
        }

        // Insert a route stop (either new or a change to a route stop using an existing master key) into the DB
        public bool InsertStop(Dictionary<string, string> keyValuePairs)
        {
            string insertSQL =
                @"INSERT INTO Stops 
                    (location_id, path_id, start_date, stop_name, assigned_stop_id) 
                VALUES (@stop_id, @start_date, @end_date, @stop_name, @assigned_stop_id)";
            using (SQLiteCommand command = new SQLiteCommand(insertSQL, sqliteConnection))
            {
                command.Parameters.Add(new SQLiteParameter("@location_id", keyValuePairs["location_id"]));
                command.Parameters.Add(new SQLiteParameter("@path_id", keyValuePairs["path_id"]));
                command.Parameters.Add(new SQLiteParameter("@start_date", keyValuePairs["start_date"]));
                try
                {
                    command.Parameters.Add(new SQLiteParameter("@stop_name", keyValuePairs["stop_name"]));
                    command.Parameters.Add(new SQLiteParameter("@assigned_stop_id", keyValuePairs["stop_id"]));
                }
                catch (KeyNotFoundException knfe)
                {
                    // Acceptable to not have this data
                }
                command.ExecuteNonQuery();
            }
            return true;
        }

        public bool InsertBulkPaths(List<Dictionary<string, string>> bulkPaths)
        {
            var command = new SQLiteCommand("begin", sqliteConnection);
            command.ExecuteNonQuery();
            foreach (var keyValuePair in bulkPaths)
            {
                InsertPath(keyValuePair);
            }
            command = new SQLiteCommand("end", sqliteConnection);
            command.ExecuteNonQuery();
            command.Dispose();
            return true;
        }

        // Creates a brand new path using available route information (NOT an update to an existing path)
        public long InsertPath(Dictionary<string, string> keyValuePairs)
        {
            // If route already exists (route_id and start_date already in Routes), then get the Path id and return it
            long? pathId = GetPathId(keyValuePairs);
            if (pathId != null)
            {
                return (long)pathId;
            }
            // Paths PK is set to auto increment
            string addToPaths = @"INSERT INTO Paths (path_id) VALUES (null)";
            using (SQLiteCommand masterCommand = new SQLiteCommand(addToPaths, sqliteConnection))
            {
                masterCommand.ExecuteNonQuery();
            }
            // Use the new master route id when inserting a route into the Routes table
            long path_id = sqliteConnection.LastInsertRowId;    
            keyValuePairs.Add("path_id", path_id.ToString());
            InsertRoute(keyValuePairs);
            return path_id;
        }

        // Creates a brand new stop location (NOT an update to an existing stop location)
        public long InsertStopLocation(Dictionary<string, string> keyValuePairs)
        {
            string addToStopLocs = @"INSERT INTO StopLocations (location_id) VALUES (null)";
            using (SQLiteCommand masterCommand = new SQLiteCommand(addToStopLocs, sqliteConnection))
            {
                masterCommand.ExecuteNonQuery();
            }
            // Use the new master route stop id when inserting a route stop into the RouteStops table
            long location_id = sqliteConnection.LastInsertRowId; 
            keyValuePairs.Add("location_id", location_id.ToString());
            InsertStop(keyValuePairs);
            return location_id;
        }

        // Creates a holiday using the holiday's date and service type (1 == HOLIDAY SERVICE, 2 == NO SERVICE)
        public bool InsertHoliday(Dictionary<string, string> keyValuePairs)
        {
            string insertSQL =
                    @"INSERT INTO Holidays 
                        (date, service_type) 
                    VALUES (@date, @service_type)";
            using (SQLiteCommand command = new SQLiteCommand(insertSQL, sqliteConnection))
            {
                command.Parameters.Add(new SQLiteParameter("@date", keyValuePairs["date"]));
                command.Parameters.Add(new SQLiteParameter("@service_type", keyValuePairs["service_type"]));
                command.ExecuteNonQuery();
            }
            return true;
        }

        public List<NameValueCollection> GetHolidaysInRange(List<DateTime> range)
        {
            var results = new List<NameValueCollection>();
            string query = @"SELECT date, service_type 
                                FROM Holidays 
                                WHERE @startDate <= Holidays.date AND @endDate >= Holidays.date";
            using (SQLiteCommand command = new SQLiteCommand(query, sqliteConnection))
            {
                command.Parameters.Add(new SQLiteParameter("@startDate", range[0].ToString("yyyy-MM-dd")));
                command.Parameters.Add(new SQLiteParameter("@endDate", range[1].ToString("yyyy-MM-dd")));
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

        // Given a district and a date range, return a list of all route's associated with that district
        public List<NameValueCollection> GetDistrictRoutes(string district, List<DateTime> reportRange)
        {
            string query = @"SELECT * 
                                FROM Routes 
                                WHERE district == @district AND start_date <= @report_start AND NOT start_date > @report_end";
            var results = new List<NameValueCollection>();
            using (var command = new SQLiteCommand(query, sqliteConnection))
            {
                command.Parameters.Add(new SQLiteParameter("@district", district));
                command.Parameters.Add(new SQLiteParameter("@report_start", reportRange[0]));
                command.Parameters.Add(new SQLiteParameter("@report_end", reportRange[1]));
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

        // Given a routeId, report range, get the ridership for a route during weekdays or saturdays (specified by isWeekday)
        public Dictionary<string, int> GetRouteRidership(int routeId, List<DateTime> reportRange, Boolean isWeekday)
        {
            var results = new Dictionary<string, int>();
            long? pathId = GetPathId(
                new Dictionary<string, string>{
                        { "route_id", routeId.ToString() },
                        { "start_date", reportRange[0].ToString("yyyy-MM-dd") }
                }
            );
            int fcTotal = 0;
            string fcQuery = @"SELECT SUM(fc.boardings) as fc_boardings  
                                FROM FareCardData as fc
                                WHERE fc.path_id == @path_id 
                                    AND fc.start_date >= @report_start 
                                    AND fc.end_date <= @report_end
                                    AND fc.is_weekday == @is_weekday";
            using (var command = new SQLiteCommand(fcQuery, sqliteConnection))
            {
                command.Parameters.Add(new SQLiteParameter("@path_id", pathId));
                command.Parameters.Add(new SQLiteParameter("@report_start", reportRange[0].ToString("yyyy-MM-dd")));
                command.Parameters.Add(new SQLiteParameter("@report_end", reportRange[1].ToString("yyyy-MM-dd")));
                command.Parameters.Add(new SQLiteParameter("@is_weekday", isWeekday.ToString()));
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var values = reader.GetValues();
                        string fcBoardingsStr = values["fc_boardings"];
                        if (!string.IsNullOrEmpty(fcBoardingsStr))
                        {
                            fcTotal += Convert.ToInt32(fcBoardingsStr);
                        }
                    }
                }
            }
            results.Add("fc_total", fcTotal);

            int nfcTotal = 0;
            string nfcQuery = @"SELECT SUM(nfc.total_ridership) as nfc_ridership, SUM(nfc.total_non_ridership) as nfc_non_ridership  
                                FROM NonFareCardData as nfc
                                WHERE nfc.path_id == @path_id 
                                    AND nfc.start_date >= @report_start 
                                    AND nfc.end_date <= @report_end
                                    AND nfc.is_weekday == @is_weekday";
            using (var command = new SQLiteCommand(nfcQuery, sqliteConnection))
            {
                command.Parameters.Add(new SQLiteParameter("@path_id", pathId));
                command.Parameters.Add(new SQLiteParameter("@report_start", reportRange[0].ToString("yyyy-MM-dd")));
                command.Parameters.Add(new SQLiteParameter("@report_end", reportRange[1].ToString("yyyy-MM-dd")));
                command.Parameters.Add(new SQLiteParameter("@is_weekday", isWeekday.ToString()));
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var values = reader.GetValues();
                        string ridershipStr = values["nfc_ridership"];
                        if (!string.IsNullOrEmpty(ridershipStr))
                        {
                            nfcTotal += Convert.ToInt32(ridershipStr);
                        }
                        /*string nonRidershipStr = values["nfc_non_ridership"];
                        if (!string.IsNullOrEmpty(nonRidershipStr))
                        {
                            nfcTotal += Convert.ToInt32(nonRidershipStr);
                        }*/
                    }
                }
            }
            results.Add("nfc_total", nfcTotal);
            results.Add("total", fcTotal + nfcTotal);
            return results;
        }

        public List<NameValueCollection> GetImportedFiles()
        {
            string query = "SELECT * FROM ImportedFiles";
            var importedFiles = new List<NameValueCollection>();
            using (var command = new SQLiteCommand(query, sqliteConnection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        NameValueCollection row = reader.GetValues();
                        importedFiles.Add(row);
                    }
                }
            }
            return importedFiles;
        }

        public List<NameValueCollection> GetValidRoutes(DateTime date)
        {
            var results = new List<NameValueCollection>();
            string query = @"SELECT *
                                FROM Routes
                                WHERE start_date <= @date
                                GROUP BY path_id";
            using (SQLiteCommand command = new SQLiteCommand(query, sqliteConnection))
            {
                command.Parameters.Add(new SQLiteParameter("@date", date.ToString("yyyy-MM-dd")));
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        NameValueCollection row = reader.GetValues();
                        foreach (string s in row)
                            foreach (string v in row.GetValues(s))
                                Console.WriteLine("{0} {1}", s, v);
                        results.Add(row);
                    }
                }
            }
            return results;
        }

        public List<NameValueCollection> GetLatestReports()
        {
            string query = "SELECT * FROM ReportHistory ORDER BY datetime_created DESC";
            var latestReports = new List<NameValueCollection>();
            using (var command = new SQLiteCommand(query, sqliteConnection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    for (int i = 0; i < 10 && reader.Read(); i++)
                    {
                        NameValueCollection row = reader.GetValues();
                        latestReports.Add(row);
                    }
                }
            }
            return latestReports;
        }

        public void DeleteImportedFile(long fileId, FileType fileType)
        {
            // Based on fileId, remove the file's entry from the ImportedFiles table
            string deleteFromFiles = $"DELETE FROM ImportedFiles WHERE file_id == @fileId";
            using (var command = new SQLiteCommand(deleteFromFiles, sqliteConnection))
            {
                command.Parameters.Add(new SQLiteParameter("@fileId", fileId));
                command.ExecuteNonQuery();
            }
            // Based on the fileId and fileType, remove all data related to the file from the appropriate data table
            string deleteFromData = string.Empty;
            switch (fileType)
            {
                case FileType.FC:
                    deleteFromData = $"DELETE FROM FareCardData WHERE file_id == @fileId";
                    break;
                case FileType.NFC:
                    deleteFromData = $"DELETE FROM NonFareCardData WHERE file_id == @fileId";
                    break;
                case FileType.RSD:
                    deleteFromData = $"DELETE FROM RouteStopData WHERE file_id == @fileId";
                    break;
            }
            using (var deleteDataCommand = new SQLiteCommand(deleteFromData, sqliteConnection))
            {
                deleteDataCommand.Parameters.Add(new SQLiteParameter("@fileId", fileId));
                deleteDataCommand.ExecuteNonQuery();
            }
        }

        public List<string> GetTableInfo(FileType fileType)
        {
            string tableInfoCmd;
            switch (fileType)
            {
                case FileType.FC:
                    tableInfoCmd = $"PRAGMA table_info(FareCardData)";
                    break;
                case FileType.NFC:
                    tableInfoCmd = $"PRAGMA table_info(NonFareCardData)";
                    break;
                case FileType.RSD:
                    tableInfoCmd = $"PRAGMA table_info(RouteStopData)";
                    break;
                default:
                    tableInfoCmd = $"PRAGMA table_info(FareCardData)";
                    break;
            }
            var columnNames = new List<string>();
            using (var command = new SQLiteCommand(tableInfoCmd, sqliteConnection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        //Console.WriteLine("Table Column: " + reader.GetString(1));
                        columnNames.Add(reader.GetString(1));
                    }
                }
            }
            return columnNames;
        }

        public List<string> GetRouteTableInfo()
        {
            string tableInfoCmd = $"PRAGMA table_info(Routes)";
            var columnNames = new List<string>();
            using (var command = new SQLiteCommand(tableInfoCmd, sqliteConnection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        //Console.WriteLine("Table Column: " + reader.GetString(1));
                        columnNames.Add(reader.GetString(1));
                    }
                }
            }
            return columnNames;
        }

        // A generic method for querying data from the database
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
            // Returns a list of NameValueCollections, which are like Dictionaries
            return results;
        }

        // Currently used for closing the Test Database before db file removal
        public void CloseDatabase()
        {
            sqliteConnection.Close();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }


        public void modifyRoute(string routeName, string option, string newTry)
        {
            option = option.Replace(" of", string.Empty)
                        .Replace(" per", string.Empty)
                        .Replace("number", "num")
                        .Replace("weekday", "week")
                        .Replace("saturday", "sat")
                        .Replace("holiday", "hol")
                        .Replace(' ', '_');
            try
            {


                string updateSQL = "UPDATE Routes SET " + option + " = " + "'" + newTry + "'" + " WHERE assigned_route_id = " + "'" + routeName + "'";
                Console.WriteLine(updateSQL);
                using (SQLiteCommand command = new SQLiteCommand(updateSQL, sqliteConnection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle.StackTrace);
            }
        }

        public void viewRoutes()
        {
            Console.WriteLine();
            Console.WriteLine("ALL Routes and associated data");
            var results = dbManagerInstance.Query(new string[] { "db_route_id", "path_id", "start_date", "route_name", "district", "distance_week", "distance_sat", "num_trips_week", "num_trips_sat",
                "num_trips_hol", "weekday_hours", "saturday_hours", "holiday_hours", "assigned_route_id" }, new string[] { "Routes" },
                "1 = 1");
            var resultStrs = new List<string>();
            foreach (var row in results)
            {
                string rowStr = "";
                foreach (string colName in row.AllKeys)
                {
                    if (rowStr.Length != 0)
                    {
                        rowStr += ", ";
                    }
                    rowStr += colName.ToString() + ": " + row[colName].ToString();
                }
                resultStrs.Add(rowStr);
                Console.WriteLine(rowStr);
            }
            Console.WriteLine();
        }


        public List<String> getRoutes()
        {
            var results = dbManagerInstance.Query(new string[] {"assigned_route_id"}, new string[] { "Routes" },
                           "1 = 1");
            var resultStrs = new List<string>();
            foreach (var row in results)
            {
                string rowStr = "";
                foreach (string colName in row.AllKeys)
                {
                    if (rowStr.Length != 0)
                    {
                        
                    }
                    rowStr += row[colName].ToString();
                }
                resultStrs.Add(rowStr);
                //Console.WriteLine(rowStr);
            }
            //List<String> distinct = resultStrs.Distinct().ToList();
            //distinct.Sort();
            return resultStrs;
        }

        public void addRouteinfo(String routeID, String start, String name, String district, String distance_week, String distance_sat, String tripsWeek,
                String tripsSat, String tripsHol, String weekdayHours, String satHours, String holHours)
        {

            var newRoute = new Dictionary<string, string>
                {
                    { "route_id", routeID },
                    { "start_date", start },
                    { "route_name", name },
                    { "district", district },
                    { "distance_week", distance_week },
                    { "distance_sat", distance_sat },
                    { "num_trips_week", tripsWeek },
                    { "num_trips_sat", tripsSat },
                    { "num_trips_hol", tripsHol },
                    { "weekday_hours", weekdayHours },
                    { "saturday_hours", satHours },
                    { "holiday_hours", holHours }
                };
            InsertPath(newRoute);
        }

        public void deleteRouteinfo(string route)
        {
            try
            {
               
                string updateSQL = "DELETE FROM Routes WHERE assigned_route_id = " + "'" + route + "'";
                using (SQLiteCommand command = new SQLiteCommand(updateSQL, sqliteConnection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle.StackTrace);
            }

            try
            {
             
                string updateSQL = "DELETE FROM Paths WHERE path_id = " + "'" + route + "'";
                using (SQLiteCommand command = new SQLiteCommand(updateSQL, sqliteConnection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle.StackTrace);
            }
        }

        public void deleteAllRouteinfo()
        {
            try
            {
                
                string updateSQL = "DELETE FROM Routes WHERE assigned_route_id >= 0";
                using (SQLiteCommand command = new SQLiteCommand(updateSQL, sqliteConnection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle.StackTrace);
            }

            try
            {

                string updateSQL = "DELETE FROM Paths WHERE path_id >= 0";
                using (SQLiteCommand command = new SQLiteCommand(updateSQL, sqliteConnection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle.StackTrace);
            }

            try
            {

                string updateSQL = "DELETE FROM FareCardData WHERE path_id >= 0";
                using (SQLiteCommand command = new SQLiteCommand(updateSQL, sqliteConnection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle.StackTrace);
            }
        }

        public void addStop(String stopName, String locationName, String locationId, String stopId, String pathId, String startDate, String minusDoor1, String minusDoor2,
                String door1, String door2)
        {
            var stop1 = new Dictionary<string, string>
                {
                    { "sd_id", stopId },
                    { "location_id", locationId },
                    { "assigned_stop_id", stopId },
                    { "minus_door_1_person", minusDoor1 },
                    { "minus_door_2_person", minusDoor2 },
                    { "door_1_person", door1 },
                    { "door_2_person", door2 },
                    { "file_id", 2.ToString() }
                };
            InsertRSD(stop1);

            var stops1 = new Dictionary<string, string>
                {
                    { "location_id", locationId },
                    { "location_name", locationName }
                };
            //InsertStopLocation(stops1);

            var stop2 = new Dictionary<string, string>
                {
                    { "stop_id", stopId },
                    { "location_id", locationId },
                    { "path_id", pathId },
                    { "start_date", startDate },
                    { "stop_name", stopName },
                    { "assigned_stop_id", stopId }
                };
            //InsertStop(stop2);

        }

        public void viewRouteStops()
        {
            Console.WriteLine();
            Console.WriteLine("ALL Stops and associated data");
            var results = dbManagerInstance.Query(new string[] { "sd_id", "location_id", "assigned_stop_id", "minus_door_1_person", "minus_door_2_person", "door_1_person", "door_2_person", "file_id"
                 }, new string[] { "routeStopData" },
                "sd_id >= 0");
            var resultStrs = new List<string>();
            foreach (var row in results)
            {
                string rowStr = "";
                foreach (string colName in row.AllKeys)
                {
                    if (rowStr.Length != 0)
                    {
                        rowStr += ", ";
                    }
                    rowStr += colName.ToString() + ": " + row[colName].ToString();
                }
                resultStrs.Add(rowStr);
                Console.WriteLine(rowStr);
            }
            Console.WriteLine();
        }

        public void modifyStop(string routeStop, string option, string newTry)
        {

            option = option.Replace("(-)", string.Empty)
                        .Replace(' ', '_');

            try
            {


                string updateSQL = "UPDATE RouteStopData SET " + option + " = " + "'" + newTry + "'" + " WHERE location_id = " + "'" + routeStop + "'";
                Console.WriteLine(updateSQL);
                using (SQLiteCommand command = new SQLiteCommand(updateSQL, sqliteConnection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle.StackTrace);
            }
        }

        public List<String> getStops()
        {
            var results = dbManagerInstance.Query(new string[] { "location_id" }, new string[] { "routeStopData" },
                           "location_id > 0");
            var resultStrs = new List<string>();
            foreach (var row in results)
            {
                string rowStr = "";
                foreach (string colName in row.AllKeys)
                {
                    if (rowStr.Length != 0)
                    {
                        rowStr += "";
                    }
                    rowStr += row[colName].ToString();
                }
                resultStrs.Add(rowStr);
            }
            List<String> distinct = resultStrs.Distinct().ToList();
            return distinct;
        }

        public void viewFCD()
        {
            Console.WriteLine();
            Console.WriteLine("FCD Stuff");
            var results = dbManagerInstance.Query(new string[] { "fc_id", "path_id", "assigned_route_id", "route_direction", "boardings", "file_id"
                 }, new string[] { "FareCardData" },
                "fc_id > 0");
            var resultStrs = new List<string>();
            foreach (var row in results)
            {
                string rowStr = "";
                foreach (string colName in row.AllKeys)
                {
                    if (rowStr.Length != 0)
                    {
                        rowStr += ", ";
                    }
                    rowStr += colName.ToString() + ": " + row[colName].ToString();
                }
                resultStrs.Add(rowStr);
                Console.WriteLine(rowStr);
            }
            Console.WriteLine();
        }

        public void getFCDRoutes()
        {
            var results = dbManagerInstance.Query(new string[] { "assigned_route_id" }, new string[] { "FareCardData" },
                           "fc_id > 0");
            var resultStrs = new List<string>();
            foreach (var row in results)
            {
                string rowStr = "";
                foreach (string colName in row.AllKeys)
                {
                    if (rowStr.Length != 0)
                    {
                        rowStr += "";
                    }
                    rowStr += row[colName].ToString();
                }
                resultStrs.Add(rowStr);
            }
            List<String> distinct = resultStrs.Distinct().ToList();
            String empty = "";
            foreach (String all in distinct)
            {
                var stored = getRoutes();
                if (!stored.Contains(all))
                {
                    var newRoute = new Dictionary<string, string>
                    {
                    { "path_id", all },
                    { "route_id", empty },
                    { "start_date", empty },
                    { "route_name", "(NO NAME)" },
                    { "district", empty },
                    { "distance", empty },
                    { "num_trips_week", empty },
                    { "num_trips_sat", empty },
                    { "num_trips_hol", empty },
                    { "weekday_hours", empty },
                    { "saturday_hours", empty },
                    { "holiday_hours", empty }
                };
                    InsertRoute(newRoute);
                }
            }
        }

        public void viewNFC()
        {
            Console.WriteLine();
            Console.WriteLine("NFC Stuff");
            var results = dbManagerInstance.Query(new string[] { "nfc_id", "path_id", "assigned_route_id", "route_direction", "ferry_passenger_headcount", "file_id"
                 }, new string[] { "NonFareCardData" },
                "nfc_id > 0");
            var resultStrs = new List<string>();
            foreach (var row in results)
            {
                string rowStr = "";
                foreach (string colName in row.AllKeys)
                {
                    if (rowStr.Length != 0)
                    {
                        rowStr += ", ";
                    }
                    rowStr += colName.ToString() + ": " + row[colName].ToString();
                }
                resultStrs.Add(rowStr);
                Console.WriteLine(rowStr);
            }
            Console.WriteLine();
        }

        public void getNFCRoutes()
        {
            var results = dbManagerInstance.Query(new string[] { "assigned_route_id" }, new string[] { "NonFareCardData" },
                           "nfc_id >= 0");
            var resultStrs = new List<string>();
            foreach (var row in results)
            {
                string rowStr = "";
                foreach (string colName in row.AllKeys)
                {
                    if (rowStr.Length != 0)
                    {
                        rowStr += "";
                    }
                    rowStr += row[colName].ToString();
                }
                resultStrs.Add(rowStr);
            }
            List<String> distinct = resultStrs.Distinct().ToList();
            String empty = "";
            foreach (String all in distinct)
            {
                var stored = getRoutes();
                if (!stored.Contains(all))
                {
                    var newRoute = new Dictionary<string, string>
                    {
                    { "path_id", all },
                    { "route_id", empty },
                    { "start_date", empty },
                    { "route_name", "(NO NAME)" },
                    { "district", empty },
                    { "distance", empty },
                    { "num_trips_week", empty },
                    { "num_trips_sat", empty },
                    { "num_trips_hol", empty },
                    { "weekday_hours", empty },
                    { "saturday_hours", empty },
                    { "holiday_hours", empty }
                };
                    InsertRoute(newRoute);
                }
            }
        }
    }
}

