﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMDT.Models;

namespace TMDT.DataAccess.Repository.IRepository
{

        public interface IProductReviewRepository : IRepository<ProductReview>
        {
            void Update(ProductReview review);
        }
}
