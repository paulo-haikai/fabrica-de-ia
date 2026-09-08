# Plano de rebalanceamento — bancadas 3 a 12

Alvo: **uma criança de 12 anos VENCE a bancada.** Não "joga e perde com dignidade" — vence.

Regras que o plano obedece:

- **Dificuldade ≠ punição.** Boa parte do alívio vem de tirar o custo de errar, não o desafio.
- **Piso sobe, teto fica.** Rodada extra opcional é melhor que rodada obrigatória mais fácil.
- **Andaime na rodada 1.** A primeira rodada de cada bancada deve ser quase impossível de perder e servir para ensinar o controle.
- **Menos texto na tela.** Nenhuma proposta aqui resolve dificuldade escrevendo instrução. Onde eu proponho texto, é *feedback do que acabou de acontecer*, no mesmo contrato do «"na lousa" — escrito 7 vezes» da b3.
- **A lição não se apaga.** Cada bancada existe para ensinar UM conceito; a seção "o que não pode mudar" é o contrato.

---

## Resumo executivo — o que eu achei

Três bancadas têm **rodadas que às vezes não têm solução nenhuma**. Não é dificuldade, é bug, e é a explicação mais provável para "ficou muito difícil da 3 em diante":

| # | Bancada | Defeito | Consequência |
|---|---------|---------|--------------|
| 1 | **b11 Fala** | O alvo é sorteado caminhando por **todas** as continuações; o aluno só vê as **3 mais prováveis** (`DesafioFala.cs:121-131` vs `:208`) | O caminho que gerou o alvo frequentemente não existe na tela. Rodada invencível. |
| 2 | **b9 Cozinha** | Nada garante que a amostra gerada contenha uma palavra-marca do prato (`DesafioCozinha.cs:139-145`) | Pode sair uma rodada sem nenhuma evidência para deduzir. |
| 3 | **b9 Cozinha** | `contaEstrela: certos >= 2` é código morto: numa bijeção de 3, os acertos possíveis são 0, 1 ou 3 (`DesafioCozinha.cs:299`) | Errar b9 **nunca** dá estrela, em nenhum caso. |

E dois lugares onde a matemática exige perfeição sem dizer:

| # | Bancada | Defeito |
|---|---------|---------|
| 4 | **b4 Fichas** | `Meta = MetaGulosa(...)` (`Fichas.cs:209`). O aluno tem que **empatar com o algoritmo guloso**, jogada por jogada, sem ver as contagens. |
| 5 | **b7 Erro** | `ErroBom = 0.06f` com 2 botões exige **acerto exato nos dois**: alvos e giros andam em passos de 0,5, então o menor erro não-nulo é 0,125 > 0,06 (`DesafioErro.cs:44`, `Afinacao.cs:59`). A rodada 1 — a do andaime — é a de tolerância mais severa das três. |

### Auditoria do andaime (rodada 1 hoje)

| Bancada | Rodada 1 é quase invencível? | Por quê |
|---------|------------------------------|---------|
| b3 Arquivo | **Quase** | 40% de casas cheias, 10 cliques, meta 3, exemplo grátis. Perder é raro. Único senão: perder a r1 não dá estrela. |
| b4 Fichas | **Não** | Exige 4 fusões gulosas perfeitas. |
| b5 Mapa | **Não** | Connections de 12 cartas com critério distribucional é o gênero mais duro do jogo. |
| b6 Malha | **Não** | Aposta só é pedida em janela **disputada** (`Rede.cs:201`). É cara-ou-coroa, e pede 2 de 3. |
| b7 Erro | **Não** | Tolerância exige acerto exato (item 5 acima). |
| b8 Treino | **Sim** | Janela de força vencedora ocupa ~63% da barra. É a melhor rodada 1 do jogo. |
| b9 Cozinha | **Não** | Tudo-ou-nada em permutação de 3; acaso = 1/6. |
| b10 Holofotes | **Não** | 4 perguntas para ~15 combinações, e a primeira costuma ser gasta no escuro. |
| b11 Fala | **Não** | Ver bug 1. |
| b12 Alinhar | **Sim** | Não tem derrota. Problema é volume de leitura, não dificuldade. |

---

# Bancada 3 — `DesafioArquivo.cs` (esparsidade)

### Por que está difícil aos 12

- **Carga cognitiva — média.** A leitura "palavra da linha + palavra da coluna" é uma operação de tabela de dupla entrada, que aos 12 ainda não é automática. O jogo já resolve isso duas vezes bem: as dicas "primeira palavra"/"…e a segunda" (`:184-196`) e o exemplo aberto de graça (`:252-274`). Não mexer nisso.
- **Dificuldade mecânica — baixa.** 24 casinhas, ~40% cheias, 10 cliques, meta 3. O aluno mediano acerta 4. A bancada está matematicamente saudável.
- **Pré-requisito — baixo.** As linhas vêm de `MaisMovimentadas()`, ou seja, palavras funcionais ("a", "o", "na", "para"), e as colunas são continuações reais delas. É português de 12 anos.

**Veredito: b3 é a bancada MENOS quebrada da lista.** O risco real é de variância — uma semente ruim baixa o preenchimento e o aluno perde a rodada que existe para ele ganhar. E a punição está mal colocada: perder a r1 dá zero estrela (`:356`), enquanto perder a r2 — que é a rodada *projetada* para ser perdida — dá estrela.

### A mudança proposta

1. `DesafioArquivo.cs:103` — `_meta = NivelAtual == 0 ? 3 : 1` → **`? 2 : 1`**.
   Com 40% de casas cheias e 10 cliques, P(≥2 acertos) passa de ~87% para ~97%. O aluno de 12 não perde a rodada 1.
2. `DesafioArquivo.cs:356` — o `Falhou` da rodada 1 passa a levar **`contaEstrela: true`**.
   Justificativa: a rodada 1 não é uma prova, é o andaime. Ela ensina a ler a tabela; quem chegou ao fim dos 10 cliques leu a tabela 10 vezes.
3. `DesafioArquivo.cs:201` e `:211` — tamanho de fonte dos rótulos de coluna 12 → **14**, e das linhas 13 → **15**. Com 4×6 há folga de pixel; hoje a palavra dentro da grade compete com a instrução do topo.

### O que NÃO pode mudar

- A rodada 2 **tem** que ser perdida. É a aposta de Aurélio, e é o argumento inteiro: fora do canto arrumado a tabela é vazia. Não subir o preenchimento dela, não baixar a meta dela abaixo de 1.
- A rodada 3 (a textura 318×318) é o clímax e não custa nada de dificuldade. Intocável.
- O exemplo grátis (`AbrirExemplo`) é o que substitui o parágrafo de instrução. Intocável.

### Custo: **baixo** (duas constantes e dois tamanhos de fonte).

---

# Bancada 4 — `DesafioFichas.cs` + `Fichas.cs` (tokenização / BPE)

### Por que está difícil aos 12

- **Dificuldade mecânica — ALTA, e é a causa principal.** `Fichas.cs:209`: `Meta = Oficina.MetaGulosa(alvo, fusoes)`. A vitória é `fichas <= Meta` (`DesafioFichas.cs:101`). Isso significa que o aluno precisa **igualar o algoritmo guloso jogada a jogada**: qualquer fusão que renda uma ocorrência a menos que a ótima e a rodada está perdida. Na rodada 3 são 8 fusões perfeitas seguidas em 6 palavras. Um adulto que conhece BPE erra isso.
- **Carga cognitiva — média-alta.** O placar (`:96-98`) tem **quatro** contadores numa linha só: fichas, meta, emendas, tesouradas. Aos 12, quatro números que mudam juntos não são lidos — são ignorados.
- **Pré-requisito — baixo.** Contar quantas palavras terminam em "ou" é acessível. O jogo até ajuda escondendo os pares que aparecem uma vez só (`:170`).
- **Feedback de progresso — ausente onde mais importa.** Depois de clicar numa emenda, o aluno não sabe se aquela emenda foi boa. Ele vê o total de fichas cair, mas não sabe se caiu *o quanto podia*. É exatamente o que faz desistir.

### A mudança proposta

1. **A folga na meta** — a mudança mais importante da bancada.
   `Fichas.cs:162-167` passa a carregar a folga junto:
   ```
   static readonly (int palavras, int fusoes, int folga)[] Rodadas =
   {
       (4, 4, 2),
       (5, 6, 2),
       (6, 6, 3)     // fusões 8 → 6 também
   };
   ```
   e `:209` vira `new Rodada4(alvo, fusoes, Oficina.MetaGulosa(alvo, fusoes) + folga)`.

   Efeito: o aluno pode errar **duas** emendas (rodada 1 e 2) ou **três** (rodada 3) e ainda vencer. A lição sobrevive intacta, porque quem gastar TODAS as emendas em pares de ocorrência única continua perdendo — a diferença entre 4 emendas boas e 4 emendas ruins é de 8 a 12 fichas, muito acima da folga.

2. **Rodada 3 encurtada**: 8 fusões → **6** (acima). Oito emendas em 6 palavras é a rodada mais longa do jogo inteiro e estoura o orçamento de 6 minutos sozinha.

3. **Feedback por emenda** — `Emendar()` (`:216-224`) passa a medir e dizer:
   ```
   var antes = _oficina.TotalFichas;
   _oficina.Fundir(a, b);
   var ganho = antes - _oficina.TotalFichas;
   Painel.Instruir($"“{a}{b}” — apagou {ganho} fichas de uma vez",
                   ganho >= 3 ? Cores.Folha : ganho >= 2 ? Cores.Luz : Cores.Neblina);
   ```
   Isso não é instrução escrita: é o mesmo contrato do «"na lousa" — escrito 7 vezes» da b3. É a lição da bancada dita pelo próprio gesto do aluno, no instante em que ele agiu. **Provavelmente vale mais que todas as outras mudanças de b4 somadas**, porque converte tentativa cega em aprendizado por rodada.

4. **Placar de quatro contadores → dois** (`:96-98`):
   ```
   _placar.text = $"fichas: {fichas}   ·   meta: {_rodada.Meta}";
   ```
   e as tesouradas descem para uma segunda linha própria, com o `Widgets.Contar` que já existe. As "emendas: X de Y" saem: são redundantes com as tesouradas e ninguém lê dois orçamentos.

5. **Andaime**: `Acabaram()` (`:247`) na **rodada 1** passa a levar `contaEstrela: true` incondicional.

### O que NÃO pode mudar

- **A contagem dos pares continua escondida.** É a decisão central da bancada (`:22-27`): com a contagem à vista, basta clicar no maior e não há leitura nenhuma. Nada aqui a expõe.
- **A tesourada continua sendo gasta mesmo ao desfazer** (`:234-238`). É o que dá peso ao clique e o que faz a bancada ser 2048 e não um sandbox.
- **A emenda vale para TODAS as palavras.** Sem isso não é tokenização.

### Custo: **médio** (1 e 2 são constantes; 3 e 4 mexem em `Redesenhar`/`Emendar`).

---

# Bancada 5 — `DesafioMapa.cs` + `Vizinhancas.cs` (embeddings)

### Por que está difícil aos 12

Esta é, junto com a b6, a bancada com a maior distância entre o público de 15-17 e o de 12.

- **Carga cognitiva — MUITO ALTA, causa principal.** Doze cartas na mesa, e cada carta carrega **duas linhas de vizinhança** com até 4 palavras (`:255-261`). São ~48 palavras de contexto na tela ao mesmo tempo, mais 12 nomes, mais 3 vagas, mais o contador de vidas. É a tela mais cheia do jogo. O espaço de busca é C(12,4) = 495 combinações.
- **Dificuldade mecânica — alta.** Connections é um quebra-cabeça de jornal *para adultos*. E aqui é pior que no NYT: o critério não é temático, é distribucional — ou seja, a intuição do aluno ("essas três são coisas de escola") trabalha **contra** ele. Isso é proposital e é a lição, mas com 3 vidas o custo de descobrir isso consome a rodada inteira.
- **Pré-requisito — médio.** "agrupe pela companhia, não pelo assunto" (`:89`) usa "companhia" num sentido que aos 12 não é imediato.
- **Orçamento de tempo — estourado.** Três tabuleiros de Connections não cabem em 6 minutos. Nem perto.

### A mudança proposta

1. **Grupos de 3 em vez de 4** — `Vizinhancas.cs:155`: `PorGrupo = 4` → **`3`**.
   A mesa cai de 12 para **9 cartas**, e o espaço de busca de 495 para C(9,3) = **84 combinações** — uma redução de 6×. E os grupos ficam *mais* coesos, não menos: `MaisParecidas(semente, PorGrupo-1)` passa a pegar as 2 vizinhas mais próximas em vez das 3, que são justamente as de cosseno mais alto. O parentesco com Connections está preservado: continua sendo "junte os que vão juntos, com vidas contadas".
2. **Vidas 3 → 5** — `DesafioMapa.cs:40`. Errar aqui é como o aluno *descobre* que o critério não é o assunto; três vidas não bastam para essa descoberta e ainda sobrar rodada. Ajustar o alarme do `Widgets.Contar` (`:129`) de 1 para 2.
3. **Duas rodadas obrigatórias + uma opcional** — `Vizinhancas.cs:154`: `PorAula = 3` → **`2`**, e depois da segunda vitória o cartaz oferece **"mais um tabuleiro?"** em vez de "voltar ao ateliê". Isso é o piso/teto: quem é rápido joga a terceira; quem não é, sai vencedor em 6 minutos.
   Consequência: `Estrelas()` precisa ser sobrescrito nesta bancada para que 2 de 2 valham 3 estrelas —
   ```
   protected override int Estrelas() => Resolvidos >= 2 ? 3 : Mathf.Clamp(Resolvidos, 0, 3);
   ```
4. **O "quase" persistente.** Hoje, ao errar, as cartas do melhor grupo recebem `Widgets.Lampejo` (`:319-320`) — um brilho de meio segundo que aos 12 passa despercebido. Passa a ser **uma marca que fica**: um pontinho dourado no canto da carta, apagado no próximo `Conferir`. É o "one away" do Connections, que é a razão de o Connections ser jogável. Não custa texto nenhum.
5. **Rótulo** (`:89`): "três grupos de quatro — agrupe pela companhia, não pelo assunto" → **"junte as que andam com as mesmas palavras"**. Menos texto, e o verbo em vez do conceito.
6. `Perder()` (`:342`) — `contaEstrela: _resolvidos.Count >= 2` → **`>= 1`**. Com grupos de 3, fechar um grupo já é ter lido a vizinhança certo uma vez.

### O que NÃO pode mudar

- **A vizinhança impressa na carta.** É o coração da bancada (`:248-254`): sem ela o aluno agrupa por significado e a máquina agrupou por lugar, e o quebra-cabeça fica injusto. Com 9 cartas ela cabe melhor, não pior.
- **O nome do grupo continua sendo a palavra-semente** ("aparecem onde 'lousa' aparece"), nunca um tema. Nomear por tema seria a tela mentindo sobre o que a máquina sabe.
- **Erro continua custando.** Sem custo, força bruta em 84 combinações resolve sem pensar.
- `Confundivel()` com limite 0,45 (`Vizinhancas.cs:245`) fica como está — é o que impede a trinca ambígua.

### Custo: **médio.** Itens 1, 2, 5, 6 são constantes (baixo). O 3 (rodada opcional) e o 4 (marca persistente) são fluxo.

---

# Bancada 6 — `DesafioMalha.cs` + `Rede.cs` (passe adiante)

### Por que está difícil aos 12

- **Dificuldade mecânica — ALTA, e de um tipo que não se resolve com esforço.** `NovoPasse` só pede aposta quando a janela está **disputada**: `EmDuvida()` é verdadeiro quando a segunda colocada tem ≥35% da força da primeira (`Rede.cs:201`, `:246`). Ou seja: **a bancada só pergunta nos casos em que a própria rede está em dúvida.** E pergunta *antes* de qualquer evidência aparecer — a luz atravessa depois da aposta (`:266-279`). O aluno não tem como acertar por mérito; ele tem 33% de acaso puro, talvez 45-50% se o palpite linguístico dele coincidir com o da rede, o que em janela disputada é justamente o que não acontece.
  Com ~45% de acerto, a rodada 1 pede 2 de 3: **P(vitória) ≈ 42%.** A rodada que deveria ser o andaime é uma moeda.
- **Carga cognitiva — média.** A animação de 2,75s em quatro fases é longa mas é o produto; não é o problema.
- **Pré-requisito — baixo.** As janelas vêm de frases reais do corpus (`Semear`, `:129-142`).

### A mudança proposta — opção A (recomendada, baixo risco)

1. **Mínimos** — `DesafioMalha.cs:61-66`:
   ```
   (3, 1),   // era (3, 2)
   (4, 2),
   (5, 3)
   ```
   Com 45% de acerto: rodada 1 sobe de 42% para **83%** de vitória, e o teto da rodada 3 não muda.
2. **A rodada 1 aposta em janela FÁCIL.** Hoje `EscreverAteDuvidar()` (`:206-226`) deixa a malha escrever sozinha enquanto ela tem certeza e só chama o aluno na dúvida. Na **rodada 1**, inverter: perguntar na primeira janela, disputada ou não.
   ```
   // em EscreverAteDuvidar, na primeira linha:
   if (NivelAtual == 0) return _rede.Prever(_frase);
   ```
   Efeito: nas janelas sem dúvida a vencedora é óbvia (a lâmpada dela chega muito mais forte, e o aluno vê isso na parede depois). A rodada 1 vira o que ela tem que ser — o lugar onde se aprende **o controle**: aposta, luz atravessa, lâmpada mais forte ganha. Sem loteria. A dúvida entra a partir da rodada 2, onde ela pertence, e aí ela ensina algo a mais: *às vezes ela tem certeza e às vezes não*.
3. `Fechar()` (`:356`) — `contaEstrela: _acertos > 0` fica como está; já é generoso.

### Opção B — o rebuild (custo alto, mas transforma a bancada)

Mover a aposta para **depois da fase "Meio"**. Hoje o aluno aposta às cegas e assiste; a animação é espetáculo sem conteúdo de decisão. Se a sequência virar

> entradas acendem → luz atravessa → **meio responde e a animação PARA** → aluno aposta → parede acende → veredito

então a aposta passa a ser **leitura de evidência**: os 12 neurônios do meio estão acesos ou apagados na tela, e é dali que a resposta sai. Isso converte um cara-ou-coroa numa habilidade, cumpre literalmente o que o cartaz final promete ("a resposta está espalhada em seis mil números, e só aparece quando todos somam") e resolve o problema dos 12 anos sem baixar nada.

Não recomendo fazer agora — é remontar a corrotina `Acendendo()` e o fluxo de `Apostar`/`Julgar`. Recomendo a opção A já e a B como evolução se sobrar tempo.

### O que NÃO pode mudar

- **A luz sai de todas as entradas ao mesmo tempo.** A imagem errada que a bancada existe para corrigir é a da bolinha achando caminho.
- **Peso negativo em turquesa, neurônio apagado escuro, parede com 318 lâmpadas.** São as três verdades da tela.
- **O placar comparativo do fim** (`Comparacao()`, `:366-368`): 68 contra 42, medidos no mesmo texto. É o fecho da aula até ali.
- **Não treinar a rede até 82%** (`:48-51`). Rede que decorou tira a dúvida e mata a aposta.

### Custo: **baixo** para a opção A (uma tabela e uma linha). **Alto** para a B.

---

# Bancada 7 — `DesafioErro.cs` + `Afinacao.cs` (função de perda)

### Por que está difícil aos 12

- **Dificuldade mecânica — ALTA, e por um motivo invisível.** `ErroBom = 0.06f` (`:44`). Os alvos são sorteados em passos de 0,5 dentro de [-4, 4] (`Afinacao.cs:59`) e os giros são de ±0,5 (`:123`, `:134`). Logo, o desvio de cada botão é sempre múltiplo de 0,5, e o erro é `média(desvio²)`. Com **2 botões**, o menor erro não-nulo possível é 0,25/2 = **0,125 — o dobro da tolerância**. Ou seja: **na rodada 1 é obrigatório acertar os dois botões exatamente.** Não há "quase".
  Pior: a tolerância é absoluta e o erro é uma média, então ela **afrouxa** conforme aumentam os botões — com 6 botões, um botão meio passo fora dá 0,0417 e **passa**. A rodada mais severa das três é a primeira. Está invertido.
- **Dificuldade mecânica, parte 2.** Alvo médio |a| ≈ 2 → 4 giros por botão. Dois botões, 8 giros mínimos, mais 2 medições para descobrir a direção de cada um, mais os erros de percurso: 12 medições é apertado para um aluno que dá um passo de cada vez.
- **Carga cognitiva — baixa.** A tela é limpa: barra, número, botões.
- **Pré-requisito — baixo.** "afinado/desafinado" é metáfora acessível.
- **Feedback de progresso — quase bom.** Existe "melhor erro até agora" (`:200`), mas ele é só um número no rodapé. E não existe como *voltar* ao melhor: o aluno que se perdeu tateando não tem como recuperar a melhor configuração, e assiste ao próprio erro piorar até as medições acabarem. Isso é punição pura — não ensina nada e é o momento exato de desistir.

### A mudança proposta

1. **Tolerância proporcional ao número de botões** — `:44` deixa de ser constante:
   ```
   /// Um botão meio passo fora é perdoado; dois não.
   float ErroBom => Mathf.Max(0.06f, 0.28f / _r.botoes);
   ```
   Valores: 2 botões → 0,14 · 4 botões → 0,07 · 6 botões → 0,06 (inalterado).
   Efeito: na rodada 1, deixar **um** botão meio passo fora passa a vencer (0,125 < 0,14). Deixar os dois fora, não (0,25). A rodada 3 fica exatamente como está — ela é para ser perdida.
2. **Amplitude do alvo menor na rodada 1.** `Mostrador` ganha um parâmetro:
   ```
   public Mostrador(int quantos, int semente, float desequilibrio = 1f, float amplitude = 4f)
   ...
   _alvo[i] = Mathf.Round((sorteio.Proximo() * 2f * amplitude - amplitude) * 2f) / 2f;
   ```
   e `DesafioErro.cs:60` passa **`amplitude: NivelAtual == 0 ? 2f : 4f`**.
   Efeito: na rodada 1, no máximo 4 giros por botão em vez de 8. Cabe folgado nas medições.
3. **Medições da rodada 1: 12 → 14** (`:36-41`). As outras duas ficam.
4. **"voltar ao melhor"** — um botão secundário que restaura a configuração de menor erro já medida, **sem gastar medição**. Guardar `float[] _melhorConfiguracao` junto com `_melhorErro` em `Medir()`.
   Isso é remover punição sem remover dificuldade: não diz qual botão está errado, não diz para que lado girar — só impede a espiral de perda. É o equivalente do "desfazer" da b4, e a bancada 7 é a única do jogo que não tem nenhum.
5. **Andaime**: `Desistir7()` (`:242`) — `contaEstrela: NivelAtual == Niveis - 1` → **`contaEstrela: NivelAtual != 1`** (rodada 1 e rodada 3 dão estrela mesmo perdendo; a 2 é a que cobra).

### O que NÃO pode mudar

- **A resposta continua sendo UM número.** Nada de "o botão 3 está alto". É a bancada inteira (`:16-22`).
- **"melhorou/piorou" é o teto do que se pode dizer** (`:159-164`). Já é o mínimo para tatear; não acrescentar direção.
- **A rodada 3, com 6 botões, é para ser perdida.** É o fracasso que dá sentido à b8. Não mexer em `(6, 16)`.
- **Girar é de graça, medir é que custa.** É o recurso escasso da bancada.

### Custo: **baixo** para 1, 2, 3, 5. **Médio** para 4 (guardar e restaurar configuração).

---

# Bancada 8 — `DesafioTreino.cs` + `Afinacao.cs` (descida de gradiente)

### Por que está difícil aos 12

Fiz a conta da convergência. Cada passo multiplica o desvio de um botão por `(1 − fração × curvatura)`; a vitória é o erro cair a 1% do inicial (`FracaoDaMeta = 0.01f`, `:95`). Daí sai a **janela de força que vence**, e daí a **janela de tempo de dedo**, já que a carga é linear em 1,15 s (`:79`, `:352`):

| Rodada | Curvaturas | Passos | Fração que vence | Faixa na barra | **Tempo de dedo** |
|--------|-----------|--------|------------------|----------------|-------------------|
| 1 (deseq 1) | 1,0 | 8 | 0,25 – 1,75 | 9% – 73% | 0,11 s – 0,84 s |
| 2 (deseq 2,5) | 1,0 – 2,5 | 8 | 0,24 – 0,73 | 9% – 30% | 0,10 s – 0,34 s |
| 3 (deseq 9) | 1,0 – 9,0 | 12 | 0,16 – 0,22 | 5,4% – 7,8% | **0,062 s – 0,090 s** |

- **Dificuldade mecânica — a rodada 1 é excelente e a rodada 3 é fisicamente impossível.** Uma janela de 28 milissegundos de aperto não é uma decisão, é sorte motora. Nem um adulto acerta de propósito. E a rodada 2 tem 3 tentativas para uma janela de 0,24 s.
- **A causa é o mapeamento linear da barra.** `Mathf.Lerp(ForcaMinima, ForcaMaxima, _carga)` espalha 0,03 a 2,4 uniformemente, mas tudo que interessa vive na primeira fração da faixa. A barra gasta 70% do seu comprimento em forças que só explodem.
- **Carga cognitiva — baixa.** Um quadro, uma barra, um botão. É a bancada mais limpa do jogo.
- **Pré-requisito — nenhum.** Angry Birds é conhecimento universal aos 12.
- **Feedback — muito bom.** A bolinha, a marca da tentativa anterior (`:301-304`), o tremor do quadro na explosão. Nada a consertar aqui.

### A mudança proposta

1. **Carga geométrica em vez de linear** — `:352`:
   ```
   // Cada pedaço da barra MULTIPLICA a força, não soma.
   // É como se escolhe taxa de aprendizado de verdade: 0,001 · 0,01 · 0,1.
   Lancar(ForcaMinima * Mathf.Pow(ForcaMaxima / ForcaMinima, _carga));
   ```
   Isso não facilita a rodada 1 (a janela dela continua enorme) e **triplica** a janela da rodada 3.
2. **Desequilíbrios menores** — `:59-66`:
   ```
   (4,  8, 1f,   4),
   (8,  8, 1.8f, 4),   // era 2.5f, 3 tentativas
   (8, 12, 5f,   5)    // era 9f, 4 tentativas
   ```
   Curvatura 5× continua desenhando dois vales visivelmente diferentes na tela — a lição é a **desigualdade**, não o número dela.
3. **Meta 1% → 2%** — `:95`: `FracaoDaMeta = 0.02f`.
4. **Tempo de carga 1,15 s → 1,8 s** — `:79`. Barra mais lenta = mais resolução no dedo.

Janelas resultantes: rodada 1 → 44% da barra (0,87 s de dedo). Rodada 2 → 33% (0,59 s). Rodada 3 → 23% (0,41 s), com 5 tentativas e a marca da anterior à vista. Todas jogáveis aos 12; nenhuma trivial.

### O que NÃO pode mudar

- **O teto acima de 1,0 tem que continuar alcançável.** Segurar demais **precisa** explodir e a bolinha precisa sair voando da tela (`:70-76`). É um terço da lição. Com o mapeamento geométrico, os últimos 25% da barra continuam divergindo — confirmado.
- **Poucos passos.** Com 24 passos qualquer força converge e o aluno nunca vê passar do ponto (`:50-57`).
- **A bolinha percorre as posições REAIS da descida de gradiente** (`Mostrador.TreinarObservando`). Nada de animação inventada.
- **Os dois vales da rodada 3 compartilham escala** (`:190-201`). Normalizar cada um apagaria a lição inteira.

### Custo: **baixo.** Quatro constantes e uma linha de fórmula.

---

# Bancada 9 — `DesafioCozinha.cs` (corpus)

> Nota de correção ao briefing: **esta bancada não é cronometrada.** Não há relógio no código. O problema é outro, e é mais sério.

### Por que está difícil aos 12

- **BUG de solubilidade.** `MontarPratos()` (`:139-145`) gera 3 frases por prato com `modelo.Gerar(_sorteio)` e aceita qualquer uma com ≥4 palavras. **Nada exige que a frase gerada contenha uma palavra-marca do prato.** E o modelo é treinado sobre um recorte que, se tiver menos de 12 frases, é completado com frases quaisquer do corpus (`:128-135`). Resultado: é perfeitamente possível cair uma rodada em que a máquina do "livro da horta" diz três frases sobre a professora. Aí a rodada é indecidível, e o aluno conclui — com razão — que é sorte.
- **BUG de estrela.** `contaEstrela: certos >= 2` (`:299`) é código morto. Numa bijeção de 3 elementos, os acertos possíveis são 0, 1 ou 3 — nunca 2. **Errar b9 nunca dá estrela, em nenhum cenário.**
- **Dificuldade mecânica — alta por tudo-ou-nada.** `certos == Pratos` (`:281`) ou nada. Acaso puro = 1/6. Sem tentativa nenhuma de correção.
- **Carga cognitiva — ALTA.** 3 amostras × 3 frases geradas por bigrama = 9 frases semi-sem-sentido por rodada, × 3 rodadas = **27 frases** para ler e comparar. É a bancada com mais texto do jogo, e o texto é de leitura difícil justamente por ser saída de modelo.
- **Orçamento de tempo — estourado.** Ler 27 frases de bigrama leva bem mais que 6 minutos.

### A mudança proposta

1. **Garantir a evidência.** Em `MontarPratos()` (`:139-145`), a amostra só aceita frase que carregue pelo menos uma marca do prato:
   ```
   for (var t = 0; t < 120 && amostra.Count < FrasesPorAmostra; t++)
   {
       var palavras = modelo.Gerar(_sorteio);
       if (palavras.Count < 4) continue;
       // A amostra tem que carregar evidência do prato, senão a rodada não
       // tem resposta dedutível — e o aluno acerta ou erra por sorte.
       if (!palavras.Any(p => marcas.Contains(p))) continue;
       ...
   }
   ```
   com um relaxamento na segunda metade das tentativas (aceitar sem marca) para nunca abrir a bancada com amostra vazia. **Isto é o equivalente ao `Holofotes.Solucao()` da b10** — o cuidado de nunca entregar nível sem solução, que as outras bancadas geradas já têm e esta não tem.
2. **Estrela real** — `:299`: `contaEstrela: certos >= 1`.
3. **Segunda prova.** Depois de `Conferir` com resultado errado, em vez de fechar a rodada: **"provar de novo"**, uma única vez, dizendo apenas *quantos* estão certos (nunca quais). É o feedback do Mastermind, e é o gênero da bancada. Só depois da segunda tentativa vem o gabarito.
   Com 1 acerto revelado, a dedução fecha: numa permutação de 3, saber que exatamente 1 está certo elimina metade do espaço.
4. **Duas rodadas, três frases → duas** — `:37` `Niveis => 2` e `:40` `FrasesPorAmostra = 2`. Isso corta a leitura de 27 frases para **12** e põe a bancada dentro do orçamento. Com `Estrelas()` sobrescrito para dar 3 em 2 de 2.

### O que NÃO pode mudar

- **Os três modelos são treinados de verdade, ali, na hora** (`:99-104`). É a honestidade da bancada: o aluno lê saída de três modelos distintos, não texto que eu escrevi imitando IA.
- **Um prato serve uma máquina só** (`:264-275`). É o que faz a dedução fechar.
- **Desfazer o palpite continua de graça** (`:250-258`). Mudar de ideia não pode custar.
- **Os recortes têm que ter vocabulário bem distinto entre si** (`:42-48`). É a condição da dedução ser possível.

### Custo: **médio.** Itens 1, 2 e 4 são baixos; o 3 (segunda prova) é fluxo.

---

# Bancada 10 — `DesafioHolofotes.cs` + `Holofotes.cs` (atenção)

### Por que está difícil aos 12

- **Dificuldade mecânica — média-alta.** `Perguntas = 4` (`:50`) fixo nas três rodadas, mas o espaço cresce: rodada 1 são C(6,2) ≈ 15 combinações, rodada 3 é C(7,3) = 35. Quatro perguntas contra 35 combinações, na rodada em que o aluno já está cansado.
- **Punição pura, e é a mais irritante do jogo.** `Perguntar()` (`:241-243`) incrementa `_perguntas` **sempre** — inclusive com **zero lâmpadas acesas**, e inclusive repetindo uma configuração já perguntada. O primeiro instinto de qualquer criança ao ver um botão "perguntar" é apertar para ver o que acontece. Ela perde 25% do orçamento da rodada por curiosidade, antes de entender a mecânica. E o próprio jogo até avisa que aquilo não vai dar em nada ("nenhuma palavra acesa — ela vai responder no escuro", `:120-122`) — e cobra assim mesmo.
- **Carga cognitiva — média.** A frase, a lacuna, os candidatos com barras, o placar. É legível.
- **Pré-requisito — baixo, e o desenho aqui é excelente.** A verificação é mecânica: a máquina acerta ou erra conforme o que está aceso, e o aluno descobre sozinho que "na"/"de"/"a" não servem. Nenhum texto ensina isso — a bancada ensina. Isto é o modelo do que as outras deveriam ser.
- **Feedback — muito bom.** As barras aparecem só depois de perguntar (`:172-184`): a informação é o que ele *compra* com a pergunta. Não mexer.

### A mudança proposta

1. **Pergunta sem lâmpada não conta** — `Perguntar()`:
   ```
   if (_acesas.Count == 0)
   {
       Painel.Instruir("ela não vê nada — acenda uma palavra", Cores.Brasa);
       Widgets.Tremer(_lacuna);
       return;                       // ANTES de _perguntas++
   }
   ```
   Deixar tentar e não cobrar: o aluno aprende que o escuro não responde, sem pagar por isso. Dificuldade zero perdida.
2. **Repetir a mesma configuração não conta.** Guardar a última `HashSet<int>` perguntada e, se for idêntica, só tremer. Repetir não traz informação nova; cobrar por isso é cobrar por um clique acidental.
3. **Perguntas decrescentes em vez de fixas** — `Perguntas` deixa de ser `const` e passa a vir de `Rodada10`, com `Holofotes.cs:137`:
   ```
   static readonly (int lampadas, int candidatos, int perguntas)[] Rodadas =
   { (2, 4, 6), (2, 5, 5), (3, 6, 5) };
   ```
   Seis perguntas para 15 combinações na rodada 1 é andaime de verdade; cinco para 35 na rodada 3 continua exigindo ler as barras.
4. **Rodada 1 com frase curta.** No filtro de `Holofotes.Sortear` (`:145-146`), a primeira rodada exige `p.Length >= 5 && p.Length <= 6` (em vez de `<= 9`). Menos palavras antes = menos combinações = andaime.
5. `Desistir10` (`:308`) — `contaEstrela: ultimoPalpite != null` fica. Já é o critério certo.

### O que NÃO pode mudar

- **As barras só depois de perguntar** (`:172-184`). Antes, a resposta fica exposta e perguntar vira decoração.
- **`Palpite` devolve `null` quando nada se associa** (`Holofotes.cs:100`). É o que impede acender só o artigo e acertar por sorte — e essa sorte destruía a lição inteira.
- **Palavras de ligação continuam clicáveis.** Descobrir que elas não servem *é* a lição. Bloqueá-las seria dar a resposta.
- **`Solucao()` por força bruta continua garantindo que cada rodada tem solução** (`Holofotes.cs:208-223`). É o padrão que a b9 e a b11 deveriam copiar.

### Custo: **baixo** para 1, 2, 4. **Médio** para 3 (mover `Perguntas` de constante para dado da rodada).

---

# Bancada 11 — `DesafioFala.cs` (amostragem)

### Por que está difícil aos 12

**Esta bancada está quebrada, e o defeito é de solubilidade.**

- **BUG.** `SortearRodada()` (`:98-145`) escolhe o alvo caminhando pelo grafo:
  ```
  var opcoes = _modelo.Continuacoes(atual)
                      .Where(c => c.Para != Bigrama.Fim && !visitadas.Contains(c.Para))
                      .ToList();
  atual = opcoes[(int)(_sorteio.Proximo() * opcoes.Count)].Para;
  ```
  — sorteio **uniforme entre TODAS as continuações**. Mas o aluno, em `DesenharEscolhas` (`:208`), só recebe `opcoes.Take(3)`: as **3 mais frequentes**.
  O comentário do método afirma "se foi a caminhada que produziu o alvo, então chegar nele é possível" (`:91-93`). **Isso é falso.** Palavras como "a" ou "o" têm dezenas de continuações; a chance de o passo sorteado estar entre os 3 mostrados é ~3/k por passo, e a caminhada tem 4 a 6 passos. Na maioria das rodadas o caminho gerador **não existe na tela**, e o aluno precisa torcer para haver outro caminho dentro do funil de 3 — o que frequentemente não há.
  Este é, na minha leitura, **o defeito mais grave do jogo inteiro**, e sozinho já justifica a queixa "ficou muito difícil".
- **Punição.** `opcoes.Count == 0` → derrota imediata (`:176-180`), "ela emudeceu". O aluno perde por ter escolhido uma palavra que o jogo ofereceu.
- **Carga cognitiva — baixa.** Três cartas, uma frase, um alvo. É limpo.
- **Pré-requisito — baixo.** Porcentagem e barra: aos 12, tranquilo.
- **Feedback de progresso — ausente.** O aluno não tem como saber se está se aproximando do alvo. Só descobre no passo final. É a definição de desistir sem saber por quê.

### A mudança proposta

1. **Gerar o alvo pelo MESMO funil que o aluno vê** — o conserto:
   ```
   // O aluno só recebe as três mais prováveis (ver DesenharEscolhas). Se a
   // caminhada que escolhe o alvo puder passar por uma quarta, o caminho que
   // produziu o alvo não existe na tela — e a rodada fica sem solução.
   var opcoes = _modelo.Continuacoes(atual)
                       .Where(c => c.Para != Bigrama.Fim && !visitadas.Contains(c.Para))
                       .Take(3)                       // <<< a única linha nova
                       .ToList();
   ```
   Isso torna **toda** rodada solúvel por construção — e faz `SortearRodada` cumprir o que a documentação dela já prometia.
   Cuidado de implementação: `Continuacoes` já vem ordenada por frequência (`Bigrama.cs:145-149`), então `Take(3)` é exatamente o conjunto da tela. Mas o filtro `!visitadas` roda **antes** do `Take` na geração e **não existe** na tela; para ficarem idênticos, tirar o `!visitadas` do `Take` e aplicá-lo depois, ou aceitar a pequena divergência (que só torna a geração mais conservadora, nunca menos).
2. **Nunca gerar caminho que passe por beco sem saída.** No mesmo laço, rejeitar candidato cujas continuações (fora `Fim`) sejam vazias. Junto com (1), elimina a derrota por "ela emudeceu" no caminho ótimo.
3. **Beco sem saída deixa de ser derrota** (`:176-180`). Vira **"volte uma palavra"**: desfaz o último passo e gasta um passo do orçamento. Punição proporcional à frequência do erro, que é o critério certo.
4. **Feedback de proximidade** — em cada uma das 3 cartas, uma marca discreta quando o alvo ainda é alcançável a partir dela dentro dos passos que sobram (busca em largura de profundidade ≤ passos restantes, no funil de 3; o grafo é minúsculo e isso custa microssegundos).
   Isto **não** é o botão verde que foi removido em `:216-224`, e o motivo da remoção não se aplica: aquilo pintava de verde a opção que *era* o alvo no passo final — ou seja, entregava o clímax. Aqui a marca aparece em 2 ou 3 das opções na maior parte do caminho e só converge no fim; ela responde "ainda dá?" e não "qual é?". É o que impede o aluno de descobrir só na última carta que estava perdido há quatro passos.
   Se o Paulo achar que é entrega demais, a alternativa mais barata é **mostrar a marca só quando NENHUMA das três leva ao alvo** — o aviso de "essa frase morreu", que hoje só chega no fim.
5. **Folga maior** — `:116`: `distancia = Mathf.Max(3, _passosRestantes - 2)` → **`- 3`**. Rodadas ficam (6 passos / alvo a 3), (7 / 4), (8 / 5).
6. `Perder()` (`:279`) — `contaEstrela: _frase.Count >= 4` → **`>= 3`**.

### O que NÃO pode mudar

- **Três opções, nunca mais** (`:194-203`). Vinte opções vira leitura de tabela.
- **A chance mostrada é a real**, contagem sobre total da linha, sem arredondamento simpático (`:240`).
- **Seguir sempre a mais provável não pode levar ao alvo com frequência.** É o coração da bancada: a diferença entre a palavra mais provável e a palavra que serve. A mudança (1) preserva isso, porque o alvo continua saindo de sorteio uniforme *entre as três*, não da mais provável.
- **O alvo tem que ser palavra de conteúdo** (`:135`).

### Custo: **baixo** para 1, 2, 5, 6 (é literalmente um `.Take(3)` e duas constantes). **Médio** para 3 e 4.

---

# Bancada 12 — `DesafioAlinhar.cs` (alinhamento)

### Por que está difícil aos 12

**Não está.** Esta bancada **não tem derrota**: as duas rodadas terminam em `Resolveu` (`:215`, `:318`), e o "teste" final tem dois botões que ambos vencem. O aluno não pode perder.

Os problemas são outros dois:

- **Pré-requisito de repertório — o real.** O eixo `Formalidade` pede que o aluno perceba a diferença entre *"Compreendo a preocupação. Sugiro identificar quais conteúdos concentraram os erros e revisá-los antes da próxima avaliação."* e *"Acontece. Vamos ver onde você tropeçou e arrumar isso."* (`:102-108`). Aos 15-17 isso é registro linguístico reconhecível; aos 12 é "duas respostas compridas que dizem a mesma coisa". Se ele não percebe o eixo, a escolha vira aleatória — e aí o teste final não reconhece nada dele, e o fecho da aula inteira sai morno.
- **Volume de leitura.** Seis blocos de até 200 caracteres em prosa formal, comparados dois a dois. É a segunda maior carga de leitura do jogo, depois da b9.
- **Recompensa injusta.** `Niveis => 2` (`:40`) com `Estrelas() => Clamp(Resolvidos, 0, 3)` significa que a **última bancada da aula dá no máximo 2 estrelas**. O aluno termina o ateliê com a sensação de ter perdido alguma coisa exatamente na hora do diploma.

### A mudança proposta

1. **Encurtar os `Mais` para ≤ 120 caracteres cada** (`:58-109`), mantendo o eixo. Exemplo do caso da prova:
   - Mais (formal): *"Compreendo. Sugiro revisar primeiro os conteúdos em que houve mais erros."*
   - Menos: *"Acontece. Vamos ver onde você tropeçou e arrumar isso."*
   A diferença de registro fica **mais** visível quando os dois têm o mesmo comprimento — hoje o eixo Formalidade está contaminado pelo eixo Tamanho, porque o "Mais" formal é sempre também o mais longo. Isso é um defeito de design, não só de leitura: **os três eixos não estão independentes**, e o aluno que escolhe "a mais curta" empurra três agulhas de uma vez.
2. **Igualar o comprimento dentro de cada par nos eixos Cautela e Formalidade.** Só o par do eixo `Tamanho` pode ter comprimentos diferentes — é a variável dele. Isso é o conserto de fundo: cada par passa a medir **um** eixo, como o `enum` promete.
3. **Três estrelas no fecho** — sobrescrever nesta bancada:
   ```
   protected override int Estrelas() => Resolvidos >= Niveis ? 3 : Resolvidos;
   ```
4. **Rodapé no teste final.** `"a resposta abaixo foi montada com as suas escolhas"` (`:226`) fica; é o que amarra tudo. Nada a acrescentar.

### O que NÃO pode mudar

- **Nenhuma das duas respostas está errada.** É o que faz a bancada ser Papers Please e não um quiz.
- **A ordem esquerda/direita é sorteada** (`:180-185`). Sem isso mede-se a mão, não o gosto.
- **A pergunta final é uma que ele nunca julgou** (`:112`). É a prova de que a política generaliza.
- **O fecho incômodo** — "quem escolheu, e o que essa pessoa preferia" (`:320-324`). É o fim da aula.

### Custo: **baixo** para 3. **Médio** para 1 e 2 (reescrever os seis casos, e isso é trabalho de conteúdo, não de código — cabe combinar com o narrative-director).

---

# Lista priorizada

Ordenada por **alívio de dificuldade ÷ risco**. Os cinco primeiros são todos de custo baixo e resolvem defeitos objetivos — nenhum deles é questão de gosto.

### Faixa 1 — consertos, não ajustes (faça hoje)

| # | Bancada | Mudança | Custo | Por que primeiro |
|---|---------|---------|-------|------------------|
| 1 | **b11** | `.Take(3)` na caminhada de `SortearRodada` | baixo | Uma linha. Torna solúvel uma bancada que hoje frequentemente não é. |
| 2 | **b9** | Amostra tem que conter marca do prato | baixo | Idem: elimina rodadas indecidíveis. |
| 3 | **b9** | `contaEstrela: certos >= 1` | baixo | Conserta código morto: hoje errar b9 nunca dá estrela. |
| 4 | **b4** | `Meta = MetaGulosa + folga` (2/2/3) | baixo | Deixa de exigir jogo perfeito. É o maior alívio isolado do plano. |
| 5 | **b7** | `ErroBom` proporcional aos botões + amplitude 2 na rodada 1 | baixo | Conserta a inversão: hoje a rodada 1 é a mais severa. |

### Faixa 2 — piso alto, teto intacto (faça em seguida)

| # | Bancada | Mudança | Custo |
|---|---------|---------|-------|
| 6 | **b6** | Mínimos (3,1)(4,2)(5,3) + rodada 1 aposta em janela sem dúvida | baixo |
| 7 | **b5** | `PorGrupo = 3` (mesa de 9 cartas) + `Vidas = 5` | baixo |
| 8 | **b8** | Carga geométrica + desequilíbrios 1,8 e 5 + `TempoDeCarga` 1,8 s + meta 2% | baixo |
| 9 | **b10** | Pergunta sem lâmpada (e repetida) não conta | baixo |
| 10 | **b3** | Meta da rodada 1 de 3 → 2; perder a rodada 1 dá estrela | baixo |
| 11 | **b12** | `Estrelas()` = 3 no fecho da aula | baixo |
| 12 | **b4** | Feedback por emenda ("apagou N fichas de uma vez") | médio |

### Faixa 3 — orçamento de tempo (6 min/bancada)

Três bancadas não cabem em 6 minutos hoje, e cortá-las alivia dificuldade e relógio ao mesmo tempo.

| # | Bancada | Mudança | Custo |
|---|---------|---------|-------|
| 13 | **b9** | 2 rodadas, 2 frases por amostra (leitura cai de 27 para 12 frases) | baixo |
| 14 | **b5** | 2 rodadas obrigatórias + 1 opcional, com `Estrelas()` sobrescrito | médio |
| 15 | **b4** | Rodada 3 de 8 para 6 fusões | baixo |
| 16 | **b10** | Perguntas 6/5/5 e frase curta na rodada 1 | médio |

### Faixa 4 — feedback de progresso (o que impede desistir)

| # | Bancada | Mudança | Custo |
|---|---------|---------|-------|
| 17 | **b5** | Marca persistente de "quase" nas cartas do melhor grupo | médio |
| 18 | **b7** | Botão "voltar ao melhor", sem gastar medição | médio |
| 19 | **b11** | Beco sem saída vira "volte uma palavra" em vez de derrota | médio |
| 20 | **b9** | "provar de novo", uma vez, dizendo só quantos estão certos | médio |
| 21 | **b11** | Marca de alcançabilidade nas 3 cartas | médio |

### Faixa 5 — conteúdo e rebuild (só se sobrar tempo)

| # | Bancada | Mudança | Custo |
|---|---------|---------|-------|
| 22 | **b12** | Reescrever os 6 casos: ≤120 caracteres e eixos independentes | médio (conteúdo) |
| 23 | **b6** | **Rebuild:** aposta depois da fase "Meio", não antes | alto |

---

## Duas observações que valem mais que a soma das mudanças

**Nenhuma bancada é insalvável aos 12.** A que eu mais temia — a b5, que é Connections com critério distribucional — fica no lugar certo com grupos de 3: 84 combinações em vez de 495, cinco vidas, e a vizinhança impressa continuando a fazer o trabalho.

**A b6 é a única cuja dificuldade não é ajustável por constante.** Ela pergunta exatamente nos casos em que a própria rede está em dúvida, e pergunta *antes* de mostrar qualquer evidência. Os itens 6 do plano fazem dela uma bancada que o aluno de 12 vence, mas ele vence sem saber por quê — e "não saber por que acertou" é o mesmo problema, do outro lado. Se em algum momento houver orçamento para um rebuild só, que seja o item 23: mover a aposta para depois de o meio da malha acender transforma sorte em leitura, e é a única mudança do plano que faz a bancada ensinar **mais** do que ensina hoje.
