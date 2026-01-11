using Microsoft.AspNetCore.Http;

namespace BlogSystem.Models.DTOs
{
    // DTO để tạo mới Blog (Có kèm file ảnh)
    public class CreateBlogDto
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public IFormFile Image { get; set; } // Để nhận file upload
    }

    // DTO để Update
    public class UpdateBlogDto
    {
        public string Title { get; set; }
        public string Content { get; set; }
    }
}