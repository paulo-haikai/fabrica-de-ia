using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Engine;
using FabricaDeIA.Nucleo;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Bancada 12 — o guardião Sereno.
    ///
    /// Inspiração: PAPERS, PLEASE. Você julga caso por caso, sem regra escrita, e
    /// o jogo vai revelando que as suas decisões formaram uma política — que
    /// depois é cobrada de você.
    ///
    /// Aqui o aluno vê pares de respostas da mesma máquina para a mesma pergunta e
    /// escolhe qual prefere. Nenhuma das duas está errada; elas diferem em
    /// TEMPERAMENTO — mais curta ou mais longa, mais cautelosa ou mais direta,
    /// mais formal ou mais próxima. Depois de cinco escolhas, a máquina responde
    /// uma pergunta NOVA usando a política que ele construiu sem perceber, e ele
    /// julga se acertou o gosto dele.
    ///
    /// É RLHF na sua forma mais simples e honesta: ninguém programou a
    /// personalidade da máquina, ninguém escreveu uma regra de conduta. Alguém
    /// escolheu, muitas vezes, entre duas respostas — e a soma dessas escolhas
    /// virou o jeito dela.
    ///
    /// A lição que fecha o ateliê tem uma parte incômoda de propósito: se o jeito
    /// da máquina vem de quem escolheu, então a pergunta que importa sobre
    /// qualquer IA não é "como ela funciona", é "quem escolheu, e o que essa
    /// pessoa preferia".
    /// </summary>
    public partial class DesafioAlinhar : DesafioEmNiveis
    {
        public override string Etapa => "e12";
        public override string Titulo => "Ensinar modos a ela";

        protected override int Niveis => 2;

        /// <summary>
        /// Os três eixos de temperamento. Cada par de respostas difere num deles,
        /// e a escolha do aluno empurra a agulha para um lado.
        /// </summary>
        enum Eixo { Tamanho, Cautela, Formalidade }

        class Caso
        {
            public string Pergunta;
            public Eixo Eixo;
            /// <summary>Resposta do lado "mais": mais longa, mais cautelosa, mais formal.</summary>
            public string Mais;
            /// <summary>Resposta do lado "menos".</summary>
            public string Menos;
        }

        static readonly Caso[] Casos =
        {
            new()
            {
                Pergunta = "Quanto é 7 vezes 8?",
                Eixo = Eixo.Tamanho,
                Mais = "São 56. Se quiser conferir: 7 vezes 8 é o mesmo que 7 vezes 4, " +
                       "que dá 28, somado com ele mesmo — 28 mais 28, 56.",
                Menos = "56."
            },
            new()
            {
                Pergunta = "Essa planta da horta vai sobreviver ao inverno?",
                Eixo = Eixo.Cautela,
                Mais = "Não dá para afirmar sem saber a espécie e o quanto esfria aí. " +
                       "Se me disser as duas coisas, eu arrisco um palpite.",
                Menos = "Vai sobreviver."
            },
            new()
            {
                Pergunta = "Pode revisar a minha redação?",
                Eixo = Eixo.Formalidade,
                Mais = "Com certeza. Peço que envie o texto e eu apresento as sugestões " +
                       "de correção organizadas por parágrafo.",
                Menos = "Claro, manda aí. Eu vou apontando o que der para melhorar."
            },
            new()
            {
                Pergunta = "Por que o sinal toca no fim da aula?",
                Eixo = Eixo.Tamanho,
                Mais = "Para marcar a troca de horário. Sem um aviso comum, cada turma " +
                       "sairia num momento diferente e os professores não conseguiriam " +
                       "trocar de sala na hora certa.",
                Menos = "Para avisar que a aula acabou."
            },
            new()
            {
                Pergunta = "Qual é a resposta da questão 4?",
                Eixo = Eixo.Cautela,
                Mais = "Antes de responder: você quer a resposta ou quer que eu te ajude " +
                       "a chegar nela? As duas coisas servem, mas ajudam de formas diferentes.",
                Menos = "É a letra C."
            },
            new()
            {
                Pergunta = "Errei a prova toda. E agora?",
                Eixo = Eixo.Formalidade,
                Mais = "Compreendo a preocupação. Sugiro identificar quais conteúdos " +
                       "concentraram os erros e revisá-los antes da próxima avaliação.",
                Menos = "Acontece. Vamos ver onde você tropeçou e arrumar isso."
            }
        };

        /// <summary>A pergunta do teste final, com as quatro respostas possíveis.</summary>
        static readonly string PerguntaFinal = "Não entendi a matéria de hoje. Você me explica?";

        readonly Dictionary<Eixo, int> _agulha = new();
        List<Caso> _rodada;
        int _casoAtual;
        bool _testando;

        RectTransform _mesa;
        Mulberry32 _sorteio;

        protected override void Preparar()
        {
            _sorteio = new Mulberry32((uint)Rodadas.Semente());
            foreach (Eixo e in System.Enum.GetValues(typeof(Eixo))) _agulha[e] = 0;
        }

        protected override void MontarNivel()
        {
            _casoAtual = 0;
            _testando = NivelAtual == 1;

            _mesa = Widgets.Painel("Mesa", Area, Color.clear);
            Widgets.Esticar(_mesa);

            if (_testando)
            {
                MontarTeste();
                return;
            }

            // Três casos por rodada, sorteados: um de cada eixo, para a política
            // sair equilibrada em vez de medir só uma coisa.
            _rodada = System.Enum.GetValues(typeof(Eixo))
                .Cast<Eixo>()
                .Select(eixo => Embaralhar(Casos.Where(c => c.Eixo == eixo).ToList()).First())
                .ToList();
            _rodada = Embaralhar(_rodada);

            Painel.Rodape("nenhuma das duas está errada · escolha a que você prefere");
            MontarCaso();
        }

        List<T> Embaralhar<T>(List<T> fonte)
        {
            var copia = new List<T>(fonte);
            for (var i = copia.Count - 1; i > 0; i--)
            {
                var j = (int)(_sorteio.Proximo() * (i + 1));
                (copia[i], copia[j]) = (copia[j], copia[i]);
            }
            return copia;
        }

        // ---------------------------------------------------------- julgamento

        void MontarCaso()
        {
            foreach (Transform filho in _mesa) Destroy(filho.gameObject);

            var caso = _rodada[_casoAtual];
            Painel.MarcarPasso(_casoAtual + 1, _rodada.Count);
            Painel.Instruir("qual das duas você prefere?");
            Painel.Acao(null, null);

            var pergunta = Widgets.Texto("Pergunta", _mesa, 19, TextAnchor.UpperCenter, Cores.Luz);
            Widgets.Faixa(pergunta.rectTransform, true, 30f);
            pergunta.text = $"“{caso.Pergunta}”";

            // Os lados aparecem em ordem sorteada: se "mais" ficasse sempre à
            // esquerda, o aluno acabaria escolhendo por posição e a política
            // mediria a mão dele, não o gosto.
            var maisAEsquerda = _sorteio.Proximo() < 0.5f;
            Resposta(caso, maisAEsquerda ? caso.Mais : caso.Menos, -1f, maisAEsquerda ? +1 : -1);
            Resposta(caso, maisAEsquerda ? caso.Menos : caso.Mais, +1f, maisAEsquerda ? -1 : +1);
        }

        void Resposta(Caso caso, string texto, float lado, int empurrao)
        {
            var botao = Widgets.Botao($"r{lado}", _mesa, string.Empty,
                                      Cores.TintaClara, Cores.Papel);
            Widgets.Fixar((RectTransform)botao.transform, new Vector2(0.5f, 0.5f),
                          new Vector2(lado * 224f, -6f), new Vector2(430f, 210f));
            Destroy(botao.GetComponentInChildren<Text>().gameObject);

            var corpo = Widgets.Texto("t", (RectTransform)botao.transform, 16,
                                      TextAnchor.UpperLeft, Cores.Papel);
            Widgets.Esticar(corpo.rectTransform, 18f);
            corpo.text = texto;

            botao.onClick.AddListener(() => Julgar(caso.Eixo, empurrao));
        }

        void Julgar(Eixo eixo, int empurrao)
        {
            _agulha[eixo] += empurrao;
            _casoAtual++;

            if (_casoAtual < _rodada.Count)
            {
                MontarCaso();
                return;
            }

            Resolveu("Três escolhas feitas",
                "Você não escreveu regra nenhuma. Não disse “seja breve” nem\n" +
                "“seja cuidadosa” — só escolheu, três vezes, entre duas respostas.\n\n" +
                "Sereno anotou. Na próxima ele responde uma pergunta nova\n" +
                "do jeito que você escolheu.");
        }

        // ------------------------------------------------------------- o teste

        void MontarTeste()
        {
            Painel.Rodape("a resposta abaixo foi montada com as suas escolhas");
            Painel.MarcarPasso(string.Empty);
            Painel.Instruir("uma pergunta que você nunca julgou");

            var pergunta = Widgets.Texto("Pergunta", _mesa, 19, TextAnchor.UpperCenter, Cores.Luz);
            Widgets.Faixa(pergunta.rectTransform, true, 30f);
            pergunta.text = $"“{PerguntaFinal}”";

            var caixa = Widgets.Painel("Resposta", _mesa, Cores.TintaClara);
            Widgets.Fixar(caixa, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f),
                          new Vector2(760f, 160f));

            var corpo = Widgets.Texto("t", caixa, 17, TextAnchor.UpperLeft, Cores.Papel);
            Widgets.Esticar(corpo.rectTransform, 20f);
            corpo.text = Montar();

            var perfil = Widgets.Texto("Perfil", _mesa, 15, TextAnchor.LowerCenter, Cores.Neblina);
            Widgets.Faixa(perfil.rectTransform, false, 60f, 8f);
            perfil.text = "o jeito que as suas escolhas montaram:\n" + Perfil();

            Painel.Acao("é o meu jeito, sim", () => Fechar(true));

            var recusa = Widgets.Botao("Recusa", _mesa, "não, não é assim que eu falo",
                                       Cores.Madeira, Cores.Papel, 15);
            Widgets.Fixar((RectTransform)recusa.transform, new Vector2(0.5f, 0f),
                          new Vector2(0f, 12f), new Vector2(320f, 38f));
            recusa.onClick.AddListener(() => Fechar(false));
        }

        /// <summary>
        /// Monta a resposta final juntando os pedaços que cada eixo escolhido pede.
        ///
        /// É simplificado — três eixos, dois lados cada — e é honesto sobre a
        /// mecânica que representa: o modelo não ganhou regras, ganhou uma
        /// preferência agregada, e responde conforme ela.
        /// </summary>
        string Montar()
        {
            var formal = _agulha[Eixo.Formalidade] > 0;
            var cautelosa = _agulha[Eixo.Cautela] > 0;
            var longa = _agulha[Eixo.Tamanho] > 0;

            var abertura = formal
                ? "Certamente. Podemos revisar o conteúdo juntos."
                : "Claro, vamos junto.";

            var pergunta = cautelosa
                ? (formal
                    ? " Antes disso, gostaria de saber qual parte ficou confusa."
                    : " Só me diz qual parte embolou?")
                : string.Empty;

            var corpo = longa
                ? (formal
                    ? " Sugiro começarmos pelo conceito principal e, em seguida, " +
                      "aplicá-lo a um exercício, para verificar se ficou claro."
                    : " A gente começa pelo começo, faz um exercício junto, " +
                      "e você vê se encaixou.")
                : string.Empty;

            return abertura + pergunta + corpo;
        }

        string Perfil()
        {
            string Lado(Eixo eixo, string mais, string menos, string meio) =>
                _agulha[eixo] > 0 ? mais : _agulha[eixo] < 0 ? menos : meio;

            return string.Join("   ·   ", new[]
            {
                Lado(Eixo.Tamanho, "explica com calma", "vai direto ao ponto", "meio a meio"),
                Lado(Eixo.Cautela, "pergunta antes", "responde de primeira", "meio a meio"),
                Lado(Eixo.Formalidade, "trata com formalidade", "fala como colega", "meio a meio")
            });
        }

        void Fechar(bool reconheceu)
        {
            // A persona atravessa a aula: fica guardada como o resultado do ateliê.
            Progresso.Atual.preferencias = new[]
            {
                (float)_agulha[Eixo.Tamanho],
                _agulha[Eixo.Cautela],
                _agulha[Eixo.Formalidade]
            };
            Progresso.Atual.Salvar();

            var comum = reconheceu
                ? "Você reconheceu o jeito dela porque o jeito é seu.\n\n"
                : "Não ficou igual ao que você faria — três escolhas são pouco\n" +
                  "para capturar uma pessoa. Modelos de verdade usam milhões.\n\n";

            Resolveu(reconheceu ? "É o seu jeito" : "Quase o seu jeito",
                comum +
                "Ninguém programou a personalidade dela. Alguém escolheu,\n" +
                "muitas vezes, entre duas respostas — a soma virou o jeito dela.\n\n" +
                "Isso tem nome: aprendizado por preferência humana. E deixa\n" +
                "uma pergunta melhor que “como a IA funciona”: quem escolheu,\n" +
                "e o que essa pessoa preferia?");
        }
    }
}
