using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace TMDT.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
        [Display(Name = "Tên sản phẩm")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc.")]
        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Mã ISBN là bắt buộc.")]
        [Display(Name = "Mã ISBN")]
        public string ISBN { get; set; }

        [Required(ErrorMessage = "Giá gốc là bắt buộc.")]
        [Display(Name = "Giá gốc")]
        [Range(1000, 100000000, ErrorMessage = "Giá gốc phải nằm trong khoảng từ 1.000 đến 100.000.000.")]
        public double ListPrice { get; set; }

        [Required(ErrorMessage = "Giá bán là bắt buộc.")]
        [Display(Name = "Giá bán")]
        [Range(1000, 100000000, ErrorMessage = "Giá bán phải nằm trong khoảng từ 1.000 đến 100.000.000.")]
        public double Price { get; set; }

        [Required(ErrorMessage = "Giá khi mua từ 50 cuốn là bắt buộc.")]
        [Display(Name = "Giá khi mua từ 50 cuốn")]
        [Range(1000, 100000000, ErrorMessage = "Giá khi mua từ 50 cuốn phải nằm trong khoảng từ 1.000 đến 100.000.000.")]
        public double Price50 { get; set; }

        [Required(ErrorMessage = "Giá khi mua từ 100 cuốn là bắt buộc.")]
        [Display(Name = "Giá khi mua từ 100 cuốn")]
        [Range(1000, 100000000, ErrorMessage = "Giá khi mua từ 100 cuốn phải nằm trong khoảng từ 1.000 đến 100.000.000.")]
        public double Price100 { get; set; }

        [Required(ErrorMessage = "Thể loại là bắt buộc.")]
        [Display(Name = "Thể loại")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tác giả là bắt buộc.")]
        [Display(Name = "Tác giả")]
        public int AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        [ValidateNever]
        public Author Authors { get; set; }

        [ForeignKey("CategoryId")]
        [ValidateNever]
        public Category Category { get; set; }

        [ValidateNever]
        public bool? IsActive { get; set; }

        [Display(Name = "Số lượng")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn hoặc bằng 0.")]
        public int? Quantity { get; set; }

        [ValidateNever]
        public List<ProductImage> ProductImages { get; set; }

        [Required(ErrorMessage = "Nhà xuất bản là bắt buộc.")]
        [Display(Name = "Nhà xuất bản")]
        public int PublisherId { get; set; }

        [ForeignKey("PublisherId")]
        [ValidateNever]
        public Publisher Publisher { get; set; }
    }
}