using Microsoft.AspNetCore.Mvc;
using ModelSaberV3API.APIControllers;

namespace ModelSaberV3API.API.PC
{
    [ApiController, Route("api/all")]
    public class ModelDownloader : ModelDownloaderController
    {
        [HttpGet]
        public override ActionResult ReturnModels(ModelTypes type)
        {
            return NotFound();
        }
    }
}