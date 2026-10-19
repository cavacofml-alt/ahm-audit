# Bibliotecas de terceiros (front-end)

Estas bibliotecas estão vendorizadas localmente — a app não depende de nenhum
CDN (ver CSP em `Middleware/SecurityHeadersMiddleware.cs`). Isto garante que a
app funciona mesmo sem acesso à internet.

| Biblioteca | Versão | Licença | Origem |
|---|---|---|---|
| Bootstrap | 5.3.3 | MIT | https://www.npmjs.com/package/bootstrap |
| Bootstrap Icons | 1.11.3 | MIT | https://www.npmjs.com/package/bootstrap-icons |
| Chart.js | 4.4.0 | MIT | https://www.npmjs.com/package/chart.js |

As versões correspondem exatamente às que a app usava antes a partir do
`cdn.jsdelivr.net`. O `chart.umd.min.js` foi gerado a partir do
`chart.umd.js` do pacote npm (que não inclui uma versão minificada) com
`terser`, mantendo o cabeçalho de licença original.

## Dependabot não vigia estas bibliotecas

Não há `package.json`/ecossistema npm neste repositório — o Dependabot só
cobre NuGet e GitHub Actions (`.github/dependabot.yml`). Estas bibliotecas
têm de ser revistas e atualizadas manualmente, tal como as bibliotecas
embutidas do Operations Center. Ao atualizar uma versão, confirmar a origem
oficial (npm, ou o site da biblioteca) e registar a alteração no
`CHANGELOG.md`.
