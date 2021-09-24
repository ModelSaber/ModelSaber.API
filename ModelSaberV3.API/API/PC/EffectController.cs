using Microsoft.AspNetCore.Mvc;
using Weather = ModelSaberV3API.APIControllers.EffectsController;

namespace ModelSaberV3API.API.PC
{
    [ApiController, Route("api/pc/[controller]")]
    public class EffectsController : Weather
    {
        [HttpGet]
        public override ActionResult ReturnEffects()
        {
            return NotFound();
        }
    }
}