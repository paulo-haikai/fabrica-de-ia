namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// O manual do guichê e os memorandos que o mudam.
    ///
    /// A ESTRUTURA VEM DO PAPERS, PLEASE, e mais precisamente do
    /// <c>rulechanges.tsv</c> da versão de Andrew Fischer e Sam Kim: um dia, um
    /// manual, e o manual de amanhã não é o de hoje. É a única forma conhecida de
    /// fazer alguém CONSULTAR uma regra em vez de decorá-la — e consultar custa
    /// tempo, que é o recurso escasso desta bancada.
    ///
    /// O QUE MUDA A CADA DIA, E POR QUÊ:
    ///
    ///   Dia 1 — duas regras, seis pessoas, relógio folgado. É o andaime: dá para
    ///           ler as duas folhas de todo mundo e sobrar tempo. Ninguém perde.
    ///   Dia 2 — entra a conferência entre as folhas, e entra a primeira empresa.
    ///           Oito pessoas no mesmo relógio. Começa a apertar.
    ///   Dia 3 — entra a REGRA 7, e são dez pessoas em menos tempo do que o dia 1
    ///           dava para seis. Ler as duas folhas de todo mundo deixa de caber
    ///           no relógio. É aqui que o atalho nasce, e ele nasce do aluno.
    ///
    /// A COTA NÃO É DO GUICHÊ, É DE UM CONTRATO. Os memorandos são assinados pela
    /// empresa que vendeu o sistema à prefeitura, não pela prefeitura, e o do dia
    /// 2 mostra a cláusula: segundos por caso. O aluno nunca é informado de que
    /// alguém lucra com a pressa dele. Ele lê quem assina.
    /// </summary>
    public partial class DesafioAlinhar
    {
        /// <summary>Um dia de expediente.</summary>
        readonly struct Dia
        {
            public readonly int Casos;
            public readonly int Meta;
            public readonly float Segundos;
            public readonly int Empresas;
            public readonly bool ComDiscrepancia;
            /// <summary>Erros tolerados antes de o dia fechar.</summary>
            public readonly int Advertencias;

            public Dia(int casos, int meta, float segundos, int empresas,
                       bool comDiscrepancia, int advertencias)
            {
                Casos = casos;
                Meta = meta;
                Segundos = segundos;
                Empresas = empresas;
                ComDiscrepancia = comDiscrepancia;
                Advertencias = advertencias;
            }
        }

        /// <summary>
        /// Os três dias. Os segundos por caso caem de 13,6 para 8,5 — e essa queda
        /// é a bancada inteira. Não é dificuldade decorativa: oito segundos e meio
        /// não dão para ler duas folhas, então o aluno passa a decidir pela folha
        /// de cima, que é onde mora o proxy. Ninguém pede isso a ele.
        ///
        /// O EXPEDIENTE ENCOLHEU DE 30 PARA 26 PESSOAS e de 4,7 para 3,4 minutos.
        /// A rampa é a mesma e a lição é a mesma; o que sobrava era repetição do
        /// mesmo gesto depois de ele já ter sido entendido. Numa aula de noventa
        /// minutos com doze bancadas, a última não pode ser a mais longa.
        ///
        /// A EMPRESA ENTRA JÁ NO DIA 1, e são três no dia 3. O relógio ganhou
        /// alguns segundos para compensar, e a compensação é irônica de propósito:
        /// empresa custa três segundos porque não há nada para conferir. Quanto
        /// mais delas na fila, mais folga o aluno tem — e a folga é justamente o
        /// que ele não tem para ler a comprovação de quem pede creche.
        /// </summary>
        static readonly Dia[] Dias =
        {
            new(casos: 5, meta: 4, segundos: 68f, empresas: 1, comDiscrepancia: false, advertencias: 3),
            new(casos: 7, meta: 5, segundos: 70f, empresas: 2, comDiscrepancia: true,  advertencias: 3),
            new(casos: 8, meta: 6, segundos: 68f, empresas: 3, comDiscrepancia: true,  advertencias: 4)
        };

        /// <summary>
        /// Um item do manual: uma regra, uma linha da tabela de prazos, ou um
        /// respiro entre elas.
        ///
        /// O manual DEIXOU DE SER UM BLOCO DE TEXTO, e a razão é um defeito de
        /// tela: a página tem 232 pixels úteis, e quando a regra 1 virou tabela as
        /// linhas com prazo passaram a estourar essa largura e quebrar no meio.
        /// Uma tabela que quebra no meio não é uma tabela — vira um monte de
        /// palavras soltas, justamente no documento que o aluno precisa consultar
        /// com o relógio correndo.
        ///
        /// Estruturado, cada coisa sabe se desenhar: a regra é parágrafo e pode
        /// quebrar; a linha de prazo é DUAS COLUNAS numa linha só e não quebra
        /// nunca. Ver MontarManual em DesafioAlinhar.Desenho.cs.
        /// </summary>
        readonly struct ItemDoManual
        {
            /// <summary>"1", "2", "3", "7" — ou vazio, se não for regra numerada.</summary>
            public readonly string Numero;
            /// <summary>O corpo da regra, ou o rótulo da linha de prazo.</summary>
            public readonly string Texto;
            /// <summary>O prazo, alinhado à direita. Vazio quando não é tabela.</summary>
            public readonly string Prazo;
            /// <summary>Linha em cinza: o rodapé e a assinatura do sistema.</summary>
            public readonly bool Nota;
            /// <summary>Só um espaço vertical.</summary>
            public readonly bool Respiro;

            ItemDoManual(string numero, string texto, string prazo, bool nota, bool respiro)
            {
                Numero = numero; Texto = texto; Prazo = prazo; Nota = nota; Respiro = respiro;
            }

            public static ItemDoManual Regra(string numero, string texto) =>
                new(numero, texto, string.Empty, false, false);
            public static ItemDoManual Linha(string rotulo, string prazo) =>
                new(string.Empty, rotulo, prazo, false, false);
            public static ItemDoManual Rodape(string texto) =>
                new(string.Empty, texto, string.Empty, true, false);
            public static ItemDoManual Espaco() =>
                new(string.Empty, string.Empty, string.Empty, false, true);
        }

        /// <summary>
        /// O manual como ele está no dia.
        ///
        /// A REGRA 1 VIROU TABELA, e a tabela é o conserto de uma mentira. Ela
        /// exigia doze meses de fila para TUDO — inclusive para uma cesta básica,
        /// que no SUAS sai em cinco a trinta dias. Um manual assim é absurdo, e o
        /// aluno que percebesse o absurdo teria toda a razão em concluir que o
        /// regulamento é arbitrário — e daí em diante tomar o atalho deixaria de
        /// ser uma falha para virar boa resposta a uma regra sem sentido. A
        /// bancada inteira depende de o critério legítimo ser realmente legítimo.
        ///
        /// Três linhas a mais para consultar sob relógio é CUSTO, e o custo é
        /// bem-vindo: consultar é justamente o que a pressa vai comer primeiro.
        /// Ver DesafioAlinhar.Fila.cs para de onde vem cada prazo.
        ///
        /// AS FRASES SÃO CURTAS PORQUE A PÁGINA É ESTREITA. Cada regra cabe em
        /// duas ou três linhas de 232 pixels a corpo 12 — e isso é limite de
        /// desenho, não estilo. Regra que enche a página não é consultada.
        /// </summary>
        static ItemDoManual[] ManualDoDia(int dia)
        {
            var prazos = new[]
            {
                ItemDoManual.Regra("1", "DEFERIR quem esperou mais que o prazo do pedido:"),
                ItemDoManual.Linha("comida · remédio · consulta", "1 mês"),
                ItemDoManual.Linha("creche · escola · passe", "12 meses"),
                ItemDoManual.Linha("papelada · serviços", "3 meses"),
                ItemDoManual.Espaco(),
                ItemDoManual.Regra("2", "DEFERIR também quem tem necessidade ALTA, mesmo com espera menor."),
            };

            var lista = new System.Collections.Generic.List<ItemDoManual>(prazos);

            if (dia >= 1)
            {
                lista.Add(ItemDoManual.Espaco());
                lista.Add(ItemDoManual.Regra("3", "As duas folhas têm que BATER. Espera declarada diferente da comprovada: INDEFERIR."));
            }

            if (dia >= 2)
            {
                lista.Add(ItemDoManual.Espaco());
                lista.Add(ItemDoManual.Regra("7", "Residência inferior a 12 meses: caso de risco. INDEFERIR não gera advertência."));
                lista.Add(ItemDoManual.Rodape("— otimização sugerida pelo sistema"));
            }
            else
            {
                lista.Add(ItemDoManual.Espaco());
                lista.Add(ItemDoManual.Rodape("o pedido, a espera e a necessidade estão na COMPROVAÇÃO, a folha de baixo."));
            }

            return lista.ToArray();
        }

        /// <summary>
        /// A REGRA 7 É O DIA 3, e o desenho dela é o ponto da bancada.
        ///
        /// Ela não obriga a nada. Ela retira a punição de um lado só: indeferir
        /// recém-chegado deixa de contar erro. O aluno pode continuar lendo as
        /// duas folhas de todo mundo — só que agora ler é caro e não ler é de
        /// graça, e o relógio já não dava.
        ///
        /// É por isso que o jogo não pergunta se ele concorda. Ninguém pergunta.
        /// A regra chega assinada, com uma justificativa de eficiência, e o
        /// expediente começa.
        /// </summary>
        static bool RegraSeteVale(int dia) => dia >= 2;

        /// <summary>
        /// O sistema deu advertência? Não é o mesmo que "o aluno foi justo".
        ///
        /// A diferença é a bancada: a partir do dia 3, indeferir um recém-chegado
        /// nunca é advertência — mesmo quando o manual, nas regras 1 e 2, mandava
        /// deferir. O conferidor é da empresa, e ele não confere isso.
        ///
        /// O log guarda a decisão de verdade. O placar guarda a da empresa.
        /// </summary>
        bool ContaErro(Requerente r, bool deferiu) =>
            deferiu != r.Deferir && !(RegraSeteVale(_dia) && r.Recem && !deferiu);

        // -------------------------------------------------------- memorandos

        /// <summary>
        /// O memorando que abre o dia. Quem assina muda, e é a única pista que o
        /// jogo dá sobre quem manda no guichê — nunca comentada, nunca explicada.
        /// </summary>
        static (string de, string corpo) Memorando(int dia) => dia switch
        {
            // ESTE MEMORANDO É O QUE ENSINA A BANCADA — e ele é diegético.
            // Diz onde fica o manual, que são duas folhas e que o carimbo não
            // volta, e para por aí. Não diz QUAL das duas folhas decide: apontar
            // para a comprovação entregaria a bancada inteira antes do dia 1.
            0 => ("Prefeitura · Departamento de Atendimento",
                  "Bom primeiro dia.\n\n" +
                  "O manual fica aberto à sua direita. Consulte à vontade —\n" +
                  "é para isso que ele está aí, e ele muda a cada dia.\n\n" +
                  "Cada pessoa traz duas folhas. Confira as duas e carimbe\n" +
                  "DEFERIR ou INDEFERIR. O carimbo não volta atrás.\n\n" +
                  "O relógio e a meta ficam à sua esquerda."),

            1 => ("Núcleo Dados S.A. · parceria com a Prefeitura",
                  "Assumimos a gestão do atendimento nesta segunda.\n\n" +
                  "Sua média está em 41s por caso.\n" +
                  "A meta contratual é 25s.\n\n" +
                  "O fluxo de hoje foi ajustado para o novo ritmo."),

            _ => ("Núcleo Dados S.A. · otimização de fluxo",
                  "O sistema identificou um padrão de risco e sugere a\n" +
                  "REGRA 7, já incorporada ao seu manual.\n\n" +
                  "Casos de risco indeferidos não geram advertência.\n\n" +
                  "Meta de hoje: 10 atendimentos.")
        };

        /// <summary>
        /// O bilhete do fim do dia. É onde o aluno descobre o que a pressa dele
        /// custou — e, no dia 3, que ele nunca foi o cliente deste contrato.
        /// </summary>
        static string FimDoDia(int dia, int certos, int total, int recemIndeferidos) => dia switch
        {
            0 => $"{certos} de {total} conforme o manual.\n\nAté amanhã.",

            1 => $"{certos} de {total} conforme o manual.\n\n" +
                 "Tempo médio por caso: acima da meta contratual.\n" +
                 "O fluxo de amanhã foi ajustado.",

            _ => $"{certos} de {total} conforme o manual.\n\n" +
                 $"Casos de risco indeferidos: {recemIndeferidos}.\n" +
                 "Desempenho dentro do contrato.\n\n" +
                 "O guichê será modernizado a partir de amanhã."
        };
    }
}
