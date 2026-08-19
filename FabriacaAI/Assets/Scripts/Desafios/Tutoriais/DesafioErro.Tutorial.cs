using System.Collections.Generic;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A explicação da bancada 7 — o robô corredor.
    ///
    /// O passo que importa é o primeiro, e ele desfaz um mal-entendido que
    /// custaria a bancada inteira: o aluno vê uma corrida de dinossauro e vai
    /// procurar a tecla de pular. Não há. Se ele descobrir isso sozinho, no
    /// meio da rodada, vai achar que o jogo travou.
    ///
    /// Por isso a demonstração solta o robô com a margem errada de propósito.
    /// Ver a lata bater é o que ensina, em dois segundos, que a batida é
    /// consequência do número — e não falta de reflexo de ninguém.
    /// </summary>
    public partial class DesafioErro
    {
        protected override IReadOnlyList<Passo> Explicacao() => new List<Passo>
        {
            new()
            {
                Texto = "Este robô corre sozinho. Você NÃO tem tecla de pular —\n" +
                        "e não vai precisar.",
                Destaque = () => _pista
            },
            new()
            {
                Texto = "O que você ajusta é a margem de erro dele: a que distância\n" +
                        "do obstáculo ele decide agir.",
                Destaque = () => _painelDeAjustes
            },
            new()
            {
                Texto = "Vou deixar a margem bem apertada e soltar. Repare no que\n" +
                        "acontece.",
                Acao = SoltarComMargemRuim,
                Espera = 3.5f,
                Destaque = () => _pista
            },
            new()
            {
                Texto = "Ele bateu. Não por falta de reflexo — reflexo ele não tem.\n" +
                        "Bateu porque reagiu tarde demais, e quem escolheu isso\n" +
                        "foi o número.",
                Destaque = () => _painelDeAjustes
            },
            new()
            {
                Texto = "Aumente a margem, solte de novo, veja o que muda. Dez\n" +
                        "segundos limpos e a rodada é sua.",
                Destaque = null
            }
        };

        /// <summary>
        /// Solta o robô com uma margem que garante a batida.
        ///
        /// Trinta unidades é o mínimo que o painel permite, e a essa distância
        /// nenhum pulo termina a tempo — a demonstração precisa da batida, e uma
        /// batida que às vezes não acontece explicaria o contrário do que quer.
        /// </summary>
        void SoltarComMargemRuim()
        {
            _margemDoPulo = 30f;
            _margemDoAbaixar = 30f;
            DesenharPainel();
            Soltar();
        }

        /// <summary>
        /// Devolve a margem ao valor de partida.
        ///
        /// A demonstração estragou o ajuste de propósito. Sem desfazer, o aluno
        /// começaria com a pior calibração possível e levaria a primeira rodada
        /// inteira só para voltar ao ponto de partida.
        /// </summary>
        protected override void AoFimDaExplicacao()
        {
            _margemDoPulo = 120f;
            _margemDoAbaixar = 120f;
            _tempoAbaixado = 0.35f;
            RecomecarNivel();
        }
    }
}
