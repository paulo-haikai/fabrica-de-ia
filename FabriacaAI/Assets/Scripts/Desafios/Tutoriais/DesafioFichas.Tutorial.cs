using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A explicação da bancada 4 — a mesa de corte.
    ///
    /// A regra que precisa de demonstração é "a emenda vale em TODAS as
    /// palavras". Escrita, ela soa como detalhe; vista, é o momento em que o
    /// aluno entende o jogo — porque quatro palavras encurtam ao mesmo tempo com
    /// um clique só, e o contador de fichas cai vários pontos de uma vez.
    ///
    /// A demonstração emenda o par MAIS frequente de propósito, para o efeito ser
    /// o maior possível. Emendar um par que aparece uma vez faria a tela mudar
    /// quase nada e a lição não apareceria.
    /// </summary>
    public partial class DesafioFichas
    {
        protected override IReadOnlyList<Passo> Explicacao() => new List<Passo>
        {
            new()
            {
                Texto = "Nara cortou estas palavras em letras soltas.\n" +
                        "Cada letra é uma FICHA, e são fichas demais.",
                Destaque = () => _mesa
            },
            new()
            {
                Texto = "O placar diz onde você está: quantas fichas tem agora,\n" +
                        "quantas precisa ter, e quantas emendas ainda pode dar.",
                Destaque = () => _placar.rectTransform
            },
            new()
            {
                Texto = "Estes são os pares que dá para emendar. Vou clicar num —\n" +
                        "repare no que acontece com as PALAVRAS, não com o par.",
                Acao = Demonstrar,
                Espera = 1.6f,
                Destaque = () => _mesa
            },
            new()
            {
                Texto = "A emenda valeu em TODAS as palavras de uma vez: é assim que\n" +
                        "tokenização funciona, senão a mesma palavra sairia partida\n" +
                        "de jeitos diferentes. E o número de fichas caiu.",
                Destaque = () => _placar.rectTransform
            },
            new()
            {
                Texto = "Os botões NÃO mostram quantas vezes o par aparece, e estão\n" +
                        "em ordem alfabética — senão bastaria clicar no maior. Você\n" +
                        "tem que olhar as palavras e achar o que se repete.",
                Destaque = () => _tesouras
            },
            new()
            {
                Texto = "Se errar, desfaça: o botão embaixo devolve a última emenda.\n" +
                        "Vou desfazer a minha — a mesa é sua.",
                Destaque = null
            }
        };

        /// <summary>
        /// Emenda o par mais frequente, que é o que o algoritmo guloso escolheria.
        ///
        /// É o mesmo par que a meta da rodada pressupõe, então a demonstração
        /// mostra um bom lance — e como ela é desfeita no fim, o aluno não ganha
        /// nada de graça, só vê como se faz.
        /// </summary>
        void Demonstrar()
        {
            var pares = _oficina.Pares();
            if (pares.Count == 0) return;
            Emendar(pares[0].a, pares[0].b);
        }

        /// <summary>Refaz a mesa: o aluno recebe as letras soltas e as emendas todas.</summary>
        protected override void AoFimDaExplicacao() => RecomecarNivel();
    }
}
