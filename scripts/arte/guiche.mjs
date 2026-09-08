/**
 * O GUICHÊ DO SERENO — as peças da bancada 12.
 *
 * O que faz o Papers, Please parecer o Papers, Please não é a arte dele: é a
 * MATERIALIDADE. Papel que é papel, apoiado numa mesa que é mesa, e um carimbo
 * que desce e deixa tinta. A decisão tem peso porque tem corpo — e é isso que se
 * copia de um jogo, não os pixels dele, que aliás são de outra pessoa.
 *
 * Então aqui não há nada importado de lugar nenhum. São cinco peças desenhadas
 * na paleta do ateliê, pelo mesmo gerador que desenha o corredor da Iara e os
 * treze mestres, porque o guichê acontece DENTRO da fábrica e uma folha com
 * cores próprias faria a bancada 12 parecer um jogo colado aqui.
 *
 * A GRAMÁTICA, herdada do corredor e válida aqui:
 *
 *   · MADEIRA é a mesa — o único plano horizontal, e o que dá o "estou sentado
 *     atrás de um balcão".
 *   · PAPEL é o que se lê, e é a coisa mais clara da tela. Tudo o mais é
 *     repartição: pedra, tinta, penumbra.
 *   · FOLHA e BRASA são o veredito, e só ele. Nenhuma outra peça usa as duas.
 *   · LATÃO é o oficial — selo, grampo, o que carimbam por cima de você.
 */

import { Tela } from './png.mjs';
import { P, escurecer, clarear } from './paleta.mjs';

export const LADO = 32;

/**
 * As peças da folha.
 *
 * A LISTA VEIO DE UMA LEITURA DO ORIGINAL, e vale registrar como: o Papers,
 * Please guarda a arte dele em arquivos de nome transparente — mesa, parede do
 * guichê, cortina, tinta aprovada, tinta negada, barra do carimbo. Ler a lista
 * de nomes diz quais peças um guichê precisa ter, que é conhecimento de desenho
 * de jogo e não pertence a ninguém.
 *
 * O que está dentro daqueles arquivos é do Lucas Pope e não entra aqui. O que
 * entra é a resposta à pergunta que a lista faz: "de que peças isto é feito?"
 */
export const PECAS = [
  ['mesa'],
  ['carimbo_defere'],
  ['carimbo_indefere'],
  ['selo'],
  ['grampo'],
  ['brasao'],
  ['suporte']
];

/**
 * Um anel de tinta. O `disco` da Tela pinta cheio e alpha não apaga, então o
 * vazado é feito à mão, medindo a distância pixel a pixel.
 */
function anel(t, cx, cy, externo, interno, cor) {
  for (let y = 0; y < LADO; y++) {
    for (let x = 0; x < LADO; x++) {
      const d = Math.hypot(x + 0.5 - cx, y + 0.5 - cy);
      if (d <= externo && d >= interno) t.ponto(x, y, cor);
    }
  }
}

/**
 * A mesa. Tem que LADRILHAR: a bancada repete esta peça pela largura toda, e
 * qualquer coisa perto da borda vira listra visível a cada 32 pixels. Por isso
 * o veio é horizontal e contínuo, e os nós ficam no miolo.
 */
function mesa(t) {
  t.retangulo(0, 0, LADO, LADO, P.madeira);

  // Veio: linhas inteiras, de ponta a ponta, senão a emenda aparece.
  for (const [y, cor] of [[3, P.madeiraFundo], [9, P.madeiraClara], [14, P.madeiraFundo],
                          [21, P.madeiraClara], [27, P.madeiraFundo]]) {
    t.linhaH(0, y, LADO, cor);
  }
  t.linhaH(0, 10, LADO, clarear(P.madeiraClara, 0.12));
  t.linhaH(0, 22, LADO, clarear(P.madeiraClara, 0.08));

  // Dois nós, longe das bordas.
  t.disco(11, 17, 1.6, P.madeiraFundo);
  t.ponto(11, 17, escurecer(P.madeiraFundo, 0.3));
  t.disco(24, 6, 1.2, P.madeiraFundo);
}

/**
 * O carimbo. Anel grosso com uma tarja no meio — a forma que se lê de relance
 * mesmo virada de lado, que é como ela cai no papel.
 *
 * A tinta é IRREGULAR de propósito: falha em alguns pixels da borda, como
 * carimbo de borracha em papel de repartição. Carimbo perfeito parece adesivo.
 */
function carimbo(t, cor) {
  const escura = escurecer(cor, 0.35);

  anel(t, 16, 16, 14, 10.5, cor);
  anel(t, 16, 16, 14, 13.2, escura);

  // A tarja central, onde iria a palavra.
  t.retangulo(7, 14, 18, 5, cor);
  t.linhaH(7, 14, 18, escura);
  t.linhaH(7, 18, 18, escura);

  // Marcas de letra dentro da tarja: sugestão, não texto. O texto de verdade
  // vem por cima, em C#, porque muda de "DEFERIDO" para "INDEFERIDO".
  for (const x of [9, 12, 15, 18, 21]) t.retangulo(x, 15, 2, 3, escurecer(cor, 0.55));

  // Falhas da borracha.
  for (const [x, y] of [[16, 2], [3, 13], [28, 19], [17, 29], [8, 5], [25, 26]]) {
    t.ponto(x, y, [0, 0, 0, 0]);
    t.ponto(x + 1, y, [0, 0, 0, 0]);
  }
}

/**
 * Selo oficial em latão, seco, na comprovação.
 *
 * A primeira versão tinha uma estrela de oito pontas feita de duas cruzes, e o
 * resultado foi uma rodela de limão: raios saindo do centro para a casca é
 * exatamente o desenho de um citrino cortado, e nenhuma quantidade de latão
 * desfaz essa leitura.
 *
 * O que faz um selo parecer selo não são raios — é ANEL COM ESCRITA DENTRO. Três
 * barras curtas bastam: em vinte pixels ninguém lê o que está escrito num selo,
 * ninguém nunca leu, e é justamente por isso que ele funciona.
 */
function selo(t) {
  t.disco(16, 16, 11, P.latão);
  anel(t, 16, 16, 11, 9.6, P.latãoFundo);
  anel(t, 16, 16, 9.2, 8.4, P.latãoLuz);

  // Serrilha da borda: o dente de um selo prensado.
  for (let i = 0; i < 12; i++) {
    const a = (i * Math.PI) / 6;
    t.ponto(Math.round(16 + Math.cos(a) * 11.6), Math.round(16 + Math.sin(a) * 11.6), P.latãoClaro);
  }

  t.disco(16, 16, 7, P.latãoFundo);

  // A "escrita": três barras, a do meio mais longa.
  t.retangulo(11, 13, 10, 2, P.latãoLuz);
  t.retangulo(10, 16, 12, 2, P.latãoClaro);
  t.retangulo(12, 19, 8, 2, P.latãoLuz);
}

/**
 * Prendedor: segura a comprovação no formulário, e diz que as duas folhas são
 * um par só.
 *
 * É um PRENDEDOR DE MOLA, e não um clipe de arame — a primeira versão era o
 * clipe, e em 32 pixels o arame de um pixel de espessura vira um caixilho de
 * porta: a forma some e sobra o retângulo. O prendedor tem massa preta e dois
 * braços de metal, que é silhueta que sobrevive ao tamanho.
 */
function prendedor(t) {
  const metal = P.pedraClara;

  // O corpo: triângulo preto, base para baixo.
  for (let y = 0; y < 13; y++) {
    const meia = 4 + y;
    t.linhaH(16 - meia, 10 + y, meia * 2, P.tinta);
  }
  t.linhaH(6, 22, 20, escurecer(P.tinta, 0.2));

  // A dobra iluminada da frente.
  for (let y = 1; y < 12; y++) t.ponto(16 - (4 + y), 10 + y, P.pedra);
  t.linhaH(12, 10, 8, P.pedra);

  // Os dois braços de mola, abertos para cima.
  t.linhaV(10, 3, 8, metal);
  t.linhaV(11, 3, 7, metal);
  t.linhaH(10, 3, 5, clarear(metal, 0.35));
  t.linhaV(21, 3, 8, metal);
  t.linhaV(22, 3, 7, metal);
  t.linhaH(18, 3, 5, clarear(metal, 0.35));
}

/**
 * O brasão da prefeitura. Vai na parede atrás do guichê e no alto dos
 * memorandos — é o que diz, sem uma linha de texto, que aqui é o Estado.
 *
 * Uma engrenagem com uma folha dentro: a fábrica e a horta, que são as duas
 * coisas de que o corpus desta aula é feito.
 */
function brasao(t) {
  t.disco(16, 16, 12, P.latãoFundo);
  anel(t, 16, 16, 12, 10, P.latão);

  // Dentes da engrenagem.
  for (let i = 0; i < 8; i++) {
    const a = (i * Math.PI) / 4;
    const x = Math.round(16 + Math.cos(a) * 13.2);
    const y = Math.round(16 + Math.sin(a) * 13.2);
    t.retangulo(x - 1, y - 1, 3, 3, P.latãoClaro);
  }

  t.disco(16, 16, 9, P.tintaClara);

  // A FOLHA DENTRO, e a ordem aqui é tudo.
  //
  // A primeira versão pintava a nervura, depois os dois lados por cima dela, e
  // depois a nervura de novo — o resultado era um borrão verde com um risco. Em
  // dezoito pixels não cabe uma folha "desenhada": cabe uma SILHUETA. Agora é um
  // corpo sólido, uma sombra de um lado só, e a nervura por último, fina.
  for (let y = -6; y <= 6; y++) {
    // Largura máxima no meio, pontas nas extremidades: um losango redondo.
    const meia = Math.round(4.5 * Math.cos((y / 7) * (Math.PI / 2)));
    if (meia <= 0) continue;
    t.linhaH(16 - meia + Math.round(y * 0.35), 16 + y, meia * 2, P.folhaClara);
  }
  for (let y = -5; y <= 5; y++) {
    const meia = Math.round(4.5 * Math.cos((y / 7) * (Math.PI / 2)));
    if (meia <= 1) continue;
    t.linhaH(16 + Math.round(y * 0.35), 16 + y, meia, P.folha);
  }
  for (let k = -6; k <= 6; k++) t.ponto(16 + Math.round(k * 0.35), 16 + k, P.broto);
}

/**
 * O suporte do carimbo: a barra em que os dois carimbos ficam pousados, à
 * espera. No original ela é uma peça só, e é ela que faz o carimbo parecer um
 * OBJETO que se pega — e não um botão.
 */
function suporte(t) {
  t.retangulo(0, 12, LADO, 12, P.pedra);
  t.linhaH(0, 12, LADO, P.pedraClara);
  t.linhaH(0, 23, LADO, escurecer(P.pedra, 0.4));
  t.retangulo(0, 24, LADO, 3, escurecer(P.pedra, 0.55));

  // Encaixes: os dois berços onde os carimbos descansam.
  t.retangulo(4, 9, 9, 4, escurecer(P.pedra, 0.5));
  t.retangulo(19, 9, 9, 4, escurecer(P.pedra, 0.5));
  t.linhaH(4, 9, 9, P.tinta);
  t.linhaH(19, 9, 9, P.tinta);
}

export function pintarPeca(nome) {
  const t = new Tela(LADO, LADO);
  switch (nome) {
    case 'mesa': mesa(t); break;
    case 'carimbo_defere': carimbo(t, P.folhaLuz); break;
    case 'carimbo_indefere': carimbo(t, P.brasaClara); break;
    case 'selo': selo(t); break;
    case 'grampo': prendedor(t); break;
    case 'brasao': brasao(t); break;
    case 'suporte': suporte(t); break;
    default: throw new Error(`peça de guichê desconhecida: ${nome}`);
  }
  return t;
}
