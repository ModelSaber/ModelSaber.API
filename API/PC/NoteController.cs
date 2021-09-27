using Microsoft.AspNetCore.Mvc;
using Note = ModelSaberV3API.APIControllers.NotesController;

namespace ModelSaberV3API.API.PC
{
    [ApiController, Route("api/pc/[controller]")]
    public class NotesController : Note
    {
        [HttpGet]
        public override ActionResult ReturnNotes()
        {
            return NotFound();
        }
    }
}