using Microsoft.EntityFrameworkCore;
using RfidLibraryApi.Models;

namespace RfidLibraryApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Patron> Patrons => Set<Patron>();
    public DbSet<Librarian> Librarians => Set<Librarian>();
    public DbSet<RfidTag> RfidTags => Set<RfidTag>();
    public DbSet<RfidReader> RfidReaders => Set<RfidReader>();
    public DbSet<LibraryTransaction> Transactions => Set<LibraryTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>()
            .HasMany(b => b.RfidTags)
            .WithOne(t => t.Book)
            .HasForeignKey(t => t.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LibraryTransaction>()
            .HasIndex(t => new { t.MemberId, t.BookId, t.Status });
    }
}
