using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppApi.Data;
using WebAppApi.Models;

namespace WebAppApi.Controllers
{
   [ApiController]
   [Route("api/[controller]")]
   public class ProductsController : ControllerBase
   {
      private readonly AppDbContext _db;
      public ProductsController(AppDbContext db) => _db = db;

      // DTO per creare/associare file
      public record FileUpsertDto(string? FileName, string? AbsolutePath, int? FileId);

      // -------------------------
      // CRUD di base (Get, Post, Put)
      // -------------------------
      [HttpGet]
      public async Task<IActionResult> GetAll()
      {
         var products = await _db.Product
                                 .Include(p => p.Brand)
                                 .Include(p => p.Collection)
                                 .AsNoTracking()
                                 .ToListAsync();
         return Ok(products);
      }

      [HttpGet("{id:int}")]
      public async Task<IActionResult> GetById(int id)
      {
         var product = await _db.Product
                                .Include(p => p.Brand)
                                .Include(p => p.Collection)
                                .Include(p => p.ProductFiles)
                                  .ThenInclude(pf => pf.File)
                                .AsNoTracking()
                                .FirstOrDefaultAsync(p => p.Id == id);

         if (product == null) return NotFound();
         return Ok(product);
      }

      [HttpPost]
      public async Task<IActionResult> Create([FromBody] Product input)
      {
         if (!ModelState.IsValid) return BadRequest(ModelState);

         if (await _db.Product.AnyAsync(p => p.Code == input.Code))
            return Conflict(new { message = "Codice prodotto già esistente." });

         if (input.FIDBrand.HasValue && !await _db.Brand.AnyAsync(b => b.Id == input.FIDBrand.Value))
            return BadRequest(new { message = "Brand non trovato." });

         if (input.FIDCollection.HasValue && !await _db.Collection.AnyAsync(c => c.Id == input.FIDCollection.Value))
            return BadRequest(new { message = "Collection non trovata." });

         // assicurati di non inserire navigation populate
         input.Brand = null;
         input.Collection = null;
         input.ProductFiles = new List<ProductFile>();

         _db.Product.Add(input);
         await _db.SaveChangesAsync();

         return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
      }

      [HttpPut("{id:int}")]
      public async Task<IActionResult> Update(int id, [FromBody] Product input)
      {
         if (id != input.Id) return BadRequest("Id mismatch");
         if (!ModelState.IsValid) return BadRequest(ModelState);

         var existing = await _db.Product.FindAsync(id);
         if (existing == null) return NotFound();

         if (!string.Equals(existing.Code, input.Code, StringComparison.OrdinalIgnoreCase))
         {
            if (await _db.Product.AnyAsync(p => p.Code == input.Code && p.Id != id))
               return Conflict(new { message = "Codice prodotto già usato da altro prodotto." });
         }

         if (input.FIDBrand.HasValue && !await _db.Brand.AnyAsync(b => b.Id == input.FIDBrand.Value))
            return BadRequest(new { message = "Brand non trovato." });

         if (input.FIDCollection.HasValue && !await _db.Collection.AnyAsync(c => c.Id == input.FIDCollection.Value))
            return BadRequest(new { message = "Collection non trovato." });

         // update dei campi consentiti
         existing.Code = input.Code;
         existing.Description = input.Description;
         existing.ExtendedDescription = input.ExtendedDescription;
         existing.FIDBrand = input.FIDBrand;
         existing.FIDCollection = input.FIDCollection;

         _db.Product.Update(existing);
         await _db.SaveChangesAsync();

         return NoContent();
      }

      // -------------------------
      // DELETE prodotto
      // -------------------------
      [HttpDelete("{id:int}")]
      public async Task<IActionResult> Delete(int id)
      {
         var product = await _db.Product
                                .Include(p => p.ProductFiles) // include per eventualmente rimuovere link
                                .FirstOrDefaultAsync(p => p.Id == id);

         if (product == null) return NotFound();

         // Rimuovi: il mapping DB (FK ON DELETE CASCADE) dovrebbe eliminare ProductFile,
         // ma includiamo comunque per chiarezza (EF risolve comunque).
         _db.Product.Remove(product);
         await _db.SaveChangesAsync();

         return NoContent();
      }

      // -------------------------
      // FILE MANAGEMENT
      // -------------------------
      // GET files collegati a prodotto
      [HttpGet("{id:int}/files")]
      public async Task<IActionResult> GetFilesForProduct(int id)
      {
         var exists = await _db.Product.AnyAsync(p => p.Id == id);
         if (!exists) return NotFound();

         var files = await _db.ProductFile
                              .Where(pf => pf.FIDProduct == id)
                              .Include(pf => pf.File)
                              .Select(pf => new
                              {
                                 pf.FIDFile,
                                 pf.Id,
                                 FileId = pf.FIDFile,
                                 FileName = pf.File.FileName,
                                 AbsolutePath = pf.File.AbsolutePath
                              })
                              .ToListAsync();

         return Ok(files);
      }

      // POST: aggiungi uno o più file ad un prodotto
      // Accetta array di FileUpsertDto oppure singolo oggetto
      [HttpPost("{id:int}/files")]
      public async Task<IActionResult> AddFilesToProduct(int id, [FromBody] List<FileUpsertDto> filesDto)
      {
         if (!await _db.Product.AnyAsync(p => p.Id == id)) return NotFound(new { message = "Product not found" });

         // user request validation
         if (filesDto == null || filesDto.Count == 0) return BadRequest(new { message = "Nessun file fornito" });

         var added = new List<object>();

         foreach (var dto in filesDto)
         {
            FileEntity fileEntity = null!;

            if (dto.FileId.HasValue)
            {
               fileEntity = await _db.File.FirstOrDefaultAsync(f => f.Id == dto.FileId.Value);
               if (fileEntity == null)
               {
                  // ignoriamo o segnaliamo? qui rispondiamo BadRequest
                  return BadRequest(new { message = $"FileId {dto.FileId.Value} non trovato" });
               }
            }
            else
            {
               if (string.IsNullOrWhiteSpace(dto.AbsolutePath))
                  return BadRequest(new { message = "AbsolutePath richiesto quando FileId non è fornito" });

               // cerca per AbsolutePath (unique key in DB)
               fileEntity = await _db.File.FirstOrDefaultAsync(f => f.AbsolutePath == dto.AbsolutePath);
               if (fileEntity == null)
               {
                  // crea nuovo file record
                  fileEntity = new FileEntity
                  {
                     FileName = dto.FileName ?? System.IO.Path.GetFileName(dto.AbsolutePath),
                     AbsolutePath = dto.AbsolutePath
                  };
                  _db.File.Add(fileEntity);
                  await _db.SaveChangesAsync(); // salvo qui per ottenere Id prima di creare ProductFile
               }
            }

            // se associazione già esiste, salta
            var existsLink = await _db.ProductFile.AnyAsync(pf => pf.FIDProduct == id && pf.FIDFile == fileEntity.Id);
            if (existsLink)
            {
               added.Add(new { fileEntity.Id, fileEntity.FileName, fileEntity.AbsolutePath, linked = false, reason = "already linked" });
               continue;
            }

            var productFile = new ProductFile
            {
               FIDProduct = id,
               FIDFile = fileEntity.Id
            };
            _db.ProductFile.Add(productFile);
            await _db.SaveChangesAsync();

            added.Add(new { fileEntity.Id, fileEntity.FileName, fileEntity.AbsolutePath, linked = true });
         }

         return Ok(new { added });
      }

      // DELETE: rimuove la singola associazione file <-> product
      // query param opcionale: removeFile=true -> cancella anche il record File se non è più referenziato altrove
      [HttpDelete("{productId:int}/files/{fileId:int}")]
      public async Task<IActionResult> RemoveFileFromProduct(int productId, int fileId, [FromQuery] bool removeFile = false)
      {
         var link = await _db.ProductFile.FirstOrDefaultAsync(pf => pf.FIDProduct == productId && pf.FIDFile == fileId);
         if (link == null) return NotFound(new { message = "Link prodotto-file non trovato." });

         _db.ProductFile.Remove(link);
         await _db.SaveChangesAsync();

         if (removeFile)
         {
            // se non ci sono più collegamenti a questo file, cancellalo
            var stillLinked = await _db.ProductFile.AnyAsync(pf => pf.FIDFile == fileId);
            if (!stillLinked)
            {
               var fileEntity = await _db.File.FindAsync(fileId);
               if (fileEntity != null)
               {
                  _db.File.Remove(fileEntity);
                  await _db.SaveChangesAsync();

                  // NOTA: qui non togliamo fisicamente il file dal filesystem.
                  // Se vuoi eliminare il file fisico, fallo con attenzione (permessi, path validation).
               }
            }
         }

         return NoContent();
      }

      // OPTIONAL: endpoint per rimuovere molteplici file da prodotto (bulk)
      [HttpDelete("{productId:int}/files")]
      public async Task<IActionResult> RemoveMultipleFilesFromProduct(int productId, [FromBody] List<int> fileIds, [FromQuery] bool removeOrphanFiles = false)
      {
         if (fileIds == null || fileIds.Count == 0) return BadRequest(new { message = "Nessun fileId fornito" });

         var links = await _db.ProductFile.Where(pf => pf.FIDProduct == productId && fileIds.Contains(pf.FIDFile)).ToListAsync();
         if (links.Count == 0) return NotFound(new { message = "Nessun link trovato per i file forniti" });

         _db.ProductFile.RemoveRange(links);
         await _db.SaveChangesAsync();

         if (removeOrphanFiles)
         {
            foreach (var fid in fileIds)
            {
               var stillLinked = await _db.ProductFile.AnyAsync(pf => pf.FIDFile == fid);
               if (!stillLinked)
               {
                  var fileEntity = await _db.File.FindAsync(fid);
                  if (fileEntity != null)
                  {
                     _db.File.Remove(fileEntity);
                  }
               }
            }
            await _db.SaveChangesAsync();
         }

         return NoContent();
      }
   }
}
