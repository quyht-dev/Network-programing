using BlogSystem.Data;
using BlogSystem.Models.DTOs;
using BlogSystem.Models.Entities;

namespace BlogSystem.Services
{
    public class BlogService : IBlogService
    {
        private readonly ApplicationDbContext _db;

        public BlogService(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Blog> GetBlogs(string? keyword, string? sort)
        {
            var query = _db.Blogs.Where(b => !b.IsDeleted).AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(b => b.Title.Contains(keyword));

            query = sort == "oldest"
                ? query.OrderBy(b => b.CreatedAt)
                : query.OrderByDescending(b => b.CreatedAt);

            return query.ToList();
        }

        public List<Blog> GetBlogsPersonal(long? userId, string? keyword, string? sort)
        {
            var query = _db.Blogs.Where(b => !b.IsDeleted && b.AuthorId == userId).AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(b => b.Title.Contains(keyword));

            query = sort == "oldest"
                ? query.OrderBy(b => b.CreatedAt)
                : query.OrderByDescending(b => b.CreatedAt);

            return query.ToList();
        }

        public Blog? GetBlogById(long id)
        {
            return _db.Blogs.FirstOrDefault(b => b.Id == id && !b.IsDeleted);
        }

        public Blog CreateBlog(CreateBlogDto request, string? thumbnailPath, long authorId)
        {
            var blog = new Blog
            {
                Title = request.Title,
                Content = request.Content,
                Thumbnail = thumbnailPath,
                AuthorId = authorId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsDeleted = false
            };

            _db.Blogs.Add(blog);
            _db.SaveChanges();
            return blog;
        }

        public Blog? UpdateBlog(long id, UpdateBlogDto request)
        {
            var blog = _db.Blogs.Find(id);
            if (blog == null || blog.IsDeleted) return null;

            blog.Title = request.Title;
            blog.Content = request.Content;
            blog.UpdatedAt = DateTime.Now;

            _db.SaveChanges();
            return blog;
        }

        public bool DeleteBlog(long id)
        {
            var blog = _db.Blogs.Find(id);
            if (blog == null) return false;

            blog.IsDeleted = true;
            _db.SaveChanges();
            return true;
        }
    }
}
