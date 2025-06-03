using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TMDT.DataAccess.Data;
using TMDT.DataAccess.Repository.IRepository;
using TMDT.Models;

namespace TMDT.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository //product có thể thay là sách or xe 
    {
        private ApplicationDbContext _db;
        public ProductRepository(ApplicationDbContext db) : base(db)
        {
            _db = db; 
        }

        public override void Add(Product product)
        {
            // Kiểm tra tính duy nhất của ISBN
            if (_db.products.Any(p => p.ISBN == product.ISBN))
            {
                throw new InvalidOperationException("ISBN đã tồn tại. Vui lòng sử dụng một ISBN duy nhất.");
            }

            base.Add(product); // Gọi phương thức Add của lớp cha (Repository<T>)
        }
        public void Update(Product obj)
        {
            var objFromDb = _db.products.FirstOrDefault(x => x.Id == obj.Id);
            if (objFromDb != null)
            {
                if (obj.ISBN != objFromDb.ISBN && _db.products.Any(p => p.ISBN == obj.ISBN && p.Id != obj.Id))
                {
                    throw new InvalidOperationException("ISBN đã tồn tại. Vui lòng sử dụng một ISBN duy nhất.");
                }
                objFromDb.Title = obj.Title;
                objFromDb.ISBN = obj.ISBN;
                objFromDb.Price = obj.Price;
                objFromDb.Price50 = obj.Price50;
                objFromDb.ListPrice = obj.ListPrice;
                objFromDb.Price100 = obj.Price100;
                objFromDb.Description = obj.Description;
                objFromDb.CategoryId = obj.CategoryId;
                objFromDb.AuthorId = obj.AuthorId;
                objFromDb.IsActive = obj.IsActive;
                objFromDb.Quantity = obj.Quantity;
                //if (obj.ImageUrl != null)
                //{
                //    objFromDb.ImageUrl = obj.ImageUrl;
                //}
                objFromDb.ProductImages = obj.ProductImages;
                objFromDb.PublisherId = obj.PublisherId;
            }
        }
    }
}
