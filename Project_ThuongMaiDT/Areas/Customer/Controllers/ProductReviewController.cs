using Microsoft.AspNetCore.Mvc;
using TMDT.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TMDT.DataAccess.Data;
using System;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Project_ThuongMaiDT.Areas.Customer.Controllers
{
    // Controller ProductReviewController

    [Area("Customer")]
    public class ProductReviewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductReviewController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductReview productReview)
        {
            if (productReview.Rating == 0)
            {
                ModelState.AddModelError("Rating", "Bạn cần phải chọn sao để đánh giá.");
                return View(productReview); // Trả về lại view với lỗi
            }
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                productReview.UserId = userId;
                productReview.CreatedAt = DateTime.Now;

                _context.ProductReview.Add(productReview);
                await _context.SaveChangesAsync();

                // Truy vấn tên người dùng từ ApplicationUser
                var user = await _context.Users
                                          .OfType<ApplicationUser>()  // Chỉ rõ rằng đây là ApplicationUser
                                          .Where(u => u.Id == userId)
                                          .Select(u => u.Name)  // Lấy Name từ ApplicationUser
                                          .FirstOrDefaultAsync();

                return Json(new
                {
                    success = true,
                    review = new
                    {
                        userName = user ?? "Ẩn danh", // Nếu không có tên, hiển thị "Ẩn danh"
                        createdAt = productReview.CreatedAt.ToString("dd/MM/yyyy"),
                        rating = productReview.Rating,
                        comment = productReview.Comment
                    }
                });
            }

            return Json(new { success = false, message = "Bạn cần đăng nhập để đánh giá." });
        }
    }
}
