using Microsoft.AspNetCore.Mvc;

namespace ModelSaber.API.PC
{
    [ApiController, Route("pc/[controller]")]
    public class NotesController : Components.NotesController
    {
        [HttpGet]
        public override ActionResult ReturnNotes()
        {
            return NotFound();
        }
    }
}