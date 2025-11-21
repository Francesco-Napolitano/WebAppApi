using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppApi.Models
{
    [Table("Product")]
    public class Product
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

        [Column("ExtendedDescription")]
        [MaxLength(250)]
        public string? ExtendedDescription { get; set; }

        [Column("TSINS")]
        public DateTime TSINS { get; set; }

        [Column("FIDBrand")]
        public int? FIDBrand { get; set; }

        [Column("FIDCollection")]
        public int? FIDCollection { get; set; }

        // navigation properties - NOMI SEMPLICI per usare Include(p => p.Brand)
        [ForeignKey(nameof(FIDBrand))]
        public virtual Brand? Brand { get; set; }

        [ForeignKey(nameof(FIDCollection))]
        public virtual Collection? Collection { get; set; }

        public virtual ICollection<ProductFile> ProductFiles { get; set; } = new List<ProductFile>();
    }
}
