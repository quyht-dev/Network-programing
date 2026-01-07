using Microsoft.AspNetCore.Mvc;
using BlogSystem.Services;

namespace MyApp.Controllers.Api;

[ApiController]
[Route("api/blogs")]
public class BlogApiController : ControllerBase
{
    private readonly BlogService _blogService;

    public BlogApiController(BlogService blogService)
    {
        _blogService = blogService;
    }

    [HttpGet]
    public IActionResult GetBlogs()
    {
        var blogs = _blogService.GetBlogs();

        if (blogs == null || blogs.Count == 0)
        {
            return Ok(new
            {
                success = true,
                message = "Không có blog nào",
                data = new List<object>()
            });
        }

        return Ok(new
        {
            success = true,
            message = "Lấy danh sách blog thành công",
            data = blogs
        });
    }
}
