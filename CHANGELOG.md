# Changelog

## 2026-10-19

### Dependabot e CI (PR "dependabot-ci")
- `.github/dependabot.yml`, novo: atualiza os pacotes NuGet (semanal) e as GitHub
  Actions usadas nos workflows (mensal) sozinho, sem precisar de acesso ao PC Linux —
  14 dias de cooldown antes de abrir a PR, exceto para correções de segurança (o
  Dependabot ignora sempre o cooldown para essas, por desenho próprio do GitHub).
- `.github/workflows/ci.yml`, novo: corre em cada PR e em cada push a `master`, em
  `ubuntu-24.04` com o SDK do .NET 8 — `dotnet restore`, `dotnet build`, e
  `dotnet list package --vulnerable --include-transitive`, a falhar (`exit 1`) se o
  output tiver "following vulnerable packages" (só aparece quando há mesmo falhas
  conhecidas; a mensagem de "sem falhas" é diferente, não dá falso positivo).
- `CLAUDE.md`: documenta que uma PR do Dependabot só entra com o CI verde, uma de cada
  vez, e que um conflito se resolve com `@dependabot rebase` comentado na PR — nunca
  editando o `.csproj` à mão.

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
- **Resolvido numa PR seguinte** (a partir de `master`, cherry-pick deste commit):
  `Microsoft.Extensions.Caching.Memory`, `System.Security.Cryptography.Pkcs` e
  `System.Security.Cryptography.Xml`, fixados explicitamente no `.csproj` para versões
  8.0.x mais recentes. `dotnet list package --vulnerable --include-transitive` passa a
  não acusar nenhuma falha.
  - `Microsoft.Extensions.Caching.Memory 8.0.1` (publicada 2024-10-08) resolve
    GHSA-qj66-m88j-hmgj.
  - `System.Security.Cryptography.Pkcs 8.0.2` resolve GHSA-846f-f764-76mx /
    GHSA-2fmv-rv2c-59x7. **Nota:** publicada 2026-10-13, só 6 dias antes desta alteração
    (2026-10-19) — abaixo dos 14 dias habituais de margem contra bugs recém-lançados.
    A `8.0.1` (2024-10-08, cumpre os 14 dias) continua vulnerável a GHSA-846f-f764-76mx;
    não há nenhuma versão 8.0.x entre as duas. Usada a `8.0.2` por decisão explícita do
    utilizador, para fechar já a falha conhecida.
  - `System.Security.Cryptography.Xml 8.0.5` resolve GHSA-7hqp-7c5m-4vq6,
    GHSA-c399-9853-6jmj, GHSA-r955-8q5j-3h83 e GHSA-vrv5-2jg9-4mmr. **Mesma nota:**
    publicada 2026-10-13 (6 dias). A `8.0.4` (2026-07-14, cumpre os 14 dias) continua
    vulnerável a estas 4 advisories — confirmado via `dotnet build` (avisos `NU1903`).
    Usada a `8.0.5` pela mesma razão acima.
  - `dotnet build` (0 erros) e arranque local contra PostgreSQL confirmados depois da
    mudança (sem suite de testes automatizados neste projeto).
