using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AHM.Audit.Data;
using AHM.Audit.Models;

namespace AHM.Audit.Pages.Admin
{
    public class PersonsModel : PageModel
    {
        private readonly AuditDbContext _context;
        public PersonsModel(AuditDbContext context) { _context = context; }

        public List<Person> Agents { get; set; } = new();
        public List<Person> Officers { get; set; } = new();
        public string Message { get; set; } = "";

        public IActionResult OnGet()
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            LoadLists();
            return Page();
        }

        public IActionResult OnPostAdd(string name, string role)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            _context.Persons.Add(new Person { Name = name, Role = role, Active = true });
            _context.SaveChanges();
            Message = $"'{name}' adicionado.";
            LoadLists();
            return Page();
        }

        public IActionResult OnPostToggle(int personId)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            var p = _context.Persons.Find(personId);
            if (p != null) { p.Active = !p.Active; _context.SaveChanges(); }
            LoadLists();
            return Page();
        }

        public IActionResult OnPostEditName(int personId, string newName)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            var p = _context.Persons.Find(personId);
            if (p != null && !string.IsNullOrWhiteSpace(newName))
            {
                var oldName = p.Name;
                var trimmedNewName = newName.Trim();

                if (oldName != trimmedNewName)
                {
                    // Atualiza em cascata as auditorias (e arquivo) que referenciam este agent/officer
                    // pelo nome em texto, para não "partir" o histórico em dois nomes diferentes
                    // nos gráficos e filtros do dashboard.
                    int updatedCount = 0;
                    if (p.Role == "Agent")
                    {
                        updatedCount += _context.Auditorias.Where(a => a.Agent == oldName)
                            .ExecuteUpdate(s => s.SetProperty(a => a.Agent, trimmedNewName));
                        _context.AuditoriaArchives.Where(a => a.Agent == oldName)
                            .ExecuteUpdate(s => s.SetProperty(a => a.Agent, trimmedNewName));
                    }
                    else if (p.Role == "Officer")
                    {
                        updatedCount += _context.Auditorias.Where(a => a.AhmOfficer == oldName)
                            .ExecuteUpdate(s => s.SetProperty(a => a.AhmOfficer, trimmedNewName));
                        _context.AuditoriaArchives.Where(a => a.AhmOfficer == oldName)
                            .ExecuteUpdate(s => s.SetProperty(a => a.AhmOfficer, trimmedNewName));
                    }

                    p.Name = trimmedNewName;
                    _context.SaveChanges();
                    Message = $"Nome atualizado para '{p.Name}' ({updatedCount} auditoria(s) atualizada(s) com o novo nome).";
                }
            }
            LoadLists();
            return Page();
        }

        private void LoadLists()
        {
            Agents   = _context.Persons.Where(p => p.Role == "Agent").OrderBy(p => p.Name).ToList();
            Officers = _context.Persons.Where(p => p.Role == "Officer").OrderBy(p => p.Name).ToList();
        }

        private bool IsAdmin()
        {
            var username = HttpContext.Session.GetString("User");
            if (username == null) return false;
            return _context.Users.Any(u => u.Username == username && u.IsAdmin);
        }
    }
}
