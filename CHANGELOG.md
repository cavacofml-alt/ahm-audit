# Changelog

## 2026-10-19

### Restos e âmbito (PR "restos-ambientes")
- Removidos `algarve_monitor.py` (outro projeto — continha 4 endereços de email,
  nenhum relacionado com o AHM Audit, reportados ao utilizador antes de apagar),
  `create-audit-module.ps1` e `iniciar_ahm.bat` (restos do Windows — o `.bat` tinha a
  password real do PostgreSQL local em texto simples).
- `Program.cs`: `ForwardedHeadersOptions.KnownNetworks`/`KnownProxies` só são limpas
  (confiar em qualquer proxy, necessário no Render) quando a env var
  `AHM_BEHIND_PROXY=true` está definida. No PC Linux, sem proxy à frente, isto não se
  define, e o ASP.NET Core fica na regra por omissão (só confia em cabeçalhos vindos do
  próprio PC) — comportamento seguro quando não há proxy nenhum. **A variável
  `AHM_BEHIND_PROXY=true` tem de ser acrescentada às env vars do serviço no Render**
  para a app continuar a reconhecer HTTPS lá (sem isto, o cookie de sessão "Secure"
  deixa de ser enviado em produção no Render).
- `CLAUDE.md`: clarifica que a produção é só o PC Linux; Render + Supabase passam a
  estar documentados como verificação pessoal do utilizador, fora do âmbito do
  ISO 27001, nunca com dados reais (auditorias, nomes de agentes/officers) nem com a
  mesma password do PostgreSQL do PC Linux; removidas as referências ao Railway
  (já não existe) em `CLAUDE.md` e em dois comentários do `Program.cs`.
- `Dockerfile` não foi tocado (continua a ser o usado pelo Render).

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
