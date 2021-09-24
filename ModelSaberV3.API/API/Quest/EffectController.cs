using Microsoft.AspNetCore.Mvc;
using Weather = ModelSaberV3API.APIControllers.EffectsController;

namespace ModelSaberV3API.API.Quest
{
    [ApiController, Route("api/quest/[controller]")]
    public class EffectsController : Weather
    {
        [HttpGet]
        public override ActionResult ReturnEffects()
        {
            return NotFound();
        }
    }
}