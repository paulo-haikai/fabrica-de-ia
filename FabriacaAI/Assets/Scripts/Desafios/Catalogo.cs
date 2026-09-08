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
            for (var n = 1; n <= Bancadas; n++)
            {
                var etapa = $"e{n}";
                if (TipoDe(etapa) != null) yield return etapa;
            }
        }

        /// <summary>
        /// Quantas bancadas a aula tem. O balcão do certificado NÃO entra: ele é a
        /// etapa seguinte (<c>e12</c>), fica aberto desde o começo e não dá estrela.
        ///
        /// Eram doze. A bancada das fichas — o match-3 de corte de texto — saiu, e
        /// as de baixo subiram um número. Este é o único lugar do código que
        /// precisa saber o total; quem conta bancada ou estrela pergunta aqui.
        /// </summary>
        public const int Bancadas = 11;

        /// <summary>Três estrelas por bancada.</summary>
        public const int EstrelasPossiveis = Bancadas * 3;

        static System.Type TipoDe(string etapa) => etapa switch
        {
            // O balcão do certificado. Não é minigame e não dá estrela;
            // entra aqui porque é assim que o salão abre qualquer estação.
            "e12" => typeof(DesafioCertificado),
            "e1" => typeof(DesafioTermo),
            "e2" => typeof(DesafioDominos),
            "e3" => typeof(DesafioArquivo),
            "e4" => typeof(DesafioMapa),
            "e5" => typeof(DesafioMalha),
            "e6" => typeof(DesafioErro),
            "e7" => typeof(DesafioTreino),
            "e8" => typeof(DesafioCozinha),
            "e9" => typeof(DesafioHolofotes),
            "e10" => typeof(DesafioFala),
            "e11" => typeof(DesafioAlinhar),
            _ => null
        };
    }
}
