using HappyWorld.HappyPlace.Web.Models.ChatMedia;
using Microsoft.AspNetCore.Mvc;

namespace HappyWorld.HappyPlace.Web.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ChatMediaController : ControllerBase {
    // Fields

    private const long MaxMediaRequestBodyBytes = 110_000_000;

    // Methods

    [HttpPost]
    [RequestSizeLimit(MaxMediaRequestBodyBytes)]
    public IActionResult Upload([FromForm] ChatMediaUploadModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        if (this.Request.Form.Files.Count > 1) return this.BadRequest();
        if (model.Media != null && model.Media.Length > MaxMediaRequestBodyBytes) return this.StatusCode(StatusCodes.Status413PayloadTooLarge);
        return this.Ok(model.Upload());
    }

    [HttpGet("/api/chatMedia/{assetId:guid}")]
    public IActionResult Fetch(Guid assetId) {
        ChatMediaContent content = ChatMediaManager.Fetch(assetId);
        if (content == null) return this.NotFound();
        this.Response.Headers.CacheControl = "private, max-age=31536000, immutable";
        if (content.PhysicalPath != null) return this.PhysicalFile(content.PhysicalPath, content.ContentType, enableRangeProcessing: true);
        return this.File(content.Bytes, content.ContentType);
    }
}
