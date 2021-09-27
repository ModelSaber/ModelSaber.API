using Microsoft.AspNetCore.Mvc;

namespace ModelSaberV3API.APIControllers
{
    public abstract class NotesController : Controller
    {
        //HTTP GET
        public abstract ActionResult ReturnNotes();
    }
}