#!/usr/bin/env node
/**
 * Monta os atlas e escreve tudo em `FabriacaAI/Assets/Resources/Arte`.
 *
 *     npm run arte
 *
 * Sai daqui um PNG por folha e um `arte.json` com o mapa de nome → índice. O
 * C# lê o manifesto e nunca escreve índice cru: quando a arte mudar de ordem,
 * o jogo continua pedindo "canteiro" e recebendo canteiro.
 *
 * As folhas são grades uniformes de propósito — é o que deixa o Unity fatiar
 * por tamanho de célula sem ninguém abrir o Sprite Editor à mão.
 */

import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

import { Tela } from './png.mjs';
import { TILES, LADO as LADO_TILE, pintarTile } from './tileset.mjs';
import { BANCADAS, LADO as LADO_BANCADA, pintarBancada } from './bancadas.mjs';
import { folhaDe, pessoa, LARGURA as L_PESSOA, ALTURA as A_PESSOA, DIRECOES, QUADROS } from './elenco.mjs';
import { PECAS as PECAS_ABERTURA, pintarPeca } from './abertura.mjs';

const AQUI = dirname(fileURLToPath(import.meta.url));
const DESTINO = join(AQUI, '..', '..', 'FabriacaAI', 'Assets', 'Resources', 'Arte');

/**
 * O elenco em parâmetros.
 *
 * Os nomes e ofícios vêm da versão web (`src/game/personagens.ts`) e não se
 * mexe neles sem motivo: a turma que jogou o protótipo já chama a costureira
 * de Nara. O que mudou foi só a forma — de cor solta para índice de paleta,
 * que é o que mantém os treze dentro do mesmo mundo.
 */
export const ELENCO = [
  { id: 'jogador', nome: 'Você', oficio: 'aprendiz', pele: 0, cabelo: 1, corte: 'curto', roupa: 2, acessorio: 'nenhum' },
  { id: 'e1', nome: 'Tico', oficio: 'aprendiz da estufa', pele: 1, cabelo: 0, corte: 'curto', roupa: 9, acessorio: 'folha' },
  { id: 'e2', nome: 'Dona Ciça', oficio: 'guarda-livros', pele: 3, cabelo: 4, corte: 'coque', roupa: 5, acessorio: 'oculos' },
  { id: 'e3', nome: 'Mestre Aurélio', oficio: 'arquivista', pele: 2, cabelo: 5, corte: 'raspado', roupa: 0, acessorio: 'oculos' },
  { id: 'e4', nome: 'Nara', oficio: 'costureira', pele: 4, cabelo: 0, corte: 'trancas', roupa: 3, acessorio: 'lenco' },
  { id: 'e5', nome: 'Bento', oficio: 'cartógrafo de jardim', pele: 1, cabelo: 2, corte: 'longo', roupa: 1, acessorio: 'folha' },
  { id: 'e6', nome: 'Iara', oficio: 'tecelã de circuitos', pele: 3, cabelo: 0, corte: 'coque', roupa: 9, acessorio: 'visor' },
  { id: 'e7', nome: 'Seu Ilo', oficio: 'afinador', pele: 2, cabelo: 4, corte: 'curto', roupa: 10, acessorio: 'oculos' },
  { id: 'e8', nome: 'Rosa', oficio: 'treinadora', pele: 4, cabelo: 3, corte: 'trancas', roupa: 11, acessorio: 'nenhum' },
  { id: 'e9', nome: 'Chef Amaro', oficio: 'cozinheiro', pele: 3, cabelo: 0, corte: 'chapeu', roupa: 11, acessorio: 'avental_couro' },
  { id: 'e10', nome: 'Lumi', oficio: 'acendedora de lampiões', pele: 1, cabelo: 4, corte: 'curto', roupa: 9, acessorio: 'visor' },
  { id: 'e11', nome: 'Vovó Zi', oficio: 'contadora de histórias', pele: 4, cabelo: 4, corte: 'longo', roupa: 8, acessorio: 'lenco' },
  { id: 'e12', nome: 'Sereno', oficio: 'guardião do ateliê', pele: 2, cabelo: 5, corte: 'longo', roupa: 0, acessorio: 'folha' }
];

/** Distribui peças de tamanho fixo numa grade de `colunas`. */
function atlas(pecas, largura, altura, colunas) {
  const linhas = Math.ceil(pecas.length / colunas);
  const t = new Tela(colunas * largura, linhas * altura);
  pecas.forEach((peca, i) => {
    t.colar(peca, (i % colunas) * largura, Math.floor(i / colunas) * altura);
  });
  return t;
}

function escrever(nome, tela) {
  const caminho = join(DESTINO, nome);
  writeFileSync(caminho, tela.paraPng());
  console.log(`  ${nome.padEnd(22)} ${tela.largura}x${tela.altura}`);
}

function main() {
  mkdirSync(DESTINO, { recursive: true });
  console.log('gerando arte em Assets/Resources/Arte');

  // --- cenário ---
  const COLUNAS_TILE = 8;
  escrever(
    'tileset_atelie.png',
    atlas(TILES.map(([nome]) => pintarTile(nome)), LADO_TILE, LADO_TILE, COLUNAS_TILE)
  );

  // --- bancadas ---
  const COLUNAS_BANCADA = 4;
  escrever(
    'bancadas.png',
    atlas(BANCADAS.map((_, i) => pintarBancada(i)), LADO_BANCADA, LADO_BANCADA, COLUNAS_BANCADA)
  );

  // --- elenco: uma pessoa por linha, 16 poses por linha ---
  const poses = ELENCO.flatMap(p => folhaDe(pessoa(p)));
  escrever('elenco.png', atlas(poses, L_PESSOA, A_PESSOA, DIRECOES * QUADROS));

  // --- abertura ---
  //
  // Peça por peça, cada uma no seu PNG. São de tamanhos muito diferentes e o
  // jogo estica algumas na tela; num atlas, esticar puxaria pixel do vizinho.
  for (const [nome] of PECAS_ABERTURA) {
    escrever(`abertura_${nome}.png`, pintarPeca(nome));
  }

  // --- manifesto ---
  // Tudo em lista, nada em objeto-dicionário: o `JsonUtility` do Unity lê
  // array de objeto e não lê mapa de chave livre. Custa um `Find` no C# e
  // evita arrastar um parser de JSON para dentro do jogo.
  const manifesto = {
    gerado: new Date().toISOString(),
    tileset: {
      arquivo: 'tileset_atelie.png',
      lado: LADO_TILE,
      colunas: COLUNAS_TILE,
      tiles: TILES.map(([nome], i) => ({ nome, indice: i }))
    },
    bancadas: {
      arquivo: 'bancadas.png',
      lado: LADO_BANCADA,
      colunas: COLUNAS_BANCADA,
      ordem: BANCADAS.map(([id]) => id)
    },
    elenco: {
      arquivo: 'elenco.png',
      largura: L_PESSOA,
      altura: A_PESSOA,
      direcoes: DIRECOES,
      quadros: QUADROS,
      // O índice do quadro é linha * 16 + direção * 4 + quadro.
      pessoas: ELENCO.map(({ id, nome, oficio }, linha) => ({ id, nome, oficio, linha }))
    },
    abertura: {
      prefixo: 'abertura_',
      pecas: PECAS_ABERTURA.map(([nome, largura, altura]) => ({ nome, largura, altura }))
    }
  };
  writeFileSync(join(DESTINO, 'arte.json'), `${JSON.stringify(manifesto, null, 2)}\n`);
  console.log(`  arte.json              ${TILES.length} tiles, ${BANCADAS.length} bancadas, ${ELENCO.length} pessoas`);
}

// Só gera quando chamado direto; `previa.mjs` importa o ELENCO daqui.
if (process.argv[1] && process.argv[1].endsWith('gerar.mjs')) main();
