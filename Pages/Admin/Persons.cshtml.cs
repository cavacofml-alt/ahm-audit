using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AHM.Audit.Data;
using AHM.Audit.Models;

namespace AHM.Audit.Pages.Admin
{
    [IgnoreAntiforgeryToken]
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
                p.Name = newName.Trim();
                _context.SaveChanges();
                Message = $"Nome atualizado para '{p.Name}'.";
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
