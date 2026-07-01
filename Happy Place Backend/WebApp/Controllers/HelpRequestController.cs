using HappyWorld.HappyPlace.Web.Models.HelpRequest;
using Microsoft.AspNetCore.Mvc;

namespace HappyWorld.HappyPlace.Web.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class HelpRequestController : ControllerBase {
    // Methods

    [HttpPost]
    public IActionResult CreateRequest(HelpCreateRequestModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Create());
    }

    [HttpPost]
    public IActionResult Connect(HelpConnectModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Connect());
    }

    [HttpPost]
    public IActionResult PollRequest(HelpPollRequestModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Poll());
    }

    [HttpPost]
    public IActionResult Cancel(HelpCancelModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Cancel());
    }

    [HttpPost]
    public IActionResult MyOpenRequest(HelpMyOpenRequestModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Load());
    }
}
