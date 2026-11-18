# UNBEATABLE — Tradução PT-BR (mod de fã)

Tradução para português do Brasil de **UNBEATABLE** (D-CELL GAMES), feita como mod BepInEx.
Cobre todas as falas da história (17.745 linhas), menus, opções, tutoriais, desafios do Arcade (folios) e notificações.

> ⚠️ **Aviso sobre testes**
>
> Até agora só foram testados no jogo **os acentos** e **o começo do capítulo 1** (o pseudo tutorial, as configurações e a calibração).
> Por conta da natureza rebelde do jogo, que espalha texto por tudo quanto é canto e em caixas de tamanho fixo, **outras partes podem aparecer mal colocadas**: palavras grandes demais, texto estourando a borda, encavalando ou cortado.
> Se encontrar algum caso, abra uma [issue](../../issues) com print e o lugar do jogo onde aconteceu. Não garanto que isso fará com que eu conserte seu bug, pois minha assinatura do Claude está acabando, mas isso pode ajudar a outros devs que quiserem fazer fork da tradução.

Mod não oficial, feito por fã com ajuda de IA (Claude Code, da Anthropic), sem ligação com a D-CELL GAMES ou a Playstack. Você precisa ter o jogo.

## Instalação

1. Instale o **BepInEx 5** (versão `win_x64`, testado com 5.4.23.6) na pasta do jogo.
   Baixe pelo GitHub oficial: <https://github.com/BepInEx/BepInEx/releases>
   (no Steam: botão direito no jogo → Gerenciar → Procurar arquivos locais).
2. Abra o jogo uma vez para o BepInEx criar as pastas e feche.
3. Baixe o `.zip` da [última release](../../releases/latest) e extraia **dentro da pasta do jogo**.
   Deve ficar assim: `UNBEATABLE/BepInEx/plugins/UnbeatablePTBR/UnbeatablePTBR.dll` (+ pasta `traducao`).
4. No jogo, deixe o idioma em **English**. O mod troca o inglês pelo português.

Para desinstalar, apague a pasta `BepInEx/plugins/UnbeatablePTBR`. O mod não altera nenhum arquivo do jogo.

## Escolhas de tradução

- **HARM** virou **DANO** (a sigla também foi traduzida, mas não vou estragar aqui); **Whacktown** virou **Tacolândia**; **Silence** virou **Silêncio**.
- Palavrões não foram suavizados.
- Avaliações: PERFEITO / ÓTIMO / QUASE / ERROU.
- Nos folios (desafios do Arcade), trechos de letras de música (como os da Jamie Paige), memes e trocadilhos que só funcionam no original foram mantidos em inglês de propósito.
- Detalhes de tom, vozes dos personagens e glossário em [`GUIA_DE_ESTILO.md`](GUIA_DE_ESTILO.md) (Aviso: contem VÁRIOS spoilers do jogo.).

## Para quem quer ajudar

- `traducao/dialogos.csv` (`id`, `pt`) e `traducao/interface.csv` (`tabela`, `chave`, `en_md5`, `pt`) têm só o texto traduzido; o texto original do jogo não é distribuído aqui.
- Para ver o inglês ao lado da tradução, use o **extrator**, que tira os textos da sua própria cópia do jogo:
  ```
  pip install UnityPy
  python extrator/extrair_textos.py
  ```
  Se o jogo não estiver na pasta padrão da Steam, passe `--jogo "caminho\da\pasta\UNBEATABLE"`. Leva menos de um minuto.
  Ele cria a pasta `textos_originais/` com:
  - `interface.csv` e `dialogos.csv`: inglês e português lado a lado, com a coluna `situacao` (`ok`, `falta`, `mudou` quando o inglês mudou num patch, `sobrando` quando o texto saiu do jogo);
  - `interface_en.csv` e `dialogos_en.csv`: só o texto original.

  Os dois CSVs lado a lado também funcionam direto no plugin.
- Edite a coluna `pt` (UTF-8), copie para `BepInEx/plugins/UnbeatablePTBR/traducao/` e abra o jogo.
- Para listar textos sem tradução: em `BepInEx/config/br.traducao.unbeatable.cfg`, ligue `RegistrarFaltantes = true`; eles aparecem em `BepInEx/LogOutput.log`.
- Compilar o plugin: `powershell -ExecutionPolicy Bypass -File build.ps1 -Install` (precisa do BepInEx já instalado no jogo).

## Como funciona

Plugin BepInEx 5 (Unity 6, Mono) com patches Harmony:
- diálogos do Yarn Spinner (`Yarn.Unity.Localization.GetLocalizedString`);
- tabelas do Unity Localization (`DetailedLocalizationTable.GetEntry`);
- alguns textos fixos no código (avaliações, nomes de cor) trocados no TextMeshPro.
