using System.Data;
using Dapper;
using ZikiBlog.Models;

namespace ZikiBlog.Data;

public class PostgresPostRepository : IPostRepository
{
    private readonly IDbConnection _db;
    public PostgresPostRepository(IDbConnection db) => _db = db;

    public async Task<(IEnumerable<Post> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, bool onlyPublished = true)
    {
        var offset = (page - 1) * pageSize;
        var where = onlyPublished ? "WHERE is_published = TRUE" : string.Empty;
        var sql = $@"SELECT id, title, slug, content_html AS ContentHtml, summary, published_at AS PublishedAt, updated_at AS UpdatedAt, is_published AS IsPublished
                     FROM posts {where}
                     ORDER BY COALESCE(published_at, updated_at) DESC
                     OFFSET @offset LIMIT @limit;
                     SELECT COUNT(*) FROM posts {where};";
        using var multi = await _db.QueryMultipleAsync(sql, new { offset, limit = pageSize });
        var items = await multi.ReadAsync<Post>();
        var total = await multi.ReadSingleAsync<int>();
        return (items, total);
    }

    public async Task<Post?> GetBySlugAsync(string slug)
    {
        const string sql = "SELECT id, title, slug, content_html AS ContentHtml, summary, published_at AS PublishedAt, updated_at AS UpdatedAt, is_published AS IsPublished FROM posts WHERE slug = @slug LIMIT 1";
        return await _db.QueryFirstOrDefaultAsync<Post>(sql, new { slug });
    }

    public async Task CreateAsync(Post post)
    {
        if (post.Id == Guid.Empty) post.Id = Guid.NewGuid();
        const string sql = @"INSERT INTO posts(id, title, slug, content_html, summary, published_at, updated_at, is_published)
                             VALUES (@Id, @Title, @Slug, @ContentHtml, @Summary, @PublishedAt, now(), @IsPublished)";
        await _db.ExecuteAsync(sql, post);
    }

    public async Task UpdateAsync(Post post)
    {
        const string sql = @"UPDATE posts SET title=@Title, slug=@Slug, content_html=@ContentHtml, summary=@Summary, published_at=@PublishedAt, updated_at=now(), is_published=@IsPublished WHERE id=@Id";
        await _db.ExecuteAsync(sql, post);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _db.ExecuteAsync("DELETE FROM posts WHERE id=@id", new { id });
    }
}
