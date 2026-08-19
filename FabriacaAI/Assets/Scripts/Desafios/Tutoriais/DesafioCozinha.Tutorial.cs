using System.Collections.Generic;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A explicação da bancada 9 — ligue os pontos.
    ///
    /// Ligar pontos não precisa de tutorial; toda criança já fez isso em papel.
    /// O que precisa de tutorial é o que está ESCRITO nos cartões — que aquele
    /// texto esquisito foi a máquina que escreveu, e que é a única pista que
    /// existe.
    ///
    /// Por isso a demonstração não ensina o gesto: aponta o vocabulário.
    /// </summary>
    public partial class DesafioCozinha
    {
        protected override IReadOnlyList<Passo> Explicacao() => new List<Passo>
        {
            new()
            {
                Texto = "Três máquinas. Cada uma escreveu essas frases sozinha.",
                Destaque = () => _pontosEsquerda.Count > 0 ? _pontosEsquerda[0] : null
            },
            new()
            {
                Texto = "Do outro lado, o que cada uma comeu — o texto que ela leu\n" +
                        "antes de aprender a escrever.",
                Destaque = () => _pontosDireita.Count > 0 ? _pontosDireita[0] : null
            },
            new()
            {
                Texto = "A pista está nas palavras. Uma máquina que comeu o livro da\n" +
                        "horta fala de planta e semente — não tem como não falar.",
                Destaque = () => _mesa
            },
            new()
            {
                Texto = "Pegue o pontinho da máquina e puxe o traço até a comida dela.\n" +
                        "Errou, o traço some e você tenta de novo.",
                Destaque = null
            }
        };

        /// <summary>
        /// A demonstração não ligou nada, então não há o que desfazer.
        ///
        /// Fica declarado assim mesmo: sem este método, a próxima pessoa que
        /// acrescentar um passo com ação vai procurar onde desfazer e não vai
        /// achar. As bancadas 5 e 7 precisaram desfazer, esta não.
        /// </summary>
        protected override void AoFimDaExplicacao() { }
    }
}
