using System.Collections;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A REVELAÇÃO — a máquina por dentro, uma vez, e é o fim da bancada.
    ///
    /// Depois de dez salas seguindo um pacote, o aluno chega ao último nível e vê DE
    /// ONDE VINHA AQUILO. A parede de trezentas e dezoito lâmpadas, os doze
    /// neurônios do meio, e a luz atravessando a malha inteira de um lado ao outro.
    ///
    /// E TAMBÉM É O QUE ELE VÊ SE PEDIR PARA SAIR ANTES. Quem aperta sair no meio do
    /// corredor cai aqui, na mesma sequência, e só então a moldura fecha — o que a
    /// desistência custa é a estrela, não a lição. Ver DesafioMalha.Desistir. Por
    /// isso este arquivo não pode supor que as dez salas foram vencidas: ele desenha
    /// a mesma coisa nos dois casos, e o único texto que fala do corredor é o do
    /// primeiro cartaz, que descreve salas que qualquer um dos dois já atravessou.
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
    /// E ELA PASSA UMA VEZ SÓ. Depois desta travessia vinham outras três, uma por
    /// palavra da frase, a um clique de "a próxima palavra" cada. Repetição não
    /// ensinava: é a MESMA tela com outra janela, e o que ela tinha a acrescentar —
    /// que a palavra escrita volta para a entrada como parte da pergunta seguinte —
    /// cabe numa frase do cartaz de fecho.
    ///
    /// O RITMO É DE QUATRO TEMPOS, e a ordem importa:
    ///
    ///   1. Um cartaz curto, ligando as salas do corredor às peneiras da malha.
    ///      Curto porque ele acabou de correr e a mão dele ainda está no teclado.
    ///   2. A animação rodando devagar, narrada, SEM PEDIR NADA. Não há palpite a
    ///      dar nem placar a fazer: é o único momento da bancada em que ele pode
    ///      olhar em vez de decidir, e é para isso que ele veio.
    ///   3. O cartaz que nomeia o que ele acabou de ver, e então a malha escreve a
    ///      palavra que sobrou — ver DesafioMalha.Revelar.
    ///   4. O cartaz de fecho, que cobra o preço daquela palavra e fecha a bancada.
    ///      Ver DesafioMalha.Fechar.
    ///
    /// Meter uma pergunta junto com a explicação seria economizar uma tela e perder
    /// a bancada: ninguém lê um parágrafo enquanto decide.
    /// </summary>
    public partial class DesafioMalha
    {
        RectTransform _cartazDaRevelacao;

        /// <summary>
        /// Abre a malha pela revelação, em vez de cair direto na animação.
        ///
        /// O cartaz conta as salas QUE ELE ATRAVESSOU, e não as dez do corredor
        /// inteiro. Dizia "dez" fixo, de quando só chegava aqui quem vencia tudo;
        /// agora chega também quem pediu para sair na terceira sala, e a bancada
        /// abriria a parte mais importante da aula mentindo na cara dele. A alegoria
        /// não precisa das dez: ela precisa que CADA sala tenha testado de novo, e
        /// três salas já testaram três vezes.
        /// </summary>
        void AbrirARevelacao()
        {
            Painel.Rodape("a máquina por dentro");

            var quantas = _salaAtual == 1 ? "uma sala" : $"{_salaAtual} salas";
            var corrida = _salaAtual == 0
                ? "Você mal entrou no corredor, e ele já estava testando se o\n" +
                  "seu pacote passava. Chão que cede, espinho que sobe, porta\n" +
                  "que foge: peneira atrás de peneira, e chega quem sobrevive.\n\n"
                : $"O seu pacote atravessou {quantas}, e cada uma testou de\n" +
                  "novo se ele passava. Chão que cede, espinho que sobe, porta\n" +
                  "que foge: peneira atrás de peneira, e chega quem sobrevive a\n" +
                  "todas.\n\n";

            Cartaz("Por que o corredor testava tanto",
                corrida +
                "Esta máquina faz isso com a INFORMAÇÃO. E não são poucas\n" +
                "peneiras: são milhares, e ela passa por todas em um milésimo\n" +
                "de segundo.\n\n" +
                "Agora você vai ver por dentro, devagar.",
                "ver a informação ser peneirada",
                () => StartCoroutine(Atravessar()));
        }

        /// <summary>
        /// A frente de luz — a única travessia da bancada, narrada e devagar.
        ///
        /// É a mesma animação de quatro tempos que a bancada sempre teve. O "devagar"
        /// era em comparação com as travessias repetidas, que corriam encurtadas;
        /// elas não existem mais, e o ritmo de demonstração é o único que sobrou —
        /// que é o certo, porque ninguém aqui está apostando contra o relógio.
        /// </summary>
        IEnumerator Atravessar()
        {
            Painel.Instruir("as três palavras entram INTEIRAS, todas ao mesmo tempo", Cores.Luz);
            yield return Percorrer(0f, 1f, 0.7f, f => Iluminar(Fase.Entradas, f));

            Painel.Instruir("cada fio testa de novo o que chegou: soma, subtrai, ou anula", Cores.Luz);
            yield return Percorrer(0f, 1f, 1.7f, f => Iluminar(Fase.Travessia, f));

            Painel.Instruir("cada neurônio resume o que sobrou — e o que não passou vira zero", Cores.Luz);
            yield return Percorrer(0f, 1f, 0.8f, f => Iluminar(Fase.Meio, f));

            Painel.Instruir("do outro lado acende só o que atravessou a peneira inteira", Cores.Luz);
            yield return Percorrer(0f, 1f, 1.1f, f => Iluminar(Fase.Parede, f));

            // Sem trava de "já está rodando": ela existia para o botão "a próxima
            // palavra", que soltava uma segunda frente de luz na mesma malha se
            // clicado duas vezes. Não há mais botão que reinicie a animação.
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
