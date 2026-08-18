using System.Linq;
using FabricaDeIA.Engine;
using FabricaDeIA.Nucleo;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Bancada 7 — o mostrador de Seu Ilo.
    ///
    /// Inspiração: MASTERMIND. Você propõe uma combinação, a máquina responde com
    /// uma medida de quão longe você está, e você tem tentativas contadas. A
    /// diferença cruel: no Mastermind a resposta diz quantas peças estão no lugar
    /// certo; aqui ela é UM número só, o erro. Não diz qual botão está errado nem
    /// para que lado girar.
    ///
    /// E é exatamente essa a informação que uma rede neural recebe. Não existe
    /// ninguém dizendo "o peso 4.812 está alto demais". Existe um número, a
    /// perda, e a pergunta de como usá-lo.
    ///
    /// A rodada 1 tem dois botões e o aluno afina rindo. A 2 tem quatro e dá
    /// trabalho. A 3 tem seis e ele desiste — e a desistência é o conteúdo, porque
    /// a malha que ele teceu na bancada anterior tem centenas de fios. É o
    /// fracasso que dá sentido à bancada 8.
    /// </summary>
    public partial class DesafioErro : DesafioEmNiveis
    {
        public override string Etapa => "e7";
        public override string Titulo => "O tamanho do erro";

        protected override int Niveis => Rodadas7.Length;

        /// <summary>Botões e medições permitidas em cada rodada.</summary>
        static readonly (int botoes, int medicoes)[] Rodadas7 =
        {
            (2, 12),
            (4, 14),
            (6, 16)
        };

        /// <summary>Abaixo disto o mostrador conta como afinado.</summary>
        const float ErroBom = 0.06f;

        (int botoes, int medicoes) _r;
        Mostrador _mostrador;
        float _ultimoErro;
        float _melhorErro;
        bool _mediuAlgumaVez;

        RectTransform _painelBotoes;
        RectTransform _agulha;
        Text _leitura;
        Text _historico;

        protected override void MontarNivel()
        {
            _r = Rodadas7[NivelAtual];
            _mostrador = new Mostrador(_r.botoes, Rodadas.Semente() + NivelAtual * 977);
            _melhorErro = float.PositiveInfinity;
            _ultimoErro = _mostrador.Erro();
            _mediuAlgumaVez = false;

            Painel.Rodape("gire os botões e mande medir · o mostrador só diz o tamanho do erro");

            var alvo = Widgets.Texto("Alvo", Area, 16, TextAnchor.UpperCenter, Cores.Neblina);
            Widgets.Faixa(alvo.rectTransform, true, 22f);
            alvo.text = $"{_r.botoes} botões · {_r.medicoes} medições · " +
                        "deixe o erro perto de zero";

            MontarMostrador();
            MontarBotoes();

            _historico = Widgets.Texto("Histórico", Area, 14, TextAnchor.LowerCenter, Cores.Neblina);
            Widgets.Faixa(_historico.rectTransform, false, 20f, 4f);

            Atualizar();
        }

        void MontarMostrador()
        {
            var caixa = Widgets.Painel("Mostrador", Area, Color.clear);
            Widgets.Faixa(caixa, true, 96f, 28f);

            _leitura = Widgets.Texto("Leitura", caixa, 30, TextAnchor.UpperCenter, Cores.Luz);
            Widgets.Faixa(_leitura.rectTransform, true, 38f);

            // A agulha é uma barra que anda: o erro tem escala aberta, e barra
            // longa comunica "muito errado" melhor que um número grande.
            const float largura = 620f;
            _agulha = Widgets.Barra("Barra", caixa, new Vector2(0f, -18f),
                                    new Vector2(largura, 20f), Cores.TintaClara, Cores.Brasa);

            var regua = Widgets.Texto("Régua", caixa, 12, TextAnchor.LowerCenter, Cores.TintaClara);
            Widgets.Faixa(regua.rectTransform, false, 16f);
            regua.text = "afinado ← — — — — — — — — — — → desafinado";
        }

        void MontarBotoes()
        {
            _painelBotoes = Widgets.Painel("Botões", Area, Color.clear);
            Widgets.Esticar(_painelBotoes);
            _painelBotoes.offsetMax = new Vector2(0f, -132f);
            _painelBotoes.offsetMin = new Vector2(0f, 28f);

            var passo = Mathf.Min(150f, 820f / _r.botoes);

            for (var i = 0; i < _r.botoes; i++)
            {
                var botao = i;
                var x = (i - (_r.botoes - 1) / 2f) * passo;

                var titulo = Widgets.Texto($"t{i}", _painelBotoes, 13,
                                           TextAnchor.UpperCenter, Cores.Neblina);
                Widgets.Fixar(titulo.rectTransform, new Vector2(0.5f, 1f),
                              new Vector2(x, -8f), new Vector2(passo - 8f, 18f));
                titulo.text = $"botão {i + 1}";

                var mais = Widgets.Botao($"+{i}", _painelBotoes, "▲", Cores.Madeira, Cores.Papel, 16);
                Widgets.Fixar((RectTransform)mais.transform, new Vector2(0.5f, 1f),
                              new Vector2(x, -46f), new Vector2(52f, 34f));
                mais.onClick.AddListener(() => Girar(botao, +0.5f));

                var valor = Widgets.Texto($"v{i}", _painelBotoes, 20,
                                          TextAnchor.MiddleCenter, Cores.Luz);
                Widgets.Fixar(valor.rectTransform, new Vector2(0.5f, 1f),
                              new Vector2(x, -84f), new Vector2(passo - 8f, 30f));
                valor.name = $"valor{i}";

                var menos = Widgets.Botao($"-{i}", _painelBotoes, "▼", Cores.Madeira, Cores.Papel, 16);
                Widgets.Fixar((RectTransform)menos.transform, new Vector2(0.5f, 1f),
                              new Vector2(x, -122f), new Vector2(52f, 34f));
                menos.onClick.AddListener(() => Girar(botao, -0.5f));
            }
        }

        void Girar(int botao, float quanto)
        {
            _mostrador.Girar(botao, quanto);
            Atualizar();
        }

        // ----------------------------------------------------------- medições

        void Medir()
        {
            var erro = _mostrador.Medir();
            var melhorou = _mediuAlgumaVez && erro < _ultimoErro;
            var piorou = _mediuAlgumaVez && erro > _ultimoErro;

            _ultimoErro = erro;
            _melhorErro = Mathf.Min(_melhorErro, erro);
            _mediuAlgumaVez = true;

            // "Melhorou" ou "piorou" é a única pista além do número — e é o
            // mínimo para o aluno poder tatear. Sem ela, medir seria inútil e a
            // bancada viraria sorteio.
            Painel.Instruir(
                erro <= ErroBom ? "afinado" :
                melhorou ? $"erro {erro:0.000} — melhorou" :
                piorou ? $"erro {erro:0.000} — piorou" :
                $"erro {erro:0.000}",
                erro <= ErroBom ? Cores.Folha : melhorou ? Cores.Luz : Cores.Brasa);

            Atualizar();

            // Durante a explicação, medir é demonstração: não fecha a rodada nem
            // gasta a paciência de Elias. Sem esta linha, um mostrador que já
            // nasceu quase afinado venceria a bancada no meio do tutorial.
            if (Congelado) return;

            if (erro <= ErroBom)
            {
                Vencer();
                return;
            }
            if (_mostrador.Medicoes >= _r.medicoes) Desistir7();
        }

        void Atualizar()
        {
            for (var i = 0; i < _r.botoes; i++)
            {
                var valor = _painelBotoes.Find($"valor{i}")?.GetComponent<Text>();
                if (valor != null) valor.text = _mostrador.Botoes[i].ToString("0.0");
            }

            var erro = _mediuAlgumaVez ? _ultimoErro : _mostrador.ErroInicial();
            _leitura.text = _mediuAlgumaVez ? erro.ToString("0.000") : "— · — —";

            // Escala relativa ao erro de quem não mexeu em nada: assim a barra
            // significa "fração do problema que ainda falta".
            var referencia = Mathf.Max(0.001f, _mostrador.ErroInicial());
            Widgets.Encher(_agulha, erro / referencia, 620f);
            _agulha.GetComponent<Image>().color =
                erro <= ErroBom ? Cores.Folha : erro < referencia * 0.4f ? Cores.Luz : Cores.Brasa;

            _historico.text = $"medições: {_mostrador.Medicoes} de {_r.medicoes}" +
                              (_melhorErro < float.PositiveInfinity
                                  ? $"   ·   melhor erro até agora: {_melhorErro:0.000}"
                                  : string.Empty);

            // Medição é o recurso escasso desta bancada — é ele que faz girar às
            // cegas doer. Mesmo assim era o único dos cinco contadores do jogo
            // que não passava pelo widget comum: ficava cinza e imóvel até o fim,
            // enquanto elos, tesouradas e perguntas já avisavam de longe.
            Widgets.Contar(_historico, _r.medicoes - _mostrador.Medicoes);

            if (_mostrador.Medicoes < _r.medicoes && _ultimoErro > ErroBom)
                Painel.Acao("medir o erro", Medir);
        }

        void Vencer()
        {
            var resposta = string.Join("  ", _mostrador.Alvo.Select(a => a.ToString("0.0")));

            if (NivelAtual < Niveis - 1)
            {
                Resolveu($"Afinado em {_mostrador.Medicoes} medições",
                    $"Os valores certos eram: {resposta}\n\n" +
                    "Você conseguiu tatear porque eram poucos botões: girar,\n" +
                    "medir, ver se melhorou, girar de novo.\n\n" +
                    "Seu Ilo vai acrescentar botões.");
                return;
            }

            Resolveu($"Afinado com {_r.botoes} botões — impressionante",
                $"Os valores certos eram: {resposta}\n\n" + Recado());
        }

        void Desistir7()
        {
            var resposta = string.Join("  ", _mostrador.Alvo.Select(a => a.ToString("0.0")));

            Falhou($"As medições acabaram — melhor erro: {_melhorErro:0.000}",
                $"Os valores certos eram: {resposta}\n\n" +
                (NivelAtual < Niveis - 1
                    ? "Com poucos botões dava para tatear. Com mais, o número de\n" +
                      "combinações cresce mais rápido que a sua paciência."
                    : Recado()),
                contaEstrela: NivelAtual == Niveis - 1);
        }

        /// <summary>
        /// O recado final da bancada, com o número que o próprio aluno criou na
        /// bancada 6. Dizer "redes têm milhões de pesos" é informação; dizer "a
        /// SUA malha tem 480 fios e você não deu conta de 6 botões" é conclusão.
        /// </summary>
        string Recado()
        {
            var camadas = Progresso.Atual.camadas;
            var fios = camadas != null && camadas.Length > 0
                ? EstimarFios(camadas)
                : 0;

            var comparacao = fios > 0
                ? $"A malha que você teceu no tear tem cerca de {fios} fios.\n"
                : "Uma malha de verdade tem centenas de milhares de fios.\n";

            return "Seis botões, um número só de resposta. Desanimador — e é\n" +
                   "exatamente a situação de quem treina uma rede.\n\n" +
                   comparacao +
                   "Girar cada um à mão, medindo a cada giro, levaria mais\n" +
                   "tempo que a idade do universo.\n\n" +
                   "Rosa, ao lado, não gira nenhum botão — ela deixa a máquina\n" +
                   "descobrir para que lado girar.";
        }

        static int EstimarFios(int[] camadas)
        {
            // Reconstrói a conta da bancada 6 com a entrada e a saída da última
            // rodada dela. É estimativa e está declarada como tal no texto.
            var colunas = new System.Collections.Generic.List<int> { 16 };
            colunas.AddRange(camadas);
            colunas.Add(12);

            var total = 0;
            for (var i = 0; i < colunas.Count - 1; i++) total += colunas[i] * colunas[i + 1];
            return total;
        }
    }
}
