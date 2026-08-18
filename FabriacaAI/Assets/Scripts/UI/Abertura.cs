using System;
using System.Collections.Generic;
using FabricaDeIA.Arte;
using FabricaDeIA.Nucleo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FabricaDeIA.UI
{
    /// <summary>
    /// A abertura: o menu e o raio.
    ///
    /// Uma cena e dois usos. O mesmo cenário — céu, colina, torres de vento, o
    /// aluno e o companheiro — serve de fundo para o menu e de palco para a
    /// animação. Montar duas vezes seria duas vezes o trabalho e duas chances de
    /// ficarem diferentes.
    ///
    /// A ANIMAÇÃO. Seis batidas curtas: o companheiro fala frases perfeitas, a
    /// tempestade chega, o raio o atinge, e a fala dele desmonta na frente do
    /// aluno. É a ÚNICA vez na aula em que se vê uma IA funcionando direito, e é
    /// contra essa lembrança que as doze bancadas trabalham. Sem esta cena, "a
    /// máquina não entende nada" é uma afirmação; com ela, é uma perda.
    ///
    /// Pulável a qualquer momento, e isso não é conveniência: numa turma de
    /// trinta, na segunda aula, alguém já viu — e prender quem já entendeu é o
    /// jeito mais rápido de perder a sala.
    ///
    /// As dezessete batidas de tempo vêm da versão web, que já rodou com duas
    /// turmas. Não mexo nelas sem playtest: o ritmo de uma cena é a coisa mais
    /// fácil de estragar por escrito e a mais difícil de acertar de novo.
    /// </summary>
    public class Abertura : MonoBehaviour
    {
        // ------------------------------------------------------------ roteiro

        struct Batida
        {
            public float Ate;
            public string Fala;
            public bool Narrador;
        }

        static readonly Batida[] Roteiro =
        {
            new() { Ate = 3.2f, Fala = "o sol está bom hoje, vamos para o ateliê" },
            new() { Ate = 6.0f, Fala = "quer que eu leia o diário da estufa?" },
            new() { Ate = 8.4f, Fala = "espera. tem uma tempestade vindo." },
            new() { Ate = 9.6f, Fala = "" },
            new() { Ate = 13.0f, Fala = "…z-zt… o… o… o… o…" },
            new() { Ate = 17.0f, Fala = "ele perdeu as palavras.", Narrador = true }
        };

        const float Fim = 17.0f;
        /// <summary>O instante do clarão.</summary>
        const float Raio = 8.7f;

        // O cenário em frações da tela, medidas DE CIMA PARA BAIXO — como se lê
        // um quadro. As âncoras do Unity contam de baixo para cima, e é `DeCima`
        // que faz a conversão num lugar só.
        //
        // Escrever a fração de cima e ancorá-la direto foi o primeiro erro desta
        // cena: a colina virou uma massa verde cobrindo 60% da tela e as torres
        // de vento foram plantadas atrás dela.
        const float AlturaDoHorizonte = 0.58f;
        const float AlturaDoChao = 0.70f;

        static float DeCima(float fracao) => 1f - fracao;

        Folhas _folhas;
        Action<bool> _aoComecar;

        RectTransform _raiz;
        RectTransform _cenario;
        Image _ceuRuim;
        Image _sol;
        Image _colinaBoa;
        Image _colinaRuim;
        Image _companheiro;
        Image _clarao;
        RectTransform _boltRaio;
        RectTransform _chuva;
        RectTransform _menu;
        Text _fala;
        Text _pular;

        readonly List<RectTransform> _pas = new();
        readonly List<RectTransform> _gotas = new();
        readonly List<Image> _faiscas = new();

        float _tempo = -1f;
        bool _rodando;
        Vector2 _pousoDoCompanheiro;

        // -------------------------------------------------------------- montar

        public static Abertura Montar(RectTransform pai, Folhas folhas, Action<bool> aoComecar)
        {
            var objeto = new GameObject("Abertura", typeof(RectTransform));
            objeto.transform.SetParent(pai, false);
            var abertura = objeto.AddComponent<Abertura>();
            abertura._folhas = folhas;
            abertura._aoComecar = aoComecar;
            abertura.Construir();
            return abertura;
        }

        void Construir()
        {
            _raiz = (RectTransform)transform;
            Widgets.Esticar(_raiz);

            _cenario = Widgets.Painel("Cenário", _raiz, Color.clear);
            Widgets.Esticar(_cenario);

            MontarCeu();
            MontarTorres();
            MontarColina();
            MontarGente();
            MontarChuva();
            MontarRaio();
            MontarFala();
            MontarMenu();

            Pintar(0f);
        }

        /// <summary>
        /// Os dois céus, um sobre o outro. O de tempestade entra por opacidade —
        /// é o que dá transição contínua sem shader e sem gradiente calculado a
        /// cada quadro.
        /// </summary>
        void MontarCeu()
        {
            var bom = Imagem("CéuBom", _cenario, "ceu_bom");
            Widgets.Esticar((RectTransform)bom.transform);

            _ceuRuim = Imagem("CéuRuim", _cenario, "ceu_ruim");
            Widgets.Esticar((RectTransform)_ceuRuim.transform);

            _sol = Imagem("Sol", _cenario, "sol");
            Fracao((RectTransform)_sol.transform, 0.78f, 0.19f, 0.15f, 0.26f);
        }

        /// <summary>
        /// As torres de vento. As pás são filhas do topo do mastro e giram por
        /// rotação do retângulo — três por torre, defasadas em 120 graus.
        ///
        /// Elas girando são o único movimento da cena antes do raio, e é o que
        /// faz o quadro parecer vivo em vez de ilustração parada.
        /// </summary>
        void MontarTorres()
        {
            var torres = new[] { (x: 0.12f, alto: 0.30f), (x: 0.24f, alto: 0.22f), (x: 0.88f, alto: 0.26f) };

            for (var t = 0; t < torres.Length; t++)
            {
                var (x, alto) = torres[t];

                var mastro = Imagem($"Mastro{t}", _cenario, "mastro_bom");

                // O topo, onde o rotor mora. É o único número daqui que não pode
                // mudar: mexer nele moveria as pás.
                var topo = DeCima(AlturaDoHorizonte) - 0.02f + alto;

                var reto = (RectTransform)mastro.transform;

                // O pé desce até a borda de baixo da tela, e não até a linha do
                // horizonte.
                //
                // A versão anterior plantava o mastro no HORIZONTE (0,40 da tela),
                // supondo que fosse ali o chão. Não é: o chão que se vê é a
                // silhueta desenhada dentro de `colina_boa.png`, e ela é bem mais
                // baixa. Medindo os pixels do PNG nas colunas exatas das três
                // torres, a superfície está a 0,315, 0,328 e 0,206 da altura da
                // tela. Sobravam de 43 a 116 pixels de ar embaixo de cada torre, e
                // o que aparecia era um rotor pendurado no céu.
                //
                // Descer até 0 não deixa mastro sobrando à vista: MontarColina roda
                // DEPOIS desta função, então a colina é irmã posterior e desenha por
                // cima. O excesso fica enterrado — que é como torre se planta.
                reto.anchorMin = new Vector2(x, 0f);
                reto.anchorMax = new Vector2(x, topo);
                reto.pivot = new Vector2(0.5f, 0.5f);
                reto.offsetMin = new Vector2(-4f, 0f);
                reto.offsetMax = new Vector2(4f, 0f);

                var eixo = Widgets.Painel($"Eixo{t}", _cenario, Color.clear);
                eixo.anchorMin = eixo.anchorMax = new Vector2(x, topo);
                eixo.pivot = new Vector2(0.5f, 0.5f);
                eixo.anchoredPosition = Vector2.zero;
                eixo.sizeDelta = Vector2.zero;

                for (var i = 0; i < 3; i++)
                {
                    var pa = Imagem($"Pá{t}_{i}", eixo, "pa_boa");
                    var r = (RectTransform)pa.transform;
                    r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                    // Pivô na ponta esquerda: é o eixo do rotor, e é em volta
                    // dele que a pá tem que girar.
                    r.pivot = new Vector2(0f, 0.5f);
                    r.anchoredPosition = Vector2.zero;
                    r.sizeDelta = new Vector2(46f, 12f);
                    r.localRotation = Quaternion.Euler(0f, 0f, i * 120f);
                    _pas.Add(r);
                }
            }
        }

        void MontarColina()
        {
            _colinaBoa = Imagem("ColinaBoa", _cenario, "colina_boa");
            Colina((RectTransform)_colinaBoa.transform);

            _colinaRuim = Imagem("ColinaRuim", _cenario, "colina_ruim");
            Colina((RectTransform)_colinaRuim.transform);
        }

        static void Colina(RectTransform reto)
        {
            reto.anchorMin = new Vector2(0f, 0f);
            reto.anchorMax = new Vector2(1f, DeCima(AlturaDoHorizonte));
            reto.pivot = new Vector2(0.5f, 0.5f);
            reto.offsetMin = Vector2.zero;
            reto.offsetMax = Vector2.zero;
        }

        void MontarGente()
        {
            var jogador = new GameObject("Jogador", typeof(RectTransform), typeof(Image));
            jogador.transform.SetParent(_cenario, false);
            // De perfil, virado para o companheiro: os dois estão conversando.
            jogador.GetComponent<Image>().sprite = _folhas.Quadro("jogador", Folhas.Direita, 0);
            jogador.GetComponent<Image>().preserveAspect = true;
            // Os dois ficam à esquerda do centro: o menu ocupa o meio da tela, e
            // personagem atrás de botão não conta história nenhuma.
            Fracao((RectTransform)jogador.transform, 0.17f, AlturaDoChao - 0.075f, 0.055f, 0.15f);

            _companheiro = Imagem("Companheiro", _cenario, "companheiro_bom");
            Fracao((RectTransform)_companheiro.transform, 0.28f, AlturaDoChao - 0.20f, 0.075f, 0.13f);
            _pousoDoCompanheiro = ((RectTransform)_companheiro.transform).anchoredPosition;

            // Duas faíscas, que piscam alternadas depois do golpe.
            for (var i = 0; i < 2; i++)
            {
                var faisca = Imagem($"Faísca{i}", _cenario, "faisca");
                Fracao((RectTransform)faisca.transform,
                       0.33f + i * 0.03f, AlturaDoChao - 0.11f - i * 0.04f, 0.028f, 0.05f);
                faisca.enabled = false;
                _faiscas.Add(faisca);
            }
        }

        /// <summary>
        /// A chuva: cinquenta gotas que caem e voltam ao topo. Cinquenta é o
        /// número em que a tela lê como chuva sem virar cortina.
        /// </summary>
        void MontarChuva()
        {
            _chuva = Widgets.Painel("Chuva", _cenario, Color.clear);
            Widgets.Esticar(_chuva);

            for (var i = 0; i < 50; i++)
            {
                var gota = Imagem($"g{i}", _chuva, "gota");
                var r = (RectTransform)gota.transform;
                r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(5f, 16f);
                // Espalhadas por ruído, não por passo fixo: chuva em grade
                // regular denuncia o truque na hora.
                r.anchoredPosition = new Vector2(UnityEngine.Random.Range(0f, 1000f),
                                                 -UnityEngine.Random.Range(0f, 620f));
                _gotas.Add(r);
            }
            _chuva.gameObject.SetActive(false);
        }

        void MontarRaio()
        {
            _boltRaio = Widgets.Painel("Raio", _cenario, Color.clear);
            Widgets.Esticar(_boltRaio);
            _boltRaio.gameObject.SetActive(false);

            _clarao = Widgets.Painel("Clarão", _cenario, new Color(1f, 0.98f, 0.925f, 0f))
                             .GetComponent<Image>();
            Widgets.Esticar((RectTransform)_clarao.transform);
        }

        void MontarFala()
        {
            _fala = Widgets.Texto("Fala", _raiz, 22, TextAnchor.LowerCenter, Cores.Papel);
            Widgets.Faixa(_fala.rectTransform, false, 76f, 92f);
            _fala.rectTransform.offsetMin = new Vector2(80f, _fala.rectTransform.offsetMin.y);
            _fala.rectTransform.offsetMax = new Vector2(-80f, _fala.rectTransform.offsetMax.y);
            _fala.text = string.Empty;
            // Fica escondida enquanto o menu está de pé: a primeira fala do
            // companheiro por baixo dos botões do título vaza a cena antes dela.
            _fala.gameObject.SetActive(false);

            var botao = Widgets.Botao("Pular", _raiz, "pular ›", Cores.Tinta, Cores.Neblina, 15);
            Widgets.Fixar((RectTransform)botao.transform, new Vector2(1f, 0f),
                          new Vector2(-90f, 34f), new Vector2(120f, 38f));
            botao.onClick.AddListener(() => Terminar());
            _pular = botao.GetComponentInChildren<Text>();
            botao.gameObject.SetActive(false);
            _pular.name = "rótulo do pular";
            _pularBotao = botao.gameObject;
        }

        GameObject _pularBotao;

        // ---------------------------------------------------------------- menu

        void MontarMenu()
        {
            _menu = Widgets.Painel("Menu", _raiz, new Color(0.106f, 0.122f, 0.165f, 0.62f));
            Widgets.Esticar(_menu);

            var titulo = Widgets.Texto("Título", _menu, 64, TextAnchor.UpperCenter, Cores.Luz);
            Widgets.Faixa(titulo.rectTransform, true, 80f, 60f);
            titulo.text = "Fábrica de IA";

            var linha = Widgets.Painel("Risco", _menu, Cores.Luz);
            Widgets.Fixar(linha, new Vector2(0.5f, 1f), new Vector2(0f, -142f), new Vector2(360f, 2f));

            var chamada = Widgets.Texto("Chamada", _menu, 19, TextAnchor.UpperCenter, Cores.Papel);
            Widgets.Faixa(chamada.rectTransform, true, 52f, 156f);
            chamada.text = "um raio queimou a máquina que falava.\n" +
                           "doze bancadas, doze peças — reconstrua a fala dela.";

            var feitas = Progresso.Atual.Concluidas;
            var comecou = feitas > 0;

            // O botão de cima é sempre o que a pessoa mais provavelmente quer:
            // continuar para quem já jogou, começar para quem chegou agora.
            var y = -244f;

            if (comecou)
            {
                Opcao(_menu, "continuar a aula", y, Cores.Folha, () => Terminar());
                y -= 58f;

                // Quem já terminou a aula precisa de um caminho de volta ao
                // diploma: fechar a aba antes de baixar não pode custar a atividade.
                if (Progresso.Atual.AulaCompleta)
                {
                    Opcao(_menu, "meu diploma (PDF)", y, Cores.Luz, () => Terminar(true));
                    y -= 58f;
                }

                Opcao(_menu, "ver a abertura de novo", y, Cores.Madeira, Comecar);
                y -= 58f;
                Opcao(_menu, "recomeçar do zero", y, Cores.TintaClara, Recomecar);
            }
            else
            {
                Opcao(_menu, "começar", y, Cores.Folha, Comecar);
                y -= 58f;
                Opcao(_menu, "pular a abertura", y, Cores.TintaClara, () => Terminar());
            }

            var placar = Widgets.Texto("Placar", _menu, 15, TextAnchor.LowerCenter, Cores.Neblina);
            Widgets.Faixa(placar.rectTransform, false, 44f, 24f);
            placar.text = comecou
                ? $"{feitas} de 12 peças montadas\nsetas ou WASD para andar · E para falar com um mestre"
                : "setas ou WASD para andar · E para falar com um mestre";
        }

        static void Opcao(RectTransform pai, string rotulo, float y, Color cor, Action aoClicar)
        {
            var botao = Widgets.Botao(rotulo, pai, rotulo, cor, Cores.Papel, 20);
            Widgets.Fixar((RectTransform)botao.transform, new Vector2(0.5f, 1f),
                          new Vector2(0f, y), new Vector2(340f, 48f));
            botao.onClick.AddListener(() => aoClicar());
        }

        void Recomecar()
        {
            Progresso.Reiniciar();
            Comecar();
        }

        /// <summary>
        /// Pula direto para um instante da cena e congela lá.
        ///
        /// Serve à ferramenta de retrato: a animação dura dezessete segundos e os
        /// momentos que decidem se ela funciona — o raio, o companheiro no chão —
        /// duram frações. Fotografar "a abertura" sem poder escolher o instante
        /// seria fotografar sempre o primeiro segundo.
        ///
        /// Funciona porque <see cref="Pintar"/> é uma função do tempo para a
        /// tela, sem estado entre quadros: qualquer instante pode ser desenhado
        /// direto, sem passar pelos anteriores.
        /// </summary>
        public void Congelar(float instante)
        {
            _menu.gameObject.SetActive(false);
            _pularBotao.SetActive(true);
            _fala.gameObject.SetActive(true);
            _rodando = false;
            _tempo = instante;
            Pintar(instante);
        }

        void Comecar()
        {
            _menu.gameObject.SetActive(false);
            _pularBotao.SetActive(true);
            _fala.gameObject.SetActive(true);
            _tempo = 0f;
            _rodando = true;
        }

        /// <summary>
        /// Sai da abertura. O argumento diz se o aluno pediu o diploma em vez do
        /// ateliê — é o único destino alternativo que o menu oferece.
        /// </summary>
        void Terminar(bool diploma = false)
        {
            if (_aoComecar == null) return;
            var seguir = _aoComecar;
            _aoComecar = null;
            seguir(diploma);
            Destroy(gameObject);
        }

        // ------------------------------------------------------------ animação

        void Update()
        {
            // Pás girando também no menu: cenário parado atrás de um título faz
            // a tela parecer uma imagem, não um jogo esperando.
            var relogio = _rodando ? _tempo : Time.unscaledTime;
            for (var i = 0; i < _pas.Count; i++)
            {
                var torre = i / 3;
                var pa = i % 3;
                _pas[i].localRotation = Quaternion.Euler(
                    0f, 0f, (relogio * 34f + torre * 40f + pa * 120f) % 360f);
            }

            if (!_rodando)
            {
                // Repinta o instante em que a cena está: zero no menu, e o que a
                // ferramenta de retrato pediu quando congelada. Fixar zero aqui
                // apagaria o quadro congelado no primeiro Update.
                Pintar(Mathf.Max(0f, _tempo));
                return;
            }

            var teclado = Keyboard.current;
            if (teclado != null &&
                (teclado.escapeKey.wasPressedThisFrame ||
                 teclado.spaceKey.wasPressedThisFrame ||
                 teclado.enterKey.wasPressedThisFrame))
            {
                Terminar();
                return;
            }

            _tempo += Time.unscaledDeltaTime;
            Pintar(_tempo);

            if (_tempo >= Fim) Terminar();
        }

        /// <summary>
        /// Desenha o instante <paramref name="t"/> da cena.
        ///
        /// Uma função só, do tempo para a tela, sem estado escondido entre
        /// quadros. É o que permite mostrar o menu no instante zero e a animação
        /// no instante que for, com o mesmo código — e é o que torna a cena
        /// pulável sem deixar nada pela metade.
        /// </summary>
        void Pintar(float t)
        {
            // A tempestade é o relógio da cena: começa a fechar em 4s e leva 4,5s.
            var escuro = Mathf.Clamp01((t - 4f) / 4.5f);

            _ceuRuim.color = new Color(1f, 1f, 1f, escuro);
            _colinaRuim.color = new Color(1f, 1f, 1f, escuro);
            _sol.color = new Color(1f, 1f, 1f, 0.9f * (1f - escuro));

            var caiu = t > Raio;
            _companheiro.sprite = _folhas.Peca(caiu ? "companheiro_ruim" : "companheiro_bom");

            var reto = (RectTransform)_companheiro.transform;
            if (caiu)
            {
                // Tombado de lado, no chão, apagando.
                reto.anchoredPosition = _pousoDoCompanheiro + new Vector2(6f, -54f);
                reto.localRotation = Quaternion.Euler(0f, 0f, -28f);
            }
            else
            {
                // Flutuando: o balanço é o que diz "no ar" sem precisar de sombra.
                reto.anchoredPosition = _pousoDoCompanheiro
                                      + new Vector2(0f, Mathf.Sin(t * 2.4f) * 7f);
                reto.localRotation = Quaternion.identity;
            }

            foreach (var faisca in _faiscas) faisca.enabled = false;
            if (caiu)
            {
                // Estalos intermitentes: ele não morreu, está gaguejando.
                var qual = Mathf.FloorToInt(t * 6f) % 3;
                if (qual < _faiscas.Count) _faiscas[qual].enabled = true;
            }

            DesenharRaio(t);
            Chover(escuro);
            Falar(t);
        }

        void DesenharRaio(float t)
        {
            var perto = t > Raio - 0.12f && t < Raio + 0.45f;
            _boltRaio.gameObject.SetActive(perto);

            var forca = perto ? Mathf.Max(0f, 1f - Mathf.Abs(t - Raio) / 0.45f) : 0f;
            _clarao.color = new Color(1f, 0.98f, 0.925f, forca * 0.85f);

            if (!perto) return;

            // O risco é redesenhado a cada quadro com quebras novas — raio parado
            // não parece raio.
            foreach (Transform filho in _boltRaio) Destroy(filho.gameObject);

            Canvas.ForceUpdateCanvases();
            var tela = _boltRaio.rect.size;
            var alvoX = tela.x * (0.28f - 0.5f);
            var de = new Vector2(alvoX - 30f, tela.y * 0.5f);
            var chao = -tela.y * 0.5f + tela.y * (1f - AlturaDoChao) + 40f;

            var y = de.y;
            var x = de.x;
            while (y > chao)
            {
                var proximoY = y - tela.y * 0.075f;
                var proximoX = x + UnityEngine.Random.Range(-0.4f, 0.6f) * 36f;
                Risco(new Vector2(x, y), new Vector2(proximoX, Mathf.Max(chao, proximoY)));
                x = proximoX;
                y = proximoY;
            }
        }

        void Risco(Vector2 a, Vector2 b)
        {
            var delta = b - a;
            var risco = Widgets.Painel("r", _boltRaio, new Color(1f, 0.98f, 0.925f));
            Widgets.Fixar(risco, new Vector2(0.5f, 0.5f), (a + b) / 2f,
                          new Vector2(delta.magnitude, 6f));
            risco.localRotation = Quaternion.Euler(
                0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        void Chover(float escuro)
        {
            var chovendo = escuro > 0.5f;
            if (_chuva.gameObject.activeSelf != chovendo) _chuva.gameObject.SetActive(chovendo);
            if (!chovendo) return;

            Canvas.ForceUpdateCanvases();
            var tela = _chuva.rect.size;
            var limite = tela.y * AlturaDoHorizonte;

            foreach (var gota in _gotas)
            {
                var p = gota.anchoredPosition;
                p.y -= 900f * Time.unscaledDeltaTime;
                p.x -= 130f * Time.unscaledDeltaTime;
                if (p.y < -limite)
                {
                    p.y = 0f;
                    p.x = UnityEngine.Random.Range(0f, tela.x + 200f);
                }
                if (p.x < -40f) p.x += tela.x + 200f;
                gota.anchoredPosition = p;
            }
        }

        void Falar(float t)
        {
            var batida = Roteiro[Roteiro.Length - 1];
            foreach (var b in Roteiro)
            {
                if (t >= b.Ate) continue;
                batida = b;
                break;
            }

            if (string.IsNullOrEmpty(batida.Fala))
            {
                _fala.text = string.Empty;
                return;
            }

            // O companheiro fala entre aspas; o narrador, sem. É a única marca
            // que separa quem está na cena de quem está contando.
            _fala.color = batida.Narrador ? Cores.Neblina : Cores.Papel;
            _fala.text = batida.Narrador ? batida.Fala : $"“{batida.Fala}”";
        }

        // ---------------------------------------------------------- utilidades

        Image Imagem(string nome, Transform pai, string peca)
        {
            var objeto = new GameObject(nome, typeof(RectTransform), typeof(Image));
            objeto.transform.SetParent(pai, false);
            var imagem = objeto.GetComponent<Image>();
            imagem.sprite = _folhas.Peca(peca);
            return imagem;
        }

        /// <summary>Ancora pelo centro, em frações da tela — serve a qualquer proporção.</summary>
        static void Fracao(RectTransform reto, float x, float y, float largura, float altura)
        {
            reto.anchorMin = new Vector2(x - largura / 2f, 1f - y - altura / 2f);
            reto.anchorMax = new Vector2(x + largura / 2f, 1f - y + altura / 2f);
            reto.pivot = new Vector2(0.5f, 0.5f);
            reto.offsetMin = Vector2.zero;
            reto.offsetMax = Vector2.zero;
        }
    }

}
