using System;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// O contrato de uma bancada.
    ///
    /// O jogo abre um desafio, some do caminho e espera. Quando o desafio
    /// termina, ele diz quantas estrelas o aluno tirou — zero significa que
    /// desistiu, e nesse caso o mestre não dá o discurso de fecho.
    ///
    /// A fronteira é estreita de propósito: são doze desafios, cada um com
    /// regras próprias. Se o mundo soubesse alguma coisa sobre eles, portar o
    /// sétimo exigiria mexer no primeiro.
    ///
    /// O que o desafio NÃO controla é a moldura: cabeçalho, botão de sair,
    /// linha de instrução e rodapé são montados por <see cref="PainelDeBancada"/>
    /// e valem igual nas doze bancadas. Um minigame não pode esquecer de pôr
    /// uma saída, nem desenhar por cima de uma faixa fixa, porque só recebe o
    /// retângulo de conteúdo.
    /// </summary>
    public abstract class Desafio : MonoBehaviour
    {
        /// <summary>Estrelas de 0 a 3. Zero é desistência.</summary>
        public event Action<int> Terminou;

        /// <summary>A etapa que este desafio resolve (<c>e1</c> a <c>e12</c>, com o balcão do certificado no fim).</summary>
        public abstract string Etapa { get; }

        /// <summary>O que aparece no cabeçalho da moldura.</summary>
        public abstract string Titulo { get; }

        protected PainelDeBancada Painel { get; private set; }

        /// <summary>Sobra alguma moldura na tela? Só verdadeiro enquanto joga.</summary>
        protected bool Aberto => Painel != null;

        /// <summary>
        /// Segundos que o aluno passou nesta bancada, do abrir ao fechar.
        ///
        /// Conta o tempo REAL e não o de jogo: a diferença aparece quando o aluno
        /// troca de aba no meio, e nesse caso o tempo de parede é o honesto — foi
        /// o que a aula custou a ele.
        /// </summary>
        public int Segundos { get; private set; }

        float _abertaEm;

        /// <summary>Monta a moldura e entrega o conteúdo ao minigame.</summary>
        public void Abrir(RectTransform paiDaTela, string subtitulo)
        {
            _abertaEm = Time.realtimeSinceStartup;
            Painel = PainelDeBancada.Montar(paiDaTela, Titulo, subtitulo);
            Painel.Fechou += Desistir;
            Montar(Painel.Conteudo);
        }

        /// <summary>
        /// Desenha o minigame dentro de <paramref name="area"/> — e só dentro
        /// dela. Nada aqui deve ancorar na tela.
        /// </summary>
        protected abstract void Montar(RectTransform area);

        /// <summary>
        /// O aluno pediu para sair. O padrão é encerrar sem estrela; um desafio
        /// que já tenha resultado a dar (o placar final, por exemplo)
        /// sobrescreve para não jogar fora o que foi conquistado.
        /// </summary>
        protected virtual void Desistir() => Encerrar(0);

        protected void Encerrar(int estrelas)
        {
            Segundos = Mathf.RoundToInt(Time.realtimeSinceStartup - _abertaEm);

            var aviso = Terminou;
            Terminou = null;

            // A moldura é filha do palco da HUD, e não deste objeto — ela tem
            // que cobrir a tela inteira, e o objeto do desafio vive na árvore do
            // jogo. Consequência: destruir o desafio NÃO destrói a moldura.
            //
            // Foi exatamente esse o bug de "não dá para fechar o minigame": o
            // Esc e o botão sair funcionavam, o aluno até voltava a andar pelo
            // ateliê, mas o painel continuava plantado na frente de tudo. Quem
            // abriu a moldura fecha a moldura.
            if (Painel != null)
            {
                Painel.Fechou -= Desistir;
                Destroy(Painel.gameObject);
                Painel = null;
            }

            aviso?.Invoke(estrelas);
            Destroy(gameObject);
        }
    }
}
