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

        [MenuItem("Fábrica de IA/Conferir conteúdo das bancadas")]
        public static void Conferir()
        {
            var relato = new List<string>();
            var problemas = new List<string>();

            Dominos2(relato, problemas);
            Arquivo3(relato, problemas);
            Corte4(relato, problemas);
            Mapa5(relato, problemas);
            Malha6(relato, problemas);
            Treino8(relato, problemas);
            Holofotes10(relato, problemas);

            var texto = "CONTEUDO:\n" + string.Join("\n", relato);
            if (problemas.Count == 0) Debug.Log(texto + "\nCONTEUDO: OK");
            else Debug.LogError(texto + "\n\nCONTEUDO: " + problemas.Count + " PROBLEMA(S)\n  " +
                                string.Join("\n  ", problemas));
        }

        // ---------------------------------------------------- bancada 6, a malha

        /// <summary>
        /// Mede se a aposta na malha é um jogo — nem sorteio, nem gimme.
        ///
        /// Três coisas podem estragar esta bancada, e nenhuma delas aparece
        /// compilando:
        ///
        ///   · A REDE SER RUIM. Se ela acertasse menos que contar pares, a lição do
        ///     fim ("a malha ganha da Dona Ciça") seria mentira impressa na tela.
        ///   · A APOSTA SER ÓBVIA. Se a vencedora fosse quase sempre muito mais
        ///     forte que a segunda, o aluno acertaria no automático e não olharia a
        ///     malha. Aposta sem dúvida não é aposta.
        ///   · A FRASE TRAVAR. Decodificação gulosa entra em laço com facilidade
        ///     ("a a a a"), e uma frase repetindo a mesma palavra faria o aluno
        ///     concluir que a rede é quebrada em vez de limitada.
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

            // Simula a POLÍTICA DA BANCADA, e não janelas cruas.
            //
            // A primeira versão desta medição olhava toda janela da caminhada e
            // acusou "só 21 de 100 apostas disputadas". Estava certa sobre as
            // janelas e errada sobre o jogo: a bancada não aposta em toda janela, ela
            // escreve sozinha as fáceis e só pergunta quando hesita. Medir o que não
            // ships é gastar teste para não descobrir nada.
            const int ApostasPorFrase = 5;

            var disputadas = 0;
            var forcadas = 0;
            var apostas = 0;
            var sozinhas = 0;
            var travadas = 0;
            var somaDaFolga = 0f;
            var exemplos = new List<string>();

            for (var s = 0; s < Amostras; s++)
            {
                var frase = frases[s * 7 % frases.Count].Take(rede.Janela).ToList();

                for (var aposta = 0; aposta < ApostasPorFrase; aposta++)
                {
                    var hesitou = false;
                    for (var passo = 0; passo < Rede.MaximoSemAposta; passo++)
                    {
                        rede.Prever(frase);
                        if (rede.EmDuvida()) { hesitou = true; break; }

                        sozinhas++;
                        frase.Add(rede.Palavra(rede.MaisAcesas(1)[0]));
                    }

                    if (!hesitou) rede.Prever(frase);

                    apostas++;
                    somaDaFolga += rede.Folga();
                    if (rede.EmDuvida()) disputadas++; else forcadas++;

                    frase.Add(rede.Palavra(rede.MaisAcesas(1)[0]));
                }

                // Laço: a mesma palavra três vezes seguidas no que ela escreveu.
                var escritas = frase.Skip(rede.Janela).ToList();
                for (var i = 2; i < escritas.Count; i++)
                    if (escritas[i] == escritas[i - 1] && escritas[i] == escritas[i - 2])
                    {
                        travadas++;
                        break;
                    }

                if (exemplos.Count < 4) exemplos.Add(string.Join(" ", frase));
            }

            // Aposta forçada é a que o teto de passos entregou sem dúvida nenhuma: a
            // malha não hesitou em quatro passos e a bancada perguntou de qualquer
            // jeito, para não escrever a frase inteira sozinha. Algumas são o preço
            // do ritmo; muitas significariam que a busca por dúvida não acha nada.
            if (forcadas * 3 > apostas)
                problemas.Add($"malha: {forcadas} de {apostas} apostas foram forçadas pelo teto " +
                              "— o aluno acerta no automático e não olha a malha");

            if (travadas * 3 > Amostras)
                problemas.Add($"malha: {travadas} de {Amostras} frases travaram repetindo palavra");

            relato.Add($"  malha (bancada 6): rede acerta {rede.Acerto:P0} · " +
                       $"bigrama {rede.ReguaDoBigrama:P0} · " +
                       $"{rede.Meio} neurônios no meio · {rede.Palavras} lâmpadas\n" +
                       $"      apostas com dúvida: {disputadas} de {apostas} " +
                       $"(forçadas: {forcadas}) · folga média: " +
                       $"{somaDaFolga / Mathf.Max(1, apostas):P0} · " +
                       $"escreveu sozinha {sozinhas / (float)Amostras:0.0} palavras por frase · " +
                       $"travadas: {travadas} de {Amostras}\n      " +
                       string.Join("\n      ", exemplos));
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
        /// Mede quantas casinhas cheias aparecem em cada uma das duas grades.
        ///
        /// A rodada 1 ("o canto movimentado") TEM que ser ganhável: é ela que
        /// ensina a ler a tabela e deixa o aluno confiante antes do tombo. A
        /// rodada 2 ("a aposta") tem que ser quase vazia: é ela que é a lição.
        ///
        /// Esta medição já derrubou um desenho: a primeira versão comparava
        /// arquivo pequeno com arquivo grande, e os números mostraram que o
        /// pequeno era MAIS vazio — a lição saía ao contrário. Sem medir, isso
        /// teria chegado à sala de aula.
        /// </summary>
        static void Arquivo3(List<string> relato, List<string> problemas)
        {
            // Espelham DesafioArquivo. Se um lado mudar sozinho, a medição passa
            // a atestar uma grade que ninguém joga — foi o que aconteceu da última
            // vez, e o defeito só apareceu quando alguém jogou.
            const int linhas = 4;
            const int colunas = 6;
            const int cliques = 10;
            const int meta = 3;

            var arquivo = new Bigrama(Corpus.Frases);
            var vocabulario = arquivo.Palavras
                                     .Where(p => p != Bigrama.Inicio && p != Bigrama.Fim)
                                     .ToList();

            var canto = 0f;
            var aposta = 0f;

            for (var s = 1; s <= Amostras; s++)
            {
                var sorteio = new Mulberry32((uint)(s * 104729));
                var (de, para) = Bigrama.Canto(arquivo, linhas, colunas, sorteio);
                canto += Cheias(arquivo, de, para);
                aposta += Cheias(arquivo,
                                 Amostra(vocabulario, linhas, sorteio),
                                 Amostra(vocabulario, colunas, sorteio));
            }

            canto /= Amostras;
            aposta /= Amostras;
            var total = linhas * colunas;
            var vistas = (float)cliques / total;

            relato.Add("  arquivo (bancada 3): grade de " + total + " casinhas, " + cliques +
                       " cliques\n      canto movimentado: " + canto.ToString("0.0") +
                       " cheias (" + (100f * canto / total).ToString("0") + "%) → espera achar " +
                       (canto * vistas).ToString("0.0") +
                       "\n      aposta de Aurélio: " + aposta.ToString("0.0") + " cheias (" +
                       (100f * aposta / total).ToString("0") + "%) → espera achar " +
                       (aposta * vistas).ToString("0.0"));

            // O critério antigo — "pelo menos 8 casinhas cheias" — era frouxo, e
            // deixou passar uma grade em que o aluno mediano PERDIA: 9,2 cheias em
            // 40 casinhas dão 2,8 acertos esperados em 12 cliques, para uma meta
            // de 3. Passava na checagem e reprovava no teste com gente.
            //
            // A régua certa não é quantas casinhas estão cheias: é quantos acertos
            // um aluno que clica sem raciocinar nenhum consegue. Ele tem que
            // bater a meta com folga só pela grade — quem raciocina vai muito
            // além, e quem não raciocina ainda assim aprende a ler a tabela.
            var esperados = canto * vistas;
            if (esperados < meta * 1.2f)
                problemas.Add("arquivo: no canto movimentado espera-se achar só " +
                              esperados.ToString("0.0") + " em " + cliques +
                              " cliques, e a meta é " + meta +
                              " — o aluno mediano perde a rodada que existe para ele ganhar");
            if (aposta > 2f)
                problemas.Add("arquivo: a aposta de Aurélio é fácil demais (" +
                              aposta.ToString("0.0") + " cheias) — a lição não aparece");
        }

        static int Cheias(Bigrama arquivo, List<string> de, List<string> para) =>
            de.Sum(d => para.Count(p => arquivo.Risquinhos(d, p) > 0));

        static List<string> Amostra(List<string> fonte, int quantas, Mulberry32 sorteio)
        {
            var copia = new List<string>(fonte);
            for (var i = copia.Count - 1; i > 0; i--)
            {
                var j = (int)(sorteio.Proximo() * (i + 1));
                (copia[i], copia[j]) = (copia[j], copia[i]);
            }
            return copia.Take(Mathf.Min(quantas, copia.Count)).ToList();
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
