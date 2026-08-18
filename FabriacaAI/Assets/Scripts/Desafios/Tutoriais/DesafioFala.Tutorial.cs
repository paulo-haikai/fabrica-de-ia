using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A explicação da bancada 11 — fazer ela falar.
    ///
    /// Esta é a bancada onde as porcentagens finalmente aparecem escritas, e a
    /// demonstração existe para dizer uma coisa que o aluno não vai acreditar
    /// sozinho: a opção de 60% NÃO é a certa por ser a de 60%. O caminho até o
    /// alvo passa por palavras improváveis, e escolher sempre a maior é o jeito
    /// mais rápido de perder.
    ///
    /// A demonstração escolhe uma palavra, mostra a frase crescer e o passo ser
    /// cobrado — o mecanismo — e evita de propósito a palavra que fecharia a
    /// rodada.
    /// </summary>
    public partial class DesafioFala
    {
        protected override IReadOnlyList<Passo> Explicacao() => new List<Passo>
        {
            new()
            {
                Texto = "Um começo de frase, e uma palavra em que ela precisa chegar.\n" +
                        "Você guia — palavra por palavra.",
                Destaque = () => _texto
            },
            new()
            {
                Texto = "Estas são as três continuações que ela mais viu. A\n" +
                        "porcentagem é real: contagem do arquivo de Aurélio sobre o total.",
                Destaque = () => _escolhas
            },
            new()
            {
                Texto = "Vou escolher uma. Olhe a frase crescer:",
                Acao = Demonstrar,
                Espera = 1.5f,
                Destaque = () => _texto
            },
            new()
            {
                Texto = "A frase andou uma palavra, e as opções mudaram — ela está\n" +
                        "olhando de outro lugar. É isso que um modelo faz para escrever:\n" +
                        "uma palavra, olha de novo, outra palavra.",
                Destaque = () => _escolhas
            },
            new()
            {
                Texto = "E custou um passo. Os passos são contados, e o caminho até\n" +
                        "o alvo é mais curto que eles — mas só se você escolher bem.",
                Destaque = () => _placar.rectTransform
            },
            new()
            {
                Texto = "O aviso mais importante: a opção de maior porcentagem NÃO é\n" +
                        "a resposta. Ela é a mais comum, não a que leva ao alvo — clicar\n" +
                        "sempre na maior barra dá frase sem graça, e não chega lá.",
                Destaque = () => _escolhas
            },
            new()
            {
                Texto = "Vou devolver a frase ao começo e os passos todos.\n" +
                        "O alvo está escrito lá em cima — repare você mesmo quando\n" +
                        "ele aparecer entre as três.",
                Destaque = null
            }
        };

        /// <summary>
        /// Escolhe a continuação mais provável que NÃO seja o alvo.
        ///
        /// Se a mais provável for justamente o alvo, escolhê-la venceria a rodada
        /// no meio da explicação e o aluno começaria a bancada com o primeiro
        /// nível de graça. Descartá-la também é honesto com a lição do último
        /// passo: a maior barra não é a resposta.
        /// </summary>
        void Demonstrar()
        {
            var opcoes = _modelo.Continuacoes(Ponta)
                                .Where(c => c.Para != Bigrama.Fim)
                                .ToList();
            if (opcoes.Count == 0) return;

            var outra = opcoes.FindIndex(c => c.Para != _alvo);
            Escolher(opcoes[outra >= 0 ? outra : 0].Para);
        }

        /// <summary>Devolve o começo de frase e os passos cheios.</summary>
        protected override void AoFimDaExplicacao() => RecomecarNivel();
    }
}
