using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// O desenho da malha da bancada 6, e a frente de luz que a atravessa.
    ///
    /// DUAS DECISÕES DE DESENHO CARREGAM A LIÇÃO INTEIRA:
    ///
    /// 1. AS CONEXÕES CRESCEM. Cada conexão é uma barra fininha com o pivô na ponta
    ///    de origem, e a animação estica a largura dela de zero até o comprimento
    ///    todo. Todas ao mesmo tempo. O efeito é uma frente atravessando a tela —
    ///    que é o que acontece de verdade — e não uma bolinha escolhendo caminho,
    ///    que é o que a intuição comum imagina e que seria árvore de decisão.
    ///
    /// 2. O BRILHO É O EMPURRÃO, NÃO O PESO. Uma conexão de peso enorme saindo de
    ///    um neurônio apagado carrega zero, e tem que aparecer apagada. Brilhar
    ///    pelo peso mostraria a malha como ela é em repouso; brilhar pelo empurrão
    ///    mostra o que ela está fazendo AGORA com estas três palavras.
    ///
    /// A segunda camada tem 12 × 318 = 3.816 conexões, e desenhá-las viraria uma
    /// mancha cinza. Então desenho só as que vão para as três lâmpadas da aposta —
    /// e aviso na tela que as outras existem. Esconder sem avisar seria ensinar que
    /// a malha é magra.
    /// </summary>
    public partial class DesafioMalha
    {
        enum Fase { Entradas, Travessia, Meio, Parede }

        const float RaioDoPonto = 11f;
        const float LadoDaLampada = 9f;
        const int ColunasDaParede = 18;

        RectTransform _malha;
        RectTransform _historico;
        RectTransform _escolhas;
        Text _legenda;

        readonly List<Image> _pontosDeEntrada = new();
        readonly List<Image> _pontosDoMeio = new();
        readonly List<Image> _lampadas = new();
        readonly List<(Image barra, int entrada, int neuronio, float comprimento)> _fios1 = new();
        readonly List<(Image barra, int neuronio, int palavra, float comprimento)> _fios2 = new();
        readonly List<Text> _rotulosDaJanela = new();
        readonly List<Button> _botoes = new();

        // ------------------------------------------------------------ montagem

        void MontarTela()
        {
            _pontosDeEntrada.Clear();
            _pontosDoMeio.Clear();
            _lampadas.Clear();
            _fios1.Clear();
            _fios2.Clear();
            _rotulosDaJanela.Clear();
            _botoes.Clear();

            // A frase que ela escreve fica em CIMA, e cresce para baixo — uma linha
            // por passe. É o laço ficando visível: cada linha é a malha inteira
            // rodando outra vez.
            _historico = Widgets.Painel("Histórico", Area, Color.clear);
            Widgets.Faixa(_historico, true, 56f);

            _malha = Widgets.Painel("Malha", Area, Color.clear);
            Widgets.Esticar(_malha);
            _malha.offsetMax = new Vector2(0f, -60f);
            _malha.offsetMin = new Vector2(0f, 112f);

            _legenda = Widgets.Texto("Legenda", Area, 12, TextAnchor.LowerCenter, Cores.Neblina);
            // Uma linha, não duas. A segunda dizia que cada neurônio se liga às 318
            // lâmpadas e que desenho só três — informação de uma vez só, que o
            // tutorial já dá. Permanente na tela ela custava altura de malha em todo
            // o resto da partida, e altura de malha é o conteúdo desta bancada.
            Widgets.Faixa(_legenda.rectTransform, false, 20f, 68f);
            _legenda.text = "dourado empurra a favor  ·  turquesa empurra contra  ·  " +
                            "neurônio escuro somou negativo";

            _escolhas = Widgets.Painel("Escolhas", Area, Color.clear);
            Widgets.Faixa(_escolhas, false, 58f, 2f);

            Canvas.ForceUpdateCanvases();
            MontarMalha();
        }

        /// <summary>
        /// Três colunas: a janela à esquerda, o meio no centro, a parede de lâmpadas
        /// à direita. As posições saem do retângulo MEDIDO, nunca da resolução de
        /// referência — a mesma aula roda no projetor 4:3 da sala e no notebook 16:9.
        /// </summary>
        void MontarMalha()
        {
            var espaco = _malha.rect.size;
            if (espaco.x < 1f)
            {
                Canvas.ForceUpdateCanvases();
                espaco = _malha.rect.size;
            }

            var xEntrada = -espaco.x * 0.42f;
            var xMeio = -espaco.x * 0.02f;
            var xParede = espaco.x * 0.30f;

            var entradas = MontarEntradas(xEntrada, espaco);
            _lugaresDoMeio = MontarMeio(xMeio, espaco);
            MontarParede(xParede, espaco);
            MontarFios1(entradas, _lugaresDoMeio);
        }

        List<Vector2> _lugaresDoMeio = new();

        /// <summary>
        /// Refaz o que muda de um passe para o outro: as conexões que vão até as três
        /// lâmpadas apostadas, os botões, e a luz zerada.
        ///
        /// A primeira camada e a parede NÃO são refeitas. É sempre a mesma rede — e
        /// remontá-la a cada passe faria a tela piscar, sugerindo que trocou de
        /// máquina quando o que trocou foi só a pergunta.
        /// </summary>
        void RefazerSaida()
        {
            foreach (var (barra, _, _, _) in _fios2)
                if (barra != null) Destroy(barra.gameObject);
            _fios2.Clear();

            foreach (var palavra in _candidatas)
            {
                var destino = LugarDaLampada(palavra);
                for (var j = 0; j < _lugaresDoMeio.Count; j++)
                {
                    var (barra, comprimento) = Fio($"f2_{j}_{palavra}",
                                                   _lugaresDoMeio[j], destino, 2.0f);
                    _fios2.Add((barra, j, palavra, comprimento));
                }
            }

            MontarBotoes();
            // Apaga tudo: a luz do passe anterior na tela com a janela nova em cima
            // seria a resposta velha ilustrando a pergunta nova.
            Iluminar(Fase.Entradas, 0f);
        }

        /// <summary>
        /// As três palavras da janela, cada uma com os seis números que a
        /// representam. São 18 pontos, agrupados de seis em seis.
        ///
        /// Agrupar importa: solto, o aluno vê dezoito entradas sem sentido. Em três
        /// grupos rotulados, ele vê o que é verdade — cada palavra virou seis
        /// números, e é só isso que a rede recebe.
        /// </summary>
        List<Vector2> MontarEntradas(float x, Vector2 espaco)
        {
            var lugares = new List<Vector2>();
            var janela = _frase.Skip(_frase.Count - _rede.Janela).ToList();

            // Os grupos ocupam 75% da altura e não 92%: a faixa vazia embaixo é o
            // espaço que o cartão do tutorial ocupa quando ele está no ar. Sem ela, a
            // foto mostrou o terceiro grupo de entradas cortado ao meio pelo cartão —
            // e o terceiro grupo é justamente a palavra mais recente da janela.
            var alturaDoGrupo = espaco.y * 0.21f;
            var vaoEntreGrupos = espaco.y * 0.035f;
            var total = _rede.Janela * alturaDoGrupo + (_rede.Janela - 1) * vaoEntreGrupos;
            var topo = total / 2f;

            for (var p = 0; p < _rede.Janela; p++)
            {
                var yGrupo = topo - p * (alturaDoGrupo + vaoEntreGrupos) - alturaDoGrupo / 2f;

                var rotulo = Widgets.Texto($"w{p}", _malha, 17, TextAnchor.MiddleRight, Cores.Papel);
                Widgets.Fixar(rotulo.rectTransform, new Vector2(0.5f, 0.5f),
                              new Vector2(x - 118f, yGrupo), new Vector2(196f, 26f));
                rotulo.text = janela[p];
                _rotulosDaJanela.Add(rotulo);

                for (var k = 0; k < _rede.Embutimento; k++)
                {
                    var y = yGrupo + (alturaDoGrupo / 2f)
                          - (k + 0.5f) * (alturaDoGrupo / _rede.Embutimento);

                    var ponto = Ponto($"e{p}_{k}", new Vector2(x, y), RaioDoPonto * 0.72f);
                    _pontosDeEntrada.Add(ponto);
                    lugares.Add(new Vector2(x, y));
                }
            }
            return lugares;
        }

        List<Vector2> MontarMeio(float x, Vector2 espaco)
        {
            var lugares = new List<Vector2>();
            var altura = espaco.y * 0.70f;

            for (var j = 0; j < _rede.Meio; j++)
            {
                var y = altura / 2f - (j + 0.5f) * (altura / _rede.Meio);
                _pontosDoMeio.Add(Ponto($"m{j}", new Vector2(x, y), RaioDoPonto));
                lugares.Add(new Vector2(x, y));
            }
            return lugares;
        }

        /// <summary>
        /// A parede: uma lâmpada por palavra do vocabulário, numa grade.
        ///
        /// Todas as 318, e é esse o ponto. Um painel com três lâmpadas diria que ela
        /// escolhe entre três; a parede inteira quase toda apagada diz a verdade —
        /// ela pesa TUDO o que sabe, toda vez, e quase tudo dá quase zero.
        /// </summary>
        void MontarParede(float x, Vector2 espaco)
        {
            var linhas = Mathf.CeilToInt(_rede.Palavras / (float)ColunasDaParede);
            var passo = LadoDaLampada + 3f;
            var largura = ColunasDaParede * passo;
            var altura = linhas * passo;

            for (var v = 0; v < _rede.Palavras; v++)
            {
                var coluna = v % ColunasDaParede;
                var linha = v / ColunasDaParede;

                var lampada = Widgets.Painel($"l{v}", _malha, Cores.TintaClara);
                Widgets.Fixar(lampada, new Vector2(0.5f, 0.5f),
                              new Vector2(x - largura / 2f + (coluna + 0.5f) * passo,
                                          altura / 2f - (linha + 0.5f) * passo),
                              new Vector2(LadoDaLampada, LadoDaLampada));
                _lampadas.Add(lampada.GetComponent<Image>());
            }

            var titulo = Widgets.Texto("Parede", _malha, 12, TextAnchor.UpperCenter, Cores.Neblina);
            Widgets.Fixar(titulo.rectTransform, new Vector2(0.5f, 0.5f),
                          new Vector2(x, altura / 2f + 16f), new Vector2(260f, 18f));
            titulo.text = $"uma lâmpada por palavra que ela sabe ({_rede.Palavras})";
        }

        Image Ponto(string nome, Vector2 onde, float raio)
        {
            var painel = Widgets.Painel(nome, _malha, Cores.TintaClara);
            Widgets.Fixar(painel, new Vector2(0.5f, 0.5f), onde, new Vector2(raio, raio));
            return painel.GetComponent<Image>();
        }

        /// <summary>
        /// As conexões. Cada uma é uma barra com o PIVÔ na ponta de origem, girada
        /// para o destino — é o que permite animar o comprimento e ver a luz
        /// atravessando em vez de aparecer pronta.
        /// </summary>
        void MontarFios1(List<Vector2> entradas, List<Vector2> meio)
        {
            for (var i = 0; i < entradas.Count; i++)
            {
                for (var j = 0; j < meio.Count; j++)
                {
                    var (barra, comprimento) = Fio($"f1_{i}_{j}", entradas[i], meio[j], 1.0f);
                    _fios1.Add((barra, i, j, comprimento));
                }
            }
        }

        (Image, float) Fio(string nome, Vector2 de, Vector2 para, float grossura)
        {
            var delta = para - de;
            var comprimento = delta.magnitude;

            var painel = Widgets.Painel(nome, _malha, Cores.TintaClara);
            painel.anchorMin = painel.anchorMax = new Vector2(0.5f, 0.5f);
            painel.pivot = new Vector2(0f, 0.5f);
            painel.anchoredPosition = de;
            painel.sizeDelta = new Vector2(0f, grossura);
            painel.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            // Atrás dos pontos: fio por cima de neurônio esconde justamente o que
            // acende no fim da travessia.
            painel.SetAsFirstSibling();
            return (painel.GetComponent<Image>(), comprimento);
        }

        Vector2 LugarDaLampada(int palavra)
        {
            var reto = (RectTransform)_lampadas[palavra].transform;
            return reto.anchoredPosition;
        }

        // -------------------------------------------------------------- iluminar

        /// <summary>
        /// Pinta a malha no instante <paramref name="fracao"/> de uma fase.
        ///
        /// Todas as fases anteriores continuam acesas: a luz não some atrás da
        /// frente. Uma frente que apaga o rastro pareceria uma bolinha viajando, que
        /// é exatamente a intuição errada que esta bancada existe para desmontar.
        /// </summary>
        void Iluminar(Fase fase, float fracao)
        {
            var entrou = fase >= Fase.Entradas;
            var atravessou = fase > Fase.Travessia;
            var respondeu = fase > Fase.Meio;

            for (var i = 0; i < _pontosDeEntrada.Count; i++)
            {
                var forca = Mathf.Abs(_rede.Entrada[i]) / 2.0f;
                var quanto = fase == Fase.Entradas ? fracao : entrou ? 1f : 0f;
                _pontosDeEntrada[i].color = Brilho(Cores.Papel, forca * quanto);
            }

            foreach (var (barra, entrada, neuronio, comprimento) in _fios1)
            {
                var quanto = fase == Fase.Travessia ? fracao : atravessou ? 1f : 0f;
                var empurrao = _rede.Empurrao1(entrada, neuronio);

                barra.rectTransform.sizeDelta =
                    new Vector2(comprimento * quanto, barra.rectTransform.sizeDelta.y);
                barra.color = Cor(empurrao, 3.0f, quanto > 0f ? 1f : 0f);
            }

            for (var j = 0; j < _pontosDoMeio.Count; j++)
            {
                var quanto = fase == Fase.Meio ? fracao : respondeu ? 1f : 0f;
                // Neurônio apagado fica apagado: a soma deu negativa, a ReLU zerou, e
                // ele não empurra nada adiante. É a verdade mais fácil de esconder sem
                // querer, e a mais fácil de mostrar.
                var forca = _rede.Ativacoes[j] / 10f;
                _pontosDoMeio[j].color = forca <= 0f
                    ? Cores.TintaClara
                    : Brilho(Cores.Luz, forca * quanto);
            }

            foreach (var (barra, neuronio, palavra, comprimento) in _fios2)
            {
                var quanto = fase == Fase.Parede ? fracao : fase > Fase.Parede ? 1f : 0f;
                var empurrao = _rede.Empurrao2(neuronio, palavra);

                barra.rectTransform.sizeDelta =
                    new Vector2(comprimento * quanto, barra.rectTransform.sizeDelta.y);
                barra.color = Cor(empurrao, 6.0f, quanto > 0f ? 1f : 0f);
            }

            for (var v = 0; v < _lampadas.Count; v++)
            {
                var quanto = fase == Fase.Parede ? fracao : fase > Fase.Parede ? 1f : 0f;
                // Raiz quarta da chance: sem ela, a vencedora com 60% ofusca a
                // segunda com 15% e a parede parece ter uma lâmpada só acesa. A conta
                // é honesta na barra numerada embaixo; aqui o serviço é o olho ver
                // que MUITAS acenderam um pouco.
                var forca = Mathf.Pow(_rede.Chances[v], 0.25f);
                _lampadas[v].color = Brilho(Cores.Luz, forca * quanto);
            }
        }

        /// <summary>
        /// Dourado empurra a favor, turquesa empurra contra — e o brilho tem que
        /// SEPARAR as conexões fortes das fracas.
        ///
        /// As escalas saíram de medir os 372 mil empurrões que a rede faz sobre o
        /// corpus, não de chute. A distribuição é muito torta: mediana 0,56, mas o
        /// percentil 90 é 2,7 e o máximo passa de 32. A primeira versão normalizava
        /// por 0,9 e o resultado foi uma manta — dois terços das conexões acendiam no
        /// máximo, e 216 linhas em opacidade cheia não são uma malha, são um tecido
        /// xadrez que não informa nada.
        ///
        /// Com escala 3 e expoente 1,5, a mediana sai quase invisível e só o décimo
        /// mais forte aparece. É o que faz o olho enxergar POR ONDE a luz de fato
        /// passou.
        /// </summary>
        static Color Cor(float empurrao, float escala, float aceso)
        {
            var forca = Mathf.Pow(Mathf.Clamp01(Mathf.Abs(empurrao) / escala), 1.5f) * aceso;
            var cor = empurrao >= 0f ? Cores.Luz : Cores.Vidro;
            return new Color(cor.r, cor.g, cor.b, 0.03f + 0.72f * forca);
        }

        static Color Brilho(Color cor, float forca)
        {
            var f = Mathf.Clamp01(forca);
            return Color.Lerp(Cores.TintaClara, cor, f);
        }

        // --------------------------------------------------------------- rodapé

        void MontarBotoes()
        {
            foreach (Transform filho in _escolhas) Destroy(filho.gameObject);
            _botoes.Clear();

            const float largura = 250f;
            for (var i = 0; i < _candidatas.Count; i++)
            {
                var palavra = _candidatas[i];
                var botao = Widgets.Botao($"o{i}", _escolhas, _rede.Palavra(palavra),
                                          Cores.Madeira, Cores.Papel, 18);
                Widgets.Fixar((RectTransform)botao.transform, new Vector2(0.5f, 0.5f),
                              new Vector2((i - (_candidatas.Count - 1) / 2f) * (largura + 14f), 0f),
                              new Vector2(largura, 46f));
                botao.onClick.AddListener(() => Apostar(palavra));
                _botoes.Add(botao);
            }
        }

        /// <summary>
        /// Redesenha o que muda entre um passe e outro: o histórico, os rótulos da
        /// janela, os botões e — depois do veredito — as porcentagens.
        ///
        /// A MALHA em si não é remontada. Ela é sempre a mesma rede, e remontá-la a
        /// cada passe faria a tela piscar e sugerir que trocou de máquina. O que
        /// muda entre passes é só o desenho de luz dentro dela.
        /// </summary>
        void Redesenhar()
        {
            DesenharHistorico();

            var janela = _frase.Skip(_frase.Count - _rede.Janela).ToList();
            for (var p = 0; p < _rotulosDaJanela.Count && p < janela.Count; p++)
                _rotulosDaJanela[p].text = janela[p];

            var revelado = _aposta >= 0 && !_rodando;

            for (var i = 0; i < _botoes.Count; i++)
            {
                var palavra = _candidatas[i];
                var ganhou = palavra == _vencedora;

                _botoes[i].interactable = _aposta < 0 && !_rodando && !Congelado;

                var fundo = _botoes[i].GetComponent<Image>();
                fundo.color = !revelado
                    ? (_aposta == palavra ? Cores.Tinta : Cores.Madeira)
                    : ganhou ? Cores.Folha
                    : _aposta == palavra ? Cores.Brasa
                    : Cores.TintaClara;

                Widgets.Rotular(_botoes[i], revelado
                    ? $"{_rede.Palavra(palavra)}   {_rede.Chances[palavra] * 100f:0}%"
                    : _rede.Palavra(palavra));
            }
        }

        void DesenharHistorico()
        {
            foreach (Transform filho in _historico) Destroy(filho.gameObject);

            var titulo = Widgets.Texto("t", _historico, 13, TextAnchor.UpperLeft, Cores.Neblina);
            Widgets.Faixa(titulo.rectTransform, true, 18f);
            titulo.rectTransform.offsetMin = new Vector2(14f, titulo.rectTransform.offsetMin.y);
            titulo.text = _linhas.Count == 0
                ? "a frase que ela vai escrever aparece aqui, uma linha por passe"
                : "cada linha é a malha inteira rodando outra vez";

            // Só as duas últimas: mais que isso não cabe na faixa de 56 pixels, e as
            // antigas já fizeram o serviço de mostrar que o laço se repete.
            // (O comentário dizia "quatro" e o código levava duas — sobra da vez em
            // que a faixa encolheu para o desenho da malha caber.)
            var mostradas = _linhas.Skip(Mathf.Max(0, _linhas.Count - 2)).ToList();
            for (var i = 0; i < mostradas.Count; i++)
            {
                var (janela, palavra, acertou, sozinha) = mostradas[i];

                // Três estados, três cores. Sem caractere de "certo": a fonte embutida
                // do Unity não tem glifo de visto e sairia um quadradinho vazio.
                var cor = sozinha ? Cores.Neblina : acertou ? Cores.Folha : Cores.Papel;

                var linha = Widgets.UmaLinha(
                    Widgets.Texto($"h{i}", _historico, 15, TextAnchor.UpperLeft, cor));
                Widgets.Faixa(linha.rectTransform, true, 17f, 20f + i * 17f);
                linha.rectTransform.offsetMin = new Vector2(26f, linha.rectTransform.offsetMin.y);
                linha.text = $"{janela}  →  {palavra}" +
                             (sozinha ? "     (ela tinha certeza)" : string.Empty);
            }
        }
    }
}
