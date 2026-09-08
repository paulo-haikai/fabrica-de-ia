using System.Collections.Generic;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// O desenho da bancada 7 — a pista do estilingue.
    ///
    /// Tudo aqui é retângulo e disco de uGUI: não há folha de sprites para esta
    /// bancada, e não precisa haver. O que faz a pista LER como Angry Birds não
    /// é textura, é a gramática — céu em cima, chão em baixo, estilingue à
    /// esquerda, porco em cima de caixas à direita, parábola pontilhada no meio.
    ///
    /// A CONVERSÃO DE ESCALA ACONTECE SÓ EM <see cref="Tela"/>. O jogo inteiro
    /// pensa em passos de pista (o porco está a 78 passos, o pássaro tem raio
    /// 1,2), e uma função sozinha traduz isso para pixels. É o que permite a
    /// rodada 3 ter duas pistas de alturas diferentes sem nenhuma conta espalhada
    /// pelo código.
    ///
    /// Os RASTROS são a peça de desenho mais importante do arquivo. Cada tiro
    /// deixa a sua fileira de pontinhos, e eles não são apagados entre um tiro e
    /// o outro: ao fim de um treino, as parábolas empilhadas na tela SÃO a curva
    /// de aprendizado. Nenhum gráfico faria esse trabalho melhor, e o gráfico
    /// precisaria de eixo, legenda e explicação.
    /// </summary>
    public partial class DesafioTreino
    {
        static readonly Color Ceu = new(0.196f, 0.267f, 0.373f);
        static readonly Color Morro = new(0.180f, 0.353f, 0.220f);
        static readonly Color Grama = new(0.318f, 0.588f, 0.322f);
        static readonly Color Terra = new(0.325f, 0.235f, 0.161f);
        static readonly Color MadeiraClara = new(0.545f, 0.376f, 0.243f);

        const float AlturaDoConsole = 82f;
        const float ChaoEmPixels = 16f;

        RectTransform _reguaTrilho, _reguaMiolo, _reguaAnterior;
        Text _leitura;
        RectTransform _puxador;

        // ------------------------------------------------------------ montagem

        void MontarPistas()
        {
            PrepararMaquinas();

            Canvas.ForceUpdateCanvases();
            var espaco = Area.rect.size;
            if (espaco.x < 1f) { Canvas.ForceUpdateCanvases(); espaco = Area.rect.size; }

            var regiao = espaco.y - AlturaDoConsole;
            var quantas = _maquinas.Count;
            var altura = quantas > 1 ? (regiao - 12f) / quantas : Mathf.Min(regiao, 320f);

            for (var i = 0; i < quantas; i++)
            {
                var m = _maquinas[i];
                m.Escala = espaco.x / Estilingue.Pista;
                m.ChaoY = ChaoEmPixels;

                m.Pista = Widgets.Painel($"Pista{i}", Area, Ceu);
                Widgets.Fixar(m.Pista, new Vector2(0.5f, 1f),
                              new Vector2(0f, -(i * (altura + 12f) + altura / 2f)),
                              new Vector2(espaco.x, altura));
                m.Pista.GetComponent<Image>().raycastTarget = false;
                // Sem recorte, um tiro que sobe demais desenha por cima da
                // outra pista e do console. Com recorte, ele SOME da pista —
                // que é exatamente o que acabou de acontecer com ele.
                m.Pista.gameObject.AddComponent<RectMask2D>();

                DesenharPista(m, quantas > 1 ? (i == 0 ? "porco perto" : "porco longe") : null);
            }

            MontarPuxador();
        }

        /// <summary>Onde um ponto do mundo cai dentro da pista, em pixels.</summary>
        static Vector2 Tela(Maquina m, Vector2 mundo) =>
            new(mundo.x * m.Escala, m.ChaoY + mundo.y * m.Escala);

        void DesenharPista(Maquina m, string rotulo)
        {
            var largura = m.Pista.sizeDelta.x;

            Nuvem(m, new Vector2(largura * 0.28f, m.Pista.sizeDelta.y - 26f), 54f);
            Nuvem(m, new Vector2(largura * 0.62f, m.Pista.sizeDelta.y - 40f), 38f);

            Colina(m, largura * 0.30f, 210f, 54f);
            Colina(m, largura * 0.72f, 260f, 38f);

            var terra = Widgets.Painel("Chão", m.Pista, Terra);
            Widgets.Faixa(terra, false, ChaoEmPixels);
            var grama = Widgets.Painel("Grama", terra, Grama);
            Widgets.Faixa(grama, true, 5f);

            m.Caixas = new RectTransform[m.Blocos.Count];
            for (var i = 0; i < m.Blocos.Count; i++) m.Caixas[i] = Caixa(m, m.Blocos[i], i);

            DesenharPorco(m);
            DesenharEstilingue(m);

            m.Rastros = Widgets.Painel("Rastros", m.Pista, Color.clear);
            Widgets.Esticar(m.Rastros);
            m.Rastros.GetComponent<Image>().raycastTarget = false;

            DesenharPassaro(m);

            if (rotulo == null) return;
            var nome = Widgets.Texto("Nome", m.Pista, 12, TextAnchor.UpperRight, Cores.Neblina);
            Widgets.Esticar(nome.rectTransform, 8f);
            nome.text = rotulo;
        }

        void Nuvem(Maquina m, Vector2 centro, float largura)
        {
            var cor = new Color(Cores.Neblina.r, Cores.Neblina.g, Cores.Neblina.b, 0.22f);
            Bola("Nuvem", m.Pista, cor, centro, largura * 0.6f);
            Bola("Nuvem", m.Pista, cor, centro + new Vector2(largura * 0.3f, -3f), largura * 0.45f);
            Bola("Nuvem", m.Pista, cor, centro - new Vector2(largura * 0.3f, 3f), largura * 0.42f);
        }

        void Colina(Maquina m, float x, float largura, float altura)
        {
            var bola = Bola("Colina", m.Pista, Morro,
                            new Vector2(x, ChaoEmPixels - altura * 0.35f), largura);
            bola.sizeDelta = new Vector2(largura, altura * 2f);
        }

        RectTransform Caixa(Maquina m, Rect bloco, int indice)
        {
            var centro = Tela(m, new Vector2(bloco.center.x, bloco.center.y));
            var caixa = Widgets.Painel($"Caixa{indice}", m.Pista, Cores.Madeira);
            caixa.anchorMin = caixa.anchorMax = Vector2.zero;
            caixa.pivot = new Vector2(0.5f, 0.5f);
            caixa.anchoredPosition = centro;
            caixa.sizeDelta = new Vector2(bloco.width * m.Escala, bloco.height * m.Escala);
            caixa.GetComponent<Image>().raycastTarget = false;

            // As duas ripas claras que fazem um retângulo marrom virar madeira.
            var ripa = Widgets.Painel("Ripa", caixa, MadeiraClara);
            Widgets.Esticar(ripa, 3f);
            ripa.GetComponent<Image>().raycastTarget = false;
            return caixa;
        }

        void DesenharPorco(Maquina m)
        {
            var centro = Tela(m, new Vector2(m.PorcoX, Estilingue.RaioDoPorco));
            var lado = Estilingue.RaioDoPorco * 2f * m.Escala;

            m.Porco = Bola("Porco", m.Pista, Cores.Folha, centro, lado);

            var orelhaE = Bola("Orelha", m.Porco, new Color(0.20f, 0.44f, 0.22f),
                               new Vector2(-lado * 0.28f, lado * 0.42f), lado * 0.26f);
            var orelhaD = Bola("Orelha", m.Porco, new Color(0.20f, 0.44f, 0.22f),
                               new Vector2(lado * 0.28f, lado * 0.42f), lado * 0.26f);
            orelhaE.anchorMin = orelhaE.anchorMax = orelhaD.anchorMin = orelhaD.anchorMax =
                new Vector2(0.5f, 0.5f);

            Olho(m.Porco, new Vector2(-lado * 0.19f, lado * 0.14f), lado * 0.26f);
            Olho(m.Porco, new Vector2(lado * 0.19f, lado * 0.14f), lado * 0.26f);

            var focinho = Bola("Focinho", m.Porco, new Color(0.23f, 0.48f, 0.24f),
                               new Vector2(0f, -lado * 0.14f), lado * 0.36f);
            focinho.anchorMin = focinho.anchorMax = new Vector2(0.5f, 0.5f);
        }

        void Olho(RectTransform pai, Vector2 posicao, float lado)
        {
            var branco = Bola("Olho", pai, Cores.Papel, posicao, lado);
            branco.anchorMin = branco.anchorMax = new Vector2(0.5f, 0.5f);
            var pupila = Bola("Pupila", branco, Cores.TintaOpaca, Vector2.zero, lado * 0.45f);
            pupila.anchorMin = pupila.anchorMax = new Vector2(0.5f, 0.5f);
        }

        void DesenharEstilingue(Maquina m)
        {
            var pe = Tela(m, new Vector2(Estilingue.BocaX, 0f));
            var boca = Tela(m, new Vector2(Estilingue.BocaX, Estilingue.BocaY));
            var alto = boca.y - pe.y;

            var tronco = Widgets.Painel("Tronco", m.Pista, Cores.Madeira);
            tronco.anchorMin = tronco.anchorMax = Vector2.zero;
            tronco.pivot = new Vector2(0.5f, 0f);
            tronco.anchoredPosition = pe;
            tronco.sizeDelta = new Vector2(6f, alto * 0.7f);
            tronco.GetComponent<Image>().raycastTarget = false;

            Fio(m.Pista, pe + new Vector2(0f, alto * 0.7f), boca + new Vector2(-9f, 2f),
                Cores.Madeira, 6f);
            Fio(m.Pista, pe + new Vector2(0f, alto * 0.7f), boca + new Vector2(9f, 2f),
                Cores.Madeira, 6f);

            // Os dois fios do elástico nascem aqui e nunca mais são recriados:
            // o arraste os reposiciona quadro a quadro, e recriar Image a cada
            // quadro suja a malha do canvas inteiro.
            m.ElasticoA = Fio(m.Pista, boca, boca, Cores.Luz, 3f);
            m.ElasticoB = Fio(m.Pista, boca, boca, Cores.Luz, 3f);
            m.ElasticoA.gameObject.SetActive(false);
            m.ElasticoB.gameObject.SetActive(false);
        }

        void DesenharPassaro(Maquina m)
        {
            var boca = Tela(m, new Vector2(Estilingue.BocaX, Estilingue.BocaY));
            var lado = Mathf.Max(13f, Estilingue.RaioDoPassaro * 2f * m.Escala);

            m.Passaro = Bola("Pássaro", m.Pista, Cores.Brasa, boca, lado);

            var olho = Bola("Olho", m.Passaro, Cores.Papel,
                            new Vector2(lado * 0.16f, lado * 0.16f), lado * 0.34f);
            olho.anchorMin = olho.anchorMax = new Vector2(0.5f, 0.5f);
            var pupila = Bola("Pupila", olho, Cores.TintaOpaca, Vector2.zero, lado * 0.16f);
            pupila.anchorMin = pupila.anchorMax = new Vector2(0.5f, 0.5f);

            var bico = Widgets.Painel("Bico", m.Passaro, Cores.Luz);
            Widgets.Fixar(bico, new Vector2(0.5f, 0.5f),
                          new Vector2(lado * 0.42f, -lado * 0.04f),
                          new Vector2(lado * 0.36f, lado * 0.24f));
            bico.GetComponent<Image>().raycastTarget = false;
        }

        // ---------------------------------------------------------- ferramentas

        /// <summary>
        /// A folha de um disco, feita em tempo de execução.
        ///
        /// A bancada precisa de círculos (pássaro, porco, nuvem, rastro) e o
        /// projeto não tem folha de sprites para ela. Uma textura de 64 pixels
        /// com a borda suavizada resolve as quatro coisas, e nasce e morre
        /// dentro do código — sem passar pelo gerador de arte, sem PNG novo, sem
        /// .meta para o Unity importar.
        /// </summary>
        static Sprite _disco;

        static Sprite DiscoBranco()
        {
            if (_disco != null) return _disco;

            const int lado = 64;
            var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var meio = new Vector2(lado / 2f, lado / 2f);
            var raio = lado / 2f - 0.5f;

            for (var y = 0; y < lado; y++)
                for (var x = 0; x < lado; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), meio);
                    textura.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(raio - d)));
                }

            textura.Apply();
            _disco = Sprite.Create(textura, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 0.5f));
            _disco.hideFlags = HideFlags.HideAndDontSave;
            return _disco;
        }

        static RectTransform Bola(string nome, Transform pai, Color cor, Vector2 centro, float lado)
        {
            var bola = Widgets.Painel(nome, pai, cor);
            var pintura = bola.GetComponent<Image>();
            pintura.sprite = DiscoBranco();
            pintura.raycastTarget = false;

            bola.anchorMin = bola.anchorMax = Vector2.zero;
            bola.pivot = new Vector2(0.5f, 0.5f);
            bola.anchoredPosition = centro;
            bola.sizeDelta = new Vector2(lado, lado);
            return bola;
        }

        static RectTransform Fio(Transform pai, Vector2 a, Vector2 b, Color cor, float grossura)
        {
            var fio = Widgets.Painel("f", pai, cor);
            fio.anchorMin = fio.anchorMax = Vector2.zero;
            fio.pivot = new Vector2(0.5f, 0.5f);
            fio.sizeDelta = new Vector2(1f, grossura);
            fio.GetComponent<Image>().raycastTarget = false;
            PorFio(fio, a, b);
            return fio;
        }

        /// <summary>Estica um fio já existente entre dois pontos.</summary>
        static void PorFio(RectTransform fio, Vector2 a, Vector2 b)
        {
            var delta = b - a;
            fio.anchoredPosition = (a + b) / 2f;
            fio.sizeDelta = new Vector2(delta.magnitude + 1f, fio.sizeDelta.y);
            fio.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }
    }
}
