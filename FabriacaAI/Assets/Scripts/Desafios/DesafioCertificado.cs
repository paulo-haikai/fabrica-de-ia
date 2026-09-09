using FabricaDeIA.Nucleo;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A bancada 12 — a retirada do certificado, com o Mestre da IA.
    ///
    /// Não é um minigame: é o fecho da aula, e a única estação do ateliê que não
    /// dá estrela nenhuma. O que ela tem para mostrar é a MÁQUINA, montada com o
    /// que o aluno percorreu.
    ///
    /// A regra é uma peça por bancada visitada — visitada, não vencida. Isso é
    /// deliberado e é a mesma escolha que governa a progressão do salão: quem
    /// entrou na bancada 6, apanhou e saiu sem estrela aprendeu o que a 7 tinha
    /// para ensinar, que é justamente o tamanho do problema. Cobrar acerto para
    /// montar a máquina transformaria a aula num teste, e ela não é.
    ///
    /// Com as onze fechadas, o robô ACENDE: a antena chega, o olho pulsa e ele
    /// para de pender. Sem elas, fica ali, incompleto e visivelmente à espera —
    /// que é o mesmo papel da máquina quebrada da abertura, agora medindo o
    /// progresso dele em vez de contar o problema.
    /// </summary>
    public class DesafioCertificado : Desafio
    {
        public override string Etapa => "e12";
        public override string Titulo => "A máquina, e o seu certificado";

        /// <summary>
        /// A tabela de peças mora em <see cref="Robo"/>, e não aqui.
        ///
        /// A mesma máquina aparece na tela de fim da aula, e as duas precisam
        /// desenhar o mesmo boneco: um robô com a cabeça em outro lugar na tela
        /// seguinte não é o robô do aluno, é outro desenho.
        /// </summary>
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
                ? $"{Catalogo.Bancadas} bancadas, {p.EstrelasTotais} estrelas"
                : $"{montadas} de {Robo.Pecas} peças no lugar";
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

        /// <summary>
        /// Quantas peças já estão no lugar.
        ///
        /// São <see cref="Robo.Pecas"/> peças para <see cref="Catalogo.Bancadas"/>
        /// bancadas, e a diferença é a antena: ela é a peça DESTE balcão, e chega
        /// quando o aluno vem retirá-la — o que só acontece com as onze fechadas.
        /// Enquanto ela era cobrada como bancada visitada, a máquina nunca ficava
        /// inteira: a tela dizia "está de pé" com um buraco no topo da cabeça.
        /// </summary>
        static int Montadas(Progresso p)
        {
            var quantas = p.TodasTentadas ? 1 : 0;
            for (var i = 1; i <= Catalogo.Bancadas; i++)
                if (p.Tentada($"e{i}")) quantas++;
            return quantas;
        }

        void DesenharRobo(RectTransform quadro, Progresso p, bool pronta)
        {
            for (var i = 0; i < Robo.Pecas; i++)
            {
                // A antena é a peça do balcão; as demais vêm de uma bancada cada.
                var tem = i + 1 > Catalogo.Bancadas ? pronta : p.Tentada($"e{i + 1}");

                // A peça que falta não some: fica como um vazio escuro, do tamanho
                // exato do que deveria estar ali. Sumir de vez faria a máquina
                // parecer menor em vez de incompleta, e é a falta que esta tela
                // precisa mostrar.
                var peca = Robo.Peca(quadro, i,
                                     tem ? Cores.Madeira : new Color(0.16f, 0.18f, 0.24f));
                if (!tem) continue;

                // Máquina desmontada não fica reta. Cada peça posta antes do fim
                // pende um pouco, e o conjunto só se alinha quando a última chega.
                if (!pronta) peca.localRotation = Quaternion.Euler(0f, 0f, Robo.Pendor(i));
            }

            var olho = Robo.Olho(quadro, pronta ? Cores.Luz : Cores.TintaOpaca);

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
