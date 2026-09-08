using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Bancada 8 — a cozinha do Chef Amaro.
    ///
    /// Inspiração: LIGUE OS PONTOS. De um lado as máquinas, do outro o que cada
    /// uma comeu. O aluno pega um pontinho e puxa um traço até o alimento certo.
    ///
    /// A lição é a mesma de sempre e o gesto agora a diz sozinho: **a máquina é
    /// o que ela leu**. Ligar uma à sua comida é essa frase virada em risco na
    /// tela — e é por isso que a bancada não precisa de parágrafo nenhum antes.
    ///
    /// A versão anterior era uma prova cega: três pratos tapados, e o aluno
    /// casava tudo de uma vez no fim. Tinha dois defeitos que este desenho
    /// resolve de graça. Era tudo-ou-nada numa permutação de três — acertar dois
    /// obriga o terceiro, então os resultados possíveis eram 0, 1 ou 3, e o acaso
    /// sozinho dava 1 em 6. E não havia nada para FAZER com as mãos: era ler,
    /// pensar e clicar uma vez.
    ///
    /// Agora cada ligação responde na hora. Errou, o traço treme e some; acertou,
    /// fica. O aluno atravessa a bancada por partes, e a parte que ele já venceu
    /// não pode ser perdida pela próxima.
    /// </summary>
    public partial class DesafioCozinha : DesafioEmNiveis
    {
        public override string Etapa => "e8";
        public override string Titulo => "O que ela come";

        protected override int Niveis => 3;

        const int Pratos = 3;
        const int FrasesPorAmostra = 3;

        /// <summary>
        /// Os recortes do corpus, e as palavras que marcam cada um.
        ///
        /// Cada filtro pega um recorte com vocabulário bem distinto dos outros —
        /// é o que torna a dedução possível. Recortes parecidos dariam três
        /// máquinas falando igual, e o jogo viraria sorteio de um em três.
        /// </summary>
        static readonly (string nome, string[] marcas)[] Cardapio =
        {
            ("o caderno de aulas",
             new[] { "professor", "professora", "matéria", "lousa", "aula", "explicou",
                     "quadro", "caderno", "copiou", "escreveu", "exercício", "lição" }),
            ("o diário do recreio",
             new[] { "recreio", "bola", "quadra", "pátio", "jogaram", "jogou", "correu",
                     "brincou", "time", "gol", "lanche", "merenda" }),
            ("o livro da horta",
             new[] { "horta", "planta", "plantou", "regou", "terra", "semente", "folha",
                     "árvore", "cresceu", "flor", "jardim", "verde" }),
            ("a caderneta de provas",
             new[] { "prova", "nota", "estudou", "difícil", "fácil", "acertou", "errou",
                     "resposta", "questão", "média", "resultado", "corrigiu" })
        };

        /// <summary>Uma máquina da rodada: o que ela comeu e o que ela escreveu.</summary>
        struct Maquina
        {
            public string Alimento;
            public List<string> Amostra;
        }

        Mulberry32 _sorteio;
        readonly List<Maquina> _maquinas = new();

        /// <summary>A ordem em que os alimentos aparecem na coluna da direita.</summary>
        readonly List<int> _ordemDosAlimentos = new();

        /// <summary>máquina → índice na coluna da direita, quando já ligada.</summary>
        readonly Dictionary<int, int> _ligacoes = new();

        int _erros;

        protected override void Preparar() =>
            _sorteio = new Mulberry32((uint)Rodadas.Semente());

        protected override void MontarNivel()
        {
            _ligacoes.Clear();
            _erros = 0;
            MontarPratos();
            MontarTela();

            Painel.Rodape("puxe um traço de cada máquina até o que ela comeu");
            Painel.Instruir("qual delas leu o quê?");
        }

        /// <summary>
        /// Sorteia os recortes, treina um modelo em cada e gera as amostras.
        ///
        /// O treino é de verdade e acontece aqui: três <see cref="Bigrama"/>
        /// construídos sobre recortes diferentes do corpus.
        /// </summary>
        void MontarPratos()
        {
            _maquinas.Clear();

            var indices = Enumerable.Range(0, Cardapio.Length).ToList();
            for (var i = indices.Count - 1; i > 0; i--)
            {
                var j = (int)(_sorteio.Proximo() * (i + 1));
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            foreach (var idx in indices.Take(Pratos))
            {
                var (nome, marcas) = Cardapio[idx];

                var frases = Corpus.Frases
                    .Where(f => marcas.Any(m => f.Contains(m)))
                    .ToList();

                // Prato magro não treina modelo. Completa com frases quaisquer
                // para o modelo ter o que dizer — e isso é honesto: corpus real
                // também é uma mistura, com um assunto dominante.
                if (frases.Count < 12)
                {
                    foreach (var f in Corpus.Frases)
                    {
                        if (frases.Count >= 12) break;
                        if (!frases.Contains(f)) frases.Add(f);
                    }
                }

                var modelo = new Bigrama(frases);
                var amostra = new List<string>();

                // A PRIMEIRA FRASE TEM QUE DENUNCIAR O ALIMENTO.
                //
                // Sem isto a bancada sorteava rodadas indecidíveis: nada obrigava a
                // amostra a conter palavra-marca nenhuma, e o prato magro, que é
                // completado com frases quaisquer, tem chance ainda maior de sair
                // mudo. Não basta a rodada ser difícil, ela precisa ser decidível.
                for (var t = 0; t < 120 && amostra.Count == 0; t++)
                {
                    var palavras = modelo.Gerar(_sorteio);
                    if (palavras.Count < 4) continue;
                    var frase = string.Join(" ", palavras);
                    if (marcas.Any(m => frase.Contains(m))) amostra.Add(frase);
                }

                for (var t = 0; t < 40 && amostra.Count < FrasesPorAmostra; t++)
                {
                    var palavras = modelo.Gerar(_sorteio);
                    if (palavras.Count < 4) continue;
                    var frase = string.Join(" ", palavras);
                    if (!amostra.Contains(frase)) amostra.Add(frase);
                }

                _maquinas.Add(new Maquina { Alimento = nome, Amostra = amostra });
            }

            // A coluna da direita não pode estar na ordem das máquinas, senão a
            // primeira ligação entrega o resto por eliminação visual.
            _ordemDosAlimentos.Clear();
            _ordemDosAlimentos.AddRange(Enumerable.Range(0, _maquinas.Count));
            for (var i = _ordemDosAlimentos.Count - 1; i > 0; i--)
            {
                var j = (int)(_sorteio.Proximo() * (i + 1));
                (_ordemDosAlimentos[i], _ordemDosAlimentos[j]) =
                    (_ordemDosAlimentos[j], _ordemDosAlimentos[i]);
            }
        }

        // -------------------------------------------------------- as ligações

        /// <summary>
        /// O aluno soltou o traço em cima de um alimento.
        ///
        /// A resposta é IMEDIATA, e é o que separa esta versão da anterior. Errar
        /// aqui custa um risco na conta e nada mais: o que ele já ligou certo
        /// continua ligado. Numa prova cega de três, acertar dois obrigava o
        /// terceiro — o aluno nunca sabia quantas das suas ideias estavam certas,
        /// só se todas estavam.
        /// </summary>
        void Ligar(int maquina, int posicaoNaDireita)
        {
            if (_ligacoes.ContainsKey(maquina)) return;
            if (_ligacoes.ContainsValue(posicaoNaDireita)) return;

            var certo = _ordemDosAlimentos[posicaoNaDireita] == maquina;

            if (!certo)
            {
                _erros++;
                RecusarLigacao(maquina, posicaoNaDireita);
                Painel.Instruir("essa não é a comida dela — repare no vocabulário",
                                Cores.Brasa);
                return;
            }

            _ligacoes[maquina] = posicaoNaDireita;
            AceitarLigacao(maquina, posicaoNaDireita);
            Painel.Instruir($"“{_maquinas[maquina].Alimento}” — é o que ela leu",
                            Cores.Folha);

            if (_ligacoes.Count < _maquinas.Count) return;

            var titulo = _erros == 0
                ? "As três, de primeira"
                : $"As três ligadas — com {_erros} " + (_erros == 1 ? "tentativa perdida" : "tentativas perdidas");

            Resolveu(titulo,
                "Você reconheceu cada máquina pela boca dela — porque a boca\n" +
                "dela é o texto que ela comeu.\n\n" +
                "As três são a MESMA máquina por dentro. Mudou só o que\n" +
                "leram.\n\n" +
                "É por isso que “que dados essa IA usou?” não é curiosidade\n" +
                "técnica — é a pergunta sobre quem ela é.");
        }
    }
}
