using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A explicação da bancada 1 — o Termo.
    ///
    /// O passo que carrega a bancada é o terceiro: a máquina digita uma palavra
    /// real e manda, e as três cores acendem. Nenhuma frase sobre "verde é letra
    /// no lugar certo" ensina tão rápido quanto ver o verde aparecer numa letra
    /// que o aluno acabou de ver ser escrita.
    /// </summary>
    public partial class DesafioTermo
    {
        /// <summary>
        /// A explicação da bancada 1, com um palpite jogado de verdade.
        ///
        /// O passo que importa é o terceiro: a máquina digita uma palavra real e
        /// manda, e as cores aparecem. Nenhuma frase sobre "verde é letra no lugar
        /// certo" ensina tão rápido quanto ver o verde acender numa letra que o
        /// aluno acabou de ver ser escrita.
        ///
        /// O palpite usado é sempre uma palavra que EXISTE no corpus e que não é a
        /// resposta — assim a demonstração mostra as três cores e não entrega a
        /// rodada de graça.
        /// </summary>
        protected override IReadOnlyList<Passo> Explicacao() => new List<Passo>
        {
            new()
            {
                Texto = "Tico quer saber se você adivinha uma palavra.\n" +
                        "Nesta rodada ele não dá pista nenhuma — só o tamanho dela.",
                Destaque = () => _contexto.rectTransform
            },
            new()
            {
                Texto = "Você tem seis tentativas, uma por linha. Digite e aperte\n" +
                        "Enter — Backspace apaga. Qualquer palavra do tamanho certo vale.",
                Destaque = () => _palco.Find("Grade") as RectTransform
            },
            new()
            {
                Texto = "Veja: eu vou chutar uma palavra qualquer.",
                Acao = Demonstrar,
                Espera = 1.4f,
                Destaque = () => _palco.Find("Grade") as RectTransform
            },
            new()
            {
                Texto = "As cores respondem. VERDE é letra no lugar certo.\n" +
                        "AMARELO é letra que existe, mas em outra posição.\n" +
                        "CINZA é letra que não está na palavra.",
                Destaque = () => _palco.Find("Grade") as RectTransform
            },
            new()
            {
                Texto = "O teclado guarda o que você já descobriu — letra que saiu\n" +
                        "cinza fica cinza, para você não gastar tentativa repetindo.",
                Destaque = () => _area.Find("Teclado") as RectTransform
            },
            new()
            {
                Texto = "São seis palavras: as três primeiras sem pista. Na quarta,\n" +
                        "Tico começa a dar uma frase — e você sente a diferença que o contexto faz.",
                Destaque = null
            }
        };

        /// <summary>
        /// Joga um palpite de verdade, do mesmo jeito que o aluno jogaria.
        ///
        /// Escolhe uma palavra do corpus do tamanho certo que NÃO seja a resposta,
        /// e que compartilhe ao menos uma letra com ela — sem letra em comum a
        /// demonstração sairia toda cinza e não mostraria verde nem amarelo, que é
        /// justamente o que ela precisa mostrar.
        /// </summary>
        void Demonstrar()
        {
            var resposta = _rodadas[_rodadaAtual].Resposta;
            var escolhida = Corpus.Vocabulario
                .Where(p => p.Length == resposta.Length)
                .Where(p => Normalizar(p) != Normalizar(resposta))
                .OrderByDescending(p => LetrasEmComum(p, resposta))
                .FirstOrDefault();

            if (escolhida == null) return;
            _digitando = escolhida;
            RedesenharLinhaAtual();
            Enviar();
        }

        static int LetrasEmComum(string a, string b)
        {
            var na = Normalizar(a);
            var nb = Normalizar(b);
            return na.Count(c => nb.Contains(c));
        }

        /// <summary>
        /// Limpa o palpite da demonstração e devolve a rodada intacta.
        ///
        /// O aluno tem que jogar as seis tentativas dele. Deixar a linha do
        /// tutorial na grade seria roubar uma — e roubar informação, porque as
        /// cores daquele palpite continuariam à vista.
        /// </summary>
        protected override void AoFimDaExplicacao()
        {
            _palpites.Clear();
            _digitando = string.Empty;
            _rodadaEncerrada = false;
            _acertos = 0;
            _custos.Clear();
            MontarGrade(_rodadas[_rodadaAtual].Resposta.Length);
            MontarTeclado();
            Painel.Instruir("sem pista nenhuma — só o tamanho");
        }
    }
}
