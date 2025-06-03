using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TMDT.DataAccess.Repository.IRepository;
using TMDT.Models;
using TMDT.Models.ViewModels;
using TMDT.Utility;

namespace Project_ThuongMaiDT.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class AuthorController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public AuthorController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            List<Author> objproductlist = _unitOfWork.Author.GetAll().ToList();

            return View(objproductlist);
        }
        public IActionResult Upsert(int? id)
        {

            if (id == null || id == 0)
            {
                //create
                return View(new Author());
            }
            else
            {
                //update
                Author companyObj = _unitOfWork.Author.Get(u => u.AuthorId == id);
                return View(companyObj);
            }
        }
        [HttpPost]
        public IActionResult Upsert(Author author, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;

                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string authorPath = Path.Combine(wwwRootPath, @"Images\authors");

                    if (!string.IsNullOrEmpty(author.ImageUrl))
                    {
                        // Xóa ảnh cũ nếu có
                        var oldImagePath = Path.Combine(wwwRootPath, author.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(authorPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    author.ImageUrl = @"\Images\authors\" + fileName;
                }

                if (author.AuthorId == 0)
                {
                    _unitOfWork.Author.Add(author);
                    TempData["success"] = "Đã thêm tác giả mới!";
                }
                else
                {
                    _unitOfWork.Author.Update(author);
                    TempData["success"] = "Đã cập nhât tác giả!";
                }

                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            return View(author);
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
            List<Author> authorList = _unitOfWork.Author.GetAll().ToList();
            return Json(new { data = authorList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var authorToBeDeleted = _unitOfWork.Author.Get(u => u.AuthorId == id);
            if (authorToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            // Xóa ảnh nếu có
            if (!string.IsNullOrEmpty(authorToBeDeleted.ImageUrl))
            {
                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, authorToBeDeleted.ImageUrl.TrimStart('\\'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            _unitOfWork.Author.Remove(authorToBeDeleted);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Author deleted successfully" });
        }

        [HttpPost]
        public IActionResult UpdateIsActive(int id)
        {
            var author = _unitOfWork.Author.Get(u => u.AuthorId == id);
            if (author == null)
            {
                return Json(new { success = false, message = "Author not found" });
            }
            _unitOfWork.Author.Update(author);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Author status updated successfully" });
        }

        #endregion


    }
}
