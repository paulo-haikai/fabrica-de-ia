using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Bancada 9 — a cozinha do Chef Amaro.
    ///
    /// Inspiração: TESTE CEGO, o jogo de mesa mais antigo do mundo — três pratos
    /// tapados, três sabores, case quem é quem. Em jogo digital é a família do
    /// Cluedo e do Papers, Please: dedução por evidência, sem sorte envolvida.
    ///
    /// Aqui os "pratos" são três textos diferentes e as "amostras" são frases que
    /// máquinas de risquinhos treinadas em cada um deles escreveram. O aluno lê o
    /// que cada máquina falou e tem que dizer qual texto ela comeu.
    ///
    /// E funciona porque as máquinas SÃO treinadas de verdade, ali, na hora: cada
    /// prato constrói um <see cref="Bigrama"/> próprio a partir de um recorte
    /// diferente do corpus, e as frases são geradas por sorteio ponderado. O aluno
    /// não está lendo texto escrito por mim imitando uma IA — está lendo a saída
    /// de três modelos distintos.
    ///
    /// A lição chega sozinha, e é a mais fácil de enunciar e a mais difícil de
    /// aceitar: a máquina não tem estilo, opinião nem assunto preferido. Ela tem
    /// o texto que comeu. Troque o prato e ela vira outra — mesma arquitetura,
    /// mesmos fios, outra boca.
    /// </summary>
    public partial class DesafioCozinha : DesafioEmNiveis
    {
        public override string Etapa => "e9";
        public override string Titulo => "O que ela come";

        protected override int Niveis => 3;

        const int Pratos = 3;
        const int FrasesPorAmostra = 3;

        /// <summary>
        /// Os pratos: um nome e o filtro que escolhe as frases do corpus.
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

        readonly List<(string nome, Bigrama modelo, List<string> amostra)> _rodada = new();
        readonly Dictionary<int, int> _palpites = new();
        int _amostraAberta = -1;

        RectTransform _amostras;
        RectTransform _bandejas;
        Mulberry32 _sorteio;

        protected override void Preparar() =>
            _sorteio = new Mulberry32((uint)Rodadas.Semente());

        protected override void MontarNivel()
        {
            MontarPratos();
            _palpites.Clear();
            _amostraAberta = -1;

            Painel.Rodape("clique numa amostra, depois no prato que você acha que ela comeu");

            var rotulo = Widgets.Texto("Rótulo", Area, 15, TextAnchor.UpperCenter, Cores.Neblina);
            Widgets.Faixa(rotulo.rectTransform, true, 20f);
            rotulo.text = "três máquinas iguais, cada uma criada com um texto diferente";

            _amostras = Widgets.Painel("Amostras", Area, Color.clear);
            Widgets.Faixa(_amostras, true, 250f, 24f);

            _bandejas = Widgets.Painel("Bandejas", Area, Color.clear);
            Widgets.Esticar(_bandejas);
            _bandejas.offsetMax = new Vector2(0f, -282f);

            Redesenhar();
        }

        /// <summary>
        /// Sorteia três pratos, treina um modelo em cada e gera as amostras.
        ///
        /// O treino é de verdade e acontece aqui: três <see cref="Bigrama"/>
        /// construídos sobre recortes diferentes do corpus. A geração usa o
        /// sorteio ponderado pela contagem, que é como o modelo escreve.
        /// </summary>
        void MontarPratos()
        {
            _rodada.Clear();

            var indices = Enumerable.Range(0, Cardapio.Length).ToList();
            for (var i = indices.Count - 1; i > 0; i--)
            {
                var j = (int)(_sorteio.Proximo() * (i + 1));
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            foreach (var idx in indices.Take(Pratos))
            {
                var (nome, marcas) = Cardapio[idx];

                // Uma frase entra no prato se contiver alguma palavra-marca dele.
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
                for (var t = 0; t < 40 && amostra.Count < FrasesPorAmostra; t++)
                {
                    var palavras = modelo.Gerar(_sorteio);
                    if (palavras.Count < 4) continue;
                    var frase = string.Join(" ", palavras);
                    if (!amostra.Contains(frase)) amostra.Add(frase);
                }

                _rodada.Add((nome, modelo, amostra));
            }

            // A ordem das amostras na tela não pode ser a ordem dos pratos.
            for (var i = _rodada.Count - 1; i > 0; i--)
            {
                var j = (int)(_sorteio.Proximo() * (i + 1));
                (_rodada[i], _rodada[j]) = (_rodada[j], _rodada[i]);
            }
        }

        // ------------------------------------------------------------ pintura

        void Redesenhar()
        {
            foreach (Transform filho in _amostras) Destroy(filho.gameObject);
            foreach (Transform filho in _bandejas) Destroy(filho.gameObject);

            DesenharAmostras();
            DesenharBandejas();

            var faltam = Pratos - _palpites.Count;
            Painel.Instruir(faltam == 0
                ? "confira as suas apostas"
                : _amostraAberta >= 0
                    ? "agora escolha o prato desta amostra"
                    : $"faltam {faltam} amostras para casar");

            Painel.Acao(faltam == 0 ? "conferir" : null,
                        faltam == 0 ? Conferir : (System.Action)null);
        }

        void DesenharAmostras()
        {
            const float largura = 290f;
            for (var i = 0; i < _rodada.Count; i++)
            {
                var indice = i;
                var escolhida = _amostraAberta == i;
                var casada = _palpites.ContainsKey(i);

                var botao = Widgets.Botao($"a{i}", _amostras, string.Empty,
                                          escolhida ? Cores.Madeira :
                                          casada ? Cores.TintaClara : Cores.Tinta,
                                          Cores.Papel);
                Widgets.Fixar((RectTransform)botao.transform, new Vector2(0.5f, 0.5f),
                              new Vector2((i - 1) * (largura + 12f), 0f),
                              new Vector2(largura, 230f));
                Destroy(botao.GetComponentInChildren<Text>().gameObject);

                var titulo = Widgets.Texto("t", (RectTransform)botao.transform, 14,
                                           TextAnchor.UpperCenter, Cores.Luz);
                Widgets.Faixa(titulo.rectTransform, true, 20f, 8f);
                titulo.text = $"máquina {(char)('A' + i)}";

                var corpo = Widgets.Texto("c", (RectTransform)botao.transform, 14,
                                          TextAnchor.UpperLeft, Cores.Papel);
                Widgets.Esticar(corpo.rectTransform, 12f);
                corpo.rectTransform.offsetMax = new Vector2(-12f, -32f);
                corpo.rectTransform.offsetMin = new Vector2(12f, 34f);
                corpo.text = string.Join("\n\n", _rodada[i].amostra.Select(f => $"“{f}”"));

                var escolha = Widgets.Texto("e", (RectTransform)botao.transform, 13,
                                            TextAnchor.LowerCenter, Cores.Folha);
                Widgets.Faixa(escolha.rectTransform, false, 20f, 8f);
                escolha.text = casada ? Cardapio[_palpites[i]].nome : "sem prato";

                botao.onClick.AddListener(() => Abrir(indice));
            }
        }

        void DesenharBandejas()
        {
            var rotulo = Widgets.Texto("Rótulo", _bandejas, 14, TextAnchor.UpperCenter, Cores.Neblina);
            Widgets.Faixa(rotulo.rectTransform, true, 18f);
            rotulo.text = "os três textos da cozinha";

            // As bandejas mostram os pratos DESTA rodada, em ordem de cardápio —
            // e o aluno tem que descobrir qual amostra veio de qual.
            var pratos = _rodada
                .Select(r => System.Array.FindIndex(Cardapio, c => c.nome == r.nome))
                .OrderBy(i => i)
                .ToList();

            const float largura = 250f;
            for (var i = 0; i < pratos.Count; i++)
            {
                var prato = pratos[i];
                var usado = _palpites.ContainsValue(prato);

                var botao = Widgets.Botao($"b{i}", _bandejas, Cardapio[prato].nome,
                                          usado ? Cores.TintaClara : Cores.Madeira,
                                          usado ? Cores.Neblina : Cores.Papel, 15);
                Widgets.Fixar((RectTransform)botao.transform, new Vector2(0.5f, 1f),
                              new Vector2((i - 1) * (largura + 12f), -46f),
                              new Vector2(largura, 48f));
                botao.onClick.AddListener(() => Casar(prato));
                botao.interactable = _amostraAberta >= 0;
            }
        }

        // ------------------------------------------------------------- jogadas

        void Abrir(int amostra)
        {
            // Clicar numa amostra já casada desfaz a aposta: mudar de ideia não
            // pode custar nada, senão o aluno para de arriscar.
            if (_palpites.Remove(amostra))
            {
                _amostraAberta = amostra;
                Redesenhar();
                return;
            }
            _amostraAberta = _amostraAberta == amostra ? -1 : amostra;
            Redesenhar();
        }

        void Casar(int prato)
        {
            if (_amostraAberta < 0) return;
            // Um prato só serve uma máquina: é o que faz a dedução fechar, como
            // no teste cego de verdade.
            var jaUsado = _palpites.FirstOrDefault(p => p.Value == prato);
            if (_palpites.ContainsValue(prato)) _palpites.Remove(jaUsado.Key);

            _palpites[_amostraAberta] = prato;
            _amostraAberta = -1;
            Redesenhar();
        }

        void Conferir()
        {
            var certos = _palpites.Count(p => _rodada[p.Key].nome == Cardapio[p.Value].nome);

            if (certos == Pratos)
            {
                Resolveu("Os três pratos, certos",
                    "Você reconheceu o texto pela boca da máquina — porque a boca\n" +
                    "dela é o texto. As três eram a MESMA máquina: só o que\n" +
                    "comeram mudava.\n\n" +
                    "É por isso que “que dados essa IA usou?” não é curiosidade\n" +
                    "técnica — é a pergunta sobre quem ela é.");
                return;
            }

            var gabarito = string.Join("\n", _rodada.Select(
                (r, i) => $"   máquina {(char)('A' + i)}:  {r.nome}"));

            Falhou($"Você acertou {certos} de {Pratos}",
                "O certo era:\n\n" + gabarito + "\n\n" +
                "Não é fácil — as três máquinas são idênticas por dentro.\n" +
                "A única diferença está no que leram.",
                contaEstrela: certos >= 2);
        }
    }
}
