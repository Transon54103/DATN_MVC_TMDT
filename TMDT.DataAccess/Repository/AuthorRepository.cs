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
    public class AuthorRepository: Repository<Author>, IAuthorRepository
    {
        private ApplicationDbContext _db;
        public AuthorRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }


        public void Update(Author obj)
        {
            _db.authors.Update(obj);
        }
    }
}
