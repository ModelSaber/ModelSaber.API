﻿using Microsoft.AspNetCore.Mvc;

namespace ModelSaber.API.PC
{
    [ApiController, Route("pc/[controller]")]
    public class PlatformsController : Components.PlatformsController
    {
        [HttpGet]
        public override ActionResult ReturnPlatforms()
        {
            return NotFound();
        }
    }
}