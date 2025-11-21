using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppApi.Models
{
    [Table("Brand")]
    public class Brand
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Column("Code")]
        [MaxLength(50)]
        [Required]
        public string Code { get; set; } = null!;

        [Column("Description")]
        [MaxLength(250)]
        [Required]
        public string Description { get; set; } = null!;

        // navigation
        public virtual ICollection<Collection> Collections { get; set; } = new List<Collection>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
