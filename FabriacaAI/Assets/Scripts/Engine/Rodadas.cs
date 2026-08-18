using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FabricaDeIA.Engine
{
    /// <summary>
    /// Uma rodada do jogo de adivinhar. <see cref="Contexto"/> vazio significa
    /// palavra solta — sem pista nenhuma.
    /// </summary>
    public readonly struct Rodada
    {
        public readonly string Contexto;
        public readonly string Resposta;

        public Rodada(string contexto, string resposta)
        {
            Contexto = contexto;
            Resposta = resposta;
        }

        public bool TemContexto => !string.IsNullOrEmpty(Contexto);
    }

    /// <summary>
    /// O material da bancada 1, em duas metades.
    ///
    /// DIVERSÃO PRIMEIRO, TEORIA DEPOIS. As três primeiras rodadas são palavra
    /// solta: nenhuma pista, só o jogo de adivinhar, que qualquer adolescente já
    /// sabe jogar e joga com gosto. Só na quarta entra a frase.
    ///
    /// A ordem não é enfeite — é o argumento inteiro da etapa. Quem começa com
    /// contexto nunca sente falta dele. Quem passa três rodadas chutando às
    /// cegas e então ganha uma frase SENTE o contexto trabalhando: de repente
    /// acerta em duas tentativas o que antes custava cinco. Essa diferença, que
    /// o próprio aluno produziu, é o que a máquina faz o tempo todo. No fim a
    /// tela mostra os dois números lado a lado, e a teoria chega como
    /// explicação de algo já vivido, não como aviso prévio.
    ///
    /// SORTEIO SEMPRE: a turma joga junta, trinta máquinas lado a lado no mesmo
    /// minuto. Se as palavras se repetissem — entre alunos ou entre partidas do
    /// mesmo aluno — a resposta vazaria pela sala em trinta segundos. Cada
    /// abertura da bancada sorteia de novo.
    /// </summary>
    public static class Rodadas
    {
        public const int SemContexto = 3;
        public const int ComContexto = 3;
        public const int Total = SemContexto + ComContexto;

        /// <summary>
        /// O saco de palavras das rodadas sem pista.
        ///
        /// São substantivos concretos do universo escolar, todos presentes no
        /// corpus — o mesmo corpus que valida os palpites, senão o aluno digita
        /// uma palavra legítima e o jogo a recusa.
        ///
        /// Ficaram de fora os verbos no passado, que o corpus tem às dezenas
        /// (avisou, apitou, ajudou, molhou…). Sem contexto, adivinhar qual dos
        /// quarenta verbos terminados em -ou é o certo não é desafio, é loteria
        /// — e loteria não diverte ninguém.
        /// </summary>
        static readonly string[] Palavras =
        {
            // quatro letras
            "arte", "aula", "bola", "café", "casa", "cola", "data", "fila", "hora",
            "jogo", "mapa", "mesa", "nota", "sala", "suco", "tela", "tema", "time", "água",

            // cinco letras
            "aluna", "aluno", "arroz", "banco", "chuva", "conta", "feira", "festa",
            "folha", "fruta", "grupo", "horta", "lista", "livro", "lição", "lousa",
            "lápis", "manhã", "museu", "noite", "papel", "porta", "praça", "prova",
            "pátio", "régua", "sinal", "tarde", "tempo", "tinta", "turma", "vento", "vídeo",

            // seis letras
            "amigos", "banana", "brasil", "caneta", "cartaz", "cidade", "colega",
            "escola", "estojo", "feijão", "física", "inglês", "janela", "lanche",
            "música", "parede", "planta", "portão", "página", "quadra", "quadro",
            "tarefa", "árvore", "ônibus",

            // sete letras
            "cadeira", "caderno", "colegas", "cozinha", "desenho", "diretor",
            "estante", "matéria", "merenda", "mochila", "passeio", "recreio",
            "relógio", "reunião", "tesoura", "torcida"
        };

        /// <summary>
        /// As frases das rodadas com pista.
        ///
        /// Todas com oração subordinada, de propósito. Contexto curto vira
        /// adivinhação de vocabulário; com a frase longa o aluno precisa ler até
        /// o fim para achar a palavra — que é exatamente o trabalho que uma LLM
        /// faz a cada palavra que escreve.
        /// </summary>
        static readonly Rodada[] ComPista =
        {
            new("no recreio, assim que o sinal tocou, os alunos correram para jogar", "bola"),
            new("o aluno que chegou atrasado largou a mochila e deixou o caderno em cima da", "mesa"),
            new("depois de corrigir os exercícios da semana, o professor entregou a cada um a sua", "nota"),
            new("na hora do lanche, quem saiu da sala primeiro ficou no começo da", "fila"),
            new("para achar em que continente ficava o rio da pergunta, a turma abriu o", "mapa"),
            new("no intervalo da tarde, quem não gostava de café acabou pedindo um copo de", "suco"),

            new("antes de começar a explicar a matéria nova, a professora escreveu a data na", "lousa"),
            new("o professor esperou de braços cruzados até conseguir a atenção de toda a", "turma"),
            new("quem tinha estudado a semana inteira chegou tranquilo no dia da", "prova"),
            new("como queria saber o final da história antes da aula acabar, a aluna abriu o", "livro"),
            new("para traçar a linha reta do desenho sem tremer, o aluno pegou a", "régua"),
            new("todo mundo parou de escrever no meio da frase quando tocou o", "sinal"),
            new("como a sala estava quente demais naquela tarde, a aula acabou sendo no", "pátio"),
            new("para poder apagar se errasse a conta, ele preferiu fazer tudo a", "lápis"),

            new("o time de futebol da escola treinou a tarde inteira debaixo de sol na", "quadra"),
            new("para entrar um pouco de ar na sala abafada, o aluno da frente abriu a", "janela"),
            new("como a ponta do lápis quebrou no meio da prova, ele terminou tudo de", "caneta"),
            new("quando a turma acabou de ler o capítulo, a professora mandou virar a", "página"),
            new("no pátio, para fugir do sol do meio dia, a turma sentou na sombra da", "árvore"),
            new("o professor apagou a conta errada e começou tudo de novo no", "quadro"),
            new("as férias tinham acabado e na segunda de manhã todos voltaram para a", "escola"),

            new("quando a última aula terminou, cada aluno guardou o material e fechou a", "mochila"),
            new("para copiar a matéria que estava na lousa antes que apagassem, o aluno abriu o", "caderno")
        };

        /// <summary>
        /// Uma semente nova a cada visita à bancada.
        ///
        /// A primeira versão gravava a semente no navegador, para o aluno rever
        /// as mesmas palavras se recarregasse a página. Estava errado, e o motivo
        /// é a sala de aula: trinta alunos jogam ao MESMO tempo, lado a lado, e
        /// quem repete a rodada com as mesmas palavras dá a resposta pronta para
        /// o vizinho — inclusive para si mesmo, o que apaga o desafio na segunda
        /// tentativa.
        ///
        /// Sortear sempre custa o quê? Que recarregar a página troque as
        /// palavras. É um preço barato: ninguém perde progresso, porque estrela
        /// conquistada fica guardada; perde-se só a repetição, que era justamente
        /// o problema.
        /// </summary>
        public static int Semente() => Aleatoria();

        /// <summary>
        /// Semente de verdade, boa o bastante para trinta navegadores abrindo o
        /// mesmo jogo no mesmo minuto.
        ///
        /// Só o relógio não serve: numa sala, as máquinas ligam juntas e os
        /// relógios batem no mesmo segundo. Misturamos o relógio de alta
        /// resolução com o <c>Random</c> do Unity (que já nasce semeado por
        /// processo) e com o tempo desde a inicialização, que varia com o
        /// hardware de cada um.
        /// </summary>
        static int Aleatoria()
        {
            var relogio = (uint)System.DateTime.Now.Ticks;
            var sorte = (uint)Random.Range(int.MinValue, int.MaxValue);
            var desdeQueLigou = (uint)(Time.realtimeSinceStartupAsDouble * 1000000.0);

            var s = relogio ^ (sorte * 2654435761u) ^ (desdeQueLigou << 7);
            s ^= s >> 16;
            s *= 2246822519u;
            s ^= s >> 13;
            return (int)(s & 0x7fffffff) | 1;
        }

        /// <summary>Código curto da semente, para o professor ver na tela que variou.</summary>
        public static string Codigo(int semente)
        {
            const string alfabeto = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var texto = string.Empty;
            var n = (uint)semente;
            while (n > 0)
            {
                texto = alfabeto[(int)(n % 36)] + texto;
                n /= 36;
            }
            texto = texto.PadLeft(4, '0');
            return texto.Substring(texto.Length - 4);
        }

        /// <summary>
        /// As seis rodadas da aula: três sem pista, três com.
        ///
        /// Dentro de cada metade os tamanhos de palavra são diferentes entre si.
        /// Três palavras de quatro letras seguidas deixam a etapa curta e não
        /// mostram que palavra maior é mais fácil de cercar — que é meio caminho
        /// para entender por que o contexto ajuda.
        /// </summary>
        public static List<Rodada> Sortear(int semente)
        {
            var sorteio = new Mulberry32((uint)semente);

            var soltas = Embaralhar(
                Palavras.Where(p => Corpus.Vocabulario.Contains(p))
                        .Select(p => new Rodada(string.Empty, p))
                        .ToList(), sorteio);

            var comFrase = Embaralhar(
                ComPista.Where(r => Corpus.Vocabulario.Contains(r.Resposta)).ToList(), sorteio);

            var escolhidas = new List<Rodada>();
            escolhidas.AddRange(TamanhosVariados(soltas, SemContexto));
            escolhidas.AddRange(TamanhosVariados(comFrase, ComContexto));
            return escolhidas;
        }

        static List<T> Embaralhar<T>(List<T> lista, Mulberry32 sorteio)
        {
            for (var i = lista.Count - 1; i > 0; i--)
            {
                var j = (int)(sorteio.Proximo() * (i + 1));
                (lista[i], lista[j]) = (lista[j], lista[i]);
            }
            return lista;
        }

        static IEnumerable<Rodada> TamanhosVariados(List<Rodada> baralho, int quantas)
        {
            var escolhidas = new List<Rodada>();
            var tamanhos = new HashSet<int>();

            foreach (var r in baralho)
            {
                if (escolhidas.Count >= quantas) break;
                if (!tamanhos.Add(r.Resposta.Length)) continue;
                escolhidas.Add(r);
            }
            // Se não houver tamanhos suficientes, completa com o que sobrou.
            foreach (var r in baralho)
            {
                if (escolhidas.Count >= quantas) break;
                if (!escolhidas.Contains(r)) escolhidas.Add(r);
            }
            return escolhidas;
        }
    }

    /// <summary>
    /// Sorteio estável — o mesmo mulberry32 que a versão web usava.
    ///
    /// O <c>Random</c> do Unity não serve aqui: ele é global, e mudar a semente
    /// dele para sortear as palavras da aula bagunçaria o piscar das luzes e
    /// qualquer outro sorteio do jogo. Um gerador próprio custa dez linhas e não
    /// vaza para o resto.
    /// </summary>
    public class Mulberry32
    {
        uint _estado;

        public Mulberry32(uint semente) => _estado = semente;

        public float Proximo()
        {
            _estado += 0x6d2b79f5u;
            var t = _estado;
            t = (t ^ (t >> 15)) * (t | 1u);
            t ^= t + (t ^ (t >> 7)) * (t | 61u);
            return ((t ^ (t >> 14)) & 0xffffffu) / (float)0x1000000;
        }
    }
}
