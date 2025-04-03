using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDT.Models
{
    public class Author
    {
        [Key]
        public int AuthorId { get; set; }  // Khóa chính
        public string Name { get; set; }

        public string? ImageUrl { get; set; }
    }
}
