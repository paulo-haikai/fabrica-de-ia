using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Quais bancadas já têm desafio portado.
    ///
    /// O porte dos doze acontece em série, e este é o único lugar que precisa
    /// saber onde a fila está. Uma etapa ausente daqui não quebra nada: o mestre
    /// diz que a bancada está em obras e o aluno segue para outra. Isso permite
    /// jogar a aula inteira desde o primeiro dia, com buracos declarados em vez
    /// de erro em tela preta.
    /// </summary>
    public static class Catalogo
    {
        /// <summary>Devolve o desafio da etapa, ou null se ainda não existir.</summary>
        public static Desafio Criar(string etapa, Transform pai)
        {
            var tipo = TipoDe(etapa);
            if (tipo == null) return null;

            var objeto = new GameObject($"Desafio {etapa}");
            objeto.transform.SetParent(pai, false);
            return (Desafio)objeto.AddComponent(tipo);
        }

        /// <summary>
        /// As etapas que já têm minigame. As ferramentas de conferência usam esta
        /// lista para não precisarem saber onde o porte parou.
        /// </summary>
        public static System.Collections.Generic.IEnumerable<string> Registradas()
        {
            for (var n = 1; n <= 12; n++)
            {
                var etapa = $"e{n}";
                if (TipoDe(etapa) != null) yield return etapa;
            }
        }

        static System.Type TipoDe(string etapa) => etapa switch
        {
            // O balcão do certificado. Não é minigame e não dá estrela;
            // entra aqui porque é assim que o salão abre qualquer estação.
            "e13" => typeof(DesafioCertificado),
            "e1" => typeof(DesafioTermo),
            "e2" => typeof(DesafioDominos),
            "e3" => typeof(DesafioArquivo),
            "e4" => typeof(DesafioFichas),
            "e5" => typeof(DesafioMapa),
            "e6" => typeof(DesafioMalha),
            "e7" => typeof(DesafioErro),
            "e8" => typeof(DesafioTreino),
            "e9" => typeof(DesafioCozinha),
            "e10" => typeof(DesafioHolofotes),
            "e11" => typeof(DesafioFala),
            "e12" => typeof(DesafioAlinhar),
            _ => null
        };
    }
}
