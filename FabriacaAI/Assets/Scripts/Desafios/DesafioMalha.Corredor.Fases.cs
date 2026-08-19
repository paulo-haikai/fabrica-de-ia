using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// AS TRINTA SALAS — port do FableDevil, de Leonxlnx.
    ///
    /// Origem: <https://github.com/Leonxlnx/level-devil>, licença MIT. O aviso de
    /// copyright e o texto da licença estão em TERCEIROS.md, na raiz do repositório,
    /// como a licença exige de quem copia porção substancial.
    ///
    /// O TRAÇADO É O DELE, coordenada por coordenada. As armadilhas, os tempos, os
    /// períodos e as posições da porta vieram do original sem ajuste — é isso que faz
    /// o jogo ser o jogo. Fase de rage platformer se calibra na mão, uma a uma, e
    /// reescrevê-las "no espírito" produz o que já se tentou duas vezes aqui: um
    /// corredor em que se anda até a porta e pega.
    ///
    /// O QUE É DAQUI: a arte, os nomes das salas e os recados na parede. O recado é
    /// metade da piada, e piada não se traduz — se reescreve.
    ///
    /// A ORDEM É SORTEADA por aula, menos as quatro primeiras: elas ensinam a andar,
    /// a pular, que o chão mente e que espinho dói. Sortear essas seria começar a
    /// aula pela sala 27.
    ///
    /// AS TRINTA SÃO O ACERVO; UMA AULA JOGA DEZ. As quatro que ensinam mais seis
    /// sorteadas — ver DesafioMalha.Corredor.cs. Trinta salas de rage platformer
    /// levam mais que os noventa minutos da bancada inteira, e a bancada não veio
    /// ensinar a pular: veio abrir a malha no fim.
    /// </summary>
    public partial class DesafioMalha
    {
        // ---------------------------------------------------- atalhos de montagem

        static ChaoQueCede Cede(Ret caixa, Ret gatilho, float tremor = 0.18f) =>
            new(caixa, gatilho, tremor);

        static EspinhosQueSobem Sobe(float x, float y, float l, Ret? gatilho,
                                     float atraso = 0f, float periodo = 0f,
                                     float fase = 0f, float segura = 0.8f) =>
            new(x, y, l, gatilho)
            { Atraso = atraso, Periodo = periodo, Fase0 = fase, Segura = segura };

        static BlocoQueCai Cai(Ret casa, Ret gatilho, float tremor = 0.12f,
                               float pisoY = Chao) =>
            new(casa, gatilho) { Tremor = tremor, PisoY = pisoY };

        static Esmagador Martelo(float x, float l, float topoY, float periodo = 0f,
                                 float fase = 0f, Ret? gatilho = null,
                                 float descida = 1500f) =>
            new(x, l)
            {
                TopoY = topoY, Periodo = periodo, Fase0 = fase,
                Gatilho = gatilho, VelDescida = descida
            };

        static PlataformaQueEsfarela Esfarela(Ret caixa, float atraso = 0.35f) =>
            new(caixa, atraso);

        static BuracoCorredico Buraco(float x0, float x1, float vao, float comeco,
                                      float rapidez, Ret gatilho) =>
            new(x0, x1) { Vao = vao, Comeco = comeco, Rapidez = rapidez, Gatilho = gatilho };

        static EspinhosFixos Fixos(float x, float y, float l, float tamanho = 26f) =>
            new(x, y, l) { Tamanho = tamanho };

        static ZonaInvertida AoContrario(Ret caixa) => new(caixa);

        static PlataformaMovel Movel(Ret caixa, float paraX = float.NaN,
                                     float paraY = float.NaN, float rapidez = 80f,
                                     float pausa = 0f, float fase = 0f)
        {
            var p = new PlataformaMovel(caixa) { Rapidez = rapidez, Pausa = pausa, Fase0 = fase };
            if (!float.IsNaN(paraX)) p.bx = paraX;
            if (!float.IsNaN(paraY)) p.by = paraY;
            return p;
        }

        static Esteira Correia(Ret caixa, int rumo, float forca) =>
            new(caixa) { Rumo = rumo, Forca = forca };

        static Mola Pula(float x, float y, float forca) => new(x, y) { Forca = forca };

        static Serra Lamina(Vector2 a, Vector2 b, float raio, float rapidez) =>
            new(new[] { a, b }) { Raio = raio, Rapidez = rapidez };

        static Raio Feixe(float x, float y, float comprimento, float periodo,
                          float aviso, float tiro, float fase = 0f) =>
            new()
            {
                x = x, y = y, Comprimento = comprimento, EmPe = true,
                Periodo = periodo, Aviso = aviso, Tiro = tiro, Fase0 = fase
            };

        static Portal Salto(float ax, float ay, float bx, float by) =>
            new(ax, ay, bx, by) { DoisSentidos = false };

        static PlataformaPisca Pisca(Ret caixa, float periodo, float ligada, float fase) =>
            new(caixa) { Periodo = periodo, Ligada = ligada, Fase0 = fase };

        static Pendulo Balanco(float px, float py, float comprimento, float amplitude,
                               float rapidez, float raio, float fase) =>
            new(px, py)
            {
                Comprimento = comprimento, Amplitude = amplitude,
                Rapidez = rapidez, Raio = raio, Fase0 = fase
            };

        static Torreta Canhao(float x, float y, float periodo, float rapidez, float fase = 0f) =>
            new(x, y) { Periodo = periodo, Rapidez = rapidez, Fase0 = fase, Rumo = -1 };

        static Botao Chave(float x, float y, string chave) => new(x, y, chave);
        static PortaoDeChave Grade(Ret caixa, string chave) => new(caixa, chave);
        static PortaFalsa Falsa(float x, float y, string rotulo) => new(x, y, rotulo);

        static Recado Nota(float x, float y, string texto, int tamanho = 16) =>
            new(x, y, texto) { Tamanho = tamanho };

        static Saida Porta(params Vector2[] onde) => new(onde);
        static Saida PortaFujona(float fuga, params Vector2[] onde) =>
            new(onde) { Fuga = fuga };
        static Vector2 Em(float x, float y) => new(x, y);

        public class Sala
        {
            public string Nome;
            public Vector2 Nasce;
            public System.Func<Saida> Saida;
            public System.Func<Ret[]> Solidos;
            public System.Func<Armadilha[]> Truques;
        }

        /// <summary>As salas, abertas para a conferência.</summary>
        public static Sala[] TodasAsSalas => Salas;

        static readonly Sala[] Salas =
        {
            // -------------------------------------------- as quatro que ensinam

            new()
            {
                Nome = "Nada para ver aqui",
                Nasce = Em(60f, 440f),
                Saida = () => Porta(Em(876f, 416f)),
                Solidos = () => new[] { Piso(0f, 400f), Piso(500f, 960f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Cede(R(400f, 480f, 100f, 60f), R(330f, 300f, 30f, 180f)),
                    Sobe(760f, 480f, 80f, R(708f, 330f, 26f, 150f), 0.06f),
                    Nota(210f, 430f, "é só andar até a porta :)")
                }
            },

            new()
            {
                Nome = "Questão de confiança",
                Nasce = Em(60f, 440f),
                Saida = () => Porta(Em(876f, 416f)),
                Solidos = () => new[] { Piso(0f, 200f), Piso(760f, 960f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Esfarela(R(270f, 408f, 92f, 16f), 0.32f),
                    Esfarela(R(430f, 360f, 92f, 16f), 0.32f),
                    Esfarela(R(590f, 408f, 92f, 16f), 0.18f),
                    Cai(R(440f, 40f, 70f, 42f), R(430f, 200f, 92f, 170f), 0.12f, 540f),
                    Sobe(764f, 480f, 70f, R(700f, 330f, 20f, 150f), 0.02f),
                    Nota(310f, 380f, "parecem firmes")
                }
            },

            new()
            {
                Nome = "Situação pontiaguda",
                Nasce = Em(50f, 440f),
                Saida = () => Porta(Em(880f, 416f)),
                Solidos = () => new[] { Piso(0f, 960f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Sobe(220f, 480f, 64f, null, 0f, 1.7f, 0.00f, 0.75f),
                    Sobe(330f, 480f, 64f, null, 0f, 1.7f, 0.28f, 0.75f),
                    Sobe(440f, 480f, 64f, null, 0f, 1.7f, 0.56f, 0.75f),
                    Sobe(550f, 480f, 64f, null, 0f, 1.7f, 0.84f, 0.75f),
                    Sobe(660f, 480f, 64f, null, 0f, 1.7f, 1.12f, 0.75f),
                    Sobe(790f, 480f, 76f, R(742f, 330f, 18f, 150f), 0.05f),
                    Nota(120f, 420f, "ache o compasso")
                }
            },

            new()
            {
                Nome = "O céu está caindo",
                Nasce = Em(55f, 440f),
                Saida = () => Porta(Em(876f, 416f)),
                Solidos = () => new[] { Piso(0f, 960f), Teto(40f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Cai(R(200f, 40f, 64f, 42f), R(180f, 200f, 104f, 280f)),
                    Cai(R(360f, 40f, 64f, 42f), R(340f, 200f, 104f, 280f)),
                    Cai(R(520f, 40f, 64f, 42f), R(500f, 200f, 104f, 280f)),
                    Cai(R(680f, 40f, 64f, 42f), R(660f, 200f, 104f, 280f)),
                    Cai(R(820f, 40f, 76f, 42f), R(770f, 200f, 40f, 280f), 0.04f),
                    Nota(120f, 100f, "olhe para cima.", 14)
                }
            },

            // ------------------------------------------- daqui em diante, sorteadas

            new()
            {
                Nome = "Subindo?",
                Nasce = Em(60f, 440f),
                Saida = () => Porta(Em(884f, 416f)),
                Solidos = () => new[] { Piso(0f, 300f), Piso(620f, 960f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Movel(R(300f, 452f, 100f, 16f), 520f, float.NaN, 95f, 0.5f),
                    Nota(160f, 430f, "sobe aí. é de graça :)"),
                    Sobe(806f, 480f, 70f, R(706f, 330f, 18f, 150f), 0.05f)
                }
            },

            new()
            {
                Nome = "Volta aqui!",
                Nasce = Em(60f, 440f),
                Saida = () => PortaFujona(105f, Em(870f, 416f), Em(470f, 416f),
                                          Em(120f, 416f), Em(856f, 288f)),
                Solidos = () => new[]
                {
                    Piso(0f, 960f), R(640f, 420f, 92f, 14f), R(800f, 352f, 160f, 16f),
                    ParedeE(), ParedeD()
                },
                Truques = () => new Armadilha[]
                {
                    Sobe(652f, 420f, 68f, R(640f, 320f, 92f, 100f), 0.45f),
                    Nota(760f, 250f, "ela só quer carinho")
                }
            },

            new()
            {
                Nome = "Dia de esteira",
                Nasce = Em(150f, 440f),
                Saida = () => Porta(Em(884f, 416f)),
                Solidos = () => new[] { Piso(120f, 960f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Correia(R(300f, 480f, 320f, 60f), -1, 165f),
                    Nota(450f, 430f, "continue andando →"),
                    Sobe(812f, 480f, 66f, R(720f, 330f, 16f, 150f), 0.04f)
                }
            },

            new()
            {
                Nome = "O chão não vai com a sua cara",
                Nasce = Em(55f, 440f),
                Saida = () => Porta(Em(880f, 416f)),
                Solidos = () => new[] { ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Buraco(0f, 960f, 96f, 760f, 150f, R(120f, 300f, 20f, 180f)),
                    Sobe(806f, 480f, 64f, R(756f, 330f, 16f, 150f), 0.03f),
                    Nota(420f, 420f, "o buraco é amigável")
                }
            },

            new()
            {
                Nome = "Boing",
                Nasce = Em(60f, 440f),
                Saida = () => Porta(Em(884f, 226f)),
                Solidos = () => new[]
                {
                    Piso(0f, 440f), R(480f, 290f, 480f, 16f), ParedeE(), ParedeD()
                },
                Truques = () => new Armadilha[]
                {
                    Pula(360f, 480f, -1220f),
                    Nota(160f, 430f, "hora do trampolim"),
                    Sobe(700f, 290f, 60f, R(580f, 200f, 18f, 90f), 0.3f)
                }
            },

            new()
            {
                Nome = "oãsufnoC",
                Nasce = Em(55f, 440f),
                Saida = () => Porta(Em(880f, 416f)),
                Solidos = () => new[]
                {
                    Piso(0f, 350f), Piso(430f, 540f), Piso(620f, 960f), ParedeE(), ParedeD()
                },
                Truques = () => new Armadilha[]
                {
                    AoContrario(R(280f, 0f, 420f, 480f)),
                    Fixos(355f, 540f, 70f, 40f),
                    Fixos(545f, 540f, 70f, 40f),
                    Sobe(700f, 480f, 64f, R(648f, 330f, 16f, 150f), 0.4f),
                    Nota(490f, 300f, "sétnoc oa", 18)
                }
            },

            new()
            {
                Nome = "Agora você vê",
                Nasce = Em(60f, 440f),
                Saida = () => Porta(Em(884f, 416f)),
                Solidos = () => new[] { Piso(0f, 250f), Piso(740f, 960f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Pisca(R(300f, 430f, 96f, 16f), 1.7f, 0.62f, 0.00f),
                    Pisca(R(444f, 400f, 96f, 16f), 1.7f, 0.62f, 0.34f),
                    Pisca(R(588f, 430f, 96f, 16f), 1.7f, 0.62f, 0.68f),
                    Nota(150f, 430f, "agora não vê"),
                    Sobe(806f, 480f, 66f, R(720f, 330f, 16f, 150f), 0.05f)
                }
            },

            new()
            {
                Nome = "Escolha uma porta",
                Nasce = Em(55f, 440f),
                Saida = () => Porta(Em(884f, 416f)),
                Solidos = () => new[] { Piso(0f, 960f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Falsa(380f, 416f, "é esta, pode confiar"),
                    Falsa(600f, 416f, "ou será esta?"),
                    Cai(R(800f, 40f, 70f, 42f), R(745f, 200f, 50f, 280f), 0.05f),
                    Sobe(700f, 480f, 70f, R(560f, 330f, 30f, 150f), 0.85f),
                    Nota(903f, 396f, "golpe", 13)
                }
            },

            new()
            {
                Nome = "Já vi essa serra",
                Nasce = Em(55f, 440f),
                Saida = () => Porta(Em(884f, 416f)),
                Solidos = () => new[] { Piso(0f, 960f), Teto(30f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Lamina(Em(300f, 444f), Em(640f, 444f), 24f, 165f),
                    Lamina(Em(520f, 90f), Em(520f, 430f), 22f, 185f),
                    Nota(150f, 430f, "perfeitamente seguro"),
                    Sobe(820f, 480f, 64f, R(740f, 330f, 16f, 150f), 0.04f)
                }
            },

            new()
            {
                Nome = "Terra plana",
                Nasce = Em(50f, 440f),
                Saida = () => Porta(Em(880f, 416f)),
                Solidos = () => new[] { Piso(0f, 960f), Teto(36f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Martelo(230f, 92f, 36f, 1.9f, 0.00f),
                    Martelo(450f, 92f, 36f, 1.9f, 0.95f),
                    Martelo(640f, 92f, 36f, 1.9f, 0.45f),
                    Martelo(806f, 100f, 36f, 0f, 0f, R(770f, 320f, 12f, 160f), 2100f),
                    Nota(340f, 110f, "aqui é tudo bem plano")
                }
            },

            new()
            {
                Nome = "Diga xis",
                Nasce = Em(55f, 440f),
                Saida = () => Porta(Em(884f, 416f)),
                Solidos = () => new[] { Piso(0f, 960f), Teto(30f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Feixe(300f, 30f, 418f, 2.2f, 0.55f, 0.5f, 0.0f),
                    Feixe(480f, 30f, 418f, 2.2f, 0.55f, 0.5f, 0.5f),
                    Feixe(660f, 30f, 418f, 2.2f, 0.55f, 0.5f, 1.0f),
                    Nota(150f, 430f, "não se mexa"),
                    Sobe(820f, 480f, 64f, R(740f, 330f, 16f, 150f), 0.04f)
                }
            },

            new()
            {
                Nome = "Cuidado com o vão",
                Nasce = Em(55f, 440f),
                Saida = () => Porta(Em(884f, 416f)),
                Solidos = () => new[] { Piso(0f, 360f), Piso(620f, 960f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Fixos(360f, 540f, 260f, 44f),
                    Salto(300f, 432f, 648f, 432f),
                    Nota(170f, 430f, "entre aí →"),
                    Sobe(806f, 480f, 66f, R(720f, 330f, 16f, 150f), 0.05f)
                }
            },

            new()
            {
                Nome = "Tique-taque",
                Nasce = Em(55f, 440f),
                Saida = () => Porta(Em(884f, 416f)),
                Solidos = () => new[] { Piso(0f, 960f), Teto(30f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Balanco(240f, 30f, 400f, 0.85f, 1.6f, 18f, 0.0f),
                    Balanco(470f, 30f, 400f, 0.85f, 1.6f, 18f, 1.1f),
                    Balanco(700f, 30f, 400f, 0.85f, 1.6f, 18f, 2.2f),
                    Nota(150f, 430f, "atenção ao balanço"),
                    Sobe(844f, 480f, 58f, R(764f, 330f, 14f, 150f), 0.04f)
                }
            },

            new()
            {
                Nome = "Lá vem",
                Nasce = Em(55f, 440f),
                Saida = () => Porta(Em(884f, 416f)),
                Solidos = () => new[] { Piso(0f, 960f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Canhao(942f, 430f, 1.3f, 300f, 0.0f),
                    Canhao(942f, 388f, 1.7f, 250f, 0.6f),
                    Nota(150f, 430f, "abaixe! (não dá)"),
                    Sobe(300f, 480f, 64f, null, 0f, 1.8f, 0f, 0.7f)
                }
            },

            new()
            {
                Nome = "Aperte para vencer",
                Nasce = Em(55f, 440f),
                Saida = () => Porta(Em(884f, 416f)),
                Solidos = () => new[] { Piso(0f, 960f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Chave(330f, 470f, "g"),
                    Grade(R(620f, 300f, 28f, 180f), "g"),
                    Falsa(720f, 416f, "por aqui!"),
                    Sobe(812f, 480f, 66f, R(740f, 330f, 16f, 150f), 0.05f),
                    Nota(352f, 440f, "aperte para abrir a grade")
                }
            },

            new()
            {
                Nome = "Elevador",
                Nasce = Em(55f, 440f),
                Saida = () => Porta(Em(884f, 416f)),
                Solidos = () => new[]
                {
                    Piso(0f, 300f), Piso(660f, 960f), R(450f, 330f, 120f, 16f),
                    ParedeE(), ParedeD()
                },
                Truques = () => new Armadilha[]
                {
                    Fixos(300f, 540f, 360f, 46f),
                    Movel(R(300f, 452f, 120f, 16f), float.NaN, 320f, 62f, 0.45f),
                    Movel(R(560f, 320f, 120f, 16f), float.NaN, 452f, 62f, 0.45f, 0.5f),
                    Nota(150f, 430f, "subindo ↑"),
                    Sobe(812f, 480f, 64f, R(740f, 330f, 16f, 150f), 0.05f)
                }
            },

            new()
            {
                Nome = "Correia desgovernada",
                Nasce = Em(80f, 440f),
                Saida = () => Porta(Em(884f, 416f)),
                Solidos = () => new[] { Piso(0f, 960f), Teto(30f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Correia(R(260f, 480f, 420f, 60f), 1, 150f),
                    Martelo(360f, 90f, 30f, 1.7f, 0.00f),
                    Martelo(520f, 90f, 30f, 1.7f, 0.85f),
                    Martelo(660f, 90f, 30f, 1.7f, 0.40f),
                    Nota(150f, 430f, "correia e martelos. divertido."),
                    Sobe(812f, 480f, 64f, R(740f, 330f, 16f, 150f), 0.04f)
                }
            },

            new()
            {
                Nome = "Febre de mola",
                Nasce = Em(60f, 440f),
                Saida = () => Porta(Em(884f, 416f)),
                Solidos = () => new[]
                {
                    Piso(0f, 300f), Piso(460f, 650f), Piso(790f, 960f), ParedeE(), ParedeD()
                },
                Truques = () => new Armadilha[]
                {
                    Fixos(300f, 540f, 160f, 44f),
                    Fixos(650f, 540f, 140f, 44f),
                    Pula(250f, 480f, -1080f),
                    Pula(600f, 480f, -1080f),
                    Nota(150f, 430f, "corra e quique →"),
                    Sobe(820f, 480f, 64f, R(742f, 330f, 16f, 150f), 0.05f)
                }
            },

            new()
            {
                Nome = "Achou",
                Nasce = Em(55f, 440f),
                Saida = () => Porta(Em(884f, 416f)),
                Solidos = () => new[] { Piso(0f, 260f), Piso(700f, 960f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Fixos(260f, 540f, 440f, 46f),
                    Pisca(R(320f, 420f, 100f, 16f), 1.6f, 0.6f, 0.0f),
                    Pisca(R(540f, 420f, 100f, 16f), 1.6f, 0.6f, 0.5f),
                    Lamina(Em(480f, 150f), Em(480f, 360f), 22f, 180f),
                    Nota(150f, 430f, "cronometre"),
                    Sobe(806f, 480f, 66f, R(720f, 330f, 16f, 150f), 0.05f)
                }
            },

            new()
            {
                Nome = "Fogo cruzado",
                Nasce = Em(55f, 440f),
                Saida = () => Porta(Em(884f, 416f)),
                Solidos = () => new[] { Piso(0f, 960f), Teto(30f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Canhao(942f, 430f, 1.2f, 300f),
                    Feixe(430f, 30f, 418f, 2.0f, 0.5f, 0.45f, 0.0f),
                    Feixe(620f, 30f, 418f, 2.0f, 0.5f, 0.45f, 0.5f),
                    Nota(150f, 430f, "fogo cruzado!"),
                    Sobe(280f, 480f, 64f, null, 0f, 1.7f, 0f, 0.7f)
                }
            },

            new()
            {
                Nome = "Salto de portal",
                Nasce = Em(55f, 440f),
                Saida = () => Porta(Em(884f, 196f)),
                Solidos = () => new[]
                {
                    Piso(0f, 300f), Piso(640f, 960f), R(760f, 260f, 200f, 16f),
                    ParedeE(), ParedeD()
                },
                Truques = () => new Armadilha[]
                {
                    Fixos(300f, 540f, 340f, 46f),
                    Movel(R(320f, 452f, 100f, 16f), 520f, float.NaN, 95f, 0.4f),
                    Salto(700f, 432f, 820f, 222f),
                    Nota(150f, 430f, "pegue carona, depois salte"),
                    Sobe(806f, 260f, 60f, R(720f, 180f, 16f, 80f), 0.05f)
                }
            },

            new()
            {
                Nome = "Inferno com compasso",
                Nasce = Em(55f, 440f),
                Saida = () => Porta(Em(884f, 416f)),
                Solidos = () => new[] { Piso(0f, 960f), Teto(30f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Sobe(240f, 480f, 64f, null, 0f, 1.5f, 0.0f, 0.7f),
                    Sobe(360f, 480f, 64f, null, 0f, 1.5f, 0.3f, 0.7f),
                    Sobe(480f, 480f, 64f, null, 0f, 1.5f, 0.6f, 0.7f),
                    Feixe(600f, 30f, 418f, 1.8f, 0.45f, 0.4f, 0.0f),
                    Feixe(720f, 30f, 418f, 1.8f, 0.45f, 0.4f, 0.9f),
                    Nota(150f, 430f, "sinta a batida"),
                    Sobe(844f, 480f, 58f, R(770f, 330f, 14f, 150f), 0.04f)
                }
            },

            new()
            {
                Nome = "Tudo junto",
                Nasce = Em(55f, 440f),
                Saida = () => Porta(Em(884f, 416f)),
                Solidos = () => new[]
                {
                    Piso(0f, 360f), Piso(540f, 960f), Teto(30f), ParedeE(), ParedeD()
                },
                Truques = () => new Armadilha[]
                {
                    Correia(R(120f, 480f, 200f, 60f), 1, 120f),
                    Fixos(360f, 540f, 180f, 44f),
                    Movel(R(360f, 452f, 100f, 16f), 450f, float.NaN, 80f, 0.4f),
                    Lamina(Em(660f, 150f), Em(660f, 360f), 20f, 170f),
                    Martelo(770f, 90f, 30f, 1.8f, 0.3f),
                    Nota(150f, 430f, "um pouco de cada coisa"),
                    Sobe(844f, 480f, 58f, R(800f, 330f, 14f, 150f), 0.04f)
                }
            },

            new()
            {
                Nome = "Não confie em ninguém",
                Nasce = Em(55f, 440f),
                Saida = () => Porta(Em(884f, 416f)),
                Solidos = () => new[] { Piso(0f, 620f), Piso(770f, 960f), ParedeE(), ParedeD() },
                Truques = () => new Armadilha[]
                {
                    Falsa(300f, 416f, "100% verdadeira"),
                    Falsa(520f, 416f, "confie em mim"),
                    Cede(R(620f, 480f, 150f, 60f), R(560f, 300f, 30f, 180f)),
                    Chave(700f, 470f, "x"),
                    Cai(R(820f, 40f, 70f, 42f), R(790f, 200f, 40f, 280f), 0.05f),
                    Nota(150f, 430f, "não confie em ninguém")
                }
            },

            new()
            {
                Nome = "A velha provação",
                Nasce = Em(45f, 440f),
                Saida = () => PortaFujona(70f, Em(884f, 416f), Em(70f, 200f)),
                Solidos = () => new[]
                {
                    Piso(0f, 230f), Piso(360f, 960f),
                    R(40f, 264f, 130f, 16f), R(205f, 336f, 90f, 14f), R(330f, 410f, 80f, 14f),
                    ParedeE(), ParedeD()
                },
                Truques = () => new Armadilha[]
                {
                    Cede(R(230f, 480f, 130f, 60f), R(170f, 280f, 24f, 200f)),
                    Cai(R(420f, 40f, 64f, 42f), R(400f, 200f, 104f, 280f)),
                    Buraco(500f, 790f, 88f, 740f, 135f, R(470f, 300f, 20f, 180f)),
                    Sobe(800f, 480f, 70f, R(750f, 330f, 20f, 150f), 0.05f),
                    Sobe(218f, 336f, 64f, R(205f, 240f, 90f, 96f), 0.5f),
                    Nota(600f, 430f, "quase lá :)"),
                    Nota(105f, 240f, "tá bom. você mereceu.", 13)
                }
            },

            new()
            {
                Nome = "A última",
                Nasce = Em(50f, 440f),
                Saida = () => PortaFujona(78f, Em(884f, 416f), Em(110f, 236f)),
                Solidos = () => new[]
                {
                    Piso(0f, 250f), Piso(700f, 960f), R(40f, 300f, 210f, 16f),
                    Teto(30f), ParedeE(), ParedeD()
                },
                Truques = () => new Armadilha[]
                {
                    Pula(150f, 480f, -1000f),
                    Movel(R(280f, 452f, 100f, 16f), 560f, float.NaN, 100f, 0.35f),
                    Lamina(Em(470f, 150f), Em(470f, 430f), 22f, 190f),
                    Feixe(610f, 30f, 418f, 1.9f, 0.45f, 0.4f),
                    Sobe(806f, 480f, 66f, R(720f, 330f, 16f, 150f), 0.05f),
                    Fixos(250f, 540f, 450f, 44f),
                    Nota(150f, 250f, "a última risada é dele")
                }
            }
        };
    }
}
