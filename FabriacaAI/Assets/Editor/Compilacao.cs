using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FabricaDeIA.Editor
{
    /// <summary>
    /// Compilação por linha de comando.
    ///
    /// O jogo tem que poder ser verificado sem ninguém abrir o Editor. Todo o
    /// mundo é montado por código e a cena é gerada por script, justamente para
    /// que a sequência "gerar arte → compilar → publicar" caiba num comando só
    /// e rode igual na máquina de qualquer pessoa.
    ///
    ///     Unity.exe -quit -batchmode -nographics \
    ///       -projectPath FabriacaAI -executeMethod FabricaDeIA.Editor.Compilacao.Web
    /// </summary>
    public static class Compilacao
    {
        const string Cena = "Assets/Scenes/Atelie.unity";
        const string Saida = "Build/Web";

        /// <summary>Só força a compilação dos scripts e relata. Roda em segundos.</summary>
        public static void Conferir()
        {
            var falhou = EditorUtility.scriptCompilationFailed;
            Debug.Log(falhou ? "COMPILACAO: FALHOU" : "COMPILACAO: OK");
            EditorApplication.Exit(falhou ? 1 : 0);
        }

        [MenuItem("Fábrica de IA/Publicar para navegador")]
        public static void Web()
        {
            if (!File.Exists(Cena))
            {
                Debug.Log("cena ausente; gerando antes de compilar");
                ConstrutorDeCena.Gerar();
            }

            AjustarWebGL();
            AjustarAbertura();

            var destino = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", Saida));
            Directory.CreateDirectory(destino);

            var opcoes = new BuildPlayerOptions
            {
                scenes = new[] { Cena },
                locationPathName = destino,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            var relatorio = BuildPipeline.BuildPlayer(opcoes);
            var resumo = relatorio.summary;

            if (resumo.result == BuildResult.Succeeded)
            {
                var mb = resumo.totalSize / 1024f / 1024f;
                Debug.Log($"BUILD: OK — {mb:F1} MB em {resumo.totalTime.TotalSeconds:F0}s → {destino}");
                if (Application.isBatchMode) EditorApplication.Exit(0);
                return;
            }

            var erros = relatorio.steps
                .SelectMany(p => p.messages)
                .Where(m => m.type == LogType.Error || m.type == LogType.Exception)
                .Select(m => m.content)
                .Take(10);
            Debug.LogError($"BUILD: FALHOU ({resumo.result})\n{string.Join("\n", erros)}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }

        /// <summary>
        /// A abertura: logo da Unity, depois o da Ludotopia.
        ///
        /// A ordem não sai do acaso e nem de `showUnityLogo` sozinho. Com o modo
        /// de desenho padrão os logos aparecem empilhados na mesma tela; o que
        /// põe um DEPOIS do outro é `AllSequential`, e aí a ordem é a ordem do
        /// array. `CreateWithUnityLogo` existe para o logo da Unity ocupar uma
        /// posição explícita nessa fila em vez de ser encaixado onde o Unity
        /// achar melhor.
        ///
        /// Dois segundos por logo é o mínimo que o Unity aceita
        /// (`SplashScreenLogo.minimumLogoTime`) — pedir menos não acelera nada,
        /// só é silenciosamente ignorado. São quatro segundos entre o clique do
        /// aluno e o jogo, uma vez por aula.
        /// </summary>
        static void AjustarAbertura()
        {
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Ludotopia.png");
            if (logo == null)
            {
                // Falhar alto. Um logo que some sem avisar vira um build publicado
                // sem a marca de quem fez, e ninguém percebe até alguém reclamar.
                Debug.LogError("ABERTURA: Assets/Art/Ludotopia.png não importou como Sprite");
                return;
            }

            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = true;
            PlayerSettings.SplashScreen.drawMode =
                PlayerSettings.SplashScreen.DrawMode.AllSequential;
            PlayerSettings.SplashScreen.animationMode =
                PlayerSettings.SplashScreen.AnimationMode.Static;
            PlayerSettings.SplashScreen.logos = new[]
            {
                PlayerSettings.SplashScreenLogo.CreateWithUnityLogo(),
                PlayerSettings.SplashScreenLogo.Create(2f, logo)
            };

            Debug.Log("ABERTURA: Unity → Ludotopia");
        }

        /// <summary>
        /// As opções de WebGL que decidem se o jogo abre numa aula ou não.
        ///
        /// A escola tem link ruim e máquina velha; um build de 60 MB não abre
        /// antes do sinal. Daí compressão Brotli, sem exceções e sem stack
        /// trace, e o data caching ligado — na segunda aula o download some.
        /// </summary>
        static void AjustarWebGL()
        {
            PlayerSettings.companyName = "Fábrica de IA";
            PlayerSettings.productName = "Fábrica de IA";
            PlayerSettings.colorSpace = ColorSpace.Linear;

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.template = "APPLICATION:Default";

            // O QUE FAZ ISTO FUNCIONAR NO GITHUB PAGES.
            //
            // Um build Brotli espera que o servidor mande `Content-Encoding: br`
            // junto com os arquivos. O GitHub Pages não permite configurar
            // cabeçalho nenhum — então o navegador recebe bytes comprimidos sem
            // ser avisado, não descomprime, e a página fica presa no loader para
            // sempre. É o erro mais comum de quem publica Unity WebGL em
            // hospedagem estática, e não dá nenhuma mensagem útil.
            //
            // Com o fallback ligado, o Unity embute um descompressor em
            // JavaScript e o próprio jogo descomprime o que baixou. Custa um
            // pouco de tempo de inicialização e funciona em qualquer servidor de
            // arquivo estático — Pages, Netlify, Drive, pasta compartilhada da
            // escola.
            PlayerSettings.WebGL.decompressionFallback = true;

            PlayerSettings.SetIl2CppCompilerConfiguration(
                NamedBuildTarget.WebGL, Il2CppCompilerConfiguration.Master);
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.WebGL, ManagedStrippingLevel.High);

            PlayerSettings.runInBackground = false;
            PlayerSettings.defaultWebScreenWidth = 960;
            PlayerSettings.defaultWebScreenHeight = 600;
        }
    }
}
