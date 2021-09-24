using Microsoft.AspNetCore.Mvc;
using Wall = ModelSaberV3API.APIControllers.WallsController;

namespace ModelSaberV3API.API.Quest
{
    [ApiController, Route("api/quest/[controller]")]
    public class WallsController : Wall
    {
        [HttpGet]
        public override ActionResult ReturnWalls()
        {
            return NotFound();
        }
    }
}