using Microsoft.AspNetCore.Mvc;
using Platform = ModelSaberV3API.APIControllers.PlatformsController;

namespace ModelSaberV3API.API.PC
{
    [ApiController, Route("api/pc/[controller]")]
    public class PlatformsController : Platform
    {
        [HttpGet]
        public override ActionResult ReturnPlatforms()
        {
            return NotFound();
        }
    }
}