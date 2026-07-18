using HappyWorld.HappyPlace.Web.Models.ChatGroup;
using Microsoft.AspNetCore.Mvc;

namespace HappyWorld.HappyPlace.Web.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ChatGroupController : ControllerBase {
    // Methods

    [HttpPost]
    public IActionResult List(ChatGroupListModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Load());
    }

    [HttpPost]
    public IActionResult ListPage(ChatGroupListPageModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Load());
    }

    [HttpPost]
    public IActionResult AvailableHelpers(ChatGroupAvailableHelpersModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Load());
    }

    [HttpPost]
    public IActionResult ListMembers(ChatGroupMembersModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Load());
    }

    [HttpPost]
    public IActionResult Rename(ChatGroupRenameModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Rename());
    }

    [HttpPost]
    public IActionResult SetVisibility(ChatGroupSetVisibilityModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.SetVisibility());
    }

    [HttpPost]
    public IActionResult Delete(ChatGroupDeleteModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Delete());
    }

    [HttpPost]
    public IActionResult Leave(ChatGroupLeaveModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Leave());
    }

    [HttpPost]
    public IActionResult RequestToJoin(ChatGroupRequestToJoinModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.RequestToJoin());
    }

    [HttpPost]
    public IActionResult CancelJoinRequest(ChatGroupCancelJoinRequestModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.CancelJoinRequest());
    }

    [HttpPost]
    public IActionResult ApproveMember(ChatGroupApproveMemberModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.ApproveMember());
    }

    [HttpPost]
    public IActionResult RejectMember(ChatGroupRejectMemberModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.RejectMember());
    }

    [HttpPost]
    public IActionResult RemoveMember(ChatGroupRemoveMemberModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.RemoveMember());
    }

    [HttpPost]
    public IActionResult OpenDirect(ChatGroupOpenDirectModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.OpenDirect());
    }

    [HttpPost]
    public IActionResult Hide(ChatGroupHideModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Hide());
    }

    [HttpPost]
    public IActionResult SetMuted(ChatGroupSetMutedModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.SetMuted());
    }

    [HttpPost]
    public IActionResult UnreadTotal(ChatGroupUnreadTotalModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.UnreadTotal());
    }

    [HttpPost]
    public IActionResult CreateWithFriends(ChatGroupCreateWithFriendsModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.CreateWithFriends());
    }
}
