using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModelSaber.Database;
using ModelSaber.Database.Models;

namespace ModelSaber.API.PC
{
    [ApiController, Route("pc/[controller]")]
    public class WallsController : Components.WallsController
    {
        [HttpGet]
        public override ActionResult<List<Model>> ReturnWalls()
        {
            return Ok(dbContext.Models.Where(t => t.Type == TypeEnum.Wall).Include(t => t.ModelVariations).Include(t => t.ModelVariation).Include(t => t.Tags).ThenInclude(t => t.Tag).Include(t => t.User).ToList());
        }

        public WallsController(ModelSaberDbContext dbContext) : base(dbContext)
        {
        }
    }
}