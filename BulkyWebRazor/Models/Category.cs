using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BulkyWebRazor.Model
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(30)]
        [DisplayName("Name")]
        public string Name { get; set; }
        [Range(1,100)]
        [DisplayName("Display Order")]
        public int DisplayOrder { get; set; }
    }
}
