using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Bancada 11 — a máquina de falar de Vovó Zi.
    ///
    /// Inspiração: LABIRINTO, na forma que jogo de puzzle chama de pathfinding —
    /// e o parente mais próximo em jogo comercial é o baralho de escolhas do Slay
    /// the Spire: a cada passo você recebe três opções e precisa chegar num
    /// destino, sem poder voltar.
    ///
    /// A máquina oferece três continuações possíveis para a frase, com a
    /// probabilidade de cada uma. O aluno escolhe uma, a frase cresce, e ela
    /// oferece três novas. O objetivo é chegar numa palavra-alvo antes de acabarem
    /// os passos.
    ///
    /// Por que isso ensina geração melhor que um botão "escrever":
    ///
    ///   · A ESCOLHA É A GERAÇÃO. Quem gera texto é quem escolhe entre as
    ///     candidatas. O aluno faz o papel do sorteio e sente que não há mágica
    ///     nenhuma ali — só uma escolha, e outra, e outra.
    ///   · A PROBABILIDADE FICA VISÍVEL. Cada opção mostra sua chance. Ele vê que
    ///     a máquina não "sabe" a próxima palavra: ela tem uma distribuição.
    ///   · O ALVO CRIA TENSÃO. Escolher sempre a mais provável costuma NÃO levar
    ///     ao alvo — e é aí que ele descobre a diferença entre a palavra mais
    ///     provável e a palavra que serve. É o problema de todo modelo gerador.
    /// </summary>
    public partial class DesafioFala : DesafioEmNiveis
    {
        public override string Etapa => "e11";
        public override string Titulo => "Fazer ela falar";

        protected override int Niveis => Rodadas11.Length;

        /// <summary>Passos disponíveis em cada rodada.</summary>
        static readonly int[] Rodadas11 = { 6, 7, 8 };

        Bigrama _modelo;
        Mulberry32 _sorteio;

        readonly List<string> _frase = new();
        string _alvo;
        int _passosRestantes;

        RectTransform _texto;
        RectTransform _escolhas;
        Text _placar;

        protected override void Preparar()
        {
            _modelo = new Bigrama(Corpus.Frases);
            _sorteio = new Mulberry32((uint)Rodadas.Semente());
        }

        protected override void MontarNivel()
        {
            _passosRestantes = Rodadas11[NivelAtual];
            SortearRodada();

            Painel.Rodape("clique numa palavra para ela continuar a frase");

            var alvo = Widgets.Texto("Alvo", Area, 17, TextAnchor.UpperCenter, Cores.Neblina);
            Widgets.Faixa(alvo.rectTransform, true, 22f);
            alvo.text = $"faça a frase chegar em “{_alvo}”";

            _texto = Widgets.Painel("Texto", Area, Cores.TintaClara);
            Widgets.Faixa(_texto, true, 92f, 28f);

            _placar = Widgets.Texto("Placar", Area, 15, TextAnchor.UpperCenter, Cores.Luz);
            Widgets.Faixa(_placar.rectTransform, true, 20f, 126f);

            var rotulo = Widgets.Texto("Rótulo", Area, 14, TextAnchor.UpperCenter, Cores.Neblina);
            Widgets.Faixa(rotulo.rectTransform, true, 18f, 154f);
            rotulo.text = "o que ela pode dizer agora, e a chance de cada uma";

            _escolhas = Widgets.Painel("Escolhas", Area, Color.clear);
            Widgets.Esticar(_escolhas);
            _escolhas.offsetMax = new Vector2(0f, -178f);

            Redesenhar();
        }

        /// <summary>
        /// Sorteia início e alvo garantindo que exista caminho.
        ///
        /// Caminha pelo grafo do modelo para escolher o alvo, do mesmo jeito que a
        /// bancada 2 faz: se foi a caminhada que produziu o alvo, então chegar
        /// nele é possível. O que muda aqui é que o aluno não tem as peças na mão
        /// — ele recebe três opções por vez, e algumas rodadas não levam a lugar
        /// nenhum.
        /// </summary>
        void SortearRodada()
        {
            _frase.Clear();

            for (var tentativa = 0; tentativa < 200; tentativa++)
            {
                var aberturas = _modelo.Continuacoes(Bigrama.Inicio)
                                       .Where(c => c.Para != Bigrama.Fim
                                                && !Corpus.EhLigacao(c.Para))
                                       .ToList();
                if (aberturas.Count == 0) break;

                var atual = aberturas[(int)(_sorteio.Proximo() * aberturas.Count)].Para;
                var inicio = atual;
                var visitadas = new HashSet<string> { atual };

                // O alvo fica a uma distância que caiba nos passos, mas não folgada:
                // dois passos de sobra dão espaço para errar e corrigir.
                var distancia = Mathf.Max(3, _passosRestantes - 2);
                var chegou = true;

                for (var i = 0; i < distancia; i++)
                {
                    var opcoes = _modelo.Continuacoes(atual)
                                        .Where(c => c.Para != Bigrama.Fim && !visitadas.Contains(c.Para))
                                        .ToList();
                    if (opcoes.Count == 0)
                    {
                        chegou = false;
                        break;
                    }
                    atual = opcoes[(int)(_sorteio.Proximo() * opcoes.Count)].Para;
                    visitadas.Add(atual);
                }

                // Alvo tem que ser palavra de conteúdo: "chegue em 'da'" não é
                // meta que alguém queira perseguir.
                if (!chegou || atual == inicio || Corpus.EhLigacao(atual)) continue;

                _frase.Add(inicio);
                _alvo = atual;
                return;
            }

            // Recurso de último caso, para nunca abrir a bancada vazia.
            _frase.Add("a");
            _alvo = "turma";
        }

        // ------------------------------------------------------------ pintura

        string Ponta => _frase[^1];

        void Redesenhar()
        {
            foreach (Transform filho in _texto) Destroy(filho.gameObject);
            foreach (Transform filho in _escolhas) Destroy(filho.gameObject);

            DesenharFrase();

            var opcoes = _modelo.Continuacoes(Ponta)
                                .Where(c => c.Para != Bigrama.Fim)
                                .ToList();

            _placar.text = $"passos que sobram: {_passosRestantes}";

            if (Ponta == _alvo)
            {
                Vencer();
                return;
            }

            if (_passosRestantes <= 0)
            {
                Perder("os passos acabaram");
                return;
            }

            if (opcoes.Count == 0)
            {
                Perder($"ela emudeceu — nunca viu nada depois de “{Ponta}”");
                return;
            }

            DesenharEscolhas(opcoes);
            Painel.Instruir($"a frase está em “{Ponta}” · falta chegar em “{_alvo}”");
            Painel.Acao(null, null);
        }

        void DesenharFrase()
        {
            var texto = Widgets.Texto("t", _texto, 22, TextAnchor.MiddleCenter, Cores.Papel);
            Widgets.Esticar(texto.rectTransform, 16f);
            texto.text = $"“{string.Join(" ", _frase)}…”";
        }

        /// <summary>
        /// As três candidatas mais prováveis, com a chance de cada uma.
        ///
        /// Três, e não todas: uma lista de vinte opções vira leitura de tabela.
        /// Três é o número que obriga a escolher e cabe num olhar — e é
        /// coincidentemente o que os jogos de baralho descobriram sobre oferta de
        /// escolha.
        ///
        /// A chance mostrada é a real: contagem daquele par dividida pelo total da
        /// linha. É a distribuição do modelo, sem arredondamento simpático.
        /// </summary>
        void DesenharEscolhas(List<Continuacao> opcoes)
        {
            var total = opcoes.Sum(o => o.Vezes);
            var mostradas = opcoes.Take(3).ToList();

            const float largura = 230f;
            for (var i = 0; i < mostradas.Count; i++)
            {
                var opcao = mostradas[i];
                var chance = (float)opcao.Vezes / total;

                // TODAS as opções na mesma cor.
                //
                // Antes, a que levava ao alvo era pintada de verde antes do clique. A
                // intenção era ajudar, e o efeito era destruir a bancada exatamente no
                // seu melhor momento: no passo em que o alvo está a uma escolha de
                // distância — o clímax da rodada — a decisão virava "clique no botão
                // verde". E o alvo já está escrito no alto da tela, então a cor não
                // informava nada que o aluno não pudesse ler; só tirava dele o
                // trabalho de reparar.
                var botao = Widgets.Botao($"o{i}", _escolhas, string.Empty,
                                          Cores.Madeira, Cores.Papel);
                Widgets.Fixar((RectTransform)botao.transform, new Vector2(0.5f, 1f),
                              new Vector2((i - (mostradas.Count - 1) / 2f) * (largura + 14f), -44f),
                              new Vector2(largura, 76f));
                Destroy(botao.GetComponentInChildren<Text>().gameObject);

                var palavra = Widgets.Texto("p", (RectTransform)botao.transform, 20,
                                            TextAnchor.UpperCenter, Cores.Papel);
                Widgets.Faixa(palavra.rectTransform, true, 26f, 6f);
                palavra.text = opcao.Para;

                var chanceTexto = Widgets.Texto("c", (RectTransform)botao.transform, 13,
                                                TextAnchor.LowerCenter, Cores.Luz);
                Widgets.Faixa(chanceTexto.rectTransform, false, 18f, 6f);
                chanceTexto.text = $"{chance * 100f:0}%  ·  {opcao.Vezes} risquinhos";

                var barra = Widgets.Barra("b", (RectTransform)botao.transform,
                                          new Vector2(0f, -4f), new Vector2(largura - 30f, 8f),
                                          Cores.Tinta, Cores.Luz);
                Widgets.Encher(barra, chance, largura - 30f);

                var escolhida = opcao.Para;
                botao.onClick.AddListener(() => Escolher(escolhida));
            }
        }

        void Escolher(string palavra)
        {
            _frase.Add(palavra);
            _passosRestantes--;
            Redesenhar();
        }

        // ------------------------------------------------------------ desfecho

        void Vencer()
        {
            Resolveu($"“{string.Join(" ", _frase)}”",
                "Cada palavra foi uma escolha entre três, e você viu a chance\n" +
                "de cada uma antes de escolher.\n\n" +
                "É isto que a máquina faz sozinha: sorteia, proporcional à\n" +
                "chance, e repete. Sem intenção, sem plano — só a próxima\n" +
                "palavra. E outra.");
        }

        void Perder(string motivo)
        {
            Falhou(motivo,
                $"A frase parou em: “{string.Join(" ", _frase)}”\n" +
                $"e o alvo era “{_alvo}”.\n\n" +
                "Seguir sempre a mais provável quase nunca leva onde você quer —\n" +
                "a máquina tem o mesmo problema: escolhe bem palavra por palavra\n" +
                "sem saber onde a frase vai terminar.",
                contaEstrela: _frase.Count >= 4);
        }
    }
}
