using System;
using System.Collections.Generic;
using UnityEngine;

namespace FabricaDeIA.Engine
{
    /// <summary>
    /// A rede neural da bancada 6, e o passe adiante dela.
    ///
    /// Os pesos vêm treinados de fora, por <c>scripts/rede/treinar.mjs</c>. Aqui
    /// só se calcula — mas se calcula de verdade: a animação da bancada lê ESTAS
    /// ativações para saber o brilho de cada neurônio e de cada conexão. É a
    /// diferença entre ensinar e enfeitar. Se o brilho fosse inventado, a bancada
    /// estaria mentindo exatamente sobre a coisa que ela existe para mostrar.
    ///
    /// A forma é pequena porque ela precisa CABER NA TELA com as conexões
    /// desenhadas uma a uma: três palavras de entrada, seis números por palavra,
    /// doze neurônios no meio, e uma lâmpada por palavra do vocabulário.
    /// </summary>
    public class Rede
    {
        [Serializable] class Arquivo
        {
            public int janela;
            public int embutimento;
            public int meio;
            public string[] vocabulario;
            public float[] embutir;
            public float[] pesos1;
            public float[] vies1;
            public float[] pesos2;
            public float[] vies2;
            public float acerto;
            public float reguaBigrama;
        }

        static Rede _unica;

        /// <summary>A rede da aula. Carrega na primeira vez que alguém pede.</summary>
        public static Rede Atual => _unica ??= Carregar();

        readonly Arquivo _d;
        readonly Dictionary<string, int> _indice = new();

        public int Janela => _d.janela;
        public int Embutimento => _d.embutimento;
        public int Entradas => _d.janela * _d.embutimento;
        public int Meio => _d.meio;
        public int Palavras => _d.vocabulario.Length;
        public IReadOnlyList<string> Vocabulario => _d.vocabulario;

        /// <summary>Acerto medido no treino, e o do bigrama para comparação.</summary>
        public float Acerto => _d.acerto;
        public float ReguaDoBigrama => _d.reguaBigrama;

        /// <summary>A forma da rede, no formato que a bancada 7 espera ler.</summary>
        public int[] Forma => new[] { _d.meio };

        // --- o que o último passe deixou para trás, para a animação ler ---

        /// <summary>Os números que entraram: seis por palavra da janela.</summary>
        public float[] Entrada { get; private set; }
        /// <summary>A soma de cada neurônio do meio ANTES da ReLU.</summary>
        public float[] Somas { get; private set; }
        /// <summary>O que cada neurônio do meio soltou. Zero é neurônio apagado.</summary>
        public float[] Ativacoes { get; private set; }
        /// <summary>A probabilidade de cada palavra do vocabulário.</summary>
        public float[] Chances { get; private set; }

        Rede(Arquivo dados)
        {
            _d = dados;
            for (var i = 0; i < _d.vocabulario.Length; i++) _indice[_d.vocabulario[i]] = i;

            Entrada = new float[Entradas];
            Somas = new float[Meio];
            Ativacoes = new float[Meio];
            Chances = new float[Palavras];
        }

        static Rede Carregar()
        {
            var texto = Resources.Load<TextAsset>("Dados/rede");
            if (texto == null)
                throw new Exception("rede.json não achado — rode `node scripts/rede/treinar.mjs`");

            var dados = JsonUtility.FromJson<Arquivo>(texto.text);
            if (dados == null || dados.vocabulario == null || dados.vocabulario.Length == 0)
                throw new Exception("rede.json ilegível — rode `node scripts/rede/treinar.mjs`");

            var esperado = dados.janela * dados.embutimento * dados.meio;
            if (dados.pesos1.Length != esperado)
                throw new Exception($"rede.json inconsistente: {dados.pesos1.Length} pesos " +
                                    $"na primeira camada, esperava {esperado}");

            return new Rede(dados);
        }

        public bool Conhece(string palavra) => _indice.ContainsKey(palavra);

        public int Indice(string palavra) => _indice.TryGetValue(palavra, out var i) ? i : -1;

        public string Palavra(int indice) =>
            indice >= 0 && indice < _d.vocabulario.Length ? _d.vocabulario[indice] : null;

        /// <summary>Um dos seis números que representam a palavra.</summary>
        public float Embutido(int palavra, int k) => _d.embutir[palavra * _d.embutimento + k];

        /// <summary>O peso da conexão entre uma entrada e um neurônio do meio.</summary>
        public float Peso1(int entrada, int neuronio) => _d.pesos1[entrada * _d.meio + neuronio];

        /// <summary>O peso da conexão entre um neurônio do meio e a lâmpada de uma palavra.</summary>
        public float Peso2(int neuronio, int palavra) => _d.pesos2[neuronio * Palavras + palavra];

        /// <summary>
        /// Quanto esta conexão está de fato empurrando, agora.
        ///
        /// Peso não é a mesma coisa que empurrão: uma conexão de peso enorme
        /// carrega zero se o neurônio de origem estiver apagado. A animação
        /// precisa do EMPURRÃO — é ele que o aluno tem que ver.
        /// </summary>
        public float Empurrao1(int entrada, int neuronio) => Entrada[entrada] * Peso1(entrada, neuronio);

        public float Empurrao2(int neuronio, int palavra) => Ativacoes[neuronio] * Peso2(neuronio, palavra);

        // --------------------------------------------------------- passe adiante

        /// <summary>
        /// Roda a rede com uma janela de palavras e devolve o índice da vencedora.
        ///
        /// Depois disto, <see cref="Entrada"/>, <see cref="Somas"/>,
        /// <see cref="Ativacoes"/> e <see cref="Chances"/> descrevem exatamente o
        /// que aconteceu por dentro — é o que a animação percorre.
        /// </summary>
        public int Prever(IReadOnlyList<string> janela)
        {
            if (janela == null || janela.Count < _d.janela)
                throw new ArgumentException($"a rede precisa de {_d.janela} palavras");

            // Só as últimas: a janela desliza, e o que sobrou atrás dela a rede
            // simplesmente não vê. É o limite que a bancada 10 vai retomar.
            var inicio = janela.Count - _d.janela;

            for (var p = 0; p < _d.janela; p++)
            {
                var palavra = Indice(janela[inicio + p]);
                for (var k = 0; k < _d.embutimento; k++)
                    Entrada[p * _d.embutimento + k] = palavra < 0 ? 0f : Embutido(palavra, k);
            }

            for (var j = 0; j < Meio; j++)
            {
                var soma = _d.vies1[j];
                for (var i = 0; i < Entradas; i++) soma += Entrada[i] * Peso1(i, j);
                Somas[j] = soma;
                // ReLU. Soma negativa apaga o neurônio — e apagado ele não empurra
                // nada para frente. É por isso que pedaços da malha ficam escuros
                // na tela, e é verdade, não licença de desenho.
                Ativacoes[j] = soma > 0f ? soma : 0f;
            }

            var maior = float.NegativeInfinity;
            for (var v = 0; v < Palavras; v++)
            {
                var soma = _d.vies2[v];
                for (var j = 0; j < Meio; j++) soma += Ativacoes[j] * Peso2(j, v);
                Chances[v] = soma;
                if (soma > maior) maior = soma;
            }

            // Softmax. Subtrair o maior antes de exponenciar não muda o resultado e
            // evita estourar o float — é o cuidado padrão, e sem ele uma soma
            // grande viraria infinito e todas as chances virariam NaN.
            var total = 0f;
            for (var v = 0; v < Palavras; v++)
            {
                Chances[v] = Mathf.Exp(Chances[v] - maior);
                total += Chances[v];
            }

            var vencedora = 0;
            for (var v = 0; v < Palavras; v++)
            {
                Chances[v] /= total;
                if (Chances[v] > Chances[vencedora]) vencedora = v;
            }
            return vencedora;
        }

        // ------------------------------------------------------------- a dúvida

        /// <summary>
        /// Abaixo desta razão a segunda colocada não tem chance de verdade.
        ///
        /// Vive AQUI, e não na bancada, porque a conferência de conteúdo precisa do
        /// mesmo número. Duas cópias do limiar em lugares diferentes já custaram
        /// caro nesta base: a tabela da bancada 8 divergiu da tabela que a
        /// conferência media, e a medição passou a atestar uma bancada que não
        /// existia mais.
        /// </summary>
        public const float Disputa = 0.35f;

        /// <summary>Quantas palavras a malha pode escrever sozinha antes de perguntar.</summary>
        public const int MaximoSemAposta = 2;

        /// <summary>
        /// Quantas palavras ela escreve antes de recomeçar de uma semente nova.
        ///
        /// Este número não é gosto: foi medido. Rodando as 326 frases do corpus e
        /// deixando a malha comer a própria saída, a pergunta é se a janela de três
        /// palavras que ELA acabou de formar é uma que alguém realmente escreveu:
        ///
        ///     passo  1   2   3   4  |  5   6   7   8   9
        ///     existe 73% 68% 68% 59% | 44% 39% 40% 37% 28%
        ///
        /// Entre o quarto e o quinto passo o chão cede. E cede por um motivo que não
        /// é defeito de implementação: a rede foi treinada em janelas ESCRITAS POR
        /// GENTE, e a partir do terceiro passo a janela é inteirinha invenção dela.
        /// Ela passa a responder sobre um texto que nunca viu no treino.
        ///
        /// O detalhe cruel, e que vale a aula inteira: a CONFIANÇA dela não cai
        /// junto. Fica entre 60% e 75% do primeiro ao nono passo. Ela segue igualmente
        /// certa enquanto a frase deixa de fazer sentido — que é exatamente o que um
        /// modelo grande faz quando inventa com convicção.
        ///
        /// Deixar a frase correr até nove palavras entregava ao aluno uma frase sem
        /// sentido no cartaz final, bem embaixo da linha "a frase é dela" — e a lição
        /// virava piada. Cortando em quatro, o que ele lê é o que ela sabe fazer.
        /// </summary>
        public const int PalavrasPorFrase = 4;

        /// <summary>
        /// A força da segunda colocada em relação à primeira, do último passe.
        ///
        /// Um perto de um é empate técnico; perto de zero é a vencedora sozinha na
        /// frente. É a medida de quanto vale olhar a malha antes de apostar.
        /// </summary>
        public float Folga()
        {
            var topo = MaisAcesas(2);
            if (topo.Count < 2) return 0f;
            return Chances[topo[1]] / Mathf.Max(1e-6f, Chances[topo[0]]);
        }

        /// <summary>A janela do último passe é digna de aposta?</summary>
        public bool EmDuvida() => Folga() >= Disputa;

        /// <summary>
        /// As <paramref name="quantas"/> palavras mais acesas do último passe, da
        /// mais forte para a mais fraca.
        /// </summary>
        public List<int> MaisAcesas(int quantas)
        {
            var topo = new List<int>();
            for (var v = 0; v < Palavras; v++)
            {
                var lugar = topo.Count;
                while (lugar > 0 && Chances[v] > Chances[topo[lugar - 1]]) lugar--;
                if (lugar >= quantas) continue;

                topo.Insert(lugar, v);
                if (topo.Count > quantas) topo.RemoveAt(topo.Count - 1);
            }
            return topo;
        }
    }
}
