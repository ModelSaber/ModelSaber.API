﻿using Microsoft.AspNetCore.Mvc;

namespace ModelSaber.API.Components
{
    public abstract class EffectsController : Controller
    {
        //HTTP GET
        public abstract ActionResult ReturnEffects();
    }
}