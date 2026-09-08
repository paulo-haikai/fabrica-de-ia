using System.Collections.Generic;
using UnityEngine;

namespace FabricaDeIA.Engine
{
    /// <summary>
    /// O estilingue de Rosa — a física da bancada 7.
    ///
    /// É um Angry Birds honesto: velocidade fixa, gravidade constante, parábola
    /// de verdade, colisão contra caixotes e contra o porco. O que muda de tiro
    /// para tiro é UMA coisa só, o ângulo, e quem mexe nele não é o aluno: é a
    /// máquina, por descida de gradiente sobre o erro do tiro anterior.
    ///
    /// Essa é a bancada inteira em uma frase. O aluno regula a máquina; a
    /// máquina joga. E o erro que ela minimiza não é um número abstrato num
    /// mostrador — é a distância entre onde o pássaro caiu e onde o porco está,
    /// medida em passos de pista, que qualquer pessoa lê de relance.
    ///
    /// POR QUE A FÍSICA MORA AQUI, e não na tela: a conferência do Editor
    /// precisa varrer milhares de treinos para medir a faixa de regulagem que
    /// vence cada rodada. Se a física estivesse presa a RectTransform, essa
    /// medida seria impossível e a rodada 3 voltaria a ser publicada sem
    /// ninguém saber se ela tem solução.
    /// </summary>
    public static class Estilingue
    {
        public const float Gravidade = 10f;

        /// <summary>
        /// v²/g — o alcance de um tiro a 45° saindo do chão. Serve de régua para
        /// tudo: as distâncias dos porcos e o erro normalizado da descida.
        /// </summary>
        public const float AlcanceMaximo = 140f;

        public static float Velocidade => Mathf.Sqrt(AlcanceMaximo * Gravidade);

        /// <summary>
        /// A boca do estilingue: de onde o pássaro sai.
        ///
        /// Doze passos, e não os cinco da primeira versão, por causa do GESTO: o
        /// puxão do elástico anda até cento e cinquenta pixels para trás, e com
        /// o estilingue colado na borda esquerda o pássaro saía da pista antes
        /// de a régua encher. Estilingue em cima de um barranco, com espaço
        /// atrás dele para puxar, é também o enquadramento do jogo original.
        /// </summary>
        public const float BocaX = 12f;
        public const float BocaY = 4f;

        /// <summary>O que a pista mostra. Além disso, o pássaro sai de cena.</summary>
        public const float Pista = 142f;

        public const float RaioDoPassaro = 1.2f;
        public const float RaioDoPorco = 2.8f;

        /// <summary>
        /// Os limites do ângulo, em radianos — e eles são generosos DE PROPÓSITO.
        ///
        /// Uma versão anterior prendia o ângulo em [0°, 45°], a faixa onde a
        /// conta se comporta. O efeito colateral apagava um terço da lição: com
        /// passo grande demais a máquina batia na trava, quicava entre os dois
        /// extremos e às vezes acertava o porco por acaso. Com folga até quase a
        /// vertical, passo grande demais FOGE de vez — a máquina sai da bacia,
        /// atira cada vez mais para cima e o pássaro cai na cabeça dela.
        /// </summary>
        public const float AnguloMinimo = -0.09f;
        public const float AnguloMaximo = 1.55f;

        /// <summary>Onde o tiro acabou, e no quê ele bateu.</summary>
        public struct Impacto
        {
            public Vector2 Onde;
            /// <summary>Acertou o porco. É a vitória da pista.</summary>
            public bool Porco;
            /// <summary>Índice do caixote atingido, ou -1.</summary>
            public int Bloco;
            /// <summary>Passou voando por cima de tudo e sumiu da pista.</summary>
            public bool Sumiu;
        }

        /// <summary>
        /// Voa um tiro e devolve o caminho percorrido, ponto a ponto.
        ///
        /// O caminho inteiro, e não só o fim, porque é ele que a tela desenha —
        /// e porque o rastro de cada tiro FICA na pista. Ver os seis rastros de
        /// um treino se fechando em cima do porco é a curva de aprendizado, sem
        /// gráfico nenhum.
        /// </summary>
        public static List<Vector2> Voo(float angulo, float porcoX,
                                        IReadOnlyList<Rect> blocos, out Impacto impacto)
        {
            var caminho = new List<Vector2>(256);
            var vx = Velocidade * Mathf.Cos(angulo);
            var vy = Velocidade * Mathf.Sin(angulo);
            var porco = new Vector2(porcoX, RaioDoPorco);

            impacto = new Impacto { Bloco = -1, Onde = new Vector2(BocaX, BocaY) };

            const float dt = 0.008f;
            for (var passo = 0; passo < 4000; passo++)
            {
                var t = passo * dt;
                var p = new Vector2(BocaX + vx * t,
                                    BocaY + vy * t - 0.5f * Gravidade * t * t);
                caminho.Add(p);

                if (Vector2.Distance(p, porco) <= RaioDoPorco + RaioDoPassaro)
                {
                    impacto.Onde = p;
                    impacto.Porco = true;
                    return caminho;
                }

                if (blocos != null)
                {
                    for (var i = 0; i < blocos.Count; i++)
                    {
                        if (!Encosta(p, blocos[i])) continue;
                        impacto.Onde = p;
                        impacto.Bloco = i;
                        return caminho;
                    }
                }

                if (p.y <= 0f)
                {
                    impacto.Onde = new Vector2(p.x, 0f);
                    // Cair rente ao porco vale como acerto: o pássaro rola nele.
                    impacto.Porco = Mathf.Abs(p.x - porcoX) <= RaioDoPorco + RaioDoPassaro;
                    // Caiu fora do que a pista mostra: para quem está olhando,
                    // ele sumiu. O erro em passos continua existindo e continua
                    // guiando a máquina — só não cabe na tela.
                    impacto.Sumiu = p.x > Pista;
                    return caminho;
                }

                if (p.x > Pista + 24f)
                {
                    impacto.Onde = p;
                    impacto.Sumiu = true;
                    return caminho;
                }
            }

            impacto.Onde = caminho[caminho.Count - 1];
            impacto.Sumiu = true;
            return caminho;
        }

        static bool Encosta(Vector2 p, Rect bloco) =>
            p.x >= bloco.xMin - RaioDoPassaro && p.x <= bloco.xMax + RaioDoPassaro &&
            p.y >= bloco.yMin - RaioDoPassaro && p.y <= bloco.yMax + RaioDoPassaro;

        /// <summary>
        /// Um passo de descida de gradiente sobre o ângulo. É o algoritmo
        /// inteiro que treina redes neurais, escrito em três linhas.
        ///
        /// A perda é meio erro ao quadrado, com o erro medido em fração do
        /// alcance máximo. A derivada do alcance em relação ao ângulo é
        /// 2·A·cos(2θ), e andar contra ela é a atualização:
        ///
        ///     θ ← θ − taxa · (erro/A) · 2·cos(2θ)
        ///
        /// Duas propriedades de que a bancada inteira depende:
        ///
        ///   · Perto de 0° o cosseno vale quase 1 e a mesma taxa move MUITO o
        ///     alcance. Perto de 45° ele encolhe e a mesma taxa mal move nada.
        ///     É por isso que a máquina do porco perto e a do porco longe não
        ///     aceitam a mesma regulagem — a rodada 3 é essa desigualdade.
        ///   · Passada de certo ponto, a correção passa do alvo e volta PIOR.
        ///     Não é bug: é a razão pela qual escolher o passo é uma decisão.
        /// </summary>
        public static float Corrigir(float angulo, float erro, float taxa)
        {
            var derivada = erro / AlcanceMaximo * 2f * Mathf.Cos(2f * angulo);
            var novo = angulo - taxa * derivada;
            if (float.IsNaN(novo) || float.IsInfinity(novo)) novo = AnguloMaximo;
            return Mathf.Clamp(novo, AnguloMinimo, AnguloMaximo);
        }

        /// <summary>
        /// Roda um treino inteiro e devolve onde cada tiro caiu.
        ///
        /// Usada pela conferência do Editor para medir, rodada por rodada, a
        /// faixa de regulagem que de fato vence. O jogo roda o mesmo treino em
        /// câmera lenta, tiro por tiro — mas a conta é esta, e é uma só.
        /// </summary>
        public static List<float> Treinar(float angulo, float taxa, float porcoX,
                                          IReadOnlyList<Rect> blocos, int tiros,
                                          out bool acertou)
        {
            var quedas = new List<float>(tiros);
            acertou = false;

            for (var i = 0; i < tiros; i++)
            {
                Voo(angulo, porcoX, blocos, out var impacto);
                quedas.Add(impacto.Onde.x);
                if (impacto.Porco) { acertou = true; return quedas; }
                angulo = Corrigir(angulo, impacto.Onde.x - porcoX, taxa);
            }
            return quedas;
        }

        /// <summary>
        /// Os caixotes de uma pista: uma caixa e uma pilha de duas, todas ATRÁS
        /// do porco.
        ///
        /// Atrás, e nunca na frente — e isso foi medido, não escolhido por
        /// gosto. Um tiro bem mirado chega quase raspando o chão, e nessa
        /// inclinação QUALQUER coisa em pé na frente do porco, mesmo com três
        /// passos de altura, para o pássaro antes dele. Uma tábua na frente não
        /// deixaria a rodada mais difícil; deixaria a máquina medindo um erro
        /// que não é o dela — o cenário mentindo para o algoritmo.
        ///
        /// Com uma tábua na frente, medido: a rodada 3 vencia numa faixa de 22%
        /// do elástico e a rodada 2 em 23% do retângulo. Sem ela, 34% e 38%. O
        /// cenário não é enfeite: ele é metade da dificuldade.
        ///
        /// As caixas de trás pegam os tiros que passam pouco, e isso é de
        /// propósito: é o que faz um exagero pequeno virar madeira voando, que é
        /// a alegria do jogo original.
        /// </summary>
        public static List<Rect> Cenario(float porcoX) => new()
        {
            new Rect(porcoX + 6f, 0f, 3.4f, 2.4f),
            new Rect(porcoX + 11f, 0f, 3.4f, 2.4f),
            new Rect(porcoX + 11f, 2.4f, 3.4f, 2.4f)
        };
    }
}
