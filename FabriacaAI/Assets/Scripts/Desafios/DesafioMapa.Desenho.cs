using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FabricaDeIA.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// O desenho da grade da bancada 5.
    ///
    /// A peça é PERSISTENTE: existe um objeto por casa e ele é movido, não
    /// recriado. A primeira versão destruía e remontava o tabuleiro a cada
    /// mudança, e o resultado era um jogo tecnicamente correto que não dava para
    /// jogar — as peças teleportavam, e num match-3 é a queda que diz de onde
    /// cada peça veio. Sem esse fio, o jogador não sabe o que mudou.
    /// </summary>
    public partial class DesafioMapa
    {
        const float LadoPeca = 104f;
        const float VaoPeca = 6f;
        const float TempoDeQueda = 0.17f;

        RectTransform _tabuleiro;
        Button[,] _pecas;
        Text _placar;

        /// <summary>
        /// As três cores das famílias.
        ///
        /// Saem da paleta do próprio jogo e foram escolhidas por distância de
        /// LUMINOSIDADE, não só de matiz: dourado é claro, turquesa é médio,
        /// brasa é escuro. Quem não distingue matiz ainda separa as três.
        ///
        /// Continua sendo cor como canal único, o que não basta para daltonismo
        /// severo — a palavra escrita ajuda, mas não resolve. Marcador de forma
        /// por família é o próximo passo.
        /// </summary>
        static Color CorDoGrupo(int grupo) => grupo switch
        {
            0 => Cores.Luz,
            1 => Cores.Vidro,
            _ => Cores.Brasa
        };

        Vector2 Posicao(int c, int l)
        {
            var largura = Colunas * LadoPeca + (Colunas - 1) * VaoPeca;
            var altura = Linhas * LadoPeca + (Linhas - 1) * VaoPeca;
            return new Vector2(
                -largura / 2f + (c + 0.5f) * (LadoPeca + VaoPeca) - VaoPeca / 2f,
                altura / 2f - (l + 0.5f) * (LadoPeca + VaoPeca) + VaoPeca / 2f);
        }

        void OnDestroy() => Widgets.Mao(false);

        void MontarTabuleiro()
        {
            Widgets.Mao(true);

            if (_tabuleiro != null) Destroy(_tabuleiro.gameObject);

            var largura = Colunas * LadoPeca + (Colunas - 1) * VaoPeca;
            var altura = Linhas * LadoPeca + (Linhas - 1) * VaoPeca;

            _tabuleiro = Widgets.Painel("Tabuleiro", Area, Color.clear);
            Widgets.Fixar(_tabuleiro, new Vector2(0.5f, 0.5f), new Vector2(0f, -16f),
                          new Vector2(largura, altura));

            // OS ENCAIXES, ANTES DAS PEÇAS.
            //
            // Uma casa vazia mostrava o fundo escuro do salão, e o tabuleiro
            // piscava buraco preto a cada estouro — parecia defeito, não jogo. O
            // encaixe é uma casa vazia DESENHADA: fica sempre lá, embaixo, e o
            // que se vê no lugar da peça estourada é a marcenaria do tabuleiro.
            //
            // Vêm primeiro no hierarquia de propósito: o uGUI desenha na ordem
            // dos filhos, então tudo o que for criado depois cai por cima.
            for (var l = 0; l < Linhas; l++)
            {
                for (var c = 0; c < Colunas; c++)
                {
                    var encaixe = Widgets.Painel($"encaixe{c}_{l}", _tabuleiro, Cores.TintaClara);
                    Widgets.Fixar(encaixe, new Vector2(0.5f, 0.5f), Posicao(c, l),
                                  new Vector2(LadoPeca, LadoPeca));
                }
            }

            _pecas = new Button[Colunas, Linhas];
            for (var l = 0; l < Linhas; l++)
                for (var c = 0; c < Colunas; c++)
                    _pecas[c, l] = CriarPeca(c, l, false);

            DesenharPlacar();
        }

        /// <summary>
        /// Cria a peça de uma casa. Se <paramref name="vindaDeCima"/>, ela nasce
        /// acima do tabuleiro para poder CAIR até o lugar — é o que faz a peça
        /// nova entrar em cena em vez de aparecer do nada.
        /// </summary>
        Button CriarPeca(int c, int l, bool vindaDeCima)
        {
            var peca = _grade[c, l];
            var botao = Widgets.Botao($"p{c}_{l}", _tabuleiro, peca.palavra,
                                      CorDoGrupo(peca.grupo), Cores.TintaOpaca, 13);

            var retangulo = (RectTransform)botao.transform;
            Widgets.Fixar(retangulo, new Vector2(0.5f, 0.5f),
                          Posicao(c, vindaDeCima ? -1 : l),
                          new Vector2(LadoPeca, LadoPeca));

            // Clique simples e ARRASTAR, os dois. O clique duplo-toque funciona em
            // trackpad ruim e em quem nunca jogou; o arrastar é o gesto que quem
            // já jogou match-3 tenta primeiro, e não achá-lo faz o jogo parecer
            // quebrado. Sai mais barato aceitar os dois do que escolher errado.
            var coluna = c;
            var linha = l;
            botao.onClick.AddListener(() => Tocar(coluna, linha));

            var gatilho = botao.gameObject.AddComponent<EventTrigger>();
            Adicionar(gatilho, EventTriggerType.PointerDown, () => Pegar(coluna, linha));
            Adicionar(gatilho, EventTriggerType.PointerEnter, () => Arrastar(coluna, linha));
            Adicionar(gatilho, EventTriggerType.PointerUp, Soltar);

            return botao;
        }

        static void Adicionar(EventTrigger gatilho, EventTriggerType tipo, System.Action acao)
        {
            var entrada = new EventTrigger.Entry { eventID = tipo };
            entrada.callback.AddListener(_ => acao());
            gatilho.triggers.Add(entrada);
        }

        /// <summary>Repinta a peça de uma casa sem recriá-la.</summary>
        void Pintar(int c, int l)
        {
            var botao = _pecas[c, l];
            if (botao == null) return;

            var peca = _grade[c, l];
            var escolhida = _escolhida.HasValue &&
                            _escolhida.Value.coluna == c && _escolhida.Value.linha == l;

            botao.GetComponent<Image>().color = escolhida ? Cores.Papel : CorDoGrupo(peca.grupo);
            var texto = botao.GetComponentInChildren<Text>();
            texto.text = peca.palavra;
            texto.color = Cores.TintaOpaca;
        }

        /// <summary>Repinta todas — usado ao trocar a escolha.</summary>
        void Repintar()
        {
            if (_pecas == null) return;
            for (var l = 0; l < Linhas; l++)
                for (var c = 0; c < Colunas; c++)
                    Pintar(c, l);
        }

        /// <summary>Manda cada peça deslizar até a casa em que ela agora está.</summary>
        void DeslizarTudo()
        {
            for (var l = 0; l < Linhas; l++)
            {
                for (var c = 0; c < Colunas; c++)
                {
                    if (_pecas[c, l] == null) continue;
                    Widgets.Deslizar((RectTransform)_pecas[c, l].transform,
                                     Posicao(c, l), TempoDeQueda);
                    Pintar(c, l);
                }
            }
        }

        /// <summary>Some com a peça estourada, deixando o buraco à vista.</summary>
        void SumirPeca(int c, int l)
        {
            var botao = _pecas[c, l];
            if (botao == null) return;

            Widgets.Pulsar((RectTransform)botao.transform, 1.25f, 0.14f);
            Destroy(botao.gameObject, 0.14f);
            _pecas[c, l] = null;
        }

        /// <summary>
        /// O placar: uma linha por cor, com o quanto falta.
        ///
        /// Por cor porque a meta é por cor. Um número único esconderia justamente
        /// a informação que decide a próxima jogada — qual família está atrasada.
        /// </summary>
        void DesenharPlacar()
        {
            if (_placar == null)
            {
                _placar = Widgets.Texto("Placar", Area, 16, TextAnchor.LowerCenter, Cores.Papel);
                Widgets.Faixa(_placar.rectTransform, false, 24f, 4f);
            }

            var partes = new string[_grupos.Count];
            for (var g = 0; g < _grupos.Count; g++)
            {
                var faltam = Mathf.Max(0, _meta - _estouradas[g]);
                partes[g] = faltam == 0 ? "pronto" : $"faltam {faltam}";
            }
            _placar.text = string.Join("      ", partes);
        }

        /// <summary>Sacode a peça de uma troca que não deu em nada.</summary>
        void Tremer(int coluna, int linha)
        {
            if (_pecas == null || _pecas[coluna, linha] == null) return;
            Widgets.Tremer((RectTransform)_pecas[coluna, linha].transform, 6f, 0.16f);
        }
    }
}
