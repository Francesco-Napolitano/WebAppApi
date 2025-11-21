using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppApi.Models
{
    [Table("Collection")]
    public class Collection
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Column("FIDBrand")]
        public int FIDBrand { get; set; }

        [Column("Code")]
        [MaxLength(50)]
        [Required]
        public string Code { get; set; } = null!;

        [Column("Description")]
        [MaxLength(250)]
        [Required]
        public string Description { get; set; } = null!;

        // navigation
        [ForeignKey(nameof(FIDBrand))]
        public virtual Brand Brand { get; set; } = null!;

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
