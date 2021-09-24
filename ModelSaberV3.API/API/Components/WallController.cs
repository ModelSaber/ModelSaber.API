using Microsoft.AspNetCore.Mvc;

namespace ModelSaberV3API.APIControllers
{
    public abstract class WallsController : Controller
    {
        //HTTP GET
        public abstract ActionResult ReturnWalls();
    }
}