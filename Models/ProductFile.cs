using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppApi.Models
{
    [Table("ProductFile")]
    public class ProductFile
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Column("FIDProduct")]
        public int FIDProduct { get; set; }

        [Column("FIDFile")]
        public int FIDFile { get; set; }

        [ForeignKey(nameof(FIDProduct))]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey(nameof(FIDFile))]
        public virtual FileEntity File { get; set; } = null!;
    }
}
