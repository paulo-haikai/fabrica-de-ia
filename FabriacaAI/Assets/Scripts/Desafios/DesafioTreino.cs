using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Bancada 8 — o treino de Rosa.
    ///
    /// Inspiração: ANGRY BIRDS. Você não controla o voo — controla uma força
    /// antes de soltar, assiste ao resultado inteiro e ajusta na próxima. O
    /// aprendizado do jogador acontece entre as tentativas, não durante.
    ///
    /// Aqui a força é o TAMANHO DO PASSO, carregada segurando o botão, e o que
    /// se assiste é uma bolinha descendo um vale. O vale não é enfeite: é a
    /// paisagem de erro de um botão de verdade — altura é o erro, largura é a
    /// distância até o alvo. A bolinha percorre exatamente as posições que a
    /// descida de gradiente calcula (ver <see cref="Mostrador.Descer"/>).
    ///
    /// Os três comportamentos que o vale mostra são os três que qualquer pessoa
    /// que treina modelos conhece de cor, e nenhum deles precisa de legenda:
    ///
    ///   · passo curto — a bolinha para na ladeira, longe do fundo;
    ///   · passo bom — ela assenta no fundo e fica brilhando;
    ///   · passo longo — ela atravessa o fundo e sobe a ladeira do OUTRO lado,
    ///     cada vez mais alto, até sair voando da tela.
    ///
    /// A versão anterior pedia isso com cinco botões rotulados ("bem curto",
    /// "curto", "médio"…). Era múltipla escolha: cinco portas, uma certa, e a
    /// calibragem — que é a coisa que se aprende — acontecia por eliminação em
    /// vez de por sensação. A carga contínua devolve a decisão ao dedo.
    ///
    /// E a rodada 3 traz o problema de verdade: botões com curvaturas muito
    /// diferentes, mostrados como DOIS vales lado a lado, um estreito e um
    /// largo, recebendo a mesma força ao mesmo tempo. Um assenta, o outro
    /// explode. Não existe passo bom para os dois, e isso se vê num quadro.
    /// </summary>
    public partial class DesafioTreino : DesafioEmNiveis
    {
        public override string Etapa => "e8";
        public override string Titulo => "Deixar a máquina treinar";

        protected override int Niveis => Rodadas8.Length;

        /// <summary>
        /// Botões, passos de treino, desequilíbrio de curvatura e tentativas.
        ///
        /// Poucos passos de propósito. A primeira versão dava 24 passos, e com
        /// isso QUALQUER passo razoável convergia — a descida é exponencial, e
        /// vinte e quatro iterações perdoam tudo. O aluno acertava na primeira
        /// tentativa e nunca via a bolinha passar do ponto nem explodir, que são
        /// dois terços da lição.
        /// </summary>
        static readonly (int botoes, int passos, float desequilibrio, int tentativas)[] Rodadas8 =
        {
            (4, 8, 1f, 4),
            // Curvatura levemente desigual: muda a resposta certa em relação à
            // rodada 1, que com o passo em fração é idêntica a ela.
            (8, 8, 2.5f, 3),
            (8, 12, 9f, 4)
        };

        /// <summary>
        /// A faixa de força, em fração do passo ideal.
        ///
        /// O teto passa de 2 de propósito: acima de 2 a conta diverge para um
        /// botão de curvatura 1, e o aluno TEM que poder alcançar a explosão
        /// segurando demais. Um limite seguro esconderia um terço da lição.
        /// </summary>
        const float ForcaMinima = 0.03f;
        const float ForcaMaxima = 2.4f;

        /// <summary>Segundos de aperto para encher a barra.</summary>
        const float TempoDeCarga = 1.15f;

        /// <summary>
        /// Abaixo disto o aperto foi um clique sem querer, e não gasta tentativa.
        /// Perder uma das três tentativas por um toque acidental é o tipo de
        /// punição que o aluno não entende, e por isso não aprende com ela.
        /// </summary>
        const float CargaMinimaValida = 0.06f;

        /// <summary>
        /// A meta é RELATIVA: o erro tem que cair a um centésimo do que era.
        ///
        /// Um limite absoluto não serviria, porque o erro inicial depende do alvo
        /// sorteado — a mesma força passaria numa rodada e falharia na outra, sem
        /// o aluno entender por quê.
        /// </summary>
        const float FracaoDaMeta = 0.01f;

        (int botoes, int passos, float desequilibrio, int tentativas) _r;
        Mostrador _mostrador;
        int _tentativas;
        float _melhorErro = float.PositiveInfinity;
        bool _treinando;

        // -------------------------------------------------------- carga
        bool _carregando;
        float _carga;
        float _cargaAnterior = -1f;
        RectTransform _mioloDaCarga;
        RectTransform _marcaAnterior;
        Button _botaoCarga;
        const float LarguraDaCarga = 420f;

        // -------------------------------------------------------- vales
        RectTransform _quadro;
        readonly List<Vale> _vales = new();

        /// <summary>
        /// Um vale desenhado: a paisagem de erro de UM botão.
        ///
        /// Guarda a escala junto com os retângulos porque os dois vales da
        /// rodada 3 precisam compartilhar a MESMA escala — se cada um fosse
        /// normalizado pelo próprio máximo, os dois sairiam com o mesmo desenho
        /// e a diferença de curvatura, que é a lição inteira, desapareceria.
        /// </summary>
        class Vale
        {
            public int Botao;
            public float Curvatura;
            public RectTransform Piso;
            public RectTransform Bola;
            public Vector2 Tamanho;
            public float Alcance;
            public float AlturaMaxima;
        }

        protected override void MontarNivel()
        {
            _r = Rodadas8[NivelAtual];
            _tentativas = 0;
            _melhorErro = float.PositiveInfinity;
            _treinando = false;
            _carregando = false;
            _carga = 0f;
            _cargaAnterior = -1f;
            _vales.Clear();

            Painel.Rodape("a máquina gira os botões sozinha · você só decide a força");

            MontarQuadro();
            MontarCarga();
            DesenharVales();

            Painel.Instruir("segure para carregar · solte para treinar");
            Atualizar();
        }

        // ------------------------------------------------------------- quadro

        void MontarQuadro()
        {
            _quadro = Widgets.Painel("Quadro", Area, Cores.TintaClara);
            Widgets.Fixar(_quadro, new Vector2(0.5f, 1f), new Vector2(0f, -128f),
                          new Vector2(760f, 236f));
        }

        /// <summary>
        /// Escolhe quais botões viram vale e desenha a paisagem de cada um.
        ///
        /// Nas rodadas parelhas, um vale só (o botão mediano). Na rodada 3, os
        /// dois extremos lado a lado — que é o único jeito de a desigualdade de
        /// curvatura virar imagem em vez de frase.
        /// </summary>
        void DesenharVales()
        {
            foreach (Transform filho in _quadro) Destroy(filho.gameObject);
            _vales.Clear();

            // Um mostrador só para medir a paisagem, com a mesma semente do
            // treino: o desenho tem que ser o do problema que o aluno vai jogar.
            var molde = NovoMostrador();
            var duplo = _r.desequilibrio > 4f;

            var botoes = new List<int>();
            if (duplo)
            {
                var (ingreme, raso) = molde.Extremos();
                botoes.Add(ingreme);
                botoes.Add(raso);
            }
            else botoes.Add(molde.Mediano());

            // Escala COMPARTILHADA entre os vales: mesmo alcance horizontal e
            // mesma altura máxima, senão o íngreme e o raso saem iguais.
            var alcance = 0.1f;
            foreach (var b in botoes)
                alcance = Mathf.Max(alcance, Mathf.Abs(molde.Desvio(b)) * 1.35f);

            var alturaMaxima = 0.001f;
            foreach (var b in botoes)
                alturaMaxima = Mathf.Max(alturaMaxima,
                                         molde.Curvatura[b] * alcance * alcance);

            var largura = duplo ? 356f : 720f;
            for (var i = 0; i < botoes.Count; i++)
            {
                var deslocamento = duplo ? (i == 0 ? -186f : 186f) : 0f;
                _vales.Add(MontarVale(botoes[i], molde.Curvatura[botoes[i]],
                                      new Vector2(deslocamento, 0f),
                                      new Vector2(largura, 200f),
                                      alcance, alturaMaxima, duplo));
            }
        }

        Vale MontarVale(int botao, float curvatura, Vector2 posicao, Vector2 tamanho,
                        float alcance, float alturaMaxima, bool rotular)
        {
            var caixa = Widgets.Painel($"Vale{botao}", _quadro, Color.clear);
            Widgets.Fixar(caixa, new Vector2(0.5f, 0.5f), posicao, tamanho);

            var vale = new Vale
            {
                Botao = botao,
                Curvatura = curvatura,
                Piso = caixa,
                Tamanho = tamanho,
                Alcance = alcance,
                AlturaMaxima = alturaMaxima
            };

            // O fundo do vale, marcado antes de a bolinha existir: o objetivo
            // tem que ser legível num olhar, sem ninguém explicar qual é.
            var fundo = Widgets.Painel("Fundo", caixa, Cores.Neblina);
            Widgets.Fixar(fundo, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f),
                          new Vector2(1.5f, tamanho.y));

            const int fatias = 44;
            var anterior = Vector2.zero;
            for (var i = 0; i <= fatias; i++)
            {
                var desvio = Mathf.Lerp(-alcance, alcance, i / (float)fatias);
                var ponto = NoVale(vale, desvio);
                if (i > 0) Encosta(caixa, anterior, ponto);
                anterior = ponto;
            }

            if (rotular)
            {
                var nome = Widgets.Texto("Nome", caixa, 13, TextAnchor.LowerCenter, Cores.Neblina);
                Widgets.Faixa(nome.rectTransform, false, 16f);
                nome.text = curvatura > 2f ? "botão estreito" : "botão largo";
            }

            vale.Bola = Widgets.Painel("Bola", caixa, Cores.Luz);
            Widgets.Fixar(vale.Bola, new Vector2(0.5f, 0.5f),
                          NoVale(vale, 0f), new Vector2(15f, 15f));
            vale.Bola.gameObject.SetActive(false);
            return vale;
        }

        /// <summary>
        /// Onde um desvio cai dentro do quadro do vale.
        ///
        /// Altura é o erro daquele botão (curvatura × desvio²) e largura é o
        /// desvio com sinal — as duas coisas que a descida de gradiente move.
        /// </summary>
        static Vector2 NoVale(Vale vale, float desvio)
        {
            var x = Mathf.Clamp(desvio / vale.Alcance, -1f, 1f) * vale.Tamanho.x / 2f;
            var altura = vale.Curvatura * desvio * desvio;
            var y = -vale.Tamanho.y / 2f +
                    vale.Tamanho.y * Mathf.Clamp01(altura / vale.AlturaMaxima);
            return new Vector2(x, y);
        }

        static void Encosta(RectTransform pai, Vector2 a, Vector2 b)
        {
            var delta = b - a;
            var fio = Widgets.Painel("e", pai, Cores.Madeira);
            Widgets.Fixar(fio, new Vector2(0.5f, 0.5f), (a + b) / 2f,
                          new Vector2(delta.magnitude + 1f, 2.5f));
            fio.localRotation = Quaternion.Euler(
                0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        // -------------------------------------------------------------- carga

        void MontarCarga()
        {
            var miolo = Widgets.Barra("Carga", Area, new Vector2(0f, 0f),
                                      new Vector2(LarguraDaCarga, 24f),
                                      Cores.TintaClara, Cores.Luz);
            var trilho = (RectTransform)miolo.parent;
            Widgets.Fixar(trilho, new Vector2(0.5f, 0f), new Vector2(0f, 118f),
                          new Vector2(LarguraDaCarga, 24f));
            _mioloDaCarga = miolo;
            Widgets.Encher(_mioloDaCarga, 0f, LarguraDaCarga);

            // A marca da tentativa anterior. Sem ela a carga contínua seria
            // impossível de calibrar: o aluno sentiria que "foi forte demais"
            // e não teria como repetir um pouco menos. É a linha pontilhada do
            // tiro anterior, do Angry Birds.
            _marcaAnterior = Widgets.Painel("Anterior", trilho, Cores.Papel);
            Widgets.Fixar(_marcaAnterior, new Vector2(0f, 0.5f), Vector2.zero,
                          new Vector2(2f, 34f));
            _marcaAnterior.gameObject.SetActive(false);

            _botaoCarga = Widgets.Botao("Empurrar", Area, "segure para carregar",
                                        Cores.Folha, Cores.Papel, 17);
            Widgets.Fixar((RectTransform)_botaoCarga.transform, new Vector2(0.5f, 0f),
                          new Vector2(0f, 60f), new Vector2(300f, 52f));

            // O clique comum não serve: o que a bancada mede é QUANTO TEMPO o
            // dedo ficou apertado, e isso só existe entre o apertar e o soltar.
            var gatilho = _botaoCarga.gameObject.AddComponent<EventTrigger>();
            Escutar(gatilho, EventTriggerType.PointerDown, _ => Apertar());
            Escutar(gatilho, EventTriggerType.PointerUp, _ => Soltar());
        }

        static void Escutar(EventTrigger gatilho, EventTriggerType tipo,
                            UnityEngine.Events.UnityAction<BaseEventData> acao)
        {
            var entrada = new EventTrigger.Entry { eventID = tipo };
            entrada.callback.AddListener(acao);
            gatilho.triggers.Add(entrada);
        }

        void Apertar()
        {
            if (_treinando || Congelado) return;
            _carregando = true;
            _carga = 0f;
        }

        void Soltar()
        {
            if (!_carregando) return;
            _carregando = false;

            if (_carga < CargaMinimaValida)
            {
                Widgets.Encher(_mioloDaCarga, 0f, LarguraDaCarga);
                // A barra treme para dizer "não vali": ignorar o toque em silêncio,
                // com um aviso só em texto, faz o aluno achar que o botão quebrou.
                Widgets.Tremer((RectTransform)_mioloDaCarga.parent);
                Painel.Instruir("segure mais tempo para carregar", Cores.Neblina);
                return;
            }

            _cargaAnterior = _carga;
            _marcaAnterior.gameObject.SetActive(true);
            _marcaAnterior.anchoredPosition = new Vector2(_carga * LarguraDaCarga, 0f);

            Lancar(Mathf.Lerp(ForcaMinima, ForcaMaxima, _carga));
        }

        void Update()
        {
            if (!_carregando) return;
            if (_treinando || Congelado)
            {
                _carregando = false;
                return;
            }
            _carga = Mathf.Min(1f, _carga + Time.unscaledDeltaTime / TempoDeCarga);
            Widgets.Encher(_mioloDaCarga, _carga, LarguraDaCarga);
        }

        void Atualizar()
        {
            _botaoCarga.interactable = !_treinando;
            Painel.MarcarPasso(NivelAtual + 1, Niveis,
                               $"tentativa {Mathf.Min(_tentativas + 1, _r.tentativas)} de {_r.tentativas}");
        }

        // ------------------------------------------------------------- treino

        Mostrador NovoMostrador() =>
            new(_r.botoes, Rodadas.Semente() + NivelAtual * 613, _r.desequilibrio);

        void Lancar(float fracao)
        {
            if (_treinando) return;
            _tentativas++;

            // Cada tentativa recomeça do zero, com o MESMO alvo. Sem isso o aluno
            // estaria continuando o treino anterior e não conseguiria comparar
            // forças — que é a única coisa que a bancada pede para ele comparar.
            _mostrador = NovoMostrador();

            var observados = _vales.Select(v => v.Botao).ToArray();
            var (curva, desvios) = _mostrador.TreinarObservando(fracao, _r.passos, observados);
            StartCoroutine(Rolando(curva, desvios));
        }

        IEnumerator Rolando(List<float> curva, List<float>[] desvios)
        {
            _treinando = true;
            Atualizar();
            Widgets.Encher(_mioloDaCarga, 0f, LarguraDaCarga);

            foreach (var vale in _vales) vale.Bola.gameObject.SetActive(true);

            for (var passo = 0; passo < curva.Count; passo++)
            {
                for (var v = 0; v < _vales.Count; v++)
                {
                    var vale = _vales[v];
                    var desvio = desvios[v][passo];

                    if (Fugiu(desvio, vale.Alcance))
                    {
                        // Saiu da paisagem: some da tela em vez de grudar na
                        // borda fingindo que ainda está no vale.
                        vale.Bola.anchoredPosition =
                            NoVale(vale, Mathf.Sign(desvio) * vale.Alcance) +
                            new Vector2(0f, 60f);
                        vale.Bola.gameObject.SetActive(false);
                        continue;
                    }

                    vale.Bola.anchoredPosition = NoVale(vale, desvio);
                    // Squash de aterrissagem: a bolinha "bate" a cada passo.
                    Widgets.Pulsar(vale.Bola, 1.45f, 0.09f);
                }
                yield return new WaitForSecondsRealtime(0.11f);
            }

            var final = curva[^1];
            _melhorErro = Mathf.Min(_melhorErro, float.IsNaN(final) ? float.MaxValue : final);
            _treinando = false;
            Atualizar();

            Julgar(curva);
        }

        static bool Fugiu(float desvio, float alcance) =>
            float.IsNaN(desvio) || float.IsInfinity(desvio) || Mathf.Abs(desvio) > alcance;

        static string Mostrar(float erro) =>
            float.IsNaN(erro) || float.IsInfinity(erro) || erro > 9999f
                ? "explodiu"
                : erro.ToString("0.000");

        void Julgar(List<float> curva)
        {
            var final = curva[^1];
            var explodiu = float.IsNaN(final) || float.IsInfinity(final) || final > curva[0];
            var serrilhou = !explodiu && Serrilhou(curva);

            if (final <= curva[0] * FracaoDaMeta)
            {
                Vencer(curva);
                return;
            }

            if (explodiu)
            {
                // O quadro inteiro treme quando a conta diverge. É o momento em
                // que "passo grande demais" deixa de ser aviso e vira estrago.
                Widgets.Tremer(_quadro, 12f, 0.3f);
                Widgets.Lampejo(_quadro, Cores.Brasa, 0.35f, 0.5f);
            }

            if (_tentativas >= _r.tentativas)
            {
                Falhou($"Acabaram as tentativas — melhor erro {Mostrar(_melhorErro)}",
                       Licao(explodiu, serrilhou),
                       contaEstrela: _melhorErro < curva[0] * 0.25f);
                return;
            }

            Painel.Instruir(
                explodiu ? "explodiu" : serrilhou ? "passou do ponto" : "não chegou",
                explodiu ? Cores.Brasa : Cores.Luz);
        }

        /// <summary>
        /// O diagnóstico da tentativa, em uma frase curta. A bolinha já mostrou
        /// o QUE aconteceu; a frase só dá nome, para o professor pegar o gancho.
        /// </summary>
        static string Licao(bool explodiu, bool serrilhou) =>
            explodiu
                ? "Força grande demais: a cada ajuste ela passava do fundo e ia\nparar mais alto do que estava."
                : serrilhou
                    ? "Ela chegava perto e passava direto, de um lado para o outro,\nsem assentar."
                    : "A direção estava certa — a bolinha desceu. Só não havia\npasso suficiente para chegar ao fundo.";

        /// <summary>
        /// A curva serrilhou se subiu em algum passo depois de ter descido. É a
        /// assinatura de passo grande: a atualização passa do alvo e volta pelo
        /// outro lado.
        /// </summary>
        static bool Serrilhou(List<float> curva)
        {
            for (var i = 2; i < curva.Count; i++)
                if (curva[i] > curva[i - 1] * 1.05f) return true;
            return false;
        }

        void Vencer(List<float> curva)
        {
            foreach (var vale in _vales)
                if (vale.Bola.gameObject.activeSelf) Widgets.Pulsar(vale.Bola, 2f, 0.25f);

            var quedas = curva[0] / Mathf.Max(0.0001f, curva[^1]);

            if (NivelAtual < Niveis - 1)
            {
                Resolveu($"Assentou — o erro caiu {quedas:0} vezes",
                    "Você não girou um único botão. A máquina calcula para que lado\n" +
                    "girar cada um, gira todos um tantinho, e repete.\n\n" +
                    "Isso tem nome: descida de gradiente.");
                return;
            }

            Resolveu("Assentou, com os dois vales desiguais",
                "Um vale estreito e um largo, a mesma força nos dois.\n\n" +
                "Você achou na mão o problema que criou uma área de pesquisa\n" +
                "inteira: ajustar o passo de cada botão separado.");
        }
    }
}
