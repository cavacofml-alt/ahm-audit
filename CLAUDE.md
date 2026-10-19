# AHM Audit

Aplicação interna de auditorias de weight & balance (AHM) para uma companhia aérea.

## Stack
- ASP.NET Core 8, Razor Pages, C#
- Entity Framework Core 8 + Npgsql (PostgreSQL — **nunca SQL Server**)
- Bootstrap 5, Chart.js, vanilla JS (sem framework front-end)

## Repositório e deploy
- GitHub: https://github.com/cavacofml-alt/ahm-audit (branch `master`)
- Produção: Render (`ahm-audit`, região Frankfurt, plano free), deploy automático a
  cada push em `master` — build via o `Dockerfile` existente na raiz do projeto.
  Dashboard: https://dashboard.render.com/web/srv-db8dgcjncjis73fggptg · URL pública:
  https://ahm-audit.onrender.com
- Anteriormente em Railway — migrado para Render + Supabase porque o trial do Railway
  acabou. Não há nada do Railway a manter; pode ser ignorado/removido se ainda existir
  alguma referência.
- Local (Linux): `/home/pedro-nunes/ahm-audit`

## Base de dados
- PostgreSQL, ligação via variável de ambiente `DATABASE_URL`
- Local: `postgresql://ahm:<password>@localhost:5432/ahmaudit` — a password real
  fica só em `~/.bashrc` na máquina Linux, nunca neste repositório (ver `MANUAL.md`
  para a trocar).
- Produção: Supabase (projeto `ahm-audit`, região eu-west-1). A `DATABASE_URL` está
  definida como env var no serviço do Render, **não** no código — aponta para o
  connection pooler Supavisor em modo sessão (`aws-0-eu-west-1.pooler.supabase.com:5432`),
  nunca para a ligação direta (`db.<ref>.supabase.co`), porque o Render só suporta
  saída IPv4 e a ligação direta do Supabase é IPv6-only no plano free.
- **Row Level Security (RLS) está ativo em todas as tabelas no Supabase, sem políticas**
  — isto bloqueia por completo a API REST pública do Supabase (PostgREST/anon key),
  que de outra forma exporia todos os dados sem precisar de login nenhum na app. A app
  liga-se sempre via Npgsql com o utilizador `postgres` (ligação direta à base de dados,
  não à API), que ignora RLS por ser o dono das tabelas — por isso RLS sem políticas não
  afeta o funcionamento normal da app, só bloqueia esse acesso externo que nunca é usado.
  Se um dia se quiser usar a API/SDK do Supabase diretamente (não é o caso hoje), é
  preciso criar políticas explícitas primeiro.
- **Nunca existiu, nem deve existir, um fallback para SQL Server.** Já houve um
  bug destes (resto de um template inicial) que causava um erro confuso —
  se `DATABASE_URL` não estiver definida, a app deve falhar logo no arranque
  com uma mensagem clara, nunca tentar outra base de dados.

## Comandos habituais
```bash
dotnet build
dotnet run
dotnet ef migrations add NomeDaMigration   # só quando o modelo de dados muda
dotnet ef migrations list                  # ver o que já foi aplicado
```

## Migrations — cuidado especial
- Antes de gerar uma migration nova, confirmar que `Migrations/AuditDbContextModelSnapshot.cs`
  está sincronizado com o estado real da base de dados (já houve um incidente em que uma
  cópia desatualizada desse ficheiro fez uma migration nova tentar recriar colunas que já
  existiam, partindo o arranque da app).
- Nunca copiar a pasta `Migrations/` de outro sítio por cima da existente — deixar sempre o
  `dotnet ef migrations add` gerar a partir do estado atual real do projeto.

## Áreas sensíveis (não mexer sem pensar duas vezes)
- **Seeds no arranque** (`Program.cs`): o seed de Agents/Officers e de Airlines só corre
  numa base de dados **completamente vazia** (`if (!db.Persons.Any())` / `if (!db.Airlines.Any())`).
  Já houve um bug em que o seed corria em todos os arranques e ressuscitava agents/officers
  apagados manualmente — nunca voltar a esse padrão.
- **Import de CSV** (`Pages/Admin/Backup.cshtml.cs`): validação estrita e tudo-ou-nada —
  nomes de Agent/Officer e razões de NOT OK têm de corresponder exatamente (tolerante a
  acentos) a registos existentes em Admin > Pessoas / Admin > Definições, senão a
  importação inteira falha sem gravar nada. O parser de CSV é feito à mão
  (`ParseCsv` em `Backup.cshtml.cs`) porque células com texto em várias linhas (quebras de
  linha dentro de campos) partiam a leitura linha-a-linha do `StreamReader.ReadLine()` — não
  voltar a essa abordagem.
- **Razões de NOT OK** (`NoReasons` em `Auditoria`): guardadas como texto
  `"campo=razão;campo2=razão2"`. Ao fazer parsing deste formato, usar sempre
  `.GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Last()[1])` em vez de
  `.ToDictionary(p => p[0], p => p[1])` direto — um campo duplicado no texto faz o
  `ToDictionary` simples rebentar com exceção.
- **DataProtection keys**: guardadas em `./dataprotection-keys` (relativo à
  `ContentRootPath` da app, não `/tmp` — `/tmp` é limpo a cada reinício e invalida
  sessões/tokens). No PC Linux isto é `/home/pedro-nunes/ahm-audit/dataprotection-keys`.
  Esta pasta está no `.gitignore` — nunca commitar chaves de encriptação. Garantir que só
  a conta que corre a app consegue ler esta pasta:
  ```bash
  chmod 700 /home/pedro-nunes/ahm-audit/dataprotection-keys
  ```
- **Autenticação**: sessão simples (`HttpContext.Session`), sem ASP.NET Identity. Toda a
  gente autenticada pode ver/editar auditorias de outros agentes — é intencional
  (equipa pequena, colaboração entre agentes), não é bug.

## Dependências
- Ao corrigir uma vulnerabilidade de uma dependência transitiva (`dotnet list package
  --vulnerable --include-transitive`), fixar a versão mais recente da série (ex.: `8.0.x`)
  que a resolve, mas só se tiver **mais de 14 dias** desde a publicação — margem contra
  bugs recém-lançados que ainda não tiveram tempo de aparecer.
- **Exceção à regra dos 14 dias:** não se aplica quando a correção é um patch de
  segurança oficial do próprio fornecedor (ex.: Microsoft, para pacotes `System.*`/
  `Microsoft.*`) para uma falha de gravidade **High** ou **Critical**, desde que a
  assinatura do pacote seja confirmada antes de o usar:
  ```bash
  dotnet nuget verify --all ~/.nuget/packages/<pacote>/<versão>/*.nupkg
  ```
  Tem de mostrar assinatura `Author` válida com `O=Microsoft Corporation` (ignorar avisos
  `NU3018`/`NU3028` de revogação de certificado se forem só por não conseguir contactar o
  servidor de CRL — acontece em ambientes com rede restrita). Nestes casos, usar a versão
  mais recente mesmo com menos de 14 dias, documentando a decisão e o resultado da
  verificação de assinatura no commit/PR.

## Regras de trabalho
- Sempre correr `dotnet build` antes de dar como terminado.
- Ao terminar alterações, fazer `git add` / `git commit` / `git push` diretamente — o
  utilizador confia no Claude Code para isso neste projeto (ao contrário da conversa no
  chat, onde ele faz o push manualmente).
- Testar mentalmente o impacto em: dashboard (`Pages/Index.cshtml`), criação de auditoria
  (`Pages/Auditorias/Create.cshtml`) e edição (`Edit.cshtml`) antes de mexer em modelos
  partilhados (`Models/Auditoria.cs`, `DashboardPermissionCatalog.cs`) — são os pontos mais
  usados e mais fáceis de partir sem perceber.
