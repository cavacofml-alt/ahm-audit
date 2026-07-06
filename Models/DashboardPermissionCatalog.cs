using System.Reflection;

namespace AHM.Audit.Models
{
    /// <summary>
    /// Lista única (fonte da verdade) dos gráficos do dashboard que podem ser
    /// ligados/desligados por utilizador em /Admin/Users ("Permissões de Dashboard").
    ///
    /// Sempre que um gráfico for adicionado, removido ou renomeado no dashboard
    /// (Pages/Index.cshtml + Pages/Index.cshtml.cs + Models/User.cs), atualizar
    /// também esta lista e o texto correspondente em Pages/Admin/Users.cshtml.
    ///
    /// A validação em Validate() garante que isto nunca fica esquecido: a aplicação
    /// não arranca se esta lista deixar de corresponder às propriedades CanView* do
    /// User.
    /// </summary>
    public static class DashboardPermissionCatalog
    {
        public record Entry(string PropertyName, string Label);

        // Nome da propriedade em User.cs  →  Texto mostrado no checkbox do modal de permissões.
        // Tem de corresponder exatamente ao data-widget de Pages/Index.cshtml.
        public static readonly Entry[] Entries = new[]
        {
            new Entry(nameof(User.CanViewTrend),            "Tendência 12 meses"),
            new Entry(nameof(User.CanViewGlobalConformity),  "Conformidade global"),
            new Entry(nameof(User.CanViewHeatmap),           "Checklist — detalhe por campo"),
            new Entry(nameof(User.CanViewNonConformities),   "Razões de NOT OK"),
            new Entry(nameof(User.CanViewAirlineChart),      "Por Airline"),
            new Entry(nameof(User.CanViewAgentChart),        "Por Agente"),
            new Entry(nameof(User.CanViewOfficerChart),      "Conformidade por Officer"),
            new Entry(nameof(User.CanViewComparativeChart),  "Comparativo anual"),
            new Entry(nameof(User.CanViewQuarterProgress),   "Progresso trimestral"),
        };

        /// <summary>
        /// Compara este catálogo com as propriedades bool "CanView*" de User.cs
        /// (excluindo CanViewDashboard, que é o interruptor principal, não um gráfico).
        /// Chamar no arranque da aplicação (ver Program.cs) para apanhar imediatamente
        /// qualquer gráfico novo/renomeado que tenha sido esquecido num dos dois lados.
        /// </summary>
        public static void Validate()
        {
            var userChartProperties = typeof(User)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(bool)
                    && p.Name.StartsWith("CanView", StringComparison.Ordinal)
                    && p.Name != nameof(User.CanViewDashboard))
                .Select(p => p.Name)
                .OrderBy(n => n)
                .ToList();

            var catalogProperties = Entries.Select(e => e.PropertyName).OrderBy(n => n).ToList();

            var missingFromCatalog = userChartProperties.Except(catalogProperties).ToList();
            var missingFromUser    = catalogProperties.Except(userChartProperties).ToList();

            if (missingFromCatalog.Count > 0 || missingFromUser.Count > 0)
            {
                var msg = "DashboardPermissionCatalog está dessincronizado com Models/User.cs.\n";
                if (missingFromCatalog.Count > 0)
                    msg += $"  - Propriedades em User.cs sem entrada no catálogo (faltam também em Pages/Admin/Users.cshtml): {string.Join(", ", missingFromCatalog)}\n";
                if (missingFromUser.Count > 0)
                    msg += $"  - Entradas no catálogo sem propriedade correspondente em User.cs: {string.Join(", ", missingFromUser)}\n";
                msg += "Atualiza DashboardPermissionCatalog.Entries e o modal em Pages/Admin/Users.cshtml.";

                throw new InvalidOperationException(msg);
            }
        }
    }
}
