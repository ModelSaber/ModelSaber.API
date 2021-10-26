﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using ModelSaber.Database;
using ModelSaber.Database.Models;

namespace ModelSaber.API.Components
{
    public abstract class NotesController : Controller
    {
        protected ModelSaberDbContext dbContext;

        public NotesController(ModelSaberDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        //HTTP GET
        public abstract ActionResult<List<Model>> ReturnNotes();
    }
}