using Microsoft.AspNetCore.Mvc;

namespace ModelSaber.API.Components
{
    public abstract class SabersController : Controller
    {
        //HTTP GET
        public abstract ActionResult ReturnSabers();
    }
}