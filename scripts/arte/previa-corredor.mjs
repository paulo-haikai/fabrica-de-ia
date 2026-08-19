/**
 * Amplia a folha do corredor num PNG grande, com fundo, para dar para OLHAR.
 *
 *     node scripts/arte/previa-corredor.mjs [destino.png]
 *
 * Arte de 16 pixels não se julga em 16 pixels. Sem esta prévia, o único jeito de
 * ver o que foi desenhado é compilar o jogo inteiro e chegar até a bancada 6.
 */

import { writeFileSync } from 'node:fs';
import { Tela } from './png.mjs';
import { P, escurecer } from './paleta.mjs';
import { PECAS, LADO, pintarPeca } from './corredor.mjs';

const ZOOM = 6;
const COLUNAS = 8;
const MARGEM = 4;

const celula = LADO * ZOOM + MARGEM * 2;
const linhas = Math.ceil(PECAS.length / COLUNAS);
const tela = new Tela(COLUNAS * celula, linhas * celula);

// Fundo xadrez discreto, para se enxergar o que é transparente na peça.
for (let y = 0; y < tela.altura; y++) {
  for (let x = 0; x < tela.largura; x++) {
    const claro = (Math.floor(x / 8) + Math.floor(y / 8)) % 2 === 0;
    tela.ponto(x, y, claro ? P.tinta : escurecer(P.tintaClara, 0.35));
  }
}

PECAS.forEach(([nome], i) => {
  const peca = pintarPeca(nome);
  const ox = (i % COLUNAS) * celula + MARGEM;
  const oy = Math.floor(i / COLUNAS) * celula + MARGEM;
  for (let y = 0; y < LADO; y++) {
    for (let x = 0; x < LADO; x++) {
      const [r, g, b, a] = peca.ler(x, y);
      if (a === 0) continue;
      for (let jy = 0; jy < ZOOM; jy++)
        for (let jx = 0; jx < ZOOM; jx++)
          tela.ponto(ox + x * ZOOM + jx, oy + y * ZOOM + jy, [r, g, b, a]);
    }
  }
  tela.contorno(ox - 1, oy - 1, LADO * ZOOM + 2, LADO * ZOOM + 2, escurecer(P.pedra, 0.2));
});

const destino = process.argv[2] || 'previa-corredor.png';
writeFileSync(destino, tela.paraPng());
console.log(`${destino}  ${tela.largura}x${tela.altura}  ${PECAS.length} peças`);
PECAS.forEach(([nome], i) => console.log(`  ${i}: ${nome}`));
