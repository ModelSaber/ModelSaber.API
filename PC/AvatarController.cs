using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModelSaber.Database;
using ModelSaber.Database.Models;

namespace ModelSaber.API.PC
{
    [ApiController, Route("pc/[controller]")]
    public class AvatarsController : Components.AvatarsController
    {
        [HttpGet]
        public override ActionResult<List<Model>> ReturnAvatars()
        {
            return Ok(dbContext.Models.Where(t => t.Type == TypeEnum.Platform).Include(t => t.ModelVariations).Include(t => t.ModelVariation).Include(t => t.Tags).ThenInclude(t => t.Tag).Include(t => t.User).ToList());
        }

        public AvatarsController(ModelSaberDbContext dbContext) : base(dbContext)
        {
        }
    }
}
