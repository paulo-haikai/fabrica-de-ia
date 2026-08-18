using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FabricaDeIA.Engine
{
    /// <summary>
    /// Quem andou com quem: a associação entre duas palavras que aparecem na
    /// mesma frase.
    ///
    /// Serve a bancada 10, e a razão de não ser bigrama é o assunto da bancada.
    /// Bigrama olha um passo para trás: só sabe o que vem colado. Atenção é
    /// justamente a capacidade de olhar LONGE — em "a professora escreveu a
    /// matéria na ___", a palavra que decide a resposta é "professora", cinco
    /// posições atrás, e não "na", que está colada e não diz nada.
    ///
    /// A medida é informação mútua pontual, na forma mais simples: quantas vezes
    /// as duas apareceram juntas, comparado com quantas vezes cada uma apareceu
    /// sozinha. Palavra que anda com todo mundo — "a", "de", "na" — tem
    /// associação baixa com tudo, e é isso que faz o holofote apontado para ela
    /// não render nada. O aluno descobre isso apontando.
    /// </summary>
    public class Companhias
    {
        readonly Dictionary<string, int> _quantas = new();
        readonly Dictionary<(string, string), int> _juntas = new();
        readonly int _frases;

        public Companhias(IEnumerable<string> frases)
        {
            foreach (var frase in frases)
            {
                _frases++;
                var palavras = frase.Split(' ').Where(p => p.Length > 0).Distinct().ToList();

                foreach (var p in palavras) _quantas[p] = _quantas.GetValueOrDefault(p) + 1;

                for (var i = 0; i < palavras.Count; i++)
                {
                    for (var j = 0; j < palavras.Count; j++)
                    {
                        if (i == j) continue;
                        var chave = (palavras[i], palavras[j]);
                        _juntas[chave] = _juntas.GetValueOrDefault(chave) + 1;
                    }
                }
            }
        }

        public int Frequencia(string palavra) => _quantas.GetValueOrDefault(palavra);

        /// <summary>
        /// A força da associação entre duas palavras. Zero se nunca apareceram
        /// juntas; alta quando aparecem juntas mais do que o acaso explicaria.
        /// </summary>
        public float Associacao(string a, string b)
        {
            var juntas = _juntas.GetValueOrDefault((a, b));
            if (juntas == 0) return 0f;

            var pa = (float)Frequencia(a) / _frases;
            var pb = (float)Frequencia(b) / _frases;
            if (pa <= 0f || pb <= 0f) return 0f;

            var pab = (float)juntas / _frases;
            // Logaritmo da razão: é a informação mútua pontual. Positiva quando
            // as duas se atraem, próxima de zero quando é coincidência.
            return Mathf.Max(0f, Mathf.Log(pab / (pa * pb)));
        }

        /// <summary>
        /// O palpite da máquina para a lacuna, usando SÓ as palavras iluminadas.
        ///
        /// É a soma das associações de cada candidato com cada holofote aceso.
        /// Nenhum holofote aceso significa nenhuma informação — e a máquina
        /// responde qualquer coisa, que é exatamente o que se quer mostrar.
        /// </summary>
        public string Palpite(IEnumerable<string> acesas, IEnumerable<string> candidatos)
        {
            var lista = acesas.ToList();
            string melhor = null;
            var melhorNota = 0f;

            foreach (var candidato in candidatos)
            {
                var nota = lista.Sum(a => Associacao(a, candidato));
                if (nota <= melhorNota) continue;
                melhor = candidato;
                melhorNota = nota;
            }

            // Sem nenhuma associação positiva, ela NÃO TEM PALPITE — e devolver
            // null é o único jeito honesto de dizer isso.
            //
            // A primeira versão começava o melhor em menos infinito e devolvia o
            // primeiro candidato da lista quando tudo empatava em zero. Resultado:
            // acender só o artigo "a", que não informa nada, acertava por sorte —
            // e destruía exatamente a lição da bancada, que é que palavra de
            // ligação não carrega informação.
            return melhorNota > 0f ? melhor : null;
        }

        public float Nota(IEnumerable<string> acesas, string candidato) =>
            acesas.Sum(a => Associacao(a, candidato));
    }

    /// <summary>Uma rodada da bancada 10.</summary>
    public class Rodada10
    {
        public readonly string[] Antes;
        public readonly string Resposta;
        public readonly string[] Candidatos;
        public readonly int Lampadas;

        public Rodada10(string[] antes, string resposta, string[] candidatos, int lampadas)
        {
            Antes = antes;
            Resposta = resposta;
            Candidatos = candidatos;
            Lampadas = lampadas;
        }
    }

    /// <summary>
    /// Monta as rodadas dos holofotes.
    ///
    /// Cada rodada nasce de uma frase real do corpus, sem a última palavra. Só
    /// entra frase para a qual EXISTE um conjunto de holofotes, dentro do
    /// orçamento, que faz a máquina acertar — a verificação é por força bruta
    /// sobre os subconjuntos, o mesmo cuidado das outras bancadas geradas: nunca
    /// entregar ao aluno um nível sem solução.
    /// </summary>
    public static class Holofotes
    {
        public const int PorAula = 3;

        static readonly (int lampadas, int candidatos)[] Rodadas = { (2, 4), (2, 5), (3, 6) };

        public static List<Rodada10> Sortear(Companhias companhias, int semente)
        {
            var sorteio = new Mulberry32((uint)semente);

            var frases = Corpus.Frases
                .Select(f => f.Split(' ').Where(p => p.Length > 0).ToArray())
                .Where(p => p.Length >= 5 && p.Length <= 9)
                .ToList();

            var comuns = Corpus.Vocabulario
                .OrderByDescending(companhias.Frequencia)
                .Take(60)
                .ToList();

            var lista = new List<Rodada10>();
            var usadas = new HashSet<string>();

            foreach (var (lampadas, quantosCandidatos) in Rodadas)
            {
                for (var tentativa = 0; tentativa < 400 && lista.Count < Rodadas.Length; tentativa++)
                {
                    var palavras = frases[(int)(sorteio.Proximo() * frases.Count)];
                    var resposta = palavras[^1];
                    if (!usadas.Add(resposta)) continue;

                    // Resposta que é palavra de ligação não tem o que deduzir.
                    if (Corpus.EhLigacao(resposta)) continue;

                    var antes = palavras.Take(palavras.Length - 1).ToArray();

                    // A resposta não pode estar no contexto. "a lição de CASA
                    // ficou pronta em ___" com resposta "casa" se resolve lendo,
                    // não deduzindo — e ainda por cima faz o holofote errado
                    // parecer certo.
                    if (antes.Contains(resposta)) continue;

                    // Precisa haver ao menos uma palavra de conteúdo para acender.
                    if (antes.Count(p => !Corpus.EhLigacao(p)) < 2) continue;

                    var candidatos = new List<string> { resposta };
                    foreach (var c in Embaralhar(comuns, sorteio))
                    {
                        if (candidatos.Count >= quantosCandidatos) break;
                        if (c != resposta && !antes.Contains(c)) candidatos.Add(c);
                    }
                    if (candidatos.Count < quantosCandidatos) continue;

                    var baralho = Embaralhar(candidatos, sorteio).ToArray();
                    var rodada = new Rodada10(antes, resposta, baralho, lampadas);

                    var solucao = Solucao(companhias, rodada);
                    if (solucao == null) continue;

                    // Se a solução mínima usa só palavras de ligação, o nível
                    // ensina o contrário do que deveria — que apontar para o
                    // artigo resolve. Fora.
                    if (solucao.All(i => Corpus.EhLigacao(antes[i]))) continue;

                    lista.Add(rodada);
                    break;
                }
            }
            return lista;
        }

        /// <summary>
        /// Um conjunto de holofotes que faz a máquina acertar, ou null se não
        /// houver. Busca exaustiva sobre os subconjuntos do tamanho permitido.
        /// </summary>
        public static List<int> Solucao(Companhias companhias, Rodada10 rodada)
        {
            var n = rodada.Antes.Length;
            for (var mascara = 1; mascara < (1 << n); mascara++)
            {
                var escolhidos = Enumerable.Range(0, n)
                                           .Where(i => (mascara & (1 << i)) != 0)
                                           .ToList();
                if (escolhidos.Count > rodada.Lampadas) continue;

                var acesas = escolhidos.Select(i => rodada.Antes[i]).ToList();
                if (companhias.Palpite(acesas, rodada.Candidatos) == rodada.Resposta)
                    return escolhidos;
            }
            return null;
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
