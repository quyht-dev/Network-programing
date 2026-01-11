using Microsoft.AspNetCore.Http;

namespace BlogSystem.Models.DTOs
{
    // DTO để tạo mới Blog (Có kèm file ảnh)
    public class CreateBlogDto
    {
        public required string Title { get; set; }
        public required string Content { get; set; }
        public IFormFile? Image { get; set; }
    }

    // DTO để Update
    public class UpdateBlogDto
    {
        public required string Title { get; set; }
        public required string Content { get; set; }
    }
}