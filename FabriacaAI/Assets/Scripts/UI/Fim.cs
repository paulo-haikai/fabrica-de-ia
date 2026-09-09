using System;
using System.Collections;
using FabricaDeIA.Desafios;
using FabricaDeIA.Nucleo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FabricaDeIA.UI
{
    /// <summary>
    /// A tela de fim: a máquina se levanta, e depois vem o certificado.
    ///
    /// A aula abre com uma máquina que quebra — o raio, o companheiro perdendo as
    /// palavras — e passa onze bancadas juntando peça. Faltava o outro lado desse
    /// arco: a última bancada fechava, o mestre dizia uma frase e o aluno voltava a
    /// andar pelo salão como se fosse uma bancada qualquer. A aula terminava sem
    /// ninguém avisar que tinha terminado.
    ///
    /// Aqui ela termina em cena. As doze peças entram de baixo para cima, na ordem
    /// em que foram conquistadas, e o conjunto que pendia se endireita: a máquina
    /// fica de pé. Então o olho acende. É a imagem da abertura invertida, e é a
    /// única recompensa que a aula tem para dar — não há pontuação nem placar
    /// entre alunos.
    ///
    /// PULÁVEL A QUALQUER MOMENTO, pela mesma razão que a abertura é: numa turma de
    /// trinta há sempre quem já viu, e prender essa pessoa em seis segundos de
    /// animação é o jeito mais rápido de perder a sala. Quem pula cai direto no
    /// formulário do certificado, que é o que ela veio buscar.
    /// </summary>
    public class Fim : MonoBehaviour
    {
        /// <summary>O quadro do robô é maior aqui do que no balcão: é a hora dele.</summary>
        const float Escala = 1.25f;

        /// <summary>Entre uma peça e a seguinte.</summary>
        const float Passo = 0.13f;

        Action _aoTerminar;
        RectTransform _raiz;
        RectTransform _quadro;
        RectTransform _olho;
        Text _titulo;
        Text _numeros;
        Text _recado;

        readonly RectTransform[] _pecas = new RectTransform[Robo.Pecas];

        bool _aceso;
        bool _terminando;

        /// <summary>Monta a tela sobre o palco da HUD e começa a animação.</summary>
        public static Fim Abrir(RectTransform palco, Action aoTerminar)
        {
            var objeto = new GameObject("Fim", typeof(RectTransform));
            objeto.transform.SetParent(palco, false);

            var tela = objeto.AddComponent<Fim>();
            tela._aoTerminar = aoTerminar;
            tela.Construir();
            tela.StartCoroutine(tela.Encenar());
            return tela;
        }

        // -------------------------------------------------------------- montar

        void Construir()
        {
            _raiz = (RectTransform)transform;
            Widgets.Esticar(_raiz);

            var fundo = Widgets.Painel("Fundo", _raiz, Cores.TintaOpaca);
            Widgets.Esticar(fundo);

            var chamada = Widgets.Texto("Chamada", fundo, 16, TextAnchor.UpperCenter, Cores.Neblina);
            Widgets.Faixa(chamada.rectTransform, true, 22f, 40f);
            chamada.text = $"as {Catalogo.Bancadas} bancadas fecharam";

            _quadro = Widgets.Painel("Quadro", fundo, Cores.TintaClara);
            Widgets.Fixar(_quadro, new Vector2(0.5f, 0.5f), new Vector2(0f, 46f),
                          new Vector2(460f, 340f));

            MontarRobo();

            _titulo = Widgets.Texto("Título", fundo, 30, TextAnchor.LowerCenter, Cores.Luz);
            Widgets.Faixa(_titulo.rectTransform, false, 38f, 128f);
            _titulo.text = string.Empty;

            _numeros = Widgets.Texto("Números", fundo, 17, TextAnchor.LowerCenter, Cores.Papel);
            Widgets.Faixa(_numeros.rectTransform, false, 24f, 100f);
            _numeros.text = string.Empty;

            _recado = Widgets.Texto("Recado", fundo, 15, TextAnchor.LowerCenter, Cores.Neblina);
            Widgets.Faixa(_recado.rectTransform, false, 22f, 62f);
            _recado.text = string.Empty;

            // A saída visível, pela mesma regra da moldura das bancadas: painel que
            // cobre a tela precisa de um botão rotulado, e não só de uma tecla que
            // ninguém adivinha.
            var pular = Widgets.Botao("Pular", fundo, "pular", Cores.Madeira, Cores.Papel, 15);
            Widgets.Fixar((RectTransform)pular.transform, new Vector2(1f, 0f),
                          new Vector2(-84f, 34f), new Vector2(120f, 38f));
            pular.onClick.AddListener(Terminar);

            var tecla = Widgets.Texto("Tecla", fundo, 13, TextAnchor.LowerLeft, Cores.Neblina);
            Widgets.Fixar(tecla.rectTransform, new Vector2(0f, 0f), new Vector2(206f, 40f),
                          new Vector2(360f, 20f));
            tecla.text = "[E] ir direto ao certificado";

            Widgets.Surgir(_raiz, 0.3f, 1f);
        }

        /// <summary>
        /// As doze peças, todas escondidas. A animação as põe uma a uma.
        ///
        /// Elas nascem PENDENDO, como no balcão do certificado, para que o
        /// endireitamento do fim seja o mesmo gesto que desfaz a máquina torta que
        /// o aluno viu esperando a aula inteira.
        /// </summary>
        void MontarRobo()
        {
            for (var i = 0; i < Robo.Pecas; i++)
            {
                var peca = Robo.Peca(_quadro, i, Cores.Madeira, Escala);
                peca.localRotation = Quaternion.Euler(0f, 0f, Robo.Pendor(i));
                peca.gameObject.SetActive(false);
                _pecas[i] = peca;
            }

            _olho = Robo.Olho(_quadro, Cores.TintaOpaca, Escala);
        }

        // ------------------------------------------------------------- a cena

        IEnumerator Encenar()
        {
            yield return Esperar(0.45f);

            // As peças chegam de baixo para cima, na ordem das bancadas. Cada uma
            // CAI no lugar em vez de aparecer: é a queda que diz que ela foi posta
            // ali, e a aula inteira foi sobre pôr peça.
            for (var i = 0; i < Robo.Pecas; i++)
            {
                if (_terminando) yield break;
                var peca = _pecas[i];
                peca.gameObject.SetActive(true);
                var posto = Robo.Posto(i, Escala);
                peca.anchoredPosition = posto + new Vector2(0f, 54f);
                Widgets.Deslizar(peca, posto, 0.18f);
                yield return Esperar(Passo);
            }

            yield return Esperar(0.25f);

            // O endireitamento. É o instante em que a máquina deixa de ser um monte
            // de peças e passa a ser uma máquina — e é por isso que ele tem um
            // gesto só para si, em vez de vir junto com a última peça.
            yield return Endireitar(0.45f);
            if (_terminando) yield break;
            Widgets.Lampejo(_quadro, Cores.Luz, 0.5f, 0.55f);

            yield return Esperar(0.2f);

            Acender();
            Widgets.Pulsar(_quadro, 1.04f, 0.3f);

            _titulo.text = "A máquina está pronta";
            Widgets.Surgir(_titulo.rectTransform, 0.35f, 0.9f);

            yield return Esperar(0.7f);

            _numeros.text = Placar();
            Widgets.UmaLinha(_numeros);
            Widgets.Surgir(_numeros.rectTransform, 0.3f, 0.96f);

            yield return Esperar(0.9f);

            _recado.color = Cores.Folha;
            _recado.text = "agora o certificado — o trabalho foi seu";
            Widgets.Surgir(_recado.rectTransform, 0.3f, 0.96f);

            yield return Esperar(1.2f);
            Terminar();
        }

        static string Placar()
        {
            var p = Progresso.Atual;
            return $"{Catalogo.Bancadas} bancadas  ·  " +
                   $"{p.EstrelasTotais} de {Catalogo.EstrelasPossiveis} estrelas  ·  " +
                   $"{Diploma.Duracao(p.SegundosTotais)} de oficina";
        }

        void Acender()
        {
            _aceso = true;
            var pintura = _olho.GetComponent<Image>();
            pintura.color = Cores.Luz;
        }

        /// <summary>Leva todas as peças ao prumo ao mesmo tempo.</summary>
        IEnumerator Endireitar(float duracao)
        {
            var pendores = new float[Robo.Pecas];
            for (var i = 0; i < Robo.Pecas; i++) pendores[i] = Robo.Pendor(i);

            for (var t = 0f; t < duracao; t += Time.unscaledDeltaTime)
            {
                if (_terminando) yield break;
                // Amortece no fim: a peça encosta no prumo em vez de bater nele.
                var resta = 1f - t / duracao;
                var f = 1f - resta * resta;
                for (var i = 0; i < Robo.Pecas; i++)
                {
                    if (_pecas[i] == null) continue;
                    _pecas[i].localRotation =
                        Quaternion.Euler(0f, 0f, Mathf.Lerp(pendores[i], 0f, f));
                }
                yield return null;
            }

            foreach (var peca in _pecas)
                if (peca != null) peca.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Espera em tempo de parede, e desiste na hora se a cena foi pulada.
        ///
        /// <c>WaitForSeconds</c> não serviria: ele conta tempo de jogo, e a espera
        /// seguiria correndo depois que o aluno já pediu para sair.
        /// </summary>
        IEnumerator Esperar(float segundos)
        {
            for (var t = 0f; t < segundos; t += Time.unscaledDeltaTime)
            {
                if (_terminando) yield break;
                yield return null;
            }
        }

        void Update()
        {
            // O olho respira enquanto a tela está no ar. É o único movimento depois
            // que a máquina levanta, e é o que separa "montada" de "ligada".
            if (_aceso && _olho != null)
            {
                var brilho = 0.82f + 0.18f * Mathf.Sin(Time.unscaledTime * 3.2f);
                _olho.GetComponent<Image>().color =
                    new Color(Cores.Luz.r * brilho, Cores.Luz.g * brilho, Cores.Luz.b * brilho, 1f);
            }

            var teclado = Keyboard.current;
            if (teclado == null) return;

            // As mesmas teclas que avançam fala no resto do jogo. Esc também: em
            // toda a aula ele significa "sair do que estou vendo", e aqui sair é ir
            // para o certificado — não há mais nada depois desta tela.
            if (teclado.eKey.wasPressedThisFrame ||
                teclado.spaceKey.wasPressedThisFrame ||
                teclado.enterKey.wasPressedThisFrame ||
                teclado.escapeKey.wasPressedThisFrame)
            {
                Terminar();
            }
        }

        /// <summary>
        /// Sai de cena e chama quem vem depois — uma vez só.
        ///
        /// A guarda não é zelo: a tecla, o botão e o fim da animação fazem a mesma
        /// coisa. Sem ela, um toque no último quadro abriria duas formaturas, uma
        /// por cima da outra.
        /// </summary>
        void Terminar()
        {
            if (_terminando) return;
            _terminando = true;

            var depois = _aoTerminar;
            _aoTerminar = null;
            Destroy(gameObject);
            depois?.Invoke();
        }
    }
}
