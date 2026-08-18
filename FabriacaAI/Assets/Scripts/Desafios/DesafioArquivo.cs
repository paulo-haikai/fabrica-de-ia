using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Bancada 3 — o arquivo de Mestre Aurélio.
    ///
    /// Inspiração: CAMPO MINADO. A tabela de pares é uma grade de casinhas
    /// fechadas; clicar revela o que tem dentro. No Campo Minado o jogador caça o
    /// que NÃO tem mina; aqui ele caça o que TEM risquinho — e descobre, com o
    /// dedo, que quase tudo está vazio.
    ///
    /// A etapa tem três tempos, e a ordem é o argumento:
    ///
    ///   Rodada 1 — "o canto movimentado". As linhas são palavras muito usadas e
    ///   várias colunas são continuações reais delas. O aluno acha os pares
    ///   porque SABE PORTUGUÊS: depois de "a" vem "turma", depois de "na" vem
    ///   "lousa". É gostoso de jogar e ele ganha.
    ///
    ///   Rodada 2 — "a aposta de Aurélio". Mesma grade, mesmos cliques, palavras
    ///   sorteadas do vocabulário inteiro. Aurélio aposta que ele não acha
    ///   nenhum par — e ganha a aposta quase sempre.
    ///
    ///   Rodada 3 — a tabela completa aparece de uma vez, desenhada pixel a
    ///   pixel: 318 por 318 casinhas e as poucas acesas. Não é jogo, é o
    ///   espanto — e num projetor funciona melhor que qualquer número.
    ///
    /// A primeira versão desta bancada comparava um arquivo PEQUENO com o
    /// arquivo inteiro, supondo que o pequeno fosse mais fácil. A ferramenta de
    /// conferência mostrou o contrário: num arquivo de seis frases, as palavras
    /// mais movimentadas têm uma ou duas continuações, e a região cheia é ainda
    /// mais vazia que a do arquivo grande. A lição saía invertida.
    ///
    /// O desenho atual é mais honesto e ensina mais: o que separa a rodada 1 da
    /// rodada 2 não é o tamanho do arquivo, é se as palavras da grade têm
    /// relação entre si. O aluno vence a primeira usando conhecimento de língua
    /// que a máquina não tem — e perde a segunda porque, sem risquinho, não há
    /// nada ali. É a diferença entre saber a língua e ter uma tabela.
    /// </summary>
    public partial class DesafioArquivo : DesafioEmNiveis
    {
        public override string Etapa => "e3";
        public override string Titulo => "O arquivo que não cabe";

        protected override int Niveis => 3;

        /// <summary>
        /// A grade, medida em vez de chutada.
        ///
        /// Era 5x8 com 12 cliques, e a medição no corpus da aula reprovou: 23% de
        /// casinhas cheias, meia linha sem par nenhum e 2,8 acertos esperados em
        /// 12 cliques para uma meta de 3 — o aluno mediano PERDIA a rodada que
        /// existe para ele ganhar. E com 23%, quatro cliques seguidos no vazio
        /// acontecem em 35% das partidas: quem cai nisso desiste achando que a
        /// bancada está quebrada, e foi exatamente o que aconteceu no teste.
        ///
        /// Agora 4x6 com 10 cliques, e as colunas escolhidas para cobrir as linhas
        /// (ver <see cref="Bigrama.Canto"/>): 40% cheias, nenhuma linha morta, 4
        /// acertos esperados. Menos casinhas também deixa cada uma maior, e a
        /// palavra dentro dela legível de longe — numa sala, o aluno do fundo
        /// também joga.
        /// </summary>
        const int Colunas = 6;
        const int Linhas = 4;
        const float LarguraRotulo = 132f;
        const float LarguraCelula = 108f;
        const float AlturaCelula = 38f;
        const int Cliques = 10;

        Mulberry32 _sorteio;
        Bigrama _arquivo;

        string[] _de;
        string[] _para;
        int _cliques;
        int _meta;
        int _achados;
        readonly Dictionary<Button, (int linha, int coluna)> _celulas = new();
        Text _contador;

        protected override void Preparar()
        {
            _sorteio = new Mulberry32((uint)Rodadas.Semente());
            _arquivo = new Bigrama(Corpus.Frases);
        }

        protected override void MontarNivel()
        {
            if (NivelAtual == 2)
            {
                MostrarTabelaInteira();
                return;
            }

            _cliques = 0;
            _achados = 0;
            _celulas.Clear();
            _meta = NivelAtual == 0 ? 3 : 1;

            EscolherPalavras();
            MontarGrade();

            Painel.Rodape("a casinha cruza a palavra da linha com a da coluna");
            Painel.Instruir(NivelAtual == 0
                ? $"ache {_meta} duplas que alguém já escreveu"
                : "Aurélio aposta que você não acha nenhuma dupla aqui");
            Atualizar();
        }

        /// <summary>
        /// Escolhe as palavras da grade — e é aqui que mora a diferença entre as
        /// duas rodadas.
        ///
        /// Rodada 1: linhas movimentadas e colunas que são, em boa parte,
        /// continuações REAIS dessas linhas. O aluno acha os pares deduzindo pela
        /// língua, não clicando à esmo — que é o que torna a rodada divertida em
        /// vez de sorteio.
        ///
        /// Rodada 2: linhas e colunas sorteadas do vocabulário inteiro, sem
        /// nenhuma relação entre si. É a tabela como ela é de verdade fora do
        /// cantinho arrumado: vazia.
        /// </summary>
        void EscolherPalavras()
        {
            if (NivelAtual == 0)
            {
                // O algoritmo vive no motor porque a conferência de conteúdo mede
                // esta mesma grade. Enquanto havia duas cópias, a medição atestava
                // um jogo e o aluno jogava outro.
                var (de, para) = Bigrama.Canto(_arquivo, Linhas, Colunas, _sorteio);
                _de = de.ToArray();
                _para = para.ToArray();
                return;
            }

            var vocabulario = _arquivo.Palavras
                                      .Where(p => p != Bigrama.Inicio && p != Bigrama.Fim)
                                      .ToList();
            _de = Amostra(vocabulario, Linhas);
            _para = Amostra(vocabulario, Colunas);
        }

        string[] Amostra(List<string> fonte, int quantas)
        {
            var copia = new List<string>(fonte);
            for (var i = copia.Count - 1; i > 0; i--)
            {
                var j = (int)(_sorteio.Proximo() * (i + 1));
                (copia[i], copia[j]) = (copia[j], copia[i]);
            }
            return copia.Take(Mathf.Min(quantas, copia.Count)).ToArray();
        }

        void MontarGrade()
        {
            var titulo = Widgets.Texto("Título", Area, 15, TextAnchor.UpperCenter, Cores.Neblina);
            Widgets.Faixa(titulo.rectTransform, true, 20f);
            titulo.text = NivelAtual == 0
                ? "o canto do arquivo que Aurélio mais usa"
                : $"um pedaço qualquer do arquivo — {_arquivo.Palavras.Count} palavras no total";

            var grade = Widgets.Painel("Grade", Area, Color.clear);
            Widgets.Fixar(grade, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f),
                          new Vector2(LarguraRotulo + Colunas * LarguraCelula,
                                      (Linhas + 1) * AlturaCelula));

            var esquerda = -(LarguraRotulo + Colunas * LarguraCelula) / 2f;
            var topo = (Linhas + 1) * AlturaCelula / 2f;

            // COMO SE LÊ A TABELA — a informação que faltava por inteiro.
            //
            // A grade sempre significou "palavra da linha, seguida da palavra da
            // coluna", e isso nunca esteve escrito em lugar nenhum da tela. Quem
            // pulou o tutorial via um quadriculado de casinhas vazias e a palavra
            // "pares" numa instrução que nunca definiu o que era um par. Sem essa
            // leitura, a rodada 1 vira sorteio: são 3 acertos em 12 cliques numa
            // grade de 40 casas, e sem saber o que a casa pergunta não há como
            // deduzir nada — o aluno conclui, com razão, que é impossível.
            var deDica = Widgets.Texto("DicaDe", grade, 11, TextAnchor.MiddleRight, Cores.Neblina);
            Widgets.Fixar(deDica.rectTransform, new Vector2(0.5f, 0.5f),
                new Vector2(esquerda + LarguraRotulo / 2f, topo + AlturaCelula * 0.55f),
                new Vector2(LarguraRotulo - 10f, AlturaCelula));
            deDica.text = "primeira palavra";

            var paraDica = Widgets.Texto("DicaPara", grade, 11, TextAnchor.MiddleCenter,
                                         Cores.Neblina);
            Widgets.Fixar(paraDica.rectTransform, new Vector2(0.5f, 0.5f),
                new Vector2(esquerda + LarguraRotulo + Colunas * LarguraCelula / 2f,
                            topo + AlturaCelula * 0.55f),
                new Vector2(Colunas * LarguraCelula, AlturaCelula));
            paraDica.text = "…e a segunda: a casinha pergunta se alguém escreveu as duas juntas";

            // Cabeçalho das colunas: "o que vem depois".
            for (var c = 0; c < _para.Length; c++)
            {
                var rotulo = Widgets.Texto($"col{c}", grade, 12, TextAnchor.MiddleCenter, Cores.Luz);
                Widgets.Fixar(rotulo.rectTransform, new Vector2(0.5f, 0.5f),
                    new Vector2(esquerda + LarguraRotulo + (c + 0.5f) * LarguraCelula,
                                topo - AlturaCelula / 2f),
                    new Vector2(LarguraCelula - 4f, AlturaCelula));
                rotulo.text = _para[c];
            }

            for (var l = 0; l < _de.Length; l++)
            {
                var rotulo = Widgets.Texto($"lin{l}", grade, 13, TextAnchor.MiddleRight, Cores.Papel);
                Widgets.Fixar(rotulo.rectTransform, new Vector2(0.5f, 0.5f),
                    new Vector2(esquerda + LarguraRotulo / 2f,
                                topo - (l + 1.5f) * AlturaCelula),
                    new Vector2(LarguraRotulo - 10f, AlturaCelula));
                rotulo.text = _de[l];

                for (var c = 0; c < _para.Length; c++)
                {
                    var botao = Widgets.Botao($"c{l}_{c}", grade, string.Empty,
                                              Cores.TintaClara, Cores.Papel, 13);
                    Widgets.Fixar((RectTransform)botao.transform, new Vector2(0.5f, 0.5f),
                        new Vector2(esquerda + LarguraRotulo + (c + 0.5f) * LarguraCelula,
                                    topo - (l + 1.5f) * AlturaCelula),
                        new Vector2(LarguraCelula - 4f, AlturaCelula - 4f));

                    var linha = l;
                    var coluna = c;
                    botao.onClick.AddListener(() => Abrir(botao, linha, coluna));
                    _celulas[botao] = (linha, coluna);
                }
            }

            _contador = Widgets.Texto("Contador", Area, 15, TextAnchor.LowerCenter, Cores.Luz);
            Widgets.Faixa(_contador.rectTransform, false, 20f, 4f);

            if (NivelAtual == 0) AbrirExemplo();
        }

        /// <summary>
        /// Abre UMA casinha cheia de graça, antes de o aluno tocar em nada.
        ///
        /// É o conserto mais barato da bancada e o que faz mais diferença. A
        /// tabela sempre significou "palavra da linha seguida da palavra da
        /// coluna", e nada na tela dizia isso; um exemplo já aberto mostra a
        /// leitura, prova que existe casinha cheia nesta grade, e dá ao aluno o
        /// padrão que ele vai procurar. Custa zero clique e zero parágrafo.
        ///
        /// Fica em cor própria, e não na cor de acerto: ela não é dele. Quem
        /// contou foi Aurélio.
        /// </summary>
        void AbrirExemplo()
        {
            foreach (var par in _celulas)
            {
                var (linha, coluna) = par.Value;
                var risquinhos = _arquivo.Risquinhos(_de[linha], _para[coluna]);
                if (risquinhos <= 0) continue;

                var botao = par.Key;
                botao.interactable = false;
                botao.GetComponent<Image>().color = Cores.Madeira;

                var texto = botao.GetComponentInChildren<Text>();
                texto.text = new string('|', Mathf.Min(risquinhos, 8));
                texto.color = Cores.Luz;

                Painel.Instruir($"exemplo: “{_de[linha]} {_para[coluna]}” — " +
                                $"alguém escreveu isso {risquinhos} " +
                                (risquinhos == 1 ? "vez" : "vezes"), Cores.Luz);
                Widgets.Pulsar((RectTransform)botao.transform, 1.2f, 0.4f);
                return;
            }
        }

        void Abrir(Button botao, int linha, int coluna)
        {
            if (!botao.interactable) return;

            var risquinhos = _arquivo.Risquinhos(_de[linha], _para[coluna]);
            _cliques++;
            botao.interactable = false;

            var fundo = botao.GetComponent<Image>();
            var texto = botao.GetComponentInChildren<Text>();

            // O gesto comum do jogo, no verbo central desta bancada: são até 36
            // cliques por visita, e até agora todos eles respondiam só com cor.
            Widgets.Marcar((RectTransform)botao.transform, risquinhos > 0);

            // A dupla que ele acabou de abrir, dita por extenso.
            //
            // É o professor invisível desta bancada: no PRIMEIRO clique o aluno lê
            // «“na lousa” — escrito 7 vezes» e entende a tabela inteira de uma vez,
            // sem tutorial e sem parágrafo. Vale mais que qualquer instrução no
            // topo, porque chega no instante em que ele acabou de agir.
            Painel.Instruir(
                risquinhos > 0
                    ? $"“{_de[linha]} {_para[coluna]}” — escrito {risquinhos} " +
                      (risquinhos == 1 ? "vez" : "vezes")
                    : $"“{_de[linha]} {_para[coluna]}” — ninguém escreveu isso",
                risquinhos > 0 ? Cores.Folha : Cores.Neblina);

            if (risquinhos > 0)
            {
                _achados++;
                fundo.color = Cores.Folha;
                texto.text = new string('|', Mathf.Min(risquinhos, 8));
                texto.color = Cores.Papel;
            }
            else
            {
                // Casinha vazia fica MAIS escura que o fechado, não igual: o mapa
                // do que já se procurou é o que faz o jogador sentir a extensão
                // do vazio em vez de só falhar.
                fundo.color = Cores.Tinta;
                texto.text = "·";
                texto.color = Cores.TintaClara;
            }

            Atualizar();
        }

        void Atualizar()
        {
            _contador.text = NivelAtual == 0
                ? $"achou {_achados} de {_meta}   ·   cliques: {_cliques} de {Cliques}"
                : $"achou {_achados}   ·   cliques: {_cliques} de {Cliques}";

            if (_achados >= _meta)
            {
                Travar();
                if (NivelAtual == 0)
                {
                    Resolveu($"Achou {_achados} em {_cliques} cliques",
                        "Repare COMO você achou: nada de chute. Leu “na”, pensou\n" +
                        "“lousa” — usou o português que sabe.\n\n" +
                        "A máquina não sabe português. Só tem os risquinhos.\n" +
                        "Agora Aurélio quer apostar com você.");
                }
                else
                {
                    Resolveu($"Você achou {_achados} e ganhou a aposta",
                        "Sorte grande — quase ninguém acha nessas condições.\n\n" +
                        "É esse o ponto: fora do cantinho arrumado, o arquivo\n" +
                        "está vazio. Aurélio vai te mostrar o tanto.");
                }
                return;
            }

            if (_cliques < Cliques) return;

            Travar();
            if (NivelAtual == 0)
            {
                Falhou($"Os cliques acabaram — você achou {_achados}",
                    "Pense como quem lê: o que costuma vir depois de cada\n" +
                    "palavra da esquerda?\n\n" +
                    "Siga assim mesmo. O que vem agora é pior.");
            }
            else
            {
                Falhou("Aurélio ganhou a aposta",
                    $"Doze casinhas abertas, {_achados} com risquinho.\n\n" +
                    "Antes você achou vários — as palavras tinham relação e\n" +
                    "você SABE a língua. Aqui não há relação: só a tabela.\n\n" +
                    "E a tabela é isto. Veja o tamanho dela.",
                    contaEstrela: true);
            }
        }

        void Travar()
        {
            foreach (var botao in _celulas.Keys) botao.interactable = false;
        }

        // -------------------------------------------------- a tabela inteira

        /// <summary>
        /// A tabela completa, desenhada como imagem.
        ///
        /// São 318 por 318 casinhas — mais de cem mil. Criar cem mil objetos de
        /// interface travaria o jogo; uma textura de 318 por 318 pixels custa
        /// nada e mostra a mesma coisa melhor. Cada pixel aceso é um par que o
        /// arquivo viu.
        ///
        /// É o momento mais barato e mais eficaz da bancada: o aluno não recebe
        /// a estatística, ele VÊ o vazio.
        /// </summary>
        void MostrarTabelaInteira()
        {
            var palavras = _arquivo.Palavras;
            var n = palavras.Count;

            var textura = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var vazio = new Color32(24, 27, 36, 255);
            var aceso = new Color32(242, 193, 78, 255);
            var pixels = new Color32[n * n];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = vazio;

            var cheias = 0;
            for (var l = 0; l < n; l++)
            {
                foreach (var c in _arquivo.Continuacoes(palavras[l]))
                {
                    var coluna = _arquivo.IndiceDe(c.Para);
                    if (coluna < 0) continue;
                    // Textura cresce de baixo para cima; a tabela, de cima para
                    // baixo. Sem inverter, o desenho sai de cabeça para baixo.
                    pixels[(n - 1 - l) * n + coluna] = aceso;
                    cheias++;
                }
            }
            textura.SetPixels32(pixels);
            textura.Apply();

            var titulo = Widgets.Texto("Título", Area, 17, TextAnchor.UpperCenter, Cores.Papel);
            Widgets.Faixa(titulo.rectTransform, true, 22f);
            titulo.text = $"a tabela inteira: {n} palavras por {n} palavras";

            var moldura = Widgets.Painel("Moldura", Area, Cores.TintaClara);
            Widgets.Fixar(moldura, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f),
                          new Vector2(268f, 268f));

            var imagem = new GameObject("Tabela", typeof(RectTransform), typeof(Image));
            imagem.transform.SetParent(moldura, false);
            Widgets.Esticar((RectTransform)imagem.transform, 4f);
            imagem.GetComponent<Image>().sprite =
                Sprite.Create(textura, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 16f);

            var total = (long)n * n;
            var conta = Widgets.Texto("Conta", Area, 16, TextAnchor.LowerCenter, Cores.Luz);
            Widgets.Faixa(conta.rectTransform, false, 44f, 4f);
            conta.text = $"{total:n0} casinhas  ·  {cheias:n0} com risquinho  ·  " +
                         $"{(100f * cheias / total):0.00}% preenchida";

            Painel.Instruir("é isto que Aurélio guarda no fichário");
            Painel.Rodape("cada pontinho aceso é um par que alguém escreveu");

            Resolveu("Noventa e nove por cento de nada",
                "A tabela cresce com o QUADRADO do vocabulário: dobre as palavras\n" +
                "e ela quadruplica — o texto do mundo nunca vai preencher.\n\n" +
                "Guardar par por par não escala. O que vem agora é aprender\n" +
                "uma REGRA que vale até para pares que ela nunca viu.");
        }
    }
}
