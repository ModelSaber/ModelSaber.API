using Microsoft.AspNetCore.Mvc;

namespace ModelSaber.API.Quest
{
    [ApiController, Route("quest/[controller]")]
    public class WallsController : Components.WallsController
    {
        [HttpGet]
        public override ActionResult ReturnWalls()
        {
            return NotFound();
        }
    }
}