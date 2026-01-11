using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlogSystem.Data;
using BlogSystem.Models.Entities;
using BlogSystem.Models.DTOs;
using BlogSystem.Models.Responses;
using System.Linq;
using System.IO;
using System;

namespace BlogSystem.Controllers.Api
{
    [Route("api/blogs")]
    [ApiController]
    public class BlogApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BlogApiController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // 1. GET: Public API - Search & Sort & View
        [HttpGet]
        // SỬA Ở ĐÂY: Thêm dấu ? vào sau string để cho phép null (không bắt buộc nhập)
        public IActionResult GetBlogs([FromQuery] string? keyword, [FromQuery] string? sort)
        {
            // Chỉ lấy những bài chưa bị xóa (IsDeleted = false)
            var query = _context.Blogs.Where(b => !b.IsDeleted).AsQueryable();

            // --- Logic Search ---
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(b => b.Title.Contains(keyword));
            }

            // --- Logic Sort ---
            if (sort == "oldest")
                query = query.OrderBy(b => b.CreatedAt);
            else
                query = query.OrderByDescending(b => b.CreatedAt); // Mặc định mới nhất

            var blogs = query.ToList();
            return Ok(ApiResponse<object>.Ok(blogs));
        }

        // 2. POST: Private API - Create & Upload Image
        [HttpPost]
        [Authorize] // Yêu cầu Login
        public IActionResult CreateBlog([FromForm] CreateBlogDto request)
        {
            string thumbnailPath = null;

            // --- Logic Upload File ---
            if (request.Image != null)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                // Tạo tên file ngẫu nhiên để tránh trùng
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + request.Image.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    request.Image.CopyTo(fileStream);
                }
                // Đường dẫn lưu vào DB (dạng /uploads/ten_file.jpg)
                thumbnailPath = "/uploads/" + uniqueFileName;
            }

            var newBlog = new Blog
            {
                Title = request.Title,
                Content = request.Content,
                Thumbnail = thumbnailPath,
                AuthorId = 1, // Tạm thời gán cứng ID=1 (Admin) để test
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsDeleted = false
            };

            _context.Blogs.Add(newBlog);
            _context.SaveChanges();

            return Ok(ApiResponse<Blog>.Ok(newBlog, "Blog created successfully"));
        }

        // 3. PUT: Private API - Update
        [HttpPut("{id}")]
        [Authorize]
        public IActionResult UpdateBlog(long id, [FromBody] UpdateBlogDto request)
        {
            var blog = _context.Blogs.Find(id);
            if (blog == null || blog.IsDeleted) 
                return NotFound(ApiResponse<string>.Fail("Blog not found"));

            blog.Title = request.Title;
            blog.Content = request.Content;
            blog.UpdatedAt = DateTime.Now; // Cập nhật thời gian sửa
            
            _context.SaveChanges();
            return Ok(ApiResponse<Blog>.Ok(blog, "Blog updated"));
        }

        // 4. DELETE: Private API
        [HttpDelete("{id}")]
        [Authorize]
        public IActionResult DeleteBlog(long id)
        {
            var blog = _context.Blogs.Find(id);
            if (blog == null) return NotFound(ApiResponse<string>.Fail("Blog not found"));

            // Soft Delete (Xóa mềm)
            blog.IsDeleted = true; 
            
            _context.SaveChanges();
            return Ok(ApiResponse<string>.Ok(null, "Blog deleted"));
        }
    }
}