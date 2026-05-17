using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;
using Microsoft.EntityFrameworkCore;

namespace MissaoBackend.Seeds;

public static class CanticoPtSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var topicoList = await db.Topicos.ToListAsync();
        if (!topicoList.Any()) return;

        // Case-insensitive match on topic name (slugs in DB have inconsistent formatting)
        var topicoByNome = topicoList.ToDictionary(
            t => t.Nome.ToLowerInvariant(),
            t => t.Id
        );

        var existingSlugs = (await db.Canticos.Select(c => c.Slug).ToListAsync()).ToHashSet();

        var novos = GetCanticos()
            .Where(c => topicoByNome.ContainsKey(c.TopicoNome))
            .Select(c => new { Data = c, Slug = SlugHelper.Slugify(c.Titulo) })
            .Where(x => !existingSlugs.Contains(x.Slug))
            .Select(x => new Cantico
            {
                Titulo = x.Data.Titulo,
                Slug = x.Slug,
                Letra = x.Data.Letra,
                TopicoId = topicoByNome[x.Data.TopicoNome]
            })
            .ToList();

        if (novos.Count == 0) return;

        db.Canticos.AddRange(novos);
        await db.SaveChangesAsync();
        Console.WriteLine($"✓ {novos.Count} cânticos PT adicionados.");
    }

    private record CanticoData(string Titulo, string Letra, string TopicoNome);

    private static List<CanticoData> GetCanticos() => new()
    {
        // ── PROCISSÃO ──────────────────────────────────────────────────────
        new CanticoData("Eis que Vem o Rei da Glória",
            @"Eis que vem o Rei da glória,
Vem ao encontro do seu povo.
Hosana nas alturas!
Bendito o que vem em nome do Senhor.

Preparai os caminhos do Rei,
Alargai as portas da cidade.
Hosana nas alturas!
Hosana ao Filho de David!

Com palmas na mão, com o coração,
Acolhemos Jesus Salvador.
Hosana nas alturas!
Hosana ao Rei que chega ao templo.", "procissão"),

        new CanticoData("Caminhemos Juntos",
            @"Caminhemos juntos na estrada de Deus,
Com alegria no coração.
Somos peregrinos rumo ao reino dos céus,
Guiados pela sua mão.

Vamos juntos cantar,
Vamos juntos louvar
O Senhor que nos chama a caminhar.

A estrada é longa, mas não andamos sós,
Jesus é o caminho e a luz.
Ele vai à nossa frente,
A guiar os nossos passos
Até à morada de Deus.", "procissão"),

        new CanticoData("Sobe ao Monte do Senhor",
            @"Sobe ao monte do Senhor
Com alegria e louvor.
Entra na sua santa morada,
Celebra o Deus da vida.

Quem subirá ao monte do Senhor?
Quem estará no seu santo lugar?
Aquele que tem mãos limpas e coração puro,
Que não se apoia na mentira.

Glorificai o Rei da glória!
Quem é este Rei da glória?
O Senhor forte e poderoso,
O Senhor poderoso nos combates.", "procissão"),

        // ── ENTRADA ────────────────────────────────────────────────────────
        new CanticoData("Cantai ao Senhor um Cântico Novo",
            @"Cantai ao Senhor um cântico novo,
Cantai ao Senhor, terra inteira.
Cantai ao Senhor e bendizei o seu nome,
Anunciai de dia em dia a sua salvação.

Narrai entre os povos a sua glória,
Entre todas as nações as suas maravilhas.
Grande é o Senhor e muito digno de louvor,
Terrível acima de todos os deuses.

O Senhor reina com majestade,
Ele firmou o mundo e não vacila.
Ele governa os povos com equidade,
Aleluia, aleluia, cantemos ao Senhor!", "entrada"),

        new CanticoData("Jubilai ao Senhor",
            @"Jubilai ao Senhor, terra inteira!
Servi o Senhor com alegria!
Entrai na sua presença com cânticos de festa.

Sabei que o Senhor é Deus:
Ele nos criou e nós somos seus,
Somos o seu povo, o rebanho que ele apascenta.

Entrai pelas suas portas com acção de graças,
Pelos seus átrios com hinos de louvor.
Dai-lhe graças, bendizei o seu nome.
O Senhor é bom, o seu amor é eterno.", "entrada"),

        new CanticoData("Senhor, Vós Sois o Nosso Deus",
            @"Senhor, vós sois o nosso Deus
E nós o vosso povo.
Reunidos nesta assembleia,
Celebramos a vossa glória.

Vós que criastes o mundo
E nos redimistes no vosso Filho,
Recebei o nosso louvor,
Aceitai a nossa oração.

Unidos em vosso nome,
Partilhamos o mesmo pão.
Senhor, guiai os nossos passos
Para a vossa salvação.", "entrada"),

        new CanticoData("Cristo Venceu",
            @"Cristo venceu, Cristo reina,
Cristo, Cristo impera!
Ao Rei dos reis, ao Senhor dos senhores,
Glória e louvor eternamente!

Louvemos o Pai que nos criou,
Louvemos o Filho que nos salvou,
Louvemos o Espírito que nos santificou.
Cristo venceu, Cristo reina!

A paz de Cristo que o mundo não pode dar,
A paz de Cristo em cada coração.
Venha o teu reino, seja feita a tua vontade,
Na terra como nos céus.", "entrada"),

        // ── ENTRONIZAÇÃO DA PALAVRA ────────────────────────────────────────
        new CanticoData("Aclamai a Palavra de Deus",
            @"Aclamai a Palavra de Deus,
Luz que ilumina o nosso caminho.
É lâmpada para os nossos pés,
Clarão para a nossa estrada.

Palavra viva e eficaz,
Mais cortante que espada de dois gumes.
Ela penetra até à divisão
Da alma, do espírito, das juntas e da medula.

Abre, Senhor, os nossos olhos,
Que vejamos as maravilhas da tua lei.
Faz ressoar no coração
A tua voz que salva e consola.", "entronização da palavra"),

        new CanticoData("Que Sede Tenho de Ti",
            @"Que sede tenho de ti, Senhor,
Que sede da tua palavra!
Como a corça que procura a água,
Assim o meu ser te procura a ti.

Anseio por ti como terra seca e árida
Onde não há água.
Abre, Senhor, a tua Palavra,
Derrama sobre mim a tua luz.

Na tua Palavra encontro a vida,
Nela descubro o caminho.
Que sede tenho de ti, Senhor,
Sacia-me com a tua presença!", "entronização da palavra"),

        new CanticoData("A Luz da Tua Palavra",
            @"A luz da tua Palavra, Senhor,
Ilumina o meu caminho.
Faz brilhar sobre mim o teu rosto,
Ensina-me os teus mandamentos.

A tua lei é perfeita e restaura a alma,
O teu testemunho é fiel e instrui o simples.
Os teus preceitos são rectos e alegram o coração,
O teu mandamento é puro e ilumina os olhos.

Mais desejáveis que o ouro,
Mais doces que o mel.
A tua Palavra, ó Senhor,
É eterna nos céus.", "entronização da palavra"),

        // ── KYRIE ──────────────────────────────────────────────────────────
        new CanticoData("Senhor, Tende Piedade",
            @"Senhor, tende piedade,
Senhor, tende piedade,
Senhor, tende piedade de nós.

Cristo, tende piedade,
Cristo, tende piedade,
Cristo, tende piedade de nós.

Senhor, tende piedade,
Senhor, tende piedade,
Senhor, tende piedade de nós.

Kyrie eleison,
Christe eleison,
Kyrie eleison.", "kyrie"),

        new CanticoData("Misericórdia, Senhor",
            @"Misericórdia, Senhor,
Misericórdia.
Tende piedade de nós,
Pecadores.

Reconhecemos as nossas falhas,
Pedimos o vosso perdão.
Confiamos na vossa misericórdia,
Em vós está a nossa salvação.

Misericórdia, Senhor,
Misericórdia.
Lavai-nos de toda a culpa,
Purificai os nossos corações.", "kyrie"),

        new CanticoData("Senhor, Pecámos",
            @"Senhor, pecámos contra vós,
Tende misericórdia de nós.

Pela vossa infinita bondade,
Perdoai as nossas ofensas.
Pela vossa misericórdia,
Apagais os nossos pecados.

Criador do céu e da terra,
Não nos abandoneis.
Pai cheio de ternura e amor,
Acolhei os vossos filhos.

Senhor, pecámos contra vós,
Tende misericórdia de nós.", "kyrie"),

        // ── ALELUIA ────────────────────────────────────────────────────────
        new CanticoData("Aleluia, Louvai o Senhor",
            @"Aleluia, aleluia, aleluia!
Louvado seja o Senhor!
Aleluia, aleluia, aleluia!
Glória ao nosso Salvador!

Cantamos de alegria
A quem ressuscitou.
Cristo vive, Cristo reina,
Cristo nos libertou.

Aleluia, aleluia, aleluia!
A morte foi vencida!
Aleluia, aleluia, aleluia!
Cristo nos deu a vida!", "aleluia"),

        new CanticoData("Cantai Aleluia ao Senhor",
            @"Cantai aleluia ao Senhor,
Aleluia, aleluia!
Louvai o Deus da salvação,
Aleluia, aleluia!

Ele é ressuscitado,
Como havia prometido.
Aleluia, aleluia!
O Senhor é o nosso Deus.

Tomai a boa nova ao mundo inteiro:
Cristo está vivo!
Aleluia, aleluia!
Aleluia, louvai o Senhor!", "aleluia"),

        new CanticoData("Ressuscitou, Aleluia",
            @"Ressuscitou, aleluia!
Cristo ressuscitou!
Aleluia, aleluia!
A morte foi vencida!

As mulheres foram ao túmulo
Ao amanhecer do primeiro dia.
O anjo disse-lhes:
Não está aqui, ressuscitou!

Ide anunciar aos discípulos:
Cristo está vivo!
Aleluia, aleluia!
Aleluia, louvai o Senhor!", "aleluia"),

        // ── ORAÇÃO DOS FIÉIS ───────────────────────────────────────────────
        new CanticoData("Ouvi-nos, Senhor",
            @"Ouvi-nos, Senhor,
Escutai a nossa prece.
Na vossa misericórdia
Atendei o vosso povo.

Por aqueles que sofrem,
Por aqueles que estão longe,
Por aqueles que não vos conhecem,
Ouvi-nos, Senhor.

Por toda a Igreja,
Pelos nossos pastores,
Pelo mundo inteiro,
Ouvi-nos, Senhor.", "oração dos fiéis"),

        new CanticoData("Senhor, Escuta-nos",
            @"Senhor, escuta-nos,
Senhor, atende-nos,
Senhor, tem piedade de nós.

Pela Igreja de Cristo,
Pelos que governam as nações,
Pelos pobres e necessitados,
Senhor, escuta-nos.

Por aqueles que partiram,
Pelos que estão doentes,
Por todos os que sofrem,
Senhor, tem piedade de nós.", "oração dos fiéis"),

        new CanticoData("Escutai, Senhor, a Nossa Voz",
            @"Escutai, Senhor, a nossa voz,
Que sobe a vós em oração.
Somos o vosso povo,
Filhos pela adopção.

Por todos os que governam,
Pelos que servem na paz,
Pelos pobres e marginalizados,
Escutai, Senhor, o nosso clamor.

Pela unidade da Igreja,
Pelo encontro dos que se afastaram,
Pelos que buscam a verdade,
Escutai, Senhor, a nossa voz.", "oração dos fiéis"),

        // ── OFERTÓRIO ──────────────────────────────────────────────────────
        new CanticoData("Recebe, Senhor, as Nossas Oferendas",
            @"Recebe, Senhor, as nossas oferendas,
Este pão e este vinho
Que serão para nós
O pão da vida e o cálice da salvação.

Pelo trabalho humano,
Pela terra que frutifica,
Recebe, Senhor,
O fruto das nossas mãos.

Transformai-nos como transformais
Este pão e este vinho.
Que sejamos também nós
Oferenda santa e agradável a vós.", "ofertório"),

        new CanticoData("A Tua Mesa, Senhor",
            @"Preparo a tua mesa, Senhor,
Com o pão e o vinho da aliança.
Recebe a minha vida
Como oferenda de louvor.

Tudo o que tenho vem de ti,
Todo o bem que faço é teu.
Aceita este sacrifício
De um coração contrito e humilde.

A tua mesa, Senhor,
É mesa de perdão e vida.
Quem comer deste pão
Viverá para sempre.", "ofertório"),

        new CanticoData("Vós Que Neste Altar",
            @"Vós que neste altar
Vos tornais presente,
Recebei de nós
Esta oferta humilde.

Do trabalho e da esperança,
Da dor e da alegria,
Fazemos oblação
Da nossa vida inteira.

Com este pão e vinho,
Nós mesmos vos oferecemos.
Transformai nossa existência
No vosso amor eterno.", "ofertório"),

        new CanticoData("Eis a Oferenda",
            @"Eis a oferenda que trazemos,
Fruto da terra e do nosso trabalho.
Receba-a, Senhor, das nossas mãos,
Para a glória do vosso nome.

O pão que apresentamos
É fruto do trigo e do suor humano.
O vinho que oferecemos
É fruto da vinha e da arte humana.

Pela vossa palavra,
Tornar-se-ão o Corpo e o Sangue de Cristo.
Que esta oferta nos una
Ao único sacrifício do vosso Filho.", "ofertório"),

        // ── ELEVAÇÃO ───────────────────────────────────────────────────────
        new CanticoData("Adorai o Senhor",
            @"Adorai o Senhor,
Prostrai-vos diante dele.
O Senhor é santo,
Santo, Santo, Santo!

Aqui está o Corpo de Cristo,
Aqui está o Sangue do Senhor.
Adoramos o Mistério da Fé,
Anunciamos a vossa morte, Senhor,
Proclamamos a vossa ressurreição.

Glória ao Pai, ao Filho,
E ao Espírito Santo.
Como era no princípio,
É agora e sempre será.", "elevação"),

        new CanticoData("Eis o Mistério da Fé",
            @"Anunciamos a vossa morte, Senhor,
Proclamamos a vossa ressurreição.
Vinde, Senhor Jesus,
Vinde na vossa glória!

Eis o Mistério da fé:
Cristo morreu, Cristo ressuscitou,
Cristo voltará!

Este é o pão vivo descido do céu.
Quem comer deste pão viverá para sempre.
O pão que eu darei é a minha carne
Para a vida do mundo.", "elevação"),

        new CanticoData("O Pão da Vida Eterna",
            @"O pão da vida eterna
Aqui se torna presente.
O cálice da nova aliança
Derrama sobre nós a sua graça.

Mysterium fidei,
Mistério da nossa fé!
Cristo ofereceu-se a si mesmo,
Sacrifício de amor perfeito.

Prostrados em adoração,
Reconhecemos o teu amor.
Jesus, pão da vida,
Fica sempre connosco.", "elevação"),

        // ── SANTO ──────────────────────────────────────────────────────────
        new CanticoData("Santo, Santo, Santo é o Senhor",
            @"Santo, Santo, Santo é o Senhor, Deus do universo!
O céu e a terra estão cheios da vossa glória.
Hosana nas alturas!
Bendito o que vem em nome do Senhor.
Hosana nas alturas!

Santo és tu, Senhor, fonte de toda a santidade.
Santificados por ti,
Oferecemos este sacrifício.
Hosana nas alturas!

Grande és tu, Senhor, e digno de louvor.
Os anjos te aclamam sem cessar:
Santo, Santo, Santo!
O Senhor Deus do universo.", "santo"),

        new CanticoData("Hosana ao Rei",
            @"Hosana, hosana, hosana ao Rei!
Hosana, hosana, hosana ao Rei!

Bendito o que vem em nome do Senhor!
Bendito o que vem em nome do Senhor!

O céu e a terra proclamam a tua glória!
Santo é o teu nome, Santo é o teu nome!

Hosana nas alturas,
Hosana nas alturas,
Hosana ao Rei dos reis!", "santo"),

        new CanticoData("Cheios Estão os Céus",
            @"Cheios estão os céus e a terra
Da vossa glória, Senhor!
Os Serafins e os Querubins
Cantam sem cessar:

Santo, Santo, Santo é o Senhor,
O Deus dos exércitos!
Os céus e a terra estão repletos
Da sua glória!

Hosana no mais alto dos céus!
Bendito o que vem em nome do Senhor!
Hosana no mais alto dos céus!", "santo"),

        // ── SAUDAÇÃO ───────────────────────────────────────────────────────
        new CanticoData("A Paz do Senhor",
            @"A paz do Senhor esteja convosco.
E convosco o esteja também.

Dai-vos a paz, dai-vos a paz,
A paz que só Cristo pode dar.
Não como o mundo a dá,
Mas a paz do coração.

Unidos pela paz de Cristo,
Partilhamos o mesmo pão.
Somos um só corpo em Cristo,
A sua Igreja no mundo.", "saudação"),

        new CanticoData("Sinal de Paz",
            @"Dá-me a tua mão, irmão,
No sinal de paz do Senhor.
Somos filhos do mesmo Pai,
Unidos no amor.

Cristo nos reuniu
À volta da mesma mesa.
Antes de partilhar o pão,
Reconciliemo-nos.

A paz do Senhor,
A paz que o mundo não pode dar,
Seja entre nós
Sinal de amor.", "saudação"),

        new CanticoData("Deixo-vos a Minha Paz",
            @"Deixo-vos a minha paz,
A minha paz vos dou.
Não como o mundo a dá,
Mas a paz do coração.

Que os vossos corações
Não se perturbem nem se assustem.
Confiai em Deus,
Confiai também em mim.

A paz de Cristo que supera
Todo o entendimento humano
Guarde os vossos corações
E os vossos pensamentos.", "saudação"),

        // ── CORDEIRO DE DEUS ───────────────────────────────────────────────
        new CanticoData("Cordeiro de Deus",
            @"Cordeiro de Deus,
Que tirais o pecado do mundo,
Tende piedade de nós.

Cordeiro de Deus,
Que tirais o pecado do mundo,
Tende piedade de nós.

Cordeiro de Deus,
Que tirais o pecado do mundo,
Dai-nos a paz.

Agnus Dei, qui tollis peccata mundi,
Miserere nobis.
Agnus Dei, qui tollis peccata mundi,
Dona nobis pacem.", "cordeiro de deus"),

        new CanticoData("Ó Cordeiro de Deus",
            @"Ó Cordeiro de Deus,
Que carregastes os nossos pecados,
Tende piedade de nós.

Tu que foste imolado
Para nos dar a vida,
Tende piedade de nós.

Tu que estás sempre vivo
À direita do Pai,
Dai-nos a tua paz.

Ó Cordeiro de Deus,
Salvador do mundo,
Dai-nos a paz do coração.", "cordeiro de deus"),

        new CanticoData("Bem-aventurados os Convidados",
            @"Bem-aventurados os convidados
Para a ceia do Senhor.
Felizes os que são chamados
À mesa do Cordeiro.

O Cordeiro que foi imolado
É digno de receber
O poder, a riqueza, a sabedoria,
A força, a honra, a glória e o louvor.

Ao que está sentado no trono
E ao Cordeiro
Sejam o louvor e a honra,
A glória e o poder pelos séculos dos séculos.", "cordeiro de deus"),

        // ── COMUNHÃO ───────────────────────────────────────────────────────
        new CanticoData("Vinde, Comei",
            @"Vinde, comei o pão da vida,
Bebei o cálice da salvação.
Cristo nos convida à sua mesa,
Vinde receber a comunhão.

Pão descido do céu,
Pão partido para nós.
Quem comer deste pão
Nunca mais terá fome.

Vinde, comei o pão da vida,
Vinde, bebei o vinho da aliança.
A quem tem sede, Cristo oferece
A fonte da salvação.", "comunhão"),

        new CanticoData("Tu És o Pão da Vida",
            @"Tu és o pão da vida,
O que vem do céu.
Quem comer deste pão
Viverá para sempre.

Tu és a fonte de água viva,
Quem beber não terá sede.
Quem acreditar em ti
Terá a vida eterna.

Bendito és tu, Senhor Deus,
Que nos dás este pão.
Fruto da terra e do trabalho humano,
Que se torna o Corpo de Cristo.", "comunhão"),

        new CanticoData("Pão do Céu",
            @"Pão do céu, pão de vida,
Pão que dás a vida ao mundo.
Somos o teu povo, Senhor,
Nutre-nos com o teu amor.

Quem come deste pão
Nunca mais terá fome.
Quem bebe desta água
Nunca mais terá sede.

Vem, Senhor Jesus,
Vem habitar em nós.
Faze de nós o teu corpo,
A tua Igreja no mundo.", "comunhão"),

        new CanticoData("Junto à Tua Mesa",
            @"Junto à tua mesa, Senhor,
Somos convidados ao banquete.
Pobres e pecadores,
Acolheis-nos com amor.

Este pão que partimos,
Este cálice que bebemos,
São o vosso Corpo e Sangue,
Penhor da nossa salvação.

Obrigado, Senhor,
Por este dom precioso.
Que a comunhão nos una
A vós e entre nós.", "comunhão"),

        new CanticoData("O Senhor É o Meu Pastor",
            @"O Senhor é o meu pastor, nada me faltará.
Em verdes prados me faz repousar.
Para águas tranquilas me conduz,
Restaura as forças da minha alma.

Pelos caminhos retos me guia,
Por amor do seu nome.
Ainda que eu atravesse o vale da morte,
Nada temerei, pois estás comigo.

Prepara uma mesa diante de mim,
Em frente aos meus adversários.
Unge a minha cabeça com óleo,
E o meu cálice transborda.

A tua bondade e misericórdia
Hão-de acompanhar-me todos os dias da minha vida.
E eu habitarei na casa do Senhor
Por todos os dias sem fim.", "comunhão"),

        new CanticoData("Que Alegria Quando Me Disseram",
            @"Que alegria quando me disseram:
Vamos à casa do Senhor!
Que alegria no meu coração,
Alegria e louvor!

Eis que os nossos pés já estão
Nas tuas portas, Jerusalém!
A Jerusalém construída
Como cidade bem compacta.

Para lá sobem as tribos,
As tribos do Senhor.
Conforme o que foi prescrito a Israel,
Para louvar o nome do Senhor.

Pediai a paz de Jerusalém,
Vivam em paz os que te amam.", "comunhão"),

        // ── ACÇÃO DE GRAÇAS ────────────────────────────────────────────────
        new CanticoData("Obrigado, Senhor",
            @"Obrigado, Senhor,
Pelo dom da vida.
Obrigado pelo teu amor,
Pela tua misericórdia.

Obrigado pelo pão da vida
Que nos deste hoje.
Obrigado por teres ficado
Connosco neste sacramento.

Agora vamos ao mundo
Para vos dar o testemunho.
Obrigado, Senhor,
Por nos enviares em missão.", "acção de graças"),

        new CanticoData("Louvado Seja o Senhor",
            @"Louvado seja o Senhor,
Agora e para sempre.
O Senhor é bom,
O seu amor é eterno.

Ele nos alimentou
Com o pão do céu.
Ele nos saciou
Com as águas da vida.

Louvado seja o Senhor,
Que habita em nós.
Louvado seja o Senhor,
Que nos faz o seu povo.", "acção de graças"),

        new CanticoData("Magnificat",
            @"A minha alma engrandece o Senhor,
E o meu espírito se alegra em Deus, meu salvador.
Porque atendeu à humildade da sua serva,
E por isso me chamarão bem-aventurada todas as gerações.

O Poderoso fez grandes coisas em mim,
Santo é o seu nome.
A sua misericórdia vai de geração em geração
Para aqueles que o temem.

O Senhor praticou proezas com o seu braço,
Dispersou os que se orgulham no coração.
Derrubou os poderosos dos seus tronos
E exaltou os humildes.

Encheu de bens os que tinham fome
E despediu os ricos de mãos vazias.
Socorreu Israel, seu servo,
Lembrando-se da sua misericórdia.", "acção de graças"),

        new CanticoData("Bendito Sejas, Senhor",
            @"Bendito sejas, Senhor nosso Deus,
Pelo pão e pelo vinho.
Bendito sejas pelo teu Filho Jesus,
Que se nos dá neste sacramento.

A tua bondade, Senhor, não tem fim,
A tua misericórdia é eterna.
Tornados um em Cristo,
Voltamos ao mundo como seus enviados.

Bendito sejas, Senhor nosso Deus,
Agora e para sempre.
Aleluia, aleluia!", "acção de graças"),

        // ── SAÍDA ──────────────────────────────────────────────────────────
        new CanticoData("Ide pelo Mundo",
            @"Ide pelo mundo,
Anunciai o Evangelho.
Baptizai em nome do Pai,
Do Filho e do Espírito Santo.

Ide, a missa acabou,
Ide em paz anunciar Cristo.
A graça do Senhor vos acompanhe
Em todo o vosso caminho.

Sois luz do mundo,
Sois sal da terra.
Ide e testemunhai
Que Cristo está vivo!", "saída"),

        new CanticoData("Vós Sois a Luz do Mundo",
            @"Vós sois a luz do mundo,
Cidade no alto do monte.
Não se esconde a luz,
Brilha para todos.

Vós sois o sal da terra,
O fermento na massa.
Ide transformar o mundo
Com o amor de Cristo.

A missa acabou, ide em paz.
O Senhor vos acompanhe.
Anunciai que Cristo vive
Com a vossa vida.", "saída"),

        new CanticoData("A Nossa Missão",
            @"A nossa missão é amar,
Como Cristo amou.
A nossa missão é servir,
Como Cristo serviu.

Enviados ao mundo
Para ser o seu amor,
Para ser a sua presença
Em cada irmão.

Vai, não tenhas medo,
O Senhor vai contigo.
A sua graça é suficiente,
A sua força é perfeita.", "saída"),

        new CanticoData("O Senhor Vos Abençoe",
            @"O Senhor vos abençoe e vos guarde.
O Senhor faça brilhar o seu rosto sobre vós.
O Senhor volte os seus olhos para vós
E vos dê a paz.

Ide em paz,
Ide em missão,
Ide anunciar
O amor de Deus.

A graça do Senhor Jesus Cristo,
O amor de Deus Pai
E a comunhão do Espírito Santo
Estejam sempre convosco.", "saída"),

        new CanticoData("Testemunhas do Ressuscitado",
            @"Somos testemunhas do Ressuscitado,
Enviados para anunciar a boa nova.
Cristo está vivo! Cristo está entre nós!
Esta é a nossa alegria, esta é a nossa missão.

Ide pelo mundo inteiro,
Proclamai o Evangelho a toda a criatura.
O que ouvistes em vossas orelhas,
Anunciai-o dos terraços.

Não tenhais medo!
Eu estarei convosco todos os dias,
Até ao fim dos tempos.
Ide, o Senhor vai convosco!", "saída"),
    };
}
