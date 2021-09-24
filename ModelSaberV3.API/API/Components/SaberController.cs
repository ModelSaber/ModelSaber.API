using Microsoft.AspNetCore.Mvc;

namespace ModelSaberV3API.APIControllers
{
    public abstract class SabersController : Controller
    {
        //HTTP GET
        public abstract ActionResult ReturnSabers();
    }
}