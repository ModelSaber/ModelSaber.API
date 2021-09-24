using Microsoft.AspNetCore.Mvc;

namespace ModelSaberV3API.APIControllers
{
    public abstract class AvatarsController : Controller
    {
        //HTTP GET
        public abstract ActionResult ReturnAvatars();
    }
}