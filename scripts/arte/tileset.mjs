/**
 * O tileset do ateliê: 16 pixels por tile, atlas de 8 colunas.
 *
 * Cada tile é uma função que pinta numa tela de 16x16. O `gerar.mjs` monta o
 * atlas e escreve, junto, um manifesto com nome → índice. O C# consulta o
 * manifesto por nome; ninguém no jogo escreve o número 13 esperando que ainda
 * seja o canteiro depois da próxima mexida na arte.
 *
 * Convenção de leitura, para quem for editar: a luz vem de cima e da esquerda.
 * Todo volume clareia na quina superior esquerda e escurece na inferior
 * direita. Manter isso é o que impede o cenário de parecer recortado e colado.
 */

import { Tela, aleatorio } from './png.mjs';
import { P, escurecer, clarear, misturar } from './paleta.mjs';

export const LADO = 16;

// ------------------------------------------------------------------- piso

/** A cor da tábua. Clara o bastante para o elenco escuro pousar em cima e ler. */
const TABUA = P.madeiraClara;

/**
 * Tábua corrida.
 *
 * O primeiro corte deste tile tinha emenda vertical em toda tábua e contraste
 * alto nas juntas — de longe o chão virava parede de tijolo. O conserto foi
 * tirar contraste das emendas e deixar a maioria das variantes SEM junta
 * vertical: tábua de ateliê é comprida, atravessa a sala inteira, e é a junta
 * curta e repetida que denuncia a grade.
 *
 * `junta` em -1 significa tábua inteira passando reto.
 */
function piso(t, semente, junta) {
  const r = aleatorio(semente);
  t.retangulo(0, 0, LADO, LADO, TABUA);

  // Duas tábuas por tile. A emenda é um risco escuro discreto com uma linha de
  // luz logo abaixo — é o degrau de luz que faz ler como duas peças de madeira.
  for (const y of [0, 8]) {
    t.linhaH(0, y, LADO, escurecer(TABUA, 0.22));
    t.linhaH(0, y + 1, LADO, clarear(TABUA, 0.1));
  }

  // Veio: riscos longos e de baixo contraste, sempre no sentido da tábua.
  for (let i = 0; i < 6; i++) {
    const y = 2 + Math.floor(r() * 5) + (i % 2) * 8;
    const x = Math.floor(r() * LADO);
    const comprimento = 4 + Math.floor(r() * 7);
    const cor = r() < 0.55 ? escurecer(TABUA, 0.1) : clarear(TABUA, 0.07);
    for (let k = 0; k < comprimento; k++) t.ponto((x + k) % LADO, y, cor);
  }

  if (junta < 0) return;
  t.linhaV(junta % LADO, 1, 7, escurecer(TABUA, 0.3));
  t.ponto(junta % LADO, 1, escurecer(TABUA, 0.16));
}

/**
 * Tábua com nó na madeira.
 *
 * Entra raro, como acidente. Na primeira versão o nó era escuro e grande e o
 * chão inteiro virou bolinha em grade regular — num tileset, quanto mais forte
 * o detalhe, mais raro ele tem que ser.
 */
function pisoNo(t) {
  piso(t, 77, -1);
  const cx = 6;
  const cy = 10;
  t.disco(cx, cy, 2.2, escurecer(TABUA, 0.16));
  t.disco(cx, cy, 1.2, escurecer(TABUA, 0.26));
  t.ponto(cx - 1, cy - 1, clarear(TABUA, 0.12));
}

/** Tábua gasta de tanto passo. Espalhada, ela abre caminho entre as bancadas. */
function pisoGasto(t) {
  piso(t, 31, -1);
  for (let y = 0; y < LADO; y++) {
    for (let x = 0; x < LADO; x++) {
      const p = t.ler(x, y);
      const hex = `#${p.slice(0, 3).map(v => v.toString(16).padStart(2, '0')).join('')}`;
      t.ponto(x, y, clarear(hex, 0.1));
    }
  }
}

/**
 * Ladrilho de oficina: em volta das bancadas, o chão vira pedra lisa.
 *
 * A pedra da paleta é azulada e, chapada em volta de doze bancadas, virava um
 * tapete azul plantado no meio da madeira. Aqui ela vem puxada para o marrom,
 * como nas paredes, e com pouquíssimo contraste contra o piso: o ladrilho tem
 * que dizer "área de trabalho" pelo canto do olho, não roubar a cena da bancada
 * que está em cima dele.
 */
function ladrilho(t, claro) {
  const base = misturar(claro ? P.pedraClara : P.pedra, P.madeira, 0.62);
  t.retangulo(0, 0, LADO, LADO, base);
  t.contorno(0, 0, LADO, LADO, escurecer(base, 0.22));
  t.linhaH(1, 1, LADO - 2, clarear(base, 0.12));
  t.linhaV(1, 1, LADO - 2, clarear(base, 0.12));
  // Um respingo de graxa: ladrilho perfeito parece render, não oficina.
  const r = aleatorio(claro ? 5 : 9);
  for (let i = 0; i < 5; i++) {
    t.ponto(2 + Math.floor(r() * 12), 2 + Math.floor(r() * 12), escurecer(base, 0.12));
  }
}

// ---------------------------------------------------------------- paredes

/**
 * Parede viva: treliça de madeira com trepadeira subindo.
 *
 * Vista de cima, uma parede precisa de duas faces para ler como parede — o topo
 * (que a câmera vê de chapa) e a frente (que encara o jogador). Sem a face da
 * frente o salão parece uma planta baixa impressa.
 */
function paredeFrente(t, comTrepadeira) {
  // Topo da parede: 5 pixels de pedra, com a quina iluminada. É a mesma pedra
  // das laterais de propósito — parede da frente com madeira clara demais fazia
  // o salão parecer dois prédios encostados.
  t.retangulo(0, 0, LADO, 5, clarear(PEDRA_QUENTE, 0.2));
  t.linhaH(0, 0, LADO, clarear(PEDRA_QUENTE, 0.4));
  t.linhaH(0, 4, LADO, escurecer(PEDRA_QUENTE, 0.35));

  // Frente: pedra na sombra com a treliça de madeira aplicada por cima.
  t.retangulo(0, 5, LADO, LADO - 5, escurecer(PEDRA_QUENTE, 0.25));
  for (let i = 0; i < 11; i++) {
    t.ponto(i + 1, 5 + i, P.madeira);
    t.ponto(LADO - 2 - i, 5 + i, P.madeira);
  }
  t.linhaH(0, LADO - 1, LADO, escurecer(PEDRA_QUENTE, 0.5));

  if (!comTrepadeira) return;
  const r = aleatorio(1234);
  for (let i = 0; i < 14; i++) {
    const x = Math.floor(r() * LADO);
    const y = 5 + Math.floor(r() * (LADO - 6));
    const cor = [P.folha, P.folhaClara, P.folhaLuz][Math.floor(r() * 3)];
    t.ponto(x, y, cor);
    if (r() < 0.5) t.ponto(x + 1, y, escurecer(cor, 0.2));
    if (r() < 0.35) t.ponto(x, y + 1, escurecer(cor, 0.3));
  }
}

/**
 * Parede vista só de topo: laterais e fundo do salão.
 *
 * A pedra da paleta é azulada, e num salão de madeira quente ela brigava — as
 * laterais pareciam de outro prédio. Aqui ela vem puxada para o marrom e mais
 * escura que o piso: parede escura fecha o ambiente e faz o chão claro e o
 * elenco saltarem, que é exatamente a ordem de leitura que este jogo quer.
 */
const PEDRA_QUENTE = misturar(P.pedra, P.madeiraFundo, 0.55);

function paredeTopo(t, comTrepadeira) {
  t.retangulo(0, 0, LADO, LADO, PEDRA_QUENTE);
  t.linhaH(0, 0, LADO, clarear(PEDRA_QUENTE, 0.22));
  t.linhaV(0, 0, LADO, clarear(PEDRA_QUENTE, 0.1));
  t.linhaH(0, LADO - 1, LADO, escurecer(PEDRA_QUENTE, 0.4));
  t.linhaV(LADO - 1, 0, LADO, escurecer(PEDRA_QUENTE, 0.3));

  // Blocos de pedra: duas fiadas por tile, desencontradas.
  const r = aleatorio(88);
  t.linhaH(0, 8, LADO, escurecer(PEDRA_QUENTE, 0.32));
  t.linhaV(6, 1, 7, escurecer(PEDRA_QUENTE, 0.28));
  t.linhaV(12, 9, 6, escurecer(PEDRA_QUENTE, 0.28));
  for (let i = 0; i < 8; i++) {
    t.ponto(1 + Math.floor(r() * 14), 1 + Math.floor(r() * 14), escurecer(PEDRA_QUENTE, 0.14));
  }

  if (!comTrepadeira) return;
  for (let i = 0; i < 12; i++) {
    const x = Math.floor(r() * LADO);
    const y = Math.floor(r() * LADO);
    t.ponto(x, y, r() < 0.5 ? P.folha : P.folhaClara);
    if (r() < 0.4) t.ponto(x, y + 1, P.folhaFundo);
  }
}

/** Rodapé de sombra: cola sob a parede e assenta o cenário no chão. */
function sombraDeParede(t) {
  for (let y = 0; y < 6; y++) {
    const a = Math.round(90 * (1 - y / 6))
      .toString(16)
      .padStart(2, '0');
    t.linhaH(0, y, LADO, `${P.tinta}${a}`);
  }
}

// ------------------------------------------------------- canteiro e props

/** Canteiro: terra escura com brotos. Bloqueia passagem, então precisa ler como obstáculo. */
function canteiro(t) {
  const terra = misturar(P.madeiraFundo, P.tinta, 0.25);
  t.retangulo(0, 0, LADO, LADO, terra);
  // Borda de madeira, que é o que diz "não pise".
  t.contorno(0, 0, LADO, LADO, P.madeiraClara);
  t.linhaH(1, 1, LADO - 2, P.madeiraLuz);
  t.linhaH(1, LADO - 2, LADO - 2, escurecer(P.madeira, 0.4));

  const r = aleatorio(404);
  for (let i = 0; i < 6; i++) {
    const x = 3 + Math.floor(r() * 10);
    const y = 4 + Math.floor(r() * 8);
    const alto = 2 + Math.floor(r() * 3);
    t.linhaV(x, y - alto, alto, P.folha);
    t.ponto(x - 1, y - alto, P.folhaClara);
    t.ponto(x + 1, y - alto - 1, P.folhaLuz);
    if (r() < 0.3) t.ponto(x, y - alto - 2, P.broto);
  }
}

/**
 * Claraboia do teto, desenhada por cima de tudo.
 *
 * Na primeira versão era um vidro opaco e virava um quadrado azul plantado no
 * chão — o aluno lia como tapete, não como teto. O que resolve não é desenhar
 * melhor o vidro: é quase apagá-lo. Sobra a moldura magra e o facho de luz, e
 * aí o cérebro entende sozinho que a fonte está acima.
 */
function painel(t) {
  // Sem preenchimento: qualquer véu chapado, por mais fraco, acinzenta o que
  // passa por baixo — e o que passa por baixo é o elenco.
  t.contorno(0, 0, LADO, LADO, `${P.latão}3a`);
  t.linhaV(5, 1, LADO - 2, `${P.luzForte}12`);
  t.linhaV(10, 1, LADO - 2, `${P.luzForte}12`);
  // O brilho diagonal é o que faz ler como vidro e não como grade.
  for (let i = 0; i < 9; i++) t.ponto(3 + i, 12 - i, `${P.luzForte}30`);
  for (let i = 0; i < 5; i++) t.ponto(6 + i, 14 - i, `${P.luzForte}1e`);
}

/**
 * Poça de luz no chão, sob a claraboia.
 *
 * São três peças e não uma. Com um tile só, repetido num bloco 3x3, cada tile
 * traz o próprio gradiente centrado em si e a poça vira nove borrões em grade —
 * exatamente o defeito que a luz deveria disfarçar. Aqui o centro é forte, o
 * lado é médio e a quina é fraca, e o conjunto lê como uma mancha só.
 *
 * `centro` desloca o pico dentro do tile, para a poça continuar contínua quando
 * a peça está na borda do bloco.
 */
function poca(t, pico, centro = [7.5, 7.5]) {
  for (let y = 0; y < LADO; y++) {
    for (let x = 0; x < LADO; x++) {
      const d = Math.hypot(x - centro[0], y - centro[1]) / 20;
      const a = Math.max(0, Math.round(pico * (1 - d)));
      if (a > 0) t.ponto(x, y, `${P.luzForte}${a.toString(16).padStart(2, '0')}`);
    }
  }
}

/** Caixote empilhado: enche canto vazio e dá escala ao salão. */
function caixote(t) {
  t.retangulo(2, 3, 12, 12, P.madeiraClara);
  t.contorno(2, 3, 12, 12, escurecer(P.madeira, 0.45));
  t.linhaH(3, 4, 10, P.madeiraLuz);
  t.linhaH(3, 8, 10, escurecer(P.madeira, 0.3));
  for (let i = 0; i < 10; i++) {
    t.ponto(3 + i, 5 + Math.floor(i / 2) % 2, P.madeiraBrilho);
  }
  t.retangulo(3, 9, 10, 5, P.madeira);
  t.linhaH(3, 9, 10, P.madeiraLuz);
  t.sombrearBordas();
}

/** Vaso alto com samambaia. É o prop que mais quebra a horizontal do salão. */
function vaso(t) {
  t.retangulo(5, 10, 6, 5, P.brasa);
  t.linhaH(4, 9, 8, P.brasaClara);
  t.linhaH(5, 14, 6, escurecer(P.brasa, 0.4));
  const r = aleatorio(7);
  for (let i = 0; i < 12; i++) {
    const ang = (i / 12) * Math.PI - 0.15;
    const comp = 4 + r() * 3;
    for (let k = 0; k < comp; k++) {
      const x = 8 - Math.cos(ang) * k;
      const y = 9 - Math.sin(ang) * k;
      t.ponto(x, y, k > comp - 2 ? P.folhaLuz : P.folha);
    }
  }
}

// -------------------------------------------------------------- catálogo

/**
 * A ordem daqui define o índice no atlas. Acrescente no fim; não intercale,
 * ou as cenas já montadas apontam para o tile errado.
 */
export const TILES = [
  // Só uma das quatro variantes leva emenda vertical. A grade some assim.
  ['piso_a', t => piso(t, 11, -1)],
  ['piso_b', t => piso(t, 22, -1)],
  ['piso_c', t => piso(t, 33, 5)],
  ['piso_d', t => piso(t, 44, -1)],
  ['piso_no', pisoNo],
  ['piso_gasto', pisoGasto],
  ['ladrilho', t => ladrilho(t, false)],
  ['ladrilho_claro', t => ladrilho(t, true)],

  ['parede_frente', t => paredeFrente(t, false)],
  ['parede_frente_verde', t => paredeFrente(t, true)],
  ['parede_topo', t => paredeTopo(t, false)],
  ['parede_topo_verde', t => paredeTopo(t, true)],
  ['sombra_parede', sombraDeParede],

  ['canteiro', canteiro],
  ['painel', painel],
  ['luz_centro', t => poca(t, 0x30)],
  ['luz_lado', t => poca(t, 0x1c)],
  ['luz_quina', t => poca(t, 0x0e)],
  ['caixote', caixote],
  ['vaso', vaso]
];

/** Pinta um tile pelo nome, numa tela nova de 16x16. */
export function pintarTile(nome) {
  const item = TILES.find(([n]) => n === nome);
  if (!item) throw new Error(`tile desconhecido: ${nome}`);
  const t = new Tela(LADO, LADO);
  item[1](t);
  return t;
}
