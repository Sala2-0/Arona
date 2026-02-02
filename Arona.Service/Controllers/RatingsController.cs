using Microsoft.AspNetCore.Mvc;
using Microsoft.Playwright;

namespace Arona.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class RatingsController(IBrowser browser) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PostAsync([FromBody] string jsonData)
    {
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            await page.AddInitScriptAsync($"window.__APP_DATA__ = {jsonData}");
            await page.GotoAsync($"http://localhost:{Global.Port}/index.html#/ratings");

            await page.WaitForFunctionAsync("() => window.__VUE_READY__ === true");

            var element = await page.WaitForSelectorAsync("#clan_root", new PageWaitForSelectorOptions
            {
                Timeout = 5000
            });

            var screenshotBuffer = await element!.ScreenshotAsync(new ElementHandleScreenshotOptions
            {
                Type = ScreenshotType.Png
            });

            return File(screenshotBuffer, "image/png");
        }
        catch (PlaywrightException ex)
        {
            Console.WriteLine(ex.Message);
            return StatusCode(500, "Playwright error: " + ex.Message);
        }
    }
}