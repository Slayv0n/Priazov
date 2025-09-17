using Backend.Models;
using Backend.Models.Dto;
using Backend.Validation;
using DataBase;
using DataBase.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Xml.Linq;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Backend.Services
{
    public interface ICommentService
    {
        Task<Comment> CreateCommentAsync(CommentDto comment);
        Task<Comment> UpdateCommentAsync(Guid id, CommentDto comment);
        Task DeleteCommentAsync(Guid id);
        Task<Comment> GetCommentAsync(Guid id);
        Task<List<Comment>> GetAllCommentsAsync(Guid pageId);
    }

    public class CommentService : ICommentService
    {
        private readonly IDbContextFactory<PriazovContext> _factory;
        private readonly ILogger<CommentService> _logger;
        private readonly IMemoryCache _cache;

        private readonly MemoryCacheEntryOptions CacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        public CommentService(
            IDbContextFactory<PriazovContext> factory,
            IMessageService messageService,
            ILogger<CommentService> logger,
            IMemoryCache cache)
        {
            _factory = factory;
            _logger = logger;
            _cache = cache;
        }

        public async Task<Comment> CreateCommentAsync(CommentDto comment)
        {
            using var db = await _factory.CreateDbContextAsync();

            var newComment = new Comment
            {
                Text = comment.Text,
                UserId = comment.UserId,
                CompanyId = comment.CompanyId
            };

            await db.Comments.AddAsync(newComment);
            await db.SaveChangesAsync();
            _logger.LogInformation($"Комментарий оставлен на странице {newComment.CompanyId} в {newComment.CreateTime}");

            return newComment;
        }

        public async Task DeleteCommentAsync(Guid id)
        {
            using var db = await _factory.CreateDbContextAsync();

            var comment = await db.Comments.FirstOrDefaultAsync(c => c.Id == id);

            if(comment == null || comment.IsDeleted == true)
            {
                _logger.LogWarning($"Комментарий не найден");
                throw new NotFoundException("Комментарий не найден");
            }

            comment.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        public async Task<List<Comment>> GetAllCommentsAsync(Guid pageId)
        {
            var cacheKey = $"comments_{pageId}";

            if (_cache.TryGetValue(cacheKey, out List<Comment>? cachedComments))
            {
                _logger.LogInformation($"Ответ взят из кэша: {cacheKey}");
                return cachedComments!;
            }
            else
            {
                _logger.LogInformation($"Кэш промах. Запрос к БД: {cacheKey}");
            }

            using var db = await _factory.CreateDbContextAsync();

            var comments = db.Comments.Where(c => c.CompanyId == pageId && !c.IsDeleted).ToList();

            _cache.Set(cacheKey, comments, CacheOptions);

            return comments;
        }

        public async Task<Comment> GetCommentAsync(Guid id)
        {
            var cacheKey = $"comments_{id}";

            if (_cache.TryGetValue(cacheKey, out Comment? cachedComment))
            {
                _logger.LogInformation($"Ответ взят из кэша: {cacheKey}");
                return cachedComment!;
            }
            else
            {
                _logger.LogInformation($"Кэш промах. Запрос к БД: {cacheKey}");
            }

            using var db = await _factory.CreateDbContextAsync();

            var comment = await db.Comments.FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null || comment.IsDeleted == true)
            {
                _logger.LogWarning($"Комментарий не найден");
                throw new NotFoundException("Комментарий не найден");
            }

            _cache.Set(cacheKey, comment, CacheOptions);

            return comment;
        }

        public async Task<Comment> UpdateCommentAsync(Guid id, CommentDto comment)
        {
            using var db = await _factory.CreateDbContextAsync();

            var newComment = await db.Comments.FirstOrDefaultAsync(c => c.Id == id);

            if (newComment == null || newComment.IsDeleted == true)
            {
                _logger.LogWarning($"Комментарий не найден");
                throw new NotFoundException("Комментарий не найден");
            }

            newComment.Text = comment.Text;

            await db.SaveChangesAsync();

            _cache.Remove($"comment_{id}");

            return newComment;
        }
    }
}
