using HappyWorld.HappyPlace.Web.Models.Friendship;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HappyWorld.HappyPlace.Web.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class FriendshipController : ControllerBase {
    // Methods

    [HttpPost]
    public IActionResult SendRequest(FriendshipSendRequestModel model) {
        var result = model.Send();
        if (result == null) return this.Unauthorized();
        if (result.Status == "rateLimited") return this.StatusCode(StatusCodes.Status429TooManyRequests);
        return this.Ok(result);
    }

    [HttpPost]
    public IActionResult CancelRequest(FriendshipCancelRequestModel model) {
        var result = model.Cancel();
        if (result == null) return this.Unauthorized();
        return this.Ok(result);
    }

    [HttpPost]
    public IActionResult AcceptRequest(FriendshipAcceptRequestModel model) {
        var result = model.Accept();
        if (result == null) return this.Unauthorized();
        return this.Ok(result);
    }

    [HttpPost]
    public IActionResult DeclineRequest(FriendshipDeclineRequestModel model) {
        var result = model.Decline();
        if (result == null) return this.Unauthorized();
        return this.Ok(result);
    }

    [HttpPost]
    public IActionResult Unfriend(FriendshipUnfriendModel model) {
        var result = model.Unfriend();
        if (result == null) return this.Unauthorized();
        return this.Ok(result);
    }

    [HttpPost]
    public IActionResult Block(FriendshipBlockModel model) {
        var result = model.Block();
        if (result == null) return this.Unauthorized();
        return this.Ok(result);
    }

    [HttpPost]
    public IActionResult Unblock(FriendshipUnblockModel model) {
        var result = model.Unblock();
        if (result == null) return this.Unauthorized();
        return this.Ok(result);
    }

    [HttpPost]
    public IActionResult ListBlocked(FriendshipListBlockedModel model) {
        var result = model.List();
        if (result == null) return this.Unauthorized();
        return this.Ok(result);
    }

    [HttpPost]
    public IActionResult ListFriends(FriendshipListFriendsModel model) {
        var result = model.List();
        if (result == null) return this.Unauthorized();
        if (result.Status == "notFound") return this.NotFound();
        return this.Ok(result);
    }

    [HttpPost]
    public IActionResult ListIncomingRequests(FriendshipListIncomingRequestsModel model) {
        var result = model.List();
        if (result == null) return this.Unauthorized();
        return this.Ok(result);
    }

    [HttpPost]
    public IActionResult ListOutgoingRequests(FriendshipListOutgoingRequestsModel model) {
        var result = model.List();
        if (result == null) return this.Unauthorized();
        return this.Ok(result);
    }

    [HttpPost]
    public IActionResult SearchUsers(FriendshipSearchUsersModel model) {
        var result = model.Search();
        if (result == null) return this.Unauthorized();
        return this.Ok(result);
    }
}
