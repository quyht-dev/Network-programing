using Microsoft.AspNetCore.Mvc;
using BlogSystem.Services;

namespace BlogSystem.Controllers;

public class HomeController : Controller
{
    private readonly BlogService _blogService;

    public HomeController(BlogService blogService)
    {
        _blogService = blogService;
    }

    // GET: /index
    public IActionResult Index()
    {
        var blogs = _blogService.GetBlogs();
        return View(blogs);
    }

}