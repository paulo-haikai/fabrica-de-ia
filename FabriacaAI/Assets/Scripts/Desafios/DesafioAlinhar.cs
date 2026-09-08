using System.Collections;
using System.Collections.Generic;
using FabricaDeIA.Engine;
using FabricaDeIA.Nucleo;
using FabricaDeIA.UI;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// Bancada 11 — o guichê do Sereno.
    ///
    /// Inspiração: PAPERS, PLEASE, agora de verdade. Uma pessoa por vez no balcão,
    /// duas folhas para conferir, um manual que muda todo dia, uma cota, um
    /// relógio, e um carimbo em cada mão.
    ///
    /// A versão anterior CITAVA o Papers, Please e não era: seis pares de
    /// respostas, o aluno clicava na que preferia, e no fim a máquina falava com o
    /// "temperamento" dele. Ensinava RLHF por gosto pessoal, e fechava noventa
    /// minutos de aula perguntando se o aluno prefere resposta curta ou longa.
    ///
    /// A lição de aprendizado por preferência humana NÃO se perdeu — ela é o motor
    /// desta aqui. O que mudou foi o que está em jogo quando as escolhas viram
    /// regra. Não é mais o jeito de falar dela. É quem entra.
    ///
    /// CINCO NÍVEIS, E OS TRÊS PRIMEIROS SÃO OUTRO JOGO:
    ///
    ///   · Dias 1 a 3 — O GUICHÊ. O aluno defere e indefere gente pedindo creche,
    ///     passe, consulta. O relógio aperta a cada dia até não caber mais ler as
    ///     duas folhas de todo mundo. Ver DesafioAlinhar.Manual.cs.
    ///   · Nível 4 — A VIRADA. O balcão é automatizado, e o modelo é treinado NA
    ///     FRENTE DELE, com o log que ele acabou de produzir.
    ///   · Nível 5 — OS ANOS. Escala, ausência de recurso, e a alça de retorno.
    ///     Ver DesafioAlinhar.Maquina.cs.
    ///
    /// ONDE O VIÉS ENTRA: em lugar nenhum deste arquivo. Ele nasce de uma frase
    /// óbvia em DesafioAlinhar.Fila.cs — a espera na fila não pode ser maior que o
    /// tempo na cidade — e é o relógio que obriga o aluno a descobri-lo sozinho.
    /// Nenhum cartaz sugere o atalho. Ele funciona, e é por isso que é usado.
    ///
    /// O FECHO CONTINUA INCÔMODO, e é a razão de a bancada ser a última. Era "quem
    /// escolheu, e o que essa pessoa preferia". Agora é: quem escolheu foi você, e
    /// você não sabia que estava escolhendo isso.
    ///
    /// O QUE ENSINA O GESTO AQUI É O MEMORANDO do primeiro dia — que já estava na
    /// tela por outro motivo, porque é assim que um expediente começa. Ele diz
    /// onde fica o manual e que cada pessoa traz duas folhas, e não diz mais nada.
    ///
    /// E não dizer mais é de propósito: explicar o gesto exigiria apontar para as
    /// folhas, e apontar para a folha de baixo é entregar a bancada. O aluno
    /// precisa descobrir sozinho qual das duas importa — e descobrir, sob
    /// relógio, que dá para não olhar.
    /// </summary>
    public partial class DesafioAlinhar : DesafioEmNiveis
    {
        public override string Etapa => "e11";
        public override string Titulo => "Quem ela deixa passar";

        /// <summary>
        /// Três dias de expediente e o epílogo. Eram cinco níveis: o Ato II tinha
        /// quatro telas de texto entre o último carimbo e a cena final.
        ///
        /// ELAS FORAM CORTADAS, e o que elas diziam a cena passou a dizer sozinha.
        /// Quem acabou de carimbar trinta pessoas sob relógio não vai ler quatro
        /// cartazes — e a única coisa que aquelas telas tinham de insubstituível, a
        /// REGRA QUE A MÁQUINA APRENDEU, agora é uma legenda dentro do epílogo, dita
        /// pela própria máquina. Ver DesafioAlinhar.Epilogo.cs.
        /// </summary>
        protected override int Niveis => Dias.Length + 1;

        /// <summary>Uma decisão do aluno, do jeito que a máquina vai receber.</summary>
        readonly struct Registro
        {
            public readonly Requerente Quem;
            public readonly bool Deferiu;
            public Registro(Requerente quem, bool deferiu) { Quem = quem; Deferiu = deferiu; }
        }

        /// <summary>
        /// TUDO o que o aluno decidiu, nos três dias. É o conjunto de treino da
        /// máquina, e não há outro: o Ato II não tem uma linha de resultado
        /// escrita à mão, ele ajusta um modelo a estes registros e diz o que saiu.
        /// </summary>
        readonly List<Registro> _log = new();

        Mulberry32 _sorteio;
        RectTransform _mesa;

        int _dia;
        List<Requerente> _fila;
        int _indice;
        float _relogio;
        bool _expediente;

        /// <summary>
        /// A fila está andando: alguém está saindo, ou alguém está chegando.
        ///
        /// Existe por dois motivos, e o segundo é o que importa. O primeiro é
        /// óbvio: carimbar durante o fade carimbaria a pessoa errada. O segundo é
        /// que O RELÓGIO PARA AQUI — a troca de pessoa não come tempo do
        /// expediente. A pressa da bancada é calibrada em segundos de LEITURA, e
        /// descontar meio segundo por atendimento mudaria a dificuldade dos três
        /// dias sem ninguém ter decidido mudá-la.
        /// </summary>
        bool _trocando;

        int _certos;
        int _erros;
        int _recemIndeferidos;
        int _diasVencidos;

        Requerente Atual => _fila != null && _indice < _fila.Count ? _fila[_indice] : null;

        protected override void Preparar()
        {
            _sorteio = new Mulberry32((uint)Rodadas.Semente());
        }

        protected override void MontarNivel()
        {
            _mesa = Widgets.Painel("Mesa", Area, Color.clear);
            Widgets.Esticar(_mesa);

            _dia = NivelAtual;

            if (NivelAtual >= Dias.Length) { MontarAnos(); return; }

            AbrirDia();
        }

        /// <summary>
        /// Uma estrela por dia de expediente vencido — a virada e os anos não dão
        /// nenhuma, porque são demonstração, e estrela de demonstração é estrela
        /// por sorte. Três dias vencidos fecham a aula com as três, que é o que a
        /// última bancada do ateliê deve poder dar.
        /// </summary>
        protected override int Estrelas() => Mathf.Clamp(_diasVencidos, 0, 3);

        // ------------------------------------------------------------- o dia

        void AbrirDia()
        {
            var d = Dias[_dia];
            _fila = SortearFila(d.Casos, d.ComDiscrepancia, d.Empresas);
            _indice = 0;
            _certos = 0;
            _erros = 0;
            _recemIndeferidos = 0;
            _relogio = d.Segundos;
            _expediente = false;
            _trocando = false;

            // O memorando ANTES do relógio, sempre. É onde o manual muda e onde a
            // assinatura muda — e ler quem assinou é a única forma de o aluno
            // perceber para quem ele trabalha.
            MostrarMemorando(Memorando(_dia), ComecarExpediente);
        }

        void ComecarExpediente()
        {
            _expediente = true;
            MontarGuiche();
            Chamar();
        }

        /// <summary>
        /// Chama a próxima pessoa da fila — e ela CHEGA, não aparece pronta.
        /// </summary>
        void Chamar()
        {
            if (Atual == null) { FecharDia(); return; }
            StartCoroutine(Chegando());
        }

        /// <summary>
        /// A entrada de quem foi chamado: a mesa é preenchida com o vão ainda
        /// apagado, e só então tudo o que é dessa pessoa aparece junto.
        ///
        /// Apagar ANTES de desenhar é o que garante que o fade não mostre o
        /// requerente anterior por um quadro — o erro que faz a troca parecer um
        /// piscar de tela em vez de uma fila andando.
        /// </summary>
        IEnumerator Chegando()
        {
            _trocando = true;
            Presenca(0f);
            DesenharRequerente(Atual);
            AtualizarPlacar();
            yield return Entrando();
            _trocando = false;
        }

        /// <summary>
        /// O carimbo. É o único gesto da bancada, e ele é irreversível de
        /// propósito: no Papers, Please o carimbo desce e a pessoa vai embora.
        /// Poder desfazer transformaria a pressa em inconveniente, e é a pressa
        /// que a bancada precisa que doa.
        /// </summary>
        void Decidir(bool deferir)
        {
            if (!_expediente || _trocando || Atual == null) return;

            var r = Atual;
            _log.Add(new Registro(r, deferir));

            if (r.Recem && !deferir) _recemIndeferidos++;

            // "Certo" aqui é certo SEGUNDO O CONFERIDOR — e a partir do dia 3 o
            // conferidor é da empresa. Ver ContaErro: indeferir recém-chegado
            // deixa de contar erro, mesmo quando o manual mandava deferir.
            var errou = ContaErro(r, deferir);
            if (errou) _erros++;
            else _certos++;

            Carimbar(deferir, errou);
            StartCoroutine(Despachando());
        }

        /// <summary>
        /// O que acontece depois que o carimbo desce: a tinta fica um instante na
        /// tela, a pessoa vai embora, e só ENTÃO a fila anda.
        ///
        /// O avanço do índice mora aqui, no meio do fade, e não no clique. É o que
        /// impede a folha da pessoa seguinte de aparecer por baixo da tinta da
        /// anterior — e é por isso que o dia só fecha depois que esta corrotina
        /// termina.
        /// </summary>
        IEnumerator Despachando()
        {
            _trocando = true;
            yield return new WaitForSecondsRealtime(TintaNaTela);
            yield return Saindo();
            if (!Aberto) yield break;

            _indice++;
            _trocando = false;

            if (_erros > Dias[_dia].Advertencias) { FecharDia(); yield break; }
            Chamar();
        }

        void Update()
        {
            // O epílogo tem o próprio relógio e roda antes de tudo: ele acontece
            // depois que o expediente acabou, e é uma função do tempo para a tela.
            // Ver DesafioAlinhar.Epilogo.cs.
            if (_cenaRodando) { RodarCena(); return; }

            // O relógio para enquanto a fila anda. Ver _trocando.
            if (!_expediente || !Aberto || _trocando) return;

            _relogio -= Time.deltaTime;
            AtualizarRelogio(Mathf.Max(0f, _relogio));

            if (_relogio <= 0f)
            {
                _expediente = false;
                FecharDia();
            }
        }

        void FecharDia()
        {
            if (!_expediente && _fila == null) return;
            _expediente = false;
            _trocando = false;

            var d = Dias[_dia];
            var venceu = _certos >= d.Meta;
            if (venceu) _diasVencidos++;

            // O ÚLTIMO DIA NÃO TEM CARTAZ. Terminou a terceira leva, a tela corta
            // para o epílogo — sem bilhete, sem "continuar", sem nada entre o
            // último carimbo e o que ele produziu. O corte seco é o efeito: o
            // expediente acaba e o aluno já está quatro anos depois.
            if (_dia >= Dias.Length - 1)
            {
                SaltarPara(Dias.Length);
                return;
            }

            var bilhete = FimDoDia(_dia, _certos, _fila.Count, _recemIndeferidos);

            if (venceu)
            {
                Resolveu($"Dia {_dia + 1} encerrado", bilhete);
                return;
            }

            // Perder um dia NÃO tira a lição, e por isso conta estrela: quem
            // atendeu a fila inteira leu o manual e carimbou, que é o gesto que a
            // bancada ensina. O que ele não fez foi bater a cota de um contrato
            // que não é dele.
            Falhou($"Dia {_dia + 1} encerrado", bilhete +
                   "\n\nA cota do dia não foi batida.", contaEstrela: true);
        }

        // ------------------------------------------------------- o que fica

        /// <summary>
        /// Guarda a regra que a máquina aprendeu, para o resto do ateliê saber o
        /// que aconteceu aqui. O campo já existia e não era lido por ninguém — era
        /// a "persona" da versão antiga.
        /// </summary>
        void GuardarRegra(int campo, int corte, float acerto)
        {
            Progresso.Atual.preferencias = new float[] { campo, corte, acerto };
            Progresso.Atual.Salvar();
        }
    }
}
