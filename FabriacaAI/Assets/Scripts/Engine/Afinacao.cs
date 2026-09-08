using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FabricaDeIA.Engine
{
    /// <summary>
    /// O mostrador de Seu Ilo: um punhado de botões, um alvo escondido e um único
    /// número dizendo o quanto está errado.
    ///
    /// A perda é a média dos quadrados das diferenças — a mesma família de função
    /// que treina rede de verdade. E ela tem CURVATURA por botão: alguns pesam
    /// mais no erro que outros. Isso não é enfeite matemático, é o que faz um
    /// único passo de aprendizado nunca servir bem para todos os botões ao mesmo
    /// tempo, e é o assunto da bancada 7.
    ///
    /// NENHUMA BANCADA USA ISTO HOJE, e o registro de para onde as duas foram
    /// vale mais que o código:
    ///
    ///   Bancada 6 — era girar botões e ver um número subir e descer. Virou o
    ///   robô corredor (ver <c>DesafioErro</c>): o aluno calibra a margem de
    ///   erro e assiste. O erro passou a ter corpo — é o robô batendo a cara.
    ///
    ///   Bancada 7 — era a mesma máquina descendo o gradiente sozinha, com o
    ///   aluno escolhendo o passo. Virou o estilingue (ver <c>Estilingue</c> e
    ///   <c>DesafioTreino</c>): o passo continua sendo a única escolha do aluno,
    ///   e o erro que a máquina minimiza é a distância até o porco.
    ///
    /// As duas mudanças têm a mesma causa: um número de erro abstrato não
    /// convence ninguém de que aquilo é aprendizado. Este arquivo fica de pé
    /// como a versão mais curta e legível da ideia — perda quadrática com
    /// curvatura por parâmetro —, e como ponto de partida se alguma bancada
    /// futura precisar de um mostrador de muitos botões.
    /// </summary>
    public class Mostrador
    {
        readonly float[] _alvo;
        readonly float[] _curvatura;

        /// <summary>A posição atual de cada botão.</summary>
        public float[] Botoes { get; }

        public int Quantos => Botoes.Length;

        /// <summary>Quantas medições o aluno já pediu.</summary>
        public int Medicoes { get; private set; }

        /// <summary>
        /// Monta um mostrador com alvo sorteado.
        ///
        /// <paramref name="desequilibrio"/> em 1 dá todos os botões com o mesmo
        /// peso no erro; acima disso, alguns passam a pesar muito mais — que é o
        /// caso realista e o que torna a escolha do passo difícil.
        /// </summary>
        public Mostrador(int quantos, int semente, float desequilibrio = 1f)
        {
            var sorteio = new Mulberry32((uint)semente);

            _alvo = new float[quantos];
            _curvatura = new float[quantos];
            Botoes = new float[quantos];

            for (var i = 0; i < quantos; i++)
            {
                // Alvo em passos de 0,5 dentro de [-4, 4]: números redondos o
                // bastante para o aluno acertar de propósito, e não por sorte.
                _alvo[i] = Mathf.Round((sorteio.Proximo() * 8f - 4f) * 2f) / 2f;
                _curvatura[i] = Mathf.Lerp(1f, desequilibrio, sorteio.Proximo());
                Botoes[i] = 0f;
            }
        }

        /// <summary>O erro atual. Zero é afinado.</summary>
        public float Erro() => ErroDe(Botoes);

        public float ErroDe(float[] posicoes)
        {
            var soma = 0f;
            for (var i = 0; i < posicoes.Length; i++)
            {
                var d = posicoes[i] - _alvo[i];
                soma += _curvatura[i] * d * d;
            }
            return soma / posicoes.Length;
        }

        /// <summary>O erro de quem nunca tocou em botão nenhum. Serve de régua.</summary>
        public float ErroInicial() => ErroDe(new float[Quantos]);

        public void Girar(int botao, float quanto)
        {
            Botoes[botao] = Mathf.Clamp(Botoes[botao] + quanto, -6f, 6f);
        }

        /// <summary>Registra uma medição do mostrador.</summary>
        public float Medir()
        {
            Medicoes++;
            return Erro();
        }

        /// <summary>
        /// Um passo de descida de gradiente.
        ///
        /// A derivada da perda em relação a cada botão é 2·curvatura·(atual −
        /// alvo) dividido pelo número de botões. Andar contra ela é o algoritmo
        /// inteiro que treina redes neurais — e cabe nestas cinco linhas.
        ///
        /// Com passo grande demais a atualização passa do alvo e volta pior:
        /// a distância se multiplica por (1 − 2·passo·curvatura/n) a cada
        /// iteração, e quando isso passa de 1 em módulo, o erro explode. Não é
        /// bug, é a razão pela qual escolher o passo é uma decisão de verdade.
        /// </summary>
        public void Descer(float passo)
        {
            for (var i = 0; i < Botoes.Length; i++)
            {
                var derivada = 2f * _curvatura[i] * (Botoes[i] - _alvo[i]) / Botoes.Length;
                Botoes[i] -= passo * derivada;
                // Sem trava aqui: se explodir, tem que explodir na cara do aluno.
                if (float.IsNaN(Botoes[i]) || Mathf.Abs(Botoes[i]) > 1e6f) Botoes[i] = 1e6f;
            }
        }

        /// <summary>
        /// O passo que zeraria o erro de um botão de curvatura 1 numa única
        /// iteração. Serve de unidade.
        ///
        /// Existe porque o passo em unidades cruas depende do número de botões
        /// (a derivada da média divide por eles), e "use passo 4" não ensina
        /// nada a ninguém. Medido em FRAÇÃO deste ideal, o número passa a
        /// significar sempre a mesma coisa: 50% desce com folga, 100% é o limite
        /// teórico, acima de 100% começa a passar do ponto e perto de 200% a
        /// conta explode. Essa é a intuição que o aluno leva embora, e ela vale
        /// para qualquer rede, de qualquer tamanho.
        /// </summary>
        public float PassoIdeal => Quantos / 2f;

        /// <summary>Um passo, medido em fração do <see cref="PassoIdeal"/>.</summary>
        public void DescerFracao(float fracao) => Descer(fracao * PassoIdeal);

        /// <summary>
        /// Roda o treino inteiro e devolve a curva de erro, passo por passo.
        ///
        /// Devolver a curva toda, em vez de só o resultado, é o que permite a
        /// bancada 7 desenhar o gráfico — e o gráfico é a lição: passo pequeno
        /// desce devagar, passo grande serrilha, passo grande demais sobe.
        /// </summary>
        public List<float> Treinar(float fracaoDoPasso, int passos)
        {
            var curva = new List<float> { Erro() };
            for (var i = 0; i < passos; i++)
            {
                DescerFracao(fracaoDoPasso);
                curva.Add(Erro());
            }
            return curva;
        }

        /// <summary>
        /// A curvatura de cada botão — o quanto ele pesa no erro.
        ///
        /// Era privada, e com isso a bancada 7 só conseguia mostrar o erro
        /// SOMADO: uma curva única, em que "um passo bom para um botão é grande
        /// demais para outro" é uma frase que o aluno lê, não uma coisa que ele
        /// vê. Para desenhar dois vales lado a lado — um íngreme, um raso — a
        /// tela precisa saber qual botão é qual.
        /// </summary>
        public IReadOnlyList<float> Curvatura => _curvatura;

        /// <summary>Os dois extremos: o botão mais íngreme e o mais raso.</summary>
        public (int ingreme, int raso) Extremos()
        {
            int ingreme = 0, raso = 0;
            for (var i = 1; i < _curvatura.Length; i++)
            {
                if (_curvatura[i] > _curvatura[ingreme]) ingreme = i;
                if (_curvatura[i] < _curvatura[raso]) raso = i;
            }
            return (ingreme, raso);
        }

        /// <summary>
        /// O botão de curvatura mais próxima da média.
        ///
        /// Nas rodadas de curvatura parelha é ele que a tela desenha: um vale
        /// só, que representa honestamente o que acontece com o conjunto.
        /// Mostrar um botão atípico ali seria a tela mentir sobre a vitória,
        /// que é medida no erro de todos.
        /// </summary>
        public int Mediano()
        {
            var media = _curvatura.Average();
            var melhor = 0;
            for (var i = 1; i < _curvatura.Length; i++)
                if (Mathf.Abs(_curvatura[i] - media) < Mathf.Abs(_curvatura[melhor] - media))
                    melhor = i;
            return melhor;
        }

        /// <summary>
        /// A distância de um botão até o alvo dele, COM SINAL.
        ///
        /// O sinal é o que permite ver o passo grande demais passar do ponto e
        /// subir a ladeira do outro lado. Em módulo, passar do ponto e não
        /// chegar são o mesmo desenho.
        /// </summary>
        public float Desvio(int botao) => Botoes[botao] - _alvo[botao];

        /// <summary>
        /// Roda o treino e devolve a curva de erro E a trajetória de cada botão
        /// observado, passo a passo.
        ///
        /// Uma passada só para as duas coisas: rodar o treino duas vezes com a
        /// mesma semente daria o mesmo resultado, mas convidaria alguém a mudar
        /// uma das chamadas e produzir uma tela que discorda do placar.
        /// </summary>
        public (List<float> erro, List<float>[] desvios) TreinarObservando(
            float fracaoDoPasso, int passos, params int[] observados)
        {
            var erro = new List<float> { Erro() };
            var desvios = new List<float>[observados.Length];
            for (var k = 0; k < observados.Length; k++)
                desvios[k] = new List<float> { Desvio(observados[k]) };

            for (var i = 0; i < passos; i++)
            {
                DescerFracao(fracaoDoPasso);
                erro.Add(Erro());
                for (var k = 0; k < observados.Length; k++)
                    desvios[k].Add(Desvio(observados[k]));
            }
            return (erro, desvios);
        }

        public void Zerar()
        {
            for (var i = 0; i < Botoes.Length; i++) Botoes[i] = 0f;
            Medicoes = 0;
        }

        /// <summary>O alvo, para o cartaz de fim de rodada mostrar a resposta.</summary>
        public IReadOnlyList<float> Alvo => _alvo;
    }
}
