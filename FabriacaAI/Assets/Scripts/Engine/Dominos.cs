using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FabricaDeIA.Engine
{
    /// <summary>
    /// Uma peça de dominó: duas palavras que o arquivo viu juntas, e quantas
    /// vezes viu.
    /// </summary>
    public class Peca
    {
        public readonly string Esquerda;
        public readonly string Direita;
        public readonly int Risquinhos;

        public Peca(string esquerda, string direita, int risquinhos)
        {
            Esquerda = esquerda;
            Direita = direita;
            Risquinhos = risquinhos;
        }
    }

    /// <summary>Uma rodada: onde começar, onde chegar, e as peças na mão.</summary>
    public class Rodada2
    {
        public readonly string Inicio;
        public readonly string Alvo;
        public readonly List<Peca> Mao;
        public readonly int MaxPecas;
        /// <summary>A frase que a caminhada original produziu. Só para conferência.</summary>
        public readonly string CaminhoExemplo;

        public Rodada2(string inicio, string alvo, List<Peca> mao, int maxPecas, string exemplo)
        {
            Inicio = inicio;
            Alvo = alvo;
            Mao = mao;
            MaxPecas = maxPecas;
            CaminhoExemplo = exemplo;
        }
    }

    /// <summary>
    /// Monta as rodadas de dominó da bancada 2.
    ///
    /// O caminho de cada rodada é um TRECHO DE FRASE REAL do corpus — uma janela
    /// de palavras contíguas que alguém escreveu de verdade. As peças desse trecho
    /// vão para a mão, misturadas com engodos.
    ///
    /// A primeira versão fazia diferente e estava errada: sorteava uma palavra e
    /// andava N passos pelo grafo de bigramas, seguindo pares que o arquivo
    /// conhece. Todo par existia, então a solução era "válida" — e a frase saía
    /// absurda. Uma rodada de teste pedia para chegar em "ficou" e o caminho certo
    /// era "amanhã a aula de matemática ficou". O aluno montava aquilo, o jogo dava
    /// como correto, e a lição saía ao contrário: ele aprendia que contar pares
    /// basta para escrever, que é exatamente o oposto do que a aula quer mostrar.
    ///
    /// Tirar o caminho de uma frase escrita por uma pessoa resolve isso na raiz. E
    /// o absurdo continua no jogo, onde ele é útil: os ENGODOS são pares reais que
    /// encaixam e não são frase, então o aluno esbarra no limite do modelo por
    /// conta própria — mas agora esbarrar nele é o erro, e não a vitória.
    ///
    /// O que a mudança preserva:
    ///
    ///   · SOLUBILIDADE GARANTIDA. O caminho existe porque foi ele que gerou a
    ///     rodada. Não há risco de um nível impossível travar um aluno.
    ///   · VARIEDADE. São 259 frases com sete palavras ou mais, e a janela pode
    ///     começar em qualquer ponto: trinta alunos recebem trinta tabuleiros.
    ///   · HONESTIDADE. As peças são pares reais, com a contagem real. O aluno
    ///     está manipulando o modelo, não uma imitação dele.
    /// </summary>
    public static class Dominos
    {
        public const int PorAula = 3;

        /// <summary>Quantos passos cada rodada pede, do mais fácil ao mais difícil.</summary>
        static readonly int[] Passos = { 4, 5, 6 };

        public static List<Rodada2> Sortear(Bigrama arquivo, int semente)
        {
            var sorteio = new Mulberry32((uint)semente);
            var rodadas = new List<Rodada2>();

            // As três rodadas de uma sessão não podem abrir com a mesma
            // palavra: três correntes seguidas começando em "hoje a turma" fazem
            // o jogo parecer um jogo só, repetido.
            var aberturasUsadas = new HashSet<string>();

            foreach (var passos in Passos)
            {
                var rodada = Tentar(arquivo, passos, sorteio, aberturasUsadas);
                if (rodada == null) continue;
                aberturasUsadas.Add(rodada.Inicio);
                rodadas.Add(rodada);
            }
            return rodadas;
        }

        static Rodada2 Tentar(Bigrama arquivo, int passos, Mulberry32 sorteio,
                              HashSet<string> aberturasUsadas)
        {
            var janelas = Janelas(passos, aberturasUsadas);
            if (janelas.Count == 0) return null;

            var escolhida = janelas[(int)(sorteio.Proximo() * janelas.Count)];
            var inicio = escolhida[0];
            var alvo = escolhida[^1];

            // As peças do trecho, com a contagem REAL de cada par no arquivo. A
            // contagem é o que o aluno lê embaixo de cada dominó, e ela tem que ser
            // a do modelo — não a deste trecho.
            var caminho = new List<Peca>();
            for (var i = 0; i + 1 < escolhida.Count; i++)
                caminho.Add(new Peca(escolhida[i], escolhida[i + 1],
                                     arquivo.Risquinhos(escolhida[i], escolhida[i + 1])));

            var mao = new List<Peca>(caminho);
            AcrescentarEngodos(arquivo, caminho, mao, sorteio);
            Embaralhar(mao, sorteio);

            return new Rodada2(inicio, alvo, mao, caminho.Count,
                               string.Join(" ", escolhida));
        }

        /// <summary>
        /// Todas as janelas de <paramref name="passos"/> emendas que servem de
        /// rodada, tiradas das frases do corpus.
        ///
        /// As quatro condições existem cada uma por um motivo de jogo:
        ///
        ///   · SEM PALAVRA REPETIDA na janela. Corrente que volta à mesma palavra
        ///     deixa dois encaixes válidos no mesmo ponto, e aí "a ponta está em X"
        ///     deixa de identificar onde a corrente está.
        ///   · COMEÇO E ALVO COM CONTEÚDO. "comece em 'da' e chegue em 'de'" não é
        ///     enunciado de nada.
        ///   · ABERTURA INÉDITA na sessão. Três correntes seguidas começando na
        ///     mesma palavra fazem as três rodadas parecerem uma só.
        /// </summary>
        static List<List<string>> Janelas(int passos, HashSet<string> aberturasUsadas)
        {
            var tamanho = passos + 1;
            var janelas = new List<List<string>>();

            foreach (var frase in Corpus.Frases)
            {
                var palavras = frase.Trim().Split(' ');
                if (palavras.Length < tamanho) continue;

                for (var i = 0; i + tamanho <= palavras.Length; i++)
                {
                    var janela = new List<string>();
                    for (var j = 0; j < tamanho; j++) janela.Add(palavras[i + j]);

                    if (janela.Distinct().Count() != janela.Count) continue;
                    // A régua nova: a corrente que o aluno vai montar tem que ser
                    // uma ORAÇÃO, e não uma fatia qualquer de frase verdadeira.
                    // Ver Corpus.EhOracao para o que isso substituiu e por quê.
                    if (!Corpus.EhOracao(janela)) continue;
                    if (aberturasUsadas.Contains(janela[0])) continue;

                    janelas.Add(janela);
                }
            }
            return janelas;
        }

        /// <summary>
        /// Engodos: peças que ENCAIXAM mas não levam ao alvo.
        ///
        /// Um dominó cujo lado esquerdo não bate com nada nunca é tentador — o
        /// aluno vê que não serve e ignora. O engodo que ensina é o que encaixa
        /// perfeitamente no ponto em que ele está e desvia a corrente. Daí eles
        /// serem tirados das OUTRAS continuações das mesmas palavras do caminho:
        /// são exatamente as escolhas que a máquina também poderia ter feito.
        /// </summary>
        static void AcrescentarEngodos(Bigrama arquivo, List<Peca> caminho,
                                       List<Peca> mao, Mulberry32 sorteio)
        {
            var noCaminho = new HashSet<string>(caminho.Select(p => p.Direita));
            noCaminho.Add(caminho[0].Esquerda);

            foreach (var passo in caminho)
            {
                var alternativas = arquivo.Continuacoes(passo.Esquerda)
                                          .Where(c => c.Para != passo.Direita
                                                   && c.Para != Bigrama.Fim
                                                   && !noCaminho.Contains(c.Para))
                                          .ToList();
                if (alternativas.Count == 0) continue;

                var escolhida = alternativas[(int)(sorteio.Proximo() * alternativas.Count)];
                mao.Add(new Peca(passo.Esquerda, escolhida.Para, escolhida.Vezes));
            }
        }

        static void Embaralhar(List<Peca> lista, Mulberry32 sorteio)
        {
            for (var i = lista.Count - 1; i > 0; i--)
            {
                var j = (int)(sorteio.Proximo() * (i + 1));
                (lista[i], lista[j]) = (lista[j], lista[i]);
            }
        }
    }
}
