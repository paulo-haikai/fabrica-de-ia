using System.Collections;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A REVELAÇÃO — o que acontece quando o aluno vence o corredor.
    ///
    /// Depois de vinte fases seguindo lâmpadas, ele chega ao último nível e vê, pela
    /// primeira vez, DE ONDE VINHA AQUELA LUZ. A parede de trezentas e dezoito
    /// lâmpadas, os doze neurônios do meio, e a frente de luz atravessando a malha
    /// inteira de um lado ao outro: é o caminho que a informação percorre para
    /// chegar à resposta.
    ///
    /// A ANIMAÇÃO NÃO É NOVA — ela já existia, e é a parte mais bem resolvida desta
    /// bancada desde sempre. O que mudou foi ONDE ela aparece. Antes ela abria o
    /// minigame, e um aluno que nunca tinha visto uma rede olhava uma tela cheia de
    /// fios acendendo e não sabia o que estava procurando. Agora ela FECHA: ele já
    /// leu aquelas lâmpadas duzentas vezes, já entrou na porta errada, já entendeu
    /// no corpo que a mais acesa é a resposta. A animação deixa de ser a introdução
    /// de um conceito e vira a resposta a uma pergunta que ele já está fazendo —
    /// "mas de onde saía aquele número?".
    ///
    /// O RITMO É DE TRÊS TEMPOS, e a ordem importa:
    ///
    ///   1. Um cartaz curto, ligando o corredor à malha. Curto porque ele acabou de
    ///      vencer e a mão dele ainda está no teclado.
    ///   2. A animação rodando SOZINHA, sem aposta e sem placar. É a única vez em
    ///      que ele pode olhar sem ter nada a perder — e é por isso que ela roda
    ///      antes da primeira aposta, e não durante.
    ///   3. O cartaz que nomeia o que ele acabou de ver, e só então as apostas.
    ///
    /// Meter a explicação junto com a primeira aposta seria economizar uma tela e
    /// perder a bancada: ninguém lê um parágrafo enquanto decide.
    /// </summary>
    public partial class DesafioMalha
    {
        RectTransform _cartazDaRevelacao;

        /// <summary>
        /// Abre o nível da malha com a revelação, em vez de cair direto na aposta.
        /// </summary>
        void AbrirARevelacao()
        {
            Painel.Rodape("a máquina por dentro");

            Cartaz("De onde vinha a luz",
                "Nas vinte fases, você seguiu a lâmpada mais acesa.\n" +
                "Cada uma delas era um número que esta máquina calculou.\n\n" +
                "Agora você vai ver a conta acontecer — o caminho inteiro que\n" +
                "a informação percorre, das três palavras do seu pacote até a\n" +
                "palavra que acende do outro lado.",
                "ver a luz atravessar",
                () => StartCoroutine(Atravessar()));
        }

        /// <summary>
        /// A frente de luz, sem aposta e sem veredito.
        ///
        /// É a mesma animação de quatro tempos que a bancada sempre teve, e ela roda
        /// mais devagar aqui de propósito: numa demonstração o aluno tem tempo, e é
        /// o único momento da bancada inteira em que ele pode olhar em vez de
        /// decidir.
        /// </summary>
        IEnumerator Atravessar()
        {
            _rodando = true;

            Painel.Instruir("as três palavras entram como números", Cores.Luz);
            yield return Percorrer(0f, 1f, 0.7f, f => Iluminar(Fase.Entradas, f));

            Painel.Instruir("a luz atravessa TODOS os fios ao mesmo tempo", Cores.Luz);
            yield return Percorrer(0f, 1f, 1.7f, f => Iluminar(Fase.Travessia, f));

            Painel.Instruir("cada neurônio soma o que chegou — e alguns apagam", Cores.Luz);
            yield return Percorrer(0f, 1f, 0.8f, f => Iluminar(Fase.Meio, f));

            Painel.Instruir("a parede inteira acende de uma vez", Cores.Luz);
            yield return Percorrer(0f, 1f, 1.1f, f => Iluminar(Fase.Parede, f));

            _rodando = false;
            Fechamento();
        }

        /// <summary>O cartaz que nomeia o que ele acabou de ver.</summary>
        void Fechamento()
        {
            var vencedora = _rede.Palavra(_vencedora);
            var chance = _rede.Chances[_vencedora];

            Cartaz("É isto que faz a lâmpada acender",
                "Não houve caminho escolhido. Não teve uma bolinha andando pela\n" +
                "malha até achar a saída — isso seria uma árvore de decisão.\n\n" +
                "A luz saiu de TODAS as entradas ao mesmo tempo, cada fio\n" +
                "carregando um número, e cada neurônio somando o que veio de\n" +
                "todos os anteriores. Do outro lado acenderam as 318 lâmpadas\n" +
                $"juntas, e a mais forte foi “{vencedora}”, com {chance * 100f:0}%.\n\n" +
                "A precisão não vem de nenhum fio em particular. Vem de seis mil\n" +
                "números somando ao mesmo tempo — e é por isso que a resposta não\n" +
                "está escrita em lugar nenhum lá dentro.",
                "agora deixe eu apostar",
                () => { Painel.Acao(null, null); NovoPasse(); });
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
