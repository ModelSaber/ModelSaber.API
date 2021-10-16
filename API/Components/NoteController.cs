using Microsoft.AspNetCore.Mvc;

namespace ModelSaber.API.Components
{
    public abstract class NotesController : Controller
    {
        //HTTP GET
        public abstract ActionResult ReturnNotes();
    }
}