using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Base dos minigames que acontecem em rodadas.
    ///
    /// Onze das doze bancadas têm a mesma espinha: sorteia N níveis, joga um por
    /// vez, ganha uma estrela por nível resolvido, e sair no meio guarda o que já
    /// valeu. Escrever isso onze vezes garantiria onze versões ligeiramente
    /// diferentes da mesma contagem de estrelas — e a que estivesse errada só
    /// apareceria na aula.
    ///
    /// A subclasse cuida de duas coisas: dizer quantos níveis tem
    /// (<see cref="Niveis"/>) e montar o nível da vez
    /// (<see cref="MontarNivel"/>). Quando o aluno resolve, ela chama
    /// <see cref="Resolveu"/>; quando quer pular, <see cref="Falhou"/>.
    /// </summary>
    public abstract class DesafioEmNiveis : Desafio
    {
        /// <summary>Quantos níveis esta bancada joga por visita.</summary>
        protected abstract int Niveis { get; }

        /// <summary>O nível em jogo, de 0 a <see cref="Niveis"/> - 1.</summary>
        protected int NivelAtual { get; private set; }

        /// <summary>Quantos níveis o aluno resolveu.</summary>
        protected int Resolvidos { get; private set; }

        /// <summary>A área de conteúdo, já limpa para o nível da vez.</summary>
        protected RectTransform Area { get; private set; }

        protected sealed override void Montar(RectTransform area)
        {
            Area = area;
            Preparar();
            AbrirNivel();
        }

        /// <summary>
        /// Roda uma vez, antes do primeiro nível. É onde a subclasse sorteia o
        /// material da aula — que precisa ser sorteado UMA vez, e não a cada
        /// nível, senão o aluno pode ver a mesma coisa duas vezes.
        /// </summary>
        protected virtual void Preparar() { }

        /// <summary>Desenha o nível da vez dentro de <see cref="Area"/>.</summary>
        protected abstract void MontarNivel();

        /// <summary>
        /// Remonta o nível atual do zero.
        ///
        /// Serve para apagar o que a demonstração do tutorial deixou na mesa: o
        /// aluno tem que jogar a rodada dele, não terminar a que a máquina
        /// começou na frente dele.
        ///
        /// Só pode ser chamado DEPOIS que a explicação acabou — ela vive dentro
        /// de <see cref="Area"/>, e limpar a área com o tutorial no ar o
        /// destruiria no meio de um passo.
        /// </summary>
        protected void RecomecarNivel() => AbrirNivel();

        void AbrirNivel()
        {
            // Limpar antes de montar: o nível anterior não pode deixar sobra na
            // tela, e delegar essa faxina a onze subclasses é pedir que uma
            // delas esqueça.
            foreach (Transform filho in Area) Destroy(filho.gameObject);

            if (Niveis > 1) Painel.MarcarPasso(NivelAtual + 1, Niveis);
            else Painel.MarcarPasso(string.Empty);

            Painel.Acao(null, null);
            MontarNivel();

            // A rodada nova entra em cena em vez de estalar pronta. Dois décimos
            // de segundo, num lugar só, dão às onze bancadas a sensação de
            // COMEÇAR uma rodada — sem isso, a troca de nível parece a mesma
            // tela piscando com outro conteúdo.
            Widgets.Surgir(Area);
        }

        /// <summary>
        /// O aluno resolveu o nível. Mostra o recado e oferece o próximo.
        ///
        /// O recado didático vem SEMPRE aqui, depois da vitória, nunca antes: é
        /// o único momento em que o aluno tem o que a frase explica.
        /// </summary>
        protected void Resolveu(string titulo, string licao)
        {
            Resolvidos++;
            MostrarCartaz(titulo, licao, Cores.Luz, true);
        }

        /// <summary>
        /// O aluno não resolveu, mas a rodada acabou. Vale para as bancadas em
        /// que falhar É a lição — a tabela que não cabe, o erro que não se ajusta
        /// à mão — e nesses casos o cartaz explica por que era impossível.
        /// </summary>
        protected void Falhou(string titulo, string licao, bool contaEstrela = false)
        {
            if (contaEstrela) Resolvidos++;
            MostrarCartaz(titulo, licao, Cores.Brasa, false);
        }

        /// <summary>
        /// O fim de rodada — o mesmo em onze das doze bancadas.
        ///
        /// Era um retângulo de texto que aparecia pronto: o instante de maior
        /// carga do minigame (ganhou? perdeu?) chegava sem nenhum peso, e a
        /// resposta vinha em forma de PARÁGRAFO, que se lê devagar, quando o
        /// aluno já queria saber de relance.
        ///
        /// Agora o cartaz entra em cena e a cor chega ANTES do texto: o
        /// estouro dourado da vitória e o tremor vermelho da derrota respondem
        /// à pergunta em um quadro; a frase, que ainda importa, fica para quem
        /// já sabe o resultado e quer o porquê.
        ///
        /// Por ser um método só, usado por onze bancadas, esta é a mudança de
        /// maior alcance por linha escrita do jogo inteiro — a sensação de
        /// fechar QUALQUER rodada muda aqui.
        /// </summary>
        void MostrarCartaz(string titulo, string licao, Color cor, bool venceu)
        {
            var cartaz = Widgets.Painel("Cartaz", Area, Cores.TintaOpaca);
            Widgets.Esticar(cartaz);

            var cabeca = Widgets.Texto("Título", cartaz, 23, TextAnchor.UpperCenter, cor);
            Widgets.Faixa(cabeca.rectTransform, true, 30f, 24f);
            cabeca.text = titulo;

            var corpo = Widgets.Texto("Corpo", cartaz, 17, TextAnchor.UpperCenter, Cores.Papel);
            Widgets.Esticar(corpo.rectTransform);
            corpo.rectTransform.offsetMin = new Vector2(60f, 40f);
            corpo.rectTransform.offsetMax = new Vector2(-60f, -72f);
            corpo.text = licao;

            Widgets.Surgir(cartaz, 0.16f, 0.97f);

            // A vitória estoura mais forte e por mais tempo; a derrota é um
            // baque curto, e treme. Derrota longa vira sermão.
            Widgets.Lampejo(cartaz, cor, venceu ? 0.45f : 0.3f, venceu ? 0.55f : 0.45f);
            if (!venceu) Widgets.Tremer(cartaz, 9f, 0.22f);

            var ultimo = NivelAtual + 1 >= Niveis;
            Painel.Rodape(ultimo ? "a peça está pronta" : "ainda falta uma rodada");
            Painel.Acao(ultimo ? "voltar ao ateliê" : "continuar", Proximo);
        }

        void Proximo()
        {
            NivelAtual++;
            if (NivelAtual >= Niveis)
            {
                Encerrar(Estrelas());
                return;
            }
            AbrirNivel();
        }

        /// <summary>
        /// Uma estrela por nível resolvido, até três. Bancada de dois níveis dá
        /// no máximo duas — é honesto: rodada mais curta vale menos.
        /// </summary>
        protected virtual int Estrelas() => Mathf.Clamp(Resolvidos, 0, 3);

        /// <summary>Sair guarda o que já foi conquistado.</summary>
        protected override void Desistir() => Encerrar(Estrelas());
    }
}
