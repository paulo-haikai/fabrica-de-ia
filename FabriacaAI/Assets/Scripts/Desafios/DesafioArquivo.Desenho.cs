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
    /// primeira. Uma textura de 120 por 120 pixels custa nada, e a bancada já
    /// usava essa técnica na tela que mostrava a tabela inteira.
    ///
    /// O efeito colateral é bonito: quando o aluno toma uma região grande, ele vê
    /// a mancha crescer como tinta, e não como uma lista de casinhas mudando de
    /// cor uma a uma.
    /// </summary>
    public partial class DesafioArquivo
    {
        const float LadoNaTela = 430f;

        RawImage _tela;
        Texture2D _textura;
        Text _placar;

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

            if (_placar == null)
            {
                _placar = Widgets.Texto("Placar", Area, 16, TextAnchor.LowerCenter, Cores.Papel);
                Widgets.Faixa(_placar.rectTransform, false, 24f, 4f);
            }

            Repintar();
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

            DesenharPlacar();
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
