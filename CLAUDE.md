# AHM Audit

Aplicação interna de auditorias de weight & balance (AHM) para uma companhia aérea.

## Stack
- ASP.NET Core 8, Razor Pages, C#
- Entity Framework Core 8 + Npgsql (PostgreSQL — **nunca SQL Server**)
- Bootstrap 5, Chart.js, vanilla JS (sem framework front-end)

## Repositório e deploy
- GitHub: https://github.com/cavacofml-alt/ahm-audit (branch `master`)
- Produção: Railway, deploy automático a cada push em `master`
- Local (Linux): `/home/pedro-nunes/ahm-audit`

## Base de dados
- PostgreSQL, ligação via variável de ambiente `DATABASE_URL`
- Local: `postgresql://ahm:ahm123@localhost:5432/ahmaudit`
  (já definida em `~/.bashrc` na máquina Linux — não precisa de export manual)
- Produção: injetada automaticamente pelo Railway
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
- **DataProtection keys**: guardadas em `./dataprotection-keys` (relativo à pasta do
  projeto, não `/tmp` — `/tmp` é limpo a cada reinício e invalida sessões/tokens). Esta
  pasta está no `.gitignore` — nunca commitar chaves de encriptação.
- **Autenticação**: sessão simples (`HttpContext.Session`), sem ASP.NET Identity. Toda a
  gente autenticada pode ver/editar auditorias de outros agentes — é intencional
  (equipa pequena, colaboração entre agentes), não é bug.

## Regras de trabalho
- Sempre correr `dotnet build` antes de dar como terminado.
- Ao terminar alterações, fazer `git add` / `git commit` / `git push` diretamente — o
  utilizador confia no Claude Code para isso neste projeto (ao contrário da conversa no
  chat, onde ele faz o push manualmente).
- Testar mentalmente o impacto em: dashboard (`Pages/Index.cshtml`), criação de auditoria
  (`Pages/Auditorias/Create.cshtml`) e edição (`Edit.cshtml`) antes de mexer em modelos
  partilhados (`Models/Auditoria.cs`, `DashboardPermissionCatalog.cs`) — são os pontos mais
  usados e mais fáceis de partir sem perceber.
