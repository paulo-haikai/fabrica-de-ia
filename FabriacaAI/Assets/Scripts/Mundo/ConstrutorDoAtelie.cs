using System.Collections.Generic;
using FabricaDeIA.Arte;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace FabricaDeIA.Mundo
{
    /// <summary>
    /// Levanta o salão inteiro em tempo de execução, a partir da planta e das
    /// folhas de arte.
    ///
    /// O mundo não está guardado numa cena montada à mão. É uma escolha, não
    /// uma limitação: uma cena de Unity é um YAML gigante que ninguém revisa
    /// num diff, e um salão de 40x19 pintado à mão seria retrabalhado inteiro a
    /// cada mudança de tileset. Aqui, mexer na planta é mexer numa string em
    /// <see cref="Atelie"/>, e a arte nova aparece assim que o gerador roda.
    ///
    /// A cena guarda apenas a câmera e um objeto com o <c>Jogo</c>. Tudo o mais
    /// nasce daqui.
    /// </summary>
    public class ConstrutorDoAtelie
    {
        // Camadas de desenho. Os objetos ficam todos na mesma e se ordenam por
        // Y (a câmera usa eixo de transparência customizado), que é o que faz o
        // jogador passar por trás de uma bancada e ser tapado por ela.
        public const int OrdemChao = -20;
        public const int OrdemLuzDeChao = -10;
        public const int OrdemParedes = -5;
        public const int OrdemObjetos = 0;
        public const int OrdemTeto = 20;

        static readonly string[] VariantesDePiso =
            { "piso_a", "piso_b", "piso_c", "piso_d" };

        readonly Folhas _folhas;
        readonly Transform _raiz;

        public Transform Raiz => _raiz;
        public List<Bancada> Bancadas { get; } = new();

        public ConstrutorDoAtelie(Folhas folhas)
        {
            _folhas = folhas;
            _raiz = new GameObject("Ateliê").transform;
        }

        public void Construir()
        {
            MontarChao();
            MontarParedes();
            MontarBancadas();
            MontarTeto();
            AcenderLuzes();
        }

        // -------------------------------------------------------------- chão

        void MontarChao()
        {
            var mapa = NovoTilemap("Chão", OrdemChao);
            var variantes = new List<Tile>();
            foreach (var nome in VariantesDePiso) variantes.Add(NovoTile(nome));
            var gasto = NovoTile("piso_gasto");
            var no = NovoTile("piso_no");
            var ladrilho = NovoTile("ladrilho");

            for (var ty = 0; ty < Atelie.AlturaTiles; ty++)
            {
                for (var tx = 0; tx < Atelie.LarguraTiles; tx++)
                {
                    if (!Atelie.EhPiso(tx, ty)) continue;

                    // Ruído determinístico: o salão é sempre o mesmo entre
                    // sessões, o que importa quando o professor projeta na
                    // parede e diz "andem até a mesa clara à esquerda".
                    var h = Embaralhar(tx, ty);
                    Tile escolhido;
                    if (PertoDeBancada(tx, ty)) escolhido = ladrilho;
                    else if (h % 97 == 0) escolhido = no;
                    else if (h % 11 == 0) escolhido = gasto;
                    else escolhido = variantes[(int)(h % (uint)variantes.Count)];

                    mapa.SetTile(EmCelula(tx, ty), escolhido);
                }
            }
        }

        /// <summary>O ladrilho de oficina marca o entorno de cada bancada.</summary>
        static bool PertoDeBancada(int tx, int ty)
        {
            foreach (var posto in Atelie.Postos)
            {
                if (Mathf.Abs(posto.Tx - tx) <= 1 && ty - posto.Ty is >= 0 and <= 1) return true;
            }
            return false;
        }

        // ----------------------------------------------------------- paredes

        /// <summary>
        /// Paredes, canteiros e caixotes vão para o MESMO tilemap.
        ///
        /// Não é preguiça: um só <c>TilemapCollider2D</c> alimentando um
        /// <c>CompositeCollider2D</c> funde tudo em poucos polígonos, em vez de
        /// espalhar setecentos colisores de caixa pelo salão. Menos objeto de
        /// física é o que segura o quadro no notebook da escola.
        /// </summary>
        void MontarParedes()
        {
            var mapa = NovoTilemap("Paredes", OrdemParedes);

            var frente = NovoTile("parede_frente");
            var frenteVerde = NovoTile("parede_frente_verde");
            var topo = NovoTile("parede_topo");
            var topoVerde = NovoTile("parede_topo_verde");
            var canteiro = NovoTile("canteiro");
            var caixote = NovoTile("caixote");

            for (var ty = 0; ty < Atelie.AlturaTiles; ty++)
            {
                for (var tx = 0; tx < Atelie.LarguraTiles; tx++)
                {
                    if (Atelie.Em(tx, ty) != '#') continue;

                    // Parede com piso logo abaixo mostra a face da frente; o
                    // resto mostra só o topo. Sem essa distinção o salão lê como
                    // planta baixa impressa em vez de lugar com altura.
                    var temPisoAbaixo = Atelie.EhPiso(tx, ty + 1);
                    var verde = Embaralhar(tx, ty) % 3 == 0;
                    var tile = temPisoAbaixo
                        ? (verde ? frenteVerde : frente)
                        : (verde ? topoVerde : topo);

                    mapa.SetTile(EmCelula(tx, ty), tile);
                }
            }

            foreach (var p in Atelie.Canteiros) mapa.SetTile(EmCelula(p.x, p.y), canteiro);
            foreach (var p in Atelie.Caixotes) mapa.SetTile(EmCelula(p.x, p.y), caixote);

            var corpo = mapa.gameObject.AddComponent<Rigidbody2D>();
            corpo.bodyType = RigidbodyType2D.Static;

            var colisor = mapa.gameObject.AddComponent<TilemapCollider2D>();
            colisor.compositeOperation = Collider2D.CompositeOperation.Merge;
            mapa.gameObject.AddComponent<CompositeCollider2D>().geometryType =
                CompositeCollider2D.GeometryType.Polygons;

            MontarSombraDeParede();
        }

        /// <summary>A sombra que a parede joga no piso. Assenta o salão no chão.</summary>
        void MontarSombraDeParede()
        {
            var mapa = NovoTilemap("Sombra das paredes", OrdemLuzDeChao);
            var sombra = NovoTile("sombra_parede");

            for (var ty = 0; ty < Atelie.AlturaTiles; ty++)
            {
                for (var tx = 0; tx < Atelie.LarguraTiles; tx++)
                {
                    if (Atelie.EhPiso(tx, ty) && Atelie.Em(tx, ty - 1) == '#')
                        mapa.SetTile(EmCelula(tx, ty), sombra);
                }
            }
        }

        // ---------------------------------------------------------- bancadas

        void MontarBancadas()
        {
            foreach (var posto in Atelie.Postos)
            {
                var objeto = new GameObject($"Bancada {posto.Etapa}");
                objeto.transform.SetParent(_raiz, false);
                // O pé da bancada encosta na base do seu tile; o móvel cresce
                // para cima a partir daí.
                objeto.transform.position = Atelie.CentroDoTile(posto.Tx, posto.Ty)
                                          + new Vector2(0f, -0.5f);

                var desenho = objeto.AddComponent<SpriteRenderer>();
                desenho.sprite = _folhas.Bancada(ArteDe(posto.Etapa));
                desenho.sortingOrder = OrdemObjetos;

                var bloqueio = objeto.AddComponent<BoxCollider2D>();
                bloqueio.size = new Vector2(1.6f, 0.55f);
                bloqueio.offset = new Vector2(0f, 0.28f);

                var bancada = objeto.AddComponent<Bancada>();
                bancada.Definir(posto);
                Bancadas.Add(bancada);

                MontarMestre(posto);
            }
        }

        /// <summary>
        /// De que etapa vem a ARTE de uma estação.
        ///
        /// Quase sempre é a dela mesma. A exceção é o balcão do certificado
        /// (e12), que entrou no salão antes de o gerador de arte ganhar um móvel
        /// e um retrato para ele: por enquanto ele empresta os da última bancada.
        ///
        /// Emprestar, e não deixar passar: `Folhas.Bancada` e `Folhas.Pessoa`
        /// LANÇAM quando não conhecem a etapa, e como o salão inteiro nasce num
        /// laço só, a exceção derrubaria as treze estações e o aluno abriria o
        /// jogo num mundo vazio. Um móvel repetido é um defeito visível e
        /// pequeno; um ateliê que não nasce é o jogo inteiro.
        /// </summary>
        static string ArteDe(string etapa) => etapa == "e12" ? "e11" : etapa;

        /// <summary>
        /// O mestre fica ao lado da própria bancada, virado para o corredor por
        /// onde o aluno chega. Um NPC de costas para quem entra é a forma mais
        /// silenciosa de o jogo parecer quebrado.
        /// </summary>
        void MontarMestre(Posto posto)
        {
            var objeto = new GameObject($"Mestre {posto.Etapa}");
            objeto.transform.SetParent(_raiz, false);
            objeto.transform.position = Atelie.CentroDoTile(posto.Tx, posto.Ty)
                                      + new Vector2(1.15f, -0.85f);

            var desenho = objeto.AddComponent<SpriteRenderer>();
            desenho.sortingOrder = OrdemObjetos;

            var pessoa = objeto.AddComponent<Andarilho>();
            pessoa.Preparar(_folhas, ArteDe(posto.Etapa), Andarilho.Baixo);
            pessoa.Respirar();

            var bloqueio = objeto.AddComponent<BoxCollider2D>();
            bloqueio.size = new Vector2(0.7f, 0.4f);
            bloqueio.offset = new Vector2(0f, 0.2f);
        }

        // --------------------------------------------------------------- teto

        void MontarTeto()
        {
            var luzNoChao = NovoTilemap("Luz das claraboias", OrdemLuzDeChao);
            var vidros = NovoTilemap("Claraboias", OrdemTeto);
            var vidro = NovoTile("painel");

            // Três intensidades montam uma mancha só. Ver `poca` no gerador de
            // arte: com um tile único repetido, a poça vira grade de borrões.
            var centro = NovoTile("luz_centro");
            var lado = NovoTile("luz_lado");
            var quina = NovoTile("luz_quina");

            foreach (var p in Atelie.Claraboias)
            {
                vidros.SetTile(EmCelula(p.x, p.y), vidro);

                // A poça cai um tile abaixo do vidro: o sol entra inclinado, e
                // essa defasagem é o que dá hora do dia ao salão.
                for (var dx = -1; dx <= 1; dx++)
                {
                    for (var dy = 0; dy <= 2; dy++)
                    {
                        if (!Atelie.EhPiso(p.x + dx, p.y + dy)) continue;
                        var distancia = Mathf.Abs(dx) + Mathf.Abs(dy - 1);
                        var tile = distancia switch
                        {
                            0 => centro,
                            1 => lado,
                            _ => quina
                        };
                        luzNoChao.SetTile(EmCelula(p.x + dx, p.y + dy), tile);
                    }
                }
            }
        }

        /// <summary>
        /// A luz de verdade, por cima da luz pintada.
        ///
        /// O tile de luz sozinho é estático e some quando o aluno para de
        /// reparar. Uma <c>Light2D</c> em cada claraboia devolve o volume: a
        /// bancada iluminada puxa o olho, o canto escuro pede exploração, e o
        /// mesmo cenário ganha duas leituras sem custar um pixel de arte nova.
        /// </summary>
        void AcenderLuzes()
        {
            var ambiente = new GameObject("Luz ambiente");
            ambiente.transform.SetParent(_raiz, false);
            var global = ambiente.AddComponent<Light2D>();
            global.lightType = Light2D.LightType.Global;
            // Ambiente puxado para o âmbar e não muito fraco: o salão é acolhedor,
            // não é masmorra, e texto branco sobre penumbra cansa em aula longa.
            global.color = new Color(1f, 0.94f, 0.84f);
            global.intensity = 0.78f;

            foreach (var p in Atelie.Claraboias)
            {
                var objeto = new GameObject("Feixe");
                objeto.transform.SetParent(_raiz, false);
                objeto.transform.position = Atelie.CentroDoTile(p.x, p.y) + new Vector2(0f, -1f);

                var feixe = objeto.AddComponent<Light2D>();
                feixe.lightType = Light2D.LightType.Point;
                feixe.color = new Color(1f, 0.9f, 0.66f);
                feixe.intensity = 0.85f;
                feixe.pointLightInnerRadius = 1.2f;
                feixe.pointLightOuterRadius = 4.5f;
                feixe.falloffIntensity = 0.7f;
                objeto.AddComponent<Piscar>();
            }
        }

        // ---------------------------------------------------------- utilidades

        Tilemap NovoTilemap(string nome, int ordem)
        {
            var objeto = new GameObject(nome);
            objeto.transform.SetParent(_raiz, false);

            var grade = objeto.AddComponent<Grid>();
            grade.cellSize = Vector3.one;

            var mapa = objeto.AddComponent<Tilemap>();
            var desenho = objeto.AddComponent<TilemapRenderer>();
            desenho.sortingOrder = ordem;
            // Um chunk por tilemap: são poucos e estáticos, e o chunk único
            // evita o custo de reordenar malha a cada quadro.
            desenho.mode = TilemapRenderer.Mode.Chunk;
            return mapa;
        }

        Tile NovoTile(string nome)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = _folhas.Tile(nome);
            tile.colliderType = Tile.ColliderType.Grid;
            return tile;
        }

        /// <summary>A planta cresce para baixo; a grade do Unity, para cima.</summary>
        static Vector3Int EmCelula(int tx, int ty) => new(tx, -ty - 1, 0);

        /// <summary>
        /// Hash de posição. Serve para variar o piso sem sortear: sorteio muda
        /// o salão a cada carregamento e desfaz a memória espacial da turma.
        /// </summary>
        static uint Embaralhar(int x, int y)
        {
            var h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177;
            return h ^ (h >> 16);
        }
    }
}
