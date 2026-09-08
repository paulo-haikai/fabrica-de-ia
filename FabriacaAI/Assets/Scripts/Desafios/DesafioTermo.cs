using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Bancada 1 — adivinhe a palavra, de Tico.
    ///
    /// Seis tentativas por rodada; letra certa no lugar certo em verde, letra
    /// certa fora do lugar em amarelo, letra que não existe em cinza.
    ///
    /// A etapa tem duas metades e a ORDEM delas é o conteúdo:
    ///
    ///   Rodadas 1 a 3 — palavra solta, sem pista nenhuma. É só o jogo, e o
    ///   aluno joga porque é divertido. Nenhuma teoria aparece na tela.
    ///
    ///   Rodadas 4 a 6 — a mesma coisa, agora com uma frase antes da lacuna. O
    ///   aluno percebe na mão que passou a acertar em duas tentativas o que
    ///   antes custava cinco.
    ///
    /// Só no fim a tela põe os dois números lado a lado e diz o que aconteceu.
    /// Ensinar assim é mais lento do que abrir com a explicação, e funciona: o
    /// aluno não RECEBE a informação de que contexto ajuda, ele CONSTATA, com
    /// dados que produziu nos últimos minutos.
    ///
    /// Por que Termo e não clicar em quatro opções: clicar não é jogo. Não há
    /// como perder, e sem perder não há tentativa e erro. Aqui existe recurso
    /// escasso (seis tentativas), custo por errar (uma a menos) e informação que
    /// só aparece QUANDO se erra — as cores. É esse trio que faz um jogo.
    /// </summary>
    public partial class DesafioTermo : Desafio
    {
        const int Tentativas = 6;
        /// <summary>Quanto uma rodada perdida conta na média. Vale o pior caso.</summary>
        const int CustoDeErrar = Tentativas + 1;

        // As três faixas dentro da área de conteúdo: a frase em cima, a grade no
        // meio, o teclado embaixo.
        const float AlturaContexto = 56f;
        const float AlturaTeclado = 100f;
        const float FolgaDaGrade = 6f;

        enum Cor { Vazia, Fora, Quase, Certa }

        public override string Etapa => "e1";
        public override string Titulo => "Adivinhe a palavra";

        List<Rodada> _rodadas;
        int _rodadaAtual;
        int _acertos;
        /// <summary>
        /// As palavras que o aluno não descobriu. Guardadas para o placar final:
        /// terminar a bancada sem saber quais eram deixa a única coisa que ele
        /// queria saber por fora do fecho.
        /// </summary>
        readonly List<string> _escaparam = new();

        /// <summary>Tentativas gastas em cada rodada, na ordem jogada.</summary>
        readonly List<int> _custos = new();

        readonly List<string> _palpites = new();
        string _digitando = string.Empty;
        bool _rodadaEncerrada;
        bool _mostrandoPlacar;

        RectTransform _area;
        RectTransform _palco;
        Text _contexto;

        // AS TRÊS PEÇAS QUE A RODADA REFAZ, guardadas por referência e não
        // procuradas pelo nome.
        //
        // Era assim: `_grade` existia e nunca era atribuída, e quem precisava da
        // grade chamava `_palco.Find("Grade")`. Custou um defeito de verdade — a
        // faixa da resposta revelada, criada como "Resposta", não estava em
        // nenhuma das duas buscas por nome e NUNCA ERA DESTRUÍDA. Ela ficava no
        // palco pela bancada inteira, aparecendo no fundo entre as células das
        // rodadas seguintes, e uma nova se empilhava a cada rodada perdida.
        //
        // Achar filho por string é frágil desse jeito: some sem erro, sem
        // warning, e sem ninguém para avisar que uma peça ficou órfã. Com o campo
        // aqui, esquecer de destruir vira uma referência pendurada que se vê ao
        // ler a classe.
        RectTransform _grade;
        RectTransform _teclado;
        RectTransform _resposta;

        Image[,] _celulas;
        Text[,] _letras;
        readonly Dictionary<char, Image> _teclas = new();

        // ------------------------------------------------------------- regras

        /// <summary>Sem acento e em minúscula — o term.ooo também ignora acento.</summary>
        static string Normalizar(string s)
        {
            var decomposto = s.Normalize(NormalizationForm.FormD);
            var limpo = new StringBuilder(decomposto.Length);
            foreach (var c in decomposto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    limpo.Append(c);
            }
            return limpo.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

        /// <summary>
        /// As cores de um palpite.
        ///
        /// Duas passadas de propósito: a primeira crava os verdes, a segunda
        /// distribui os amarelos só entre as letras que sobraram. Numa passada
        /// só, palavra com letra repetida pinta amarelo a mais e o aluno deduz
        /// errado — é o defeito clássico de clone de Wordle.
        /// </summary>
        static Cor[] Avaliar(string palpite, string resposta)
        {
            var p = Normalizar(palpite);
            var r = Normalizar(resposta);
            var cores = new Cor[p.Length];
            var sobrou = new Dictionary<char, int>();

            for (var i = 0; i < p.Length; i++)
            {
                if (p[i] == r[i])
                {
                    cores[i] = Cor.Certa;
                }
                else
                {
                    cores[i] = Cor.Fora;
                    sobrou[r[i]] = sobrou.GetValueOrDefault(r[i]) + 1;
                }
            }

            for (var i = 0; i < p.Length; i++)
            {
                if (cores[i] == Cor.Certa) continue;
                if (sobrou.GetValueOrDefault(p[i]) <= 0) continue;
                cores[i] = Cor.Quase;
                sobrou[p[i]]--;
            }
            return cores;
        }

        // -------------------------------------------------------------- abrir

        protected override void Montar(RectTransform area)
        {
            _area = area;
            _rodadas = Rodadas.Sortear(Rodadas.Semente());

            // A frase de contexto mora no ALTO da área de conteúdo, e o teclado
            // no pé dela. Como os dois são filhos da mesma área, e não da tela,
            // não há como um invadir a faixa de instrução da moldura.
            _contexto = Widgets.Texto("Contexto", _area, 20, TextAnchor.UpperCenter, Cores.Papel);
            Widgets.Faixa(_contexto.rectTransform, true, AlturaContexto - 4f);

            _palco = Widgets.Painel("Palco", _area, Color.clear);
            Widgets.Esticar(_palco);
            _palco.offsetMax = new Vector2(0f, -AlturaContexto);
            _palco.offsetMin = new Vector2(0f, AlturaTeclado + 4f);

            Painel.Rodape("digite a palavra e aperte Enter  ·  Backspace apaga");
            IniciarRodada();
        }

        void IniciarRodada()
        {
            _palpites.Clear();
            _digitando = string.Empty;
            _rodadaEncerrada = false;

            var rodada = _rodadas[_rodadaAtual];
            Painel.MarcarPasso(_rodadaAtual + 1, _rodadas.Count);
            Painel.Acao(null, null);

            if (rodada.TemContexto)
            {
                Painel.Instruir("complete a frase");
                _contexto.text = $"“{rodada.Contexto} ___”";
                _contexto.color = Cores.Papel;
            }
            else
            {
                // Sem pista, a única informação é o tamanho. Dizer isso em voz
                // alta evita a queixa legítima de "como eu ia adivinhar?".
                Painel.Instruir("sem pista nenhuma — só o tamanho");
                _contexto.text = $"uma palavra de {rodada.Resposta.Length} letras";
                _contexto.color = Cores.Neblina;
            }

            MontarGrade(rodada.Resposta.Length);
            MontarTeclado();
        }

        // ------------------------------------------------------------ teclado

        void Update()
        {
            var teclado = Keyboard.current;
            if (teclado == null) return;

            if (_mostrandoPlacar)
            {
                if (teclado.enterKey.wasPressedThisFrame ||
                    teclado.numpadEnterKey.wasPressedThisFrame ||
                    teclado.spaceKey.wasPressedThisFrame)
                {
                    Encerrar(Estrelas());
                }
                return;
            }

            if (_rodadaEncerrada) return;

            var tamanho = _rodadas[_rodadaAtual].Resposta.Length;

            if (teclado.backspaceKey.wasPressedThisFrame && _digitando.Length > 0)
            {
                _digitando = _digitando.Substring(0, _digitando.Length - 1);
                RedesenharLinhaAtual();
                return;
            }

            if (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame)
            {
                Enviar();
                return;
            }

            for (var c = 'a'; c <= 'z'; c++)
            {
                var tecla = teclado[Key.A + (c - 'a')];
                if (tecla == null || !tecla.wasPressedThisFrame) continue;

                // Linha cheia não pode ficar MUDA. Antes ela simplesmente ignorava
                // a tecla, e ignorar em silêncio é indistinguível de travar: o
                // aluno aperta letra, nada acontece, e ele conclui que o jogo
                // morreu. Dizer o que fazer custa uma linha de texto.
                if (_digitando.Length >= tamanho)
                {
                    Painel.Instruir("a linha está cheia — Enter envia, Backspace apaga",
                                    Cores.Luz);
                    return;
                }

                _digitando += c;
                RedesenharLinhaAtual();
                return;
            }
        }

        /// <summary>
        /// Sair no meio não joga fora o que já foi conquistado. Quem chegou ao
        /// placar e clicou em sair leva as estrelas; quem desiste antes, não.
        /// </summary>
        protected override void Desistir() => Encerrar(_mostrandoPlacar ? Estrelas() : 0);

        // ------------------------------------------------------------ palpite

        void Enviar()
        {
            var resposta = _rodadas[_rodadaAtual].Resposta;
            var tamanho = resposta.Length;

            if (_digitando.Length < tamanho)
            {
                Painel.Instruir($"faltam letras — a palavra tem {tamanho}", Cores.Brasa);
                return;
            }

            // QUALQUER palavra do tamanho certo vale como palpite. A primeira
            // versão só aceitava palavra do corpus escolar, argumentando que um
            // palpite que a máquina não poderia dar estragaria a comparação entre
            // os dois. O argumento era falso e o preço era altíssimo:
            //
            //   · Bancada 1 não compara aluno com máquina em lugar nenhum. Ela
            //     compara o aluno SEM pista com o aluno COM pista — 4,7 contra 2,3
            //     tentativas. A restrição não protegia conta nenhuma.
            //
            //   · O corpus tem 318 palavras, e só 73 delas têm cinco letras. Ou
            //     seja: o aluno digitava português correto e era recusado quase
            //     sempre, sem ter como saber quais setenta e três eram válidas.
            //
            //   · E a recusa TRAVAVA o jogo. O buffer ficava cheio, o `Update`
            //     ignorava toda tecla nova, e só o Backspace saía — sem nada na
            //     tela dizendo isso. Parecia que o jogo tinha morrido.
            //
            // Palpite bobo custa uma tentativa, então o jogo se regula sozinho: e
            // sondar letras de propósito ("aeiou" na primeira linha) é jogada
            // legítima de Termo, não trapaça.
            var cores = Avaliar(_digitando, resposta);
            var linha = _palpites.Count;
            for (var i = 0; i < tamanho; i++)
            {
                _celulas[linha, i].color = Pintura(cores[i]);
                _letras[linha, i].text = _digitando[i].ToString().ToUpperInvariant();
                MarcarTecla(_digitando[i], cores[i]);
            }

            _palpites.Add(_digitando);
            var acertou = Normalizar(_digitando) == Normalizar(resposta);
            _digitando = string.Empty;

            if (acertou)
            {
                _acertos++;
                _custos.Add(_palpites.Count);
                _rodadaEncerrada = true;
                Painel.Instruir(_palpites.Count == 1
                    ? $"de primeira! — {resposta}"
                    : $"acertou em {_palpites.Count} — {resposta}", Cores.Folha);
                Invoke(nameof(Avancar), 1.3f);
                return;
            }

            if (_palpites.Count >= Tentativas)
            {
                _custos.Add(CustoDeErrar);
                _escaparam.Add(resposta);
                _rodadaEncerrada = true;

                // A resposta já era dita aqui, mas só na linha fina de instrução e
                // por 1,8 segundo — piscava e sumia, e o aluno saía da rodada sem
                // saber qual era a palavra. É a única informação que ele quer nesse
                // instante, e a que fecha o aprendizado da tentativa perdida.
                RevelarResposta(resposta);
                Painel.Instruir("essa escapou", Cores.Brasa);
                Invoke(nameof(Avancar), 3.2f);
                return;
            }

            var restam = Tentativas - _palpites.Count;
            Painel.Instruir(restam == 1 ? "última tentativa" : $"restam {restam} tentativas");
        }

        void Avancar()
        {
            _rodadaAtual++;

            if (_rodadaAtual >= _rodadas.Count)
            {
                MostrarPlacar();
                return;
            }

            // A virada da quarta rodada é anunciada. Trocar a regra do jogo em
            // silêncio faria a frase parecer enfeite; anunciada, ela vira
            // presente — e o aluno repara no efeito dela.
            if (_rodadaAtual == Rodadas.SemContexto)
            {
                MostrarVirada();
                return;
            }

            IniciarRodada();
        }

        // ------------------------------------------------------------- placar

        float Media(int de, int ate)
        {
            var fatia = _custos.Skip(de).Take(Mathf.Max(0, ate - de)).ToList();
            return fatia.Count == 0 ? 0f : (float)fatia.Average();
        }

        /// <summary>
        /// Estrelas pelo desempenho. Errar tudo ainda encerra a etapa — o aluno
        /// viu o problema, que é o que a bancada tinha para ensinar — mas sem
        /// estrela, senão a estrela não significa nada.
        /// </summary>
        int Estrelas() => _acertos switch
        {
            >= 5 => 3,
            >= 3 => 2,
            >= 1 => 1,
            _ => 0
        };
    }
}
