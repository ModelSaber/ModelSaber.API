using Microsoft.AspNetCore.Mvc;
using Platform = ModelSaberV3API.APIControllers.PlatformsController;

namespace ModelSaberV3API.API.Quest
{
    [ApiController, Route("api/quest/[controller]")]
    public class PlatformsController : Platform
    {
        [HttpGet]
        public override ActionResult ReturnPlatforms()
        {
            return NotFound();
        }
    }
}