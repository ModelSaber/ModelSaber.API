using Microsoft.AspNetCore.Mvc;

namespace ModelSaber.API.Quest
{
    [ApiController, Route("quest/[controller]")]
    public class EffectsController : Components.EffectsController
    {
        [HttpGet]
        public override ActionResult ReturnEffects()
        {
            return NotFound();
        }
    }
}