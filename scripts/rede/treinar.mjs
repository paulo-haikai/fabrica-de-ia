// Treina a rede neural da bancada 6 e grava os pesos para o jogo ler.
//
// POR QUE FORA DO JOGO. Treinar isto dentro do navegador custaria uns quarenta
// segundos de tela preta antes da primeira jogada, numa aula de noventa minutos.
// E não há nada a ganhar: o treino é determinístico, então o resultado é o mesmo
// toda vez. Mesma decisão da arte — gerar fora, versionar o resultado, e o jogo
// só lê.
//
// POR QUE PESOS DE VERDADE. A bancada mostra o brilho de cada conexão e de cada
// neurônio. Se os números fossem inventados, a animação seria decoração bonita
// mentindo sobre a coisa que ela existe para ensinar. Com estes pesos, o que o
// aluno vê acender é a conta que a rede realmente fez.
//
// A REDE. Pequena de propósito, e o "de propósito" tem um motivo de desenho:
// ela precisa CABER NA TELA com as conexões desenhadas uma por uma. Uma rede
// grande seria melhor de prever e impossível de mostrar, e mostrar é o serviço
// desta bancada.
//
//   janela de 3 palavras
//     -> cada palavra vira 6 números (a tabela de embutimento)
//        = 18 neurônios de entrada
//     -> 12 neurônios no meio, com ReLU
//     -> uma lâmpada por palavra do vocabulário, com softmax
//
// São 216 conexões na primeira camada — desenháveis. A segunda tem milhares, e
// a bancada é honesta sobre isso na tela em vez de fingir que não existem.

import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const AQUI = dirname(fileURLToPath(import.meta.url));
const RAIZ = join(AQUI, '..', '..');
const CORPUS = join(RAIZ, 'FabriacaAI', 'Assets', 'Resources', 'Dados', 'corpusEscolar.json');
const SAIDA = join(RAIZ, 'FabriacaAI', 'Assets', 'Resources', 'Dados', 'rede.json');

const JANELA = 3;
const EMBUTIMENTO = 6;
const MEIO = 12;

/**
 * Onde PARAR de treinar — e a escolha não é "o mais treinado possível".
 *
 * Acerto e dúvida trocam entre si, medidos ao longo do treino:
 *
 *     época 150   acerto 42%   dúvida 44%
 *     época 300   acerto 64%   dúvida 32%
 *     época 350   acerto 68%   dúvida 31%
 *     época 600   acerto 82%   dúvida 23%
 *
 * A rede de 600 épocas é a melhor de prever e a pior de ENSINAR. Ela decorou o
 * corpus, então responde com certeza quase absoluta: a segunda colocada vira
 * poeira, a parede de lâmpadas acende uma só, e o aluno aposta no óbvio e ganha
 * sem olhar a malha. Some a dúvida, some o jogo.
 *
 * Em 350 a rede ainda ganha de longe da régua do bigrama — 68 contra 42 —, um
 * terço das janelas fica em disputa de verdade, e a parede acende como
 * constelação em vez de holofote. É a única das duas que mostra que ela pesa
 * TUDO o que sabe.
 */
const EPOCAS = 350;
const PASSO_INICIAL = 0.08;

/**
 * O horizonte do cronograma do passo, que NÃO é o mesmo que onde o treino para.
 *
 * Os dois números vêm separados porque a primeira tentativa juntou-os e o
 * resultado foi o contrário do pretendido. O passo decai como
 * `1 - época / horizonte`; com o horizonte igual ao total, encurtar o treino de
 * 600 para 350 épocas fez o passo decair mais rápido, a rede convergiu MAIS em
 * menos épocas, e saiu com 86% de acerto e 22% de dúvida — pior para a bancada
 * que a de 600 épocas que eu estava tentando evitar.
 *
 * Mantendo o horizonte em 600 e parando em 350, o passo no fim do treino é o
 * mesmo que era na época 350 da corrida longa — que é o ponto medido, e o ponto
 * que a bancada quer.
 */
const HORIZONTE = 600;

// ------------------------------------------------------------------- sorteio

/** Mulberry32 — o mesmo gerador do jogo, para o treino ser reproduzível. */
function aleatorio(semente) {
  let a = semente >>> 0;
  return () => {
    a = (a + 0x6d2b79f5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

const sorteio = aleatorio(20260817);

/** Normal aproximada por soma de uniformes. Boa o bastante para inicializar. */
function normal(escala) {
  let s = 0;
  for (let i = 0; i < 6; i++) s += sorteio();
  return (s - 3) / 1.5 * escala;
}

// -------------------------------------------------------------------- dados

const corpus = JSON.parse(readFileSync(CORPUS, 'utf8'));
const frases = corpus.frases.map((f) => f.trim().split(/\s+/));

const vocabulario = [];
const indice = new Map();
for (const frase of frases) {
  for (const palavra of frase) {
    if (indice.has(palavra)) continue;
    indice.set(palavra, vocabulario.length);
    vocabulario.push(palavra);
  }
}

const V = vocabulario.length;

// Os exemplos: três palavras seguidas e a quarta como resposta.
//
// Só janelas inteiramente DENTRO de uma frase. Atravessar a fronteira entre duas
// frases ensinaria à rede uma emenda que ninguém escreveu — é o mesmo cuidado
// que a bancada 2 precisou aprender.
const exemplos = [];
for (const frase of frases) {
  for (let i = 0; i + JANELA < frase.length; i++) {
    exemplos.push({
      entrada: frase.slice(i, i + JANELA).map((p) => indice.get(p)),
      alvo: indice.get(frase[i + JANELA])
    });
  }
}

// ------------------------------------------------------------------ a rede

const ENTRADA = JANELA * EMBUTIMENTO;

// Tudo em vetores planos: é como o Unity vai ler (o JsonUtility não desserializa
// matriz de matriz), e evita converter formato na fronteira.
const E = new Float64Array(V * EMBUTIMENTO);   // tabela de embutimento
const W1 = new Float64Array(ENTRADA * MEIO);
const b1 = new Float64Array(MEIO);
const W2 = new Float64Array(MEIO * V);
const b2 = new Float64Array(V);

for (let i = 0; i < E.length; i++) E[i] = normal(0.6);
for (let i = 0; i < W1.length; i++) W1[i] = normal(Math.sqrt(2 / ENTRADA));
for (let i = 0; i < W2.length; i++) W2[i] = normal(Math.sqrt(2 / MEIO));

const x = new Float64Array(ENTRADA);
const h = new Float64Array(MEIO);
const z = new Float64Array(MEIO);
const saida = new Float64Array(V);

function adiante(entrada) {
  for (let p = 0; p < JANELA; p++) {
    const base = entrada[p] * EMBUTIMENTO;
    for (let k = 0; k < EMBUTIMENTO; k++) x[p * EMBUTIMENTO + k] = E[base + k];
  }

  for (let j = 0; j < MEIO; j++) {
    let soma = b1[j];
    for (let i = 0; i < ENTRADA; i++) soma += x[i] * W1[i * MEIO + j];
    z[j] = soma;
    // ReLU: soma negativa apaga o neurônio. É o que faz partes da malha ficarem
    // escuras na animação, e é verdade — não é licença artística.
    h[j] = soma > 0 ? soma : 0;
  }

  let maior = -Infinity;
  for (let v = 0; v < V; v++) {
    let soma = b2[v];
    for (let j = 0; j < MEIO; j++) soma += h[j] * W2[j * V + v];
    saida[v] = soma;
    if (soma > maior) maior = soma;
  }

  let total = 0;
  for (let v = 0; v < V; v++) {
    saida[v] = Math.exp(saida[v] - maior);
    total += saida[v];
  }
  for (let v = 0; v < V; v++) saida[v] /= total;
}

const dSaida = new Float64Array(V);
const dH = new Float64Array(MEIO);
const dX = new Float64Array(ENTRADA);

function atras(entrada, alvo, passo) {
  // Derivada da entropia cruzada com softmax: probabilidade menos o alvo. Esta
  // é a conta que a bancada 8 vai mostrar a máquina fazendo sozinha.
  for (let v = 0; v < V; v++) dSaida[v] = saida[v];
  dSaida[alvo] -= 1;

  dH.fill(0);
  for (let j = 0; j < MEIO; j++) {
    let soma = 0;
    const linha = j * V;
    for (let v = 0; v < V; v++) {
      soma += dSaida[v] * W2[linha + v];
      W2[linha + v] -= passo * dSaida[v] * h[j];
    }
    dH[j] = z[j] > 0 ? soma : 0;
  }
  for (let v = 0; v < V; v++) b2[v] -= passo * dSaida[v];

  dX.fill(0);
  for (let i = 0; i < ENTRADA; i++) {
    const linha = i * MEIO;
    let soma = 0;
    for (let j = 0; j < MEIO; j++) {
      soma += dH[j] * W1[linha + j];
      W1[linha + j] -= passo * dH[j] * x[i];
    }
    dX[i] = soma;
  }
  for (let j = 0; j < MEIO; j++) b1[j] -= passo * dH[j];

  for (let p = 0; p < JANELA; p++) {
    const base = entrada[p] * EMBUTIMENTO;
    for (let k = 0; k < EMBUTIMENTO; k++) E[base + k] -= passo * dX[p * EMBUTIMENTO + k];
  }
}

// ------------------------------------------------------------------- treino

/**
 * Acerto e DÚVIDA, medidos juntos — porque a bancada precisa dos dois.
 *
 * Acerto sozinho não diz se a bancada é jogável. Uma rede que decorou tudo acerta
 * muito e responde com certeza absoluta: a segunda colocada vira poeira, o aluno
 * aposta no óbvio e ganha sem olhar a malha. Aposta sem dúvida não é aposta.
 *
 * A dúvida aqui é a fração de janelas em que a segunda colocada tem ao menos 35%
 * da força da primeira. É o número que decide onde parar de treinar.
 */
function medir() {
  let certos = 0;
  let disputadas = 0;

  for (const ex of exemplos) {
    adiante(ex.entrada);

    let melhor = 0;
    for (let v = 1; v < V; v++) if (saida[v] > saida[melhor]) melhor = v;
    if (melhor === ex.alvo) certos++;

    let segunda = 0;
    for (let v = 0; v < V; v++) if (v !== melhor && saida[v] > segunda) segunda = saida[v];
    if (segunda / saida[melhor] >= 0.35) disputadas++;
  }

  return {
    acerto: certos / exemplos.length,
    duvida: disputadas / exemplos.length
  };
}

/**
 * A régua contra a qual a rede tem que ganhar: chutar sempre a palavra que mais
 * vezes seguiu a ÚLTIMA palavra da janela.
 *
 * É a bancada 2 em forma de número. Sem essa comparação eu não saberia dizer se
 * a rede aprendeu algo ou se só decorou o óbvio — e prometer "ela aprendeu" sem
 * medir seria a mesma desonestidade que a animação inventada.
 */
function reguaDoBigrama() {
  const contagem = new Map();
  for (const ex of exemplos) {
    const ultima = ex.entrada[JANELA - 1];
    if (!contagem.has(ultima)) contagem.set(ultima, new Map());
    const linha = contagem.get(ultima);
    linha.set(ex.alvo, (linha.get(ex.alvo) || 0) + 1);
  }

  let certos = 0;
  for (const ex of exemplos) {
    const linha = contagem.get(ex.entrada[JANELA - 1]);
    let melhor = -1;
    let maior = -1;
    for (const [alvo, n] of linha) if (n > maior) { maior = n; melhor = alvo; }
    if (melhor === ex.alvo) certos++;
  }
  return certos / exemplos.length;
}

const ordem = exemplos.map((_, i) => i);

console.log(`vocabulário: ${V} palavras · exemplos: ${exemplos.length}`);
console.log(`régua do bigrama: ${(reguaDoBigrama() * 100).toFixed(1)}% de acerto`);

for (let epoca = 0; epoca < EPOCAS; epoca++) {
  // Embaralha a cada época: exemplos sempre na mesma ordem fazem o passo andar
  // em ziguezague preso, e a rede para de melhorar cedo.
  for (let i = ordem.length - 1; i > 0; i--) {
    const j = Math.floor(sorteio() * (i + 1));
    [ordem[i], ordem[j]] = [ordem[j], ordem[i]];
  }

  const passo = PASSO_INICIAL * (1 - epoca / HORIZONTE) + 0.004;
  for (const i of ordem) {
    adiante(exemplos[i].entrada);
    atras(exemplos[i].entrada, exemplos[i].alvo, passo);
  }

  if (epoca % 50 === 49 || epoca === 0) {
    const m = medir();
    console.log(`  época ${String(epoca + 1).padStart(3)}: ` +
                `acerto ${(m.acerto * 100).toFixed(1)}%  ·  ` +
                `apostas com dúvida ${(m.duvida * 100).toFixed(1)}%`);
  }
}

const medida = medir();
const acerto = medida.acerto;
console.log(`rede treinada: ${(acerto * 100).toFixed(1)}% de acerto · ` +
            `${(medida.duvida * 100).toFixed(1)}% das janelas em dúvida`);

// -------------------------------------------------------------------- gravar

const arredondar = (v) => Math.round(v * 10000) / 10000;

mkdirSync(dirname(SAIDA), { recursive: true });
writeFileSync(SAIDA, JSON.stringify({
  janela: JANELA,
  embutimento: EMBUTIMENTO,
  meio: MEIO,
  vocabulario,
  embutir: Array.from(E, arredondar),
  pesos1: Array.from(W1, arredondar),
  vies1: Array.from(b1, arredondar),
  pesos2: Array.from(W2, arredondar),
  vies2: Array.from(b2, arredondar),
  acerto: arredondar(acerto),
  reguaBigrama: arredondar(reguaDoBigrama())
}));

console.log(`gravado: ${SAIDA}`);
