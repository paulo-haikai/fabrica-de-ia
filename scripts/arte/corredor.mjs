/**
 * O CORREDOR DA IARA — as peças da bancada 6.
 *
 * Dezesseis pixels por peça, atlas de 8 colunas. A paleta é a do ateliê: o
 * corredor acontece DENTRO da fábrica, e uma folha com cores próprias faria a
 * bancada 6 parecer um jogo que alguém colou aqui.
 *
 * O DESENHO É CHAPADO DE PROPÓSITO, e isso mudou depois do primeiro teste com
 * gente. A primeira folha tinha latão escovado, rebite, veio e sombra em cada
 * bloco — bonita numa prévia ampliada e péssima em jogo, porque o cenário
 * competia com o personagem, que tem meia casa de largura e é a única coisa que
 * o aluno precisa achar em meio segundo. Agora cada peça é uma cor sólida com
 * contorno escuro forte: é o que o Level Devil faz, e é o que faz uma armadilha
 * ser lida no instante em que aparece.
 *
 * A GRAMÁTICA DE COR, aprendida na primeira fase sem uma linha de texto:
 *
 *   · LATÃO é o que sustenta — chão, parede, plataforma.
 *   · BRASA é o que mata — espinho, bloco que cai, tudo que encosta e derruba.
 *   · LUZ é o que informa — a lâmpada, e só ela.
 *   · VIDRO é o que muda — o chão que vai desmoronar, a parede que vai subir.
 *
 * A quarta é a mais importante e é a que o Level Devil não tem: aqui o aluno
 * precisa poder aprender. Traição sem telegrama nenhum é engraçada uma vez e
 * cruel na décima; o vidro é o aviso de meio segundo que transforma "o jogo me
 * sacaneou" em "eu vi e não desviei".
 */

import { Tela } from './png.mjs';
import { P, escurecer, clarear, misturar } from './paleta.mjs';

export const LADO = 16;

const CONTORNO = escurecer(P.tinta, 0);
const FUNDO = misturar(P.tintaClara, P.madeiraFundo, 0.35);

/** Bloco chapado com contorno. A base de quase tudo nesta folha. */
function chapado(t, cor, { cima = true, baixo = true, esquerda = true, direita = true } = {}) {
  t.retangulo(0, 0, LADO, LADO, cor);
  if (cima) t.linhaH(0, 0, LADO, CONTORNO);
  if (baixo) t.linhaH(0, LADO - 1, LADO, CONTORNO);
  if (esquerda) t.linhaV(0, 0, LADO, CONTORNO);
  if (direita) t.linhaV(LADO - 1, 0, LADO, CONTORNO);
}

// --------------------------------------------------------------- estrutura

/** O chão e a parede. Uma cor, um contorno, e uma faixa de luz no topo. */
function macico(t) {
  chapado(t, P.latão);
  t.linhaH(1, 1, LADO - 2, clarear(P.latão, 0.22));
}

/**
 * O chão que vai desmoronar.
 *
 * Igual ao maciço, com trincas de vidro. As trincas são o telegrama: quem já
 * jogou uma fase sabe que aquele chão não vai aguentar, e passa correndo.
 */
function trincado(t) {
  macico(t);
  const c = P.vidroClaro;
  t.ponto(4, 2, c); t.ponto(4, 3, c); t.ponto(5, 4, c); t.ponto(5, 5, c);
  t.ponto(6, 6, c); t.ponto(6, 7, c); t.ponto(7, 8, c); t.ponto(8, 9, c);
  t.ponto(8, 10, c); t.ponto(9, 11, c); t.ponto(9, 12, c); t.ponto(10, 13, c);
  t.ponto(3, 5, c); t.ponto(2, 6, c);
  t.ponto(11, 9, c); t.ponto(12, 10, c);
}

/** O chão já cedendo: o quadro entre pisar e cair. */
function caindo(t) {
  const cor = P.latão;
  const caco = (x, y, l, a) => {
    t.retangulo(x, y, l, a, cor);
    t.contorno(x, y, l, a, CONTORNO);
  };
  caco(0, 2, 5, 5);
  caco(6, 0, 5, 6);
  caco(12, 3, 4, 5);
  caco(2, 9, 5, 5);
  caco(9, 10, 5, 5);
}

// -------------------------------------------------------------------- perigo

/** Espinhos apontando para cima. Cinco dentes, ímpar, com um no centro exato. */
function espinhos(t, paraBaixo = false) {
  const cor = P.brasa;
  t.retangulo(0, 0, LADO, LADO, `${FUNDO}00`);

  const base = paraBaixo ? 0 : LADO - 2;
  t.retangulo(0, base, LADO, 2, escurecer(cor, 0.35));
  t.linhaH(0, paraBaixo ? 1 : base, LADO, CONTORNO);

  for (let d = 0; d < 5; d++) {
    const cx = 1 + d * 3;
    for (let h = 0; h < 12; h++) {
      const largura = Math.max(1, 3 - Math.floor(h / 5));
      const y = paraBaixo ? 2 + h : LADO - 3 - h;
      for (let i = 0; i < largura; i++) t.ponto(cx + i, y, cor);
      t.ponto(cx - 1, y, CONTORNO);
      t.ponto(cx + largura, y, CONTORNO);
    }
  }
}

/** A marca do espinho que AINDA não subiu: três pontinhos no chão. */
function furos(t) {
  macico(t);
  const c = escurecer(P.latão, 0.55);
  for (let d = 0; d < 5; d++) {
    const cx = 2 + d * 3;
    t.ponto(cx, 2, c);
    t.ponto(cx, 3, c);
  }
}

/** O bloco que cai do teto. Brasa, porque esmaga. */
function bloco(t) {
  chapado(t, P.brasa);
  t.linhaH(1, 1, LADO - 2, clarear(P.brasa, 0.3));
  t.linhaH(1, LADO - 2, LADO - 2, escurecer(P.brasa, 0.35));
  for (const [x, y] of [[4, 5], [11, 5], [4, 10], [11, 10]]) {
    t.ponto(x, y, escurecer(P.brasa, 0.5));
    t.ponto(x + 1, y, escurecer(P.brasa, 0.5));
  }
}

/** O bloco pendurado, antes de soltar. Vidro: é o telegrama. */
function blocoPreso(t) {
  chapado(t, misturar(P.brasa, P.vidro, 0.45));
  t.linhaH(1, 1, LADO - 2, P.vidroLuz);
  t.linhaV(7, 0, 4, P.pedraClara);
  t.linhaV(8, 0, 4, P.pedra);
}

/** A parede que vai subir do chão. Vidro: telegrama de novo. */
function paredePreta(t) {
  chapado(t, misturar(P.latão, P.vidro, 0.5));
  t.linhaH(1, 1, LADO - 2, P.vidroLuz);
  for (let y = 3; y < LADO - 2; y += 4) t.linhaH(2, y, LADO - 4, escurecer(P.vidro, 0.25));
}

// -------------------------------------------------------------------- lâmpada

/**
 * A lâmpada da confiança, em cinco forças.
 *
 * É o único objeto do corredor que nunca mente, e por isso é o mais desenhado.
 * A força entra como raio de halo e brilho de filamento, não como cor — cor
 * diferente por força faria o aluno decorar "verde é bom" em vez de COMPARAR as
 * lâmpadas entre si, que é a operação da rede.
 */
function lampada(t, forca) {
  const vidro = misturar(P.vidro, P.tinta, 0.6);
  const cx = 7.5;
  const cy = 8.5;
  const q = forca / 4;

  if (forca > 0) {
    const raio = 2.2 + forca * 1.5;
    t.disco(cx, cy, raio + 2.4, `${P.luz}14`);
    t.disco(cx, cy, raio + 1.1, `${P.luz}26`);
    t.disco(cx, cy, raio, `${P.luz}3c`);
  }

  t.retangulo(6, 0, 4, 3, P.latãoFundo);
  t.linhaH(6, 0, 4, P.latão);

  // Apagada é vidro escuro; acesa vai para o dourado. A ordem importa:
  // misturar(a, b, q) anda de A para B, e escrever isto ao contrário — que foi o
  // primeiro corte desta folha — deixava a lâmpada mais forte MAIS ESCURA que a
  // mais fraca, invertendo a única informação que o corredor dá de graça.
  t.disco(cx, cy, 4.6, vidro);
  t.disco(cx, cy, 3.6, misturar(vidro, P.luz, q));
  if (forca >= 3) t.disco(cx, cy, 2.2, misturar(P.luz, P.luzForte, (forca - 2) / 2));

  const fio = forca === 0 ? P.pedraClara : misturar(P.latãoLuz, P.branco, q);
  t.ponto(6, 6, fio); t.ponto(9, 6, fio);
  t.ponto(7, 7, fio); t.ponto(8, 7, fio);
  t.ponto(6, 8, fio); t.ponto(9, 8, fio);
  t.ponto(7, 9, fio); t.ponto(8, 9, fio);
  t.ponto(6, 10, fio); t.ponto(9, 10, fio);

  if (forca > 1) {
    t.ponto(5, 6, `${P.branco}d0`);
    t.ponto(5, 7, `${P.branco}70`);
  }

  const borda = forca >= 3 ? clarear(P.luz, 0.2) : escurecer(vidro, 0.4);
  for (let a = 0; a < 360; a += 10) {
    const rad = (a * Math.PI) / 180;
    t.ponto(cx + Math.cos(rad) * 4.7, cy + Math.sin(rad) * 4.7, borda);
  }
}

// --------------------------------------------------------------------- portas

/**
 * A porta. TODAS IGUAIS, e é a regra mais importante desta folha.
 *
 * Não existe versão "certa" e versão "errada": a certa é a que está com a
 * lâmpada acesa por cima, e mais nada. Desenhar a certa diferente destruiria a
 * bancada inteira — o aluno leria a porta e nunca a lâmpada, e a lâmpada é a
 * rede.
 *
 * São duas peças porque a porta tem duas casas de altura: `alto` desenha a
 * bandeira e o arco, `baixo` desenha o vão e a soleira.
 */
function porta(t, alto, aberta) {
  t.retangulo(0, 0, LADO, LADO, `${FUNDO}00`);
  const moldura = aberta ? P.luz : P.latãoClaro;
  const vao = aberta ? P.luzForte : escurecer(P.tinta, 0);

  t.retangulo(2, alto ? 2 : 0, 12, alto ? 14 : 15, vao);
  t.linhaV(2, alto ? 2 : 0, alto ? 14 : 16, moldura);
  t.linhaV(3, alto ? 2 : 0, alto ? 14 : 16, escurecer(moldura, 0.3));
  t.linhaV(12, alto ? 2 : 0, alto ? 14 : 16, escurecer(moldura, 0.3));
  t.linhaV(13, alto ? 2 : 0, alto ? 14 : 16, moldura);

  t.linhaV(1, alto ? 2 : 0, alto ? 14 : 16, CONTORNO);
  t.linhaV(14, alto ? 2 : 0, alto ? 14 : 16, CONTORNO);

  if (alto) {
    t.linhaH(1, 1, 14, CONTORNO);
    t.linhaH(2, 2, 12, moldura);
    t.linhaH(3, 3, 10, escurecer(moldura, 0.4));
    if (aberta) t.retangulo(4, 4, 8, 12, `${P.luzForte}66`);
  } else {
    t.linhaH(1, LADO - 1, 14, CONTORNO);
    t.linhaH(2, LADO - 2, 12, escurecer(moldura, 0.2));
    if (aberta) t.retangulo(4, 0, 8, 14, `${P.luzForte}66`);
    else { t.ponto(10, 8, P.latãoLuz); t.ponto(10, 9, P.latãoFundo); }
  }
}

// ---------------------------------------------------------------- o mensageiro

/**
 * O mensageiro, de lado, carregando o pacote.
 *
 * Silhueta escura e chapada sobre cenário claro — é o que garante que ele seja
 * a coisa mais legível da tela, que é o requisito número um num jogo em que se
 * morre depressa e se reinicia depressa.
 *
 * O PACOTE nas costas é a mensagem: as três palavras que a máquina está lendo.
 * Ele é a única parte clara do sprite, e não encolhe nunca — a janela é a mesma
 * do começo ao fim da fase.
 */
function mensageiro(t, quadro) {
  const corpo = misturar(P.panoFrio, P.tinta, 0.35);
  const x0 = 5;
  const alt = 11;
  const y0 = LADO - alt - 1;

  t.retangulo(x0, y0, 6, alt, corpo);
  t.contorno(x0, y0, 6, alt, CONTORNO);

  // Cabeça mais clara, para haver silhueta de gente e não de tijolo.
  t.retangulo(x0 + 1, y0 + 1, 4, 3, misturar(P.papel, corpo, 0.35));

  // Pernas: dois quadros de caminhada, alternados pelo jogo.
  const passo = quadro % 2 === 0;
  t.retangulo(x0, LADO - 1, passo ? 2 : 3, 1, CONTORNO);
  t.retangulo(x0 + (passo ? 4 : 3), LADO - 1, passo ? 2 : 3, 1, CONTORNO);

  // O pacote.
  t.retangulo(1, y0 + 2, 4, 4, P.papel);
  t.contorno(1, y0 + 2, 4, 4, CONTORNO);
  t.linhaV(3, y0 + 2, 4, P.brasa);
}

// -------------------------------------------------------------------- cenário

/** O fundo. Chapado de propósito: quem tem que se ver é o mensageiro. */
function fundo(t) {
  t.retangulo(0, 0, LADO, LADO, FUNDO);
}

/** Véu do escuro. */
function bruma(t) {
  t.retangulo(0, 0, LADO, LADO, `${P.tinta}f0`);
}

// ------------------------------------------------------------------ manifesto

export const PECAS = [
  ['macico', macico],
  ['trincado', trincado],
  ['caindo', caindo],
  ['espinhos', t => espinhos(t, false)],
  ['espinhos_teto', t => espinhos(t, true)],
  ['furos', furos],
  ['bloco', bloco],
  ['bloco_preso', blocoPreso],
  ['parede_presa', paredePreta],
  ['lampada_0', t => lampada(t, 0)],
  ['lampada_1', t => lampada(t, 1)],
  ['lampada_2', t => lampada(t, 2)],
  ['lampada_3', t => lampada(t, 3)],
  ['lampada_4', t => lampada(t, 4)],
  ['porta_alta', t => porta(t, true, false)],
  ['porta_baixa', t => porta(t, false, false)],
  ['porta_alta_aberta', t => porta(t, true, true)],
  ['porta_baixa_aberta', t => porta(t, false, true)],
  ['mensageiro_0', t => mensageiro(t, 0)],
  ['mensageiro_1', t => mensageiro(t, 1)],
  ['fundo', fundo],
  ['bruma', bruma]
];

export function pintarPeca(nome) {
  const peca = PECAS.find(([n]) => n === nome);
  if (!peca) throw new Error(`peça do corredor desconhecida: ${nome}`);
  const t = new Tela(LADO, LADO);
  peca[1](t);
  return t;
}
