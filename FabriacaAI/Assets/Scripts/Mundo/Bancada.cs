using UnityEngine;

namespace FabricaDeIA.Mundo
{
    /// <summary>
    /// Uma das doze estações de trabalho.
    ///
    /// A bancada não sabe nada sobre o desafio que guarda — ela só se anuncia
    /// quando o aluno chega perto e avisa quem perguntar qual etapa é a sua.
    /// Quem decide o que acontece ao interagir é o <c>Jogo</c>. Essa fronteira
    /// é o que permite portar os doze desafios um a um sem tocar no mundo.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Bancada : MonoBehaviour
    {
        public string Etapa { get; private set; }
        public int Ordem { get; private set; }
        public Vector2 PontoDeChegada { get; private set; }

        SpriteRenderer _desenho;
        bool _emDestaque;
        float _animacao;
        Vector3 _pouso;

        /// <summary>
        /// Bancada trancada não se apaga da tela — ela fica ESCURA.
        ///
        /// Sumir com as onze que ainda não abriram tiraria do aluno a única coisa
        /// que o salão tem para contar: o tamanho da aula. Ver as doze desde o
        /// começo, e ver as luzes acendendo uma a uma, é o progresso virando
        /// paisagem em vez de barra.
        /// </summary>
        bool _liberada = true;

        /// <summary>Já foi aberta alguma vez? Muda só o brilho de "pronta".</summary>
        bool _tentada;

        public bool Liberada => _liberada;

        static readonly Color Trancada = new(0.34f, 0.36f, 0.44f);
        static readonly Color Acesa = new(1.25f, 1.2f, 1.05f);

        public void Definir(Posto posto)
        {
            Etapa = posto.Etapa;
            Ordem = posto.Ordem;
            _desenho = GetComponent<SpriteRenderer>();
            _pouso = transform.position;
            // O aluno para logo abaixo da bancada, que é de onde ela é visível
            // por inteiro — parar ao lado esconde metade do aparelho.
            PontoDeChegada = (Vector2)_pouso + new Vector2(0f, -0.6f);
        }

        /// <summary>
        /// Realce de "dá para mexer aqui".
        ///
        /// Não é enfeite: com doze bancadas à vista e nenhuma borda de seleção,
        /// o aluno aperta a tecla na frente da errada e conclui que o jogo não
        /// responde. O realce é a resposta antes do erro.
        /// </summary>
        public void Destacar(bool ativo) => _emDestaque = ativo;

        /// <summary>
        /// Diz à bancada em que estado ela está. Chamado quando o progresso muda.
        /// </summary>
        public void Estado(bool liberada, bool tentada)
        {
            _liberada = liberada;
            _tentada = tentada;
        }

        void Update()
        {
            if (!_liberada)
            {
                // Trancada não reage a nada: nem ao realce de aproximação, nem à
                // respiração. Um alvo que responde ao mouse e não abre é pior que
                // um alvo apagado, porque promete.
                _desenho.color = Trancada;
                transform.position = _pouso;
                return;
            }

            var alvo = _emDestaque ? 1f : 0f;
            _animacao = Mathf.MoveTowards(_animacao, alvo, Time.deltaTime * 6f);

            if (_animacao <= 0f && !_emDestaque)
            {
                _desenho.color = Respirando();
                transform.position = _pouso;
                return;
            }

            var suave = Mathf.SmoothStep(0f, 1f, _animacao);
            // Clareia e sobe um pixel. O deslocamento vertical é o que o olho
            // pega pela periferia; a cor sozinha passa despercebida.
            _desenho.color = Color.Lerp(Respirando(), Acesa, suave);
            transform.position = _pouso + new Vector3(0f, suave / 16f, 0f);
        }

        /// <summary>
        /// O brilho de "aberta e ainda não jogada": uma pulsação lenta, fraca.
        ///
        /// Fraca de propósito. São até doze bancadas à vista e a que interessa é
        /// UMA; doze piscando forte viraria árvore de natal, e o aluno pararia de
        /// ver qualquer uma. Assim que ele entra na bancada, a pulsação para —
        /// convite dado é convite gasto.
        /// </summary>
        Color Respirando()
        {
            if (_tentada) return Color.white;
            var onda = 0.5f + 0.5f * Mathf.Sin(Time.time * 2.2f + _pouso.x);
            return Color.Lerp(Color.white, Acesa, onda * 0.45f);
        }
    }
}
