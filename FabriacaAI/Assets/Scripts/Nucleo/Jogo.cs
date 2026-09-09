using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Arte;
using FabricaDeIA.Mundo;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FabricaDeIA.Nucleo
{
    /// <summary>
    /// O ponto de partida. É o único componente que a cena guarda.
    ///
    /// Ele carrega a arte, levanta o ateliê, põe o aluno de pé e liga a HUD nos
    /// eventos do mundo. Tudo o mais nasce daqui — a cena salva no disco tem
    /// duas linhas de conteúdo, o que significa que revisar o jogo é ler código
    /// e não abrir o Editor.
    ///
    /// O caminho de uma bancada: o aluno chega, o mestre fala, o desafio
    /// acontece — e acabou. Fechar a bancada devolve o aluno ao salão andando,
    /// sem mais nenhuma tecla no meio.
    ///
    /// HAVIA UM FECHO, e ele saiu. O mestre dizia uma última frase quando o
    /// aluno voltava com a peça pronta, e para andar de novo era preciso ler a
    /// caixa e apertar E. Na aula isso virou confusão: quem sai de um minigame
    /// está saindo, não começando a ler; a caixa aparecia como se fosse um erro,
    /// e a turma travava perguntando "e agora?". Uma tela que se fecha sozinha
    /// não custa nada; uma que exige tecla para devolver o controle custa a
    /// atenção de trinta pessoas de uma vez.
    ///
    /// O que ficou no lugar do fecho é a tela de fim (<see cref="UI.Fim"/>),
    /// UMA vez só, quando a última bancada fecha: a máquina se levanta e o
    /// certificado vem em seguida.
    /// </summary>
    public class Jogo : MonoBehaviour
    {
        enum Estado { NaAbertura, Andando, Conversando, NaBancada }

        Folhas _folhas;
        Jogador _jogador;
        IReadOnlyList<Mundo.Bancada> _bancadas;
        Hud _hud;
        Estado _estado = Estado.NaAbertura;
        Bancada _bancadaAberta;

        void Awake()
        {
            Application.targetFrameRate = 60;

#if UNITY_EDITOR
            // No Editor, o jogo continua rodando com a janela em segundo plano.
            // Sem isto o loop congela assim que o foco sai, e qualquer
            // ferramenta que dirija o Editor de fora — a de retrato, por
            // exemplo — fica esperando para sempre um quadro que não vem.
            // No build de navegador fica desligado de propósito: aba escondida
            // não deve gastar bateria de notebook de escola.
            Application.runInBackground = true;
#endif

            // Física a 50 Hz e ordenação por Y são pressupostos do mundo; ambos
            // ficam aqui para não dependerem de alguém ter mexido no Editor.
            Time.fixedDeltaTime = 0.02f;

            Progresso.Carregar();
            _folhas = Folhas.Carregar();

            var construtor = new ConstrutorDoAtelie(_folhas);
            construtor.Construir();

            _bancadas = construtor.Bancadas;
            _jogador = CriarJogador(construtor);
            CriarCamera();

            _hud = gameObject.AddComponent<Hud>();
            _hud.Construir();

            _jogador.AlcanceMudou += bancada =>
            {
                if (_estado == Estado.Andando) _hud.MostrarAviso(bancada);
            };
            _jogador.Interagiu += Abordar;

            // As luzes do salão seguem o progresso sozinhas. Sem isto, a bancada
            // recém-liberada só acenderia na próxima vez que a cena nascesse — e o
            // aluno acabou de sair da anterior, olhando para o salão.
            Progresso.Mudou += AtualizarBancadas;
            AtualizarBancadas();

            // O ateliê é levantado antes da abertura, e fica esperando atrás
            // dela. Construir depois faria o aluno olhar uma tela preta no
            // instante em que apertasse "começar" — e o mundo leva um piscar de
            // olhos para nascer, então não há o que ganhar adiando.
            AbrirAbertura();

            // Relatório de boot. Com o mundo nascendo de código, é a única
            // forma barata de responder "montou tudo?" sem abrir o Editor —
            // e é o que aparece no log de um build de navegador com problema.
            Debug.Log($"ATELIÊ: OK — {Atelie.LarguraTiles}x{Atelie.AlturaTiles} tiles, " +
                      $"{construtor.Bancadas.Count} bancadas, {Atelie.Claraboias.Count} claraboias, " +
                      $"{Progresso.Atual.Concluidas} etapas feitas");
        }

        Jogador CriarJogador(ConstrutorDoAtelie construtor)
        {
            var objeto = new GameObject("Jogador");
            objeto.transform.SetParent(construtor.Raiz, false);
            objeto.transform.position = Atelie.CentroDoTile(Atelie.Inicio.x, Atelie.Inicio.y)
                                      + new Vector2(0f, -0.4f);

            var desenho = objeto.AddComponent<SpriteRenderer>();
            desenho.sortingOrder = ConstrutorDoAtelie.OrdemObjetos;

            // A caixa é mais estreita que um tile de propósito. Do tamanho
            // exato, o aluno engancha em quina e sente o jogo travando; mais
            // estreita, ele passa por corredor de um tile sem precisar de
            // pontaria. Foi a queixa número um do playtest da versão web.
            var caixa = objeto.AddComponent<CapsuleCollider2D>();
            caixa.size = new Vector2(0.5f, 0.34f);
            caixa.offset = new Vector2(0f, 0.17f);
            caixa.direction = CapsuleDirection2D.Horizontal;

            objeto.AddComponent<Rigidbody2D>();
            objeto.AddComponent<Andarilho>();

            var jogador = objeto.AddComponent<Jogador>();
            jogador.Preparar(_folhas, construtor.Bancadas);
            return jogador;
        }

        void CriarCamera()
        {
            var existente = Camera.main;
            var objeto = existente != null ? existente.gameObject : new GameObject("Câmera", typeof(Camera));
            objeto.tag = "MainCamera";

            var seguidora = objeto.GetComponent<CameraSeguidora>() ?? objeto.AddComponent<CameraSeguidora>();
            seguidora.Preparar(_jogador.transform);
        }

        // -------------------------------------------------------- a abertura

        /// <summary>
        /// Mostra o menu e, se o aluno quiser, a cena do raio.
        ///
        /// O aluno fica travado e a interface do salão desligada até a abertura
        /// sair de cena. Sem travar, ele andaria pelo ateliê atrás do menu e
        /// chegaria numa bancada sem nunca ter visto por que a máquina está
        /// quebrada — que é a única coisa que dá sentido às doze.
        /// </summary>
        void AbrirAbertura()
        {
            _estado = Estado.NaAbertura;
            _jogador.Travado = true;
            _hud.MostrarSalao(false);

            UI.Abertura.Montar(_hud.Palco, _folhas, querDiploma =>
            {
                _hud.MostrarSalao(true);
                if (querDiploma) AbrirFormatura();
                else VoltarAoSalao();
            });
        }

        // ------------------------------------------------------- a conversa

        /// <summary>
        /// O quadro em que o aluno foi devolvido ao salão.
        ///
        /// Existe por causa de um laço que prendia o aluno na aula: a MESMA tecla
        /// fecha o que está na frente dele e aborda o mestre. Quem devolve o
        /// controle destrava o jogador no mesmo quadro — e como o
        /// `wasPressedThisFrame` daquela tecla continua verdadeiro, o
        /// `Jogador.Update`, que roda depois, entendia o mesmo toque como "falar
        /// com quem está ao alcance" e reabria o que tinha acabado de fechar. O
        /// aluno apertava E, a caixa piscava e voltava; apertava de novo e caía
        /// dentro do minigame. Não havia saída, porque ele estava colado na
        /// bancada que acabou de terminar.
        ///
        /// Nasceu para as caixas de fala e hoje vale para toda saída, marcada em
        /// `VoltarAoSalao`: com o fecho do mestre removido, é a tecla que encerra
        /// o próprio minigame que passou a chegar destravada no salão.
        ///
        /// O caso simétrico — a tecla que ABRE a caixa engolindo a primeira fala —
        /// já tinha guarda dentro da HUD. Faltava esta.
        /// </summary>
        int _quadroDoFecho = -1;

        void Abordar(Bancada bancada)
        {
            if (_estado != Estado.Andando) return;
            // O toque que devolveu o aluno ao salão não aborda ninguém.
            if (Time.frameCount == _quadroDoFecho) return;

            if (!Progresso.Atual.Liberada(bancada.Etapa))
            {
                Trancada(bancada.Etapa);
                return;
            }

            var mestre = Elenco.De(bancada.Etapa);
            if (mestre == null) return;

            _bancadaAberta = bancada;
            _estado = Estado.Conversando;
            _jogador.Travado = true;
            _hud.Falar($"{mestre.Nome}, {mestre.Oficio}", mestre.Antes, AbrirBancada);
        }

        /// <summary>
        /// O evento é estático e vive além desta cena: sem soltar, uma partida
        /// nova acumularia inscritos mortos e o progresso passaria a acender
        /// bancadas de um salão que já não existe.
        /// </summary>
        void OnDestroy() => Progresso.Mudou -= AtualizarBancadas;

        /// <summary>
        /// Acende, apaga ou deixa respirando cada uma das doze.
        /// </summary>
        void AtualizarBancadas()
        {
            if (_bancadas == null) return;
            foreach (var bancada in _bancadas)
            {
                // O balcão do certificado nunca é "tentado" — não se registra
                // passagem por ele. Aqui a pulsação significa outra coisa: ele
                // chama quando há algo a retirar, ou seja, quando as doze
                // acabaram. Antes disso fica quieto, para não competir com a
                // bancada que o aluno realmente precisa achar agora.
                var chama = bancada.Etapa == "e12"
                    ? !Progresso.Atual.TodasTentadas
                    : Progresso.Atual.Tentada(bancada.Etapa);

                bancada.Estado(Progresso.Atual.Liberada(bancada.Etapa), chama);
            }
        }

        /// <summary>
        /// O aluno bateu numa bancada que ainda não abriu.
        ///
        /// Diz para onde ir, e não só que não pode. "Fechado" sem destino faz o
        /// aluno andar pelo salão testando porta por porta, e são doze.
        /// </summary>
        void Trancada(string etapa)
        {
            var anterior = Anterior(etapa);
            var mestre = anterior == null ? null : Elenco.De(anterior);

            _estado = Estado.Conversando;
            _jogador.Travado = true;
            _hud.Falar("Esta bancada ainda não abriu",
                new[]
                {
                    mestre == null
                        ? "Comece pela primeira bancada do salão."
                        : $"Passe primeiro por {mestre.Nome}, {mestre.Oficio}."
                },
                VoltarAoSalao);
        }

        /// <summary>A etapa imediatamente anterior na ordem da aula, ou nulo.</summary>
        static string Anterior(string etapa)
        {
            if (string.IsNullOrEmpty(etapa) || etapa.Length < 2) return null;
            if (!int.TryParse(etapa.Substring(1), out var numero)) return null;
            return numero <= 1 ? null : $"e{numero - 1}";
        }

        /// <summary>
        /// Abre a bancada de uma etapa sem passar pelo salão nem pelas falas.
        ///
        /// É por aqui que a ferramenta de retrato fotografa cada minigame para
        /// conferência de layout, e é por aqui que um dia entra o atalho do
        /// professor — que na versão web era o `?balcao=8`, para demonstrar uma
        /// etapa na frente da turma sem atravessar a aula inteira.
        /// </summary>
        public void AbrirDireto(string etapa)
        {
            var bancada = _bancadas.FirstOrDefault(b => b.Etapa == etapa);
            if (bancada == null)
            {
                Debug.LogWarning($"bancada inexistente: {etapa}");
                return;
            }
            // Se a abertura ainda estiver na tela, ela sai antes: a moldura da
            // bancada é filha do mesmo palco e entraria POR CIMA dela, deixando
            // a cena do raio viva embaixo — invisível, e consumindo quadro.
            var abertura = FindFirstObjectByType<UI.Abertura>();
            if (abertura != null)
            {
                Destroy(abertura.gameObject);
                _hud.MostrarSalao(true);
            }

            // E se alguma bancada ainda estiver aberta, ela sai também.
            //
            // Isto custou uma investigação: uma conferência que fotografa a
            // bancada sem fechá-la deixava a moldura viva, a conferência seguinte
            // abria a sua POR CIMA, e o clique em "sair" achava o botão da
            // fantasma primeiro — doze falsas falhas de fechamento seguidas, todas
            // por causa da que sobrou.
            foreach (var aberta in FindObjectsByType<UI.PainelDeBancada>(FindObjectsSortMode.None))
                Destroy(aberta.gameObject);

            // Leva o aluno até a bancada antes de abrir. Não é enfeite: é o que faz
            // o atalho terminar na mesma situação do caminho normal — de pé, colado
            // na bancada. Foi ali que o laço da caixa de fala se escondeu do teste
            // de fechamento por semanas.
            _jogador.Colocar(bancada.PontoDeChegada);

            _bancadaAberta = bancada;
            _jogador.Travado = true;
            AbrirBancada();
        }

        void AbrirBancada()
        {
            _estado = Estado.NaBancada;
            var etapa = _bancadaAberta.Etapa;

            var desafio = Desafios.Catalogo.Criar(etapa, transform);
            if (desafio == null)
            {
                // Bancada ainda sem desafio portado: devolve o aluno ao salão
                // sem estrela. É melhor uma porta que se anuncia fechada do que
                // uma que dá prêmio de mentira.
                var mestre = Elenco.De(etapa);
                _hud.Falar(
                    $"{mestre.Nome}, {mestre.Oficio}",
                    new[] { "Minha bancada ainda está em obras.", "Volte quando eu tiver montado." },
                    Encerrar);
                return;
            }

            desafio.Terminou += estrelas =>
            {
                // Antes de registrar, porque é a comparação com o depois que diz
                // se FOI ESTA bancada que fechou a aula. Ver só o depois faria a
                // tela de fim voltar toda vez que o aluno reabrisse qualquer
                // bancada para rever.
                var faltavam = !Progresso.Atual.TodasTentadas;

                // Registra SEMPRE, inclusive com zero estrela: quem tentou e não
                // resolveu gastou aula, e o professor precisa ver isso. Zero
                // estrela não conta como concluída em lugar nenhum — `Concluida`
                // exige estrela > 0 —, então guardar não inventa progresso.
                // O balcão do certificado fica de fora do registro: ele não é
                // uma das onze, não dá estrela, e anotá-lo faria a folha do aluno
                // dizer "visitadas: 12 de 11".
                if (etapa != "e12")
                    Progresso.Atual.Concluir(etapa, estrelas, desafio.Segundos);

                _fechouAAula = faltavam && Progresso.Atual.TodasTentadas;
                Encerrar();
            };

            // Se a bancada quebrar ao montar, o aluno ficaria preso: travado no
            // salão, sem tela e sem saída, no meio de uma aula de 90 minutos.
            // Melhor engolir a falha, devolvê-lo ao ateliê e deixar o erro no
            // log para quem for consertar depois.
            try
            {
                var mestre = Elenco.De(etapa);
                desafio.Abrir(_hud.Palco, $"{mestre.Nome}, {mestre.Oficio}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"bancada {etapa} não abriu: {e}");
                Destroy(desafio.gameObject);
                VoltarAoSalao();
            }
        }

        /// <summary>
        /// A bancada fechou. Nada de fala: o aluno volta a andar no mesmo quadro.
        ///
        /// Vale para as duas saídas — o botão "sair" e o fim do minigame —,
        /// porque as duas são a mesma coisa do ponto de vista de quem está com a
        /// mão no teclado: a tela sumiu, e o boneco tem que responder de novo.
        /// </summary>
        void Encerrar()
        {
            var etapa = _bancadaAberta != null ? _bancadaAberta.Etapa : null;

            // Do balcão do certificado se sai para a formatura, sempre — com a
            // aula completa ou pela metade. A folha registra o que a pessoa fez,
            // e quem parou na sétima bancada fez sete bancadas de trabalho.
            if (etapa == "e12")
            {
                AbrirFormatura();
                return;
            }

            // A última bancada da aula não devolve ninguém ao salão: fecha a aula
            // em cena. É a marca escrita em `AbrirBancada`, e não uma pergunta ao
            // progresso, para que isto aconteça UMA vez — quem voltar depois a
            // uma bancada já feita sai andando como em qualquer outra.
            if (_fechouAAula)
            {
                _fechouAAula = false;
                AbrirFim();
                return;
            }

            VoltarAoSalao();
        }

        /// <summary>
        /// Esta bancada acabou de completar a aula? Zerado assim que é usado.
        /// </summary>
        bool _fechouAAula;

        /// <summary>
        /// O fim da aula: a máquina se levanta e o certificado vem logo depois.
        ///
        /// A tela de fim entra ANTES da formatura, e não em vez dela: uma é a
        /// recompensa, a outra é o papel que o aluno leva. Trocar a ordem faria a
        /// aula terminar num formulário.
        /// </summary>
        void AbrirFim()
        {
            _estado = Estado.Conversando;
            _jogador.Travado = true;
            _bancadaAberta = null;
            UI.Fim.Abrir(_hud.Palco, AbrirFormatura);
        }

        /// <summary>
        /// Abre a formatura: o painel de resultados e o diploma.
        ///
        /// Vale para quem acabou de fechar a décima segunda bancada e para quem
        /// voltou depois querendo o arquivo de novo — daí ser público. Um aluno que
        /// fechou a aba antes de baixar não pode perder a atividade por isso.
        /// </summary>
        public void AbrirFormatura()
        {
            _estado = Estado.Conversando;
            _jogador.Travado = true;
            UI.Formatura.Abrir(_hud.Palco, VoltarAoSalao);
        }

        /// <summary>
        /// O menu de pausa. Trava o aluno enquanto está no ar, pelo mesmo motivo
        /// que a conversa trava: painel na frente e personagem andando atrás é
        /// como o aluno acaba dentro de uma bancada sem ter pedido.
        /// </summary>
        void AbrirMenuDePausa()
        {
            _estado = Estado.Conversando;
            _jogador.Travado = true;

            _hud.AbrirMenu(
                continuar: () =>
                {
                    _hud.FecharMenu();
                    VoltarAoSalao();
                },
                inicio: () =>
                {
                    _hud.FecharMenu();
                    AbrirAbertura();
                },
                sair: () =>
                {
                    _hud.FecharMenu();
                    // No navegador isto não fecha nada — a aba é do aluno, não do
                    // jogo. Por isso a tela inicial fica LOGO ACIMA no menu: é ela
                    // a saída de verdade de quem joga na web, e "fechar" existe
                    // para quem roda o executável na sala de informática.
                    Application.Quit();
                    VoltarAoSalao();
                });
        }

        void VoltarAoSalao()
        {
            // O toque que devolveu o aluno ao salão não aborda ninguém.
            //
            // A marca era posta só ao fechar uma caixa de fala, porque era a fala
            // que ficava entre o minigame e o salão. Sem ela, agora que o fecho
            // saiu, a MESMA tecla que encerrou a bancada — E, espaço ou Enter,
            // que é como metade das bancadas confirma — destravaria o aluno e, no
            // mesmo quadro, seria lida pelo `Jogador.Update` como "trabalhar na
            // bancada ao alcance". Ele está colado nela: entraria de novo, sem
            // ter pedido, na bancada que acabou de terminar.
            _quadroDoFecho = Time.frameCount;

            _estado = Estado.Andando;
            _jogador.Travado = false;
            _bancadaAberta = null;
            _hud.MostrarAviso(_jogador.AoAlcance);
        }

        void Update()
        {
            var teclado = Keyboard.current;
            if (teclado == null) return;

#if UNITY_EDITOR
            // O ATALHO DO PROFESSOR: F1 a F12 abrem a bancada de mesmo número.
            //
            // `AbrirDireto` já existia e já fazia a coisa certa — fecha a abertura,
            // fecha bancada que tenha sobrado e coloca o aluno de pé no ponto de
            // chegada, para o atalho terminar na mesma situação do caminho normal.
            // Faltava só quem o chamasse.
            //
            // SÓ NO EDITOR, de propósito. O jogo publicado é aberto por link numa
            // sala inteira ao mesmo tempo; uma tecla que pula para a bancada 11
            // viraria a brincadeira da aula em trinta segundos. Quem precisa do
            // atalho é quem está montando a aula, e essa pessoa está no Editor.
            //
            // F1-F12 e não 1-9: são doze bancadas, e as teclas de número já são
            // usadas dentro de bancada. Teclas de função não colidem com nada.
            for (var n = 1; n <= 12; n++)
            {
                var tecla = teclado[(Key)((int)Key.F1 + n - 1)];
                if (tecla == null || !tecla.wasPressedThisFrame) continue;

                Debug.Log($"atalho do Editor: abrindo a bancada e{n}");
                AbrirDireto($"e{n}");
                return;
            }
#endif

            // Esc sai da conversa. O Esc já fechava a moldura da bancada, e por
            // isso o aluno aprendia na primeira bancada que Esc é a tecla de sair —
            // e depois batia numa caixa de fala que não escutava tecla nenhuma além
            // de avançar. Uma tecla que funciona em metade das telas é pior do que
            // uma que não existe.
            if (_hud.FalaAberta && teclado.escapeKey.wasPressedThisFrame)
            {
                _hud.FecharFala();
                _quadroDoFecho = Time.frameCount;
                VoltarAoSalao();
                return;
            }

            // Esc no salão abre o menu; Esc com o menu aberto volta ao trabalho.
            // A mesma tecla nos dois sentidos, porque é assim que ela se comporta
            // em todo o resto do jogo: Esc é sempre "sair do que estou vendo".
            if (teclado.escapeKey.wasPressedThisFrame)
            {
                if (_hud.MenuAberto)
                {
                    _hud.FecharMenu();
                    VoltarAoSalao();
                    return;
                }
                if (_estado == Estado.Andando)
                {
                    AbrirMenuDePausa();
                    return;
                }
            }

            // A mesma tecla que abre a conversa avança a fala. Duas teclas para
            // dois momentos parecidos é como se perde a turma no primeiro minuto.
            if (_hud.FalaAberta &&
                (teclado.eKey.wasPressedThisFrame ||
                 teclado.spaceKey.wasPressedThisFrame ||
                 teclado.enterKey.wasPressedThisFrame))
            {
                // Se esta tecla FECHOU a caixa, o quadro fica marcado — ver
                // `Abordar`, que é onde a marca serve.
                if (!_hud.AvancarFala()) _quadroDoFecho = Time.frameCount;
            }
        }
    }
}
