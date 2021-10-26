﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModelSaber.Database;
using ModelSaber.Database.Models;

namespace ModelSaber.API.Quest
{
    [ApiController, Route("quest/[controller]")]
    public class EffectsController : Components.EffectsController
    {
        [HttpGet]
        public override ActionResult<List<Model>> ReturnEffects()
        {
            return Ok(dbContext.Models.Where(t => t.Type == TypeEnum.Effect).Include(t => t.ModelVariations).Include(t => t.ModelVariation).Include(t => t.Tags).ThenInclude(t => t.Tag).Include(t => t.User).ToList());
        }

        public EffectsController(ModelSaberDbContext dbContext) : base(dbContext)
        {
        }
    }
}