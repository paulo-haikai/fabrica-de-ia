using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FabricaDeIA.Engine
{
    /// <summary>
    /// A vizinhança de cada palavra — e a semelhança que sai dela.
    ///
    /// A ideia é a mais importante de toda a aula, e é simples de enunciar: uma
    /// palavra é descrita pelas companhias que ela mantém. Se "lousa" e "quadro"
    /// aparecem depois das mesmas palavras e antes das mesmas palavras, então
    /// para a máquina elas são parecidas — mesmo que ela não saiba o que
    /// nenhuma das duas significa.
    ///
    /// Aqui isso é feito do jeito mais direto que existe: cada palavra ganha um
    /// vetor com a contagem dos vizinhos (à esquerda e à direita, marcados
    /// separadamente, porque "a casa" e "casa a" não são a mesma informação), e a
    /// semelhança é o cosseno entre dois vetores.
    ///
    /// Não é word2vec nem GloVe — é o avô deles, a matriz de coocorrência. E é
    /// honesto chamar isso de embedding: os vetores são reais, a semelhança é
    /// real, e o mapa que a bancada 5 mostra sai destes números.
    /// </summary>
    public class Vizinhancas
    {
        /// <summary>palavra → (vizinho marcado por lado) → vezes</summary>
        readonly Dictionary<string, Dictionary<string, int>> _contexto = new();
        readonly Dictionary<string, float> _normas = new();

        public IReadOnlyCollection<string> Palavras => _contexto.Keys;

        public Vizinhancas(IEnumerable<string> frases)
        {
            foreach (var frase in frases)
            {
                var palavras = frase.Split(' ').Where(p => p.Length > 0).ToArray();
                for (var i = 0; i < palavras.Length; i++)
                {
                    var vetor = Vetor(palavras[i]);
                    // O lado importa: "<" é vizinho da esquerda, ">" da direita.
                    if (i > 0) Somar(vetor, "<" + palavras[i - 1]);
                    if (i < palavras.Length - 1) Somar(vetor, ">" + palavras[i + 1]);
                }
            }

            foreach (var par in _contexto)
                _normas[par.Key] = Mathf.Sqrt(par.Value.Values.Sum(v => (float)v * v));
        }

        Dictionary<string, int> Vetor(string palavra)
        {
            if (_contexto.TryGetValue(palavra, out var v)) return v;
            v = new Dictionary<string, int>();
            _contexto[palavra] = v;
            return v;
        }

        static void Somar(Dictionary<string, int> vetor, string chave) =>
            vetor[chave] = vetor.GetValueOrDefault(chave) + 1;

        /// <summary>Quantos vizinhos diferentes a palavra teve. Serve de "peso".</summary>
        public int Vizinhos(string palavra) =>
            _contexto.TryGetValue(palavra, out var v) ? v.Count : 0;

        /// <summary>
        /// Cosseno entre as vizinhanças de duas palavras: 1 é "aparecem sempre
        /// nos mesmos lugares", 0 é "nunca se cruzam".
        /// </summary>
        public float Semelhanca(string a, string b)
        {
            if (a == b) return 1f;
            if (!_contexto.TryGetValue(a, out var va) || !_contexto.TryGetValue(b, out var vb))
                return 0f;

            var normaA = _normas[a];
            var normaB = _normas[b];
            if (normaA <= 0f || normaB <= 0f) return 0f;

            // Percorre o vetor menor: o produto interno só depende das chaves em
            // comum, e a maioria das palavras não compartilha quase nenhuma.
            if (va.Count > vb.Count) (va, vb) = (vb, va);

            var produto = 0f;
            foreach (var par in va)
                if (vb.TryGetValue(par.Key, out var outro)) produto += (float)par.Value * outro;

            return produto / (normaA * normaB);
        }

        /// <summary>
        /// Os vizinhos mais frequentes de um lado da palavra.
        ///
        /// É a evidência crua que a bancada 5 mostra ao aluno, e mostrar isso
        /// mudou o jogo dela de adivinhação para dedução. Sem a vizinhança à
        /// vista, ele agrupa por SIGNIFICADO e a máquina agrupou por LUGAR — e
        /// quando os dois discordam, como em "professor" e "professora", o
        /// quebra-cabeça fica injusto. Com a vizinhança à vista, ele resolve pela
        /// mesma informação que a máquina usou.
        /// </summary>
        public List<string> PrincipaisVizinhos(string palavra, bool aEsquerda, int quantos)
        {
            if (!_contexto.TryGetValue(palavra, out var vetor)) return new List<string>();
            var marca = aEsquerda ? '<' : '>';

            return vetor
                .Where(par => par.Key[0] == marca)
                .OrderByDescending(par => par.Value)
                .ThenBy(par => par.Key, System.StringComparer.Ordinal)
                .Take(quantos)
                .Select(par => par.Key.Substring(1))
                .ToList();
        }

        /// <summary>As palavras mais parecidas com esta, da mais para a menos.</summary>
        public List<string> MaisParecidas(string palavra, int quantas,
                                          System.Func<string, bool> filtro = null)
        {
            return _contexto.Keys
                .Where(p => p != palavra && (filtro == null || filtro(p)))
                .OrderByDescending(p => Semelhanca(palavra, p))
                .Take(quantas)
                .ToList();
        }
    }

    /// <summary>Um grupo de palavras que a máquina considera parecidas.</summary>
    public class Grupo
    {
        public readonly string Semente;
        public readonly List<string> Palavras;
        /// <summary>Semelhança média dentro do grupo. Quanto maior, mais coeso.</summary>
        public readonly float Coesao;

        public Grupo(string semente, List<string> palavras, float coesao)
        {
            Semente = semente;
            Palavras = palavras;
            Coesao = coesao;
        }
    }

    /// <summary>
    /// Monta as rodadas do mapa: três grupos de quatro palavras que a máquina
    /// acha parecidas, para o aluno tentar separar.
    ///
    /// Os grupos NÃO são temáticos ("coisas de escola", "verbos"). São
    /// distribucionais: palavras que ocupam o mesmo tipo de lugar na frase. É
    /// isso que a máquina enxerga, e a surpresa da bancada é justamente
    /// descobrir que o agrupamento dela faz sentido sem ela entender nada.
    /// </summary>
    public static class Mapas
    {
        public const int PorAula = 3;
        public const int PorGrupo = 4;
        public const int Grupos = 3;

        public static List<List<Grupo>> Sortear(Vizinhancas mapa, int semente, int rodadas = PorAula)
        {
            var sorteio = new Mulberry32((uint)semente);

            // Só palavras com vizinhança rica o bastante para a semelhança
            // significar algo. Palavra vista duas vezes tem vetor de dois
            // números, e o cosseno dela com qualquer coisa é ruído.
            var candidatas = mapa.Palavras
                                 .Where(p => mapa.Vizinhos(p) >= 6)
                                 .OrderBy(p => p, System.StringComparer.Ordinal)
                                 .ToList();

            var lista = new List<List<Grupo>>();
            var sementesUsadas = new HashSet<string>();

            for (var r = 0; r < rodadas; r++)
            {
                var melhor = (List<Grupo>)null;
                var melhorNota = float.NegativeInfinity;

                // Tenta várias trincas e fica com a mais SEPARÁVEL: grupos coesos
                // por dentro e distantes entre si. Uma trinca embaralhada não tem
                // resposta certa, e o aluno erra sem entender por quê.
                for (var tentativa = 0; tentativa < 300; tentativa++)
                {
                    var trinca = Trinca(mapa, candidatas, sementesUsadas, sorteio);
                    if (trinca == null) continue;

                    // Rejeita de saída trincas com dois grupos que se confundem.
                    // A conferência mostrou trincas do tipo "professora aluna
                    // turma conta | professor aluno trabalho grupo": as duas são
                    // grupos de gente, e nem uma pessoa saberia dizer qual é qual.
                    if (Confundivel(mapa, trinca)) continue;

                    var nota = Nota(mapa, trinca);
                    if (nota <= melhorNota) continue;
                    melhor = trinca;
                    melhorNota = nota;
                }

                if (melhor == null) continue;
                foreach (var g in melhor) sementesUsadas.Add(g.Semente);
                lista.Add(melhor);
            }
            return lista;
        }

        static List<Grupo> Trinca(Vizinhancas mapa, List<string> candidatas,
                                  HashSet<string> proibidas, Mulberry32 sorteio)
        {
            var grupos = new List<Grupo>();
            var usadas = new HashSet<string>(proibidas);

            for (var g = 0; g < Grupos; g++)
            {
                string semente = null;
                for (var tentativa = 0; tentativa < 40 && semente == null; tentativa++)
                {
                    var escolhida = candidatas[(int)(sorteio.Proximo() * candidatas.Count)];
                    if (!usadas.Contains(escolhida)) semente = escolhida;
                }
                if (semente == null) return null;

                var vizinhas = mapa.MaisParecidas(semente, PorGrupo - 1,
                                                  p => !usadas.Contains(p) && candidatas.Contains(p));
                if (vizinhas.Count < PorGrupo - 1) return null;

                var palavras = new List<string> { semente };
                palavras.AddRange(vizinhas);
                foreach (var p in palavras) usadas.Add(p);

                grupos.Add(new Grupo(semente, palavras, CoesaoDe(mapa, palavras)));
            }
            return grupos;
        }

        /// <summary>
        /// Verdadeiro se algum par de grupos diferentes tem palavras parecidas
        /// demais — o que tornaria o quebra-cabeça ambíguo em vez de difícil.
        /// </summary>
        static bool Confundivel(Vizinhancas mapa, List<Grupo> trinca)
        {
            // 0,45 e não 0,62: com 0,62 passavam trincas como "professora aluna
            // turma conta | professor aluno trabalho grupo", em que a semelhança
            // entre "professor" e "professora" fica logo abaixo do corte. O
            // limiar tem que ser mais severo que a intuição sugere, porque
            // palavras que uma PESSOA confunde nem sempre têm cosseno alto.
            const float limite = 0.45f;

            for (var i = 0; i < trinca.Count; i++)
            {
                for (var j = i + 1; j < trinca.Count; j++)
                {
                    foreach (var a in trinca[i].Palavras)
                    {
                        foreach (var b in trinca[j].Palavras)
                        {
                            if (mapa.Semelhanca(a, b) > limite) return true;
                        }
                    }
                }
            }
            return false;
        }

        static float CoesaoDe(Vizinhancas mapa, List<string> palavras)
        {
            var soma = 0f;
            var pares = 0;
            for (var i = 0; i < palavras.Count; i++)
            {
                for (var j = i + 1; j < palavras.Count; j++)
                {
                    soma += mapa.Semelhanca(palavras[i], palavras[j]);
                    pares++;
                }
            }
            return pares == 0 ? 0f : soma / pares;
        }

        /// <summary>
        /// Nota de uma trinca: coesão dentro dos grupos menos a semelhança entre
        /// grupos. É o que separa um quebra-cabeça com resposta de um sopão.
        /// </summary>
        static float Nota(Vizinhancas mapa, List<Grupo> trinca)
        {
            var dentro = trinca.Average(g => g.Coesao);

            var fora = 0f;
            var pares = 0;
            for (var i = 0; i < trinca.Count; i++)
            {
                for (var j = i + 1; j < trinca.Count; j++)
                {
                    foreach (var a in trinca[i].Palavras)
                    {
                        foreach (var b in trinca[j].Palavras)
                        {
                            fora += mapa.Semelhanca(a, b);
                            pares++;
                        }
                    }
                }
            }
            if (pares > 0) fora /= pares;

            return dentro - fora * 1.5f;
        }
    }
}
