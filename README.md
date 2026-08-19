# Fábrica de IA

**[▶ Jogar no navegador](https://paulo-haikai.github.io/fabrica-de-ia/)**

Uma aula de 90 minutos, em forma de jogo, sobre como uma IA de linguagem
funciona por dentro. Em português, sem pré-requisito de programação nem de
matemática além da que o aluno já tem.

Não traz série indicada de propósito. Quem conhece a turma é quem está na
frente dela — as bancadas foram testadas com estudantes a partir dos 12 anos,
e cabem tanto num Fundamental II quanto num Médio, mudando só o que o professor
amarra depois de cada uma.

O aluno atravessa doze bancadas de uma oficina. Em cada uma, um artesão entrega
uma tarefa concreta — cortar palavras em pedaços, encaixar dominós, apostar em
qual lâmpada acende — e a tarefa **é** o conceito. Não há slide, não há vídeo,
não há definição antes do jogo: a mecânica ensina, e o professor amarra depois.

> O princípio que guia o projeto inteiro: **menos texto na tela, a mecânica é que
> ensina.** Se uma bancada precisa de um parágrafo para ser entendida, a bancada
> está errada, não o aluno.

---

## As doze bancadas

| # | Bancada | O que o aluno faz | O que ele aprende |
|---|---------|-------------------|-------------------|
| 1 | **Adivinhe a palavra** — Tico | Termo/Wordle em duas metades | Que informação é aquilo que corta possibilidades |
| 2 | **O dominó das palavras** — Dona Ciça | Encaixa peças de duas pontas até formar uma frase | Bigramas: prever a próxima palavra pela anterior |
| 3 | **O arquivo que não cabe** — Mestre Aurélio | Campo minado numa tabela de pares | Por que a tabela de todos os pares é grande demais |
| 4 | **A mesa de corte** — Nara | 2048 com pedaços de palavra | Tokenização: por que a máquina não lê letra por letra |
| 5 | **O mapa das palavras** — Bento | Connections: doze palavras, três grupos | Embeddings: palavras próximas em significado ficam próximas no espaço |
| 6 | **A malha que escolhe** — Iara | Leva um pacote por dez salas armadilhadas, e vê a malha abrir no fim | Um passe adiante de uma rede neural, do começo ao fim |
| 7 | **O tamanho do erro** — Seu Ilo | Mastermind com um número só de resposta | Função de perda: um número que não diz onde você errou |
| 8 | **Deixar a máquina treinar** — Rosa | Angry Birds: regula a força e assiste | Gradiente descendente e taxa de aprendizado |
| 9 | **O que ela come** — Chef Amaro | Prova cega cronometrada | Que o modelo é o que o corpus dele foi |
| 10 | **Onde ela olha** — Lumi | Duas lâmpadas para sete palavras | Atenção: gastar foco onde importa |
| 11 | **Fazer ela falar** — Vovó Zi | Labirinto de três escolhas por passo | Amostragem, temperatura e por que ela às vezes inventa |
| 12 | **Ensinar modos a ela** — Sereno | Papers, Please: julga caso a caso | Alinhamento: suas decisões viraram uma política |

No fim, o balcão do **certificado** emite um diploma em PDF gerado dentro do
próprio navegador — nada é enviado a servidor nenhum.

Cada bancada nasceu de um jogo conhecido, escolhido porque a estrutura dele já
carrega o conceito: Campo Minado para esparsidade, 2048 para fusão de tokens,
Mastermind para um sinal de erro sem direção, Flow Free para escassez de
atenção, Papers Please para política emergente.

---

## Onde isto encosta na BNCC

O jogo não foi escrito a partir da BNCC — foi escrito a partir de como uma IA de
linguagem funciona. Mas ele encosta em quatro Competências Gerais da Educação
Básica com bastante naturalidade, e é assim que ele costuma entrar no
planejamento:

| Competência Geral | Como aparece na aula |
|---|---|
| **5 — Cultura Digital** | O eixo do projeto. O aluno não *usa* uma IA: ele monta as peças dela e vê que não há mágica em etapa nenhuma. É compreensão crítica de uma tecnologia que ele já usa todo dia. |
| **2 — Pensamento científico, crítico e criativo** | Sete das doze bancadas são hipótese e teste com recurso escasso: aposta na bancada 6, medição na 7, força na 8, dedução por evidência na 9. O aluno formula, testa e corrige. |
| **7 — Argumentação** | As bancadas 9 e 12 terminam em pergunta que não tem resposta no jogo — "que dados essa IA leu?", "quem escolheu por ela?" — e é aí que a discussão de sala começa. |
| **10 — Responsabilidade e cidadania** | A bancada 12 mostra que a personalidade do modelo veio de escolhas humanas repetidas. Alinhamento deixa de ser assunto técnico e vira assunto de quem decide. |

**Sobre as habilidades específicas:** este README não lista códigos (EF/EM) de
propósito. A habilidade certa depende do ano, do componente e do recorte que
você vai dar — a mesma bancada 4 serve a Língua Portuguesa falando de morfologia
e a Matemática falando de otimização. Escolha os códigos do seu contexto; o
material não amarra você a um.

---

## Rodando

### Jogar
Abra o link no topo. Funciona em qualquer navegador moderno; não instala nada e
não pede login. O primeiro carregamento baixa alguns MB — a partir do segundo,
fica em cache.

### Abrir o projeto
```
Unity 6000.5.2f1 (Unity 6.5)
```
Abra a pasta `FabriacaAI/` pelo Unity Hub. **A cena não está montada à mão:**
todo o mundo — telas, botões, layout — é construído por código a partir de
`ConstrutorDeCena.cs`. Isso é deliberado: prefabs e cenas em disco viram
conflito de merge ilegível e escondem a lógica de layout.

### Compilar pela linha de comando
Com o Editor **fechado**:

```bash
scripts/unity.sh conferir   # só compila os scripts C# e relata
scripts/unity.sh cena       # regenera Assets/Scenes/Atelie.unity
scripts/unity.sh web        # gera a cena se faltar e compila para navegador
```

A saída vai para `Build/Web/`. O script descobre a versão do Unity lendo
`ProjectSettings/ProjectVersion.txt`, então não quebra no próximo salto de
versão.

Com o Editor **aberto**, para saber se a última compilação passou limpo:

```bash
scripts/unity-status.sh --agora
```

### Regerar a arte e a rede
Toda a arte é gerada por script — não há artista nem arquivo binário editado à
mão. Precisa só de Node, sem dependências externas:

```bash
node scripts/arte/gerar.mjs      # escreve os atlas em Assets/Resources/Arte
node scripts/rede/treinar.mjs    # treina a rede e escreve Assets/Resources/Dados/rede.json
```

---

## Notas de implementação

**WebGL com Brotli em hospedagem estática.** Um build Brotli espera que o
servidor mande `Content-Encoding: br`. O GitHub Pages não permite configurar
cabeçalho nenhum, então o navegador receberia bytes comprimidos sem ser avisado
e a página ficaria presa no loader para sempre — sem mensagem de erro útil. Por
isso `decompressionFallback` fica ligado: o Unity embute um descompressor em
JavaScript e o próprio jogo descomprime o que baixou. Custa um pouco de tempo de
partida e funciona em qualquer servidor de arquivo estático.

**Compressão e stripping agressivos.** A escola-alvo tem link ruim e máquina
velha. IL2CPP em `Master`, stripping em `High`, exceções desligadas: um build de
60 MB não abre antes do sinal tocar.

**Sem servidor, sem telemetria, sem conta.** O jogo roda inteiro na aba do
aluno. O diploma em PDF é montado em memória e entregue por um `Blob` local.
Nenhum dado sai da máquina.

---

## Contribuindo

Pull requests são bem-vindos, em especial:

- **Bancadas novas ou repensadas.** O teste é sempre o mesmo: dá para entender
  jogando, sem ler? Se precisa de legenda, ainda não está pronta.
- **Traduções.** Todo o texto de tela está no código das bancadas.
- **Relatos de sala de aula.** Se você deu esta aula, o que travou? Que bancada
  a turma não entendeu? Isso vale mais que código.

Convenções: código e comentários em português; comentário explica *por que*, não
*o quê*; arquivos abaixo de 500 linhas; cartazes de tela com no máximo 3-4
linhas.

## Licença

MIT — veja [LICENSE](LICENSE). Use em sala, adapte, redistribua. Se der esta
aula, eu adoraria saber como foi.
