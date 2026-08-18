using FabricaDeIA.Arte;
using UnityEngine;

namespace FabricaDeIA.Mundo
{
    /// <summary>
    /// Quem anda pelo salão: o aluno e os doze mestres.
    ///
    /// A animação é de quatro quadros por direção, trocados por distância
    /// percorrida e não por tempo. Trocar por tempo faz o boneco patinar quando
    /// desacelera — os pés se mexem e o corpo não sai do lugar. Ligar o quadro
    /// ao caminho andado custa uma linha e resolve isso de vez.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Andarilho : MonoBehaviour
    {
        // Repassadas de `Folhas`, que é de quem elas são: a ordem dos quadros é
        // um fato da folha de sprites, não de quem caminha.
        public const int Baixo = Folhas.Baixo;
        public const int Cima = Folhas.Cima;
        public const int Esquerda = Folhas.Esquerda;
        public const int Direita = Folhas.Direita;

        /// <summary>Passos por unidade de mundo. Mais alto, passo mais miúdo.</summary>
        const float PassosPorUnidade = 2.6f;

        SpriteRenderer _desenho;
        Folhas _folhas;
        string _id;
        int _direcao = Baixo;
        float _caminho;
        bool _respira;
        float _faseDoRespiro;
        Vector3 _pousoOriginal;

        public int Direcao => _direcao;

        public void Preparar(Folhas folhas, string id, int direcaoInicial = Baixo)
        {
            _desenho = GetComponent<SpriteRenderer>();
            _folhas = folhas;
            _id = id;
            _direcao = direcaoInicial;
            // Fase própria por personagem: doze mestres respirando em uníssono
            // parecem uma vitrine de manequins.
            _faseDoRespiro = Mathf.Abs(id.GetHashCode() % 100) / 100f * Mathf.PI * 2f;
            Desenhar(0);
        }

        /// <summary>
        /// Liga o balanço de quem está parado trabalhando. Guarda o lugar exato
        /// onde o personagem foi posto — o balanço oscila em torno dele, em vez
        /// de arrastar o mestre para a origem a cada quadro.
        /// </summary>
        public void Respirar()
        {
            _pousoOriginal = transform.localPosition;
            _respira = true;
        }

        /// <summary>
        /// Atualiza a pose a partir do deslocamento do quadro. Passe o vetor de
        /// movimento já aplicado, não a intenção de movimento: um personagem
        /// empurrando parede deve ficar parado, e é isso que diferencia os dois.
        /// </summary>
        public void Andar(Vector2 deslocamento)
        {
            var distancia = deslocamento.magnitude;
            if (distancia < 0.0005f)
            {
                _caminho = 0f;
                Desenhar(0);
                return;
            }

            // A direção só troca quando um eixo domina o outro com folga. Sem a
            // folga, andar na diagonal faz o boneco tremer entre dois lados.
            if (Mathf.Abs(deslocamento.x) > Mathf.Abs(deslocamento.y) * 1.15f)
                _direcao = deslocamento.x > 0f ? Direita : Esquerda;
            else if (Mathf.Abs(deslocamento.y) > Mathf.Abs(deslocamento.x) * 1.15f)
                _direcao = deslocamento.y > 0f ? Cima : Baixo;

            _caminho += distancia * PassosPorUnidade;
            Desenhar(Mathf.FloorToInt(_caminho) % 4);
        }

        /// <summary>Vira o personagem para encarar um ponto do salão.</summary>
        public void Encarar(Vector2 alvo)
        {
            var d = alvo - (Vector2)transform.position;
            if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) _direcao = d.x > 0f ? Direita : Esquerda;
            else _direcao = d.y > 0f ? Cima : Baixo;
            Desenhar(0);
        }

        void Update()
        {
            if (!_respira) return;
            // Meio pixel de sobe-e-desce, bem devagar. Mais que isso vira
            // flutuação e o mestre parece um fantasma.
            var y = Mathf.Sin(Time.time * 1.6f + _faseDoRespiro) * 0.5f / Folhas.PixelsPorUnidade;
            transform.localPosition = _pousoOriginal + new Vector3(0f, y, 0f);
        }

        void Desenhar(int quadro)
        {
            if (_folhas == null) return;
            _desenho.sprite = _folhas.Quadro(_id, _direcao, quadro);
        }
    }
}
