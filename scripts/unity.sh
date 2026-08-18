#!/usr/bin/env bash
# Roda o Unity sem interface, para conferir, gerar a cena e publicar.
#
#     scripts/unity.sh conferir   # só compila os scripts e relata
#     scripts/unity.sh cena       # (re)gera Assets/Scenes/Atelie.unity
#     scripts/unity.sh web        # gera a cena se faltar e compila para navegador
#
# O EDITOR PRECISA ESTAR FECHADO: o Unity tranca a pasta do projeto e o
# batchmode falha com "another instance is running" sem explicar direito.
#
# A saída interessante fica no log, não no terminal — o Unity manda quase tudo
# para o arquivo. Por isso o script imprime a cauda dele no fim.
set -euo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJETO="$RAIZ/FabriacaAI"
# A versão vem do próprio projeto: ele já foi promovido de 6000.0 para 6000.5
# uma vez, e um script fixado numa versão só volta a quebrar no próximo salto.
VERSAO="$(sed -n 's/^m_EditorVersion: //p' "$RAIZ/FabriacaAI/ProjectSettings/ProjectVersion.txt" | tr -d '\r')"
UNITY="/c/Program Files/Unity/Hub/Editor/$VERSAO/Editor/Unity.exe"
LOG="$RAIZ/.unity-batch.log"

case "${1:-web}" in
  conferir) METODO="FabricaDeIA.Editor.Compilacao.Conferir" ;;
  cena)     METODO="FabricaDeIA.Editor.ConstrutorDeCena.Gerar" ;;
  web)      METODO="FabricaDeIA.Editor.Compilacao.Web" ;;
  *) echo "uso: $0 [conferir|cena|web]" >&2; exit 2 ;;
esac

if [ ! -x "$UNITY" ]; then
  echo "Unity 6000.0.38f1 não encontrado em: $UNITY" >&2
  exit 1
fi

if tasklist //FI "IMAGENAME eq Unity.exe" //NH 2>/dev/null | grep -q Unity.exe; then
  echo "O Unity Editor está aberto. Feche-o antes de rodar em batchmode." >&2
  exit 1
fi

echo "rodando $METODO…"
set +e
"$UNITY" -quit -batchmode -nographics \
  -projectPath "$(cygpath -w "$PROJETO")" \
  -logFile "$(cygpath -w "$LOG")" \
  -executeMethod "$METODO"
CODIGO=$?
set -e

echo "--- fim do log ---"
grep -E "COMPILACAO:|CENA:|BUILD:|error CS[0-9]+|Exception:" "$LOG" | tail -25 || true
echo "--- log completo em $LOG (saída $CODIGO) ---"
exit $CODIGO
