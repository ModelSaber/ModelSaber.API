using Microsoft.AspNetCore.Mvc;

namespace ModelSaberV3API.APIControllers
{
    public abstract class PlatformsController : Controller
    {
        //HTTP GET
        public abstract ActionResult ReturnPlatforms();
    }
}