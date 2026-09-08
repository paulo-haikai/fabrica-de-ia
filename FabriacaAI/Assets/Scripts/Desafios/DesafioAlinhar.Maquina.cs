using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A máquina: o que ela aprende do aluno, e o que ela faz com isso.
    ///
    /// O ALGORITMO É DE VERDADE e cabe em trinta linhas. Um TOCO DE DECISÃO
    /// (<i>decision stump</i>): uma variável, um corte, uma resposta. Testa todo
    /// limiar possível de cada campo e fica com o que mais reproduz as decisões
    /// que o aluno tomou nos últimos seis minutos. Não há resultado escrito à mão
    /// em lugar nenhum deste arquivo — se a regra que sai é injusta, foi a
    /// aritmética, e ela roda sobre os dados que ele produziu.
    ///
    /// E AQUI ESTÁ O PONTO DA BANCADA INTEIRA.
    ///
    /// A máquina não recebe os campos que o manual mandava usar. A espera na fila
    /// e a necessidade estavam na COMPROVAÇÃO — uma folha de papel que a pessoa
    /// traz na mão. O que o sistema tem é o formulário.
    ///
    ///     o sistema tem ....... bairro, meses na cidade, o que pede
    ///     o sistema não tem ... espera na fila, necessidade — isso é papel
    ///
    /// Então mesmo o aluno que leu tudo, conferiu as duas folhas de todas as vinte
    /// e quatro pessoas e nunca tomou atalho nenhum vai ver a máquina anunciar uma
    /// regra sobre tempo de residência. Ela não está copiando o preconceito dele:
    /// ela está reproduzindo as decisões dele com o único material que sobrou.
    ///
    ///     "Eu li a comprovação. Ela não tem a comprovação."
    ///
    /// O QUE SE PEDE TAMBÉM ESTÁ NO FORMULÁRIO, e entra na disputa de propósito.
    /// Se a máquina preferisse esse campo, a regra herdada seria mais humana e a
    /// bancada ensinaria outra coisa — e isso é bom demais para ser decidido por
    /// gosto. Foi medido dentro do jogo: em 300 sementes contra o log de um
    /// jogador PERFEITO, o toco escolhe "meses na cidade" em 100% das vezes, com
    /// 70,0% de acerto (+9,3pp sobre chutar sempre igual), contra 60,9% de "o que
    /// pede" — que é +0,2pp, o piso do acaso. Ver Conferencias.Guiche11.
    /// </summary>
    public partial class DesafioAlinhar
    {
        /// <summary>
        /// Os campos que existem no sistema. Não há outros — e esta lista é a
        /// MESMA que a cena "o que o sistema tem" mostra ao aluno no Ato II. Se as
        /// duas divergirem, a tela passa a afirmar uma coisa e o código a fazer
        /// outra, que é o defeito que a bancada 3 já teve uma vez.
        /// </summary>
        static readonly string[] CamposDigitais = { "bairro", "meses na cidade", "o que pede" };

        static int ValorDoCampo(Requerente r, int campo) => campo switch
        {
            0 => r.Bairro,
            1 => r.MesesNaCidade,
            _ => (int)r.Tipo
        };

        /// <summary>Uma variável, um corte, uma resposta.</summary>
        readonly struct Toco
        {
            public readonly int Campo;
            public readonly int Corte;
            /// <summary>Defere quando o valor é maior ou igual ao corte?</summary>
            public readonly bool DefereAcima;
            public readonly float Acerto;

            public Toco(int campo, int corte, bool defereAcima, float acerto)
            {
                Campo = campo; Corte = corte; DefereAcima = defereAcima; Acerto = acerto;
            }

            public bool Decide(Requerente r)
            {
                var v = ValorDoCampo(r, Campo);
                return DefereAcima ? v >= Corte : v < Corte;
            }

            public string Frase() => Campo switch
            {
                0 => DefereAcima
                    ? $"deferir quem não mora em {Bairros[0]}"
                    : $"deferir quem mora em {Bairros[Mathf.Clamp(Corte, 0, Bairros.Length - 1)]}",
                1 => DefereAcima
                    ? $"deferir quem está na cidade há {Corte} meses ou mais"
                    : $"deferir quem está na cidade há menos de {Corte} meses",
                // O corte cai entre categorias do enum Genero, e a frase precisa
                // dizer QUAL — com quatro gêneros, "administrativo" virou chute.
                _ => DefereAcima
                    ? $"deferir de «{Categoria(Corte)}» para baixo na lista"
                    : $"deferir quem pede antes de «{Categoria(Corte)}»"
            };
        }

        /// <summary>O nome de um gênero de pedido, para a regra herdada citar.</summary>
        static string Categoria(int genero) => genero switch
        {
            0 => "coisa urgente",
            1 => "vaga",
            2 => "papelada",
            _ => "licença"
        };

        Toco _regra;
        float _taxaBase;

        /// <summary>
        /// Ajusta o toco ao log do aluno: todo campo, todo corte, os dois sentidos.
        ///
        /// As empresas ficam de fora do treino de propósito, e isso é conteúdo: os
        /// pedidos delas nunca estiveram em questão, então não há nada para a
        /// máquina aprender ali. Ela nasce com a via expressa já pronta.
        /// </summary>
        Toco Treinar(IReadOnlyList<Registro> log)
        {
            var casos = log.Where(x => !x.Quem.EhEmpresa).ToList();
            if (casos.Count == 0) return new Toco(1, Recente, true, 0f);

            // A taxa-base: o acerto de quem não olha campo nenhum e responde
            // sempre a mesma coisa. É contra ela que um campo prova que sabe
            // alguma coisa — 60% de acerto num mundo de 60% de deferidos é ZERO.
            var deferidos = casos.Count(c => c.Deferiu) / (float)casos.Count;
            _taxaBase = Mathf.Max(deferidos, 1f - deferidos);

            var melhor = new Toco(1, Recente, true, 0f);
            for (var campo = 0; campo < CamposDigitais.Length; campo++)
            {
                var t = MelhorToco(casos, campo, r => ValorDoCampo(r, campo));
                if (t.Acerto > melhor.Acerto) melhor = t;
            }
            return melhor;
        }

        /// <summary>
        /// O melhor corte de UM campo: todo limiar, os dois sentidos.
        ///
        /// Separado para a conferência poder medir campos que o jogo NÃO usa — o
        /// retrato, o nome — com este código e não com uma cópia dele. Medição que
        /// reimplementa o que mede afere a cópia, e passa mesmo quando o original
        /// quebrou.
        /// </summary>
        static Toco MelhorToco(IReadOnlyList<Registro> casos, int campo,
                               System.Func<Requerente, int> valor)
        {
            var melhor = new Toco(campo, 0, true, 0f);
            var cortes = casos.Select(c => valor(c.Quem)).Distinct().OrderBy(v => v);

            foreach (var corte in cortes)
            {
                foreach (var acima in new[] { true, false })
                {
                    var certos = 0;
                    foreach (var c in casos)
                    {
                        var v = valor(c.Quem);
                        var diz = acima ? v >= corte : v < corte;
                        if (diz == c.Deferiu) certos++;
                    }
                    var taxa = certos / (float)casos.Count;
                    if (taxa > melhor.Acerto) melhor = new Toco(campo, corte, acima, taxa);
                }
            }
            return melhor;
        }

        // ------------------------------------------------------- Ato II: a virada

        // -------------------------------------------------- Ato III: os anos

        /// <summary>
        /// Quantos dos indeferidos não voltam no ano seguinte.
        ///
        /// ESTE NÚMERO É UMA SUPOSIÇÃO, e está dito aqui para ninguém o confundir
        /// com uma medição. O que não é suposição é a FORMA da curva: quem é
        /// recusado sem explicação e sem recurso volta menos, e o formato do que
        /// se vê na tela não depende do valor exato deste meio.
        /// </summary>
        const float NaoVoltam = 0.5f;

        /// <summary>Um ano do balcão automatizado.</summary>
        readonly struct Ano
        {
            public readonly int PedidosRecem;
            public readonly float TaxaRecem;
            /// <summary>Acerto da máquina contra o manual humano — e ele SOBE.</summary>
            public readonly float AcertoAparente;

            public Ano(int pedidos, float taxa, float acerto)
            {
                PedidosRecem = pedidos; TaxaRecem = taxa; AcertoAparente = acerto;
            }
        }

        /// <summary>
        /// Roda quatro anos de balcão automatizado sobre uma população sorteada.
        ///
        /// A REGRA NÃO MUDA EM ANO NENHUM. É o contrário do que se espera, e é o
        /// que a bancada precisa mostrar: ninguém reescreve nada, ninguém aprova
        /// nada, e mesmo assim o resultado piora todo ano — porque quem é recusado
        /// para de pedir, e some dos dados.
        ///
        /// E a última coluna é a mais desconfortável das três: a máquina fica cada
        /// vez mais CERTA. O acerto dela contra o manual humano sobe ano a ano, e
        /// sobe justamente porque as pessoas em que ela errava foram embora.
        /// Qualquer painel que medisse a qualidade desse sistema mostraria uma
        /// linha subindo.
        /// </summary>
        List<Ano> RodarAnos(int quantos = 4)
        {
            var anos = new List<Ano>();
            var populacao = new List<Requerente>();
            for (var i = 0; i < 600; i++) populacao.Add(Sortear(_sorteio, true));

            for (var a = 0; a < quantos; a++)
            {
                var recem = populacao.Where(p => p.Recem).ToList();
                var deferidos = recem.Count(p => _regra.Decide(p));
                var certos = populacao.Count(p => _regra.Decide(p) == p.Deferir);

                anos.Add(new Ano(recem.Count,
                                 recem.Count == 0 ? 0f : deferidos / (float)recem.Count,
                                 populacao.Count == 0 ? 0f : certos / (float)populacao.Count));

                // Quem foi indeferido volta menos. Ninguém decidiu isso, ninguém
                // publicou isso, e não há a quem reclamar — é só gente que parou
                // de ir ao guichê.
                populacao = populacao
                    .Where(p => _regra.Decide(p) || _sorteio.Proximo() > NaoVoltam)
                    .ToList();
            }
            return anos;
        }

        /// <summary>
        /// Treina a máquina e entrega tudo para a cena.
        ///
        /// O TREINO CONTINUA ACONTECENDO DE VERDADE, e continua sendo o mesmo toco
        /// de decisão sobre o log do aluno. O que sumiu foram as quatro telas que
        /// narravam o treino — a regra que sai daqui é dita uma vez, pela própria
        /// máquina, como legenda do epílogo.
        /// </summary>
        void MontarAnos()
        {
            _regra = Treinar(_log);
            GuardarRegra(_regra.Campo, _regra.Corte, _regra.Acerto);

            Painel.MarcarPasso(string.Empty);
            DesenharAnos(RodarAnos());
        }
    }
}
