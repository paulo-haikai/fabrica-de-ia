using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FabricaDeIA.Arte
{
    /// <summary>
    /// O manifesto que o gerador de arte escreve junto com os PNGs.
    /// Os nomes dos campos batem com as chaves do JSON — é assim que o
    /// <c>JsonUtility</c> preenche, e por isso nenhum deles pode ser renomeado
    /// de um lado só.
    /// </summary>
    [Serializable] public class Manifesto
    {
        public FolhaTileset tileset;
        public FolhaBancadas bancadas;
        public FolhaElenco elenco;
        public FolhaAbertura abertura;
    }

    [Serializable] public class FolhaAbertura
    {
        public string prefixo;
        public PecaDaAbertura[] pecas;
    }

    [Serializable] public class PecaDaAbertura
    {
        public string nome;
        public int largura;
        public int altura;
    }

    [Serializable] public class FolhaTileset
    {
        public string arquivo;
        public int lado;
        public int colunas;
        public TileNomeado[] tiles;
    }

    [Serializable] public class TileNomeado
    {
        public string nome;
        public int indice;
    }

    [Serializable] public class FolhaBancadas
    {
        public string arquivo;
        public int lado;
        public int colunas;
        public string[] ordem;
    }

    [Serializable] public class FolhaElenco
    {
        public string arquivo;
        public int largura;
        public int altura;
        public int direcoes;
        public int quadros;
        public PessoaDaFolha[] pessoas;
    }

    [Serializable] public class PessoaDaFolha
    {
        public string id;
        public string nome;
        public string oficio;
        public int linha;
    }

    /// <summary>
    /// Carrega as folhas de arte e as recorta em sprites.
    ///
    /// O recorte acontece aqui, em tempo de execução, e não no Sprite Editor.
    /// A arte é gerada por script e muda de tamanho quando alguém acrescenta um
    /// tile; se o fatiamento morasse no <c>.meta</c>, cada mudança de arte
    /// exigiria abrir o Editor e refatiar à mão — e é exatamente aí que a arte
    /// e o jogo se desencontram. Com as medidas vindo do <c>arte.json</c>, o
    /// jogo se ajusta sozinho.
    ///
    /// O custo é uma varredura de textura no carregamento. Nas três folhas
    /// deste jogo isso é ruído; se um dia forem trinta, vale um atlas de
    /// verdade.
    /// </summary>
    public class Folhas
    {
        public const int PixelsPorUnidade = 16;

        // As direções, na ordem em que o gerador de arte escreveu os quadros.
        // Moram aqui, e não em quem anda, porque são um fato da FOLHA: quem
        // desenha um personagem — o mundo ou a abertura — precisa delas, e duas
        // cópias divergiriam na primeira vez que a folha mudasse.
        public const int Baixo = 0;
        public const int Cima = 1;
        public const int Esquerda = 2;
        public const int Direita = 3;

        public readonly Manifesto Dados;

        readonly Sprite[] _tiles;
        readonly Sprite[] _bancadas;
        readonly Sprite[] _elenco;

        Folhas(Manifesto dados, Sprite[] tiles, Sprite[] bancadas, Sprite[] elenco)
        {
            Dados = dados;
            _tiles = tiles;
            _bancadas = bancadas;
            _elenco = elenco;
        }

        public static Folhas Carregar()
        {
            var json = Resources.Load<TextAsset>("Arte/arte");
            if (json == null)
                throw new InvalidOperationException(
                    "Arte/arte.json não encontrado. Rode `npm run arte` na raiz do repositório.");

            var dados = JsonUtility.FromJson<Manifesto>(json.text);
            Conferir(dados);

            var tiles = Recortar(dados.tileset.arquivo, dados.tileset.lado, dados.tileset.lado,
                                 dados.tileset.colunas, new Vector2(0f, 0f));

            // As bancadas têm o pivô no pé do móvel: é o pé que encosta no chão
            // e é por ele que a ordenação por profundidade tem que decidir quem
            // fica na frente de quem.
            var bancadas = Recortar(dados.bancadas.arquivo, dados.bancadas.lado, dados.bancadas.lado,
                                    dados.bancadas.colunas, new Vector2(0.5f, 0.09f));

            var elenco = Recortar(dados.elenco.arquivo, dados.elenco.largura, dados.elenco.altura,
                                  dados.elenco.direcoes * dados.elenco.quadros,
                                  new Vector2(0.5f, 0.04f));

            return new Folhas(dados, tiles, bancadas, elenco);
        }

        /// <summary>
        /// Confere se o manifesto tem tudo o que o jogo vai pedir.
        ///
        /// O <c>JsonUtility</c> não reclama de campo ausente: ele devolve null e
        /// segue em frente. Sem esta conferência, um <c>arte.json</c> gerado por
        /// uma versão antiga do script quebra lá adiante, num
        /// <c>ArgumentNullException</c> dentro de <see cref="Tile"/>, e a pista
        /// aponta para o lugar errado. Falhar aqui custa dez linhas e economiza
        /// meia hora de caça.
        /// </summary>
        static void Conferir(Manifesto dados)
        {
            string falta = null;
            if (dados == null) falta = "o arquivo não é um manifesto válido";
            else if (dados.tileset?.tiles == null || dados.tileset.tiles.Length == 0)
                falta = "tileset.tiles";
            else if (dados.bancadas?.ordem == null || dados.bancadas.ordem.Length == 0)
                falta = "bancadas.ordem";
            else if (dados.elenco?.pessoas == null || dados.elenco.pessoas.Length == 0)
                falta = "elenco.pessoas";

            if (falta == null) return;
            throw new InvalidOperationException(
                $"Arte/arte.json está desatualizado ou incompleto ({falta}). " +
                "Rode `npm run arte` na raiz do repositório e deixe o Unity reimportar.");
        }

        /// <summary>
        /// Recorta uma grade uniforme, da esquerda para a direita e de cima
        /// para baixo — a ordem em que o gerador escreveu.
        ///
        /// A inversão do Y existe porque textura no Unity começa embaixo e o
        /// gerador escreve de cima para baixo. Errar este sinal é o clássico
        /// "a folha carregou de cabeça para baixo".
        /// </summary>
        static Sprite[] Recortar(string arquivo, int largura, int altura, int colunas, Vector2 pivo)
        {
            var nome = arquivo.Replace(".png", string.Empty);
            var textura = Resources.Load<Texture2D>($"Arte/{nome}");
            if (textura == null)
                throw new InvalidOperationException($"folha ausente: Resources/Arte/{nome}.png");

            var linhas = textura.height / altura;
            var sprites = new Sprite[linhas * colunas];

            for (var linha = 0; linha < linhas; linha++)
            {
                for (var coluna = 0; coluna < colunas; coluna++)
                {
                    var retangulo = new Rect(
                        coluna * largura,
                        textura.height - (linha + 1) * altura,
                        largura,
                        altura);

                    var sprite = Sprite.Create(textura, retangulo, pivo, PixelsPorUnidade,
                                               0, SpriteMeshType.FullRect);
                    sprite.name = $"{nome}_{linha}_{coluna}";
                    sprites[linha * colunas + coluna] = sprite;
                }
            }
            return sprites;
        }

        // ------------------------------------------------------------ acesso

        /// <summary>Um tile de cenário pelo nome que o gerador deu a ele.</summary>
        public Sprite Tile(string nome)
        {
            var achado = Dados.tileset.tiles.FirstOrDefault(t => t.nome == nome);
            if (achado == null)
                throw new ArgumentException($"tile desconhecido: {nome}", nameof(nome));
            return _tiles[achado.indice];
        }

        /// <summary>A bancada de uma etapa (<c>e1</c> a <c>e12</c>).</summary>
        public Sprite Bancada(string etapa)
        {
            var i = Array.IndexOf(Dados.bancadas.ordem, etapa);
            if (i < 0) throw new ArgumentException($"bancada sem etapa: {etapa}", nameof(etapa));
            return _bancadas[i];
        }

        /// <summary>
        /// Um quadro do elenco. <paramref name="direcao"/> segue a ordem do
        /// gerador: 0 baixo, 1 cima, 2 esquerda, 3 direita.
        /// </summary>
        public Sprite Quadro(string idPessoa, int direcao, int quadro)
        {
            var pessoa = Pessoa(idPessoa);
            var porLinha = Dados.elenco.direcoes * Dados.elenco.quadros;
            var indice = pessoa.linha * porLinha
                       + Mathf.Clamp(direcao, 0, Dados.elenco.direcoes - 1) * Dados.elenco.quadros
                       + Mathf.Clamp(quadro, 0, Dados.elenco.quadros - 1);
            return _elenco[indice];
        }

        /// <summary>Os quatro quadros da caminhada numa direção.</summary>
        public Sprite[] Caminhada(string idPessoa, int direcao)
        {
            var quadros = new Sprite[Dados.elenco.quadros];
            for (var i = 0; i < quadros.Length; i++) quadros[i] = Quadro(idPessoa, direcao, i);
            return quadros;
        }

        readonly Dictionary<string, Sprite> _abertura = new();

        /// <summary>
        /// Uma peça da abertura, carregada sob demanda.
        ///
        /// Cada peça é um PNG próprio, e não uma célula de atlas: são de tamanhos
        /// muito diferentes e a cena estica algumas delas na tela — céu e colina
        /// cobrem a largura inteira. Esticar uma célula de atlas puxaria pixel da
        /// peça vizinha na borda.
        ///
        /// Carrega quando pedem e guarda: quem só joga o ateliê nunca paga por
        /// isto, e quem vê a abertura paga uma vez.
        /// </summary>
        public Sprite Peca(string nome)
        {
            if (_abertura.TryGetValue(nome, out var pronta)) return pronta;

            var prefixo = Dados.abertura != null ? Dados.abertura.prefixo : "abertura_";
            var textura = Resources.Load<Texture2D>($"Arte/{prefixo}{nome}");
            if (textura == null)
                throw new InvalidOperationException(
                    $"peça de abertura ausente: Resources/Arte/{prefixo}{nome}.png");

            var sprite = Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height),
                                       new Vector2(0.5f, 0.5f), PixelsPorUnidade,
                                       0, SpriteMeshType.FullRect);
            sprite.name = nome;
            _abertura[nome] = sprite;
            return sprite;
        }

        public PessoaDaFolha Pessoa(string id)
        {
            var achada = Dados.elenco.pessoas.FirstOrDefault(p => p.id == id);
            if (achada == null)
                throw new ArgumentException($"pessoa fora do elenco: {id}", nameof(id));
            return achada;
        }
    }
}
