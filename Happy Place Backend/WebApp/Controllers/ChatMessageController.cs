using HappyWorld.HappyPlace.Web.Models.ChatMessage;
using Microsoft.AspNetCore.Mvc;

namespace HappyWorld.HappyPlace.Web.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ChatMessageController : ControllerBase {
    // Methods

    [HttpPost]
    public IActionResult Send(ChatMessageSendModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Send());
    }

    [HttpPost]
    public IActionResult ListPage(ChatMessageListPageModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.ListPage());
    }

    [HttpPost]
    public IActionResult Poll(ChatMessagePollModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Poll());
    }

    [HttpPost]
    public IActionResult MarkRead(ChatMessageMarkReadModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.MarkRead());
    }

    [HttpPost]
    public IActionResult Typing(ChatMessageTypingModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Typing());
    }

    [HttpPost]
    public IActionResult React(ChatMessageReactModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.React());
    }

    [HttpPost]
    public IActionResult DeleteOwn(ChatMessageDeleteOwnModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.DeleteOwn());
    }

    [HttpPost]
    public IActionResult Report(ChatMessageReportModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Report());
    }
}
