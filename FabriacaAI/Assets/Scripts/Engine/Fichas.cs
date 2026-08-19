using System.Collections.Generic;
using System.Linq;

namespace FabricaDeIA.Engine
{
    /// <summary>
    /// A mesa de corte: um punhado de palavras partidas em pedaços, e a operação
    /// de emendar dois pedaços vizinhos em todas as palavras de uma vez.
    ///
    /// É o algoritmo BPE — Byte Pair Encoding — que é como quase todo modelo de
    /// linguagem de verdade corta texto hoje. Começa com letras soltas e vai
    /// juntando o par vizinho mais frequente. Depois de alguns milhares de
    /// junções, "professora" virou dois ou três pedaços em vez de dez letras.
    ///
    /// A implementação é honesta: as junções acontecem de verdade, a contagem de
    /// fichas é a contagem real, e o vocabulário é o conjunto real de pedaços. O
    /// aluno está rodando BPE com a mão.
    /// </summary>
    public class Oficina
    {
        readonly string[] _palavras;
        readonly List<(string a, string b)> _fusoes = new();

        /// <summary>Cada palavra como lista de pedaços, no estado atual.</summary>
        public List<List<string>> Cortes { get; private set; }

        public IReadOnlyList<(string a, string b)> Fusoes => _fusoes;

        public Oficina(IEnumerable<string> palavras)
        {
            _palavras = palavras.ToArray();
            Recomeçar();
        }

        void Recomeçar()
        {
            Cortes = _palavras
                .Select(p => p.Select(c => c.ToString()).ToList())
                .ToList();
        }

        /// <summary>Quantas fichas o texto todo ocupa agora.</summary>
        public int TotalFichas => Cortes.Sum(c => c.Count);

        /// <summary>Quantos pedaços diferentes a costureira precisa guardar.</summary>
        public int Vocabulario => Cortes.SelectMany(c => c).Distinct().Count();

        /// <summary>
        /// Os pares vizinhos que existem agora, e quantas vezes cada um aparece.
        /// Ordenado do mais frequente ao menos — quem chama decide se mostra a
        /// contagem ao aluno.
        /// </summary>
        public List<(string a, string b, int vezes)> Pares()
        {
            var contagem = new Dictionary<(string, string), int>();
            foreach (var palavra in Cortes)
            {
                for (var i = 0; i < palavra.Count - 1; i++)
                {
                    var chave = (palavra[i], palavra[i + 1]);
                    contagem[chave] = contagem.GetValueOrDefault(chave) + 1;
                }
            }
            return contagem
                .Select(p => (p.Key.Item1, p.Key.Item2, p.Value))
                .OrderByDescending(p => p.Item3)
                .ThenBy(p => p.Item1 + p.Item2)
                .ToList();
        }

        /// <summary>
        /// Emenda o par em TODAS as palavras. Emendar num lugar só não seria
        /// tokenização: o corte tem que valer para o texto inteiro, senão a
        /// mesma palavra sairia partida de dois jeitos diferentes.
        /// </summary>
        public void Fundir(string a, string b)
        {
            _fusoes.Add((a, b));
            AplicarFusao(a, b);
        }

        void AplicarFusao(string a, string b)
        {
            foreach (var palavra in Cortes)
            {
                for (var i = 0; i < palavra.Count - 1; i++)
                {
                    if (palavra[i] != a || palavra[i + 1] != b) continue;
                    palavra[i] = a + b;
                    palavra.RemoveAt(i + 1);
                }
            }
        }

        /// <summary>
        /// Desfaz a última junção.
        ///
        /// Refaz tudo do zero em vez de tentar separar o pedaço emendado. Separar
        /// exigiria saber ONDE cada emenda foi aplicada, e é onde nasce o bug de
        /// desfazer que deixa uma palavra partida errado. Replicar cinco junções
        /// sobre cinco palavras custa microssegundos.
        /// </summary>
        public void Desfazer()
        {
            if (_fusoes.Count == 0) return;
            _fusoes.RemoveAt(_fusoes.Count - 1);

            var historico = new List<(string a, string b)>(_fusoes);
            _fusoes.Clear();
            Recomeçar();
            foreach (var (a, b) in historico) Fundir(a, b);
        }

        /// <summary>
        /// Roda BPE guloso por N junções e devolve quantas fichas sobram.
        ///
        /// É o alvo do quebra-cabeça: se o algoritmo consegue chegar a esse
        /// número, o aluno também consegue — a meta nunca é impossível. E é
        /// honesto chamar de meta o que a máquina faria.
        /// </summary>
        public static int MetaGulosa(IEnumerable<string> palavras, int fusoes)
        {
            var oficina = new Oficina(palavras);
            for (var i = 0; i < fusoes; i++)
            {
                var pares = oficina.Pares();
                if (pares.Count == 0) break;
                oficina.Fundir(pares[0].a, pares[0].b);
            }
            return oficina.TotalFichas;
        }
    }

    /// <summary>Uma rodada da bancada 4.</summary>
    public class Rodada4
    {
        public readonly string[] Palavras;
        public readonly int Fusoes;
        public readonly int Meta;

        public Rodada4(string[] palavras, int fusoes, int meta)
        {
            Palavras = palavras;
            Fusoes = fusoes;
            Meta = meta;
        }
    }

    /// <summary>
    /// Monta as rodadas da mesa de corte, tiradas do vocabulário real.
    ///
    /// A escolha das palavras é o que faz o quebra-cabeça ter graça: precisa
    /// haver estrutura repetida para encontrar. Palavras sorteadas ao acaso
    /// dariam junções que economizam uma ficha cada, e a decisão viraria
    /// indiferente. Então cada rodada leva um grupo que compartilha terminação —
    /// "guardou, chamou, ganhou" — mais algumas soltas para não ficar óbvio.
    /// </summary>
    public static class Cortes
    {
        public const int PorAula = 3;

        static readonly (int palavras, int fusoes)[] Rodadas =
        {
            (4, 4),
            (5, 6),
            (6, 8)
        };

        /// <summary>
        /// Quantas fichas a mais que o algoritmo guloso ainda contam como vitória.
        ///
        /// A meta era o resultado guloso puro, e isso pedia ao aluno que EMPATASSE
        /// com o algoritmo — escolhendo, a cada jogada, a emenda que aparece mais
        /// vezes, sem ver contagem nenhuma na tela. Uma emenda subótima em oito e
        /// a rodada estava perdida, ainda que ele tivesse entendido a ideia por
        /// completo. Era o lugar do jogo que mais exigia perfeição sem avisar.
        ///
        /// A folga cresce com a rodada porque o número de decisões cresce: em
        /// quatro fusões um deslize é escolha ruim, em oito é estatística.
        /// </summary>
        static readonly int[] Folga = { 2, 2, 3 };

        public static List<Rodada4> Sortear(int semente)
        {
            var sorteio = new Mulberry32((uint)semente);

            // Só palavras de tamanho médio: com três letras não há o que emendar,
            // e com doze a fila de fichas não cabe na tela.
            var vocabulario = Corpus.Vocabulario
                                    .Where(p => p.Length >= 5 && p.Length <= 9)
                                    .OrderBy(p => p, System.StringComparer.Ordinal)
                                    .ToList();

            // Grupos por terminação de três letras: é a estrutura que o aluno vai
            // descobrir e emendar.
            var familias = vocabulario
                .GroupBy(p => p.Substring(p.Length - 3))
                .Where(g => g.Count() >= 3)
                .Select(g => g.ToList())
                .ToList();

            var lista = new List<Rodada4>();
            var usadas = new HashSet<string>();

            for (var rodada = 0; rodada < Rodadas.Length; rodada++)
            {
                var (quantas, fusoes) = Rodadas[rodada];
                var palavras = new List<string>();

                if (familias.Count > 0)
                {
                    var familia = familias[(int)(sorteio.Proximo() * familias.Count)];
                    foreach (var p in Embaralhar(familia, sorteio).Take(3))
                        if (usadas.Add(p)) palavras.Add(p);
                }

                foreach (var p in Embaralhar(vocabulario, sorteio))
                {
                    if (palavras.Count >= quantas) break;
                    if (usadas.Add(p)) palavras.Add(p);
                }

                var alvo = palavras.ToArray();
                lista.Add(new Rodada4(alvo, fusoes,
                                      Oficina.MetaGulosa(alvo, fusoes) + Folga[rodada]));
            }
            return lista;
        }

        static List<string> Embaralhar(List<string> fonte, Mulberry32 sorteio)
        {
            var copia = new List<string>(fonte);
            for (var i = copia.Count - 1; i > 0; i--)
            {
                var j = (int)(sorteio.Proximo() * (i + 1));
                (copia[i], copia[j]) = (copia[j], copia[i]);
            }
            return copia;
        }
    }
}
