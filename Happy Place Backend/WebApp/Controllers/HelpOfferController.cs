using HappyWorld.HappyPlace.Web.Models.HelpOffer;
using Microsoft.AspNetCore.Mvc;

namespace HappyWorld.HappyPlace.Web.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class HelpOfferController : ControllerBase {
    // Methods

    [HttpPost]
    public IActionResult CreateOffer(CreateOfferModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Create());
    }

    [HttpPost]
    public IActionResult DeclineOffer(DeclineOfferModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Decline());
    }

    [HttpPost]
    public IActionResult OpenRequests(OpenRequestsModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Load());
    }

    [HttpPost]
    public IActionResult PollOffer(HelpPollOfferModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Poll());
    }
}
