# Manual de instalação / atualização manual (Linux)

Sequência completa, um comando a seguir ao outro (Enter entre cada um), na máquina
Linux (`/home/pedro-nunes/ahm-audit`):

```bash
cd /home/pedro-nunes/ahm-audit
git pull
dotnet --list-sdks
dotnet build
dotnet run
```

Não é preciso correr `dotnet ef database update` manualmente mesmo quando há
migrations novas — `Program.cs` corre `db.Database.Migrate()` sempre no arranque,
que aplica sozinho qualquer migration pendente antes da app ficar disponível.

**`dotnet --list-sdks`**: envia o resultado deste comando ao responsável depois de
cada `git pull` — é como se sabe, à distância, quando o .NET 10 já está instalado
nesta máquina (ver `PENDENTES-PC-LINUX.md`, ponto 2). Antes de ter o .NET 10
instalado, só deve aparecer uma linha com `8.0.x`; depois de instalado, aparecem as
duas (`8.0.x` e `10.0.x`) — as duas podem coexistir sem problema.

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
