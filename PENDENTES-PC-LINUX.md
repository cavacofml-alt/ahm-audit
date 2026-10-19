# Pendentes que só quem tem acesso ao PC Linux pode fazer

Nada disto o Claude Code consegue fazer à distância — precisa de alguém com acesso
físico/SSH à máquina Linux onde o AHM Audit corre. Ordem recomendada: de cima para
baixo (cada ponto assume que o anterior já foi feito).

## 1. Trocar a password do utilizador `ahm` no PostgreSQL e o `DATABASE_URL`

**Porquê:** a password antiga (`ahm123`) esteve neste repositório GitHub — mesmo
depois de removida do código (`CLAUDE.md`/`Program.cs`), continua visível no
histórico de commits antigos a quem tiver acesso ao repo. Só uma password nova
fecha isto.

```bash
openssl rand -base64 24
sudo -u postgres psql
```
Dentro do `psql`, de forma interativa (evita que a password fique no
histórico do terminal):
```
\password ahm
```
```bash
nano ~/.bashrc   # trocar só a password na linha export DATABASE_URL=...
source ~/.bashrc
```

Sequência completa e explicada em `MANUAL.md` > "Trocar a password do utilizador
`ahm` no PostgreSQL".

## 2. Instalar o .NET 10 — antes de 10NOV26

**Porquê:** é o prazo combinado para a migração do projeto para .NET 10. A PR que
faz essa migração **só pode ser mergeada depois** de o SDK 10 estar instalado aqui
— sem ele, nem o `dotnet build` local nem o arranque da app funcionam depois do
merge.

```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0
dotnet --list-sdks
```

O SDK 10 coexiste com o 8 já instalado (não o substitui) — `dotnet --list-sdks`
deve mostrar as duas versões depois disto. Enviem o resultado ao responsável (ver
`MANUAL.md`) para se confirmar à distância que este passo está feito.

## 3. Restringir a porta 5000 aos computadores que usam o AHM Audit

**Porquê:** hoje a porta 5000 está acessível a qualquer máquina que consiga
alcançar este PC na rede — não só aos computadores da equipa que realmente usam a
app.

```bash
sudo ufw allow from <IP_OU_SUBREDE_DA_EQUIPA> to any port 5000 proto tcp
sudo ufw deny 5000/tcp
sudo ufw status numbered
```

Substituir `<IP_OU_SUBREDE_DA_EQUIPA>` pelo IP de cada computador que precisa de
aceder (ex. `192.168.1.50`) ou pela sub-rede inteira (ex. `192.168.1.0/24`), se
for mais prático — pode repetir-se a linha `ufw allow` uma vez por IP/sub-rede.
A ordem importa: a(s) linha(s) `allow` específica(s) têm de ficar **antes** da
`deny` geral (o `ufw` aplica as regras pela ordem em que foram adicionadas,
primeira que corresponder ganha) — `ufw status numbered` confirma a ordem final.

## 4. HTTPS — proxy (Caddy) + app só em `127.0.0.1:5000`

**Porquê:** hoje as passwords (login) e os dados das auditorias passam pela rede
às claras (HTTP simples) — qualquer pessoa na mesma rede local consegue ver esse
tráfego. Um proxy com HTTPS na porta 443 resolve isto, mantendo a porta 5000
fechada ao exterior (só acessível de dentro da própria máquina).

**Este ponto é para fazer junto com a instalação do servidor de acessos do
Operations Center** (que vai ficar no mesmo PC) — os dois vão partilhar o mesmo
proxy Caddy e o mesmo certificado, por isso não vale a pena montar isto duas
vezes em separado. Coordenar com quem estiver a tratar dessa instalação antes de
avançar sozinho aqui.

Passos de alto nível (confirmar o detalhe exato — nome do domínio/certificado
interno, etc. — com quem estiver a tratar do Operations Center):

```bash
sudo apt-get install -y caddy
```

Configurar o `Caddyfile` para servir o AHM Audit em HTTPS, encaminhando para
`127.0.0.1:5000` por trás. Depois de confirmado que o proxy funciona, o `app.Run`
no `Program.cs` tem de passar de `http://0.0.0.0:5000` para
`http://127.0.0.1:5000` (só escutar em loopback, não na rede) — **isto é uma
alteração de código, não algo que se faça só no PC**: pedir ao Claude Code para a
fazer numa PR, depois de o proxy já estar a funcionar (nunca antes, ou a app fica
inacessível enquanto o proxy não estiver pronto). Com a app a escutar só em
loopback, o `ufw deny 5000/tcp` do ponto 3 passa a ser redundante (a porta já não
seria alcançável de fora mesmo sem firewall) mas não faz mal manter os dois.

## 5. Arranque como serviço (systemd)

**Porquê:** hoje a app só corre enquanto alguém tiver um terminal aberto com
`dotnet run` — um reinício da máquina, ou alguém fechar essa janela por engano,
derruba a app. Um serviço `systemd` corre sozinho, arranca com a máquina, e volta
a pôr a app a correr se ela cair.

**Também para fazer junto com o servidor de acessos do Operations Center**, pelo
mesmo motivo do ponto 4.

```bash
sudo useradd --system --no-create-home ahm-audit
cd /home/pedro-nunes/ahm-audit
dotnet publish -c Release -o /opt/ahm-audit
sudo chown -R ahm-audit:ahm-audit /opt/ahm-audit
```

Ficheiro de unidade `systemd` (`/etc/systemd/system/ahm-audit.service`):

```ini
[Unit]
Description=AHM Audit
After=network.target postgresql.service

[Service]
Type=simple
User=ahm-audit
WorkingDirectory=/opt/ahm-audit
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DATABASE_URL=postgresql://ahm:NOVA_PASSWORD@localhost:5432/ahmaudit
ExecStart=/usr/bin/dotnet /opt/ahm-audit/AHM.Audit.dll
Restart=on-failure

[Install]
WantedBy=multi-user.target
```

(a `DATABASE_URL` aqui é a mesma do ponto 1, já com a password nova — nunca a
antiga)

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now ahm-audit
sudo systemctl status ahm-audit
```

A partir daqui, a sequência de atualização do `MANUAL.md` passa a terminar com
`sudo systemctl restart ahm-audit` em vez de `dotnet run` manual numa janela de
terminal.

## 6. Confirmar o `chmod 700` da pasta `dataprotection-keys`

**Porquê:** garantir que só a conta que corre a app (`ahm-audit`, a partir do
ponto 5) consegue ler as chaves de encriptação de sessão — já documentado em
`CLAUDE.md`, mas vale a pena confirmar explicitamente depois de criar a conta
`ahm-audit` e mudar o dono da pasta de publicação no ponto 5.

```bash
sudo chown -R ahm-audit:ahm-audit /opt/ahm-audit/dataprotection-keys
chmod 700 /opt/ahm-audit/dataprotection-keys
```

(o caminho muda de `/home/pedro-nunes/ahm-audit/dataprotection-keys` para
`/opt/ahm-audit/dataprotection-keys` a partir do momento em que o ponto 5 estiver
feito — antes disso, usar o caminho antigo, como já documentado em `CLAUDE.md` e
`MANUAL.md`)
