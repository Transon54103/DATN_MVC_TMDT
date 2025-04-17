using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMDT.DataAccess.Repository.IRepository;
using TMDT.Models;
using TMDT.Utility;

namespace Project_ThuongMaiDT.Areas.Admin.Controllers
{

        [Area("Admin")]
        [Authorize(Roles = SD.Role_Admin)]
        public class PublisherController : Controller
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly IWebHostEnvironment _webHostEnvironment;

            public PublisherController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
            {
                _unitOfWork = unitOfWork;
                _webHostEnvironment = webHostEnvironment;
            }

            // GET: Publisher/Index
            public IActionResult Index()
            {
                var publisherList = _unitOfWork.Publisher.GetAll().ToList();
                return View(publisherList);
            }

            // GET: Publisher/Upsert
            public IActionResult Upsert(int? id)
            {
                if (id == null || id == 0)
                {
                    // Create
                    return View(new Publisher());
                }
                else
                {
                    // Update
                    Publisher publisherObj = _unitOfWork.Publisher.Get(u => u.Id == id);
                    return View(publisherObj);
                }
            }

            // POST: Publisher/Upsert
            [HttpPost]
        public IActionResult Upsert(Publisher publisher, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;

                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string publisherPath = Path.Combine(wwwRootPath, @"Images\publishers");

                    // Tạo thư mục nếu chưa tồn tại
                    if (!Directory.Exists(publisherPath))
                    {
                        Directory.CreateDirectory(publisherPath);
                    }

                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(publisher.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, publisher.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Lưu ảnh mới
                    using (var fileStream = new FileStream(Path.Combine(publisherPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    publisher.ImageUrl = @"\Images\publishers\" + fileName;
                }

                if (publisher.Id == 0)
                {
                    _unitOfWork.Publisher.Add(publisher);
                    TempData["success"] = "Publisher added successfully";
                }
                else
                {
                    _unitOfWork.Publisher.Update(publisher);
                    TempData["success"] = "Publisher updated successfully";
                }

                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            return View(publisher);
        }


        // API CALLS

        [HttpGet]
            public IActionResult GetAll()
            {
                var publisherList = _unitOfWork.Publisher.GetAll().ToList();
                return Json(new { data = publisherList });
            }

            [HttpDelete]
            public IActionResult Delete(int? id)
            {
                var publisherToBeDeleted = _unitOfWork.Publisher.Get(u => u.Id == id);
                if (publisherToBeDeleted == null)
                {
                    return Json(new { success = false, message = "Error while deleting" });
                }

                // Xóa ảnh nếu có
                if (!string.IsNullOrEmpty(publisherToBeDeleted.ImageUrl))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, publisherToBeDeleted.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                _unitOfWork.Publisher.Remove(publisherToBeDeleted);
                _unitOfWork.Save();
                return Json(new { success = true, message = "Publisher deleted successfully" });
            }

            [HttpPost]
            public IActionResult UpdateIsActive(int id)
            {
                var publisher = _unitOfWork.Publisher.Get(u => u.Id == id);
                if (publisher == null)
                {
                    return Json(new { success = false, message = "Publisher not found" });
                }
                _unitOfWork.Publisher.Update(publisher);
                _unitOfWork.Save();

                return Json(new { success = true, message = "Publisher status updated successfully" });
            }
        }
    }

