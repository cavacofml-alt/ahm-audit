# Changelog

## 2026-10-22 (3)

### Export/Import Excel passam a incluir as Observações por item da checklist
- `Data/AuditXlsxExporter.cs`: as observações de texto livre por item da checklist
  (`Auditoria.ItemObservations`, visíveis no separador "Observations" de cada auditoria)
  não iam nem no CSV antigo nem no novo export `.xlsx` — ficavam de fora de qualquer
  backup/restore. Agora o export escreve cada observação como **comentário na célula**
  do item correspondente (ex.: a célula "B1" da coluna da auditoria), em vez de lhe dar
  uma linha própria no layout — não altera o formato que a equipa já conhece.
- A leitura do `.xlsx` na importação (`ReadToCsvRows`) passa a ler esses comentários de
  volta e a reconstruir o `ItemObservations` de cada auditoria importada
  (`Pages/Admin/Backup.cshtml.cs`). O CSV continua sem conseguir guardar observações
  (não há onde as pôr num ficheiro de texto simples) — como já acontecia antes.
- Testado isoladamente (export → import no mesmo ficheiro), incluindo texto com acentos:
  as observações voltam exatamente iguais.

## 2026-10-22 (2)

### Correção de segurança — rascunho só pode ser continuado pelo próprio Agent (PR "fix-draft-owner-access")
- `Pages/Auditorias/Create.cshtml.cs` (`OnGet`/`OnPost`) e `Edit.cshtml.cs` (`OnGet`/`OnPost`):
  qualquer utilizador autenticado conseguia abrir e continuar o rascunho de **outro** Agent,
  pelo link "Continuar rascunho" ou diretamente por `/Auditorias/Edit?id=`. Só auditorias
  finalizadas são (por desenho) editáveis por toda a gente — um rascunho só deve ser
  continuado pelo próprio Agent ou por um admin.
- Os três endpoints de autosave (`/api/autosave`, `/api/autosave-field`,
  `/api/autosave-observation`, `Program.cs`) tinham o mesmo problema — só bloqueavam
  auditorias já finalizadas, nunca verificavam o dono de um rascunho. Um utilizador
  conseguia gravar alterações diretamente por AJAX no rascunho de outro Agent, mesmo sem
  conseguir abrir a página. Corrigido com a mesma regra, partilhada (`IsForbiddenDraftEdit`).
- **Bug relacionado encontrado ao testar**: todos os `Forbid()`/`Results.Forbid()` da
  aplicação (9 sítios, incluindo os já existentes antes desta alteração) rebentavam com
  uma exceção não tratada em vez de devolver um 403 limpo — esta app usa sessão simples,
  sem nenhum esquema de autenticação registado (`AddAuthentication`), e `Forbid()` precisa
  de um. Substituídos por `StatusCode(403)`/`Results.StatusCode(403)`.
- Testado com dois utilizadores não-admin ligados a Agents diferentes, e com o admin:
  confirmado que um Agent não consegue abrir, gravar nem alterar por AJAX o rascunho de
  outro (403 limpo, dados na base de dados inalterados), que o próprio Agent continua a
  conseguir, e que o admin continua a aceder a qualquer rascunho.

## 2026-10-22

### Import volta a aceitar o .xlsx do export, além do CSV (PR "feature-xlsx-import")
- `Data/AuditXlsxExporter.cs`: novo `ReadToCsvRows`, que lê um `.xlsx` no formato do
  export (coluna A com os rótulos, uma coluna por auditoria) e devolve as linhas no
  mesmo formato que o resto do import (`Backup.cshtml.cs`) já sabia processar a partir
  do CSV — por isso toda a validação existente (nomes/razões exatos, tudo-ou-nada)
  continua a aplicar-se sem duplicar código.
- "Restaurar Backup" (Admin > Backup & Restauro) passa a aceitar `.csv` **e** `.xlsx` —
  detetado pela extensão do ficheiro enviado.
- Antes desta alteração, descarregar auditorias em Excel (PR "feature-xlsx-export-
  transposed", 2026-10-21) deixava de ser possível voltar a importar esse mesmo
  ficheiro — o backup/restauro deixava de ser um ciclo fechado.
- Testado o ciclo completo com dados reais: exportar `.xlsx` → apagar todas as
  auditorias → importar o mesmo ficheiro `.xlsx` → dados confirmados idênticos aos
  originais (datas, razões de NOT OK múltiplas, notas com várias linhas/vírgulas/
  aspas). Confirmado também que o import de `.csv` simples continua a funcionar sem
  alterações.

## 2026-10-21 (2)

### Export de auditorias volta ao formato Excel antigo (PR "feature-xlsx-export-transposed")
- `Data/AuditXlsxExporter.cs` (novo, usa a biblioteca ClosedXML): o export passa a gerar
  um `.xlsx` real, no formato que a equipa já usava antes de haver aplicação — cada
  auditoria é uma coluna, os campos ficam fixos na coluna A (com destaque azul, igual ao
  ficheiro antigo), coluna A fixa ao deslocar para a direita. Datas como datas reais do
  Excel. Só inclui os campos que o AHM Audit guarda hoje — "Pedro's review" e "Employee
  ID" do ficheiro antigo ficam de fora, por não haver dados reais para lá pôr.
- `Pages/Admin/BackupExport.cshtml.cs` e `Pages/Auditorias/Export.cshtml.cs` (os dois
  pontos de download) passam a produzir este `.xlsx` em vez do `.csv` anterior. Testado
  nos dois, com auditorias com notas em várias linhas, razões de NOT OK e datas —
  confirmado o conteúdo com um leitor de Excel real.
- O **import** de CSV (Admin > Backup > Importar) não muda — continua a aceitar o
  formato antigo (uma auditoria por linha), não o novo formato de export transposto.

## 2026-10-21

### Correção — export de CSV desformatado com notas em várias linhas (PR "fix-csv-export-line-endings")
- `Data/AuditCsvExporter.cs`: o CSV era gerado com `AppendLine` (fim de linha `\n`, por a
  app correr em Linux) — igual à quebra de linha que uma nota em várias linhas já tinha
  dentro da própria célula. O Excel não conseguia distinguir "nota com 2 linhas" de
  "próxima auditoria", e a linha de dados ficava partida em várias linhas do ficheiro,
  desalinhando tudo a seguir. Corrigido para usar sempre `\r\n` como fim de linha do CSV,
  independente do sistema operativo — agora inequívoco em relação a uma quebra de linha
  dentro de um campo. O export de auditorias já não usa este código (passou a `.xlsx`,
  ver entrada acima) — mantido correto para o dia em que o CSV voltar a ser usado (ex.
  import/export de rotina, scripts externos).
- Reproduzido o bug e confirmada a correção com uma exportação real (auditoria com notas
  em várias linhas, vírgulas e aspas): antes, 1 linha de dados aparecia fisicamente
  espalhada por 4 linhas do ficheiro; depois, fica numa só, lida corretamente por um
  parser de CSV (50 colunas, nota intacta).

## 2026-10-19 (2)

### Correção de segurança — XSS nas razões de NOT OK (PR "fix-xss-nonconformity-reason")
- `Pages/Auditorias/Create.cshtml` e `Edit.cshtml`: o texto da razão de NOT OK (escolhida
  de Admin > Definições) era inserido no ecrã via `innerHTML` sem nenhum escape — uma
  razão configurada com HTML/JavaScript corria no browser de qualquer pessoa que depois
  visse essa auditoria. Corrigido construindo o elemento com `textContent`/`createElement`
  em vez de concatenar HTML.
- `Pages/Index.cshtml`: o mesmo problema no painel "drill-down" por Officer do dashboard
  (razões já guardadas na base de dados, mostradas sem escape). Mesma correção.
- Como a razão só pode ser escolhida de uma lista configurada por um administrador (nunca
  texto livre de um utilizador normal), não era explorável por qualquer pessoa — mas um
  admin com a conta comprometida, ou um erro ao configurar uma razão, afetaria todos os
  utilizadores que vissem essa razão. Testado com um payload real (`<img src=x
  onerror=...>`) numa razão: já não corre, aparece como texto inofensivo, em ambos os
  pontos corrigidos.

## 2026-10-19

### Bibliotecas do browser vendorizadas localmente (PR "vendor-local-libs")
- A app deixa de depender do `cdn.jsdelivr.net`: Bootstrap 5.3.3 (CSS + bundle JS),
  Bootstrap Icons 1.11.3 (CSS + fontes) e Chart.js 4.4.0 (`chart.umd.min.js`) passam a
  estar em `wwwroot/lib/` (mesmas versões que já estavam a ser usadas). `chart.umd.min.js`
  foi gerado com `terser` a partir do `chart.umd.js` do pacote npm (que não inclui uma
  versão minificada), mantendo o cabeçalho de licença.
- `_Layout.cshtml`, `Login.cshtml` e `Index.cshtml`: ligações trocadas de
  `https://cdn.jsdelivr.net/...` para `~/lib/...`.
- Apagados o `wwwroot/lib/bootstrap` antigo (5.1.0), `jquery`, `jquery-validation`,
  `jquery-validation-unobtrusive` e `_ValidationScriptsPartial.cshtml` — confirmado por
  grep no repositório que nada os usava.
- Novo `wwwroot/lib/README.md`: origem, versão e licença (MIT) de cada biblioteca, e
  nota de que o Dependabot não as vigia (não há ecossistema npm neste repo) — são
  revistas à mão, como as bibliotecas embutidas do Operations Center.
- `Middleware/SecurityHeadersMiddleware.cs`: `https://cdn.jsdelivr.net` removido da CSP
  (`script-src`, `style-src`, `font-src`) — fica só `'self'` (e `'unsafe-inline'` onde já
  estava).
- `.github/workflows/ci.yml`: `set -o pipefail` no passo de `dotnet list package
  --vulnerable`, para o CI falhar também se o comando em si der erro (ex.: NuGet sem
  resposta), não só quando encontra vulnerabilidades conhecidas.
- `MANUAL.md` e `PENDENTES-PC-LINUX.md`: troca da password do utilizador `ahm` passa a
  usar o comando interativo do `psql` (`sudo -u postgres psql`, depois `\password ahm`)
  em vez de `ALTER USER ... WITH PASSWORD '...'` numa só linha, para a password nunca
  aparecer no histórico do terminal.
- Verificado num browser real (Chromium headless, sem passar pelo proxy de rede do
  ambiente de build — `cdn.jsdelivr.net` confirmado inacessível nessas condições) que
  login, lista de auditorias, edição de uma auditoria e o dashboard (com os gráficos do
  Chart.js) funcionam sem nenhum pedido falhado nem erro de consola.

### Correção urgente — AllowedHosts (PR "fix-allowedhosts-lan")
- `appsettings.json`: `AllowedHosts` de `"localhost"` para `"localhost;192.168.2.15"` —
  os outros computadores da equipa abrem o AHM Audit em
  `http://192.168.2.15:5000/Account/Login`, e com só `"localhost"` o ASP.NET Core
  recusava esses pedidos com `400 Bad Request` ("Invalid Hostname"). Introduzido na PR
  "correcoes-seguranca" (que trocou o `"*"` original por `"localhost"` sem saber o
  endereço real de rede) — corrigido agora que se sabe qual é.
- `CLAUDE.md`: nova nota em "Áreas sensíveis" — `AllowedHosts` tem de ter **todos** os
  endereços pelos quais a app é aberta na rede, nunca voltar a `"*"`.

### Manual e pendentes do PC Linux (PR "manual-pendentes-pc-linux")
- Confirmado em `Program.cs` (`db.Database.Migrate()`, linha ~299) que as migrations
  são sempre aplicadas automaticamente no arranque — o passo manual
  `dotnet ef database update` saiu do `MANUAL.md` por ser redundante.
- `MANUAL.md`: acrescentado `dotnet --list-sdks` à sequência, logo a seguir ao
  `git pull` — o resultado deve ser enviado ao responsável, para se saber à
  distância quando o .NET 10 fica instalado no PC Linux (sem precisar de acesso
  direto à máquina).
- `PENDENTES-PC-LINUX.md`, novo: os 6 passos que só alguém com acesso ao PC Linux
  consegue fazer, por ordem, cada um com o comando e a razão — trocar a password
  do `ahm`/`DATABASE_URL` (a antiga esteve no repositório), instalar o .NET 10
  antes de 10NOV26, restringir a porta 5000 por firewall, HTTPS (Caddy + app só
  em loopback — a par da instalação do servidor de acessos do Operations Center,
  que vai partilhar o mesmo proxy/certificado), arranque como serviço systemd, e
  confirmar o `chmod 700` da pasta de chaves depois de mudar para a conta de
  serviço.

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
