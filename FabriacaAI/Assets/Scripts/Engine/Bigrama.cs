using System.Collections.Generic;
using System.Linq;

namespace FabricaDeIA.Engine
{
    /// <summary>Uma continuação possível e quantos risquinhos ela levou.</summary>
    public readonly struct Continuacao
    {
        public readonly string Para;
        public readonly int Vezes;

        public Continuacao(string para, int vezes)
        {
            Para = para;
            Vezes = vezes;
        }
    }

    /// <summary>
    /// A máquina de risquinhos: um modelo de linguagem feito só de contagem.
    ///
    /// Para cada palavra, conta quantas vezes cada outra apareceu logo depois.
    /// Gerar texto é sortear pela contagem. É historicamente honesto — foi assim
    /// que se fez modelo de linguagem antes das redes — e dá ao aluno uma IA
    /// funcionando já na segunda etapa da aula.
    ///
    /// Ter uma IA de pé tão cedo muda todo o resto do percurso: cada etapa
    /// seguinte passa a responder "por que contar não basta" em vez de empilhar
    /// conceito solto no ar.
    ///
    /// É também o que sustenta a bancada 3: a tabela é quase toda vazia, e é
    /// essa esparsidade que justifica a rede neural mais adiante.
    /// </summary>
    public class Bigrama
    {
        public const string Inicio = "·início·";
        public const string Fim = "·fim·";

        /// <summary>de → (para → vezes)</summary>
        readonly Dictionary<string, Dictionary<string, int>> _tabela = new();
        readonly SortedSet<string> _palavras = new(System.StringComparer.Ordinal);

        public int TotalPares { get; private set; }

        /// <summary>Quantas casinhas da tabela têm ao menos um risquinho.</summary>
        public int CasasPreenchidas { get; private set; }

        List<string> _listaDePalavras;
        Dictionary<string, int> _indiceDePalavra;

        /// <summary>
        /// O vocabulário em ordem. A lista é construída uma vez e reaproveitada —
        /// a primeira versão devolvia <c>ToList()</c> a cada acesso, e a bancada 3
        /// (que percorre o vocabulário inteiro para desenhar a tabela) chamava
        /// isso centenas de vezes.
        /// </summary>
        public IReadOnlyList<string> Palavras => _listaDePalavras ??= _palavras.ToList();

        /// <summary>A posição de uma palavra no vocabulário, ou -1.</summary>
        public int IndiceDe(string palavra)
        {
            if (_indiceDePalavra == null)
            {
                _indiceDePalavra = new Dictionary<string, int>();
                for (var i = 0; i < Palavras.Count; i++) _indiceDePalavra[Palavras[i]] = i;
            }
            return _indiceDePalavra.TryGetValue(palavra, out var i2) ? i2 : -1;
        }

        /// <summary>
        /// O tamanho da tabela se ela fosse completa. Cresce com o QUADRADO do
        /// vocabulário, e é esse número — sempre absurdo ao lado do anterior —
        /// que a bancada 3 põe na cara do aluno.
        /// </summary>
        public long CasasTotais => (long)_palavras.Count * _palavras.Count;

        public Bigrama(IEnumerable<string> frases)
        {
            _palavras.Add(Inicio);
            _palavras.Add(Fim);

            foreach (var frase in frases)
            {
                var palavras = new List<string> { Inicio };
                palavras.AddRange(frase.Trim().Split(' ').Where(p => p.Length > 0));
                palavras.Add(Fim);

                foreach (var p in palavras) _palavras.Add(p);

                for (var i = 0; i < palavras.Count - 1; i++)
                {
                    var de = palavras[i];
                    var para = palavras[i + 1];

                    if (!_tabela.TryGetValue(de, out var linha))
                    {
                        linha = new Dictionary<string, int>();
                        _tabela[de] = linha;
                    }
                    linha[para] = linha.GetValueOrDefault(para) + 1;
                    TotalPares++;
                }
            }

            CasasPreenchidas = _tabela.Values.Sum(l => l.Count);
        }

        public bool Conhece(string palavra) => _tabela.ContainsKey(palavra);

        /// <summary>Quantos risquinhos tem a casinha (de, para). Zero se vazia.</summary>
        public int Risquinhos(string de, string para) =>
            _tabela.TryGetValue(de, out var linha) && linha.TryGetValue(para, out var n) ? n : 0;

        /// <summary>
        /// As palavras com mais continuações diferentes, das mais movimentadas
        /// para as menos.
        ///
        /// A bancada 3 mostra ao aluno justamente a região MAIS cheia da tabela —
        /// e é o que torna a lição inescapável: se até a parte movimentada está
        /// quase toda vazia, imagine o resto.
        /// </summary>
        public IEnumerable<string> MaisMovimentadas() =>
            _tabela.OrderByDescending(par => par.Value.Count)
                   .Select(par => par.Key)
                   .Where(p => p != Inicio);

        /// <summary>As palavras que mais aparecem como continuação.</summary>
        public IEnumerable<string> MaisRecebidas()
        {
            var quantas = new Dictionary<string, int>();
            foreach (var linha in _tabela.Values)
            {
                foreach (var par in linha)
                    quantas[par.Key] = quantas.GetValueOrDefault(par.Key) + par.Value;
            }
            return quantas.OrderByDescending(p => p.Value)
                          .Select(p => p.Key)
                          .Where(p => p != Fim);
        }

        /// <summary>As continuações de uma palavra, da mais frequente para a menos.</summary>
        public List<Continuacao> Continuacoes(string de)
        {
            if (!_tabela.TryGetValue(de, out var linha)) return new List<Continuacao>();
            return linha
                .Select(par => new Continuacao(par.Key, par.Value))
                .OrderByDescending(c => c.Vezes)
                .ThenBy(c => c.Para, System.StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Sorteia a próxima palavra proporcionalmente aos risquinhos.
        ///
        /// Sortear pela contagem, e não pegar sempre a mais frequente, é o que
        /// faz a máquina escrever frases diferentes a cada vez. Com o argmax ela
        /// repetiria a mesma frase para sempre e o aluno concluiria, com razão,
        /// que aquilo é uma gravação.
        /// </summary>
        static string Sortear(List<Continuacao> opcoes, Mulberry32 sorteio)
        {
            var total = opcoes.Sum(o => o.Vezes);
            var r = sorteio.Proximo() * total;
            foreach (var o in opcoes)
            {
                r -= o.Vezes;
                if (r <= 0f) return o.Para;
            }
            return opcoes[opcoes.Count - 1].Para;
        }

        /// <summary>Gera uma frase do zero. Devolve lista vazia se travar de cara.</summary>
        public List<string> Gerar(Mulberry32 sorteio, int maxPalavras = 16)
        {
            var saida = new List<string>();
            var atual = Inicio;

            for (var i = 0; i < maxPalavras; i++)
            {
                var opcoes = Continuacoes(atual);
                if (opcoes.Count == 0) break;

                var escolhida = Sortear(opcoes, sorteio);
                if (escolhida == Fim) break;
                saida.Add(escolhida);
                atual = escolhida;
            }
            return saida;
        }

        /// <summary>
        /// Continua uma frase que o aluno digitou.
        ///
        /// <paramref name="ultimaVista"/> é falso quando a última palavra nunca
        /// apareceu no texto — é o momento exato da bancada 3 em que a máquina
        /// emudece, e o aluno descobre sozinho o buraco da tabela.
        /// </summary>
        public List<string> Continuar(string inicio, Mulberry32 sorteio,
                                      out bool ultimaVista, int maxPalavras = 10)
        {
            var dadas = inicio.Trim().Split(' ').Where(p => p.Length > 0).ToList();
            var ultima = dadas.Count > 0 ? dadas[^1] : Inicio;
            ultimaVista = Conhece(ultima);

            var saida = new List<string>();
            if (!ultimaVista) return saida;

            var atual = ultima;
            for (var i = 0; i < maxPalavras; i++)
            {
                var opcoes = Continuacoes(atual);
                if (opcoes.Count == 0) break;

                var escolhida = Sortear(opcoes, sorteio);
                if (escolhida == Fim) break;
                saida.Add(escolhida);
                atual = escolhida;
            }
            return saida;
        }

        // ------------------------------------------- o canto da bancada 3

        /// <summary>
        /// Escolhe as linhas e as colunas do "canto movimentado" da bancada 3.
        ///
        /// Mora no motor, e não na bancada, porque a conferência de conteúdo
        /// precisa medir EXATAMENTE a grade que o aluno vai ver. Enquanto as duas
        /// tinham cópias do algoritmo, a medição atestava um jogo e o aluno jogava
        /// outro — e foi assim que uma grade quase invencível passou por boa.
        ///
        /// O critério mudou depois de medir a versão anterior. Ela sorteava cinco
        /// colunas entre as continuações das linhas, sem olhar QUANTAS linhas cada
        /// coluna servia: dava 23% de casinhas cheias, meia linha sem par nenhum,
        /// e — o número que condena — 2,8 acertos esperados em 12 cliques para uma
        /// meta de 3. Ou seja, o aluno mediano perdia. Pior: com 23% de casinhas
        /// cheias, quatro cliques seguidos no vazio acontecem em 35% das partidas,
        /// e quem cai nisso conclui que a bancada está quebrada.
        ///
        /// Agora as colunas são escolhidas para COBRIR as linhas: uma garantida
        /// para cada linha, e o resto preenchido pelas que servem a mais linhas de
        /// uma vez. Medido no corpus da aula: 40% de casinhas cheias, nenhuma
        /// linha sem par, 4 acertos esperados em 10 cliques.
        /// </summary>
        public static (List<string> de, List<string> para) Canto(
            Bigrama arquivo, int linhas, int colunas, Mulberry32 sorteio)
        {
            // SEM PALAVRA DE LIGAÇÃO NAS LINHAS — o conserto que fez a rodada 1
            // voltar a ser dedutível.
            //
            // `MaisMovimentadas` devolve as palavras com mais continuações
            // distintas, e em qualquer corpus essas são as gramaticais: aqui, "a"
            // com 71 continuações, "o" com 43, "na" com 24. Uma linha "a" não dá
            // o que deduzir — "a lição" está cheia, "a matemática" está vazia, e
            // as duas são português perfeito. O aluno usava a intuição CERTA e
            // levava "ninguém escreveu isso" na cara; metade das grades vinha com
            // duas ou mais linhas assim.
            //
            // Medido em 20 mil grades, tirar as ligações não custa densidade —
            // AUMENTA, porque as palavras de conteúdo mais movimentadas ("turma"
            // com 47, "aluno" com 26) continuam muito conectadas e a cobertura de
            // colunas trabalha melhor com linhas que significam alguma coisa:
            //
            //                          antes    depois
            //   casinhas cheias         40%      46%
            //   vazias em linha de ligação   41%       0%
            //   linha com sua continuação nº1  35%      49%
            //   grade sem nenhuma dessas       14%     6,9%
            //
            // Por isso não há compensação a fazer: nem mais cliques, nem grade
            // maior, nem meta menor. A alavanca era a escolha das linhas.
            var de = Sortear(arquivo.MaisMovimentadas()
                                    .Where(p => !Corpus.EhLigacao(p))
                                    .Take(16).ToList(), linhas, sorteio);
            var para = new List<string>();

            // Uma coluna garantida por linha. É isto que acaba com a linha morta —
            // a que o aluno lê inteira, deduz com capricho e não tem o que achar.
            foreach (var d in de)
            {
                var fortes = arquivo.Continuacoes(d)
                                    .Where(c => c.Para != Fim && c.Para != Inicio)
                                    .Take(6)
                                    .Select(c => c.Para)
                                    .Where(b => !para.Contains(b) && !de.Contains(b))
                                    .ToList();
                if (fortes.Count == 0) continue;
                para.Add(fortes[(int)(sorteio.Proximo() * fortes.Count)]);
            }

            // O resto: as continuações que servem a MAIS linhas de uma vez. Uma
            // coluna que continua três das quatro linhas enche três casinhas.
            var candidatas = de
                .SelectMany(d => arquivo.Continuacoes(d)
                                        .Where(c => c.Para != Fim && c.Para != Inicio)
                                        .Select(c => c.Para))
                .Distinct()
                .Where(b => !para.Contains(b) && !de.Contains(b))
                .OrderByDescending(b => de.Count(d => arquivo.Risquinhos(d, b) > 0))
                .ToList();

            foreach (var b in candidatas)
            {
                if (para.Count >= colunas) break;
                para.Add(b);
            }

            return (de, Embaralhar(para, sorteio));
        }

        static List<string> Sortear(List<string> fonte, int quantas, Mulberry32 sorteio) =>
            Embaralhar(new List<string>(fonte), sorteio)
                .Take(quantas < fonte.Count ? quantas : fonte.Count).ToList();

        static List<string> Embaralhar(List<string> lista, Mulberry32 sorteio)
        {
            for (var i = lista.Count - 1; i > 0; i--)
            {
                var j = (int)(sorteio.Proximo() * (i + 1));
                (lista[i], lista[j]) = (lista[j], lista[i]);
            }
            return lista;
        }
    }
}
