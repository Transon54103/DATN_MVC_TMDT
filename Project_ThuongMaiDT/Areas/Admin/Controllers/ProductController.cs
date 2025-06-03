using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMDT.DataAccess.Data;
using TMDT.DataAccess.Repository.IRepository;
using TMDT.Models;
using TMDT.Models.ViewModels;
using TMDT.Utility;


namespace Project_ThuongMaiDT.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            List<Product> objproductlist = _unitOfWork.Product.GetAll(includeProperties: "Category,Authors,Publisher").ToList();

            return View(objproductlist);
        }
        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new()
            {
                CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                AuthorList = _unitOfWork.Author.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.AuthorId.ToString()
                }),
                PublisherList = _unitOfWork.Publisher.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                Product = new Product()
            };
            if (id == null || id == 0)
            {
                //create
                return View(productVM);
            }
            else
            {
                //update
                productVM.Product = _unitOfWork.Product.Get(u => u.Id == id, includeProperties: "ProductImages,Publisher,Authors");
                return View(productVM);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVM productVM, List<IFormFile> files)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (productVM.Product.Id == 0)
                    {
                        // Thêm mới sản phẩm
                        _unitOfWork.Product.Add(productVM.Product);
                    }
                    else
                    {
                        // Cập nhật sản phẩm
                        _unitOfWork.Product.Update(productVM.Product);
                    }

                    _unitOfWork.Save();

                    string wwwRootPath = _webHostEnvironment.WebRootPath;

                    if (files != null && files.Count > 0)
                    {
                        string productPath = @"images\products\product-" + productVM.Product.Id;
                        string finalPath = Path.Combine(wwwRootPath, productPath);

                        if (!Directory.Exists(finalPath))
                            Directory.CreateDirectory(finalPath);

                        foreach (IFormFile file in files)
                        {
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            string filePath = Path.Combine(finalPath, fileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                file.CopyTo(fileStream);
                            }

                            ProductImage productImage = new ProductImage
                            {
                                ImageUrl = @"\" + productPath + @"\" + fileName,
                                ProductId = productVM.Product.Id,
                            };

                            _unitOfWork.ProductImage.Add(productImage);
                        }

                        _unitOfWork.Save();
                    }

                    TempData["success"] = "Sản phẩm đã chỉnh sửa thành công!";
                    return RedirectToAction("Index");
                }
                catch (InvalidOperationException ex)
                {
                    // Xử lý lỗi từ ProductRepository (ví dụ: ISBN trùng lặp)
                    ModelState.AddModelError("Product.ISBN", ex.Message);
                }
                catch (DbUpdateException ex)
                {
                    // Xử lý lỗi từ cơ sở dữ liệu (ví dụ: vi phạm ràng buộc duy nhất)
                    if (ex.InnerException?.Message.Contains("ISBN") ?? false)
                    {
                        ModelState.AddModelError("Product.ISBN", "ISBN đã tồn tại. Vui lòng sử dụng một ISBN duy nhất.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu sản phẩm.");
                    }
                }
            }

            // Nếu ModelState không hợp lệ, nạp lại các danh sách dropdown
            productVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });

            productVM.AuthorList = _unitOfWork.Author.GetAll().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.AuthorId.ToString()
            });

            productVM.PublisherList = _unitOfWork.Publisher.GetAll().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });

            return View(productVM);
        }

        public IActionResult DeleteImage(int imageId)
        {
            var imageToBeDeleted = _unitOfWork.ProductImage.Get(u => u.Id == imageId);
            int productId = imageToBeDeleted.ProductId;
            if (imageToBeDeleted != null)
            {
                if (!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
                {
                    var oldImagePath =
                                   Path.Combine(_webHostEnvironment.WebRootPath,
                                   imageToBeDeleted.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                _unitOfWork.ProductImage.Remove(imageToBeDeleted);
                _unitOfWork.Save();

                TempData["success"] = "Deleted successfully";
            }

            return RedirectToAction(nameof(Upsert), new { id = productId });
        }
        //public IActionResult Edit(int? id)
        //{
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }
        //    Product productFromDb = _unitOfWork.Product.Get(u => u.Id == id);
        //    //Product productFromDb1 = _dB.categories.FirstOrDefault(u => u.Id==id);
        //    //Product productFromDb2 = _dB.categories.Where(u => u.Id==id).FirstOrDefault();
        //    if (productFromDb == null) { return NotFound(); }

        //    return View(productFromDb);
        //}
        //[HttpPost]
        //public IActionResult Edit(Product obj)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _unitOfWork.Product.Update(obj);
        //        _unitOfWork.Save();
        //        TempData["success"] = "Product update successfully";
        //        return RedirectToAction("Index");
        //    }
        //    return View();
        //}

        //public IActionResult Delete(int? id)
        //{
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }
        //    Product productFromDb = _unitOfWork.Product.Get(u => u.Id == id);
        //    if (productFromDb == null) { return NotFound(); }

        //    return View(productFromDb);
        //}
        //[HttpPost, ActionName("Delete")]
        //public IActionResult DeletePOST(int? id)
        //{
        //    Product obj = _unitOfWork.Product.Get(u => u.Id == id);
        //    if (obj == null) { return NotFound(); }
        //    _unitOfWork.Product.Remove(obj);
        //    _unitOfWork.Save();
        //    TempData["success"] = "Product deleted successfully";
        //    return RedirectToAction("Index");
        //}
        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objproductlist = _unitOfWork.Product.GetAll(includeProperties: "Category,Authors").ToList();
            return Json(new { data = objproductlist });
        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productToBeDeleted = _unitOfWork.Product.Get(u => u.Id == id);
            if (productToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            string productPath = @"images\products\product-" + id;
            string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);

            if (Directory.Exists(finalPath))
            {
                string[] filePaths = Directory.GetFiles(finalPath);
                foreach (string filePath in filePaths)
                {
                    System.IO.File.Delete(filePath);
                }

                Directory.Delete(finalPath);
            }

            _unitOfWork.Product.Remove(productToBeDeleted);
            _unitOfWork.Save();
            return Json(new { success = false, message = "Error successful" });
        }
        [HttpPost]
        public IActionResult UpdateIsActive(int id)
        {
            var product = _unitOfWork.Product.Get(u => u.Id == id);
            if (product.IsActive == null)
            {
                product.IsActive = false;
            }

            product.IsActive = !product.IsActive;
            _unitOfWork.Product.Update(product);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Sản phẩm đã được cập nhật" });
        }

        #endregion

    }
}
