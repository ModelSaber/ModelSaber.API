using Microsoft.AspNetCore.Mvc;
using ModelSaber.API.Components;

namespace ModelSaber.API.PC
{
    [ApiController, Route("/all")]
    public class ModelDownloader : ModelDownloaderController
    {
        [HttpGet]
        public override ActionResult ReturnModels(ModelTypes type)
        {
            return NotFound();
        }
    }
}