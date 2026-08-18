/**
 * As doze bancadas, 32x32 cada.
 *
 * Uma bancada tem que se anunciar de longe. O aluno atravessa o salão vendo
 * doze silhuetas e precisa decidir para qual anda — se todas forem "mesa com
 * coisas em cima", a escolha vira sorteio e o mapa perde a função. Por isso
 * cada uma repete a base de madeira (que diz "aqui se trabalha") mas leva um
 * aparelho de silhueta única em cima: o tear é alto e estreito, o fogão é
 * baixo e largo, os lampiões furam a linha do teto.
 *
 * O aparelho também é o ofício do mestre, não o conceito abstrato da etapa.
 * A costureira tem tesoura e tecido; que isso ensine tokenização é descoberta
 * do aluno na bancada, não spoiler pintado no cenário.
 */

import { Tela } from './png.mjs';
import { P, escurecer, clarear } from './paleta.mjs';

export const LADO = 32;

/** Tampo, pés e a sombra que assenta a bancada no chão. */
function base(t, { largura = 26, altura = 7, y = 18 } = {}) {
  const x = Math.round((LADO - largura) / 2);

  // Sombra primeiro, para tudo pousar por cima dela.
  t.retangulo(x + 1, y + altura - 1, largura - 2, 3, `${P.tinta}44`);
  t.retangulo(x + 3, y + altura + 1, largura - 6, 1, `${P.tinta}28`);

  // Pés.
  t.retangulo(x + 2, y + 4, 3, altura, escurecer(P.madeira, 0.45));
  t.retangulo(x + largura - 5, y + 4, 3, altura, escurecer(P.madeira, 0.45));

  // Tampo: a faixa clara no topo é o que faz ler como superfície e não parede.
  t.retangulo(x, y, largura, 5, P.madeira);
  t.linhaH(x, y, largura, P.madeiraLuz);
  t.linhaH(x, y + 1, largura, P.madeiraBrilho);
  t.linhaH(x, y + 4, largura, escurecer(P.madeira, 0.4));
  t.linhaV(x, y, 5, P.madeiraClara);
  t.linhaV(x + largura - 1, y, 5, escurecer(P.madeira, 0.3));

  return { x, y, largura };
}

// ------------------------------------------------------- os doze aparelhos

/** 1 — Tico, aprendiz da estufa: uma muda sob campânula e a placa com a lacuna. */
function estufa(t) {
  base(t);
  t.disco(11, 13, 5, `${P.vidroLuz}44`);
  for (let i = 0; i < 10; i++) t.ponto(7 + i, 8 + Math.abs(i - 5) - 1, `${P.vidroLuz}99`);
  t.linhaV(6, 13, 5, `${P.vidroLuz}77`);
  t.linhaV(16, 13, 5, `${P.vidroLuz}77`);
  t.linhaV(11, 13, 5, P.folha);
  t.ponto(10, 12, P.folhaClara);
  t.ponto(12, 11, P.folhaLuz);
  t.ponto(11, 10, P.broto);
  t.retangulo(9, 17, 5, 2, P.brasa);

  // A placa de trás, com a palavra que falta.
  t.retangulo(19, 8, 10, 8, P.papel);
  t.contorno(19, 8, 10, 8, P.madeiraClara);
  t.linhaH(21, 11, 6, P.pedra);
  t.linhaH(21, 13, 3, P.pedra);
  t.linhaH(25, 13, 2, P.brasa);
}

/** 2 — Dona Ciça, guarda-livros: o livro-caixa aberto, cheio de risquinhos. */
function livroCaixa(t) {
  base(t);
  t.retangulo(6, 10, 20, 9, P.papel);
  t.retangulo(5, 9, 22, 2, P.brasa);
  t.linhaV(16, 10, 9, escurecer(P.papel, 0.25));
  t.contorno(6, 10, 20, 9, escurecer(P.madeira, 0.3));

  // Risquinhos em grupos de cinco, que é o conceito inteiro da etapa.
  for (let g = 0; g < 4; g++) {
    const x = 8 + (g % 2) * 10;
    const y = 12 + Math.floor(g / 2) * 4;
    for (let i = 0; i < 4; i++) t.linhaV(x + i, y, 3, P.tinta);
    for (let i = 0; i < 5; i++) t.ponto(x - 1 + i, y + i * 0.6, P.brasa);
  }
  t.retangulo(27, 12, 3, 7, P.latão);
}

/** 3 — Mestre Aurélio, arquivista: o fichário de gavetas, com a grade vazia. */
function fichario(t) {
  base(t, { y: 20, altura: 5 });
  t.retangulo(4, 4, 24, 16, P.madeiraClara);
  t.contorno(4, 4, 24, 16, escurecer(P.madeira, 0.5));
  t.linhaH(5, 5, 22, P.madeiraBrilho);

  for (let l = 0; l < 3; l++) {
    for (let c = 0; c < 3; c++) {
      const x = 6 + c * 7;
      const y = 6 + l * 5;
      t.retangulo(x, y, 6, 4, escurecer(P.madeira, 0.15));
      t.linhaH(x, y, 6, P.madeiraLuz);
      t.linhaH(x + 2, y + 2, 2, P.latãoLuz); // puxador
    }
  }
  // Uma gaveta aberta com a ficha em branco: é o gancho da etapa.
  t.retangulo(13, 16, 6, 4, P.papel);
  t.linhaH(13, 16, 6, P.branco);
}

/** 4 — Nara, costureira: mesa de corte, tesoura e o tecido já retalhado. */
function mesaDeCorte(t) {
  base(t);
  // Rolo de tecido atrás.
  t.retangulo(3, 8, 6, 11, P.panoRosa);
  t.linhaV(3, 8, 11, clarear(P.panoRosa, 0.3));
  t.linhaV(8, 8, 11, escurecer(P.panoRosa, 0.3));
  t.retangulo(3, 7, 6, 1, P.papel);

  // O tecido cortado em tiras: cada tira é uma ficha.
  const tiras = [P.vidroClaro, P.luz, P.folhaLuz, P.brasaClara];
  for (let i = 0; i < 4; i++) {
    t.retangulo(11 + i * 5, 13, 4, 5, tiras[i]);
    t.linhaH(11 + i * 5, 13, 4, clarear(tiras[i], 0.35));
    t.linhaH(11 + i * 5, 17, 4, escurecer(tiras[i], 0.3));
  }

  // Tesoura aberta, de lâmina de latão.
  t.linhaV(24, 6, 6, P.neblina);
  t.linhaV(26, 6, 6, P.neblina);
  t.ponto(25, 12, P.pedraClara);
  t.contorno(23, 12, 3, 3, P.latão);
  t.contorno(26, 12, 3, 3, P.latão);
}

/** 5 — Bento, cartógrafo de jardim: o mapa estendido com alfinetes. */
function mesaDeMapa(t) {
  base(t);
  t.retangulo(4, 8, 24, 11, P.papel);
  t.contorno(4, 8, 24, 11, P.madeiraClara);

  // Curvas de nível: dizem "mapa" em três riscos.
  for (let i = 0; i < 3; i++) {
    for (let x = 0; x < 18; x++) {
      const y = 12 + i * 3 + Math.round(Math.sin((x + i * 3) / 3) * 1.2);
      t.ponto(6 + x, y, escurecer(P.papel, 0.3));
    }
  }
  // Alfinetes agrupados: palavras parecidas ficam perto, que é a etapa toda.
  const pinos = [[9, 11, P.brasa], [11, 12, P.brasa], [10, 14, P.brasa],
                 [21, 15, P.vidro], [23, 14, P.vidro], [22, 17, P.vidro]];
  for (const [x, y, cor] of pinos) {
    t.ponto(x, y, cor);
    t.ponto(x, y - 1, clarear(cor, 0.4));
  }
}

/** 6 — Iara, tecelã de circuitos: o tear em pé, com fios entre os nós. */
function tear(t) {
  base(t, { largura: 22, y: 22, altura: 5 });
  // Moldura alta: é a silhueta mais vertical do salão, de propósito.
  t.retangulo(6, 2, 2, 20, P.madeiraClara);
  t.retangulo(24, 2, 2, 20, P.madeiraClara);
  t.retangulo(6, 2, 20, 2, P.madeira);
  t.linhaH(6, 2, 20, P.madeiraLuz);

  // Três colunas de nós, ligadas por fios: a rede, literalmente tecida.
  const colunas = [[9, 3], [16, 4], [23, 2]];
  const pontos = colunas.map(([x, n]) =>
    Array.from({ length: n }, (_, i) => [x, 7 + i * (14 / Math.max(1, n))])
  );
  for (let c = 0; c < pontos.length - 1; c++) {
    for (const [x1, y1] of pontos[c]) {
      for (const [x2, y2] of pontos[c + 1]) {
        const passos = Math.max(Math.abs(x2 - x1), Math.abs(y2 - y1));
        for (let k = 0; k <= passos; k++) {
          t.ponto(x1 + ((x2 - x1) * k) / passos, y1 + ((y2 - y1) * k) / passos, `${P.latão}88`);
        }
      }
    }
  }
  for (const coluna of pontos) {
    for (const [x, y] of coluna) {
      t.disco(x, y, 1.6, P.vidroClaro);
      t.ponto(x - 1, y - 1, P.vidroLuz);
    }
  }
}

/** 7 — Seu Ilo, afinador: o mostrador de agulha, marcando o quanto errou. */
function afinador(t) {
  base(t);
  t.retangulo(7, 5, 18, 14, P.pedraClara);
  t.contorno(7, 5, 18, 14, escurecer(P.pedra, 0.5));
  t.linhaH(8, 6, 16, clarear(P.pedraClara, 0.3));

  // Mostrador, com a faixa boa em verde e a ruim em brasa.
  t.disco(16, 13, 7, P.papel);
  t.disco(16, 13, 6, clarear(P.papel, 0.35));
  for (let a = 0; a <= 20; a++) {
    const ang = Math.PI + (a / 20) * Math.PI;
    const cor = a < 7 ? P.brasa : a > 13 ? P.brasa : P.folhaClara;
    t.ponto(16 + Math.cos(ang) * 6, 13 + Math.sin(ang) * 6, cor);
  }
  // Agulha fora do lugar: a máquina ainda está desafinada.
  for (let k = 0; k < 5; k++) t.ponto(16 - k * 0.8, 13 - k * 0.8, P.tinta);
  t.ponto(16, 13, P.latãoLuz);

  // Diapasão encostado.
  t.linhaV(27, 8, 8, P.latãoClaro);
  t.linhaV(29, 8, 8, P.latãoClaro);
  t.retangulo(27, 16, 3, 3, P.latão);
}

/** 8 — Rosa, treinadora: manivela, roldana e a curva que desce. */
function bancadaDeTreino(t) {
  base(t);
  // O quadro com a curva de perda caindo — o gráfico é a recompensa da etapa.
  t.retangulo(4, 5, 17, 13, P.tintaClara);
  t.contorno(4, 5, 17, 13, P.madeiraClara);
  t.linhaV(6, 7, 9, P.neblina);
  t.linhaH(6, 15, 13, P.neblina);
  for (let x = 0; x < 12; x++) {
    const y = 15 - Math.round(7 * Math.exp(-x / 3.5));
    t.ponto(7 + x, y, P.luz);
    t.ponto(7 + x, y + 1, `${P.luz}66`);
  }

  // Manivela: o aluno gira, a máquina aprende.
  t.disco(26, 12, 5, P.latão);
  t.disco(26, 12, 3, P.latãoFundo);
  t.ponto(25, 10, P.latãoLuz);
  t.linhaV(26, 12, 6, P.pedraClara);
  t.retangulo(24, 17, 5, 2, P.madeiraClara);
}

/** 9 — Chef Amaro, cozinheiro: fogão baixo e largo, com a panela do corpus. */
function fogao(t) {
  base(t, { largura: 28, y: 16, altura: 9 });
  t.retangulo(2, 16, 28, 5, P.pedra);
  t.linhaH(2, 16, 28, P.pedraClara);

  // Panela.
  t.retangulo(9, 8, 14, 8, P.pedraClara);
  t.linhaH(9, 8, 14, P.neblina);
  t.retangulo(8, 11, 1, 3, P.pedra);
  t.retangulo(23, 11, 1, 3, P.pedra);
  t.retangulo(10, 9, 12, 2, P.folhaClara); // caldo

  // Vapor: três fiapos, que é o que dá vida a um sprite parado.
  for (let i = 0; i < 3; i++) {
    for (let k = 0; k < 4; k++) {
      t.ponto(12 + i * 4 + Math.round(Math.sin(k) * 1.2), 6 - k, `${P.branco}${(70 - k * 14).toString(16)}0`);
    }
  }
  // Chama sob o fogão.
  for (let i = 0; i < 5; i++) {
    t.ponto(13 + i, 20, i % 2 ? P.luz : P.brasa);
    t.ponto(14 + i, 19, P.luzForte);
  }
}

/** 10 — Lumi, acendedora: os lampiões apontados, um só aceso. */
function lampioes(t) {
  base(t);
  // Três lampiões pendurados em alturas diferentes, furando a linha do teto.
  const postes = [[7, 3], [16, 1], [25, 4]];
  postes.forEach(([x, y], i) => {
    t.linhaV(x, 0, y + 4, P.pedra);
    const aceso = i === 1;
    t.retangulo(x - 3, y + 4, 7, 6, aceso ? P.latãoClaro : P.latãoFundo);
    t.contorno(x - 3, y + 4, 7, 6, P.latão);
    t.retangulo(x - 2, y + 5, 5, 4, aceso ? P.luzForte : P.tintaClara);

    if (!aceso) return;
    // O cone de luz é o que transforma três lampiões numa lição sobre atenção.
    for (let k = 0; k < 9; k++) {
      const meia = 2 + k * 0.8;
      const a = Math.max(0, 60 - k * 6);
      for (let dx = -meia; dx <= meia; dx++) {
        t.ponto(x + dx, y + 10 + k, `${P.luz}${a.toString(16).padStart(2, '0')}`);
      }
    }
  });
}

/** 11 — Vovó Zi, contadora de histórias: a máquina de falar, com corneta. */
function maquinaDeFalar(t) {
  base(t);
  t.retangulo(4, 11, 15, 8, P.madeiraClara);
  t.contorno(4, 11, 15, 8, escurecer(P.madeira, 0.45));
  t.linhaH(5, 12, 13, P.madeiraBrilho);
  t.disco(9, 15, 2.5, P.tintaClara); // prato
  t.ponto(9, 15, P.latãoLuz);
  t.linhaV(14, 12, 4, P.latão); // braço

  // Corneta: cresce da esquerda para a direita, bem larga na boca.
  for (let k = 0; k < 12; k++) {
    const meia = 1 + k * 0.62;
    for (let dy = -meia; dy <= meia; dy++) {
      t.ponto(18 + k, 9 + dy, k > 9 ? P.latãoLuz : P.latãoClaro);
    }
    t.ponto(18 + k, 9 - meia, P.latãoLuz);
    t.ponto(18 + k, 9 + meia, P.latãoFundo);
  }
  // Notas saindo.
  t.ponto(30, 4, P.luz);
  t.ponto(30, 3, P.luz);
  t.ponto(29, 3, P.luz);
}

/** 12 — Sereno, guardião: a balança de dois pratos, quase em equilíbrio. */
function balanca(t) {
  base(t);
  t.linhaV(16, 3, 14, P.latão);
  t.linhaV(15, 3, 14, P.latãoFundo);
  t.retangulo(13, 17, 7, 2, P.latãoClaro);

  // Braço inclinado: em equilíbrio perfeito não haveria o que a etapa ensinar.
  const inclinacao = -1;
  for (let x = -9; x <= 9; x++) {
    t.ponto(16 + x, 5 + Math.round((x * inclinacao) / 6), P.latãoClaro);
  }
  const pratos = [[7, 5 + 1], [25, 5 - 1]];
  pratos.forEach(([px, py], i) => {
    t.linhaV(px, py, 4, `${P.neblina}cc`);
    for (let dx = -4; dx <= 4; dx++) {
      t.ponto(px + dx, py + 4 + Math.round(Math.abs(dx) / 3), P.latãoLuz);
    }
    t.ponto(px + (i ? 1 : -1), py + 3, i ? P.vidroClaro : P.brasa);
  });
}

/** Na ordem das etapas. O índice no atlas é o índice aqui. */
export const BANCADAS = [
  ['e1', estufa],
  ['e2', livroCaixa],
  ['e3', fichario],
  ['e4', mesaDeCorte],
  ['e5', mesaDeMapa],
  ['e6', tear],
  ['e7', afinador],
  ['e8', bancadaDeTreino],
  ['e9', fogao],
  ['e10', lampioes],
  ['e11', maquinaDeFalar],
  ['e12', balanca]
];

export function pintarBancada(indice) {
  const t = new Tela(LADO, LADO);
  BANCADAS[indice][1](t);
  return t;
}
