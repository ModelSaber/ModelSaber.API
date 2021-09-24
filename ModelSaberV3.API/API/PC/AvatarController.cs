using Microsoft.AspNetCore.Mvc;
using Avatar = ModelSaberV3API.APIControllers.AvatarsController;

namespace ModelSaberV3API.API.PC
{
    [ApiController, Route("api/pc/[controller]")]
    public class AvatarsController : Avatar
    {
        [HttpGet]
        public override ActionResult ReturnAvatars()
        {
            return NotFound();
        }
    }
}
