#!/usr/bin/env bash
# Diz se o Unity compilou limpo, olhando SÓ a última compilação.
#
#     scripts/unity-status.sh            # espera o log estabilizar e relata
#     scripts/unity-status.sh --agora    # relata sem esperar
#
# Por que existe: o Editor.log é acumulativo e guarda todos os erros da sessão.
# Um `grep "error CS"` na cauda do arquivo devolve erros já consertados há horas
# e faz a gente perseguir fantasma. O que importa é o trecho depois do último
# "Tundra build" — que é onde está o resultado da compilação mais recente.
set -uo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOG="$RAIZ/FabriacaAI/Logs/Editor.log"

if [ ! -f "$LOG" ]; then
  echo "log não encontrado: $LOG" >&2
  exit 1
fi

if [ "${1:-}" != "--agora" ]; then
  # Espera o log parar de crescer: enquanto cresce, o Unity ainda trabalha.
  ANT=-1; ESTAVEL=0
  for _ in $(seq 1 60); do
    sleep 5
    TAM=$(stat -c %s "$LOG")
    if [ "$TAM" = "$ANT" ]; then ESTAVEL=$((ESTAVEL + 5)); else ESTAVEL=0; fi
    ANT=$TAM
    [ "$ESTAVEL" -ge 20 ] && break
  done
fi

# O Unity imprime o resumo da compilação ANTES de repetir os erros no console.
# Ou seja: os erros de uma compilação aparecem DEPOIS da linha "Tundra build" que
# a fecha. Então a janela que interessa é a que começa na ÚLTIMA linha de build —
# tomar da penúltima, como a primeira versão fazia, arrastava os erros da
# compilação anterior e dava falso alarme mesmo com o build atual limpo.
ULTIMA_LINHA=$(grep -n "Tundra build" "$LOG" | tail -1 | cut -d: -f1)
[ -z "$ULTIMA_LINHA" ] && ULTIMA_LINHA=1

ULTIMA=$(sed -n "${ULTIMA_LINHA}p" "$LOG")
ERROS=$(tail -n "+$ULTIMA_LINHA" "$LOG" | grep -E "error CS[0-9]+" | sort -u)

echo "${ULTIMA:-sem compilação registrada}"

if [ -n "$ERROS" ]; then
  echo "--- erros da compilação atual ---"
  printf '%s\n' "$ERROS" | head -20
  exit 1
fi

echo "COMPILAÇÃO: limpa"
