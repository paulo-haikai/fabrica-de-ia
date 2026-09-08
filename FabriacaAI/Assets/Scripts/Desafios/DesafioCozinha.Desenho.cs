using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using FabricaDeIA.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// As duas colunas da bancada 8 e o traço que liga uma à outra.
    ///
    /// O traço é feito de quadradinhos, não de uma linha lisa: o uGUI não desenha
    /// linha, e enfiar um <c>LineRenderer</c> aqui traria câmera, ordenação e
    /// material só para um risco. Uma fileira de quadrados espaçados custa nada,
    /// combina com o resto da arte do jogo e ainda parece desenhado à mão.
    /// </summary>
    public partial class DesafioCozinha
    {
        const float LarguraCartao = 340f;
        const float AlturaCartao = 96f;
        const float VaoVertical = 22f;
        const float XdaEsquerda = -230f;
        const float XdaDireita = 230f;
        const float RaioDoPonto = 15f;
        const float PassoDoTraco = 11f;

        RectTransform _mesa;
        readonly List<RectTransform> _pontosEsquerda = new();
        readonly List<RectTransform> _pontosDireita = new();
        readonly List<RectTransform> _riscos = new();
        readonly List<RectTransform> _tracoEmCurso = new();

        int _puxando = -1;

        void OnDestroy() => Widgets.Mao(false);

        void MontarTela()
        {
            Widgets.Mao(true);

            if (_mesa != null) Destroy(_mesa.gameObject);
            _pontosEsquerda.Clear();
            _pontosDireita.Clear();
            _riscos.Clear();
            _tracoEmCurso.Clear();
            _puxando = -1;

            _mesa = Widgets.Painel("Mesa", Area, Color.clear);
            Widgets.Esticar(_mesa, 8f);

            for (var i = 0; i < _maquinas.Count; i++)
            {
                var y = Altura(i);

                // A máquina: uma amostra do que ela escreve. É a única evidência,
                // e por isso ocupa o cartão inteiro.
                var cartao = Widgets.Painel($"maq{i}", _mesa, Cores.TintaClara);
                Widgets.Fixar(cartao, new Vector2(0.5f, 0.5f), new Vector2(XdaEsquerda, y),
                              new Vector2(LarguraCartao, AlturaCartao));

                var titulo = Widgets.Texto($"tm{i}", cartao, 12, TextAnchor.UpperLeft, Cores.Neblina);
                Widgets.Fixar(titulo.rectTransform, new Vector2(0.5f, 0.5f),
                              new Vector2(0f, AlturaCartao / 2f - 14f),
                              new Vector2(LarguraCartao - 24f, 18f));
                titulo.text = $"MÁQUINA {(char)('A' + i)}";

                var amostra = Widgets.Texto($"am{i}", cartao, 13, TextAnchor.UpperLeft, Cores.Papel);
                Widgets.Fixar(amostra.rectTransform, new Vector2(0.5f, 0.5f),
                              new Vector2(0f, -8f),
                              new Vector2(LarguraCartao - 24f, AlturaCartao - 34f));
                amostra.text = string.Join("\n", _maquinas[i].Amostra);

                _pontosEsquerda.Add(Ponto($"pe{i}", XdaEsquerda + LarguraCartao / 2f + 14f, y,
                                          i, true));

                // O alimento, na ordem embaralhada.
                var alimento = Widgets.Painel($"ali{i}", _mesa, Cores.Madeira);
                Widgets.Fixar(alimento, new Vector2(0.5f, 0.5f), new Vector2(XdaDireita, y),
                              new Vector2(LarguraCartao * 0.78f, AlturaCartao * 0.62f));

                var nome = Widgets.Texto($"tn{i}", alimento, 15, TextAnchor.MiddleCenter, Cores.Papel);
                Widgets.Esticar(nome.rectTransform, 12f);
                nome.text = _maquinas[_ordemDosAlimentos[i]].Alimento;

                _pontosDireita.Add(Ponto($"pd{i}", XdaDireita - LarguraCartao * 0.39f - 14f, y,
                                         i, false));
            }
        }

        float Altura(int i)
        {
            var total = _maquinas.Count * AlturaCartao + (_maquinas.Count - 1) * VaoVertical;
            return total / 2f - AlturaCartao / 2f - i * (AlturaCartao + VaoVertical) - 10f;
        }

        /// <summary>
        /// Um pontinho de ligação. Da esquerda começa o traço; na direita ele
        /// termina.
        /// </summary>
        RectTransform Ponto(string nome, float x, float y, int indice, bool esquerda)
        {
            var ponto = Widgets.Painel(nome, _mesa, Cores.Luz);
            Widgets.Fixar(ponto, new Vector2(0.5f, 0.5f), new Vector2(x, y),
                          new Vector2(RaioDoPonto * 2f, RaioDoPonto * 2f));

            var gatilho = ponto.gameObject.AddComponent<EventTrigger>();
            if (esquerda)
            {
                Gatilho(gatilho, EventTriggerType.PointerDown, () => Puxar(indice));
            }
            // A DIREITA NÃO ESCUTA EVENTO NENHUM, E É DE PROPÓSITO.
            //
            // A primeira versão pendurava `PointerUp` aqui, e a ligação nunca
            // acontecia: o Unity entrega o `PointerUp` ao objeto onde o clique
            // COMEÇOU, não ao que está debaixo do ponteiro na hora de soltar.
            // Como o arraste começa na esquerda, o ponto da direita jamais era
            // avisado.
            //
            // Quem decide agora é o `Update`, por distância. Além de funcionar,
            // perdoa mira ruim — e mira ruim é a regra num trackpad de escola.
            return ponto;
        }

        static void Gatilho(EventTrigger alvo, EventTriggerType tipo, System.Action acao)
        {
            var entrada = new EventTrigger.Entry { eventID = tipo };
            entrada.callback.AddListener(_ => acao());
            alvo.triggers.Add(entrada);
        }

        // ------------------------------------------------------------- o arraste

        void Puxar(int maquina)
        {
            if (_ligacoes.ContainsKey(maquina)) return;
            _puxando = maquina;
        }

        /// <summary>
        /// Qual ponto da direita está perto o bastante do ponteiro.
        ///
        /// O raio é generoso — bem maior que o pontinho desenhado — porque acertar
        /// um alvo de trinta pixels arrastando num trackpad é exigência de
        /// coordenação, e coordenação não é o que esta bancada quer medir.
        /// </summary>
        int PontoPerto(Vector2 ponta)
        {
            const float alcance = 58f;
            var melhor = -1;
            var menor = alcance;

            for (var i = 0; i < _pontosDireita.Count; i++)
            {
                if (_ligacoes.ContainsValue(i)) continue;
                var d = Vector2.Distance(ponta, _pontosDireita[i].anchoredPosition);
                if (d > menor) continue;
                menor = d;
                melhor = i;
            }
            return melhor;
        }

        void Update()
        {
            if (_puxando < 0 || Mouse.current == null) { LimparTracoEmCurso(); return; }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _mesa, Mouse.current.position.ReadValue(), null, out var ponta))
                return;

            var alvo = PontoPerto(ponta);

            // O ponto sob a mira cresce. É o que diz ao aluno "solta aqui" antes
            // de ele soltar — sem isso ele erra e não sabe se errou de mira ou de
            // resposta, que são coisas muito diferentes de aprender.
            for (var i = 0; i < _pontosDireita.Count; i++)
            {
                if (_ligacoes.ContainsValue(i)) continue;
                var escala = i == alvo ? 1.4f : 1f;
                _pontosDireita[i].localScale = new Vector3(escala, escala, 1f);
            }

            // Com um alvo na mira, o traço gruda nele em vez de seguir o ponteiro:
            // a ligação aparece pronta antes de acontecer.
            var fim = alvo >= 0 ? _pontosDireita[alvo].anchoredPosition : ponta;
            Riscar(_tracoEmCurso, _pontosEsquerda[_puxando].anchoredPosition, fim,
                   alvo >= 0 ? Cores.Papel : Cores.Luz);

            if (Mouse.current.leftButton.isPressed) return;

            // Soltou. Com alvo, liga; sem alvo, desiste — e desistir precisa ser
            // possível, senão o traço fica pendurado no ponteiro para sempre.
            var maquina = _puxando;
            _puxando = -1;
            LimparTracoEmCurso();
            foreach (var p in _pontosDireita) p.localScale = Vector3.one;

            if (alvo >= 0) Ligar(maquina, alvo);
        }

        // --------------------------------------------------------- os traços

        /// <summary>
        /// Enfileira quadradinhos entre dois pontos.
        ///
        /// Reaproveita os que já existem em vez de destruir e recriar: enquanto o
        /// aluno arrasta, isto roda a sessenta quadros por segundo, e criar
        /// cinquenta objetos por quadro encheria a memória de lixo à toa.
        /// </summary>
        void Riscar(List<RectTransform> pedaços, Vector2 de, Vector2 ate, Color cor)
        {
            var distancia = Vector2.Distance(de, ate);
            var quantos = Mathf.Max(1, Mathf.RoundToInt(distancia / PassoDoTraco));

            while (pedaços.Count < quantos)
            {
                var q = Widgets.Painel($"traco{pedaços.Count}", _mesa, cor);
                q.SetAsLastSibling();
                pedaços.Add(q);
            }

            for (var i = 0; i < pedaços.Count; i++)
            {
                if (i >= quantos) { pedaços[i].gameObject.SetActive(false); continue; }
                pedaços[i].gameObject.SetActive(true);
                pedaços[i].GetComponent<Image>().color = cor;

                var t = quantos == 1 ? 0f : (float)i / (quantos - 1);
                Widgets.Fixar(pedaços[i], new Vector2(0.5f, 0.5f),
                              Vector2.Lerp(de, ate, t), new Vector2(6f, 6f));
            }
        }

        void LimparTracoEmCurso()
        {
            foreach (var p in _tracoEmCurso) if (p != null) p.gameObject.SetActive(false);
        }

        /// <summary>A ligação certa vira um traço verde que fica.</summary>
        void AceitarLigacao(int maquina, int posicao)
        {
            var risco = new List<RectTransform>();
            Riscar(risco, _pontosEsquerda[maquina].anchoredPosition,
                   _pontosDireita[posicao].anchoredPosition, Cores.Folha);
            _riscos.AddRange(risco);

            _pontosEsquerda[maquina].GetComponent<Image>().color = Cores.Folha;
            _pontosDireita[posicao].GetComponent<Image>().color = Cores.Folha;
            _pontosDireita[posicao].localScale = Vector3.one;
            Widgets.Marcar(_pontosDireita[posicao], true);
        }

        /// <summary>A ligação errada treme e não fica.</summary>
        void RecusarLigacao(int maquina, int posicao)
        {
            Widgets.Tremer(_pontosDireita[posicao], 8f, 0.2f);
            Widgets.Marcar(_pontosDireita[posicao], false);
            _pontosDireita[posicao].localScale = Vector3.one;
        }
    }
}
