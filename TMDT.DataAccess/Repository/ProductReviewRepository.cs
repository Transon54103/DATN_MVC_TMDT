using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMDT.DataAccess.Data;
using TMDT.DataAccess.Repository.IRepository;
using TMDT.Models;

namespace TMDT.DataAccess.Repository
{
    public class ProductReviewRepository : Repository<ProductReview>, IProductReviewRepository
    {
        private readonly ApplicationDbContext _db;

        public ProductReviewRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ProductReview review)
        {
            _db.ProductReview.Update(review);
        }
    }
}
