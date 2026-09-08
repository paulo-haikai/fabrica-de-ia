using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Bancada 2 — o dominó de Dona Ciça.
    ///
    /// Inspiração: DOMINÓ. Não é analogia frouxa — é a mesma estrutura. Uma peça
    /// de dominó tem duas pontas e só encaixa se a ponta bate; um bigrama tem
    /// duas palavras e só serve se a palavra bate. Encaixar peças até formar uma
    /// corrente é, literalmente, o que a máquina faz para escrever uma frase.
    ///
    /// O aluno recebe uma mão de peças — pares que o arquivo de Dona Ciça
    /// realmente contou, com o número de risquinhos impresso — e precisa ligar a
    /// palavra inicial até a palavra-alvo. Lendo a corrente da esquerda para a
    /// direita sai uma frase.
    ///
    /// O que faz disto um jogo:
    ///   · ENCAIXE — a regra é física e se aprende sem explicação: bate ou não bate;
    ///   · MÃO LIMITADA — o número de peças é justo, e há engodos que encaixam
    ///     perfeitamente e levam para o lugar errado;
    ///   · BECO SEM SAÍDA — colocar a peça errada pode travar a corrente, e ver a
    ///     máquina emudecer no meio é a lição da bancada 3 chegando cedo;
    ///   · DESFAZER — sem custo, sempre. É a regra que o Baba Is You ensinou:
    ///     experimentar precisa ser de graça, ou o aluno para de experimentar.
    ///
    /// As rodadas nascem de uma caminhada pelo grafo do corpus (ver
    /// <see cref="Dominos"/>), então há solução garantida e nunca se repetem.
    /// </summary>
    public partial class DesafioDominos : DesafioEmNiveis
    {
        public override string Etapa => "e2";
        public override string Titulo => "O dominó das palavras";

        protected override int Niveis => _rodadas?.Count ?? Dominos.PorAula;

        // Medidas generosas: metade da turma vê isto projetado na parede.
        const float LarguraPeca = 188f;
        const float AlturaPeca = 50f;
        const float FolgaPeca = 10f;

        Bigrama _arquivo;
        List<Rodada2> _rodadas;
        Rodada2 _rodada;

        /// <summary>
        /// Elos que o aluno pode GASTAR na rodada, contando os desfeitos.
        ///
        /// Aqui estava o defeito que fazia esta bancada não ser jogo. `MaxPecas` limita
        /// o comprimento da corrente ATIVA, não o número de tentativas — e desfazer era
        /// grátis e infinito. Ou seja: dava para testar todos os caminhos possíveis até
        /// achar o certo, e escolher bem valia exatamente o mesmo que escolher ao acaso.
        ///
        /// Com orçamento, o engodo passa a custar. Ele encaixa, você gasta um elo nele,
        /// e volta com um elo a menos — que é exatamente o dilema que a bancada quer
        /// dar: o par existe no arquivo e ainda assim é o caminho errado.
        ///
        /// A folga é generosa de propósito (o caminho certo mais dois desvios inteiros),
        /// porque o objetivo é criar peso na escolha, não punir quem explora.
        /// </summary>
        int Orcamento => _rodada.MaxPecas * 2 + 2;

        int _gastos;

        /// <summary>As peças ainda na mão, na ordem em que foram sorteadas.</summary>
        readonly List<Peca> _mao = new();
        /// <summary>A corrente montada, da primeira peça à última.</summary>
        readonly List<Peca> _corrente = new();

        RectTransform _mesa;
        RectTransform _bancada;
        Text _placar;

        protected override void Preparar()
        {
            _arquivo = new Bigrama(Corpus.Frases);
            _rodadas = Dominos.Sortear(_arquivo, Rodadas.Semente());
        }

        protected override void MontarNivel()
        {
            _rodada = _rodadas[NivelAtual];
            _mao.Clear();
            _mao.AddRange(_rodada.Mao);
            _corrente.Clear();
            _gastos = 0;

            Painel.Rodape("clique numa peça para encaixar · a ponta esquerda tem que bater");

            var alvo = Widgets.Texto("Alvo", Area, 18, TextAnchor.UpperCenter, Cores.Neblina);
            Widgets.Faixa(alvo.rectTransform, true, 22f);
            alvo.text = $"comece em “{_rodada.Inicio}”   e chegue em “{_rodada.Alvo}”";

            _mesa = Widgets.Painel("Mesa", Area, Color.clear);
            Widgets.Faixa(_mesa, true, 118f, 26f);

            _placar = Widgets.Texto("Placar", Area, 15, TextAnchor.UpperRight, Cores.Luz);
            Widgets.Faixa(_placar.rectTransform, true, 20f, 148f);

            var rotulo = Widgets.Texto("Rótulo", Area, 14, TextAnchor.UpperLeft, Cores.Neblina);
            Widgets.Faixa(rotulo.rectTransform, true, 20f, 148f);
            rotulo.text = "peças na sua mão";

            _bancada = Widgets.Painel("Peças", Area, Color.clear);
            Widgets.Esticar(_bancada);
            _bancada.offsetMax = new Vector2(0f, -172f);

            Redesenhar();
        }

        // -------------------------------------------------------------- regras

        /// <summary>A palavra em que a corrente está parada agora.</summary>
        string Ponta => _corrente.Count == 0 ? _rodada.Inicio : _corrente[^1].Direita;

        bool Encaixa(Peca peca) => peca.Esquerda == Ponta;

        /// <summary>As palavras da corrente, do início até a ponta.</summary>
        List<string> Palavras()
        {
            var palavras = new List<string> { _rodada.Inicio };
            palavras.AddRange(_corrente.Select(p => p.Direita));
            return palavras;
        }

        /// <summary>
        /// Chegou no alvo. Não é vitória por si — ver <see cref="Venceu"/>.
        /// </summary>
        bool NoAlvo => Ponta == _rodada.Alvo;

        /// <summary>
        /// Venceu: chegou no alvo POR UM CAMINHO QUE É FRASE.
        ///
        /// A segunda metade da condição é a bancada inteira. Antes bastava chegar
        /// no alvo, e o jogo aceitava "amanhã a aula de matemática ficou" — todos os
        /// pares existem no arquivo, nenhuma pessoa escreveu aquilo. Dar isso por
        /// correto ensinava que contar pares basta para escrever, que é o contrário
        /// do que a bancada existe para mostrar.
        ///
        /// Agora o absurdo é jogável e continua sendo lição: montar a corrente
        /// absurda é possível, chegar ao alvo com ela é possível, e o que o jogo diz
        /// nesse momento é POR QUE aquilo não vale.
        /// </summary>
        bool Venceu => NoAlvo && Corpus.EhTrechoReal(Palavras());

        /// <summary>
        /// Travou: a corrente não chegou ao alvo e nenhuma peça da mão encaixa na
        /// ponta. É o beco sem saída — e ele é informação, não castigo.
        /// </summary>
        bool Travou => !Venceu && !_mao.Any(Encaixa);

        void Encaixar(Peca peca)
        {
            if (!Encaixa(peca) || Venceu) return;
            if (_corrente.Count >= _rodada.MaxPecas) return;

            _mao.Remove(peca);
            _corrente.Add(peca);
            _gastos++;
            Redesenhar();
        }

        void Desfazer()
        {
            if (_corrente.Count == 0) return;
            var ultima = _corrente[^1];
            _corrente.RemoveAt(_corrente.Count - 1);
            _mao.Add(ultima);
            Redesenhar();
        }

        // ------------------------------------------------------------- pintura

        /// <summary>
        /// Redesenha mesa e mão do zero a cada jogada.
        ///
        /// São no máximo dez peças; o custo é invisível. E redesenhar tudo é o que
        /// impede a classe de bug mais comum deste tipo de tela — peça que
        /// continua na mão depois de ter sido jogada, ou corrente que mostra uma
        /// ponta que já mudou.
        /// </summary>
        void Redesenhar()
        {
            foreach (Transform filho in _mesa) Destroy(filho.gameObject);
            foreach (Transform filho in _bancada) Destroy(filho.gameObject);

            DesenharCorrente();
            DesenharMao();

            var restam = Orcamento - _gastos;
            _placar.text = $"corrente: {_corrente.Count} de {_rodada.MaxPecas}" +
                           $"        elos: {restam} de {Orcamento}";
            Widgets.Contar(_placar, restam);

            if (Venceu)
            {
                Painel.Instruir($"“{Frase()}”", Cores.Folha);
                Painel.Acao("mandar ela falar", Falar);
                return;
            }

            // Chegou no alvo, e a corrente não é frase de ninguém. O recado diz
            // ONDE ela deixou de ser texto real — sem isso o aluno teria que testar
            // caminhos às cegas, e a dica é justamente o conteúdo da bancada.
            // A derrota vem PRIMEIRO, antes de oferecer desfazer: sem elo no bolso não
            // há o que refazer, e oferecer o botão seria mentir sobre a saída.
            if (_gastos >= Orcamento && !Venceu)
            {
                Acabaram();
                return;
            }

            if (NoAlvo)
            {
                var palavras = Palavras();
                var quebra = Corpus.OndeQuebra(palavras);
                var culpada = quebra > 0 && quebra < palavras.Count
                    ? $" — ninguém nunca escreveu “{palavras[quebra - 1]} {palavras[quebra]}”" +
                      " no meio de uma frase"
                    : string.Empty;

                Painel.Instruir($"chegou no alvo, mas essa frase não existe{culpada}", Cores.Brasa);
                Painel.Acao("desfazer a última", Desfazer);
                return;
            }

            if (Travou)
            {
                Painel.Instruir($"a corrente morreu em “{Ponta}” — nenhuma peça encaixa aí",
                                Cores.Brasa);
                Painel.Acao("desfazer a última", Desfazer);
                return;
            }

            if (_corrente.Count >= _rodada.MaxPecas)
            {
                Painel.Instruir("as peças acabaram e ela não chegou no alvo", Cores.Brasa);
                Painel.Acao("desfazer a última", Desfazer);
                return;
            }

            Painel.Instruir(_corrente.Count == 0
                ? $"encaixe uma peça que comece com “{Ponta}”"
                : $"a corrente está em “{Ponta}”");
            Painel.Acao(_corrente.Count > 0 ? "desfazer a última" : null,
                        _corrente.Count > 0 ? Desfazer : (System.Action)null);
        }

        /// <summary>
        /// A corrente na mesa. As peças se sobrepõem na palavra compartilhada,
        /// como dominó de verdade: a ponta direita de uma É a ponta esquerda da
        /// seguinte, e aparece uma vez só.
        /// </summary>
        void DesenharCorrente()
        {
            var palavras = new List<string> { _rodada.Inicio };
            palavras.AddRange(_corrente.Select(p => p.Direita));

            var larguras = palavras.Select(p => 22f + p.Length * 11f).ToArray();
            var total = larguras.Sum() + (palavras.Count - 1) * 8f;
            var x = -total / 2f;

            for (var i = 0; i < palavras.Count; i++)
            {
                // A palavra inicial é dada; as outras foram conquistadas. Cor
                // diferente para o aluno ver o que ELE construiu.
                var dada = i == 0;
                var chegou = i == palavras.Count - 1 && Venceu;
                var fundo = chegou ? Cores.Folha : dada ? Cores.TintaClara : Cores.Madeira;

                Widgets.Ficha($"c{i}", _mesa, palavras[i],
                              new Vector2(x + larguras[i] / 2f, 14f),
                              new Vector2(larguras[i], 42f),
                              fundo, dada ? Cores.Neblina : Cores.Papel, 17);
                x += larguras[i];

                if (i >= palavras.Count - 1) continue;

                // A emenda entre duas peças, com os risquinhos daquele par.
                var emenda = Widgets.Ficha($"e{i}", _mesa, _corrente[i].Risquinhos.ToString(),
                                           new Vector2(x + 4f, 14f), new Vector2(8f, 42f),
                                           Cores.Luz, Cores.Tinta, 10);
                emenda.rectTransform.sizeDelta = new Vector2(20f, 14f);
                x += 8f;
            }

            var alvo = Widgets.UmaLinha(
                Widgets.Texto("Falta", _mesa, 14, TextAnchor.LowerCenter, Cores.Neblina));
            Widgets.Faixa(alvo.rectTransform, false, 18f);
            alvo.text = Venceu
                ? "chegou"
                : $"a ponta está em “{Ponta}” · falta chegar em “{_rodada.Alvo}”";
        }

        void DesenharMao()
        {
            const int porFileira = 4;
            for (var i = 0; i < _mao.Count; i++)
            {
                var peca = _mao[i];
                var coluna = i % porFileira;
                var fileira = i / porFileira;

                var pode = Encaixa(peca) && !Venceu && _corrente.Count < _rodada.MaxPecas;
                var botao = Widgets.Botao($"p{i}", _bancada, string.Empty,
                                          pode ? Cores.Madeira : Cores.TintaClara, Cores.Papel);
                Widgets.Fixar((RectTransform)botao.transform, new Vector2(0.5f, 1f),
                    new Vector2((coluna - (porFileira - 1) / 2f) * (LarguraPeca + FolgaPeca),
                                -(AlturaPeca / 2f + fileira * (AlturaPeca + FolgaPeca))),
                    new Vector2(LarguraPeca, AlturaPeca));

                // O rótulo do botão fica vazio; a peça é desenhada como dominó
                // de duas metades, que é o que faz a regra de encaixe ser óbvia.
                Destroy(botao.GetComponentInChildren<Text>().gameObject);
                DesenharPeca((RectTransform)botao.transform, peca, pode);

                var alvo = peca;
                botao.onClick.AddListener(() => Encaixar(alvo));
                botao.interactable = pode;
            }

            if (_mao.Count != 0) return;
            var vazio = Widgets.Texto("Vazio", _bancada, 15, TextAnchor.UpperCenter, Cores.Neblina);
            Widgets.Faixa(vazio.rectTransform, true, 20f, 10f);
            vazio.text = "a mão acabou";
        }

        /// <summary>Uma peça: metade esquerda, divisória, metade direita, risquinhos.</summary>
        static void DesenharPeca(RectTransform caixa, Peca peca, bool pode)
        {
            var tinta = pode ? Cores.Papel : Cores.Neblina;

            var esquerda = Widgets.Texto("esq", caixa, 15, TextAnchor.MiddleCenter, tinta);
            esquerda.rectTransform.anchorMin = new Vector2(0f, 0f);
            esquerda.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            esquerda.rectTransform.offsetMin = new Vector2(4f, 12f);
            esquerda.rectTransform.offsetMax = new Vector2(-2f, 0f);
            esquerda.text = peca.Esquerda;

            var divisoria = Widgets.Painel("div", caixa, Cores.Tinta);
            divisoria.anchorMin = new Vector2(0.5f, 0f);
            divisoria.anchorMax = new Vector2(0.5f, 1f);
            divisoria.pivot = new Vector2(0.5f, 0.5f);
            divisoria.anchoredPosition = Vector2.zero;
            divisoria.sizeDelta = new Vector2(2f, -10f);

            var direita = Widgets.Texto("dir", caixa, 15, TextAnchor.MiddleCenter, tinta);
            direita.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            direita.rectTransform.anchorMax = new Vector2(1f, 1f);
            direita.rectTransform.offsetMin = new Vector2(2f, 12f);
            direita.rectTransform.offsetMax = new Vector2(-4f, 0f);
            direita.text = peca.Direita;

            // Os risquinhos daquele par, no pé da peça. É o número que a bancada
            // anterior mandou o aluno contar, reaparecendo como propriedade da
            // peça — quanto mais risquinhos, mais a máquina gosta desse caminho.
            var marcas = Widgets.Texto("risq", caixa, 11, TextAnchor.LowerCenter,
                                       pode ? Cores.Luz : Cores.TintaClara);
            Widgets.Esticar(marcas.rectTransform);
            marcas.rectTransform.offsetMin = new Vector2(0f, 3f);
            marcas.text = new string('|', Mathf.Min(peca.Risquinhos, 12));
        }

        string Frase() => _rodada.Inicio + " " + string.Join(" ", _corrente.Select(p => p.Direita));

        /// <summary>
        /// Os elos acabaram sem a corrente virar frase.
        ///
        /// A bancada não tinha derrota nenhuma — e sem derrota, o engodo era só um
        /// clique a mais. O recado aqui diz o que ele gastou os elos fazendo, porque a
        /// resposta certa é essa mesma: ele gastou encaixando pares REAIS que não
        /// formavam frase, e isso é o limite do modelo, não a falha dele.
        /// </summary>
        void Acabaram()
        {
            var frase = Frase();
            var palavras = Palavras();
            var quebra = Corpus.OndeQuebra(palavras);

            Falhou("Os elos acabaram",
                $"Você parou em “{frase}”.\n\n" +
                (quebra > 0 && quebra < palavras.Count
                    ? $"A corrente deixou de ser frase em “{palavras[quebra - 1]} " +
                      $"{palavras[quebra]}”.\n\n"
                    : string.Empty) +
                "Toda peça que você encaixou existia — os risquinhos provam. E ainda\n" +
                "assim o caminho não deu frase.\n\n" +
                "É esse o tamanho do que contar pares sabe: as emendas, uma por uma.\n" +
                "A frase, não.",
                contaEstrela: _corrente.Count >= 2);
        }

        /// <summary>
        /// O fecho da bancada.
        ///
        /// A lição mudou de lugar quando a vitória passou a exigir frase real. Antes
        /// o recado era "olhe, você inventou uma frase encaixando pares" — e ele era
        /// falso, porque a corrente premiada podia ser um absurdo. Agora o recado
        /// está no que o aluno acabou de VIVER: ele tentou caminhos que encaixavam
        /// perfeitamente e foram recusados, e a diferença entre aqueles e este é a
        /// coisa que a máquina de contagem não sabe ver.
        /// </summary>
        void Falar()
        {
            var completa = Corpus.Frases.Contains(Frase());

            Resolveu($"“{Frase()}”",
                (completa
                    ? "Essa é, palavra por palavra, uma frase que Dona Ciça leu.\n\n"
                    : "Esse é um pedaço exato de uma frase que Dona Ciça leu.\n\n") +
                "Repare: outras peças encaixavam DE VERDADE — pares que\n" +
                "alguém escreveu, com os risquinhos para provar — e davam\n" +
                "frase nenhuma.\n\n" +
                "Aí está o limite de uma máquina de contar pares: ela conhece\n" +
                "as emendas, não a frase. Quem separou as duas foi você.");
        }
    }
}
