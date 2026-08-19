using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Engine;
using FabricaDeIA.Nucleo;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Bancada 6 — a malha de Iara. Corra pelo farol, e depois veja o farol por
    /// dentro.
    ///
    /// SEIS NÍVEIS, E OS CINCO PRIMEIROS SÃO OUTRO JOGO.
    ///
    ///   · Níveis 1 a 5 — O CORREDOR: vinte fases à maneira de Level Devil, quatro
    ///     por nível. O aluno leva um pacote de mensagens por uma fase comprida com
    ///     várias saídas, e só a saída da lâmpada mais acesa aceita o pacote. As
    ///     palavras trocam de porta enquanto ele corre. Ver DesafioMalha.Corredor.cs.
    ///   · Nível 6 — A MALHA, exatamente como sempre foi: o passe adiante animado,
    ///     a aposta antes de a luz atravessar, e o placar que compara com Dona
    ///     Ciça. É o momento em que ele vê o que estava por trás das lâmpadas o
    ///     tempo todo — cada uma delas era esta parede acendendo.
    ///
    /// A ORDEM É O CONTEÚDO. Correr primeiro e explicar depois é o contrário do que
    /// uma aula costuma fazer, e é de propósito: quando a animação da malha começa,
    /// o aluno já leu aquelas lâmpadas duzentas vezes e já apanhou de entrar na
    /// porta errada. A animação não é a introdução de um conceito novo; é a resposta
    /// a uma pergunta que ele já está fazendo.
    ///
    /// E o corredor carrega uma lição que a animação sozinha não dava: as portas
    /// TROCAM DE LUGAR, e a lâmpada vai junto com a palavra. A resposta de uma rede
    /// não é um endereço, é um valor — não existe a gaveta da palavra certa. Quem
    /// decora o lugar erra; quem lê a luz acerta sempre.
    ///
    /// O QUE O CORREDOR CONSERTOU. A bancada só pedia aposta quando a própria rede
    /// estava em dúvida, e pedia ANTES de qualquer evidência aparecer: com ~45% de
    /// acerto, a primeira rodada dava 42% de vitória, e o aluno ganhava ou perdia
    /// sem saber por quê. Era o defeito que nenhuma constante consertava, medido em
    /// Conferencias.Malha6. No corredor a evidência está na tela em todo instante e
    /// o erro custa um tombo, não um ponto.
    ///
    /// A VERSÃO DE ANTES DA ANIMAÇÃO era outra coisa ainda, e estava errada. Ela
    /// pedia para dimensionar camadas com botões de mais e menos até o número de
    /// fios cair numa faixa. Ensinava um fato verdadeiro — parâmetro cresce
    /// multiplicativamente — e não ensinava o que a bancada tinha que ensinar: o
    /// que a rede FAZ.
    ///
    /// O QUE A ANIMAÇÃO CORRIGE NA INTUIÇÃO COMUM. A imagem que quase todo mundo
    /// tem é de uma bolinha achando um caminho pela malha, como num pinball. Isso
    /// é árvore de decisão, não rede neural. Aqui a luz sai de TODAS as entradas
    /// ao mesmo tempo, cada conexão carrega um número, e cada neurônio soma o que
    /// vem de todos os anteriores. O que chega do outro lado não é "a bolinha na
    /// saída 7": é luz em TODAS as lâmpadas, e a mais forte ganha.
    ///
    /// Três verdades que a tela mostra sem precisar dizer:
    ///
    ///   · CONEXÃO SOMA OU SUBTRAI. Peso positivo acende dourado, negativo acende
    ///     turquesa. A malha não é só reforço; metade dela empurra contra.
    ///   · NEURÔNIO APAGA. Soma negativa vira zero, e zero não empurra nada
    ///     adiante. Pedaços da malha ficam escuros, e é assim mesmo.
    ///   · ELA ESCOLHE ENTRE TUDO O QUE SABE. A parede tem uma lâmpada por palavra
    ///     do vocabulário — 318 delas —, quase todas apagadas.
    ///
    /// E o placar do fim é o fecho da aula até aqui: esta malha acerta 68 de 100, e
    /// contar pares como Dona Ciça acerta 42. A diferença é olhar três palavras de
    /// uma vez em vez de uma — e os dois números saem do MESMO texto, senão a
    /// comparação não valeria nada.
    ///
    /// Nota sobre o 68: dá para treinar a mesma rede até 82, e a conferência mostrou
    /// por que isso PIORA a bancada. Rede que decorou responde com certeza quase
    /// absoluta, a parede acende uma lâmpada só, e a aposta deixa de ter dúvida.
    /// O ponto de parada foi escolhido pelo jogo, não pelo placar.
    /// </summary>
    public partial class DesafioMalha : DesafioEmNiveis
    {
        public override string Etapa => "e6";
        public override string Titulo => "A malha que escolhe";

        /// <summary>Cinco níveis de corredor, quatro fases cada, e um de malha.</summary>
        protected override int Niveis => NiveisDeCorredor + 1;

        /// <summary>Quantas apostas o nível da malha pede, e o mínimo para passar.</summary>
        static readonly (int apostas, int minimo) RodadaDaMalha = (3, 2);

        const int Opcoes = 3;

        Rede _rede;
        (int apostas, int minimo) _r;

        /// <summary>A frase que a malha está escrevendo, palavra por palavra.</summary>
        readonly List<string> _frase = new();
        /// <summary>
        /// Uma linha por passe: a janela lida, o palpite dela, se eu acertei, e se ela
        /// escreveu sozinha (janela sem dúvida, em que não fui consultado).
        /// </summary>
        readonly List<(string janela, string palavra, bool acertou, bool sozinha)> _linhas = new();

        /// <summary>Palavras que ELA escreveu na frase atual, fora as três da semente.</summary>
        int _geradas;

        /// <summary>As frases que ela terminou nesta rodada.</summary>
        readonly List<string> _prontas = new();

        List<int> _candidatas = new();
        int _aposta = -1;
        int _acertos;
        int _passes;
        bool _rodando;

        Mulberry32 _sorteio;

        protected override void Preparar()
        {
            _rede = Rede.Atual;
            _sorteio = new Mulberry32((uint)Rodadas.Semente());

            // A forma da rede atravessa para a bancada 7, que vai perguntar ao aluno
            // quantos desses números ele daria conta de girar à mão. Agora é a forma
            // da rede DE VERDADE, e não a de um tear de faz de conta.
            Progresso.Atual.camadas = _rede.Forma;
            Progresso.Atual.Salvar();
        }

        protected override void MontarNivel()
        {
            // Níveis 1 e 2: o corredor.
            //
            // Ele não mexe mais na frase nem na rede. O corredor virou um jogo de
            // plataforma puro — o boneco leva um pacote até a saída — e a máquina só
            // aparece no fim, inteira, em vez de vazar palavra por palavra pelas
            // portas. Espremer a rede dentro do platformer deixava o jogo pequeno e a
            // explicação pela metade ao mesmo tempo.
            if (NivelAtual < NiveisDeCorredor)
            {
                // A ordem das salas e sorteada uma vez por aula, na primeira. Sortear
                // a cada nivel embaralharia de novo no meio, e o aluno poderia repetir
                // uma sala que ja jogou.
                if (NivelAtual == 0) SortearOrdem();

                _salaAtual = NivelAtual * SalasPorNivel;
                _tombosNoNivel = 0;
                MontarSala();
                return;
            }

            // Nível 6: a malha, como sempre foi.
            //
            // O histórico é zerado aqui e as FRASES prontas não. O painel de cima
            // mostra uma linha por passe e cabem umas poucas; chegar com vinte
            // linhas do corredor o encheria antes do primeiro passe. As frases
            // sobrevivem porque o cartaz de fecho cita a última que ela terminou, e
            // a melhor delas foi escrita lá atrás, correndo.
            _noCorredor = false;
            _r = RodadaDaMalha;
            Semear();
            _acertos = 0;
            _passes = 0;
            _aposta = -1;
            _rodando = false;
            _linhas.Clear();

            MontarTela();

            // Prepara o primeiro passe e ABRE PELA REVELAÇÃO, em vez de cair direto
            // na aposta. NovoPasse deixa a rede rodada e a malha desenhada; a
            // revelação usa esse estado para animar a travessia sem cobrar nada, e
            // só depois devolve o jogo. Ver DesafioMalha.Revelacao.cs.
            NovoPasse();
            AbrirARevelacao();
        }

        /// <summary>
        /// Escolhe as três palavras com que a malha começa.
        ///
        /// Saem do começo de uma frase real do corpus, porque três palavras
        /// sorteadas soltas dariam uma janela que ninguém escreveu — e a rede
        /// responderia a uma pergunta absurda, o que confunde em vez de ensinar.
        /// </summary>
        void Semear()
        {
            var boas = Corpus.Frases
                .Select(f => f.Trim().Split(' '))
                .Where(p => p.Length > _rede.Janela)
                .Where(p => p.Take(_rede.Janela).All(_rede.Conhece))
                .ToList();

            var escolhida = boas[(int)(_sorteio.Proximo() * boas.Count)];

            _frase.Clear();
            for (var i = 0; i < _rede.Janela; i++) _frase.Add(escolhida[i]);
            _geradas = 0;
        }

        // ---------------------------------------------------------------- rodada

        /// <summary>
        /// Roda a rede na janela atual e oferece as três palavras mais acesas.
        ///
        /// As três MAIS ACESAS, e não três quaisquer: a resposta tem que estar entre
        /// as opções, senão a aposta é pegadinha. O que o aluno descobre é qual das
        /// três, e a evidência está na tela.
        ///
        /// E só pede aposta quando a janela está DISPUTADA. A conferência mostrou o
        /// motivo em números: em dois terços das janelas a vencedora é tão mais forte
        /// que a segunda que o aluno acerta no automático, sem olhar a malha. Aposta
        /// sem dúvida não é aposta — é uma tecla a apertar.
        ///
        /// Nas janelas fáceis a malha escreve sozinha, com a animação rodando. Isso
        /// não é pular conteúdo: é o ritmo certo, e ensina algo a mais que a versão
        /// anterior não ensinava — que às vezes ela tem certeza e às vezes não.
        /// </summary>
        void NovoPasse()
        {
            // A frase terminou: guarda e semeia outra. Deixar correr além disto era
            // o que produzia a frase sem sentido do cartaz final — ver o número
            // medido em Rede.PalavrasPorFrase.
            if (_geradas >= Rede.PalavrasPorFrase)
            {
                _prontas.Add(string.Join(" ", _frase));
                Semear();
            }

            var vencedora = EscreverAteDuvidar();
            _candidatas = _rede.MaisAcesas(Opcoes);

            // Embaralha: em ordem de força, a primeira seria sempre a resposta.
            for (var i = _candidatas.Count - 1; i > 0; i--)
            {
                var j = (int)(_sorteio.Proximo() * (i + 1));
                (_candidatas[i], _candidatas[j]) = (_candidatas[j], _candidatas[i]);
            }

            _aposta = -1;
            _vencedora = vencedora;

            Painel.MarcarPasso(NivelAtual + 1, Niveis,
                               $"aposta {_passes + 1} de {_r.apostas}");
            Painel.Acao(null, null);
            Painel.Instruir("qual lâmpada vai acender mais forte?");

            RefazerSaida();
            Redesenhar();
        }

        int _vencedora;

        /// <summary>
        /// Deixa a malha escrever as palavras de que ela tem certeza, e para na
        /// primeira em que hesita. Devolve o índice da vencedora dessa janela.
        ///
        /// As palavras escritas sozinha entram no histórico marcadas — o aluno vê
        /// que ela andou, e vê por que não foi consultado. Se ela não hesitar em
        /// quatro passos seguidos, eu pergunto de qualquer jeito: a bancada não pode
        /// escrever a frase inteira sem dar a vez a ele.
        /// </summary>
        int EscreverAteDuvidar()
        {
            // Duas travas: quantas ela pode escrever sem me consultar, e quantas
            // cabem nesta frase. A segunda para em PalavrasPorFrase-1, para sobrar
            // sempre a última palavra da frase para a aposta do aluno — senão ela
            // fecharia a frase sozinha e ele apostaria só no começo da seguinte.
            for (var passo = 0; passo < Rede.MaximoSemAposta &&
                                _geradas < Rede.PalavrasPorFrase - 1; passo++)
            {
                var vencedora = _rede.Prever(_frase);
                if (_rede.EmDuvida()) return vencedora;

                var palavra = _rede.Palavra(vencedora);
                _linhas.Add((string.Join(" ", _frase.Skip(_frase.Count - _rede.Janela)),
                             palavra, false, true));
                _frase.Add(palavra);
                _geradas++;
            }

            return _rede.Prever(_frase);
        }

        /// <summary>
        /// Registra a aposta e solta a frente de luz.
        ///
        /// SEM guarda de <c>Congelado</c> aqui, de propósito: é este o método que a
        /// explicação chama para demonstrar. O clique do aluno durante a explicação
        /// já está barrado duas vezes — pelo véu invisível do tutorial e pelo
        /// `interactable` dos botões —, então uma terceira tranca só impediria a
        /// demonstração de acontecer.
        /// </summary>
        void Apostar(int palavra)
        {
            if (_rodando || _aposta >= 0) return;

            // A luz leva quase três segundos para atravessar a malha. Sem uma
            // resposta imediata ao dedo, esses três segundos parecem o jogo ter
            // ignorado o clique — e o aluno clica de novo.
            Widgets.Pulsar(BotaoDe(palavra));

            _aposta = palavra;
            StartCoroutine(Acendendo());
        }

        /// <summary>O botão que oferece uma palavra, ou nulo se ela não está na mesa.</summary>
        RectTransform BotaoDe(int palavra)
        {
            for (var i = 0; i < _candidatas.Count && i < _botoes.Count; i++)
                if (_candidatas[i] == palavra) return (RectTransform)_botoes[i].transform;
            return null;
        }

        /// <summary>
        /// A frente de luz, em quatro tempos, e só então o veredito.
        ///
        /// A ordem é o conteúdo: as entradas acendem, a luz ATRAVESSA, o meio
        /// responde (com pedaços apagados), e a parede inteira acende de uma vez. Se
        /// o veredito viesse antes do fim, o aluno leria o placar e não olharia a
        /// malha — que é a única coisa que esta bancada tem para mostrar.
        /// </summary>
        IEnumerator Acendendo()
        {
            _rodando = true;
            Painel.Instruir("olhe a luz atravessar", Cores.Luz);
            Redesenhar();

            yield return Percorrer(0f, 1f, 0.45f, f => Iluminar(Fase.Entradas, f));
            yield return Percorrer(0f, 1f, 1.15f, f => Iluminar(Fase.Travessia, f));
            yield return Percorrer(0f, 1f, 0.40f, f => Iluminar(Fase.Meio, f));
            yield return Percorrer(0f, 1f, 0.75f, f => Iluminar(Fase.Parede, f));

            Julgar();
            _rodando = false;
        }

        /// <summary>Interpola de a até b em tempo REAL, chamando <paramref name="pintar"/>.</summary>
        static IEnumerator Percorrer(float a, float b, float segundos, System.Action<float> pintar)
        {
            var t = 0f;
            while (t < segundos)
            {
                t += Time.unscaledDeltaTime;
                pintar(Mathf.Lerp(a, b, Mathf.Clamp01(t / segundos)));
                yield return null;
            }
            pintar(b);
        }

        void Julgar()
        {
            var acertou = _aposta == _vencedora;
            if (acertou) _acertos++;

            // A cor do botão já mudava; faltava o MOVIMENTO. Cor parada, no fim de
            // uma animação de quase três segundos, se confunde com mais um estado
            // da própria animação.
            Widgets.Marcar(BotaoDe(_aposta), acertou);

            var palavra = _rede.Palavra(_vencedora);
            var chance = _rede.Chances[_vencedora];

            _linhas.Add((string.Join(" ", _frase.Skip(_frase.Count - _rede.Janela)),
                         palavra, acertou, false));

            // A palavra que ELA escolheu entra na frase, não a que estava escrita no
            // corpus. É o laço de verdade: ela escreve o que ela acha, lê de novo o
            // que acabou de escrever, e continua. A frase que se forma é dela.
            _frase.Add(palavra);
            _geradas++;
            _passes++;

            Painel.Instruir(acertou
                    ? $"acertou — “{palavra}” com {chance * 100f:0}%"
                    : $"ela disse “{palavra}” ({chance * 100f:0}%), não a sua",
                acertou ? Cores.Folha : Cores.Brasa);

            Redesenhar();

            if (_passes >= _r.apostas)
            {
                Painel.Acao("ver o resultado", Fechar);
                return;
            }
            Painel.Acao("próximo passe", NovoPasse);
        }

        void Fechar()
        {
            var placar = $"{_acertos} de {_r.apostas}";

            // A última frase que ela FECHOU. A que está pela metade na tela pode ter
            // uma palavra só dela, e "a frase é dela" com uma palavra não convence
            // ninguém.
            var frase = _prontas.Count > 0 ? _prontas[^1] : string.Join(" ", _frase);

            if (_acertos >= _r.minimo)
            {
                Resolveu($"{placar} — e a frase é dela",
                    $"“{frase}”\n\n" +
                    "Ninguém escreveu isso. Cada palavra foi a malha inteira acendendo\n" +
                    "de uma vez, e a lâmpada mais forte voltando para a entrada.\n\n" +
                    Comparacao());
                return;
            }

            Falhou($"{placar} — não deu",
                $"“{frase}”\n\n" +
                "A resposta não está em lugar nenhum em letra grande: ela está\n" +
                "espalhada em seis mil números, e só aparece quando todos somam.\n\n" +
                Comparacao(),
                contaEstrela: _acertos > 0);
        }

        /// <summary>
        /// O número que amarra a bancada 2 nesta.
        ///
        /// É a comparação que dá tamanho ao que ele acabou de ver: contar pares,
        /// que foi o trabalho de Dona Ciça, contra somar seis mil números. E os dois
        /// medidos no MESMO texto, senão a comparação não valeria nada.
        /// </summary>
        string Comparacao() =>
            $"No mesmo texto:  contando pares, como Dona Ciça, {_rede.ReguaDoBigrama * 100f:0} em 100.\n" +
            $"Esta malha, {_rede.Acerto * 100f:0} em 100 — porque olha TRÊS palavras de uma vez.";
    }
}
