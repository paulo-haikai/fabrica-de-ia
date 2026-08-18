using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// A explicação da bancada 5 — o mapa.
    ///
    /// A demonstração fecha um grupo inteiro, e isso é deliberado: sem ver um
    /// grupo FECHAR, o aluno não sabe o que está procurando. Com um fechado na
    /// tela, os oito que sobram viram um problema claro.
    ///
    /// O passo mais importante é o segundo, que aponta a companhia impressa na
    /// carta. É a informação que a máquina usou para montar os grupos, e é a
    /// única forma de o aluno chegar às mesmas conclusões que ela — se ele
    /// agrupar por significado, vai discordar dela em algum ponto e o
    /// quebra-cabeça fica injusto.
    /// </summary>
    public partial class DesafioMapa
    {
        protected override IReadOnlyList<Passo> Explicacao() => new List<Passo>
        {
            new()
            {
                Texto = "Doze palavras. Elas formam três grupos de quatro,\n" +
                        "e Bento quer que você descubra quais.",
                Destaque = () => _mesa
            },
            new()
            {
                Texto = "Olhe as duas linhas miúdas de cada carta: dizem quais palavras\n" +
                        "vêm ANTES e DEPOIS. Agrupa-se pela companhia, não pelo assunto.",
                Destaque = () => _mesa.childCount > 0 ? _mesa.GetChild(0) as RectTransform : null
            },
            new()
            {
                Texto = "Vou escolher quatro que andam com a mesma companhia.",
                Acao = EscolherUmGrupo,
                Espera = 1.2f,
                Destaque = () => _mesa
            },
            new()
            {
                Texto = "E confirmar.",
                Acao = Conferir,
                Espera = 1.2f,
                Destaque = () => _acertos
            },
            new()
            {
                Texto = "Grupo fechado. Ele sobe para o alto com o nome que a máquina\n" +
                        "deu: “aparecem onde tal palavra aparece”.",
                Destaque = () => _acertos
            },
            new()
            {
                Texto = "Você tem três erros de margem. Quando errar, digo quantas\n" +
                        "das quatro pertencem ao mesmo grupo — errar também informa.",
                Destaque = () => _vidas.rectTransform
            },
            new()
            {
                Texto = "Vou desfazer o que montei. Os doze são seus.",
                Destaque = null
            }
        };

        /// <summary>
        /// Seleciona as quatro palavras de um grupo que a máquina montou.
        ///
        /// Usa o gabarito porque a demonstração precisa ACERTAR: um grupo errado
        /// gastaria uma vida do aluno e ensinaria a regra ao contrário.
        /// </summary>
        void EscolherUmGrupo()
        {
            var grupo = _rodada.FirstOrDefault(g => !_resolvidos.Contains(g));
            if (grupo == null) return;

            _selecionadas.Clear();
            foreach (var palavra in grupo.Palavras.Take(Engine.Mapas.PorGrupo))
                _selecionadas.Add(palavra);

            Redesenhar();
        }

        /// <summary>
        /// Refaz a rodada com as vidas cheias e os doze na mesa.
        ///
        /// Sem isto o aluno começaria com um grupo já resolvido — um terço do
        /// quebra-cabeça entregue.
        /// </summary>
        protected override void AoFimDaExplicacao()
        {
            _resolvidos.Clear();
            _selecionadas.Clear();
            RecomecarNivel();
        }
    }
}
