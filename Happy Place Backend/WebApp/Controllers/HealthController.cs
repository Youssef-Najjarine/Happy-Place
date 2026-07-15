using Microsoft.AspNetCore.Mvc;

namespace HappyWorld.HappyPlace.Web.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class HealthController : ControllerBase {
    // Methods

    [HttpGet]
    public IActionResult Check() {
        bool databaseAvailable = HealthManager.IsDatabaseAvailable();
        var result = new HealthCheckResult(databaseAvailable ? "healthy" : "unavailable", databaseAvailable);
        if (!databaseAvailable) return this.StatusCode(StatusCodes.Status503ServiceUnavailable, result);
        return this.Ok(result);
    }

    // Records
    private record HealthCheckResult(string Status, bool Database);
}
