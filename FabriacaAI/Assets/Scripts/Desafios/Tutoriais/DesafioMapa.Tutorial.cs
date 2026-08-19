using System.Collections.Generic;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A explicação da bancada 5 — o mapa.
    ///
    /// Quase toda criança já jogou um match-3, então a demonstração não gasta
    /// passo ensinando a trocar peça: ela gasta os passos na única coisa que este
    /// match-3 tem de diferente, que é o que está ESCRITO nas peças.
    ///
    /// Por isso a troca de mostra acontece cedo e sem cerimônia. O que interessa
    /// é o instante seguinte, quando as três palavras que saíram aparecem juntas
    /// na linha de instrução — é ali que o aluno vê que a cor não era enfeite.
    /// </summary>
    public partial class DesafioMapa
    {
        protected override IReadOnlyList<Passo> Explicacao() => new List<Passo>
        {
            new()
            {
                Texto = "Cada peça é uma palavra. Troque duas vizinhas para\n" +
                        "juntar três da mesma cor.",
                Destaque = () => _tabuleiro
            },
            new()
            {
                Texto = "A cor não é enfeite: é uma família que a máquina montou\n" +
                        "sozinha. Três da mesma cor são três palavras que ela\n" +
                        "acha parecidas.",
                Destaque = () => _tabuleiro != null && _tabuleiro.childCount > 0
                    ? _tabuleiro.GetChild(0) as RectTransform
                    : null
            },
            new()
            {
                Texto = "Olhe o que sai quando eu estouro.",
                Acao = TrocarDeMostra,
                Espera = 1.6f,
                Destaque = () => _tabuleiro
            },
            new()
            {
                Texto = "Bento pede um tanto de CADA cor — não adianta caçar só\n" +
                        "a que estiver mais fácil.",
                Destaque = () => _placar != null ? _placar.rectTransform : null
            },
            new()
            {
                Texto = "Vou devolver o tabuleiro como estava. É seu.",
                Destaque = null
            }
        };

        /// <summary>
        /// Faz, sozinha, uma troca que estoura — usando o mesmo caminho do aluno.
        ///
        /// Procura o primeiro par de vizinhas cuja troca fecha trinca e chama
        /// <c>Tocar</c> duas vezes, que é exatamente o que dois toques dele
        /// fariam. Simular pelo caminho de verdade é o que garante que a
        /// demonstração não possa divergir do jogo — se a regra mudar, isto muda
        /// junto ou quebra alto.
        /// </summary>
        void TrocarDeMostra()
        {
            for (var l = 0; l < Linhas; l++)
            {
                for (var c = 0; c < Colunas; c++)
                {
                    for (var d = 0; d < 2; d++)
                    {
                        var c2 = c + (d == 0 ? 1 : 0);
                        var l2 = l + (d == 0 ? 0 : 1);
                        if (c2 >= Colunas || l2 >= Linhas) continue;

                        Trocar(c, l, c2, l2);
                        var fecha = Trincas().Count > 0;
                        Trocar(c, l, c2, l2);

                        if (!fecha) continue;

                        Tocar(c, l);
                        Tocar(c2, l2);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Refaz a rodada do zero.
        ///
        /// A demonstração estourou peças e contou pontos no placar. Sem refazer,
        /// o aluno começaria com parte da meta já cumprida por outra pessoa.
        /// </summary>
        protected override void AoFimDaExplicacao() => RecomecarNivel();
    }
}
