using BlogSystem.Models.DTOs;
using BlogSystem.Models.Entities;

namespace BlogSystem.Services
{
    public interface IBlogService
    {
        List<Blog> GetBlogs(string? keyword, string? sort);
        Blog? CreateBlog(CreateBlogDto request, string? thumbnailPath, long authorId);
        Blog? UpdateBlog(long id, UpdateBlogDto request);
        bool DeleteBlog(long id);
        Blog? GetBlogById(long id);
    }
}
