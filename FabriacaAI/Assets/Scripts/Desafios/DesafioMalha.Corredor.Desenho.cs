using System.Collections.Generic;
using FabricaDeIA.Arte;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// O desenho do corredor. A arte é daqui; o traçado e a mecânica são do
    /// FableDevil, de Leonxlnx (ver TERCEIROS.md).
    ///
    /// AQUI MORAVA O TRAVAMENTO. Uma versão anterior desenhava o cenário como uma
    /// grade de casas com câmera, e a cada casa que a câmera andava trocava o sprite
    /// de todas elas: quatrocentas e quarenta imagens sujas oito vezes por segundo
    /// obrigam o uGUI a remontar a malha de vértices do canvas inteiro. Era isso que
    /// se sentia ao caminhar.
    ///
    /// Agora a sala é uma tela só e cada peça tem UMA imagem, criada quando a sala
    /// monta. Por quadro só se mexe no que de fato anda — o boneco, o elevador, o
    /// martelo, a serra — e mexer num transforme não suja o canvas.
    ///
    /// A CONVERSÃO DE EIXO ACONTECE SÓ AQUI, em <see cref="Por"/>. O mundo inteiro
    /// pensa com o Y para baixo, como o canvas do original, para as centenas de
    /// coordenadas das trinta salas entrarem sem tradução. Uma única função vira o
    /// eixo para o jeito da Unity, e é o lugar certo para essa dívida ficar.
    /// </summary>
    public partial class DesafioMalha
    {
        static readonly Color Latao = new(0.612f, 0.443f, 0.157f);
        static readonly Color LataoLuz = new(0.784f, 0.604f, 0.235f);
        static readonly Color Contorno = new(0.106f, 0.122f, 0.165f);
        static readonly Color FundoDaSala = new(0.157f, 0.161f, 0.204f);
        static readonly Color Ferro = new(0.373f, 0.416f, 0.502f);

        RectTransform _janelaDoCorredor;
        RectTransform _palco;
        Text _placar;

        Image _portaAlta, _portaBaixa;

        /// <summary>
        /// A IMAGEM do boneco. O boneco em si — posicao, velocidade, caixa — vive em
        /// DesafioMalha.Corredor.cs como dado puro, sem nada de tela.
        ///
        /// Nomes diferentes de proposito: os dois ja colidiram, e a separacao entre o
        /// que a fisica move e o que o desenho mostra e justamente o que permite rodar
        /// a fisica em passo fixo de 120 Hz e desenhar uma vez por quadro.
        /// </summary>
        Image _bonecoNaTela;

        /// <summary>O que precisa ser reposicionado a cada quadro.</summary>
        readonly List<System.Action> _animar = new();

        Folhas _folhas;
        float _escala;
        Sprite _spEspinho, _spEspinhoTeto, _spBonecoA, _spBonecoB;
        Sprite _spPortaAlta, _spPortaBaixa, _spPortaAltaAberta, _spPortaBaixaAberta;

        // ------------------------------------------------------------- montagem

        void MontarCorredorNaTela()
        {
            _folhas ??= Folhas.Atual;
            CarregarSprites();

            foreach (Transform filho in Area) Destroy(filho.gameObject);
            _animar.Clear();

            _janelaDoCorredor = Widgets.Painel("Sala", Area, FundoDaSala);
            Widgets.Esticar(_janelaDoCorredor);
            _janelaDoCorredor.offsetMax = new Vector2(-6f, -6f);
            _janelaDoCorredor.offsetMin = new Vector2(6f, 22f);

            _placar = Widgets.Texto("Placar", Area, 13, TextAnchor.LowerCenter, Cores.Neblina);
            Widgets.Faixa(_placar.rectTransform, false, 18f, 3f);

            Canvas.ForceUpdateCanvases();
            var espaco = _janelaDoCorredor.rect.size;
            if (espaco.x < 1f) { Canvas.ForceUpdateCanvases(); espaco = _janelaDoCorredor.rect.size; }

            _escala = Mathf.Min(espaco.x / Larg, espaco.y / Alto);

            _palco = Widgets.Painel("Palco", _janelaDoCorredor, Color.clear);
            Widgets.Fixar(_palco, new Vector2(0.5f, 0.5f), Vector2.zero,
                          new Vector2(Larg * _escala, Alto * _escala));

            foreach (var s in _solidosDaSala) Bloco(s, Latao, true);
            foreach (var a in _truques) Desenhar(a);
            DesenharSaida();
            DesenharBoneco();

            Repintar();
        }

        void CarregarSprites()
        {
            if (_spEspinho != null) return;
            _spEspinho = _folhas.Corredor("espinhos");
            _spEspinhoTeto = _folhas.Corredor("espinhos_teto");
            _spBonecoA = _folhas.Corredor("mensageiro_0");
            _spBonecoB = _folhas.Corredor("mensageiro_1");
            _spPortaAlta = _folhas.Corredor("porta_alta");
            _spPortaBaixa = _folhas.Corredor("porta_baixa");
            _spPortaAltaAberta = _folhas.Corredor("porta_alta_aberta");
            _spPortaBaixaAberta = _folhas.Corredor("porta_baixa_aberta");
        }

        /// <summary>
        /// Põe um retângulo do mundo na tela. É AQUI que o eixo Y vira.
        /// </summary>
        void Por(RectTransform r, Ret c)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.zero;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(c.l * _escala, c.a * _escala);
            r.anchoredPosition = new Vector2((c.x + c.l * 0.5f) * _escala,
                                             (Alto - (c.y + c.a * 0.5f)) * _escala);
        }

        RectTransform Bloco(Ret c, Color cor, bool comLuz = false, string nome = "bloco")
        {
            // Fora da tela não se desenha: as paredes das pontas são metade dos
            // sólidos de toda sala e nenhuma delas aparece.
            if (c.Direita <= 0f || c.x >= Larg || c.Base <= 0f || c.y >= Alto) return null;

            var fora = Widgets.Painel(nome, _palco, Contorno);
            Por(fora, c);

            var dentro = Widgets.Painel("miolo", fora, cor);
            Widgets.Esticar(dentro, Mathf.Max(1.5f, 2f * _escala));

            if (!comLuz) return fora;

            var luz = Widgets.Painel("luz", fora, LataoLuz);
            Widgets.Fixar(luz, new Vector2(0.5f, 1f),
                          new Vector2(0f, -Mathf.Max(2f, 3f * _escala)),
                          new Vector2(c.l * _escala - 4f, Mathf.Max(2f, 3f * _escala)));
            return fora;
        }

        /// <summary>Uma fileira de espinhos, um sprite por dezoito pixels de mundo.</summary>
        RectTransform Espinhos(Ret c, bool paraBaixo, Transform pai = null)
        {
            var caixa = Widgets.Painel("espinhos", pai == null ? _palco : pai, Color.clear);
            Por(caixa, c);

            var quantos = Mathf.Max(2, Mathf.RoundToInt(c.l / 18f));
            for (var i = 0; i < quantos; i++)
            {
                var o = new GameObject($"p{i}", typeof(RectTransform), typeof(Image));
                o.transform.SetParent(caixa, false);
                var img = o.GetComponent<Image>();
                img.sprite = paraBaixo ? _spEspinhoTeto : _spEspinho;
                img.raycastTarget = false;

                var r = (RectTransform)o.transform;
                r.anchorMin = new Vector2((float)i / quantos, 0f);
                r.anchorMax = new Vector2((i + 1f) / quantos, 1f);
                r.offsetMin = Vector2.zero;
                r.offsetMax = Vector2.zero;
            }
            return caixa;
        }

        // ------------------------------------------------------- uma por armadilha

        void Desenhar(Armadilha a)
        {
            switch (a)
            {
                case ChaoQueCede c:
                {
                    var r = Bloco(c.Caixa, Latao, true, "cede");
                    if (r != null) _animar.Add(() => Por(r, c.Agora));
                    break;
                }

                case EspinhosQueSobem e:
                {
                    var caixa = Widgets.Painel("sobem", _palco, Color.clear);
                    var dentro = Espinhos(new Ret(e.x, e.y - e.Tamanho, e.l, e.Tamanho),
                                          e.ParaBaixo, caixa);
                    Widgets.Esticar(dentro);
                    caixa.gameObject.SetActive(false);
                    _animar.Add(() =>
                    {
                        var fora = e.Fora > 0.02f;
                        if (caixa.gameObject.activeSelf != fora) caixa.gameObject.SetActive(fora);
                        if (!fora) return;
                        var h = e.Tamanho * e.Fora;
                        Por(caixa, e.ParaBaixo
                            ? new Ret(e.x, e.y, e.l, h)
                            : new Ret(e.x, e.y - h, e.l, h));
                    });
                    break;
                }

                case BlocoQueCai b:
                {
                    var r = Bloco(b.Casa, Cores.Brasa, false, "cai");
                    if (r != null) _animar.Add(() => Por(r, b.Caixa));
                    break;
                }

                case Esmagador m:
                {
                    // A haste, presa no teto, e a cabeça que desce.
                    var haste = Widgets.Painel("haste", _palco, Ferro);
                    var cabeca = Bloco(m.Cabeca, Cores.Brasa, false, "martelo");
                    _animar.Add(() =>
                    {
                        if (cabeca != null) Por(cabeca, m.Cabeca);
                        Por(haste, new Ret(m.x + m.l * 0.5f - 6f, m.TopoY,
                                           12f, Mathf.Max(1f, m.y - m.TopoY)));
                    });
                    break;
                }

                case PlataformaQueEsfarela p:
                {
                    var r = Bloco(p.Casa, Cores.Vidro, true, "esfarela");
                    if (r != null) _animar.Add(() => Por(r, p.Caixa));
                    break;
                }

                case BuracoCorredico h:
                {
                    // Dois pedaços de piso que abrem e fecham: são recolocados por
                    // quadro porque o vão persegue o jogador.
                    var e = Bloco(new Ret(0f, h.y, 10f, h.a), Latao, true, "pisoE");
                    var d = Bloco(new Ret(0f, h.y, 10f, h.a), Latao, true, "pisoD");
                    _animar.Add(() =>
                    {
                        var lados = h.Solidos();
                        if (e != null) Mostrar(e, lados.Count > 0 ? lados[0] : (Ret?)null);
                        if (d != null) Mostrar(d, lados.Count > 1 ? lados[1] : (Ret?)null);
                    });
                    break;
                }

                case EspinhosFixos f:
                    Espinhos(f.ParaBaixo
                        ? new Ret(f.x, f.y, f.l, f.Tamanho)
                        : new Ret(f.x, f.y - f.Tamanho, f.l, f.Tamanho), f.ParaBaixo);
                    break;

                case ZonaInvertida z:
                {
                    var zona = Widgets.Painel("aoContrario", _palco,
                        new Color(Cores.Vidro.r, Cores.Vidro.g, Cores.Vidro.b, 0.14f));
                    Por(zona, z.Caixa);
                    var aviso = Widgets.Texto("aviso", zona, 15, TextAnchor.UpperCenter, Cores.Vidro);
                    Widgets.Faixa(aviso.rectTransform, true, 22f, 12f);
                    aviso.text = "◄ ao contrário ►";
                    break;
                }

                case PlataformaMovel p:
                {
                    var r = Bloco(new Ret(p.ax, p.ay, p.l, p.a), LataoLuz, true, "movel");
                    if (r != null) _animar.Add(() => Por(r, new Ret(p.px, p.py, p.l, p.a)));
                    break;
                }

                case Esteira c:
                {
                    var r = Bloco(c.Caixa, Cores.Vidro, true, "esteira");
                    if (r == null) break;
                    var seta = Widgets.Texto("rumo", r, 18, TextAnchor.MiddleCenter, Cores.Tinta);
                    Widgets.Esticar(seta.rectTransform);
                    seta.text = c.Rumo > 0 ? "» » » »" : "« « « «";
                    break;
                }

                case Mola m2:
                {
                    var r = Bloco(new Ret(m2.x, m2.y - m2.a, m2.l, m2.a), Cores.Folha, true, "mola");
                    if (r != null)
                        _animar.Add(() => Por(r, new Ret(m2.x, m2.y - m2.a + m2.c * 6f,
                                                         m2.l, m2.a - m2.c * 6f)));
                    break;
                }

                case Serra s:
                {
                    var r = Bloco(new Ret(s.x - s.Raio, s.y - s.Raio, s.Raio * 2f, s.Raio * 2f),
                                  Cores.Brasa, false, "serra");
                    if (r == null) break;
                    _animar.Add(() =>
                    {
                        Por(r, new Ret(s.x - s.Raio, s.y - s.Raio, s.Raio * 2f, s.Raio * 2f));
                        r.localRotation = Quaternion.Euler(0f, 0f, -s.giro * Mathf.Rad2Deg);
                    });
                    break;
                }

                case Raio l2:
                {
                    var feixe = Widgets.Painel("raio", _palco, Cores.Brasa);
                    Por(feixe, l2.Feixe);
                    _animar.Add(() =>
                    {
                        var e2 = l2.Estado;
                        var img = feixe.GetComponent<Image>();
                        img.enabled = e2 != "apagado";
                        // O aviso é fino e translúcido; o tiro é grosso e opaco. É a
                        // diferença que dá ao jogador o meio segundo de que precisa.
                        img.color = e2 == "aviso"
                            ? new Color(Cores.Brasa.r, Cores.Brasa.g, Cores.Brasa.b, 0.35f)
                            : Cores.Luz;
                        var b = l2.Feixe;
                        Por(feixe, e2 == "aviso"
                            ? new Ret(b.x + b.l * 0.35f, b.y, b.l * 0.3f, b.a)
                            : b);
                    });
                    break;
                }

                case Portal t:
                    Bloco(t.A, Cores.Vidro, false, "portalA");
                    Bloco(t.B, Cores.Vidro, false, "portalB");
                    break;

                case Botao b2:
                {
                    var r = Bloco(new Ret(b2.x, b2.y, b2.l, b2.a), Cores.Luz, false, "botao");
                    if (r != null)
                        _animar.Add(() => Por(r, new Ret(b2.x, b2.y + b2.afunda * 5f, b2.l, b2.a)));
                    break;
                }

                case PortaoDeChave g:
                {
                    var r = Bloco(g.Caixa, Ferro, false, "grade");
                    if (r != null) _animar.Add(() => Por(r, g.Agora));
                    break;
                }

                case PlataformaPisca p2:
                {
                    var r = Bloco(p2.Caixa, Cores.Vidro, true, "pisca");
                    if (r == null) break;
                    var img = r.GetComponent<Image>();
                    _animar.Add(() =>
                    {
                        var on = p2.Acesa;
                        if (r.gameObject.activeSelf != on) r.gameObject.SetActive(on);
                    });
                    break;
                }

                case Pendulo pe:
                {
                    var haste = Widgets.Painel("corda", _palco, Ferro);
                    var peso = Bloco(new Ret(0f, 0f, pe.Raio * 2f, pe.Raio * 2f),
                                     Cores.Brasa, false, "peso");
                    _animar.Add(() =>
                    {
                        var b = pe.Peso;
                        if (peso != null)
                            Por(peso, new Ret(b.x - pe.Raio, b.y - pe.Raio,
                                              pe.Raio * 2f, pe.Raio * 2f));

                        var meioX = (pe.px + b.x) * 0.5f;
                        var meioY = (pe.py + b.y) * 0.5f;
                        var comp = Vector2.Distance(new Vector2(pe.px, pe.py), b);
                        Por(haste, new Ret(meioX - 2f, meioY - comp * 0.5f, 4f, comp));
                        var ang = Mathf.Atan2(b.x - pe.px, b.y - pe.py) * Mathf.Rad2Deg;
                        haste.localRotation = Quaternion.Euler(0f, 0f, ang);
                    });
                    break;
                }

                case Torreta t2:
                {
                    Bloco(new Ret(t2.x - 14f, t2.y - 12f, 28f, 24f), Ferro, false, "canhao");

                    // Uma piscina de balas: criar e destruir por tiro sujaria o canvas
                    // várias vezes por segundo, que é justamente o que travava a versão
                    // anterior deste arquivo.
                    var balas = new List<RectTransform>();
                    for (var i = 0; i < 8; i++)
                    {
                        var b = Bloco(new Ret(0f, 0f, t2.Raio * 2f, t2.Raio * 2f),
                                      Cores.Luz, false, $"bala{i}");
                        if (b == null) continue;
                        b.gameObject.SetActive(false);
                        balas.Add(b);
                    }

                    _animar.Add(() =>
                    {
                        for (var i = 0; i < balas.Count; i++)
                        {
                            var viva = i < t2.Tiros.Count;
                            if (balas[i].gameObject.activeSelf != viva)
                                balas[i].gameObject.SetActive(viva);
                            if (!viva) continue;
                            var s2 = t2.Tiros[i];
                            Por(balas[i], new Ret(s2.x - t2.Raio, s2.y - t2.Raio,
                                                  t2.Raio * 2f, t2.Raio * 2f));
                        }
                    });
                    break;
                }

                case PortaFalsa pf:
                {
                    var porta = Bloco(new Ret(pf.x, pf.y, pf.l, pf.a), Cores.Folha, false, "falsa");
                    if (porta != null && !string.IsNullOrEmpty(pf.Rotulo))
                    {
                        var rot = Widgets.Texto("rotulo", _palco, 12, TextAnchor.MiddleCenter,
                                                new Color(1f, 1f, 1f, 0.5f));
                        Por(rot.rectTransform, new Ret(pf.x - 50f, pf.y - 24f, pf.l + 100f, 20f));
                        rot.text = pf.Rotulo;
                    }

                    var dentes = Espinhos(new Ret(pf.x - 6f, pf.y, pf.l + 12f, 30f), true);
                    dentes.gameObject.SetActive(false);
                    _animar.Add(() =>
                    {
                        var fora = pf.fora > 0.05f;
                        if (dentes.gameObject.activeSelf != fora) dentes.gameObject.SetActive(fora);
                        if (fora) Por(dentes, new Ret(pf.x - 6f, pf.y, pf.l + 12f, 30f + pf.fora * 24f));
                    });
                    break;
                }

                case Recado n:
                {
                    var t3 = Widgets.Texto("recado", _palco,
                                           Mathf.Max(11, Mathf.RoundToInt(n.Tamanho * 1.3f * _escala)),
                                           TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.26f));
                    Por(t3.rectTransform, new Ret(n.x - 160f, n.y - 16f, 320f, 32f));
                    t3.text = n.Texto;
                    break;
                }
            }
        }

        void Mostrar(RectTransform r, Ret? c)
        {
            var tem = c.HasValue;
            if (r.gameObject.activeSelf != tem) r.gameObject.SetActive(tem);
            if (tem) Por(r, c.Value);
        }

        // ---------------------------------------------------------------- a saída

        void DesenharSaida()
        {
            _portaBaixa = MetadeDaPorta(false);
            _portaAlta = MetadeDaPorta(true);
            MudouASaida();
        }

        Image MetadeDaPorta(bool alta)
        {
            var o = new GameObject(alta ? "portaAlta" : "portaBaixa",
                                   typeof(RectTransform), typeof(Image));
            o.transform.SetParent(_palco, false);
            var img = o.GetComponent<Image>();
            img.sprite = alta ? _spPortaAlta : _spPortaBaixa;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>A saída mudou de lugar (ou a sala recomeçou).</summary>
        void MudouASaida()
        {
            if (_portaAlta == null || _saida == null) return;
            var c = _saida.Caixa;
            Por((RectTransform)_portaAlta.transform, new Ret(c.x, c.y, c.l, c.a * 0.5f));
            Por((RectTransform)_portaBaixa.transform,
                new Ret(c.x, c.y + c.a * 0.5f, c.l, c.a * 0.5f));
        }

        void DesenharBoneco()
        {
            var o = new GameObject("Mensageiro", typeof(RectTransform), typeof(Image));
            o.transform.SetParent(_palco, false);
            _bonecoNaTela = o.GetComponent<Image>();
            _bonecoNaTela.sprite = _spBonecoA;
            _bonecoNaTela.raycastTarget = false;
        }

        // -------------------------------------------------------------- repintar

        void Repintar()
        {
            if (_palco == null) return;

            foreach (var passo in _animar) passo();

            if (_bonecoNaTela != null)
            {
                Por((RectTransform)_bonecoNaTela.transform,
                    new Ret(_boneco.x, _boneco.y, Boneco.L, Boneco.A));
                _bonecoNaTela.transform.localScale = new Vector3(_olhando >= 0 ? 1f : -1f, 1f, 1f);

                var quadro = _andando && _boneco.NoChao &&
                             Mathf.FloorToInt(_passoDaAnimacao) % 2 == 1 ? _spBonecoB : _spBonecoA;
                if (_bonecoNaTela.sprite != quadro) _bonecoNaTela.sprite = quadro;
            }

            if (_travado && _portaAlta != null && _portaAlta.sprite != _spPortaAltaAberta)
            {
                _portaAlta.sprite = _spPortaAltaAberta;
                _portaBaixa.sprite = _spPortaBaixaAberta;
            }
            else if (!_travado && _portaAlta != null && _portaAlta.sprite != _spPortaAlta)
            {
                _portaAlta.sprite = _spPortaAlta;
                _portaBaixa.sprite = _spPortaBaixa;
            }

            if (_placar != null)
                _placar.text = $"pacote a entregar   ·   tombos: {_tombosNoNivel}";
        }
    }
}
