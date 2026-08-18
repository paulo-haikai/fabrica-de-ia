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
            // Marca o tutorial como visto: aqui o assunto é o teclado, e seis
            // passos de explicação só atrasariam a prova.
            Progresso.Atual.MarcarTutorial("e1");
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
        /// Faz o jogo esquecer que já explicou uma bancada, para o tutorial nascer
        /// de novo na próxima visita. Existe só para a conferência: sem isto, uma
        /// foto de tutorial só sai na primeira execução da vida do PlayerPrefs.
        /// </summary>
        public static void EsquecerTutorial(string etapa) =>
            Progresso.Atual.tutoriaisVistos.Remove(etapa);

        /// <summary>
        /// Roda os doze tutoriais do começo ao fim, apertando "entendi" até o
        /// último passo, e reclama de qualquer exceção pelo caminho.
        ///
        /// Este é o teste que o compilador não pode fazer. Um roteiro de tutorial
        /// é uma lista de <c>Action</c>s que mexem no tabuleiro por fora do fluxo
        /// normal do jogo — chamar <c>Encaixar</c> sem clique, medir sem o aluno
        /// ter medido, avançar um caso que já era o último. Nada disso quebra a
        /// compilação; tudo isso quebra em execução, e sempre no meio de uma
        /// demonstração, que é o pior lugar possível para quebrar.
        ///
        /// Por isso o teste apalpa três coisas: que a explicação NASCE (a bancada
        /// que esqueceu o roteiro reprova), que ela chega ao fim sem estourar, e
        /// que ao terminar ela SAI da tela deixando a bancada jogável.
        /// </summary>
        public static void TestarTutoriais()
        {
            var jogo = FindFirstObjectByType<Jogo>();
            if (jogo == null)
            {
                Debug.LogError("TUTORIAL: o jogo não está rodando");
                return;
            }
            var carregador = jogo.gameObject.GetComponent<Retrato>()
                          ?? jogo.gameObject.AddComponent<Retrato>();
            carregador.StartCoroutine(carregador.Explicando(jogo));
        }

        IEnumerator Explicando(Jogo jogo)
        {
            var falhas = 0;
            var estouros = 0;
            void Ouvir(string recado, string pilha, LogType tipo)
            {
                if (tipo == LogType.Exception || tipo == LogType.Error) estouros++;
            }

            Application.logMessageReceived += Ouvir;

            foreach (var etapa in Desafios.Catalogo.Registradas())
            {
                // Esquecer que já viu é o que faz o tutorial nascer de novo. Sem
                // isto o teste passaria trivialmente na segunda execução.
                Progresso.Atual.tutoriaisVistos.Remove(etapa);

                var antes = estouros;
                jogo.AbrirDireto(etapa);
                yield return null;
                yield return null;

                var tutorial = FindFirstObjectByType<UI.Tutorial>();
                if (tutorial == null)
                {
                    Debug.LogError($"TUTORIAL: {etapa} abriu sem explicação");
                    falhas++;
                    Clicar("sair");
                    yield return null;
                    continue;
                }

                var passos = 0;
                // Teto de segurança: um roteiro que não termina travaria o teste
                // para sempre, e travado ele não reporta nada.
                while (FindFirstObjectByType<UI.Tutorial>() != null && passos < 40)
                {
                    if (Avancar()) passos++;
                    yield return null;
                    yield return new WaitForSecondsRealtime(0.05f);
                }

                var sobrou = FindFirstObjectByType<UI.Tutorial>() != null;
                var jogavel = FindFirstObjectByType<UI.PainelDeBancada>() != null;

                if (sobrou)
                {
                    Debug.LogError($"TUTORIAL: {etapa} não terminou em {passos} avanços");
                    falhas++;
                }
                else if (!jogavel)
                {
                    Debug.LogError($"TUTORIAL: {etapa} terminou e levou a bancada com ele");
                    falhas++;
                }
                else if (estouros > antes)
                {
                    Debug.LogError($"TUTORIAL: {etapa} estourou durante a demonstração");
                    falhas++;
                }
                else
                {
                    Debug.Log($"TUTORIAL: {etapa} explicou {passos} passos e liberou a bancada");
                }

                Clicar("sair");
                yield return null;
                for (var i = 0; i < 5; i++)
                {
                    Clicar("continuar");
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(0.15f);
            }

            Application.logMessageReceived -= Ouvir;
            Debug.Log(falhas == 0 ? "TUTORIAL: OK" : $"TUTORIAL: {falhas} FALHA(S)");
        }

        /// <summary>
        /// Aperta o botão do tutorial, se ele estiver liberado.
        ///
        /// Devolve false enquanto a demonstração roda — o botão fica travado de
        /// propósito nesse intervalo, e insistir nele não adianta; o laço de fora
        /// só precisa esperar o próximo quadro.
        /// </summary>
        static bool Avancar()
        {
            var tutorial = FindFirstObjectByType<UI.Tutorial>();
            if (tutorial == null) return false;

            foreach (var botao in tutorial.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                if (botao.name != "Avançar" || !botao.interactable) continue;

                // Um estouro dentro do passo sobe pelo onClick e mataria a
                // corrotina do teste — e uma bancada quebrada esconderia as outras
                // onze. O log já registrou; aqui só não deixamos parar a fila.
                try { botao.onClick.Invoke(); }
                catch (System.Exception erro) { Debug.LogError($"TUTORIAL: passo estourou — {erro.Message}"); }
                return true;
            }
            return false;
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
                Progresso.Atual.MarcarTutorial(etapa);
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
