using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace FabricaDeIA.Nucleo
{
    /// <summary>
    /// Fotografa um minigame e grava o PNG em disco.
    ///
    /// Existe por um motivo prático: a interface do jogo é desenhada num Canvas
    /// em modo Overlay, e Overlay não aparece na captura de nenhuma câmera. Ou
    /// seja, sem isto o único jeito de conferir o layout de uma bancada é abrir
    /// o Editor, andar até ela e olhar — e a maior parte dos defeitos de
    /// interface que apareceram até agora (o teclado tapando a instrução, o
    /// painel sem botão de sair) eram justamente defeitos de layout, do tipo que
    /// salta em dois segundos numa imagem e passa despercebido no código.
    ///
    /// <c>CaptureScreenshotAsTexture</c> pega o quadro inteiro, Overlay
    /// incluído, e por isso precisa rodar depois que o quadro terminou de
    /// desenhar — daí a corrotina com <c>WaitForEndOfFrame</c>.
    /// </summary>
    public class Retrato : MonoBehaviour
    {
        /// <summary>
        /// Abre a bancada da etapa e grava a foto. O caminho vai para o log,
        /// com o prefixo RETRATO, que é como a ferramenta do Editor o encontra.
        /// </summary>
        public static void Tirar(string etapa, string pasta, float esperaSegundos = 0.6f)
        {
            var jogo = FindFirstObjectByType<Jogo>();
            if (jogo == null)
            {
                Debug.LogError("RETRATO: o jogo não está rodando");
                return;
            }

            var carregador = jogo.gameObject.GetComponent<Retrato>()
                          ?? jogo.gameObject.AddComponent<Retrato>();
            carregador.StartCoroutine(carregador.Fotografar(jogo, etapa, pasta, esperaSegundos));
        }

        /// <summary>
        /// Fotografa todas as bancadas registradas, uma por uma.
        ///
        /// Abre, espera o layout assentar, fotografa e fecha antes da seguinte.
        /// Fechar entre uma e outra não é zelo: sem isso as molduras se
        /// empilhariam e a segunda foto sairia com a primeira bancada por baixo.
        /// </summary>
        public static void TirarTodas(string pasta)
        {
            var jogo = FindFirstObjectByType<Jogo>();
            if (jogo == null)
            {
                Debug.LogError("RETRATO: o jogo não está rodando");
                return;
            }
            var carregador = jogo.gameObject.GetComponent<Retrato>()
                          ?? jogo.gameObject.AddComponent<Retrato>();
            carregador.StartCoroutine(carregador.TodasAsBancadas(jogo, pasta));
        }

        IEnumerator TodasAsBancadas(Jogo jogo, string pasta)
        {
            foreach (var etapa in Desafios.Catalogo.Registradas())
            {
                yield return Fotografar(jogo, etapa, pasta, 0.5f);

                Clicar("sair");
                yield return null;
                for (var i = 0; i < 5; i++)
                {
                    Clicar("continuar");
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(0.15f);
            }
            Debug.Log("RETRATO: todas as bancadas fotografadas");
        }

        /// <summary>
        /// Digita uma palavra na bancada 1 e prova que dá para continuar digitando
        /// depois de mandar.
        ///
        /// Este teste nasceu de um travamento que nenhuma foto mostraria: um
        /// palpite recusado deixava o buffer cheio, o <c>Update</c> passava a
        /// ignorar toda tecla, e o teclado morria sem uma palavra na tela dizendo
        /// por quê. De fora era idêntico a um jogo congelado.
        ///
        /// A palavra usada é proposital: um bolo de letras que não existe em
        /// português nenhum. Se ela for aceita e a linha 2 receber letra depois, as
        /// duas metades do defeito estão mortas.
        /// </summary>
        public static void TestarDigitacao()
        {
            var jogo = FindFirstObjectByType<Jogo>();
            if (jogo == null)
            {
                Debug.LogError("DIGITAÇÃO: o jogo não está rodando");
                return;
            }
            var carregador = jogo.gameObject.GetComponent<Retrato>()
                          ?? jogo.gameObject.AddComponent<Retrato>();
            carregador.StartCoroutine(carregador.Digitando(jogo));
        }

        IEnumerator Digitando(Jogo jogo)
        {
            jogo.AbrirDireto("e1");
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            var grade = AcharGrade();
            if (grade == null)
            {
                Debug.LogError("DIGITAÇÃO: grade não encontrada");
                yield break;
            }

            // Seis fileiras: o resto das casinhas diz quantas letras tem a palavra.
            var colunas = grade.childCount / 6;
            if (colunas < 2)
            {
                Debug.LogError($"DIGITAÇÃO: grade estranha — {grade.childCount} casinhas");
                yield break;
            }

            for (var i = 0; i < colunas; i++) yield return Teclar(Key.Z);

            // Uma letra a mais numa linha cheia. Ela não deve entrar — mas a
            // bancada TEM que dizer isso, porque silêncio aqui é o que parecia
            // travamento.
            yield return Teclar(Key.Z);
            yield return new WaitForSecondsRealtime(0.15f);
            var avisou = Instrucao() != null && Instrucao().Contains("cheia");
            if (!avisou)
                Debug.LogError("DIGITAÇÃO: linha cheia ficou muda — parece travada");

            yield return Teclar(Key.Enter);
            yield return new WaitForSecondsRealtime(0.2f);

            var mandou = Letra(grade, 0, 0) == "Z";
            if (!mandou)
            {
                Debug.LogError("DIGITAÇÃO: a palavra inventada foi recusada — " +
                               "o travamento do buffer cheio pode voltar");
            }

            yield return Teclar(Key.A);
            yield return new WaitForSecondsRealtime(0.2f);

            var continuou = Letra(grade, 1, 0) == "A";
            if (!continuou)
                Debug.LogError("DIGITAÇÃO: a segunda linha não aceitou letra — TRAVOU");

            Debug.Log(mandou && continuou && avisou
                ? $"DIGITAÇÃO: OK — palavra de {colunas} letras aceita, " +
                  "linha cheia avisa, e linha 2 livre"
                : "DIGITAÇÃO: FALHOU");

            Clicar("sair");
        }

        /// <summary>Aperta e solta uma tecla, respeitando a fronteira de quadro.</summary>
        static IEnumerator Teclar(Key tecla)
        {
            // `wasPressedThisFrame` só é verdade no quadro em que o estado muda, e
            // o Input System só processa a fila no começo do quadro. Sem os dois
            // `yield`, a tecla é apertada e solta dentro do mesmo quadro e a
            // bancada nunca a vê.
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(tecla));
            yield return null;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            yield return null;
        }

        /// <summary>
        /// O aluno está livre para andar?
        ///
        /// É a outra metade de "não ficou preso": uma caixa que fecha e deixa o
        /// jogador travado prende do mesmo jeito, só sem nada na tela para explicar.
        /// </summary>
        static bool Andando()
        {
            var jogador = FindFirstObjectByType<Mundo.Jogador>();
            return jogador != null && !jogador.Travado;
        }

        /// <summary>A caixa de fala do mestre está aberta agora?</summary>
        static bool FalandoAgora()
        {
            var hud = FindFirstObjectByType<UI.Hud>();
            return hud != null && hud.FalaAberta;
        }

        /// <summary>A linha de orientação da moldura, ou nulo se ela não existir.</summary>
        static string Instrucao()
        {
            var painel = FindFirstObjectByType<UI.PainelDeBancada>();
            if (painel == null) return null;

            foreach (var t in painel.GetComponentsInChildren<RectTransform>(true))
            {
                if (t.name != "Instrução") continue;
                var texto = t.GetComponentInChildren<UnityEngine.UI.Text>();
                return texto == null ? null : texto.text;
            }
            return null;
        }

        static RectTransform AcharGrade()
        {
            var painel = FindFirstObjectByType<UI.PainelDeBancada>();
            if (painel == null) return null;
            foreach (var t in painel.GetComponentsInChildren<RectTransform>(true))
                if (t.name == "Grade") return t;
            return null;
        }

        static string Letra(RectTransform grade, int linha, int coluna)
        {
            var caixa = grade.Find($"c{linha}_{coluna}");
            var texto = caixa == null ? null : caixa.GetComponentInChildren<UnityEngine.UI.Text>();
            return texto == null ? null : texto.text;
        }

        /// <summary>
        /// Prova que o botão de sair realmente fecha a bancada.
        ///
        /// Este teste existe porque o bug era invisível de dentro: o Esc e o
        /// botão disparavam, o desafio encerrava, o aluno voltava a andar — e a
        /// moldura CONTINUAVA na tela, porque ela é filha do palco da HUD e não
        /// do objeto do desafio. Do lado de fora parecia que a tecla não fazia
        /// nada. A verificação certa não é "o evento disparou?", é "sobrou
        /// alguma coisa na tela?".
        /// </summary>
        public static void TestarFechamento()
        {
            var jogo = FindFirstObjectByType<Jogo>();
            if (jogo == null)
            {
                Debug.LogError("FECHAMENTO: o jogo não está rodando");
                return;
            }
            var carregador = jogo.gameObject.GetComponent<Retrato>()
                          ?? jogo.gameObject.AddComponent<Retrato>();
            carregador.StartCoroutine(carregador.Fechando(jogo));
        }

        IEnumerator Fechando(Jogo jogo)
        {
            var falhas = 0;

            // O progresso de verdade do Editor volta no fim: o teste precisa mexer
            // nele para chegar na fala de fecho, e não pode cobrar isso de quem
            // estava jogando.
            var guardado = JsonUtility.ToJson(Progresso.Atual);

            foreach (var etapa in Desafios.Catalogo.Registradas())
            {
                // Finge que esta bancada JÁ foi resolvida.
                //
                // Sem isto o teste não chega onde o defeito mora: a fala de fecho do
                // mestre só aparece para quem resolveu, e sair sem resolver volta
                // direto ao salão. Foi por isso que doze "fechou limpo" seguidos
                // conviveram com um aluno preso na caixa de diálogo.
                //
                // Uma bancada por vez, e as outras zeradas, para a aula nunca ficar
                // completa aqui — completa, o fecho abriria a formatura em vez de
                // devolver o aluno ao salão, e o teste mediria outra coisa.
                Progresso.Reiniciar();
                Progresso.Atual.Concluir(etapa, 1);

                jogo.AbrirDireto(etapa);
                yield return null;
                yield return null;

                var abriu = FindFirstObjectByType<UI.PainelDeBancada>() != null;
                if (!abriu)
                {
                    Debug.LogError($"FECHAMENTO: {etapa} não abriu");
                    falhas++;
                    continue;
                }

                if (!Clicar("sair")) Debug.LogWarning("FECHAMENTO: botão sair não achado");
                yield return null;
                yield return null;

                // A bancada 5 não fecha no primeiro pedido, E ISSO É DE PROPÓSITO:
                // quem pede para sair no meio do corredor ainda vê a malha rodar uma
                // vez, porque a rede neural é o assunto do dia e um aluno travado na
                // sala 4 do platformer sairia da aula sem ter visto nenhuma. Ver
                // DesafioMalha.Desistir.
                //
                // O teste continua cobrando a MESMA coisa — que a moldura saia da
                // tela quando o aluno insiste — e não vira um cheque em branco: um
                // segundo "sair" que também não fechasse cairia no erro abaixo,
                // exatamente como antes.
                if (FindFirstObjectByType<UI.PainelDeBancada>() != null && Clicar("sair"))
                {
                    yield return null;
                    yield return null;
                }

                var sobrou = FindFirstObjectByType<UI.PainelDeBancada>() != null;
                if (sobrou)
                {
                    Debug.LogError($"FECHAMENTO: {etapa} deixou a moldura na tela");
                    falhas++;
                }
                else
                {
                    Debug.Log($"FECHAMENTO: {etapa} fechou limpo");
                }

                // Vence a caixa de fala do mestre COM A TECLA, que é o único jeito
                // que o aluno tem.
                //
                // A versão anterior deste laço clicava num botão "continuar" — e
                // esse botão não existe: a caixa de fala tem só o texto "[E]
                // continuar". Ou seja, o teste nunca tocou na caixa, e por isso não
                // viu o laço que prendia o aluno nela. Um teste que aperta um botão
                // inexistente passa sempre.
                if (!FalandoAgora())
                {
                    Debug.LogError($"FECHAMENTO: {etapa} não falou o fecho do mestre — " +
                                   "o teste não chegou onde queria");
                    falhas++;
                }

                for (var i = 0; i < 6 && FalandoAgora(); i++)
                {
                    yield return Teclar(Key.E);
                    yield return new WaitForSecondsRealtime(0.05f);
                }

                if (FalandoAgora())
                {
                    Debug.LogError($"FECHAMENTO: {etapa} deixou o aluno preso na caixa de fala");
                    falhas++;
                }
                else if (!Andando())
                {
                    Debug.LogError($"FECHAMENTO: {etapa} fechou a fala e deixou o aluno travado");
                    falhas++;
                }

                // E agora o Esc, que é a outra saída.
                //
                // O aluno está de pé na bancada (o `AbrirDireto` o levou até lá),
                // então apertar E abre a fala de abertura do mestre — a mesma que
                // ele veria andando até ali. Esc dali tem que devolvê-lo ao salão
                // SEM abrir a bancada: quem desiste da conversa desiste do trabalho.
                yield return Teclar(Key.E);
                yield return new WaitForSecondsRealtime(0.1f);

                if (!FalandoAgora())
                {
                    Debug.LogError($"FECHAMENTO: {etapa} não abriu conversa para testar o Esc");
                    falhas++;
                }
                else
                {
                    yield return Teclar(Key.Escape);
                    yield return new WaitForSecondsRealtime(0.1f);

                    if (FalandoAgora())
                    {
                        Debug.LogError($"FECHAMENTO: {etapa} — Esc não sai da conversa");
                        falhas++;
                    }
                    else if (FindFirstObjectByType<UI.PainelDeBancada>() != null)
                    {
                        Debug.LogError($"FECHAMENTO: {etapa} — Esc na conversa ABRIU a bancada");
                        falhas++;
                        Clicar("sair");
                        yield return null;
                    }
                    else if (!Andando())
                    {
                        Debug.LogError($"FECHAMENTO: {etapa} — Esc fechou a fala e travou o aluno");
                        falhas++;
                    }
                }

                yield return new WaitForSecondsRealtime(0.2f);
            }

            JsonUtility.FromJsonOverwrite(guardado, Progresso.Atual);
            Progresso.Atual.Salvar();

            Debug.Log(falhas == 0 ? "FECHAMENTO: OK" : $"FECHAMENTO: {falhas} FALHA(S)");
        }

        /// <summary>
        /// Abre uma bancada, segue um roteiro de cliques e só então fotografa.
        ///
        /// Serve para conferir os estados que só existem DEPOIS de jogar — a curva
        /// de treino desenhando, as ligações fechadas, o cartaz de vitória.
        /// Nenhum deles aparece numa foto da tela inicial, e todos são os que mais
        /// dependem de layout, que é justamente o que estas fotos existem para
        /// verificar.
        ///
        /// Os cliques são por RÓTULO e não por posição, porque quase toda bancada
        /// embaralha o que mostra.
        /// </summary>
        public static void TirarJogando(string etapa, string pasta, string[] cliques,
                                        float esperaEntre, float esperaFinal, string nome)
        {
            var jogo = FindFirstObjectByType<Jogo>();
            if (jogo == null)
            {
                Debug.LogError("RETRATO: o jogo não está rodando");
                return;
            }
            var carregador = jogo.gameObject.GetComponent<Retrato>()
                          ?? jogo.gameObject.AddComponent<Retrato>();
            carregador.StartCoroutine(carregador.Jogando(
                jogo, etapa, pasta, cliques, esperaEntre, esperaFinal, nome));
        }

        IEnumerator Jogando(Jogo jogo, string etapa, string pasta, string[] cliques,
                            float esperaEntre, float esperaFinal, string nome)
        {
            jogo.AbrirDireto(etapa);
            yield return null;
            yield return null;

            foreach (var rotulo in cliques)
            {
                if (!Clicar(rotulo)) Debug.LogWarning($"RETRATO: botão não achado — {rotulo}");
                yield return null;
                if (esperaEntre > 0f) yield return new WaitForSecondsRealtime(esperaEntre);
            }

            yield return Fotografar(jogo, null, pasta, esperaFinal, nome);
        }

        /// <summary>
        /// Fotografa a abertura em instantes escolhidos da cena.
        ///
        /// Os momentos que decidem se a animação funciona duram frações de
        /// segundo — o clarão do raio, o companheiro caído estalando. Sem poder
        /// congelar o instante, toda foto sairia do primeiro segundo.
        /// </summary>
        public static void TirarAbertura(string pasta, float[] instantes)
        {
            var jogo = FindFirstObjectByType<Jogo>();
            if (jogo == null)
            {
                Debug.LogError("RETRATO: o jogo não está rodando");
                return;
            }
            var carregador = jogo.gameObject.GetComponent<Retrato>()
                          ?? jogo.gameObject.AddComponent<Retrato>();
            carregador.StartCoroutine(carregador.Abrindo(jogo, pasta, instantes));
        }

        IEnumerator Abrindo(Jogo jogo, string pasta, float[] instantes)
        {
            var abertura = FindFirstObjectByType<UI.Abertura>();
            if (abertura == null)
            {
                Debug.LogError("RETRATO: a abertura não está na tela");
                yield break;
            }

            foreach (var instante in instantes)
            {
                abertura.Congelar(instante);
                yield return null;
                yield return Fotografar(jogo, null, pasta, 0.1f,
                                        $"abertura-{instante:0.0}s".Replace(",", "-"));
            }
        }

        /// <summary>Aciona o botão cujo rótulo contém o texto dado.</summary>
        static bool Clicar(string rotulo)
        {
            foreach (var botao in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
            {
                var texto = botao.GetComponentInChildren<UnityEngine.UI.Text>();
                if (texto == null || !texto.text.Contains(rotulo)) continue;
                botao.onClick.Invoke();
                return true;
            }
            return false;
        }

        // ------------------------------------------------- o clique de verdade

        /// <summary>
        /// O que está POR CIMA do centro deste retângulo, na visão do EventSystem.
        ///
        /// É a pergunta que <see cref="Clicar"/> não faz. <c>Clicar</c> chama
        /// <c>onClick.Invoke()</c>, que é acionar o BOTÃO e não dar um CLIQUE: pula
        /// o EventSystem, pula o raycast, e portanto pula tudo o que estiver na
        /// frente. Um véu transparente cobrindo o tabuleiro — o tipo de coisa que
        /// deixa uma bancada inteira surda ao mouse — passa incólume por ele.
        ///
        /// Aqui a resposta vem do mesmo caminho que o mouse do aluno percorre, e é
        /// por isso que ela sabe dizer "quem recebeu o clique não foi a palavra,
        /// foi um painel por cima dela".
        /// </summary>
        static GameObject SobOPonto(RectTransform alvo)
        {
            if (alvo == null) return null;

            var sistema = UnityEngine.EventSystems.EventSystem.current;
            if (sistema == null) return null;

            var cantos = new Vector3[4];
            alvo.GetWorldCorners(cantos);
            var centro = new Vector2((cantos[0].x + cantos[2].x) / 2f,
                                     (cantos[0].y + cantos[2].y) / 2f);

            var dados = new UnityEngine.EventSystems.PointerEventData(sistema) { position = centro };
            var atingidos =
                new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            sistema.RaycastAll(dados, atingidos);

            return atingidos.Count == 0 ? null : atingidos[0].gameObject;
        }

        /// <summary>
        /// Clica onde o aluno clicaria, e só acerta se o alvo estiver mesmo
        /// alcançável. Devolve falso quando o ponto não acertou nada ou quando
        /// quem estava por cima era outra coisa.
        /// </summary>
        static bool Apontar(RectTransform alvo)
        {
            var topo = SobOPonto(alvo);
            if (topo == null || !topo.transform.IsChildOf(alvo)) return false;

            var sistema = UnityEngine.EventSystems.EventSystem.current;
            var dados = new UnityEngine.EventSystems.PointerEventData(sistema);
            var tratador = UnityEngine.EventSystems.ExecuteEvents
                .GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(topo);
            if (tratador == null) return false;

            UnityEngine.EventSystems.ExecuteEvents.Execute(
                tratador, dados, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
            return true;
        }

        /// <summary>
        /// Prova que dá para ESCOLHER UMA PALAVRA na bancada dos holofotes.
        ///
        /// Nasceu de um defeito que nenhuma foto mostrava e nenhum teste daqui
        /// pegava: a bancada abria inteira, com as palavras desenhadas no lugar
        /// certo, e o clique nelas não fazia nada. A causa não estava na bancada —
        /// estava por CIMA dela, num painel transparente com <c>raycastTarget</c>
        /// ligado que comia o toque. De fora, indistinguível de um jogo travado.
        ///
        /// A verificação certa, então, não é "o botão existe?" nem "o listener
        /// dispara?" — as duas dariam verde com o véu no ar. É "o clique CHEGA?".
        /// Daí <see cref="Apontar"/>, e daí este teste exigir que o raycast do
        /// EventSystem caia dentro de cada palavra, uma por uma.
        ///
        /// E não para no alcance: acende uma lâmpada, confere o placar, exige que
        /// as barras de força estejam VAZIAS antes da pergunta (cheias, elas
        /// entregariam a resposta antes da aposta) e pergunta, exigindo que a
        /// rodada ande.
        /// </summary>
        public static void TestarHolofotes()
        {
            var jogo = FindFirstObjectByType<Jogo>();
            if (jogo == null)
            {
                Debug.LogError("HOLOFOTES: o jogo não está rodando");
                return;
            }
            var carregador = jogo.gameObject.GetComponent<Retrato>()
                          ?? jogo.gameObject.AddComponent<Retrato>();
            carregador.StartCoroutine(carregador.Holofoteando(jogo));
        }

        IEnumerator Holofoteando(Jogo jogo)
        {
            var falhas = 0;

            // A etapa vem do próprio desafio, e não de um "e10" escrito aqui.
            // As bancadas já foram renumeradas uma vez; um teste que guarda o
            // número por fora passa, no dia da renumeração, a testar calado a
            // bancada do vizinho — e continua dando verde.
            var sonda = new GameObject("sonda").AddComponent<Desafios.DesafioHolofotes>();
            var etapa = sonda.Etapa;
            Destroy(sonda.gameObject);

            jogo.AbrirDireto(etapa);
            // Dois quadros: o UGUI só resolve o layout no seguinte, e um raycast
            // feito cedo demais erra por medir retângulos que ainda não existem.
            yield return null;
            yield return null;

            var painel = FindFirstObjectByType<UI.PainelDeBancada>();
            if (painel == null)
            {
                Debug.LogError($"HOLOFOTES: {etapa} não abriu");
                yield break;
            }

            var frase = Achar(painel, "Frase");
            if (frase == null)
            {
                Debug.LogError("HOLOFOTES: a frase não foi montada");
                yield break;
            }

            // 1. TODA PALAVRA TEM QUE SER ALCANÇÁVEL PELO MOUSE.
            var palavras = new System.Collections.Generic.List<RectTransform>();
            foreach (Transform filho in frase)
                if (filho.name.Length > 1 && filho.name[0] == 'p') palavras.Add((RectTransform)filho);

            if (palavras.Count < 2)
            {
                Debug.LogError($"HOLOFOTES: só {palavras.Count} palavra(s) na frase");
                yield break;
            }

            foreach (var palavra in palavras)
            {
                var rotulo = palavra.GetComponentInChildren<UnityEngine.UI.Text>();
                var nome = rotulo == null ? palavra.name : rotulo.text;

                var botao = palavra.GetComponent<UnityEngine.UI.Button>();
                var pintura = palavra.GetComponent<UnityEngine.UI.Graphic>();

                if (botao == null || !botao.enabled || !botao.interactable)
                {
                    Debug.LogError($"HOLOFOTES: “{nome}” não tem botão vivo");
                    falhas++;
                    continue;
                }
                if (pintura == null || !pintura.raycastTarget)
                {
                    Debug.LogError($"HOLOFOTES: “{nome}” não recebe raycast");
                    falhas++;
                    continue;
                }

                var topo = SobOPonto(palavra);
                if (topo == null)
                {
                    Debug.LogError($"HOLOFOTES: o clique em “{nome}” não acerta nada — " +
                                   "a palavra está fora da tela");
                    falhas++;
                }
                else if (!topo.transform.IsChildOf(palavra))
                {
                    Debug.LogError($"HOLOFOTES: o clique em “{nome}” cai em " +
                                   $"“{topo.name}” — há algo por cima comendo o toque");
                    falhas++;
                }
            }

            // 2. ACENDER UMA PALAVRA MUDA O PLACAR.
            var antes = Placar(painel);
            if (!Apontar(palavras[0]))
                Debug.LogError("HOLOFOTES: a primeira palavra não aceitou o clique");
            yield return null;

            var depois = Placar(painel);
            if (depois == antes)
            {
                Debug.LogError($"HOLOFOTES: acender não mudou nada — placar continua “{antes}”");
                falhas++;
            }
            else if (!depois.StartsWith("acesas: 1"))
            {
                Debug.LogError($"HOLOFOTES: acender deu “{depois}”, e não uma lâmpada acesa");
                falhas++;
            }

            // 3. AS BARRAS DE FORÇA COMEÇAM VAZIAS.
            //
            // O trilho nasce cheio, então esquecer de preenchê-lo mostra TUDO em vez
            // de nada — e a bancada entregaria de graça a informação que a pergunta
            // deveria comprar. Um número, e não um "parece certo".
            var cheias = 0;
            foreach (var miolo in painel.GetComponentsInChildren<RectTransform>(true))
                if (miolo.name == "miolo" && miolo.sizeDelta.x > 1f) cheias++;

            if (cheias > 0)
            {
                Debug.LogError($"HOLOFOTES: {cheias} barra(s) de força já cheias ANTES " +
                               "de perguntar — a resposta está exposta");
                falhas++;
            }

            // 4. PERGUNTAR FAZ A RODADA ANDAR.
            var acao = Achar(painel, "Ação");
            var placarAntes = Placar(painel);
            if (acao == null || !Apontar(acao))
            {
                Debug.LogError("HOLOFOTES: o botão de perguntar não aceitou o clique");
                falhas++;
            }
            else
            {
                yield return null;
                // Ou o contador de perguntas andou, ou ela acertou e o cartaz subiu.
                // As duas são "a rodada andou"; nenhuma das duas é "nada aconteceu".
                var andou = Placar(painel) != placarAntes || Achar(painel, "Cartaz") != null;
                if (!andou)
                {
                    Debug.LogError("HOLOFOTES: perguntar não mudou nada na tela");
                    falhas++;
                }
            }

            Debug.Log(falhas == 0
                ? $"HOLOFOTES: OK — {palavras.Count} palavras alcançáveis pelo clique, " +
                  "lâmpada acende, barras vazias antes da pergunta, pergunta anda"
                : $"HOLOFOTES: {falhas} FALHA(S)");

            Clicar("sair");
        }

        /// <summary>O primeiro descendente da moldura com este nome, ou nulo.</summary>
        static RectTransform Achar(UI.PainelDeBancada painel, string nome)
        {
            foreach (var t in painel.GetComponentsInChildren<RectTransform>(true))
                if (t.name == nome) return t;
            return null;
        }

        /// <summary>A linha de placar da bancada, ou string vazia se não houver.</summary>
        static string Placar(UI.PainelDeBancada painel)
        {
            foreach (var t in painel.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                if (t.name == "Placar") return t.text;
            return string.Empty;
        }

        IEnumerator Fotografar(Jogo jogo, string etapa, string pasta, float espera,
                               string nomeDoArquivo = null)
        {
            if (!string.IsNullOrEmpty(etapa)) jogo.AbrirDireto(etapa);

            // Um quadro não basta: o UGUI só resolve o layout no fim do quadro
            // seguinte, e uma foto tirada cedo demais mostra tudo empilhado na
            // origem — o que daria um falso positivo de "layout quebrado".
            yield return null;
            yield return null;
            if (espera > 0f) yield return new WaitForSecondsRealtime(espera);
            yield return new WaitForEndOfFrame();

            var foto = ScreenCapture.CaptureScreenshotAsTexture();
            var bytes = foto.EncodeToPNG();
            Destroy(foto);

            Directory.CreateDirectory(pasta);
            var nome = nomeDoArquivo ?? (string.IsNullOrEmpty(etapa) ? "atelie" : etapa);
            var caminho = Path.Combine(pasta, $"{nome}.png");
            File.WriteAllBytes(caminho, bytes);

            Debug.Log($"RETRATO: {caminho}");
        }
    }
}
