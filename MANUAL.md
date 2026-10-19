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
