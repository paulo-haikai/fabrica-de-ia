using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FabricaDeIA.Mundo
{
    /// <summary>
    /// Faz a luz da claraboia respirar.
    ///
    /// Luz 2D parada é indistinguível de uma mancha pintada no chão. Uma
    /// variação lenta e pequena — nunca mais que um décimo da intensidade —
    /// basta para o cenário deixar de parecer uma imagem congelada. Passar
    /// disso vira lâmpada com mau contato, que é outro recado, e errado.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public class Piscar : MonoBehaviour
    {
        Light2D _luz;
        float _base;
        float _fase;
        float _ritmo;

        void Awake()
        {
            _luz = GetComponent<Light2D>();
            _base = _luz.intensity;
            // Fase e ritmo próprios: oito claraboias pulsando juntas viram
            // sirene, e o olho passa a acompanhar o efeito em vez do jogo.
            _fase = Random.value * Mathf.PI * 2f;
            _ritmo = Random.Range(0.35f, 0.6f);
        }

        void Update()
        {
            var onda = Mathf.Sin(Time.time * _ritmo + _fase)
                     + Mathf.Sin(Time.time * _ritmo * 2.3f + _fase) * 0.4f;
            _luz.intensity = _base * (1f + onda * 0.05f);
        }
    }
}
