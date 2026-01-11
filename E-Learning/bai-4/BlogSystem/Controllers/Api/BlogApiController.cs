using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlogSystem.Models.DTOs;
using BlogSystem.Models.Responses;
using BlogSystem.Services;
using System.IO;

namespace BlogSystem.Controllers.Api
{
    [Route("api/blogs")]
    [ApiController]
    public class BlogApiController : ControllerBase
    {
        private readonly IBlogService _blogService;
        private readonly IWebHostEnvironment _env;

        public BlogApiController(IBlogService blogService, IWebHostEnvironment env)
        {
            _blogService = blogService;
            _env = env;
        }

        // GET
        [HttpGet]
        public IActionResult GetBlogs([FromQuery] string? keyword, [FromQuery] string? sort)
        {
            var blogs = _blogService.GetBlogs(keyword, sort);
            return Ok(ApiResponse<object?>.Ok(blogs));
        }

        // POST
        [HttpPost]
        [Authorize]
        public IActionResult CreateBlog([FromForm] CreateBlogDto request)
        {
            string? thumbnailPath = null;

            if (request.Image != null)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid() + "_" + request.Image.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using var fileStream = new FileStream(filePath, FileMode.Create);
                request.Image.CopyTo(fileStream);

                thumbnailPath = "/uploads/" + uniqueFileName;
            }

            var blog = _blogService.CreateBlog(request, thumbnailPath, 1);
            return Ok(ApiResponse<object?>.Ok(blog, "Blog created successfully"));
        }

        // PUT
        [HttpPut("{id}")]
        [Authorize]
        public IActionResult UpdateBlog(long id, [FromBody] UpdateBlogDto request)
        {
            var blog = _blogService.UpdateBlog(id, request);
            if (blog == null)
                return NotFound(ApiResponse<string>.Fail("Blog not found"));

            return Ok(ApiResponse<object?>.Ok(blog, "Blog updated"));
        }

        // DELETE
        [HttpDelete("{id}")]
        [Authorize]
        public IActionResult DeleteBlog(long id)
        {
            var success = _blogService.DeleteBlog(id);
            if (!success)
                return NotFound(ApiResponse<string>.Fail("Blog not found"));

            return Ok(ApiResponse<string?>.Ok(null, "Blog deleted"));
        }
    }
}
