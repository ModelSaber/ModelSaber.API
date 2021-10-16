using Microsoft.AspNetCore.Mvc;

namespace ModelSaber.API.Components
{
    public abstract class PlatformsController : Controller
    {
        //HTTP GET
        public abstract ActionResult ReturnPlatforms();
    }
}