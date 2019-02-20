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
            long fc_file_id = dbManager.InsertNewFile("AUGUST 2018 ORCA Boardings by Route BY TRIP",
                "C:\\AUGUST 2018 ORCA Boardings by Route BY TRIP.XLS", DatabaseManager.FileType.FC, "2019-02-16");
            long nfc_file_id = dbManager.InsertNewFile("AUGUST 2018 Non-Fare Card Activity by Route WEEKDAY", "C:\\AUGUST 2018 Non-Fare Card Activity by Route WEEKDAY.XLS", 
                DatabaseManager.FileType.NFC, "2019-02-16");

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
            long route11PathId = dbManager.InsertPath(route11);

            var route20 = new Dictionary<string, string>
                {
                    { "route_id", 20.ToString() },
                    { "start_date", "2017-01-01" },
                    { "route_name", "Navy Yard City" },
                    { "district", "Bremerton" },
                    { "distance", 20.ToString() },
                    { "num_trips_week", 11.ToString() },
                    { "num_trips_sat", 9.ToString() },
                    { "num_trips_hol", 2.ToString() },
                    { "weekday_hours", 5.5.ToString() },
                    { "saturday_hours", 4.5.ToString() },
                    { "holiday_hours", 2.5.ToString() }
                };
            long route20PathId = dbManager.InsertPath(route20);

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
            long route12PathId = dbManager.InsertPath(route12);

            var route12Future = new Dictionary<string, string>
                {
                    { "path_id",  route12PathId.ToString()},
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
                    { "start_date", "2019-02-01" },
                    { "end_date", "2019-02-28" },
                    { "transit_operator", "Kitsap Transit" },
                    { "source_participant", "Kitsap Transit" },
                    { "service_participant", "Kitsap Transit" },
                    { "mode", "Bus" },
                    { "route_direction", "Inbound" },
                    { "trip_start", "10:00AM" },
                    { "boardings", 36.ToString() },
                    { "file_id", fc_file_id.ToString() }
                };
            dbManager.InsertFCD(fcd1);

            var fcd2 = new Dictionary<string, string>
                {
                    { "route_id", 11.ToString() },
                    { "is_weekday", false.ToString() },
                    { "start_date", "2019-02-01" },
                    { "end_date", "2019-02-28" },
                    { "transit_operator", "Kitsap Transit" },
                    { "source_participant", "Kitsap Transit" },
                    { "service_participant", "Kitsap Transit" },
                    { "mode", "Bus" },
                    { "route_direction", "Inbound" },
                    { "trip_start", "11:00AM" },
                    { "boardings", 42.ToString() },
                    { "file_id", fc_file_id.ToString() }
                };
            dbManager.InsertFCD(fcd2);

            var fcd3 = new Dictionary<string, string>
                {
                    { "route_id", 12.ToString() },
                    { "is_weekday", false.ToString() },
                    { "start_date", "2019-02-01" },
                    { "end_date", "2019-02-28" },
                    { "transit_operator", "Kitsap Transit" },
                    { "source_participant", "Kitsap Transit" },
                    { "service_participant", "Kitsap Transit" },
                    { "mode", "Bus" },
                    { "route_direction", "Outbound" },
                    { "trip_start", "10:30AM" },
                    { "boardings", 14.ToString() },
                    { "file_id", fc_file_id.ToString() }
                };
            dbManager.InsertFCD(fcd3);

            var fcd4 = new Dictionary<string, string>
                {
                    { "route_id", 11.ToString() },
                    { "is_weekday", true.ToString() },
                    { "start_date", "2019-02-01" },
                    { "end_date", "2019-02-28" },
                    { "transit_operator", "Kitsap Transit" },
                    { "source_participant", "Kitsap Transit" },
                    { "service_participant", "Kitsap Transit" },
                    { "mode", "Bus" },
                    { "route_direction", "Outbound" },
                    { "trip_start", "10:30AM" },
                    { "boardings", 1000.ToString() },
                    { "file_id", fc_file_id.ToString() }
                };
            dbManager.InsertFCD(fcd4);

            var nfc1 = new Dictionary<string, string>
                {
                    { "route_id", 11.ToString() },
                    { "is_weekday", true.ToString() },
                    { "start_date", "2019-02-01" },
                    { "end_date", "2019-02-28" },
                    { "route_direction", "Inbound" },
                    { "total_ridership", 1047.ToString() },
                    { "total_non_ridership", 512.ToString() },
                    { "adult_cash_fare", 800.ToString() },
                    { "youth_cash_fare", 12.ToString() },
                    { "reduced_cash_fare", 321.ToString() },
                    { "paper_transfer", 123.ToString() },
                    { "free_ride", 2.ToString() },
                    { "personal_care_attendant", 78.ToString() },
                    { "passenger_headcount", 546.ToString() },
                    { "cash_fare_underpmnt", 0.ToString() },
                    { "cash_upgrade", 44.ToString() },
                    { "special_survey", 1.ToString() },
                    { "wheelchair", 23.ToString() },
                    { "bicycle", 9.ToString() },
                    { "ferry_passenger_headcount", 7.ToString() },
                    { "file_id", nfc_file_id.ToString() }
                };
            dbManager.InsertNFC(nfc1);
        }
        public void RemoveDB()
        {
            dbManager.CloseDatabase();
            File.Delete("TestDatabase.sqlite3");
        }
    }
}
