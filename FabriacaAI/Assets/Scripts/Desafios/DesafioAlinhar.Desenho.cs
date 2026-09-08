using System;
using System.Collections;
using FabricaDeIA.Arte;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// O guichê na tela.
    ///
    /// O QUE FAZ UM GUICHÊ PARECER UM GUICHÊ é materialidade, não desenho bonito:
    /// uma parede atrás, uma MESA embaixo — o único plano horizontal da tela —,
    /// papel que projeta sombra sobre essa mesa, e um carimbo que é objeto
    /// pousado num suporte, não botão. A decisão pesa porque tem corpo.
    ///
    /// A GEOMETRIA É O CONTEÚDO, e isto vale mais que a materialidade toda.
    ///
    /// O formulário fica EM CIMA e a comprovação EMBAIXO. Não é arranjo estético:
    /// o formulário carrega bairro e meses na cidade — o que o sistema tem, e o
    /// que engana — e a comprovação carrega espera e necessidade, que é o que o
    /// manual manda usar. A folha certa é a de baixo, e ela é a segunda a ser
    /// lida. Com o relógio apertando, é a primeira a ser abandonada.
    ///
    /// Quem projeta um formulário nunca acha que está decidindo quem entra. Está.
    ///
    /// A área útil é 912 × 426 (ver PainelDeBancada): 960 menos as margens, 600
    /// menos cabeçalho, instrução e rodapé. A mesa toma a faixa de baixo; a
    /// parede, o resto.
    /// </summary>
    public partial class DesafioAlinhar
    {
        const float ColunaEsquerda = -348f;
        const float ColunaFolhas = -14f;
        const float ColunaManual = 316f;

        /// <summary>Onde a parede acaba e a mesa começa.</summary>
        const float AlturaDaMesa = 122f;

        /// <summary>Pixels de tela por pixel de arte nos requerentes.</summary>
        const float Ampliacao = 7f;

        static readonly Color Parede = new(0.129f, 0.145f, 0.192f, 1f);
        static readonly Color Penumbra = new(0.086f, 0.098f, 0.133f, 1f);

        Text _relogioTexto;
        Text _placarTexto;
        Text _nomeTexto;
        Text _falaTexto;
        RectTransform _retrato;
        RectTransform _folhaFormulario;
        RectTransform _folhaComprovacao;

        // As sombras das folhas andam junto com elas no fade. Papel que some
        // deixando a sombra para trás é o defeito clássico de quem esqueceu que
        // a sombra é outro objeto.
        RectTransform _sombraFormulario;
        RectTransform _sombraComprovacao;

        /// <summary>
        /// Tudo o que pertence a QUEM está no guichê agora — e nada do guichê em
        /// si. Ver <see cref="ReunirPresenca"/>.
        /// </summary>
        CanvasGroup[] _presenca;

        Folhas _folhas;
        int _ultimoSegundo = -1;

        // Chama-se Atlas, e não Arte, porque `Arte` é o nome do NAMESPACE de
        // onde vem a classe Folhas — um membro com o nome de um namespace usado
        // no mesmo arquivo é resolvido de um jeito que ninguém adivinha lendo.
        Folhas Atlas => _folhas ??= Folhas.Atual;

        /// <summary>Uma peça do guichê, ou nulo se a arte não foi regerada.</summary>
        Sprite Peca(string nome)
        {
            try { return Atlas?.PecaDoGuiche(nome); }
            catch (Exception) { return null; }
        }

        // ------------------------------------------------------------ memorando

        /// <summary>
        /// O memorando que abre o dia, em cima de tudo — e ele é o único tutorial
        /// desta bancada.
        ///
        /// Quem assina fica sozinho na primeira linha, sob o brasão: é a única
        /// pista do jogo sobre quem manda no guichê, e ela nunca é comentada. No
        /// dia 1 é a Prefeitura. Do dia 2 em diante, não é mais.
        /// </summary>
        void MostrarMemorando((string de, string corpo) memo, Action aoFechar)
        {
            var folha = Widgets.Painel("Memorando", _mesa, new Color(0f, 0f, 0f, 0.86f));
            Widgets.Esticar(folha);

            var sombra = Widgets.Painel("Sombra", folha, new Color(0f, 0f, 0f, 0.5f));
            Widgets.Fixar(sombra, new Vector2(0.5f, 0.5f), new Vector2(6f, 10f),
                          new Vector2(536f, 296f));

            var papel = Widgets.Painel("Papel", folha, Cores.Papel);
            Widgets.Fixar(papel, new Vector2(0.5f, 0.5f), new Vector2(0f, 16f),
                          new Vector2(536f, 296f));

            var brasao = Widgets.Painel("Brasão", papel, Color.white);
            Widgets.Fixar(brasao, new Vector2(0f, 1f), new Vector2(46f, -46f),
                          new Vector2(48f, 48f));
            var sp = Peca("brasao");
            if (sp != null) brasao.GetComponent<Image>().sprite = sp;
            else brasao.GetComponent<Image>().color = Cores.Madeira;

            var de = Widgets.Texto("De", papel, 14, TextAnchor.MiddleLeft, Cores.Madeira);
            Widgets.Fixar(de.rectTransform, new Vector2(0f, 1f), new Vector2(300f, -46f),
                          new Vector2(390f, 34f));
            de.text = memo.de;

            var risco = Widgets.Painel("Risco", papel, Cores.Madeira);
            Widgets.Faixa(risco, true, 1f, 78f);

            var corpo = Widgets.Texto("Corpo", papel, 16, TextAnchor.UpperLeft, Cores.Tinta);
            Widgets.Esticar(corpo.rectTransform);
            corpo.rectTransform.offsetMin = new Vector2(46f, 26f);
            corpo.rectTransform.offsetMax = new Vector2(-40f, -92f);
            corpo.text = memo.corpo;

            Widgets.Surgir(papel, 0.16f, 0.97f);

            Painel.Instruir($"dia {_dia + 1}");
            Painel.Rodape($"meta do dia: {Dias[_dia].Meta} de {Dias[_dia].Casos}");
            Painel.Acao("abrir o guichê", () =>
            {
                Destroy(folha.gameObject);
                aoFechar();
            });
        }

        // -------------------------------------------------------------- guichê

        void MontarGuiche()
        {
            _ultimoSegundo = -1;
            Painel.Acao(null, null);
            Painel.Instruir("confira as duas folhas e carimbe");
            Painel.Rodape($"manual à direita · meta do dia: {Dias[_dia].Meta}");

            MontarSala();
            MontarJanela();
            MontarFolhas();
            MontarManual();
            MontarCarimbos();

            // O guichê abre VAZIO. Quem chega, chega aparecendo — inclusive a
            // primeira pessoa do dia, que senão já estaria lá quando o memorando
            // sai da frente, como se o expediente tivesse começado sem ela.
            ReunirPresenca();
            Presenca(0f);
        }

        /// <summary>A parede, a mesa e o relógio de parede da repartição.</summary>
        void MontarSala()
        {
            var parede = Widgets.Painel("Parede", _mesa, Parede);
            Widgets.Esticar(parede);

            Ladrilhar();

            // A quina da mesa: a linha que separa o plano vertical do horizontal
            // é o que faz a tela virar um lugar em vez de um cartaz.
            var quina = Widgets.Painel("Quina", _mesa, Penumbra);
            Widgets.Fixar(quina, new Vector2(0.5f, 0f), new Vector2(0f, AlturaDaMesa),
                          new Vector2(912f, 3f));

            _relogioTexto = Widgets.Texto("Relógio", _mesa, 32, TextAnchor.MiddleCenter, Cores.Luz);
            Widgets.Fixar(_relogioTexto.rectTransform, new Vector2(0.5f, 0.5f),
                          new Vector2(ColunaEsquerda, 182f), new Vector2(200f, 38f));

            _placarTexto = Widgets.Texto("Placar", _mesa, 13, TextAnchor.MiddleCenter, Cores.Neblina);
            Widgets.Fixar(_placarTexto.rectTransform, new Vector2(0.5f, 0.5f),
                          new Vector2(ColunaEsquerda, 152f), new Vector2(220f, 20f));
        }

        /// <summary>
        /// A mesa, peça por peça.
        ///
        /// Ladrilhada à mão em vez de <c>Image.Type.Tiled</c>: o modo Tiled repete
        /// no tamanho NATIVO do sprite, e 32 pixels numa mesa de 912 dariam
        /// veios finíssimos de fio de cabelo. Aqui cada ladrilho sai a 64, que é a
        /// escala em que o veio da madeira se lê.
        /// </summary>
        void Ladrilhar()
        {
            const float lado = 64f;
            var madeira = Peca("mesa");

            var faixa = Widgets.Painel("Mesa", _mesa, madeira == null ? Cores.Madeira : Color.white);
            Widgets.Fixar(faixa, new Vector2(0.5f, 0f), new Vector2(0f, AlturaDaMesa / 2f),
                          new Vector2(912f, AlturaDaMesa));
            if (madeira == null) return;

            faixa.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            for (var x = 0; x < Mathf.CeilToInt(912f / lado); x++)
            {
                for (var y = 0; y < Mathf.CeilToInt(AlturaDaMesa / lado); y++)
                {
                    var t = Widgets.Painel("t", faixa, Color.white);
                    Widgets.Fixar(t, new Vector2(0f, 0f),
                                  new Vector2(x * lado + lado / 2f, y * lado + lado / 2f),
                                  new Vector2(lado, lado));
                    t.GetComponent<Image>().sprite = madeira;
                }
            }
        }

        /// <summary>
        /// A janela do guichê: um vão escuro na parede, com moldura, e a pessoa
        /// dentro dele. Enquadrar é o que transforma um boneco solto na tela em
        /// alguém que está do outro lado do balcão.
        /// </summary>
        void MontarJanela()
        {
            var moldura = Widgets.Painel("Moldura", _mesa, Cores.Madeira);
            Widgets.Fixar(moldura, new Vector2(0.5f, 0.5f),
                          new Vector2(ColunaEsquerda, 22f), new Vector2(196f, 216f));

            var vao = Widgets.Painel("Vão", moldura, Penumbra);
            Widgets.Esticar(vao, 6f);

            _retrato = Widgets.Painel("Retrato", vao, Color.clear);
            Widgets.Esticar(_retrato);

            _nomeTexto = Widgets.Texto("Nome", _mesa, 17, TextAnchor.MiddleCenter, Cores.Papel);
            Widgets.Fixar(_nomeTexto.rectTransform, new Vector2(0.5f, 0.5f),
                          new Vector2(ColunaEsquerda, -104f), new Vector2(230f, 24f));
            Widgets.UmaLinha(_nomeTexto, 12);

            _falaTexto = Widgets.Texto("Fala", _mesa, 14, TextAnchor.UpperCenter, Cores.Neblina);
            Widgets.Fixar(_falaTexto.rectTransform, new Vector2(0.5f, 0.5f),
                          new Vector2(ColunaEsquerda, -146f), new Vector2(230f, 52f));
        }

        void MontarFolhas()
        {
            _folhaFormulario = Papel("Formulário", 124f, 168f, -1.1f, out _sombraFormulario);
            _folhaComprovacao = Papel("Comprovação", -50f, 168f, 1.4f, out _sombraComprovacao);
        }

        /// <summary>
        /// Uma folha de papel: sombra atrás, papel na frente, torta.
        ///
        /// A INCLINAÇÃO É O TRUQUE MAIS BARATO DA BANCADA. Um grau e meio de
        /// rotação e quatro pixels de sombra transformam um retângulo numa folha
        /// largada na mesa — e não custam arte nenhuma. Papel perfeitamente
        /// alinhado parece caixa de diálogo; papel torto parece expediente.
        /// </summary>
        RectTransform Papel(string nome, float y, float altura, float giro,
                            out RectTransform sombra)
        {
            sombra = Widgets.Painel(nome + " sombra", _mesa, new Color(0f, 0f, 0f, 0.45f));
            Widgets.Fixar(sombra, new Vector2(0.5f, 0.5f),
                          new Vector2(ColunaFolhas + 5f, y - 6f), new Vector2(404f, altura));
            sombra.localRotation = Quaternion.Euler(0f, 0f, giro);

            var papel = Widgets.Painel(nome, _mesa, Cores.Papel);
            Widgets.Fixar(papel, new Vector2(0.5f, 0.5f),
                          new Vector2(ColunaFolhas, y), new Vector2(404f, altura));
            papel.localRotation = Quaternion.Euler(0f, 0f, giro);
            return papel;
        }

        /// <summary>
        /// O manual aberto à direita, montado ITEM A ITEM.
        ///
        /// Era um único <c>Text</c> com as regras separadas por quebra de linha, e
        /// funcionou enquanto as regras eram frases. Quando a regra 1 virou tabela
        /// de prazos, as linhas passaram a estourar os 232 pixels úteis da página e
        /// a quebrar no meio — e tabela quebrada no meio não é tabela, é um monte
        /// de palavras soltas. Justamente no documento que precisa ser consultado
        /// com o relógio correndo.
        ///
        /// Agora cada item sabe se desenhar. A regra é parágrafo com o número numa
        /// calha à esquerda e pode quebrar em quantas linhas precisar; a linha de
        /// prazo é DUAS COLUNAS numa linha só — rótulo à esquerda, prazo à direita —
        /// e não quebra nunca, porque é ela que precisa ficar alinhada.
        ///
        /// A altura de cada bloco vem de <c>preferredHeight</c>, e não de uma
        /// estimativa de caracteres: os retângulos têm largura fixa e ancoragem
        /// pontual, então o Unity já sabe responder quanto o texto ocupa antes do
        /// primeiro quadro. Chutar a altura é como o manual ficou desconfigurado da
        /// primeira vez.
        /// </summary>
        void MontarManual()
        {
            var caixa = Widgets.Painel("Manual", _mesa, Cores.Madeira);
            Widgets.Fixar(caixa, new Vector2(0.5f, 0.5f),
                          new Vector2(ColunaManual, 32f), new Vector2(272f, 356f));

            // A aba lateral: o que faz o manual parecer um livro aberto e não um
            // quadro de avisos.
            var aba = Widgets.Painel("Aba", _mesa, Cores.Luz);
            Widgets.Fixar(aba, new Vector2(0.5f, 0.5f),
                          new Vector2(ColunaManual + 142f, 116f), new Vector2(14f, 76f));

            var pagina = Widgets.Painel("Página", caixa, Cores.Papel);
            Widgets.Esticar(pagina, 7f);

            var titulo = Widgets.Texto("t", pagina, 13, TextAnchor.UpperLeft, Cores.Madeira);
            Widgets.Faixa(titulo.rectTransform, true, 18f, 12f);
            titulo.rectTransform.offsetMin = new Vector2(MargemDaPagina, titulo.rectTransform.offsetMin.y);
            titulo.text = "MANUAL · dia " + (_dia + 1);

            var risco = Widgets.Painel("Risco", pagina, Cores.Madeira);
            Widgets.Faixa(risco, true, 1f, 34f);

            var y = -42f;
            foreach (var item in ManualDoDia(_dia)) y = DesenharItemDoManual(pagina, item, y);

            // A regra 7 chega piscando uma vez, e só. Ninguém a anuncia — ela está
            // no manual como se sempre tivesse estado.
            if (RegraSeteVale(_dia)) Widgets.Lampejo(caixa, Cores.Brasa, 0.35f, 0.5f);
        }

        /// <summary>Margem interna da página do manual, dos dois lados.</summary>
        const float MargemDaPagina = 13f;

        /// <summary>A calha do número da regra, à esquerda do texto dela.</summary>
        const float CalhaDoNumero = 16f;

        /// <summary>
        /// Largura útil da página: os 272 da caixa, menos os 7 de moldura de cada
        /// lado, menos a margem interna dos dois lados.
        /// </summary>
        const float LarguraDaPagina = 272f - 14f - MargemDaPagina * 2f;

        /// <summary>
        /// Desenha um item do manual em <paramref name="y"/> e devolve onde o
        /// próximo começa.
        /// </summary>
        float DesenharItemDoManual(RectTransform pagina, ItemDoManual item, float y)
        {
            if (item.Respiro) return y - 9f;

            // Linha de prazo: duas colunas, uma linha, sem quebra. O prazo vai
            // alinhado à direita para os três números formarem coluna — é a coluna
            // que faz a tabela ser lida de relance em vez de lida palavra a palavra.
            if (!string.IsNullOrEmpty(item.Prazo))
            {
                var rotulo = Widgets.Texto("pr", pagina, 12, TextAnchor.MiddleLeft, Cores.Tinta);
                PorNaPagina(rotulo.rectTransform, MargemDaPagina + CalhaDoNumero, y,
                            LarguraDaPagina - CalhaDoNumero - 62f, 15f);
                rotulo.text = item.Texto;
                Widgets.UmaLinha(rotulo, 9);

                var prazo = Widgets.Texto("pz", pagina, 12, TextAnchor.MiddleRight, Cores.Madeira);
                PorNaPagina(prazo.rectTransform, MargemDaPagina + LarguraDaPagina - 62f, y, 62f, 15f);
                prazo.text = item.Prazo;
                Widgets.UmaLinha(prazo, 9);

                return y - 16f;
            }

            var ehNota = item.Nota;
            var corpo = Widgets.Texto(ehNota ? "nota" : "regra", pagina, ehNota ? 11 : 12,
                                      TextAnchor.UpperLeft, ehNota ? Cores.Madeira : Cores.Tinta);

            var recuo = string.IsNullOrEmpty(item.Numero) ? MargemDaPagina
                                                          : MargemDaPagina + CalhaDoNumero;
            PorNaPagina(corpo.rectTransform, recuo, y, LarguraDaPagina - (recuo - MargemDaPagina), 15f);
            corpo.text = item.Texto;

            // A altura de verdade, perguntada ao próprio texto depois de a largura
            // estar fixa. É o que impede uma regra de três linhas de escrever por
            // cima da regra seguinte.
            var altura = Mathf.Max(15f, corpo.preferredHeight);
            var reto = corpo.rectTransform;
            reto.sizeDelta = new Vector2(reto.sizeDelta.x, altura);

            if (!string.IsNullOrEmpty(item.Numero))
            {
                var numero = Widgets.Texto("n", pagina, 12, TextAnchor.UpperLeft, Cores.Brasa);
                PorNaPagina(numero.rectTransform, MargemDaPagina, y, CalhaDoNumero, 15f);
                numero.text = item.Numero + " ·";
                Widgets.UmaLinha(numero, 9);
            }

            return y - altura - 4f;
        }

        /// <summary>
        /// Ancora um retângulo no canto superior esquerdo da página, em pixels.
        ///
        /// Ancoragem PONTUAL (min = max) de propósito: é ela que deixa
        /// <c>rect.width</c> resolvido na hora, sem esperar o canvas — que é o que
        /// permite perguntar <c>preferredHeight</c> ainda dentro desta função.
        /// </summary>
        static void PorNaPagina(RectTransform reto, float x, float y, float largura, float altura)
        {
            reto.anchorMin = reto.anchorMax = new Vector2(0f, 1f);
            reto.pivot = new Vector2(0f, 1f);
            reto.sizeDelta = new Vector2(largura, altura);
            reto.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>
        /// Os dois carimbos, pousados num suporte em cima da mesa.
        ///
        /// São botões por baixo, mas não parecem: um disco de tinta com a palavra
        /// embaixo, sobre um berço de metal. É a diferença entre apertar um botão
        /// e PEGAR o carimbo — e é a diferença que faz o aluno hesitar meio
        /// segundo antes de indeferir alguém.
        /// </summary>
        void MontarCarimbos()
        {
            var suporte = Peca("suporte");
            var berco = Widgets.Painel("Suporte", _mesa, suporte == null ? Cores.TintaClara : Color.white);
            Widgets.Fixar(berco, new Vector2(0.5f, 0f), new Vector2(ColunaFolhas + 6f, 44f),
                          new Vector2(420f, 88f));
            if (suporte != null) berco.GetComponent<Image>().sprite = suporte;

            Carimbo("Deferir", "carimbo_defere", "DEFERIR", Cores.Folha, -100f, true);
            Carimbo("Indeferir", "carimbo_indefere", "INDEFERIR", Cores.Brasa, 108f, false);
        }

        void Carimbo(string nome, string peca, string rotulo, Color tinta, float dx, bool defere)
        {
            var botao = Widgets.Botao(nome, _mesa, string.Empty, Color.clear, Cores.Papel, 14);
            Widgets.Fixar((RectTransform)botao.transform, new Vector2(0.5f, 0f),
                          new Vector2(ColunaFolhas + dx, 52f), new Vector2(168f, 96f));
            Destroy(botao.GetComponentInChildren<Text>().gameObject);
            botao.onClick.AddListener(() => Decidir(defere));

            var disco = Widgets.Painel("Disco", (RectTransform)botao.transform, tinta);
            Widgets.Fixar(disco, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(58f, 58f));
            var sp = Peca(peca);
            if (sp != null)
            {
                disco.GetComponent<Image>().sprite = sp;
                disco.GetComponent<Image>().color = Color.white;
            }

            var texto = Widgets.Texto("t", (RectTransform)botao.transform, 15,
                                      TextAnchor.LowerCenter, tinta);
            Widgets.Faixa(texto.rectTransform, false, 20f, 4f);
            texto.text = rotulo;
        }

        // ----------------------------------------------------------- a pessoa

        void DesenharRequerente(Requerente r)
        {
            _nomeTexto.text = r.Nome;
            _falaTexto.text = r.EhEmpresa ? "— sem atendimento presencial —" : $"“{r.Fala}”";

            DesenharRosto(r);
            PreencherFormulario(r);
            PreencherComprovacao(r);
        }

        /// <summary>
        /// A pessoa no vão da janela, vinda do mesmo gerador paramétrico que
        /// desenha os treze mestres do ateliê (<c>scripts/arte/elenco.mjs</c>) —
        /// doze requerentes acrescentados ao atlas, sem o avental branco, que ali
        /// marca quem é da casa.
        ///
        /// E é aqui que mora a TRAVA da bancada: o <c>Retrato</c> foi sorteado sem
        /// olhar para bairro, tempo de cidade, espera nem necessidade. Quem tentar
        /// decidir pela cara acerta por acaso — e isso é MEDIDO: a aparência ganha
        /// +1,8pp sobre a taxa-base, contra +1,6pp de um número puramente
        /// aleatório. Dois décimos separam a cara de lixo estatístico. Um jogo
        /// sobre viés em que a fisionomia previsse a resposta estaria ensinando
        /// exatamente o que veio desarmar.
        /// </summary>
        void DesenharRosto(Requerente r)
        {
            foreach (Transform filho in _retrato) Destroy(filho.gameObject);

            if (r.EhEmpresa)
            {
                // Empresa não tem rosto: tem selo. E é assim que ela atravessa o
                // guichê — não há ninguém para olhar, e ninguém a quem perguntar.
                var marca = Widgets.Painel("Selo", _retrato, Color.white);
                Widgets.Fixar(marca, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 96f));
                var selo = Peca("selo");
                if (selo != null) marca.GetComponent<Image>().sprite = selo;
                else marca.GetComponent<Image>().color = Cores.Madeira;
                return;
            }

            var corpo = Widgets.Painel("Pessoa", _retrato, Color.white);
            Widgets.Fixar(corpo, new Vector2(0.5f, 0.5f), new Vector2(0f, -6f),
                          new Vector2(16f * Ampliacao, 24f * Ampliacao));

            var sprite = SpriteDoRequerente(r.Retrato);
            var imagem = corpo.GetComponent<Image>();
            if (sprite != null) imagem.sprite = sprite;
            else imagem.color = Cores.Madeira;   // atlas velho: silhueta, nunca vão vazio
        }

        Sprite SpriteDoRequerente(int indice)
        {
            if (Atlas == null) return null;
            try
            {
                // Direção 0 é de frente, quadro 0 é o apoio: parado, olhando para
                // quem atende. É a pose do balcão.
                return Atlas.Quadro($"r{Mathf.Abs(indice) % Requerentes}", 0, 0);
            }
            catch (ArgumentException) { return null; }
        }

        // ------------------------------------------------- quem chega e quem sai

        /// <summary>
        /// Quanto dura a entrada e quanto dura a saída de uma pessoa.
        ///
        /// A SAÍDA É MAIS CURTA QUE A ENTRADA, e não é capricho: quem sai já foi
        /// decidido e não tem mais nada a dizer, enquanto quem chega precisa de um
        /// instante para o olho pousar no rosto antes de o aluno ir para as folhas.
        /// Fade simétrico faz o guichê parecer lento; assimétrico faz parecer que
        /// a fila anda.
        /// </summary>
        const float EntradaDaPessoa = 0.22f;
        const float SaidaDaPessoa = 0.14f;

        /// <summary>
        /// Depois do carimbo, o tempo em que a tinta fica na tela antes de a
        /// pessoa começar a sumir. Sem esta pausa o carimbo é engolido pelo fade e
        /// o aluno não chega a LER o que ele mesmo decidiu.
        /// </summary>
        const float TintaNaTela = 0.26f;

        /// <summary>
        /// Junta, uma vez por guichê montado, tudo o que some quando a pessoa vai
        /// embora: o rosto no vão, o nome, a fala, as duas folhas e as sombras
        /// delas. O carimbo entra de graça — ele é filho da folha.
        ///
        /// O QUE FICA É O GUICHÊ: parede, mesa, moldura da janela, manual e os
        /// dois carimbos no berço. É isso que faz o fade parecer uma pessoa indo
        /// embora, e não a tela inteira piscando. Se a moldura sumisse junto, seria
        /// um corte de cena; sumindo só quem está dentro dela, é uma fila andando.
        /// </summary>
        void ReunirPresenca()
        {
            _presenca = new[]
            {
                Grupo(_retrato),
                Grupo(_nomeTexto.rectTransform),
                Grupo(_falaTexto.rectTransform),
                Grupo(_folhaFormulario),
                Grupo(_folhaComprovacao),
                Grupo(_sombraFormulario),
                Grupo(_sombraComprovacao)
            };
        }

        static CanvasGroup Grupo(RectTransform alvo)
        {
            if (alvo == null) return null;
            return alvo.GetComponent<CanvasGroup>() ?? alvo.gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>Põe todo mundo na mesma opacidade, de uma vez.</summary>
        void Presenca(float alfa)
        {
            if (_presenca == null) return;
            foreach (var g in _presenca) if (g != null) g.alpha = alfa;
        }

        /// <summary>
        /// A pessoa entra: aparece subindo um triz.
        ///
        /// O deslocamento é de seis pixels e serve para uma coisa só — dar direção
        /// ao aparecimento. Opacidade sozinha faz a pessoa se materializar no ar;
        /// com um resto de movimento, ela CHEGA ao balcão.
        /// </summary>
        IEnumerator Entrando()
        {
            Presenca(0f);
            var t = 0f;
            while (t < EntradaDaPessoa)
            {
                if (!Aberto) yield break;
                t += Time.unscaledDeltaTime;
                var f = Mathf.Clamp01(t / EntradaDaPessoa);
                Presenca(f);
                if (_retrato != null)
                    _retrato.anchoredPosition = new Vector2(0f, Mathf.Lerp(-6f, 0f, f));
                yield return null;
            }
            Presenca(1f);
            if (_retrato != null) _retrato.anchoredPosition = Vector2.zero;
        }

        /// <summary>A pessoa sai: apaga onde está, sem se arrastar pela tela.</summary>
        IEnumerator Saindo()
        {
            var t = 0f;
            while (t < SaidaDaPessoa)
            {
                if (!Aberto) yield break;
                t += Time.unscaledDeltaTime;
                Presenca(1f - Mathf.Clamp01(t / SaidaDaPessoa));
                yield return null;
            }
            Presenca(0f);
        }

        // ------------------------------------------------------------- folhas

        void PreencherFormulario(Requerente r)
        {
            foreach (Transform filho in _folhaFormulario) Destroy(filho.gameObject);
            Cabecalho(_folhaFormulario, r.EhEmpresa ? "PROTOCOLO — PESSOA JURÍDICA" : "FORMULÁRIO");

            if (r.EhEmpresa)
            {
                Linha(_folhaFormulario, 0, "razão social", r.Nome, Cores.Tinta);
                Linha(_folhaFormulario, 1, "pede", r.Pede, Cores.Tinta);
                Linha(_folhaFormulario, 2, "análise", "dispensada", Cores.Madeira);
                return;
            }

            Linha(_folhaFormulario, 0, "bairro", Bairros[r.Bairro], Cores.Tinta);
            Linha(_folhaFormulario, 1, "na cidade há", $"{r.MesesNaCidade} meses", Cores.Tinta);
            Linha(_folhaFormulario, 2, "pede", r.Pede, Cores.Tinta);

            // A ESPERA DECLARADA, e ela é o conserto de um defeito que tornava a
            // bancada injusta: a regra 3 manda indeferir quando «a espera do
            // formulário não for a da comprovação», e o formulário NÃO TRAZIA
            // espera nenhuma. Em 18% dos casos o aluno errava sem ter como saber —
            // num jogo cuja promessa inteira é que dá para acertar todas lendo.
            //
            // Declarada, e não comprovada: esta é a folha em que a pessoa ESCREVE
            // o que diz. A de baixo é a que prova. Quando as duas discordam, é a
            // discordância que decide, e não qualquer uma das duas.
            Linha(_folhaFormulario, 3, "espera declarada", $"{r.Espera} meses", Cores.Tinta);
        }

        void PreencherComprovacao(Requerente r)
        {
            foreach (Transform filho in _folhaComprovacao) Destroy(filho.gameObject);
            Cabecalho(_folhaComprovacao, r.EhEmpresa ? "—" : "COMPROVAÇÃO");

            if (r.EhEmpresa)
            {
                Linha(_folhaComprovacao, 1, "", "não exigida", Cores.Madeira);
                return;
            }

            // O prendedor: diz que esta folha é par da de cima, e é o convite
            // silencioso a ler as duas.
            var clipe = Widgets.Painel("Prendedor", _folhaComprovacao, Color.white);
            Widgets.Fixar(clipe, new Vector2(0f, 1f), new Vector2(40f, 6f), new Vector2(40f, 40f));
            var sp = Peca("grampo");
            if (sp != null) clipe.GetComponent<Image>().sprite = sp;
            else clipe.GetComponent<Image>().color = Cores.Neblina;

            var selo = Widgets.Painel("Selo", _folhaComprovacao, Color.white);
            Widgets.Fixar(selo, new Vector2(1f, 0f), new Vector2(-46f, 44f), new Vector2(56f, 56f));
            var spSelo = Peca("selo");
            if (spSelo != null) selo.GetComponent<Image>().sprite = spSelo;
            else selo.GetComponent<Image>().color = Cores.Luz;

            // "Referente a" existe porque o prazo do manual passou a depender do
            // que se pede — uma fila de creche leva um ano e a de cesta básica
            // leva um mês, e um manual que exigisse o mesmo dos dois seria o
            // absurdo que esta bancada não pode se dar ao luxo de ter.
            //
            // E ela fica AQUI, na comprovação, para a promessa da bancada
            // continuar de pé: dá para acertar todas lendo a folha de baixo.
            Linha(_folhaComprovacao, 0, "referente a", r.Pede, Cores.Tinta);
            Linha(_folhaComprovacao, 1, "espera comprovada", $"{r.EsperaNaFolha} meses", Cores.Tinta);
            Linha(_folhaComprovacao, 2, "necessidade",
                  r.Urgencia == Necessidade.Alta ? "ALTA"
                  : r.Urgencia == Necessidade.Media ? "média" : "baixa",
                  r.Urgencia == Necessidade.Alta ? Cores.Brasa : Cores.Tinta);
            Linha(_folhaComprovacao, 3, "assinatura", r.Nome.Split(' ')[0] + ".", Cores.Madeira);
        }

        static void Cabecalho(RectTransform folha, string texto)
        {
            var faixa = Widgets.Painel("Faixa", folha, new Color(0.83f, 0.77f, 0.65f, 1f));
            Widgets.Faixa(faixa, true, 26f);

            var t = Widgets.Texto("Cabeçalho", faixa, 12, TextAnchor.MiddleLeft, Cores.Madeira);
            Widgets.Esticar(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(16f, 0f);
            t.text = texto;
        }

        /// <summary>Uma linha de campo: rótulo à esquerda, valor à direita.</summary>
        static void Linha(RectTransform folha, int indice, string rotulo, string valor, Color cor)
        {
            var y = -48f - indice * 30f;

            var r = Widgets.Texto($"r{indice}", folha, 13, TextAnchor.MiddleLeft, Cores.Madeira);
            Widgets.Fixar(r.rectTransform, new Vector2(0f, 1f), new Vector2(100f, y), new Vector2(168f, 24f));
            r.text = rotulo;

            var v = Widgets.Texto($"v{indice}", folha, 16, TextAnchor.MiddleLeft, cor);
            Widgets.Fixar(v.rectTransform, new Vector2(0f, 1f), new Vector2(280f, y), new Vector2(196f, 24f));
            Widgets.UmaLinha(v, 11);
            v.text = valor;
        }

        // ------------------------------------------------------------- placar

        /// <summary>O relógio só remonta a string quando o SEGUNDO muda.</summary>
        void AtualizarRelogio(float segundos)
        {
            if (_relogioTexto == null) return;

            var inteiro = Mathf.CeilToInt(segundos);
            if (inteiro != _ultimoSegundo)
            {
                _ultimoSegundo = inteiro;
                _relogioTexto.text = $"{inteiro / 60}:{inteiro % 60:00}";
            }

            // Vermelho no último terço. A cor chega antes do número, que é o
            // mesmo contrato do cartaz de fim de rodada das outras bancadas.
            _relogioTexto.color = segundos < Dias[_dia].Segundos / 3f ? Cores.Brasa : Cores.Luz;
        }

        void AtualizarPlacar()
        {
            if (_placarTexto == null) return;
            var d = Dias[_dia];
            _placarTexto.text = $"fila {_fila.Count - _indice}   ·   {_certos}/{d.Meta}   ·   " +
                                $"advertências {_erros}/{d.Advertencias}";
            Painel.MarcarPasso($"dia {_dia + 1} de {Dias.Length}");
        }

        /// <summary>
        /// O carimbo desce e deixa TINTA NO PAPEL — torto, por cima do que estava
        /// escrito, e ele fica lá até a próxima pessoa chegar.
        ///
        /// A cor é a do que o aluno escolheu, e NÃO a de ele ter acertado: a
        /// resposta certa não é revelada caso a caso. Revelar aqui mataria a
        /// bancada — o aluno passaria a corrigir o atalho em vez de confiar nele,
        /// e a máquina do Ato II herdaria um log limpo que não é o dele. O acerto
        /// aparece uma vez só, no fim do dia, em número.
        /// </summary>
        void Carimbar(bool deferiu, bool errou)
        {
            var alvo = deferiu ? _folhaFormulario : _folhaComprovacao;
            var tinta = deferiu ? Cores.Folha : Cores.Brasa;

            var marca = Widgets.Painel("Tinta", alvo, tinta);
            Widgets.Fixar(marca, new Vector2(0.5f, 0.5f), new Vector2(58f, -14f),
                          new Vector2(104f, 104f));
            marca.localRotation = Quaternion.Euler(0f, 0f, deferiu ? -13f : 11f);

            var sp = Peca(deferiu ? "carimbo_defere" : "carimbo_indefere");
            var img = marca.GetComponent<Image>();
            if (sp != null) { img.sprite = sp; img.color = new Color(1f, 1f, 1f, 0.92f); }

            var palavra = Widgets.Texto("p", marca, 13, TextAnchor.MiddleCenter, tinta);
            Widgets.Esticar(palavra.rectTransform);
            palavra.text = deferiu ? "DEFERIDO" : "INDEFERIDO";
            Widgets.UmaLinha(palavra, 9);

            Widgets.Pulsar(marca, 1.35f, 0.08f);

            // A advertência é a ÚNICA informação que volta durante o expediente, e
            // ela não diz onde foi o erro. É o sinal sem direção da bancada 6,
            // reaparecendo na última bancada da aula.
            if (errou) Widgets.Tremer(_mesa, 6f, 0.18f);
        }
    }
}
