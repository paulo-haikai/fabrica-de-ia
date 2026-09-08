using System.Collections.Generic;
using FabricaDeIA.Desafios;
using UnityEditor;
using UnityEngine;

namespace FabricaDeIA.Editor
{
    /// <summary>
    /// O menu da conferência do guichê da bancada 11.
    ///
    /// A medição em si mora em <c>Desafios/DesafioAlinhar.Conferencia.cs</c>, e
    /// não aqui, porque os tipos que ela mede — o requerente, o toco de decisão —
    /// são privados da bancada. Trazê-los para o Editor obrigaria a torná-los
    /// públicos, e uma conferência não vale abrir a bancada inteira.
    ///
    /// Aqui fica só o que é do Editor: o item de menu e a impressão do relatório.
    /// </summary>
    public static partial class Conferencias
    {
        [MenuItem("Fábrica de IA/Conferir o guichê da bancada 11")]
        public static void ConferirGuiche12()
        {
            var relato = new List<string>();
            var problemas = new List<string>();
            DesafioAlinhar.Conferir(relato, problemas);

            var texto = "GUICHÊ (b12):\n" + string.Join("\n", relato);
            if (problemas.Count == 0) Debug.Log(texto + "\nGUICHÊ: OK");
            else Debug.LogError(texto + "\n\nGUICHÊ: " + problemas.Count +
                                " PROBLEMA(S)\n  " + string.Join("\n  ", problemas));
        }
    }
}
