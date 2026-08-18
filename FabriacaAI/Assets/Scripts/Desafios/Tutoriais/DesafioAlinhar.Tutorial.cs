using System.Collections.Generic;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A explicação da bancada 12 — alinhar.
    ///
    /// Esta é a única bancada em que a demonstração precisa DESFAZER o que fez por
    /// dentro, e não só na tela. A agulha da política vive em <c>Preparar</c>, não
    /// em <c>MontarNivel</c>: ela atravessa as duas rodadas de propósito, porque é
    /// ela que faz a resposta final da máquina sair com a cara do aluno. Se a
    /// escolha da demonstração ficasse gravada, a política testada na rodada 2
    /// teria um voto meu dentro.
    ///
    /// Então a explicação tira uma fotografia da agulha antes de clicar e a
    /// devolve no fim.
    /// </summary>
    public partial class DesafioAlinhar
    {
        /// <summary>A agulha como estava antes da demonstração mexer nela.</summary>
        Dictionary<Eixo, int> _agulhaAntes;

        protected override IReadOnlyList<Passo> Explicacao() => new List<Passo>
        {
            new()
            {
                Texto = "Sereno já sabe falar. O que ele não sabe é COMO você quer que\n" +
                        "ele fale — e ninguém vai escrever isso para ele.",
                Destaque = () => _mesa
            },
            new()
            {
                Texto = "Ele faz uma pergunta e mostra duas respostas — as duas certas,\n" +
                        "as duas verdadeiras. Você só escolhe a que prefere.",
                Destaque = () => _mesa
            },
            new()
            {
                Texto = "Vou escolher a da esquerda. Repare que não acontece nada\n" +
                        "espetacular — ele só passa para o caso seguinte.",
                Acao = Demonstrar,
                Espera = 1.6f,
                Destaque = () => _mesa
            },
            new()
            {
                Texto = "É isso, e só isso: comparar duas respostas e apontar a melhor.\n" +
                        "É assim que se ajusta um modelo de verdade — milhares de\n" +
                        "pessoas escolhendo entre duas respostas, sem escrever regra nenhuma.",
                Destaque = () => _mesa
            },
            new()
            {
                Texto = "São três casos, nenhum com resposta certa: medem se você\n" +
                        "prefere curto ou completo, direto ou cuidadoso, pronto ou que faz pensar.",
                Destaque = () => _mesa
            },
            new()
            {
                Texto = "Depois dos três, Sereno responde uma pergunta nova, usando\n" +
                        "o que aprendeu do seu gosto — e você vê se ele acertou.",
                Destaque = null
            },
            new()
            {
                Texto = "Vou apagar minha escolha da caderneta dele, por dentro também\n" +
                        "— a resposta final tem que ter a sua cara. Comece do primeiro caso.",
                Destaque = null
            }
        };

        /// <summary>
        /// Clica de verdade no botão da esquerda do caso 1.
        ///
        /// Invoca o <c>onClick</c> em vez de chamar <c>Julgar</c> direto porque o
        /// lado de cada resposta é sorteado — só o botão sabe qual empurrão ele
        /// carrega, e duplicar essa conta aqui seria duplicar o sorteio.
        /// </summary>
        void Demonstrar()
        {
            _agulhaAntes = new Dictionary<Eixo, int>(_agulha);

            var esquerda = _mesa.Find("r-1")?.GetComponent<Button>();
            if (esquerda != null) esquerda.onClick.Invoke();
        }

        /// <summary>
        /// Devolve a agulha à fotografia e recomeça a rodada.
        ///
        /// A ordem importa: recomeçar sozinho voltaria para o caso 1 mas deixaria
        /// o voto da demonstração gravado na política.
        /// </summary>
        protected override void AoFimDaExplicacao()
        {
            if (_agulhaAntes != null)
            {
                foreach (var par in _agulhaAntes) _agulha[par.Key] = par.Value;
                _agulhaAntes = null;
            }

            RecomecarNivel();
        }
    }
}
