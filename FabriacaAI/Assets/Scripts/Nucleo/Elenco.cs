using System.Collections.Generic;

namespace FabricaDeIA.Nucleo
{
    /// <summary>Um mestre e as três falas que ele tem direito de dizer.</summary>
    public class Mestre
    {
        public string Etapa;
        public string Nome;
        public string Oficio;
        /// <summary>O que ele diz antes do desafio. No máximo duas.</summary>
        public string[] Antes;
        /// <summary>O que ele diz quando o aluno volta com a peça pronta.</summary>
        public string Depois;
        /// <summary>O nome da bancada na HUD. Curto: cabe numa linha.</summary>
        public string Titulo;
    }

    /// <summary>
    /// Os doze mestres do ateliê.
    ///
    /// Cada um domina uma etapa da reconstrução da máquina e fala como quem faz
    /// aquilo com as mãos — a costureira fala de corte, o cozinheiro fala do que
    /// a máquina come. É o que deixa a explicação caber em duas linhas: o ofício
    /// da pessoa já carrega metade do conceito.
    ///
    /// REGRA DE TEXTO, herdada de dois playtests reprovados: no máximo duas
    /// falas antes do desafio e uma depois, cada uma de até nove palavras. Cena
    /// de visual novel é a desculpa clássica para escrever demais, e esta turma
    /// lê pouco. Se um conceito não cabe aqui, ele pertence à bancada, não ao
    /// diálogo.
    /// </summary>
    public static class Elenco
    {
        public static readonly IReadOnlyList<Mestre> Todos = new[]
        {
            new Mestre
            {
                Etapa = "e1", Nome = "Tico", Oficio = "aprendiz da estufa",
                Titulo = "Adivinhar a próxima palavra",
                Antes = new[] { "Você também completa a frase sozinho.", "Repara." },
                Depois = "Você acertou sem tabela nenhuma. Ele não."
            },
            new Mestre
            {
                Etapa = "e2", Nome = "Dona Ciça", Oficio = "guarda-livros",
                Titulo = "Contar risquinhos",
                Antes = new[] { "Eu não adivinho. Eu conto risquinho.", "Marque comigo." },
                Depois = "Pronto. Uma máquina de escrever feita de contagem."
            },
            new Mestre
            {
                Etapa = "e3", Nome = "Mestre Aurélio", Oficio = "arquivista",
                Titulo = "A tabela que não cabe",
                Antes = new[] { "Meu arquivo tem buraco.", "Veja o tamanho dele." },
                Depois = "Contar não basta. Falta caber."
            },
            new Mestre
            {
                Etapa = "e4", Nome = "Nara", Oficio = "costureira",
                Titulo = "Cortar o texto em fichas",
                Antes = new[] { "Texto não entra inteiro.", "Escolha por onde cortar." },
                Depois = "Bom corte. Nenhuma palavra ficou de fora."
            },
            new Mestre
            {
                Etapa = "e5", Nome = "Bento", Oficio = "cartógrafo de jardim",
                Titulo = "O mapa das palavras",
                Antes = new[] { "Toda palavra tem lugar.", "Junte as parecidas." },
                Depois = "Seu mapa e o dela nasceram do mesmo texto."
            },
            new Mestre
            {
                Etapa = "e6", Nome = "Iara", Oficio = "tecelã de circuitos",
                Titulo = "Tecer a malha",
                Antes = new[] { "Aqui a gente tece a malha dela.", "Puxe uma camada." },
                Depois = "Tem a forma certa. E não sabe nada ainda."
            },
            new Mestre
            {
                Etapa = "e7", Nome = "Seu Ilo", Oficio = "afinador",
                Titulo = "O tamanho do erro",
                Antes = new[] { "Todo erro tem tamanho.", "Tente diminuir na mão." },
                Depois = "Cansou, né? São milhares de botões."
            },
            new Mestre
            {
                Etapa = "e8", Nome = "Rosa", Oficio = "treinadora",
                Titulo = "Deixar a máquina treinar",
                Antes = new[] { "Agora ela desce sozinha.", "Olhe a frase mudando." },
                Depois = "Ela aprendeu. Você não girou nada."
            },
            new Mestre
            {
                Etapa = "e9", Nome = "Chef Amaro", Oficio = "cozinheiro",
                Titulo = "O que ela come",
                Antes = new[] { "Ela vira o que ela come.", "Escolha o prato." },
                Depois = "Mesma máquina. Outro texto. Outra boca."
            },
            new Mestre
            {
                Etapa = "e10", Nome = "Lumi", Oficio = "acendedora de lampiões",
                Titulo = "Onde ela olha",
                Antes = new[] { "Ela só enxerga um pedaço.", "Aponte o holofote." },
                Depois = "Onde ela olha muda tudo."
            },
            new Mestre
            {
                Etapa = "e11", Nome = "Vovó Zi", Oficio = "contadora de histórias",
                Titulo = "Fazer ela falar",
                Antes = new[] { "Falar é sortear a próxima palavra.", "Escreve comigo." },
                Depois = "Nada de mágica. Sorteio, e de novo."
            },
            new Mestre
            {
                Etapa = "e12", Nome = "Sereno", Oficio = "guardião do ateliê",
                Titulo = "Ensinar modos a ela",
                Antes = new[] { "Falta ela aprender modos.", "Diga do que você gosta." },
                Depois = "Mesma rede. Você mudou quem escolhe."
            },
            // A décima terceira estação não é bancada: é o balcão onde o aluno
            // retira o certificado, e onde a máquina da abertura reaparece —
            // agora montada com as peças que ele foi buscar. Entra no elenco
            // porque o salão anuncia toda estação pelo nome de quem a atende.
            new Mestre
            {
                Etapa = "e13", Nome = "Mestra Aurora", Oficio = "guarda da máquina",
                Titulo = "Retirar o certificado",
                Antes = new[] { "Ela ainda está em pedaços.", "Cada bancada devolve uma peça." },
                Depois = "Leve o papel. O trabalho foi seu."
            }
        };

        public static Mestre De(string etapa)
        {
            foreach (var m in Todos)
                if (m.Etapa == etapa) return m;
            return null;
        }
    }
}
