using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModelSaber.Database;
using ModelSaber.Models;

namespace ModelSaber.API.Quest
{
    [ApiController, Route("quest/[controller]"), Obsolete("Use GraphQL")]
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