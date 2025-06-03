using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDT.Models
{
    public class Publisher
    {
        [Key]
        public int Id { get; set; }
        [Display(Name = "Tên NXB")]

        [Required(ErrorMessage = "Tên nhà xuất bản là bắt buộc.")]
        public string Name { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public string ImageUrl { get; set; } = "https://placehold.co/500x600/png";  // Thêm thuộc tính ảnh

        [Display(Name = "Mô tả")]

        [Required(ErrorMessage = "Mô tả là bắt buộc.")]
        public string Description { get; set; }  // Thêm thuộc tính mô tả
    }
}
