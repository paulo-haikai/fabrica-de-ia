using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FabricaDeIA.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A pista da bancada 6 e os botões de calibração.
    ///
    /// A pista é desenhada com retângulos de interface, não com física: são
    /// quatro ou cinco caixas andando para a esquerda. Física de verdade traria
    /// gravidade, colisor e camada, e nada disso apareceria na tela — o aluno
    /// veria exatamente o mesmo robô pulando, ao custo de um sistema inteiro a
    /// mais para alguém manter.
    /// </summary>
    public partial class DesafioErro
    {
        RectTransform _pista;
        RectTransform _robo;
        RectTransform _painelDeAjustes;
        Text _relogioNaTela;
        Text _contador;
        readonly List<RectTransform> _caixas = new();

        static Sprite _emPe, _agachado;

        /// <summary>
        /// O robô, desenhado em pixel a pixel no código.
        ///
        /// Ele era um retângulo amarelo, e retângulo não ganha ninguém: o aluno
        /// precisa TORCER por essa lata para se importar com a calibração dela.
        /// Antena, dois olhos e uma grade de boca já bastam — a cara faz o resto.
        ///
        /// Gerado aqui e não gerado pelos scripts de arte porque são dezoito
        /// linhas de texto e nenhum arquivo novo no repositório; a pasta de arte
        /// existe para os atlas grandes, não para dois bonecos.
        ///
        /// 1 = contorno, 2 = corpo, e = olho.
        /// </summary>
        static Sprite Boneco(bool abaixado)
        {
            if (abaixado && _agachado != null) return _agachado;
            if (!abaixado && _emPe != null) return _emPe;

            string[] linhas = abaixado
                ? new[]
                {
                    "..1......1..",
                    "..1......1..",
                    ".1111111111.",
                    "12222222222 ",
                    "12e2222e222.",
                    "12222222222.",
                    "12111111122.",
                    ".1111111111.",
                    "..11....11..",
                }
                : new[]
                {
                    "....1..1....",
                    "....1..1....",
                    "..11111111..",
                    ".1222222221.",
                    ".12e222e221.",
                    ".1222222221.",
                    ".1211111121.",
                    ".1222222221.",
                    "..11111111..",
                    "...122221...",
                    "..12222221..",
                    ".122222221..",
                    ".122222221..",
                    ".111111111..",
                    "..11....11..",
                    "..11....11..",
                };

            var largura = 12;
            var altura = linhas.Length;
            var textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < altura; y++)
            {
                for (var x = 0; x < largura; x++)
                {
                    var ch = x < linhas[y].Length ? linhas[y][x] : '.';
                    textura.SetPixel(x, altura - 1 - y, ch switch
                    {
                        '1' => Cores.TintaOpaca,
                        '2' => Cores.Luz,
                        'e' => Cores.Vidro,
                        _ => Color.clear
                    });
                }
            }
            textura.Apply();

            var sprite = Sprite.Create(textura, new Rect(0, 0, largura, altura),
                                       new Vector2(0.5f, 0.5f), 16f);
            if (abaixado) _agachado = sprite; else _emPe = sprite;
            return sprite;
        }

        void MontarTela()
        {
            _pista = Widgets.Painel("Pista", Area, Cores.TintaClara);
            Widgets.Fixar(_pista, new Vector2(0.5f, 0.5f), new Vector2(0f, 54f),
                          new Vector2(LarguraPista, AlturaPista));

            // O chão, para o pulo ter de onde sair.
            var chao = Widgets.Painel("Chao", _pista, Cores.Neblina);
            Widgets.Fixar(chao, new Vector2(0.5f, 0.5f),
                          new Vector2(0f, -AlturaPista / 2f + 18f),
                          new Vector2(LarguraPista, 3f));

            _robo = Widgets.Painel("Robo", _pista, Color.white);
            var imagemDoRobo = _robo.GetComponent<Image>();
            imagemDoRobo.sprite = Boneco(false);
            imagemDoRobo.preserveAspect = true;
            Widgets.Fixar(_robo, new Vector2(0.5f, 0.5f),
                          new Vector2(XdoRobo, Chao(AlturaRobo)),
                          new Vector2(LarguraRobo, AlturaRobo));

            _relogioNaTela = Widgets.Texto("Relogio", _pista, 15, TextAnchor.UpperRight, Cores.Papel);
            Widgets.Faixa(_relogioNaTela.rectTransform, true, 22f, 10f);

            _contador = Widgets.Texto("Contador", Area, 15, TextAnchor.LowerCenter, Cores.Papel);
            Widgets.Faixa(_contador.rectTransform, false, 22f, 4f);

            foreach (var caixa in _caixas) if (caixa != null) Destroy(caixa.gameObject);
            _caixas.Clear();

            DesenharPainel();
        }

        /// <summary>Onde fica a base de algo com esta altura, apoiado no chão.</summary>
        static float Chao(float altura) => -AlturaPista / 2f + 18f + altura / 2f;

        // -------------------------------------------------------- a calibração

        /// <summary>
        /// Os números do robô, com − e + de cada lado.
        ///
        /// Botão e não barra deslizante: o aluno precisa saber QUANTO mexeu para
        /// ligar a mudança ao resultado. Arrastar uma barra dá um número que ele
        /// não escolheu e não consegue repetir — e sem repetir não há calibração,
        /// há tentativa.
        /// </summary>
        void DesenharPainel()
        {
            if (_painelDeAjustes != null) Destroy(_painelDeAjustes.gameObject);

            _painelDeAjustes = Widgets.Painel("Ajustes", Area, Color.clear);
            Widgets.Fixar(_painelDeAjustes, new Vector2(0.5f, 0.5f), new Vector2(0f, -104f),
                          new Vector2(LarguraPista, 128f));

            var titulo = Widgets.Texto("TituloAjuste", _painelDeAjustes, 14,
                                       TextAnchor.UpperCenter, Cores.Neblina);
            Widgets.Faixa(titulo.rectTransform, true, 20f);
            titulo.text = "A MARGEM DE ERRO DELE";

            var linha = 0;
            Ajuste(linha++, "salta quando está a", _margemDoPulo, "de distância",
                   d => _margemDoPulo = Mathf.Clamp(_margemDoPulo + d * 10f, 30f, 320f),
                   _r.baixos);

            if (_r.botoes >= 2)
            {
                Ajuste(linha++, "abaixa quando está a", _margemDoAbaixar, "de distância",
                       d => _margemDoAbaixar = Mathf.Clamp(_margemDoAbaixar + d * 10f, 30f, 320f),
                       _r.altos);

                Ajuste(linha, "fica abaixado por", _tempoAbaixado * 100f, "centésimos",
                       d => _tempoAbaixado = Mathf.Clamp(_tempoAbaixado + d * 0.05f, 0.1f, 0.9f),
                       _r.altos);
            }

            // UM BOTÃO SÓ, QUE SOLTA E PARA.
            //
            // Antes só dava para calibrar entre uma corrida e outra, e isso obriga
            // o aluno a assistir dez segundos de erro que ele já entendeu no
            // segundo dois. Poder parar no instante em que vê a batida é o que
            // torna a calibração um diálogo em vez de um formulário.
            var rodando = _correndo;
            var botao = Widgets.Botao("Soltar", Area,
                                      rodando ? "parar e calibrar" : "soltar o robô  ▸",
                                      rodando ? Cores.Brasa : Cores.Folha, Cores.Papel, 17);
            Widgets.Fixar((RectTransform)botao.transform, new Vector2(0.5f, 0.5f),
                          new Vector2(0f, -184f), new Vector2(230f, 44f));
            botao.onClick.AddListener(() => { if (_correndo) Parar(); else Soltar(); });
        }

        void Ajuste(int linha, string antes, float valor, string depois,
                    System.Action<int> mexer, bool ativo)
        {
            var y = 34f - linha * 40f;
            var cor = ativo ? Cores.Papel : Cores.Neblina;

            var texto = Widgets.Texto($"aj{linha}", _painelDeAjustes, 15,
                                      TextAnchor.MiddleCenter, cor);
            Widgets.Fixar(texto.rectTransform, new Vector2(0.5f, 0.5f),
                          new Vector2(0f, y), new Vector2(520f, 32f));
            texto.text = $"{antes}  {valor:0}  {depois}";

            if (!ativo) return;

            Seta(-190f, y, "−", () => mexer(-1));
            Seta(190f, y, "+", () => mexer(+1));
        }

        void Seta(float x, float y, string rotulo, System.Action acao)
        {
            var botao = Widgets.Botao($"s{x}_{y}", _painelDeAjustes, rotulo,
                                      Cores.TintaClara, Cores.Papel, 18);
            Widgets.Fixar((RectTransform)botao.transform, new Vector2(0.5f, 0.5f),
                          new Vector2(x, y), new Vector2(40f, 32f));
            botao.onClick.AddListener(() =>
            {
                if (_correndo || _travado) return;
                acao();
                DesenharPainel();
            });
        }

        // ------------------------------------------------------------ a corrida

        void DesenharCorrida()
        {
            // Uma caixa por obstáculo, reaproveitadas: a pista cria no máximo umas
            // cinco de cada vez, e destruir e recriar a cada quadro daria lixo de
            // memória num laço que roda sessenta vezes por segundo.
            while (_caixas.Count < _obstaculos.Count)
            {
                var nova = Widgets.Painel($"obst{_caixas.Count}", _pista, Cores.Brasa);
                _caixas.Add(nova);
            }

            for (var i = 0; i < _caixas.Count; i++)
            {
                if (i >= _obstaculos.Count) { _caixas[i].gameObject.SetActive(false); continue; }

                var o = _obstaculos[i];
                _caixas[i].gameObject.SetActive(true);
                _caixas[i].GetComponent<Image>().color = o.Alto ? Cores.Vidro : Cores.Brasa;

                // O alto voa na altura da cabeça; o baixo fica no chão.
                var altura = o.Alto ? 26f : 40f;
                var y = o.Alto ? Chao(AlturaRobo) + 26f : Chao(altura);
                Widgets.Fixar(_caixas[i], new Vector2(0.5f, 0.5f),
                              new Vector2(o.X, y), new Vector2(30f, altura));
            }

            var alturaAtual = Abaixado ? AlturaAbaixado : AlturaRobo;
            var largura = Abaixado ? LarguraRobo * 1.35f : LarguraRobo;
            Widgets.Fixar(_robo, new Vector2(0.5f, 0.5f),
                          new Vector2(XdoRobo, Chao(alturaAtual) + _alturaDoRobo),
                          new Vector2(largura, alturaAtual));

            var imagem = _robo.GetComponent<Image>();
            imagem.sprite = Boneco(Abaixado);
            // Tinge de brasa depois de bater. Tingir e nao trocar de sprite porque
            // a batida precisa ser lida de relance, e cor chega antes de desenho.
            imagem.color = _batidas > 0 ? new Color(1f, 0.72f, 0.62f) : Color.white;

            _relogioNaTela.text = $"{Mathf.Max(0f, Duracao - _relogio):0.0}s";
            _contador.text = $"desviou {_desviados}   ·   bateu {_batidas}";
        }
    }
}
