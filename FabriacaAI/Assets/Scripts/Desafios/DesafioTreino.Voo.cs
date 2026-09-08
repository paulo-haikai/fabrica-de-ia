using System.Collections;
using System.Collections.Generic;
using FabricaDeIA.Engine;
using FabricaDeIA.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// O gesto e o voo da bancada 7: puxar o elástico, soltar, ver o tiro.
    ///
    /// O puxão é o do Angry Birds e nada mais: pega no pássaro, arrasta para
    /// trás, solta. O que ele regula é que mudou — em vez da força do tiro, ele
    /// regula o tamanho da correção que a máquina vai fazer depois de errar. A
    /// régua embaixo mostra o número, e a marca da tentativa anterior fica
    /// plantada nela.
    ///
    /// Essa marca não é enfeite. Sem ela a regulagem contínua seria impossível
    /// de calibrar: o aluno sentiria que "foi demais" e não teria como repetir
    /// um pouco menos. É a linha pontilhada do tiro anterior, do jogo original,
    /// no lugar onde esta bancada de fato mira.
    /// </summary>
    public partial class DesafioTreino
    {
        /// <summary>Quanto tempo um tiro leva na tela. O treino inteiro cabe em segundos.</summary>
        const float TempoDeVoo = 0.55f;

        // ------------------------------------------------------------- o puxão

        void MontarPuxador()
        {
            // Um véu transparente por cima de tudo, só para pegar o arraste. O
            // pássaro é pequeno demais para ser o alvo do dedo, e um alvo de
            // dezesseis pixels seria a bancada inteira travada num detalhe de
            // mira que não é o assunto dela.
            _puxador = Widgets.Painel("Puxador", Area, new Color(0f, 0f, 0f, 0f));
            Widgets.Esticar(_puxador);
            _puxador.SetAsLastSibling();

            var gatilho = _puxador.gameObject.AddComponent<EventTrigger>();
            Escutar(gatilho, EventTriggerType.PointerDown, d => Puxar(PontoNaPista(d)));
            Escutar(gatilho, EventTriggerType.Drag, d => Mirar(PontoNaPista(d)));
            Escutar(gatilho, EventTriggerType.PointerUp, _ => Soltar());
            Escutar(gatilho, EventTriggerType.EndDrag, _ => Soltar());
        }

        static void Escutar(EventTrigger gatilho, EventTriggerType tipo,
                            System.Action<PointerEventData> acao)
        {
            var entrada = new EventTrigger.Entry { eventID = tipo };
            entrada.callback.AddListener(dados => acao((PointerEventData)dados));
            gatilho.triggers.Add(entrada);
        }

        /// <summary>
        /// Onde o ponteiro está, na régua da PRIMEIRA pista — a mesma de
        /// <see cref="Tela"/>, com origem no canto de baixo à esquerda.
        /// </summary>
        Vector2 PontoNaPista(PointerEventData dados)
        {
            var pista = _maquinas[0].Pista;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                pista, dados.position, dados.pressEventCamera, out var local);
            return local - pista.rect.min;
        }

        Vector2 BocaNaTela(Maquina m) =>
            Tela(m, new Vector2(Estilingue.BocaX, Estilingue.BocaY));

        /// <summary>
        /// Estica o elástico e leva o pássaro junto, em todas as pistas.
        ///
        /// Todas, e não só a que recebe o dedo: na rodada 3 a mesma regulagem
        /// vale para as duas máquinas, e ver os dois elásticos esticarem juntos
        /// é o que diz isso sem escrever isso.
        /// </summary>
        void DesenharPuxada()
        {
            var direcao = Mira
                ? new Vector2(-Mathf.Cos(_mira), -Mathf.Sin(_mira))
                : Vector2.left;
            // O pássaro anda MENOS que o dedo (dois quintos do puxão). O dedo
            // precisa de curso para a régua ter resolução; o pássaro precisa
            // caber na pista — a boca do estilingue fica a uns oitenta pixels
            // da borda esquerda, e um recuo de tamanho igual ao do dedo o
            // jogaria para fora do recorte.
            var recuo = direcao * (_fracao * PuxadaMaxima * 0.4f);

            foreach (var m in _maquinas)
            {
                var boca = BocaNaTela(m);
                m.Passaro.gameObject.SetActive(true);
                m.Passaro.localRotation = Quaternion.identity;
                m.Passaro.anchoredPosition = boca + recuo;

                var esticado = _fracao > 0.001f;
                m.ElasticoA.gameObject.SetActive(esticado);
                m.ElasticoB.gameObject.SetActive(esticado);
                if (!esticado) continue;

                PorFio(m.ElasticoA, boca + new Vector2(-9f, 2f), boca + recuo);
                PorFio(m.ElasticoB, boca + new Vector2(9f, 2f), boca + recuo);
            }

            AtualizarRegua();
        }

        void SoltarElastico()
        {
            foreach (var m in _maquinas)
            {
                m.ElasticoA.gameObject.SetActive(false);
                m.ElasticoB.gameObject.SetActive(false);
            }
        }

        // -------------------------------------------------------------- régua

        const float LarguraDaRegua = 380f;

        void MontarRegua()
        {
            _reguaMiolo = Widgets.Barra("Régua", Area, Vector2.zero,
                                        new Vector2(LarguraDaRegua, 20f),
                                        Cores.TintaClara, Cores.Luz);
            _reguaTrilho = (RectTransform)_reguaMiolo.parent;
            Widgets.Fixar(_reguaTrilho, new Vector2(0.5f, 0f), new Vector2(0f, 48f),
                          new Vector2(LarguraDaRegua, 20f));
            Widgets.Encher(_reguaMiolo, 0f, LarguraDaRegua);

            _reguaAnterior = Widgets.Painel("Anterior", _reguaTrilho, Cores.Papel);
            Widgets.Fixar(_reguaAnterior, new Vector2(0f, 0.5f), Vector2.zero,
                          new Vector2(2f, 30f));
            _reguaAnterior.gameObject.SetActive(false);

            _leitura = Widgets.Texto("Leitura", Area, 14, TextAnchor.LowerCenter, Cores.Neblina);
            Widgets.Faixa(_leitura.rectTransform, false, 18f, 20f);
        }

        void MarcarAnterior(float fracao)
        {
            _reguaAnterior.gameObject.SetActive(true);
            _reguaAnterior.anchoredPosition = new Vector2(fracao * LarguraDaRegua, 0f);
        }

        void AtualizarRegua()
        {
            if (_reguaMiolo == null) return;
            Widgets.Encher(_reguaMiolo, _fracao, LarguraDaRegua);

            var correcao = Correcao(_fracao).ToString("0.00");
            _leitura.text = Mira
                ? $"correção {correcao}   ·   mira {(_mira * Mathf.Rad2Deg):0}°"
                : $"correção {correcao}";
        }

        // --------------------------------------------------------------- o voo

        void LimparRastros()
        {
            foreach (var m in _maquinas)
            {
                foreach (Transform rastro in m.Rastros) Destroy(rastro.gameObject);
                m.Porco.gameObject.SetActive(true);
                for (var i = 0; i < m.Caixas.Length; i++)
                {
                    m.Caixas[i].gameObject.SetActive(true);
                    m.Caixas[i].localRotation = Quaternion.identity;
                }
            }
        }

        /// <summary>
        /// Anima um tiro pelo caminho que a física calculou — nem um pixel
        /// inventado — e deixa o rastro dele plantado na pista.
        /// </summary>
        IEnumerator VoarNaTela(Maquina m, List<Vector2> caminho,
                               Estilingue.Impacto impacto, int tiro)
        {
            SoltarElastico();
            m.Passaro.gameObject.SetActive(true);

            var ultimo = caminho.Count - 1;
            var espacamento = Mathf.Max(3, caminho.Count / 20);
            var proximo = 0;

            for (var t = 0f; t < TempoDeVoo; t += Time.unscaledDeltaTime)
            {
                var i = Mathf.Min(ultimo, Mathf.FloorToInt(t / TempoDeVoo * ultimo));
                var p = Tela(m, caminho[i]);
                m.Passaro.anchoredPosition = p;
                m.Passaro.localRotation = Quaternion.Euler(0f, 0f, -t * 900f);

                if (i >= proximo)
                {
                    Rastro(m, p, tiro);
                    proximo = i + espacamento;
                }
                yield return null;
            }

            m.Passaro.anchoredPosition = Tela(m, caminho[ultimo]);
            yield return Bater(m, impacto);

            m.Passaro.gameObject.SetActive(false);
            m.Passaro.localRotation = Quaternion.identity;
            m.Passaro.anchoredPosition = BocaNaTela(m);
        }

        /// <summary>
        /// Um pontinho do rastro. Os tiros mais novos são mais fortes, para o
        /// olho ler a ORDEM das parábolas empilhadas sem precisar de número.
        /// </summary>
        void Rastro(Maquina m, Vector2 onde, int tiro)
        {
            var forca = Mathf.Lerp(0.22f, 0.75f, tiro / Mathf.Max(1f, _r.Tiros - 1f));
            var cor = new Color(Cores.Papel.r, Cores.Papel.g, Cores.Papel.b, forca);
            Bola("p", m.Rastros, cor, onde, 4.5f);
        }

        IEnumerator Bater(Maquina m, Estilingue.Impacto impacto)
        {
            if (impacto.Sumiu)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                yield break;
            }

            var onde = Tela(m, impacto.Onde);

            if (impacto.Porco)
            {
                Widgets.Pulsar(m.Porco, 1.7f, 0.16f);
                yield return new WaitForSecondsRealtime(0.16f);
                m.Porco.gameObject.SetActive(false);
                Estouro(m, Tela(m, new Vector2(m.PorcoX, Estilingue.RaioDoPorco)), Cores.Folha, 10);
                Widgets.Lampejo(m.Pista, Cores.Luz, 0.3f, 0.4f);
                yield return new WaitForSecondsRealtime(0.28f);
                yield break;
            }

            if (impacto.Bloco >= 0 && impacto.Bloco < m.Caixas.Length)
            {
                var caixa = m.Caixas[impacto.Bloco];
                Widgets.Tremer(caixa, 6f, 0.16f);
                caixa.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-24f, 24f));
            }

            Estouro(m, onde, Cores.Neblina, 6);
            yield return new WaitForSecondsRealtime(0.16f);
        }

        /// <summary>Uma nuvenzinha de cacos que sobe e some.</summary>
        void Estouro(Maquina m, Vector2 onde, Color cor, int quantos)
        {
            for (var i = 0; i < quantos; i++)
            {
                var caco = Bola("c", m.Rastros, cor, onde, Random.Range(4f, 9f));
                var destino = onde + new Vector2(Random.Range(-34f, 34f), Random.Range(6f, 40f));
                Widgets.Deslizar(caco, destino, 0.3f);
                Destroy(caco.gameObject, 0.34f);
            }
        }
    }
}
