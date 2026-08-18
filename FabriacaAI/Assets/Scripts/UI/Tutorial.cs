using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.UI
{
    /// <summary>
    /// Um passo da explicação.
    ///
    /// <see cref="Destaque"/> é uma FUNÇÃO e não um retângulo pronto: quase toda
    /// bancada redesenha a tela inteira a cada jogada, então o botão que o passo
    /// aponta pode ter sido destruído e recriado desde que o roteiro foi escrito.
    /// Perguntar na hora é o que faz o destaque nunca apontar para um objeto
    /// morto.
    /// </summary>
    public class Passo
    {
        public string Texto;
        /// <summary>O que a explicação aponta na tela. Pode ser nulo.</summary>
        public Func<RectTransform> Destaque;
        /// <summary>O que a máquina FAZ ao entrar neste passo. Pode ser nulo.</summary>
        public Action Acao;
        /// <summary>Segundos de espera antes de liberar o botão, para animação terminar.</summary>
        public float Espera;
    }

    /// <summary>
    /// O tutorial de uma bancada: a máquina joga uma vez, explicando.
    ///
    /// Por que executar de verdade em vez de descrever: um texto dizendo "clique
    /// num par para emendá-lo" é uma instrução, e instrução se esquece entre a
    /// leitura e o clique. Uma emenda ACONTECENDO na tela, com a fila de fichas
    /// encurtando na frente do aluno, é uma demonstração — e depois dela ele não
    /// precisa mais do texto.
    ///
    /// O que o tutorial faz, então, é dirigir o minigame de verdade: os passos
    /// chamam os mesmos métodos que o clique do aluno chamaria. Não existe
    /// simulação nem tela falsa; existe a bancada sendo jogada por outra mão.
    ///
    /// TRÊS REGRAS DE DESENHO:
    ///
    ///   · NÃO SE PULA. Só há um botão, e ele avança. O aluno pode sair da
    ///     bancada — isso é sempre permitido —, mas aí o tutorial não conta como
    ///     visto e volta na próxima visita.
    ///   · NÃO SE CLICA POR CIMA. Um véu invisível come os cliques enquanto a
    ///     explicação está no ar, senão o aluno mexe no tabuleiro no meio da
    ///     demonstração e a próxima frase deixa de fazer sentido.
    ///   · O CARTÃO FOGE DO QUE APONTA. Se o destaque está na metade de baixo da
    ///     tela, a explicação vai para cima. Explicação cobrindo justamente a
    ///     peça que ela descreve é o defeito clássico de tutorial.
    /// </summary>
    public class Tutorial : MonoBehaviour
    {
        const float AlturaCartao = 132f;

        IReadOnlyList<Passo> _passos;
        Action _aoTerminar;

        RectTransform _veu;
        RectTransform _cartao;
        Text _texto;
        Text _contador;
        Button _avancar;
        RectTransform _moldura;

        int _atual = -1;
        bool _esperando;

        /// <summary>De onde o destaque do passo atual sai, perguntado a cada quadro.</summary>
        Func<RectTransform> _destaque;
        bool _cartaoNoTopo;

        /// <summary>
        /// Monta e começa. O tutorial se destrói quando o último passo é aceito,
        /// e só então chama <paramref name="aoTerminar"/> — quem marca "visto" é
        /// esse retorno, e é por isso que sair no meio faz o tutorial voltar.
        /// </summary>
        public static Tutorial Rodar(RectTransform pai, IReadOnlyList<Passo> passos,
                                     Action aoTerminar)
        {
            var objeto = new GameObject("Tutorial", typeof(RectTransform));
            objeto.transform.SetParent(pai, false);

            var tutorial = objeto.AddComponent<Tutorial>();
            tutorial._passos = passos;
            tutorial._aoTerminar = aoTerminar;
            tutorial.Construir();
            tutorial.Ir(0);
            return tutorial;
        }

        void Construir()
        {
            var raiz = (RectTransform)transform;
            Widgets.Esticar(raiz);

            // O véu é transparente e come clique. Escurecer a tela deixaria o
            // tabuleiro ilegível justamente quando a explicação fala dele.
            _veu = Widgets.Painel("Véu", raiz, new Color(0f, 0f, 0f, 0f));
            Widgets.Esticar(_veu);

            // A moldura é filha do TUTORIAL e nunca de quem ela cerca. A primeira
            // versão a reparentava para dentro do alvo — economizava a conversão
            // de coordenadas, e custou caro: quase toda bancada redesenha a tela
            // destruindo os filhos, e levava a moldura junto. Aí o tutorial ficava
            // vivo com a moldura morta, estourando a cada quadro, e o passo
            // seguinte morria no meio.
            _moldura = Widgets.Painel("Moldura", raiz, Color.clear);
            _moldura.gameObject.SetActive(false);
            MontarMoldura();

            _cartao = Widgets.Painel("Cartão", raiz, Cores.TintaOpaca);
            Widgets.Faixa(_cartao, false, AlturaCartao, 8f);

            var borda = Widgets.Painel("Borda", _cartao, Cores.Luz);
            Widgets.Faixa(borda, true, 3f);

            var titulo = Widgets.Texto("Título", _cartao, 14, TextAnchor.UpperLeft, Cores.Luz);
            Widgets.Faixa(titulo.rectTransform, true, 20f, 8f);
            titulo.rectTransform.offsetMin = new Vector2(20f, titulo.rectTransform.offsetMin.y);
            titulo.text = "como esta bancada funciona";

            _contador = Widgets.Texto("Contador", _cartao, 13, TextAnchor.UpperRight, Cores.Neblina);
            Widgets.Faixa(_contador.rectTransform, true, 20f, 8f);
            _contador.rectTransform.offsetMax =
                new Vector2(-20f, _contador.rectTransform.offsetMax.y);

            _texto = Widgets.Texto("Texto", _cartao, 18, TextAnchor.UpperLeft, Cores.Papel);
            Widgets.Esticar(_texto.rectTransform);
            _texto.rectTransform.offsetMin = new Vector2(20f, 46f);
            _texto.rectTransform.offsetMax = new Vector2(-200f, -30f);

            _avancar = Widgets.Botao("Avançar", _cartao, "entendi ›", Cores.Folha, Cores.Papel, 17);
            Widgets.Fixar((RectTransform)_avancar.transform, new Vector2(1f, 0.5f),
                          new Vector2(-104f, -8f), new Vector2(176f, 44f));
            _avancar.onClick.AddListener(Avancar);
        }

        // -------------------------------------------------------------- passos

        void Ir(int indice)
        {
            _atual = indice;
            var passo = _passos[indice];

            _texto.text = passo.Texto;
            _contador.text = $"{indice + 1} de {_passos.Count}";
            Widgets.Rotular(_avancar, indice + 1 >= _passos.Count ? "agora é sua vez ›" : "entendi ›");

            // A ação primeiro, o destaque depois: a ação costuma redesenhar a
            // tela, e um destaque montado antes dela apontaria para um objeto que
            // acabou de ser destruído.
            passo.Acao?.Invoke();
            _destaque = passo.Destaque;
            Acompanhar();

            if (passo.Espera > 0f) StartCoroutine(Aguardar(passo.Espera));
        }

        /// <summary>
        /// Trava o botão enquanto a demonstração roda.
        ///
        /// Sem isto, o aluno adianta o passo no meio da animação que ele deveria
        /// estar assistindo — e a frase seguinte comenta algo que ele não viu.
        /// </summary>
        IEnumerator Aguardar(float segundos)
        {
            _esperando = true;
            _avancar.interactable = false;
            Widgets.Rotular(_avancar, "olhe…");

            yield return new WaitForSecondsRealtime(segundos);

            _esperando = false;
            _avancar.interactable = true;
            Widgets.Rotular(_avancar,
                _atual + 1 >= _passos.Count ? "agora é sua vez ›" : "entendi ›");
        }

        void Avancar()
        {
            if (_esperando) return;

            if (_atual + 1 < _passos.Count)
            {
                Ir(_atual + 1);
                return;
            }

            // Some da tela ANTES de avisar. Quem escuta costuma reconstruir o
            // tabuleiro para limpar a demonstração, e reconstruir com o tutorial
            // ainda pendurado na árvore é pedir para ele ser destruído no meio de
            // um método seu.
            var fim = _aoTerminar;
            _aoTerminar = null;
            Destroy(gameObject);
            fim?.Invoke();
        }

        // ------------------------------------------------------------ destaque

        /// <summary>
        /// Recoloca a moldura em volta do alvo do passo atual, e o cartão no lado
        /// oposto da tela.
        ///
        /// Pergunta o alvo de novo — não guarda o retângulo — e roda a cada quadro.
        /// Isso é o que faz o destaque sobreviver ao redesenho: a bancada destrói e
        /// recria o botão, e no quadro seguinte a moldura já achou o novo. Se o
        /// alvo simplesmente deixou de existir, a moldura se apaga em vez de
        /// apontar para um fantasma.
        /// </summary>
        void Acompanhar()
        {
            RectTransform alvo;
            try
            {
                alvo = _destaque?.Invoke();
            }
            catch (MissingReferenceException)
            {
                // O roteiro apontou para dentro de algo que a bancada acabou de
                // destruir — `() => _mesa.GetChild(0)` com a mesa já morta, por
                // exemplo. Perder o destaque é aceitável; derrubar a explicação
                // por causa dele não é.
                alvo = null;
            }

            if (alvo == null)
            {
                if (_moldura.gameObject.activeSelf) _moldura.gameObject.SetActive(false);
                Lado(false);
                return;
            }

            var raiz = (RectTransform)transform;
            var cantos = new Vector3[4];
            alvo.GetWorldCorners(cantos);

            // Converter para o espaço da raiz do tutorial, que cobre a tela toda.
            // É o que permite a moldura ficar fora da árvore da bancada e ainda
            // assim cair exatamente em cima dela.
            var canto0 = (Vector2)raiz.InverseTransformPoint(cantos[0]);
            var canto2 = (Vector2)raiz.InverseTransformPoint(cantos[2]);

            const float folga = 7f;
            _moldura.anchorMin = _moldura.anchorMax = new Vector2(0.5f, 0.5f);
            _moldura.pivot = new Vector2(0.5f, 0.5f);
            _moldura.anchoredPosition = (canto0 + canto2) / 2f;
            _moldura.sizeDelta = new Vector2(Mathf.Abs(canto2.x - canto0.x) + folga * 2f,
                                             Mathf.Abs(canto2.y - canto0.y) + folga * 2f);

            if (!_moldura.gameObject.activeSelf) _moldura.gameObject.SetActive(true);

            // O cartão foge do destaque: alvo na metade de baixo manda a
            // explicação para o topo.
            Lado((cantos[0].y + cantos[1].y) / 2f < Screen.height / 2f);
        }

        /// <summary>
        /// Move o cartão para o topo ou para o pé da tela, e só quando muda.
        ///
        /// O "só quando muda" importa: <see cref="Acompanhar"/> roda a cada quadro,
        /// e remontar a faixa toda vez faria o cartão tremer em cima de um alvo que
        /// está na fronteira do meio da tela.
        /// </summary>
        void Lado(bool noTopo)
        {
            // Nasce no pé da tela, em `Construir`, e `_cartaoNoTopo` nasce falso
            // junto — então a primeira chamada concordando com o estado atual não
            // precisa mexer em nada.
            if (noTopo == _cartaoNoTopo) return;
            _cartaoNoTopo = noTopo;
            Widgets.Faixa(_cartao, noTopo, AlturaCartao, 8f);
        }

        void MontarMoldura()
        {
            foreach (Transform filho in _moldura) Destroy(filho.gameObject);

            // Quatro barras em vez de um retângulo cheio: preenchido, o destaque
            // taparia o que está destacando.
            for (var lado = 0; lado < 4; lado++)
            {
                var barra = Widgets.Painel($"l{lado}", _moldura, Cores.Luz);
                var horizontal = lado < 2;
                barra.anchorMin = horizontal
                    ? new Vector2(0f, lado == 0 ? 1f : 0f)
                    : new Vector2(lado == 2 ? 0f : 1f, 0f);
                barra.anchorMax = horizontal
                    ? new Vector2(1f, lado == 0 ? 1f : 0f)
                    : new Vector2(lado == 2 ? 0f : 1f, 1f);
                barra.pivot = new Vector2(0.5f, 0.5f);
                barra.anchoredPosition = Vector2.zero;
                barra.sizeDelta = horizontal ? new Vector2(0f, 3f) : new Vector2(3f, 0f);
            }
        }

        void Update()
        {
            // Refaz a mira antes de pulsar: entre um quadro e o outro a bancada
            // pode ter redesenhado a peça inteira.
            Acompanhar();
            if (!_moldura.gameObject.activeSelf) return;

            // A moldura pulsa. Borda parada se confunde com decoração da própria
            // peça; pulsando, o olho vai nela em meio segundo.
            var brilho = 0.62f + 0.38f * Mathf.Sin(Time.unscaledTime * 4.2f);
            foreach (Transform barra in _moldura)
            {
                var imagem = barra.GetComponent<Image>();
                if (imagem != null) imagem.color = new Color(Cores.Luz.r, Cores.Luz.g,
                                                             Cores.Luz.b, brilho);
            }
        }
    }
}
