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

            if (string.IsNullOrWhiteSpace(name))
            {
                Message = "O nome não pode estar vazio.";
                LoadLists();
                return Page();
            }

            var trimmedName = name.Trim();
            // Compara ignorando maiúsculas/minúsculas E acentos (ex.: "Joao" == "João"),
            // para não voltar a criar duplicados por pequenas diferenças de escrita.
            var isDuplicate = _context.Persons
                .Where(p => p.Role == role)
                .AsEnumerable()
                .Any(p => NormalizeName(p.Name) == NormalizeName(trimmedName));

            if (isDuplicate)
            {
                Message = $"Já existe um(a) {(role == "Agent" ? "agent" : "officer")} com um nome igual ou muito parecido a '{trimmedName}'. Verifica a lista antes de adicionar (pode ser um acento a mais/menos).";
                LoadLists();
                return Page();
            }

            _context.Persons.Add(new Person { Name = trimmedName, Role = role, Active = true });
            _context.SaveChanges();
            Message = $"'{trimmedName}' adicionado.";
            LoadLists();
            return Page();
        }

        // Remove acentos e normaliza para comparação de nomes tolerante a diferenças de escrita.
        private static string NormalizeName(string name)
        {
            var normalized = name.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var c in normalized)
            {
                var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString();
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

        public IActionResult OnPostDelete(int personId)
        {
            if (!IsAdmin()) return RedirectToPage("/Account/Login");
            ViewData["IsAdmin"] = true;
            var p = _context.Persons.Find(personId);
            if (p != null)
            {
                // Uma pessoa ligada a uma conta de login (User.PersonId) é usada para preencher
                // automaticamente o "Agent" quando essa conta cria uma auditoria. Apagar sem
                // desligar primeiro deixava isso em branco silenciosamente, sem qualquer aviso.
                var linkedUsers = _context.Users.Where(u => u.PersonId == personId).Select(u => u.Username).ToList();
                if (linkedUsers.Any())
                {
                    Message = $"Não é possível apagar '{p.Name}' — está ligado à conta de login de {string.Join(", ", linkedUsers)}. "
                        + "Vai a Admin > Utilizadores e muda o agent associado a essa conta antes de apagar.";
                    LoadLists();
                    return Page();
                }

                var name = p.Name;
                var role = p.Role;
                _context.Persons.Remove(p);
                _context.SaveChanges();

                // O nome fica gravado como texto simples nas auditorias existentes (não é uma
                // referência), por isso apagar a pessoa não altera nada no histórico — só deixa
                // de aparecer nos dropdowns/listas para novas auditorias.
                var usedInAudits = role == "Agent"
                    ? _context.Auditorias.Count(a => a.Agent == name)
                    : _context.Auditorias.Count(a => a.AhmOfficer == name);

                Message = $"'{name}' apagado.";
                if (usedInAudits > 0)
                    Message += $" Nota: continua a aparecer em {usedInAudits} auditoria(s) já existente(s) (o histórico não é alterado).";
            }
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
