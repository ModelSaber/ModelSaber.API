using Microsoft.AspNetCore.Mvc;

namespace ModelSaber.API.PC
{
    [ApiController, Route("api/pc/[controller]")]
    public class EffectsController : Components.EffectsController
    {
        [HttpGet]
        public override ActionResult ReturnEffects()
        {
            return NotFound();
        }
    }
}