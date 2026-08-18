using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A explicação da bancada 6 — a malha que escolhe.
    ///
    /// Esta explicação tem um trabalho a mais que as outras: além de ensinar o
    /// jogo, ela tem que DESMONTAR uma intuição que o aluno já traz. Todo mundo
    /// imagina rede neural como uma bolinha achando caminho, tipo pinball. Isso é
    /// árvore de decisão. Se ele entrar na bancada com essa imagem, vai olhar a
    /// animação e ver o que já esperava, sem aprender nada.
    ///
    /// Então o passo 3 roda um passe inteiro e o passo 4 diz, com a tela ainda
    /// acesa na frente dele, a única frase que importa aqui: não teve caminho
    /// escolhido, teve tudo acendendo de uma vez.
    /// </summary>
    public partial class DesafioMalha
    {
        protected override IReadOnlyList<Passo> Explicacao() => new List<Passo>
        {
            new()
            {
                Texto = "Iara terminou a malha. Estas três palavras são o que ela\n" +
                        "está lendo agora — e cada uma virou seis números.\n" +
                        "É só isso que a rede recebe: números.",
                Destaque = null
            },
            new()
            {
                Texto = "Do outro lado tem uma lâmpada para CADA palavra que ela sabe.\n" +
                        "São 318, e a resposta dela é a que acender mais forte.\n" +
                        "Cada neurônio do meio se liga a todas as 318 — desenho só as\n" +
                        "três da sua aposta, senão a tela vira mancha.",
                Destaque = null
            },
            new()
            {
                Texto = "Vou apostar numa e deixar a luz atravessar. Olhe a malha,\n" +
                        "não o placar.",
                Acao = Demonstrar,
                Espera = 3.4f,
                Destaque = null
            },
            new()
            {
                Texto = "Repare no que NÃO aconteceu: nenhuma bolinha escolheu caminho.\n" +
                        "A luz saiu de todas as entradas ao mesmo tempo, toda conexão\n" +
                        "carregou um número, e cada neurônio somou o que veio de TODOS\n" +
                        "os anteriores. Rede neural é mistura, não desvio.",
                Destaque = null
            },
            new()
            {
                Texto = "Dourado empurra a favor, turquesa empurra CONTRA.\n" +
                        "Metade da malha trabalha subtraindo — e é aí que a aposta\n" +
                        "óbvia costuma morrer.",
                Destaque = () => _legenda.rectTransform
            },
            new()
            {
                Texto = "E olhe os neurônios escuros do meio. A soma deles deu negativa,\n" +
                        "então eles zeraram e não empurraram nada adiante. Uma parte da\n" +
                        "malha simplesmente não participa desta palavra.",
                Destaque = null
            },
            new()
            {
                Texto = "A palavra que ganhou entra na frase e volta para a entrada.\n" +
                        "A malha roda outra vez, inteira, com a janela um passo à frente.\n" +
                        "É assim que a frase cresce aqui em cima — uma linha por passe.",
                Destaque = () => _historico
            },
            new()
            {
                Texto = "Ela não lembra do que escreveu. Ela relê e recalcula, sempre.\n" +
                        "Agora é sua vez: aposte antes de a luz chegar do outro lado.",
                Destaque = () => _escolhas
            }
        };

        /// <summary>
        /// Aposta na primeira opção e deixa a animação inteira rodar.
        ///
        /// Na PRIMEIRA opção, não na certa. A demonstração existe para ele ver a luz
        /// atravessar; apostar certo de propósito ensinaria que existe um jeito de
        /// saber a resposta de antemão, que é exatamente o contrário do que a
        /// bancada quer dizer. Se acertar, foi sorte — e a explicação não comenta.
        /// </summary>
        void Demonstrar()
        {
            if (_candidatas.Count == 0) return;
            Apostar(_candidatas[0]);
        }

        /// <summary>
        /// Refaz a rodada: frase nova, apostas cheias, luz apagada.
        ///
        /// A aposta da demonstração não pode contar no placar dele — nem para bem
        /// nem para mal —, e a frase que ela começou a escrever é minha, não dele.
        /// </summary>
        protected override void AoFimDaExplicacao() => RecomecarNivel();
    }
}
