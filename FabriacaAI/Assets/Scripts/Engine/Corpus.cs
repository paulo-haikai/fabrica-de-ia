using System;
using System.Collections.Generic;
using UnityEngine;

namespace FabricaDeIA.Engine
{
    /// <summary>
    /// O texto que a máquina come e o vocabulário que ele produz.
    ///
    /// É o mesmo corpus escolar da versão web, palavra por palavra. Isso não é
    /// nostalgia: o corpus foi escrito para uma turma de ensino médio, revisado
    /// contra as respostas do desafio 1 e usado para treinar os modelos das
    /// etapas seguintes. Trocá-lo por um texto qualquer quebraria as três
    /// coisas de uma vez.
    /// </summary>
    public static class Corpus
    {
        [Serializable] class Arquivo
        {
            public string nome;
            public string descricao;
            public string[] frases;
        }

        static string[] _frases;
        static HashSet<string> _vocabulario;

        public static IReadOnlyList<string> Frases
        {
            get { Garantir(); return _frases; }
        }

        /// <summary>
        /// As palavras que o corpus conhece. É o dicionário que valida os
        /// palpites do aluno — aceitar palavra de fora seria aceitar palpite
        /// que a máquina jamais poderia dar, e a comparação entre os dois é
        /// justamente o que a etapa 1 quer mostrar.
        /// </summary>
        public static HashSet<string> Vocabulario
        {
            get { Garantir(); return _vocabulario; }
        }

        /// <summary>
        /// Palavras de ligação: artigo, preposição, conjunção.
        ///
        /// Não têm significado próprio e aparecem em quase toda frase, o que as
        /// torna informação quase nula — e por isso três bancadas precisam
        /// reconhecê-las:
        ///
        ///   · a 2 evita emendar três seguidas, senão a corrente de dominó sai
        ///     "antes do fim da mochila";
        ///   · a 10 não pode aceitar que acender o artigo resolva o quebra-cabeça,
        ///     porque a lição dela é justamente que artigo não informa nada;
        ///   · a 11 não usa palavra de ligação como alvo, porque "chegue em 'da'"
        ///     não é meta que alguém queira perseguir.
        ///
        /// A lista estava escondida dentro da bancada 2 e foi promovida para cá
        /// quando a segunda bancada precisou dela. Duas cópias divergiriam.
        /// </summary>
        static readonly HashSet<string> Ligacoes = new()
        {
            "a", "o", "as", "os", "de", "da", "do", "das", "dos", "e", "em",
            "na", "no", "nas", "nos", "um", "uma", "para", "por", "com", "que",
            "ao", "à", "se", "mais", "muito", "já", "também",
            "sobre", "entre", "até", "desde", "contra", "sem", "sob", "após"
        };

        public static bool EhLigacao(string palavra) => Ligacoes.Contains(palavra);

        /// <summary>
        /// Artigos, que abrem sujeito. São subconjunto das ligações e precisam ser
        /// distinguidos delas: uma oração em ordem direta COMEÇA em artigo
        /// ("o professor escreveu…"), enquanto começar em preposição ("da lousa…")
        /// é sinal de que a janela pegou o meio de outra oração.
        /// </summary>
        static readonly HashSet<string> Artigos = new()
        {
            "o", "a", "os", "as", "um", "uma", "uns", "umas"
        };

        /// <summary>
        /// Substantivos que terminam como verbo. Sem esta lista, "museu" e "troféu"
        /// passam por verbo pela terminação -eu e uma janela sem verbo nenhum é
        /// dada como oração.
        /// </summary>
        static readonly HashSet<string> FalsosVerbos = new()
        {
            "museu", "troféu", "chapéu", "céu", "véu", "escarcéu", "judeu", "ateu"
        };

        static readonly HashSet<string> VerbosIrregulares = new()
        {
            "foi", "foram", "era", "eram", "é", "fez", "fizeram", "teve", "tem",
            "têm", "veio", "vieram", "deu", "deram", "pôs", "trouxe", "disse",
            "quis", "soube", "viu", "leu", "vai", "vão", "estava", "estavam",
            "havia", "fica", "ficam", "devolve", "pode", "pôde"
        };

        /// <summary>
        /// Verdadeiro se a palavra é verbo neste corpus.
        ///
        /// Não é um etiquetador de português: é uma regra sob medida para 326
        /// frases escolares, todas no passado ou no presente simples. Terminação
        /// resolve a maioria (-ou, -eu, -iu, -aram, -eram, -iram), uma lista curta
        /// resolve os irregulares, e outra lista curta tira os substantivos que
        /// terminam igual. Medido no corpus: erra em 2 das 326 frases.
        ///
        /// Existe para a bancada 2 poder exigir que a corrente forme uma ORAÇÃO, e
        /// não um pedaço qualquer de frase verdadeira.
        /// </summary>
        public static bool EhVerbo(string palavra)
        {
            if (string.IsNullOrEmpty(palavra)) return false;
            if (FalsosVerbos.Contains(palavra)) return false;
            if (VerbosIrregulares.Contains(palavra)) return true;

            return palavra.EndsWith("ou") || palavra.EndsWith("eu") ||
                   palavra.EndsWith("iu") || palavra.EndsWith("aram") ||
                   palavra.EndsWith("eram") || palavra.EndsWith("iram");
        }

        /// <summary>
        /// Verdadeiro se estas palavras, nesta ordem, formam uma oração em ordem
        /// direta — sujeito, verbo, e o que vier depois.
        ///
        /// É a régua que faltava na bancada 2. O motor dela já tirava o caminho de
        /// uma frase escrita por gente, mas a janela era uma FATIA QUALQUER dessa
        /// frase: media-se 40% de orações completas nas janelas de cinco palavras.
        /// O aluno montava "a matéria na lousa para a" — trecho verdadeiro, oração
        /// nenhuma — e o jogo dava por bom.
        ///
        /// Pior: o filtro antigo exigia que a janela NÃO começasse em ligação, e
        /// artigo é ligação. Ele proibia justamente "o professor escreveu…", que é
        /// a abertura mais comum do corpus, e empurrava a janela para o meio da
        /// oração. A regra que devia garantir sentido produzia o contrário.
        ///
        /// As três condições, cada uma por um motivo:
        ///
        ///   · COMEÇA EM SUJEITO — artigo ou substantivo, nunca preposição. "da
        ///     lousa…" só existe grudado no que veio antes.
        ///   · TEM VERBO, e depois do começo. Sem verbo não há oração; verbo na
        ///     primeira posição seria imperativo, que não é o que o corpus tem.
        ///   · NÃO TERMINA EM LIGAÇÃO. "o professor escreveu a" fica pedindo o
        ///     resto, e a corrente teria fechado no meio de um sintagma.
        /// </summary>
        public static bool EhOracao(IReadOnlyList<string> palavras)
        {
            if (palavras == null || palavras.Count < 3) return false;

            var abertura = palavras[0];
            if (EhLigacao(abertura) && !Artigos.Contains(abertura)) return false;
            if (EhVerbo(abertura)) return false;
            if (EhLigacao(palavras[^1])) return false;

            for (var i = 1; i < palavras.Count - 1; i++)
                if (EhVerbo(palavras[i])) return true;

            return false;
        }

        /// <summary>
        /// Verdadeiro se estas palavras aparecem GRUDADAS, nesta ordem, em alguma
        /// frase do corpus.
        ///
        /// É a diferença entre "os pares existem" e "a frase existe", e é a coisa
        /// que a bancada 2 tem para ensinar. Um modelo de bigramas só conhece
        /// pares: ele deixa montar "amanhã a aula de matemática ficou", porque cada
        /// emenda dessa corrente foi vista de verdade — e nenhuma pessoa escreveu
        /// aquela frase. Sem esta verificação o jogo dava a corrente por correta e
        /// o aluno saía com a lição ao contrário.
        ///
        /// Compara palavra por palavra, e não com <c>Contains</c> de texto: por
        /// texto, "a aula" casaria dentro de "da aulinha".
        /// </summary>
        public static bool EhTrechoReal(IReadOnlyList<string> palavras)
        {
            if (palavras == null || palavras.Count == 0) return false;

            foreach (var frase in Frases)
            {
                var dela = frase.Trim().Split(' ');
                for (var i = 0; i + palavras.Count <= dela.Length; i++)
                {
                    var bate = true;
                    for (var j = 0; j < palavras.Count && bate; j++)
                        if (dela[i + j] != palavras[j]) bate = false;

                    if (bate) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// O primeiro ponto em que a corrente deixa de ser texto real: devolve o
        /// índice da palavra que já não continua nenhuma frase escrita.
        ///
        /// Serve de dica honesta. Dizer "a frase não existe" e parar aí obriga o
        /// aluno a testar às cegas; dizer ONDE ela deixou de existir transforma o
        /// erro em informação, que é o que uma dica tem que fazer.
        /// </summary>
        public static int OndeQuebra(IReadOnlyList<string> palavras)
        {
            if (palavras == null || palavras.Count < 2) return -1;

            for (var n = 2; n <= palavras.Count; n++)
            {
                var prefixo = new List<string>();
                for (var i = 0; i < n; i++) prefixo.Add(palavras[i]);
                if (!EhTrechoReal(prefixo)) return n - 1;
            }
            return -1;
        }

        static void Garantir()
        {
            if (_frases != null) return;

            var bruto = Resources.Load<TextAsset>("Dados/corpusEscolar");
            if (bruto == null)
                throw new InvalidOperationException("Resources/Dados/corpusEscolar.json não encontrado.");

            var arquivo = JsonUtility.FromJson<Arquivo>(bruto.text);
            _frases = arquivo.frases;

            _vocabulario = new HashSet<string>();
            foreach (var frase in _frases)
            {
                foreach (var palavra in frase.Split(' '))
                {
                    if (palavra.Length > 0) _vocabulario.Add(palavra);
                }
            }
        }
    }
}
