using Microsoft.AspNetCore.Mvc;
using HappyWorld.HappyPlace.Web.Models.Device;

namespace HappyWorld.HappyPlace.Web.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class DeviceController : ControllerBase {
    // Methods

    [HttpPost]
    public IActionResult RegisterDevice(DeviceRegisterModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Register());
    }

    [HttpPost]
    public IActionResult UnregisterDevice(DeviceUnregisterModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        return this.Ok(model.Unregister());
    }
}
