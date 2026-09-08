using System.Collections;
using System.Collections.Generic;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Bancada 6 — o robô de Seu Ilo.
    ///
    /// Inspiração: A CORRIDA DO DINOSSAURO do Chrome sem internet. Todo aluno já
    /// jogou, e ninguém precisa explicar o que é desviar de obstáculo.
    ///
    /// A diferença, e é a bancada inteira: **o aluno não joga**. Ele calibra a
    /// MARGEM DE ERRO do robô, solta, e assiste dez segundos. Quem pula é o robô,
    /// sozinho, obedecendo ao número que o aluno deixou ajustado. Se bate, não foi
    /// falta de reflexo — foi a margem errada.
    ///
    /// É a diferença entre jogar e TREINAR, e é a razão de a bancada existir. Na
    /// versão anterior isto era um Mastermind: o aluno girava botões e recebia um
    /// número de erro. Ensinava a mesma coisa e não convencia ninguém, porque o
    /// número não tinha corpo. Aqui o erro tem corpo: é o robô batendo a cara.
    ///
    /// As três rodadas acrescentam um parâmetro por vez, que é como se afina
    /// qualquer modelo — um botão de cada vez, senão não se sabe qual mexeu:
    ///
    ///   1. Só pular. UM número: a que distância ele decide saltar.
    ///   2. Só abaixar. DOIS: a que distância abaixa, e quanto tempo fica baixo.
    ///   3. Os dois tipos misturados, com os três números — e agora levantar na
    ///      hora certa importa, porque pode vir um obstáculo baixo logo depois.
    /// </summary>
    public partial class DesafioErro : DesafioEmNiveis
    {
        public override string Etapa => "e6";
        public override string Titulo => "O tamanho do erro";

        protected override int Niveis => Rodadas7.Length;

        /// <summary>Segundos de corrida por tentativa.</summary>
        const float Duracao = 10f;

        /// <summary>
        /// A velocidade de CADA obstáculo, sorteada nesta faixa.
        ///
        /// Era um número só, e com ele bastava achar a margem exata daquela
        /// velocidade — 58 a 141 servia, e o aluno acertava uma vez e pronto.
        /// Isso ensina a achar um número, não a calibrar uma margem.
        ///
        /// Variando obstáculo a obstáculo, nenhuma margem é perfeita para todos:
        /// ele precisa achar uma que AGUENTE a variação inteira. É literalmente o
        /// que "margem de erro" quer dizer, e agora a bancada cobra isso em vez
        /// de só falar disso.
        ///
        /// Medido: com a faixa de 280 a 380, a margem tem que ficar entre 61 e
        /// 118 — cinco dos trinta valores servem. Aperta sem fechar.
        /// </summary>
        const float VelocidadeMinima = 280f;
        const float VelocidadeMaxima = 380f;

        const float LarguraPista = 760f;
        const float AlturaPista = 210f;
        const float XdoRobo = -260f;
        const float LarguraRobo = 44f;
        const float AlturaRobo = 62f;
        const float AlturaAbaixado = 30f;
        const float AlturaDoPulo = 96f;
        const float TempoDoPulo = 0.62f;

        /// <summary>Que tipos de obstáculo cada rodada solta, e o que se ajusta nela.</summary>
        static readonly (bool baixos, bool altos, int botoes)[] Rodadas7 =
        {
            (true,  false, 1),
            (false, true,  2),
            (true,  true,  3)
        };

        /// <summary>
        /// Um obstáculo na pista. <c>Alto</c> é o que passa por cima e obriga a
        /// abaixar; o outro fica no chão e obriga a pular.
        /// </summary>
        struct Obstaculo
        {
            public float X;
            public float Velocidade;
            public bool Alto;
            public bool Contado;
        }

        // ---------------------------------------------------- os números do robô
        //
        // São a "margem de erro" que o aluno calibra. Ficam em unidades de tela
        // porque é o que ele vê: a distância entre o robô e a coisa que vem.

        // OS VALORES DE PARTIDA SÃO ERRADOS DE PROPÓSITO.
        //
        // Eles começavam em 120, e 120 está DENTRO da janela que vence — medido:
        // o pulo funciona de 60 a 140. O aluno apertava "soltar" e ganhava a
        // primeira rodada sem encostar num botão, o que apaga a bancada inteira:
        // ela existe para ele descobrir que o número é que decide.
        //
        // 300 erra do lado que ENSINA. Com a margem no máximo, o robô salta muito
        // antes do obstáculo e aterrissa bem na frente dele — dá para ver, sem
        // ninguém explicar, que ele reagiu cedo demais. Se errasse por 30, ele
        // bateria de frente e pareceria só lentidão.
        float _margemDoPulo = 300f;
        float _margemDoAbaixar = 300f;
        float _tempoAbaixado = 0.9f;

        (bool baixos, bool altos, int botoes) _r;
        Mulberry32 _sorteio;

        readonly List<Obstaculo> _obstaculos = new();
        float _relogio;
        float _proximo;
        float _alturaDoRobo;
        float _tempoDePulo = -1f;
        float _tempoDeAbaixar = -1f;
        int _desviados;
        int _batidas;
        bool _correndo;
        bool _travado;

        protected override void Preparar() =>
            _sorteio = new Mulberry32((uint)Rodadas.Semente());

        protected override void MontarNivel()
        {
            _r = Rodadas7[NivelAtual];
            _travado = false;
            _correndo = false;
            _desviados = 0;
            _batidas = 0;

            MontarTela();
            Painel.Rodape("você não controla o robô — você ajusta a margem de erro dele");
            Painel.Instruir(NivelAtual switch
            {
                0 => "a que distância ele deve saltar?",
                1 => "a que distância ele deve se abaixar, e por quanto tempo?",
                _ => "os dois agora — e ele precisa levantar a tempo"
            });
        }

        // ------------------------------------------------------------- a corrida

        void Soltar()
        {
            if (_correndo || _travado) return;
            // Quem redesenha o painel é a própria corrida, no primeiro quadro:
            // `StartCoroutine` roda até o primeiro yield ainda aqui dentro, então
            // o botão já volta como "parar" sem uma segunda chamada.
            StartCoroutine(Correr());
        }

        /// <summary>
        /// Corta a corrida no meio.
        ///
        /// Só levanta a bandeira; quem sai é o laço, no quadro seguinte. Matar a
        /// corrotina por fora deixaria o robô parado no ar e os obstáculos na
        /// tela, e o aluno teria que adivinhar se aquilo era o jogo travado.
        ///
        /// Parar NÃO conta como rodada vencida nem perdida: é o aluno dizendo
        /// "já vi o que precisava". Assistir dez segundos de um erro entendido no
        /// segundo dois não ensina nada e cansa.
        /// </summary>
        void Parar() => _correndo = false;

        /// <summary>
        /// Dez segundos de corrida, com o robô decidindo sozinho.
        ///
        /// A decisão dele cabe em duas linhas — «tem obstáculo mais perto que a
        /// minha margem? então age» — e é de propósito que seja tão pouco: o
        /// aluno precisa acreditar que não há inteligência escondida ali, só o
        /// número que ele mesmo deixou ajustado.
        /// </summary>
        IEnumerator Correr()
        {
            _correndo = true;
            _obstaculos.Clear();
            _relogio = 0f;
            _proximo = 0.9f;
            _alturaDoRobo = 0f;
            _tempoDePulo = -1f;
            _tempoDeAbaixar = -1f;
            _desviados = 0;
            _batidas = 0;

            DesenharPainel();

            while (_relogio < Duracao && _correndo)
            {
                var dt = Time.deltaTime;
                _relogio += dt;

                Semear(dt);
                Andar(dt);
                Decidir();
                Mover(dt);
                Bater();
                DesenharCorrida();

                yield return null;
            }

            var completou = _relogio >= Duracao;
            _correndo = false;
            DesenharPainel();

            if (completou) Encerrar();
            else Painel.Instruir("parado — ajuste a margem e solte de novo", Cores.Neblina);
        }

        void Semear(float dt)
        {
            _proximo -= dt;
            if (_proximo > 0f) return;

            // O intervalo nunca é menor que o tempo de um pulo inteiro mais folga:
            // dois obstáculos colados fariam o robô bater por impossibilidade, e o
            // aluno culparia a margem que estava certa.
            var alto = _r.altos && (!_r.baixos || _sorteio.Proximo() < 0.5f);
            var velocidade = Mathf.Lerp(VelocidadeMinima, VelocidadeMaxima, _sorteio.Proximo());
            _obstaculos.Add(new Obstaculo
            {
                X = LarguraPista / 2f + 40f,
                Velocidade = velocidade,
                Alto = alto
            });

            // O espaçamento acompanha a velocidade sorteada: em unidades de tempo,
            // e não de distância. Espaçar por distância faria o obstáculo rápido
            // chegar antes de o robô ter descido do pulo anterior, e a batida seria
            // por impossibilidade — o aluno culparia a margem que estava certa.
            _proximo = TempoDoPulo + 0.45f + _sorteio.Proximo() * 0.5f;
        }

        void Andar(float dt)
        {
            for (var i = 0; i < _obstaculos.Count; i++)
            {
                var o = _obstaculos[i];
                o.X -= o.Velocidade * dt;
                _obstaculos[i] = o;
            }
            _obstaculos.RemoveAll(o => o.X < -LarguraPista);
        }

        /// <summary>A cabeça do robô, inteira.</summary>
        void Decidir()
        {
            if (_tempoDePulo >= 0f || _tempoDeAbaixar >= 0f) return;

            foreach (var o in _obstaculos)
            {
                var distancia = o.X - XdoRobo;
                if (distancia < 0f) continue;

                if (o.Alto)
                {
                    if (distancia <= _margemDoAbaixar) { _tempoDeAbaixar = 0f; return; }
                }
                else
                {
                    if (distancia <= _margemDoPulo) { _tempoDePulo = 0f; return; }
                }
            }
        }

        void Mover(float dt)
        {
            if (_tempoDePulo >= 0f)
            {
                _tempoDePulo += dt;
                var f = _tempoDePulo / TempoDoPulo;
                // Arco simples: sobe e desce numa parábola.
                _alturaDoRobo = f >= 1f ? 0f : AlturaDoPulo * 4f * f * (1f - f);
                if (f >= 1f) _tempoDePulo = -1f;
            }
            else if (_tempoDeAbaixar >= 0f)
            {
                _tempoDeAbaixar += dt;
                if (_tempoDeAbaixar >= _tempoAbaixado) _tempoDeAbaixar = -1f;
            }
        }

        bool Abaixado => _tempoDeAbaixar >= 0f;

        void Bater()
        {
            var meioRobo = LarguraRobo / 2f;

            for (var i = 0; i < _obstaculos.Count; i++)
            {
                var o = _obstaculos[i];
                if (o.Contado) continue;

                var encostou = Mathf.Abs(o.X - XdoRobo) < meioRobo + 16f;
                if (!encostou)
                {
                    // Passou inteiro sem encostar: desviou.
                    if (o.X < XdoRobo - meioRobo - 16f)
                    {
                        o.Contado = true;
                        _desviados++;
                        _obstaculos[i] = o;
                    }
                    continue;
                }

                // Alto exige estar abaixado; baixo exige estar no ar.
                var livrou = o.Alto ? Abaixado : _alturaDoRobo > AlturaRobo * 0.55f;
                if (livrou) continue;

                o.Contado = true;
                _batidas++;
                _obstaculos[i] = o;
                Widgets.Tremer(_pista, 9f, 0.2f);
            }
        }

        // -------------------------------------------------------------- o fecho

        void Encerrar()
        {
            var total = _desviados + _batidas;
            var passou = _batidas == 0 && _desviados >= 3;

            if (!passou)
            {
                Painel.Instruir(
                    _batidas == 0
                        ? "ninguém veio — deixe rodar de novo"
                        : $"bateu {_batidas} de {total}. mexa na margem e solte outra vez",
                    Cores.Brasa);
                DesenharPainel();
                return;
            }

            _travado = true;

            // Na última rodada o robô toma a palavra. A poesia entra no mesmo
            // cartaz, e não numa tela à parte, porque o aluno já está lendo aqui —
            // uma tela a mais só adiaria a piada.
            // No último cartaz a lição encolhe para a poesia caber.
            //
            // O cartaz tem altura fixa, e somar a lição inteira à poesia estourava
            // a área e cobria o botão de sair. Entre cortar a explicação e cortar
            // a piada, corta-se a explicação: a essa altura o aluno já calibrou
            // três vezes e a lição está nas mãos dele, não no texto.
            var fecho = NivelAtual == Niveis - 1
                ? "Você mexeu nos números, mediu o estrago, mexeu de novo.\n" +
                  "É isso que o treino de uma máquina faz sozinho, milhões\n" +
                  "de vezes.\n\n" + Poesia()
                : Licao();

            Resolveu($"Dez segundos limpos — {_desviados} desviados", fecho);
        }

        /// <summary>
        /// A poesia do robô, no fim da bancada.
        ///
        /// É piada, e é piada com endereço: ele agradece dizendo que está QUASE
        /// falando sozinho — e "quase" é a palavra honesta, porque falar sozinho é
        /// exatamente a bancada 10. A 7 treinou o pulo dele; a fala vem depois, e
        /// o aluno vai treinar aquela também.
        ///
        /// A métrica é capenga de propósito. Robô que acabou de aprender a pular
        /// não rima direito, e a rima torta é a piada.
        /// </summary>
        static string Poesia() =>
            "O ROBÔ PIGARREIA:\n\n" +
            "  “Antes eu batia a lata,\n" +
            "  hoje eu pulo e não me assusto.\n" +
            "  Já sei a hora de abaixar,\n" +
            "  já sei a hora de... de... — isso!\n\n" +
            "  Viu? Quase falei sozinho.\n" +
            "  Hoje eu só corro. E corro bem,\n" +
            "  porque vocês me calibraram.”";

        string Licao() => NivelAtual switch
        {
            0 => "Você não pulou nenhuma vez. Ajustou UM número — a que\n" +
                 "distância ele reage — e ele fez o resto sozinho.\n\n" +
                 "Esse número é a margem de erro dele. Cedo demais, ele cai\n" +
                 "em cima do obstáculo; tarde demais, bate de frente.",

            1 => "Agora foram dois números, e repare que eles brigam: abaixar\n" +
                 "cedo e levantar rápido é o mesmo que não ter abaixado.\n\n" +
                 "Ajustar um de cada vez é o único jeito de saber qual dos\n" +
                 "dois estava errado. É assim que se afina um modelo de verdade.",

            _ => "Três números, dois tipos de obstáculo, e uma corrida em que\n" +
                 "levantar cedo demais é tão ruim quanto não abaixar.\n\n" +
                 "Você acabou de fazer, na mão, o que o treino de uma máquina\n" +
                 "faz sozinho milhões de vezes: mexer nos números, medir o\n" +
                 "estrago, mexer de novo."
        };

    }
}
