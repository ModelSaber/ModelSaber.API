using Microsoft.AspNetCore.Mvc;

namespace ModelSaber.API.Quest
{
    [ApiController, Route("quest/[controller]")]
    public class SabersController : Components.SabersController
    {
        [HttpGet]
        public override ActionResult ReturnSabers()
        {
            return NotFound();
        }
    }
}