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
    ///   · Níveis 1 e 2 — O CORREDOR: dez salas à maneira de Level Devil, cinco por
    ///     nível, sorteadas de um acervo de trinta. O aluno leva um pacote de
    ///     informação até a saída, e cada sala testa de novo se ele passa. Ver
    ///     DesafioMalha.Corredor.cs.
    ///   · Nível 3 — A MALHA, e ela não pede nada: roda inteira uma vez por palavra
    ///     da frase, narrada, e escreve. É o momento em que ele vê o que as dez salas
    ///     eram por dentro — peneira atrás de peneira, e só o que sobrevive a todas
    ///     chega do outro lado.
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
        public override string Etapa => "e6";
        public override string Titulo => "A malha que escolhe";

        /// <summary>Dois níveis de corredor, cinco salas cada, e um de malha.</summary>
        protected override int Niveis => NiveisDeCorredor + 1;

        /// <summary>
        /// Quantas vezes a malha roda inteira no nível do fecho: UMA POR PALAVRA da
        /// frase que ela escreve.
        ///
        /// Amarrado a <see cref="Rede.PalavrasPorFrase"/> e não a um número solto,
        /// porque é isso que ele significa: o nível dura exatamente uma frase dela,
        /// e no fim o cartaz cita a frase que acabou de nascer na tela. Solto, um
        /// dos dois um dia mudaria e o cartaz passaria a citar meia frase.
        /// </summary>
        const int TravessiasDaMalha = Rede.PalavrasPorFrase;

        const int Opcoes = 3;

        Rede _rede;

        /// <summary>A frase que a malha está escrevendo, palavra por palavra.</summary>
        readonly List<string> _frase = new();
        /// <summary>
        /// Uma linha por travessia: a janela que entrou, a palavra que sobreviveu à
        /// peneira, e com que força ela acendeu.
        /// </summary>
        readonly List<(string janela, string palavra, float chance)> _linhas = new();

        /// <summary>Palavras que ELA escreveu na frase atual, fora as três da semente.</summary>
        int _geradas;

        /// <summary>As frases que ela terminou nesta rodada.</summary>
        readonly List<string> _prontas = new();

        List<int> _candidatas = new();
        int _passes;
        bool _rodando;

        /// <summary>Depois da travessia, o mostrador mostra a força de cada lâmpada.</summary>
        bool _forcaAMostra;

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

            // Nível 3: a malha.
            //
            // O histórico é zerado aqui e as FRASES prontas não. O painel de cima
            // mostra uma linha por travessia e cabem umas poucas. As frases
            // sobrevivem porque o cartaz de fecho cita a última que ela terminou.
            _noCorredor = false;
            Semear();
            _passes = 0;
            _rodando = false;
            _linhas.Clear();

            MontarTela();

            // Prepara a primeira travessia e ABRE PELA REVELAÇÃO. PrepararPasse
            // deixa a rede rodada e a malha desenhada sem animar nada; a revelação
            // usa esse estado para narrar a filtragem devagar, por cima de um cartaz.
            // Ver DesafioMalha.Revelacao.cs.
            PrepararPasse();
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
            // A frase terminou: guarda e semeia outra. Deixar correr além disto era
            // o que produzia a frase sem sentido do cartaz final — ver o número
            // medido em Rede.PalavrasPorFrase.
            if (_geradas >= Rede.PalavrasPorFrase)
            {
                _prontas.Add(string.Join(" ", _frase));
                Semear();
            }

            _vencedora = _rede.Prever(_frase);
            _candidatas = _rede.MaisAcesas(Opcoes);
            _forcaAMostra = false;

            Painel.MarcarPasso(NivelAtual + 1, Niveis,
                               $"travessia {_passes + 1} de {TravessiasDaMalha}");
            Painel.Acao(null, null);

            RefazerSaida();
            Redesenhar();
        }

        int _vencedora;

        /// <summary>
        /// Prepara a próxima janela e solta a luz nela.
        ///
        /// A trava de <c>_rodando</c> não é zelo: a animação leva quase três
        /// segundos, e dois cliques seguidos no botão soltariam duas frentes de luz
        /// na mesma malha, cada uma escrevendo uma palavra.
        /// </summary>
        void Travessia()
        {
            if (_rodando) return;
            PrepararPasse();
            StartCoroutine(Acendendo());
        }

        /// <summary>
        /// A frente de luz, em quatro tempos.
        ///
        /// A ordem é o conteúdo: a janela entra INTEIRA, a luz é testada fio a fio,
        /// o meio resume o que sobrou (com pedaços apagados), e só então a parede
        /// acende. Se o resultado viesse antes do fim, o aluno leria a palavra e não
        /// olharia a peneira — que é a única coisa que esta bancada tem para mostrar.
        /// </summary>
        IEnumerator Acendendo()
        {
            _rodando = true;

            Painel.Instruir("a janela entra inteira, como números", Cores.Luz);
            yield return Percorrer(0f, 1f, 0.45f, f => Iluminar(Fase.Entradas, f));

            Painel.Instruir("cada fio testa de novo o que chegou", Cores.Luz);
            yield return Percorrer(0f, 1f, 1.15f, f => Iluminar(Fase.Travessia, f));

            Painel.Instruir("o que não passou na peneira vira zero", Cores.Luz);
            yield return Percorrer(0f, 1f, 0.40f, f => Iluminar(Fase.Meio, f));

            Painel.Instruir("do outro lado sobra o que atravessou tudo", Cores.Luz);
            yield return Percorrer(0f, 1f, 0.75f, f => Iluminar(Fase.Parede, f));

            _rodando = false;
            Revelar();
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

        /// <summary>
        /// A palavra que sobreviveu entra na frase, e o laço recomeça.
        ///
        /// É o laço de verdade: ela escreve o que sobrou da peneira, lê de novo o
        /// que acabou de escrever, e a janela seguinte já é outra pergunta. A frase
        /// que se forma é dela — ninguém a escreveu antes.
        /// </summary>
        void Revelar()
        {
            var palavra = _rede.Palavra(_vencedora);
            var chance = _rede.Chances[_vencedora];

            _forcaAMostra = true;
            Widgets.Marcar(PlaquetaDe(_vencedora), true);

            _linhas.Add((string.Join(" ", _frase.Skip(_frase.Count - _rede.Janela)),
                         palavra, chance));

            _frase.Add(palavra);
            _geradas++;
            _passes++;

            Painel.Instruir($"sobrou “{palavra}” — {chance * 100f:0}% de toda a luz da parede",
                            Cores.Folha);
            Redesenhar();

            if (_passes >= TravessiasDaMalha)
            {
                Painel.Acao("ver o resultado", Fechar);
                return;
            }
            Painel.Acao("a próxima palavra", Travessia);
        }

        void Fechar()
        {
            // A frase da tela, que a esta altura está inteira: são TravessiasDaMalha
            // travessias e PalavrasPorFrase palavras, e os dois são o mesmo número.
            // O `_prontas` cobre o caso de o aluno ter chegado aqui com uma frase já
            // fechada atrás — "a frase é dela" com uma palavra não convence ninguém.
            var frase = _geradas > 0 ? string.Join(" ", _frase)
                      : _prontas.Count > 0 ? _prontas[^1]
                      : string.Join(" ", _frase);

            // Sem Falhou: não há o que errar aqui. O nível deixou de cobrar palpite,
            // e cobrar estrela de quem assistiu a uma demonstração seria cobrar por
            // sorte. A estrela é do corredor, que é onde a mão dele trabalhou.
            Resolveu($"{TravessiasDaMalha} travessias — e a frase é dela",
                $"“{frase}”\n\n" +
                "Ninguém escreveu isso. Cada palavra custou a malha INTEIRA rodando\n" +
                "outra vez, do zero: a janela entra, é testada fio a fio, o que não\n" +
                "passa vira zero, e o que sobrevive volta para a entrada como parte\n" +
                "da pergunta seguinte.\n\n" +
                Comparacao());
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
