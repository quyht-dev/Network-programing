using System.Reflection.Metadata;
using BlogSystem.Data;
using BlogSystem.Models.Entities;

namespace BlogSystem.Services;

public class BlogService
{
    private readonly ApplicationDbContext _db;

    public BlogService(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<Blog>? GetBlogs()
    {
        var blogs = _db.Blogs
            .Where(b => !b.IsDeleted)
            .OrderByDescending(b => b.CreatedAt)
            .Take(5)
            .ToList();

        if (blogs == null)
            return null;

        return blogs;
    }

}