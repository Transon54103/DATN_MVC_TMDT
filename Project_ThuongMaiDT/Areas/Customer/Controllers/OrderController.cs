﻿using BulkyBook.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Stripe;
using System.Security.Claims;
using TMDT.DataAccess.Repository.IRepository;
using TMDT.Models.ViewModels;
using TMDT.Utility;

namespace Project_ThuongMaiDT.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int orderId)
        {
            OrderVM = new()
            {
                OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
            };

            return View(OrderVM);
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
            orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.TrackingNumber;
            }
            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();

            TempData["Success"] = "Chi tiết đơn hàng đã được cập nhật";


            return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["Success"] = "Chi tiết đơn hàng đã được cập nhật";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {

            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
            orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
            }

            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();
            TempData["Success"] = "Chi tiết đơn hàng đã được cập nhật";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
        }
        [HttpPost]
        public IActionResult CancelOrder()
        {

            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

            if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
            }
            var orderDetails = _unitOfWork.OrderDetail.GetAll(od => od.OrderHeaderId == orderHeader.Id);
            foreach (var item in orderDetails)
            {
                var product = _unitOfWork.Product.Get(p => p.Id == item.ProductId);
                if (product != null)
                {
                    product.Quantity += item.Count;
                    _unitOfWork.Product.Update(product);
                }
            }
            _unitOfWork.Save();
            TempData["Success"] = "Đã hủy đơn hàng";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });

        }


        [ActionName("Details")]
        [HttpPost]
        public IActionResult Details_PAY_NOW()
        {
            OrderVM.OrderHeader = _unitOfWork.OrderHeader
                .Get(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
            OrderVM.OrderDetail = _unitOfWork.OrderDetail
                .GetAll(u => u.OrderHeaderId == OrderVM.OrderHeader.Id, includeProperties: "Product");

            //stripe logic
            var domain = Request.Scheme + "://" + Request.Host.Value + '/';
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"customer/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
                CancelUrl = domain + $"customer/order/details?orderId={OrderVM.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                Currency = "vnd"
            };

            foreach (var item in OrderVM.OrderDetail)
            {
                var sessionLineItem = new SessionLineItemOptions
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
                };
                options.LineItems.Add(sessionLineItem);
            }


            var service = new SessionService();
            Session session = service.Create(options);
            _unitOfWork.OrderHeader.UpdateStripePaymentID(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {

            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment || orderHeader.PaymentStatus == SD.PaymentStatusPending)
            {
                //this is an order by company

                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId);
                    if(orderHeader.OrderStatus == SD.StatusPending)
                    {
                        _unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, SD.StatusApproved, SD.PaymentStatusApproved);
                    }
                    else
                    {
                        _unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    }
                    HttpContext.Session.Clear();
                    _unitOfWork.Save();
                }
            }


            return View(orderHeaderId);
        }
        public IActionResult SalesStatistics(int? startMonth, int? startYear, int? endMonth, int? endYear)
        {
            DateTime today = DateTime.Today;
            DateTime defaultStart = new DateTime(today.Year, today.Month, 1).AddMonths(-6); // Mặc định 7 tháng gần nhất
            DateTime defaultEnd = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

            DateTime startDate = (startMonth.HasValue && startYear.HasValue)
                ? new DateTime(startYear.Value, startMonth.Value, 1)
                : defaultStart;

            DateTime endDate = (endMonth.HasValue && endYear.HasValue)
                ? new DateTime(endYear.Value, endMonth.Value, DateTime.DaysInMonth(endYear.Value, endMonth.Value))
                : defaultEnd;

            var completedOrders = _unitOfWork.OrderHeader
                .GetAll(u => u.OrderStatus == SD.StatusShipped && u.ShippingDate >= startDate && u.ShippingDate <= endDate)
                .GroupBy(o => new { o.ShippingDate.Year, o.ShippingDate.Month }) // Nhóm theo tháng
                .Select(g => new
                {
                    Thang = new DateTime(g.Key.Year, g.Key.Month, 1),
                    DoanhThu = g.Sum(o => (decimal)o.OrderTotal)
                })
                .OrderBy(x => x.Thang)
                .ToList();

            // Truyền dữ liệu cho View
            ViewBag.Labels = completedOrders.Select(x => x.Thang.ToString("MM/yyyy")).ToList();
            ViewBag.Data = completedOrders.Select(x => x.DoanhThu).ToList();
            ViewBag.StartMonth = startMonth ?? defaultStart.Month;
            ViewBag.StartYear = startYear ?? defaultStart.Year;
            ViewBag.EndMonth = endMonth ?? defaultEnd.Month;
            ViewBag.EndYear = endYear ?? defaultEnd.Year;

            var totalRevenue = completedOrders.Sum(x => x.DoanhThu);

            return View(totalRevenue);
        }
        #region API CALLS

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> objOrderHeaders;
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                objOrderHeaders = _unitOfWork.OrderHeader
                    .GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser");


            switch (status)
            {
                case "pending":
                    objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;

            }


            return Json(new { data = objOrderHeaders });
        }

        // ... existing code ...
        #endregion
    }
}
