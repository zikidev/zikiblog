using System.Xml.Linq;
using ZikiBlog.Data;
using ZikiBlog.Models;

namespace ZikiBlog.Services;

public class BloggerImportService
{
    private readonly string _imagesFolder;
    private readonly IPostRepository _posts;
    private readonly SlugService _slugs;

    public BloggerImportService(IWebHostEnvironment environment, IPostRepository posts, SlugService slugs)
    {
        _imagesFolder = Path.Combine(environment.WebRootPath, "images", "imported");
        Directory.CreateDirectory(_imagesFolder);

        _posts = posts;
        _slugs = slugs;
    }

    public async Task<int> ImportFromTakeoutAsync(string takeoutBloggerPath)
    {
        // Find the feed.atom file in Blogs subdirectory
        var xmlFile = Directory.GetFiles(takeoutBloggerPath, "feed.atom", SearchOption.AllDirectories).FirstOrDefault();
        if (xmlFile == null)
        {
            // Fallback to any XML file
            xmlFile = Directory.GetFiles(takeoutBloggerPath, "*.xml", SearchOption.AllDirectories).FirstOrDefault();
        }
        
        if (xmlFile == null)
            throw new FileNotFoundException("No feed.atom or XML file found in the Takeout folder");

        // Images are in Albums folder (parallel to Blogs folder)
        var albumsPath = Path.Combine(takeoutBloggerPath, "Albums");

        using var xmlStream = File.OpenRead(xmlFile);
        var doc = XDocument.Load(xmlStream);
        XNamespace atom = "http://www.w3.org/2005/Atom";
        XNamespace blogger = "http://schemas.google.com/blogger/2018";

        int imported = 0;
        foreach (var entry in doc.Root!.Elements(atom + "entry"))
        {
            // Check if it's a POST type
            var type = (string?)entry.Element(blogger + "type");
            if (type != "POST") continue;

            // Check if it's published (LIVE status)
            var status = (string?)entry.Element(blogger + "status");
            if (status != "LIVE") continue;

            var title = (string?)entry.Element(atom + "title") ?? "(untitled)";
            var content = (string?)entry.Element(atom + "content") ?? "";
            var publishedStr = (string?)entry.Element(atom + "published");
            DateTimeOffset? published = null;
            if (DateTimeOffset.TryParse(publishedStr, out var dto)) published = dto;

            // Copy local images and update content (search in Albums folder)
            content = CopyLocalImagesAndUpdateContent(content, albumsPath);

            var post = new Post
            {
                Title = title,
                Slug = _slugs.Generate(title),
                ContentHtml = content,
                Summary = null,
                PublishedAt = published,
                IsPublished = true
            };

            await _posts.CreateAsync(post);
            imported++;
        }
        return imported;
    }

    private string CopyLocalImagesAndUpdateContent(string content, string albumsPath)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // Find all image URLs in both <img src> and <a href> tags
        var urlPattern = @"(?:src|href)=[""']([^""']+(?:\.jpg|\.jpeg|\.png|\.gif|\.webp|\.bmp)[^""']*)[""']";
        var urlRegex = new System.Text.RegularExpressions.Regex(urlPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var urlMatches = urlRegex.Matches(content);
        var urlReplacements = new Dictionary<string, string>();

        foreach (System.Text.RegularExpressions.Match match in urlMatches)
        {
            var originalUrl = match.Groups[1].Value;
            
            // Skip if we already processed this URL
            if (urlReplacements.ContainsKey(originalUrl))
                continue;

            try
            {
                // Try multiple strategies to find the image
                string? sourceFile = null;
                
                // Strategy 1: Extract filename from URL path (after last /)
                var uri = new Uri(originalUrl);
                var urlPath = uri.AbsolutePath;
                
                // Remove size parameters (s1600, s400, etc.) from Blogger URLs
                urlPath = System.Text.RegularExpressions.Regex.Replace(urlPath, @"/s\d+(-h)?/", "/");
                
                var fileName = Path.GetFileName(urlPath);
                if (!string.IsNullOrEmpty(fileName))
                {
                    sourceFile = FindImageInAlbums(albumsPath, fileName);
                }
                
                // Strategy 2: If not found, try without URL decoding issues
                if (sourceFile == null && !string.IsNullOrEmpty(fileName))
                {
                    fileName = System.Web.HttpUtility.UrlDecode(fileName);
                    sourceFile = FindImageInAlbums(albumsPath, fileName);
                }
                
                // Strategy 3: Try to match by checking all image files
                if (sourceFile == null && Directory.Exists(albumsPath) && !string.IsNullOrEmpty(fileName))
                {
                    var allImages = Directory.GetFiles(albumsPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => IsImageFile(f));
                    
                    // Try matching by filename without extension or with URL-encoded variations
                    var searchName = Path.GetFileNameWithoutExtension(fileName);
                    sourceFile = allImages.FirstOrDefault(f => 
                        Path.GetFileNameWithoutExtension(f).Equals(searchName, StringComparison.OrdinalIgnoreCase) ||
                        Path.GetFileName(f).Equals(fileName, StringComparison.OrdinalIgnoreCase)
                    );
                }

                if (sourceFile != null && File.Exists(sourceFile))
                {
                    // Generate new filename to avoid conflicts
                    var extension = Path.GetExtension(sourceFile);
                    var newFileName = $"{Guid.NewGuid()}{extension}";
                    var destPath = Path.Combine(_imagesFolder, newFileName);

                    // Copy the file
                    File.Copy(sourceFile, destPath, overwrite: true);

                    // Store the replacement
                    var newUrl = $"/images/imported/{newFileName}";
                    urlReplacements[originalUrl] = newUrl;
                    
                    Console.WriteLine($"Copied image: {fileName} -> {newFileName}");
                }
                else
                {
                    Console.WriteLine($"Image not found in Albums: {fileName} (URL: {originalUrl})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to copy image {originalUrl}: {ex.Message}");
            }
        }

        // Apply all replacements
        foreach (var (oldUrl, newUrl) in urlReplacements)
        {
            content = content.Replace(oldUrl, newUrl);
        }

        return content;
    }

    private string? FindImageInAlbums(string albumsPath, string fileName)
    {
        // Check if Albums folder exists
        if (!Directory.Exists(albumsPath))
            return null;

        // Search for the file in all subdirectories within Albums
        var files = Directory.GetFiles(albumsPath, fileName, SearchOption.AllDirectories);
        return files.FirstOrDefault();
    }
    
    private bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp";
    }
}