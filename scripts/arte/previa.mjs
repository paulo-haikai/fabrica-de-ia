#!/usr/bin/env node
/**
 * Contact sheet ampliado, para conferir a arte com o olho.
 *
 *     node scripts/arte/previa.mjs [pasta-de-saida]
 *
 * Pixel art de 16 pixels não se avalia em tamanho real: erro de um pixel na
 * sombra some na miniatura e salta na tela do jogo. Aqui tudo sai em 5x, com
 * as peças separadas por um fundo neutro, e o ateliê montado num pedaço de
 * mapa de verdade — que é o único jeito de ver se os tiles casam nas emendas.
 */

import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

import { Tela } from './png.mjs';
import { P } from './paleta.mjs';
import { TILES, LADO as LADO_TILE, pintarTile } from './tileset.mjs';
import { BANCADAS, LADO as LADO_BANCADA, pintarBancada } from './bancadas.mjs';
import { folhaDe, pessoa, LARGURA as L_PESSOA, ALTURA as A_PESSOA } from './elenco.mjs';
import { pintarPeca } from './abertura.mjs';
import { ELENCO } from './gerar.mjs';

const AQUI = dirname(fileURLToPath(import.meta.url));
const SAIDA = process.argv[2] ?? join(AQUI, 'previa');

/** Amplia por repetição de pixel — nada de suavizar, ou perde a borda dura. */
function ampliar(tela, fator) {
  const nova = new Tela(tela.largura * fator, tela.altura * fator);
  for (let y = 0; y < tela.altura; y++) {
    for (let x = 0; x < tela.largura; x++) {
      const p = tela.ler(x, y);
      if (p[3] === 0) continue;
      nova.retangulo(x * fator, y * fator, fator, fator, p);
    }
  }
  return nova;
}

function fundo(tela, cor = P.tintaClara) {
  const nova = new Tela(tela.largura, tela.altura);
  nova.retangulo(0, 0, tela.largura, tela.altura, cor);
  nova.colar(tela, 0, 0);
  return nova;
}

/** Grade de peças com respiro entre elas. */
function contato(pecas, colunas, fator, respiro = 6) {
  const l = pecas[0].largura * fator;
  const a = pecas[0].altura * fator;
  const linhas = Math.ceil(pecas.length / colunas);
  const t = new Tela(colunas * (l + respiro) + respiro, linhas * (a + respiro) + respiro);
  t.retangulo(0, 0, t.largura, t.altura, P.tinta);
  pecas.forEach((peca, i) => {
    const x = respiro + (i % colunas) * (l + respiro);
    const y = respiro + Math.floor(i / colunas) * (a + respiro);
    t.retangulo(x, y, l, a, P.tintaClara);
    t.colar(ampliar(peca, fator), x, y);
  });
  return t;
}

/**
 * Um pedaço de ateliê montado de verdade: parede em volta, piso variado,
 * bancadas em cima e gente andando. É esta imagem que decide se a arte presta.
 */
function cenaDeTeste() {
  const largura = 22;
  const altura = 14;
  const t = new Tela(largura * LADO_TILE, altura * LADO_TILE);

  const pisos = ['piso_a', 'piso_b', 'piso_c', 'piso_d', 'piso_no', 'piso_gasto'];
  const cache = Object.fromEntries(TILES.map(([n]) => [n, pintarTile(n)]));

  for (let ty = 0; ty < altura; ty++) {
    for (let tx = 0; tx < largura; tx++) {
      const borda = tx === 0 || ty === 0 || tx === largura - 1 || ty === altura - 1;
      let nome;
      if (ty === 0) nome = 'parede_frente_verde';
      else if (borda) nome = tx % 3 === 0 ? 'parede_topo_verde' : 'parede_topo';
      else nome = pisos[(tx * 7 + ty * 3) % pisos.length];
      t.colar(cache[nome], tx * LADO_TILE, ty * LADO_TILE);
    }
  }

  // Sombra descendo da parede de cima: assenta o salão.
  for (let tx = 1; tx < largura - 1; tx++) t.colar(cache.sombra_parede, tx * LADO_TILE, LADO_TILE);

  // Luz dos painéis no chão.
  for (const [tx, ty] of [[4, 4], [11, 4], [17, 8]]) {
    t.colar(cache.luz_centro, tx * LADO_TILE, ty * LADO_TILE);
  }

  // Props encostados.
  t.colar(cache.caixote, 1 * LADO_TILE, 11 * LADO_TILE);
  t.colar(cache.caixote, 2 * LADO_TILE, 11 * LADO_TILE);
  t.colar(cache.vaso, 20 * LADO_TILE, 2 * LADO_TILE);
  t.colar(cache.canteiro, 1 * LADO_TILE, 2 * LADO_TILE);
  t.colar(cache.canteiro, 20 * LADO_TILE, 11 * LADO_TILE);

  // Seis bancadas, com o mestre de cada uma ao lado.
  const postos = [[3, 3], [9, 3], [15, 3], [3, 8], [9, 8], [15, 8]];
  postos.forEach(([tx, ty], i) => {
    t.colar(pintarBancada(i), tx * LADO_TILE, ty * LADO_TILE - 8);
    const mestre = folhaDe(pessoa(ELENCO[i + 1]));
    t.colar(mestre[0], (tx + 2) * LADO_TILE + 4, ty * LADO_TILE + 6);
  });

  // O jogador no meio, de costas, no meio de um passo.
  t.colar(folhaDe(pessoa(ELENCO[0]))[1 * 4 + 1], 11 * LADO_TILE, 10 * LADO_TILE);

  // Painéis solares por cima de tudo, como teto.
  for (const [tx, ty] of [[4, 4], [11, 4], [17, 8]]) {
    t.colar(cache.painel, tx * LADO_TILE, ty * LADO_TILE);
  }
  return t;
}

/**
 * A cena da abertura montada, num instante escolhido da tempestade.
 *
 * `escuro` de 0 a 1 é o quanto a tempestade já chegou — o mesmo número que o
 * jogo anima ao longo dos dezessete segundos. Ver a cena montada nos dois
 * extremos é o único jeito de julgar se a transição vale: peça por peça, tudo
 * parece bem.
 */
function cenaDeAbertura(escuro, companheiroCaido) {
  const L = 220;
  const A = 130;
  const t = new Tela(L, A);

  // Céu: o bom por baixo, o de tempestade por cima com opacidade.
  const bom = pintarPeca('ceu_bom');
  const ruim = pintarPeca('ceu_ruim');
  for (let x = 0; x < L; x++) {
    for (let y = 0; y < A; y++) {
      const fonteY = Math.min(bom.altura - 1, Math.floor((y / A) * bom.altura));
      t.ponto(x, y, bom.ler(x % bom.largura, fonteY));
    }
  }
  const alfa = Math.round(escuro * 255).toString(16).padStart(2, '0');
  for (let x = 0; x < L; x++) {
    for (let y = 0; y < A; y++) {
      const fonteY = Math.min(ruim.altura - 1, Math.floor((y / A) * ruim.altura));
      const p = ruim.ler(x % ruim.largura, fonteY);
      const hex = `#${p.slice(0, 3).map(v => v.toString(16).padStart(2, '0')).join('')}${alfa}`;
      t.ponto(x, y, hex);
    }
  }

  // Sol desbotando.
  if (escuro < 0.95) {
    const s = pintarPeca('sol');
    const aSol = Math.round((1 - escuro) * 220).toString(16).padStart(2, '0');
    for (let y = 0; y < s.altura; y++) {
      for (let x = 0; x < s.largura; x++) {
        const p = s.ler(x, y);
        if (p[3] === 0) continue;
        const hex = `#${p.slice(0, 3).map(v => v.toString(16).padStart(2, '0')).join('')}${aSol}`;
        t.ponto(L * 0.74 + x - s.largura / 2, A * 0.22 + y - s.altura / 2, hex);
      }
    }
  }

  // Torres, com as pás em ângulos diferentes.
  const mastro = pintarPeca(escuro > 0.5 ? 'mastro_ruim' : 'mastro_bom');
  const pa = pintarPeca(escuro > 0.5 ? 'pa_ruim' : 'pa_boa');
  for (const [k, tx, alt] of [[0, 0.12, 46], [1, 0.24, 34], [2, 0.88, 40]]) {
    const bx = Math.round(L * tx);
    const by = Math.round(A * 0.74);
    for (let y = 0; y < alt; y++) {
      for (let x = 0; x < mastro.largura; x++) {
        const p = mastro.ler(x, Math.floor((y / alt) * mastro.altura));
        if (p[3] > 0) t.ponto(bx + x - 1, by - y, p);
      }
    }
    for (let i = 0; i < 3; i++) {
      const ang = k * 0.7 + (i * Math.PI * 2) / 3;
      for (let d = 0; d < pa.largura; d++) {
        t.ponto(bx + Math.cos(ang) * d, by - alt + Math.sin(ang) * d,
                escuro > 0.5 ? '#6b7186' : '#e8d8b4');
      }
    }
  }

  // Colina.
  const colina = pintarPeca(escuro > 0.5 ? 'colina_ruim' : 'colina_boa');
  for (let x = 0; x < L; x++) {
    for (let y = 0; y < colina.altura; y++) {
      const p = colina.ler(Math.floor((x / L) * colina.largura), y);
      if (p[3] > 0) t.ponto(x, A * 0.58 + y, p);
    }
  }

  // Jogador e companheiro.
  const chao = Math.round(A * 0.80);
  t.colar(folhaDe(pessoa(ELENCO[0]))[3 * 4], Math.round(L * 0.40), chao - 24);

  const comp = pintarPeca(companheiroCaido ? 'companheiro_ruim' : 'companheiro_bom');
  if (companheiroCaido) t.colar(comp, Math.round(L * 0.56), chao - 12);
  else t.colar(comp, Math.round(L * 0.56), chao - 40);

  if (companheiroCaido) t.colar(pintarPeca('faisca'), Math.round(L * 0.56) + 20, chao - 20);

  // Chuva.
  if (escuro > 0.5) {
    const gota = pintarPeca('gota');
    for (let i = 0; i < 40; i++) {
      t.colar(gota, (i * 37) % L, (i * 53) % Math.round(A * 0.62));
    }
  }
  return t;
}

mkdirSync(SAIDA, { recursive: true });

const escrever = (nome, tela) => {
  writeFileSync(join(SAIDA, nome), tela.paraPng());
  console.log(`  ${nome.padEnd(18)} ${tela.largura}x${tela.altura}`);
};

console.log(`prévia em ${SAIDA}`);
escrever('tiles.png', contato(TILES.map(([n]) => pintarTile(n)), 6, 5));
escrever('bancadas.png', contato(BANCADAS.map((_, i) => pintarBancada(i)), 4, 4));
escrever(
  'elenco.png',
  contato(
    ELENCO.flatMap(p => {
      const f = folhaDe(pessoa(p));
      // Uma linha por pessoa: parado nas quatro direções, mais um passo.
      return [f[0], f[1], f[4], f[8], f[12], f[13]];
    }),
    6,
    4
  )
);
escrever('atelie.png', fundo(ampliar(cenaDeTeste(), 3), P.tinta));
escrever('abertura-dia.png', ampliar(cenaDeAbertura(0.05, false), 4));
escrever('abertura-raio.png', ampliar(cenaDeAbertura(0.95, true), 4));
