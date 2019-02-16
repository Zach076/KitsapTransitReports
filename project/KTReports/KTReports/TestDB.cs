using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KTReports
{
    public class TestDB
    {
        DatabaseManager dbManager = null;
        public TestDB()
        {
            dbManager = DatabaseManager.GetDBManager("TestDatabase");
        }

        public void TestInsertions()
        {
            // Insert new file information into the database
            long? file_id = dbManager.InsertNewFile("AUGUST 2018 ORCA Boardings by Route BY TRIP.XLS",
                "C:\\AUGUST 2018 ORCA Boardings by Route BY TRIP.XLS", DatabaseManager.FileType.FC, "2019-02-16", "2018-08-01", "2018-08-31");
            if (file_id == null)
            {
                return;
            }
            dbManager.InsertNewFile("AUGUST 2018 Non-Fare Card Activity by Route WEEKDAY.XLS", "C:\\AUGUST 2018 Non-Fare Card Activity by Route WEEKDAY.XLS", 
                DatabaseManager.FileType.FC, "2019-02-16", "2018-08-01", "2018-08-31");

            // Insert new routes into the database
            var route11 = new Dictionary<string, string>
                {
                    { "route_id", 11.ToString() },
                    { "start_date", "2017-01-01" },
                    { "route_name", "Crosstown Limited" },
                    { "district", "Bremerton" },
                    { "distance", 9.45.ToString() },
                    { "num_trips_week", 8.ToString() },
                    { "num_trips_sat", 6.ToString() },
                    { "num_trips_hol", 0.ToString() },
                    { "weekday_hours", 3.ToString() },
                    { "saturday_hours", 2.5.ToString() },
                    { "holiday_hours", 0.ToString() }
                };
            dbManager.InsertPath(route11);

            var route12 = new Dictionary<string, string>
                {
                    { "route_id", 12.ToString() },
                    { "start_date", "2017-01-01" },
                    { "route_name", "Silverdale West" },
                    { "district", "Central Kitsap" },
                    { "distance", 5.5.ToString() },
                    { "num_trips_week", 9.ToString() },
                    { "num_trips_sat", 7.ToString() },
                    { "num_trips_hol", 5.ToString() },
                    { "weekday_hours", 4.ToString() },
                    { "saturday_hours", 3.5.ToString() },
                    { "holiday_hours", 3.ToString() }
                };
            long? path_id = dbManager.InsertPath(route12);
            if (path_id == null) return;
            var route12Future = new Dictionary<string, string>
                {
                    { "path_id",  path_id.ToString()},
                    { "route_id", 12.ToString() },
                    { "start_date", "2019-05-01" },
                    { "route_name", "Silverdale West" },
                    { "district", "Central Kitsap" },
                    { "distance", 10.5.ToString() },
                    { "num_trips_week", 15.ToString() },
                    { "num_trips_sat", 8.ToString() },
                    { "num_trips_hol", 5.ToString() },
                    { "weekday_hours", 6.ToString() },
                    { "saturday_hours", 5.5.ToString() },
                    { "holiday_hours", 3.ToString() }
                };
            dbManager.InsertRoute(route12Future);

            // Insert new fare card data into the database
            var fcd1 = new Dictionary<string, string>
                {
                    { "route_id", 11.ToString() },
                    { "is_weekday", false.ToString() },
                    { "start_date", "2018-08-01" },
                    { "end_date", "2018-08-31" },
                    { "transit_operator", "Kitsap Transit" },
                    { "source_participant", "Kitsap Transit" },
                    { "service_participant", "Kitsap Transit" },
                    { "mode", "Bus" },
                    { "route_direction", "Inbound" },
                    { "trip_start", "10:00AM" },
                    { "boardings", 36.ToString() },
                    { "file_id", file_id.ToString() }
                };
            dbManager.InsertFCD(fcd1);

            var fcd2 = new Dictionary<string, string>
                {
                    { "route_id", 11.ToString() },
                    { "is_weekday", false.ToString() },
                    { "start_date", "2018-08-01" },
                    { "end_date", "2018-08-31" },
                    { "transit_operator", "Kitsap Transit" },
                    { "source_participant", "Kitsap Transit" },
                    { "service_participant", "Kitsap Transit" },
                    { "mode", "Bus" },
                    { "route_direction", "Inbound" },
                    { "trip_start", "11:00AM" },
                    { "boardings", 42.ToString() },
                    { "file_id", file_id.ToString() }
                };
            dbManager.InsertFCD(fcd2);

            var fcd3 = new Dictionary<string, string>
                {
                    { "route_id", 12.ToString() },
                    { "is_weekday", false.ToString() },
                    { "start_date", "2018-08-01" },
                    { "end_date", "2018-08-31" },
                    { "transit_operator", "Kitsap Transit" },
                    { "source_participant", "Kitsap Transit" },
                    { "service_participant", "Kitsap Transit" },
                    { "mode", "Bus" },
                    { "route_direction", "Outbound" },
                    { "trip_start", "10:30AM" },
                    { "boardings", 14.ToString() },
                    { "file_id", file_id.ToString() }
                };
            dbManager.InsertFCD(fcd3);
        }

        public void TestQueries()
        {
            //Test1();
            //Test2();
            //Test3();
        }

        public void CheckTestMatch(List<string> resultStrs, string[] truth, int testNum)
        {
            if (!Enumerable.SequenceEqual(resultStrs, truth))
            {
                string expectedStr = "Expected: ";
                foreach (var truthStr in truth)
                {
                    expectedStr += truthStr + "\n";
                }
                throw new Exception("Failed Test" + testNum + "\n" + expectedStr);
            }
            else
            {
                Console.WriteLine($"Passed Test{testNum}!");
            }
        }

        public void Test1()
        {
            Console.WriteLine("Starting Test1...");
            var results = dbManager.Query(new string[] { "*" }, new string[] { "ImportedFiles" }, "date(\"1980-02-05\") > import_date AND file_type == \"FC\"");
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
            // truth is the expected results from the query
            string[] truth = { "file_id: 1, name: test_file_name.csv, dir_location: C:\\folder\\kt, file_type: FC, import_date: 1980-02-03" };
            CheckTestMatch(resultStrs, truth, 1);
        }

        public void Test2()
        {
            Console.WriteLine("Starting Test2...");
            var results = dbManager.Query(new string[] { "*" }, new string[] { "ImportedFiles" }, "date(\"1980-01-15\") > import_date");
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
            string[] truth = { };
            CheckTestMatch(resultStrs, truth, 2);
        }

        public void Test3()
        {
            Console.WriteLine("Starting Test3...");
            var results = dbManager.Query(new string[] { "fc_id", "route_name", "boardings" }, new string[] { "FareCardData as f, Routes as r" }, 
                "boardings > 200 AND f.route_id == r.route_id");
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
            string[] truth = { "fc_id: 1, route_name: The Best Route, boardings: 536",
                                "fc_id: 2, route_name: Route Num 50, boardings: 205"};
            CheckTestMatch(resultStrs, truth, 3);
        }

        public void RemoveDB()
        {
            dbManager.CloseDatabase();
            File.Delete("TestDatabase.sqlite3");
        }
    }
}
