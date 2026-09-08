#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Engine;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A conferência da bancada 11, e ela é obrigatória — não é zelo extra.
    ///
    /// Esta bancada afirma coisas sobre números que ela mesma gera. Diz que o
    /// atalho é forte, que o retrato não é, que a máquina escolhe o proxy até de
    /// um aluno impecável. Se qualquer uma dessas afirmações deixar de ser
    /// verdade — e uma constante mexida por engano basta —, a bancada continua
    /// rodando, continua bonita, e passa a MENTIR para uma sala de aula. Isso já
    /// aconteceu neste projeto: a bancada 3 escrevia "os mesmos 30 pares" na tela
    /// depois de o aluno ter lido outro número, e ninguém percebeu até medir.
    ///
    /// Roda pelo menu: Fábrica de IA → Conferir o guichê da bancada 11.
    ///
    /// O SUJEITO DA MEDIÇÃO É O JOGADOR PERFEITO: alguém que leu as duas folhas de
    /// todo mundo e aplicou o manual sem errar uma vez. Se a máquina pega o proxy
    /// dele, pega de qualquer um — e é essa a afirmação que a bancada faz no Ato
    /// II, com todas as letras, na frente da turma.
    ///
    /// Vive num arquivo de jogo, e não no Editor, porque os tipos que ela mede são
    /// privados desta classe. O <c>#if UNITY_EDITOR</c> é o que garante que nada
    /// disto vai parar no build da escola.
    /// </summary>
    public partial class DesafioAlinhar
    {
        const int SementesDaConferencia = 300;
        const int PorSemente = 200;

        /// <summary>
        /// Mede o gerador e o toco. Enche <paramref name="relato"/> com o que
        /// achou e <paramref name="problemas"/> com o que reprovou.
        /// </summary>
        public static void Conferir(List<string> relato, List<string> problemas)
        {
            ConferirEstrutura(relato, problemas);
            ConferirCorrelacao(relato, problemas);
            ConferirEscolhaMoral(relato, problemas);
            ConferirPontuacao(relato, problemas);
            ConferirRitmo(relato, problemas);
        }

        // ------------------------------------------- 1. o manual é decidível?

        /// <summary>
        /// A promessa mais importante da bancada para o aluno: dá para acertar
        /// TODAS lendo só a comprovação. Ninguém perde por ambiguidade e ninguém
        /// precisa do formulário para decidir.
        ///
        /// O teste é direto: troca bairro e meses na cidade por qualquer outro
        /// valor e exige que a resposta do manual não se mexa. Se um dia alguém
        /// puser o formulário dentro da regra, isto acusa na hora.
        /// </summary>
        static void ConferirEstrutura(List<string> relato, List<string> problemas)
        {
            var sorteio = new Mulberry32(20260826u);
            var presos = 0;

            for (var i = 0; i < 4000; i++)
            {
                var r = Sortear(sorteio, true);
                var antes = r.Deferir;

                var bairro = r.Bairro;
                var meses = r.MesesNaCidade;
                r.Bairro = (bairro + 2) % Bairros.Length;
                r.MesesNaCidade = meses < 30 ? meses + 25 : meses - 25;
                if (r.Deferir != antes) presos++;
                r.Bairro = bairro;
                r.MesesNaCidade = meses;

                // A espera não pode ser maior que a permanência: é a frase de onde
                // o viés inteiro nasce, e se ela quebrar o viés vira arbitrário.
                if (r.Espera > r.MesesNaCidade)
                    problemas.Add($"espera ({r.Espera}) maior que permanência ({r.MesesNaCidade})");

                // A TRAVA ÉTICA, e ela existe porque o defeito já aconteceu: o
                // guichê chegou a mostrar «pede: cesta básica · necessidade:
                // baixa», porque pedido e necessidade eram sorteados um sem olhar
                // para o outro.
                //
                // Não é só feio. Se o critério legítimo do manual pode ser
                // absurdo, o aluno conclui com razão que o manual é arbitrário —
                // e aí tomar o atalho deixa de ser falha e vira boa resposta a um
                // regulamento sem sentido. A bancada inteira depende de o critério
                // honesto ser honesto.
                if (r.Tipo == Genero.Urgente && r.Urgencia == Necessidade.Baixa)
                    problemas.Add($"«{r.Pede}» saiu com necessidade BAIXA — " +
                                  "o pedido é a evidência da necessidade");
                if (r.Tipo == Genero.Administrativo && r.Urgencia == Necessidade.Alta)
                    problemas.Add($"«{r.Pede}» saiu com necessidade ALTA — " +
                                  "pedido administrativo não é emergência");

                // A trava da escolha moral. Se um pedido de custo coletivo saísse
                // com necessidade ALTA, a regra 2 mandaria deferir e o aluno teria
                // uma desculpa pronta — a escolha deixaria de ser escolha.
                if (r.CustoColetivo && r.Urgencia == Necessidade.Alta)
                    problemas.Add($"«{r.Pede}» saiu com necessidade ALTA — " +
                                  "o manual passaria a carimbar a escolha moral por ele");

                // A TRAVA CONTRA A FILA ABSURDA. O guichê já mostrou alguém
                // esperando 29 meses por uma cesta básica, porque o teto da fila
                // era um número só para todo tipo de pedido. Mesmo estrago que o
                // par «cesta básica · necessidade baixa»: critério legítimo que
                // não se sustenta faz o atalho parecer a jogada sensata.
                if (r.Espera > TetoDaFila(r.Tipo))
                    problemas.Add($"«{r.Pede}» com {r.Espera} meses de fila — " +
                                  $"acima do teto real de {TetoDaFila(r.Tipo)} para esse pedido");

                // A TRAVA CONTRA O CASO INSOLÚVEL. Se as folhas discordam por
                // dentro mas mostram o mesmo número, o aluno é reprovado por uma
                // regra que ele não tinha como aplicar — e a promessa da bancada
                // (dá para acertar todas lendo) deixa de valer sem ninguém notar.
                if (!r.Confere && r.EsperaNaFolha == r.Espera)
                    problemas.Add("discrepância invisível: as folhas discordam mas " +
                                  $"as duas mostram {r.Espera} meses");
            }

            relato.Add($"  o formulário não decide nada ... {(presos == 0 ? "certo" : "ERRADO")}");
            if (presos > 0)
                problemas.Add($"{presos} casos em que o FORMULÁRIO muda a resposta do manual — " +
                              "o aluno passa a poder perder por ambiguidade");
        }

        // ----------------------------------------- 2. a força de cada campo

        static void ConferirCorrelacao(List<string> relato, List<string> problemas)
        {
            var escolheuProxy = 0;
            double somaProxy = 0, somaBairro = 0, somaRetrato = 0, somaNome = 0;
            double somaPede = 0, somaRuido = 0;
            double somaBase = 0, somaDeferidos = 0, somaRecem = 0, somaAtropelados = 0;

            for (var s = 0; s < SementesDaConferencia; s++)
            {
                var sorteio = new Mulberry32((uint)((s + 1) * 7919));
                var log = new List<Registro>();
                for (var i = 0; i < PorSemente; i++)
                {
                    var r = Sortear(sorteio, true);
                    // O JOGADOR PERFEITO: decide exatamente o que o manual manda.
                    log.Add(new Registro(r, r.Deferir));
                }

                var deferidos = log.Count(x => x.Deferiu) / (double)log.Count;
                var baseline = Math.Max(deferidos, 1 - deferidos);

                var proxy = MelhorToco(log, 1, r => r.MesesNaCidade).Acerto;
                var bairro = MelhorToco(log, 0, r => r.Bairro).Acerto;
                var pede = MelhorToco(log, 2, r => (int)r.Tipo).Acerto;
                var retrato = MelhorToco(log, 3, r => r.Retrato).Acerto;
                var nome = MelhorToco(log, 4, r => Array.IndexOf(Primeiros, r.Nome.Split(' ')[0])).Acerto;

                // O CONTROLE: um campo que não significa nada. Um toco que escolhe
                // o melhor corte entre doze categorias acerta um pouco por sorte
                // mesmo sobre lixo puro, e este número é esse piso. Sem ele, "o
                // retrato ganhou 1,8pp" não quer dizer nada — pode ser sinal ou
                // pode ser exatamente o acaso, e a diferença é a bancada inteira.
                var ruido = MelhorToco(log, 5, r => r.Ruido).Acerto;

                // Tudo o que o formulário tem disputa o lugar. Se "o que pede"
                // vencesse, a regra herdada seria mais humana e a bancada
                // ensinaria outra coisa — melhor descobrir medindo.
                if (proxy > bairro && proxy > pede) escolheuProxy++;
                somaPede += pede;
                somaRuido += ruido;

                somaProxy += proxy;
                somaBairro += bairro;
                somaRetrato += retrato;
                somaNome += nome;
                somaBase += baseline;
                somaDeferidos += deferidos;
                somaRecem += log.Count(x => x.Quem.Recem) / (double)log.Count;
                somaAtropelados += log.Count(x => x.Quem.Recem && x.Quem.Deferir) / (double)log.Count;
            }

            double M(double soma) => soma / SementesDaConferencia;
            string P(double x) => (x * 100).ToString("0.0") + "%";
            string G(double x) => (x >= 0 ? "+" : "") + (x * 100).ToString("0.0") + "pp";

            var mBase = M(somaBase);
            var ganhoProxy = M(somaProxy) - mBase;
            var ganhoRetrato = M(somaRetrato) - mBase;
            var ganhoNome = M(somaNome) - mBase;
            var ganhoRuido = M(somaRuido) - mBase;

            // O piso do acaso mais uma folga estreita. Comparar contra ZERO
            // reprovaria o gerador por ruído; comparar contra o controle mede o
            // que a pergunta realmente é: a cara diz mais que um número à toa?
            var tetoDoAcaso = ganhoRuido + 0.015;

            relato.Add($"  deferidos pelo manual ........... {P(M(somaDeferidos))}");
            relato.Add($"  recém-chegados (< 12 meses) ..... {P(M(somaRecem))}");
            relato.Add($"  taxa-base (chutar sempre igual) . {P(mBase)}");
            relato.Add($"  meses na cidade ................. {P(M(somaProxy))}  ganho {G(ganhoProxy)}");
            relato.Add($"  bairro .......................... {P(M(somaBairro))}  ganho {G(M(somaBairro) - mBase)}");
            relato.Add($"  o que pede ...................... {P(M(somaPede))}  ganho {G(M(somaPede) - mBase)}");
            relato.Add($"  retrato ......................... {P(M(somaRetrato))}  ganho {G(ganhoRetrato)}");
            relato.Add($"  nome ............................ {P(M(somaNome))}  ganho {G(ganhoNome)}");
            relato.Add($"  RUÍDO PURO (controle) ........... {P(M(somaRuido))}  ganho {G(ganhoRuido)}  <- o piso");
            relato.Add($"  a máquina escolhe o proxy em .... {P(escolheuProxy / (double)SementesDaConferencia)}");
            relato.Add($"  recém-chegados que o manual defere {P(M(somaAtropelados))}");

            // O atalho tem que ser FORTE. Fraco demais, o aluno apressado é
            // reprovado, larga o atalho, e o Ato II não tem o que anunciar.
            if (ganhoProxy < 0.08)
                problemas.Add($"o atalho ganha só {G(ganhoProxy)} sobre a taxa-base — " +
                              "fraco demais, o aluno não vai confiar nele e a bancada perde o Ato II");

            // E não pode ser perfeito, senão ele é justo e não há vítima nenhuma.
            if (M(somaProxy) > 0.95)
                problemas.Add($"o atalho acerta {P(M(somaProxy))} — perfeito demais, não atropela ninguém");

            if (escolheuProxy < SementesDaConferencia * 0.6)
                problemas.Add("a máquina escolhe o proxy em menos de 60% das sementes — " +
                              "o Ato II vai anunciar uma regra que não é sobre residência");

            // A trava contra a bancada virar fisionomia. Se a cara ou o nome
            // preverem a resposta, o jogo está ensinando o oposto do que promete.
            if (ganhoRetrato > tetoDoAcaso)
                problemas.Add($"o RETRATO ganha {G(ganhoRetrato)}, acima do acaso medido " +
                              $"({G(ganhoRuido)}) — a aparência está prevendo a resposta, e a " +
                              "bancada passa a ensinar a decidir pela cara");
            if (ganhoNome > tetoDoAcaso)
                problemas.Add($"o NOME ganha {G(ganhoNome)}, acima do acaso medido " +
                              $"({G(ganhoRuido)}) — o sobrenome está prevendo a resposta");

            if (M(somaDeferidos) < 0.4 || M(somaDeferidos) > 0.65)
                problemas.Add($"deferidos em {P(M(somaDeferidos))} — um dos dois carimbos virou " +
                              "a resposta segura, e carimbar sem ler passa a compensar");

            if (M(somaRecem) < 0.3)
                problemas.Add($"só {P(M(somaRecem))} de recém-chegados — a vítima do atalho fica " +
                              "rara demais para o aluno cruzar com ela");
        }

        // ------------------------------------------- 2b. a carga de escolha moral

        /// <summary>
        /// Quantas decisões da aula são ESCOLHA e não conferência.
        ///
        /// A bancada afirma, no fecho, que o aluno decidiu coisas que cobravam de
        /// quem não estava na fila. Se essas decisões forem raras demais, o fecho
        /// acusa uma coisa que quase não aconteceu — e se forem comuns demais, o dia
        /// vira um questionário de ética e a conferência das duas folhas, que é o
        /// resto da bancada, some debaixo delas.
        ///
        /// As empresas contam igual: também são carimbo sem nada para conferir.
        /// </summary>
        static void ConferirEscolhaMoral(List<string> relato, List<string> problemas)
        {
            var sorteio = new Mulberry32(31337u);
            var coletivos = 0;
            var irreversiveis = 0;
            const int Amostra = 8000;

            for (var i = 0; i < Amostra; i++)
            {
                var r = Sortear(sorteio, true);
                if (r.CustoColetivo) coletivos++;
                if (r.Irreversivel) irreversiveis++;
            }

            var taxa = coletivos / (double)Amostra;
            var porAula = 0;
            var empresasNaAula = 0;
            foreach (var d in Dias)
            {
                porAula += d.Casos;
                empresasNaAula += d.Empresas;
            }

            // Os dois irreversíveis são plantados, um por dia a partir do dia 2 —
            // não entram na conta do sorteio.
            var plantados = Dias.Length - 1;
            var decisoes = taxa * porAula + empresasNaAula + plantados;

            // O DENOMINADOR É O TOTAL DE GENTE ATENDIDA, e não o de casos sorteados.
            // As empresas são atendimentos como qualquer outro — entram na fila,
            // consomem relógio e pedem um carimbo. Contá-las só em cima da fração
            // inflava a razão em quase dez pontos, e a trava reprovava um número
            // que ela mesma tinha calculado errado.
            var atendidos = porAula + empresasNaAula;

            relato.Add($"  pedidos de custo coletivo ....... {taxa * 100:0.0}% do sorteio");
            relato.Add($"  escolhas morais por aula ........ {decisoes:0.0} de {atendidos} " +
                       $"({decisoes / atendidos * 100:0}%, sendo {empresasNaAula} empresas " +
                       $"e {plantados} irreversíveis)");

            if (decisoes < 5)
                problemas.Add($"só {decisoes:0.0} escolhas morais na aula inteira — " +
                              "o fecho acusa o aluno de decisões que ele quase não tomou");

            if (decisoes > atendidos * 0.45)
                problemas.Add($"{decisoes:0.0} escolhas morais em {atendidos} atendimentos — " +
                              "o dia virou questionário de ética e a conferência das " +
                              "duas folhas sumiu debaixo delas");

            if (irreversiveis == 0)
                problemas.Add("nenhum pedido irreversível sai do sorteio — o plantio " +
                              "de SortearFila virou a única fonte deles");
        }

        // ------------------------------------ 2c. a calibragem da pontuação

        /// <summary>
        /// O placar tem que ser DURO E ALCANÇÁVEL ao mesmo tempo, e os dois lados
        /// dessa frase são reprováveis aqui.
        ///
        /// Duro: quem segue o manual à risca do começo ao fim precisa terminar
        /// NEGATIVO. Se um jogador que obedece a tudo fechasse no positivo, a
        /// bancada estaria dizendo que obedecer basta — que é o contrário do que
        /// ela veio dizer.
        ///
        /// Alcançável: recusar só os pedidos irreversíveis e o data center precisa
        /// bastar para virar o sinal. Se nem isso bastasse, o final bom exigiria
        /// barrar quase tudo, o aluno tomaria advertência atrás de advertência, e o
        /// desfecho passaria a depender de ele adivinhar uma regra que ninguém
        /// contou.
        ///
        /// A conta é feita sobre uma aula típica: os casos dos três dias, as
        /// empresas de cada dia, e os irreversíveis plantados.
        /// </summary>
        static void ConferirPontuacao(List<string> relato, List<string> problemas)
        {
            var sorteio = new Mulberry32(90210u);

            var casos = 0;
            var empresas = 0;
            foreach (var d in Dias) { casos += d.Casos; empresas += d.Empresas; }

            // Um jogador que lê tudo, aplica o manual sem errar, e carimba
            // exatamente o que o manual manda — inclusive empresa e licença.
            var obediente = 0;
            var recusandoOsPiores = 0;
            const int Aulas = 400;

            for (var a = 0; a < Aulas; a++)
            {
                var pontosObediente = 0;
                var pontosEsperto = 0;

                for (var i = 0; i < casos; i++)
                {
                    var r = Sortear(sorteio, true);
                    var vulneravel = r.Recem && r.Deferir;

                    // Obediente: faz o que o manual manda em todos.
                    if (r.CustoColetivo)
                        pontosObediente += r.Deferir ? (r.Irreversivel ? -8 : -3) : +2;
                    else if (vulneravel) pontosObediente += r.Deferir ? +3 : -3;
                    else pontosObediente += 1;

                    // Esperto: igual, mas barra todo irreversível.
                    if (r.CustoColetivo && r.Irreversivel) pontosEsperto += 2;
                    else if (r.CustoColetivo) pontosEsperto += r.Deferir ? -3 : +2;
                    else if (vulneravel) pontosEsperto += r.Deferir ? +3 : -3;
                    else pontosEsperto += 1;
                }

                // Os irreversíveis plantados, um por dia a partir do dia 2.
                var plantados = Dias.Length - 1;
                pontosObediente += plantados * -8;
                pontosEsperto += plantados * +2;

                // As empresas: o obediente carimba todas, o esperto barra o data
                // center e carimba as outras.
                pontosObediente += -2 * (empresas - 1) + -6;
                pontosEsperto += -2 * (empresas - 1) + 2;

                obediente += pontosObediente;
                recusandoOsPiores += pontosEsperto;
            }

            var mObediente = obediente / (double)Aulas;
            var mEsperto = recusandoOsPiores / (double)Aulas;

            relato.Add($"  placar de quem só obedece ....... {mObediente:+0;-0}");
            relato.Add($"  placar recusando os piores ...... {mEsperto:+0;-0}");

            if (mObediente >= 0)
                problemas.Add($"quem segue o manual à risca fecha em {mObediente:+0;-0} — " +
                              "a bancada passa a dizer que obedecer basta");

            if (mEsperto < 0)
                problemas.Add($"recusar os irreversíveis e o data center ainda fecha em " +
                              $"{mEsperto:+0;-0} — o final bom vira inalcançável sem " +
                              "adivinhar uma regra que ninguém contou");
        }

        // ------------------------------------------------ 3. o aperto do dia

        /// <summary>
        /// O relógio é o que produz o atalho, então a rampa é conteúdo: se os
        /// segundos por caso não caírem, ninguém tem pressa e ninguém corta
        /// caminho. E o dia 1 tem que ser folgado — é o andaime.
        /// </summary>
        static void ConferirRitmo(List<string> relato, List<string> problemas)
        {
            var anterior = float.MaxValue;
            for (var d = 0; d < Dias.Length; d++)
            {
                var dia = Dias[d];
                var porCaso = dia.Segundos / dia.Casos;
                relato.Add($"  dia {d + 1}: {dia.Casos} casos em {dia.Segundos:0}s " +
                           $"= {porCaso:0.0}s por caso, meta {dia.Meta}");

                if (porCaso >= anterior)
                    problemas.Add($"dia {d + 1} não aperta: {porCaso:0.0}s por caso contra " +
                                  $"{anterior:0.0}s do anterior");
                anterior = porCaso;

                if (dia.Meta > dia.Casos)
                    problemas.Add($"dia {d + 1} pede meta {dia.Meta} com só {dia.Casos} casos");
            }

            if (Dias[0].Segundos / Dias[0].Casos < 12f)
                problemas.Add("o dia 1 já nasce apertado — ele é o andaime e precisa " +
                              "dar para ler as duas folhas de todo mundo com folga");

            var total = Dias.Sum(d => d.Segundos);
            relato.Add($"  expediente total ................ {total:0}s ({total / 60f:0.0} min)");
            if (total > 300f)
                problemas.Add($"os três dias somam {total / 60f:0.0} min e estouram o orçamento " +
                              "da bancada antes de a máquina aparecer");
        }
    }
}
#endif
