using BulkyBook.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Stripe.Checkout;
using System.Security.Claims;
using TMDT.DataAccess.Repository.IRepository;
using TMDT.Models;
using TMDT.Models.ViewModels;
using TMDT.Utility;

namespace Project_ThuongMaiDT.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender; // Inject Email Sender
        [BindProperty]
		public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Nếu đã đăng nhập, xóa session giỏ hàng cũ của khách
                HttpContext.Session.Remove("CartSession");
            }

            // Lấy giỏ hàng từ DB nếu có userId
            var cartFromDb = _unitOfWork.ShoppingCart.GetAll(
                u => u.ApplicationUserId == userId && u.Product.IsActive == true,
                includeProperties: "Product.ProductImages"
            );

            // Lấy giỏ hàng từ session nếu chưa đăng nhập
            var cartFromSession = HttpContext.Session.GetObjectFromJson<List<CartSessionItem>>("CartSession") ?? new List<CartSessionItem>();

            // Chuyển CartSessionItem thành ShoppingCart giả
            var cartSessionConverted = cartFromSession.Select(item =>
            {
                var productWithImages = _unitOfWork.Product.Get(
                    p => p.Id == item.ProductId,
                    includeProperties: "ProductImages"
                );

                return new ShoppingCart
                {
                    Price = productWithImages.Price,
                    ProductId = item.ProductId,
                    Count = item.Count,
                    Product = productWithImages
                };
            });

            // Gộp 2 nguồn giỏ hàng lại
            var combinedCart = cartFromDb.Concat(cartSessionConverted);

            // Tạo ViewModel
            ShoppingCartVM = new()
            {
                ShoppingCartList = combinedCart,
                OrderHeader = new()
            };

            // Tính tổng giá trị và điều chỉnh giá theo số lượng
            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }




        public IActionResult Summary()
        {
            ShoppingCartVM = new()
            {
                OrderHeader = new()
            };

            if (User.Identity.IsAuthenticated)
            {
                // Người dùng đã đăng nhập → lấy giỏ từ DB
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(
                    u => u.ApplicationUserId == userId && u.Product.IsActive == true,
                    includeProperties: "Product"
                );

                // Lấy thông tin user để fill OrderHeader
                var appUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);
                ShoppingCartVM.OrderHeader.ApplicationUser = appUser;
                ShoppingCartVM.OrderHeader.Name = appUser.Name;
                ShoppingCartVM.OrderHeader.PhoneNumber = appUser.PhoneNumber;
                ShoppingCartVM.OrderHeader.StreetAddress = appUser.StreetAddress;
                ShoppingCartVM.OrderHeader.City = appUser.City;
                ShoppingCartVM.OrderHeader.State = appUser.State;
                ShoppingCartVM.OrderHeader.PostalCode = appUser.PostalCode;
            }
            else
            {
                // Chưa đăng nhập → lấy giỏ từ Session
                var cartFromSession = HttpContext.Session.GetObjectFromJson<List<CartSessionItem>>("CartSession") ?? new List<CartSessionItem>();

                // Convert session sang ShoppingCart list
                ShoppingCartVM.ShoppingCartList = cartFromSession.Select(item =>
                {
                    var product = _unitOfWork.Product.Get(p => p.Id == item.ProductId);
                    return new ShoppingCart
                    {
                        ProductId = item.ProductId,
                        Count = item.Count,
                        Product = product,
                        Price = product.Price
                    };
                }).ToList();
            }

            // Tính tổng tiền
            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                // Nếu chưa set Price thì set theo số lượng
                if (cart.Price == 0)
                {
                    cart.Price = GetPriceBasedOnQuantity(cart);
                }
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }
        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPOST(OrderVM orderHeaderVM)
        {
            string userId = null;
            List<ShoppingCart> cartList = new();
            double cartTotal = 0;

            // Xác định user
            if (User.Identity.IsAuthenticated)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                // Lấy giỏ hàng từ DB
                cartList = _unitOfWork.ShoppingCart.GetAll(
                    u => u.ApplicationUserId == userId && u.Product.IsActive == true,
                    includeProperties: "Product"
                ).ToList();
            }
            else
            {
                // Lấy giỏ hàng từ Session
                var cartFromSession = HttpContext.Session.GetObjectFromJson<List<CartSessionItem>>("CartSession") ?? new List<CartSessionItem>();
                cartList = cartFromSession.Select(item =>
                {
                    var product = _unitOfWork.Product.Get(p => p.Id == item.ProductId);
                    return new ShoppingCart
                    {
                        ProductId = item.ProductId,
                        Count = item.Count,
                        Product = product,
                        Price = product.Price
                    };
                }).ToList();
            }

            // Kiểm tra giỏ có hàng không
            if (!cartList.Any())
            {
                // Nếu rỗng quay về giỏ
                return RedirectToAction("Summary");
            }

            // Tính tổng tiền
            foreach (var cart in cartList)
            {
                if (cart.Price == 0)
                {
                    cart.Price = GetPriceBasedOnQuantity(cart);
                }
                cartTotal += cart.Count * cart.Price;
            }

            // Tạo order header
            var orderHeader = new OrderHeader()
            {
				//ApplicationUser.Email = Email,
				GuestEmail = orderHeaderVM.OrderHeader.GuestEmail,
				ApplicationUserId = userId,
                Name = orderHeaderVM.OrderHeader.Name,
                PhoneNumber = orderHeaderVM.OrderHeader.PhoneNumber,
                StreetAddress = orderHeaderVM.OrderHeader.StreetAddress,
                City = orderHeaderVM.OrderHeader.City,
                State = orderHeaderVM.OrderHeader.State,
                PostalCode = orderHeaderVM.OrderHeader.PostalCode,
                OrderDate = DateTime.Now,
                OrderTotal = cartTotal,
                PaymentStatus = SD.PaymentStatusPending,
                OrderStatus = SD.StatusPending
            };

            _unitOfWork.OrderHeader.Add(orderHeader);
            _unitOfWork.Save();

            // Tạo order detail
            foreach (var cart in cartList)
            {
                var orderDetail = new OrderDetail()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = orderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
                var product = _unitOfWork.Product.Get(p => p.Id == cart.ProductId);
                product.Quantity -= cart.Count;
                _unitOfWork.Product.Update(product);
            }
            _unitOfWork.Save();

            // Tạo Stripe session
            var domain = $"{Request.Scheme}://{Request.Host.Value}/";
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = cartList.Select(item => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price),
                        Currency = "vnd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                }).ToList(),
                Mode = "payment",
                SuccessUrl = domain + $"Customer/Cart/OrderConfirmation?id={orderHeader.Id}",
                CancelUrl = domain + "Customer/Cart/Summary"
            };

            var service = new SessionService();
            Session session = service.Create(options);

            // Lưu sessionId
            orderHeader.SessionId = session.Id;
            _unitOfWork.Save();

            // Redirect tới Stripe Checkout
            return Redirect(session.Url);
        }


        public IActionResult OrderConfirmation(int id)
		{

            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");

            if (!string.IsNullOrEmpty(orderHeader.SessionId) && orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                var session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }

                HttpContext.Session.Clear();
            }

            // Xóa giỏ hàng sau khi đơn hàng thành công
            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
                .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);

            _unitOfWork.Save();
            if (orderHeader.ApplicationUser != null && !string.IsNullOrEmpty(orderHeader.ApplicationUser.Email))
            {
                var subject = "Xác nhận đơn hàng thành công";
                var message = $@"
                                Xin chào {orderHeader.ApplicationUser.Name},<br/><br/>
                                Cảm ơn bạn đã đặt hàng! Đơn hàng của bạn đã được xác nhận thành công.<br/>
                                Tổng tiền: {orderHeader.OrderTotal:N0} VNĐ.<br/>
                                Đơn hàng đang được chuẩn bị và sẽ sớm được vận chuyển.<br/><br/>
                                Xin cảm ơn quý khách và chúc quý khách một ngày tốt lành!
                            ";

                _emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, subject, message);
            }


            return View(id);
        }
        public IActionResult Plus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart
                                        .Get(u => u.Id == cartId, includeProperties: "Product");


            // Kiểm tra nếu số lượng không vượt quá giới hạn
            if (cartFromDb.Count < cartFromDb.Product.Quantity)
            {
                cartFromDb.Count += 1;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
                _unitOfWork.Save();
            }

            return RedirectToAction(nameof(Index)); // Quay lại trang danh sách giỏ hàng
        }

        public IActionResult Minus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId, tracked: true);

            if (cartFromDb.Count <= 1)
            {
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
                _unitOfWork.ShoppingCart.Remove(cartFromDb);
            }
            else
            {
                cartFromDb.Count -= 1;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }

            _unitOfWork.Save();

            return RedirectToAction(nameof(Index)); // Quay lại trang danh sách giỏ hàng
        }

        public IActionResult Remove(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId, tracked: true);
            _unitOfWork.ShoppingCart.Remove(cartFromDb);
            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if(shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }
            else
            {
                if(shoppingCart.Count <= 100)
                {
                    return shoppingCart.Product.Price50;
                }
                else
                {
                    return shoppingCart.Product.Price100;
                }
            }
            
        }
    }
}
