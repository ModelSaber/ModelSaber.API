using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using ModelSaber.Database;
using ModelSaber.Models;

namespace ModelSaber.API.Components
{
    public abstract class WallsController : Controller
    {
        protected ModelSaberDbContext dbContext;

        public WallsController(ModelSaberDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        //HTTP GET
        public abstract ActionResult<List<Model>> ReturnWalls();
    }
}