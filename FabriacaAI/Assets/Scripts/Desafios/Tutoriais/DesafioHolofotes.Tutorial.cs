using System.Collections.Generic;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A explicação da bancada 10 — os holofotes.
    ///
    /// A demonstração pergunta no ESCURO de propósito, com zero lâmpadas acesas.
    /// É o único jeito de o aluno ver que a máquina responde com o que está aceso
    /// e nada mais: no escuro ela não tem ideia, e a frase inteira estava ali na
    /// tela o tempo todo.
    ///
    /// Depois acende uma lâmpada errada e pergunta de novo. Duas perguntas
    /// perdidas que ensinam a regra e não gastam nada — perguntar aqui é
    /// ilimitado, o placar só conta para o aluno saber quantas tentativas levou.
    /// </summary>
    public partial class DesafioHolofotes
    {
        protected override IReadOnlyList<Passo> Explicacao() => new List<Passo>
        {
            new()
            {
                Texto = "Uma frase com o fim faltando. Ela tem que dizer qual palavra\n" +
                        "entra no espaço vazio.",
                Destaque = () => _frase
            },
            new()
            {
                Texto = "Mas ela não lê a frase toda. Ela só lê o que está ACESO —\n" +
                        "e agora não tem nada aceso. Vou perguntar assim mesmo:",
                Acao = Perguntar,
                Espera = 1.6f,
                Destaque = () => _frase
            },
            new()
            {
                Texto = "No escuro ela não tem ideia. A frase estava aqui inteira,\n" +
                        "na sua frente — e para ela não existia.",
                Destaque = () => _frase
            },
            new()
            {
                Texto = "Clicar numa palavra acende o holofote dela.\n" +
                        "Vou acender uma e perguntar de novo:",
                Acao = AcenderEPerguntar,
                Espera = 2.2f,
                Destaque = () => _frase
            },
            new()
            {
                Texto = "Errou — mas chutou ALGO, porque tinha algo para ler. Acendi\n" +
                        "uma palavra que não ajuda. O jogo é achar QUAIS carregam a resposta.",
                Destaque = () => _opcoes
            },
            new()
            {
                Texto = "E as lâmpadas são contadas — não dá para acender tudo, senão\n" +
                        "não haveria escolha. Esse aperto é o que se chama ATENÇÃO.",
                Destaque = () => _placar.rectTransform
            },
            new()
            {
                Texto = "Vou apagar tudo e devolver a frase. Perguntar é de graça,\n" +
                        "quantas vezes quiser.",
                Destaque = null
            }
        };

        /// <summary>
        /// Acende a primeira palavra da frase e pergunta.
        ///
        /// A primeira, que quase sempre é um artigo ou preposição: a demonstração
        /// tem que MOSTRAR um chute ruim, e a primeira palavra de uma frase é o
        /// lugar mais confiável para achar uma palavra que não informa nada.
        /// </summary>
        void AcenderEPerguntar()
        {
            Alternar(0);
            Perguntar();
        }

        /// <summary>Apaga os holofotes e zera o contador de perguntas.</summary>
        protected override void AoFimDaExplicacao() => RecomecarNivel();
    }
}
