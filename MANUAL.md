# Manual de instalação / atualização manual (Linux)

Sequência completa, um comando a seguir ao outro (Enter entre cada um), na máquina
Linux (`/home/pedro-nunes/ahm-audit`):

```bash
cd /home/pedro-nunes/ahm-audit
git pull
dotnet build
dotnet run
```

Se o Claude Code tiver feito `push` de alterações ao modelo de dados (migrations
novas), é preciso mais um passo depois do `git pull` e antes do `dotnet build`:

```bash
dotnet ef database update
```

(só corram este se souberem que houve uma migration nova — o Claude Code deve
avisar-vos nesse caso)

Para parar a app: `Ctrl+C` na mesma janela.

## Se o `git pull` der conflito

Alterações locais que ainda não fizeram commit:

```bash
git status
```

Vejam o que aparece; se não houver nada importante para preservar:

```bash
git fetch origin
git reset --hard origin/master
```

## Primeira instalação / permissões da pasta de chaves

A app guarda as chaves de encriptação de sessão em
`/home/pedro-nunes/ahm-audit/dataprotection-keys` (ver `CLAUDE.md`). Depois do
primeiro arranque, que cria essa pasta automaticamente, restrinjam o acesso só
à conta que corre a app:

```bash
chmod 700 /home/pedro-nunes/ahm-audit/dataprotection-keys
```

## Trocar a password do utilizador `ahm` no PostgreSQL

A password real da base de dados local nunca fica no repositório (`CLAUDE.md` e
`Program.cs` só têm exemplos genéricos) — só em `~/.bashrc`. Para a trocar:

1. Gerar uma password nova, forte:
   ```bash
   openssl rand -base64 24
   ```
2. Aplicá-la ao utilizador `ahm` no PostgreSQL (substituir `NOVA_PASSWORD` pelo
   valor gerado em cima — sem `@`, `:` nem `/`, que partem o `DATABASE_URL` mais
   abaixo):
   ```bash
   sudo -u postgres psql -c "ALTER USER ahm WITH PASSWORD 'NOVA_PASSWORD';"
   ```
3. Atualizar o `DATABASE_URL` em `~/.bashrc` (abrir com `nano ~/.bashrc` ou
   equivalente, encontrar a linha `export DATABASE_URL=...` e trocar só a
   password, mantendo o resto do URL igual):
   ```bash
   export DATABASE_URL=postgresql://ahm:NOVA_PASSWORD@localhost:5432/ahmaudit
   ```
4. Recarregar o shell atual (ou abrir um terminal novo) e confirmar que a app
   ainda liga:
   ```bash
   source ~/.bashrc
   cd /home/pedro-nunes/ahm-audit && dotnet run
   ```

Nunca commitar a password nova no `.bashrc` para o repositório, nem a colar em
nenhum ficheiro dentro de `/home/pedro-nunes/ahm-audit` — fica só no `~/.bashrc`,
fora do repo.
