using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TMDT.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên thể loại là bắt buộc.")]
        [MaxLength(30, ErrorMessage = "Tên thể loại tối đa 30 ký tự.")]
        [DisplayName("Tên thể loại")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Thứ tự hiển thị là bắt buộc.")]
        [DisplayName("Thứ tự hiển thị")]
        [Range(1, 100, ErrorMessage = "Thứ tự hiển thị phải từ 1 đến 100.")]
        public int DisplayOrder { get; set; }
    }
}
