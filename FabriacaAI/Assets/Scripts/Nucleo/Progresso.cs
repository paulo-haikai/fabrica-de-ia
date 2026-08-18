using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FabricaDeIA.Nucleo
{
    /// <summary>
    /// O que o aluno já fez e o que cada etapa entregou para a seguinte.
    ///
    /// Sem backend e sem login: o progresso mora no armazenamento do navegador,
    /// via <c>PlayerPrefs</c> (que no WebGL cai em IndexedDB). Isso mantém o
    /// jogo fora do escopo sensível da LGPD com menores e deixa a aula
    /// acontecer mesmo com a internet da escola caindo.
    ///
    /// O tipo guarda também os artefatos que uma etapa passa para a próxima —
    /// o corte que a costureira escolheu, a rede que a tecelã montou. É isso
    /// que faz a aula ser uma linha só, e não doze demonstrações soltas.
    /// </summary>
    [Serializable]
    public class Progresso
    {
        const string Chave = "fabrica-de-ia:progresso";
        const int Versao = 4;

        public int versao = Versao;
        public string atual = "e1";

        /// <summary>
        /// Nome e turma, para o diploma.
        ///
        /// Ficam AQUI e só aqui: no armazenamento do próprio navegador do aluno.
        /// Não sobem para servidor nenhum, porque não existe servidor — o diploma
        /// é montado dentro do jogo e baixado direto para a máquina dele. Quem
        /// entrega o nome ao professor é o aluno, mandando o arquivo, e é assim
        /// que deve ser numa turma de menores de idade.
        /// </summary>
        public string nome = string.Empty;
        public string turma = string.Empty;

        /// <summary>Quando a primeira bancada foi concluída, em ISO-8601 local.</summary>
        public string comecouEm = string.Empty;
        /// <summary>Quando a décima segunda foi concluída.</summary>
        public string terminouEm = string.Empty;

        /// <summary>
        /// Estrelas por etapa, em listas paralelas. Um dicionário seria mais
        /// natural de ler, mas o <c>JsonUtility</c> não serializa dicionário —
        /// e trocar de serializador por causa disso seria caro demais.
        /// </summary>
        public List<string> etapasFeitas = new();
        public List<int> estrelasFeitas = new();

        /// <summary>
        /// Segundos gastos em cada etapa, na mesma ordem das duas listas acima.
        ///
        /// Serve ao professor mais que ao aluno: numa turma de trinta, o tempo por
        /// bancada é o que mostra onde a aula emperrou. Acumula quando o aluno
        /// volta a uma bancada — o número é "quanto tempo esta pessoa passou
        /// nisto", não "quanto durou a melhor tentativa".
        /// </summary>
        public List<int> segundosFeitas = new();

        /// <summary>
        /// As bancadas cujo tutorial o aluno já viu do começo ao fim.
        ///
        /// Só entra aqui quem COMPLETOU: sair da bancada no meio da explicação
        /// não conta, e o tutorial volta na próxima visita. É assim que "não se
        /// pula" convive com "sair é sempre permitido" — ninguém fica preso, e
        /// ninguém chega ao minigame sem ter visto como ele funciona.
        /// </summary>
        public List<string> tutoriaisVistos = new();

        // --- artefatos que atravessam a aula ---
        public string corte;             // etapa 4: o modo de corte escolhido
        public int[] camadas;            // etapa 6: a rede montada
        public float melhorPerda = -1f;  // etapa 8: a menor perda alcançada
        public string corpusEscolhido;   // etapa 9: o texto dado à máquina
        public float[] preferencias;     // etapa 12: a persona

        public static Progresso Atual { get; private set; } = new();

        /// <summary>Disparado quando algo muda, para a HUD não precisar sondar.</summary>
        public static event Action Mudou;

        public int Estrelas(string etapa)
        {
            var i = etapasFeitas.IndexOf(etapa);
            return i < 0 ? 0 : estrelasFeitas[i];
        }

        public bool Concluida(string etapa) => Estrelas(etapa) > 0;

        /// <summary>
        /// A etapa já foi ABERTA alguma vez, com ou sem estrela.
        ///
        /// É uma pergunta diferente de <see cref="Concluida"/>, e a aula precisa
        /// das duas. Concluída é mérito: serve ao diploma e às estrelas. Tentada é
        /// presença: serve à progressão, porque prender um aluno numa bancada que
        /// ele não conseguiu resolver seria transformar dificuldade em muro — e
        /// numa aula de 90 minutos ele simplesmente pararia ali.
        /// </summary>
        public bool Tentada(string etapa) => etapasFeitas.Contains(etapa);

        /// <summary>
        /// A bancada está aberta para o aluno?
        ///
        /// A primeira sempre está; as outras abrem quando a anterior foi TENTADA.
        /// A ordem importa porque a aula é uma linha só — a bancada 6 mostra a
        /// rede que a 5 ajudou a entender, e a 8 treina os botões que derrotaram
        /// o aluno na 7. Ver o fim antes do começo não é liberdade, é confusão.
        ///
        /// Mas a chave é "tentada", não "vencida": quem travou na 7 segue para a
        /// 8, que é justamente a bancada onde a máquina resolve o que ele não deu
        /// conta. Barrar ali inverteria a lição.
        /// </summary>
        public bool Liberada(string etapa)
        {
            if (string.IsNullOrEmpty(etapa) || etapa.Length < 2) return true;
            if (!int.TryParse(etapa.Substring(1), out var numero)) return true;
            // Só as doze da aula têm ordem. O balcão do certificado (e13) fica
            // aberto desde o começo, de propósito: a máquina em pedaços é o que
            // dá tamanho ao que falta, e trancá-la esconderia justamente isso.
            if (numero > 12) return true;
            return numero <= 1 || Tentada($"e{numero - 1}");
        }

        /// <summary>
        /// Quantas das DOZE ele abriu, resolvendo ou não.
        ///
        /// Conta e1..e12 uma a uma em vez de medir o tamanho da lista: o balcão
        /// do certificado também é uma estação e entraria na conta, e a folha
        /// diria "13 de 12".
        /// </summary>
        public int Tentadas
        {
            get
            {
                var quantas = 0;
                for (var i = 1; i <= 12; i++)
                    if (Tentada($"e{i}")) quantas++;
                return quantas;
            }
        }

        /// <summary>
        /// Passou pelas doze, acertando ou não. É o que decide se o robô do
        /// mestre da IA está montado ou ainda em pedaços.
        /// </summary>
        public bool TodasTentadas
        {
            get
            {
                for (var i = 1; i <= 12; i++)
                    if (!Tentada($"e{i}")) return false;
                return true;
            }
        }

        public bool ViuTutorial(string etapa) => tutoriaisVistos.Contains(etapa);

        public void MarcarTutorial(string etapa)
        {
            if (tutoriaisVistos.Contains(etapa)) return;
            tutoriaisVistos.Add(etapa);
            Salvar();
        }

        public int Concluidas => estrelasFeitas.Count(e => e > 0);

        /// <summary>Estrelas somadas, de 0 a 36.</summary>
        public int EstrelasTotais => estrelasFeitas.Sum();

        /// <summary>Segundos somados em todas as bancadas.</summary>
        public int SegundosTotais => segundosFeitas.Sum();

        /// <summary>Segundos gastos numa etapa, ou zero se ela nunca foi aberta.</summary>
        public int Segundos(string etapa)
        {
            var i = etapasFeitas.IndexOf(etapa);
            return i < 0 || i >= segundosFeitas.Count ? 0 : segundosFeitas[i];
        }

        /// <summary>Todas as doze feitas — é o que libera o diploma.</summary>
        public bool AulaCompleta => Proxima() == null;

        /// <summary>
        /// Registra o resultado de uma etapa. Nunca rebaixa: quem tirou três
        /// estrelas e voltou para rever a bancada não perde o que conquistou.
        ///
        /// O tempo, ao contrário das estrelas, SOMA. São perguntas diferentes:
        /// estrela é "o melhor que ele fez", tempo é "quanto isto custou a ele".
        /// </summary>
        public void Concluir(string etapa, int estrelas, int segundos = 0)
        {
            var i = etapasFeitas.IndexOf(etapa);
            if (i < 0)
            {
                etapasFeitas.Add(etapa);
                estrelasFeitas.Add(estrelas);
                segundosFeitas.Add(segundos);
            }
            else
            {
                estrelasFeitas[i] = Mathf.Max(estrelas, estrelasFeitas[i]);
                // Listas paralelas de saves antigos podem estar mais curtas.
                while (segundosFeitas.Count < etapasFeitas.Count) segundosFeitas.Add(0);
                segundosFeitas[i] += segundos;
            }

            var agora = DateTime.Now.ToString("s");
            if (string.IsNullOrEmpty(comecouEm)) comecouEm = agora;
            if (AulaCompleta && string.IsNullOrEmpty(terminouEm)) terminouEm = agora;

            Salvar();
        }

        /// <summary>
        /// A próxima bancada por fazer, na ordem da aula. É para onde a seta da
        /// HUD aponta quando o aluno se perde no salão.
        /// </summary>
        public string Proxima()
        {
            for (var i = 1; i <= 12; i++)
            {
                var etapa = $"e{i}";
                if (!Concluida(etapa)) return etapa;
            }
            return null;
        }

        public void Salvar()
        {
            versao = Versao;
            PlayerPrefs.SetString(Chave, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
            Mudou?.Invoke();
        }

        public static void Carregar()
        {
            var bruto = PlayerPrefs.GetString(Chave, null);
            if (!string.IsNullOrEmpty(bruto))
            {
                try
                {
                    var lido = JsonUtility.FromJson<Progresso>(bruto);
                    // Save de versão antiga não é erro nem é migração: numa aula
                    // solta, recomeçar limpo custa menos que manter conversor.
                    if (lido != null && lido.versao == Versao)
                    {
                        Atual = lido;
                        Mudou?.Invoke();
                        return;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"progresso ilegível, recomeçando: {e.Message}");
                }
            }
            Atual = new Progresso();
            Mudou?.Invoke();
        }

        public static void Reiniciar()
        {
            PlayerPrefs.DeleteKey(Chave);
            PlayerPrefs.Save();
            Atual = new Progresso();
            Mudou?.Invoke();
        }
    }
}
