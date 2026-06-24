using Microsoft.EntityFrameworkCore;
using AHM.Audit.Data;
using AHM.Audit.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Usa PostgreSQL se DATABASE_URL existir (Railway), senão usa SQL Server (local)
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    // Railway PostgreSQL
    builder.Services.AddDbContext<AuditDbContext>(options =>
        options.UseNpgsql(databaseUrl));
}
else
{
    // SQL Server local
    builder.Services.AddDbContext<AuditDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
}

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    db.Database.Migrate();

    if (!db.Users.Any())
    {
        db.Users.Add(new User { Username = "admin", PasswordHash = "AHM123%%", IsAdmin = true, Active = true });
        db.SaveChanges();
    }

    var currentYear = DateTime.Now.Year;
    var toArchive = db.Auditorias.Where(a => a.Date.Year < currentYear).ToList();

    if (toArchive.Any())
    {
        foreach (var a in toArchive)
        {
            if (!db.AuditoriaArchives.Any(x => x.Ticket == a.Ticket && x.ArchiveYear == a.Date.Year))
            {
                var archive = new AuditoriaArchive { ArchiveYear = a.Date.Year };
                foreach (var prop in typeof(Auditoria).GetProperties())
                {
                    var archiveProp = typeof(AuditoriaArchive).GetProperty(prop.Name);
                    if (archiveProp != null && archiveProp.CanWrite && prop.Name != "Id")
                        archiveProp.SetValue(archive, prop.GetValue(a));
                }
                db.AuditoriaArchives.Add(archive);
            }
        }
        db.Auditorias.RemoveRange(toArchive);
        db.SaveChanges();
    }
}

app.Run("http://0.0.0.0:5000");
