using UnityEngine;
using UnityEngine.UI;
using FabricaDeIA.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// O tabuleiro da bancada 3, desenhado como TEXTURA — um pixel por casinha.
    ///
    /// Na última rodada são 120×120, ou seja catorze mil e quatrocentas casas.
    /// Um objeto de interface por casa travaria o jogo antes de desenhar a
    /// primeira. Uma textura de 120 por 120 pixels custa nada.
    ///
    /// O efeito colateral é bonito: quando o aluno toma uma região grande, ele vê
    /// a mancha crescer como tinta, e não como uma lista de casinhas mudando de
    /// cor uma a uma.
    ///
    /// A TELA NÃO MOSTRA A TEXTURA INTEIRA.
    ///
    /// A textura é sempre <c>_lado × _lado</c> — a tabela completa —, mas o que
    /// aparece nos 430 px fixos da tela é só um RECORTE dela, do tamanho da
    /// rodada 1 (ver <see cref="DesafioArquivo.Rodadas3"/>[0]), seguindo o
    /// bonequinho. É <see cref="RawImage.uvRect"/> quem faz o corte — a mesma
    /// textura, uma janela menor sobre ela — e é por isso que a casinha vale
    /// sempre os mesmos ~21,5 px de tela em qualquer rodada, e não encolhe mais.
    /// Ver <see cref="AtualizarJanela"/>.
    ///
    /// Como o aluno deixa de ver o todo, dois remendos:
    ///
    ///   1. Um MINIMAPA no canto mostra a textura inteira, sem recorte — a
    ///      mesma tabela, com a mesma tinta, só que cabendo na tela. É onde ele
    ///      enxerga a região já tomada e onde ele está dentro dela.
    ///   2. No fecho de cada rodada, <see cref="MostrarTabelaInteira"/> larga o
    ///      recorte e mostra a tabela completa — minúscula — antes do cartaz de
    ///      resultado cobrir tudo. É o afastamento que prova, sozinho, que a
    ///      fatia jogada era pequena perto do total.
    /// </summary>
    public partial class DesafioArquivo
    {
        const float LadoNaTela = 430f;

        /// <summary>Tamanho do quadradinho do minimapa, sem contar a moldura.</summary>
        const float LadoDoMinimapa = 96f;

        RawImage _tela;
        Texture2D _textura;
        Text _placar;

        RectTransform _minimapaFundo;
        RawImage _minimapa;
        RectTransform _janelaNoMinimapa;
        RectTransform _marcador;

        void MontarTela()
        {
            if (_tela != null) Destroy(_tela.gameObject);

            var objeto = new GameObject("Tabela", typeof(RectTransform), typeof(RawImage));
            objeto.transform.SetParent(Area, false);
            _tela = objeto.GetComponent<RawImage>();

            Widgets.Fixar((RectTransform)objeto.transform, new Vector2(0.5f, 0.5f),
                          new Vector2(0f, 16f), new Vector2(LadoNaTela, LadoNaTela));

            _textura = new Texture2D(_lado, _lado, TextureFormat.RGBA32, false)
            {
                // Ponto, e não bilinear: casinha tem que ter borda dura. Com
                // filtro suave a mancha do território vira borrão e o aluno perde
                // a conta do que já é dele.
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _tela.texture = _textura;
            _tela.uvRect = new Rect(0f, 0f, 1f, 1f);

            if (_placar == null)
            {
                _placar = Widgets.Texto("Placar", Area, 16, TextAnchor.LowerCenter, Cores.Papel);
                Widgets.Faixa(_placar.rectTransform, false, 24f, 4f);
            }

            MontarMinimapa();
            Repintar();
        }

        /// <summary>
        /// O mapa pequeno do canto: a textura inteira, sem recorte nenhum — a
        /// mesma tabela que a tela principal desenha, só que cabendo por inteiro.
        ///
        /// Só existe da rodada 2 em diante. Na rodada 1 a janela da tela
        /// principal JÁ é a tabela inteira (ver <see cref="AtualizarJanela"/>), e
        /// um minimapa mostrando de novo o que a tela grande já mostra não
        /// orienta ninguém — só ocuparia canto à toa.
        /// </summary>
        void MontarMinimapa()
        {
            if (_minimapaFundo != null) Destroy(_minimapaFundo.gameObject);
            _minimapa = null;
            _janelaNoMinimapa = null;
            _marcador = null;

            if (_lado <= Rodadas3[0]) return;

            _minimapaFundo = Widgets.Painel("MinimapaFundo", Area, Cores.TintaClara);
            Widgets.Fixar(_minimapaFundo, new Vector2(1f, 1f), new Vector2(-16f, -16f),
                          new Vector2(LadoDoMinimapa + 8f, LadoDoMinimapa + 8f));

            var objeto = new GameObject("Minimapa", typeof(RectTransform), typeof(RawImage));
            objeto.transform.SetParent(_minimapaFundo, false);
            Widgets.Esticar((RectTransform)objeto.transform, 4f);

            _minimapa = objeto.GetComponent<RawImage>();
            _minimapa.texture = _textura;
            _minimapa.uvRect = new Rect(0f, 0f, 1f, 1f);

            // A moldura: um retalho translúcido do TAMANHO da janela visível,
            // sobre o minimapa. Não é decoração — é a resposta a "que pedaço
            // desse mapa inteiro eu estou vendo na tela grande".
            _janelaNoMinimapa = Widgets.Painel("Janela",
                objeto.transform, new Color(Cores.Luz.r, Cores.Luz.g, Cores.Luz.b, 0.35f));
            _janelaNoMinimapa.GetComponent<Image>().raycastTarget = false;

            // O ponto do boneco. A textura já carrega um pixel Papel na posição
            // dele (ver Repintar), mas num minimapa de ~120 casinhas em 96px
            // esse pixel é menor que um pixel de tela — pode simplesmente não
            // aparecer. Esta marca não depende de escala.
            _marcador = Widgets.Painel("Boneco", objeto.transform, Cores.Papel);
            _marcador.GetComponent<Image>().raycastTarget = false;
            _marcador.pivot = new Vector2(0.5f, 0.5f);
            _marcador.sizeDelta = new Vector2(6f, 6f);
        }

        /// <summary>
        /// Repinta a tabela inteira.
        ///
        /// Inteira mesmo, e não só o que mudou: são no máximo catorze mil pixels
        /// num vetor contíguo, o que o processador faz sem suar, e a alternativa
        /// (rastrear casas sujas) seria mais código para ganhar tempo que ninguém
        /// está perdendo.
        /// </summary>
        void Repintar()
        {
            if (_textura == null) return;

            var pixels = new Color32[_lado * _lado];
            for (var y = 0; y < _lado; y++)
            {
                for (var x = 0; x < _lado; x++)
                {
                    // A textura conta as linhas de baixo para cima; a tabela conta
                    // de cima para baixo, como se lê. Inverter aqui é o que faz o
                    // canto da base aparecer no alto à esquerda, onde o aluno o vê.
                    pixels[(_lado - 1 - y) * _lado + x] = _tabela[x, y] switch
                    {
                        Casa.Meu => (Color32)Cores.Vidro,
                        Casa.Trilha => (Color32)Cores.Luz,
                        Casa.Risco => (Color32)Cores.Brasa,
                        _ => (Color32)Cores.TintaClara
                    };
                }
            }

            // O bonequinho, por cima de tudo.
            pixels[(_lado - 1 - _onde.y) * _lado + _onde.x] = (Color32)Cores.Papel;

            _textura.SetPixels32(pixels);
            _textura.Apply(false);

            AtualizarJanela();
            DesenharPlacar();
        }

        /// <summary>
        /// Recorta a textura numa janela de <c>Rodadas3[0]</c> casinhas ao redor
        /// do bonequinho — a câmera que segue o boneco.
        ///
        /// A tabela pode ter até 120×120 casinhas, mas os 430 px da tela sempre
        /// mostram uma janela de <c>Rodadas3[0]</c> = 20 casinhas de lado. Por
        /// isso a casinha vale sempre ~21,5 px (430/20), na rodada 1, na 2 e na 3
        /// — o tamanho que o primeiro teste com gente já validou. O que muda de
        /// rodada para rodada é só quanto da tabela fica FORA da janela.
        ///
        /// NA RODADA 1 A CÂMERA NÃO SE MOVE, e isso é intencional, não um
        /// acidente da conta: <c>_lado == Rodadas3[0]</c> nessa rodada, então o
        /// clamp abaixo só tem o valor 0 para dar em qualquer eixo — a janela
        /// cobre a tabela inteira e fica parada, exatamente o comportamento que
        /// já existia antes desta bancada ganhar câmera. Nenhuma rodada tem
        /// tratamento especial no código: é a mesma fórmula para as três, e na
        /// rodada 1 o intervalo de destino do clamp degenera num ponto só.
        /// </summary>
        void AtualizarJanela()
        {
            var janela = Mathf.Min(Rodadas3[0], _lado);
            var colEsquerda = Mathf.Clamp(_onde.x - janela / 2, 0, _lado - janela);
            var linhaTopo = Mathf.Clamp(_onde.y - janela / 2, 0, _lado - janela);

            // V cresce de baixo para cima (convenção de textura), e a linha 0 da
            // tabela mora no TOPO da tela (ver o comentário de Cima/Baixo em
            // DesafioArquivo.cs) — por isso é a linha de BAIXO da janela que dá o
            // V menor (a base do retângulo de recorte).
            var linhaAbaixoDaJanela = linhaTopo + janela;
            var u = (float)colEsquerda / _lado;
            var v = (float)(_lado - linhaAbaixoDaJanela) / _lado;
            var fracao = (float)janela / _lado;

            _tela.uvRect = new Rect(u, v, fracao, fracao);
            PosicionarNoMinimapa(u, v, fracao);
        }

        void PosicionarNoMinimapa(float u, float v, float fracao)
        {
            if (_janelaNoMinimapa == null) return;

            _janelaNoMinimapa.anchorMin = new Vector2(u, v);
            _janelaNoMinimapa.anchorMax = new Vector2(u + fracao, v + fracao);
            _janelaNoMinimapa.offsetMin = Vector2.zero;
            _janelaNoMinimapa.offsetMax = Vector2.zero;

            // Centro da casinha do boneco, não o canto — senão a marca fica meia
            // casinha deslocada da moldura que ela devia acompanhar.
            var centroU = (_onde.x + 0.5f) / _lado;
            var centroV = (_lado - 1 - _onde.y + 0.5f) / _lado;
            _marcador.anchorMin = _marcador.anchorMax = new Vector2(centroU, centroV);
            _marcador.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// O afastamento do fecho: larga o recorte e mostra a tabela inteira,
        /// minúscula — do jeito que ela sempre foi, e que a janela escondeu de
        /// propósito durante o jogo.
        ///
        /// Chamado por <see cref="DesafioArquivo.FecharRodada"/> assim que a
        /// rodada é vencida, antes do cartaz de resultado cobrir a tela. É o
        /// argumento visual mais forte que a bancada tem, e só funciona porque
        /// até aqui o aluno não tinha visto o todo.
        /// </summary>
        void MostrarTabelaInteira()
        {
            if (_tela != null) _tela.uvRect = new Rect(0f, 0f, 1f, 1f);
            if (_minimapaFundo != null) _minimapaFundo.gameObject.SetActive(false);
        }

        void DesenharPlacar()
        {
            if (_placar == null) return;
            var pct = Mathf.RoundToInt(Fracao * 100f);
            var falta = Mathf.RoundToInt(Meta[NivelAtual] * 100f);
            _placar.text = $"tomou {pct}%   ·   precisa de {falta}%   ·   " +
                           $"{_lado}×{_lado} casinhas   ·   {_riscos} pares escritos";
        }
    }
}
