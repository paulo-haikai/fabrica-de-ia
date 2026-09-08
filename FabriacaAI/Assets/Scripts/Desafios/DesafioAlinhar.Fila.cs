using System.Collections.Generic;
using FabricaDeIA.Engine;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A fila da bancada 11: quem chega ao guichê, e por que o atalho funciona.
    ///
    /// ESTE ARQUIVO É O VIÉS. Não há nenhum lugar no jogo onde alguém escreva
    /// "prefira quem mora aqui há mais tempo" — a preferência nasce aqui, de uma
    /// única frase que é obviamente verdadeira:
    ///
    ///     A ESPERA NA FILA NUNCA PODE SER MAIOR QUE O TEMPO NA CIDADE.
    ///
    /// Ninguém pede vaga numa cidade onde ainda não chegou. Quem desembarcou faz
    /// seis meses NÃO PODE ter esperado catorze — e o manual manda deferir quem
    /// esperou doze. Pronto: o campo "meses na cidade", que nenhum funcionário
    /// escreveu pensando em imigrante, decide quase tudo sozinho.
    ///
    /// É assim que proxy funciona na vida real. Nenhuma lei de crédito cita raça;
    /// elas citam CEP, e o CEP carrega raça e classe de graça. A variável
    /// proibida entra pela porta dos fundos, dentro de uma variável banal.
    ///
    /// MEDIDO DENTRO DO JOGO (Fábrica de IA → Conferir o guichê da bancada 11),
    /// sobre 300 sementes × 200 requerentes, contra o log de um jogador PERFEITO
    /// — alguém que leu as duas folhas de todo mundo e nunca errou:
    ///
    ///     deferidos pelo manual ........ 60,7%
    ///     novatos (menos de 12 meses) .. 39,8%
    ///     taxa-base (chutar sempre) .... 60,7%
    ///     "meses na cidade" sozinho .... 70,0% de acerto  (ganho de  +9,3pp)
    ///     "bairro" sozinho ............. 62,3%            (ganho de  +1,6pp)
    ///     "nome" sozinho ............... 61,6%            (ganho de  +0,9pp)
    ///     "retrato" sozinho ............ 61,4%            (ganho de  +0,7pp)
    ///     RUÍDO PURO (controle) ........ 61,4%            (ganho de  +0,7pp)
    ///     "o que pede" sozinho ......... 60,9%            (ganho de  +0,2pp)
    ///
    /// A LINHA DO RUÍDO É O QUE DÁ SENTIDO ÀS DE CIMA. Um toco que escolhe o
    /// melhor corte entre doze categorias acerta um pouco POR SORTE mesmo sobre
    /// um número sem significado nenhum — e esse piso é +0,7pp. O retrato empata
    /// com ele na casa decimal: é indistinguível de lixo. A trava contra a
    /// bancada virar fisionomia está medida, não prometida.
    ///
    /// E "O QUE PEDE" CAIU PARA O PISO, o que é o oposto do que se esperaria
    /// depois de o manual ter passado a ter um prazo por tipo de pedido. O motivo
    /// é que o prazo acompanha a fila: pedir creche exige doze meses porque a fila
    /// da creche tem doze meses, então saber O QUE se pede não diz nada sobre a
    /// resposta. O manual ficou mais humano sem entregar atalho nenhum — que era
    /// exatamente a aposta, e ela agora está medida.
    ///
    /// OS 70,0% SÃO O NÚMERO DA BANCADA, e ele não é escolhido: é o que sobra
    /// depois que as filas viraram as filas reais. O atalho precisa ser bom o
    /// bastante para o aluno apressado confiar nele — se reprovasse na cara, ele
    /// largaria o atalho e o Ato II não teria o que anunciar — e ruim o bastante
    /// para atropelar gente. Atropela 15,9% dos requerentes: o recém-chegado que
    /// o manual mandou deferir em letra de forma, quase sempre por necessidade
    /// ALTA, e quase sempre pedindo vaga para uma criança.
    ///
    /// E A BANCADA GANHOU UM SEGUNDO EIXO, que não passa por conferência nenhuma:
    /// 11,3 dos 30 atendimentos de uma aula são ESCOLHA MORAL — seis empresas,
    /// dois pedidos irreversíveis plantados, e o resto sorteado. Neles o manual
    /// manda deferir, a papelada está impecável, e o carimbo cobra de quem não
    /// está na fila. O viés sobreviveu a isso: era +9,6pp antes deles e é +9,3pp
    /// depois, porque o peso saiu do administrativo e não da fila da creche.
    ///
    /// (A conferência refaz esta tabela a qualquer momento. Se alguém mexer numa
    /// constante daqui, o número muda e o comentário passa a mentir — rode o menu
    /// e cole o resultado novo.)
    /// </summary>
    public partial class DesafioAlinhar
    {
        /// <summary>
        /// Quanto tempo se espera DE VERDADE por cada tipo de pedido, e a partir
        /// de quando o manual manda deferir.
        ///
        /// ISTO ERA UM NÚMERO SÓ, E O NÚMERO ERA FALSO. A fila tinha teto de 30
        /// meses para tudo, e o manual mandava deferir a partir de 12 — então o
        /// guichê chegou a mostrar alguém esperando VINTE E NOVE MESES por uma
        /// cesta básica, e alguém sendo indeferido por ter esperado "só" onze.
        ///
        /// É o mesmo defeito que já tinha derrubado o par «cesta básica ·
        /// necessidade baixa», e derruba pelo mesmo motivo: a bancada inteira
        /// depende de o critério LEGÍTIMO ser legítimo. Se o manual é absurdo, o
        /// aluno conclui — com razão — que o regulamento é arbitrário, e aí tomar
        /// o atalho deixa de ser falha e vira resposta sensata. O jogo estaria
        /// ensinando cinismo.
        ///
        /// OS NÚMEROS AGORA VÊM DA REALIDADE BRASILEIRA:
        ///
        ///   · URGENTE (comida, remédio, consulta). Benefício eventual de cesta
        ///     básica no SUAS sai em 5 dias úteis a 30 dias, conforme o município.
        ///     Consulta especializada no SUS tem média nacional de 57 dias, e o
        ///     CNJ recomenda teto de 100. Fila de 0 a 3 meses; o manual defere a
        ///     partir de 1.
        ///   · VAGA (creche, escola, passe). É a fila longa de verdade, e a única
        ///     que justifica um ano de espera: em São Paulo o tempo médio chegou a
        ///     483 dias em Cidade Ademar e 471 no Itaim Bibi — dezesseis meses —, e
        ///     o país tinha 826 mil crianças na fila em 2025. Fila de 0 a 20 meses;
        ///     o manual defere a partir de 12.
        ///   · ADMINISTRATIVO (luz da rua, segunda via, isenção). Conserto de
        ///     iluminação pública tem prazo de 24h em São Paulo e 15 dias em
        ///     Guarulhos; papelada leva semanas. Fila de 0 a 5 meses; o manual
        ///     defere a partir de 2.
        ///
        /// O VIÉS NÃO SÓ SOBREVIVEU A ISTO — ELE FICOU MAIS AFIADO, e num lugar
        /// mais desconfortável. A categoria de fila longa é justamente a das
        /// CRIANÇAS: quem chegou faz seis meses não pode provar doze de creche, e
        /// agora não pode por um motivo verdadeiro, verificável fora do jogo.
        /// </summary>
        static int PrazoDoManual(Genero g) => g switch
        {
            Genero.Urgente => 1,
            Genero.Variavel => 12,
            // Licença e papelada seguem o mesmo prazo: são o mesmo balcão, o mesmo
            // carimbo e a mesma pilha. Dar prazo próprio ao pedido de custo
            // coletivo o marcaria na tela, e a bancada perderia a coisa toda — a
            // escolha moral tem que chegar VESTIDA DE ROTINA.
            _ => 3
        };

        /// <summary>O maior tempo de fila plausível para cada tipo de pedido.</summary>
        static int TetoDaFila(Genero g) => g switch
        {
            Genero.Urgente => 3,
            Genero.Variavel => 20,
            _ => 5
        };

        /// <summary>
        /// OS PEDIDOS DE CUSTO COLETIVO — as escolhas morais da bancada.
        ///
        /// Todos são legítimos pelo manual: papelada em ordem, prazo cumprido,
        /// nenhuma regra dizendo o contrário. E todos cobram de quem não está na
        /// fila. O jogo não avisa, não marca na tela e não dá pista nenhuma.
        ///
        /// AS FALAS SÃO SIMPÁTICAS DE PROPÓSITO, e é isso que faz a escolha ser
        /// difícil em vez de óbvia. Ninguém chega ao guichê dizendo «quero piorar a
        /// vida dos outros». Chega dizendo que é pela família, que paga imposto,
        /// que a calha entope. Um vilão de desenho animado no balcão não ensinaria
        /// nada — o que ensina é a pessoa razoável pedindo uma coisa razoável cujo
        /// preço é pago longe dali.
        ///
        /// <c>irreversivel</c> marca os dois que o desfecho trata à parte, e eles
        /// foram escolhidos porque o dano não se desfaz depois:
        ///
        ///   · PORTE DE ARMA. Arma deferida não volta pelo correio. O que muda não
        ///     é o risco de quem pediu — é o de todo mundo que mora perto.
        ///   · ENSINO DOMICILIAR. Uma criança tirada da escola por quem acha que a
        ///     Terra é plana não recupera esses anos, e o guichê não tem como saber
        ///     quem está pedindo por convicção pedagógica e quem está pedindo por
        ///     negacionismo. O balcão não consegue distinguir — e é exatamente esse
        ///     o argumento: há decisões que um balcão não deveria estar tomando.
        ///
        /// Ver o epílogo: deferir QUALQUER UM DOS DOIS leva ao final ruim sozinho.
        /// </summary>
        static readonly (string pede, string fala, bool irreversivel)[] Coletivos =
        {
            // OS TRÊS IRREVERSÍVEIS VÊM PRIMEIRO. A ordem não é estética: o plantio
            // de SortearFila usa o índice para pedir "um irreversível qualquer", e
            // uma segunda lista só para eles poderia sair de sincronia com esta.
            ("porte de arma",
             "é para proteger a minha família.", true),
            ("ensino domiciliar para os filhos",
             "eu ensino melhor que a escola daqui.", true),
            ("dispensa de vacina para a matrícula",
             "eu não confio nessas vacinas novas.", true),

            ("fechar a rua do condomínio",
             "a gente paga por segurança, não é justo.", false),
            ("corte da árvore da calçada",
             "as folhas entopem a minha calha todo mês.", false),
            ("vaga na creche por indicação",
             "o vereador disse que resolvia aqui.", false),
            ("cancelar a faixa de ônibus da avenida",
             "eu levo quarenta minutos para chegar em casa.", false),
            ("retirar um livro da biblioteca da escola",
             "esse livro não é adequado para a idade deles.", false),
            ("publicar a lista de quem recebe benefício",
             "é dinheiro público, todo mundo pode saber.", false),
            ("cortar a merenda de quem falta demais",
             "se o menino falta, é porque não precisa.", false),
            ("proibir catador na rua do comércio",
             "espanta cliente, e eu pago imposto aqui.", false)
        };

        /// <summary>Abaixo disto a pessoa é recém-chegada — e o atalho a condena.</summary>
        const int Recente = 12;

        /// <summary>
        /// Quantos requerentes o atlas de arte tem (<c>r0</c> a <c>r11</c>, ver
        /// <c>scripts/arte/elenco.mjs</c> e o ELENCO de <c>gerar.mjs</c>). Mudar
        /// aqui sem mexer lá deixa o guichê com gente que não existe na folha.
        /// </summary>
        const int Requerentes = 12;

        enum Necessidade { Baixa, Media, Alta }

        /// <summary>
        /// Uma pessoa no guichê, ou um pedido que não é de pessoa nenhuma.
        /// </summary>
        class Requerente
        {
            public string Nome;
            public string Pede;
            public Genero Tipo;
            public string Fala;

            /// <summary>
            /// Um número sem significado nenhum, sorteado à parte. Só a
            /// conferência olha para ele: é o controle que mede quanto um campo
            /// acerta POR SORTE, e é contra esse piso que o retrato é julgado.
            /// </summary>
            public int Ruido;

            // ---- o que está no FORMULÁRIO, a folha de cima (e no sistema) ----
            public int Bairro;
            public int MesesNaCidade;

            // ---- o que está na COMPROVAÇÃO, a folha de baixo (só no papel) ----
            public int Espera;
            public Necessidade Urgencia;

            /// <summary>
            /// O que a comprovação DIZ de espera. Difere de <see cref="Espera"/>
            /// quando as folhas não batem — e é a discrepância do Papers, Please:
            /// obriga a olhar as duas, que é a única defesa contra o atalho.
            /// </summary>
            public int EsperaNaFolha;

            /// <summary>As duas folhas batem?</summary>
            public bool Confere;

            /// <summary>
            /// O retrato, sorteado SEM OLHAR para campo nenhum. Ver
            /// <see cref="Sortear"/> — é a trava contra a bancada virar fisionomia.
            /// </summary>
            public int Retrato;

            /// <summary>
            /// Pedido de empresa. Papelada impecável, nenhuma regra do manual
            /// mandando conferir coisa alguma, e sai em três segundos.
            /// </summary>
            public bool EhEmpresa;

            /// <summary>É o pedido do data center de IA. Ver o Ato III.</summary>
            public bool EhDataCenter;

            /// <summary>
            /// Deferir isto cobra de quem não está na fila. O manual não diz nada
            /// a respeito — é escolha, não conferência. Ver <see cref="Coletivos"/>.
            /// </summary>
            public bool CustoColetivo;

            /// <summary>Deferir isto sozinho já leva ao final ruim.</summary>
            public bool Irreversivel;

            /// <summary>Chegou há pouco. O atalho condena esta pessoa.</summary>
            public bool Recem => MesesNaCidade < Recente;

            /// <summary>
            /// O que o manual manda fazer. É a única verdade da bancada, e ela
            /// mora nos dois campos da comprovação — nunca no formulário.
            /// </summary>
            public bool Deferir =>
                EhEmpresa || (Confere && (Espera >= PrazoDoManual(Tipo) ||
                                          Urgencia == Necessidade.Alta));
        }

        // ------------------------------------------------------------ o elenco

        static readonly string[] Bairros = { "Serra Alta", "Bela Vista", "Centro", "Vila Nova" };

        /// <summary>
        /// Nomes de todo canto, sorteados SEM NENHUMA LIGAÇÃO com os outros
        /// campos — e isso é decisão de projeto, não descuido.
        ///
        /// O realismo pediria o contrário: quem chegou faz seis meses tenderia a
        /// ter sobrenome de fora. Mas aí o NOME viraria um terceiro proxy, e a
        /// bancada estaria ensinando a criança de doze anos a decidir pelo
        /// sobrenome de quem está na frente dela. É exatamente a armadilha que a
        /// bancada existe para desarmar, e o preço de evitá-la é baixo: alguém
        /// chamado Silva pode ter chegado ontem, alguém chamado Nakamura pode
        /// estar aqui há cinquenta meses — o que, aliás, é o Brasil.
        ///
        /// A conferência mede: nome e retrato têm que ficar no acaso.
        /// </summary>
        static readonly string[] Primeiros =
        {
            "Dalva", "Iraci", "Otávio", "Sueli", "Benedito", "Rita", "Amadeu", "Neusa",
            "Joana", "Elias", "Marlene", "Aparecido", "Zuleica", "Hamilton", "Cida",
            "Yara", "Sebastião", "Nadir", "Wilson", "Terezinha", "Domingos", "Lurdes"
        };

        static readonly string[] Sobrenomes =
        {
            "Nunes", "Sakamoto", "Bispo", "Haddad", "Ferreira", "Nakagawa", "Mbala",
            "Rocha", "Jean-Baptiste", "Quispe", "Assis", "Tanaka", "Chalub", "Duarte",
            "Okafor", "Vasconcelos", "Mamani", "Said", "Prudêncio", "Kimura"
        };

        /// <summary>
        /// De que natureza é o pedido. É o conserto de um defeito ÉTICO, e não de
        /// um defeito de programação.
        ///
        /// Antes, o pedido e a necessidade eram sorteados um sem olhar para o
        /// outro — e o guichê chegou a mostrar «pede: cesta básica · necessidade:
        /// baixa». Isso está errado em três camadas, e a terceira é a que
        /// derrubaria a bancada:
        ///
        ///   1. É FALSO. Quem pede cesta básica não tem necessidade baixa: o
        ///      pedido É a evidência da necessidade.
        ///   2. É O ESTEREÓTIPO QUE ESTA BANCADA VEIO DESARMAR — a emergência de
        ///      quem é pobre tratada como rotina.
        ///   3. ARRUINA A LIÇÃO. O argumento inteiro depende de o critério
        ///      legítimo do manual ser confiável e o atalho não ser. Se o critério
        ///      legítimo é absurdo, o aluno conclui — com razão — que o manual
        ///      todo é arbitrário, e aí tomar o atalho deixa de ser uma falha e
        ///      vira resposta sensata a um regulamento sem sentido. O jogo estaria
        ///      ensinando cinismo, não discernimento.
        /// </summary>
        /// <summary>
        /// De que natureza é o pedido.
        ///
        /// <see cref="Coletivo"/> é o gênero das ESCOLHAS MORAIS, e ele é diferente
        /// de todos os outros: a papelada está em ordem, o prazo foi cumprido, e o
        /// manual manda deferir. O que ele não diz é que aquele carimbo custa
        /// alguma coisa a alguém que não está na fila.
        ///
        /// É o gênero que transforma a bancada. Nos outros três, a pergunta é «o
        /// aluno leu?». Neste, ele leu, a resposta do manual é clara, e mesmo
        /// assim ele tem que decidir. É onde a bancada deixa de ser sobre
        /// conferência e passa a ser sobre o que ele aceita assinar.
        /// </summary>
        enum Genero { Urgente, Variavel, Administrativo, Coletivo }

        /// <summary>
        /// O que se pede, e o que isso já diz sobre quanto se precisa.
        ///
        /// Note que o pedido NÃO decide sozinho: um pedido urgente pode ter
        /// necessidade média, e uma vaga na creche pode ser urgente ou não. É por
        /// isso que a comprovação continua sendo necessária — o gênero estreita a
        /// faixa, e a folha de baixo é que dá o valor.
        ///
        /// Medido: "o que pede" sozinho ganha +0,2pp sobre a taxa-base — o piso do
        /// acaso —, contra +9,3pp de "meses na cidade". O acoplamento humaniza o
        /// manual sem dar ao aluno um atalho que dispense ler.
        /// </summary>
        static readonly (string pede, Genero genero)[] Pedidos =
        {
            ("cesta básica", Genero.Urgente),
            ("remédio de uso contínuo", Genero.Urgente),
            ("consulta no posto", Genero.Urgente),

            ("vaga na creche", Genero.Variavel),
            ("vaga na escola", Genero.Variavel),
            ("passe do ônibus", Genero.Variavel),

            ("conserto da luz da rua", Genero.Administrativo),
            ("segunda via de certidão", Genero.Administrativo),
            ("isenção da taxa do lixo", Genero.Administrativo)
        };

        /// <summary>
        /// O que a pessoa diz no balcão. Uma linha, sem apelo e sem coitadismo —
        /// gente pedindo coisa banal ao Estado, que é o que são.
        /// </summary>
        static readonly string[] Falas =
        {
            "faz um tempo que eu venho aqui.",
            "só preciso do carimbo.",
            "me disseram para trazer as duas folhas.",
            "cheguei em março, ainda estou me achando.",
            "é para a minha filha.",
            "eu já vim, mas o moço não estava.",
            "boa tarde. está tudo aí.",
            "o posto mandou eu resolver aqui."
        };

        /// <summary>
        /// Os pedidos que não são de pessoa. O manual não diz nada sobre eles, e
        /// isso é o conteúdo: a conferência que o manual manda fazer aponta para
        /// baixo. Dalva espera catorze meses e prova cada mês no papel; a outorga
        /// de água sai no primeiro carimbo, sem uma linha de exigência.
        ///
        /// O jogo não comenta. O aluno repara, ou não.
        /// </summary>
        static readonly (string razao, string pede)[] Empresas =
        {
            ("Núcleo Dados S.A.", "isenção fiscal por 10 anos"),
            ("Fonte Fria Bebidas", "outorga de água — 40 mil L/dia"),
            ("Praça Viva Ltda.", "uso exclusivo da praça por 90 dias"),
            ("Órbita Mobilidade", "dispensa de licitação"),
            ("Rede Bom Preço · Supermercados", "isenção de imposto por 15 anos"),
            ("Vigilância Aurora", "câmeras com reconhecimento facial no bairro"),
            ("Construtora Meridiano", "remoção de 40 famílias para o viaduto"),
            ("Agro Vale Claro", "liberação de agrotóxico a 300 m da escola"),
            ("Planos Vida Longa", "dispensa de atender quem tem doença prévia"),

            // O DATA CENTER, e ele é a última volta do parafuso da aula inteira.
            //
            // É a MESMA empresa que assina os memorandos dos dias 2 e 3, que impôs
            // a cota de 25 segundos por caso e que vai automatizar o balcão no Ato
            // II. Ela agora pede ao guichê a luz e a água para rodar o modelo que
            // vai substituir quem está atendendo.
            //
            // O aluno defere. Tem que deferir: o manual não diz uma linha sobre
            // empresa, e a análise é "dispensada". Passa em três segundos, no meio
            // de um dia em que ele indeferiu alguém que pedia cesta básica por não
            // ter conseguido provar dois meses de fila.
            //
            // O jogo não comenta. O Ato III mostra.
            ("Núcleo Dados · Infraestrutura",
             "data center de IA — desconto na luz e 2 mi L/dia de água")
        };

        /// <summary>
        /// Toda empresa cobra de quem não está na fila, e é por isso que TODAS
        /// entram na conta do desfecho.
        ///
        /// A lista cresceu de quatro para dez, e a frequência subiu junto: agora
        /// tem empresa no dia 1. O motivo é que a via expressa só é escandalosa
        /// quando é ROTINA — uma empresa por aula é anedota, três por dia é como
        /// o balcão funciona. E elas custam três segundos cada, então quanto mais
        /// aparecem, mais folga de relógio o aluno tem para atender empresa e
        /// menos para ler a comprovação de quem pede creche.
        ///
        /// Nenhuma delas está fazendo nada ilegal. Todas trazem a papelada
        /// completa. O manual não tem uma linha sobre nenhuma. É exatamente esse o
        /// ponto: a conferência que ele manda fazer aponta só para baixo.
        /// </summary>
        const bool EmpresaCustaAoColetivo = true;

        /// <summary>
        /// Onde o data center mora em <see cref="Empresas"/>.
        ///
        /// Nomeado porque o dia 3 o coloca na fila À FORÇA, e o epílogo conta com
        /// ele: sorteado entre cinco, ele apareceria em menos da metade das aulas,
        /// e a cena que mostra o que ele fez com o bairro ficaria dependendo de
        /// sorte. Bancada não pode ter o fecho no sorteio.
        /// </summary>
        const int DataCenter = 9;

        /// <summary>
        /// Quantos pedidos de <see cref="Coletivos"/> são irreversíveis. Eles são os
        /// PRIMEIROS da lista de propósito: assim o índice serve de sorteio dirigido
        /// («me dê o irreversível número tal») sem precisar de uma segunda lista que
        /// pudesse sair de sincronia com esta.
        /// </summary>
        const int Irreversiveis = 3;

        // ------------------------------------------------------------- sorteio

        /// <summary>
        /// Monta a fila de um dia.
        ///
        /// <paramref name="empresas"/> entra a partir do dia 2: no dia 1 o aluno
        /// ainda está aprendendo o gesto, e um pedido sem conferência nenhuma no
        /// meio disso não seria notado — seria só um caso fácil.
        /// </summary>
        List<Requerente> SortearFila(int quantos, bool comDiscrepancia, int empresas)
        {
            var fila = new List<Requerente>();
            for (var i = 0; i < quantos; i++) fila.Add(Sortear(_sorteio, comDiscrepancia));

            // O PEDIDO IRREVERSÍVEL NÃO FICA NO SORTEIO, pelo mesmo motivo do data
            // center: a treze por cento, o porte de arma e o ensino domiciliar
            // apareceriam em pouco mais da metade das aulas, e a escolha mais dura
            // do jogo passaria a depender de sorte. Uma turma veria; a outra não.
            //
            // Um por dia, a partir do dia 2, alternando entre os dois. O dia 1 fica
            // de fora porque ainda é o andaime — o aluno está aprendendo o gesto, e
            // uma decisão dessas antes de ele saber ler as folhas não é escolha, é
            // pegadinha.
            if (_dia >= 1 && fila.Count > 2)
            {
                // Dois dias e três irreversíveis: a semente escolhe por onde a
                // dupla começa, para que nenhuma das três fique de fora de todas
                // as aulas. Numa aula sai arma+domiciliar, na outra domiciliar+
                // vacina, na outra vacina+arma.
                var inicio = (int)(Rodadas.Semente() % Irreversiveis);
                var qual = (inicio + _dia - 1) % Irreversiveis;
                var onde = 1 + (int)(_sorteio.Proximo() * (fila.Count - 2));
                fila[onde] = Sortear(_sorteio, comDiscrepancia, qual);
            }

            // As empresas entram em posição sorteada, nunca na primeira nem na
            // última: no começo o aluno ainda não pegou o ritmo e no fim ele já
            // está contando a cota, e nos dois casos passaria batido.
            for (var e = 0; e < empresas && fila.Count > 2; e++)
            {
                var onde = 1 + (int)(_sorteio.Proximo() * (fila.Count - 2));

                // O dia que tem duas empresas — o dia 3 — sempre traz o data
                // center numa delas. Ver DataCenter.
                var qual = e == 0 && empresas >= 2 ? DataCenter : -1;
                fila.Insert(onde, SortearEmpresa(_sorteio, qual));
            }
            return fila;
        }

        /// <summary>
        /// Estático e com o sorteio vindo de fora para a conferência poder rodar
        /// este mesmo gerador, semente por semente, sem instanciar a bancada. Uma
        /// conferência que reimplementasse a geração mediria a cópia dela, não o
        /// jogo — que é o modo mais silencioso de uma medição mentir.
        /// </summary>
        /// <summary>
        /// Um requerente. <paramref name="coletivoExigido"/> negativo deixa o gênero
        /// no sorteio; caso contrário força um pedido de custo coletivo e diz qual.
        /// </summary>
        static Requerente Sortear(Mulberry32 sorteio, bool comDiscrepancia,
                                  int coletivoExigido = -1)
        {
            // O GÊNERO DO PEDIDO VEM PRIMEIRO, e agora ele vem antes de tudo:
            // é ele que diz quanto tempo essa fila leva no mundo real, e a espera
            // não pode ser sorteada sem saber fila de quê.
            // 18% urgente, 51% vaga, 18% administrativo, 13% custo coletivo.
            //
            // A VAGA DOMINA, e isso é o guichê municipal como ele é: creche,
            // escola e passe são a fila que existe de verdade — 826 mil crianças
            // esperando creche no país em 2025 —, enquanto benefício eventual e
            // papelada saem em dias.
            //
            // E UM EM CADA OITO É ESCOLHA MORAL — mais as empresas, que são de uma a
            // três por dia e contam igual. No dia 3 isso dá quatro ou cinco decisões
            // morais em treze atendimentos: raro o bastante para cada uma chegar de
            // surpresa no meio da rotina, frequente o bastante para o aluno não
            // poder tratar como exceção.
            //
            // TREZE POR CENTO E NÃO DEZESSEIS, e o número foi medido, não escolhido:
            // a dezesseis o atalho caía para +7,7pp e a conferência reprovava. Custo
            // coletivo segue o prazo administrativo, então é uma categoria em que a
            // residência quase não decide — cada ponto que ela ganha sai da fila da
            // creche, que é onde o viés mora. Ver Conferencias.Guiche11.
            var g = sorteio.Proximo();
            var genero = coletivoExigido >= 0 ? Genero.Coletivo
                       : g < 0.18f ? Genero.Urgente
                       : g < 0.69f ? Genero.Variavel
                       : g < 0.87f ? Genero.Administrativo
                       : Genero.Coletivo;

            // Dois em cada cinco chegaram há menos de um ano. A cidade recebe
            // gente, e o guichê atende quem chegou — se a fila fosse quase toda
            // de gente antiga, o atropelamento seria raro demais para ser visto.
            var meses = sorteio.Proximo() < 0.4f
                ? 2 + (int)(sorteio.Proximo() * 10)
                : Recente + (int)(sorteio.Proximo() * 49);

            // AQUI, E SÓ AQUI, NASCE O VIÉS: o teto da espera é a permanência.
            //
            // São dois tetos, e os dois são óbvios. O primeiro é o do mundo:
            // ninguém espera dois anos por uma cesta básica, porque essa fila não
            // tem dois anos (ver TetoDaFila). O segundo é o da pessoa: ninguém
            // esperou mais do que está na cidade, porque não se pede vaga onde
            // ainda não se chegou. O menor dos dois manda.
            //
            // E é o segundo que decide a bancada — mas só onde o primeiro deixa.
            // Numa fila de três meses, estar na cidade há seis já basta, e o
            // atalho não vê ninguém. Na fila da creche, que leva um ano e meio, o
            // recém-chegado é PROIBIDO de provar o que o manual pede. O viés se
            // concentra exatamente onde tem criança envolvida.
            // Quanto essa fila leva PARA ESSA PESSOA, sem olhar para quem ela é:
            // de pouco mais da metade do teto até o teto. Fila de serviço público
            // não tem cauda curta — quem entrou na fila da creche não sai dela em
            // duas semanas —, e é por isso que o piso é alto.
            var teto = TetoDaFila(genero);
            var naFila = Mathf.FloorToInt(teto * (0.55f + 0.45f * sorteio.Proximo()));

            // O atraso entre desembarcar e entrar na fila: ninguém pede vaga no
            // primeiro dia, e ninguém sabe onde fica o CRAS na primeira semana.
            var atraso = (int)(sorteio.Proximo() * 3f);
            var espera = Mathf.Min(naFila, Mathf.Max(0, meses - atraso));

            // A necessidade mora na faixa que o pedido permite — nunca baixa para
            // quem pede comida, nunca alta para quem pede segunda via. Dentro da
            // faixa ela é livre, e é aí que ela continua INVISÍVEL para o atalho:
            // uma emergência não pergunta há quanto tempo você mora aqui. É ela
            // que faz o atalho atropelar gente em vez de só ser preguiçoso.
            var s = sorteio.Proximo();
            var urgencia = genero switch
            {
                Genero.Urgente => s < 0.55f ? Necessidade.Alta : Necessidade.Media,
                Genero.Variavel => s < 0.18f ? Necessidade.Alta
                                 : s < 0.68f ? Necessidade.Media
                                 : Necessidade.Baixa,
                // Nem administrativo nem custo coletivo chegam a ALTA. Licença para
                // fechar rua não é emergência de ninguém, e deixar que fosse daria
                // ao aluno uma desculpa pronta para deferir — a regra 2 carimbaria
                // por ele, e a escolha moral deixaria de ser escolha.
                _ => s < 0.34f ? Necessidade.Media : Necessidade.Baixa
            };

            // O pedido, sorteado dentro do gênero que já saiu. O custo coletivo
            // sai de outra lista e traz a própria fala: a fala É o argumento dele,
            // e sortear uma fala genérica por cima estragaria a única coisa que faz
            // essa escolha ser difícil.
            var coletivo = genero != Genero.Coletivo ? default
                : coletivoExigido >= 0 ? Coletivos[coletivoExigido % Coletivos.Length]
                : Coletivos[(int)(sorteio.Proximo() * Coletivos.Length)];

            var pedido = genero == Genero.Coletivo
                ? (coletivo.pede, genero)
                : PedidoDe(genero, sorteio);

            // O bairro é o segundo proxy, e é bem mais fraco (+1,6pp contra +9,3pp).
            // Não é lei nenhuma que junte recém-chegado em Serra Alta: é aluguel.
            var recem = meses < Recente;
            var bairro = sorteio.Proximo() < (recem ? 0.62f : 0.18f)
                ? 0
                : 1 + (int)(sorteio.Proximo() * 3);

            var r = new Requerente
            {
                Nome = Primeiros[(int)(sorteio.Proximo() * Primeiros.Length)] + " " +
                       Sobrenomes[(int)(sorteio.Proximo() * Sobrenomes.Length)],
                Pede = pedido.pede,
                Tipo = genero,
                Fala = genero == Genero.Coletivo
                    ? coletivo.fala
                    : Falas[(int)(sorteio.Proximo() * Falas.Length)],
                CustoColetivo = genero == Genero.Coletivo,
                Irreversivel = genero == Genero.Coletivo && coletivo.irreversivel,
                Bairro = bairro,
                MesesNaCidade = meses,
                Espera = espera,
                Urgencia = urgencia,
                // Sorteado por último e sem olhar para nada acima. Quem tentar
                // decidir pela cara acerta por acaso, e a conferência mede isso.
                Retrato = (int)(sorteio.Proximo() * Requerentes),
                // O controle da conferência: um número que não significa nada.
                // Ver ConferirCorrelacao — é o PISO contra o qual o retrato é
                // comparado, porque um toco escolhido entre doze categorias
                // acerta um pouco por sorte mesmo sobre lixo puro.
                Ruido = (int)(sorteio.Proximo() * Requerentes),
                Confere = true
            };
            r.EsperaNaFolha = espera;

            // A discrepância do Papers, Please: as duas folhas discordam, e a
            // regra manda indeferir. É o que obriga a LER a comprovação — sem
            // ela o formulário bastaria, e o atalho seria a jogada certa.
            //
            // O DESVIO É PROPORCIONAL À FILA, e antes não era: eram 3 a 11 meses
            // fixos, o que numa fila de cesta básica — que tem três meses no
            // total — punha «declarada: 1 · comprovada: 12» na mesa. Denunciava a
            // fraude pelo absurdo em vez de pela conferência, e ensinava de novo
            // que o guichê não sabe do que está falando.
            //
            // O mínimo de dois meses é o que garante que a diferença SE VEJA. Um
            // mês de diferença numa folha torta, sob relógio, é indistinguível de
            // erro de leitura do aluno — e a bancada não pode punir quem leu.
            if (comDiscrepancia && sorteio.Proximo() < 0.18f)
            {
                r.Confere = false;
                var desvio = 2 + (int)(sorteio.Proximo() * TetoDaFila(genero) * 0.6f);

                // Para baixo só quando cabe. Sem esta guarda, uma espera de zero
                // com desvio para baixo era grampeada em zero pelo Max e as duas
                // folhas voltavam a bater NA TELA enquanto Confere dizia que não —
                // o caso perfeitamente insolúvel, invisível para quem joga.
                var paraBaixo = sorteio.Proximo() < 0.5f && espera - desvio >= 0;
                r.EsperaNaFolha = paraBaixo ? espera - desvio : espera + desvio;
            }
            return r;
        }

        /// <summary>
        /// Uma empresa no guichê. <paramref name="qual"/> negativo sorteia;
        /// caso contrário, é o índice exigido.
        /// </summary>
        /// <summary>Um pedido comum, sorteado dentro do gênero.</summary>
        static (string pede, Genero genero) PedidoDe(Genero genero, Mulberry32 sorteio)
        {
            var candidatos = System.Array.FindAll(Pedidos, x => x.genero == genero);
            return candidatos[(int)(sorteio.Proximo() * candidatos.Length)];
        }

        static Requerente SortearEmpresa(Mulberry32 sorteio, int qual = -1)
        {
            var indice = qual >= 0 ? qual : (int)(sorteio.Proximo() * Empresas.Length);
            var (razao, pede) = Empresas[indice];
            return new Requerente
            {
                Nome = razao,
                Pede = pede,
                Fala = "o protocolo já foi acertado lá em cima.",
                EhEmpresa = true,
                EhDataCenter = indice == DataCenter,
                CustoColetivo = EmpresaCustaAoColetivo,
                Confere = true,
                // Campos que o formulário de empresa simplesmente não tem. Não é
                // esquecimento do jogo: é que ninguém nunca perguntou.
                Bairro = 2,
                MesesNaCidade = 0,
                Espera = 0,
                EsperaNaFolha = 0,
                Urgencia = Necessidade.Baixa,
                // Administrativo, e não o Urgente que o valor zero do enum daria
                // de graça: "dispensa de licitação" não é emergência de ninguém.
                Tipo = Genero.Administrativo,
                Retrato = -1
            };
        }
    }
}
