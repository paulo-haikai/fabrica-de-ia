using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Bancada 5 — o mapa de Bento.
    ///
    /// Inspiração: CANDY CRUSH. Uma grade de peças, troca entre vizinhas, três
    /// em linha estouram, o que está em cima cai e entra peça nova. Qualquer
    /// criança de doze anos já sabe jogar isto sem ler nada — e essa é metade do
    /// motivo de a bancada existir nesta forma.
    ///
    /// A outra metade é o que está ESCRITO nas peças. Cada peça é uma palavra, e
    /// a cor dela não é enfeite: é o grupo que a máquina montou sozinha, por
    /// coocorrência — palavras que ocupam o mesmo tipo de lugar nas frases,
    /// medidas por cosseno entre vetores de vizinhança (ver
    /// <see cref="Vizinhancas"/>). Estourar três da mesma cor é, literalmente,
    /// juntar três palavras que a máquina considera parecidas.
    ///
    /// POR QUE A COR ENTREGA A RESPOSTA, DE PROPÓSITO.
    ///
    /// Esta bancada era um CONNECTIONS: doze palavras embaralhadas, três grupos,
    /// o aluno separando no escuro. Era o gênero mais duro do jogo — agrupar por
    /// critério distribucional, que ninguém enuncia, com três vidas e 495
    /// combinações possíveis. A turma travava aqui.
    ///
    /// Pintar cada família de uma cor parece jogar a lição fora: se dá para casar
    /// por cor, para que ler a palavra? Só que o match-3 obriga a olhar a grade
    /// inteira muitas vezes por rodada, e o que está escrito nas peças entra pelos
    /// olhos junto. O aluno estoura «lousa · quadro · mesa» e, três jogadas
    /// depois, «entrou · saiu · chegou», e a regularidade aparece sem ninguém
    /// anunciar. O fecho da rodada só dá nome ao que ele já viu.
    ///
    /// A troca de dificuldade é honesta e vale a pena: antes ele tinha que
    /// DESCOBRIR os grupos; agora ele tem que NOTAR que os grupos existem. A
    /// segunda coisa é a lição; a primeira era só o obstáculo.
    /// </summary>
    public partial class DesafioMapa : DesafioEmNiveis
    {
        public override string Etapa => "e5";
        public override string Titulo => "O mapa das palavras";

        protected override int Niveis => _rodadas?.Count ?? Mapas.PorAula;

        const int Colunas = 6;
        const int Linhas = 6;

        /// <summary>
        /// Quantas peças de CADA cor a rodada pede.
        ///
        /// Por cor, e não no total, porque é o que obriga o aluno a trabalhar as
        /// três famílias. Uma meta única de total seria vencida martelando a cor
        /// mais fácil de casar, e ele terminaria sem ter lido dois terços das
        /// palavras.
        /// </summary>
        /// Subiu de 6/8/10: com a meta antiga a rodada acabava em menos de um
        /// minuto, e a bancada terminava antes de o aluno reparar no que estava
        /// escrito nas peças — que é a bancada inteira. A cascata entrega muitas
        /// peças por jogada, então a meta precisa ser generosa para o tabuleiro
        /// durar tempo de ser LIDO.
        static readonly int[] MetaPorCor = { 12, 16, 20 };

        Vizinhancas _mapa;
        List<List<Grupo>> _rodadas;

        List<Grupo> _grupos;
        Mulberry32 _sorteio;

        /// <summary>A grade. -1 em <c>grupo</c> é buraco à espera de queda.</summary>
        (string palavra, int grupo)[,] _grade;

        int[] _estouradas;
        int _meta;
        int _jogadas;
        (int coluna, int linha)? _escolhida;
        (int coluna, int linha)? _arrastando;
        bool _trocouArrastando;
        bool _travado;

        protected override void Preparar()
        {
            _mapa = new Vizinhancas(Corpus.Frases);
            _sorteio = new Mulberry32((uint)Rodadas.Semente());
            _rodadas = Mapas.Sortear(_mapa, Rodadas.Semente());
        }

        protected override void MontarNivel()
        {
            _grupos = _rodadas[NivelAtual];
            _meta = MetaPorCor[Mathf.Min(NivelAtual, MetaPorCor.Length - 1)];
            _estouradas = new int[_grupos.Count];
            _jogadas = 0;
            _escolhida = null;
            _travado = false;

            Encher();
            DesembaracarSePreciso();
            MontarTabuleiro();

            Painel.Rodape("troque duas peças vizinhas · três iguais em linha estouram");
            Painel.Instruir("junte três da mesma cor");
        }

        // ------------------------------------------------------------ a grade

        /// <summary>
        /// Enche a grade sem nenhuma trinca pronta.
        ///
        /// Começar com trinca já formada seria dar pontos antes da primeira
        /// jogada e, pior, esconder do aluno o que a jogada dele causou — a
        /// primeira coisa que ele precisa entender é que foi a troca DELE que
        /// estourou.
        /// </summary>
        void Encher()
        {
            _grade = new (string, int)[Colunas, Linhas];

            for (var l = 0; l < Linhas; l++)
            {
                for (var c = 0; c < Colunas; c++)
                {
                    for (var tentativa = 0; tentativa < 20; tentativa++)
                    {
                        _grade[c, l] = Sortear();
                        if (!FormaTrinca(c, l)) break;
                    }
                }
            }
        }

        (string palavra, int grupo) Sortear()
        {
            var g = (int)(_sorteio.Proximo() * _grupos.Count);
            var palavras = _grupos[g].Palavras;
            var p = palavras[(int)(_sorteio.Proximo() * palavras.Count)];
            return (p, g);
        }

        /// <summary>
        /// A peça em (c,l) fecha três iguais olhando só para trás?
        ///
        /// Só para trás porque o preenchimento anda da esquerda para a direita e
        /// de cima para baixo: o que está à frente ainda não existe.
        /// </summary>
        bool FormaTrinca(int c, int l)
        {
            var g = _grade[c, l].grupo;
            if (c >= 2 && _grade[c - 1, l].grupo == g && _grade[c - 2, l].grupo == g) return true;
            if (l >= 2 && _grade[c, l - 1].grupo == g && _grade[c, l - 2].grupo == g) return true;
            return false;
        }

        // ----------------------------------------------------------- a jogada

        /// <summary>
        /// Um toque: escolhe, ou troca com a escolhida se forem vizinhas.
        ///
        /// Dois toques em vez de arrastar. Arrastar depende de precisão de mouse
        /// e de touch bem calibrado, e a sala tem máquina velha e trackpad ruim;
        /// tocar duas vezes funciona em tudo e não pede coordenação nenhuma.
        /// </summary>
        void Tocar(int coluna, int linha)
        {
            if (_travado) return;

            // O botão dispara o clique ao soltar, inclusive quando o gesto foi um
            // arrastar que JÁ trocou as peças. Sem esta guarda, um arrastar valeria
            // por dois: a troca e mais uma seleção solta em cima dela.
            if (_trocouArrastando) { _trocouArrastando = false; return; }

            if (_escolhida == null)
            {
                _escolhida = (coluna, linha);
                Repintar();
                return;
            }

            var (ec, el) = _escolhida.Value;

            if (ec == coluna && el == linha)          // tocou na mesma: desfaz
            {
                _escolhida = null;
                Repintar();
                return;
            }

            if (Mathf.Abs(ec - coluna) + Mathf.Abs(el - linha) != 1)
            {
                _escolhida = (coluna, linha);          // não é vizinha: só muda a escolha
                Repintar();
                return;
            }

            Trocar(ec, el, coluna, linha);
            _escolhida = null;
            _jogadas++;
            DeslizarTudo();

            var trincas = Trincas();
            if (trincas.Count == 0)
            {
                // Troca que não estoura nada volta atrás, com tremor. É a regra do
                // gênero, e aqui ela ainda diz uma coisa: essas duas palavras não
                // formam família com as vizinhas.
                Trocar(ec, el, coluna, linha);
                DeslizarTudo();
                Tremer(coluna, linha);
                Painel.Instruir("essas não fecham três — tente outra", Cores.Neblina);
                return;
            }

            StartCoroutine(Resolver(trincas));
        }

        // ---------------------------------------------------------- arrastar
        //
        // O gesto do gênero. Quem já jogou match-3 aperta numa peça e puxa para o
        // lado; não achar isso faz o jogo parecer quebrado antes de o aluno
        // descobrir que aqui se joga com dois toques. Os dois caminhos convivem:
        // o arrastar para quem já sabe, o toque duplo para trackpad ruim e para
        // quem nunca jogou.

        void Pegar(int coluna, int linha)
        {
            if (_travado) return;
            _arrastando = (coluna, linha);
            _trocouArrastando = false;
        }

        /// <summary>O ponteiro entrou noutra peça com o botão apertado.</summary>
        void Arrastar(int coluna, int linha)
        {
            if (_travado || _arrastando == null) return;

            var (ac, al) = _arrastando.Value;
            if (Mathf.Abs(ac - coluna) + Mathf.Abs(al - linha) != 1) return;

            // Reaproveita o caminho do toque: seleciona a de origem e "toca" na
            // vizinha. Assim arrastar e tocar não podem divergir de regra — é uma
            // regra só, com duas entradas.
            _escolhida = (ac, al);
            _arrastando = null;
            Tocar(coluna, linha);

            // A marca vem DEPOIS da troca, e não antes: `Tocar` começa checando
            // esta mesma bandeira, então marcá-la antes faria a troca ser engolida
            // pela própria guarda. Ela existe só para o clique que ainda vai
            // chegar quando o dedo soltar não valer por uma segunda jogada.
            _trocouArrastando = true;
        }

        void Soltar() => _arrastando = null;

        /// <summary>
        /// Troca duas casas — a grade E o objeto que está em cima dela.
        ///
        /// As duas coisas andam sempre juntas. Enquanto só a grade se movia, a
        /// peça na tela ficava onde estava e passava a mostrar outra palavra: o
        /// jogo ficava certo por dentro e mentiroso por fora.
        /// </summary>
        void Trocar(int c1, int l1, int c2, int l2)
        {
            (_grade[c1, l1], _grade[c2, l2]) = (_grade[c2, l2], _grade[c1, l1]);
            if (_pecas != null)
                (_pecas[c1, l1], _pecas[c2, l2]) = (_pecas[c2, l2], _pecas[c1, l1]);
        }

        /// <summary>
        /// Estoura, deixa cair, enche de novo — e repete enquanto houver trinca.
        ///
        /// A cascata é o que dá o "só mais uma jogada" do gênero, e aqui rende de
        /// graça uma segunda leitura: o aluno vê outra trinca da mesma cor se
        /// formar sozinha e lê mais três palavras da família sem ter pedido.
        ///
        /// É CORROTINA, e isso não é detalhe de implementação.
        ///
        /// A primeira versão resolvia a cascata inteira num quadro só e desenhava
        /// no fim. O tabuleiro mudava de uma vez, sem nenhum estouro visível — e,
        /// pior, a frase com as três palavras que saíram era sobrescrita a cada
        /// trinca dentro do mesmo quadro, de modo que só a última chegava à tela.
        /// Justamente a frase que carrega a lição inteira desta bancada.
        ///
        /// Com a pausa entre os passos, cada trinca tem o seu instante: as peças
        /// somem, o nome das três palavras fica lido, e só então o resto cai. É
        /// o que separa "casei três coisas coloridas" de "ah, essas três andam
        /// juntas".
        /// </summary>
        IEnumerator Resolver(List<(int c, int l)> trincas)
        {
            var cascata = 0;
            _travado = true;

            while (trincas.Count > 0)
            {
                var palavras = new List<string>();
                var grupo = _grade[trincas[0].c, trincas[0].l].grupo;

                foreach (var (c, l) in trincas)
                {
                    var peca = _grade[c, l];
                    _estouradas[peca.grupo]++;
                    if (!palavras.Contains(peca.palavra)) palavras.Add(peca.palavra);
                    _grade[c, l] = (null, -1);
                }

                // O PROFESSOR INVISÍVEL DESTA BANCADA.
                //
                // Dizer as palavras que acabaram de sair, juntas, no instante em
                // que saem. É o mesmo recurso da bancada 3 e pela mesma razão:
                // chega no momento em que o aluno acabou de agir, custa zero
                // parágrafo, e é o que transforma "casei três amarelas" em "ah,
                // essas três aparecem nos mesmos lugares".
                Painel.Instruir("“" + string.Join("” · “", palavras) + "”" +
                                (cascata > 0 ? "   — e caiu outra!" : string.Empty),
                                CorDoGrupo(grupo));

                // Os buracos ficam à vista antes de a coluna cair. É o quadro que
                // diz "foi isto que você tirou".
                foreach (var (c, l) in trincas) SumirPeca(c, l);
                yield return new WaitForSeconds(cascata == 0 ? 0.45f : 0.35f);

                Cair();
                DeslizarTudo();
                yield return new WaitForSeconds(TempoDeQueda + 0.06f);

                cascata++;
                trincas = Trincas();
            }

            DesembaracarSePreciso();
            _travado = false;
            Conferir();
        }

        /// <summary>
        /// Se não sobrou jogada nenhuma, embaralha até sobrar.
        ///
        /// Um tabuleiro de match-3 pode fechar sem nenhuma troca que estoure, e
        /// aí o aluno fica clicando para sempre num jogo que não responde. É a
        /// mesma família de defeito que tornava a bancada 11 invencível — rodada
        /// sem saída, com a diferença de que aqui ela não parece culpa dele, o
        /// que é pior: parece jogo quebrado.
        ///
        /// Embaralhar, e não redistribuir do zero, porque as peças que ele já
        /// leu continuam na tela; só mudam de lugar.
        /// </summary>
        void DesembaracarSePreciso()
        {
            for (var tentativa = 0; tentativa < 40 && !TemJogada(); tentativa++)
            {
                for (var i = Colunas * Linhas - 1; i > 0; i--)
                {
                    var j = (int)(_sorteio.Proximo() * (i + 1));
                    var (ci, li) = (i % Colunas, i / Colunas);
                    var (cj, lj) = (j % Colunas, j / Colunas);
                    Trocar(ci, li, cj, lj);
                }

                // Embaralhar pode ter criado trinca de graça. Desfaz trocando as
                // peças dela de lugar, até o tabuleiro nascer limpo.
                var solta = Trincas();
                if (solta.Count > 0)
                {
                    var (c, l) = solta[0];
                    _grade[c, l] = Sortear();
                }
            }

            // As peças mudaram de casa (e uma pode ter mudado de palavra): manda
            // todas para o lugar novo. Sem isto o embaralhamento acontece só na
            // grade e a tela continua mostrando o tabuleiro travado de antes.
            if (_pecas != null) DeslizarTudo();
        }

        /// <summary>Existe alguma troca entre vizinhas que estoure alguma coisa?</summary>
        bool TemJogada()
        {
            for (var l = 0; l < Linhas; l++)
            {
                for (var c = 0; c < Colunas; c++)
                {
                    for (var d = 0; d < 2; d++)
                    {
                        var c2 = c + (d == 0 ? 1 : 0);
                        var l2 = l + (d == 0 ? 0 : 1);
                        if (c2 >= Colunas || l2 >= Linhas) continue;

                        Trocar(c, l, c2, l2);
                        var fecha = Trincas().Count > 0;
                        Trocar(c, l, c2, l2);
                        if (fecha) return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Puxa tudo para baixo e enche o vão de cima com peça nova.
        ///
        /// Move a grade e os objetos juntos, e a peça nova nasce ACIMA do
        /// tabuleiro para cair até o lugar. É por isso que a coluna parece uma
        /// coluna e não uma lista que se reescreve.
        /// </summary>
        void Cair()
        {
            for (var c = 0; c < Colunas; c++)
            {
                var destino = Linhas - 1;
                for (var l = Linhas - 1; l >= 0; l--)
                {
                    if (_grade[c, l].grupo < 0) continue;
                    if (destino != l)
                    {
                        _grade[c, destino] = _grade[c, l];
                        if (_pecas != null) _pecas[c, destino] = _pecas[c, l];
                        _grade[c, l] = (null, -1);
                        if (_pecas != null) _pecas[c, l] = null;
                    }
                    destino--;
                }

                for (var l = destino; l >= 0; l--)
                {
                    _grade[c, l] = Sortear();
                    if (_pecas != null) _pecas[c, l] = CriarPeca(c, l, true);
                }
            }
        }

        /// <summary>
        /// Todas as peças que estão em trinca — a primeira que encontrar.
        ///
        /// Devolve UMA trinca por vez, e não todas de uma vez, porque cada trinca
        /// vira uma frase na tela. Estourar quatro trincas caladas de uma vez daria
        /// pontos e nenhuma leitura.
        /// </summary>
        List<(int c, int l)> Trincas()
        {
            for (var l = 0; l < Linhas; l++)
            {
                for (var c = 0; c < Colunas; c++)
                {
                    var g = _grade[c, l].grupo;
                    if (g < 0) continue;

                    var deitada = Corrida(c, l, 1, 0, g);
                    if (deitada.Count >= 3) return deitada;

                    var empe = Corrida(c, l, 0, 1, g);
                    if (empe.Count >= 3) return empe;
                }
            }
            return new List<(int, int)>();
        }

        List<(int c, int l)> Corrida(int c, int l, int dc, int dl, int grupo)
        {
            var saida = new List<(int, int)>();
            while (c < Colunas && l < Linhas && _grade[c, l].grupo == grupo)
            {
                saida.Add((c, l));
                c += dc;
                l += dl;
            }
            return saida;
        }

        // ------------------------------------------------------------- o fecho

        void Conferir()
        {
            if (_estouradas.Any(n => n < _meta))
            {
                Painel.MarcarPasso(NivelAtual + 1, Niveis);
                return;
            }

            _travado = true;

            // As famílias nomeadas PELA COR que o aluno acabou de caçar.
            //
            // Dizer "dourado", "turquesa" e "laranja" em vez de listar três linhas
            // soltas é o que costura o fecho ao que ele fez com as mãos nos
            // últimos minutos: ele passou a rodada caçando dourado, e agora
            // descobre o que o dourado era.
            var nomes = new[] { "dourado", "turquesa", "laranja" };
            var familias = string.Join("\n\n", _grupos.Select(
                (g, i) => $"   {nomes[Mathf.Min(i, nomes.Length - 1)].ToUpper()}\n" +
                          $"   {string.Join("  ·  ", g.Palavras)}"));

            Resolveu($"As três famílias, em {_jogadas} jogadas",
                "As cores não eram enfeite. Cada uma é um grupo que a\n" +
                "máquina montou sozinha:\n\n" + familias + "\n\n" +
                "Ela não sabe o que nenhuma dessas palavras significa. Só\n" +
                "notou que as de cada grupo aparecem nos mesmos lugares das\n" +
                "frases. Vizinhança parecida é o sentido, para ela.");
        }
    }
}
