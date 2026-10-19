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

### Correções de segurança (PR "correcoes-seguranca")
- Password real (`ahm123`) retirada do `CLAUDE.md` e dos exemplos nas mensagens de erro
  do `Program.cs` — ficam só exemplos genéricos (`<password>`/`utilizador:password@...`).
  `MANUAL.md` ganhou uma secção nova: como trocar a password do utilizador `ahm` no
  PostgreSQL local e atualizar o `DATABASE_URL` em `~/.bashrc`, sem nenhuma password
  real no repositório.
- `Program.cs`: as mensagens de erro do arranque (URI inválido, falta utilizador/password)
  já não mostram a password do `DATABASE_URL` em texto simples — nova função
  `MaskDatabaseUrl` tapa-a (`utilizador:***@...`), com regex de texto (não `Uri`) para
  funcionar mesmo quando o URL está malformado.
- `AuditCsvExporter.Escape`: um valor que comece por `=`, `+`, `-`, `@`, tab ou carriage
  return leva um apóstrofo à frente no CSV exportado — proteção contra injeção de
  fórmulas ao abrir no Excel. `Backup.cshtml.cs` (`SafeGet`, nova `StripFormulaGuard`)
  tira esse apóstrofo de volta ao importar, para reconstruir o valor original. Testado
  com um ticket `=CMD|'/c calc'!A1`: exportado como `'=CMD|'/c calc'!A1`, e a mensagem de
  erro da importação mostra de volta `=CMD|'/c calc'!A1` sem o apóstrofo — o roundtrip
  está correto. Dados normais (sem esses carateres no início) continuam a exportar sem
  nenhuma alteração.
- Password mínima de **10 caracteres**, com a mesma mensagem, ao criar um utilizador
  (`Pages/Admin/Users.cshtml.cs`, `OnPostCreate` — antes não validava comprimento
  nenhum) e ao repor a password de um existente (`OnPostResetPassword` — antes exigia só
  6).
- Cabeçalhos de segurança em todas as respostas (`Middleware/SecurityHeadersMiddleware.cs`,
  novo, registado logo após `UseForwardedHeaders` para apanhar tudo, incluindo ficheiros
  estáticos e páginas de erro): `X-Content-Type-Options: nosniff`,
  `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, e uma
  `Content-Security-Policy`. O Bootstrap, os ícones do Bootstrap e o Chart.js vêm todos
  de `cdn.jsdelivr.net` (não estão em `wwwroot/lib`) — a CSP permite esse domínio em
  `script-src`/`style-src`/`font-src`. As páginas têm scripts e estilos inline (sem passo
  de build que gere nonces), por isso `'unsafe-inline'` fica em `script-src`/`style-src`.
  Confirmado com Playwright (Chromium) contra a app a correr localmente: login, dashboard,
  `Auditorias/Create`, `Auditorias`, `Admin/Backup` e `Admin/Users` — zero mensagens de
  violação de CSP na consola. Os únicos erros vistos foram falhas de rede do próprio
  sandbox a chegar ao `cdn.jsdelivr.net` (`ERR_TUNNEL_CONNECTION_FAILED`, confirmado não
  ser CSP), não um problema da configuração — **vale a pena confirmar uma vez no browser
  real do PC Linux, com acesso à internet a sério, que o CDN carrega**.
- `AllowedHosts` em `appsettings.json`: de `"*"` para `"localhost"`. **Não sei os nomes/IPs
  reais pelos quais a app é aberta no PC Linux** — se for mais do que `localhost` (ex. um
  IP de LAN ou outro hostname), é preciso ajustar este valor, senão a app responde
  `400 Bad Request` a esses pedidos.
- `Pages/Auditorias/Index.cshtml.cs`: a cópia local de `ChecklistFields` foi substituída
  por `AuditCsvExporter.ChecklistFields` (a mesma fonte única já usada no resto do
  projeto). Notas: `Create.cshtml.cs` e `Archive.cshtml.cs` continuam com cópias locais
  próprias — não foram tocados, só o ficheiro pedido.

**Confirmado no fim:** `dotnet build` (0 erros, 0 avisos). App a arrancar e ligar ao
PostgreSQL local; login funciona; `Auditorias/Create`, `Auditorias/Edit`,
`Auditorias/Detail`, dashboard (`Index`), `Admin/Backup` e `Admin/Users` todos a
responder `200`; criação de auditoria via autosave testada (incluindo o ticket de teste
da proteção contra fórmulas); exportação e importação de CSV testadas (incluindo o
roundtrip do apóstrofo de proteção). Não foi feito um teste completo do formulário de
`Edit` via POST direto (só GET + autosave, que é o mecanismo real que a página usa para
a maior parte dos campos) — risco residual baixo, mas fica registado.

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
