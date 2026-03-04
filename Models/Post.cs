namespace ZikiBlog.Models;

public class Post
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsPublished { get; set; } = true;
}
