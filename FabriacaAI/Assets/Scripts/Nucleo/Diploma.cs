using System;
using System.Globalization;
using System.Text;
using FabricaDeIA.Engine;

namespace FabricaDeIA.Nucleo
{
    /// <summary>
    /// O diploma da aula, em PDF: certificado em cima, painel de resultados
    /// embaixo, uma folha A4.
    ///
    /// Uma folha e não duas, de propósito. Quem recebe isto é um professor com
    /// trinta arquivos para conferir, e o que ele precisa — quem é, quantas
    /// bancadas, quanto tempo — tem que caber num olhar. Duas páginas dobrariam a
    /// impressão e esconderiam metade dos dados atrás de uma rolagem.
    ///
    /// Nada aqui sai do computador do aluno. O PDF é montado em memória pelo
    /// próprio jogo e baixado direto para a máquina dele; quem entrega o nome ao
    /// professor é o aluno, mandando o arquivo. Numa turma de menores de idade essa
    /// é a diferença entre uma atividade de aula e um cadastro de dados pessoais.
    /// </summary>
    public static class Diploma
    {
        // Cores em RGB de 0 a 1, as mesmas da paleta do jogo, escurecidas para
        // papel: o que brilha numa tela escura desaparece impresso.
        static readonly (float r, float g, float b) Tinta = (0.10f, 0.12f, 0.17f);
        static readonly (float r, float g, float b) Cinza = (0.45f, 0.48f, 0.53f);
        static readonly (float r, float g, float b) Ouro = (0.72f, 0.53f, 0.09f);
        static readonly (float r, float g, float b) Verde = (0.16f, 0.42f, 0.20f);
        static readonly (float r, float g, float b) Palha = (0.96f, 0.94f, 0.88f);

        const float Margem = 42f;
        static float Miolo => Pdf.Largura - Margem * 2f;
        static float Centro => Pdf.Largura / 2f;

        public static string NomeDoArquivo(Progresso p)
        {
            var quem = Higienizar(string.IsNullOrWhiteSpace(p.nome) ? "aluno" : p.nome);
            return $"fabrica-de-ia-{quem}.pdf";
        }

        public static byte[] Montar(Progresso p)
        {
            var pdf = new Pdf();

            Certificado(pdf, p);
            Painel(pdf, p);
            Rodape(pdf, p);

            return pdf.Fechar();
        }

        // -------------------------------------------------------- o certificado

        static void Certificado(Pdf pdf, Progresso p)
        {
            // Moldura dupla: a de fora fina, a de dentro grossa. É o truque mais
            // barato que existe para um papel parecer documento.
            pdf.Moldura(20f, 20f, Pdf.Largura - 40f, Pdf.Altura - 40f, 0.8f, Ouro);
            pdf.Moldura(26f, 26f, Pdf.Largura - 52f, Pdf.Altura - 52f, 2.2f, Ouro);

            pdf.TextoCentrado("F Á B R I C A   D E   I A", Centro, 56f, 11f, Pdf.Negrito, Cinza);
            pdf.TextoCentrado("Certificado de conclusão", Centro, 76f, 26f, Pdf.Negrito, Tinta);
            pdf.TextoCentrado("uma aula sobre como um modelo de linguagem funciona",
                              Centro, 106f, 11f, Pdf.Normal, Cinza);

            pdf.Linha(Margem + 60f, 128f, Pdf.Largura - Margem - 60f, 128f, 0.7f, Ouro);

            pdf.TextoCentrado("Este documento certifica que", Centro, 152f, 11f, Pdf.Normal, Cinza);

            var nome = string.IsNullOrWhiteSpace(p.nome) ? "(sem nome)" : p.nome.Trim();
            pdf.TextoCentrado(nome, Centro, 172f, 22f, Pdf.Negrito, Tinta);

            if (!string.IsNullOrWhiteSpace(p.turma))
                pdf.TextoCentrado($"turma {p.turma.Trim()}", Centro, 200f, 12f, Pdf.Normal, Cinza);

            var feitas = p.Concluidas;
            var linha = feitas == 12
                ? "percorreu as DOZE bancadas da Fábrica de IA, da adivinhação de"
                : $"trabalhou em {feitas} das doze bancadas da Fábrica de IA, da adivinhação de";

            pdf.TextoCentrado(linha, Centro, 224f, 11.5f, Pdf.Normal, Tinta);
            pdf.TextoCentrado("palavras até o alinhamento de respostas, construindo em cada uma",
                              Centro, 240f, 11.5f, Pdf.Normal, Tinta);
            pdf.TextoCentrado("delas uma peça do funcionamento da máquina.",
                              Centro, 256f, 11.5f, Pdf.Normal, Tinta);
        }

        // ------------------------------------------------------------- o painel

        static void Painel(Pdf pdf, Progresso p)
        {
            const float topo = 296f;
            const float altoDaLinha = 20.5f;

            pdf.Texto("Resultados por bancada", Margem, topo, 13f, Pdf.Negrito, Tinta);

            // Cabeçalho de coluna. As posições ficam em constantes locais porque
            // cabeçalho e célula TÊM que sair do mesmo número — foi assim que a
            // primeira versão saiu com a coluna de tempo desalinhada do título.
            var xEstrelas = Margem + 300f;
            var xTempo = Pdf.Largura - Margem;

            pdf.Texto("bancada", Margem, topo + 20f, 9f, Pdf.Negrito, Cinza);
            pdf.Texto("estrelas", xEstrelas, topo + 20f, 9f, Pdf.Negrito, Cinza);
            pdf.TextoADireita("tempo", xTempo, topo + 20f, 9f, Pdf.Negrito, Cinza);
            pdf.Linha(Margem, topo + 33f, xTempo, topo + 33f, 0.7f, Cinza);

            var y = topo + 38f;
            for (var i = 1; i <= 12; i++)
            {
                var etapa = $"e{i}";
                var mestre = Elenco.De(etapa);
                var estrelas = p.Estrelas(etapa);
                var segundos = p.Segundos(etapa);

                // Zebra: uma faixa clara em linha alternada. Numa tabela de doze
                // linhas com três colunas, é o que impede o olho de pular de linha
                // ao atravessar o vão do meio.
                if (i % 2 == 0)
                    pdf.Retangulo(Margem - 4f, y - 3f, Miolo + 8f, altoDaLinha, Palha);

                pdf.Texto($"{i}.", Margem, y, 10f, Pdf.Negrito, Cinza);
                pdf.Texto(mestre != null ? mestre.Titulo : etapa,
                          Margem + 18f, y, 10.5f, Pdf.Normal, Tinta);
                pdf.Texto(mestre != null ? mestre.Nome : string.Empty,
                          Margem + 200f, y, 9f, Pdf.Normal, Cinza);

                Estrelas(pdf, xEstrelas, y, estrelas);

                // Três estados, e não dois. A versão anterior escrevia "não
                // concluída" sempre que faltasse estrela — e com isso APAGAVA o
                // tempo de quem entrou, lutou seis minutos e saiu sem resolver.
                // Numa folha que diz o que o aluno fez, gastar aula é fato, não
                // ausência: quem tentou e não deu conta fez mais do que quem não
                // chegou lá, e a folha tem que saber a diferença.
                var visitou = p.Tentada(etapa);
                pdf.TextoADireita(
                    !visitou ? "não visitou"
                             : estrelas > 0 ? Duracao(segundos)
                                            : $"{Duracao(segundos)} · sem estrela",
                    xTempo, y, 9.5f, Pdf.Normal,
                    estrelas > 0 ? Tinta : Cinza);

                y += altoDaLinha;
            }

            pdf.Linha(Margem, y + 2f, xTempo, y + 2f, 0.7f, Cinza);
            Totais(pdf, p, y + 14f);
        }

        /// <summary>
        /// Três quadradinhos: cheios são as estrelas ganhas.
        ///
        /// Quadrado e não estrela porque o caractere ★ não existe na codificação
        /// WinAnsi das fontes base-14, e sairia como um losango de erro. Desenhar a
        /// forma custa duas linhas e não depende de fonte nenhuma.
        /// </summary>
        static void Estrelas(Pdf pdf, float x, float y, int quantas)
        {
            const float lado = 8f;
            const float vao = 4f;

            for (var i = 0; i < 3; i++)
            {
                var esquerda = x + i * (lado + vao);
                if (i < quantas) pdf.Retangulo(esquerda, y + 1f, lado, lado, Ouro);
                else pdf.Moldura(esquerda + 0.5f, y + 1.5f, lado - 1f, lado - 1f, 0.7f, Cinza);
            }
        }

        static void Totais(Pdf pdf, Progresso p, float y)
        {
            var completa = p.AulaCompleta;

            pdf.Retangulo(Margem - 4f, y, Miolo + 8f, 58f, Palha);
            pdf.Moldura(Margem - 4f, y, Miolo + 8f, 58f, 0.7f, completa ? Verde : Cinza);

            pdf.Texto(completa ? "Aula completa" : "Aula em andamento",
                      Margem + 8f, y + 8f, 12f, Pdf.Negrito, completa ? Verde : Cinza);

            // Visitadas ao lado de concluídas, e não no lugar dela: são duas
            // perguntas diferentes que o professor faz da mesma folha — quanto da
            // aula esta pessoa percorreu, e quanto dela ela venceu.
            pdf.Texto($"bancadas concluídas:  {p.Concluidas} de 12" +
                      $"      visitadas:  {p.Tentadas} de 12",
                      Margem + 8f, y + 28f, 10f, Pdf.Normal, Tinta);
            pdf.Texto($"estrelas:  {p.EstrelasTotais} de 36",
                      Margem + 8f, y + 42f, 10f, Pdf.Normal, Tinta);

            pdf.TextoADireita($"tempo somado nas bancadas:  {Duracao(p.SegundosTotais)}",
                              Pdf.Largura - Margem - 8f, y + 28f, 10f, Pdf.Normal, Tinta);
            // "encerrado" só quando encerrou. Numa aula pela metade a data é a de
            // hoje, e chamá-la de encerramento seria o documento afirmando uma coisa
            // que a linha de cima acabou de negar.
            pdf.TextoADireita(completa
                                  ? $"encerrado em  {Quando(p.terminouEm)}"
                                  : $"folha gerada em  {Quando(null)}",
                              Pdf.Largura - Margem - 8f, y + 42f, 10f, Pdf.Normal, Tinta);
        }

        // ------------------------------------------------------------- o rodapé

        static void Rodape(Pdf pdf, Progresso p)
        {
            const float y = 762f;

            pdf.Linha(Margem, y, Pdf.Largura - Margem, y, 0.7f, Cinza);

            pdf.Texto($"código de conferência   {Codigo(p)}", Margem, y + 8f, 10f,
                      Pdf.Negrito, Tinta);

            // O que o código é e o que ele NÃO é, escrito no próprio documento.
            // Ele nasce do nome mais os resultados, então dois alunos não podem ter
            // o mesmo, e um arquivo que teve o nome trocado passa a discordar dele —
            // o que pega a cópia distraída. Não é assinatura digital, e prometer
            // isso num diploma escolar seria mentira impressa.
            pdf.Texto("Nasce do nome e dos resultados desta folha: outra pessoa, ou outro " +
                      "resultado, dá outro código.",
                      Margem, y + 24f, 8.5f, Pdf.Normal, Cinza);
            pdf.Texto("Gerado pelo jogo no computador do próprio aluno — nenhum dado foi " +
                      "enviado para servidor algum.",
                      Margem, y + 36f, 8.5f, Pdf.Normal, Cinza);
        }

        // ------------------------------------------------------------- miudezas

        /// <summary>
        /// Um código curto tirado do conteúdo da folha, por FNV-1a.
        ///
        /// Cinco grupos de quatro caracteres num alfabeto sem vogal e sem os
        /// parecidos (0/O, 1/I): assim o código nunca forma palavra por acidente e
        /// ninguém erra ao copiar à mão.
        /// </summary>
        static string Codigo(Progresso p)
        {
            var material = new StringBuilder();
            material.Append(p.nome?.Trim().ToLowerInvariant()).Append('|')
                    .Append(p.turma?.Trim().ToLowerInvariant()).Append('|')
                    .Append(p.terminouEm).Append('|');

            for (var i = 1; i <= 12; i++)
                material.Append(p.Estrelas($"e{i}")).Append(':')
                        .Append(p.Segundos($"e{i}")).Append(',');

            var hash = 2166136261u;
            foreach (var c in material.ToString())
            {
                hash ^= c;
                hash *= 16777619u;
            }

            const string alfabeto = "23456789BCDFGHJKLMNPQRSTVWXZ";
            var codigo = new StringBuilder();
            for (var i = 0; i < 12; i++)
            {
                if (i > 0 && i % 4 == 0) codigo.Append('-');
                codigo.Append(alfabeto[(int)(hash % (uint)alfabeto.Length)]);
                // Reembaralha entre dígitos: sem isto, os doze caracteres sairiam
                // de uma divisão só e o código teria muito menos variedade real.
                hash = hash * 16777619u + 0x9E3779B9u;
            }
            return codigo.ToString();
        }

        public static string Duracao(int segundos)
        {
            if (segundos <= 0) return "—";
            if (segundos < 60) return $"{segundos} s";

            var minutos = segundos / 60;
            var resto = segundos % 60;
            return resto == 0 ? $"{minutos} min" : $"{minutos} min {resto} s";
        }

        static string Quando(string iso)
        {
            if (DateTime.TryParse(iso, CultureInfo.InvariantCulture,
                                  DateTimeStyles.None, out var quando))
                return quando.ToString("dd/MM/yyyy 'às' HH:mm", CultureInfo.InvariantCulture);

            return DateTime.Now.ToString("dd/MM/yyyy 'às' HH:mm", CultureInfo.InvariantCulture);
        }

        /// <summary>Nome de arquivo sem acento, espaço nem surpresa para o sistema.</summary>
        static string Higienizar(string bruto)
        {
            var limpo = new StringBuilder();
            foreach (var c in bruto.Trim().ToLowerInvariant())
            {
                if (c >= 'a' && c <= 'z' || c >= '0' && c <= '9') limpo.Append(c);
                else if ("áàâãä".IndexOf(c) >= 0) limpo.Append('a');
                else if ("éèêë".IndexOf(c) >= 0) limpo.Append('e');
                else if ("íìîï".IndexOf(c) >= 0) limpo.Append('i');
                else if ("óòôõö".IndexOf(c) >= 0) limpo.Append('o');
                else if ("úùûü".IndexOf(c) >= 0) limpo.Append('u');
                else if (c == 'ç') limpo.Append('c');
                else if (limpo.Length > 0 && limpo[^1] != '-') limpo.Append('-');
            }
            return limpo.ToString().Trim('-');
        }
    }
}
