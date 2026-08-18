using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FabricaDeIA.Editor
{
    /// <summary>
    /// Gera a cena do jogo.
    ///
    /// A cena tem exatamente dois objetos: a câmera e o <c>Jogo</c>. Todo o
    /// ateliê nasce em tempo de execução. Isso deixa o arquivo de cena
    /// pequeno e estável — nada de conflito de merge em YAML de mil linhas
    /// quando duas pessoas mexem no salão — e permite regenerar a cena do zero
    /// por linha de comando sempre que a estrutura mudar.
    /// </summary>
    public static class ConstrutorDeCena
    {
        const string Pasta = "Assets/Scenes";
        const string Caminho = Pasta + "/Atelie.unity";

        [MenuItem("Fábrica de IA/Gerar cena do ateliê")]
        public static void Gerar()
        {
            Directory.CreateDirectory(Pasta);

            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = new GameObject("Câmera", typeof(Camera));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 0f, -10f);
            var lente = camera.GetComponent<Camera>();
            lente.orthographic = true;
            lente.clearFlags = CameraClearFlags.SolidColor;
            lente.backgroundColor = new Color(0.106f, 0.122f, 0.165f);

            new GameObject("Jogo", typeof(Nucleo.Jogo));

            EditorSceneManager.SaveScene(cena, Caminho);
            RegistrarNaCompilacao();
            AssetDatabase.SaveAssets();
            Debug.Log($"CENA: OK — {Caminho}");
        }

        /// <summary>A cena precisa estar na lista, ou o build sai vazio.</summary>
        static void RegistrarNaCompilacao()
        {
            var lista = new[] { new EditorBuildSettingsScene(Caminho, true) };
            EditorBuildSettings.scenes = lista;
        }
    }
}
