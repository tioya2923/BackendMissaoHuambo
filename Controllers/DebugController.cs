using Microsoft.AspNetCore.Mvc;
using MissaoBackend.Services;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    [HttpGet("hash")]
    public IActionResult GetHash([FromQuery] string password)
    {
        var hash = PasswordHasher.Hash(password);
        return Ok(new { password, hash });
    }
}
