Write-Host "==============================================="
Write-Host " AHM AUDIT SYSTEM - MEGA BUILDER"
Write-Host " Criar Auditorias + Autenticação + Dashboard"
Write-Host "==============================================="
Write-Host ""

$projectPath = "C:\AHM.Audit"

if (!(Test-Path $projectPath)) {
    Write-Host "ERRO: O projeto não existe em C:\AHM.Audit"
    exit
}

# ---------------------------------------------------------
# 1. Criar Modelo Auditoria
# ---------------------------------------------------------
Write-Host "A criar modelo Auditoria..."

@'
using System;

namespace AHM.Audit.Models
{
    public class Auditoria
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataConclusao { get; set; }
        public string Estado { get; set; }
        public string Responsavel { get; set; }
        public string Departamento { get; set; }
        public int Pontuacao { get; set; }
        public string Observacoes { get; set; }
    }
}
'@ | Set-Content "$projectPath\Models\Auditoria.cs"

# ---------------------------------------------------------
# 2. Atualizar AuditDbContext
# ---------------------------------------------------------
Write-Host "A atualizar AuditDbContext..."

@'
using Microsoft.EntityFrameworkCore;
using AHM.Audit.Models;

namespace AHM.Audit.Data
{
    public class AuditDbContext : DbContext
    {
        public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) {}

        public DbSet<User> Users { get; set; }
        public DbSet<Auditoria> Auditorias { get; set; }
    }
}
'@ | Set-Content "$projectPath\Data\AuditDbContext.cs"

# ---------------------------------------------------------
# 3. Criar páginas Razor: Lista, Criar, Editar
# ---------------------------------------------------------
Write-Host "A criar páginas de Auditorias..."

New-Item -ItemType Directory -Path "$projectPath\Pages\Auditorias" -Force | Out-Null

# LISTA
@'
@page
@model AHM.Audit.Pages.Auditorias.IndexModel
@{
    ViewData["Title"] = "Lista de Auditorias";
}

<h2>Auditorias</h2>

<a class="btn btn-primary mb-3" href="/Auditorias/Create">Nova Auditoria</a>

<table class="table table-striped">
    <thead>
        <tr>
            <th>Título</th>
            <th>Estado</th>
            <th>Responsável</th>
            <th>Departamento</th>
            <th>Data</th>
            <th></th>
        </tr>
    </thead>
    <tbody>
@foreach (var item in Model.Auditorias)
{
        <tr>
            <td>@item.Titulo</td>
            <td>@item.Estado</td>
            <td>@item.Responsavel</td>
            <td>@item.Departamento</td>
            <td>@item.DataCriacao.ToShortDateString()</td>
            <td>
                <a class="btn btn-sm btn-warning" href="/Auditorias/Edit?id=@item.Id">Editar</a>
            </td>
        </tr>
}
    </tbody>
</table>
'@ | Set-Content "$projectPath\Pages\Auditorias\Index.cshtml"

# LISTA - CODE BEHIND
@'
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AHM.Audit.Data;
using AHM.Audit.Models;
using Microsoft.AspNetCore.Mvc;

namespace AHM.Audit.Pages.Auditorias
{
    public class IndexModel : PageModel
    {
        private readonly AuditDbContext _context;

        public IndexModel(AuditDbContext context)
        {
            _context = context;
        }

        public List<Auditoria> Auditorias { get; set; }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("User") == null)
                return RedirectToPage("/Account/Login");

            Auditorias = _context.Auditorias.ToList();
            return Page();
        }
    }
}
'@ | Set-Content "$projectPath\Pages\Auditorias\Index.cshtml.cs"

# CREATE
@'
@page
@model AHM.Audit.Pages.Auditorias.CreateModel
@{
    ViewData["Title"] = "Nova Auditoria";
}

<h2>Nova Auditoria</h2>

<form method="post">
    <div class="mb-3">
        <label>Título</label>
        <input class="form-control" asp-for="Auditoria.Titulo" />
    </div>

    <div class="mb-3">
        <label>Descrição</label>
        <textarea class="form-control" asp-for="Auditoria.Descricao"></textarea>
    </div>

    <div class="mb-3">
        <label>Responsável</label>
        <input class="form-control" asp-for="Auditoria.Responsavel" />
    </div>

    <div class="mb-3">
        <label>Departamento</label>
        <input class="form-control" asp-for="Auditoria.Departamento" />
    </div>

    <div class="mb-3">
        <label>Estado</label>
        <select class="form-control" asp-for="Auditoria.Estado">
            <option>Pendente</option>
            <option>Em curso</option>
            <option>Concluída</option>
        </select>
    </div>

    <button class="btn btn-success">Guardar</button>
</form>
'@ | Set-Content "$projectPath\Pages\Auditorias\Create.cshtml"

# CREATE - CODE BEHIND
@'
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using AHM.Audit.Models;

namespace AHM.Audit.Pages.Auditorias
{
    public class CreateModel : PageModel
    {
        private readonly AuditDbContext _context;

        public CreateModel(AuditDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Auditoria Auditoria { get; set; }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("User") == null)
                return RedirectToPage("/Account/Login");

            return Page();
        }

        public IActionResult OnPost()
        {
            Auditoria.DataCriacao = DateTime.Now;
            _context.Auditorias.Add(Auditoria);
            _context.SaveChanges();
            return RedirectToPage("Index");
        }
    }
}
'@ | Set-Content "$projectPath\Pages\Auditorias\Create.cshtml.cs"

# EDIT
@'
@page
@model AHM.Audit.Pages.Auditorias.EditModel
@{
    ViewData["Title"] = "Editar Auditoria";
}

<h2>Editar Auditoria</h2>

<form method="post">
    <input type="hidden" asp-for="Auditoria.Id" />

    <div class="mb-3">
        <label>Título</label>
        <input class="form-control" asp-for="Auditoria.Titulo" />
    </div>

    <div class="mb-3">
        <label>Descrição</label>
        <textarea class="form-control" asp-for="Auditoria.Descricao"></textarea>
    </div>

    <div class="mb-3">
        <label>Estado</label>
        <select class="form-control" asp-for="Auditoria.Estado">
            <option>Pendente</option>
            <option>Em curso</option>
            <option>Concluída</option>
        </select>
    </div>

    <button class="btn btn-primary">Guardar</button>
</form>
'@ | Set-Content "$projectPath\Pages\Auditorias\Edit.cshtml"

# EDIT - CODE BEHIND
@'
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using AHM.Audit.Models;

namespace AHM.Audit.Pages.Auditorias
{
    public class EditModel : PageModel
    {
        private readonly AuditDbContext _context;

        public EditModel(AuditDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Auditoria Auditoria { get; set; }

        public IActionResult OnGet(int id)
        {
            if (HttpContext.Session.GetString("User") == null)
                return RedirectToPage("/Account/Login");

            Auditoria = _context.Auditorias.Find(id);
            return Page();
        }

        public IActionResult OnPost()
        {
            _context.Auditorias.Update(Auditoria);
            _context.SaveChanges();
            return RedirectToPage("Index");
        }
    }
}
'@ | Set-Content "$projectPath\Pages\Auditorias\Edit.cshtml.cs"

# ---------------------------------------------------------
# 4. Autenticação com sessão real
# ---------------------------------------------------------
Write-Host "A atualizar Login para usar sessão..."

@'
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using System.Linq;

namespace AHM.Audit.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly AuditDbContext _context;

        public LoginModel(AuditDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public void OnGet() {}

        public IActionResult OnPost()
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Username == Username && u.PasswordHash == Password);

            if (user == null)
            {
                ViewData["Error"] = "Credenciais inválidas";
                return Page();
            }

            HttpContext.Session.SetString("User", user.Username);
            return RedirectToPage("/Index");
        }
    }
}
'@ | Set-Content "$projectPath\Pages\Account\Login.cshtml.cs"

# ---------------------------------------------------------
# 5. Dashboard com estatísticas reais
# ---------------------------------------------------------
Write-Host "A atualizar Dashboard..."

@'
@page
@model IndexModel
@{
    ViewData["Title"] = "Dashboard";
}

<h2>Dashboard</h2>

<div class="row mt-4">
    <div class="col-md-4">
        <div class="card shadow-sm">
            <div class="card-body">
                <h5>Total de Auditorias</h5>
                <p class="display-6">@Model.Total</p>
            </div>
        </div>
    </div>

    <div class="col-md-4">
        <div class="card shadow-sm">
            <div class="card-body">
                <h5>Concluídas</h5>
                <p class="display-6">@Model.Concluidas</p>
            </div>
        </div>
    </div>

    <div class="col-md-4">
        <div class="card shadow-sm">
            <div class="card-body">
                <h5>Pendentes</h5>
                <p class="display-6">@Model.Pendentes</p>
            </div>
        </div>
    </div>
</div>
'@ | Set-Content "$projectPath\Pages\Index.cshtml"

# Dashboard code-behind
@'
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

public class IndexModel : PageModel
{
    private readonly AuditDbContext _context;

    public IndexModel(AuditDbContext context)
    {
        _context = context;
    }

    public int Total { get; set; }
    public int Concluidas { get; set; }
    public int Pendentes { get; set; }

    public IActionResult OnGet()
    {
        if (HttpContext.Session.GetString("User") == null)
            return RedirectToPage("/Account/Login");

        Total = _context.Auditorias.Count();
        Concluidas = _context.Auditorias.Count(a => a.Estado == "Concluída");
        Pendentes = _context.Auditorias.Count(a => a.Estado == "Pendente");

        return Page();
    }
}
'@ | Set-Content "$projectPath\Pages\Index.cshtml.cs"

# ---------------------------------------------------------
# 6. Criar migrations e atualizar BD
# ---------------------------------------------------------
Write-Host "A criar migrations..."
cd $projectPath
dotnet ef migrations add CreateAuditorias --project "$projectPath\AHM.Audit.csproj"

Write-Host "A atualizar base de dados..."
dotnet ef database update --project "$projectPath\AHM.Audit.csproj"

Write-Host "==============================================="
Write-Host " MODULO COMPLETO CRIADO COM SUCESSO!"
Write-Host "==============================================="