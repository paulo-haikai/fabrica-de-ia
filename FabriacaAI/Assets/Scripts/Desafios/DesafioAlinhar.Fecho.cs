using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// O que vem depois do último carimbo: a cena, e uma pergunta.
    ///
    /// ESTE ARQUIVO ERA O ATO II — quatro cartazes explicando o que o sistema tem
    /// sobre cada pessoa, o que ele não tem, a regra que ele aprendeu, e um fecho.
    /// Mais dois cartazes de despedida. Seis telas de texto entre o último
    /// carimbo e o fim da aula, e o aluno acabava de atender trinta pessoas sob
    /// relógio.
    ///
    /// SUMIRAM TODAS. A cena animada mostra o que elas explicavam, e a única
    /// coisa que elas tinham de insubstituível — a regra que a máquina aprendeu
    /// das decisões dele — virou uma legenda dentro da cena, dita pela própria
    /// máquina. Ver DesafioAlinhar.Epilogo.cs.
    ///
    /// O que sobra aqui é o que nenhuma animação pode dar: a pergunta que a turma
    /// vai discutir depois que a tela apagar.
    ///
    /// A ÉTICA DO DESENHO, e ela é deliberada: a bancada NÃO acusa o aluno. A cota
    /// era de um contrato que não é dele, o formulário foi desenhado antes de ele
    /// chegar, e a Regra 7 tirou a punição de um lado só. O alvo é a ESTRUTURA, e
    /// um jogo que fizesse uma criança de doze anos sair de lá com vergonha de si
    /// mesma teria ensinado a lição errada — a de que o problema é o caráter de
    /// quem atende, que é exatamente a desculpa de quem projeta o sistema.
    /// </summary>
    public partial class DesafioAlinhar
    {
        List<Ano> _anos;
        RectTransform _palco;

        // ------------------------------------------------------------ a pergunta

        /// <summary>
        /// A ÚNICA TELA DEPOIS DA CENA, e ela tem uma frase.
        ///
        /// Aqui havia quatro cartazes do Ato II — o que o sistema tem, o que ele
        /// não tem, a regra que ele aprendeu, e um fecho — mais um cartaz de
        /// pergunta e um de despedida. Seis telas de texto entre o último carimbo
        /// e o fim da aula.
        ///
        /// Sumiram todas. O que elas ensinavam a cena passou a mostrar, e a regra
        /// que a máquina aprendeu virou uma legenda dentro dela, dita pela própria
        /// máquina. O que sobra é o que nenhuma animação pode dar: a pergunta que a
        /// turma vai discutir depois que a tela apagar.
        ///
        /// É o contrato do README — as bancada 8 e 12 terminam em pergunta que o
        /// jogo não responde — e agora é literal: o jogo não diz mais nada.
        /// </summary>
        void CenaPergunta()
        {
            Painel.Instruir(string.Empty);
            Painel.Rodape(string.Empty);

            var pergunta = Widgets.Texto("P", _palco, 25, TextAnchor.MiddleCenter, Cores.Luz);
            Widgets.Fixar(pergunta.rectTransform, new Vector2(0.5f, 0.5f),
                          new Vector2(0f, 8f), new Vector2(820f, 220f));
            pergunta.text = "O viés da IA são os dados de quem treinou ela.\n\n" +
                            "É seguro entregarmos escolhas sensíveis nas mãos\n" +
                            "de quem domina essa tecnologia?";

            Painel.Acao("voltar ao ateliê", () => Encerrar(Estrelas()));
        }

        // ------------------------------------------------------------ pinturas

        void Limpar()
        {
            foreach (Transform filho in _mesa) Destroy(filho.gameObject);
            _palco = Widgets.Painel("Palco", _mesa, Color.clear);
            Widgets.Esticar(_palco);
            Widgets.Surgir(_palco, 0.2f, 0.98f);
        }
    }
}
