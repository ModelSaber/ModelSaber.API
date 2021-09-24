using Microsoft.AspNetCore.Mvc;
using Note = ModelSaberV3API.APIControllers.NotesController;

namespace ModelSaberV3API.API.Quest
{
    [ApiController, Route("api/quest/[controller]")]
    public class NotesController : Note
    {
        [HttpGet]
        public override ActionResult ReturnNotes()
        {
            return NotFound();
        }
    }
}