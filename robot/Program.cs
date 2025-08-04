using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Data.SQLite;
using robot.core;

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
            string url = "https://www.trendyol.com/kadin-t-shirt-x-g1-c73";
            driver.Navigate().GoToUrl(url);

            Thread.Sleep(4000); // Sayfa yüklenmesini bekle

            // Tüm ürün linklerini al
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

                    // Yorum veritabanına kaydedilir
                    int stars = 0;
                    int.TryParse(starCount, out stars);
                    DatabaseHelper.SaveComment(item, username, stars, date, body);
                }
            }

            driver.Quit();
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