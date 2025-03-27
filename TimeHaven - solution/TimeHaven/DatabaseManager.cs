using System;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Security.RightsManagement;
using TimeHaven;
using System.Data.Common;


public class DatabaseManager
{
    private static readonly DatabaseManager _instance = new DatabaseManager();
    public static DatabaseManager Instance => _instance;

    private readonly string connectionString;

    public string ConnectionString => connectionString;
    public event Action DatabaseUpdated = delegate { };

    private DatabaseManager()
    {
        string exePath = AppDomain.CurrentDomain.BaseDirectory;
        string databasePath = Path.Combine(exePath, "datatime.db");
        connectionString = $"Data Source={databasePath};Version=3;";
        InitializeDatabase();
    }
    private void InitializeDatabase()
    {
        if (!File.Exists("datatime.db"))
        {
            SQLiteConnection.CreateFile("datatime.db");
        }

        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ProcessStats (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        process_name TEXT NOT NULL UNIQUE,
                        icon BLOB,
                        total_time INTEGER DEFAULT 0
                    )";
                command.ExecuteNonQuery();

                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS DailyStats (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        process_name TEXT NOT NULL,
                        date TEXT NOT NULL,
                        time_spent INTEGER DEFAULT 0,
                        FOREIGN KEY (process_name) REFERENCES ProcessStats (process_name)
                    )";
                command.ExecuteNonQuery();
            }
        }
    }

    public void AddProcessIfNotExists(string processName, byte[] icon)
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = "INSERT OR IGNORE INTO ProcessStats (process_name, icon) VALUES (@name, @icon)";
                command.Parameters.AddWithValue("@name", processName);
                command.Parameters.AddWithValue("@icon", (object)icon ?? DBNull.Value);
                command.ExecuteNonQuery();
            }
        }
    }

    public void EnsureDailyEntry(string processName)
    {
        if (string.IsNullOrEmpty(processName)) return; 

        string today = DateTime.Now.ToString("yyyy-MM-dd");

        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = "SELECT COUNT(*) FROM DailyStats WHERE process_name = @name AND date = @date";
                command.Parameters.AddWithValue("@name", processName);
                command.Parameters.AddWithValue("@date", today);

                int count = Convert.ToInt32(command.ExecuteScalar());

                if (count == 0)
                {
                    command.CommandText = "INSERT INTO DailyStats (process_name, date, time_spent) VALUES (@name, @date, 0)";
                    command.ExecuteNonQuery();
                }
            }
        }
    }



    public void IncrementTime(string processName)
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = "UPDATE DailyStats SET time_spent = time_spent + 1 WHERE process_name = @name AND date = @date";
                command.Parameters.AddWithValue("@name", processName);
                command.Parameters.AddWithValue("@date", today);
                command.ExecuteNonQuery();
            }

            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = "UPDATE ProcessStats SET total_time = total_time + 1 WHERE process_name = @name";
                command.Parameters.AddWithValue("@name", processName);
                command.ExecuteNonQuery();
            }
        }
        DatabaseUpdated?.Invoke();
    }

    public int GetTimeSpentLast7Days(string processName)
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = "SELECT SUM(time_spent) FROM DailyStats WHERE process_name = @name AND date >= date('now', '-7 days')";
                command.Parameters.AddWithValue("@name", processName);
                object result = command.ExecuteScalar();
                return result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
        }
    }

    public int GetTimeSpentLast30Days(string processName)
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = "SELECT SUM(time_spent) FROM DailyStats WHERE process_name = @name AND date >= date('now', '-30 days')";
                command.Parameters.AddWithValue("@name", processName);
                object result = command.ExecuteScalar();
                return result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
        }
    }

    public int GetTotalTimeSpent(string processName)
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = "SELECT total_time FROM ProcessStats WHERE process_name = @name";
                command.Parameters.AddWithValue("@name", processName);
                object result = command.ExecuteScalar();
                return result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
        }
    }

    public void ResetDatabase()
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = "DELETE FROM ProcessStats";
                command.ExecuteNonQuery();

                command.CommandText = "DELETE FROM DailyStats";
                command.ExecuteNonQuery();

                command.CommandText = "DELETE FROM sqlite_sequence WHERE name='ProcessStats'";
                command.ExecuteNonQuery();

                command.CommandText = "DELETE FROM sqlite_sequence WHERE name='DailyStats'";
                command.ExecuteNonQuery();

                command.CommandText = "VACUUM";
                command.ExecuteNonQuery();
            }
        }
        DatabaseUpdated?.Invoke();
    }

    public void InsertTestData()
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = "DELETE FROM DailyStats WHERE process_name = 'TestProcess'";
                command.ExecuteNonQuery();

                command.CommandText = "DELETE FROM ProcessStats WHERE process_name = 'TestProcess'";
                command.ExecuteNonQuery();

                int totalTimeInSeconds = 27 * 3600;
                command.CommandText = "INSERT INTO ProcessStats (id, process_name, total_time) VALUES (27, 'TestProcess', @totalTime)";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@totalTime", totalTimeInSeconds);
                command.ExecuteNonQuery();

                int year = DateTime.Now.Year;
                int month = DateTime.Now.Month;
                Random random = new Random();

                int daysInMonth = DateTime.DaysInMonth(year, month);

                int daysCount = Math.Min(10, daysInMonth);

                HashSet<int> selectedDays = new HashSet<int>();
                while (selectedDays.Count < daysCount)
                {
                    selectedDays.Add(random.Next(1, daysInMonth + 1));
                }
                int remainingTime = totalTimeInSeconds;
                List<int> hoursPerDay = new List<int>(new int[daysCount]);

                for (int i = 0; i < daysCount - 1; i++)
                {
                    int maxTime = Math.Min(5 * 3600, remainingTime);
                    int allocatedTime = random.Next(0, maxTime + 1);
                    hoursPerDay[i] = allocatedTime;
                    remainingTime -= allocatedTime;
                }
                hoursPerDay[daysCount - 1] = remainingTime;

                int index = 0;
                foreach (int day in selectedDays.OrderBy(d => d))
                {
                    command.CommandText = "INSERT INTO DailyStats (process_name, date, time_spent) VALUES (@name, @date, @time)";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@name", "TestProcess");
                    command.Parameters.AddWithValue("@date", new DateTime(year, month, day).ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@time", hoursPerDay[index]);
                    command.ExecuteNonQuery();
                    index++;
                }
            }
        }
        DatabaseUpdated?.Invoke();
    }

    public List<ProcessStatsData> GetAllProcessStats()
    {
        List<ProcessStatsData> processStats = new List<ProcessStatsData>();

        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = @"
            SELECT 
    p.process_name,
    p.icon, 
    IFNULL(d_today.time_spent, 0) AS today,
    IFNULL(d_7days.time_spent, 0) AS last7days,
    IFNULL(d_30days.time_spent, 0) AS last30days,
    IFNULL(p.total_time, 0) AS total
FROM ProcessStats p
LEFT JOIN (
    SELECT process_name, SUM(time_spent) AS time_spent 
    FROM DailyStats 
    WHERE date(date) = date('now', 'localtime')
    GROUP BY process_name
) d_today ON p.process_name = d_today.process_name
LEFT JOIN (
    SELECT process_name, SUM(time_spent) AS time_spent 
    FROM DailyStats 
    WHERE date(date) >= date('now', 'localtime', '-6 days')
    GROUP BY process_name
) d_7days ON p.process_name = d_7days.process_name
LEFT JOIN (
    SELECT process_name, SUM(time_spent) AS time_spent 
    FROM DailyStats 
    WHERE date(date) >= date('now', 'localtime', '-29 days')
    GROUP BY process_name
) d_30days ON p.process_name = d_30days.process_name;
";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        processStats.Add(new ProcessStatsData
                        {
                            Icon = reader["icon"] is DBNull ? Array.Empty<byte>() : (byte[])reader["icon"],
                            Name = reader["process_name"]?.ToString() ?? string.Empty,
                            Today = reader["today"] is DBNull ? 0 : Convert.ToInt32(reader["today"]),
                            Last7Days = reader["last7days"] is DBNull ? 0 : Convert.ToInt32(reader["last7days"]),
                            Last30Days = reader["last30days"] is DBNull ? 0 : Convert.ToInt32(reader["last30days"]),
                            Total = reader["total"] is DBNull ? 0 : Convert.ToInt32(reader["total"])
                        });
                    }
                }
            }
        }
        return processStats;
    }

    public void ClearTestData()
    {
        using (var connection = new SQLiteConnection(ConnectionString))
        {
            connection.Open();
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = "DELETE FROM ProcessStats;";
                command.ExecuteNonQuery();

                command.CommandText = "DELETE FROM DailyStats;";
                command.ExecuteNonQuery();

                command.CommandText = "DELETE FROM sqlite_sequence WHERE name='ProcessStats';";
                command.ExecuteNonQuery();

                command.CommandText = "DELETE FROM sqlite_sequence WHERE name='DailyStats';";
                command.ExecuteNonQuery();
            }
        }
        DatabaseUpdated?.Invoke();
    }

}
