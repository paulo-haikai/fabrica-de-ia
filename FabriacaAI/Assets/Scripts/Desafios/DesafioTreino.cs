using System.Collections;
using System.Collections.Generic;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Bancada 7 — o treino de Rosa, jogado como um Angry Birds.
    ///
    /// A pista é a de sempre: estilingue à esquerda, porco em cima de umas
    /// caixas à direita, parábola no meio. O que muda é quem mira.
    ///
    /// VOCÊ NÃO MIRA. A máquina mira. Ela atira, olha onde o pássaro caiu, mede
    /// a distância até o porco e corrige o ângulo sozinha — descida de gradiente,
    /// a mesma conta que treina qualquer rede. O que o aluno regula é UMA coisa:
    /// o TAMANHO da correção que ela faz depois de cada erro.
    ///
    /// E ele regula com o gesto do Angry Birds: puxando o elástico. Puxar mais
    /// não atira mais forte — atira o mesmo, e faz a máquina se corrigir mais de
    /// uma vez para outra. É a única liberdade que o aprendizado de máquina de
    /// verdade dá a quem treina, e aqui ela está no lugar em que a mão do
    /// jogador já sabe ir.
    ///
    /// Os três comportamentos, agora sem metáfora nenhuma no meio:
    ///
    ///   · correção pequena — os tiros vão chegando de pouquinho em pouquinho e
    ///     acabam os tiros antes de chegar (140, 138, 135, 132, 130, 127);
    ///   · correção boa — três tiros e o porco explode (140, 111, 74);
    ///   · correção grande — a máquina passa do ponto e volta pior a cada vez,
    ///     até atirar quase para cima e o pássaro cair na cabeça dela
    ///     (140, 35, 153, 115, 18…).
    ///
    /// Os três são saída de verdade do simulador, com o porco a 78 passos e as
    /// correções 0,04 · 0,40 · 1,60.
    ///
    /// O rastro de cada tiro FICA na pista. Ao fim de um treino, os rastros
    /// desenham sozinhos a curva de aprendizado — sem gráfico, sem eixo, sem
    /// legenda. Foi por isso que o vale abstrato da versão anterior saiu: ele
    /// pedia que o aluno acreditasse que a bolinha era o erro de um botão. Aqui
    /// não há nada para acreditar; o erro é a distância até o porco.
    ///
    /// A rodada 3 é a lição que exige duas pistas: um porco perto e um longe, as
    /// duas máquinas com a MESMA regulagem. Perto, o ângulo é sensível e a
    /// correção grande faz a máquina enlouquecer; longe, ele é surdo e a
    /// correção pequena nunca chega. Existe uma faixa que serve para as duas, e
    /// achá-la é descobrir na mão o problema que criou uma área de pesquisa.
    /// </summary>
    public partial class DesafioTreino : DesafioEmNiveis
    {
        public override string Etapa => "e7";
        public override string Titulo => "Deixar a máquina treinar";

        protected override int Niveis => Rodadas8.Length;

        /// <summary>
        /// Uma rodada: onde estão os porcos, com que ângulo a máquina começa,
        /// quantos tiros ela tem por treino e quantos treinos o aluno tem.
        /// </summary>
        struct Rodada8
        {
            public float[] Porcos;
            /// <summary>
            /// O chute inicial da máquina, em graus. Negativo quer dizer que a
            /// MIRA é do aluno — é a rodada em que ele também escolhe de onde a
            /// máquina parte, e descobre que um chute bom vale mais que um
            /// monte de correção.
            /// </summary>
            public float Chute;
            public int Tiros;
            public int Tentativas;
        }

        /// <summary>
        /// As três rodadas, com as faixas medidas por
        /// <c>Conferencias.Treino8</c>. Mudar um número aqui muda a
        /// jogabilidade da bancada, e a conferência do Editor cobra:
        ///
        ///   rodada 1 — vence com correção de 0,15 a 0,98 (41% do elástico)
        ///   rodada 2 — 38% do retângulo mira × correção, e uma em cada seis
        ///              miras vence com QUALQUER correção: é a rodada dizendo
        ///              que um bom chute inicial encurta o treino
        ///   rodada 3 — vence de 0,19 a 0,89 (34%), e as duas pontas vêm de
        ///              pistas DIFERENTES — o porco longe cobra o piso (menos
        ///              que 0,19 e ele nunca chega) e o perto cobra o teto (mais
        ///              que 0,89 e ele enlouquece). É a rodada inteira.
        /// </summary>
        static readonly Rodada8[] Rodadas8 =
        {
            new() { Porcos = new[] { 78f },        Chute = 30f, Tiros = 6, Tentativas = 4 },
            new() { Porcos = new[] { 95f },        Chute = -1f, Tiros = 3, Tentativas = 4 },
            new() { Porcos = new[] { 65f, 122f },  Chute = 18f, Tiros = 4, Tentativas = 5 }
        };

        /// <summary>
        /// A faixa de correção, ponta a ponta.
        ///
        /// O passeio pela barra é GEOMÉTRICO: cada pedaço multiplica a correção
        /// em vez de somar. É como se escolhe taxa de aprendizado de verdade
        /// (0,001 · 0,01 · 0,1), e é o que faz a metade de baixo da barra ter
        /// resolução em vez de ser um borrão onde tudo funciona igual. Com
        /// mapeamento linear, 70% do elástico só produzia máquinas malucas.
        /// </summary>
        const float CorrecaoMinima = 0.02f;
        const float CorrecaoMaxima = 2f;

        /// <summary>Puxão que enche a régua, em pixels de tela.</summary>
        const float PuxadaMaxima = 150f;

        /// <summary>
        /// Abaixo disto o puxão foi um clique sem querer, e não gasta treino.
        /// Perder uma tentativa por um toque acidental é o tipo de punição que o
        /// aluno não entende, e por isso não aprende com ela.
        /// </summary>
        const float PuxadaMinima = 0.05f;

        /// <summary>A mira que o aluno pode dar, quando a rodada deixa.</summary>
        const float MiraMinima = 4f * Mathf.Deg2Rad;
        const float MiraMaxima = 46f * Mathf.Deg2Rad;

        Rodada8 _r;
        readonly List<Maquina> _maquinas = new();

        int _tentativas;
        float _melhorFalta = float.PositiveInfinity;
        bool _treinando;

        // --------------------------------------------------------- a regulagem
        bool _puxando;
        float _fracao;
        float _mira = 20f * Mathf.Deg2Rad;

        /// <summary>Uma das máquinas: o estilingue dela, o porco dela, o ângulo dela.</summary>
        class Maquina
        {
            public float PorcoX;
            public List<Rect> Blocos;
            public float Angulo;
            public bool Acertou;

            /// <summary>Por quantos passos o tiro da vez errou, e se ele sumiu.</summary>
            public float FaltaAtual;
            public bool SumiuAtual;

            /// <summary>
            /// O diagnóstico do treino da vez, e ele é GRUDENTO: uma vez ligado,
            /// fica ligado até o fim do treino.
            ///
            /// Olhar só o ângulo final não serve. Uma máquina que enlouqueceu
            /// pode terminar o treino com o ângulo de volta em cinco graus, por
            /// acaso do quique, e o cartaz diria "faltou correção" bem depois de
            /// o aluno ter visto o pássaro subir para o céu.
            /// </summary>
            public bool Enlouqueceu;
            public bool Serrilhou;
            public float SinalDoErro;

            /// <summary>A melhor de todos os treinos — é ela que decide a estrela de consolo.</summary>
            public float MelhorFalta = float.PositiveInfinity;

            // o que a tela montou para esta pista (ver DesafioTreino.Desenho.cs)
            public RectTransform Pista, Passaro, Porco, Rastros, ElasticoA, ElasticoB;
            public RectTransform[] Caixas;
            public float Escala, ChaoY;
        }

        protected override void MontarNivel()
        {
            _r = Rodadas8[NivelAtual];
            _tentativas = 0;
            _melhorFalta = float.PositiveInfinity;
            _treinando = false;
            _puxando = false;
            _fracao = 0f;
            _mira = 20f * Mathf.Deg2Rad;

            MontarPistas();
            MontarRegua();
            // O véu do arraste por último: a régua nasce depois das pistas e
            // roubaria o dedo de quem puxasse o elástico perto do console.
            _puxador.SetAsLastSibling();

            Painel.Rodape(Mira
                ? "puxe o pássaro para trás e para cima · a mira é sua, o resto é da máquina"
                : "puxe o pássaro para trás · quanto mais longe, maior a correção da máquina");
            Painel.Instruir(Mira
                ? "escolha a mira do primeiro tiro e o tamanho da correção"
                : "escolha o tamanho da correção e solte");

            Atualizar();
        }

        /// <summary>Esta rodada entrega a mira ao aluno?</summary>
        bool Mira => _r.Chute < 0f;

        /// <summary>Monta as máquinas da rodada — uma por porco.</summary>
        void PrepararMaquinas()
        {
            _maquinas.Clear();
            foreach (var porco in _r.Porcos)
                _maquinas.Add(new Maquina
                {
                    PorcoX = porco,
                    Blocos = Estilingue.Cenario(porco)
                });
            RecomecarMaquinas();
        }

        /// <summary>Devolve as máquinas ao ponto de partida, antes de cada treino.</summary>
        void RecomecarMaquinas()
        {
            var partida = Mira ? _mira : _r.Chute * Mathf.Deg2Rad;
            foreach (var m in _maquinas)
            {
                m.Angulo = partida;
                m.Acertou = false;
                m.FaltaAtual = 0f;
                m.SumiuAtual = false;
                m.Enlouqueceu = false;
                m.Serrilhou = false;
                m.SinalDoErro = 0f;
            }
        }

        // ------------------------------------------------------------- o puxão

        void Puxar(Vector2 ponteiro)
        {
            if (_treinando) return;
            _puxando = true;
            Mirar(ponteiro);
        }

        /// <summary>
        /// Converte o puxão em regulagem.
        ///
        /// Nas rodadas de mira fixa só o quanto se puxa PARA TRÁS conta, e o
        /// elástico é desenhado na horizontal: dar dois eixos ao aluno num
        /// momento em que um deles não faz nada é convidá-lo a procurar sentido
        /// onde não há. Na rodada da mira, os dois eixos valem, e aí o gesto é o
        /// do Angry Birds inteiro.
        /// </summary>
        void Mirar(Vector2 ponteiro)
        {
            if (!_puxando) return;

            var boca = BocaNaTela(_maquinas[0]);
            var puxao = ponteiro - boca;

            if (Mira)
            {
                _fracao = Mathf.Clamp01(puxao.magnitude / PuxadaMaxima);
                var tiro = -puxao;
                if (tiro.x > 0.001f)
                    _mira = Mathf.Clamp(Mathf.Atan2(tiro.y, tiro.x), MiraMinima, MiraMaxima);
            }
            else
            {
                _fracao = Mathf.Clamp01(Mathf.Max(0f, -puxao.x) / PuxadaMaxima);
            }

            DesenharPuxada();
        }

        void Soltar()
        {
            if (!_puxando) return;
            _puxando = false;

            if (_fracao < PuxadaMinima)
            {
                DesenharPuxada();
                // A régua treme para dizer "não vali". Ignorar o toque em
                // silêncio faz o aluno achar que o botão quebrou.
                Widgets.Tremer(_reguaTrilho);
                Painel.Instruir("puxe mais para trás", Cores.Neblina);
                return;
            }

            MarcarAnterior(_fracao);
            StartCoroutine(Treinar(Correcao(_fracao)));
        }

        /// <summary>A correção que uma fração de puxão vale.</summary>
        static float Correcao(float fracao) =>
            CorrecaoMinima * Mathf.Pow(CorrecaoMaxima / CorrecaoMinima, fracao);

        // ------------------------------------------------------------- o treino

        /// <summary>
        /// Um treino inteiro: as máquinas atiram em coro, tiro a tiro, e entre
        /// um tiro e o outro cada uma corrige o próprio ângulo.
        ///
        /// Em coro DE PROPÓSITO na rodada 3. É a única maneira de a desigualdade
        /// virar imagem: no mesmo instante, com a mesma regulagem, uma máquina
        /// assenta no porco e a outra sai atirando para o céu.
        /// </summary>
        IEnumerator Treinar(float correcao)
        {
            _treinando = true;
            _tentativas++;
            RecomecarMaquinas();
            LimparRastros();
            Atualizar();

            for (var tiro = 0; tiro < _r.Tiros; tiro++)
            {
                var voos = new List<Coroutine>();
                var faltou = 0f;

                foreach (var m in _maquinas)
                {
                    if (m.Acertou) continue;
                    voos.Add(StartCoroutine(Atirar(m, tiro, correcao)));
                }
                if (voos.Count == 0) break;

                foreach (var voo in voos) yield return voo;

                // A pior das pistas manda no recado: na rodada 3, dizer que
                // "errou por 2" enquanto a outra máquina atira para o céu seria
                // a tela contando metade do que aconteceu.
                var vivas = 0;
                var sumiu = false;
                foreach (var m in _maquinas)
                {
                    if (m.Acertou) continue;
                    vivas++;
                    faltou = Mathf.Max(faltou, m.FaltaAtual);
                    sumiu |= m.SumiuAtual;
                }

                if (vivas == 0) break;
                Painel.Instruir(Recado(tiro + 1, faltou, sumiu), Cores.Papel);
                yield return new WaitForSecondsRealtime(0.22f);
            }

            foreach (var m in _maquinas)
                _melhorFalta = Mathf.Min(_melhorFalta, m.MelhorFalta);

            _treinando = false;
            Atualizar();
            Julgar();
        }

        /// <summary>Um tiro: voa, bate, e a máquina corrige o ângulo dela.</summary>
        IEnumerator Atirar(Maquina m, int tiro, float correcao)
        {
            var caminho = Estilingue.Voo(m.Angulo, m.PorcoX, m.Blocos, out var impacto);
            yield return VoarNaTela(m, caminho, impacto, tiro);

            var erro = impacto.Onde.x - m.PorcoX;
            m.FaltaAtual = Mathf.Abs(erro);
            m.SumiuAtual = impacto.Sumiu;
            m.MelhorFalta = Mathf.Min(m.MelhorFalta, m.FaltaAtual);

            if (impacto.Porco)
            {
                m.Acertou = true;
                yield break;
            }

            // Trocou de lado: passou do porco depois de ter ficado curto, ou o
            // contrário. É a assinatura de passo grande — a correção atravessa
            // o alvo em vez de assentar nele.
            var sinal = Mathf.Sign(erro);
            if (m.SinalDoErro != 0f && sinal != m.SinalDoErro) m.Serrilhou = true;
            m.SinalDoErro = sinal;

            m.Angulo = Estilingue.Corrigir(m.Angulo, erro, correcao);
            if (m.Angulo > 50f * Mathf.Deg2Rad || m.Angulo < 0f) m.Enlouqueceu = true;
        }

        static string Recado(int tiro, float faltou, bool sumiu) =>
            sumiu
                ? $"tiro {tiro} — sumiu no horizonte"
                : $"tiro {tiro} — errou por {Mathf.RoundToInt(faltou)} passos";

        void Atualizar()
        {
            Painel.MarcarPasso(NivelAtual + 1, Niveis,
                $"treino {Mathf.Min(_tentativas + (_treinando ? 0 : 1), _r.Tentativas)}" +
                $" de {_r.Tentativas}");
            AtualizarRegua();
        }

        // ------------------------------------------------------------- o juízo

        void Julgar()
        {
            var todos = true;
            foreach (var m in _maquinas) todos &= m.Acertou;

            if (todos)
            {
                Vencer();
                return;
            }

            var enlouqueceu = false;
            var serrilhou = false;
            foreach (var m in _maquinas)
            {
                enlouqueceu |= m.Enlouqueceu;
                serrilhou |= m.Serrilhou;
            }

            if (enlouqueceu)
            {
                foreach (var m in _maquinas) Widgets.Tremer(m.Pista, 11f, 0.3f);
                Widgets.Lampejo(Area, Cores.Brasa, 0.3f, 0.35f);
            }

            if (_tentativas >= _r.Tentativas)
            {
                Falhou($"Acabaram os treinos — a máquina chegou a {Mathf.RoundToInt(_melhorFalta)} passos",
                       Licao(enlouqueceu, serrilhou),
                       contaEstrela: _melhorFalta <= 12f);
                return;
            }

            Painel.Instruir(
                enlouqueceu ? "a máquina passou do ponto e enlouqueceu"
                : serrilhou ? "ela pulou de um lado para o outro do porco"
                : "a correção foi pequena demais",
                enlouqueceu ? Cores.Brasa : Cores.Luz);
        }

        /// <summary>
        /// O diagnóstico do treino, em uma frase. Os rastros na pista já
        /// mostraram o QUE aconteceu; a frase só dá nome, para o professor pegar
        /// o gancho.
        /// </summary>
        static string Licao(bool enlouqueceu, bool serrilhou) =>
            enlouqueceu
                ? "Correção grande demais: a cada erro ela se corrigia mais do que\nprecisava, passava do porco e voltava pior."
                : serrilhou
                    ? "Ela chegava perto e passava direto, de um lado para o outro do\nporco, sem assentar."
                    : "A direção estava certa — os tiros foram chegando. Só que de tão\npouquinho em pouquinho que os tiros acabaram antes.";

        void Vencer()
        {
            if (_maquinas.Count > 1)
            {
                Resolveu("As duas máquinas acertaram, com a mesma regulagem",
                    "Uma correção grande fazia a de perto enlouquecer; uma pequena\n" +
                    "deixava a de longe pelo caminho.\n\n" +
                    "Você achou na mão o problema que criou uma área de pesquisa\n" +
                    "inteira: dar um passo diferente para cada coisa que se ajusta.");
                return;
            }

            if (Mira)
            {
                Resolveu("Acertou — e você só deu o primeiro chute",
                    "Um chute inicial bom encurta o treino inteiro. É por isso que\n" +
                    "ninguém treina uma máquina começando do zero quando dá para\n" +
                    "começar de uma que já sabe alguma coisa.");
                return;
            }

            Resolveu("O porco era da máquina",
                "Você não mirou uma única vez. Ela atira, mede o quanto errou,\n" +
                "corrige o ângulo um tantinho e repete.\n\n" +
                "Isso tem nome: descida de gradiente.");
        }
    }
}
