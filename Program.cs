using Microsoft.EntityFrameworkCore;
using AHM.Audit.Data;
using AHM.Audit.Models;
using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using BCrypt.Net;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new System.IO.DirectoryInfo("/tmp/dataprotection-keys"));

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    Uri uri;
    try
    {
        uri = new Uri(databaseUrl.Trim().Trim('"'));
    }
    catch (UriFormatException ex)
    {
        throw new InvalidOperationException(
            $"DATABASE_URL não é um URI válido. Valor lido: \"{databaseUrl}\". " +
            "Formato esperado: postgresql://utilizador:password@host:porta/basededados", ex);
    }

    var port = uri.Port > 0 ? uri.Port : 5432; // Uri.Port devolve -1 se a porta não estiver explícita no URI
    var userInfo = uri.UserInfo.Split(':');
    if (userInfo.Length < 2)
        throw new InvalidOperationException(
            $"DATABASE_URL não contém utilizador/password. Valor lido: \"{databaseUrl}\".");

    var isLocal = uri.Host == "localhost" || uri.Host == "127.0.0.1";
    var sslMode = isLocal ? "Disable" : "Require";
    var npgsqlConn = $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode={sslMode};Trust Server Certificate=true";

    builder.Services.AddDbContext<AuditDbContext>(options =>
        options.UseNpgsql(npgsqlConn));
}
else
{
    builder.Services.AddDbContext<AuditDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
}

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(5),
                PermitLimit = 10,
                QueueLimit = 0
            }));
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Falha imediatamente no arranque se alguém adicionar/remover/renomear um gráfico do
// dashboard (Models/User.cs) e se esquecer de atualizar o catálogo correspondente
// (Models/DashboardPermissionCatalog.cs) e o modal em Pages/Admin/Users.cshtml.
AHM.Audit.Models.DashboardPermissionCatalog.Validate();

app.UseMiddleware<AHM.Audit.Middleware.ExceptionMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseMiddleware<AHM.Audit.Middleware.AuthMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();
app.MapRazorPages();

// ── Endpoint de auto-save (AJAX) ──
app.MapPost("/api/autosave", async (HttpContext ctx, AuditDbContext db) =>
{
    var username = ctx.Session.GetString("User");
    if (username == null) return Results.Unauthorized();

    var form = await ctx.Request.ReadFormAsync();
    var idStr = form["id"].ToString();
    var field = form["field"].ToString();
    var value = form["value"].ToString();
    var reason = form["reason"].ToString();

    if (string.IsNullOrEmpty(field)) return Results.BadRequest("Campo inválido");

    Auditoria audit;
    if (string.IsNullOrEmpty(idStr) || idStr == "0")
    {
        // Nova auditoria em rascunho
        audit = new Auditoria { CreatedAt = DateTime.Now, IsDraft = true, Date = DateTime.Today };

        // Preencher Agent automaticamente
        var user = db.Users.FirstOrDefault(u => u.Username == username);
        if (user?.PersonId != null)
        {
            var person = db.Persons.Find(user.PersonId);
            if (person != null) audit.Agent = person.Name;
        }

        db.Auditorias.Add(audit);
        db.SaveChanges();
    }
    else
    {
        var id = int.Parse(idStr);
        audit = db.Auditorias.Find(id);
        if (audit == null) return Results.NotFound();
        if (audit.IsFinalized)
        {
            var isAdmin = db.Users.Any(u => u.Username == username && u.IsAdmin);
            if (!isAdmin) return Results.Forbid();
        }
    }

    // Whitelist de campos permitidos na checklist
    var allowedChecklistFields = new HashSet<string> {
        "B1","B2","B3","C1","C2","C2_3","C3","C4_TakeOff","C4_ZeroFuel",
        "C4_Landing","C4_Inflight","C4_IdealTrim","C5","C7_1","D1","D2",
        "D3","D5_1","D5_2","D6_2","E1_DOW","E1_MRW","E1_MTOW","E1_MZFW",
        "E1_MLAW","E2_1","E2_2","E3_1","G1","RevisionUpdate","LIR","LS","DatabasePrintout"
    };
    if (!allowedChecklistFields.Contains(field)) return Results.BadRequest("Campo não permitido");

    var allowedValues = new HashSet<string> { "YES", "NO", "N/A" };
    if (!allowedValues.Contains(value)) return Results.BadRequest("Valor inválido");

    var prop = typeof(Auditoria).GetProperty(field);
    if (prop == null || prop.PropertyType != typeof(string)) return Results.BadRequest("Campo desconhecido");
    prop.SetValue(audit, value);

    // Guardar razão para NO
    if (value == "NO" && !string.IsNullOrEmpty(reason))
    {
        var reasons = string.IsNullOrEmpty(audit.NoReasons)
            ? new Dictionary<string, string>()
            : audit.NoReasons.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('='))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0], p => p[1]);
        reasons[field] = reason;
        audit.NoReasons = string.Join(";", reasons.Select(kv => $"{kv.Key}={kv.Value}"));
    }
    else if (value != "NO")
    {
        // Limpar razão se mudou de NO para outra opção
        if (!string.IsNullOrEmpty(audit.NoReasons))
        {
            var reasons = audit.NoReasons.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('='))
                .Where(p => p.Length == 2 && p[0] != field)
                .ToDictionary(p => p[0], p => p[1]);
            audit.NoReasons = string.Join(";", reasons.Select(kv => $"{kv.Key}={kv.Value}"));
        }
    }

    db.SaveChanges();
    return Results.Ok(new { id = audit.Id });
});

// ── Endpoint de auto-save para campos gerais (não-checklist) ──
app.MapPost("/api/autosave-field", async (HttpContext ctx, AuditDbContext db) =>
{
    var username = ctx.Session.GetString("User");
    if (username == null) return Results.Unauthorized();

    var form = await ctx.Request.ReadFormAsync();
    var idStr = form["id"].ToString();
    var field = form["field"].ToString();
    var value = form["value"].ToString();

    if (string.IsNullOrEmpty(idStr) || idStr == "0" || string.IsNullOrEmpty(field))
        return Results.BadRequest();

    var id = int.Parse(idStr);
    var audit = db.Auditorias.Find(id);
    if (audit == null) return Results.NotFound();
    if (audit.IsFinalized)
    {
        var isAdmin = db.Users.Any(u => u.Username == username && u.IsAdmin);
        if (!isAdmin) return Results.Forbid();
    }

    var allowedFields = new[] { "Ticket", "Airline", "Aircraft", "Registration", "Date", "RevisionUpdates",
        "AhmOfficer", "CorrectionTicket", "ReasonForRecertification", "CorrectionsMade", "AircraftRecertified", "Notes" };
    if (!allowedFields.Contains(field)) return Results.BadRequest("Campo não permitido");

    var prop = typeof(Auditoria).GetProperty(field);
    if (prop == null) return Results.BadRequest();

    if (field == "Date" && DateTime.TryParse(value, out var dt))
        prop.SetValue(audit, dt);
    else if (prop.PropertyType == typeof(string))
        prop.SetValue(audit, value);

    db.SaveChanges();
    return Results.Ok();
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    db.Database.Migrate();

    // Seed admin
    if (!db.Users.Any())
    {
        db.Users.Add(new User { Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("AHM123%%"), IsAdmin = true, Active = true,
            CanViewDashboard = true, CanViewSectionChart = true, CanViewNonConformities = true, CanViewGlobalConformity = true });
        db.SaveChanges();
    }

    // Seed Agents
    var defaultAgents = new[] { "Alejandro Alvarado", "Joao Viriato", "Luis Cavaco", "Pedro Jesus", "Pedro Nunes", "Renato Silva", "Tiago Malheiro", "Valter Augusto" };
    foreach (var name in defaultAgents)
        if (!db.Persons.Any(p => p.Name == name && p.Role == "Agent"))
            db.Persons.Add(new Person { Name = name, Role = "Agent", Active = true });

    // Seed Officers
    var defaultOfficers = new[] { "Alejandro Alvarado", "Ihor Holovchak", "Joao Viriato", "Luis Cavaco", "Pedro Jesus", "Renato Silva", "Tiago Malheiro", "Valter Augusto" };
    foreach (var name in defaultOfficers)
        if (!db.Persons.Any(p => p.Name == name && p.Role == "Officer"))
            db.Persons.Add(new Person { Name = name, Role = "Officer", Active = true });
    db.SaveChanges();

    // Seed Non-Conformity Reasons
    if (!db.NonConformityReasons.Any())
    {
        db.NonConformityReasons.Add(new NonConformityReason { Reason = "Information incomplete in AHM", Active = true });
        db.NonConformityReasons.Add(new NonConformityReason { Reason = "Agent Mistake", Active = true });
        db.SaveChanges();
    }

    // Seed Airlines (lista IATA/ICAO completa)
    var allAirlineCodes = new[] { "0B", "1I", "2B", "2L", "2S", "2U", "2W", "3E", "3F", "3H", "3L", "3N", "3O", "3P", "3U", "3V", "4D", "4G", "4M", "4O", "4S", "4U", "4V", "4X", "4Y", "5A", "5F", "5K", "5M", "5N", "5O", "5P", "5T", "5W", "5Y", "6A", "6E", "6G", "6H", "6I", "6K", "6O", "6S", "6W", "7B", "7C", "7K", "7R", "7W", "7Y", "8B", "8D", "8H", "8L", "8M", "8Q", "8R", "8U", "9L", "9P", "9S", "A2", "A3", "A4", "A5", "A9", "AA", "AB", "AC", "AEH", "AF", "AGR", "AH", "AHO", "AI", "AIA", "AL", "AME", "AMT", "AMV", "ANK", "AOA", "AP", "AQ", "ARN", "AT", "ATV", "AV", "AW", "AXY", "AY", "AZ", "AZD", "B0", "B2", "B3", "B4", "B5", "B8", "BA", "BAF", "BAY", "BBD", "BBK", "BC", "BG", "BH", "BHA", "BIX", "BJ", "BK", "BL", "BLE", "BLX", "BN", "BPS", "BQ", "BRO", "BS", "BT", "BTX", "BUC", "BUR", "BUY", "BVL", "BW", "BZ", "C3", "C6", "C9", "CA", "CAJ", "CAT", "CBX", "CD", "CE", "CEF", "CJ", "CL", "CM", "CMV", "COB", "CRL", "CS", "CTM", "CY", "CZ", "D0", "D3", "D4", "D7", "D8", "DAF", "DAK", "DE", "DI", "DK", "DN", "DNU", "DO", "DP", "DT", "DV", "DW", "DX", "DY", "E2", "E3", "E5", "E8", "E9", "EB", "ED", "EE", "EG", "EI", "EJU", "EL", "EN", "ENT", "EO", "ERS", "ET", "EU", "EUP", "EVJ", "EW", "EY", "EZS", "EZY", "EZZ", "F3", "F6", "F7", "F8", "FB", "FG", "FH", "FI", "FJK", "FJR", "FL", "FM", "FOR", "FPE", "FQ", "FR", "FRO", "FS", "FT", "FV", "FY", "FZ", "G2", "G5", "G6", "G9", "GAF", "GAV", "GCL", "GDM", "GF", "GH", "GKS", "GMS", "GQ", "GR", "GTR", "GUM", "GW", "GWR", "GX", "GXA", "H3", "H4", "H7", "H9", "HAT", "HB", "HC", "HF", "HFM", "HG", "HH", "HK", "HLR", "HM", "HN", "HO", "HP", "HQ", "HR", "HRN", "HU", "HV", "HY", "I2", "IA", "IB", "IF", "IG", "IO", "IT", "IV", "IX", "IY", "IZ", "J2", "J4", "J8", "J9", "JD", "JF", "JI", "JL", "JO", "JTD", "JU", "JUP", "JW", "JX", "JZ", "K6", "KA", "KC", "KE", "KF", "KHH", "KK", "KKK", "KL", "KLJ", "KM", "KR", "KS", "KU", "KW", "L6", "L9", "LA", "LG", "LH", "LIT", "LL", "LM", "LN", "LO", "LQ", "LS", "LV", "LX", "LXX", "LY", "LYX", "M2", "M4", "M9", "MDM", "ME", "MF", "MH", "MHV", "MI", "MJ", "MLM", "MLT", "MM", "MMF", "MN", "MR", "MS", "MT", "MTL", "MTX", "MU", "MUS", "MV", "MX", "MYX", "N0", "N4", "N5", "N8", "N9", "NB", "ND", "NE", "NFA", "NH", "NN", "NO", "NP", "NR", "NT", "NX", "O8", "OA", "OAE", "OB", "OG", "OJ", "OK", "OL", "OM", "ON", "ONS", "OR", "OS", "OTF", "OU", "OV", "OZ", "P6", "PB", "PC", "PE", "PK", "PN", "PNX", "PQ", "PS", "PU", "PY", "Q2", "Q9", "QA", "QC", "QF", "QH", "QR", "QS", "QU", "QV", "QW", "R3", "R5", "R6", "RA", "RC", "RF", "RFF", "RJ", "RL", "RM", "RO", "RQ", "RRR", "RT", "RU", "RW", "S4", "S7", "SA", "SCT", "SDR", "SE", "SEK", "SF", "SG", "SHS", "SI", "SID", "SK", "SL", "SLD", "SM", "SN", "SO", "SR", "SRN", "ST", "SU", "SUS", "SV", "SVB", "SZ", "SZS", "T3", "T5", "T7", "TB", "TD", "TE", "TF", "TG", "TJS", "TK", "TO", "TOM", "TP", "TU", "TV", "TW", "TWI", "TX", "TZ", "U5", "U6", "U8", "UA", "UB", "UF", "UG", "UH", "UJ", "UK", "UL", "UN", "UT", "UU", "UX", "UY", "UZ", "V3", "V7", "V8", "V9", "VBB", "VC", "VF", "VG", "VJ", "VK", "VLJ", "VN", "VO", "VR", "VS", "VU", "VY", "VZ", "W3", "W4", "W6", "W9", "WA", "WAL", "WE", "WF", "WI", "WK", "WM", "WT", "WY", "WZ", "X0B", "X2N", "X3", "X3E", "X5P", "X6Y", "X9", "X9C", "XA", "XAB", "XBM", "XC", "XCU", "XEZ", "XFQ", "XG", "XH", "XH3", "XHA", "XHE", "XJ", "XK", "XM", "XMI", "XMN", "XOE", "XP", "XQ", "XR", "XSE", "XVO", "XW", "XW5", "XWW", "XX5", "XY", "XZ", "Y3", "Y7", "YC", "YD", "YE", "YI", "YK", "YL", "YP", "YQ", "YU", "YW", "Z0", "Z6", "Z8", "Z9", "ZA", "ZB", "ZE", "ZF", "ZG", "ZM", "ZQ", "ZT", "ZU" };
    // Nota: este seed corre em TODOS os arranques da aplicação. Antes fazia uma query
    // (db.Airlines.Any(...)) por cada um dos ~400 códigos, o que gerava ~400 linhas de log
    // por deploy/restart e chegou a fazer o Railway descartar mensagens por exceder o limite
    // de 500 logs/seg. Agora carrega-se a lista de códigos existentes de uma só vez.
    var existingAirlineCodes = db.Airlines.Select(a => a.Code).ToHashSet();
    foreach (var code in allAirlineCodes)
    {
        if (!existingAirlineCodes.Contains(code))
            db.Airlines.Add(new Airline { Code = code, Name = "", Active = true });
    }
    db.SaveChanges();

    // Arquivo anual — só arquiva auditorias já FINALIZADAS de anos anteriores.
    // (Antes também arquivava auditorias não-draft mas ainda por finalizar, o que podia
    // "congelar" no arquivo, de forma irreversível, uma auditoria que ainda estava a ser
    // trabalhada só porque o ano civil mudou entretanto.)
    var currentYear = DateTime.Now.Year;
    var toArchive = db.Auditorias.Where(a => a.Date.Year < currentYear && !a.IsDraft && a.IsFinalized).ToList();

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
