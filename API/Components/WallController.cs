using Microsoft.AspNetCore.Mvc;

namespace ModelSaber.API.Components
{
    public abstract class WallsController : Controller
    {
        //HTTP GET
        public abstract ActionResult ReturnWalls();
    }
}