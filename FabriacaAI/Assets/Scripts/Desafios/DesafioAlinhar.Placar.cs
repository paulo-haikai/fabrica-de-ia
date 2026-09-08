using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// O PLACAR — o que ele escolheu, quanto cada escolha valeu, e por quê.
    ///
    /// Aparece DEPOIS da cena, e a ordem é o conteúdo. Se a tabela viesse antes, o
    /// aluno assistiria ao epílogo conferindo a própria nota, e o epílogo é sobre
    /// pessoas, não sobre ele. Vindo depois, ela responde a uma pergunta que ele já
    /// está fazendo: «por que deu esse final?».
    ///
    /// E ELA NÃO EXISTE PARA DAR NOTA. O placar do dia — certos, advertências,
    /// cota — é o da EMPRESA, e mede obediência ao manual. Este é outro: mede o que
    /// as decisões custaram a quem não estava na fila. Os dois quase nunca
    /// concordam, e é essa discordância que a tabela põe na tela:
    ///
    ///     carimbar as seis empresas ..... a empresa aprova, o placar cobra
    ///     barrar as seis empresas ....... seis advertências, o placar paga
    ///
    /// A CALIBRAGEM É DELIBERADA e está medida em ConferirPontuacao: quem segue o
    /// manual à risca do começo ao fim TERMINA NEGATIVO. Não porque tenha sido
    /// desleixado — porque o manual não tem uma linha sobre empresa nem sobre
    /// licença, e obedecer a um regulamento que se cala não é o mesmo que decidir
    /// bem. Basta recusar os pedidos irreversíveis e o data center para virar o
    /// sinal: três carimbos vermelhos, três advertências, e o final muda.
    ///
    /// NENHUM PONTO APARECE DURANTE O EXPEDIENTE. Um contador na tela viraria a
    /// bancada num jogo de otimizar pontos, e o aluno passaria a procurar o que o
    /// jogo premia em vez de decidir. A conta só é mostrada quando não dá mais para
    /// jogar em função dela.
    /// </summary>
    public partial class DesafioAlinhar
    {
        /// <summary>Uma linha do placar: o que foi feito, quantas vezes, e quanto vale.</summary>
        readonly struct LinhaDoPlacar
        {
            public readonly string Rotulo;
            public readonly int Quantas;
            public readonly int PorUma;
            public int Total => Quantas * PorUma;

            public LinhaDoPlacar(string rotulo, int quantas, int porUma)
            {
                Rotulo = rotulo; Quantas = quantas; PorUma = porUma;
            }
        }

        /// <summary>
        /// A tabela de pontos.
        ///
        /// OS PESOS NÃO SÃO GOSTO, são a distância entre quem paga e quem decide. O
        /// pedido comum é ±1: erra-se um caso, corrige-se no seguinte. Quem não
        /// tinha como provar vale 3, porque o erro ali não é sobre papel, é sobre
        /// uma pessoa que não pode voltar com a folha certa. E os irreversíveis
        /// valem 8, que é o maior número da tabela, porque são os únicos em que
        /// nada do que vier depois desfaz o que o carimbo fez.
        /// </summary>
        List<LinhaDoPlacar> MontarPlacar()
        {
            var pessoas = _log.Where(x => !x.Quem.EhEmpresa).ToList();
            var vulneraveis = pessoas.Where(x => x.Quem.Recem && x.Quem.Deferir).ToList();
            var coletivos = pessoas.Where(x => x.Quem.CustoColetivo).ToList();

            // O caso comum: nem vulnerável, nem escolha moral. Aqui o placar
            // concorda com o manual, e é de propósito — é a parte da bancada em que
            // conferir direito É decidir direito.
            var comuns = pessoas.Where(x => !x.Quem.CustoColetivo &&
                                            !(x.Quem.Recem && x.Quem.Deferir)).ToList();

            var empresas = _log.Where(x => x.Quem.EhEmpresa).ToList();

            var linhas = new List<LinhaDoPlacar>
            {
                new("leu as folhas e acertou o manual",
                    comuns.Count(x => x.Deferiu == x.Quem.Deferir), +1),
                new("carimbou contra o que a comprovação dizia",
                    comuns.Count(x => x.Deferiu != x.Quem.Deferir), -1),

                new("deferiu quem não tinha como provar",
                    vulneraveis.Count(x => x.Deferiu), +3),
                new("indeferiu quem não tinha como provar",
                    vulneraveis.Count(x => !x.Deferiu), -3),

                new("barrou pedido que cobrava de terceiros",
                    coletivos.Count(x => !x.Deferiu), +2),
                new("cedeu a pedido que cobrava de terceiros",
                    coletivos.Count(x => x.Deferiu && !x.Quem.Irreversivel), -3),
                new("assinou o que não volta atrás",
                    coletivos.Count(x => x.Deferiu && x.Quem.Irreversivel), -8),

                new("barrou empresa",
                    empresas.Count(x => !x.Deferiu), +2),
                new("carimbou empresa",
                    empresas.Count(x => x.Deferiu && !x.Quem.EhDataCenter), -2),
                new("carimbou o data center",
                    empresas.Count(x => x.Deferiu && x.Quem.EhDataCenter), -6)
            };

            // Só o que aconteceu vai para a tela. Linha zerada não informa nada e
            // rouba espaço de altura da que informa — e é altura sobrando que
            // impede as linhas de se encavalarem.
            return linhas.Where(l => l.Quantas > 0).ToList();
        }

        int Pontos() => MontarPlacar().Sum(l => l.Total);

        // ------------------------------------------------------------- a tela

        /// <summary>
        /// Altura de cada linha, e o número de onde toda a geometria do quadro sai.
        ///
        /// VINTE E CINCO, E NÃO VINTE E SETE, porque com dez linhas na tela o
        /// quadro a 27 empurrava a nota de rodapé para fora da área útil — a área é
        /// 426 de altura e o cartaz não tem para onde crescer. Os números abaixo
        /// foram conferidos para 5, 6, 8 e 10 linhas: sobram 15 pixels de margem
        /// interna em todos os casos, e a nota fica dentro da tela em todos.
        /// </summary>
        const float AlturaDaLinha = 25f;

        /// <summary>Onde o quadro fica, e a folga de topo e de base dele.</summary>
        const float CentroDoQuadro = 26f;
        const float MargemDoQuadro = 40f;

        /// <summary>
        /// Desenha o placar.
        ///
        /// CADA LINHA TEM ALTURA FIXA E POSIÇÃO CALCULADA, e o texto de cada célula
        /// é preso a uma linha só que encolhe se precisar (<c>UmaLinha</c>). É o que
        /// garante que nada se sobreponha: um rótulo comprido diminui de corpo em
        /// vez de quebrar por cima da linha de baixo — que foi como o manual da
        /// bancada ficou ilegível uma vez.
        /// </summary>
        void MostrarPlacar()
        {
            Limpar();
            Painel.Instruir(string.Empty);
            Painel.Rodape(string.Empty);

            var linhas = MontarPlacar();
            var total = linhas.Sum(l => l.Total);

            // A altura sai do número de linhas, e não de um valor fixo: linha
            // zerada não entra na tabela, então o quadro encolhe junto e nunca
            // sobra vão preto embaixo da última linha.
            var altura = MargemDoQuadro + (linhas.Count + 2) * AlturaDaLinha;

            var quadro = Widgets.Painel("Placar", _palco, new Color(0.09f, 0.10f, 0.13f, 0.97f));
            Widgets.Fixar(quadro, new Vector2(0.5f, 0.5f), new Vector2(0f, CentroDoQuadro),
                          new Vector2(760f, altura));

            var titulo = Widgets.Texto("t", quadro, 15, TextAnchor.UpperLeft, Cores.Neblina);
            PorNoQuadro(titulo.rectTransform, 22f, -10f, 500f);
            titulo.text = "O QUE VOCÊ DECIDIU";
            Widgets.UmaLinha(titulo, 11);

            var risco = Widgets.Painel("Risco", quadro, Cores.Neblina);
            PorNoQuadro(risco, 22f, -32f, 716f, 1f);
            risco.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.22f);

            var y = -40f;
            foreach (var l in linhas)
            {
                Escrever(quadro, 22f, y, 470f, l.Rotulo, Cores.Papel, TextAnchor.MiddleLeft);
                Escrever(quadro, 500f, y, 90f, $"× {l.Quantas}", Cores.Neblina, TextAnchor.MiddleRight);
                Escrever(quadro, 604f, y, 134f, Sinal(l.Total),
                         l.Total >= 0 ? Cores.Folha : Cores.Brasa, TextAnchor.MiddleRight);
                y -= AlturaDaLinha;
            }

            var linhaDoTotal = Widgets.Painel("Fio", quadro, Color.white);
            PorNoQuadro(linhaDoTotal, 22f, y - 2f, 716f, 1f);
            linhaDoTotal.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.22f);
            y -= 8f;

            Escrever(quadro, 22f, y, 470f, "TOTAL", Cores.Luz, TextAnchor.MiddleLeft, 17);
            Escrever(quadro, 604f, y, 134f, Sinal(total),
                     total >= 0 ? Cores.Folha : Cores.Brasa, TextAnchor.MiddleRight, 21);

            // O veredito em uma linha, e ele não julga a pessoa: diz o que a conta
            // deu. Quem ficou negativo não é mau caráter — é alguém que obedeceu a
            // um manual que se cala sobre metade do que passa pelo balcão.
            var nota = Widgets.Texto("n", _palco, 15, TextAnchor.UpperCenter, Cores.Neblina);
            Widgets.Fixar(nota.rectTransform, new Vector2(0.5f, 0.5f),
                          new Vector2(0f, CentroDoQuadro - altura / 2f - 22f),
                          new Vector2(760f, 40f));
            Widgets.UmaLinha(nota, 11);
            nota.text = _finalBom
                ? "Positivo. Você recusou o que o manual não olhava."
                : "Negativo. O manual não pedia isso de você — e ninguém pediu.";

            Painel.Acao("continuar", CenaPergunta);
        }

        static string Sinal(int n) => n > 0 ? "+" + n : n.ToString();

        void Escrever(RectTransform quadro, float x, float y, float largura, string texto,
                      Color cor, TextAnchor alinhamento, int corpo = 16)
        {
            var t = Widgets.Texto("l", quadro, corpo, alinhamento, cor);
            PorNoQuadro(t.rectTransform, x, y, largura);
            t.text = texto;
            Widgets.UmaLinha(t, 11);
        }

        /// <summary>
        /// Ancora no canto superior esquerdo do quadro, em pixels. Ancoragem
        /// pontual, como no manual: é ela que deixa a largura resolvida na hora e
        /// as colunas alinhadas sem esperar o canvas.
        /// </summary>
        static void PorNoQuadro(RectTransform reto, float x, float y, float largura,
                                float altura = AlturaDaLinha)
        {
            reto.anchorMin = reto.anchorMax = new Vector2(0f, 1f);
            reto.pivot = new Vector2(0f, 1f);
            reto.sizeDelta = new Vector2(largura, altura);
            reto.anchoredPosition = new Vector2(x, y);
        }
    }
}
