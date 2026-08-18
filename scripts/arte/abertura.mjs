/**
 * A arte da abertura: céu, colina, torres de vento e o companheiro.
 *
 * A cena tem seis batidas e um raio no meio. A tempestade chegando é o relógio
 * dela — o aluno sente a hora de olhar sem que ninguém escreva "atenção" na
 * tela. Para isso funcionar, o céu precisa MUDAR, e mudar suave.
 *
 * A técnica: dois céus prontos, um de dia bom e um de tempestade, ambos com o
 * gradiente inteiro pintado. O jogo desenha o de tempestade por cima do outro e
 * sobe a opacidade dele com o tempo. Isso dá uma transição contínua sem shader,
 * sem gradiente em tempo de execução e sem depender de nada que o WebGL possa
 * implementar diferente.
 *
 * O companheiro é a peça mais importante do jogo inteiro, mesmo aparecendo
 * quinze segundos: é a única vez que se vê uma IA funcionando direito, e é contra
 * essa lembrança que as doze bancadas trabalham. Por isso ele tem duas versões
 * desenhadas com cuidado — inteiro e queimado — e não uma só com filtro escuro.
 */

import { Tela, aleatorio } from './png.mjs';
import { P, ABERTURA as A, escurecer, clarear, misturar } from './paleta.mjs';

/** O céu é gerado alto e estreito; o jogo o estica na largura. */
export const CEU_LARGURA = 32;
export const CEU_ALTURA = 120;

export const LADO_COMPANHEIRO = 24;

// --------------------------------------------------------------------- céu

/**
 * Um gradiente vertical, do topo ao horizonte.
 *
 * Os últimos 20% ficam de uma cor só: é onde a colina entra, e gradiente atrás
 * de silhueta é trabalho perdido.
 */
function ceu(t, alto, baixo) {
  for (let y = 0; y < CEU_ALTURA; y++) {
    const k = Math.min(1, y / (CEU_ALTURA * 0.78));
    t.linhaH(0, y, CEU_LARGURA, misturar(alto, baixo, k));
  }
}

/** Nuvens carregadas, para o céu de tempestade não ser só um degradê escuro. */
function ceuDeTempestade(t) {
  ceu(t, A.ceuAltoRuim, A.ceuBaixoRuim);

  const r = aleatorio(9091);
  for (let n = 0; n < 14; n++) {
    const cx = r() * CEU_LARGURA;
    const cy = r() * CEU_ALTURA * 0.5;
    const raio = 3 + r() * 5;
    const cor = r() < 0.5
      ? escurecer(A.ceuAltoRuim, 0.25)
      : clarear(A.ceuAltoRuim, 0.12);
    t.disco(cx, cy, raio, `${cor}70`);
  }
}

// ------------------------------------------------------------------ cenário

/**
 * A colina. Sai larga e baixa, para o jogo esticá-la sem deformar a curva.
 * O topo leva uma faixa mais clara — é a grama pegando sol de lado.
 */
function colina(t, cor) {
  const largura = t.largura;
  const altura = t.altura;

  for (let x = 0; x < largura; x++) {
    // Uma lombada suave, mais alta à esquerda, como no original.
    const k = x / (largura - 1);
    // Lombada: sobe à esquerda, cai à direita, com uma barriga no meio. Os
    // coeficientes são o que dá "colina" em vez de "rampa" — sem o seno a
    // silhueta vira uma reta inclinada e o horizonte perde profundidade.
    const topo = Math.round(altura * (0.30 + 0.34 * k - 0.24 * Math.sin(k * Math.PI)));
    for (let y = topo; y < altura; y++) t.ponto(x, y, cor);
    t.ponto(x, topo, clarear(cor, 0.28));
    t.ponto(x, topo + 1, clarear(cor, 0.12));
  }
}

/** O sol: disco com um halo que o jogo desbota junto com o céu. */
function sol(t) {
  const c = t.largura / 2 - 0.5;
  for (let k = 5; k >= 1; k--) {
    const a = Math.round(22 * k / 5).toString(16).padStart(2, '0');
    t.disco(c, c, c * (0.45 + k * 0.11), `${A.sol}${a}`);
  }
  t.disco(c, c, c * 0.44, A.sol);
  t.disco(c - 1, c - 1, c * 0.2, '#ffffff');
}

/** O mastro da torre de vento. As pás são desenhadas à parte, para girarem. */
function mastro(t, cor) {
  const x = Math.floor(t.largura / 2);
  for (let y = 0; y < t.altura; y++) {
    // Dois pixels de largura em vez de um: esticado na tela, um pixel só some
    // e a torre fica um fio de cabelo.
    t.ponto(x - 1, y, cor);
    t.ponto(x, y, cor);
    // Engrossa para a base: torre de verdade é cônica, e um pixel a mais
    // embaixo é o que faz ela parecer plantada no chão.
    if (y > t.altura * 0.55) t.ponto(x + 1, y, escurecer(cor, 0.2));
  }
  t.ponto(x, 0, clarear(cor, 0.35));
  t.ponto(x - 1, 0, clarear(cor, 0.35));
}

/** Uma pá: barra estreita que afina na ponta, com o eixo na esquerda. */
function pa(t, cor) {
  for (let x = 0; x < t.largura; x++) {
    const k = x / (t.largura - 1);
    const meia = Math.max(0.5, (1 - k) * (t.altura / 2 - 0.5));
    for (let dy = -meia; dy <= meia; dy++) {
      t.ponto(x, t.altura / 2 + dy, cor);
    }
    t.ponto(x, t.altura / 2 - meia, clarear(cor, 0.3));
  }
}

/** Um pingo de chuva, riscado na diagonal. */
function gota(t) {
  for (let k = 0; k < t.altura; k++) {
    t.ponto(t.largura - 1 - Math.floor(k * 0.35), k, `${A.chuva}70`);
  }
}

/** Faísca: quatro pontas, para ler como estalo elétrico e não como estrela. */
function faisca(t) {
  const c = (t.largura - 1) / 2;
  t.linhaH(0, c, t.largura, A.clarao);
  t.linhaV(c, 0, t.altura, A.clarao);
  t.ponto(c - 1, c - 1, P.luz);
  t.ponto(c + 1, c + 1, P.luz);
  t.disco(c, c, 1.6, '#ffffff');
}

// ------------------------------------------------------------ companheiro

/**
 * O companheiro, inteiro.
 *
 * Um lampião flutuante de latão e vidro, com um olho aceso. Solarpunk: a
 * máquina do mundo do jogo é feita de metal honesto e luz, não de plástico
 * branco. O olho é uma barra horizontal e não um ponto — barra lê como
 * "aparelho olhando", ponto lê como "bicho".
 */
function companheiroBom(t) {
  const L = LADO_COMPANHEIRO;
  const c = L / 2;

  // Corpo: cápsula de latão.
  t.retangulo(c - 6, 6, 12, 12, P.latão);
  t.retangulo(c - 5, 5, 10, 1, P.latãoClaro);
  t.retangulo(c - 5, 18, 10, 1, P.latãoFundo);
  t.linhaV(c - 6, 7, 10, P.latãoClaro);
  t.linhaV(c + 5, 7, 10, P.latãoFundo);

  // Visor de vidro, com o olho aceso.
  t.retangulo(c - 5, 9, 10, 5, P.vidroFundo);
  t.retangulo(c - 4, 10, 8, 3, P.vidro);
  t.linhaH(c - 3, 11, 6, P.vidroLuz);
  t.ponto(c - 3, 10, '#ffffff');

  // Antena e aro superior.
  t.linhaV(c, 1, 4, P.latãoClaro);
  t.disco(c, 1, 1.4, P.luzForte);
  t.retangulo(c - 3, 4, 6, 1, P.latãoLuz);

  // Aletas laterais: dão silhueta e sugerem que ele flutua.
  t.retangulo(c - 9, 11, 3, 2, P.latãoClaro);
  t.retangulo(c + 6, 11, 3, 2, P.latãoClaro);

  // Halo de luz por baixo — é o que o faz parecer no ar.
  for (let k = 0; k < 4; k++) {
    const a = (40 - k * 9).toString(16).padStart(2, '0');
    t.linhaH(c - 4 + k, 19 + k, 8 - k * 2, `${P.luz}${a}`);
  }
}

/**
 * O companheiro, depois do raio.
 *
 * Mesma silhueta — precisa ser reconhecível como o mesmo — com o visor
 * rachado, o olho apagado, fuligem e a antena torta. É deliberado que ele
 * continue inteiro por fora: o que quebrou foi a fala, não a carcaça, e é
 * disso que a aula trata.
 */
function companheiroRuim(t) {
  const L = LADO_COMPANHEIRO;
  const c = L / 2;
  const queimado = misturar(P.latão, P.tinta, 0.45);

  t.retangulo(c - 6, 6, 12, 12, queimado);
  t.retangulo(c - 5, 5, 10, 1, misturar(P.latãoClaro, P.tinta, 0.35));
  t.retangulo(c - 5, 18, 10, 1, P.tinta);
  t.linhaV(c - 6, 7, 10, misturar(P.latãoClaro, P.tinta, 0.3));
  t.linhaV(c + 5, 7, 10, P.tinta);

  // Visor apagado, com a rachadura atravessando.
  t.retangulo(c - 5, 9, 10, 5, escurecer(P.vidroFundo, 0.55));
  t.retangulo(c - 4, 10, 8, 3, escurecer(P.vidroFundo, 0.3));
  for (let k = 0; k < 5; k++) {
    t.ponto(c - 4 + k, 9 + (k % 3), P.tinta);
  }
  // Um resto de brilho num canto: ele não morreu, emudeceu.
  t.ponto(c + 3, 12, `${P.vidroClaro}90`);

  // Antena torta e apagada.
  t.ponto(c, 4, queimado);
  t.ponto(c, 3, queimado);
  t.ponto(c + 1, 2, queimado);
  t.ponto(c + 2, 2, escurecer(queimado, 0.3));
  t.retangulo(c - 3, 4, 6, 1, queimado);

  // Aletas amassadas.
  t.retangulo(c - 9, 12, 3, 2, queimado);
  t.retangulo(c + 6, 10, 2, 2, queimado);

  // Fuligem.
  const r = aleatorio(31337);
  for (let i = 0; i < 10; i++) {
    t.ponto(c - 6 + Math.floor(r() * 12), 6 + Math.floor(r() * 12), `${P.tinta}80`);
  }
}

// -------------------------------------------------------------- catálogo

/**
 * As peças da abertura. Cada uma sai num PNG próprio, e não num atlas: são
 * poucas, de tamanhos muito diferentes, e o jogo estica algumas delas — o que
 * num atlas puxaria pixel do vizinho.
 */
export const PECAS = [
  ['ceu_bom', CEU_LARGURA, CEU_ALTURA, t => ceu(t, A.ceuAltoBom, A.ceuBaixoBom)],
  ['ceu_ruim', CEU_LARGURA, CEU_ALTURA, ceuDeTempestade],
  // Larga de propósito: esticada de 160 para 1920 pixels, cada degrau da
  // silhueta virava um bloco de doze. Em 320 o degrau cai para metade.
  ['colina_boa', 320, 96, t => colina(t, A.colinaBoa)],
  ['colina_ruim', 320, 96, t => colina(t, A.colinaRuim)],
  // O sol tem halo, e halo de 40 pixels ampliado vira anel serrilhado.
  ['sol', 72, 72, sol],
  ['mastro_bom', 6, 64, t => mastro(t, A.torreBoa)],
  ['mastro_ruim', 6, 64, t => mastro(t, A.torreRuim)],
  ['pa_boa', 14, 4, t => pa(t, A.torreBoa)],
  ['pa_ruim', 14, 4, t => pa(t, A.torreRuim)],
  ['gota', 4, 12, gota],
  ['faisca', 9, 9, faisca],
  ['companheiro_bom', LADO_COMPANHEIRO, LADO_COMPANHEIRO, companheiroBom],
  ['companheiro_ruim', LADO_COMPANHEIRO, LADO_COMPANHEIRO, companheiroRuim]
];

export function pintarPeca(nome) {
  const item = PECAS.find(([n]) => n === nome);
  if (!item) throw new Error(`peça de abertura desconhecida: ${nome}`);
  const [, largura, altura, pintar] = item;
  const t = new Tela(largura, altura);
  pintar(t);
  return t;
}
