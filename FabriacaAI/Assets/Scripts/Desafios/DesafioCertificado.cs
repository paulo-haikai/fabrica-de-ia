using FabricaDeIA.Nucleo;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A bancada 13 — a retirada do certificado, com o Mestre da IA.
    ///
    /// Não é um minigame: é o fecho da aula, e a única estação do ateliê que não
    /// dá estrela nenhuma. O que ela tem para mostrar é a MÁQUINA, montada com o
    /// que o aluno percorreu.
    ///
    /// A regra é uma peça por bancada visitada — visitada, não vencida. Isso é
    /// deliberado e é a mesma escolha que governa a progressão do salão: quem
    /// entrou na bancada 7, apanhou e saiu sem estrela aprendeu o que a 7 tinha
    /// para ensinar, que é justamente o tamanho do problema. Cobrar acerto para
    /// montar a máquina transformaria a aula num teste, e ela não é.
    ///
    /// Com as doze no lugar, o robô ACENDE: o olho pulsa e ele para de pender.
    /// Sem elas, fica ali, incompleto e visivelmente à espera — que é o mesmo
    /// papel da máquina quebrada da abertura, agora medindo o progresso dele em
    /// vez de contar o problema.
    /// </summary>
    public class DesafioCertificado : Desafio
    {
        public override string Etapa => "e13";
        public override string Titulo => "A máquina, e o seu certificado";

        /// <summary>
        /// As doze peças, na ordem das bancadas.
        ///
        /// Posição e tamanho em pixels da área de conteúdo, com o zero no centro.
        /// A ordem importa: a peça da bancada 1 é o pé, a da 12 é a antena — a
        /// máquina cresce de baixo para cima conforme a aula anda, e o aluno que
        /// volta ao ateliê vê o próprio avanço sem ler número nenhum.
        /// </summary>
        static readonly (float x, float y, float largura, float altura)[] Pecas =
        {
            (-26f, -96f, 30f, 26f),    // e1  pé esquerdo
            (26f, -96f, 30f, 26f),     // e2  pé direito
            (-22f, -62f, 20f, 44f),    // e3  perna esquerda
            (22f, -62f, 20f, 44f),     // e4  perna direita
            (0f, -24f, 92f, 40f),      // e5  quadril
            (0f, 18f, 108f, 46f),      // e6  peito
            (-72f, 10f, 34f, 20f),     // e7  ombro esquerdo
            (72f, 10f, 34f, 20f),      // e8  ombro direito
            (-78f, -26f, 20f, 52f),    // e9  braço esquerdo
            (78f, -26f, 20f, 52f),     // e10 braço direito
            (0f, 66f, 76f, 50f),       // e11 cabeça
            (0f, 100f, 8f, 22f)        // e12 antena
        };

        protected override void Montar(RectTransform area)
        {
            var p = Progresso.Atual;
            var montadas = Montadas(p);
            var pronta = p.TodasTentadas;

            Painel.Rodape(pronta
                ? "a máquina está de pé"
                : "cada bancada que você visita devolve uma peça a ela");

            var quadro = Widgets.Painel("Quadro", area, Cores.TintaClara);
            Widgets.Fixar(quadro, new Vector2(0.5f, 0.5f), new Vector2(0f, 24f),
                          new Vector2(420f, 300f));

            DesenharRobo(quadro, p, pronta);

            var conta = Widgets.Texto("Conta", area, 17, TextAnchor.LowerCenter,
                                      pronta ? Cores.Luz : Cores.Neblina);
            Widgets.Faixa(conta.rectTransform, false, 24f, 44f);
            conta.text = pronta
                ? $"doze bancadas, {p.EstrelasTotais} estrelas"
                : $"{montadas} de 12 peças no lugar";
            Widgets.UmaLinha(conta);

            Painel.Instruir(pronta
                ? "ela funciona — o certificado é seu"
                : "ainda faltam peças, mas o certificado sai assim mesmo",
                pronta ? Cores.Folha : Cores.Papel);

            // O certificado sai SEMPRE, completa ou não a aula. Quem parou na
            // sétima bancada fez sete bancadas de trabalho, e a folha existe para
            // registrar o que a pessoa fez — não para premiar quem terminou.
            Painel.Acao("retirar o certificado", () => Encerrar(0));

            Widgets.Surgir(quadro);
            if (pronta) Widgets.Lampejo(quadro, Cores.Luz, 0.5f, 0.45f);
        }

        static int Montadas(Progresso p)
        {
            var quantas = 0;
            for (var i = 1; i <= 12; i++)
                if (p.Tentada($"e{i}")) quantas++;
            return quantas;
        }

        void DesenharRobo(RectTransform quadro, Progresso p, bool pronta)
        {
            for (var i = 0; i < Pecas.Length; i++)
            {
                var (x, y, largura, altura) = Pecas[i];
                var tem = p.Tentada($"e{i + 1}");

                var peca = Widgets.Painel($"p{i + 1}", quadro,
                                          tem ? Cores.Madeira : new Color(0.16f, 0.18f, 0.24f));
                Widgets.Fixar(peca, new Vector2(0.5f, 0.5f), new Vector2(x, y),
                              new Vector2(largura, altura));

                if (!tem)
                {
                    // A peça que falta não some: fica como um vazio escuro, do
                    // tamanho exato do que deveria estar ali. Sumir de vez faria a
                    // máquina parecer menor em vez de incompleta, e é a falta que
                    // esta tela precisa mostrar.
                    continue;
                }

                // Máquina desmontada não fica reta. Cada peça posta antes do fim
                // pende um pouco, e o conjunto só se alinha quando a última chega.
                if (!pronta) peca.localRotation = Quaternion.Euler(0f, 0f, (i % 3 - 1) * 3.5f);
            }

            var olho = Widgets.Painel("Olho", quadro, pronta ? Cores.Luz : Cores.TintaOpaca);
            Widgets.Fixar(olho, new Vector2(0.5f, 0.5f), new Vector2(0f, 68f),
                          new Vector2(30f, 12f));

            if (pronta)
            {
                // O olho pulsando é o único movimento da tela, e é o que separa
                // "montada" de "ligada" sem precisar de uma frase dizendo isso.
                Widgets.Pulsar(olho, 1.35f, 0.9f);
                return;
            }

            var faisca = Widgets.Texto("Faísca", quadro, 14, TextAnchor.UpperCenter, Cores.Brasa);
            Widgets.Faixa(faisca.rectTransform, true, 20f, 10f);
            faisca.text = "sem energia";
        }

        /// <summary>
        /// Sair daqui é a mesma coisa que retirar: não há nada a perder nesta
        /// bancada, e um aluno que aperta Esc não pode ficar sem o certificado
        /// por isso.
        /// </summary>
        protected override void Desistir() => Encerrar(0);
    }
}
