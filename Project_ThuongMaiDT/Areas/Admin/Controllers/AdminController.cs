using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMDT.DataAccess.Repository.IRepository;
using TMDT.Models;
using TMDT.Utility;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using BulkyBook.Models;
using TMDT.Models.ViewModels;

namespace YourNamespace.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index( DateTime? startDate, DateTime? endDate)
        {
            DateTime today = DateTime.Today;
            DateTime defaultStart = today.AddDays(-30);
            DateTime defaultEnd = today;

            DateTime fromDate = startDate ?? defaultStart;
            DateTime toDate = endDate ?? defaultEnd;
            var orders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser")
                .OrderByDescending(o => o.OrderDate)
                .ToList();
            var revenueByCategory = _unitOfWork.OrderDetail.GetAll()
                .Where(od => od.OrderHeader.OrderStatus == SD.StatusComplete)
                .Join(_unitOfWork.Product.GetAll(includeProperties: "Category"),
                    od => od.ProductId,
                    p => p.Id,
                    (od, p) => new { CategoryName = p.Category.Name, Revenue = (od.Price - od.Product.ListPrice) * od.Count })
                .GroupBy(x => x.CategoryName)
                .Select(g => new { Category = g.Key, TotalRevenue = g.Sum(x => x.Revenue) })
                .ToDictionary(x => x.Category, x => x.TotalRevenue);
            var revenueByPublisher = _unitOfWork.OrderDetail.GetAll()
                .Where(od => od.OrderHeader.OrderStatus == SD.StatusComplete)
                .Join(_unitOfWork.Product.GetAll(includeProperties: "Publisher"),
                    od => od.ProductId,
                    p => p.Id,
                    (od, p) => new { PublisherName = p.Publisher.Name, Revenue = (od.Price - od.Product.ListPrice) * od.Count })
                .GroupBy(x => x.PublisherName)
                .Select(g => new { Publisher = g.Key, TotalRevenue = g.Sum(x => x.Revenue) })
                .ToDictionary(x => x.Publisher, x => x.TotalRevenue);
            var completedOrderHeaders = _unitOfWork.OrderHeader
               .GetAll(u => u.OrderStatus == SD.StatusComplete
                         && u.ShippingDate >= fromDate
                         && u.ShippingDate <= toDate
                         && u.PaymentStatus == SD.PaymentStatusApproved)
               .ToList();

            var orderIds = completedOrderHeaders.Select(o => o.Id).ToList();

            // Lấy chi tiết đơn hàng
            var orderDetails = _unitOfWork.OrderDetail
                .GetAll(od => orderIds.Contains(od.OrderHeaderId), includeProperties: "Product")
                .ToList();

            // Tính doanh thu và lợi nhuận theo ngày
            var statistics = completedOrderHeaders
                            .GroupBy(o => o.ShippingDate.Date)
                            .Select(g =>
                            {
                                var dayOrders = g.ToList();
                                var dayOrderIds = dayOrders.Select(o => o.Id).ToList();

                                decimal revenue = (decimal)dayOrders.Sum(o => o.OrderTotal);

                                var cost = orderDetails
                                    .Where(od => dayOrderIds.Contains(od.OrderHeaderId))
                                    .Sum(od => od.Count * (decimal)od.Product.ListPrice);

                                decimal profit = revenue - cost;

                                return new
                                {
                                    Ngay = g.Key,
                                    DoanhThu = revenue,
                                    LoiNhuan = profit
                                };
                            })
                            .OrderBy(x => x.Ngay)
                            .ToList();
            var viewModel = new OrderStatusVM
            {
                CompletedOrders = orders.Count(o => o.OrderStatus == SD.StatusComplete),
                PendingOrders = orders.Count(o => o.PaymentStatus == SD.PaymentStatusDelayedPayment || o.PaymentStatus == "Thanh toán bằng tiền mặt"),
                InProcessOrders = orders.Count(o => o.OrderStatus == SD.StatusInProcess),
                CancelledOrders = orders.Count(),
                Orders = orders,
                ProductCountByCategory = _unitOfWork.Product.GetAll(includeProperties: "Category")
                    .GroupBy(p => p.Category.Name)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ChartLabels = revenueByCategory.Any() ? $"[{string.Join(",", revenueByCategory.Keys.Select(k => $"'{k}'"))}]" : "[]",
                ChartData = revenueByCategory.Any() ? $"[{string.Join(",", revenueByCategory.Values)}]" : "[]",
                PublisherChartLabels = revenueByPublisher.Any() ? $"[{string.Join(",", revenueByPublisher.Keys.Select(k => $"'{k}'"))}]" : "[]",
                PublisherChartData = revenueByPublisher.Any() ? $"[{string.Join(",", revenueByPublisher.Values)}]" : "[]"
            };
            ViewBag.Labels = statistics.Select(x => x.Ngay.ToString("dd/MM/yyyy")).ToList();
            ViewBag.Data_DoanhThu = statistics.Select(x => x.DoanhThu).ToList();
            ViewBag.Data_LoiNhuan = statistics.Select(x => x.LoiNhuan).ToList();
            ViewBag.StartDate = fromDate.ToString("yyyy-MM-dd");
            ViewBag.EndDate = toDate.ToString("yyyy-MM-dd");
            ViewBag.TotalProfit = statistics.Sum(x => x.LoiNhuan);
            ViewBag.TotalRevenue = statistics.Sum(x => x.DoanhThu); 
            return View(viewModel);
        }

        public IActionResult Details(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var order = _unitOfWork.OrderHeader.Get(o => o.Id == id, includeProperties: "ApplicationUser,OrderDetails.Product");
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var order = _unitOfWork.OrderHeader.Get(o => o.Id == id, includeProperties: "ApplicationUser");
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        public IActionResult Edit(OrderHeader obj)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.OrderHeader.Update(obj);
                _unitOfWork.Save();
                TempData["success"] = "Đã cập nhật đơn hàng thành công";
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var order = _unitOfWork.OrderHeader.Get(o => o.Id == id, includeProperties: "ApplicationUser");
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePOST(int? id)
        {
            var order = _unitOfWork.OrderHeader.Get(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            _unitOfWork.OrderHeader.Remove(order);
            _unitOfWork.Save();
            TempData["success"] = "Đã xóa đơn hàng thành công";
            return RedirectToAction("Index");
        }
    }
}