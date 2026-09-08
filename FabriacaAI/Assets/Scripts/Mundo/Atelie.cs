using System.Collections.Generic;
using UnityEngine;

namespace FabricaDeIA.Mundo
{
    /// <summary>Uma bancada no salão: qual etapa ela guarda e onde fica.</summary>
    public readonly struct Posto
    {
        public readonly string Etapa;
        /// <summary>Posição na ordem da aula, de 0 a 11.</summary>
        public readonly int Ordem;
        public readonly int Tx;
        public readonly int Ty;

        public Posto(string etapa, int ordem, int tx, int ty)
        {
            Etapa = etapa;
            Ordem = ordem;
            Tx = tx;
            Ty = ty;
        }

        public Vector2 Centro => Atelie.CentroDoTile(Tx, Ty);
    }

    /// <summary>
    /// A planta do ateliê.
    ///
    /// Um salão só, maior que a tela, com a câmera seguindo o jogador. A opção
    /// por salas separadas com portas foi descartada na versão web e continua
    /// descartada: numa aula de 90 minutos cada transição é tempo andando, e o
    /// professor perde a turma. Com tudo à vista, o aluno mede a empreitada no
    /// primeiro olhar.
    ///
    /// Legenda:
    ///   #  parede            .  piso              P  canteiro (bloqueia)
    ///   S  claraboia         @  início do jogador  ~  caixote (bloqueia)
    ///   1..9 a b c  as doze bancadas, na ordem das etapas
    ///   d  o balcão do certificado, que não é uma das doze
    ///
    /// O mapa é o mesmo da versão web (<c>src/game/mundo.ts</c>) — a turma que
    /// jogou o protótipo já sabe onde fica a costureira, e mudar a planta sem
    /// motivo joga fora esse conhecimento.
    /// </summary>
    public static class Atelie
    {
        static readonly string[] Planta =
        {
            "########################################",
            "#......................................#",
            "#..P.......1..........2..........3.....#",
            "#......................................#",
            "#....S.........S..........S........S...#",
            "#......................................#",
            "#..........4..........5..........6.....#",
            "#......................................#",
            "#..~...................................#",
            "#.......P.......................P......#",
            "#.....d.............@..................#",
            "#......................................#",
            "#..........7..........8..........9.....#",
            "#......................................#",
            "#....S.........S..........S........S...#",
            "#.....................................~#",
            "#..P.......a..........b................#",
            "#......................................#",
            "########################################"
        };

        /// <summary>
        /// A ordem das estações no mapa, e é a POSIÇÃO NESTA STRING que dá o
        /// número da etapa — o caractere da planta é só um crachá.
        ///
        /// O "d" no fim é o balcão do certificado: entra como posto para o aluno
        /// poder chegar nele e conversar, mas não conta como bancada em lugar
        /// nenhum — quem conta bancada percorre e1..e11 explicitamente.
        ///
        /// O "c" SAIU JUNTO COM A BANCADA DAS FICHAS, e saiu do fim da fila em vez
        /// do meio. Tirar o "4" deixaria um buraco no meio da grade de três
        /// colunas; tirando o último, a grade fica 3-3-3-2 e continua parecendo um
        /// salão arrumado. Quem mudou de móvel foi todo mundo da quarta em diante,
        /// e isso a renumeração da arte já resolveu.
        /// </summary>
        const string Codigos = "123456789abd";

        public static readonly int LarguraTiles = Planta[0].Length;
        public static readonly int AlturaTiles = Planta.Length;

        public static readonly IReadOnlyList<Posto> Postos;
        public static readonly IReadOnlyList<Vector2Int> Claraboias;
        public static readonly IReadOnlyList<Vector2Int> Canteiros;
        public static readonly IReadOnlyList<Vector2Int> Caixotes;
        public static readonly Vector2Int Inicio;

        static Atelie()
        {
            var postos = new List<Posto>();
            var claraboias = new List<Vector2Int>();
            var canteiros = new List<Vector2Int>();
            var caixotes = new List<Vector2Int>();
            var inicio = new Vector2Int(20, 10);

            for (var ty = 0; ty < AlturaTiles; ty++)
            {
                for (var tx = 0; tx < Planta[ty].Length; tx++)
                {
                    var c = Planta[ty][tx];
                    var ordem = Codigos.IndexOf(c);
                    if (ordem >= 0) postos.Add(new Posto($"e{ordem + 1}", ordem, tx, ty));
                    else if (c == 'S') claraboias.Add(new Vector2Int(tx, ty));
                    else if (c == 'P') canteiros.Add(new Vector2Int(tx, ty));
                    else if (c == '~') caixotes.Add(new Vector2Int(tx, ty));
                    else if (c == '@') inicio = new Vector2Int(tx, ty);
                }
            }

            postos.Sort((a, b) => a.Ordem.CompareTo(b.Ordem));
            Postos = postos;
            Claraboias = claraboias;
            Canteiros = canteiros;
            Caixotes = caixotes;
            Inicio = inicio;
        }

        public static char Em(int tx, int ty)
        {
            if (tx < 0 || ty < 0 || ty >= AlturaTiles || tx >= LarguraTiles) return '#';
            return Planta[ty][tx];
        }

        /// <summary>Tiles que o jogador não atravessa.</summary>
        public static bool Bloqueado(int tx, int ty)
        {
            var c = Em(tx, ty);
            return c == '#' || c == 'P' || c == '~' || Codigos.IndexOf(c) >= 0;
        }

        public static bool EhPiso(int tx, int ty)
        {
            var c = Em(tx, ty);
            return c != '#';
        }

        /// <summary>
        /// O centro de um tile em unidades de mundo.
        ///
        /// A planta cresce para baixo e o mundo do Unity cresce para cima, daí
        /// o Y negativo. Toda conversão passa por aqui — espalhar esse sinal de
        /// menos pelo código é receita de personagem andando invertido.
        /// </summary>
        public static Vector2 CentroDoTile(int tx, int ty) => new(tx + 0.5f, -(ty + 0.5f));

        public static Vector2Int TileDaPosicao(Vector2 mundo) =>
            new(Mathf.FloorToInt(mundo.x), Mathf.FloorToInt(-mundo.y));

        /// <summary>Os limites do salão, para prender a câmera dentro dele.</summary>
        public static Rect Limites => new(0f, -AlturaTiles, LarguraTiles, AlturaTiles);
    }
}
