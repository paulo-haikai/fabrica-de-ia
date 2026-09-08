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
    /// Bancada 5 — a malha de Iara. Corra pelo farol, e depois veja o farol por
    /// dentro.
    ///
    /// TRÊS NÍVEIS, E OS DOIS PRIMEIROS SÃO OUTRO JOGO.
    ///
    ///   · Níveis 1 e 2 — O CORREDOR: dez salas à maneira de Level Devil, cinco por
    ///     nível, sorteadas de um acervo de trinta. O aluno leva um pacote de
    ///     informação até a saída, e cada sala testa de novo se ele passa. Ver
    ///     DesafioMalha.Corredor.cs.
    ///   · Nível 3 — A MALHA, e ela não pede nada: roda inteira UMA VEZ, narrada, e
    ///     escreve UMA palavra. É o momento em que ele vê o que as dez salas eram por
    ///     dentro — peneira atrás de peneira, e só o que sobrevive a todas chega do
    ///     outro lado.
    ///
    /// UMA TRAVESSIA, E NÃO QUATRO. A malha rodava uma vez por palavra da frase, e
    /// entre uma e outra o aluno clicava "a próxima palavra". A segunda travessia não
    /// ensinava nada que a primeira já não tivesse ensinado — é a MESMA animação com
    /// outra janela — e cobrava do aluno a atenção que o fecho da aula precisava
    /// inteira. Agora a luz atravessa uma vez, e a bancada acaba.
    ///
    /// E ELA PASSA NOS DOIS CAMINHOS. Quem vence as dez salas cai na animação; quem
    /// aperta sair no meio do corredor também a vê, uma vez, antes de a moldura
    /// fechar. Trancar a animação atrás de dez salas de plataforma significava que o
    /// aluno que não dá conta do platformer saía da aula sem nunca ter visto uma rede
    /// neural — e a rede é o que esta bancada tem para ensinar; o corredor é a
    /// alegoria dela. O que a desistência custa é a ESTRELA, não a lição. Ver
    /// <see cref="Desistir"/> e <see cref="Estrelas"/>.
    ///
    /// A ORDEM É O CONTEÚDO. Correr primeiro e explicar depois é o contrário do que
    /// uma aula costuma fazer, e é de propósito: quando a animação da malha começa,
    /// o aluno já leu aquelas lâmpadas duzentas vezes e já apanhou de entrar na
    /// porta errada. A animação não é a introdução de um conceito novo; é a resposta
    /// a uma pergunta que ele já está fazendo.
    ///
    /// E o corredor carrega a alegoria que a animação sozinha não dava: o pacote
    /// atravessa sala após sala, e CADA UMA testa de novo se ele passa. É o que a
    /// malha faz com a informação, só que em milésimos de segundo e com milhares de
    /// peneiras em vez de dez.
    ///
    /// O NÍVEL DA MALHA NÃO PEDE MAIS PALPITE. Ele pedia: o aluno escolhia entre as
    /// três lâmpadas mais acesas e ganhava ponto se acertasse. Duas coisas estavam
    /// erradas nisso. A escolha disputava atenção com a única coisa que o nível tem
    /// para mostrar — quem está decidindo olha as três palavras e o placar, não os
    /// fios. E ela ensinava a coisa errada: o que a malha faz não é escolher entre
    /// três, é peneirar 318 até sobrar uma. O fecho virou demonstração narrada, que
    /// é o que ele sempre foi de fato.
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
    /// E a comparação do fim é o fecho da aula até aqui: esta malha acerta 68 de
    /// 100, e contar pares como Dona Ciça acerta 42. A diferença é olhar três
    /// palavras de uma vez em vez de uma — e os dois números saem do MESMO texto,
    /// senão a comparação não valeria nada.
    ///
    /// Nota sobre o 68: dá para treinar a mesma rede até 82, e a conferência mostrou
    /// por que isso PIORA a bancada. Rede que decorou responde com certeza quase
    /// absoluta, e a parede acende uma lâmpada só — a peneira fica invisível
    /// justamente na tela feita para mostrá-la. O ponto de parada foi escolhido pelo
    /// jogo, não pelo placar.
    /// </summary>
    public partial class DesafioMalha : DesafioEmNiveis
    {
        public override string Etapa => "e5";
        public override string Titulo => "A malha que escolhe";

        /// <summary>Dois níveis de corredor, cinco salas cada, e um de malha.</summary>
        protected override int Niveis => NiveisDeCorredor + 1;

        const int Opcoes = 3;

        Rede _rede;

        /// <summary>A semente de três palavras, mais a que a malha escreveu.</summary>
        readonly List<string> _frase = new();

        /// <summary>
        /// A travessia que aconteceu: a janela que entrou, a palavra que sobreviveu à
        /// peneira, e com que força ela acendeu. Nula até a luz chegar do outro lado.
        ///
        /// Era uma LISTA, com uma linha por travessia, de quando a malha rodava
        /// quatro vezes seguidas. Roda uma só, e lista de um item é lista de mentira.
        /// </summary>
        (string janela, string palavra, float chance)? _linha;

        List<int> _candidatas = new();

        /// <summary>
        /// A malha já foi ao ar? Verdadeiro do instante em que o cartaz da revelação
        /// abre em diante — durante a animação, e depois dela.
        ///
        /// É o que separa os dois sentidos de "sair": o primeiro pedido de saída
        /// ainda deve a lição ao aluno e chama a animação; do segundo em diante ele
        /// já viu (ou está vendo) e sair encerra de verdade.
        /// </summary>
        bool _malhaNoAr;

        /// <summary>Depois da travessia, o mostrador mostra a força de cada lâmpada.</summary>
        bool _forcaAMostra;

        Mulberry32 _sorteio;

        protected override void Preparar()
        {
            _rede = Rede.Atual;
            _sorteio = new Mulberry32((uint)Rodadas.Semente());

            // A forma da rede atravessa para a bancada 6, que vai perguntar ao aluno
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

            // Nível 3: a malha.
            AbrirAMalha(despedida: false);
        }

        /// <summary>
        /// Põe a malha na tela e abre a revelação. É o nível 3 — e também é o que o
        /// aluno vê quando pede para sair antes de chegar nele.
        /// </summary>
        /// <param name="despedida">
        /// Verdadeiro quando isto é um ATALHO: o aluno apertou sair no corredor e vai
        /// ver a animação a caminho da porta. A diferença é só de moldura — o contador
        /// de níveis some, porque contar rodadas para quem já decidiu ir embora é
        /// cobrar uma dívida que ele não vai pagar.
        /// </param>
        void AbrirAMalha(bool despedida)
        {
            _malhaNoAr = true;

            // Desligar o corredor ANTES de tocar na tela: o laço de física roda em
            // Update, e no quadro seguinte ele leria um palco meio destruído.
            _noCorredor = false;
            _travado = true;

            // No caminho normal quem limpa a área é DesafioEmNiveis, antes de chamar
            // MontarNivel; no atalho não há ninguém, e a sala inteira ainda está lá.
            // Limpar duas vezes não custa nada e faz este método bastar-se sozinho.
            foreach (Transform filho in Area) Destroy(filho.gameObject);

            if (despedida) Painel.MarcarPasso(string.Empty);
            Painel.Acao(null, null);
            // Senão o "tombos: 7" da última sala fica pendurado embaixo da revelação.
            Painel.Instruir(string.Empty);

            Semear();
            _linha = null;

            MontarTela();

            // Prepara a travessia e ABRE PELA REVELAÇÃO. PrepararPasse deixa a rede
            // rodada e a malha desenhada sem animar nada; a revelação usa esse estado
            // para narrar a filtragem devagar, por cima de um cartaz. Ver
            // DesafioMalha.Revelacao.cs.
            PrepararPasse();
            AbrirARevelacao();
        }

        /// <summary>
        /// Escolhe as três palavras com que a malha começa. Roda uma vez por visita,
        /// porque a malha atravessa uma vez por visita.
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
        }

        // ------------------------------------------------------------- travessia

        /// <summary>
        /// Roda a rede na janela atual e deixa a malha pronta, SEM animar.
        ///
        /// Separado da animação de propósito: o nível abre com um cartaz por cima, e
        /// a luz não pode estar atravessando atrás dele. Quem prepara não acende.
        ///
        /// As três lâmpadas mais acesas continuam na tela, agora em ordem de força e
        /// como mostrador. São elas que recebem os fios desenhados até a parede —
        /// apagá-las apagaria metade do desenho — e é nelas que se lê que o resultado
        /// não é um endereço, é um valor.
        /// </summary>
        void PrepararPasse()
        {
            _vencedora = _rede.Prever(_frase);
            _candidatas = _rede.MaisAcesas(Opcoes);
            _forcaAMostra = false;

            // O contador do cabeçalho não ganha mais um "travessia 2 de 4": é uma só,
            // e contar até um é anunciar um laço que não existe.
            Painel.Acao(null, null);

            RefazerSaida();
            Redesenhar();
        }

        int _vencedora;

        // A ANIMAÇÃO RÁPIDA FOI EMBORA. Havia duas: uma lenta e narrada, para a
        // primeira travessia, e uma encurtada — Travessia() e Acendendo() — para as
        // repetições, porque quem já viu a filtragem três vezes não quer esperar os
        // quatro tempos completos de novo. Sem repetição não há o que encurtar, e a
        // única que sobra é a lenta, em DesafioMalha.Revelacao.cs. Duas animações
        // para o mesmo acontecimento também eram duas narrações para manter em pé.

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

        /// <summary>
        /// A palavra que sobreviveu entra na frase, e a bancada vai para o fecho.
        ///
        /// Aqui era o LAÇO: a palavra entrava na frase, a janela seguinte já era
        /// outra pergunta, e o botão convidava à travessia seguinte. O laço saiu — ver
        /// o cabeçalho desta classe. O que ele mostrava, que a palavra escrita volta
        /// para a entrada como parte da pergunta seguinte, passou a ser DITO no cartaz
        /// de fecho, onde cabe sem cobrar mais três voltas de animação do aluno.
        /// </summary>
        void Revelar()
        {
            var palavra = _rede.Palavra(_vencedora);
            var chance = _rede.Chances[_vencedora];

            _forcaAMostra = true;
            Widgets.Marcar(PlaquetaDe(_vencedora), true);

            // A janela é gravada ANTES de a palavra entrar na frase: o que a faixa de
            // cima mostra é a pergunta que a malha respondeu, não a que viria depois.
            _linha = (string.Join(" ", _frase.Skip(_frase.Count - _rede.Janela)),
                      palavra, chance);

            _frase.Add(palavra);

            Painel.Instruir($"sobrou “{palavra}” — {chance * 100f:0}% de toda a luz da parede",
                            Cores.Folha);
            Redesenhar();
            Painel.Acao("ver o resultado", Fechar);
        }

        /// <summary>
        /// O cartaz de fecho, e ele fala de UMA palavra — porque foi uma que ela
        /// escreveu.
        ///
        /// A versão anterior citava a frase inteira e se chamava "e a frase é dela".
        /// Era verdade enquanto a malha rodava quatro vezes seguidas e escrevia as
        /// quatro palavras. Com uma travessia só, três das quatro vieram de um texto
        /// que gente escreveu — vender a frase como dela seria mentir na última tela
        /// da bancada, e o aluno que prestou atenção veria a mentira.
        ///
        /// O que precisa sobreviver ao corte é o PREÇO: aquela palavra única custou a
        /// malha inteira rodando do zero, e sobrou de 318 lâmpadas acesas ao mesmo
        /// tempo. E a comparação com a bancada 2, que é o que dá tamanho ao número.
        ///
        /// Não passa por <c>Resolveu</c>, e é de propósito. O nível deixou de cobrar
        /// palpite, e estrela de demonstração é estrela por sorte: quem assistiu não
        /// fez nada, e pagar por assistir ensinaria que a bancada se vence esperando.
        /// A estrela é do corredor, que é onde a mão dele trabalhou — ver
        /// <see cref="Estrelas"/>. Encerrar daqui também evita o pior efeito
        /// colateral do atalho de saída: por <c>Resolveu</c>, o botão do cartaz cairia
        /// em <c>Proximo</c>, que devolveria ao corredor o aluno que pediu para sair.
        /// </summary>
        void Fechar()
        {
            var semente = string.Join(" ", _frase.Take(_rede.Janela));
            var palavra = _frase[^1];
            var chance = _rede.Chances[_vencedora];

            // O rodapé continua em "a máquina por dentro", posto pela revelação. O
            // "a peça está pronta" das outras bancadas não serve aos dois caminhos:
            // para quem pediu para sair no meio do corredor, a peça não está pronta.
            Cartaz("Uma palavra, e a malha inteira por trás dela",
                $"As três primeiras palavras ela leu de um texto: “{semente}”.\n" +
                $"A quarta não estava escrita em lugar nenhum: “{palavra}”.\n\n" +
                "Essa uma palavra custou a MALHA INTEIRA rodando do zero. A janela\n" +
                "entra como números, cada fio testa de novo o que chegou, o que não\n" +
                "passa vira zero — e no fim as 318 lâmpadas da parede acenderam\n" +
                $"juntas. “{palavra}” é a que sobrou: {chance * 100f:0}% de toda a luz.\n\n" +
                "Para escrever a palavra seguinte, tudo isso acontece OUTRA VEZ, do\n" +
                "zero, com essa palavra já fazendo parte da pergunta.\n\n" +
                Comparacao(),
                "voltar ao ateliê",
                () => Encerrar(Estrelas()));
        }

        // ------------------------------------------------------------ sair e valer

        /// <summary>
        /// Sair — e a malha ainda assim passa uma vez.
        ///
        /// O primeiro pedido de saída, vindo do corredor, não fecha a moldura: ele
        /// pula para a animação. É a única coisa que esta bancada precisa ter
        /// mostrado, e um aluno que travou na sala 4 do platformer sairia da aula sem
        /// ter visto uma rede neural — que é o assunto do dia, e não o pulo duplo.
        ///
        /// Do segundo pedido em diante, encerra de verdade. Vale para o Esc apertado
        /// NO MEIO da animação: quem pede para sair duas vezes está pedindo para sair,
        /// e prender o aluno numa tela que ele já recusou seria transformar a lição em
        /// castigo. <see cref="_malhaNoAr"/> sobe no instante em que a revelação abre,
        /// então também não há como ver a animação duas vezes.
        ///
        /// Reentrada não é problema: <c>Encerrar</c> desinscreve <c>Painel.Fechou</c> e
        /// destrói a moldura, então o Esc do quadro seguinte não acha mais ninguém.
        /// </summary>
        protected override void Desistir()
        {
            if (_malhaNoAr)
            {
                Encerrar(Estrelas());
                return;
            }
            AbrirAMalha(despedida: true);
        }

        /// <summary>
        /// Três estrelas para quem atravessou as DEZ salas, e zero para todo o resto.
        ///
        /// Nada de meias estrelas por nível: o corredor é uma coisa só, e a bancada
        /// pergunta se o pacote chegou. Cinco salas de dez é o pacote no meio do
        /// caminho.
        ///
        /// Amarrado às SALAS, e não ao <c>Resolvidos</c> de DesafioEmNiveis, por dois
        /// motivos. O primeiro é que o nível da malha resolvia sozinho e inflava a
        /// conta — assistir à animação valia uma estrela. O segundo é que os cartazes
        /// do corredor distinguem passar limpo de passar tropeçando, e essa distinção
        /// é RECADO, não nota: tombar é de graça neste jogo, e a bancada avisa isso no
        /// primeiro minuto. Cobrar tombo na estrela seria desmentir o próprio aviso.
        ///
        /// A animação da malha não dá estrela por si em nenhum dos dois caminhos —
        /// estrela de demonstração é estrela por sorte. Ver <see cref="Fechar"/>.
        /// </summary>
        protected override int Estrelas() => VenceuOCorredor ? 3 : 0;

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
