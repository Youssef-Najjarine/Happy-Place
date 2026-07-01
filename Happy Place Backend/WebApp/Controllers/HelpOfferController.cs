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
    public IActionResult WithdrawOffer(WithdrawOfferModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Withdraw());
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

    [HttpPost]
    public IActionResult Join(HelpJoinModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Join());
    }

    [HttpPost]
    public IActionResult DeclineInvite(DeclineInviteModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.DeclineInvite());
    }
}
