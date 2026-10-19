# Changelog

## 2026-10-19

### Segurança / dependências
- Removida a `PackageReference` não usada `Microsoft.EntityFrameworkCore.SqlServer` do
  `AHM.Audit.csproj` — confirmado por pesquisa em todos os `.cs` (incluindo `Migrations/`
  e `*ModelSnapshot.cs`) e `appsettings*.json` que não havia nenhum uso; a app liga-se
  sempre e só a PostgreSQL via `DATABASE_URL` (ver `CLAUDE.md`). Esta referência era resto
  de um template inicial e trazia consigo um bloco grande de dependências transitivas sem
  necessidade (`Microsoft.Data.SqlClient`, `Azure.Core`, `Azure.Identity`,
  `Microsoft.Identity.Client`, etc.).
- A remoção acima resolveu, por arrasto, as advisories de `Azure.Identity`
  (GHSA-wvxc-855f-jvrv, GHSA-m5vv-6r4h-3vj9) e `Microsoft.Identity.Client`
  (GHSA-x674-v45j-fwxw, GHSA-m5vv-6r4h-3vj9), que só eram trazidas por esse pacote.
- Fixadas explicitamente as versões `System.Text.Json 8.0.6` e `System.Formats.Asn1 8.0.2`
  (ambas publicadas há mais de um ano) para resolver as advisories High
  GHSA-hh2w-p6rv-4g7w / GHSA-8g4q-xg66-9fp4 (System.Text.Json 8.0.0) e
  GHSA-447r-wph3-92pm (System.Formats.Asn1 8.0.0).
- Confirmado `Microsoft.EntityFrameworkCore.Design` já tinha `<PrivateAssets>all</PrivateAssets>`
  — nada a mudar aí.
- `dotnet build` e arranque local contra PostgreSQL confirmados depois de cada alteração
  (sem suite de testes automatizados neste projeto).
- **Por resolver, fora do âmbito desta alteração** (continuam a aparecer em
  `dotnet list package --vulnerable --include-transitive`, não vêm do SqlServer nem do
  System.Text.Json/Asn1, por isso não mexidos agora): `Microsoft.Extensions.Caching.Memory`
  8.0.0 (High, GHSA-qj66-m88j-hmgj), `System.Security.Cryptography.Pkcs` 8.0.0 (High +
  Moderate) e `System.Security.Cryptography.Xml` 8.0.0 (vários High) — vêm transitivamente
  do EF Core / ASP.NET Core DataProtection em si, não de um pacote solto.
