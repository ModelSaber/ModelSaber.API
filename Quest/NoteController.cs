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
    public class NotesController : Components.NotesController
    {
        [HttpGet]
        public override ActionResult<List<Model>> ReturnNotes()
        {
            return Ok(dbContext.Models.Where(t => t.Type == TypeEnum.Note).Include(t => t.ModelVariations).Include(t => t.ModelVariation).Include(t => t.Tags).ThenInclude(t => t.Tag).Include(t => t.User).ToList());
        }

        public NotesController(ModelSaberDbContext dbContext) : base(dbContext)
        {
        }
    }
}