using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FabricaDeIA.UI
{
    /// <summary>
    /// A moldura comum dos doze minigames.
    ///
    /// Existe por causa de dois defeitos que apareceram no primeiro playtest e
    /// que eram, os dois, de arquitetura e não de descuido:
    ///
    /// 1. NÃO DAVA PARA FECHAR. O único jeito de sair era a tecla Esc, que
    ///    ninguém adivinha. Regra de interface: todo painel que cobre a tela
    ///    precisa de uma saída VISÍVEL. Aqui ela é um botão rotulado, sempre no
    ///    mesmo canto, montado pela moldura — nenhum minigame pode esquecer de
    ///    pôr o dele.
    ///
    /// 2. O TECLADO NA TELA TAMPAVA A INSTRUÇÃO. Os dois estavam ancorados na
    ///    borda de baixo, cada um sem saber do outro. Consertar aquele caso
    ///    específico não resolveria nada: o décimo minigame repetiria o erro.
    ///
    /// A correção de verdade é tirar dos minigames o direito de posicionar
    /// coisas na TELA. A moldura reserva as faixas fixas (cabeçalho, instrução,
    /// rodapé) e entrega ao minigame apenas <see cref="Conteudo"/> — o retângulo
    /// que sobrou. Quem desenha dentro dele não tem como colidir com o que está
    /// fora, porque não alcança.
    ///
    /// Medidas em pixels da resolução de referência (960x600), em ritmo de 4.
    /// </summary>
    public class PainelDeBancada : MonoBehaviour
    {
        const float AlturaCabecalho = 52f;
        const float AlturaInstrucao = 40f;
        /// <summary>
        /// O rodapé abriga o botão de ação E a linha de dica, empilhados. Na
        /// primeira versão eram 60 pixels para um botão de 44 mais um texto de
        /// 16 — não cabia, e a dica saiu por baixo do botão. É o mesmo erro que
        /// esta moldura existe para impedir, cometido dentro dela: faixa fixa
        /// precisa ter altura suficiente para o que ela promete guardar.
        /// </summary>
        const float AlturaRodape = 82f;
        const float Margem = 24f;
        /// <summary>Mínimo de alvo de toque. Vale para mouse desatento também.</summary>
        const float AlvoMinimo = 44f;

        RectTransform _raiz;
        Text _instrucao;
        Text _dica;
        Button _acao;
        Text _passo;

        /// <summary>
        /// O retângulo que o minigame pode usar. É tudo o que ele recebe: quem
        /// desenha aqui dentro não alcança as faixas fixas.
        /// </summary>
        public RectTransform Conteudo { get; private set; }

        /// <summary>
        /// A resolução de referência do Canvas.
        ///
        /// Serve para dimensionar faixas e fontes, e NÃO para calcular quanto
        /// espaço sobra: com <c>matchWidthOrHeight</c> em 0,5, a tela real mede
        /// outra coisa em unidades de referência, dependendo da proporção do
        /// monitor. Quem precisa saber o tamanho de um retângulo deve medi-lo
        /// (<c>Canvas.ForceUpdateCanvases</c> e depois <c>rect</c>), nunca
        /// deduzi-lo destes dois números.
        /// </summary>
        public const float LarguraDeReferencia = 960f;
        public const float AlturaDeReferencia = 600f;

        /// <summary>Pedido de saída — pelo botão ou pelo Esc.</summary>
        public event Action Fechou;

        public static PainelDeBancada Montar(RectTransform pai, string titulo, string subtitulo)
        {
            var objeto = new GameObject("Painel", typeof(RectTransform));
            objeto.transform.SetParent(pai, false);
            var painel = objeto.AddComponent<PainelDeBancada>();
            painel.Construir(titulo, subtitulo);
            return painel;
        }

        void Construir(string titulo, string subtitulo)
        {
            _raiz = (RectTransform)transform;
            Widgets.Esticar(_raiz);

            // Fundo opaco. Um painel translúcido deixaria o ateliê aparecendo
            // atrás, e o aluno tenta andar no meio do raciocínio.
            var fundo = Widgets.Painel("Fundo", _raiz, Cores.TintaOpaca);
            Widgets.Esticar(fundo);

            MontarCabecalho(titulo, subtitulo);
            MontarInstrucao();
            MontarRodape();

            Conteudo = Widgets.Painel("Conteúdo", _raiz, Color.clear);
            Widgets.Esticar(Conteudo);
            Conteudo.offsetMax = new Vector2(-Margem, -(AlturaCabecalho + AlturaInstrucao));
            Conteudo.offsetMin = new Vector2(Margem, AlturaRodape);
        }

        void MontarCabecalho(string titulo, string subtitulo)
        {
            var faixa = Widgets.Painel("Cabeçalho", _raiz, Cores.TintaClara);
            Widgets.Faixa(faixa, true, AlturaCabecalho);

            var risco = Widgets.Painel("Risco", faixa, Cores.Luz);
            Widgets.Faixa(risco, false, 2f);

            var nome = Widgets.Texto("Título", faixa, 22, TextAnchor.UpperLeft, Cores.Luz);
            Widgets.Esticar(nome.rectTransform);
            nome.rectTransform.offsetMin = new Vector2(Margem, 0f);
            nome.rectTransform.offsetMax = new Vector2(-260f, -8f);
            nome.text = titulo;

            var oficio = Widgets.Texto("Subtítulo", faixa, 13, TextAnchor.LowerLeft, Cores.Neblina);
            Widgets.Esticar(oficio.rectTransform);
            oficio.rectTransform.offsetMin = new Vector2(Margem, 8f);
            oficio.rectTransform.offsetMax = new Vector2(-260f, 0f);
            oficio.text = subtitulo;

            _passo = Widgets.Texto("Passo", faixa, 14, TextAnchor.MiddleRight, Cores.Neblina);
            Widgets.Esticar(_passo.rectTransform);
            _passo.rectTransform.offsetMax = new Vector2(-(Margem + 120f), 0f);

            // A saída. Rotulada, e não um "X" solto: num público de ensino médio
            // o rótulo custa 60 pixels e elimina a dúvida. Fica no canto superior
            // direito, onde todo mundo já procura, e é a mesma em todas as doze.
            var sair = Widgets.Botao("Sair", faixa, "✕  sair", Cores.Madeira, Cores.Papel, 15);
            Widgets.Fixar((RectTransform)sair.transform, new Vector2(1f, 0.5f),
                          new Vector2(-(Margem + 44f), 0f), new Vector2(88f, AlvoMinimo - 8f));
            sair.onClick.AddListener(() => Fechou?.Invoke());
        }

        void MontarInstrucao()
        {
            var faixa = Widgets.Painel("Instrução", _raiz, Color.clear);
            Widgets.Faixa(faixa, true, AlturaInstrucao, AlturaCabecalho);

            _instrucao = Widgets.Texto("Texto", faixa, 17, TextAnchor.MiddleCenter, Cores.Papel);
            Widgets.Esticar(_instrucao.rectTransform);
            _instrucao.rectTransform.offsetMin = new Vector2(Margem, 0f);
            _instrucao.rectTransform.offsetMax = new Vector2(-Margem, 0f);
        }

        void MontarRodape()
        {
            var faixa = Widgets.Painel("Rodapé", _raiz, Color.clear);
            Widgets.Faixa(faixa, false, AlturaRodape);

            // Uma única ação principal, sempre no mesmo lugar. Minigame com três
            // botões de peso igual é minigame em que o aluno não sabe o que
            // fazer primeiro.
            // Botão em cima, dica embaixo, sem se tocarem: 8 de folga, 44 de
            // botão, 8, 14 de texto, 8.
            _acao = Widgets.Botao("Ação", faixa, string.Empty, Cores.Folha, Cores.Papel, 17);
            Widgets.Fixar((RectTransform)_acao.transform, new Vector2(0.5f, 0f),
                          new Vector2(0f, 30f + AlvoMinimo / 2f), new Vector2(300f, AlvoMinimo));
            _acao.gameObject.SetActive(false);

            _dica = Widgets.Texto("Dica", faixa, 13, TextAnchor.LowerCenter, Cores.Neblina);
            Widgets.Faixa(_dica.rectTransform, false, 16f, 8f);
        }

        // ------------------------------------------------------------ pintura

        /// <summary>A linha de instrução. Nunca é coberta por nada.</summary>
        public void Instruir(string texto, Color? cor = null)
        {
            _instrucao.text = texto;
            _instrucao.color = cor ?? Cores.Papel;
        }

        /// <summary>O contador de progresso, no canto do cabeçalho.</summary>
        public void MarcarPasso(string texto) => _passo.text = texto;

        /// <summary>
        /// O mesmo contador, em bolinhas: ● ● ○ ○ ○ em vez de "3 de 5".
        ///
        /// Aqui, e não em cada bancada, porque as onze que contam rodadas
        /// escreviam a mesma frase com palavras ligeiramente diferentes
        /// ("caso 2 de 4", "palavra 3 de 5"). Forma se lê de relance; frase
        /// obriga a parar e ler no meio do raciocínio do jogo.
        ///
        /// <paramref name="extra"/> guarda o que a bancada conta ALÉM da rodada
        /// (a aposta da vez, a tentativa da vez) — duas fileiras de bolinhas
        /// lado a lado se confundiriam, então essa segunda contagem continua
        /// em texto.
        /// </summary>
        public void MarcarPasso(int feitos, int total, string extra = null)
        {
            var pontos = Widgets.Pontos(feitos, total);
            _passo.text = string.IsNullOrEmpty(extra) ? pontos : $"{pontos}   ·   {extra}";
        }

        /// <summary>A linha miúda do rodapé: teclas e lembretes.</summary>
        public void Rodape(string texto) => _dica.text = texto;

        /// <summary>
        /// Liga a ação principal. Passar null esconde o botão — um botão que
        /// não faz nada ainda é pior do que botão nenhum, porque o aluno clica
        /// e conclui que o jogo travou.
        /// </summary>
        public void Acao(string rotulo, Action aoClicar)
        {
            if (rotulo == null || aoClicar == null)
            {
                _acao.gameObject.SetActive(false);
                return;
            }
            _acao.gameObject.SetActive(true);
            Widgets.Rotular(_acao, rotulo);
            _acao.onClick.RemoveAllListeners();
            _acao.onClick.AddListener(() =>
            {
                // O botão principal é clicado centenas de vezes numa aula, e era
                // o único elemento sem resposta própria — o realce padrão do
                // Unity reage ao mouse em cima, não ao clique.
                Widgets.Pulsar((RectTransform)_acao.transform);
                aoClicar();
            });
        }

        /// <summary>Desliga a ação sem escondê-la, para o aluno ver que existe.</summary>
        public void AcaoDisponivel(bool pode)
        {
            _acao.interactable = pode;
            var fundo = _acao.GetComponent<Image>();
            fundo.color = pode ? Cores.Folha : Cores.TintaClara;
            _acao.GetComponentInChildren<Text>().color = pode ? Cores.Papel : Cores.Neblina;
        }

        RectTransform _bloqueio;
        Button _tutorial;

        /// <summary>
        /// Impede o aluno de mexer no minigame enquanto a explicação roda.
        ///
        /// Um véu invisível sobre a área de conteúdo, e a ação principal desligada.
        /// Sem isto, ele clica no tabuleiro no meio da demonstração e a frase
        /// seguinte comenta um estado que já mudou.
        /// </summary>
        public void Bloquear(bool bloquear)
        {
            if (_bloqueio == null)
            {
                _bloqueio = Widgets.Painel("Bloqueio", Conteudo, new Color(0f, 0f, 0f, 0f));
                Widgets.Esticar(_bloqueio);
            }
            _bloqueio.gameObject.SetActive(bloquear);
            // Por cima de tudo o que o minigame montou.
            if (bloquear) _bloqueio.SetAsLastSibling();
            _acao.interactable = !bloquear;
        }

        /// <summary>
        /// Põe o botão de rever a explicação no cabeçalho. Aparece só depois de o
        /// aluno ter visto o tutorial uma vez.
        /// </summary>
        public void OferecerTutorial(Action aoPedir)
        {
            if (_tutorial == null)
            {
                _tutorial = Widgets.Botao("Tutorial", _raiz, "? tutorial",
                                          Cores.TintaClara, Cores.Neblina, 14);
                Widgets.Fixar((RectTransform)_tutorial.transform, new Vector2(1f, 1f),
                              new Vector2(-(Margem + 190f), -(AlturaCabecalho / 2f)),
                              new Vector2(108f, AlvoMinimo - 10f));
            }
            _tutorial.gameObject.SetActive(true);
            _tutorial.onClick.RemoveAllListeners();
            _tutorial.onClick.AddListener(() =>
            {
                _tutorial.gameObject.SetActive(false);
                aoPedir();
            });
        }

        void Update()
        {
            var teclado = Keyboard.current;
            if (teclado != null && teclado.escapeKey.wasPressedThisFrame) Fechou?.Invoke();
        }
    }
}
