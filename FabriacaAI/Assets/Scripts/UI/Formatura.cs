using System;
using FabricaDeIA.Nucleo;
using UnityEngine;
using UnityEngine.UI;

namespace FabricaDeIA.UI
{
    /// <summary>
    /// A tela de formatura: o painel de resultados da aula e o botão que baixa o
    /// diploma em PDF.
    ///
    /// Ela mostra na tela o MESMO que o PDF vai conter, e isso não é redundância. O
    /// aluno precisa ver o que está assinando antes de mandar para o professor —
    /// baixar um arquivo às cegas e descobrir depois que o nome saiu errado é o tipo
    /// de atrito que faz metade da turma não entregar.
    ///
    /// O nome é pedido AQUI, no fim, e não no começo. Pedir cadastro antes de jogar
    /// afasta; pedir depois de doze bancadas vencidas é só o passo final de uma
    /// coisa que o aluno já quer levar.
    /// </summary>
    public class Formatura : MonoBehaviour
    {
        RectTransform _raiz;
        InputField _nome;
        InputField _turma;
        Text _recado;
        Button _baixar;
        Action _aoFechar;

        /// <summary>Monta a tela sobre o palco da HUD e devolve o componente.</summary>
        public static Formatura Abrir(RectTransform palco, Action aoFechar)
        {
            var objeto = new GameObject("Formatura", typeof(RectTransform));
            objeto.transform.SetParent(palco, false);

            var tela = objeto.AddComponent<Formatura>();
            tela._aoFechar = aoFechar;
            tela.Construir();
            return tela;
        }

        void Construir()
        {
            _raiz = (RectTransform)transform;
            Widgets.Esticar(_raiz);

            var fundo = Widgets.Painel("Fundo", _raiz, Cores.TintaOpaca);
            Widgets.Esticar(fundo);

            Cabecalho(fundo);
            Tabela(fundo);
            Identidade(fundo);
            Acoes(fundo);
        }

        void Cabecalho(RectTransform pai)
        {
            var p = Progresso.Atual;

            var titulo = Widgets.Texto("Título", pai, 30, TextAnchor.UpperCenter, Cores.Luz);
            Widgets.Faixa(titulo.rectTransform, true, 38f, 20f);
            titulo.text = p.AulaCompleta ? "A máquina está pronta" : "O seu boletim da fábrica";

            var linha = Widgets.Texto("Linha", pai, 16, TextAnchor.UpperCenter, Cores.Neblina);
            Widgets.Faixa(linha.rectTransform, true, 22f, 58f);
            linha.text = p.AulaCompleta
                ? $"doze bancadas · {p.EstrelasTotais} de 36 estrelas · " +
                  $"{Diploma.Duracao(p.SegundosTotais)} de oficina"
                : $"{p.Concluidas} de 12 bancadas · {p.EstrelasTotais} de 36 estrelas";
        }

        /// <summary>
        /// As doze linhas do painel, em duas colunas de seis.
        ///
        /// Duas colunas porque doze linhas empilhadas mais o campo de nome mais os
        /// botões não caberiam numa tela 16:9 sem letra miúda — e letra miúda num
        /// resumo de resultados é o que faz o aluno não ler o próprio boletim.
        /// </summary>
        void Tabela(RectTransform pai)
        {
            const float alturaDaLinha = 30f;
            const float larguraDaColuna = 452f;

            var quadro = Widgets.Painel("Quadro", pai, Color.clear);
            Widgets.Fixar(quadro, new Vector2(0.5f, 1f), new Vector2(0f, -96f),
                          new Vector2(larguraDaColuna * 2f + 24f, alturaDaLinha * 6f));

            for (var i = 1; i <= 12; i++)
            {
                var etapa = $"e{i}";
                var mestre = Elenco.De(etapa);
                var estrelas = Progresso.Atual.Estrelas(etapa);
                var segundos = Progresso.Atual.Segundos(etapa);

                var coluna = (i - 1) / 6;
                var fileira = (i - 1) % 6;

                var linha = Widgets.Painel($"l{i}", quadro,
                                           fileira % 2 == 0 ? Cores.Tinta : Color.clear);
                Widgets.Fixar(linha, new Vector2(0f, 1f),
                              new Vector2(coluna * (larguraDaColuna + 24f) + larguraDaColuna / 2f,
                                          -fileira * alturaDaLinha - alturaDaLinha / 2f),
                              new Vector2(larguraDaColuna, alturaDaLinha - 2f));

                var titulo = Widgets.Texto("t", linha, 15, TextAnchor.MiddleLeft,
                                           estrelas > 0 ? Cores.Papel : Cores.Neblina);
                Widgets.Esticar(titulo.rectTransform);
                titulo.rectTransform.offsetMin = new Vector2(10f, 0f);
                titulo.rectTransform.offsetMax = new Vector2(-190f, 0f);
                titulo.text = $"{i}. {(mestre != null ? mestre.Titulo : etapa)}";

                // Três marcas SEMPRE desenhadas: as ganhas acesas, as demais
                // apagadas. Mostrar só as ganhas faria "uma estrela" e "três"
                // parecerem a mesma linha curta de longe.
                //
                // São retângulos e não caracteres: a fonte embutida do Unity não tem
                // glifo de losango nem de estrela, e o que apareceria na tela do
                // aluno seria uma fileira de quadradinhos vazios.
                for (var e = 0; e < 3; e++)
                {
                    var marca = Widgets.Painel($"e{e}", linha,
                                               e < estrelas ? Cores.Luz : Cores.TintaClara);
                    Widgets.Fixar(marca, new Vector2(1f, 0.5f),
                                  new Vector2(-146f + e * 14f, 0f), new Vector2(9f, 9f));
                }

                var tempo = Widgets.Texto("s", linha, 13, TextAnchor.MiddleRight, Cores.Neblina);
                Widgets.Esticar(tempo.rectTransform);
                tempo.rectTransform.offsetMax = new Vector2(-10f, 0f);
                tempo.text = estrelas > 0 ? Diploma.Duracao(segundos) : "não feita";
            }
        }

        void Identidade(RectTransform pai)
        {
            var aviso = Widgets.Texto("Aviso", pai, 15, TextAnchor.UpperCenter, Cores.Papel);
            Widgets.Fixar(aviso.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -292f),
                          new Vector2(880f, 22f));
            aviso.text = "Escreva o seu nome como o professor vai reconhecer:";

            _nome = Widgets.Campo("Nome", pai, "seu nome completo");
            Widgets.Fixar((RectTransform)_nome.transform, new Vector2(0.5f, 1f),
                          new Vector2(-136f, -326f), new Vector2(560f, 38f));
            _nome.text = Progresso.Atual.nome ?? string.Empty;

            _turma = Widgets.Campo("Turma", pai, "turma", 18, 12);
            Widgets.Fixar((RectTransform)_turma.transform, new Vector2(0.5f, 1f),
                          new Vector2(+282f, -326f), new Vector2(260f, 38f));
            _turma.text = Progresso.Atual.turma ?? string.Empty;

            var nota = Widgets.Texto("Nota", pai, 13, TextAnchor.UpperCenter, Cores.Neblina);
            Widgets.Fixar(nota.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -372f),
                          new Vector2(900f, 20f));
            nota.text = "fica salvo só neste computador — o arquivo baixado é você " +
                        "que manda para o professor";
        }

        void Acoes(RectTransform pai)
        {
            _recado = Widgets.Texto("Recado", pai, 15, TextAnchor.LowerCenter, Cores.Folha);
            Widgets.Faixa(_recado.rectTransform, false, 22f, 72f);

            _baixar = Widgets.Botao("Baixar", pai, "baixar o diploma (PDF)",
                                    Cores.Folha, Cores.Papel, 19);
            Widgets.Fixar((RectTransform)_baixar.transform, new Vector2(0.5f, 0f),
                          new Vector2(-134f, 40f), new Vector2(300f, 50f));
            _baixar.onClick.AddListener(Gerar);

            var voltar = Widgets.Botao("Voltar", pai, "voltar ao ateliê",
                                       Cores.Madeira, Cores.Papel, 17);
            Widgets.Fixar((RectTransform)voltar.transform, new Vector2(0.5f, 0f),
                          new Vector2(+150f, 40f), new Vector2(220f, 50f));
            voltar.onClick.AddListener(Fechar);
        }

        // ------------------------------------------------------------- o arquivo

        void Gerar()
        {
            var nome = _nome.text.Trim();
            if (nome.Length < 3)
            {
                _recado.color = Cores.Brasa;
                _recado.text = "escreva o seu nome antes — é ele que o professor vai procurar";
                return;
            }

            var p = Progresso.Atual;
            p.nome = nome;
            p.turma = _turma.text.Trim();
            p.Salvar();

            try
            {
                var caminho = Baixar.Entregar(Diploma.NomeDoArquivo(p), Diploma.Montar(p));
                _recado.color = Cores.Folha;
                _recado.text = string.IsNullOrEmpty(caminho)
                    ? $"pronto — procure “{Diploma.NomeDoArquivo(p)}” nos seus downloads"
                    : $"gravado em {caminho}";
            }
            catch (Exception e)
            {
                // Um diploma que não sai não pode ser um jogo que trava: a aula
                // acabou, e o aluno precisa poder tentar de novo ou sair.
                Debug.LogError($"diploma não saiu: {e}");
                _recado.color = Cores.Brasa;
                _recado.text = "não deu para gerar o arquivo — chame o professor";
            }
        }

        void Fechar()
        {
            var fim = _aoFechar;
            _aoFechar = null;
            Destroy(gameObject);
            fim?.Invoke();
        }
    }
}
