using Microsoft.AspNetCore.Mvc;
using Arona.Shared;
using Microsoft.Playwright;

namespace Arona.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class VerifyBuildLinkController(IBrowser browser) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] BuildInfo? info)
    {
        if (info == null)
            return BadRequest("Invalid data.");

        if (!info.Url.Contains("share.wowssb.com") && !info.Url.Contains("app.wowssb.com"))
            return BadRequest("Invalid build link");

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

            return Ok("Success");
        }
        catch (PlaywrightException ex)
        {
            return BadRequest("Playwright error: " + ex.Message);
        }
    }
}