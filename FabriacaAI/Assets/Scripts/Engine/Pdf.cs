using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FabricaDeIA.Engine
{
    /// <summary>
    /// Um escritor de PDF de uma página, escrito à mão.
    ///
    /// Por que à mão e não uma biblioteca: o jogo roda em WebGL, e toda biblioteca
    /// de PDF em C# ou não compila sob IL2CPP, ou arrasta megabytes para um build
    /// que a escola vai baixar num Wi-Fi ruim. O que este diploma precisa é
    /// texto, retângulo e linha — e isso é meia página de operadores.
    ///
    /// Duas decisões que evitam quase toda a complexidade do formato:
    ///
    ///   · FONTES BASE-14. Helvetica e Helvetica-Bold existem dentro de todo
    ///     leitor de PDF por definição do padrão, então não há fonte para embutir.
    ///     Embutir TrueType exigiria tabela de larguras, subconjunto de glifos e
    ///     mapa de codificação — três problemas que aqui simplesmente não existem.
    ///
    ///   · SEM COMPRESSÃO. O documento tem uns 6 KB. Comprimir economizaria uns 3
    ///     e custaria um envelope zlib escrito à mão, com Adler-32.
    ///
    /// Sistema de coordenadas: o PDF conta do CANTO INFERIOR esquerdo, para cima.
    /// Como pensar um diploma de baixo para cima é receita de erro, esta classe
    /// recebe tudo em coordenadas de TOPO e faz a inversão ela mesma.
    /// </summary>
    public class Pdf
    {
        /// <summary>A4 em pontos tipográficos, que é a unidade do PDF.</summary>
        public const float Largura = 595f;
        public const float Altura = 842f;

        public const string Normal = "F1";
        public const string Negrito = "F2";

        readonly StringBuilder _corpo = new();

        /// <summary>Cada JPEG colado na folha, na ordem em que foi pedido.</summary>
        readonly List<(string dados, int largura, int altura)> _figuras = new();

        static string N(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        /// <summary>Converte y medido do topo para o y que o PDF espera.</summary>
        static float DoTopo(float y) => Altura - y;

        // ------------------------------------------------------------ desenho

        /// <summary>Escreve uma linha de texto, com o x sendo a borda esquerda.</summary>
        public void Texto(string texto, float x, float yDoTopo, float tamanho,
                          string fonte = Normal, (float r, float g, float b)? cor = null)
        {
            if (string.IsNullOrEmpty(texto)) return;
            var c = cor ?? (0f, 0f, 0f);

            _corpo.Append("BT ")
                  .Append($"{N(c.r)} {N(c.g)} {N(c.b)} rg ")
                  .Append($"/{fonte} {N(tamanho)} Tf ")
                  .Append($"1 0 0 1 {N(x)} {N(DoTopo(yDoTopo) - tamanho)} Tm ")
                  .Append($"({Escapar(texto)}) Tj ET\n");
        }

        /// <summary>
        /// Escreve centralizado em torno de um x.
        ///
        /// A largura sai de uma tabela de larguras da Helvetica. Não é exata ao
        /// centésimo, e não precisa: um título dois pontos fora do centro ninguém
        /// vê, e a alternativa seria embutir métricas de fonte no jogo.
        /// </summary>
        public void TextoCentrado(string texto, float centro, float yDoTopo, float tamanho,
                                  string fonte = Normal, (float r, float g, float b)? cor = null)
        {
            var largura = LarguraDe(texto, tamanho, fonte == Negrito);
            Texto(texto, centro - largura / 2f, yDoTopo, tamanho, fonte, cor);
        }

        /// <summary>Escreve alinhado à DIREITA de um x — para colunas de número.</summary>
        public void TextoADireita(string texto, float direita, float yDoTopo, float tamanho,
                                  string fonte = Normal, (float r, float g, float b)? cor = null)
        {
            Texto(texto, direita - LarguraDe(texto, tamanho, fonte == Negrito),
                  yDoTopo, tamanho, fonte, cor);
        }

        public void Retangulo(float x, float yDoTopo, float largura, float altura,
                              (float r, float g, float b) cor)
        {
            _corpo.Append($"{N(cor.r)} {N(cor.g)} {N(cor.b)} rg ")
                  .Append($"{N(x)} {N(DoTopo(yDoTopo) - altura)} {N(largura)} {N(altura)} re f\n");
        }

        public void Moldura(float x, float yDoTopo, float largura, float altura,
                            float espessura, (float r, float g, float b) cor)
        {
            _corpo.Append($"{N(cor.r)} {N(cor.g)} {N(cor.b)} RG {N(espessura)} w ")
                  .Append($"{N(x)} {N(DoTopo(yDoTopo) - altura)} {N(largura)} {N(altura)} re S\n");
        }

        public void Linha(float x1, float y1DoTopo, float x2, float y2DoTopo,
                          float espessura, (float r, float g, float b) cor)
        {
            _corpo.Append($"{N(cor.r)} {N(cor.g)} {N(cor.b)} RG {N(espessura)} w ")
                  .Append($"{N(x1)} {N(DoTopo(y1DoTopo))} m {N(x2)} {N(DoTopo(y2DoTopo))} l S\n");
        }

        /// <summary>
        /// Cola um JPEG na folha e devolve a altura que ele ocupou.
        ///
        /// Só JPEG, e de propósito: o PDF tem um filtro chamado DCTDecode que É o
        /// JPEG: os bytes do arquivo entram no documento como estão, sem
        /// decodificar nem recomprimir nada. Um PNG exigiria desinflar o zlib dele
        /// e reinflar do jeito do PDF, ou despejar o bitmap cru — meio megabyte
        /// para um logotipo de vinte quilobytes.
        ///
        /// Pede-se só a LARGURA. A altura sai da proporção do próprio arquivo,
        /// porque a única coisa que ninguém quer num logotipo é vê-lo espremido, e
        /// deixar os dois lados soltos é convidar exatamente isso.
        /// </summary>
        public float Imagem(byte[] jpeg, float x, float yDoTopo, float largura)
        {
            if (jpeg == null || jpeg.Length == 0) return 0f;

            var (px, py) = MedidasDoJpeg(jpeg);
            if (px <= 0 || py <= 0) return 0f;

            var altura = largura * py / px;

            var dados = new StringBuilder(jpeg.Length);
            foreach (var b in jpeg) dados.Append((char)b);
            _figuras.Add((dados.ToString(), px, py));

            // O `cm` desenha a imagem sempre dentro do quadrado unitário: a matriz
            // é que a estica para o tamanho pedido. O q/Q em volta guarda e devolve
            // o estado gráfico, senão essa escala vazaria para tudo que vier depois.
            _corpo.Append("q ")
                  .Append($"{N(largura)} 0 0 {N(altura)} {N(x)} {N(DoTopo(yDoTopo) - altura)} cm ")
                  .Append($"/Im{_figuras.Count} Do Q\n");

            return altura;
        }

        /// <summary>Cola um JPEG centrado em torno de um x.</summary>
        public float ImagemCentrada(byte[] jpeg, float centro, float yDoTopo, float largura)
            => Imagem(jpeg, centro - largura / 2f, yDoTopo, largura);

        /// <summary>
        /// Lê largura e altura de um JPEG no cabeçalho SOF.
        ///
        /// O dicionário da imagem no PDF tem que declarar as medidas em pixels, e
        /// elas TÊM que bater com o arquivo — um leitor que discorda das duas
        /// simplesmente não desenha nada. Ler do próprio arquivo é o que impede
        /// esse número de envelhecer no dia em que alguém trocar o logotipo por
        /// outro de tamanho diferente.
        /// </summary>
        public static (int largura, int altura) MedidasDoJpeg(byte[] j)
        {
            if (j == null || j.Length < 4 || j[0] != 0xFF || j[1] != 0xD8) return (0, 0);

            var i = 2;
            while (i + 9 < j.Length)
            {
                if (j[i] != 0xFF) { i++; continue; }

                var marca = j[i + 1];

                // 0xD0-0xD9 e 0x01 não têm campo de tamanho; pular pelos dois bytes.
                if (marca == 0xFF) { i++; continue; }
                if (marca == 0x01 || (marca >= 0xD0 && marca <= 0xD9)) { i += 2; continue; }

                var tamanho = (j[i + 2] << 8) | j[i + 3];

                // SOF0 a SOFF, tirando os que não descrevem quadro: DHT (C4),
                // JPG (C8) e DAC (CC) caem no meio da faixa e trariam lixo.
                if (marca >= 0xC0 && marca <= 0xCF &&
                    marca != 0xC4 && marca != 0xC8 && marca != 0xCC)
                {
                    var altura = (j[i + 5] << 8) | j[i + 6];
                    var largura = (j[i + 7] << 8) | j[i + 8];
                    return (largura, altura);
                }

                if (tamanho < 2) return (0, 0);
                i += 2 + tamanho;
            }
            return (0, 0);
        }

        // ------------------------------------------------------------ métricas

        /// <summary>
        /// Larguras da Helvetica, em milésimos do corpo da fonte.
        ///
        /// Só as faixas que este documento usa. Fora delas devolve a largura de um
        /// 'n', que é próxima da média — errar a largura de um caractere raro
        /// desloca um título por um fio de cabelo, e não vale uma tabela de 256
        /// entradas escrita à mão.
        /// </summary>
        static int LarguraDoGlifo(char c, bool negrito)
        {
            if (c == ' ') return 278;
            if (c >= '0' && c <= '9') return 556;
            if (c == '.' || c == ',' || c == ':' || c == ';' || c == '\'') return 278;
            if (c == '-') return negrito ? 333 : 333;
            if (c == '(' || c == ')') return 333;
            if (c == 'i' || c == 'l' || c == 'j' || c == 'I') return negrito ? 278 : 222;
            if (c == 'f' || c == 't' || c == 'r') return negrito ? 389 : 333;
            if (c == 'm' || c == 'M' || c == 'W' || c == 'w') return negrito ? 889 : 833;
            if (c >= 'A' && c <= 'Z') return negrito ? 722 : 667;
            return negrito ? 611 : 556;
        }

        public static float LarguraDe(string texto, float tamanho, bool negrito = false)
        {
            if (string.IsNullOrEmpty(texto)) return 0f;
            var soma = 0;
            foreach (var c in texto) soma += LarguraDoGlifo(c, negrito);
            return soma * tamanho / 1000f;
        }

        // ------------------------------------------------------------- montagem

        /// <summary>
        /// Escapa o que quebraria uma string de PDF e converte para WinAnsi.
        ///
        /// WinAnsi cobre todo acento do português em um byte por caractere, o que
        /// dispensa UTF-16 e mapa de codificação. As aspas curvas e os travessões
        /// que o jogo usa não estão em Latin-1, mas ESTÃO em WinAnsi, em posições
        /// próprias — daí o punhado de casos especiais. Sem eles um "não" com aspas
        /// tipográficas sairia com losangos no diploma.
        /// </summary>
        static string Escapar(string texto)
        {
            var saida = new StringBuilder(texto.Length + 8);
            foreach (var c in texto)
            {
                var b = c switch
                {
                    '‘' => 0x91,  // ‘
                    '’' => 0x92,  // ’
                    '“' => 0x93,  // “
                    '”' => 0x94,  // ”
                    '•' => 0x95,  // •
                    '–' => 0x96,  // –
                    '—' => 0x97,  // —
                    '…' => 0x85,  // …
                    _ => c < 256 ? c : '?'
                };

                if (b == '(' || b == ')' || b == '\\') saida.Append('\\');

                // Byte alto vira escape de três dígitos em OCTAL — que é a base que
                // o PDF usa, e a origem de um estrago silencioso: escrito em
                // decimal, "ã" (227) saía como \227, que o leitor interpreta como
                // octal 227 = 151, e imprimia um travessão. "conclusão" virava
                // "conclus—o". Pior no "Á" (193): \193 nem é octal, porque 9 não é
                // dígito octal, e o leitor engolia dois caracteres e cuspia o
                // terceiro — "FÁBRICA" saía "F 93BRICA".
                if (b < 32 || b > 126)
                    saida.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'));
                else
                    saida.Append((char)b);
            }
            return saida.ToString();
        }

        /// <summary>
        /// Fecha o documento e devolve os bytes.
        ///
        /// A tabela xref precisa do deslocamento em BYTES de cada objeto, então o
        /// arquivo é montado num buffer de bytes e não numa string: contar
        /// caracteres daria o número errado no primeiro acento que aparecesse.
        /// </summary>
        public byte[] Fechar()
        {
            var conteudo = _corpo.ToString();

            var objetos = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {N(Largura)} {N(Altura)}] " +
                "/Resources << /Font << /F1 5 0 R /F2 6 0 R >> >> /Contents 4 0 R >>",
                $"<< /Length {Bytes(conteudo).Length} >>\nstream\n{conteudo}endstream",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica " +
                "/Encoding /WinAnsiEncoding >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold " +
                "/Encoding /WinAnsiEncoding >>"
            };

            var arquivo = new List<byte>();
            void Escrever(string s) => arquivo.AddRange(Bytes(s));

            Escrever("%PDF-1.4\n");
            // Um comentário com bytes altos avisa o leitor de que o arquivo é
            // binário. É convenção do padrão e evita que algum transporte
            // "conserte" as quebras de linha pelo caminho.
            arquivo.AddRange(new byte[] { 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A });

            var deslocamentos = new List<int>();
            for (var i = 0; i < objetos.Count; i++)
            {
                deslocamentos.Add(arquivo.Count);
                Escrever($"{i + 1} 0 obj\n{objetos[i]}\nendobj\n");
            }

            var inicioDoXref = arquivo.Count;
            Escrever($"xref\n0 {objetos.Count + 1}\n");
            Escrever("0000000000 65535 f \n");
            foreach (var d in deslocamentos)
                Escrever($"{d.ToString("0000000000", CultureInfo.InvariantCulture)} 00000 n \n");

            Escrever($"trailer\n<< /Size {objetos.Count + 1} /Root 1 0 R >>\n" +
                     $"startxref\n{inicioDoXref}\n%%EOF\n");

            return arquivo.ToArray();
        }

        /// <summary>
        /// Um byte por caractere, sem passar por UTF-8.
        ///
        /// <see cref="Escapar"/> já transformou todo acento em sequência octal
        /// ASCII, então tudo que chega aqui cabe em um byte. Usar UTF-8 aqui
        /// inseriria bytes a mais e a tabela xref apontaria para o lugar errado.
        /// </summary>
        static byte[] Bytes(string s)
        {
            var bytes = new byte[s.Length];
            for (var i = 0; i < s.Length; i++) bytes[i] = (byte)(s[i] & 0xFF);
            return bytes;
        }
    }
}
