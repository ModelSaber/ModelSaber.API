using Microsoft.AspNetCore.Mvc;
using Saber = ModelSaberV3API.APIControllers.SabersController;

namespace ModelSaberV3API.API.PC
{
    [ApiController, Route("api/pc/[controller]")]
    public class SabersController : Saber
    {
        [HttpGet]
        public override ActionResult ReturnSabers()
        {
            return NotFound();
        }
    }
}