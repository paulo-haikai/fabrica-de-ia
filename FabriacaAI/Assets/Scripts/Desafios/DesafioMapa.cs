using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Bancada 5 — o mapa de Bento.
    ///
    /// Inspiração: CONNECTIONS, do New York Times. Dezesseis palavras, quatro
    /// grupos de quatro, e a graça está em que uma palavra parece caber em dois
    /// grupos ao mesmo tempo. Aqui são doze palavras e três grupos — a turma tem
    /// noventa minutos e o resto da aula pela frente.
    ///
    /// A diferença que faz a bancada ensinar: no Connections os grupos são
    /// temáticos, decididos por um humano. Aqui eles saem da COOCORRÊNCIA do
    /// corpus — palavras que aparecem nos mesmos lugares das frases, medidas por
    /// cosseno entre vetores de vizinhança (ver <see cref="Vizinhancas"/>).
    ///
    /// Isso produz o momento inteiro da etapa. O aluno agrupa "lousa, quadro,
    /// mesa" e acerta — e então descobre que a máquina montou o mesmo grupo sem
    /// saber o que nenhuma das três palavras significa. Ela só notou que as três
    /// aparecem depois de "na" e antes de "e". O significado, para ela, é o
    /// endereço.
    ///
    /// Três vidas, como no Connections: erro custa, mas não encerra. Sem custo o
    /// aluno tentaria todas as combinações por força bruta e não pensaria em
    /// nenhuma.
    /// </summary>
    public partial class DesafioMapa : DesafioEmNiveis
    {
        public override string Etapa => "e5";
        public override string Titulo => "O mapa das palavras";

        protected override int Niveis => _rodadas?.Count ?? Mapas.PorAula;

        const int Vidas = 3;
        const float LarguraCarta = 216f;
        const float AlturaCarta = 76f;
        const float Folga = 10f;

        Vizinhancas _mapa;
        List<List<Grupo>> _rodadas;
        List<Grupo> _rodada;

        readonly List<Grupo> _resolvidos = new();
        readonly List<string> _selecionadas = new();
        readonly Dictionary<string, Button> _cartas = new();
        List<string> _naMesa;
        int _errosRestantes;

        RectTransform _acertos;
        RectTransform _mesa;
        Text _vidas;

        protected override void Preparar()
        {
            _mapa = new Vizinhancas(Corpus.Frases);
            _rodadas = Mapas.Sortear(_mapa, Rodadas.Semente());
        }

        protected override void MontarNivel()
        {
            _rodada = _rodadas[NivelAtual];
            _resolvidos.Clear();
            _selecionadas.Clear();
            _cartas.Clear();
            _errosRestantes = Vidas;

            _naMesa = Embaralhar(_rodada.SelectMany(g => g.Palavras).ToList());

            // O rodapé diz o VERBO; a teoria fica para depois do acerto.
            //
            // A versão anterior escrevia aqui a regra do jogo inteira — "as
            // palavras de um grupo andam com a MESMA companhia, leia as duas
            // linhas de baixo" —, uma frase de teoria na letra mais miúda da
            // tela. Quem pula o tutorial (e a maioria pula) chegava numa mesa de
            // doze cartas sem saber sequer que elas se clicam.
            Painel.Rodape("toque nas palavras para juntá-las · 4 formam um grupo");

            _vidas = Widgets.Texto("Vidas", Area, 15, TextAnchor.UpperRight, Cores.Luz);
            Widgets.Faixa(_vidas.rectTransform, true, 20f);

            var rotulo = Widgets.Texto("Rótulo", Area, 14, TextAnchor.UpperLeft, Cores.Neblina);
            Widgets.Faixa(rotulo.rectTransform, true, 20f);
            rotulo.text = "três grupos de quatro — agrupe pela companhia, não pelo assunto";

            _acertos = Widgets.Painel("Acertos", Area, Color.clear);
            Widgets.Faixa(_acertos, true, 3f * 30f, 22f);

            _mesa = Widgets.Painel("Mesa", Area, Color.clear);
            Widgets.Esticar(_mesa);
            _mesa.offsetMax = new Vector2(0f, -(22f + 3f * 30f + 6f));

            Redesenhar();
        }

        static string Juntar(List<string> palavras) =>
            palavras.Count == 0 ? "—" : string.Join(", ", palavras);

        List<string> Embaralhar(List<string> fonte)
        {
            var sorteio = new Mulberry32((uint)(Rodadas.Semente() + NivelAtual * 7717));
            for (var i = fonte.Count - 1; i > 0; i--)
            {
                var j = (int)(sorteio.Proximo() * (i + 1));
                (fonte[i], fonte[j]) = (fonte[j], fonte[i]);
            }
            return fonte;
        }

        // ------------------------------------------------------------ pintura

        void Redesenhar()
        {
            foreach (Transform filho in _acertos) Destroy(filho.gameObject);
            foreach (Transform filho in _mesa) Destroy(filho.gameObject);
            _cartas.Clear();

            DesenharResolvidos();
            DesenharMesa();

            _vidas.text = $"erros que ainda pode dar: {_errosRestantes}";
            // O último erro tem que doer mais que o primeiro, e com três vidas o
            // alarme só faz sentido na última: alarme cedo demais vira paisagem.
            Widgets.Contar(_vidas, _errosRestantes, 1);

            if (_resolvidos.Count == Mapas.Grupos)
            {
                Vencer();
                return;
            }

            if (_errosRestantes <= 0)
            {
                Perder();
                return;
            }

            // A instrução carrega a TAREFA, e não o placar da tarefa.
            //
            // "escolhidas: 0 de 4" responde uma pergunta que ninguém fez. A
            // pergunta que o aluno tem na cabeça ao chegar é "o que eu faço
            // aqui?", e a única linha da tela que ele lê de verdade — a do meio,
            // grande — não respondia.
            var faltam = Mapas.PorGrupo - _selecionadas.Count;
            Painel.Instruir(
                _selecionadas.Count == 0
                    ? "junte 4 palavras que aparecem nos MESMOS lugares"
                    : faltam == 0
                        ? "confira o grupo"
                        : $"faltam {faltam} — compare as duas linhas de cada carta");

            Painel.Acao(_selecionadas.Count == Mapas.PorGrupo ? "conferir grupo" : null,
                        _selecionadas.Count == Mapas.PorGrupo ? Conferir : (System.Action)null);
        }

        /// <summary>
        /// Os grupos fechados, e as VAGAS dos que faltam.
        ///
        /// A vaga vazia é o objetivo virando forma. Sem ela, a mesa de doze
        /// cartas não diz quantos grupos existem nem que existe um fim — o aluno
        /// junta quatro palavras, acerta, e só então descobre que havia mais.
        /// Três retângulos vazios no topo respondem "quanto falta" de relance, e
        /// vão sendo preenchidos: é o mesmo contrato das bolinhas de progresso do
        /// cabeçalho, aplicado ao conteúdo da bancada.
        /// </summary>
        void DesenharResolvidos()
        {
            var total = _rodada?.Count ?? 0;
            for (var i = _resolvidos.Count; i < total; i++)
            {
                var vaga = Widgets.Painel($"vaga{i}", _acertos, Cores.TintaClara);
                Widgets.Fixar(vaga, new Vector2(0.5f, 1f),
                              new Vector2(0f, -(15f + i * 30f)), new Vector2(860f, 26f));

                var espera = Widgets.Texto("t", vaga, 13, TextAnchor.MiddleCenter, Cores.Neblina);
                Widgets.Esticar(espera.rectTransform);
                espera.text = "grupo por achar";
            }

            for (var i = 0; i < _resolvidos.Count; i++)
            {
                var grupo = _resolvidos[i];
                var faixa = Widgets.Painel($"g{i}", _acertos, Cores.Folha);
                Widgets.Fixar(faixa, new Vector2(0.5f, 1f),
                              new Vector2(0f, -(15f + i * 30f)), new Vector2(860f, 26f));

                var texto = Widgets.Texto("t", faixa, 15, TextAnchor.MiddleCenter, Cores.Papel);
                Widgets.Esticar(texto.rectTransform);
                // O nome do grupo é a palavra-semente: "aparecem onde 'lousa'
                // aparece". Nomear por tema seria mentira — a máquina não sabe
                // o tema, ela sabe o lugar.
                texto.text = $"aparecem onde “{grupo.Semente}” aparece:   " +
                             string.Join("  ·  ", grupo.Palavras);
            }
        }

        void DesenharMesa()
        {
            var disponiveis = _naMesa
                .Where(p => !_resolvidos.Any(g => g.Palavras.Contains(p)))
                .ToList();

            // Tamanho medido, não chutado.
            //
            // Doze cartas com duas linhas de vizinhança cada não caberiam num
            // tamanho fixo: a área muda com a proporção da tela e com quantos
            // grupos já foram resolvidos. Medir o retângulo e dividir é o que
            // impede a última fileira de sair pela borda — foi exatamente esse o
            // defeito da primeira versão desta bancada.
            const int porFileira = 4;
            var fileiras = Mathf.CeilToInt((float)disponiveis.Count / porFileira);

            Canvas.ForceUpdateCanvases();
            var espaco = _mesa.rect.size;

            var altura = Mathf.Min(AlturaCarta,
                                   (espaco.y - fileiras * Folga) / Mathf.Max(1, fileiras));
            var largura = Mathf.Min(LarguraCarta,
                                    (espaco.x - porFileira * Folga) / porFileira);

            for (var i = 0; i < disponiveis.Count; i++)
            {
                var palavra = disponiveis[i];
                var coluna = i % porFileira;
                var fileira = i / porFileira;
                var escolhida = _selecionadas.Contains(palavra);

                var botao = Widgets.Botao($"c{i}", _mesa, string.Empty,
                                          escolhida ? Cores.Madeira : Cores.TintaClara,
                                          escolhida ? Cores.Luz : Cores.Papel);
                Widgets.Fixar((RectTransform)botao.transform, new Vector2(0.5f, 1f),
                    new Vector2((coluna - (porFileira - 1) / 2f) * (largura + Folga),
                                -(altura / 2f + fileira * (altura + Folga))),
                    new Vector2(largura, altura));
                Destroy(botao.GetComponentInChildren<Text>().gameObject);

                var nome = Widgets.Texto("n", (RectTransform)botao.transform, 17,
                                         TextAnchor.UpperCenter,
                                         escolhida ? Cores.Luz : Cores.Papel);
                Widgets.Faixa(nome.rectTransform, true, 24f, 6f);
                nome.text = palavra;

                // A COMPANHIA DA PALAVRA, impressa na carta.
                //
                // É o coração da bancada. Sem isto o aluno agrupa por
                // significado; com isto ele agrupa lendo a vizinhança — que é
                // exatamente o número que a máquina usou para montar os grupos.
                // Ele não está adivinhando o que a máquina pensou: está olhando o
                // que ela olhou.
                var esquerda = _mapa.PrincipaisVizinhos(palavra, true, 2);
                var direita = _mapa.PrincipaisVizinhos(palavra, false, 2);

                var companhia = Widgets.Texto("v", (RectTransform)botao.transform, 11,
                                              TextAnchor.LowerCenter, Cores.Neblina);
                Widgets.Faixa(companhia.rectTransform, false, 32f, 4f);
                companhia.text = $"depois de: {Juntar(esquerda)}\nantes de: {Juntar(direita)}";

                var alvo = palavra;
                botao.onClick.AddListener(() => Alternar(alvo));
                _cartas[palavra] = botao;
            }
        }

        // ------------------------------------------------------------- jogadas

        void Alternar(string palavra)
        {
            if (_selecionadas.Remove(palavra))
            {
                Redesenhar();
                return;
            }
            if (_selecionadas.Count >= Mapas.PorGrupo) return;
            _selecionadas.Add(palavra);
            Redesenhar();
        }

        void Conferir()
        {
            var acertou = _rodada.FirstOrDefault(
                g => !_resolvidos.Contains(g) &&
                     _selecionadas.All(p => g.Palavras.Contains(p)));

            if (acertou != null)
            {
                _resolvidos.Add(acertou);
                _selecionadas.Clear();
                Painel.Instruir($"grupo de “{acertou.Semente}” fechado", Cores.Folha);
                Redesenhar();
                return;
            }

            _errosRestantes--;

            // Gesto em vez de frase: a mesma pista que o texto dava ("três destas
            // quatro são do mesmo grupo"), só que sem texto — tremor nas quatro
            // escolhidas, brilho nas que de fato pertenciam ao mesmo grupo.
            var melhor = _rodada
                .Where(g => !_resolvidos.Contains(g))
                .OrderByDescending(g => _selecionadas.Count(p => g.Palavras.Contains(p)))
                .First();
            var erradas = new List<string>(_selecionadas);

            _selecionadas.Clear();
            Redesenhar();

            // Só depois do redesenho: as cartas de antes já foram destruídas, e
            // animar um objeto morto não mostra nada.
            foreach (var palavra in erradas)
            {
                if (!_cartas.TryGetValue(palavra, out var carta)) continue;
                var retangulo = (RectTransform)carta.transform;
                Widgets.Tremer(retangulo);
                if (melhor.Palavras.Contains(palavra))
                    Widgets.Lampejo(retangulo, Cores.Luz);
            }
        }

        void Vencer()
        {
            Resolveu("Os três grupos, fechados",
                "Você juntou essas palavras porque sabe o que significam.\n\n" +
                "A máquina montou os MESMOS grupos sem saber o significado de\n" +
                "nenhuma — só contou quem andava com quem: o que vinha antes,\n" +
                "o que vinha depois. Vizinhança parecida é o sentido, para ela.");
        }

        void Perder()
        {
            var resposta = string.Join("\n", _rodada.Select(
                g => $"   {g.Semente}:  {string.Join("  ·  ", g.Palavras)}"));

            Falhou("Os erros acabaram",
                "Os grupos que a máquina tinha montado eram estes:\n\n" + resposta +
                "\n\nNão são grupos de assunto — são grupos de LUGAR: palavras\n" +
                "que caem no mesmo ponto da frase.",
                contaEstrela: _resolvidos.Count >= 2);
        }
    }
}
