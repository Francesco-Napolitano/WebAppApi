using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppApi.Models
{
    [Table("File")]
    public class FileEntity
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Column("FileName")]
        [MaxLength(250)]
        [Required]
        public string FileName { get; set; } = null!;

        [Column("AbsolutePath")]
        [MaxLength(250)]
        [Required]
        public string AbsolutePath { get; set; } = null!;

        public virtual ICollection<ProductFile> ProductFiles { get; set; } = new List<ProductFile>();
    }
}
