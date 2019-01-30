using System;
using System.Collections.Generic;
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
            dbManager = DatabaseManager.GetDBManager();
        }

        public void TestInsertions()
        {
            // Insert new file information into the database
            long? file_id = dbManager.InsertNewFile("test_file_name.csv", "C:\\folder\\kt", DatabaseManager.FileType.FC, new string[] { "1980-01-01", "1980-01-31" });
            if (file_id == null)
            {
                return;
            }
            dbManager.InsertNewFile("no_data_file.csv", "C:\\random\\dir", DatabaseManager.FileType.FC, new string[] { "1980-02-01", "1980-02-31" });

            // Insert new fare card data into the database
            var fcd1 = new Dictionary<string, string>
                {
                    { "route_id", 90.ToString() },
                    { "is_weekday", false.ToString() },
                    { "transit_operator", "Kitsap Transit" },
                    { "source_participant", "Kitsap Transit" },
                    { "service_participant", "Kitsap Transit" },
                    { "mode", "Bus" },
                    { "route_direction", "Inbound" },
                    { "trip_start", "10:00" },
                    { "boardings", 536.ToString() },
                    { "file_id", file_id.ToString() }
                };
            dbManager.InsertFCD(fcd1);

            var fcd2 = new Dictionary<string, string>
                {
                    { "route_id", 50.ToString() },
                    { "is_weekday", true.ToString() },
                    { "transit_operator", "Kitsap Transit" },
                    { "source_participant", "Kitsap Transit" },
                    { "service_participant", "Kitsap Transit" },
                    { "mode", "Bus" },
                    { "route_direction", "Outbound" },
                    { "trip_start", "14:00" },
                    { "boardings", 205.ToString() },
                    { "file_id", file_id.ToString() }
                };
            dbManager.InsertFCD(fcd2);

            var fcd3 = new Dictionary<string, string>
                {
                    { "route_id", 90.ToString() },
                    { "is_weekday", false.ToString() },
                    { "transit_operator", "Kitsap Transit" },
                    { "source_participant", "Kitsap Transit" },
                    { "service_participant", "Kitsap Transit" },
                    { "mode", "Bus" },
                    { "route_direction", "Outbound" },
                    { "trip_start", "12:00" },
                    { "boardings", 170.ToString() },
                    { "file_id", file_id.ToString() }
                };
            dbManager.InsertFCD(fcd3);

            // Insert new routes into the database
            var route90 = new Dictionary<string, string>
                {
                    { "assigned_route_id", 90.ToString() },
                    { "start_date", "1975-01-01" },
                    // Leave out end_date
                    { "route_name", "The Best Route" },
                    { "district", "Bremerton" },
                    { "distance", 9.45.ToString() },
                    { "num_trips_week", 8.ToString() },
                    { "num_trips_sat", 6.ToString() },
                    { "num_trips_hol", 0.ToString() },
                    { "weekday_hours", 3.ToString() },
                    { "saturday_hours", 2.5.ToString() },
                    { "holiday_hours", 0.ToString() }
                };
            dbManager.InsertNewRoute(route90);

            var route50 = new Dictionary<string, string>
                {
                    { "assigned_route_id", 50.ToString() },
                    { "start_date", "1975-01-01" },
                    // Leave out end_date
                    { "route_name", "Route Num 50" },
                    { "district", "Poulsbo" },
                    { "distance", 5.5.ToString() },
                    { "num_trips_week", 9.ToString() },
                    { "num_trips_sat", 7.ToString() },
                    { "num_trips_hol", 5.ToString() },
                    { "weekday_hours", 4.ToString() },
                    { "saturday_hours", 3.5.ToString() },
                    { "holiday_hours", 3.ToString() }
                };
            dbManager.InsertNewRoute(route50);
        }

        public void TestQueries()
        {
            Test1();
            Test2();
            Test3();
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
            var results = dbManager.Query(new string[] { "*" }, new string[] { "ImportedFiles" }, "date(\"1980-02-01\") > start_date");
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
            string[] truth = { "file_id: 1, name: test_file_name.csv, dir_location: C:\\folder\\kt, file_type: FC, start_date: 1980-01-01, end_date: 1980-01-31" };
            CheckTestMatch(resultStrs, truth, 1);
        }

        public void Test2()
        {
            Console.WriteLine("Starting Test2...");
            var results = dbManager.Query(new string[] { "*" }, new string[] { "ImportedFiles" }, "date(\"1980-01-15\") > end_date");
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
            var results = dbManager.Query(new string[] { "fc_id, route_id, boardings, i.file_id" }, new string[] { "FareCardData as f, ImportedFiles as i" }, 
                "f.route_id == 90 AND f.file_id == i.file_id AND i.end_date < date(\"1980-05-01\")");
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
            string[] truth = { "fc_id: 1, route_id: 90, boardings: 536, file_id: 1",
                                "fc_id: 3, route_id: 90, boardings: 170, file_id: 1"};
            CheckTestMatch(resultStrs, truth, 3);
        }
    }
}
