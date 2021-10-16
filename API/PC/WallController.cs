using Microsoft.AspNetCore.Mvc;

namespace ModelSaber.API.PC
{
    [ApiController, Route("api/pc/[controller]")]
    public class WallsController : Components.WallsController
    {
        [HttpGet]
        public override ActionResult ReturnWalls()
        {
            return NotFound();
        }
    }
}