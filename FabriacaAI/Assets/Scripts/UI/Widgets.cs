using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.UI
{
    /// <summary>
    /// A paleta e a fabriquinha de widgets da interface.
    ///
    /// As cores são as mesmas do cenário, tiradas de <c>scripts/arte/paleta.mjs</c>.
    /// Uma UI com paleta própria é o jeito mais rápido de o jogo parecer duas
    /// coisas coladas: o salão em madeira quente e um menu cinza de sistema
    /// operacional por cima.
    /// </summary>
    public static class Cores
    {
        public static readonly Color Tinta = new(0.106f, 0.122f, 0.165f, 0.94f);
        public static readonly Color TintaOpaca = new(0.106f, 0.122f, 0.165f, 1f);
        public static readonly Color TintaClara = new(0.173f, 0.196f, 0.259f, 1f);
        public static readonly Color Papel = new(0.937f, 0.890f, 0.784f);
        public static readonly Color Neblina = new(0.545f, 0.584f, 0.659f);
        public static readonly Color Luz = new(0.949f, 0.757f, 0.306f);
        public static readonly Color Folha = new(0.278f, 0.561f, 0.298f);
        public static readonly Color Brasa = new(0.878f, 0.396f, 0.247f);
        public static readonly Color Madeira = new(0.420f, 0.275f, 0.188f);
        public static readonly Color Vidro = new(0.310f, 0.690f, 0.690f);
    }

    /// <summary>
    /// Monta widgets de UGUI por código.
    ///
    /// Existe para a HUD e os desafios não repetirem trinta linhas de
    /// <c>RectTransform</c> cada um. Toda a ancoragem passa por aqui, que é
    /// onde os erros de layout costumam morar — concentrá-los num lugar só
    /// significa consertar uma vez.
    /// </summary>
    public static class Widgets
    {
        static Font _fonte;

        public static Font Fonte =>
            _fonte ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static RectTransform Painel(string nome, Transform pai, Color cor)
        {
            var objeto = new GameObject(nome, typeof(RectTransform), typeof(Image));
            objeto.transform.SetParent(pai, false);
            objeto.GetComponent<Image>().color = cor;
            return (RectTransform)objeto.transform;
        }

        public static Text Texto(string nome, Transform pai, int tamanho,
                                 TextAnchor alinhamento, Color cor)
        {
            var objeto = new GameObject(nome, typeof(RectTransform), typeof(Text));
            objeto.transform.SetParent(pai, false);

            var texto = objeto.GetComponent<Text>();
            texto.font = Fonte;
            texto.fontSize = tamanho;
            texto.alignment = alinhamento;
            texto.color = cor;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            return texto;
        }

        /// <summary>
        /// Botão com rótulo. Devolve o <c>Button</c> para quem chama ligar o
        /// clique e o <c>Image</c> de fundo, que é o que muda de cor quando o
        /// estado do botão muda.
        /// </summary>
        public static Button Botao(string nome, Transform pai, string rotulo,
                                   Color fundo, Color tinta, int tamanho = 16,
                                   TextAnchor alinhamento = TextAnchor.MiddleCenter)
        {
            var painel = Painel(nome, pai, fundo);
            var botao = painel.gameObject.AddComponent<Button>();
            botao.targetGraphic = painel.GetComponent<Image>();

            // Realce de passagem do mouse. Numa lista de catorze frases, sem
            // isso o aluno não sabe qual delas o clique vai pegar.
            var cores = botao.colors;
            cores.normalColor = Color.white;
            cores.highlightedColor = new Color(1.18f, 1.18f, 1.18f);
            cores.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            cores.fadeDuration = 0.08f;
            botao.colors = cores;

            var texto = Texto("Rótulo", painel, tamanho, alinhamento, tinta);
            Esticar(texto.rectTransform, 8f);
            texto.text = rotulo;
            return botao;
        }

        /// <summary>
        /// Prende um texto a UMA linha: ele encolhe para caber em vez de invadir.
        ///
        /// <see cref="Texto"/> nasce com <c>Wrap</c> + <c>Overflow</c>, que é o certo
        /// para cartaz — parágrafo tem que poder crescer. Mas é errado para rótulo em
        /// faixa de altura fixa: a frase comprida quebra em duas linhas e a segunda
        /// desce POR CIMA da linha de baixo. É o "frase cobrindo frase" que aparecia
        /// no histórico da malha, onde cada linha tem 17 pixels e o conteúdo tem
        /// comprimento variável.
        ///
        /// A saída não é truncar no seco (o aluno perderia a última palavra da frase,
        /// que é justamente a que a malha acabou de escolher) nem estourar para os
        /// lados. É deixar o texto DIMINUIR até caber: perde-se legibilidade nos
        /// casos extremos, e não se perde informação nem se estraga a linha vizinha.
        /// </summary>
        public static Text UmaLinha(Text texto, int minimo = 10)
        {
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Truncate;
            texto.resizeTextForBestFit = true;
            texto.resizeTextMinSize = minimo;
            texto.resizeTextMaxSize = texto.fontSize;
            return texto;
        }

        /// <summary>Troca o rótulo de um botão feito por <see cref="Botao"/>.</summary>
        public static void Rotular(Button botao, string texto) =>
            botao.GetComponentInChildren<Text>().text = texto;

        /// <summary>
        /// Um campo de digitação, para o aluno escrever o nome no diploma.
        ///
        /// Três cuidados que um campo de uGUI montado em código sempre pede, e que
        /// sem eles ele parece quebrado:
        ///
        ///   · O componente de texto precisa ser filho e ter <c>supportRichText</c>
        ///     desligado, senão um aluno que digite "&lt;b&gt;" some com a própria
        ///     letra.
        ///   · O <c>InputField</c> quer uma referência explícita ao Text e ao
        ///     placeholder; sem elas ele aceita tecla e não mostra nada.
        ///   · <c>raycastTarget</c> do Text vem desligado do <see cref="Texto"/>,
        ///     mas o FUNDO tem que estar ligado — é ele que recebe o clique que dá
        ///     foco ao campo.
        /// </summary>
        public static InputField Campo(string nome, Transform pai, string dica,
                                       int tamanho = 18, int maximo = 40)
        {
            var fundo = Painel(nome, pai, Cores.TintaClara);
            var campo = fundo.gameObject.AddComponent<InputField>();

            var texto = Texto("Texto", fundo, tamanho, TextAnchor.MiddleLeft, Cores.Papel);
            Esticar(texto.rectTransform, 10f);
            texto.supportRichText = false;

            var placeholder = Texto("Dica", fundo, tamanho, TextAnchor.MiddleLeft, Cores.Neblina);
            Esticar(placeholder.rectTransform, 10f);
            placeholder.text = dica;

            campo.targetGraphic = fundo.GetComponent<Image>();
            campo.textComponent = texto;
            campo.placeholder = placeholder;
            campo.characterLimit = maximo;
            campo.lineType = InputField.LineType.SingleLine;

            return campo;
        }

        /// <summary>Cola o retângulo nos quatro cantos do pai, com margem.</summary>
        public static void Esticar(RectTransform retangulo, float margem = 0f)
        {
            retangulo.anchorMin = Vector2.zero;
            retangulo.anchorMax = Vector2.one;
            retangulo.pivot = new Vector2(0.5f, 0.5f);
            retangulo.offsetMin = new Vector2(margem, margem);
            retangulo.offsetMax = new Vector2(-margem, -margem);
        }

        /// <summary>
        /// Ancora num ponto do pai. <paramref name="ancora"/> em (0,0) é canto
        /// inferior esquerdo, (0.5,0.5) é o centro, (1,1) o superior direito.
        /// </summary>
        public static void Fixar(RectTransform retangulo, Vector2 ancora,
                                 Vector2 posicao, Vector2 tamanho)
        {
            retangulo.anchorMin = ancora;
            retangulo.anchorMax = ancora;
            retangulo.pivot = new Vector2(0.5f, 0.5f);
            retangulo.anchoredPosition = posicao;
            retangulo.sizeDelta = tamanho;
        }

        /// <summary>
        /// Uma ficha: caixinha com rótulo, do tamanho pedido. É a peça de que os
        /// minigames são feitos — palavra, letra, neurônio, botão de mostrador.
        /// </summary>
        public static Text Ficha(string nome, Transform pai, string rotulo, Vector2 posicao,
                                 Vector2 tamanho, Color fundo, Color tinta, int fonte = 16)
        {
            var caixa = Painel(nome, pai, fundo);
            Fixar(caixa, new Vector2(0.5f, 0.5f), posicao, tamanho);
            var texto = Texto("t", caixa, fonte, TextAnchor.MiddleCenter, tinta);
            Esticar(texto.rectTransform);
            texto.text = rotulo;
            return texto;
        }

        /// <summary>
        /// Barra de preenchimento horizontal. Devolve o retângulo do miolo, que
        /// é o que quem chama vai redimensionar.
        ///
        /// Barra em vez de número porque proporção se lê de relance e número
        /// exige comparar — e a maior parte do que estes minigames mostram é
        /// proporção: quanto do orçamento foi gasto, quanto do erro sobrou.
        /// </summary>
        public static RectTransform Barra(string nome, Transform pai, Vector2 posicao,
                                          Vector2 tamanho, Color fundo, Color miolo)
        {
            var trilho = Painel(nome, pai, fundo);
            Fixar(trilho, new Vector2(0.5f, 0.5f), posicao, tamanho);

            var cheio = Painel("miolo", trilho, miolo);
            cheio.anchorMin = new Vector2(0f, 0f);
            cheio.anchorMax = new Vector2(0f, 1f);
            cheio.pivot = new Vector2(0f, 0.5f);
            cheio.anchoredPosition = Vector2.zero;
            cheio.sizeDelta = new Vector2(tamanho.x, 0f);
            return cheio;
        }

        /// <summary>Ajusta o preenchimento de uma barra feita por <see cref="Barra"/>.</summary>
        public static void Encher(RectTransform miolo, float fracao, float larguraTotal)
        {
            miolo.sizeDelta = new Vector2(Mathf.Clamp01(fracao) * larguraTotal, 0f);
        }

        /// <summary>Faixa horizontal encostada no topo ou na base do pai.</summary>
        public static void Faixa(RectTransform retangulo, bool noTopo, float altura, float margem = 0f)
        {
            retangulo.anchorMin = new Vector2(0f, noTopo ? 1f : 0f);
            retangulo.anchorMax = new Vector2(1f, noTopo ? 1f : 0f);
            retangulo.pivot = new Vector2(0.5f, noTopo ? 1f : 0f);
            retangulo.anchoredPosition = new Vector2(0f, noTopo ? -margem : margem);
            retangulo.sizeDelta = new Vector2(0f, altura);
        }

        // ------------------------------------------------------------ movimento

        /// <summary>
        /// O anfitrião das animações curtas.
        ///
        /// <see cref="Widgets"/> é estático e corrotina precisa de um
        /// MonoBehaviour vivo para rodar. Exigir um anfitrião em cada chamada
        /// espalharia essa preocupação pelas doze bancadas e por qualquer widget
        /// que quisesse se mexer; um motor escondido, criado na primeira vez e
        /// nunca destruído, mantém a chamada em uma linha.
        /// </summary>
        class Motor : MonoBehaviour { }

        static Motor _motor;

        static Motor Maquina
        {
            get
            {
                if (_motor != null) return _motor;
                var objeto = new GameObject("Animações");
                Object.DontDestroyOnLoad(objeto);
                _motor = objeto.AddComponent<Motor>();
                return _motor;
            }
        }

        /// <summary>
        /// A escala e a posição de repouso de quem está animando no momento.
        ///
        /// Sem isto, dois pulsos no mesmo botão (um clique duplo, que acontece)
        /// fazem o segundo capturar a escala já inflada do primeiro e devolvê-la
        /// como "normal" — o botão fica maior para sempre. A entrada só existe
        /// enquanto a animação roda, e o <c>finally</c> a remove mesmo quando o
        /// alvo morre no meio.
        /// </summary>
        static readonly Dictionary<RectTransform, Vector3> _escalaEmRepouso = new();
        static readonly Dictionary<RectTransform, Vector2> _postoEmRepouso = new();

        /// <summary>
        /// Pulso rápido de escala — 1 → pico → 1.
        ///
        /// É o "eu recebi seu clique" mais barato que existe: nenhuma ação do
        /// aluno deve deixar a tela parada.
        /// </summary>
        public static void Pulsar(RectTransform alvo, float pico = 1.15f, float duracao = 0.1f)
        {
            if (alvo == null) return;
            Maquina.StartCoroutine(Pulso(alvo, pico, duracao));
        }

        static IEnumerator Pulso(RectTransform alvo, float pico, float duracao)
        {
            if (!_escalaEmRepouso.TryGetValue(alvo, out var repouso))
                _escalaEmRepouso[alvo] = repouso = alvo.localScale;

            try
            {
                for (var t = 0f; t < duracao; t += Time.unscaledDeltaTime)
                {
                    if (alvo == null) yield break;
                    // Sobe na primeira metade do tempo, desce na segunda.
                    var forca = 1f - Mathf.Abs(t / duracao * 2f - 1f);
                    alvo.localScale = repouso * Mathf.Lerp(1f, pico, forca);
                    yield return null;
                }
                if (alvo != null) alvo.localScale = repouso;
            }
            finally { _escalaEmRepouso.Remove(alvo); }
        }

        /// <summary>
        /// Tremor horizontal que vai perdendo força. O "não" do jogo.
        ///
        /// Cor sozinha já era usada para erro em quase todas as bancadas; o que
        /// faltava era MOVIMENTO — vermelho parado se confunde com decoração,
        /// vermelho que treme é resposta.
        /// </summary>
        public static void Tremer(RectTransform alvo, float amplitude = 7f, float duracao = 0.18f)
        {
            if (alvo == null) return;
            Maquina.StartCoroutine(Tremor(alvo, amplitude, duracao));
        }

        static IEnumerator Tremor(RectTransform alvo, float amplitude, float duracao)
        {
            if (!_postoEmRepouso.TryGetValue(alvo, out var origem))
                _postoEmRepouso[alvo] = origem = alvo.anchoredPosition;

            try
            {
                for (var t = 0f; t < duracao; t += Time.unscaledDeltaTime)
                {
                    if (alvo == null) yield break;
                    var restante = 1f - t / duracao;
                    alvo.anchoredPosition =
                        origem + new Vector2(Mathf.Sin(t * 70f) * amplitude * restante, 0f);
                    yield return null;
                }
                if (alvo != null) alvo.anchoredPosition = origem;
            }
            finally { _postoEmRepouso.Remove(alvo); }
        }

        /// <summary>
        /// Entrada em cena: aparece esmaecido e um tico menor, e assenta.
        ///
        /// Menos de dois décimos de segundo, o suficiente para a troca de rodada
        /// virar um começo em vez de um estalo. Quem chama não precisa preparar
        /// nada — o <c>CanvasGroup</c> é posto aqui se ainda não houver.
        /// </summary>
        public static void Surgir(RectTransform alvo, float duracao = 0.18f,
                                  float escalaInicial = 0.96f)
        {
            if (alvo == null) return;
            var grupo = alvo.GetComponent<CanvasGroup>();
            if (grupo == null) grupo = alvo.gameObject.AddComponent<CanvasGroup>();
            Maquina.StartCoroutine(Surgimento(alvo, grupo, duracao, escalaInicial));
        }

        static IEnumerator Surgimento(RectTransform alvo, CanvasGroup grupo,
                                      float duracao, float escalaInicial)
        {
            var final = alvo.localScale;
            grupo.alpha = 0f;
            alvo.localScale = final * escalaInicial;

            for (var t = 0f; t < duracao; t += Time.unscaledDeltaTime)
            {
                if (alvo == null) yield break;
                var fase = Mathf.Clamp01(t / duracao);
                grupo.alpha = fase;
                alvo.localScale = final * Mathf.Lerp(escalaInicial, 1f, fase);
                yield return null;
            }

            if (alvo == null) yield break;
            grupo.alpha = 1f;
            alvo.localScale = final;
        }

        /// <summary>
        /// Um lampejo de cor sobre um retângulo, que apaga e se destrói sozinho.
        ///
        /// O véu não recebe clique — um flash que engolisse o toque do aluno
        /// seria pior do que flash nenhum.
        /// </summary>
        public static void Lampejo(RectTransform sobre, Color cor,
                                   float duracao = 0.35f, float forca = 0.5f)
        {
            if (sobre == null) return;

            var veu = Painel("Lampejo", sobre, new Color(cor.r, cor.g, cor.b, forca));
            Esticar(veu);
            veu.GetComponent<Image>().raycastTarget = false;
            veu.SetAsLastSibling();

            Maquina.StartCoroutine(Apagar(veu, cor, duracao, forca));
        }

        static IEnumerator Apagar(RectTransform veu, Color cor, float duracao, float forca)
        {
            var pintura = veu.GetComponent<Image>();
            for (var t = 0f; t < duracao; t += Time.unscaledDeltaTime)
            {
                if (veu == null) yield break;
                pintura.color = new Color(cor.r, cor.g, cor.b, forca * (1f - t / duracao));
                yield return null;
            }
            if (veu != null) Object.Destroy(veu.gameObject);
        }

        /// <summary>
        /// Confirma uma escolha com movimento: pulo no acerto, tremor no erro.
        ///
        /// Um lugar só para os doze minigames chamarem, para que "certo" e
        /// "errado" tenham o MESMO gesto em toda a fábrica — a cor já era comum,
        /// o movimento não era.
        /// </summary>
        public static void Marcar(RectTransform alvo, bool acertou)
        {
            if (acertou) Pulsar(alvo, 1.18f, 0.14f);
            else Tremer(alvo);
        }

        /// <summary>
        /// O último valor mostrado por cada contador, para saber se ele CAIU.
        ///
        /// As bancadas redesenham o placar inteiro a cada quadro de mudança;
        /// sem lembrar o valor anterior, não há como distinguir "sobraram 3"
        /// de "acabou de gastar um e sobraram 3".
        /// </summary>
        static readonly Dictionary<Text, int> _ultimoContado = new();

        /// <summary>
        /// Pinta e sacode um contador de recurso escasso.
        ///
        /// Cinco bancadas (elos, tesouradas, perguntas, tentativas, medições)
        /// tinham cada uma a sua versão da mesma regra — vermelho perto do fim,
        /// dourado no resto —, escrita ligeiramente diferente em cada arquivo.
        /// Aqui elas passam a compartilhar a regra E ganham o que nenhuma
        /// tinha: um pulso no instante em que o número cai, para que gastar o
        /// penúltimo recurso DOA um pouco.
        /// </summary>
        public static void Contar(Text mostrador, int restam, int alarme = 2)
        {
            if (mostrador == null) return;

            mostrador.color = restam <= alarme ? Cores.Brasa : Cores.Luz;

            if (_ultimoContado.TryGetValue(mostrador, out var antes) && restam < antes)
                Pulsar(mostrador.rectTransform, 1.12f, 0.12f);

            _ultimoContado[mostrador] = restam;
        }

        /// <summary>
        /// Progresso como bolinhas — "● ● ○ ○ ○" no lugar de "3 de 5".
        ///
        /// Quantidade pequena se lê de relance como forma; como número, exige
        /// ler, comparar e subtrair.
        /// </summary>
        public static string Pontos(int feitos, int total)
        {
            if (total <= 0) return string.Empty;

            var linha = new StringBuilder(total * 2);
            for (var i = 0; i < total; i++)
            {
                if (i > 0) linha.Append(' ');
                linha.Append(i < feitos ? '●' : '○');
            }
            return linha.ToString();
        }
    }
}
