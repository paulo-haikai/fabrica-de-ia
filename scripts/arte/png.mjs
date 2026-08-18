/**
 * Escritor de PNG e uma tela de pixels, sem dependência nenhuma.
 *
 * A arte do jogo é código: cada tile e cada sprite nasce de uma função que
 * pinta pixel a pixel. Isso é proposital. Um PNG desenhado à mão vira um
 * binário opaco que ninguém revisa e que só uma pessoa sabe alterar; uma
 * função de 20 linhas se lê no diff, se ajusta em segundos e mantém as 200
 * peças do jogo dentro da mesma paleta sem esforço de disciplina.
 *
 * O encoder cobre só o que precisamos: RGBA de 8 bits, sem entrelace. O zlib
 * do próprio Node faz a compressão, então não há pacote a instalar.
 */

import { deflateSync } from 'node:zlib';

// ---------------------------------------------------------------- PNG cru

const TABELA_CRC = (() => {
  const t = new Uint32Array(256);
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    t[n] = c >>> 0;
  }
  return t;
})();

function crc32(buf) {
  let c = 0xffffffff;
  for (let i = 0; i < buf.length; i++) c = TABELA_CRC[(c ^ buf[i]) & 0xff] ^ (c >>> 8);
  return (c ^ 0xffffffff) >>> 0;
}

function bloco(tipo, dados) {
  const nome = Buffer.from(tipo, 'ascii');
  const corpo = Buffer.concat([nome, dados]);
  const saida = Buffer.alloc(corpo.length + 8);
  saida.writeUInt32BE(dados.length, 0);
  corpo.copy(saida, 4);
  saida.writeUInt32BE(crc32(corpo), corpo.length + 4);
  return saida;
}

/** Codifica RGBA cru (4 bytes por pixel, linha a linha) num arquivo PNG. */
export function codificarPng(largura, altura, rgba) {
  // Cada scanline leva um byte de filtro na frente; usamos 0 (nenhum), que é o
  // que comprime melhor em pixel art com grandes áreas de cor chapada.
  const linhas = Buffer.alloc(altura * (largura * 4 + 1));
  for (let y = 0; y < altura; y++) {
    const destino = y * (largura * 4 + 1);
    linhas[destino] = 0;
    rgba.copy(linhas, destino + 1, y * largura * 4, (y + 1) * largura * 4);
  }

  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(largura, 0);
  ihdr.writeUInt32BE(altura, 4);
  ihdr[8] = 8; // bits por canal
  ihdr[9] = 6; // RGBA
  // 10, 11 e 12 ficam em zero: deflate, filtro padrão, sem entrelace.

  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    bloco('IHDR', ihdr),
    bloco('IDAT', deflateSync(linhas, { level: 9 })),
    bloco('IEND', Buffer.alloc(0))
  ]);
}

// ------------------------------------------------------------------- tela

/** `#rrggbb` ou `#rrggbbaa` para os quatro canais. */
export function corDeHex(hex) {
  const s = hex.replace('#', '');
  return [
    parseInt(s.slice(0, 2), 16),
    parseInt(s.slice(2, 4), 16),
    parseInt(s.slice(4, 6), 16),
    s.length >= 8 ? parseInt(s.slice(6, 8), 16) : 255
  ];
}

/**
 * Uma superfície de pixels com o mínimo de ferramenta para desenhar sprites.
 *
 * Tudo escreve com mistura alfa simples, o que deixa sombrear com preto a 20%
 * ser uma linha só em vez de uma conta de cor à mão em cada chamada.
 */
export class Tela {
  constructor(largura, altura) {
    this.largura = largura;
    this.altura = altura;
    this.dados = Buffer.alloc(largura * altura * 4);
  }

  /** Pinta um pixel, misturando pelo alfa da cor que chega. */
  ponto(x, y, hex) {
    x = Math.round(x);
    y = Math.round(y);
    if (x < 0 || y < 0 || x >= this.largura || y >= this.altura) return;
    const [r, g, b, a] = typeof hex === 'string' ? corDeHex(hex) : hex;
    if (a === 0) return;
    const i = (y * this.largura + x) * 4;
    if (a === 255) {
      this.dados[i] = r;
      this.dados[i + 1] = g;
      this.dados[i + 2] = b;
      this.dados[i + 3] = 255;
      return;
    }
    const af = a / 255;
    const aDestino = this.dados[i + 3] / 255;
    const aFinal = af + aDestino * (1 - af);
    if (aFinal === 0) return;
    for (let c = 0; c < 3; c++) {
      this.dados[i + c] = Math.round(
        (([r, g, b][c] * af) + this.dados[i + c] * aDestino * (1 - af)) / aFinal
      );
    }
    this.dados[i + 3] = Math.round(aFinal * 255);
  }

  /** Lê um pixel como `[r, g, b, a]`. */
  ler(x, y) {
    if (x < 0 || y < 0 || x >= this.largura || y >= this.altura) return [0, 0, 0, 0];
    const i = (y * this.largura + x) * 4;
    return [this.dados[i], this.dados[i + 1], this.dados[i + 2], this.dados[i + 3]];
  }

  retangulo(x, y, l, a, hex) {
    for (let j = 0; j < a; j++) for (let i = 0; i < l; i++) this.ponto(x + i, y + j, hex);
  }

  /** Só a borda, um pixel de espessura. */
  contorno(x, y, l, a, hex) {
    for (let i = 0; i < l; i++) {
      this.ponto(x + i, y, hex);
      this.ponto(x + i, y + a - 1, hex);
    }
    for (let j = 0; j < a; j++) {
      this.ponto(x, y + j, hex);
      this.ponto(x + l - 1, y + j, hex);
    }
  }

  linhaH(x, y, l, hex) {
    for (let i = 0; i < l; i++) this.ponto(x + i, y, hex);
  }

  linhaV(x, y, a, hex) {
    for (let j = 0; j < a; j++) this.ponto(x, y + j, hex);
  }

  /** Disco cheio. Raio em pixels, centro podendo cair em meio pixel. */
  disco(cx, cy, raio, hex) {
    const r2 = raio * raio;
    for (let y = Math.floor(cy - raio); y <= Math.ceil(cy + raio); y++) {
      for (let x = Math.floor(cx - raio); x <= Math.ceil(cx + raio); x++) {
        const dx = x - cx;
        const dy = y - cy;
        if (dx * dx + dy * dy <= r2) this.ponto(x, y, hex);
      }
    }
  }

  /**
   * Desenha a partir de uma grade de caracteres, um caractere por pixel.
   * O ponto é transparente. É a forma mais legível de autorar um sprite
   * pequeno: o desenho aparece no próprio código-fonte.
   */
  grade(x, y, linhas, paleta) {
    for (let j = 0; j < linhas.length; j++) {
      for (let i = 0; i < linhas[j].length; i++) {
        const c = linhas[j][i];
        if (c === '.' || c === ' ') continue;
        const cor = paleta[c];
        if (cor) this.ponto(x + i, y + j, cor);
      }
    }
  }

  /** Copia outra tela por cima, respeitando transparência. */
  colar(outra, x, y) {
    for (let j = 0; j < outra.altura; j++) {
      for (let i = 0; i < outra.largura; i++) {
        const p = outra.ler(i, j);
        if (p[3] > 0) this.ponto(x + i, y + j, p);
      }
    }
  }

  /** Espelha na horizontal. Serve para virar o personagem sem redesenhar. */
  espelhada() {
    const nova = new Tela(this.largura, this.altura);
    for (let y = 0; y < this.altura; y++) {
      for (let x = 0; x < this.largura; x++) {
        nova.ponto(this.largura - 1 - x, y, this.ler(x, y));
      }
    }
    return nova;
  }

  /**
   * Escurece a borda de baixo e da direita de tudo que está opaco. É o truque
   * mais barato para dar volume a pixel art: sem isso o tile parece adesivo.
   */
  sombrearBordas(hex = '#00000038') {
    const alvo = [];
    for (let y = 0; y < this.altura; y++) {
      for (let x = 0; x < this.largura; x++) {
        if (this.ler(x, y)[3] === 0) continue;
        if (this.ler(x, y + 1)[3] === 0 || this.ler(x + 1, y)[3] === 0) alvo.push([x, y]);
      }
    }
    for (const [x, y] of alvo) this.ponto(x, y, hex);
  }

  paraPng() {
    return codificarPng(this.largura, this.altura, this.dados);
  }
}

/** Ruído determinístico: mesma semente, mesma arte, sempre. */
export function aleatorio(semente) {
  let s = semente >>> 0 || 1;
  return () => {
    s ^= s << 13;
    s >>>= 0;
    s ^= s >> 17;
    s ^= s << 5;
    s >>>= 0;
    return s / 0x100000000;
  };
}
