using System.Collections.Generic;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Bancada 3 — o arquivo de Mestre Aurélio.
    ///
    /// Inspiração: PAPER.IO. O aluno dirige um bonequinho pela tabela de pares,
    /// dá voltas em torno de regiões e toma cada uma para si.
    ///
    /// O QUE ELE CAÇA É O VAZIO, E ISSO É A BANCADA INTEIRA.
    ///
    /// Todas as versões anteriores pediam que ele achasse a casinha CHEIA — o par
    /// que alguém escreveu. Não funcionava, e não era questão de calibragem: a
    /// lição desta bancada é uma ausência, e procurar o raro num espaço vazio é
    /// frustrante por construção. O aluno clicava dez vezes no nada e concluía,
    /// com razão, que o jogo estava quebrado.
    ///
    /// Invertido o alvo, tudo se resolve sozinho. Achar nada é fácil porque quase
    /// tudo é nada — e é justamente a FACILIDADE que prova a lição. Ele conquista
    /// oitenta, noventa, noventa e nove por cento do arquivo, e a conta de quanto
    /// tomou é a estatística que a bancada queria ensinar, produzida por ele.
    ///
    /// POR QUE A TABELA ESVAZIA — dito pela mecânica, não por cartaz.
    ///
    /// De uma rodada para a outra muda UMA coisa só: quantas palavras o Aurélio
    /// pôs no arquivo. O texto que ele leu não muda nunca — e o vazio cresce:
    ///
    ///     rodada 1 —  20 palavras,  20×20 =    400 casinhas, 86,0% a 88,3% de vazio
    ///     rodada 2 —  60 palavras,  60×60 =  3.600 casinhas, 94,1% a 96,3% de vazio
    ///     rodada 3 — 120 palavras, 120×120 = 14.400 casinhas, 97,6% a 98,5% de vazio
    ///
    /// O aluno fica cada vez melhor no jogo sem ficar melhor em nada: o placar
    /// sobe porque o vazio cresceu. É a tabela crescendo com o QUADRADO do
    /// vocabulário enquanto os pares crescem devagar — e ele sente isso antes de
    /// alguém dizer a palavra "quadrado".
    ///
    /// AS FAIXAS ACIMA SÃO MEDIDAS, não estimadas: saem de Conferencias.Arquivo3
    /// rodando o sorteio de verdade sobre o corpus de verdade. São faixas, e não
    /// números fixos, porque O TABULEIRO É SORTEADO — cada aluno recebe um
    /// labirinto diferente, e dois colegas compartilham perto de metade das
    /// palavras. Ver Escolher().
    ///
    /// Duas armadilhas já pegas por medição, as duas invisíveis compilando:
    ///
    ///   · A primeira versão usava 12/40/120 palavras e prometia "os mesmos ~30
    ///     pares" nas três rodadas. Os pares eram 17, 183 e 510 — trinta vezes
    ///     mais, não os mesmos — e o aluno lia "os mesmos 510" logo depois de ter
    ///     visto 17 na tela anterior. O vazio entre a rodada 1 e a 2 nem sequer
    ///     crescia: 88,2% contra 88,6%.
    ///   · Abaixo de vinte palavras a densidade OSCILA em vez de cair (8 palavras
    ///     dão 18,7% de pares, 12 dão 11,8%, 16 dão 14,8%), e é por isso que a
    ///     rodada 1 começa em 20.
    /// </summary>
    public partial class DesafioArquivo : DesafioEmNiveis
    {
        public override string Etapa => "e3";
        public override string Titulo => "O arquivo que não cabe";

        protected override int Niveis => Rodadas3.Length;

        /// <summary>Quantas palavras o arquivo conhece em cada rodada.</summary>
        static readonly int[] Rodadas3 = { 20, 60, 120 };

        /// <summary>Quanto do vazio a rodada pede para ser vencida.</summary>
        static readonly float[] Meta = { 0.60f, 0.80f, 0.90f };

        /// <summary>
        /// De quão fundo no arquivo cada rodada pode sortear as palavras.
        ///
        /// A rodada 1 usa 20 palavras tiradas das 40 mais movimentadas, a 2 usa 60
        /// tiradas de 120, a 3 usa 120 tiradas de 240. É daqui que vem o labirinto
        /// ser diferente para cada aluno: dois alunos da mesma turma compartilham
        /// perto de METADE das palavras, e portanto metade dos riscos.
        ///
        /// A faixa não pode ser o vocabulário inteiro. Sortear 20 palavras entre
        /// 318 daria uma tabela sem par nenhum — nada em que esbarrar, e a rodada 1
        /// perderia a única coisa que ela tem para mostrar antes do tombo.
        /// </summary>
        static readonly int[] Faixa = { 40, 120, 240 };

        /// <summary>
        /// O vazio que cada rodada PERSEGUE ao sortear.
        ///
        /// Sortear sem alvo estraga a bancada, e não é hipótese: medido em 300
        /// alunos simulados, com faixa larga e sorteio solto o tabuleiro da rodada 1
        /// de um aluno sai MAIS vazio que o da rodada 2 dele, e a lição — a rodada
        /// seguinte fica mais fácil sem você ter melhorado em nada — desmonta na
        /// mão dele.
        ///
        /// O conserto é sortear várias vezes e ficar com a tentativa cujo vazio
        /// chega mais perto do alvo da rodada. O labirinto continua sorteado; só a
        /// DENSIDADE fica presa. Nos mesmos 300 alunos: nenhum caiu abaixo da meta,
        /// e nenhum teve vazio não-crescente.
        /// </summary>
        static readonly float[] VazioAlvo = { 0.87f, 0.94f, 0.975f };

        /// <summary>
        /// Quantos tabuleiros sortear antes de escolher.
        ///
        /// Doze fecha a faixa de vazio o bastante (rodada 1 fica entre 86,0% e
        /// 88,3% em 300 alunos) sem custar nada que se perceba: cada tentativa é
        /// uma varredura de lado×lado consultas à tabela de pares.
        /// </summary>
        const int Tentativas = 12;

        /// <summary>
        /// Segundos para atravessar a tabela de ponta a ponta, seja ela de que
        /// tamanho for.
        ///
        /// A velocidade é dada em travessias, e não em casinhas por segundo, porque
        /// a tabela é sempre desenhada no mesmo quadrado de tela: uma casinha da
        /// rodada 3 tem menos de um sexto da largura de uma da rodada 1. Um passo
        /// fixo de nove casinhas por segundo — que era o que estava aqui — deixaria
        /// o bonequinho arrastando na última rodada, onde uma volta pela borda
        /// custaria quase um minuto, e a rodada que pede 90% seria a mais lenta de
        /// todas. Assim ele anda sempre à mesma velocidade AOS OLHOS de quem joga.
        ///
        /// O número saiu de 3,5 para 6,5 depois do primeiro teste com gente: a
        /// tabela é desenhada em 430 pixels, então 3,5 segundos davam 123 pixels por
        /// segundo, e não sobrava tempo de ver o risco chegando e virar. É o único
        /// botão de velocidade desta bancada — aumentar este número deixa TODAS as
        /// rodadas mais lentas na mesma proporção.
        /// </summary>
        const float Travessia = 6.5f;

        float _passoPorSegundo;

        enum Casa : byte { Livre, Risco, Meu, Trilha }

        // SUBIR NA TELA É DIMINUIR O Y, e não aumentar.
        //
        // A tabela é lida como se lê uma tabela: a linha 0 é a primeira palavra e
        // aparece no ALTO. Quem desenha inverte (Repintar troca y por _lado-1-y),
        // e é isso que põe a base do aluno no canto de cima à esquerda, onde ele a
        // vê. Só que Vector2Int.up é (0, +1) — no sentido do eixo da tela, não no
        // da tabela. Usá-lo aqui fazia a seta para cima descer, que foi o primeiro
        // defeito que apareceu quando alguém finalmente jogou.
        //
        // Ficam nomeados para ninguém mais confundir os dois sistemas.
        static readonly Vector2Int Cima = new(0, -1);
        static readonly Vector2Int Baixo = new(0, 1);

        Casa[,] _tabela;
        int _lado;
        int _riscos;

        Vector2Int _onde;
        Vector2Int _rumo = Vector2Int.right;
        readonly List<Vector2Int> _trilha = new();
        float _passoPendente;

        int _tomadas;
        int _esbarrou;
        bool _travado;

        /// <summary>Pares que a rodada 1 pôs na tela. O fecho compara com eles.</summary>
        int _riscosDaPrimeira;

        /// <summary>
        /// O sorteio da aula. Vem de Rodadas.Semente(), como nas outras onze
        /// bancadas — esta era a única que dava o MESMO tabuleiro para todo mundo,
        /// toda vez.
        /// </summary>
        Mulberry32 _sorteio;

        // O teto: o arquivo inteiro do Aurélio, todas as palavras que ele conhece.
        // É o número que fecha a bancada, porque é o único que NÃO cresce quando o
        // vocabulário cresce.
        int _vocabularioTodo;
        int _paresTodos;

        protected override void Preparar() =>
            _sorteio = new Mulberry32((uint)Rodadas.Semente());

        protected override void MontarNivel()
        {
            _lado = Rodadas3[NivelAtual];
            _passoPorSegundo = _lado / Travessia;
            _travado = false;
            _esbarrou = 0;
            _trilha.Clear();
            _passoPendente = 0f;

            Semear();
            MontarTela();

            Painel.Rodape("setas para andar · dê a volta numa região para tomá-la");
            Painel.Instruir($"o arquivo agora tem {_lado} palavras — tome o que estiver vazio");
        }

        /// <summary>
        /// Monta a tabela da rodada: um risquinho para cada par que o corpus
        /// realmente tem, dentro do vocabulário desta rodada.
        ///
        /// Os pares saem do corpus de verdade, e é isso que faz a conta ser honesta
        /// — não são trinta marcas espalhadas a esmo para o jogo dar certo, são os
        /// pares que alguém escreveu mesmo.
        /// </summary>
        void Semear()
        {
            _tabela = new Casa[_lado, _lado];
            _riscos = 0;

            var arquivo = new Bigrama(Corpus.Frases);
            MedirOTeto(arquivo);

            var palavras = Escolher(arquivo);

            for (var i = 0; i < palavras.Count; i++)
            {
                for (var j = 0; j < palavras.Count; j++)
                {
                    if (arquivo.Risquinhos(palavras[i], palavras[j]) <= 0) continue;
                    _tabela[i, j] = Casa.Risco;
                    _riscos++;
                }
            }

            // A base: um cantinho já tomado, para o aluno ter de onde sair e para
            // onde voltar. Sem base, a primeira volta não tem como fechar.
            for (var y = 0; y < 2; y++)
                for (var x = 0; x < 2; x++)
                    if (_tabela[x, y] != Casa.Risco) _tabela[x, y] = Casa.Meu;

            _onde = new Vector2Int(1, 1);
            _rumo = Vector2Int.right;
            _tomadas = Contar(Casa.Meu);

            if (NivelAtual == 0) _riscosDaPrimeira = _riscos;
        }

        /// <summary>
        /// Conta o arquivo COMPLETO: todas as palavras que o corpus tem e todos os
        /// pares distintos entre elas.
        ///
        /// É a única conta desta bancada que não depende da rodada, e é por isso que
        /// ela existe. As três rodadas mostram os pares subirem junto com as
        /// palavras, e sozinhas dariam a impressão errada — a de que basta ler mais
        /// para o arquivo encher. O teto desmente: por mais palavras que o Aurélio
        /// ponha no fichário, os pares param aqui, porque quem os escreve é o texto,
        /// e o texto acabou.
        ///
        /// Percorre as linhas da tabela, e não as casinhas: são mil e poucos pares
        /// contra cem mil casinhas, e o resultado é o mesmo.
        /// </summary>
        void MedirOTeto(Bigrama arquivo)
        {
            if (_paresTodos > 0) return;

            foreach (var palavra in arquivo.Palavras)
            {
                if (palavra == Bigrama.Inicio || palavra == Bigrama.Fim) continue;
                _vocabularioTodo++;
                foreach (var continuacao in arquivo.Continuacoes(palavra))
                    if (continuacao.Para != Bigrama.Fim) _paresTodos++;
            }
        }

        /// <summary>
        /// Sorteia o vocabulário desta rodada, várias vezes, e fica com o tabuleiro
        /// cujo vazio chega mais perto do alvo.
        ///
        /// A lista sai ORDENADA POR MOVIMENTO, e a ordem importa tanto quanto o
        /// sorteio. Os eixos em ordem de posto deixam os riscos amontoados no canto
        /// das palavras movimentadas e o resto do tabuleiro limpo, e é o que torna
        /// a bancada jogável: medindo a maior volta única possível, com os eixos
        /// ordenados ela cerca 42-60% na rodada 1, 68-78% na 2 e 66-84% na 3 — logo
        /// abaixo de cada meta, de modo que a primeira volta grande dá quase tudo e
        /// faltam poucas para fechar.
        ///
        /// Embaralhar os eixos foi medido e reprovado: espalha os riscos por todo o
        /// tabuleiro, a maior volta da rodada 1 cai para 33-39% contra uma meta de
        /// 60%, e a bancada vira uma sequência longa de voltinhas. O canto cheio
        /// também é o retrato honesto da tabela — é ali que as palavras que todo
        /// mundo usa se encontram.
        /// </summary>
        List<string> Escolher(Bigrama arquivo)
        {
            var ordenadas = new List<string>();
            foreach (var p in arquivo.MaisMovimentadas())
            {
                if (p == Bigrama.Inicio || p == Bigrama.Fim) continue;
                ordenadas.Add(p);
            }

            var fundo = Mathf.Min(Faixa[NivelAtual], ordenadas.Count);
            var alvo = VazioAlvo[NivelAtual];
            var casas = (float)_lado * _lado;

            List<int> melhor = null;
            var melhorErro = float.MaxValue;

            for (var t = 0; t < Tentativas; t++)
            {
                var postos = Sortear(fundo, _lado);

                var riscos = 0;
                foreach (var i in postos)
                    foreach (var j in postos)
                        if (arquivo.Risquinhos(ordenadas[i], ordenadas[j]) > 0) riscos++;

                var erro = Mathf.Abs(1f - riscos / casas - alvo);
                if (erro >= melhorErro) continue;
                melhorErro = erro;
                melhor = postos;
            }

            melhor.Sort();
            var palavras = new List<string>(melhor.Count);
            foreach (var i in melhor) palavras.Add(ordenadas[i]);
            return palavras;
        }

        /// <summary>
        /// Tira <paramref name="quantos"/> postos distintos entre 0 e
        /// <paramref name="fundo"/>, por embaralhamento parcial de Fisher-Yates.
        /// </summary>
        List<int> Sortear(int fundo, int quantos)
        {
            var saco = new int[fundo];
            for (var i = 0; i < fundo; i++) saco[i] = i;

            for (var i = 0; i < quantos; i++)
            {
                var j = i + (int)(_sorteio.Proximo() * (fundo - i));
                if (j >= fundo) j = fundo - 1;
                (saco[i], saco[j]) = (saco[j], saco[i]);
            }

            var saida = new List<int>(quantos);
            for (var i = 0; i < quantos; i++) saida.Add(saco[i]);
            return saida;
        }

        int Contar(Casa que)
        {
            var n = 0;
            foreach (var c in _tabela) if (c == que) n++;
            return n;
        }

        // ------------------------------------------------------------- o passo

        void Update()
        {
            if (_travado || _tabela == null) return;

            Rumar();

            _passoPendente += Time.deltaTime * _passoPorSegundo;
            while (_passoPendente >= 1f)
            {
                _passoPendente -= 1f;
                Andar();
                if (_travado) return;
            }

            // Repinta a cada quadro em que houve passo. O bonequinho é um pixel
            // que anda; sem repintar, ele ficaria parado na base enquanto o resto
            // do jogo acontece.
            Repintar();
        }

        void Rumar()
        {
            var t = Keyboard.current;
            if (t == null) return;

            // Não deixa dar meia-volta em cima da própria trilha: seria suicídio
            // sem aviso, e o aluno não veria o que fez de errado.
            if (t.leftArrowKey.wasPressedThisFrame && _rumo != Vector2Int.right) _rumo = Vector2Int.left;
            else if (t.rightArrowKey.wasPressedThisFrame && _rumo != Vector2Int.left) _rumo = Vector2Int.right;
            else if (t.upArrowKey.wasPressedThisFrame && _rumo != Baixo) _rumo = Cima;
            else if (t.downArrowKey.wasPressedThisFrame && _rumo != Cima) _rumo = Baixo;
        }

        void Andar()
        {
            var proximo = _onde + _rumo;

            // A borda devolve em vez de matar. Morrer na parede num jogo em que a
            // parede está longe e o aluno está lendo a tela é castigo por distração,
            // e distração não é o que esta bancada mede.
            if (proximo.x < 0 || proximo.y < 0 || proximo.x >= _lado || proximo.y >= _lado)
            {
                _rumo = -_rumo;
                return;
            }

            _onde = proximo;
            var casa = _tabela[_onde.x, _onde.y];

            if (casa == Casa.Risco)
            {
                // Esbarrou num par que existe. Perde a volta que estava dando —
                // e é a única forma de perder alguma coisa aqui.
                _esbarrou++;
                Recolher();
                Painel.Instruir($"esbarrou num par que alguém escreveu — {_esbarrou} até agora",
                                Cores.Brasa);
                return;
            }

            if (casa == Casa.Trilha)
            {
                _esbarrou++;
                Recolher();
                Painel.Instruir("cruzou o próprio traço", Cores.Brasa);
                return;
            }

            if (casa == Casa.Meu)
            {
                if (_trilha.Count > 0) Fechar();
                return;
            }

            _tabela[_onde.x, _onde.y] = Casa.Trilha;
            _trilha.Add(_onde);
        }

        /// <summary>Apaga a trilha inteira: a volta não fechou.</summary>
        void Recolher()
        {
            foreach (var c in _trilha) _tabela[c.x, c.y] = Casa.Livre;
            _trilha.Clear();
            _onde = new Vector2Int(1, 1);
            _rumo = Vector2Int.right;
            Repintar();
        }

        // ------------------------------------------------------- fechar a volta

        /// <summary>
        /// Fechou o contorno: a trilha vira território e o miolo dela também.
        ///
        /// O miolo sai por eliminação, e não por varredura de dentro: inunda a
        /// tabela a partir das BORDAS por tudo o que ainda é livre; o que a
        /// inundação não alcançar está cercado, e é do aluno. É o algoritmo do
        /// paper.io e evita ter que descobrir onde é "dentro" de um contorno
        /// torto.
        /// </summary>
        void Fechar()
        {
            foreach (var c in _trilha) _tabela[c.x, c.y] = Casa.Meu;
            _trilha.Clear();

            var alcancado = new bool[_lado, _lado];
            var fila = new Queue<Vector2Int>();

            for (var i = 0; i < _lado; i++)
            {
                Molhar(new Vector2Int(i, 0), alcancado, fila);
                Molhar(new Vector2Int(i, _lado - 1), alcancado, fila);
                Molhar(new Vector2Int(0, i), alcancado, fila);
                Molhar(new Vector2Int(_lado - 1, i), alcancado, fila);
            }

            while (fila.Count > 0)
            {
                var c = fila.Dequeue();
                Molhar(c + Vector2Int.up, alcancado, fila);
                Molhar(c + Vector2Int.down, alcancado, fila);
                Molhar(c + Vector2Int.left, alcancado, fila);
                Molhar(c + Vector2Int.right, alcancado, fila);
            }

            var ganhou = 0;
            for (var y = 0; y < _lado; y++)
            {
                for (var x = 0; x < _lado; x++)
                {
                    if (alcancado[x, y] || _tabela[x, y] == Casa.Meu) continue;
                    // Cercado. Inclusive o risquinho que ficou dentro: cercar um
                    // par também é tomá-lo, e o aluno não precisa pisar nele.
                    _tabela[x, y] = Casa.Meu;
                    ganhou++;
                }
            }

            _tomadas = Contar(Casa.Meu);
            Repintar();

            if (ganhou > 0)
                Painel.Instruir($"tomou {ganhou} casinhas de uma vez", Cores.Folha);

            Conferir();
        }

        void Molhar(Vector2Int c, bool[,] alcancado, Queue<Vector2Int> fila)
        {
            if (c.x < 0 || c.y < 0 || c.x >= _lado || c.y >= _lado) return;
            if (alcancado[c.x, c.y]) return;
            if (_tabela[c.x, c.y] == Casa.Meu) return;
            alcancado[c.x, c.y] = true;
            fila.Enqueue(c);
        }

        // -------------------------------------------------------------- o fecho

        float Fracao => (float)_tomadas / (_lado * _lado);

        void Conferir()
        {
            if (Fracao < Meta[NivelAtual]) return;

            _travado = true;
            var pct = Mathf.RoundToInt(Fracao * 100f);

            var casas = _lado * _lado;

            if (NivelAtual < Niveis - 1)
            {
                Resolveu($"Você tomou {pct}% do arquivo",
                    $"Nesta tabela de {_lado}×{_lado} havia {_riscos} pares escritos.\n" +
                    $"As outras {casas - _riscos} casinhas eram vazio — e por isso\n" +
                    "foi tão fácil tomar.\n\n" +
                    "Aurélio vai pôr mais palavras no arquivo. Repare no que\n" +
                    "acontece com o tanto de vazio.");
                return;
            }

            // As três razões, calculadas e não escritas à mão: se alguém mexer nos
            // tamanhos das rodadas, o texto acompanha em vez de mentir.
            var casasIniciais = Rodadas3[0] * Rodadas3[0];
            var vezesPalavras = (float)_lado / Rodadas3[0];
            var vezesCasas = (float)casas / casasIniciais;
            var vezesPares = _riscosDaPrimeira > 0 ? (float)_riscos / _riscosDaPrimeira : 0f;

            Resolveu($"{pct}% — quase tudo era vazio",
                $"O vocabulário foi de {Rodadas3[0]} para {_lado} palavras: " +
                $"{vezesPalavras:0.#} vezes mais.\n" +
                $"As casinhas foram de {casasIniciais} para {casas}: " +
                $"{vezesCasas:0.#} vezes mais.\n" +
                $"Os pares escritos foram de {_riscosDaPrimeira} para {_riscos}: " +
                $"{vezesPares:0.#} vezes mais.\n\n" +
                "Palavra nova multiplica casinha. Não traz frase junto.\n\n" +
                $"E há um teto. O texto que o Aurélio leu tem {_paresTodos} pares\n" +
                $"diferentes, em {_vocabularioTodo} palavras — e é tudo que ele tem.\n" +
                $"A tabela completa, de {_vocabularioTodo}×{_vocabularioTodo} = " +
                $"{_vocabularioTodo * _vocabularioTodo} casinhas,\n" +
                $"teria esses mesmos {_paresTodos}.\n\n" +
                "Não é o arquivo que é grande demais — é que ele cresce ao\n" +
                "quadrado e o mundo não acompanha.");
        }
    }
}
