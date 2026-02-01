using Microsoft.AspNetCore.Mvc;
using Arona.Shared;
using Microsoft.Playwright;

namespace Arona.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class GetBuildController(IBrowser browser) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] BuildInfo? info)
    {
        if (info == null)
            return BadRequest("Invalid data.");

        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            await page.GotoAsync(info.Url, new PageGotoOptions
            {
                Timeout = 10000,
                WaitUntil = WaitUntilState.NetworkIdle
            });

            var title = await page.TitleAsync();

            if (title != "Ship stats")
                return BadRequest("Invalid build link");

            var options = new PageWaitForSelectorOptions
            {
                Timeout = 5000
            };

            var shareLinkButton = await page.WaitForSelectorAsync("button:has-text('Share Build Image')", options);

            await shareLinkButton?.ClickAsync()!;

            var image = await page.WaitForSelectorAsync("#image", options);
            var screenshotBuffer = await image?.ScreenshotAsync(new ElementHandleScreenshotOptions
            {
                Type = ScreenshotType.Png
            })!;

            return File(screenshotBuffer, "image/png");
        }
        catch (PlaywrightException ex)
        {
            return StatusCode(500, "Playwright error: " + ex.Message);
        }
    }
}