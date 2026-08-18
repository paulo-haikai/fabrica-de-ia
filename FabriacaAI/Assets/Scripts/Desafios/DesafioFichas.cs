using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Bancada 4 — a mesa de corte de Nara.
    ///
    /// Inspiração: 2048 / THREES. Peças que se juntam para formar peças maiores,
    /// e cada junção muda o tabuleiro inteiro. A diferença é que aqui a junção
    /// não é um número dobrando: é um pedaço de palavra virando pedaço maior — e
    /// o placar que precisa CAIR é a quantidade de fichas.
    ///
    /// O aluno recebe palavras partidas em letras e um número limitado de
    /// junções. Cada junção emenda um par de pedaços vizinhos em TODAS as
    /// palavras, e a fila de fichas encurta. A meta é chegar a um número de
    /// fichas — o mesmo número que o algoritmo guloso alcançaria, então sempre
    /// existe caminho.
    ///
    /// A contagem NÃO é mostrada nos pares de propósito. Se mostrasse, bastaria
    /// clicar sempre no maior e não haveria decisão. Sem ela, o aluno precisa
    /// olhar as palavras e enxergar qual emenda se repete — que é exatamente a
    /// pergunta que o BPE responde, e a única forma de entendê-la é procurando.
    ///
    /// O que ele está rodando com a mão é BPE de verdade: é assim que quase todo
    /// modelo de linguagem em uso hoje corta texto.
    /// </summary>
    public partial class DesafioFichas : DesafioEmNiveis
    {
        public override string Etapa => "e4";
        public override string Titulo => "A mesa de corte";

        protected override int Niveis => _rodadas?.Count ?? Cortes.PorAula;

        const float AlturaFicha = 34f;
        const float AlturaLinha = 44f;

        List<Rodada4> _rodadas;
        Rodada4 _rodada;
        Oficina _oficina;

        RectTransform _mesa;
        RectTransform _tesouras;
        Text _placar;

        protected override void Preparar() => _rodadas = Cortes.Sortear(Rodadas.Semente());

        protected override void MontarNivel()
        {
            _rodada = _rodadas[NivelAtual];
            _oficina = new Oficina(_rodada.Palavras);
            _gastas = 0;

            Painel.Rodape("clique num par para emendá-lo em todas as palavras");

            var rotulo = Widgets.Texto("Rótulo", Area, 14, TextAnchor.UpperLeft, Cores.Neblina);
            Widgets.Faixa(rotulo.rectTransform, true, 18f);
            rotulo.text = "o texto cortado em fichas";

            _placar = Widgets.Texto("Placar", Area, 15, TextAnchor.UpperRight, Cores.Luz);
            Widgets.Faixa(_placar.rectTransform, true, 18f);

            _mesa = Widgets.Painel("Mesa", Area, Color.clear);
            Widgets.Faixa(_mesa, true, _rodada.Palavras.Length * AlturaLinha + 8f, 22f);

            var rotuloPares = Widgets.Texto("RótuloPares", Area, 14,
                                            TextAnchor.UpperLeft, Cores.Neblina);
            Widgets.Faixa(rotuloPares.rectTransform, true, 18f,
                          _rodada.Palavras.Length * AlturaLinha + 36f);
            rotuloPares.text = "emendas possíveis — qual delas se repete mais?";

            _tesouras = Widgets.Painel("Pares", Area, Color.clear);
            Widgets.Esticar(_tesouras);
            _tesouras.offsetMax = new Vector2(0f, -(_rodada.Palavras.Length * AlturaLinha + 58f));

            Redesenhar();
        }

        // ------------------------------------------------------------ pintura

        void Redesenhar()
        {
            foreach (Transform filho in _mesa) Destroy(filho.gameObject);
            foreach (Transform filho in _tesouras) Destroy(filho.gameObject);

            DesenharPalavras();
            DesenharPares();

            var fichas = _oficina.TotalFichas;
            var restam = Tesouradas - _gastas;

            _placar.text = $"fichas: {fichas}  ·  meta: {_rodada.Meta}  ·  " +
                           $"emendas: {_oficina.Fusoes.Count} de {_rodada.Fusoes}  ·  " +
                           $"tesouradas: {restam} de {Tesouradas}";
            Widgets.Contar(_placar, restam);

            if (fichas <= _rodada.Meta)
            {
                Vencer();
                return;
            }

            // As tesouradas acabaram: fim de jogo DURO, sem desfazer.
            //
            // Esta bancada se inspira no 2048, e faltava dele justamente a parte que
            // faz o 2048 ser jogo — o tabuleiro que TRAVA. Antes, quando as emendas
            // acabavam, o aluno recebia "desfazer a última" e podia refazer para
            // sempre; o orçamento de emendas limitava o comprimento da solução, não o
            // número de tentativas. Escolher o par certo e sair clicando valiam igual.
            if (_gastas >= Tesouradas)
            {
                Acabaram(fichas);
                return;
            }

            if (_oficina.Fusoes.Count >= _rodada.Fusoes)
            {
                Painel.Instruir($"as emendas acabaram e sobraram {fichas} fichas — " +
                                "desfaça e tente outro corte", Cores.Brasa);
                Painel.Acao("desfazer a última", Desfazer);
                return;
            }

            Painel.Instruir(restam <= 2
                ? $"faltam {fichas - _rodada.Meta} fichas — e quase não há tesourada"
                : $"faltam {fichas - _rodada.Meta} fichas para a meta");
            Painel.Acao(_oficina.Fusoes.Count > 0 ? "desfazer a última" : null,
                        _oficina.Fusoes.Count > 0 ? Desfazer : (System.Action)null);
        }

        void DesenharPalavras()
        {
            // As fichas se ancoram no CENTRO da mesa, não no topo dela. Contar as
            // linhas de cima para baixo a partir do zero — como a primeira versão
            // fazia — empurrava as palavras metade da mesa para baixo, e elas
            // acabavam por cima da fileira de emendas.
            var alturaDaMesa = _oficina.Cortes.Count * AlturaLinha;

            for (var i = 0; i < _oficina.Cortes.Count; i++)
            {
                var pedacos = _oficina.Cortes[i];
                var larguras = pedacos.Select(p => 14f + p.Length * 11f).ToArray();
                var total = larguras.Sum() + (pedacos.Count - 1) * 4f;
                var x = -total / 2f;
                var y = alturaDaMesa / 2f - (i * AlturaLinha + AlturaLinha / 2f);

                for (var k = 0; k < pedacos.Count; k++)
                {
                    // Pedaço de uma letra é matéria-prima; emendado já é ficha
                    // conquistada. A cor diferente mostra o progresso sem placar.
                    var emendado = pedacos[k].Length > 1;
                    Widgets.Ficha($"f{i}_{k}", _mesa, pedacos[k],
                                  new Vector2(x + larguras[k] / 2f, y),
                                  new Vector2(larguras[k], AlturaFicha),
                                  emendado ? Cores.Madeira : Cores.TintaClara,
                                  emendado ? Cores.Luz : Cores.Papel, 16);
                    x += larguras[k] + 4f;
                }
            }
        }

        void DesenharPares()
        {
            // Só os pares que ainda podem render algo. Mostrar todos encheria a
            // tela de emendas que aparecem uma vez só e não levam a nada.
            var pares = _oficina.Pares().Where(p => p.vezes > 1).Take(12).ToList();
            if (pares.Count == 0) pares = _oficina.Pares().Take(12).ToList();

            // Ordem alfabética na tela, e não por frequência: em ordem de
            // frequência o primeiro botão seria sempre a resposta certa.
            pares = pares.OrderBy(p => p.a + p.b, System.StringComparer.Ordinal).ToList();

            const int porFileira = 6;
            const float largura = 120f;
            const float altura = 40f;

            var podeEmendar = _oficina.Fusoes.Count < _rodada.Fusoes;

            for (var i = 0; i < pares.Count; i++)
            {
                var (a, b, _) = pares[i];
                var coluna = i % porFileira;
                var fileira = i / porFileira;

                var botao = Widgets.Botao($"par{i}", _tesouras, $"{a}+{b}",
                                          podeEmendar ? Cores.Madeira : Cores.TintaClara,
                                          podeEmendar ? Cores.Papel : Cores.Neblina, 16);
                Widgets.Fixar((RectTransform)botao.transform, new Vector2(0.5f, 1f),
                    new Vector2((coluna - (porFileira - 1) / 2f) * (largura + 8f),
                                -(altura / 2f + fileira * (altura + 8f))),
                    new Vector2(largura, altura));

                var pa = a;
                var pb = b;
                botao.onClick.AddListener(() => Emendar(pa, pb));
                botao.interactable = podeEmendar;
            }
        }

        // ------------------------------------------------------------- jogadas

        /// <summary>
        /// Tesouradas por rodada: cada emenda TENTADA gasta uma, desfeita ou não.
        ///
        /// É o recurso escasso que faltava. Sem ele, desfazer era grátis e infinito, e
        /// a bancada premiava paciência em vez de leitura.
        /// </summary>
        int Tesouradas => _rodada.Fusoes * 2 + 2;

        int _gastas;

        void Emendar(string a, string b)
        {
            if (_oficina.Fusoes.Count >= _rodada.Fusoes) return;
            if (_gastas >= Tesouradas) return;

            _gastas++;
            _oficina.Fundir(a, b);
            Redesenhar();
        }

        /// <summary>
        /// Desfaz a última emenda — e a tesourada gasta NÃO volta.
        ///
        /// É essa assimetria que dá peso ao clique. Desfazer continua de graça no
        /// sentido que importa (o tabuleiro volta, nada fica travado por engano), mas
        /// custa no recurso — então errar sai mais barato que não tentar, e mais caro
        /// que ler a mesa antes.
        /// </summary>
        void Desfazer()
        {
            _oficina.Desfazer();
            Redesenhar();
        }

        /// <summary>
        /// As tesouradas acabaram longe da meta. Fim de rodada, sem volta.
        ///
        /// A lição sobrevive à derrota, e é a mesma da vitória vista do outro lado: ele
        /// gastou emendas em pares que apareciam pouco, então cada corte encurtou uma
        /// palavra só. O par que se repete é o que paga.
        /// </summary>
        void Acabaram(int fichas)
        {
            var letras = _rodada.Palavras.Sum(p => p.Length);

            Falhou($"Pararam em {fichas} fichas, a meta era {_rodada.Meta}",
                $"Você saiu de {letras} letras e chegou a {fichas}.\n\n" +
                "O que decide aqui não é quantas emendas você dá — é QUAIS. Emenda\n" +
                "num par que aparece uma vez encurta uma palavra; num par que se\n" +
                "repete em quatro, encurta quatro de uma vez.\n\n" +
                "É por isso que a máquina escolhe sempre o par mais frequente\n" +
                "primeiro. Ela não é esperta: ela conta.",
                contaEstrela: fichas < letras * 0.75f);
        }

        void Vencer()
        {
            var vocabulario = _oficina.Vocabulario;
            var letras = _rodada.Palavras.Sum(p => p.Length);

            Painel.Instruir($"chegou: {_oficina.TotalFichas} fichas", Cores.Folha);

            Resolveu($"De {letras} letras para {_oficina.TotalFichas} fichas",
                $"Você guardou {vocabulario} pedaços e escreveu {_rodada.Palavras.Length} " +
                $"palavras com {_oficina.TotalFichas} fichas.\n\n" +
                "É a troca de toda máquina de linguagem: poucos pedaços, sequência\n" +
                "longa — muitos pedaços, sequência curta. Isso que você fez à mão\n" +
                "chama-se BPE, e é como quase todo modelo hoje corta o texto.");
        }
    }
}
