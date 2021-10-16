using Microsoft.AspNetCore.Mvc;

namespace ModelSaber.API.Quest
{
    [ApiController, Route("api/quest/[controller]")]
    public class PlatformsController : Components.PlatformsController
    {
        [HttpGet]
        public override ActionResult ReturnPlatforms()
        {
            return NotFound();
        }
    }
}