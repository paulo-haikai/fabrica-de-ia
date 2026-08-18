using UnityEngine;

namespace FabricaDeIA.Mundo
{
    /// <summary>
    /// A câmera que acompanha o aluno pelo salão.
    ///
    /// Duas regras que parecem detalhe e não são. Primeira: a câmera nunca sai
    /// dos limites do ateliê — deixar aparecer o vazio além da parede destrói
    /// em um segundo a impressão de lugar. Segunda: a posição final é encaixada
    /// na grade de pixels; sem isso, a arte de 16 pixels ampliada tremula e
    /// mostra costura entre tiles, que é o defeito que mais denuncia pixel art
    /// mal montada.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraSeguidora : MonoBehaviour
    {
        /// <summary>
        /// Altura visível em unidades de mundo (tiles). Doze tiles de altura
        /// mostram três fileiras de bancadas — o bastante para o aluno escolher
        /// para onde ir sem que os personagens fiquem pequenos demais.
        /// </summary>
        public const float AlturaEmTiles = 12f;

        const float Suavidade = 0.12f;

        Camera _camera;
        Transform _alvo;
        Vector2 _posicao;

        public void Preparar(Transform alvo)
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = AlturaEmTiles / 2f;
            _camera.backgroundColor = new Color(0.106f, 0.122f, 0.165f);

            // Ordenação por profundidade: quem está mais embaixo na tela é
            // desenhado na frente. É o que deixa o aluno passar por trás de uma
            // bancada e ser tapado por ela, sem gerenciar sortingOrder na mão.
            _camera.transparencySortMode = TransparencySortMode.CustomAxis;
            _camera.transparencySortAxis = new Vector3(0f, 1f, 0f);

            _alvo = alvo;
            _posicao = alvo.position;
            Aplicar(true);
        }

        void LateUpdate()
        {
            if (_alvo == null) return;
            _posicao = Vector2.Lerp(_posicao, _alvo.position, 1f - Mathf.Pow(1f - Suavidade, Time.deltaTime * 60f));
            Aplicar(false);
        }

        void Aplicar(bool imediato)
        {
            if (imediato) _posicao = _alvo.position;

            var meiaAltura = _camera.orthographicSize;
            var meiaLargura = meiaAltura * _camera.aspect;
            var limites = Atelie.Limites;

            // Quando o salão é menor que a tela num eixo, centraliza nele em vez
            // de grudar numa das bordas.
            var x = limites.width <= meiaLargura * 2f
                ? limites.center.x
                : Mathf.Clamp(_posicao.x, limites.xMin + meiaLargura, limites.xMax - meiaLargura);
            var y = limites.height <= meiaAltura * 2f
                ? limites.center.y
                : Mathf.Clamp(_posicao.y, limites.yMin + meiaAltura, limites.yMax - meiaAltura);

            const float pixel = 1f / Arte.Folhas.PixelsPorUnidade;
            transform.position = new Vector3(
                Mathf.Round(x / pixel) * pixel,
                Mathf.Round(y / pixel) * pixel,
                -10f);
        }
    }
}
