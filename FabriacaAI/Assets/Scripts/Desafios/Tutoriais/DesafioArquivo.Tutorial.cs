using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A explicação da bancada 3 — o arquivo.
    ///
    /// Esta é a única explicação que abre DUAS casinhas: uma cheia e uma vazia.
    /// Precisa das duas. Só com a cheia o aluno acha que o arquivo é bem
    /// abastecido; só com a vazia ele acha que a grade está quebrada. Uma depois
    /// da outra, ele entende que a tabela tem alguns pares e um oceano de nada —
    /// e é essa proporção que a bancada inteira existe para mostrar.
    /// </summary>
    public partial class DesafioArquivo
    {
        protected override IReadOnlyList<Passo> Explicacao() => new List<Passo>
        {
            new()
            {
                Texto = "O fichário de Aurélio é tabela: LINHA e COLUNA são palavras,\n" +
                        "e o cruzamento guarda quantas vezes uma veio depois da outra.",
                Destaque = () => Area.Find("Grade") as RectTransform
            },
            new()
            {
                Texto = "As casinhas estão fechadas. Clicar abre uma.\n" +
                        "Veja o que tem dentro desta:",
                Acao = AbrirUmaCheia,
                Espera = 1.2f,
                Destaque = () => Area.Find("Grade") as RectTransform
            },
            new()
            {
                Texto = "Verde com risquinhos: este par existe no arquivo.\n" +
                        "Os risquinhos são quantas vezes ele apareceu.",
                Destaque = () => Area.Find("Grade") as RectTransform
            },
            new()
            {
                Texto = "Agora esta outra:",
                Acao = AbrirUmaVazia,
                Espera = 1.2f,
                Destaque = () => Area.Find("Grade") as RectTransform
            },
            new()
            {
                Texto = "Um pontinho: nada. Ninguém escreveu essas duas juntas — a\n" +
                        "máquina não sabe nada sobre esse par.",
                Destaque = () => Area.Find("Grade") as RectTransform
            },
            new()
            {
                Texto = "Doze cliques para achar três casinhas cheias. Dica: não\n" +
                        "clique à esmo — leia a palavra da linha, pense no que vem depois.",
                Destaque = null
            },
            new()
            {
                Texto = "Fecho as duas que abri. A grade é sua, são três rodadas —\n" +
                        "no fim Aurélio mostra o tamanho do fichário inteiro.",
                Destaque = null
            }
        };

        /// <summary>Abre uma casinha que tem risquinho, se houver alguma na grade.</summary>
        void AbrirUmaCheia() => AbrirPrimeira(cheia: true);

        /// <summary>Abre uma casinha vazia.</summary>
        void AbrirUmaVazia() => AbrirPrimeira(cheia: false);

        void AbrirPrimeira(bool cheia)
        {
            foreach (var par in _celulas)
            {
                var (linha, coluna) = par.Value;
                if (!par.Key.interactable) continue;

                var tem = _arquivo.Risquinhos(_de[linha], _para[coluna]) > 0;
                if (tem != cheia) continue;

                Abrir(par.Key, linha, coluna);
                return;
            }
        }

        /// <summary>
        /// Refaz a grade. As duas casinhas que a demonstração abriu tinham que
        /// voltar a fechar: o aluno recebe os doze cliques inteiros, e — mais
        /// importante — não recebe duas respostas de graça.
        /// </summary>
        protected override void AoFimDaExplicacao() => RecomecarNivel();
    }
}
