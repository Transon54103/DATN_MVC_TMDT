using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TMDT.DataAccess.Repository.IRepository;
using TMDT.Utility;
using Microsoft.AspNetCore.Http;
using System.Linq;
using TMDT.Models;

namespace Project_ThuongMaiDT.ViewComponents
{
    public class ShoppingCartViewComponent : ViewComponent
    {
        private readonly IUnitOfWork _unitOfWork;

        public ShoppingCartViewComponent(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);

            int cartCount = 0;

            if (claim != null)
            {
                // Nếu người dùng đã đăng nhập
                cartCount = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value && u.Product.IsActive == true).Count();
                HttpContext.Session.SetInt32(SD.SessionCart, cartCount);
            }
            else
            {
                // Nếu người dùng chưa đăng nhập, lấy số lượng từ session
                var cartSession = HttpContext.Session.GetObjectFromJson<List<CartSessionItem>>("CartSession");
                cartCount = cartSession != null ? cartSession.Sum(c => c.Count) : 0;
            }

            return View(cartCount);
        }
    }
}
