using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using TMDT.DataAccess.Repository.IRepository;
using TMDT.Models;
using TMDT.Models.ViewModels;
using TMDT.Utility;

namespace Project_ThuongMaiDT.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork; //thuô?c ti?nh cu?a class na?y 

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index(string searchTerm, string category, string author, string publisher, int pageNumber = 1, int pageSize = 12)
        {
            // Chỉ lấy sản phẩm có IsActive = true
            IEnumerable<Product> productList = _unitOfWork.Product.GetAll(includeProperties: "Category,Authors,Publisher,ProductImages")
                .Where(p => p.IsActive == true);

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                productList = productList.Where(p =>
                    (p.Title != null && p.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (p.Authors.Name != null && p.Authors.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                );
            }

            // Lọc theo danh mục
            if (!string.IsNullOrEmpty(category))
            {
                productList = productList.Where(p => p.Category.Name == category);
            }

            // Lọc theo tác giả
            if (!string.IsNullOrEmpty(author))
            {
                productList = productList.Where(p => p.Authors.Name == author);
            }

            // Lọc theo nhà xuất bản
            if (!string.IsNullOrEmpty(publisher))
            {
                productList = productList.Where(p => p.Publisher.Name == publisher);
            }

            // Tính tổng số sản phẩm sau khi lọc
            var totalProducts = productList.Count();
            var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

            // Phân trang
            var paginatedProducts = productList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Lấy danh sách danh mục
            var categories = _unitOfWork.Category.GetAll()
                .Select(c => c.Name)
                .Distinct()
                .ToList();

            // Lấy danh sách tác giả
            var authors = _unitOfWork.Author.GetAll()
                .Select(a => a.Name)
                .Distinct()
                .ToList();

            // Lấy danh sách nhà xuất bản
            var publishers = _unitOfWork.Publisher.GetAll()
                .Select(p => p.Name)
                .Distinct()
                .ToList();

            // Lưu thông tin vào ViewData để hiển thị trên giao diện
            ViewData["CurrentFilter"] = searchTerm;
            ViewData["Categories"] = categories;
            ViewData["Authors"] = authors;
            ViewData["Publishers"] = publishers;

            ViewData["SelectedCategory"] = category;
            ViewData["SelectedAuthor"] = author;
            ViewData["SelectedPublisher"] = publisher;

            var viewModel = new ProductViewModel
            {
                Products = paginatedProducts,
                CurrentPage = pageNumber,
                TotalPages = totalPages
            };

            return View(viewModel);
        }



        public IActionResult Details(int productId)
        {
            // Lấy sản phẩm theo productId và kiểm tra xem sản phẩm có tồn tại và có trạng thái IsActive = true không
            var product = _unitOfWork.Product
                .Get(u => u.Id == productId && u.IsActive == true, includeProperties: "Category,Authors,ProductImages");

            if (product == null)
            {
                return NotFound(); // Trả về 404 nếu sản phẩm không tồn tại hoặc không được duyệt
            }

            // Kiểm tra nếu người dùng đã mua sản phẩm này
            string userId = User.Identity.Name; // Hoặc cách lấy UserId từ Authentication
            var orderHeaders = _unitOfWork.OrderDetail.GetAll(od => od.OrderHeader.ApplicationUser.UserName == userId && od.ProductId == productId
                                    && od.OrderHeader.OrderStatus == SD.StatusShipped && od.OrderHeader.PaymentStatus == SD.PaymentStatusApproved)
                                                      .Select(od => od.OrderHeader)
                                                      .Distinct()
                                                      .ToList();
            bool hasPurchased = orderHeaders.Any(); // Kiểm tra xem người dùng có đơn hàng chứa sản phẩm này

            // Gợi ý sản phẩm
            var recommendedProductIds = _unitOfWork.Recommendation.GetRecommendedProducts(productId);
            var recommendedProducts = _unitOfWork.Product
                .GetAll(u => recommendedProductIds.Contains(u.Id) && u.IsActive == true, includeProperties: "Authors,ProductImages")
                .ToList();

            // Lấy danh sách review cho sản phẩm này
            var reviews = _unitOfWork.ProductReview
                .GetAll(u => u.ProductId == productId, includeProperties: "User")
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            // Tạo ShoppingCart để giữ sản phẩm và số lượng
            ShoppingCart cart = new()
            {
                Product = product,
                Count = 1,
                ProductId = productId,
            };

            // Gửi các dữ liệu cần sang View
            ViewBag.RecommendedProducts = recommendedProducts;
            ViewBag.Reviews = reviews;
            ViewBag.HasPurchased = hasPurchased; // Truyền thông tin về việc mua sản phẩm sang view
            ViewBag.IsLoggedIn = User.Identity.IsAuthenticated; // Truyền trạng thái đăng nhập

            return View(cart);
        }




        [HttpGet]
        public IActionResult AddToCart()
        {
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public IActionResult AddToCart(int productId)
        {
            if (User.Identity.IsAuthenticated)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                ShoppingCart cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.ApplicationUserId == userId && u.ProductId == productId);

                if (cartFromDb != null)
                {
                    cartFromDb.Count += 1;
                    _unitOfWork.ShoppingCart.Update(cartFromDb);
                }
                else
                {
                    ShoppingCart shoppingCart = new()
                    {
                        ApplicationUserId = userId,
                        ProductId = productId,
                        Count = 1
                    };
                    _unitOfWork.ShoppingCart.Add(shoppingCart);
                }

                _unitOfWork.Save();

                int cartCount = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId).Count();
                HttpContext.Session.SetInt32(SD.SessionCart, cartCount);

                return Json(new { success = true, cartCount = cartCount, message = "Sản phẩm đã được thêm vào giỏ hàng!" });
            }
            else
            {
                // Nếu chưa đăng nhập — lưu vào session
                var cartSession = HttpContext.Session.GetObjectFromJson<List<CartSessionItem>>("CartSession") ?? new List<CartSessionItem>();

                var existingItem = cartSession.FirstOrDefault(c => c.ProductId == productId);
                if (existingItem != null)
                {
                    existingItem.Count += 1;
                }
                else
                {
                    cartSession.Add(new CartSessionItem
                    {
                        ProductId = productId,
                        Count = 1
                    });
                }

                // Cập nhật lại session
                HttpContext.Session.SetObjectAsJson("CartSession", cartSession);

                return Json(new { success = true, cartCount = cartSession.Sum(c => c.Count), message = "Sản phẩm đã được thêm vào giỏ hàng!" });
            }
        }




        [HttpPost]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            if (User.Identity.IsAuthenticated)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                shoppingCart.ApplicationUserId = userId;

                ShoppingCart cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.ApplicationUserId == userId && u.ProductId == shoppingCart.ProductId);

                if (cartFromDb != null)
                {
                    // Nếu đã có sản phẩm này thì cộng thêm số lượng
                    cartFromDb.Count += shoppingCart.Count;
                    _unitOfWork.ShoppingCart.Update(cartFromDb);
                }
                else
                {
                    // Nếu chưa có thì thêm mới
                    _unitOfWork.ShoppingCart.Add(shoppingCart);
                }

                _unitOfWork.Save();

                // Cập nhật lại số lượng giỏ hàng trong session
                int cartCount = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId).Sum(u => u.Count);
                HttpContext.Session.SetInt32(SD.SessionCart, cartCount);

                TempData["success"] = "Bạn đã thêm vào giỏ hàng";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                // Nếu chưa đăng nhập — lưu vào session
                var cartSession = HttpContext.Session.GetObjectFromJson<List<CartSessionItem>>("CartSession") ?? new List<CartSessionItem>();

                var existingItem = cartSession.FirstOrDefault(c => c.ProductId == shoppingCart.ProductId);
                if (existingItem != null)
                {
                    existingItem.Count += shoppingCart.Count;
                }
                else
                {
                    cartSession.Add(new CartSessionItem
                    {
                        ProductId = shoppingCart.ProductId,
                        Count = shoppingCart.Count
                    });
                }

                // Cập nhật lại session
                HttpContext.Session.SetObjectAsJson("CartSession", cartSession);

                TempData["success"] = "Bạn đã thêm vào giỏ hàng";
                return RedirectToAction(nameof(Index));
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult TopSellingBooks()
        {
            var topProducts = _unitOfWork.OrderDetail.GetTopSellingProducts(6);
            return View(topProducts);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


    }
}