using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Engine;
using UnityEditor;
using UnityEngine;

namespace FabricaDeIA.Editor
{
    /// <summary>
    /// Confere o CONTEÚDO que as bancadas geram, e não o código delas.
    ///
    /// Quase todo material dos minigames é gerado a partir do corpus — correntes
    /// de dominó por caminhada no grafo, grades da tabela pela região mais
    /// movimentada, mesas de corte por família de terminação. Isso dá variedade
    /// infinita e elimina o risco de nível impossível escrito à mão, mas cria um
    /// risco novo: gerador que produz nível fácil demais, difícil demais ou sem
    /// sentido. Esse defeito não aparece no compilador nem numa captura de tela —
    /// aparece na aula, com trinta alunos travados.
    ///
    /// Então aqui os geradores rodam dezenas de vezes e a gente olha os números.
    ///
    ///     Menu: Fábrica de IA → Conferir conteúdo das bancadas
    /// </summary>
    public static partial class Conferencias
    {
        const int Amostras = 20;

        /// <summary>
        /// Confere as trinta salas do acervo do corredor da bancada 6 — todas elas,
        /// e não só as dez que uma aula joga, porque o sorteio pode pegar qualquer uma.
        ///
        /// A NATUREZA DESTA CONFERÊNCIA MUDOU, e vale dizer por quê. Enquanto as
        /// salas eram escritas aqui, ela fazia uma varredura de alcance e exigia que
        /// toda saída fosse alcançável a pé — e ganhou o pão: pegou dezesseis saídas
        /// fora do alcance do pulo de uma vez, e uma ponte que sumia antes de dar
        /// para atravessar.
        ///
        /// Agora as salas são um port do FableDevil, de Leonxlnx, que é um jogo
        /// publicado e jogado. A solubilidade vem de lá, e a mesma varredura passaria
        /// a mentir: ela não sabe que uma mola arremessa, que um portal teleporta,
        /// que uma esteira empurra nem que um elevador carrega — reprovaria meia
        /// dúzia de salas que se atravessa sem esforço. Medição que dá alarme falso
        /// é pior que medição nenhuma, porque ensina a ignorar o alarme.
        ///
        /// O que sobra é o que continua sendo nosso e continua podendo quebrar no
        /// port: cada sala precisa MONTAR sem estourar, o boneco precisa nascer em
        /// cima de alguma coisa, e nada pode estar fora do mundo. Um erro de dígito
        /// numa das centenas de coordenadas copiadas aparece aqui, e não numa aula.
        /// </summary>
        [MenuItem("Fábrica de IA/Conferir as salas da bancada 6")]
        public static void ConferirCorredor()
        {
            var relato = new List<string>();
            var problemas = new List<string>();
            Corredor6(relato, problemas);

            var texto = "CORREDOR:\n" + string.Join("\n", relato);
            if (problemas.Count == 0) Debug.Log(texto + "\nCORREDOR: OK");
            else Debug.LogError(texto + "\n\nCORREDOR: " + problemas.Count +
                                " PROBLEMA(S)\n  " + string.Join("\n  ", problemas));
        }

        static void Corredor6(List<string> relato, List<string> problemas)
        {
            var salas = Desafios.DesafioMalha.TodasAsSalas;
            var truquesAoTodo = 0;

            for (var i = 0; i < salas.Length; i++)
            {
                var sala = salas[i];

                Desafios.DesafioMalha.Ret[] solidos;
                Desafios.DesafioMalha.Armadilha[] truques;
                Desafios.DesafioMalha.Saida saida;

                try
                {
                    solidos = sala.Solidos();
                    truques = sala.Truques();
                    saida = sala.Saida();
                }
                catch (System.Exception e)
                {
                    problemas.Add($"corredor sala {i + 1} ({sala.Nome}): não monta — {e.Message}");
                    continue;
                }

                truquesAoTodo += truques.Length;

                // O boneco nasce em cima de quê? Se de nada, a sala começa com uma
                // queda que ninguém pediu.
                //
                // O chão pode vir de uma ARMADILHA, e não dos sólidos da sala: a do
                // buraco corrediço é dona do piso inteiro e o devolve como dois
                // pedaços. Ignorar isso me fez acusar uma sala perfeitamente boa de
                // começar no ar.
                var chaoTodo = new List<Desafios.DesafioMalha.Ret>(solidos);
                foreach (var t in truques)
                {
                    t.Zerar();
                    var dele = t.Solidos();
                    for (var k = 0; k < dele.Count; k++) chaoTodo.Add(dele[k]);
                }

                var apoiado = false;
                foreach (var s in chaoTodo)
                {
                    if (sala.Nasce.x + 26f < s.x || sala.Nasce.x > s.Direita) continue;
                    if (sala.Nasce.y + 32f > s.y + 4f || sala.Nasce.y + 32f < s.y - 120f) continue;
                    apoiado = true;
                    break;
                }
                if (!apoiado)
                    problemas.Add($"corredor sala {i + 1} ({sala.Nome}): o boneco nasce " +
                                  $"em {sala.Nasce.x:0}×{sala.Nasce.y:0} sem chão embaixo");

                // A saída, e todas as posições para onde ela foge, dentro do mundo.
                for (var d = 0; d < saida.Posicoes.Length; d++)
                {
                    var q = saida.Posicoes[d];
                    if (q.x >= -10f && q.x + Desafios.DesafioMalha.Saida.L <= Desafios.DesafioMalha.Larg + 10f &&
                        q.y >= -10f && q.y + Desafios.DesafioMalha.Saida.A <= Desafios.DesafioMalha.Alto + 10f)
                        continue;

                    problemas.Add($"corredor sala {i + 1} ({sala.Nome}): a posição {d + 1} " +
                                  $"da saída, em {q.x:0}×{q.y:0}, está fora da tela");
                }

                if (truques.Length == 0)
                    problemas.Add($"corredor sala {i + 1} ({sala.Nome}): nenhuma armadilha — " +
                                  "é só andar até a porta");

                relato.Add($"  sala {i + 1,2} — {sala.Nome}: {solidos.Length} sólidos, " +
                           $"{truques.Length} armadilhas, {saida.Posicoes.Length} " +
                           (saida.Posicoes.Length > 1 ? "posições de saída (ela foge)" : "posição de saída"));
            }

            relato.Add($"      {salas.Length} salas no acervo · {truquesAoTodo} armadilhas " +
                       $"ao todo · a aula joga {Desafios.DesafioMalha.SalasNaAula}, " +
                       $"{Desafios.DesafioMalha.SalasPorNivel} por nível em " +
                       $"{Desafios.DesafioMalha.NiveisDeCorredor} níveis");

            // O acervo é maior que a aula de propósito: dez salas por turma, sorteadas
            // das trinta. O que a conferência precisa garantir é que o sorteio tem de
            // onde tirar — se o acervo encolher abaixo do roteiro, o corredor repetiria
            // sala ou nasceria com uma sala nula.
            if (salas.Length < Desafios.DesafioMalha.SalasNaAula)
                problemas.Add($"corredor: {salas.Length} salas no acervo para uma aula " +
                              $"de {Desafios.DesafioMalha.SalasNaAula} — " +
                              "o sorteio não tem de onde tirar");
        }

        // ---------------------------------------------------- bancada 6, a malha

        /// <summary>
        /// Mede a MALHA — a rede que o último nível da bancada 6 abre e anima.
        ///
        /// A medição encolheu junto com a bancada, e vale dizer por quê. Enquanto o
        /// corredor tinha uma porta por palavra candidata, esta conferência simulava
        /// as vinte fases e exigia que as duas lâmpadas de cima se separassem — se
        /// empatassem, o aluno não teria o que ler. Aquele desenho foi abandonado: as
        /// portas ficavam na mesma linha do chão, o jogador esbarrava na primeira, e
        /// o jogo virava correr para a direita.
        ///
        /// Agora o corredor é um platformer sem palavra nenhuma, e a rede aparece
        /// inteira no fim. Sobrou o que sempre importou aqui, e que não aparece
        /// compilando:
        ///
        ///   · A REDE TEM QUE GANHAR DO BIGRAMA. Se ela acertasse menos que contar
        ///     pares, a frase do fecho — "a malha ganha da Dona Ciça" — seria mentira
        ///     impressa na tela.
        ///   · A FRASE NÃO PODE TRAVAR. Decodificação gulosa entra em laço com
        ///     facilidade ("a a a a"), e uma frase repetindo a mesma palavra faria o
        ///     aluno concluir que a rede é quebrada em vez de limitada.
        ///   · TEM QUE HAVER SEMENTE PARA TODO MUNDO. Com poucas frases de partida,
        ///     trinta navegadores numa sala veem a mesma.
        ///
        /// As fases do corredor têm conferência própria, que é de outra natureza —
        /// alcance, não estatística. Ver ConferirCorredor.
        /// </summary>
        static void Malha6(List<string> relato, List<string> problemas)
        {
            var rede = Rede.Atual;

            if (rede.Acerto <= rede.ReguaDoBigrama)
                problemas.Add($"malha: a rede acerta {rede.Acerto:P0} e o bigrama " +
                              $"{rede.ReguaDoBigrama:P0} — a lição do fecho fica falsa");

            var frases = Corpus.Frases.Select(f => f.Trim().Split(' '))
                                      .Where(p => p.Length > rede.Janela)
                                      .Where(p => p.Take(rede.Janela).All(rede.Conhece))
                                      .ToList();

            if (frases.Count < 40)
                problemas.Add($"malha: só {frases.Count} sementes de frase — " +
                              "trinta alunos veriam a mesma");

            var travadas = 0;
            var exemplos = new List<string>();

            for (var a = 0; a < Amostras; a++)
            {
                var frase = frases[a * 7 % frases.Count].Take(rede.Janela).ToList();

                for (var passo = 0; passo < Rede.PalavrasPorFrase; passo++)
                {
                    rede.Prever(frase);
                    frase.Add(rede.Palavra(rede.MaisAcesas(1)[0]));
                }

                var escritas = frase.Skip(rede.Janela).ToList();
                for (var i = 2; i < escritas.Count; i++)
                    if (escritas[i] == escritas[i - 1] && escritas[i] == escritas[i - 2])
                    {
                        travadas++;
                        break;
                    }

                if (exemplos.Count < 4) exemplos.Add(string.Join(" ", frase));
            }

            relato.Add($"  malha (bancada 6): rede acerta {rede.Acerto:P0} · " +
                       $"bigrama {rede.ReguaDoBigrama:P0} · {rede.Meio} neurônios · " +
                       $"{rede.Palavras} lâmpadas · {frases.Count} sementes · " +
                       $"travadas {travadas} de {Amostras}\n      " +
                       string.Join($"\n      ", exemplos));

            if (travadas * 3 > Amostras)
                problemas.Add($"malha: {travadas} de {Amostras} frases travaram repetindo palavra");
        }

        // --------------------------------------------------- bancada 2, dominó

        static void Dominos2(List<string> relato, List<string> problemas)
        {
            var arquivo = new Bigrama(Corpus.Frases);
            var correntes = 0;
            var maos = 0;
            var aberturas = new HashSet<string>();
            var exemplos = new List<string>();
            var comDesvio = 0;
            var apertadas = 0;
            var piorDosPiores = 0;
            var somaDoPiorCaso = 0f;

            for (var s = 1; s <= Amostras; s++)
            {
                var rodadas = Dominos.Sortear(arquivo, s * 7919);
                if (rodadas.Count != Dominos.PorAula)
                    problemas.Add("dominó: semente " + s + " gerou " + rodadas.Count + " rodadas");

                foreach (var r in rodadas)
                {
                    correntes++;
                    maos += r.Mao.Count;
                    aberturas.Add(r.Inicio);
                    if (exemplos.Count < 4) exemplos.Add(r.CaminhoExemplo);

                    // A mão precisa conter o caminho inteiro, senão é insolúvel.
                    if (r.Mao.Count < r.MaxPecas)
                        problemas.Add("dominó: mão de " + r.Mao.Count +
                                      " para " + r.MaxPecas + " peças");
                    // Engodo é o que faz pensar: mão do tamanho exato do caminho
                    // não tem decisão nenhuma.
                    if (r.Mao.Count <= r.MaxPecas)
                        problemas.Add("dominó: sem engodos em " + r.CaminhoExemplo);

                    // O caminho certo TEM que ser texto real, senão a vitória é
                    // inalcançável — foi a inversão que fez a bancada aceitar
                    // "amanhã a aula de matemática ficou" como resposta correta.
                    var certo = r.CaminhoExemplo.Split(' ');
                    if (!Corpus.EhTrechoReal(certo))
                    {
                        problemas.Add("dominó: o caminho certo não é frase real — " +
                                      r.CaminhoExemplo);
                        continue;
                    }

                    // E TEM que existir caminho que encaixa e não é frase, senão
                    // não há nada para o aluno errar e a lição não aparece.
                    if (TemDesvioQueEncaixa(r)) comDesvio++;

                    // O mesmo orçamento que a bancada usa. Duplicado aqui de propósito:
                    // um teste que LÊ a constante do jogo não pode acusar a constante de
                    // estar errada, e é exatamente isso que eu quero medir.
                    var orcamento = r.MaxPecas * 2 + 2;
                    var piorCaso = ElosNoPiorCaso(r);

                    somaDoPiorCaso += piorCaso;
                    piorDosPiores = Mathf.Max(piorDosPiores, piorCaso);
                    if (piorCaso > orcamento) apertadas++;
                }
            }

            if (apertadas > 0)
                problemas.Add($"dominó: {apertadas} de {correntes} correntes gastam mais elos " +
                              "no pior caso do que o orçamento permite — dá para ficar preso " +
                              "sem elo, e isso não é dificuldade, é bloqueio");

            if (comDesvio < correntes / 2)
                problemas.Add("dominó: só " + comDesvio + " de " + correntes +
                              " correntes têm desvio tentador — a bancada perde o ensino");

            relato.Add("  dominó (bancada 2): " + correntes + " correntes · mão média " +
                       ((float)maos / Mathf.Max(1, correntes)).ToString("0.0") + " peças · " +
                       aberturas.Count + " aberturas diferentes · " +
                       comDesvio + " com desvio tentador\n" +
                       "      elos no pior caso: média " +
                       (somaDoPiorCaso / Mathf.Max(1, correntes)).ToString("0.0") +
                       ", máximo " + piorDosPiores +
                       " · orçamento vai de 10 a 14 · apertadas: " + apertadas + "\n      " +
                       string.Join("\n      ", exemplos));
        }

        /// <summary>
        /// O orçamento de elos dá para ganhar a rodada?
        ///
        /// Esta medição existe porque eu acabei de PÔR uma condição de derrota nesta
        /// bancada, e condição de derrota mal calibrada não deixa o jogo difícil —
        /// deixa impossível. E impossível numa aula de 90 minutos é pior que fácil.
        ///
        /// A conta é o pior caso de um aluno sistemático: em cada ponto da corrente
        /// ele experimenta todas as peças que encaixam ali antes de achar a certa,
        /// desfazendo cada erro. Isso gasta (encaixes possíveis − 1) elos perdidos por
        /// passo, mais os elos do caminho certo. Se isso couber no orçamento, ninguém
        /// fica preso por falta de elo, só por falta de leitura.
        /// </summary>
        static int ElosNoPiorCaso(Rodada2 rodada)
        {
            var certo = rodada.CaminhoExemplo.Split(' ');
            var gasto = 0;

            for (var passo = 0; passo + 1 < certo.Length; passo++)
            {
                var ponta = certo[passo];
                var encaixam = rodada.Mao.Count(p => p.Esquerda == ponta);

                // Os desvios que ele testa e desfaz, mais o elo certo.
                gasto += Mathf.Max(1, encaixam);
            }
            return gasto;
        }

        /// <summary>
        /// Existe alguma peça que encaixa no início e leva a corrente para fora do
        /// texto real?
        ///
        /// É a medida de se a rodada tem armadilha. Uma rodada em que só o caminho
        /// certo encaixa é um corredor, não um quebra-cabeça — e é justamente o
        /// desvio que encaixa e não é frase que carrega a lição da bancada.
        ///
        /// Olha só o primeiro passo de propósito: se já ali existe uma bifurcação
        /// que sai do texto real, a armadilha está armada. Percorrer a árvore
        /// inteira daria a mesma resposta em muito mais tempo.
        /// </summary>
        static bool TemDesvioQueEncaixa(Rodada2 rodada)
        {
            var certo = rodada.CaminhoExemplo.Split(' ');

            foreach (var peca in rodada.Mao)
            {
                if (peca.Esquerda != rodada.Inicio) continue;
                if (peca.Direita == certo[1]) continue;
                if (!Corpus.EhTrechoReal(new[] { rodada.Inicio, peca.Direita })) continue;

                // Encaixa (o par existe), e o par existir não garante que a frase
                // continue existindo: é esse o engodo que ensina.
                return true;
            }
            return false;
        }

        // -------------------------------------------------- bancada 3, arquivo

        /// <summary>
        /// Roda o SORTEIO da bancada 3 muitas vezes e confere que o vazio se
        /// comporta em todas as aulas, não só na média.
        ///
        /// Desde que o tabuleiro passou a ser sorteado, a média não basta: quem
        /// joga é um aluno, com uma semente só, e a lição acontece dentro das três
        /// rodadas DELE. Três coisas precisam valer em CADA aula, e as três já
        /// falharam alguma vez:
        ///
        ///   · O VAZIO TEM QUE CRESCER, aula por aula. É a lição inteira, e é o que
        ///     o aluno sente quando a rodada seguinte fica mais fácil sem ele ter
        ///     melhorado em nada. Com 12/40/120 palavras fixas o vazio ia de 88,2%
        ///     para 88,6% da rodada 1 para a 2 — não crescia. Com sorteio de faixa
        ///     larga e sem alvo, voltava a não crescer, agora só para alguns alunos,
        ///     que é pior porque não aparece testando uma vez.
        ///   · A META TEM QUE CABER NO VAZIO. Cercar tudo é impossível, então meta
        ///     colada no vazio disponível faz rodada invencível. Foi o defeito que
        ///     derrubou a bancada 11, onde dois terços das rodadas não tinham
        ///     solução.
        ///   · O FECHO TEM QUE SER VERDADE. A tela final compara os pares da última
        ///     rodada com os da primeira, e o aluno viu os dois números. A versão
        ///     anterior dizia "os mesmos 510 pares" depois de ter mostrado 17.
        ///
        /// Espelha DesafioArquivo.Escolher(). Se um lado mudar sozinho, a medição
        /// passa a atestar um tabuleiro que ninguém joga — já aconteceu uma vez.
        /// </summary>
        static void Arquivo3(List<string> relato, List<string> problemas)
        {
            var rodadas = new[] { 20, 60, 120 };
            var metas = new[] { 0.60f, 0.80f, 0.90f };
            var faixas = new[] { 40, 120, 240 };
            var alvos = new[] { 0.87f, 0.94f, 0.975f };
            const int tentativas = 12;
            const int aulas = 60;

            var arquivo = new Bigrama(Corpus.Frases);
            var ordenadas = arquivo.MaisMovimentadas()
                                   .Where(w => w != Bigrama.Inicio && w != Bigrama.Fim)
                                   .ToList();

            var menorVazio = new float[rodadas.Length];
            var maiorVazio = new float[rodadas.Length];
            var somaVazio = new float[rodadas.Length];
            var menorPares = new int[rodadas.Length];
            var maiorPares = new int[rodadas.Length];
            for (var r = 0; r < rodadas.Length; r++)
            {
                menorVazio[r] = 1f;
                menorPares[r] = int.MaxValue;
            }

            var semCrescer = 0;
            var semFolga = 0;
            var fechoFalso = 0;
            var iguais = 0;

            for (var a = 0; a < aulas; a++)
            {
                var sorteio = new Mulberry32((uint)(a * 2654435761u + 17u));
                var vazioAnterior = -1f;
                var paresDaPrimeira = 0;
                var primeiraLista = new List<int>();
                var terceiraLista = new List<int>();

                for (var r = 0; r < rodadas.Length; r++)
                {
                    var lado = rodadas[r];
                    var fundo = Mathf.Min(faixas[r], ordenadas.Count);
                    var casas = (float)lado * lado;

                    List<int> melhor = null;
                    var melhorPares = 0;
                    var melhorErro = float.MaxValue;

                    for (var t = 0; t < tentativas; t++)
                    {
                        var postos = Punhado(sorteio, fundo, lado);
                        var pares = 0;
                        foreach (var i in postos)
                            foreach (var j in postos)
                                if (arquivo.Risquinhos(ordenadas[i], ordenadas[j]) > 0) pares++;

                        var erro = Mathf.Abs(1f - pares / casas - alvos[r]);
                        if (erro >= melhorErro) continue;
                        melhorErro = erro;
                        melhor = postos;
                        melhorPares = pares;
                    }

                    var vazio = 1f - melhorPares / casas;
                    somaVazio[r] += vazio;
                    if (vazio < menorVazio[r]) menorVazio[r] = vazio;
                    if (vazio > maiorVazio[r]) maiorVazio[r] = vazio;
                    if (melhorPares < menorPares[r]) menorPares[r] = melhorPares;
                    if (melhorPares > maiorPares[r]) maiorPares[r] = melhorPares;

                    if (vazio < metas[r] + 0.05f) semFolga++;
                    if (r > 0 && vazio <= vazioAnterior) semCrescer++;
                    vazioAnterior = vazio;

                    if (r == 0) { paresDaPrimeira = melhorPares; primeiraLista = melhor; }
                    if (r == rodadas.Length - 1)
                    {
                        terceiraLista = melhor;
                        // O fecho mostra "de X para Y pares". Se Y não for maior que
                        // X, a frase "palavra nova multiplica casinha" fica sem
                        // sustentação na tela do aluno.
                        if (melhorPares <= paresDaPrimeira) fechoFalso++;
                    }
                }

                // Quanto o labirinto muda de aluno para aluno: comparo esta aula com
                // a anterior na rodada 1. Se as listas fossem sempre iguais, o
                // sorteio não estaria sorteando nada.
                if (a > 0 && primeiraLista.Count > 0 && terceiraLista.Count > 0 &&
                    primeiraLista.SequenceEqual(_ultimaPrimeira)) iguais++;
                _ultimaPrimeira = primeiraLista;
            }

            var linhas = new List<string>();
            for (var r = 0; r < rodadas.Length; r++)
                linhas.Add("      rodada " + (r + 1) + ": " + rodadas[r] + "×" + rodadas[r] +
                           " sorteadas entre as " + faixas[r] + " mais movimentadas · pares " +
                           menorPares[r] + " a " + maiorPares[r] + " · vazio " +
                           (100f * menorVazio[r]).ToString("0.0") + "% a " +
                           (100f * maiorVazio[r]).ToString("0.0") + "% (médio " +
                           (100f * somaVazio[r] / aulas).ToString("0.0") + "%) · meta " +
                           (100f * metas[r]).ToString("0") + "%");

            var vocabulario = 0;
            var paresTodos = 0;
            foreach (var palavra in arquivo.Palavras)
            {
                if (palavra == Bigrama.Inicio || palavra == Bigrama.Fim) continue;
                vocabulario++;
                foreach (var c in arquivo.Continuacoes(palavra))
                    if (c.Para != Bigrama.Fim) paresTodos++;
            }
            linhas.Add("      teto do arquivo: " + vocabulario + " palavras, " +
                       (long)vocabulario * vocabulario + " casinhas, " + paresTodos + " pares (" +
                       (100f * paresTodos / ((float)vocabulario * vocabulario)).ToString("0.00") + "%)");

            relato.Add("  arquivo (bancada 3): conquistar o vazio · " + aulas + " aulas sorteadas\n" +
                       string.Join("\n", linhas));

            if (semCrescer > 0)
                problemas.Add("arquivo: em " + semCrescer + " de " + aulas +
                              " aulas o vazio não cresceu de uma rodada para a seguinte — " +
                              "nessas, a rodada do meio não tem o que ensinar");
            if (semFolga > 0)
                problemas.Add("arquivo: " + semFolga + " rodadas sorteadas ficaram sem folga " +
                              "de 5 pontos sobre a meta — o aluno não tem margem para errar");
            if (fechoFalso > 0)
                problemas.Add("arquivo: em " + fechoFalso + " aulas a última rodada tem MENOS " +
                              "pares que a primeira, e o fecho promete o contrário");
            if (iguais > aulas / 10)
                problemas.Add("arquivo: " + iguais + " aulas sortearam o mesmo vocabulário da " +
                              "anterior na rodada 1 — o labirinto não está variando");
            if (paresTodos <= maiorPares[^1])
                problemas.Add("arquivo: o fecho diz que o arquivo inteiro tem " + paresTodos +
                              " pares, mas uma rodada sorteada chegou a " + maiorPares[^1] +
                              " — não sobra teto nenhum para a frase final");
        }

        static List<int> _ultimaPrimeira = new();

        /// <summary>Espelha DesafioArquivo.Sortear.</summary>
        static List<int> Punhado(Mulberry32 sorteio, int fundo, int quantos)
        {
            var saco = new int[fundo];
            for (var i = 0; i < fundo; i++) saco[i] = i;

            for (var i = 0; i < quantos; i++)
            {
                var j = i + (int)(sorteio.Proximo() * (fundo - i));
                if (j >= fundo) j = fundo - 1;
                (saco[i], saco[j]) = (saco[j], saco[i]);
            }

            var saida = new List<int>(quantos);
            for (var i = 0; i < quantos; i++) saida.Add(saco[i]);
            saida.Sort();
            return saida;
        }

        // ---------------------------------------------------- bancada 4, corte

        static void Corte4(List<string> relato, List<string> problemas)
        {
            var rodadas = 0;
            var economia = 0f;
            var exemplos = new List<string>();

            for (var s = 1; s <= Amostras; s++)
            {
                foreach (var r in Cortes.Sortear(s * 15485863))
                {
                    rodadas++;
                    var letras = r.Palavras.Sum(p => p.Length);
                    economia += letras - r.Meta;

                    if (exemplos.Count < 3)
                        exemplos.Add(string.Join(" ", r.Palavras) + " → " + letras +
                                     " letras, meta " + r.Meta + " em " + r.Fusoes + " emendas");

                    // Meta igual ao número de letras significa que nenhuma emenda
                    // rende nada: as palavras sorteadas não compartilham estrutura.
                    if (r.Meta >= letras)
                        problemas.Add("corte: meta " + r.Meta + " não economiza nada (" +
                                      string.Join(" ", r.Palavras) + ")");
                    if (r.Palavras.Length < 3)
                        problemas.Add("corte: rodada com menos de três palavras");
                }
            }

            relato.Add("  corte (bancada 4): " + rodadas + " rodadas · economia média " +
                       (economia / Mathf.Max(1, rodadas)).ToString("0.0") + " fichas\n      " +
                       string.Join("\n      ", exemplos));
        }

        // ----------------------------------------------------- bancada 5, mapa

        /// <summary>
        /// Mostra os grupos que a coocorrência produz, para eu poder LER se são
        /// humanamente agrupáveis.
        ///
        /// Nenhum número decide isso. Um grupo pode ter coesão alta e ainda assim
        /// ser impossível de adivinhar, se as quatro palavras não tiverem nada em
        /// comum aos olhos de uma pessoa. O que a conferência faz é medir a
        /// separação (que é objetiva) e imprimir os grupos (que é para o olho).
        /// </summary>
        static void Mapa5(List<string> relato, List<string> problemas)
        {
            var mapa = new Vizinhancas(Corpus.Frases);
            var exemplos = new List<string>();
            var coesao = 0f;
            var grupos = 0;

            for (var s = 1; s <= 6; s++)
            {
                var rodadas = Mapas.Sortear(mapa, s * 27644437);
                if (rodadas.Count < Mapas.PorAula)
                    problemas.Add("mapa: semente " + s + " gerou só " + rodadas.Count + " rodadas");

                foreach (var trinca in rodadas)
                {
                    foreach (var g in trinca)
                    {
                        grupos++;
                        coesao += g.Coesao;
                        if (g.Palavras.Count != Mapas.PorGrupo)
                            problemas.Add("mapa: grupo com " + g.Palavras.Count + " palavras");
                    }
                    if (exemplos.Count < 6)
                        exemplos.Add(string.Join("   |   ",
                            trinca.Select(g => string.Join(" ", g.Palavras))));
                }
            }

            relato.Add("  mapa (bancada 5): " + grupos + " grupos · coesão média " +
                       (coesao / Mathf.Max(1, grupos)).ToString("0.00") + "\n      " +
                       string.Join("\n      ", exemplos));

            if (coesao / Mathf.Max(1, grupos) < 0.15f)
                problemas.Add("mapa: grupos com coesão baixa — não há resposta certa a achar");
        }

        // --------------------------------------------------- bancada 8, treino

        /// <summary>
        /// Varre a faixa de força do treino e diz que pedaço dela vence.
        ///
        /// É a conferência mais importante desta leva. A bancada 8 depende de
        /// haver força que funcione (senão o nível é impossível) e de NÃO
        /// funcionar quase toda força (senão não há decisão).
        ///
        /// A varredura passou a ser contínua junto com a bancada: enquanto o
        /// aluno escolhia entre cinco rótulos, bastava testar os cinco. Agora
        /// ele carrega uma barra, e o que precisa ser medido é a LARGURA da
        /// faixa que vence — uma faixa estreita demais é um nível que só passa
        /// por sorte do dedo, e isso a checagem antiga não teria como ver.
        /// </summary>
        static void Treino8(List<string> relato, List<string> problemas)
        {
            var rodadas = new[]
            {
                (nome: "rodada 1", botoes: 4, passos: 8, desequilibrio: 1f),
                (nome: "rodada 2", botoes: 8, passos: 8, desequilibrio: 2.5f),
                (nome: "rodada 3", botoes: 8, passos: 12, desequilibrio: 9f)
            };

            // Os mesmos números de DesafioTreino: se um lado mudar, a checagem
            // deixa de checar o jogo que existe.
            const float forcaMinima = 0.03f;
            const float forcaMaxima = 2.4f;
            const float meta = 0.01f;
            const int amostras = 40;

            foreach (var r in rodadas)
            {
                var vencedoras = new List<float>();
                var linhas = new List<string>();

                for (var a = 0; a < amostras; a++)
                {
                    var fracao = Mathf.Lerp(forcaMinima, forcaMaxima, a / (float)(amostras - 1));

                    // Média de várias sementes: uma só poderia ser sortuda.
                    var vitorias = 0;
                    for (var s = 1; s <= 12; s++)
                    {
                        var m = new Mostrador(r.botoes, s * 7013, r.desequilibrio);
                        var curva = m.Treinar(fracao, r.passos);
                        var razao = curva[^1] / Mathf.Max(1e-9f, curva[0]);
                        if (float.IsNaN(razao) || float.IsInfinity(razao)) razao = 1e6f;
                        if (razao <= meta) vitorias++;
                    }

                    // Vence "de verdade" só se vencer na maioria das sementes —
                    // uma força que passa em 3 de 12 é armadilha, não solução.
                    if (vitorias >= 7) vencedoras.Add(fracao);
                }

                var largura = vencedoras.Count / (float)amostras;
                linhas.Add(vencedoras.Count == 0
                    ? "nenhuma força vence"
                    : "vence de " + vencedoras[0].ToString("0.00") + " a " +
                      vencedoras[^1].ToString("0.00") + " (" +
                      (largura * 100f).ToString("0") + "% da barra)");

                relato.Add("  treino (bancada 8) " + r.nome + ", " + r.botoes + " botões, " +
                           r.passos + " passos\n      " + string.Join("\n      ", linhas));

                if (vencedoras.Count == 0)
                    problemas.Add("treino: " + r.nome + " não tem força que vença — impossível");
                else if (largura < 0.06f)
                    problemas.Add("treino: " + r.nome + " vence numa faixa estreita demais (" +
                                  (largura * 100f).ToString("0") + "%) — passa por sorte do dedo");
                else if (largura > 0.85f)
                    problemas.Add("treino: " + r.nome + " vence com quase toda força — sem decisão");
            }
        }

        // ----------------------------------------------- bancada 10, holofotes

        /// <summary>
        /// Confere que cada rodada dos holofotes tem solução dentro do orçamento
        /// de lâmpadas — o gerador já filtra por isso, e aqui a gente cobra.
        /// </summary>
        static void Holofotes10(List<string> relato, List<string> problemas)
        {
            var companhias = new Companhias(Corpus.Frases);
            var geradas = 0;
            var exemplos = new List<string>();

            for (var s = 1; s <= 10; s++)
            {
                var rodadas = Holofotes.Sortear(companhias, s * 49999);
                if (rodadas.Count == 0)
                {
                    problemas.Add("holofotes: semente " + s + " não gerou rodada nenhuma");
                    continue;
                }

                foreach (var r in rodadas)
                {
                    geradas++;
                    var solucao = Holofotes.Solucao(companhias, r);
                    if (solucao == null)
                    {
                        problemas.Add("holofotes: rodada sem solução — " +
                                      string.Join(" ", r.Antes));
                        continue;
                    }
                    if (exemplos.Count < 4)
                        exemplos.Add(string.Join(" ", r.Antes) + " ___ = " + r.Resposta +
                                     "  ·  acender: " +
                                     string.Join(" + ", solucao.Select(i => r.Antes[i])));
                }
            }

            relato.Add("  holofotes (bancada 10): " + geradas + " rodadas, todas com solução\n      " +
                       string.Join("\n      ", exemplos));
        }
    }
}
