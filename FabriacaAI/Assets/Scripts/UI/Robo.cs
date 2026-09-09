using UnityEngine;

namespace FabricaDeIA.UI
{
    /// <summary>
    /// A máquina do ateliê, em doze peças.
    ///
    /// Ela aparece em duas telas — o balcão do certificado, onde vai se montando
    /// conforme a aula anda, e a tela de fim, onde se levanta inteira. As duas
    /// precisam desenhar o MESMO boneco: um robô com a cabeça em outro lugar na
    /// tela seguinte não é o robô do aluno, é outro desenho.
    ///
    /// Por isso a tabela de peças mora aqui e não dentro de uma das duas. Duas
    /// cópias divergiriam — e divergiriam em silêncio, porque nada quebra quando
    /// um braço anda três pixels.
    ///
    /// Medidas em pixels, com o zero no centro do quadro. A ordem importa: a
    /// peça da bancada 1 é o pé e a última é a antena, de modo que a máquina
    /// cresce de baixo para cima conforme as bancadas fecham.
    /// </summary>
    public static class Robo
    {
        /// <summary>Quantas peças a máquina tem. Onze bancadas mais a antena.</summary>
        public const int Pecas = 12;

        static readonly (float x, float y, float largura, float altura)[] Corpo =
        {
            (-26f, -96f, 30f, 26f),    // 1  pé esquerdo
            (26f, -96f, 30f, 26f),     // 2  pé direito
            (-22f, -62f, 20f, 44f),    // 3  perna esquerda
            (22f, -62f, 20f, 44f),     // 4  perna direita
            (0f, -24f, 92f, 40f),      // 5  quadril
            (0f, 18f, 108f, 46f),      // 6  peito
            (-72f, 10f, 34f, 20f),     // 7  ombro esquerdo
            (72f, 10f, 34f, 20f),      // 8  ombro direito
            (-78f, -26f, 20f, 52f),    // 9  braço esquerdo
            (78f, -26f, 20f, 52f),     // 10 braço direito
            (0f, 66f, 76f, 50f),       // 11 cabeça
            (0f, 100f, 8f, 22f)        // 12 antena
        };

        static readonly Vector2 PostoDoOlho = new(0f, 68f);
        static readonly Vector2 TamanhoDoOlho = new(30f, 12f);

        /// <summary>Onde a peça descansa, no sistema do quadro.</summary>
        public static Vector2 Posto(int indice, float escala = 1f) =>
            new Vector2(Corpo[indice].x, Corpo[indice].y) * escala;

        /// <summary>Desenha uma peça e devolve o retângulo, para quem quiser animá-la.</summary>
        public static RectTransform Peca(RectTransform quadro, int indice, Color cor,
                                         float escala = 1f)
        {
            var (x, y, largura, altura) = Corpo[indice];
            var peca = Widgets.Painel($"p{indice + 1}", quadro, cor);
            Widgets.Fixar(peca, new Vector2(0.5f, 0.5f), new Vector2(x, y) * escala,
                          new Vector2(largura, altura) * escala);
            return peca;
        }

        /// <summary>O olho. Apagado é só um vazio escuro; aceso, é a máquina ligada.</summary>
        public static RectTransform Olho(RectTransform quadro, Color cor, float escala = 1f)
        {
            var olho = Widgets.Painel("Olho", quadro, cor);
            Widgets.Fixar(olho, new Vector2(0.5f, 0.5f), PostoDoOlho * escala,
                          TamanhoDoOlho * escala);
            return olho;
        }

        /// <summary>
        /// O quanto a peça pende enquanto a máquina está incompleta.
        ///
        /// Máquina desmontada não fica reta, e é o desalinho que faz a tela dizer
        /// "falta coisa" sem escrever a frase. Vem do índice para que a mesma peça
        /// penda sempre para o mesmo lado — pendor sorteado a cada quadro viraria
        /// tremedeira.
        /// </summary>
        public static float Pendor(int indice) => (indice % 3 - 1) * 3.5f;
    }
}
