using UnityEditor;
using UnityEngine;

namespace FabricaDeIA.Editor
{
    /// <summary>
    /// Ajusta a importação de todo PNG que cair em <c>Resources/Arte</c>.
    ///
    /// Sem isto, o Unity importa pixel art com filtro bilinear e compressão com
    /// perda — a borda dura vira borrão e a paleta de 32 cores ganha sujeira nos
    /// gradientes. Ajustar no Sprite Editor à mão funcionaria uma vez e seria
    /// esquecido na próxima folha gerada; num postprocessador, vale para sempre
    /// e para quem chegar depois.
    ///
    /// O fatiamento NÃO acontece aqui: as folhas são grades uniformes e o jogo
    /// recorta em tempo de execução com <c>Sprite.Create</c>, lendo as medidas
    /// do <c>arte.json</c>. Assim a arte pode mudar de tamanho sem ninguém
    /// reabrir o Editor.
    /// </summary>
    public class ImportadorDeArte : AssetPostprocessor
    {
        const string Pasta = "/Resources/Arte/";

        void OnPreprocessTexture()
        {
            if (!assetPath.Contains(Pasta)) return;

            var importador = (TextureImporter)assetImporter;
            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.filterMode = FilterMode.Point;
            importador.mipmapEnabled = false;
            importador.alphaIsTransparency = true;
            importador.spritePixelsPerUnit = 16f;
            importador.wrapMode = TextureWrapMode.Clamp;

            // O jogo recorta a textura em tempo de execução, então ela precisa
            // continuar legível pela CPU e sem compressão de bloco.
            importador.isReadable = true;
            importador.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
