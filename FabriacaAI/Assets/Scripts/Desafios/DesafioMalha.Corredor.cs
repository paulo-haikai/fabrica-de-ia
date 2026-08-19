using System.Collections.Generic;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// O CORREDOR DA IARA — port do FableDevil, de Leonxlnx.
    ///
    /// Origem: <https://github.com/Leonxlnx/level-devil>, licença MIT. O aviso de
    /// copyright e o texto da licença estão em TERCEIROS.md, na raiz do repositório,
    /// como a licença exige de quem copia porção substancial.
    ///
    /// Este arquivo é o laço: comandos, física e colisão. As armadilhas estão em
    /// DesafioMalha.Armadilhas.cs e as trinta salas em DesafioMalha.Corredor.Fases.cs.
    ///
    /// AS CONSTANTES SÃO AS DO ORIGINAL, sem uma vírgula mexida: 265 de velocidade,
    /// 2600 de aceleração no chão e 1800 no ar, 2150 de gravidade, 645 de pulo com
    /// corte em 220, queda máxima de 980, boneco de 26 por 32. Num platformer é a
    /// relação entre esses números que produz a sensação, e mexer num deles isolado
    /// — a tentação de "deixar o pulo um pouquinho mais alto" — desmonta trinta salas
    /// calibradas contra eles.
    ///
    /// O EIXO Y APONTA PARA BAIXO, como no canvas do original. Quem converte para o
    /// jeito da Unity é o desenho, num lugar só.
    ///
    /// O QUE ISTO TEM A VER COM A AULA. O boneco carrega um PACOTE DE INFORMAÇÃO e
    /// precisa entregá-lo na saída. Dez salas depois, o último nível abre a
    /// máquina e mostra o caminho que a informação percorre de verdade — a parede de
    /// 318 lâmpadas, os doze neurônios, a frente de luz atravessando a malha. Ver
    /// DesafioMalha.Revelacao.cs. A bancada não tenta explicar a rede DURANTE o jogo:
    /// tentou, em duas versões, e o resultado foi um jogo pequeno e uma explicação
    /// pela metade.
    /// </summary>
    public partial class DesafioMalha
    {
        // As constantes de física do original.
        const float Velocidade = 265f;
        const float AceleraNoChao = 2600f;
        const float AceleraNoAr = 1800f;
        const float Gravidade = 2150f;
        const float ImpulsoDoPulo = -645f;
        const float CortaOPulo = -220f;
        const float QuedaMaxima = 980f;

        const float FolgaDaBeirada = 0.10f;
        const float PuloGuardado = 0.12f;

        /// <summary>
        /// Cinco salas por nível, dois níveis. O terceiro é a revelação.
        ///
        /// DEZ SALAS POR AULA, e não as trinta que existem. Trinta é o acervo; dez é
        /// a partida. A bancada tem noventa minutos divididos por doze, e o corredor
        /// comia a aula inteira antes de a malha aparecer — que é o que a bancada
        /// veio ensinar. As vinte que sobram não são desperdício: a ordem é sorteada,
        /// então cada turma pega um corredor diferente, e quem repetir a bancada não
        /// repete a aula.
        /// </summary>
        public const int SalasPorNivel = 5;
        public const int NiveisDeCorredor = 2;

        /// <summary>Quantas salas o aluno atravessa numa aula.</summary>
        public const int SalasNaAula = SalasPorNivel * NiveisDeCorredor;

        /// <summary>Quantos tombos cabem por nível e ainda dão a estrela.</summary>
        static readonly int[] TombosPermitidos = { 12, 20 };

        // --------------------------------------------------------------- estado

        bool _noCorredor;
        int _salaAtual;
        Sala _sala;

        /// <summary>A ordem em que as salas aparecem nesta aula.</summary>
        int[] _ordem;

        Ret[] _solidosDaSala;
        Armadilha[] _truques;
        Saida _saida;
        readonly Estado _estado = new();
        readonly Boneco _boneco = new();

        float _puloPedido;
        float _desdeOChao;
        int _olhando = 1;
        float _passoDaAnimacao;
        bool _andando;

        int _tombos;
        int _tombosNoNivel;
        bool _travado;

        readonly List<Ret> _solidosAgora = new();
        readonly List<Ret> _mataAgora = new();

        // --------------------------------------------------------------- montar

        /// <summary>
        /// Monta o roteiro da aula: DEZ salas tiradas das trinta, com as quatro
        /// primeiras no lugar.
        ///
        /// As quatro que ensinam ficam fixas: andar, pular, o chão que mente e o
        /// espinho que dói. Sortear essas seria começar a aula pela sala 27, e um
        /// aluno que morre seis vezes antes de entender o controle desiste antes de
        /// achar graça. As outras seis saem sorteadas do acervo inteiro — duas
        /// turmas não pegam a mesma sequência, e quem repetir a bancada não repete
        /// a aula.
        ///
        /// Embaralha as vinte e seis restantes por Fisher-Yates e corta na décima.
        /// Sortear tudo e cortar dá a mesma distribuição que sortear seis sem
        /// reposição, e cabe nas linhas que já estavam escritas.
        /// </summary>
        void SortearOrdem()
        {
            const int fixas = 4;
            var acervo = new int[Salas.Length];
            for (var i = 0; i < acervo.Length; i++) acervo[i] = i;

            for (var i = acervo.Length - 1; i > fixas; i--)
            {
                var j = fixas + (int)(_sorteio.Proximo() * (i - fixas + 1));
                if (j > i) j = i;
                (acervo[i], acervo[j]) = (acervo[j], acervo[i]);
            }

            _ordem = new int[SalasNaAula];
            System.Array.Copy(acervo, _ordem, SalasNaAula);
        }

        void MontarSala()
        {
            _sala = Salas[_ordem[_salaAtual]];
            _noCorredor = true;
            _travado = false;

            _solidosDaSala = _sala.Solidos();
            _truques = _sala.Truques();
            _saida = _sala.Saida();

            _estado.Jogador = _boneco;
            _estado.Sacudir = (forca, tempo) =>
            {
                if (_janelaDoCorredor != null) Widgets.Tremer(_janelaDoCorredor, forca, tempo);
            };

            Recomecar();
            MontarCorredorNaTela();

            Painel.MarcarPasso(NivelAtual + 1, Niveis,
                               $"sala {_salaAtual + 1} de {SalasNaAula} · {_sala.Nome}");
            Painel.Rodape("setas para correr · espaço para pular");
            Painel.Instruir($"tombos: {_tombosNoNivel}", Cores.Neblina);
        }

        void Recomecar()
        {
            _boneco.x = _sala.Nasce.x;
            _boneco.y = _sala.Nasce.y;
            _boneco.vx = 0f;
            _boneco.vy = 0f;
            _boneco.NoChao = true;

            _puloPedido = 0f;
            _desdeOChao = 0f;
            _olhando = 1;

            _estado.Chaves.Clear();
            _estado.Invertido = false;

            _saida.Zerar();
            foreach (var a in _truques) a.Zerar();
        }

        // ---------------------------------------------------------------- o laço

        void Update()
        {
            if (!_noCorredor || _travado || _sala == null) return;

            // Passo fixo. Com dt livre, um engasgo de duzentos milissegundos atravessa
            // o boneco por dentro de um bloco e ele cai pelo chão — e o jogador leva
            // um tombo que não foi dele.
            var restante = Mathf.Min(Time.deltaTime, 0.1f);
            const float fatia = 1f / 120f;

            while (restante > 0f && !_travado)
            {
                var dt = Mathf.Min(fatia, restante);
                restante -= dt;
                UmQuadro(dt);
            }

            Repintar();
        }

        void UmQuadro(float dt)
        {
            _estado.Invertido = false;
            foreach (var a in _truques) a.Passo(dt, _estado);

            Comandos(dt);
            Mover(dt);
            if (_travado) return;

            if (Morreu()) { Tombar(); return; }

            if (_saida.Passo(_boneco)) { MudouASaida(); return; }
            if (_boneco.Caixa.Toca(_saida.Caixa)) Venceu();
        }

        void Comandos(float dt)
        {
            var t = Keyboard.current;
            if (t == null) return;

            var esquerda = t.leftArrowKey.isPressed || t.aKey.isPressed;
            var direita = t.rightArrowKey.isPressed || t.dKey.isPressed;
            if (_estado.Invertido) (esquerda, direita) = (direita, esquerda);

            var rumo = 0f;
            if (esquerda) rumo -= 1f;
            if (direita) rumo += 1f;

            _andando = rumo != 0f;
            if (_andando) _olhando = rumo > 0f ? 1 : -1;

            var alvo = rumo * Velocidade;
            var acelera = (_boneco.NoChao ? AceleraNoChao : AceleraNoAr) * dt;
            _boneco.vx = Mathf.MoveTowards(_boneco.vx, alvo, acelera);

            var pulou = t.spaceKey.wasPressedThisFrame || t.upArrowKey.wasPressedThisFrame ||
                        t.wKey.wasPressedThisFrame;
            _puloPedido = pulou ? PuloGuardado : Mathf.Max(0f, _puloPedido - dt);

            if (_puloPedido > 0f && _desdeOChao < FolgaDaBeirada)
            {
                _boneco.vy = ImpulsoDoPulo;
                _boneco.NoChao = false;
                _desdeOChao = FolgaDaBeirada;
                _puloPedido = 0f;
            }

            // Corte do pulo: soltar cedo trava a subida em 220. É o que separa um pulo
            // que obedece de um pulo que só tem uma altura.
            var segurando = t.spaceKey.isPressed || t.upArrowKey.isPressed || t.wKey.isPressed;
            if (!segurando && _boneco.vy < CortaOPulo) _boneco.vy = CortaOPulo;

            _passoDaAnimacao += Mathf.Abs(_boneco.vx) * dt * 0.06f;
        }

        void Mover(float dt)
        {
            _boneco.vy = Mathf.Min(_boneco.vy + Gravidade * dt, QuedaMaxima);

            JuntarSolidos();

            _boneco.x += _boneco.vx * dt;
            ResolverX();

            _boneco.y += _boneco.vy * dt;
            ResolverY();

            _desdeOChao = _boneco.NoChao ? 0f : _desdeOChao + dt;
            _boneco.x = Mathf.Clamp(_boneco.x, -20f, Larg - Boneco.L + 20f);
        }

        void JuntarSolidos()
        {
            _solidosAgora.Clear();
            _solidosAgora.AddRange(_solidosDaSala);
            foreach (var a in _truques)
            {
                var s = a.Solidos();
                for (var i = 0; i < s.Count; i++) _solidosAgora.Add(s[i]);
            }
        }

        void ResolverX()
        {
            foreach (var s in _solidosAgora)
            {
                if (!s.Toca(_boneco.Caixa)) continue;
                if (_boneco.vx > 0f) _boneco.x = s.x - Boneco.L - 0.01f;
                else if (_boneco.vx < 0f) _boneco.x = s.Direita + 0.01f;
                else continue;
                _boneco.vx = 0f;
            }
        }

        void ResolverY()
        {
            _boneco.NoChao = false;

            foreach (var s in _solidosAgora)
            {
                if (!s.Toca(_boneco.Caixa)) continue;

                if (_boneco.vy >= 0f)
                {
                    _boneco.y = s.y - Boneco.A;
                    _boneco.vy = 0f;
                    _boneco.NoChao = true;
                }
                else
                {
                    _boneco.y = s.Base;
                    _boneco.vy = 0f;
                }
            }
        }

        bool Morreu()
        {
            if (_boneco.y > Alto + 40f) return true;

            _mataAgora.Clear();
            foreach (var a in _truques)
            {
                var m = a.Mata();
                for (var i = 0; i < m.Count; i++) _mataAgora.Add(m[i]);
            }

            var caixa = _boneco.Caixa;
            foreach (var m in _mataAgora)
                if (m.Toca(caixa)) return true;

            return false;
        }

        // -------------------------------------------------------- errar e acertar

        /// <summary>
        /// As frases de morte. São do gênero, e não enfeite: um jogo que faz morrer
        /// trinta vezes precisa que a trigésima ainda arranque um sorriso.
        /// </summary>
        static readonly string[] Tombadas =
        {
            "essa doeu.", "de novo?", "clássico.", "quase.", "quem pôs isso aí?",
            "perfeitamente planejado.", "você caiu direitinho.", "ops.",
            "tente andar mais devagar.", "essa foi sua.", "he-he.", "boa tentativa."
        };

        void Tombar()
        {
            if (_travado) return;

            _tombos++;
            _tombosNoNivel++;

            Widgets.Tremer(_janelaDoCorredor, 11f, 0.22f);
            Widgets.Lampejo(_janelaDoCorredor, Cores.Brasa, 0.35f, 0.28f);

            var frase = Tombadas[(int)(_sorteio.Proximo() * Tombadas.Length) % Tombadas.Length];
            Painel.Instruir($"{frase}   ·   tombos: {_tombosNoNivel}", Cores.Brasa);

            Recomecar();
            MudouASaida();
            Repintar();
        }

        void Venceu()
        {
            _travado = true;

            Painel.Instruir("pacote entregue", Cores.Folha);
            Widgets.Lampejo(_janelaDoCorredor, Cores.Luz, 0.4f, 0.4f);
            Repintar();

            _salaAtual++;
            var ate = (NivelAtual + 1) * SalasPorNivel;

            if (_salaAtual < ate)
            {
                Painel.Acao("próxima sala", () => { Painel.Acao(null, null); MontarSala(); });
                return;
            }

            Painel.Acao("ver o resultado", FecharCorredor);
        }

        void FecharCorredor()
        {
            _noCorredor = false;
            var permitidos = TombosPermitidos[NivelAtual];
            var ultimo = NivelAtual == NiveisDeCorredor - 1;

            var fecho = ultimo
                ? "O pacote atravessou as dez salas. Falta ver o que ele atravessa\n" +
                  "de verdade — e é isso que vem agora."
                : "Cada sala é um pedaço do caminho. O pacote continua com você.";

            if (_tombosNoNivel <= permitidos)
            {
                Resolveu($"{SalasPorNivel} salas · {_tombosNoNivel} tombos", fecho);
                return;
            }

            Falhou($"{SalasPorNivel} salas · {_tombosNoNivel} tombos",
                $"Passou, mas tombou {_tombosNoNivel} vezes — mais que os {permitidos} de folga.\n\n" +
                "Este lugar mente o tempo todo, e é assim que ele foi feito.\n\n" + fecho,
                contaEstrela: _tombosNoNivel <= permitidos * 2);
        }
    }
}
