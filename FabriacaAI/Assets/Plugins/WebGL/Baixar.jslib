// Entrega um arquivo ao aluno pelo navegador.
//
// O jogo monta o PDF em memória e chama isto. Não há upload nem servidor: o Blob
// nasce e morre na aba do aluno, e o único destino dele é a pasta de downloads da
// própria máquina.
//
// Sobre o `<a download>` criado e clicado por script: é o único jeito de um Canvas
// WebGL entregar bytes ao usuário. O elemento precisa estar no documento antes do
// clique — no Firefox um elemento solto não dispara nada — e a URL do Blob precisa
// ser revogada depois, senão cada diploma gerado fica pendurado na memória da aba.

var BaixarArquivo = {
  BaixarBytes: function (nomePtr, dadosPtr, tamanho, tipoPtr) {
    var nome = UTF8ToString(nomePtr);
    var tipo = UTF8ToString(tipoPtr);

    // `HEAPU8.subarray` devolve uma janela para dentro do heap do WASM, e o heap
    // pode ser realocado a qualquer momento. `slice` copia — sem a cópia, um
    // arquivo grande pode chegar corrompido ao Blob.
    var bytes = HEAPU8.slice(dadosPtr, dadosPtr + tamanho);
    var blob = new Blob([bytes], { type: tipo });
    var url = URL.createObjectURL(blob);

    var elo = document.createElement('a');
    elo.href = url;
    elo.download = nome;
    elo.style.display = 'none';
    document.body.appendChild(elo);
    elo.click();

    // Uma folga antes de limpar: o Safari começa a leitura do Blob DEPOIS do
    // clique, e revogar na mesma volta do laço de eventos cancela o download.
    setTimeout(function () {
      document.body.removeChild(elo);
      URL.revokeObjectURL(url);
    }, 4000);
  }
};

mergeInto(LibraryManager.library, BaixarArquivo);
