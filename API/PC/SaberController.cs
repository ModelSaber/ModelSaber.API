using Microsoft.AspNetCore.Mvc;

namespace ModelSaber.API.PC
{
    [ApiController, Route("pc/[controller]")]
    public class SabersController : Components.SabersController
    {
        [HttpGet]
        public override ActionResult ReturnSabers()
        {
            return NotFound();
        }
    }
}