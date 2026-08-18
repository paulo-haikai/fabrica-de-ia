/**
 * A paleta do ateliê — 32 cores e nem uma a mais.
 *
 * Paleta travada é a decisão que mais faz arte gerada parecer arte desenhada.
 * Quando cada peça pode escolher qualquer cor, doze bancadas viram doze jogos
 * diferentes; quando todas puxam das mesmas 32, até um sprite improvisado às
 * pressas ainda pertence ao mesmo mundo.
 *
 * A escolha temática: madeira quente e planta viva para o ateliê (é uma estufa
 * onde se conserta uma máquina, não um laboratório frio), latão para tudo que é
 * mecanismo, e um azul-petróleo escuro fazendo as vezes de preto. Preto puro
 * nunca aparece — sombra colorida é o que dá o ar de ilustração em vez de
 * clip-art.
 */

export const P = {
  // --- estrutura: o quase-preto e os neutros ---
  tinta: '#1b1f2a',
  tintaClara: '#2c3242',
  pedra: '#454d61',
  pedraClara: '#5f6a80',
  neblina: '#8b95a8',

  // --- madeira: piso, bancadas, vigas ---
  madeiraFundo: '#4a3122',
  madeira: '#6b4630',
  madeiraClara: '#8a5c3d',
  madeiraLuz: '#a9784f',
  madeiraBrilho: '#c79a6b',

  // --- planta: trepadeira, canteiro, folhagem ---
  folhaFundo: '#20402c',
  folha: '#31633c',
  folhaClara: '#478f4c',
  folhaLuz: '#6ab55d',
  broto: '#9ad46f',

  // --- latão e cobre: mecanismo, tubos, engrenagem ---
  latãoFundo: '#6a4a1e',
  latão: '#9c7128',
  latãoClaro: '#c99a3c',
  latãoLuz: '#e8c463',

  // --- vidro e água: painel, frasco, tela ---
  vidroFundo: '#1d4650',
  vidro: '#2e7c86',
  vidroClaro: '#4fb0b0',
  vidroLuz: '#8fdcd2',

  // --- destaque: luz, faísca, o que pede o dedo do aluno ---
  luz: '#f2c14e',
  luzForte: '#ffe9a8',
  brasa: '#e0653f',
  brasaClara: '#f2926a',

  // --- pele e pano, para o elenco ---
  panoFrio: '#3d6a9c',
  panoRoxo: '#6b4a86',
  panoRosa: '#c05f7a',
  papel: '#efe3c8',
  branco: '#fdf8ee'
};

/**
 * As cores da abertura — a única cena ao ar livre do jogo.
 *
 * Não saem da paleta do ateliê de propósito. Lá dentro é madeira e latão à luz
 * de claraboia; aqui é céu, sol e campo, e depois tempestade. Usar os mesmos 32
 * tons faria a cena de fora parecer um cenário de dentro mal iluminado.
 *
 * Os valores vêm da versão web, que já foi jogada por duas turmas: o azul do céu
 * claro e o âmbar do horizonte são o que a sala reconheceu como "dia bom", e é
 * contra essa lembrança que a tempestade tem que trabalhar.
 */
export const ABERTURA = {
  ceuAltoBom: '#7fd4e8',
  ceuBaixoBom: '#ffd9a0',
  ceuAltoRuim: '#2b3a52',
  ceuBaixoRuim: '#4a4a63',
  colinaBoa: '#7cb069',
  colinaRuim: '#3f5c46',
  torreBoa: '#e8d8b4',
  torreRuim: '#6b7186',
  sol: '#fff0c2',
  clarao: '#fffaec',
  chuva: '#c8dcf0'
};

/** Tons de pele do elenco. Doze pessoas, doze aparências plausíveis. */
export const PELES = [
  ['#f0c9a4', '#d2a179', '#a87850'],
  ['#e0a878', '#bd8558', '#8f6238'],
  ['#c78c5c', '#a06d43', '#78502f'],
  ['#8d5a37', '#6f4527', '#52311b'],
  ['#5f3a24', '#4a2c1a', '#361f12'],
  ['#fadfc0', '#dcb896', '#b28f6d']
];

/** Cores de cabelo. */
export const CABELOS = [
  ['#2a2018', '#161009'],
  ['#4a3020', '#2f1d12'],
  ['#7a4a22', '#543014'],
  ['#a9682c', '#7d4a1c'],
  ['#c8b18a', '#a08a60'],
  ['#9aa0ad', '#6e737e'],
  ['#5c3a5e', '#3d2440'],
  ['#2f5f5c', '#1e403e']
];

/** Cores de roupa: sempre puxadas da paleta, nunca inventadas na hora. */
export const ROUPAS = [
  P.vidro,
  P.folha,
  P.panoFrio,
  P.brasa,
  P.panoRoxo,
  P.latão,
  P.panoRosa,
  P.pedra,
  P.folhaClara,
  P.vidroClaro,
  P.madeiraClara,
  P.luz
];

/** Escurece uma cor da paleta em direção à tinta. `q` de 0 a 1. */
export function escurecer(hex, q = 0.3) {
  return misturar(hex, P.tinta, q);
}

/** Clareia em direção ao branco quente. */
export function clarear(hex, q = 0.3) {
  return misturar(hex, P.branco, q);
}

export function misturar(a, b, q) {
  const pa = a.replace('#', '');
  const pb = b.replace('#', '');
  const canais = [0, 2, 4].map(i => {
    const va = parseInt(pa.slice(i, i + 2), 16);
    const vb = parseInt(pb.slice(i, i + 2), 16);
    return Math.round(va + (vb - va) * q)
      .toString(16)
      .padStart(2, '0');
  });
  return `#${canais.join('')}`;
}
