using System.Collections.Generic;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A explicação da bancada 7 — o mostrador do erro.
    ///
    /// A demonstração precisa medir DUAS vezes, com um giro entre elas. Uma
    /// medição só mostra um número, e um número sozinho não ensina nada. Duas
    /// medições em torno de um giro mostram a coisa que importa: o erro respondeu
    /// ao botão, mas só depois de medir — antes de medir, nem a máquina sabe se
    /// piorou.
    ///
    /// É a lição que a bancada 8 vai completar. Aqui o aluno tem que sentir na
    /// mão o custo de tatear seis botões; lá ele vê a máquina fazer isso sozinha
    /// e entende por que não se treina uma rede a dedo.
    /// </summary>
    public partial class DesafioErro
    {
        protected override IReadOnlyList<Passo> Explicacao() => new List<Passo>
        {
            new()
            {
                Texto = "O mostrador de Seu Ilo está desafinado. Estes botões afinam —\n" +
                        "e cada um mexe numa coisa diferente que você não sabe qual é.",
                Destaque = () => _painelBotoes
            },
            new()
            {
                Texto = "Aqui em cima fica o ERRO: o tamanho da desafinação.\n" +
                        "Agora está apagado, porque ninguém mediu ainda.",
                Destaque = () => _leitura.rectTransform
            },
            new()
            {
                Texto = "Medir acende o número. Vou medir:",
                Acao = MedirUmaVez,
                Espera = 1.3f,
                Destaque = () => _leitura.rectTransform
            },
            new()
            {
                Texto = "Este é o erro de agora. Quanto menor, mais afinado.\n" +
                        "A barra é o mesmo número desenhado.",
                Destaque = () => _agulha.parent as RectTransform
            },
            new()
            {
                Texto = "Vou girar um botão e medir de novo. Repare que o número NÃO\n" +
                        "muda quando eu giro — só quando eu meço.",
                Acao = GirarEMedir,
                Espera = 2.2f,
                Destaque = () => _leitura.rectTransform
            },
            new()
            {
                Texto = "Mudou, e só descobri depois de medir — é assim de verdade:\n" +
                        "a máquina testa e compara. Por isso digo “melhorou” ou “piorou”.",
                Destaque = () => _leitura.rectTransform
            },
            new()
            {
                Texto = "E as medições são contadas. Você tem um número limitado delas —\n" +
                        "girar é de graça, medir é que custa.",
                Destaque = () => _historico.rectTransform
            },
            new()
            {
                Texto = "Vou devolver o mostrador como estava. Boa sorte: são três\n" +
                        "rodadas, e a última tem seis botões.",
                Destaque = null
            }
        };

        /// <summary>Mede uma vez, para o número sair do traço e aparecer.</summary>
        void MedirUmaVez() => Medir();

        /// <summary>
        /// Gira um botão e mede — nesta ordem, com a fala do passo dizendo para
        /// olhar o número entre as duas coisas.
        ///
        /// O giro é grande (meia volta) de propósito: um giro pequeno poderia
        /// mudar o erro na terceira casa decimal e o aluno concluiria que o botão
        /// não faz nada.
        /// </summary>
        void GirarEMedir()
        {
            Girar(0, 0.5f);
            Medir();
        }

        /// <summary>
        /// Refaz a rodada: mostrador novo, medições cheias.
        ///
        /// Sem isto o aluno herdaria duas medições gastas e um botão torto por
        /// mim — e o placar de medições é o recurso escasso da bancada.
        /// </summary>
        protected override void AoFimDaExplicacao() => RecomecarNivel();
    }
}
