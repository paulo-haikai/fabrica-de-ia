using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FabricaDeIA.Nucleo
{
    /// <summary>
    /// Entrega um arquivo ao aluno, do jeito que a plataforma permite.
    ///
    /// No navegador, que é onde a aula acontece, isso é um Blob e um clique
    /// sintético — está no <c>Baixar.jslib</c>. No Editor não existe pasta de
    /// downloads, então o arquivo vai para a raiz do projeto e o caminho vai para o
    /// log: é assim que a conferência abre o PDF sem precisar de um navegador.
    /// </summary>
    public static class Baixar
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void BaixarBytes(string nome, byte[] dados, int tamanho, string tipo);
#endif

        /// <summary>
        /// Manda os bytes para o aluno. Devolve o caminho no disco quando houve um,
        /// ou string vazia no navegador — onde o destino é a pasta de downloads dele
        /// e o jogo não tem como saber qual é.
        /// </summary>
        public static string Entregar(string nome, byte[] dados, string tipo = "application/pdf")
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            BaixarBytes(nome, dados, dados.Length, tipo);
            return string.Empty;
#else
            var caminho = Path.Combine(Directory.GetCurrentDirectory(), nome);
            File.WriteAllBytes(caminho, dados);
            Debug.Log($"ARQUIVO: {caminho}");
            return caminho;
#endif
        }
    }
}
