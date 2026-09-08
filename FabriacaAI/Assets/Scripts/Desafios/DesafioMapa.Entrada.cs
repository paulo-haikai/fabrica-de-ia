using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// O gesto da bancada 4: toque duplo OU arrastar, os dois desembocando em
    /// <see cref="Tocar"/> — a regra mora lá, este arquivo só decide QUANDO
    /// chamá-la.
    ///
    /// A primeira versão do arrastar decidia o vizinho de destino pelo evento
    /// <c>PointerEnter</c>: o ponteiro precisava passar, quadro a quadro, POR
    /// CIMA do botão da peça vizinha para o jogo perceber a troca. Num trackpad
    /// de escola — a mesma máquina que fez a bancada da cozinha abandonar o
    /// <c>PointerUp</c> por um motivo parecido, ver
    /// <c>DesafioCozinha.Desenho.cs</c> — a amostragem de posição é esparsa: o
    /// gesto de arrastar salta direto da peça de origem para dentro do vão entre
    /// peças, ou para além da vizinha, sem NUNCA cruzar o retângulo dela. Sem
    /// esse cruzamento, <c>PointerEnter</c> nunca disparava, e a troca
    /// simplesmente não acontecia — sem tremor, sem mensagem, sem nada visível.
    /// Era exatamente isto que o relato "palavras da mesma cor que não são
    /// arrastadas" descrevia: não um erro na regra, um gesto que nunca chegava
    /// a ela.
    ///
    /// A troca agora é por VETOR. Guarda o ponto onde o dedo pousou e, a cada
    /// quadro em que ele ainda está apertado, mede a distância até o ponto
    /// atual. Passou de um limiar, decide a direção pelo eixo de maior
    /// deslocamento e resolve a troca — sem precisar que o ponteiro tenha
    /// passado por cima de peça nenhuma no caminho. E o evento que alimenta essa
    /// medida, <c>Drag</c> do Unity, é entregue sempre à peça em que o gesto
    /// COMEÇOU, nunca à que está debaixo do dedo no instante — então mesmo um
    /// salto grosseiro continua sendo visto pela peça certa.
    /// </summary>
    public partial class DesafioMapa
    {
        (int coluna, int linha)? _origemArraste;
        Vector2 _pontoOrigemArraste;
        bool _arrastoResolvido;
        bool _trocouArrastando;

        void Pegar(int coluna, int linha)
        {
            if (_travado) return;
            _trocouArrastando = false;
        }

        /// <summary>O dedo pousou aqui e começou a se mover.</summary>
        void IniciarArraste(int coluna, int linha, Vector2 pontoLocal)
        {
            if (_travado) return;
            _origemArraste = (coluna, linha);
            _pontoOrigemArraste = pontoLocal;
            _arrastoResolvido = false;
        }

        /// <summary>
        /// O dedo ainda está apertado e se moveu. Resolve a troca assim que o
        /// deslocamento passar do limiar — só uma vez por gesto, mesmo que o
        /// dedo continue se mexendo depois.
        /// </summary>
        void Arrastando(Vector2 pontoLocal)
        {
            if (_travado || _origemArraste == null || _arrastoResolvido) return;

            var deslocamento = pontoLocal - _pontoOrigemArraste;
            if (deslocamento.magnitude < LimiarArraste) return;

            var (oc, ol) = _origemArraste.Value;
            int dc, dl;
            if (Mathf.Abs(deslocamento.x) >= Mathf.Abs(deslocamento.y))
            {
                dc = deslocamento.x > 0 ? 1 : -1;
                dl = 0;
            }
            else
            {
                // O eixo Y local do tabuleiro sobe (ver Posicao); dedo descendo
                // na tela é linha MAIOR.
                dc = 0;
                dl = deslocamento.y > 0 ? -1 : 1;
            }

            var c2 = oc + dc;
            var l2 = ol + dl;
            if (c2 < 0 || c2 >= Colunas || l2 < 0 || l2 >= Linhas) return;

            // Não marca resolvido antes de saber se a casa existe: se o gesto
            // mirou para fora do tabuleiro, o aluno ainda pode corrigir a
            // direção sem soltar o dedo.
            _arrastoResolvido = true;

            // Reaproveita o caminho do toque: seleciona a de origem e "toca" na
            // vizinha. Assim arrastar e tocar não podem divergir de regra — é
            // uma regra só, com duas entradas.
            _escolhida = (oc, ol);
            Tocar(c2, l2);

            // A marca vem DEPOIS da troca, e não antes: `Tocar` começa checando
            // esta mesma bandeira, então marcá-la antes faria a troca ser
            // engolida pela própria guarda.
            _trocouArrastando = true;
        }

        void TerminarArraste() => _origemArraste = null;
    }
}
