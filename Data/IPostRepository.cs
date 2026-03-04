using ZikiBlog.Models;

namespace ZikiBlog.Data;

public interface IPostRepository
{
    Task<(IEnumerable<Post> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, bool onlyPublished = true);
    Task<Post?> GetBySlugAsync(string slug);
    Task CreateAsync(Post post);
    Task UpdateAsync(Post post);
    Task DeleteAsync(Guid id);
}
