using System.Collections.Generic;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A explicação da bancada 3 — o paper.io do arquivo.
    ///
    /// Um passo é obrigatório e os outros são luxo: dizer que ele caça o VAZIO.
    /// Toda criança que vê uma grade com marquinhas vermelhas supõe que as
    /// marquinhas são o objetivo — é o que Campo Minado, caça-palavras e caça ao
    /// tesouro ensinaram a vida inteira. Aqui é o contrário, e sem essa frase o
    /// aluno passa a rodada perseguindo justamente o que deve evitar.
    ///
    /// O tutorial não demonstra o gesto: dirigir com as setas se entende no
    /// primeiro segundo, e gastar um passo mostrando isso seria roubar do aluno a
    /// única coisa que ele faz sozinho aqui.
    /// </summary>
    public partial class DesafioArquivo
    {
        protected override IReadOnlyList<Passo> Explicacao() => new List<Passo>
        {
            new()
            {
                Texto = "Esta é a tabela de pares do Aurélio. Cada casinha pergunta\n" +
                        "se alguém já escreveu uma palavra logo depois da outra.",
                Destaque = () => _tela != null ? (RectTransform)_tela.transform : null
            },
            new()
            {
                Texto = "As casinhas vermelhas são os pares que existem de verdade.\n" +
                        "Repare em quantas são.",
                Destaque = () => _tela != null ? (RectTransform)_tela.transform : null
            },
            new()
            {
                Texto = "Você não está atrás delas. Você está atrás do VAZIO:\n" +
                        "dê a volta em torno de uma região e ela vira sua.",
                Destaque = () => _tela != null ? (RectTransform)_tela.transform : null
            },
            new()
            {
                Texto = "Setas para andar. Encostar num par vermelho corta o seu\n" +
                        "traço e você perde a volta — só isso.",
                Destaque = null
            }
        };

        /// <summary>
        /// A demonstração não moveu o bonequinho nem tomou nada, então não há o
        /// que desfazer. Declarado para quem vier acrescentar um passo com ação
        /// saber onde desfazer — as bancadas 5 e 7 precisaram, esta não.
        /// </summary>
        protected override void AoFimDaExplicacao() { }
    }
}
