using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace TimeHaven
{
    public class MainDatabaseManager
    {
        private static readonly MainDatabaseManager _instance = new MainDatabaseManager();
        public static MainDatabaseManager Instance => _instance;

        string today = DateTime.Now.ToString("yyyy-MM-dd"); 
        public event Action DayUpdate = delegate { };
        public event Action HistoryUpdate = delegate { };

        private readonly string DatabaseFile;
        private readonly string ConnectionString;
        private MainDatabaseManager()
        {
                DatabaseFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "timehaven.db");
                ConnectionString = $"Data Source={DatabaseFile};Version=3;";
                InitializeDatabase();
                Debug.WriteLine($"Путь к БД2: {DatabaseFile}");
        }

        private void InitializeDatabase()
        {
            try
            {
                if (!File.Exists(DatabaseFile))
                {
                    SQLiteConnection.CreateFile(DatabaseFile);
                }

                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    Console.WriteLine("База данных успешно открыта.");

                    EnsureTableExists(connection, "DayStats", @"CREATE TABLE IF NOT EXISTS DayStats (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                hour INTEGER NOT NULL,
                minutes INTEGER NOT NULL,
                date TEXT NOT NULL,
                UNIQUE(hour, date)
            );");
                    Console.WriteLine("Таблица DayStats создана или уже существует.");

                    EnsureTableExists(connection, "HistoryStats", @"CREATE TABLE IF NOT EXISTS HistoryStats (
                date TEXT PRIMARY KEY NOT NULL,
                totalMinutes INTEGER NOT NULL DEFAULT 0
            ) WITHOUT ROWID;");
                    Console.WriteLine("Таблица HistoryStats создана или уже существует.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при инициализации базы данных: " + ex.Message);
            }

            ClearTestData();                          //////////////////////////////////////
            FillTestMonthData(today);                 //////////////////////////////////////
            FillTestDayData(today, DateTime.Now.Hour);//////////////////////////////////////
        }


        private void EnsureTableExists(SQLiteConnection connection, string tableName, string createTableSql)
        {
            string checkTableSql = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';";

            using (var command = new SQLiteCommand(checkTableSql, connection))
            {
                object result = command.ExecuteScalar();
                if (result == null)
                {
                    using (var createCommand = new SQLiteCommand(createTableSql, connection))
                    {
                        createCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        public void IncrementDayStats()
        {
            CleanupOldDayStats();

            int currentHour = DateTime.Now.Hour;
            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                INSERT INTO DayStats (hour, minutes, date)
                VALUES (@hour, 1, @date) 
                ON CONFLICT(hour, date) 
                DO UPDATE SET minutes = minutes + 1;";
                    command.Parameters.AddWithValue("@hour", currentHour);
                    command.Parameters.AddWithValue("@date", currentDate);
                    command.ExecuteNonQuery();
                }
            }
            DayUpdate?.Invoke();
        }


        public void IncrementHistoryStats()
        {
            CleanupOldHistoryStats();

            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                INSERT INTO HistoryStats (date, totalMinutes)
                VALUES (@date, 6)
                ON CONFLICT(date) DO UPDATE SET totalMinutes = totalMinutes + 6;
            ";
                    command.Parameters.AddWithValue("@date", currentDate);
                    command.ExecuteNonQuery();
                }
            }
            HistoryUpdate?.Invoke();
        }

        public (string day, double hours)[] GetLastNDaysData(int daysCount)
        {
            (string day, double hours)[] daysData = new (string, double)[daysCount];

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(@"
            SELECT date, totalMinutes FROM HistoryStats 
            WHERE date >= @startDate 
            ORDER BY date ASC", connection))
                {
                    string startDate = DateTime.Now.AddDays(-(daysCount - 1)).ToString("yyyy-MM-dd");
                    command.Parameters.AddWithValue("@startDate", startDate);

                    using (var reader = command.ExecuteReader())
                    {
                        var resultDict = Enumerable.Range(0, daysCount)
                            .ToDictionary(i => DateTime.Now.AddDays(-(daysCount - 1) + i).ToString("yyyy-MM-dd"), _ => 0.0);

                        while (reader.Read())
                        {
                            string date = reader.GetString(0);
                            double hours = Math.Round(reader.GetInt32(1) / 60.0, 1);
                            resultDict[date] = hours;
                        }

                        int index = 0;
                        foreach (var entry in resultDict)
                        {
                            daysData[index++] = (DateTime.Parse(entry.Key).ToString("dd.MM"), entry.Value);
                        }
                    }
                }
            }
            return daysData;
        }


        private void CleanupOldDayStats()
        {
            string cutoffDate = DateTime.Now.AddHours(-24).ToString("yyyy-MM-dd");
            int cutoffHour = DateTime.Now.AddHours(-24).Hour;

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(@"
            DELETE FROM DayStats 
            WHERE (date < @cutoffDate) 
               OR (date = @cutoffDate AND hour < @cutoffHour);", connection))
                {
                    command.Parameters.AddWithValue("@cutoffDate", cutoffDate);
                    command.Parameters.AddWithValue("@cutoffHour", cutoffHour);
                    command.ExecuteNonQuery();
                }
            }
        }
        private void CleanupOldHistoryStats()
        {
            string cutoffDate = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(@"
            DELETE FROM HistoryStats 
            WHERE date < @cutoffDate;", connection))
                {
                    command.Parameters.AddWithValue("@cutoffDate", cutoffDate);
                    command.ExecuteNonQuery();
                }
            }
        }

        public (int hour, int minutes)[] GetLast24HoursData()
        {
            (int hour, int minutes)[] hoursData = new (int, int)[24];

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(@"
            SELECT hour, SUM(minutes) 
            FROM DayStats
            WHERE date >= @yesterday
            GROUP BY hour
            ORDER BY hour ASC;", connection))
                {
                    string yesterday = DateTime.Now.AddHours(-24).ToString("yyyy-MM-dd");
                    command.Parameters.AddWithValue("@yesterday", yesterday);

                    using (var reader = command.ExecuteReader())
                    {
                        var resultDict = Enumerable.Range(0, 24)
                            .ToDictionary(i => (DateTime.Now.Hour - i + 24) % 24, _ => 0);

                        while (reader.Read())
                        {
                            int hour = reader.GetInt32(0);
                            int minutes = reader.GetInt32(1);
                            resultDict[hour] = minutes;
                        }

                        var orderedHours = resultDict
                        .OrderByDescending(e => (DateTime.Now.Hour - e.Key + 24) % 24)
                        .ToArray();


                        for (int i = 0; i < 24; i++)
                        {
                            hoursData[i] = (orderedHours[i].Key, orderedHours[i].Value);
                        }
                    }
                }
            }
            return hoursData;
        }


        public void FillTestMonthData(string todayDate)
        {
            Random random = new Random();
            DateTime startDate = DateTime.Parse(todayDate).AddDays(-29);

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    for (int i = 0; i < 30; i++)
                    {
                        if (random.NextDouble() < 0.35) continue;

                        string date = startDate.AddDays(i).ToString("yyyy-MM-dd");
                        int totalMinutes = random.Next(54, 120) * 5;

                        command.CommandText = @"
                INSERT INTO HistoryStats (date, totalMinutes)
                VALUES (@date, @minutes)
                ON CONFLICT(date) DO UPDATE SET totalMinutes = @minutes;";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@date", date);
                        command.Parameters.AddWithValue("@minutes", totalMinutes);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public void FillTestDayData(string todayDate, int currentHour)
        {
            Random random = new Random();
            DateTime startDate = DateTime.Parse(todayDate);

            HashSet<int> missingHours = new HashSet<int>();
            while (missingHours.Count < 9)
            {
                missingHours.Add(random.Next(0, 24));
            }

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    for (int i = 0; i < 24; i++)
                    {
                        int hour = (currentHour - 23 + i + 24) % 24;
                        if (missingHours.Contains(hour)) continue;

                        int minutes = random.Next(20, 61);

                        command.CommandText = @"
                INSERT INTO DayStats (hour, minutes, date)
                VALUES (@hour, @minutes, @date)
                ON CONFLICT(hour, date) DO UPDATE SET minutes = @minutes;";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@hour", hour);
                        command.Parameters.AddWithValue("@minutes", minutes);
                        command.Parameters.AddWithValue("@date", startDate.ToString("yyyy-MM-dd"));
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public void DeleteDatabase()
        {
            try
            {
                if (File.Exists(DatabaseFile))
                {
                    File.Delete(DatabaseFile);
                    Console.WriteLine("База данных удалена.");
                }
                else
                {
                    Console.WriteLine("Файл базы данных не найден.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при удалении базы данных: " + ex.Message);
            }
        }

        public void ClearTestData()
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "DELETE FROM HistoryStats;";
                    command.ExecuteNonQuery();

                    command.CommandText = "DELETE FROM DayStats;";
                    command.ExecuteNonQuery();

                    command.CommandText = "DELETE FROM sqlite_sequence WHERE name='HistoryStats';";
                    command.ExecuteNonQuery();

                    command.CommandText = "DELETE FROM sqlite_sequence WHERE name='DayStats';";
                    command.ExecuteNonQuery();
                }
            }
            DayUpdate?.Invoke();
            HistoryUpdate?.Invoke();
        }
    }
}
