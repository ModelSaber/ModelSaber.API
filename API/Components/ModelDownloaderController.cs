using Microsoft.AspNetCore.Mvc;

namespace ModelSaber.API.Components
{
    public abstract class ModelDownloaderController : Controller
    {
        //HTTP GET
        public abstract ActionResult ReturnModels(ModelTypes type);
    }
}