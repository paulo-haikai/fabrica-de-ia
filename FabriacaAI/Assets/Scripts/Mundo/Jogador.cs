using System;
using System.Collections.Generic;
using FabricaDeIA.Arte;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FabricaDeIA.Mundo
{
    /// <summary>
    /// O aluno andando pelo ateliê.
    ///
    /// Movimento por física: corpo dinâmico sem gravidade, dirigido por
    /// velocidade. Deslizar na parede em vez de grudar nela é a diferença entre
    /// um corredor que se atravessa de primeira e um que exige pontaria — e o
    /// solucionador do motor já faz isso de graça. A versão web resolvia com
    /// colisão de quatro cantos escrita à mão; aqui, qualquer obstáculo novo
    /// passa a funcionar sem uma linha a mais.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Andarilho))]
    public class Jogador : MonoBehaviour
    {
        /// <summary>
        /// Unidades por segundo. Atravessar o salão de ponta a ponta leva uns
        /// oito segundos — rápido o bastante para não entediar entre bancadas,
        /// devagar o bastante para o cenário ser visto.
        /// </summary>
        public const float Velocidade = 5.2f;

        /// <summary>Distância em que uma bancada passa a responder.</summary>
        const float Alcance = 2.1f;

        Rigidbody2D _corpo;
        Andarilho _andarilho;
        IReadOnlyList<Bancada> _bancadas;
        Bancada _aoAlcance;
        bool _travado;
        Vector2 _ultimaPosicao;

        /// <summary>A bancada ao alcance da mão mudou (ou deixou de haver uma).</summary>
        public event Action<Bancada> AlcanceMudou;

        /// <summary>O aluno pediu para trabalhar na bancada em que está.</summary>
        public event Action<Bancada> Interagiu;

        public Bancada AoAlcance => _aoAlcance;

        /// <summary>Congela o aluno enquanto um diálogo ou desafio está aberto.</summary>
        public bool Travado
        {
            get => _travado;
            set
            {
                _travado = value;
                if (value) _andarilho.Andar(Vector2.zero);
            }
        }

        /// <summary>
        /// Põe o aluno num ponto do ateliê, sem animação nem colisão.
        ///
        /// Serve ao atalho que abre uma bancada direto — o do professor, e o das
        /// conferências. Sem isto, quem entra por atalho SAI da bancada de um lugar
        /// onde ele nunca esteve, e a conferência deixa de exercitar a geometria
        /// que o aluno de verdade vive: sair da bancada colado nela.
        /// </summary>
        public void Colocar(Vector2 onde)
        {
            _corpo.position = onde;
            _ultimaPosicao = onde;
            AtualizarAlcance();
        }

        public void Preparar(Folhas folhas, IReadOnlyList<Bancada> bancadas)
        {
            _corpo = GetComponent<Rigidbody2D>();
            _corpo.bodyType = RigidbodyType2D.Dynamic;
            _corpo.gravityScale = 0f;
            _corpo.freezeRotation = true;
            // Sem atrito e sem quique: o aluno não deve escorregar depois de
            // soltar a tecla nem ricochetear numa quina de bancada.
            _corpo.linearDamping = 0f;
            _corpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _corpo.interpolation = RigidbodyInterpolation2D.Interpolate;

            _andarilho = GetComponent<Andarilho>();
            _andarilho.Preparar(folhas, "jogador");
            _bancadas = bancadas;
            _ultimaPosicao = _corpo.position;
        }

        void FixedUpdate()
        {
            // O componente é acrescentado por código e só fica utilizável depois
            // de `Preparar`. Se a montagem do mundo for interrompida no meio —
            // recarga de assemblies durante o Play é o caso comum — este método
            // rodaria mesmo assim e encheria o log de NullReference a 50 Hz,
            // escondendo o erro de verdade lá atrás.
            if (_bancadas == null) return;

            // O deslocamento é medido ANTES de mandar a nova velocidade: o que
            // está na posição agora é o resultado do passo de física anterior.
            // Medir depois daria sempre zero, porque a integração ainda não
            // aconteceu — e o boneco andaria o salão inteiro em pose de parado.
            var deslocamento = _corpo.position - _ultimaPosicao;
            _ultimaPosicao = _corpo.position;

            _corpo.linearVelocity = _travado ? Vector2.zero : LerTeclado() * Velocidade;

            // A pose vem do que o corpo ANDOU, não do que a tecla pediu. É essa
            // diferença que faz o boneco parar de pedalar quando empurra parede.
            _andarilho.Andar(deslocamento);
            AtualizarAlcance();
        }

        /// <summary>
        /// Setas e WASD, sempre as duas. Numa sala de informática de escola há
        /// teclado sem seta e aluno canhoto; oferecer os dois esquemas custa uma
        /// linha e evita meia aula perdida.
        /// </summary>
        static Vector2 LerTeclado()
        {
            var teclado = Keyboard.current;
            if (teclado == null) return Vector2.zero;

            var d = Vector2.zero;
            if (teclado.leftArrowKey.isPressed || teclado.aKey.isPressed) d.x -= 1f;
            if (teclado.rightArrowKey.isPressed || teclado.dKey.isPressed) d.x += 1f;
            if (teclado.upArrowKey.isPressed || teclado.wKey.isPressed) d.y += 1f;
            if (teclado.downArrowKey.isPressed || teclado.sKey.isPressed) d.y -= 1f;

            // Normalizar impede o clássico "na diagonal eu corro mais".
            return d.sqrMagnitude > 1f ? d.normalized : d;
        }

        void Update()
        {
            if (_travado || _aoAlcance == null) return;
            var teclado = Keyboard.current;
            if (teclado == null) return;

            if (teclado.eKey.wasPressedThisFrame ||
                teclado.spaceKey.wasPressedThisFrame ||
                teclado.enterKey.wasPressedThisFrame)
            {
                Interagiu?.Invoke(_aoAlcance);
            }
        }

        void AtualizarAlcance()
        {
            Bancada maisPerto = null;
            var menorDistancia = Alcance;

            foreach (var bancada in _bancadas)
            {
                var d = Vector2.Distance(_corpo.position, bancada.PontoDeChegada);
                if (d >= menorDistancia) continue;
                menorDistancia = d;
                maisPerto = bancada;
            }

            if (ReferenceEquals(maisPerto, _aoAlcance)) return;

            if (_aoAlcance != null) _aoAlcance.Destacar(false);
            _aoAlcance = maisPerto;
            if (_aoAlcance != null) _aoAlcance.Destacar(true);
            AlcanceMudou?.Invoke(_aoAlcance);
        }
    }
}
