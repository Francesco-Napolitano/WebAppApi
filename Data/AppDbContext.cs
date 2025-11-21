using Microsoft.EntityFrameworkCore;
using WebAppApi.Models;

namespace WebAppApi.Data
{
   public class AppDbContext : DbContext
   {
      public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

      public virtual DbSet<Brand> Brand { get; set; } = null!;
      public virtual DbSet<Collection> Collection { get; set; } = null!;
      public virtual DbSet<Product> Product { get; set; } = null!;
      public virtual DbSet<FileEntity> File { get; set; } = null!;
      public virtual DbSet<ProductFile> ProductFile { get; set; } = null!;

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
         // Mappe esplicite per sicurezza (colonne/constraint già definite con attributi)
         modelBuilder.Entity<Brand>().ToTable("Brand");
         modelBuilder.Entity<Collection>().ToTable("Collection");
         modelBuilder.Entity<Product>().ToTable("Product");
         modelBuilder.Entity<FileEntity>().ToTable("File");
         modelBuilder.Entity<ProductFile>().ToTable("ProductFile");

         // Relation: Collection -> Brand
         modelBuilder.Entity<Collection>()
                     .HasOne(c => c.Brand)
                     .WithMany(b => b.Collections)
                     .HasForeignKey(c => c.FIDBrand)
                     .OnDelete(DeleteBehavior.Restrict)
                     .HasConstraintName("fk_collection_brand");

         // Relation: Product -> Brand (nullable, set null on delete)
         modelBuilder.Entity<Product>()
                     .HasOne(p => p.Brand)
                     .WithMany(b => b.Products)
                     .HasForeignKey(p => p.FIDBrand)
                     .OnDelete(DeleteBehavior.SetNull)
                     .HasConstraintName("fk_product_brand");

         // Relation: Product -> Collection (nullable, set null on delete)
         modelBuilder.Entity<Product>()
                     .HasOne(p => p.Collection)
                     .WithMany(c => c.Products)
                     .HasForeignKey(p => p.FIDCollection)
                     .OnDelete(DeleteBehavior.SetNull)
                     .HasConstraintName("fk_product_collection");

         // Relation: ProductFile -> Product
         modelBuilder.Entity<ProductFile>()
                     .HasOne(pf => pf.Product)
                     .WithMany(p => p.ProductFiles)
                     .HasForeignKey(pf => pf.FIDProduct)
                     .OnDelete(DeleteBehavior.Cascade)
                     .HasConstraintName("fk_productfile_product");

         // Relation: ProductFile -> FileEntity
         modelBuilder.Entity<ProductFile>()
                     .HasOne(pf => pf.File)
                     .WithMany(f => f.ProductFiles)
                     .HasForeignKey(pf => pf.FIDFile)
                     .OnDelete(DeleteBehavior.Cascade)
                     .HasConstraintName("fk_productfile_file");

         base.OnModelCreating(modelBuilder);
      }
   }
}
