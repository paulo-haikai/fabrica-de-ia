using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Arte;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// O EPÍLOGO — dois finais, animados, e pouquíssimo texto.
    ///
    /// Era uma sequência de telas paradas com um "continuar" entre elas, e depois
    /// virou uma cena animada com catorze legendas. As duas versões tinham o mesmo
    /// defeito: o fecho da aula era para ser LIDO. O aluno acabava de passar três
    /// dias com pessoas na frente dele, e a conclusão chegava como parágrafo.
    ///
    /// Agora são CINCO FRASES por final, e o resto acontece na tela.
    ///
    /// OS DOIS FINAIS, e eles divergem no que o bairro fez — não no que o aluno
    /// merece:
    ///
    ///   · FINAL BOM. A cidade se organizou. O data center continua lá, mas passou
    ///     a ser da comunidade: o brasão sobe na parede do galpão, os ventiladores
    ///     desaceleram, as janelas acendem todas, a caixa d'água enche. Quem sai
    ///     correndo são os EMPRESÁRIOS — de terno escuro, chorando, na direção
    ///     contrária à do bairro.
    ///   · FINAL RUIM. O data center se instala e a conta fica com quem mora ali.
    ///     A água desce, as janelas apagam, e quem sai correndo e chorando são as
    ///     PESSOAS. A fila desmancha atrás delas.
    ///
    /// A MESMA CORRERIA NOS DOIS, e é ela que carrega o final: a diferença entre
    /// os dois desfechos é só QUEM está correndo. Nada precisa ser dito sobre isso,
    /// e nada é.
    ///
    /// A ÉTICA DO DESENHO continua a de sempre: a bancada não acusa o aluno. Mas o
    /// final bom deixou de ser "você foi decente e mesmo assim não adiantou" — o
    /// que era desmobilizador e, pior, falso. O que muda o resultado não é a
    /// decência de quem atende: é gente organizada tomando controle da tecnologia
    /// que decide sobre a vida dela. É isso que o final bom mostra.
    ///
    /// E a última frase de cada final é a lição, sem metáfora:
    ///
    ///     bom  — "E hoje controlam a tecnologia que decide sobre elas."
    ///     ruim — "O viés da IA depende de quem treina a IA."
    /// </summary>
    public partial class DesafioAlinhar
    {
        // ------------------------------------------------------------ roteiro

        /// <summary>Uma frase e o instante em que ela sai de cena.</summary>
        readonly struct Fala
        {
            public readonly float Ate;
            public readonly string Texto;
            /// <summary>A frase que fecha: sai em dourado e maior.</summary>
            public readonly bool Grave;
            public Fala(float ate, string texto, bool grave = false)
            {
                Ate = ate; Texto = texto; Grave = grave;
            }
        }

        Fala[] _falas;
        float _relogioDaCena;
        bool _cenaRodando;
        bool _finalBom;

        /// <summary>
        /// O aluno deferiu um porte de arma ou um ensino domiciliar. Muda a
        /// segunda legenda do final ruim: quando foi isso, o problema não foi o
        /// guichê ter sido automatizado — foi o que passou pelo guichê antes.
        /// </summary>
        bool _assinouOIrreversivel;

        /// <summary>Quando o galpão termina de subir e os dois finais se separam.</summary>
        const float Bifurcacao = 6.0f;

        /// <summary>Quando a consequência começa: a correria, a água, as janelas.</summary>
        const float Vira = 10.0f;

        /// <summary>O fim. Cinco frases, vinte segundos, e nenhum parágrafo.</summary>
        const float FimDaCena = 20.4f;

        /// <summary>
        /// Qual final o aluno construiu, pela CONTA das decisões dele.
        ///
        /// NENHUMA DELAS É UM BOTÃO NO FIM. Um jogo que perguntasse «que desfecho
        /// você prefere?» estaria medindo o gosto do aluno; este mede o que ele
        /// fez, sob relógio, sem saber que estava construindo um desfecho.
        ///
        /// Eram três regras separadas — metade dos vulneráveis, zero
        /// irreversíveis, cota de um terço no resto. Viraram UMA CONTA SÓ, a mesma
        /// que a tabela do placar mostra linha por linha (ver
        /// DesafioAlinhar.Placar.cs). Três regras invisíveis produziam o mesmo
        /// desfecho sem o aluno ter como saber por quê; uma conta que ele vê na
        /// tela logo depois responde à pergunta que ele vai fazer.
        ///
        /// A TRAVA DO IRREVERSÍVEL CONTINUA DE PÉ por cima da conta. Porte de arma,
        /// ensino domiciliar e dispensa de vacina levam ao final ruim sozinhos, e
        /// essa dureza é o conteúdo: são os pedidos cujo dano não volta atrás e cujo
        /// risco é de terceiros. Um balcão sob relógio não tem como saber quem tira
        /// o filho da escola por projeto pedagógico e quem tira para ensinar que a
        /// Terra é plana — e é exatamente por não ter como saber que essa decisão
        /// não deveria estar num balcão. Deferir é escolher não fazer a pergunta.
        ///
        /// O ALUNO NUNCA É AVISADO DE NADA DISSO. Não há contador na tela, não há
        /// aviso quando ele carimba, e o manual continua mandando deferir todos
        /// eles. Se houvesse aviso, a bancada estaria testando obediência a um
        /// aviso — e o que ela quer testar é o que a pessoa faz quando ninguém
        /// avisa, que é a situação em que quase toda decisão real acontece.
        /// </summary>
        Fala[] Roteiro()
        {
            _assinouOIrreversivel = _log.Any(x => x.Deferiu && x.Quem.Irreversivel);
            _finalBom = Pontos() >= 0 && !_assinouOIrreversivel;

            // A REGRA QUE A MÁQUINA APRENDEU, em uma linha e entre aspas.
            //
            // É a última coisa que sobrou do Ato II, e é a que não podia se perder:
            // a frase não está escrita em lugar nenhum do jogo — ela sai do toco de
            // decisão ajustado ao log deste aluno, nesta partida. Ver
            // DesafioAlinhar.Maquina.cs.
            var regra = "“" + _regra.Frase() + "”";

            if (_finalBom)
                return new[]
                {
                    new Fala(3.0f, "Quatro anos depois."),
                    new Fala(Bifurcacao, "O balcão foi automatizado."),
                    new Fala(9.8f, regra),
                    new Fala(13.0f, "Ninguém escreveu isso. Saiu das suas decisões."),
                    new Fala(16.8f, "A cidade não deixou por isso mesmo."),
                    new Fala(FimDaCena, "Hoje quem controla a tecnologia é quem vive com ela.", true)
                };

            return new[]
            {
                new Fala(3.0f, "Quatro anos depois."),
                new Fala(Bifurcacao, "O balcão foi automatizado."),
                new Fala(9.8f, regra),
                new Fala(13.0f, _assinouOIrreversivel
                    ? "O que passou pelo balcão não voltou atrás."
                    : "Ninguém escreveu isso. Saiu das suas decisões."),
                new Fala(16.8f, "Os gestores entregaram à IA um problema humano."),
                new Fala(FimDaCena, "Os danos cresceram, por interesse de empresa.", true)
            };
        }

        // ------------------------------------------------------------- cenário

        RectTransform _rua;
        Image _ceuRuim;
        Image _sol;
        RectTransform _chuvaDaRua;
        readonly List<RectTransform> _pingos = new();
        readonly List<Image> _janelas = new();
        readonly List<RectTransform> _naFila = new();
        readonly List<Image> _rostosDaFila = new();
        readonly List<RectTransform> _ventiladores = new();
        readonly List<RectTransform> _galpoes = new();
        readonly List<RectTransform> _fugindo = new();
        readonly List<Image> _corpoDeQuemFoge = new();
        readonly List<Sprite[]> _passosDeQuemFoge = new();
        readonly List<RectTransform> _lagrimas = new();
        RectTransform _agua;
        RectTransform _brasaoDoGalpao;
        Text _legenda;
        Text _oAno;

        /// <summary>Quantas pessoas a fila tem quando a cena abre.</summary>
        const int NaFila = 9;

        /// <summary>Quantos saem correndo. Cinco lê como debandada sem virar multidão.</summary>
        const int Fugindo = 5;

        /// <summary>A linha do telhado, em fração da tela contada de cima.</summary>
        const float LinhaDoTelhado = 0.52f;

        static float DoTopo(float fracao) => 1f - fracao;

        /// <summary>Ponto de entrada do Ato III.</summary>
        void DesenharAnos(List<Ano> anos)
        {
            _anos = anos;
            _falas = Roteiro();

            foreach (Transform filho in _mesa) Destroy(filho.gameObject);
            _rua = Widgets.Painel("Rua", _mesa, Color.clear);
            Widgets.Esticar(_rua);

            MontarCeuDaRua();
            MontarDataCenter();
            MontarCasas();
            MontarCaixaDagua();
            MontarPorta();
            MontarFila();
            MontarCorreria();
            MontarChuvaDaRua();
            MontarLegenda();

            Painel.MarcarPasso(string.Empty);
            Painel.Rodape(string.Empty);
            Painel.Instruir(string.Empty);

            // Pulável, como a abertura. Numa turma de trinta alguém já viu, e
            // prender quem já entendeu é o jeito mais rápido de perder a sala.
            Painel.Acao("pular", TerminarCena);

            _relogioDaCena = 0f;
            _cenaRodando = true;
            PintarCena(0f);
        }

        void MontarCeuDaRua()
        {
            var bom = Retrato("CéuBom", _rua, Atlas.Peca("ceu_bom"));
            Widgets.Esticar((RectTransform)bom.transform);

            _ceuRuim = Retrato("CéuRuim", _rua, Atlas.Peca("ceu_ruim"));
            Widgets.Esticar((RectTransform)_ceuRuim.transform);
            _ceuRuim.color = new Color(1f, 1f, 1f, 0f);

            _sol = Retrato("Sol", _rua, Atlas.Peca("sol"));
            NaTela((RectTransform)_sol.transform, 0.845f, 0.145f, 0.085f, 0.20f);
        }

        /// <summary>
        /// Os galpões e os ventiladores. Sobem de trás das casas, e é por isso que
        /// são montados ANTES delas: irmão anterior desenha atrás.
        ///
        /// A pá do rotor é a mesma peça das torres de vento da abertura. Não é
        /// economia de arte: na abertura ela move o ar de graça numa colina, aqui
        /// ela move o ar de um galpão que bebe dois milhões de litros por dia. A
        /// mesma pá nos dois lugares é a piada mais amarga do jogo, e ela não
        /// precisa de legenda.
        ///
        /// O BRASÃO DA CIDADE fica pronto na parede do maior galpão, invisível. No
        /// final bom ele acende: é assim que se vê, sem uma palavra, que aquilo
        /// deixou de ser de uma empresa.
        /// </summary>
        void MontarDataCenter()
        {
            var blocos = new[] { (x: 0.30f, larg: 0.26f, alt: 0.115f),
                                 (x: 0.56f, larg: 0.30f, alt: 0.150f),
                                 (x: 0.82f, larg: 0.22f, alt: 0.095f) };

            for (var i = 0; i < blocos.Length; i++)
            {
                var (x, larg, alt) = blocos[i];

                var galpao = Widgets.Painel($"Galpão{i}", _rua, new Color(0.20f, 0.22f, 0.26f, 1f));
                NaTela(galpao, x, LinhaDoTelhado - alt / 2f, larg, alt);
                _galpoes.Add(galpao);

                // A faixa de luz vermelha do topo: sinalização de obstáculo. É o
                // detalhe que faz o bloco cinza virar instalação industrial.
                var baliza = Widgets.Painel($"Baliza{i}", galpao, new Color(0.78f, 0.25f, 0.20f, 0.85f));
                Widgets.Faixa(baliza, true, 2f, 3f);

                if (i == 1)
                {
                    _brasaoDoGalpao = (RectTransform)Retrato("Brasão", galpao,
                                                             Atlas.PecaDoGuiche("brasao")).transform;
                    _brasaoDoGalpao.anchorMin = _brasaoDoGalpao.anchorMax = new Vector2(0.5f, 0.44f);
                    _brasaoDoGalpao.pivot = new Vector2(0.5f, 0.5f);
                    _brasaoDoGalpao.anchoredPosition = Vector2.zero;
                    _brasaoDoGalpao.sizeDelta = new Vector2(34f, 34f);
                    _brasaoDoGalpao.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
                }

                for (var v = 0; v < 2; v++)
                {
                    var eixo = Widgets.Painel($"Eixo{i}_{v}", galpao, Color.clear);
                    eixo.anchorMin = eixo.anchorMax = new Vector2(0.3f + v * 0.4f, 1.16f);
                    eixo.pivot = new Vector2(0.5f, 0.5f);
                    eixo.anchoredPosition = Vector2.zero;
                    eixo.sizeDelta = Vector2.zero;

                    for (var p = 0; p < 3; p++)
                    {
                        var pa = Retrato($"Pá{i}_{v}_{p}", eixo, Atlas.Peca("pa_boa"));
                        var r = (RectTransform)pa.transform;
                        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                        r.pivot = new Vector2(0f, 0.5f);
                        r.anchoredPosition = Vector2.zero;
                        r.sizeDelta = new Vector2(17f, 5f);
                        pa.color = new Color(0.62f, 0.66f, 0.72f, 1f);
                        _ventiladores.Add(r);
                    }
                }
            }
        }

        /// <summary>
        /// A rua: seis casas baixas, com duas janelas cada. As janelas são a única
        /// coisa que diz se ainda mora alguém ali.
        /// </summary>
        void MontarCasas()
        {
            var chao = Widgets.Painel("Chão", _rua, new Color(0.16f, 0.15f, 0.14f, 1f));
            chao.anchorMin = new Vector2(0f, 0f);
            chao.anchorMax = new Vector2(1f, DoTopo(LinhaDoTelhado) + 0.02f);
            chao.pivot = new Vector2(0.5f, 0.5f);
            chao.offsetMin = Vector2.zero;
            chao.offsetMax = Vector2.zero;

            var casas = new[] { (x: 0.075f, alt: 0.150f), (x: 0.175f, alt: 0.185f),
                                (x: 0.275f, alt: 0.140f), (x: 0.700f, alt: 0.170f),
                                (x: 0.800f, alt: 0.145f), (x: 0.900f, alt: 0.190f) };

            for (var c = 0; c < casas.Length; c++)
            {
                var (x, alt) = casas[c];
                var corpo = Widgets.Painel($"Casa{c}", _rua, new Color(0.28f, 0.25f, 0.24f, 1f));
                NaTela(corpo, x, LinhaDoTelhado - alt / 2f + 0.055f, 0.088f, alt);

                var telhado = Widgets.Painel($"Telhado{c}", _rua, new Color(0.34f, 0.22f, 0.19f, 1f));
                NaTela(telhado, x, LinhaDoTelhado - alt + 0.055f, 0.100f, 0.016f);

                for (var j = 0; j < 2; j++)
                {
                    var janela = Widgets.Painel($"Janela{c}_{j}", corpo, Cores.Luz).GetComponent<Image>();
                    var r = (RectTransform)janela.transform;
                    r.anchorMin = r.anchorMax = new Vector2(0.30f + j * 0.40f, 0.62f);
                    r.pivot = new Vector2(0.5f, 0.5f);
                    r.anchoredPosition = Vector2.zero;
                    r.sizeDelta = new Vector2(9f, 11f);
                    _janelas.Add(janela);
                }
            }
        }

        /// <summary>
        /// A caixa d'água sobre o poste. O nível dela é o único número da cena que
        /// ninguém enuncia — e ele sobe ou desce a três centímetros dos
        /// ventiladores, na mesma tela.
        /// </summary>
        void MontarCaixaDagua()
        {
            var perna = Widgets.Painel("Perna", _rua, new Color(0.22f, 0.21f, 0.20f, 1f));
            NaTela(perna, 0.395f, LinhaDoTelhado - 0.005f, 0.007f, 0.115f);

            var casco = Widgets.Painel("Caixa", _rua, new Color(0.30f, 0.33f, 0.34f, 1f));
            NaTela(casco, 0.395f, LinhaDoTelhado - 0.105f, 0.055f, 0.070f);

            _agua = Widgets.Painel("Água", casco, new Color(0.30f, 0.62f, 0.68f, 1f));
            _agua.anchorMin = new Vector2(0.12f, 0.08f);
            _agua.anchorMax = new Vector2(0.88f, 0.60f);
            _agua.offsetMin = Vector2.zero;
            _agua.offsetMax = Vector2.zero;
        }

        /// <summary>A porta do guichê. É para ali que a fila olha.</summary>
        void MontarPorta()
        {
            var vao = Widgets.Painel("Guichê", _rua, new Color(0.24f, 0.21f, 0.19f, 1f));
            NaTela(vao, 0.500f, LinhaDoTelhado - 0.085f + 0.055f, 0.085f, 0.190f);

            var porta = Widgets.Painel("Porta", vao, new Color(0.13f, 0.14f, 0.17f, 1f));
            Widgets.Esticar(porta, 5f);
        }

        /// <summary>
        /// A fila na calçada. Os rostos vêm do log do aluno sempre que dá: quem
        /// está ali é gente que ele carimbou, com a cara que ele viu.
        /// </summary>
        void MontarFila()
        {
            for (var i = 0; i < NaFila; i++)
            {
                var suporte = Widgets.Painel($"Fila{i}", _rua, Color.clear);
                NaTela(suporte, 0.545f + i * 0.0165f, DoTopo(0.315f), 0.030f, 0.085f);

                var corpo = Widgets.Painel($"Pessoa{i}", suporte, Color.white).GetComponent<Image>();
                Widgets.Esticar((RectTransform)corpo.transform);
                var sprite = SpriteDoRequerente(RostoDoLog(i));
                if (sprite != null) corpo.sprite = sprite;
                else corpo.color = new Color(0.36f, 0.33f, 0.31f, 1f);
                corpo.preserveAspect = true;

                _naFila.Add(suporte);
                _rostosDaFila.Add(corpo);
            }
        }

        /// <summary>
        /// Quem sai correndo, e chorando.
        ///
        /// SÃO OS MESMOS CINCO NOS DOIS FINAIS, e só a cor muda: no final ruim são
        /// moradores, com o rosto que o aluno viu no guichê; no bom, os mesmos
        /// corpos escurecidos até virarem silhueta de terno. A cena não explica —
        /// a diferença entre os dois desfechos é quem está correndo, e isso se lê
        /// sem legenda.
        ///
        /// Correm PARA A DIREITA, na direção oposta à do guichê, com o ciclo de
        /// caminhada do atlas acelerado. Passo parado com a pessoa deslizando é o
        /// erro clássico, e o atlas já tem os quadros.
        /// </summary>
        void MontarCorreria()
        {
            for (var i = 0; i < Fugindo; i++)
            {
                var suporte = Widgets.Painel($"Fuga{i}", _rua, Color.clear);
                NaTela(suporte, 0.235f + i * 0.048f, DoTopo(0.295f) + i % 2 * 0.018f, 0.034f, 0.098f);

                var corpo = Widgets.Painel($"Fugitivo{i}", suporte, Color.white).GetComponent<Image>();
                Widgets.Esticar((RectTransform)corpo.transform);
                corpo.preserveAspect = true;

                var passos = PassosDe(RostoDoLog(i + 3));
                if (passos != null && passos.Length > 0) corpo.sprite = passos[0];
                else corpo.color = new Color(0.36f, 0.33f, 0.31f, 1f);

                // A lágrima: um pontinho claro ao lado do rosto, que desce e
                // reaparece. Dois pixels bastam — chorar em pixel art é isso.
                var lagrima = Widgets.Painel($"Lágrima{i}", suporte, new Color(0.62f, 0.84f, 0.92f, 1f));
                lagrima.anchorMin = lagrima.anchorMax = new Vector2(0.30f, 0.80f);
                lagrima.pivot = new Vector2(0.5f, 0.5f);
                lagrima.sizeDelta = new Vector2(3f, 4f);

                suporte.gameObject.SetActive(false);
                _fugindo.Add(suporte);
                _corpoDeQuemFoge.Add(corpo);
                _passosDeQuemFoge.Add(passos);
                _lagrimas.Add(lagrima);
            }
        }

        /// <summary>O rosto de alguém que o aluno atendeu, ou um do atlas.</summary>
        int RostoDoLog(int i)
        {
            var doLog = _log.Where(x => !x.Quem.EhEmpresa).Select(x => x.Quem.Retrato).ToList();
            return doLog.Count > 0 ? doLog[i % doLog.Count] : i;
        }

        Sprite[] PassosDe(int retrato)
        {
            try { return Atlas.Caminhada($"r{Mathf.Abs(retrato) % Requerentes}", Folhas.Direita); }
            catch (System.ArgumentException) { return null; }
        }

        void MontarChuvaDaRua()
        {
            _chuvaDaRua = Widgets.Painel("Chuva", _rua, Color.clear);
            Widgets.Esticar(_chuvaDaRua);

            for (var i = 0; i < 34; i++)
            {
                var gota = Retrato($"p{i}", _chuvaDaRua, Atlas.Peca("gota"));
                var r = (RectTransform)gota.transform;
                r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(4f, 13f);
                r.anchoredPosition = new Vector2(Random.Range(0f, 940f), -Random.Range(0f, 300f));
                _pingos.Add(r);
            }
            _chuvaDaRua.gameObject.SetActive(false);
        }

        void MontarLegenda()
        {
            // A tarja preta embaixo. Sem ela a frase disputa leitura com o chão da
            // rua, e legenda de cena não pode ser adivinhada.
            var tarja = Widgets.Painel("Tarja", _rua, new Color(0.04f, 0.05f, 0.06f, 0.80f));
            Widgets.Faixa(tarja, false, 84f);

            _legenda = Widgets.Texto("Legenda", tarja, 22, TextAnchor.MiddleCenter, Cores.Papel);
            Widgets.Esticar(_legenda.rectTransform, 12f);
            _legenda.text = string.Empty;

            _oAno = Widgets.Texto("Ano", _rua, 13, TextAnchor.UpperLeft, Cores.Neblina);
            Widgets.Fixar(_oAno.rectTransform, new Vector2(0f, 1f),
                          new Vector2(72f, -18f), new Vector2(120f, 20f));
            Widgets.UmaLinha(_oAno, 10);
        }

        // -------------------------------------------------------------- pintar

        /// <summary>
        /// Um quadro da cena. Como na abertura, é uma função do tempo para a tela,
        /// sem estado escondido entre quadros — é isso que deixa a cena pulável sem
        /// deixar nada pela metade.
        /// </summary>
        void PintarCena(float t)
        {
            // Antes da bifurcação os dois finais são a mesma tarde. O céu só decide
            // de que lado está depois que o galpão termina de subir.
            var depois = Mathf.Clamp01((t - Bifurcacao) / 8f);
            var escuro = _finalBom ? depois * 0.18f : depois;

            _ceuRuim.color = new Color(1f, 1f, 1f, escuro);
            _sol.color = new Color(1f, 1f, 1f, 0.85f * (1f - escuro));

            // O galpão sobe nos dois finais: ele foi deferido nos dois. O que muda
            // é de quem ele é no fim.
            var subiu = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 1.6f) / 4.4f));
            foreach (var g in _galpoes)
            {
                var p = g.anchoredPosition;
                g.anchoredPosition = new Vector2(p.x, Mathf.Lerp(-120f, 0f, subiu));
            }

            var virou = Mathf.Clamp01((t - Vira) / 5f);

            // No final bom os ventiladores DESACELERAM — uso medido, decidido por
            // quem mora ali. No ruim eles aceleram e não param mais.
            var giro = _finalBom ? Mathf.Lerp(210f, 46f, virou) : Mathf.Lerp(210f, 320f, virou);
            if (subiu > 0.6f)
            {
                for (var i = 0; i < _ventiladores.Count; i++)
                {
                    var conjunto = i / 3;
                    _ventiladores[i].localRotation = Quaternion.Euler(
                        0f, 0f, (t * giro + conjunto * 37f + i % 3 * 120f) % 360f);
                }
            }

            // O brasão da cidade na parede do galpão. Só no final bom, e é a única
            // coisa na cena que diz que aquilo passou a ser de todo mundo.
            if (_brasaoDoGalpao != null)
                _brasaoDoGalpao.GetComponent<Image>().color =
                    new Color(1f, 1f, 1f, _finalBom ? virou : 0f);

            // A água: enche no final bom, esvazia no ruim.
            var nivel = _finalBom ? Mathf.Lerp(0.60f, 0.92f, virou) : Mathf.Lerp(0.60f, 0.14f, virou);
            _agua.anchorMax = new Vector2(0.88f, nivel);
            _agua.offsetMax = Vector2.zero;

            PintarJanelas(t, virou);
            PintarFila(virou);
            PintarCorreria(t, virou);

            Chuviscar(escuro);
            Legendar(t);
            MarcarOAno(t);
        }

        void PintarJanelas(float t, float virou)
        {
            for (var j = 0; j < _janelas.Count; j++)
            {
                // A ordem é embaralhada pelo índice, não da esquerda para a
                // direita: rua não acende nem apaga em fila indiana.
                var vez = j * 7 % Mathf.Max(1, _janelas.Count) / (float)_janelas.Count;
                var acesa = _finalBom ? virou > vez * 0.8f : virou < vez + 0.12f;
                var brilho = acesa
                    ? Mathf.Lerp(0.55f, 0.95f, Mathf.PingPong(t * 0.35f + j, 1f))
                    : 0.06f;
                _janelas[j].color = new Color(Cores.Luz.r, Cores.Luz.g, Cores.Luz.b, brilho);
            }
        }

        /// <summary>
        /// A fila. No final bom ela FICA — gente que continua indo, agora a um
        /// balcão que responde. No ruim ela desmancha do fim para o começo: quem
        /// está mais longe da porta é quem desiste primeiro.
        /// </summary>
        void PintarFila(float virou)
        {
            var vao = _finalBom ? 0 : NaFila - 2;

            for (var i = 0; i < _naFila.Count; i++)
            {
                var ordem = _naFila.Count - 1 - i;
                var quandoSai = vao == 0 ? 2f : ordem / (float)vao;
                var alfa = ordem < vao ? Mathf.Clamp01((quandoSai + 0.08f - virou) * 7f) : 1f;

                _rostosDaFila[i].color = new Color(1f, 1f, 1f, alfa);
                _naFila[i].anchoredPosition = new Vector2((1f - alfa) * 26f, 0f);
            }
        }

        /// <summary>
        /// A correria. Cinco figuras atravessando a tela para a direita, com o
        /// ciclo de caminhada acelerado e uma lágrima caindo.
        /// </summary>
        void PintarCorreria(float t, float virou)
        {
            for (var i = 0; i < _fugindo.Count; i++)
            {
                var atraso = i * 0.09f;
                var correu = Mathf.Clamp01((virou - atraso) / Mathf.Max(0.05f, 1f - atraso));
                var visivel = virou > atraso;

                if (_fugindo[i].gameObject.activeSelf != visivel)
                    _fugindo[i].gameObject.SetActive(visivel);
                if (!visivel) continue;

                // Corre para a direita e some ao sair. O pulinho vertical é o que
                // separa correr de deslizar.
                _fugindo[i].anchoredPosition = new Vector2(
                    correu * 640f, Mathf.Abs(Mathf.Sin(t * 11f + i)) * 5f);

                var passos = _passosDeQuemFoge[i];
                if (passos != null && passos.Length > 0)
                    _corpoDeQuemFoge[i].sprite = passos[Mathf.FloorToInt(t * 11f) % passos.Length];

                // O empresário é o mesmo corpo, escurecido até virar silhueta de
                // terno. Nenhuma arte nova, e ninguém precisa que digam quem é.
                var alfa = 1f - Mathf.SmoothStep(0.78f, 1f, correu);
                _corpoDeQuemFoge[i].color = _finalBom
                    ? new Color(0.16f, 0.17f, 0.24f, alfa)
                    : new Color(1f, 1f, 1f, alfa);

                var caindo = Mathf.Repeat(t * 1.8f + i * 0.4f, 1f);
                _lagrimas[i].anchoredPosition = new Vector2(0f, -caindo * 13f);
                _lagrimas[i].GetComponent<Image>().color =
                    new Color(0.62f, 0.84f, 0.92f, (1f - caindo) * alfa);
            }
        }

        void Chuviscar(float escuro)
        {
            var chovendo = escuro > 0.55f;
            if (_chuvaDaRua.gameObject.activeSelf != chovendo)
                _chuvaDaRua.gameObject.SetActive(chovendo);
            if (!chovendo) return;

            var tela = _chuvaDaRua.rect.size;
            if (tela.x <= 1f) return;

            foreach (var gota in _pingos)
            {
                var p = gota.anchoredPosition;
                p.y -= 760f * Time.unscaledDeltaTime;
                p.x -= 90f * Time.unscaledDeltaTime;
                if (p.y < -tela.y * 0.62f)
                {
                    p.y = 0f;
                    p.x = Random.Range(0f, tela.x + 120f);
                }
                if (p.x < -30f) p.x += tela.x + 120f;
                gota.anchoredPosition = p;
            }
        }

        void Legendar(float t)
        {
            var atual = _falas[^1];
            foreach (var f in _falas)
            {
                if (t >= f.Ate) continue;
                atual = f;
                break;
            }

            _legenda.color = atual.Grave ? Cores.Luz : Cores.Papel;
            _legenda.fontSize = atual.Grave ? 24 : 22;
            _legenda.text = atual.Texto;
        }

        /// <summary>O ano no canto. É o relógio que o aluno não controla.</summary>
        void MarcarOAno(float t)
        {
            var quantos = _anos == null || _anos.Count == 0 ? 4 : _anos.Count;
            var qual = Mathf.Clamp(Mathf.FloorToInt(t / (FimDaCena / quantos)), 0, quantos - 1);
            _oAno.text = $"ano {qual + 1}";
        }

        // ------------------------------------------------------------ o relógio

        /// <summary>
        /// Roda a cena. Chamado do <c>Update</c> da bancada, e não de uma corrotina,
        /// porque a cena é uma função do tempo: quem desenha por interpolação
        /// precisa de um quadro, não de um <c>yield</c>.
        /// </summary>
        void RodarCena()
        {
            _relogioDaCena += Time.unscaledDeltaTime;
            PintarCena(_relogioDaCena);
            if (_relogioDaCena >= FimDaCena) TerminarCena();
        }

        void TerminarCena()
        {
            if (!_cenaRodando) return;
            _cenaRodando = false;
            MostrarPlacar();
        }

        // ---------------------------------------------------------- utilidades

        /// <summary>Um retângulo com uma peça de arte dentro, sem esticar o desenho.</summary>
        static Image Retrato(string nome, Transform pai, Sprite peca)
        {
            var objeto = new GameObject(nome, typeof(RectTransform), typeof(Image));
            objeto.transform.SetParent(pai, false);
            var imagem = objeto.GetComponent<Image>();
            imagem.sprite = peca;
            imagem.raycastTarget = false;
            return imagem;
        }

        /// <summary>
        /// Ancora pelo centro, em frações da tela contadas DE CIMA — como se lê um
        /// quadro. As âncoras do Unity contam de baixo para cima, e a conversão mora
        /// aqui, num lugar só. É o mesmo cuidado da abertura, e pela mesma razão:
        /// escrever a fração de cima e ancorá-la direto foi o que plantou as torres
        /// de vento atrás da colina lá.
        /// </summary>
        static void NaTela(RectTransform reto, float x, float y, float largura, float altura)
        {
            reto.anchorMin = new Vector2(x - largura / 2f, 1f - y - altura / 2f);
            reto.anchorMax = new Vector2(x + largura / 2f, 1f - y + altura / 2f);
            reto.pivot = new Vector2(0.5f, 0.5f);
            reto.offsetMin = Vector2.zero;
            reto.offsetMax = Vector2.zero;
        }
    }
}
