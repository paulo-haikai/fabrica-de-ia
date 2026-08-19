using System.Collections.Generic;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// AS ARMADILHAS DO CORREDOR — port do FableDevil, de Leonxlnx.
    ///
    /// Origem: <https://github.com/Leonxlnx/level-devil>, licença MIT. O aviso de
    /// copyright e o texto da licença estão em TERCEIROS.md, na raiz do repositório,
    /// como a licença exige de quem copia porção substancial.
    ///
    /// O que veio de lá: a física do boneco, o conjunto de armadilhas e o traçado das
    /// trinta fases. O que é daqui: a arte, os textos e a moldura da aula.
    ///
    /// O EIXO Y APONTA PARA BAIXO, como no canvas do original, e isso é deliberado.
    /// A tentação era virar tudo para o jeito da Unity, mas as trinta fases são
    /// centenas de coordenadas escritas à mão contra um piso em y=480: converter
    /// número a número seria a fonte mais provável de erro do port inteiro, e um erro
    /// desses não aparece compilando — aparece numa sala impossível, três semanas
    /// depois, com uma turma esperando. Quem converte é o desenho, num lugar só.
    ///
    /// A FORMA DE CADA ARMADILHA é a do original: quatro métodos e nada mais.
    /// `Zerar` volta ao estado inicial quando a sala recomeça, `Passo` anda um quadro,
    /// `Solidos` diz em que dá para pisar agora, e `Mata` diz o que encosta e derruba.
    /// Manter essa forma é o que deixa o port conferível linha a linha contra o
    /// original — e é o que permite acrescentar uma armadilha nova sem tocar no laço
    /// do jogo.
    /// </summary>
    public partial class DesafioMalha
    {
        public const float Larg = 960f;
        public const float Alto = 540f;

        /// <summary>O y do piso rasteiro. No eixo do original, para BAIXO.</summary>
        public const float Chao = 480f;

        /// <summary>Retângulo do mundo. Y cresce para baixo, como no canvas.</summary>
        public struct Ret
        {
            public float x, y, l, a;
            public Ret(float x, float y, float l, float a) { this.x = x; this.y = y; this.l = l; this.a = a; }
            public float Direita => x + l;
            public float Base => y + a;
            public bool Toca(Ret o) => x < o.Direita && Direita > o.x && y < o.Base && Base > o.y;
        }

        public static Ret R(float x, float y, float l, float a) => new(x, y, l, a);

        /// <summary>Um trecho de piso, de x0 a x1. Espelha `floorSeg` do original.</summary>
        public static Ret Piso(float x0, float x1, float y = Chao) => new(x0, y, x1 - x0, Alto - y);

        public static Ret ParedeE() => new(-40f, -200f, 40f, Alto + 400f);
        public static Ret ParedeD() => new(Larg, -200f, 40f, Alto + 400f);
        public static Ret Teto(float a = 30f) => new(0f, 0f, Larg, a);

        /// <summary>O boneco, do jeito que as armadilhas precisam mexer nele.</summary>
        public class Boneco
        {
            public float x, y, vx, vy;
            public const float L = 26f;
            public const float A = 32f;
            public bool NoChao;
            public Ret Caixa => new(x, y, L, A);
        }

        /// <summary>O que uma armadilha enxerga do jogo. Espelha o `g` do original.</summary>
        public class Estado
        {
            public Boneco Jogador;
            public readonly Dictionary<string, bool> Chaves = new();
            public bool Invertido;
            /// <summary>Pedido de tremor de tela: força e duração.</summary>
            public System.Action<float, float> Sacudir = (_, _) => { };
        }

        static readonly Ret[] Nenhum = System.Array.Empty<Ret>();

        public abstract class Armadilha
        {
            public virtual void Zerar() { }
            public virtual void Passo(float dt, Estado g) { }
            public virtual IReadOnlyList<Ret> Solidos() => Nenhum;
            public virtual IReadOnlyList<Ret> Mata() => Nenhum;
        }

        // ------------------------------------------------------------ CollapseFloor

        /// <summary>O chão que treme e cai. Dispara ao jogador entrar no gatilho.</summary>
        public class ChaoQueCede : Armadilha
        {
            public Ret Caixa, Gatilho;
            public float Tremor = 0.18f, Atraso;
            public string Fase = "parado";
            public float t, dy, vy;

            public ChaoQueCede(Ret caixa, Ret gatilho, float tremor = 0.18f, float atraso = 0f)
            { Caixa = caixa; Gatilho = gatilho; Tremor = tremor; Atraso = atraso; }

            public override void Zerar() { Fase = "parado"; t = 0f; dy = 0f; vy = 0f; }

            public override void Passo(float dt, Estado g)
            {
                if (Fase == "parado")
                {
                    if (g.Jogador.Caixa.Toca(Gatilho)) { Fase = "espera"; t = 0f; }
                }
                else if (Fase == "espera")
                {
                    t += dt;
                    if (t >= Atraso) { Fase = "treme"; t = 0f; g.Sacudir(4f, 0.18f); }
                }
                else if (Fase == "treme")
                {
                    t += dt;
                    if (t >= Tremor) Fase = "cai";
                }
                else if (Fase == "cai")
                {
                    vy += 2400f * dt;
                    dy += vy * dt;
                }
            }

            public Ret Agora => new(Caixa.x, Caixa.y + dy, Caixa.l, Caixa.a);
            public override IReadOnlyList<Ret> Solidos() => Fase == "cai" ? Nenhum : new[] { Caixa };
        }

        // -------------------------------------------------------------- PopSpikes

        /// <summary>
        /// Espinhos que sobem do chão. Com gatilho, sobem uma vez; sem gatilho, sobem
        /// e descem no ritmo do período — é a diferença entre a armadilha e a dança.
        /// </summary>
        public class EspinhosQueSobem : Armadilha
        {
            public float x, y, l, Tamanho = 26f, Atraso, Periodo, Fase0, Segura = 0.8f, Rapidez = 14f;
            public bool ParaBaixo;
            public Ret? Gatilho;
            public float Fora, t, ct;
            string _estado;

            public EspinhosQueSobem(float x, float y, float l, Ret? gatilho)
            { this.x = x; this.y = y; this.l = l; Gatilho = gatilho; }

            public override void Zerar()
            { Fora = 0f; _estado = Gatilho.HasValue ? "parado" : "ciclo"; t = -Atraso; ct = Fase0; }

            public override void Passo(float dt, Estado g)
            {
                if (_estado == "parado")
                {
                    if (Gatilho.HasValue && g.Jogador.Caixa.Toca(Gatilho.Value))
                    { _estado = "subindo"; t = -Atraso; }
                }
                else if (_estado == "subindo")
                {
                    t += dt;
                    if (t >= 0f) Fora = Mathf.Clamp01(Fora + Rapidez * dt);
                }
                else if (_estado == "ciclo")
                {
                    ct += dt;
                    var c = Periodo <= 0f ? 0f : Mathf.Repeat(ct, Periodo);
                    if (c < Segura) Fora = Mathf.Clamp01(Fora + Rapidez * dt);
                    else Fora = Mathf.Clamp01(Fora - Rapidez * 0.6f * dt);
                }
            }

            public override IReadOnlyList<Ret> Mata()
            {
                if (Fora < 0.45f) return Nenhum;
                var h = Tamanho * Fora - 6f;
                return ParaBaixo
                    ? new[] { new Ret(x + 4f, y, l - 8f, h) }
                    : new[] { new Ret(x + 4f, y - h, l - 8f, h) };
            }
        }

        // -------------------------------------------------------------- FallBlock

        /// <summary>Bloco preso no teto que despenca. Só mata enquanto cai.</summary>
        public class BlocoQueCai : Armadilha
        {
            public Ret Casa, Gatilho;
            public float Tremor = 0.12f, PisoY = Chao;
            public Ret Caixa;
            string _estado; float t, vy;

            public BlocoQueCai(Ret casa, Ret gatilho) { Casa = casa; Gatilho = gatilho; Caixa = casa; }

            public override void Zerar() { Caixa = Casa; _estado = "parado"; t = 0f; vy = 0f; }

            public override void Passo(float dt, Estado g)
            {
                if (_estado == "parado")
                {
                    if (g.Jogador.Caixa.Toca(Gatilho)) { _estado = "treme"; t = 0f; }
                }
                else if (_estado == "treme")
                {
                    t += dt;
                    if (t > Tremor) _estado = "cai";
                }
                else if (_estado == "cai")
                {
                    vy += 3000f * dt;
                    Caixa.y += vy * dt;
                    if (Caixa.Base >= PisoY)
                    {
                        Caixa.y = PisoY - Caixa.a;
                        _estado = "pousou";
                        g.Sacudir(7f, 0.22f);
                    }
                }
            }

            public bool Tremendo => _estado == "treme";
            public override IReadOnlyList<Ret> Solidos() => _estado == "cai" ? Nenhum : new[] { Caixa };
            public override IReadOnlyList<Ret> Mata() => _estado == "cai"
                ? new[] { new Ret(Caixa.x + 3f, Caixa.y + 4f, Caixa.l - 6f, Caixa.a - 4f) }
                : Nenhum;
        }

        // ---------------------------------------------------------------- Crusher

        /// <summary>O martelo que desce do teto e volta. Só mata na descida.</summary>
        public class Esmagador : Armadilha
        {
            public float x, l, TopoY, AlturaDaCabeca = 46f, PisoY = Chao;
            public float Periodo, Fase0, VelDescida = 1500f, VelSubida = 240f, Segura = 0.32f;
            public Ret? Gatilho;
            public float y;
            string _estado; float t; bool _bateu;

            public Esmagador(float x, float l) { this.x = x; this.l = l; }

            public override void Zerar()
            { y = TopoY; _estado = Gatilho.HasValue ? "armado" : "esperando"; t = Fase0; _bateu = false; }

            public override void Passo(float dt, Estado g)
            {
                var maxY = PisoY - AlturaDaCabeca;

                if (_estado == "armado")
                {
                    if (Gatilho.HasValue && g.Jogador.Caixa.Toca(Gatilho.Value)) _estado = "desce";
                }
                else if (_estado == "esperando")
                {
                    t += dt;
                    if (t >= Periodo) { t = 0f; _estado = "desce"; }
                }
                else if (_estado == "desce")
                {
                    y += VelDescida * dt;
                    if (y >= maxY)
                    {
                        y = maxY; _estado = "segura"; t = 0f;
                        if (!_bateu) g.Sacudir(6f, 0.18f);
                        _bateu = true;
                    }
                }
                else if (_estado == "segura")
                {
                    t += dt;
                    if (t >= Segura) _estado = "sobe";
                }
                else if (_estado == "sobe")
                {
                    y -= VelSubida * dt;
                    if (y <= TopoY)
                    {
                        y = TopoY; _bateu = false;
                        _estado = Gatilho.HasValue ? "gasto" : "esperando";
                        t = 0f;
                    }
                }
            }

            public Ret Cabeca => new(x, y, l, AlturaDaCabeca);
            public override IReadOnlyList<Ret> Solidos() => new[] { Cabeca };
            public override IReadOnlyList<Ret> Mata() => _estado == "desce"
                ? new[] { new Ret(x + 2f, y + AlturaDaCabeca - 14f, l - 4f, 16f) }
                : Nenhum;
        }

        // ------------------------------------------------------- CrumblePlatform

        /// <summary>A plataforma que esfarela sob o peso.</summary>
        public class PlataformaQueEsfarela : Armadilha
        {
            public Ret Casa;
            public float Atraso = 0.35f;
            public Ret Caixa;
            string _estado; float t, vy;

            public PlataformaQueEsfarela(Ret casa, float atraso = 0.35f)
            { Casa = casa; Caixa = casa; Atraso = atraso; }

            public override void Zerar() { Caixa = Casa; _estado = "parado"; t = 0f; vy = 0f; }

            public override void Passo(float dt, Estado g)
            {
                var p = g.Jogador;
                var emCima = p.vy >= -1f && p.x + Boneco.L > Caixa.x + 2f && p.x < Caixa.Direita - 2f &&
                             Mathf.Abs(p.y + Boneco.A - Caixa.y) <= 6f;

                if (_estado == "parado") { if (emCima) { _estado = "treme"; t = 0f; } }
                else if (_estado == "treme")
                {
                    t += dt;
                    if (t >= Atraso) _estado = "cai";
                }
                else if (_estado == "cai")
                {
                    vy += 2400f * dt;
                    Caixa.y += vy * dt;
                }
            }

            public bool Tremendo => _estado == "treme";
            public override IReadOnlyList<Ret> Solidos() => _estado == "cai" ? Nenhum : new[] { Caixa };
        }

        // ----------------------------------------------------------- SlidingHole

        /// <summary>
        /// O buraco que persegue o jogador pelo chão. O piso inteiro é dele: o que
        /// ele devolve como sólido são os dois pedaços de cada lado do vão.
        /// </summary>
        public class BuracoCorredico : Armadilha
        {
            public float x0, x1, y = Chao, a = 60f, Vao = 92f, Comeco, Rapidez = 130f;
            public Ret? Gatilho;
            public float gx; bool _ativo;

            public BuracoCorredico(float x0, float x1) { this.x0 = x0; this.x1 = x1; Comeco = x1 - 100f; }

            public override void Zerar() { gx = Comeco; _ativo = !Gatilho.HasValue; }

            public override void Passo(float dt, Estado g)
            {
                if (!_ativo && Gatilho.HasValue && g.Jogador.Caixa.Toca(Gatilho.Value)) _ativo = true;
                if (!_ativo) return;

                var alvo = Mathf.Clamp(g.Jogador.x + Boneco.L * 0.5f,
                                       x0 + Vao * 0.5f + 4f, x1 - Vao * 0.5f - 4f);
                gx = Mathf.MoveTowards(gx, alvo, Rapidez * dt);
            }

            public override IReadOnlyList<Ret> Solidos()
            {
                var e = gx - Vao * 0.5f;
                var d = gx + Vao * 0.5f;
                var fora = new List<Ret>(2);
                if (e > x0 + 2f) fora.Add(new Ret(x0, y, e - x0, a));
                if (d < x1 - 2f) fora.Add(new Ret(d, y, x1 - d, a));
                return fora;
            }
        }

        // ---------------------------------------------------------- StaticSpikes

        public class EspinhosFixos : Armadilha
        {
            public float x, y, l, Tamanho = 26f;
            public bool ParaBaixo;

            public EspinhosFixos(float x, float y, float l) { this.x = x; this.y = y; this.l = l; }

            public override IReadOnlyList<Ret> Mata() => ParaBaixo
                ? new[] { new Ret(x + 4f, y, l - 8f, Tamanho - 8f) }
                : new[] { new Ret(x + 4f, y - Tamanho + 8f, l - 8f, Tamanho - 8f) };
        }

        // ------------------------------------------------------------ InvertZone

        public class ZonaInvertida : Armadilha
        {
            public Ret Caixa;
            public ZonaInvertida(Ret caixa) { Caixa = caixa; }
            public override void Passo(float dt, Estado g)
            { if (g.Jogador.Caixa.Toca(Caixa)) g.Invertido = true; }
        }

        // -------------------------------------------------------- MovingPlatform

        /// <summary>
        /// A plataforma que CARREGA o jogador. O empurrão em quem está em cima é a
        /// parte que não se pode esquecer: sem ele o boneco escorrega da plataforma
        /// como se ela fosse gelo, e o elevador deixa de ser elevador.
        /// </summary>
        public class PlataformaMovel : Armadilha
        {
            public float ax, ay, bx, by, l, a, Rapidez = 80f, Fase0, Pausa;
            public float px, py, dx, dy;
            float _travessia, _ciclo, t;

            public PlataformaMovel(Ret caixa) { ax = caixa.x; ay = caixa.y; bx = caixa.x; by = caixa.y; l = caixa.l; a = caixa.a; }

            public override void Zerar()
            {
                var d = Mathf.Max(1f, Vector2.Distance(new Vector2(ax, ay), new Vector2(bx, by)));
                _travessia = d / Rapidez;
                _ciclo = _travessia * 2f + Pausa * 2f;
                t = Fase0 * _ciclo;
                var p = Onde(t);
                px = p.x; py = p.y; dx = 0f; dy = 0f;
            }

            Vector2 Onde(float tempo)
            {
                var u = Mathf.Repeat(tempo, _ciclo);
                float f;
                if (u < _travessia) f = u / _travessia;
                else if (u < _travessia + Pausa) f = 1f;
                else if (u < _travessia * 2f + Pausa) f = 1f - (u - _travessia - Pausa) / _travessia;
                else f = 0f;

                // Aceleração suave nas pontas, como no original: sem ela o elevador
                // arranca e para de supetão, e o boneco é cuspido fora.
                var e = f < 0.5f ? 2f * f * f : 1f - Mathf.Pow(-2f * f + 2f, 2f) / 2f;
                return new Vector2(Mathf.Lerp(ax, bx, e), Mathf.Lerp(ay, by, e));
            }

            public override void Passo(float dt, Estado g)
            {
                var antesX = px; var antesY = py;
                t += dt;
                var p = Onde(t);
                px = p.x; py = p.y;
                dx = px - antesX; dy = py - antesY;

                var j = g.Jogador;
                var emCima = j.vy >= -1f &&
                             j.x + Boneco.L > antesX + 2f && j.x < antesX + l - 2f &&
                             Mathf.Abs(j.y + Boneco.A - antesY) <= 8f;
                if (!emCima) return;
                j.x += dx; j.y += dy;
            }

            public override IReadOnlyList<Ret> Solidos() => new[] { new Ret(px, py, l, a) };
        }

        // -------------------------------------------------------------- Conveyor

        public class Esteira : Armadilha
        {
            public Ret Caixa;
            public int Rumo = 1;
            public float Forca = 150f;
            public float t;

            public Esteira(Ret caixa) { Caixa = caixa; }
            public override void Zerar() { t = 0f; }

            public override void Passo(float dt, Estado g)
            {
                t += dt * Rumo;
                var p = g.Jogador;
                var emCima = p.NoChao && Mathf.Abs(p.y + Boneco.A - Caixa.y) < 4f &&
                             p.x + Boneco.L > Caixa.x && p.x < Caixa.Direita;
                if (emCima) p.x += Rumo * Forca * dt;
            }

            public override IReadOnlyList<Ret> Solidos() => new[] { Caixa };
        }

        // ---------------------------------------------------------------- Spring

        public class Mola : Armadilha
        {
            public float x, y, l = 50f, a = 14f, Forca = -980f;
            public float c;

            public Mola(float x, float y) { this.x = x; this.y = y; }
            public override void Zerar() { c = 0f; }

            public override void Passo(float dt, Estado g)
            {
                c = Mathf.Max(0f, c - dt * 5f);
                var p = g.Jogador;
                var emCima = p.x + Boneco.L > x + 3f && p.x < x + l - 3f;
                var pe = p.y + Boneco.A;
                if (!emCima || p.vy < 0f || pe < y - 6f || pe > y + 40f) return;

                p.y = y - Boneco.A;
                p.vy = Forca;
                p.NoChao = false;
                c = 1f;
            }
        }

        // ------------------------------------------------------------------ Saw

        public class Serra : Armadilha
        {
            public Vector2[] Trilho;
            public float Raio = 22f, Rapidez = 130f;
            public float x, y, giro;
            int _trecho, _rumo; float _f;

            public Serra(Vector2[] trilho) { Trilho = trilho; }

            public override void Zerar()
            {
                _trecho = 0; _rumo = 1; _f = 0f; giro = 0f;
                x = Trilho[0].x; y = Trilho[0].y;
            }

            public override void Passo(float dt, Estado g)
            {
                giro += dt * 9f;
                if (Trilho.Length < 2) return;

                var a = Trilho[_trecho];
                var iB = _trecho + _rumo;
                if (iB < 0 || iB >= Trilho.Length) { _rumo *= -1; return; }
                var b = Trilho[iB];

                var comp = Mathf.Max(1f, Vector2.Distance(a, b));
                _f += Rapidez * dt / comp;
                while (_f >= 1f)
                {
                    _f -= 1f;
                    _trecho += _rumo;
                    if (_trecho + _rumo < 0 || _trecho + _rumo >= Trilho.Length) _rumo *= -1;
                }

                var a2 = Trilho[_trecho];
                var i2 = _trecho + _rumo;
                var b2 = i2 >= 0 && i2 < Trilho.Length ? Trilho[i2] : a2;
                x = Mathf.Lerp(a2.x, b2.x, _f);
                y = Mathf.Lerp(a2.y, b2.y, _f);
            }

            public override IReadOnlyList<Ret> Mata() =>
                new[] { new Ret(x - Raio * 0.66f, y - Raio * 0.66f, Raio * 1.32f, Raio * 1.32f) };
        }

        // ---------------------------------------------------------------- Laser

        /// <summary>
        /// O raio que avisa antes de disparar: apagado, aviso, tiro. O aviso é o que
        /// torna a armadilha justa — sem ele o raio é uma moeda jogada no ar.
        /// </summary>
        public class Raio : Armadilha
        {
            public float x, y, Comprimento = 400f, Grossura = 10f;
            public bool EmPe;
            public float Periodo = 2.2f, Aviso = 0.55f, Tiro = 0.5f, Fase0;
            float t;

            public override void Zerar() { t = Fase0 * Periodo; }
            public override void Passo(float dt, Estado g) { t += dt; }

            public string Estado
            {
                get
                {
                    var u = Mathf.Repeat(t, Periodo);
                    if (u < Periodo - Aviso - Tiro) return "apagado";
                    return u < Periodo - Tiro ? "aviso" : "tiro";
                }
            }

            public Ret Feixe => EmPe
                ? new Ret(x - Grossura * 0.5f, y, Grossura, Comprimento)
                : new Ret(x, y - Grossura * 0.5f, Comprimento, Grossura);

            public override IReadOnlyList<Ret> Mata() =>
                Estado == "tiro" ? new[] { Feixe } : Nenhum;
        }

        // ------------------------------------------------------------ Teleporter

        public class Portal : Armadilha
        {
            public Ret A, B;
            public bool DoisSentidos = true;
            public float t, esfria;

            public Portal(float ax, float ay, float bx, float by, float l = 30f, float a = 48f)
            { A = new Ret(ax, ay, l, a); B = new Ret(bx, by, l, a); }

            public override void Zerar() { esfria = 0f; t = 0f; }

            public override void Passo(float dt, Estado g)
            {
                t += dt;
                esfria = Mathf.Max(0f, esfria - dt);
                if (esfria > 0f) return;

                var p = g.Jogador;
                if (p.Caixa.Toca(A)) Levar(p, B);
                else if (DoisSentidos && p.Caixa.Toca(B)) Levar(p, A);
            }

            void Levar(Boneco p, Ret para)
            {
                p.x = para.x + para.l * 0.5f - Boneco.L * 0.5f;
                p.y = para.Base - Boneco.A;
                p.vx = 0f;
                esfria = 0.45f;
            }
        }

        // ------------------------------------------------------------ Button/Gate

        public class Botao : Armadilha
        {
            public float x, y, l = 44f, a = 10f;
            public string Chave;
            public bool SoEnquantoPisa;
            public bool Apertado; public float afunda;

            public Botao(float x, float y, string chave) { this.x = x; this.y = y; Chave = chave; }
            public override void Zerar() { Apertado = false; afunda = 0f; }

            public override void Passo(float dt, Estado g)
            {
                var area = new Ret(x, y - 8f, l, a + 12f);
                var em = g.Jogador.Caixa.Toca(area);
                if (SoEnquantoPisa) Apertado = em;
                else if (em) Apertado = true;

                g.Chaves[Chave] = Apertado;
                afunda = Mathf.Clamp01(afunda + (Apertado ? 1f : -1f) * dt * 8f);
            }
        }

        public class PortaoDeChave : Armadilha
        {
            public Ret Caixa;
            public string Chave;
            public bool AoContrario;
            public float aberto;

            public PortaoDeChave(Ret caixa, string chave) { Caixa = caixa; Chave = chave; }
            public override void Zerar() { aberto = 0f; }

            public override void Passo(float dt, Estado g)
            {
                var quer = g.Chaves.TryGetValue(Chave, out var v) && v;
                if (AoContrario) quer = !quer;
                aberto = Mathf.Clamp01(aberto + (quer ? 1f : -1f) * dt * 4f);
            }

            public Ret Agora => new(Caixa.x, Caixa.y - aberto * (Caixa.a + 4f), Caixa.l, Caixa.a);
            public override IReadOnlyList<Ret> Solidos() => aberto > 0.92f ? Nenhum : new[] { Agora };
        }

        // ------------------------------------------------------- BlinkPlatform

        public class PlataformaPisca : Armadilha
        {
            public Ret Caixa;
            public float Periodo = 1.8f, Ligada = 0.5f, Fase0;
            float t;

            public PlataformaPisca(Ret caixa) { Caixa = caixa; }
            public override void Zerar() { t = Fase0 * Periodo; }
            public override void Passo(float dt, Estado g) { t += dt; }

            public bool Acesa => Mathf.Repeat(t, Periodo) < Periodo * Ligada;
            public override IReadOnlyList<Ret> Solidos() => Acesa ? new[] { Caixa } : Nenhum;
        }

        // ------------------------------------------------------------- Pendulum

        public class Pendulo : Armadilha
        {
            public float px, py, Comprimento = 380f, Amplitude = 0.85f, Rapidez = 1.6f, Raio = 18f, Fase0;
            float t;

            public Pendulo(float px, float py) { this.px = px; this.py = py; }
            public override void Zerar() { t = Fase0; }
            public override void Passo(float dt, Estado g) { t += dt; }

            public Vector2 Peso
            {
                get
                {
                    var a = Mathf.Sin(t * Rapidez) * Amplitude;
                    return new Vector2(px + Mathf.Sin(a) * Comprimento, py + Mathf.Cos(a) * Comprimento);
                }
            }

            public override IReadOnlyList<Ret> Mata()
            {
                var b = Peso;
                return new[] { new Ret(b.x - Raio * 0.66f, b.y - Raio * 0.66f, Raio * 1.32f, Raio * 1.32f) };
            }
        }

        // --------------------------------------------------------------- Turret

        public class Torreta : Armadilha
        {
            public float x, y, Periodo = 1.6f, Rapidez = 260f, Fase0, Raio = 7f;
            public int Rumo = -1;
            public readonly List<Vector2> Tiros = new();
            float t;

            public Torreta(float x, float y) { this.x = x; this.y = y; }
            public override void Zerar() { t = Fase0 * Periodo; Tiros.Clear(); }

            public override void Passo(float dt, Estado g)
            {
                t += dt;
                if (t >= Periodo) { t -= Periodo; Tiros.Add(new Vector2(x, y)); }

                for (var i = 0; i < Tiros.Count; i++)
                    Tiros[i] = new Vector2(Tiros[i].x + Rumo * Rapidez * dt, Tiros[i].y);

                Tiros.RemoveAll(s => s.x < -30f || s.x > Larg + 30f);
            }

            public override IReadOnlyList<Ret> Mata()
            {
                var fora = new List<Ret>(Tiros.Count);
                foreach (var s in Tiros) fora.Add(new Ret(s.x - Raio, s.y - Raio, Raio * 2f, Raio * 2f));
                return fora;
            }
        }

        // ------------------------------------------------------------ FakeDoor

        /// <summary>
        /// A porta que não é porta. Fica parada até alguém encostar e então cospe
        /// espinhos. É a piada mais cruel do original e a que mais ensina: a partir
        /// dela, nenhuma porta é de confiança até a lâmpada dizer.
        /// </summary>
        public class PortaFalsa : Armadilha
        {
            public float x, y, l = 38f, a = 64f;
            public string Rotulo;
            public bool Disparou; public float fora;

            public PortaFalsa(float x, float y, string rotulo = null) { this.x = x; this.y = y; Rotulo = rotulo; }
            public override void Zerar() { Disparou = false; fora = 0f; }

            public override void Passo(float dt, Estado g)
            {
                if (!Disparou && g.Jogador.Caixa.Toca(new Ret(x - 2f, y, l + 4f, a))) Disparou = true;
                if (Disparou) fora = Mathf.Clamp01(fora + 16f * dt);
            }

            public override IReadOnlyList<Ret> Mata() =>
                fora > 0.4f ? new[] { new Ret(x - 6f, y, l + 12f, a) } : Nenhum;
        }

        // --------------------------------------------------------------- Recado

        /// <summary>O recadinho na parede. Não faz nada, e é metade do jogo.</summary>
        public class Recado : Armadilha
        {
            public float x, y;
            public string Texto;
            public float Angulo;
            public int Tamanho = 16;

            public Recado(float x, float y, string texto) { this.x = x; this.y = y; Texto = texto; }
        }

        // ----------------------------------------------------------------- Door

        /// <summary>
        /// A saída. Recebe uma LISTA de posições e foge para a seguinte quando o
        /// jogador chega perto — a última não foge mais.
        /// </summary>
        public class Saida
        {
            public readonly Vector2[] Posicoes;
            public float Fuga = 110f;
            public const float L = 38f;
            public const float A = 64f;
            public int i;

            public Saida(Vector2[] posicoes) { Posicoes = posicoes; }
            public void Zerar() { i = 0; }
            public Vector2 Onde => Posicoes[Mathf.Min(i, Posicoes.Length - 1)];
            public Ret Caixa => new(Onde.x, Onde.y, L, A);

            /// <summary>Devolve true quando fugiu neste quadro.</summary>
            public bool Passo(Boneco p)
            {
                if (i >= Posicoes.Length - 1) return false;

                var dx = p.x + Boneco.L * 0.5f - (Onde.x + L * 0.5f);
                var dy = p.y + Boneco.A * 0.5f - (Onde.y + A * 0.5f);
                if (Mathf.Sqrt(dx * dx + dy * dy) >= Fuga) return false;

                i++;
                return true;
            }
        }
    }
}
