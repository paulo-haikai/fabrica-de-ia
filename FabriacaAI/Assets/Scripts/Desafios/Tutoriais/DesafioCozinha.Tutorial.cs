using System.Collections.Generic;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A explicação da bancada 9 — a cozinha.
    ///
    /// O gesto desta bancada tem duas metades e ninguém adivinha a segunda:
    /// clicar na amostra SELECIONA, e só depois clicar na bandeja CASA. Sem a
    /// demonstração o aluno clica na bandeja primeiro, nada acontece, e ele
    /// conclui que o jogo está quebrado.
    ///
    /// Por isso a demonstração faz o par completo: escolhe uma amostra, casa com
    /// um prato, e desfaz.
    /// </summary>
    public partial class DesafioCozinha
    {
        protected override IReadOnlyList<Passo> Explicacao() => new List<Passo>
        {
            new()
            {
                Texto = "Três máquinas escreveram estas frases — IDÊNTICAS por\n" +
                        "dentro: mesma conta, mesmo sorteio. Só o que leram era diferente.",
                Destaque = () => _amostras
            },
            new()
            {
                Texto = "Embaixo está o cardápio: o que existia para ler.\n" +
                        "Seu trabalho é dizer quem comeu o quê.",
                Destaque = () => _bandejas
            },
            new()
            {
                Texto = "São dois cliques. Primeiro a amostra — ela acende:",
                Acao = EscolherAmostra,
                Espera = 1.2f,
                Destaque = () => _amostras
            },
            new()
            {
                Texto = "Depois o prato. Aí a aposta fica registrada na amostra.",
                Acao = ApostarPrato,
                Espera = 1.4f,
                Destaque = () => _bandejas
            },
            new()
            {
                Texto = "Cada prato serve UMA máquina só — é isso que faz a dedução\n" +
                        "fechar: acertar duas entrega a terceira.",
                Destaque = () => _bandejas
            },
            new()
            {
                Texto = "Clicar de novo numa amostra já casada desfaz a aposta.\n" +
                        "Mudar de ideia não custa nada. Vou desfazer a minha.",
                Destaque = () => _amostras
            },
            new()
            {
                Texto = "A dica: não procure o ASSUNTO das frases, procure o JEITO —\n" +
                        "comprimento, formalidade, que palavras aparecem sem precisar.",
                Destaque = () => _amostras
            }
        };

        /// <summary>Primeiro clique: seleciona a máquina A.</summary>
        void EscolherAmostra() => Abrir(0);

        /// <summary>
        /// Segundo clique: casa com o primeiro prato do cardápio.
        ///
        /// O primeiro, e não o certo. O cardápio está em ordem fixa e as amostras
        /// vêm sorteadas, então esta aposta acerta por acidente em uma vez a cada
        /// três — e como ela é desfeita, mesmo o acidente não vale nada.
        /// </summary>
        void ApostarPrato() => Casar(0);

        /// <summary>Devolve as três amostras sem aposta nenhuma.</summary>
        protected override void AoFimDaExplicacao() => RecomecarNivel();
    }
}
