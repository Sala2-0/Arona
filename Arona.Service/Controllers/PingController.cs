using Microsoft.AspNetCore.Mvc;

namespace Arona.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class PingController
{
    [HttpGet]
    public IActionResult Get() => new OkResult();
}
