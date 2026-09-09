using System;
using FabricaDeIA.Mundo;
using FabricaDeIA.Nucleo;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FabricaDeIA.UI
{
    /// <summary>
    /// A interface do ateliê: placar, aviso de interação, caixa de fala e o
    /// palco onde os desafios se montam.
    ///
    /// Construída em código, como o resto do jogo. Uma tela de UGUI montada no
    /// Editor é um prefab que ninguém lê em diff e que quebra em silêncio
    /// quando um componente muda de nome; aqui, cada elemento tem uma linha
    /// visível e um motivo escrito ao lado.
    ///
    /// O texto usa a fonte legada embutida no motor de propósito. Uma fonte de
    /// pixel art é a próxima melhoria óbvia, mas ela precisa de arquivo,
    /// licença e passo de importação — e nada disso pode atrasar ter o jogo
    /// rodando no navegador.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        Text _placar;
        RectTransform _aviso;
        Text _avisoTexto;
        RectTransform _caixa;
        Text _falaNome;
        Text _falaTexto;

        Action _aoFecharFala;
        string[] _falasPendentes;
        int _falaAtual;
        int _quadroDeAbertura = -1;

        /// <summary>
        /// Onde os desafios montam a própria tela. Fica acima da HUD do salão
        /// e ocupa tudo: uma bancada toma a tela inteira, porque o aluno que
        /// ainda vê o cenário atrás fica tentado a sair andando no meio do
        /// raciocínio.
        /// </summary>
        public RectTransform Palco { get; private set; }

        public bool FalaAberta => _caixa != null && _caixa.gameObject.activeSelf;

        public void Construir()
        {
            GarantirEventSystem();

            var tela = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            tela.transform.SetParent(transform, false);

            var canvas = tela.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var escala = tela.GetComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution =
                new Vector2(PainelDeBancada.LarguraDeReferencia, PainelDeBancada.AlturaDeReferencia);
            // Meio a meio entre largura e altura: a mesma HUD tem que servir ao
            // projetor 4:3 da sala e ao notebook 16:9 do aluno.
            escala.matchWidthOrHeight = 0.5f;

            MontarPlacar(tela.transform);
            MontarAviso(tela.transform);
            MontarCaixaDeFala(tela.transform);

            // Por último, para ficar por cima de tudo na ordem de desenho.
            Palco = Widgets.Painel("Palco", tela.transform, Color.clear);
            Widgets.Esticar(Palco);

            Progresso.Mudou += AtualizarPlacar;
            AtualizarPlacar();
        }

        void OnDestroy() => Progresso.Mudou -= AtualizarPlacar;

        /// <summary>
        /// Liga ou desliga a interface do salão.
        ///
        /// Existe para a abertura: o placar "0 de 11 peças" aparecendo por cima
        /// da tela de título quebraria a cena antes de ela começar. O palco fica
        /// de fora — é onde a própria abertura se monta.
        /// </summary>
        public void MostrarSalao(bool visivel)
        {
            _placar.transform.parent.gameObject.SetActive(visivel);
            if (!visivel)
            {
                _aviso.gameObject.SetActive(false);
                _caixa.gameObject.SetActive(false);
            }
        }

        // --------------------------------------------------------- menu de pausa

        RectTransform _menu;

        /// <summary>Há um menu de pausa no ar?</summary>
        public bool MenuAberto => _menu != null && _menu.gameObject.activeSelf;

        /// <summary>
        /// O menu de Esc.
        ///
        /// Existe porque Esc já era a tecla de sair em toda a aula — fecha a
        /// bancada, fecha a conversa — e no salão ela não fazia nada. Uma tecla
        /// que funciona em duas telas de três é pior do que uma que não existe:
        /// o aluno aperta, nada acontece, e conclui que travou.
        ///
        /// O menu é montado na hora e destruído ao fechar. Guardar a árvore viva
        /// e só desligar economizaria alguns milissegundos e traria de volta um
        /// bug que esta base já pagou caro: painel invisível continua recebendo
        /// clique e engolindo o toque de quem está jogando embaixo.
        /// </summary>
        public void AbrirMenu(Action continuar, Action inicio, Action sair)
        {
            FecharMenu();
            GarantirEventSystem();

            _menu = Widgets.Painel("Menu", Palco, new Color(0.04f, 0.05f, 0.07f, 0.82f));
            Widgets.Esticar(_menu);

            var caixa = Widgets.Painel("Caixa", _menu, Cores.TintaOpaca);
            Widgets.Fixar(caixa, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 300f));

            var risco = Widgets.Painel("Risco", caixa, Cores.Luz);
            Widgets.Faixa(risco, true, 3f);

            var titulo = Widgets.Texto("Título", caixa, 22, TextAnchor.UpperCenter, Cores.Luz);
            Widgets.Faixa(titulo.rectTransform, true, 30f, 22f);
            titulo.text = "A fábrica está parada";

            var feitas = Progresso.Atual.Tentadas;
            var conta = Widgets.Texto("Conta", caixa, 15, TextAnchor.UpperCenter, Cores.Neblina);
            Widgets.Faixa(conta.rectTransform, true, 20f, 56f);
            conta.text = $"{feitas} de {Desafios.Catalogo.Bancadas} bancadas visitadas  ·  " +
                         $"{Progresso.Atual.EstrelasTotais} estrelas";

            Botao(caixa, "voltar ao trabalho", 0, Cores.Folha, continuar);
            Botao(caixa, "ir para a tela inicial", 1, Cores.Madeira, inicio);
            Botao(caixa, "fechar o jogo", 2, Cores.TintaClara, sair);

            var dica = Widgets.Texto("Dica", caixa, 13, TextAnchor.LowerCenter, Cores.Neblina);
            Widgets.Faixa(dica.rectTransform, false, 18f, 10f);
            dica.text = "[Esc] volta ao trabalho";

            Widgets.Surgir(_menu, 0.14f, 0.97f);
        }

        static void Botao(RectTransform pai, string rotulo, int linha, Color cor, Action ao)
        {
            var botao = Widgets.Botao($"b{linha}", pai, rotulo, cor, Cores.Papel, 17);
            Widgets.Fixar((RectTransform)botao.transform, new Vector2(0.5f, 0.5f),
                          new Vector2(0f, 46f - linha * 56f), new Vector2(320f, 46f));
            botao.onClick.AddListener(() =>
            {
                Widgets.Pulsar((RectTransform)botao.transform);
                ao();
            });
        }

        public void FecharMenu()
        {
            if (_menu == null) return;
            Destroy(_menu.gameObject);
            _menu = null;
        }

        /// <summary>
        /// Sem <c>EventSystem</c>, botão de UGUI não recebe clique — e falha
        /// calada: o botão aparece, acende no passar do mouse e simplesmente
        /// não faz nada. Como a cena nasce de código, ninguém a arrastou para
        /// dentro dela pelo menu do Editor; então ela nasce aqui.
        /// </summary>
        static void GarantirEventSystem()
        {
            if (EventSystem.current != null) return;
            var objeto = new GameObject("EventSystem", typeof(EventSystem));
            objeto.AddComponent<InputSystemUIInputModule>();
        }

        // ------------------------------------------------------------ placar

        void MontarPlacar(Transform pai)
        {
            var faixa = Widgets.Painel("Placar", pai, Cores.Tinta);
            Widgets.Faixa(faixa, true, 36f);

            _placar = Widgets.Texto("Texto", faixa, 18, TextAnchor.MiddleLeft, Cores.Papel);
            Widgets.Esticar(_placar.rectTransform);
            _placar.rectTransform.offsetMin = new Vector2(18f, 0f);
            _placar.rectTransform.offsetMax = new Vector2(-18f, 0f);
        }

        void AtualizarPlacar()
        {
            if (_placar == null) return;
            var feitas = Progresso.Atual.Concluidas;
            var proxima = Progresso.Atual.Proxima();
            var alvo = proxima == null
                ? "a máquina está pronta"
                : $"agora: {Elenco.De(proxima)?.Titulo}";
            // A conta sai de `Catalogo.Bancadas` e não de um doze escrito à mão:
            // a aula tem onze bancadas desde que a das fichas saiu, e o placar
            // continuava prometendo doze peças — a tela de fim chegava dizendo
            // "a máquina está pronta" com "11 de 12" na faixa de cima.
            _placar.text = $"Fábrica de IA        {feitas} de {Desafios.Catalogo.Bancadas} peças        {alvo}";
        }

        // ------------------------------------------------------------- aviso

        void MontarAviso(Transform pai)
        {
            _aviso = Widgets.Painel("Aviso", pai, Cores.Tinta);
            Widgets.Fixar(_aviso, new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(480f, 44f));

            _avisoTexto = Widgets.Texto("Texto", _aviso, 17, TextAnchor.MiddleCenter, Cores.Luz);
            Widgets.Esticar(_avisoTexto.rectTransform);

            _aviso.gameObject.SetActive(false);
        }

        /// <summary>Mostra (ou esconde) o convite para trabalhar numa bancada.</summary>
        public void MostrarAviso(Bancada bancada)
        {
            if (bancada == null)
            {
                _aviso.gameObject.SetActive(false);
                return;
            }

            var mestre = Elenco.De(bancada.Etapa);

            // A bancada trancada continua anunciando o nome do mestre: é o que
            // transforma "não pode" em "ainda não", e dá ao salão a forma de uma
            // aula com ordem, em vez de um monte de portas iguais.
            if (!Progresso.Atual.Liberada(bancada.Etapa))
            {
                _avisoTexto.text = $"trancada  ·  {mestre?.Nome} — ainda não abriu";
                _aviso.gameObject.SetActive(true);
                return;
            }

            var verbo = Progresso.Atual.Concluida(bancada.Etapa) ? "rever" : "trabalhar";
            _avisoTexto.text = $"[E]  {verbo} com {mestre?.Nome} — {mestre?.Titulo}";
            _aviso.gameObject.SetActive(true);
        }

        // ------------------------------------------------------------- falas

        void MontarCaixaDeFala(Transform pai)
        {
            _caixa = Widgets.Painel("Fala", pai, Cores.Tinta);
            Widgets.Fixar(_caixa, new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(760f, 128f));

            var borda = Widgets.Painel("Borda", _caixa, Cores.Luz);
            Widgets.Faixa(borda, true, 3f);

            _falaNome = Widgets.Texto("Nome", _caixa, 17, TextAnchor.UpperLeft, Cores.Luz);
            Widgets.Esticar(_falaNome.rectTransform);
            _falaNome.rectTransform.offsetMin = new Vector2(22f, 0f);
            _falaNome.rectTransform.offsetMax = new Vector2(-22f, -14f);

            _falaTexto = Widgets.Texto("Texto", _caixa, 22, TextAnchor.UpperLeft, Cores.Papel);
            Widgets.Esticar(_falaTexto.rectTransform);
            _falaTexto.rectTransform.offsetMin = new Vector2(22f, 34f);
            _falaTexto.rectTransform.offsetMax = new Vector2(-22f, -48f);

            var dica = Widgets.Texto("Dica", _caixa, 14, TextAnchor.LowerRight, Cores.Neblina);
            Widgets.Esticar(dica.rectTransform);
            dica.rectTransform.offsetMin = new Vector2(0f, 10f);
            dica.rectTransform.offsetMax = new Vector2(-22f, 0f);
            dica.text = "[E] continuar        [Esc] sair da conversa";

            _caixa.gameObject.SetActive(false);
        }

        /// <summary>
        /// Abre uma sequência de falas. Chama <paramref name="aoFechar"/> só
        /// quando a última for lida — quem chama não precisa contar falas.
        /// </summary>
        public void Falar(string nome, string[] falas, Action aoFechar)
        {
            _falasPendentes = falas;
            _falaAtual = 0;
            _aoFecharFala = aoFechar;
            // A mesma tecla abre a conversa e avança a fala. Sem marcar o quadro
            // de abertura, o toque que abriu a caixa também engole a primeira
            // fala — e o mestre parece mudo.
            _quadroDeAbertura = Time.frameCount;
            _falaNome.text = nome;
            _caixa.gameObject.SetActive(true);
            _aviso.gameObject.SetActive(false);
            MostrarFalaAtual();
        }

        /// <summary>Avança a fala. Devolve true enquanto a caixa seguir aberta.</summary>
        public bool AvancarFala()
        {
            if (!FalaAberta || Time.frameCount == _quadroDeAbertura) return FalaAberta;

            _falaAtual++;
            if (_falaAtual < _falasPendentes.Length)
            {
                MostrarFalaAtual();
                return true;
            }

            _caixa.gameObject.SetActive(false);
            var fim = _aoFecharFala;
            _aoFecharFala = null;
            fim?.Invoke();
            return false;
        }

        /// <summary>
        /// Fecha a caixa de fala DESCARTANDO o que vinha depois dela.
        ///
        /// É o Esc, e a diferença em relação a <see cref="AvancarFala"/> é toda: a
        /// tecla de avanço lê a conversa até o fim e ENTÃO faz o que a conversa
        /// prometia — abrir a bancada, mostrar a formatura. O Esc desiste da
        /// conversa, e desistir não pode disparar a promessa. Se disparasse, o
        /// aluno que apertasse Esc para NÃO entrar na bancada entraria nela.
        ///
        /// Devolve true se havia mesmo uma caixa aberta para fechar.
        /// </summary>
        public bool FecharFala()
        {
            if (!FalaAberta) return false;

            _aoFecharFala = null;
            _falasPendentes = null;
            _caixa.gameObject.SetActive(false);
            return true;
        }

        void MostrarFalaAtual() => _falaTexto.text = _falasPendentes[_falaAtual];
    }
}
