using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMDT.DataAccess.Data;
using TMDT.DataAccess.Repository.IRepository;


namespace TMDT.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _db;
        public IAuthorRepository Author { get; set; }
        public ICategoryRepository Category { get; private set; }
        public IProductRepository Product { get; private set; }
        public ICompanyRepository Company { get; private set; }
        public IApplicationUserRepository ApplicationUser { get; private set; }
        public IShoppingCartRepository ShoppingCart { get; private set; }
        public IOrderHeaderRepository OrderHeader { get; private set; }
        public IOrderDetailRepository OrderDetail { get; private set; }
        public IRecommendationRepository Recommendation { get; private set; } // ✅ Thêm mới
        public IProductImageRepository ProductImage { get; private set; }
        public IPublisherRepository Publisher { get; private set; }
        public IProductReviewRepository ProductReview { get; private set; }
        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;
            ApplicationUser = new ApplicationUserRepository(_db);
            ShoppingCart = new ShoppingCartRepository(_db);
            Category = new CategoryRepository(_db);
            Product = new ProductRepository(_db);
            Company = new CompanyRepository(_db);
            OrderHeader = new OrderHeaderRepository(_db);
            OrderDetail = new OrderDetailRepository(_db);
            Recommendation = new RecommendationRepository(_db);
            Author = new AuthorRepository(_db);// ✅ Khởi tạo
            ProductImage = new ProductImageRepository(_db);
            Publisher = new PublisherRepository(_db);
            ProductReview = new ProductReviewRepository(_db);
        }

        public void Save()
        {
            _db.SaveChanges();
        }
    }
}
