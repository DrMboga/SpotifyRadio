using Microsoft.Playwright;

namespace RadioApp.RadioStreaming.WebScraper;

public class MyTunerScraperBase
{
    protected Task<IBrowserContext> GenerateBrowserContext(IBrowser browser)
    {
        return browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 Chrome/126 Safari/537.36"
            }
        );
    }
}