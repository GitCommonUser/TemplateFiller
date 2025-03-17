using Microsoft.EntityFrameworkCore;
using TemplateFiller.Models;

namespace TemplateFiller.Data
{
    public class TemplateFillerContext : DbContext
    {
        public DbSet<Log> Logs {get; set;}
        public DbSet<Template> Templates { get; set; }
        public DbSet<Document> Documents { get; set; }
        protected override void OnConfiguring (DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=database.db");
        }
    }
}