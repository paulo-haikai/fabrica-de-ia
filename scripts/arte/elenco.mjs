/**
 * O elenco: um corpo humano de 16x24 pixels, montado por parâmetro.
 *
 * Treze pessoas desenhadas à mão em quatro direções e quatro quadros seriam 208
 * desenhos. Aqui há um único boneco cuja pele, cabelo, corte, roupa e acessório
 * entram como argumento — e as 208 imagens caem de graça. O preço é que ninguém
 * tem pose própria; o ganho é que os doze mestres pertencem visivelmente à mesma
 * oficina, que é o que importa num jogo onde o elenco é cenário vivo e não
 * protagonista.
 *
 * Direções na ordem: 0 baixo, 1 cima, 2 esquerda, 3 direita. Quadros: 0 e 2 são
 * o apoio, 1 e 3 são os passos. Andar é 0,1,2,3 em laço; parado é só o 0.
 */

import { Tela } from './png.mjs';
import { P, PELES, CABELOS, ROUPAS, escurecer, clarear } from './paleta.mjs';

export const LARGURA = 16;
export const ALTURA = 24;
export const DIRECOES = 4;

/** Ordem das direções: 0 baixo, 1 cima, 2 esquerda, 3 direita. */
const Baixo = 0;
export const QUADROS = 4;

/**
 * Fisionomia de uma pessoa. Tudo que muda de um mestre para o outro cabe aqui.
 */
export function pessoa({ pele = 0, cabelo = 0, corte = 'curto', roupa = 0, acessorio = 'nenhum',
                         avental = true }) {
  return { pele, cabelo, corte, roupa, acessorio, avental };
}

// ------------------------------------------------------------------ peças

function sombra(t) {
  t.retangulo(5, 22, 6, 2, `${P.tinta}55`);
  t.ponto(4, 23, `${P.tinta}33`);
  t.ponto(11, 23, `${P.tinta}33`);
}

function cabeca(t, p, dir, y) {
  const [claro, medio, escuro] = PELES[p.pele % PELES.length];

  // A cabeça é um bloco de 8x8 com as quinas comidas — quadrada demais vira
  // caixote, redonda demais some no tamanho.
  t.retangulo(4, y, 8, 8, medio);
  t.retangulo(5, y - 1, 6, 1, medio);
  t.ponto(4, y, [0, 0, 0, 0]);
  t.ponto(11, y, [0, 0, 0, 0]);
  t.ponto(4, y + 7, [0, 0, 0, 0]);
  t.ponto(11, y + 7, [0, 0, 0, 0]);

  // Luz vindo de cima e da esquerda.
  t.linhaH(5, y - 1, 5, claro);
  t.linhaV(4, y + 1, 4, claro);
  t.linhaV(11, y + 1, 6, escuro);
  t.linhaH(5, y + 7, 6, escuro);

  if (dir === 1) return; // de costas: só nuca

  if (dir === 0) {
    // De frente: dois olhos e uma boca de um pixel.
    t.ponto(6, y + 4, P.tinta);
    t.ponto(9, y + 4, P.tinta);
    t.ponto(6, y + 3, clarear(medio, 0.35));
    t.ponto(9, y + 3, clarear(medio, 0.35));
    t.linhaH(7, y + 6, 2, escurecer(medio, 0.45));
  } else {
    // De perfil (desenhado para a direita; a esquerda é o espelho).
    t.ponto(10, y + 4, P.tinta);
    t.ponto(11, y + 4, escuro);
    t.linhaH(9, y + 6, 2, escurecer(medio, 0.45));
    t.ponto(11, y + 3, medio); // nariz
  }
}

function cabeloDe(t, p, dir, y) {
  const [tom, sombraTom] = CABELOS[p.cabelo % CABELOS.length];

  const franja = () => {
    t.retangulo(4, y - 2, 8, 3, tom);
    t.linhaH(5, y - 3, 6, tom);
    t.linhaH(5, y - 3, 6, clarear(tom, 0.22));
    t.ponto(4, y - 2, sombraTom);
    t.ponto(11, y - 2, sombraTom);
  };

  switch (p.corte) {
    case 'curto':
      franja();
      t.linhaV(4, y - 1, 3, tom);
      t.linhaV(11, y - 1, 3, sombraTom);
      break;

    case 'longo':
      franja();
      t.linhaV(3, y - 1, 9, tom);
      t.linhaV(12, y - 1, 9, sombraTom);
      t.linhaV(4, y - 1, 2, tom);
      t.linhaV(11, y - 1, 2, sombraTom);
      if (dir === 1) t.retangulo(4, y, 8, 8, tom);
      break;

    case 'coque':
      franja();
      t.disco(8, y - 4, 2.2, tom);
      t.ponto(7, y - 5, clarear(tom, 0.3));
      t.linhaV(4, y - 1, 2, tom);
      t.linhaV(11, y - 1, 2, sombraTom);
      break;

    case 'trancas':
      franja();
      for (const x of [3, 12]) {
        for (let k = 0; k < 8; k++) {
          t.ponto(x, y + k, k % 2 === 0 ? tom : sombraTom);
        }
      }
      if (dir === 1) t.retangulo(4, y, 8, 6, tom);
      break;

    case 'raspado':
      t.retangulo(4, y - 1, 8, 2, sombraTom);
      t.linhaH(5, y - 2, 6, sombraTom);
      t.linhaH(5, y - 2, 6, tom);
      break;

    case 'chapeu': {
      // Chapéu de palha: a aba é o que dá silhueta reconhecível de longe.
      t.retangulo(2, y - 1, 12, 2, P.latão);
      t.linhaH(2, y - 1, 12, P.latãoLuz);
      t.retangulo(5, y - 4, 6, 3, P.latãoClaro);
      t.linhaH(5, y - 4, 6, P.latãoLuz);
      t.linhaH(5, y - 2, 6, P.latãoFundo);
      break;
    }
  }
}

function corpo(t, p, dir, y, bracoAtras) {
  const cor = ROUPAS[p.roupa % ROUPAS.length];
  const luz = clarear(cor, 0.22);
  const som = escurecer(cor, 0.3);
  const [pelClara, pelMedia] = PELES[p.pele % PELES.length];

  // Tronco.
  t.retangulo(4, y, 8, 7, cor);
  t.linhaH(4, y, 8, luz);
  t.linhaV(4, y, 7, luz);
  t.linhaV(11, y, 7, som);
  t.linhaH(4, y + 6, 8, som);

  // Avental: marca que é gente que trabalha com as mãos.
  //
  // Deixou de ser obrigatório por causa da bancada 11: quem chega ao guichê da
  // prefeitura não é do ateliê, e um avental em todo mundo dizia justamente o
  // contrário. Os treze mestres continuam com ele por omissão do parâmetro.
  if (p.avental) {
    t.retangulo(6, y + 2, 4, 5, P.papel);
    t.linhaH(6, y + 2, 4, clarear(P.papel, 0.3));
    t.ponto(5, y + 1, P.papel);
    t.ponto(10, y + 1, P.papel);
  }

  // Braços: um vai à frente e o outro atrás, conforme o passo.
  const alturaBraco = 5;
  t.retangulo(3, y + 1 + (bracoAtras ? 1 : 0), 1, alturaBraco, cor);
  t.retangulo(12, y + 1 + (bracoAtras ? 0 : 1), 1, alturaBraco, som);
  t.ponto(3, y + alturaBraco + 1 + (bracoAtras ? 1 : 0), pelMedia);
  t.ponto(12, y + alturaBraco + 1 + (bracoAtras ? 0 : 1), pelMedia);

  if (dir === 0) t.ponto(8, y + 1, pelClara); // gola aberta
}

function pernas(t, p, y, passo) {
  const calca = escurecer(ROUPAS[p.roupa % ROUPAS.length], 0.55);
  // `passo` de -1 a 1: qual perna está à frente.
  const esq = passo > 0 ? 1 : 0;
  const dir = passo < 0 ? 1 : 0;
  t.retangulo(5, y, 2, 4 - esq, calca);
  t.retangulo(9, y, 2, 4 - dir, calca);
  t.retangulo(5, y + 4 - esq, 2, 1, P.madeiraFundo);
  t.retangulo(9, y + 4 - dir, 2, 1, P.madeiraFundo);
}

function acessorioDe(t, p, dir, y) {
  switch (p.acessorio) {
    case 'oculos':
      if (dir === 1) break;
      if (dir === 0) {
        t.contorno(5, y + 3, 3, 3, P.latãoLuz);
        t.contorno(8, y + 3, 3, 3, P.latãoLuz);
        t.ponto(8, y + 4, P.latãoLuz);
      } else {
        t.contorno(9, y + 3, 3, 3, P.latãoLuz);
      }
      break;

    case 'lenco':
      t.retangulo(4, y + 8, 8, 2, P.brasa);
      t.linhaH(4, y + 8, 8, P.brasaClara);
      if (dir === 0) t.ponto(8, y + 10, P.brasa);
      break;

    case 'folha':
      t.ponto(12, y - 2, P.folhaClara);
      t.ponto(13, y - 3, P.folhaLuz);
      t.ponto(12, y - 3, P.folha);
      break;

    case 'visor': {
      // Visor de vidro: o único elemento do elenco que brilha, reservado para
      // quem mexe com a máquina.
      const faixa = dir === 1 ? null : [4, y + 2, 8, 3];
      if (faixa) {
        t.retangulo(...faixa, `${P.vidroFundo}dd`);
        t.linhaH(4, y + 2, 8, P.vidroLuz);
        t.ponto(5, y + 3, P.vidroClaro);
      }
      break;
    }

    case 'avental_couro':
      t.retangulo(6, y + 12, 4, 5, P.madeira);
      t.linhaH(6, y + 12, 4, P.madeiraLuz);
      break;
  }
}

// ------------------------------------------------------------------ quadro

/** Um quadro: pessoa, direção e fase da caminhada. */
export function quadro(p, dir, indice) {
  const t = new Tela(LARGURA, ALTURA);

  // Balanço vertical: o corpo sobe um pixel no meio de cada passo. É a metade
  // do que faz a caminhada parecer caminhada; a outra metade são as pernas.
  const sobe = indice === 1 || indice === 3 ? 1 : 0;
  const passo = indice === 1 ? 1 : indice === 3 ? -1 : 0;
  const bracoAtras = indice === 3;

  sombra(t);
  const yCabeca = 3 - sobe;
  const yCorpo = 11 - sobe;

  // O ROSTO NÃO GIRA.
  //
  // A primeira versão desenhava nuca para cima e perfil para os lados, como
  // manda o manual. Ficou ruim de jogar: o rosto trocava a cada tecla e o
  // personagem parecia quatro pessoas diferentes se revezando. Num sprite de
  // 16 pixels não há detalhe suficiente para o olho reconhecer que a nuca e o
  // rosto são a mesma pessoa — ele só registra que a cara mudou.
  //
  // Então a cabeça é sempre a de frente, em qualquer direção. Quem informa para
  // onde o personagem anda é o corpo, o balanço dos braços e as pernas — que é
  // informação de sobra. É a escolha de muito jogo de cima: manter a cara
  // visível vale mais que a exatidão anatômica.
  pernas(t, p, 19 - sobe, passo);
  corpo(t, p, dir, yCorpo, bracoAtras);
  cabeca(t, p, Baixo, yCabeca);
  cabeloDe(t, p, Baixo, yCabeca);
  acessorioDe(t, p, Baixo, yCabeca);

  // Esquerda é a direita espelhada — desenhar as duas à mão só cria chance de
  // ficarem diferentes uma da outra.
  return dir === 2 ? t.espelhada() : t;
}

/** As 16 poses de uma pessoa, na ordem `dir * 4 + quadro`. */
export function folhaDe(p) {
  const quadros = [];
  for (let d = 0; d < DIRECOES; d++) {
    for (let f = 0; f < QUADROS; f++) quadros.push(quadro(p, d, f));
  }
  return quadros;
}
