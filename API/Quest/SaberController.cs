using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Saber = ModelSaberV3API.APIControllers.SabersController;

namespace ModelSaberV3API.API.Quest
{
    [ApiController, Route("api/quest/[controller]")]
    public class SabersController : Saber
    {
        [HttpGet]
        public override ActionResult ReturnSabers()
        {
            return NotFound();
        }
    }
}