using Microsoft.AspNetCore.Mvc;

namespace ModelSaber.API.Quest
{
    [ApiController, Route("quest/[controller]")]
    public class NotesController : Components.NotesController
    {
        [HttpGet]
        public override ActionResult ReturnNotes()
        {
            return NotFound();
        }
    }
}