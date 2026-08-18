using System.IO;
using UnityEditor;
using UnityEngine;

namespace FabricaDeIA.Editor
{
    /// <summary>
    /// Fotografa as bancadas, para conferir layout sem andar pelo salão a cada
    /// mudança.
    ///
    /// São doze minigames e a interface de todos é construída por código: um erro
    /// de ancoragem não aparece no compilador, aparece na tela. Poder ver as doze
    /// telas em sequência, sem tocar no teclado, é o que torna viável revisar
    /// layout de doze bancadas numa sessão.
    ///
    /// Precisa do jogo em Play. As fotos vão para `Retratos/` na raiz do
    /// repositório — fora de `Assets`, para o Unity não importar PNG de
    /// depuração como sprite do jogo.
    /// </summary>
    public static class Retratos
    {
        static string Pasta =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Retratos"));

        [MenuItem("Fábrica de IA/Retrato/Todas as bancadas")]
        public static void Todas()
        {
            if (!Pronto()) return;
            Nucleo.Retrato.TirarTodas(Pasta);
        }

        [MenuItem("Fábrica de IA/Retrato/Bancada 8 — treinando")]
        public static void Treinando()
        {
            if (!Pronto()) return;
            // "médio" é o passo que converge; a curva desce e assenta.
            Nucleo.Retrato.TirarJogando("e8", Pasta, new[] { "médio" }, 0f, 2.2f, "e8-treinando");
        }

        [MenuItem("Fábrica de IA/Retrato/Bancada 2 — corrente montada")]
        public static void DominoMontado()
        {
            if (!Pronto()) return;
            // A primeira peça jogável é sempre a que encaixa na ponta inicial.
            Nucleo.Retrato.TirarJogando("e2", Pasta, new string[0], 0f, 0.4f, "e2-inicio");
        }

        [MenuItem("Fábrica de IA/Retrato/Abertura — instantes")]
        public static void AberturaEmInstantes()
        {
            if (!Pronto()) return;
            // Antes da tempestade, no meio dela, no clarão, e depois do golpe.
            Nucleo.Retrato.TirarAbertura(Pasta, new[] { 2f, 7f, 8.7f, 12f });
        }

        [MenuItem("Fábrica de IA/Retrato/Tutorial da bancada 1")]
        public static void Tutorial1()
        {
            if (!Pronto()) return;
            // Passo 1, e depois o passo 3, que é onde a máquina joga de verdade.
            Nucleo.Retrato.TirarJogando("e1", Pasta, new string[0], 0f, 0.5f, "tut-e1-1");
        }

        [MenuItem("Fábrica de IA/Retrato/Tutorial da bancada 1 — demonstrando")]
        public static void Tutorial1Demo()
        {
            if (!Pronto()) return;
            Nucleo.Retrato.TirarJogando("e1", Pasta, new[] { "entendi", "entendi" },
                                        0.4f, 2.2f, "tut-e1-3");
        }

        [MenuItem("Fábrica de IA/Retrato/Ateliê")]
        public static void Atelie()
        {
            if (!Pronto()) return;
            Nucleo.Retrato.Tirar(null, Pasta);
        }

        [MenuItem("Fábrica de IA/Testar saída das bancadas")]
        public static void TestarSaida()
        {
            if (!Pronto()) return;
            Nucleo.Retrato.TestarFechamento();
        }

        /// <summary>
        /// Fotografa o passo em que a máquina acabou de treinar sozinha: é o
        /// momento da aula em que a curva do erro desce na tela, e o único jeito
        /// de conferir se a moldura acertou o gráfico é olhando.
        /// </summary>
        [MenuItem("Fábrica de IA/Retrato/Tutorial da bancada 8 — a curva descendo")]
        public static void Tutorial8Demo()
        {
            if (!Pronto()) return;
            Nucleo.Retrato.EsquecerTutorial("e8");
            Nucleo.Retrato.TirarJogando("e8", Pasta,
                new[] { "entendi", "entendi" }, 0.5f, 1.2f, "tut-e8-demo");
        }

        /// <summary>
        /// Fotografa a bancada 6 com a frente de luz no MEIO da travessia.
        ///
        /// O instante importa: no começo a malha está apagada e no fim está toda
        /// acesa — as duas fotos parecem corretas. O defeito de layout, se existir,
        /// aparece justamente quando as conexões estão meio crescidas, porque é aí
        /// que se vê se elas saem do ponto certo e chegam no ponto certo.
        ///
        /// Entra pelo tutorial porque é ele que dispara a aposta sozinho: dois
        /// "entendi" e o passo 3 roda a demonstração.
        /// </summary>
        [MenuItem("Fábrica de IA/Retrato/Bancada 6 — a luz atravessando")]
        public static void MalhaAtravessando()
        {
            if (!Pronto()) return;
            Nucleo.Retrato.EsquecerTutorial("e6");
            Nucleo.Retrato.TirarJogando("e6", Pasta,
                new[] { "entendi", "entendi" }, 0.5f, 1.25f, "malha-travessia");
        }

        [MenuItem("Fábrica de IA/Retrato/Bancada 6 — a parede acesa")]
        public static void MalhaParede()
        {
            if (!Pronto()) return;
            Nucleo.Retrato.EsquecerTutorial("e6");
            Nucleo.Retrato.TirarJogando("e6", Pasta,
                new[] { "entendi", "entendi" }, 0.5f, 3.2f, "malha-parede");
        }

        [MenuItem("Fábrica de IA/Retrato/Tutorial da bancada 10 — no escuro")]
        public static void Tutorial10Demo()
        {
            if (!Pronto()) return;
            Nucleo.Retrato.EsquecerTutorial("e10");
            Nucleo.Retrato.TirarJogando("e10", Pasta,
                new[] { "entendi", "entendi" }, 0.6f, 1.2f, "tut-e10-demo");
        }

        /// <summary>
        /// Gera um diploma com resultados inventados e o grava em disco.
        ///
        /// Não exige Play: o PDF é montado a partir de um <c>Progresso</c>, e um
        /// Progresso pode ser fabricado aqui. Sem este atalho, conferir o
        /// documento exigiria jogar as doze bancadas até o fim a cada ajuste de
        /// meio milímetro no layout.
        /// </summary>
        [MenuItem("Fábrica de IA/Gerar diploma de exemplo")]
        public static void DiplomaDeExemplo()
        {
            var p = new Nucleo.Progresso
            {
                nome = "Maria Aparecida de Souza Lima",
                turma = "9º B",
                terminouEm = System.DateTime.Now.ToString("s")
            };

            // Resultados variados de propósito: três estrelas, duas, uma, e uma
            // bancada não concluída. Um diploma de exemplo com tudo cheio esconde
            // justamente as linhas que podem quebrar o layout.
            var estrelas = new[] { 3, 3, 2, 3, 1, 2, 3, 2, 3, 1, 3, 0 };
            var tempos = new[] { 412, 268, 190, 305, 520, 176, 240, 388, 210, 460, 232, 95 };

            for (var i = 0; i < 12; i++)
                p.Concluir($"e{i + 1}", estrelas[i], tempos[i]);

            // `Concluir` grava no PlayerPrefs do Editor, e isto é só um exemplo:
            // devolve o progresso de verdade no lugar.
            Nucleo.Progresso.Carregar();

            var arquivo = System.IO.Path.Combine(
                System.IO.Directory.GetCurrentDirectory(), "diploma-exemplo.pdf");
            System.IO.File.WriteAllBytes(arquivo, Nucleo.Diploma.Montar(p));
            Debug.Log($"DIPLOMA: {arquivo}");
        }

        [MenuItem("Fábrica de IA/Testar digitação da bancada 1")]
        public static void TestarDigitacao()
        {
            if (!Pronto()) return;
            Nucleo.Retrato.TestarDigitacao();
        }

        [MenuItem("Fábrica de IA/Testar tutoriais das bancadas")]
        public static void TestarTutoriais()
        {
            if (!Pronto()) return;
            Nucleo.Retrato.TestarTutoriais();
        }

        static bool Pronto()
        {
            if (Application.isPlaying) return true;
            Debug.LogError("RETRATO: entre em Play antes.");
            return false;
        }
    }
}
