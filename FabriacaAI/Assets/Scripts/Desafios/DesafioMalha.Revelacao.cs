using System.Collections;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A REVELAÇÃO — o que acontece quando o aluno vence o corredor.
    ///
    /// Depois de dez salas seguindo um pacote, ele chega ao último nível e vê DE
    /// ONDE VINHA AQUILO. A parede de trezentas e dezoito lâmpadas, os doze
    /// neurônios do meio, e a luz atravessando a malha inteira de um lado ao outro.
    ///
    /// A ALEGORIA É O CORREDOR, e é ela que este nível cobra de volta. Cada sala
    /// testou de novo se o pacote passava; a malha faz o mesmo com a informação,
    /// milhares de vezes e em milésimos de segundo. O aluno não precisa aprender uma
    /// imagem nova aqui — precisa descobrir que a imagem que ele já tem nas mãos era
    /// a máquina o tempo todo.
    ///
    /// PENEIRA, E NÃO CAMINHO. É a correção que vale a bancada inteira: quase todo
    /// mundo imagina uma bolinha achando um caminho pela malha, como num pinball —
    /// isso é árvore de decisão. Aqui a informação entra INTEIRA por todas as
    /// entradas, cada fio a testa de novo (soma, subtrai, ou anula), cada neurônio
    /// resume o que sobrou, e o que não passa vira zero e não segue. O que chega do
    /// outro lado não é "a bolinha na saída 7": é luz em TODAS as lâmpadas, e sobra
    /// a mais forte.
    ///
    /// A ANIMAÇÃO NÃO É NOVA — ela já existia, e é a parte mais bem resolvida desta
    /// bancada desde sempre. O que mudou foi ONDE ela aparece. Antes ela abria o
    /// minigame, e um aluno que nunca tinha visto uma rede olhava uma tela cheia de
    /// fios acendendo e não sabia o que estava procurando. Agora ela FECHA: ele já
    /// atravessou dez salas e já sabe no corpo o que é passar por peneira. A
    /// animação deixa de ser a introdução de um conceito e vira a resposta a uma
    /// pergunta que ele já está fazendo.
    ///
    /// O RITMO É DE TRÊS TEMPOS, e a ordem importa:
    ///
    ///   1. Um cartaz curto, ligando as salas do corredor às peneiras da malha.
    ///      Curto porque ele acabou de vencer e a mão dele ainda está no teclado.
    ///   2. A animação rodando devagar, narrada, SEM PEDIR NADA. Não há palpite a
    ///      dar nem placar a fazer: é o único momento da bancada em que ele pode
    ///      olhar em vez de decidir, e é para isso que ele veio.
    ///   3. O cartaz que nomeia o que ele acabou de ver, e então a malha escreve.
    ///
    /// Meter uma pergunta junto com a explicação seria economizar uma tela e perder
    /// a bancada: ninguém lê um parágrafo enquanto decide.
    /// </summary>
    public partial class DesafioMalha
    {
        RectTransform _cartazDaRevelacao;

        /// <summary>
        /// Abre o nível da malha com a revelação, em vez de cair direto na animação.
        /// </summary>
        void AbrirARevelacao()
        {
            Painel.Rodape("a máquina por dentro");

            Cartaz("Por que eram tantas salas",
                "O seu pacote atravessou dez salas, e cada uma testou de novo\n" +
                "se ele passava. Chão que cede, espinho que sobe, porta que\n" +
                "foge: peneira atrás de peneira, e chega quem sobrevive a todas.\n\n" +
                "Esta máquina faz isso com a INFORMAÇÃO. Não são dez peneiras:\n" +
                "são milhares, e ela passa por todas em um milésimo de segundo.\n\n" +
                "Agora você vai ver por dentro, devagar.",
                "ver a informação ser peneirada",
                () => StartCoroutine(Atravessar()));
        }

        /// <summary>
        /// A frente de luz, narrada e mais devagar que nas travessias seguintes.
        ///
        /// É a mesma animação de quatro tempos que a bancada sempre teve. Ela roda
        /// mais devagar aqui de propósito: numa demonstração o aluno tem tempo.
        /// </summary>
        IEnumerator Atravessar()
        {
            _rodando = true;

            Painel.Instruir("as três palavras entram INTEIRAS, todas ao mesmo tempo", Cores.Luz);
            yield return Percorrer(0f, 1f, 0.7f, f => Iluminar(Fase.Entradas, f));

            Painel.Instruir("cada fio testa de novo o que chegou: soma, subtrai, ou anula", Cores.Luz);
            yield return Percorrer(0f, 1f, 1.7f, f => Iluminar(Fase.Travessia, f));

            Painel.Instruir("cada neurônio resume o que sobrou — e o que não passou vira zero", Cores.Luz);
            yield return Percorrer(0f, 1f, 0.8f, f => Iluminar(Fase.Meio, f));

            Painel.Instruir("do outro lado acende só o que atravessou a peneira inteira", Cores.Luz);
            yield return Percorrer(0f, 1f, 1.1f, f => Iluminar(Fase.Parede, f));

            _rodando = false;
            Fechamento();
        }

        /// <summary>O cartaz que nomeia o que ele acabou de ver.</summary>
        void Fechamento()
        {
            var vencedora = _rede.Palavra(_vencedora);
            var chance = _rede.Chances[_vencedora];

            Cartaz("Peneira atrás de peneira",
                "Não houve caminho escolhido. Não teve uma bolinha andando pela\n" +
                "malha até achar a saída — isso seria uma árvore de decisão.\n\n" +
                "A informação entrou INTEIRA e foi testada de novo em cada fio.\n" +
                "Uns empurram a favor, outros empurram contra, e o neurônio que\n" +
                "soma negativo apaga: o que não passa numa peneira não segue\n" +
                "para a próxima. Camada após camada, sobra cada vez menos.\n\n" +
                "No fim acenderam as 318 lâmpadas juntas, e o que restou de\n" +
                $"tudo isso foi “{vencedora}”, com {chance * 100f:0}% da luz.\n\n" +
                "Nenhum fio apontou essa palavra. Ela é o que sobreviveu a seis\n" +
                "mil números peneirando ao mesmo tempo — e é por isso que a\n" +
                "resposta não está escrita em lugar nenhum lá dentro.",
                "ver ela escrever",
                () => { Painel.Acao(null, null); Revelar(); });
        }

        /// <summary>
        /// Um cartaz por cima da malha, que não a apaga.
        ///
        /// Fundo translúcido de propósito: a malha continua visível atrás do texto,
        /// e é ela que o texto está explicando. Cartaz opaco faria o aluno ler sobre
        /// uma coisa que sumiu da tela.
        /// </summary>
        void Cartaz(string titulo, string corpo, string botao, System.Action aoTocar)
        {
            if (_cartazDaRevelacao != null) Destroy(_cartazDaRevelacao.gameObject);

            _cartazDaRevelacao = Widgets.Painel("Revelação", Area,
                new Color(Cores.Tinta.r, Cores.Tinta.g, Cores.Tinta.b, 0.93f));
            Widgets.Esticar(_cartazDaRevelacao);

            var cabeca = Widgets.Texto("Título", _cartazDaRevelacao, 22,
                                       TextAnchor.UpperCenter, Cores.Luz);
            Widgets.Faixa(cabeca.rectTransform, true, 30f, 26f);
            cabeca.text = titulo;

            var texto = Widgets.Texto("Corpo", _cartazDaRevelacao, 16,
                                      TextAnchor.UpperCenter, Cores.Papel);
            Widgets.Esticar(texto.rectTransform);
            texto.rectTransform.offsetMin = new Vector2(48f, 44f);
            texto.rectTransform.offsetMax = new Vector2(-48f, -70f);
            texto.text = corpo;

            Widgets.Surgir(_cartazDaRevelacao, 0.18f, 0.97f);

            Painel.Acao(botao, () =>
            {
                Painel.Acao(null, null);
                if (_cartazDaRevelacao != null) Destroy(_cartazDaRevelacao.gameObject);
                _cartazDaRevelacao = null;
                aoTocar();
            });
        }
    }
}
