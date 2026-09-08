using System.Collections.Generic;
using System.Linq;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// O desenho da bancada 1 — a grade, o teclado, a revelação e as telas de
    /// fecho.
    ///
    /// Separado da lógica de rodada pelo mesmo motivo que na bancada 5: o
    /// arquivo tinha passado de seiscentas linhas e as duas metades mudam por
    /// razões diferentes. Regra de jogo muda quando a lição muda; layout muda
    /// quando alguém joga numa tela de outro tamanho. Misturadas, toda mexida
    /// numa obrigava a reler a outra.
    ///
    /// O que fica aqui não decide nada: só pinta o que <see cref="DesafioTermo"/>
    /// já resolveu.
    /// </summary>
    public partial class DesafioTermo
    {
        /// <summary>
        /// O lado da célula, medido no espaço que a grade realmente tem.
        ///
        /// Duas versões erradas antes desta, e a segunda é a que ensina algo.
        ///
        /// A primeira usava 42 fixo: seis fileiras de 42 mais folga dão 288, e
        /// o palco tinha menos que isso, então a última fileira ia parar debaixo
        /// do teclado.
        ///
        /// A segunda calculou o espaço a partir da resolução de referência do
        /// Canvas (960x600) — e continuou transbordando, porque essa resolução
        /// NÃO é o tamanho do palco. Com <c>matchWidthOrHeight</c> em 0,5 o
        /// escalonador mistura largura e altura, e a 1920x1080 a tela mede 1012
        /// por 569 unidades de referência, não 960 por 600. Deduzir o tamanho
        /// da tela a partir da resolução de referência só acerta quando a
        /// proporção bate — e aqui ela nunca bate, porque a mesma aula roda no
        /// projetor 4:3 da sala e no notebook 16:9 do aluno.
        ///
        /// O que funciona em qualquer proporção é não deduzir: perguntar ao
        /// retângulo quanto ele mede. <c>ForceUpdateCanvases</c> resolve o
        /// layout na hora, porque o <c>rect</c> de um objeto recém-criado ainda
        /// vem zerado.
        /// </summary>
        float LadoDaCelula(int colunas)
        {
            Canvas.ForceUpdateCanvases();
            var espaco = _palco.rect;

            var porAltura = (espaco.height - Tentativas * FolgaDaGrade) / Tentativas;
            var porLargura = (espaco.width - colunas * FolgaDaGrade) / colunas;
            return Mathf.Max(16f, Mathf.Floor(Mathf.Min(42f, porAltura, porLargura)));
        }

        static Color Pintura(Cor cor) => cor switch
        {
            Cor.Certa => Cores.Folha,
            Cor.Quase => Cores.Luz,
            Cor.Fora => Cores.TintaClara,
            _ => new Color(0.13f, 0.15f, 0.20f)
        };

        void MontarGrade(int colunas)
        {
            if (_grade != null) Destroy(_grade.gameObject);

            // A faixa da rodada anterior sai JUNTO com a grade dela. As duas são a
            // mesma coisa — o tabuleiro de uma rodada — e separá-las foi o que
            // deixou a resposta revelada sobreviver à rodada que a revelou.
            LimparResposta();

            var grade = Widgets.Painel("Grade", _palco, Color.clear);
            _grade = grade;
            var lado = LadoDaCelula(colunas);
            const float folga = FolgaDaGrade;
            Widgets.Fixar(grade, new Vector2(0.5f, 0.5f), Vector2.zero,
                          new Vector2(colunas * (lado + folga), Tentativas * (lado + folga)));

            _celulas = new Image[Tentativas, colunas];
            _letras = new Text[Tentativas, colunas];

            for (var linha = 0; linha < Tentativas; linha++)
            {
                for (var coluna = 0; coluna < colunas; coluna++)
                {
                    var caixa = Widgets.Painel($"c{linha}_{coluna}", grade, Pintura(Cor.Vazia));
                    Widgets.Fixar(caixa, new Vector2(0.5f, 0.5f),
                        new Vector2((coluna - (colunas - 1) / 2f) * (lado + folga),
                                    ((Tentativas - 1) / 2f - linha) * (lado + folga)),
                        new Vector2(lado, lado));

                    var letra = Widgets.Texto("l", caixa, Mathf.RoundToInt(lado * 0.62f),
                                              TextAnchor.MiddleCenter, Cores.Papel);
                    Widgets.Esticar(letra.rectTransform);

                    _celulas[linha, coluna] = caixa.GetComponent<Image>();
                    _letras[linha, coluna] = letra;
                }
            }
        }

        /// <summary>
        /// O teclado na tela não é para clicar — é o registro do que já se sabe.
        /// Sem ele o aluno repete letra que já saiu cinza e gasta tentativa à
        /// toa, o que faz o desafio parecer injusto em vez de difícil.
        ///
        /// Fica ancorado no pé da ÁREA DE CONTEÚDO. Na primeira versão estava
        /// ancorado no pé da tela, junto com a linha de orientação, e tapava
        /// justamente o texto que dizia o que fazer.
        /// </summary>
        void MontarTeclado()
        {
            if (_teclado != null) Destroy(_teclado.gameObject);

            _teclas.Clear();
            var teclado = Widgets.Painel("Teclado", _area, Color.clear);
            _teclado = teclado;
            Widgets.Faixa(teclado, false, AlturaTeclado);

            string[] fileiras = { "qwertyuiop", "asdfghjkl", "zxcvbnm" };
            const float lado = 29f;
            const float folga = 3f;

            for (var f = 0; f < fileiras.Length; f++)
            {
                var fileira = fileiras[f];
                for (var i = 0; i < fileira.Length; i++)
                {
                    var caixa = Widgets.Painel(fileira[i].ToString(), teclado, Cores.TintaClara);
                    Widgets.Fixar(caixa, new Vector2(0.5f, 0.5f),
                        new Vector2((i - (fileira.Length - 1) / 2f) * (lado + folga),
                                    (1 - f) * (lado + folga)),
                        new Vector2(lado, lado));

                    var letra = Widgets.Texto("l", caixa, 15, TextAnchor.MiddleCenter, Cores.Neblina);
                    Widgets.Esticar(letra.rectTransform);
                    letra.text = fileira[i].ToString().ToUpperInvariant();

                    _teclas[fileira[i]] = caixa.GetComponent<Image>();
                }
            }
        }

        void RedesenharLinhaAtual()
        {
            var linha = _palpites.Count;
            if (linha >= Tentativas) return;
            for (var i = 0; i < _letras.GetLength(1); i++)
            {
                _letras[linha, i].text = i < _digitando.Length
                    ? _digitando[i].ToString().ToUpperInvariant()
                    : string.Empty;
            }
        }

        /// <summary>
        /// Carimba a palavra certa por cima da grade.
        ///
        /// Em cima da grade, e não embaixo dela, por um motivo prático: embaixo
        /// fica o teclado, e uma faixa nova ali teria que adivinhar a altura dele.
        /// Sobre a grade a revelação não depende de nada em volta — e as linhas
        /// que ela cobre são palpites já gastos, num momento em que a rodada
        /// acabou.
        /// </summary>
        void RevelarResposta(string resposta)
        {
            if (_grade == null) return;
            LimparResposta();

            var lado = LadoDaCelula(resposta.Length);
            var faixa = Widgets.Painel("Resposta", _palco, Cores.TintaOpaca);
            _resposta = faixa;
            Widgets.Fixar(faixa, new Vector2(0.5f, 0.5f), Vector2.zero,
                          new Vector2(_grade.sizeDelta.x + 20f, lado + 20f));
            faixa.SetAsLastSibling();

            var texto = Widgets.Texto("Palavra", faixa, Mathf.RoundToInt(lado * 0.6f),
                                      TextAnchor.MiddleCenter, Cores.Brasa);
            Widgets.Esticar(texto.rectTransform, 6f);
            // Espaçada como as células da grade: é a mesma coisa que ele tentou
            // escrever seis vezes, e a forma tem que ser reconhecível.
            texto.text = string.Join(" ", resposta.ToUpperInvariant().ToCharArray());
            Widgets.UmaLinha(texto);

            Widgets.Surgir(faixa, 0.18f, 0.9f);
            Widgets.Lampejo(faixa, Cores.Brasa, 0.4f, 0.45f);
        }

        /// <summary>
        /// Tira a faixa da resposta revelada, se houver uma.
        ///
        /// Chamada nos três lugares em que o tabuleiro deixa de valer: ao montar a
        /// grade da rodada seguinte, ao revelar outra resposta, e ao abrir o
        /// placar. O último importa: a última rodada perdida não passa por
        /// MontarGrade nenhuma, e sem esta chamada a palavra ficaria por baixo do
        /// placar até o aluno sair da bancada.
        /// </summary>
        void LimparResposta()
        {
            if (_resposta == null) return;
            Destroy(_resposta.gameObject);
            _resposta = null;
        }

        void MarcarTecla(char letra, Cor cor)
        {
            if (!_teclas.TryGetValue(letra, out var tecla)) return;
            // Uma tecla nunca volta atrás: verde não vira amarelo depois. O
            // teclado é memória do que já se sabe, e memória que piora mente.
            var atual = tecla.color;
            if (atual == Cores.Folha) return;
            if (atual == Cores.Luz && cor != Cor.Certa) return;
            tecla.color = Pintura(cor);
        }

        void MostrarVirada()
        {
            _rodadaEncerrada = true;

            var aviso = Widgets.Painel("Virada", _area, Cores.TintaOpaca);
            Widgets.Esticar(aviso);

            // Entrada em cena, sem lampejo: a virada é um aviso, não um veredito,
            // e um estouro dourado aqui prometeria uma vitória que não houve.
            Widgets.Surgir(aviso, 0.16f, 0.97f);

            var texto = Widgets.Texto("Texto", aviso, 23, TextAnchor.MiddleCenter, Cores.Luz);
            Widgets.Fixar(texto.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 24f),
                          new Vector2(680f, 120f));
            texto.text = "Agora Tico te dá uma pista.\n\n" +
                         "As próximas três palavras vêm no fim de uma frase.";

            var dica = Widgets.Texto("Dica", aviso, 15, TextAnchor.MiddleCenter, Cores.Neblina);
            Widgets.Fixar(dica.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -70f),
                          new Vector2(620f, 24f));
            dica.text = "repare em quantas tentativas você vai precisar";

            Painel.Instruir("a regra mudou");
            Painel.Acao("continuar", () =>
            {
                Destroy(aviso.gameObject);
                Painel.Acao(null, null);
                IniciarRodada();
            });
        }

        /// <summary>
        /// A conta que a etapa inteira estava montando.
        ///
        /// Aparece só aqui, no fim, e com os números do próprio aluno. Dizer
        /// "contexto ajuda" no começo seria informação; mostrar que ELE gastou
        /// 4,7 tentativas às cegas e 2,3 com a frase é constatação — e o que a
        /// turma leva para casa é a constatação.
        /// </summary>
        void MostrarPlacar()
        {
            _mostrandoPlacar = true;
            LimparResposta();

            var painel = Widgets.Painel("Placar", _area, Cores.TintaOpaca);
            Widgets.Esticar(painel);

            // As outras onze bancadas ganham isto de graça, porque fecham rodada
            // pelo MostrarCartaz de DesafioEmNiveis. Termo não herda daquela base
            // — tem estrutura de rodadas própria —, e por isso era a única tela de
            // fecho do jogo que ainda aparecia pronta, sem peso nenhum. O aluno
            // via o placar final da PRIMEIRA bancada da aula surgir como um
            // estalo, e só descobria que fechar rodada era um momento na segunda.
            Widgets.Surgir(painel, 0.16f, 0.97f);
            Widgets.Lampejo(painel, Cores.Luz, 0.45f, 0.55f);

            var titulo = Widgets.Texto("Título", painel, 25, TextAnchor.UpperCenter, Cores.Luz);
            Widgets.Faixa(titulo.rectTransform, true, 32f, 16f);
            titulo.text = $"Você acertou {_acertos} de {_rodadas.Count}";

            var semPista = Media(0, Rodadas.SemContexto);
            var comPista = Media(Rodadas.SemContexto, _custos.Count);

            var numeros = Widgets.Texto("Números", painel, 21, TextAnchor.MiddleCenter, Cores.Papel);
            Widgets.Fixar(numeros.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 66f),
                          new Vector2(700f, 80f));
            numeros.text =
                $"sem pista nenhuma:   {semPista:0.0} tentativas por palavra\n" +
                $"com a frase antes:   {comPista:0.0} tentativas por palavra";

            var licao = Widgets.Texto("Lição", painel, 18, TextAnchor.MiddleCenter, Cores.Luz);
            Widgets.Fixar(licao.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -46f),
                          new Vector2(720f, 110f));
            licao.text = comPista < semPista
                ? "A frase não te deu a resposta — te deu o contexto, e com\n" +
                  "ele você precisou de menos chutes.\n\n" +
                  "É exatamente isso que a máquina faz, palavra por palavra.\n"
                : "Desta vez a frase não ajudou muito — mas repare: ela encolhe\n" +
                  "o monte de palavras possíveis.\n\n" +
                  "É nesse monte menor que a máquina procura, o tempo todo.";

            if (_escaparam.Count > 0)
            {
                var escapou = Widgets.Texto("Escaparam", painel, 16,
                                            TextAnchor.MiddleCenter, Cores.Brasa);
                Widgets.Fixar(escapou.rectTransform, new Vector2(0.5f, 0.5f),
                              new Vector2(0f, -132f), new Vector2(720f, 40f));
                escapou.text = (_escaparam.Count == 1 ? "escapou: " : "escaparam: ") +
                               string.Join(",  ", _escaparam);
                Widgets.UmaLinha(escapou);
            }

            Painel.Instruir("o que os seus números dizem");
            Painel.MarcarPasso(string.Empty);
            Painel.Acao("voltar ao ateliê", () => Encerrar(Estrelas()));
        }
    }
}
