using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Data.SQLite;
using robot.core;
using System.IO;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

class TrendyolCommentScraper
{
    static void Main()
    {
        // Veritabanı ilk kurulumu yapılır
        DatabaseHelper.InitializeDatabase();

        ChromeOptions options = new ChromeOptions();
        options.AddArgument("--start-maximized");

        using (IWebDriver driver = new ChromeDriver(options))
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

            string url = "https://www.trendyol.com/kadin-t-shirt-x-g1-c73";
            driver.Navigate().GoToUrl(url);

            // Scroll yaparak ürünlerin yüklenmesini sağla
            for (int i = 0; i < 5; i++)
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
                Thread.Sleep(2000);
            }

            // Ürünleri bekle ve al
            wait.Until(ExpectedConditions.ElementExists(By.CssSelector(".p-card-chldrn-cntnr.card-border")));
            var productLinkElements = driver.FindElements(By.CssSelector(".p-card-chldrn-cntnr.card-border"));
            var productLinks = new List<string>();

            foreach (var element in productLinkElements)
            {
                string href = element.GetAttribute("href");
                if (!string.IsNullOrEmpty(href))
                {
                    if (href.StartsWith("/"))
                        href = "https://www.trendyol.com" + href;

                    productLinks.Add(href);
                    Console.WriteLine(href);
                    }
            }

            Console.WriteLine($"Toplam ürün linki: {productLinks.Count}");

            foreach (var item in productLinks)
            {
                Console.WriteLine("ÜRÜN AYRIMI");
                driver.Navigate().GoToUrl(item + "/yorumlar");

                ScrollToLoadAllComments(driver);

                IReadOnlyCollection<IWebElement> commentElements = driver.FindElements(By.CssSelector(".comment"));

                foreach (var comment in commentElements)
                {
                    string username = SafeGetText(comment, By.CssSelector(".comment-info-item"));
                    string date = SafeGetText(comment, By.CssSelector(".comment-info .comment-info-item:nth-child(2)"));
                    string body = SafeGetText(comment, By.CssSelector(".comment-text p"));
                    string starCount = comment.FindElements(By.CssSelector(".comment-rating .full")).Count.ToString();

                    Console.WriteLine("⭐ Yıldız: " + starCount);
                    Console.WriteLine("👤 Kullanıcı: " + username);
                    Console.WriteLine("📅 Tarih: " + date);
                    Console.WriteLine("💬 Yorum: " + body);
                    Console.WriteLine(new string('-', 50));

                    int stars = 0;
                    int.TryParse(starCount, out stars);
                    DatabaseHelper.SaveComment(item, username, stars, date, body);
                }
            }

            driver.Quit();
        }

        // ✅ CSV dışa aktarımı
        string outputDir = Path.Combine(
     Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
     "AskMeNotData"
 );
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }
        string csvPath = Path.Combine(outputDir, "output.csv");
        DatabaseHelper.ExportToCsv(csvPath);
        Console.WriteLine($"✅ CSV dosyası oluşturuldu: {csvPath}");
        System.Threading.Thread.Sleep(1000);

        // ✅ Python (streamlit app.py) başlat
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo();
            psi.FileName = "cmd.exe";

            // ✅ Dinamik olarak app.py yolunu al
            string appPyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.py");

            // ✅ Arguments satırını bu şekilde güncelle
            psi.Arguments = $"/C streamlit run \"{appPyPath}\"";
            psi.UseShellExecute = false;
            psi.CreateNoWindow = false;

            System.Diagnostics.Process.Start(psi);
            Console.WriteLine("🚀 Python/Streamlit uygulaması başlatıldı.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("🔥 Python çalıştırılamadı: " + ex.Message);
        }
    }

    static void ScrollToLoadAllComments(IWebDriver driver)
    {
        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
        long lastHeight = (long)js.ExecuteScript("return document.body.scrollHeight");

        while (true)
        {
            js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
            Thread.Sleep(2000); // Yorumlar yüklensin diye beklenir

            long newHeight = (long)js.ExecuteScript("return document.body.scrollHeight");
            if (newHeight == lastHeight)
                break;

            lastHeight = newHeight;
        }
    }

    static string SafeGetText(IWebElement parent, By by)
    {
        try
        {
            return parent.FindElement(by).Text.Trim();
        }
        catch
        {
            return "Yok";
        }
    }
}