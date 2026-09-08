using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Bancada 9 — os holofotes de Lumi.
    ///
    /// Inspiração: FLOW FREE, pela escassez. Lá você tem menos tubo do que
    /// gostaria e precisa decidir onde gastar; aqui você tem duas lâmpadas para
    /// sete palavras e precisa decidir quais valem a pena acender.
    ///
    /// A frase aparece sem a última palavra. As palavras anteriores começam
    /// apagadas, e a máquina só vê o que está aceso. O aluno acende duas e manda
    /// perguntar; se ela acertar a palavra que falta, ele apontou bem.
    ///
    /// O que faz esta bancada funcionar é que a verificação é MECÂNICA. Não sou eu
    /// dizendo "essa é a palavra importante" — é a máquina acertando ou errando
    /// conforme o que está iluminado (ver <see cref="Companhias"/>). O aluno
    /// descobre sozinho, e sempre, a mesma coisa:
    ///
    ///   · acender "na", "de", "a" não serve de nada, mesmo estando ao lado da
    ///     lacuna, porque essas palavras andam com todas as outras;
    ///   · acender "professora" ou "horta", longe da lacuna, resolve.
    ///
    /// Ou seja: o que decide a próxima palavra não é a proximidade, é a
    /// informação. E escolher onde olhar, dentro de uma frase, é exactamente a
    /// operação que dá nome aos modelos de atenção — os que fazem o T de GPT.
    /// </summary>
    public partial class DesafioHolofotes : DesafioEmNiveis
    {
        public override string Etapa => "e9";
        public override string Titulo => "Onde ela olha";

        protected override int Niveis => _rodadas?.Count ?? Holofotes.PorAula;

        Companhias _companhias;
        List<Rodada10> _rodadas;
        Rodada10 _rodada;

        /// <summary>
        /// Perguntas por rodada. Perguntar era ilimitado, e por isso não era jogo:
        /// dava para varrer todas as combinações de lâmpadas até uma dar certo, sem
        /// custo. Quatro é o bastante para tatear e pouco para brutalizar.
        /// </summary>
        const int Perguntas = 4;

        readonly HashSet<int> _acesas = new();
        int _perguntas;

        /// <summary>Já perguntou nesta configuração? É o que libera as barras de força.</summary>
        bool _revelado;

        RectTransform _frase;

        /// <summary>
        /// A caixinha da lacuna. Guardada porque é ela que treme quando a
        /// resposta sai errada — o alvo que falhou aponta para si mesmo.
        /// </summary>
        RectTransform _lacuna;
        RectTransform _opcoes;
        Text _placar;

        protected override void Preparar()
        {
            _companhias = new Companhias(Corpus.Frases);
            _rodadas = Holofotes.Sortear(_companhias, Rodadas.Semente());
        }

        protected override void MontarNivel()
        {
            _rodada = _rodadas[NivelAtual];
            _acesas.Clear();
            _perguntas = 0;
            _revelado = false;

            Painel.Rodape("a máquina só vê o que está aceso · " +
                          "clique nas palavras DA FRASE para acender");

            var rotulo = Widgets.Texto("Rótulo", Area, 15, TextAnchor.UpperCenter, Cores.Neblina);
            Widgets.Faixa(rotulo.rectTransform, true, 20f);
            rotulo.text = $"acenda até {_rodada.Lampadas} palavras e pergunte o que vem depois";

            _frase = Widgets.Painel("Frase", Area, Color.clear);
            Widgets.Faixa(_frase, true, 120f, 26f);

            _placar = Widgets.Texto("Placar", Area, 15, TextAnchor.UpperCenter, Cores.Luz);
            Widgets.Faixa(_placar.rectTransform, true, 20f, 152f);

            var rotuloOpcoes = Widgets.Texto("RótuloOpções", Area, 14,
                                             TextAnchor.UpperCenter, Cores.Neblina);
            Widgets.Faixa(rotuloOpcoes.rectTransform, true, 18f, 182f);
            // Diz QUEM escolhe. O texto anterior — "as palavras entre as quais ela
            // vai escolher" — era verdadeiro e lido ao contrário: soava como um menu
            // de opções para o aluno, que é o único convite que esta tela não pode
            // fazer.
            rotuloOpcoes.text = "ELA escolhe entre estas quatro · você escolhe onde apontar a luz";

            _opcoes = Widgets.Painel("Opções", Area, Color.clear);
            Widgets.Esticar(_opcoes);
            _opcoes.offsetMax = new Vector2(0f, -206f);

            Redesenhar();
        }

        // ------------------------------------------------------------ pintura

        void Redesenhar()
        {
            foreach (Transform filho in _frase) Destroy(filho.gameObject);
            foreach (Transform filho in _opcoes) Destroy(filho.gameObject);

            DesenharFrase();
            DesenharOpcoes();

            var restam = Perguntas - _perguntas;
            _placar.text = $"acesas: {_acesas.Count} de {_rodada.Lampadas}" +
                           $"   ·   perguntas: {restam} de {Perguntas}";
            Widgets.Contar(_placar, restam, 1);

            Painel.Instruir(_acesas.Count == 0
                ? "nenhuma palavra acesa — ela vai responder no escuro"
                : restam == 1 ? "última pergunta — escolha bem onde apontar"
                : "aponte os holofotes e pergunte");
            Painel.Acao("perguntar", Perguntar);
        }

        void DesenharFrase()
        {
            var palavras = _rodada.Antes;
            var larguras = palavras.Select(p => 18f + p.Length * 10f).ToArray();
            var vazio = 90f;
            var total = larguras.Sum() + palavras.Length * 8f + vazio;
            var x = -total / 2f;

            for (var i = 0; i < palavras.Length; i++)
            {
                var indice = i;
                var acesa = _acesas.Contains(i);

                var botao = Widgets.Botao($"p{i}", _frase, palavras[i],
                                          acesa ? Cores.Luz : Cores.TintaClara,
                                          acesa ? Cores.Tinta : Cores.Neblina, 16);
                Widgets.Fixar((RectTransform)botao.transform, new Vector2(0.5f, 0.5f),
                              new Vector2(x + larguras[i] / 2f, 12f),
                              new Vector2(larguras[i], 46f));
                botao.onClick.AddListener(() => Alternar(indice));

                // O facho: um risquinho de luz descendo da palavra acesa. É o que
                // faz "aceso" parecer holofote e não seleção de texto.
                if (acesa)
                {
                    var facho = Widgets.Painel("f", _frase, new Color(0.95f, 0.76f, 0.31f, 0.35f));
                    Widgets.Fixar(facho, new Vector2(0.5f, 0.5f),
                                  new Vector2(x + larguras[i] / 2f, -26f),
                                  new Vector2(larguras[i] * 0.6f, 24f));
                }

                x += larguras[i] + 8f;
            }

            // A lacuna, no fim da frase.
            //
            // Fundo mais claro que as palavras, e não mais escuro: ela é o alvo
            // da tela e precisa ser a coisa que o olho encontra primeiro. Na
            // primeira versão era tinta sobre tinta e sumia no fundo.
            var lacuna = Widgets.Ficha("lacuna", _frase, "? ? ?",
                                       new Vector2(x + vazio / 2f, 12f),
                                       new Vector2(vazio, 46f), Cores.Madeira, Cores.Luz, 18);
            _lacuna = (RectTransform)lacuna.transform.parent;
        }

        /// <summary>
        /// Os candidatos. As barras de força aparecem só DEPOIS de perguntar.
        ///
        /// Antes elas eram desenhadas ao vivo, e isso quebrava a bancada como jogo: a
        /// resposta ficava exposta antes da pergunta. Bastava acender lâmpadas até a
        /// barra do candidato certo passar as outras e clicar em "perguntar" —
        /// perguntar era decoração, porque a informação que a pergunta ia revelar já
        /// estava na tela.
        ///
        /// Agora a ordem é a de qualquer aposta honesta, a mesma de e1 e e6: você se
        /// compromete, e só então descobre. E as barras continuam aparecendo — no
        /// feedback, onde elas ensinam em vez de entregar.
        /// </summary>
        void DesenharOpcoes()
        {
            const float largura = 160f;

            var maior = !_revelado ? 0f : _rodada.Candidatos.Max(
                c => _companhias.Nota(_acesas.Select(k => _rodada.Antes[k]), c));

            for (var i = 0; i < _rodada.Candidatos.Length; i++)
            {
                var candidato = _rodada.Candidatos[i];

                // MOSTRADOR, E NÃO BOTÃO — e a cor é o que diz isso.
                //
                // Era Cores.TintaClara: EXATAMENTE o mesmo fundo das palavras
                // clicáveis da frase. Quatro caixas do tamanho de um botão, com a
                // cor de um botão, sob um rótulo que dizia "as palavras entre as
                // quais ela vai escolher" — e que não respondiam a clique nenhum,
                // porque quem escolhe o candidato é a MÁQUINA, não o aluno.
                //
                // O aluno clicava nelas, nada acontecia, e ele concluía que a
                // bancada estava quebrada. Não estava: ele estava clicando no lugar
                // errado, e a tela tinha mandado ele clicar ali.
                //
                // Cores.Tinta é mais escura que o fundo das palavras e recua para
                // trás. Affordance errada é bug: um controle que não controla mente
                // tanto quanto um número errado.
                var caixa = Widgets.Painel($"o{i}", _opcoes, Cores.Tinta);
                Widgets.Fixar(caixa, new Vector2(0.5f, 1f),
                              new Vector2((i - (_rodada.Candidatos.Length - 1) / 2f) * (largura + 8f),
                                          -34f),
                              new Vector2(largura, 56f));

                var texto = Widgets.Texto("t", caixa, 16, TextAnchor.UpperCenter, Cores.Papel);
                Widgets.Faixa(texto.rectTransform, true, 22f, 4f);
                texto.text = candidato;

                var barra = Widgets.Barra("b", caixa, new Vector2(0f, -14f),
                                          new Vector2(largura - 20f, 12f),
                                          Cores.Tinta, Cores.Vidro);

                // O trilho NASCE CHEIO: <see cref="Widgets.Barra"/> cria o miolo com a
                // largura do trilho inteiro. Quem desenha a barra e não a preenche não
                // está mostrando "nada" — está mostrando TUDO.
                //
                // Era o que acontecia aqui antes de perguntar: um `continue` pulava o
                // preenchimento, e os quatro candidatos apareciam com a força no
                // máximo. Duas coisas quebravam de uma vez. A promessa desta bancada,
                // dita no resumo acima, é que a barra só existe depois da aposta — e
                // quatro barras cheias são barras existindo. E a lição saía ao
                // contrário: no feedback as barras DESCIAM da borda até o valor real, e
                // queda se lê como perda, não como revelação.
                //
                // Por isso `Encher` é chamado SEMPRE, com zero enquanto não houve
                // pergunta (`maior` é zero nesse caso, e a fração vai a zero junto).
                // Nenhuma das outras bancadas com barra confia no estado em que o
                // widget nasce; as duas chamam `Encher` na linha seguinte à criação.
                var nota = _revelado
                    ? _companhias.Nota(_acesas.Select(k => _rodada.Antes[k]), candidato)
                    : 0f;
                Widgets.Encher(barra, maior <= 0f ? 0f : nota / maior, largura - 20f);
            }
        }

        // ------------------------------------------------------------- jogadas

        void Alternar(int indice)
        {
            // Mexer nos holofotes esconde as barras de novo. Elas descrevem a
            // configuração que FOI perguntada; deixá-las na tela depois de mover uma
            // lâmpada devolveria o defeito antigo pela porta dos fundos — o aluno
            // moveria as luzes vendo as barras reagirem, sem gastar pergunta.
            _revelado = false;

            if (_acesas.Remove(indice))
            {
                Redesenhar();
                return;
            }
            if (_acesas.Count >= _rodada.Lampadas)
            {
                Painel.Instruir("as lâmpadas acabaram — apague uma para acender outra", Cores.Brasa);
                return;
            }
            _acesas.Add(indice);
            Redesenhar();
        }

        void Perguntar()
        {
            _perguntas++;
            var palpite = _companhias.Palpite(_acesas.Select(k => _rodada.Antes[k]),
                                              _rodada.Candidatos);

            if (palpite == _rodada.Resposta)
            {
                Vencer();
                return;
            }

            // Perguntou: agora as barras de força podem aparecer. Elas mostram POR QUE
            // ela respondeu aquilo, e essa é a informação que o aluno compra ao gastar
            // uma pergunta.
            _revelado = true;

            // Redesenha ANTES do recado: `Redesenhar` reescreve a instrução, e
            // pôr o recado antes dela seria escrevê-lo para ser apagado.
            Redesenhar();

            // Só depois do redesenho, que destrói e refaz a frase inteira: tremer
            // a lacuna velha seria animar um objeto que já morreu. O tremor diz,
            // sem frase, ONDE a resposta falhou — a palavra que continua faltando.
            Widgets.Tremer(_lacuna);

            if (_perguntas >= Perguntas)
            {
                Desistir10(palpite);
                return;
            }

            // Palpite nulo significa que nada do que está aceso se associa a
            // nenhum candidato. Dizer "ela não tem ideia" é mais verdadeiro — e
            // mais informativo — que fingir um chute.
            Painel.Instruir(palpite == null
                ? "ela não tem ideia — o que está aceso não diz nada"
                : $"ela respondeu “{palpite}” — mova os holofotes e tente de novo",
                Cores.Brasa);
        }

        /// <summary>
        /// As perguntas acabaram sem ela acertar.
        ///
        /// A bancada não tinha derrota, e por isso não era jogo: perguntar era grátis
        /// e infinito, então dava para varrer todas as combinações de lâmpadas até uma
        /// funcionar. Sem custo por errar, apontar bem e apontar mal valiam o mesmo.
        ///
        /// A estrela sai quando ela chegou a chutar ALGO — porque tirar um chute do
        /// escuro já é ter apontado para informação, e é metade da lição.
        /// </summary>
        void Desistir10(string ultimoPalpite)
        {
            var frase = string.Join(" ", _rodada.Antes);

            Falhou("As perguntas acabaram",
                $"“{frase} {_rodada.Resposta}”\n\n" +
                (ultimoPalpite == null
                    ? "Ela terminou sem ideia nenhuma. O que estava aceso não " +
                      "associava\ncom nenhum dos candidatos.\n\n"
                    : $"O último palpite dela foi “{ultimoPalpite}”.\n\n") +
                "Olhe as barras: elas mostram com o que cada candidato combina.\n" +
                "As palavras coladas na lacuna quase nunca são as que decidem —\n" +
                "quem decide é a palavra específica, mesmo lá atrás na frase.\n\n" +
                "Escolher onde olhar tem nome: atenção.",
                contaEstrela: ultimoPalpite != null);
        }

        void Vencer()
        {
            var acesas = _acesas.Select(k => _rodada.Antes[k]).ToList();
            var frase = string.Join(" ", _rodada.Antes);

            // Compara o que o aluno acendeu com o que estava colado na lacuna.
            var colada = _rodada.Antes[^1];
            var acendeuAColada = acesas.Contains(colada);

            Resolveu($"“{frase} {_rodada.Resposta}”",
                $"Ela acertou vendo só: {string.Join(" e ", acesas.Select(a => $"“{a}”"))}.\n\n" +
                (acendeuAColada
                    ? "A palavra colada na lacuna ajudou pouco sozinha — quem\n" +
                      "decidiu foi a outra, mais distante e mais específica.\n\n"
                    : "Nenhuma das que você acendeu estava colada na lacuna —\n" +
                      "as vizinhas imediatas eram justamente as que não diziam nada.\n\n") +
                "O que decide a próxima palavra não é a proximidade: é a\n" +
                "informação. Isso tem nome — atenção — e é a peça que faz os\n" +
                "modelos de hoje funcionarem melhor que a tabela de Dona Ciça.");
        }
    }
}
