using System.Collections.Generic;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A explicação da bancada 8 — o treino.
    ///
    /// Aqui a demonstração é o ponto alto da aula inteira, e por um motivo de
    /// contraste: o aluno acabou de passar a bancada 7 girando botões um por um e
    /// medindo com parcimônia. Neste passo ele vê UMA carga e a máquina girar
    /// todos os botões, oito vezes, sozinha.
    ///
    /// A demonstração usa uma carga fraquinha deliberadamente. A bolinha desce e
    /// para na ladeira: mostra o mecanismo por inteiro sem entregar a resposta da
    /// rodada, que é justamente calibrar a força.
    /// </summary>
    public partial class DesafioTreino
    {
        protected override IReadOnlyList<Passo> Explicacao() => new List<Passo>
        {
            new()
            {
                Texto = "Mesmo mostrador da bancada anterior, mais botões — mas agora\n" +
                        "você não gira nada. Quem gira é a máquina.",
                Destaque = () => _quadro
            },
            new()
            {
                Texto = "Este vale é o erro de um botão: fundo é acertar, ladeira é\n" +
                        "errar. A máquina empurra a bolinha ladeira abaixo.",
                Destaque = () => _quadro
            },
            new()
            {
                Texto = "Você decide uma coisa só: a FORÇA do empurrão. Segurando o\n" +
                        "botão ela carrega; soltando, a máquina treina.",
                Destaque = () => _botaoCarga == null
                    ? null
                    : (RectTransform)_botaoCarga.transform
            },
            new()
            {
                Texto = "Vou dar um empurrãozinho fraco. Olhe a bolinha:",
                Acao = Demonstrar,
                Espera = 2.2f,
                Destaque = () => _quadro
            },
            new()
            {
                Texto = "Ela desceu e PAROU no meio da ladeira. Força fraca demais não\n" +
                        "chega ao fundo antes de o treino acabar.",
                Destaque = () => _quadro
            },
            new()
            {
                Texto = "Com força demais acontece o contrário: ela atravessa o fundo e\n" +
                        "sobe do outro lado, cada vez mais alto, até sair voando.\n" +
                        "Existe uma faixa boa no meio — achar ela é seu trabalho.",
                Destaque = () => _quadro
            },
            new()
            {
                Texto = "Vou limpar o vale. Suas tentativas estão todas intactas.",
                Destaque = null
            }
        };

        /// <summary>
        /// Roda um treino com uma carga fraca.
        ///
        /// Não é a força certa, e isso é intencional: com 8 passos e fração 0,05
        /// o erro cai para uns dois terços do inicial — longe do centésimo que a
        /// meta pede. A demonstração ensina o MECANISMO e deixa a escolha de pé.
        /// </summary>
        void Demonstrar() => Lancar(0.05f);

        /// <summary>
        /// Refaz o nível: vale limpo e o contador de tentativas de volta em
        /// zero, porque a tentativa que EU gastei não era dele.
        /// </summary>
        protected override void AoFimDaExplicacao() => RecomecarNivel();
    }
}
