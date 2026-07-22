using HappyWorld.HappyPlace.Web.Models.HelpAvailability;
using Microsoft.AspNetCore.Mvc;

namespace HappyWorld.HappyPlace.Web.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class HelpAvailabilityController : ControllerBase {
    // Methods

    [HttpPost]
    public IActionResult SetAvailability(SetAvailabilityModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Apply());
    }

    [HttpPost]
    public IActionResult GetAvailability(GetAvailabilityModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Read());
    }
}
