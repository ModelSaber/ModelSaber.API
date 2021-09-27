using Microsoft.AspNetCore.Mvc;

namespace ModelSaberV3API.APIControllers
{
    public abstract class EffectsController : Controller
    {
        //HTTP GET
        public abstract ActionResult ReturnEffects();
    }
}