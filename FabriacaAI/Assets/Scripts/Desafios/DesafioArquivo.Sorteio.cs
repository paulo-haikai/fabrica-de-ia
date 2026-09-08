using System.Collections.Generic;
using FabricaDeIA.Engine;
using UnityEngine;

namespace FabricaDeIA.Desafios
{
    /// <summary>
    /// O sorteio da bancada 3: que vocabulário compõe a tabela desta rodada,
    /// e o tabuleiro nasce daí.
    ///
    /// Separado de <c>DesafioArquivo.cs</c> só para caber no limite de linhas do
    /// projeto — é a mesma classe, partida no mesmo padrão de
    /// <c>.Desenho.cs</c>.
    /// </summary>
    public partial class DesafioArquivo
    {
        /// <summary>
        /// Monta a tabela da rodada: um risquinho para cada par que o corpus
        /// realmente tem, dentro do vocabulário desta rodada.
        ///
        /// Os pares saem do corpus de verdade, e é isso que faz a conta ser honesta
        /// — não são trinta marcas espalhadas a esmo para o jogo dar certo, são os
        /// pares que alguém escreveu mesmo.
        /// </summary>
        void Semear()
        {
            _tabela = new Casa[_lado, _lado];
            _riscos = 0;

            var arquivo = new Bigrama(Corpus.Frases);
            MedirOTeto(arquivo);

            var palavras = Escolher(arquivo);

            for (var i = 0; i < palavras.Count; i++)
            {
                for (var j = 0; j < palavras.Count; j++)
                {
                    if (arquivo.Risquinhos(palavras[i], palavras[j]) <= 0) continue;
                    _tabela[i, j] = Casa.Risco;
                    _riscos++;
                }
            }

            // A base: um cantinho já tomado, para o aluno ter de onde sair e para
            // onde voltar. Sem base, a primeira volta não tem como fechar.
            for (var y = 0; y < 2; y++)
                for (var x = 0; x < 2; x++)
                    if (_tabela[x, y] != Casa.Risco) _tabela[x, y] = Casa.Meu;

            _onde = new Vector2Int(1, 1);
            _rumo = Vector2Int.right;
            _tomadas = Contar(Casa.Meu);

            if (NivelAtual == 0) _riscosDaPrimeira = _riscos;
        }

        /// <summary>
        /// Conta o arquivo COMPLETO: todas as palavras que o corpus tem e todos os
        /// pares distintos entre elas.
        ///
        /// É a única conta desta bancada que não depende da rodada, e é por isso que
        /// ela existe. As três rodadas mostram os pares subirem junto com as
        /// palavras, e sozinhas dariam a impressão errada — a de que basta ler mais
        /// para o arquivo encher. O teto desmente: por mais palavras que o Aurélio
        /// ponha no fichário, os pares param aqui, porque quem os escreve é o texto,
        /// e o texto acabou.
        ///
        /// Percorre as linhas da tabela, e não as casinhas: são mil e poucos pares
        /// contra cem mil casinhas, e o resultado é o mesmo.
        /// </summary>
        void MedirOTeto(Bigrama arquivo)
        {
            if (_paresTodos > 0) return;

            foreach (var palavra in arquivo.Palavras)
            {
                if (palavra == Bigrama.Inicio || palavra == Bigrama.Fim) continue;
                _vocabularioTodo++;
                foreach (var continuacao in arquivo.Continuacoes(palavra))
                    if (continuacao.Para != Bigrama.Fim) _paresTodos++;
            }
        }

        /// <summary>
        /// Sorteia o vocabulário desta rodada, várias vezes, e fica com o tabuleiro
        /// cujo vazio chega mais perto do alvo.
        ///
        /// A lista sai ORDENADA POR MOVIMENTO, e a ordem importa tanto quanto o
        /// sorteio. Os eixos em ordem de posto deixam os riscos amontoados no canto
        /// das palavras movimentadas e o resto do tabuleiro limpo, e é o que torna
        /// a bancada jogável: medindo a maior volta única possível, com os eixos
        /// ordenados ela cerca 42-60% na rodada 1, 68-78% na 2 e 66-84% na 3 — logo
        /// abaixo de cada meta, de modo que a primeira volta grande dá quase tudo e
        /// faltam poucas para fechar.
        ///
        /// Embaralhar os eixos foi medido e reprovado: espalha os riscos por todo o
        /// tabuleiro, a maior volta da rodada 1 cai para 33-39% contra uma meta de
        /// 60%, e a bancada vira uma sequência longa de voltinhas. O canto cheio
        /// também é o retrato honesto da tabela — é ali que as palavras que todo
        /// mundo usa se encontram.
        /// </summary>
        List<string> Escolher(Bigrama arquivo)
        {
            var ordenadas = new List<string>();
            foreach (var p in arquivo.MaisMovimentadas())
            {
                if (p == Bigrama.Inicio || p == Bigrama.Fim) continue;
                ordenadas.Add(p);
            }

            var fundo = Mathf.Min(Faixa[NivelAtual], ordenadas.Count);
            var alvo = VazioAlvo[NivelAtual];
            var casas = (float)_lado * _lado;

            List<int> melhor = null;
            var melhorErro = float.MaxValue;

            for (var t = 0; t < Tentativas; t++)
            {
                var postos = Sortear(fundo, _lado);

                var riscos = 0;
                foreach (var i in postos)
                    foreach (var j in postos)
                        if (arquivo.Risquinhos(ordenadas[i], ordenadas[j]) > 0) riscos++;

                var erro = Mathf.Abs(1f - riscos / casas - alvo);
                if (erro >= melhorErro) continue;
                melhorErro = erro;
                melhor = postos;
            }

            melhor.Sort();
            var palavras = new List<string>(melhor.Count);
            foreach (var i in melhor) palavras.Add(ordenadas[i]);
            return palavras;
        }

        /// <summary>
        /// Tira <paramref name="quantos"/> postos distintos entre 0 e
        /// <paramref name="fundo"/>, por embaralhamento parcial de Fisher-Yates.
        /// </summary>
        List<int> Sortear(int fundo, int quantos)
        {
            var saco = new int[fundo];
            for (var i = 0; i < fundo; i++) saco[i] = i;

            for (var i = 0; i < quantos; i++)
            {
                var j = i + (int)(_sorteio.Proximo() * (fundo - i));
                if (j >= fundo) j = fundo - 1;
                (saco[i], saco[j]) = (saco[j], saco[i]);
            }

            var saida = new List<int>(quantos);
            for (var i = 0; i < quantos; i++) saida.Add(saco[i]);
            return saida;
        }

        int Contar(Casa que)
        {
            var n = 0;
            foreach (var c in _tabela) if (c == que) n++;
            return n;
        }
    }
}
