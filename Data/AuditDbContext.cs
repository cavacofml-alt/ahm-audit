using Microsoft.EntityFrameworkCore;
using AHM.Audit.Models;

namespace AHM.Audit.Data
{
    public class AuditDbContext : DbContext
    {
        public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) {}

        public DbSet<User> Users { get; set; }
        public DbSet<Auditoria> Auditorias { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<AuditoriaArchive> AuditoriaArchives { get; set; }
    }
}
