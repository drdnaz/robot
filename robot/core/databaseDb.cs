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
        // using System.Environment; ve using System.IO; yukarıda olmalı
        private static string dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AskMeNotData"
        );
        private static string dbPath = Path.Combine(dataDir, "TrendyolComments.db");
        private static string connectionString = $"Data Source={dbPath};Version=3;";

        public static void InitializeDatabase()
        {
            try
            {
                Console.WriteLine("📦 Veritabanı oluşturuluyor...");

                // 1. Belgeler klasörünü oluştur
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }

                // 2. Veritabanı dosyasını oluştur
                if (!File.Exists(dbPath))
                {
                    SQLiteConnection.CreateFile(dbPath);
                    Console.WriteLine("✅ Veritabanı dosyası oluşturuldu.");
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
                    Console.WriteLine("✅ Tablo oluşturuldu.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 HATA (InitializeDatabase): " + ex.Message);
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

        // ✅ CSV dosyasına verileri dışa aktarır
        public static void ExportToCsv(string csvPath)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var query = "SELECT ProductUrl, CommentText FROM Comments";
                    var command = new SQLiteCommand(query, connection);
                    var reader = command.ExecuteReader();

                    using (var writer = new StreamWriter(csvPath, false, Encoding.UTF8))
                    {
                        writer.WriteLine("description,comments");
                        while (reader.Read())
                        {
                            var desc = reader["ProductUrl"].ToString().Replace(",", " ");
                            var comment = reader["CommentText"].ToString().Replace(",", " ");
                            writer.WriteLine($"\"{desc}\",\"{comment}\"");
                        }
                    }
                    Console.WriteLine($"📁 CSV başarıyla oluşturuldu: {csvPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 HATA (ExportToCsv): " + ex.Message);
            }
        }
    }
}