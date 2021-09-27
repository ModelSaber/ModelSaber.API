using Microsoft.AspNetCore.Mvc;

namespace ModelSaberV3API.APIControllers
{
    public abstract class ModelDownloaderController : Controller
    {
        //HTTP GET
        public abstract ActionResult ReturnModels(ModelTypes type);
    }
}