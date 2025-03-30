using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TMDT.DataAccess.Repository.IRepository;
using TMDT.Utility;

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
                cartCount = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value && u.Product.IsActive==true).Count();
                HttpContext.Session.SetInt32(SD.SessionCart, cartCount);
            }

            return View(cartCount);
        }
    }
}
