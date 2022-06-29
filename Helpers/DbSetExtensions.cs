using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ModelSaber.Database;
using ModelSaber.Models;

namespace ModelSaber.Database
{
    public static class DbSetExtensions
    {
        // ReSharper disable PossibleInvalidOperationException
        public static async Task<List<Model>> GetModelsFromDBFiltered(this List<Model> models, Status status, bool nsfw, Platform platform)
        {
            var tmp = new List<Model>();
            tmp.AddRange(models.ToList().Where(t => (t.Status & status) == status && t.Nsfw == false && t.Platform == platform).ToList());
            if(nsfw) tmp.AddRange(models.ToList().Where(t => (t.Status & status) == status && t.Nsfw && t.Platform == platform).ToList());
            return tmp;
        }

        public static async Task<List<Model>> GetModelAsync(this DbSet<Model> models, int? first, Guid? createdAfter, string? filter, TypeEnum? mType, bool nsfw, Status status, Platform platform, CancellationToken cancellationToken)
        {
            var regexs = string.IsNullOrWhiteSpace(filter) ? Array.Empty<Regex>() : filter.Split(' ').Select(t => new Regex(t, RegexOptions.Compiled | RegexOptions.IgnoreCase)).ToArray();
            var id = createdAfter.HasValue ? models.Single(t => t.Uuid == createdAfter).Id : 0;
            var modelsReturn = await models.IncludeModelData().Where(t => t.Id > id).ToList().GetModelsFromDBFiltered(status, nsfw, platform);
            return modelsReturn.OrderBy(t => t.Id)
                .If(regexs.Any(), x => x.Select(t => new FilterRank<Model>(t, regexs, arg => arg.Name)).OrderByDescending(t => t.Counts).Where(t => t.PassCheck()).Select(t => t.Value))
                .If(mType.HasValue, x => x.Where(t => t.Type == mType!.Value))                          
                .If(first.HasValue, x => x.Take(first!.Value)).ToList();
        }

        public static async Task<List<Model>> GetModelReverseAsync(this DbSet<Model> models, int? last, Guid? createdBefore, string? filter, TypeEnum? mType, bool nsfw, Status status, Platform platform, CancellationToken cancellationToken)
        {
            var regexs = string.IsNullOrWhiteSpace(filter) ? Array.Empty<Regex>() : filter.Split(' ').Select(t => new Regex(t, RegexOptions.Compiled | RegexOptions.IgnoreCase)).ToArray();
            var id = createdBefore.HasValue ? models.Single(t => t.Uuid == createdBefore).Id : 0;
            var modelsReturn = await models.IncludeModelData().Where(t => t.Id < id).ToList().GetModelsFromDBFiltered(status, nsfw, platform);
            return modelsReturn.OrderByDescending(t => t.Id)
                .If(regexs.Any(), x => x.Select(t => new FilterRank<Model>(t, regexs, arg => arg.Name)).OrderByDescending(t => t.Counts).Where(t => t.PassCheck()).Select(t => t.Value))
                .If(mType.HasValue, x => x.Where(t => t.Type == mType!.Value))                            
                .If(last.HasValue, x => x.Take(last!.Value)).ToList();
        }

        public static Task<bool> GetModelNextPageAsync(this DbSet<Model> models, CancellationToken cancellationToken, uint? id) => id.HasValue ?
            Task.FromResult(models.Any(t => t.Id > id.Value))
            : Task.FromResult(false);

        public static Task<bool> GetModelPreviousPageAsync(this DbSet<Model> models, CancellationToken cancellationToken, uint? id) => id.HasValue ?
            Task.FromResult(models.Any(t => t.Id < id.Value))
            : Task.FromResult(false);

        public static IQueryable<Model> IncludeModelData(this IQueryable<Model> models) => 
            models.Include(t => t.Tags).ThenInclude(t => t.Tag).Include(t => t.User).Include(t => t.Users).ThenInclude(t => t.User).ThenInclude(t => t.UserTags);

        public static Task<List<Tag>> GetTagAsync(this DbSet<Tag> tags, int? first, Guid? createdAfter, string? filter, CancellationToken cancellationToken)
        {
            var regexs = string.IsNullOrWhiteSpace(filter) ? Array.Empty<Regex>() : filter.Split(' ').Select(t => new Regex(t, RegexOptions.Compiled | RegexOptions.IgnoreCase)).ToArray();
            return Task.FromResult(tags.IncludeTagData().OrderBy(t => t.Id).ToList()
                .If(regexs.Any(), x => x.Select(t => new FilterRank<Tag>(t, regexs, arg => arg.Name)).OrderByDescending(t => t.Counts).Where(t => t.PassCheck()).Select(t => t.Value))
                .If(createdAfter.HasValue, x => x.SkipWhile(y => y.CursorId != createdAfter!.Value).Skip(1))
                .If(first.HasValue, x => x.Take(first!.Value)).ToList());
        }

        public static Task<List<Tag>> GetTagReverseAsync(this DbSet<Tag> tags, int? last, Guid? createdBefore, string? filter, CancellationToken cancellationToken)
        {
            var regexs = string.IsNullOrWhiteSpace(filter) ? Array.Empty<Regex>() : filter.Split(' ').Select(t => new Regex(t, RegexOptions.Compiled | RegexOptions.IgnoreCase)).ToArray();
            return Task.FromResult(tags.IncludeTagData().OrderByDescending(t => t.Id).ToList()
                .If(regexs.Any(), x => x.Select(t => new FilterRank<Tag>(t, regexs, arg => arg.Name)).OrderByDescending(t => t.Counts).Where(t => t.PassCheck()).Select(t => t.Value))
                .If(createdBefore.HasValue, x => x.SkipWhile(y => y.CursorId != createdBefore!.Value).Skip(1))
                .If(last.HasValue, x => x.Take(last!.Value)).ToList());
        }

        public static Task<bool> GetTagNextPageAsync(this DbSet<Tag> tags, CancellationToken cancellationToken, uint? id) => id.HasValue ?
            Task.FromResult(tags.Any(t => t.Id > id.Value))
            : Task.FromResult(false);

        public static Task<bool> GetTagPreviousPageAsync(this DbSet<Tag> tags, CancellationToken cancellationToken, uint? id) => id.HasValue ?
            Task.FromResult(tags.Any(t => t.Id < id.Value))
            : Task.FromResult(false);

        public static IQueryable<Tag> IncludeTagData(this IQueryable<Tag> models) => 
            models.Include(t => t.ModelTags).ThenInclude(t => t.Model).ThenInclude(t => t.Users).ThenInclude(t => t.User).ThenInclude(t => t.UserTags);
        // ReSharper restore PossibleInvalidOperationException
    }
}
