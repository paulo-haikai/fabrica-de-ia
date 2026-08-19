using System.Collections.Generic;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A explicação da bancada 6 — o corredor de Iara.
    ///
    /// TRÊS COISAS, e nenhuma a mais:
    ///
    ///   1. Você carrega um pacote de informação e precisa entregá-lo na saída.
    ///   2. Este lugar TRAPACEIA. Tombar é de graça.
    ///   3. A saída não fica quieta, e quando se muda, sobe.
    ///
    /// A segunda não pode faltar. Sem ela, o primeiro chão que cede parece defeito
    /// do jogo, e quem acha que o jogo está quebrado para de tentar em vez de ficar
    /// curioso. Avisar que a trapaça é a regra transforma o mesmo tombo em piada, e é
    /// a piada que segura vinte salas.
    ///
    /// O QUE ESTA EXPLICAÇÃO NÃO DIZ, de propósito: o que o pacote tem dentro, e por
    /// que as salas sobem. Isso é o fecho da bancada, e o fecho só funciona se ele
    /// chegar lá com a pergunta na cabeça. Contar agora seria dar a resposta antes de
    /// alguém ter perguntado — que é o defeito de toda aula que começa pela
    /// definição.
    ///
    /// NÃO HÁ DEMONSTRAÇÃO AUTOMÁTICA, ao contrário das outras bancadas. Correr e
    /// pular se entende no primeiro segundo com o dedo na tecla, e uma demonstração
    /// roubaria do aluno justamente a parte que ele já sabe fazer. A sala 1 não tem
    /// traição nenhuma — ela É a demonstração, jogada por ele.
    /// </summary>
    public partial class DesafioMalha
    {
        protected override IReadOnlyList<Passo> Explicacao() => new List<Passo>
        {
            new()
            {
                Texto = "Iara te entregou um PACOTE DE INFORMAÇÃO — é aquele fardo\n" +
                        "nas suas costas. Ele precisa chegar até a saída.\n\n" +
                        "Setas para correr, espaço para pular. Segurar o espaço\n" +
                        "pula mais alto que tocar de leve.",
                Destaque = () => _bonecoNaTela != null ? (RectTransform)_bonecoNaTela.transform : null
            },
            new()
            {
                Texto = "Um aviso, e é sério: este lugar TRAPACEIA.\n" +
                        "O chão cede, espinhos sobem do piso, blocos despencam\n" +
                        "do teto — e a saída às vezes se muda de lugar.\n\n" +
                        "Você NÃO vai ver a armadilha antes da primeira vez.\n" +
                        "É assim mesmo: você cai, e a partir daí você sabe onde ela está.",
                Destaque = () => _palco
            },
            new()
            {
                Texto = "Tombar aqui não custa nada: você volta na hora e tenta\n" +
                        "de novo, quantas vezes quiser. É esse o jogo.\n\n" +
                        "São dez salas, sorteadas de um acervo de trinta. No fim,\n" +
                        "Iara abre a máquina e mostra por onde a informação passou.",
                Destaque = null
            }
        };

        /// <summary>
        /// A explicação não jogou nada: não moveu o boneco, não gastou sala, não
        /// entregou pacote. Então não há o que desfazer, e remontar o nível aqui só
        /// faria o aluno ver a sala 1 nascer duas vezes.
        ///
        /// Declarado, e não herdado em silêncio, porque as bancadas 5 e 7 precisaram
        /// desfazer e quem chegar aqui depois vai procurar este método.
        /// </summary>
        protected override void AoFimDaExplicacao() { }
    }
}
