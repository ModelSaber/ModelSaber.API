using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModelSaber.Database;
using ModelSaber.Models;

namespace ModelSaber.API.PC
{
    [ApiController, Route("pc/[controller]"), Obsolete("Use GraphQL")]
    public class SabersController : Components.SabersController
    {
        [HttpGet]
        public override ActionResult<List<Model>> ReturnSabers()
        {
            return Ok(dbContext.Models.Where(t => t.Type == TypeEnum.Saber).Include(t => t.ModelVariations).Include(t => t.ModelVariation).Include(t => t.Tags).ThenInclude(t => t.Tag).Include(t => t.User).ToList());
        }

        public SabersController(ModelSaberDbContext dbContext) : base(dbContext)
        {
        }
    }
}