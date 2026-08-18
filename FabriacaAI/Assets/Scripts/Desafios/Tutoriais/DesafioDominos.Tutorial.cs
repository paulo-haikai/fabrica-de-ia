using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A explicação da bancada 2 — o dominó.
    ///
    /// A regra de encaixe é a coisa mais fácil de mostrar e a mais chata de
    /// escrever: "a ponta esquerda da peça tem que ser igual à palavra em que a
    /// corrente parou" é uma frase que ninguém termina de ler. Uma peça ENTRANDO
    /// na corrente, com a palavra compartilhada aparecendo uma vez só, dispensa a
    /// frase inteira.
    ///
    /// Por isso o passo do meio joga de verdade: procura na mão a peça que serve
    /// e a encaixa, exatamente como o clique do aluno faria.
    /// </summary>
    public partial class DesafioDominos
    {
        protected override IReadOnlyList<Passo> Explicacao() => new List<Passo>
        {
            new()
            {
                Texto = "Dona Ciça quer que a máquina diga uma frase.\n" +
                        "Ela começa em “" + _rodada.Inicio + "” e tem que chegar em “" +
                        _rodada.Alvo + "”.",
                Destaque = () => _mesa
            },
            new()
            {
                Texto = "Estas são suas peças: um par de palavras que alguém\n" +
                        "escreveu juntas — os risquinhos embaixo dizem quantas vezes.",
                Destaque = () => _bancada
            },
            new()
            {
                Texto = "Uma peça encaixa quando a ponta ESQUERDA dela é igual à\n" +
                        "palavra em que a corrente parou. Veja:",
                Acao = Demonstrar,
                Espera = 1.5f,
                Destaque = () => _mesa
            },
            new()
            {
                Texto = "A palavra em comum aparece uma vez só — é a emenda. Lendo\n" +
                        "a corrente da esquerda para a direita, sai uma frase.",
                Destaque = () => _mesa
            },
            new()
            {
                Texto = "Cuidado: peça pode encaixar e levar ao lugar errado. Se a\n" +
                        "corrente morrer, desfaça — de graça, quantas vezes quiser.",
                Destaque = null
            },
            new()
            {
                Texto = "Vou tirar a peça que coloquei. A corrente é sua.",
                Destaque = () => _mesa
            }
        };

        /// <summary>
        /// Encaixa a primeira peça da mão que serve na ponta atual.
        ///
        /// "A primeira que serve" e não "a certa": a demonstração ensina a REGRA
        /// DE ENCAIXE, não a solução do nível. Mostrar o caminho inteiro
        /// resolveria o quebra-cabeça na frente do aluno e não sobraria jogo.
        /// </summary>
        void Demonstrar()
        {
            var serve = _mao.FirstOrDefault(Encaixa);
            if (serve != null) Encaixar(serve);
        }

        /// <summary>
        /// Devolve o tabuleiro intacto. A peça da demonstração volta para a mão —
        /// o aluno tem que ter as peças todas e o orçamento inteiro.
        /// </summary>
        protected override void AoFimDaExplicacao() => RecomecarNivel();
    }
}
