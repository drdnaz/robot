using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data.SQLite;
using System.IO;

namespace robot.core
{
    public static class DatabaseHelper
    {
        private static string dbPath = "TrendyolComments.db";
        private static string connectionString = $"Data Source={dbPath};Version=3;";

        public static void InitializeDatabase()
        {
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Comments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProductUrl TEXT,
                    Username TEXT,
                    StarCount INTEGER,
                    Date TEXT,
                    CommentText TEXT
                )";

                var command = new SQLiteCommand(createTableQuery, connection);
                command.ExecuteNonQuery();
            }
        }

        public static void SaveComment(string productUrl, string username, int starCount, string date, string commentText)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string insertQuery = "INSERT INTO Comments (ProductUrl, Username, StarCount, Date, CommentText) VALUES (@productUrl, @username, @starCount, @date, @commentText)";
                var command = new SQLiteCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@productUrl", productUrl);
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@starCount", starCount);
                command.Parameters.AddWithValue("@date", date);
                command.Parameters.AddWithValue("@commentText", commentText);
                command.ExecuteNonQuery();
            }
        }
    }
}
