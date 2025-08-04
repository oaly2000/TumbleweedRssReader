using Microsoft.EntityFrameworkCore;
using Tumbleweed.Data.Models;

namespace Tumbleweed.Data;

public class AppDbContext : DbContext
{
    public DbSet<Feed> Feeds => Set<Feed>();
    public DbSet<Episode> Episodes => Set<Episode>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var dbPath = Path.Combine(userDocumentsFolderPath, "TumbleweedRssReader.db");
        var connectionString = $"Data Source={dbPath}";
        Console.WriteLine($"Using database at {dbPath}");
        optionsBuilder.UseSqlite(connectionString);
        base.OnConfiguring(optionsBuilder);
    }
}
