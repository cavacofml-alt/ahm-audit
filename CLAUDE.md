# AHM Audit

Aplicação interna de auditorias de weight & balance (AHM) para uma companhia aérea.

## Stack
- ASP.NET Core 8, Razor Pages, C#
- Entity Framework Core 8 + Npgsql (PostgreSQL — **nunca SQL Server**)
- Bootstrap 5, Chart.js, vanilla JS (sem framework front-end)

## Repositório e deploy
- GitHub: https://github.com/cavacofml-alt/ahm-audit (branch `master`)
- **Produção é só o PC Linux** (`/home/pedro-nunes/ahm-audit`, com PostgreSQL local) —
  é aí que correm auditorias reais, com nomes reais de agentes/officers. Ver `MANUAL.md`
  para a instalação/atualização manual.
- **Render + Supabase são uma verificação pessoal do utilizador, fora do âmbito do
  ISO 27001** (serviço `ahm-audit` no Render, Frankfurt, plano free; projeto `ahm-audit`
  no Supabase, eu-west-1). Servem só para confirmar que o código builda e arranca numa
  plataforma diferente antes de fazer `git pull` no PC Linux — **nunca usar dados reais
  aí**: nunca auditorias reais, nunca nomes reais de agentes/officers, e a password da
  base de dados do Supabase **nunca** pode ser igual à do PostgreSQL do PC Linux. Se um
  dia passar a ter dados reais, passa a entrar no âmbito do ISO 27001 e este documento
  tem de ser atualizado.
- Local (Linux): `/home/pedro-nunes/ahm-audit`

## Base de dados
- PostgreSQL, ligação via variável de ambiente `DATABASE_URL`
- Local (produção): `postgresql://ahm:<password>@localhost:5432/ahmaudit` — a password
  real fica só em `~/.bashrc` na máquina Linux, nunca neste repositório (ver `MANUAL.md`
  para a trocar).
- Render/Supabase (verificação pessoal, fora do âmbito ISO 27001): `DATABASE_URL` é
  uma env var no serviço do Render, **não** no código — aponta para o connection pooler
  Supavisor em modo sessão (`aws-0-eu-west-1.pooler.supabase.com:5432`), nunca para a
  ligação direta (`db.<ref>.supabase.co`), porque o Render só suporta saída IPv4 e a
  ligação direta do Supabase é IPv6-only no plano free. Password diferente da do PC
  Linux (ver acima).
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
- **`AllowedHosts`** (`appsettings.json`): tem de incluir **todos** os endereços pelos
  quais a aplicação é aberta na rede, separados por `;` — não só `localhost`. Hoje é
  `"localhost;192.168.2.15"`, porque os outros computadores da equipa abrem a app em
  `http://192.168.2.15:5000`. Sem o endereço certo aqui, o ASP.NET Core recusa esses
  pedidos com `400 Bad Request` ("Invalid Hostname") — já aconteceu uma vez. Se o IP do
  PC Linux mudar, ou se passar a ser aberto por outro nome/endereço também, acrescentar
  aqui (nunca voltar a `"*"`, que desativa esta proteção por completo).
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
- **Dependabot** (`.github/dependabot.yml`) mantém os pacotes NuGet (semanal) e as
  GitHub Actions dos workflows (mensal) atualizados sozinho, sem precisar de acesso ao
  PC Linux — com 14 dias de cooldown antes de abrir a PR (a mesma regra em cima), exceto
  para correções de segurança, que o Dependabot abre logo (o cooldown não se aplica a
  essas, por desenho). O CI (`.github/workflows/ci.yml`) corre em cada PR e em cada push
  a `master`: `dotnet build` e `dotnet list package --vulnerable --include-transitive`,
  a falhar se houver alguma falha conhecida.
- Uma PR do Dependabot só entra com o **CI verde**, **uma de cada vez** (nunca mesclar
  várias em paralelo — cada merge pode mudar o que as outras resolvem). Se tiver
  conflitos, comentar `@dependabot rebase` na própria PR — **nunca editar à mão** o
  `.csproj` dessa PR para resolver o conflito; o Dependabot sabe recalcular.

## Regras de trabalho
- Sempre correr `dotnet build` antes de dar como terminado.
- Ao terminar alterações, fazer `git add` / `git commit` / `git push` diretamente — o
  utilizador confia no Claude Code para isso neste projeto (ao contrário da conversa no
  chat, onde ele faz o push manualmente).
- Testar mentalmente o impacto em: dashboard (`Pages/Index.cshtml`), criação de auditoria
  (`Pages/Auditorias/Create.cshtml`) e edição (`Edit.cshtml`) antes de mexer em modelos
  partilhados (`Models/Auditoria.cs`, `DashboardPermissionCatalog.cs`) — são os pontos mais
  usados e mais fáceis de partir sem perceber.
