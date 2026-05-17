using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;
using Microsoft.EntityFrameworkCore;

namespace MissaoBackend.Seeds;

public static class CanticoLatSeeder
{
    private static readonly string[] TopicoNomes =
    [
        "Procissão", "Entrada", "Entronização da Palavra", "Kyrie", "Aleluia",
        "Oração dos Fiéis", "Ofertório", "Elevação", "Santo", "Saudação",
        "Cordeiro de Deus", "Comunhão", "Acção de Graças", "Saída",
    ];

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.TopicosLat.AnyAsync()) return;

        // Criar tópicos (mesmos nomes que PT, conteúdo em Latim)
        var topicos = TopicoNomes.Select(nome => new TopicoLat
        {
            Nome = nome,
            Slug = SlugHelper.Slugify(nome),
        }).ToList();

        db.TopicosLat.AddRange(topicos);
        await db.SaveChangesAsync();

        var topicoMap = topicos.ToDictionary(t => t.Nome, t => t.Id);
        var canticos = GetCanticos()
            .Where(c => topicoMap.ContainsKey(c.Topico))
            .Select(c => new CanticoLat
            {
                Titulo = c.Titulo,
                Slug = SlugHelper.Slugify(c.Titulo),
                Letra = c.Letra,
                TopicoId = topicoMap[c.Topico],
            })
            .ToList();

        db.CanticosLat.AddRange(canticos);
        await db.SaveChangesAsync();
        Console.WriteLine($"✓ {topicos.Count} tópicos e {canticos.Count} cânticos Latim adicionados.");
    }

    private record CanticoData(string Titulo, string Letra, string Topico);

    private static List<CanticoData> GetCanticos() =>
    [
        // ── PROCISSÃO ─────────────────────────────────────────────────────
        new("Procedamus in Pace",
            @"Procedamus in pace.
In nomine Christi. Amen.

Procedamus, procedamus,
In nomine Domini.
Benedictus qui venit
In nomine Domini.

Hosanna in excelsis,
Hosanna Filio David.
Benedictus qui venit
In nomine Domini.", "Procissão"),

        new("Rorate Caeli",
            @"Rorate caeli desuper,
Et nubes pluant justum.

Ne irascaris, Domine,
Ne ultra memineris iniquitatis.
Ecce civitas Sancti facta est deserta,
Sion deserta facta est.

Rorate caeli desuper,
Et nubes pluant justum.
Aperiatur terra et germinet Salvatorem.", "Procissão"),

        new("Ecce Advenit Dominator",
            @"Ecce advenit dominator Dominus,
Et regnum in manu ejus et potestas et imperium.

Deus, judicium tuum regi da,
Et justitiam tuam filio regis.

Gloria Patri et Filio
Et Spiritui Sancto.
Sicut erat in principio,
Et nunc et semper
Et in saecula saeculorum. Amen.", "Procissão"),

        // ── ENTRADA ────────────────────────────────────────────────────────
        new("Introibo ad Altare Dei",
            @"Introibo ad altare Dei,
Ad Deum qui laetificat juventutem meam.

Judica me, Deus,
Et discerne causam meam
De gente non sancta.

Ab homine iniquo et doloso erue me.
Quia tu es, Deus, fortitudo mea:
Quare me repulisti?

Emitte lucem tuam et veritatem tuam,
Ipsa me deduxerunt,
Et adduxerunt in montem sanctum tuum.", "Entrada"),

        new("Gaudeamus Omnes",
            @"Gaudeamus omnes in Domino,
Diem festum celebrantes
Sub honore beatorum.

De quorum solemnitate
Gaudent angeli
Et collaudant Filium Dei.

Gloria Patri et Filio
Et Spiritui Sancto.
Sicut erat in principio,
Et nunc et semper
Et in saecula saeculorum. Amen.", "Entrada"),

        new("Jubilate Deo",
            @"Jubilate Deo omnis terra,
Servite Domino in laetitia.

Introite in conspectu ejus
In exsultatione.

Scitote quoniam Dominus ipse est Deus,
Ipse fecit nos, et non ipsi nos.
Populus ejus et oves pascuae ejus.

Laudate nomen ejus,
Quoniam suavis est Dominus,
In aeternum misericordia ejus.", "Entrada"),

        new("Adeste Fideles",
            @"Adeste fideles, laeti triumphantes,
Venite, venite in Bethlehem.
Natum videte Regem angelorum.

Venite adoremus,
Venite adoremus,
Venite adoremus Dominum.

Deum de Deo, Lumen de Lumine,
Gestant puellae viscera.
Deum verum, genitum non factum.

Venite adoremus,
Venite adoremus,
Venite adoremus Dominum.", "Entrada"),

        // ── ENTRONIZAÇÃO DA PALAVRA ────────────────────────────────────────
        new("Verbum Domini Manet in Aeternum",
            @"Verbum Domini manet in aeternum.
Verbum Domini manet in aeternum.

Non in solo pane vivit homo,
Sed in omni verbo quod procedit
Ex ore Dei.

Verbum Domini manet in aeternum.
Verbum Domini manet in aeternum.

Caelum et terra transibunt,
Verba autem mea non transibunt.

Verbum Domini manet in aeternum.", "Entronização da Palavra"),

        new("Lucerna Pedibus Meis",
            @"Lucerna pedibus meis verbum tuum,
Et lumen semitis meis.

Juro et statui
Custodire judicia justitiae tuae.

Humiliatus sum usquequaque:
Domine, vivifica me secundum verbum tuum.

Voluntaria oris mei beneplacita fac, Domine,
Et judicia tua doce me.

Lucerna pedibus meis verbum tuum,
Et lumen semitis meis.", "Entronização da Palavra"),

        // ── KYRIE ─────────────────────────────────────────────────────────
        new("Kyrie Eleison (Gregoriano)",
            @"Kyrie eleison.
Kyrie eleison.
Kyrie eleison.

Christe eleison.
Christe eleison.
Christe eleison.

Kyrie eleison.
Kyrie eleison.
Kyrie eleison.", "Kyrie"),

        new("Miserere Mei, Deus",
            @"Miserere mei, Deus,
Secundum magnam misericordiam tuam.

Et secundum multitudinem miserationum tuarum
Dele iniquitatem meam.

Amplius lava me ab iniquitate mea,
Et a peccato meo munda me.

Tibi soli peccavi,
Et malum coram te feci.

Cor mundum crea in me, Deus,
Et spiritum rectum innova in visceribus meis.", "Kyrie"),

        new("Asperges Me",
            @"Asperges me, Domine, hyssopo,
Et mundabor.
Lavabis me,
Et super nivem dealbabor.

Miserere mei, Deus,
Secundum magnam misericordiam tuam.

Gloria Patri et Filio
Et Spiritui Sancto.
Sicut erat in principio, et nunc et semper,
Et in saecula saeculorum. Amen.", "Kyrie"),

        // ── ALELUIA ────────────────────────────────────────────────────────
        new("Alleluia (Gregoriano)",
            @"Alleluia, alleluia, alleluia!

Laudate Dominum omnes gentes,
Laudate eum omnes populi.
Quoniam confirmata est super nos misericordia ejus,
Et veritas Domini manet in aeternum.

Alleluia, alleluia, alleluia!", "Aleluia"),

        new("Haec Dies",
            @"Haec dies quam fecit Dominus,
Exsultemus et laetemur in ea.

Confitemini Domino quoniam bonus,
Quoniam in saeculum misericordia ejus.

Haec dies quam fecit Dominus,
Exsultemus et laetemur in ea.

Alleluia, alleluia!
Confitemini Domino quoniam bonus.
Alleluia!", "Aleluia"),

        new("Victimae Paschali Laudes",
            @"Victimae paschali laudes
Immolent Christiani.

Agnus redemit oves,
Christus innocens Patri
Reconciliavit peccatores.

Mors et vita duello
Conflixere mirando:
Dux vitae mortuus regnat vivus.

Dic nobis, Maria, quid vidisti in via?
Sepulcrum Christi viventis
Et gloriam vidi resurgentis.

Credendum est magis soli
Mariae veraci
Quam Judaeorum turbae fallaci.

Scimus Christum surrexisse
A mortuis vere:
Tu nobis, victor Rex, miserere.", "Aleluia"),

        // ── ORAÇÃO DOS FIÉIS ───────────────────────────────────────────────
        new("Exaudi Nos, Domine",
            @"Exaudi nos, Domine,
Exaudi nos, Domine.

Pro Ecclesia sancta Dei,
Pro papa et episcopis,
Pro sacerdotibus et ministris.
Exaudi nos, Domine.

Pro pace inter gentes,
Pro pauperibus et infirmis,
Pro iis qui in errore versantur.
Exaudi nos, Domine.

Pro fidelibus defunctis,
Pro nobis omnibus.
Exaudi nos, Domine.", "Oração dos Fiéis"),

        new("Domine, Exaudi Orationem Meam",
            @"Domine, exaudi orationem meam,
Et clamor meus ad te veniat.

Deus, in adjutorium meum intende,
Domine, ad adjuvandum me festina.

Gloria Patri et Filio
Et Spiritui Sancto.

Sicut erat in principio, et nunc et semper,
Et in saecula saeculorum. Amen.", "Oração dos Fiéis"),

        // ── OFERTÓRIO ──────────────────────────────────────────────────────
        new("Suscipe, Sancte Pater",
            @"Suscipe, sancte Pater, omnipotens aeterne Deus,
Hanc immaculatam hostiam,
Quam ego, indignus famulus tuus,
Offero tibi Deo meo vivo et vero,
Pro innumerabilibus peccatis et offensionibus meis.

Deus, qui humanae substantiae dignitatem
Et mirabiliter condidisti
Et mirabilius reformasti,
Da nobis per hujus aquae et vini mysterium
Ejus divinitatis esse consortes.", "Ofertório"),

        new("Orate Fratres",
            @"Orate, fratres,
Ut meum ac vestrum sacrificium
Acceptabile fiat apud Deum Patrem omnipotentem.

Suscipiat Dominus sacrificium
De manibus tuis,
Ad laudem et gloriam nominis sui,
Ad utilitatem quoque nostram,
Totiusque Ecclesiae suae sanctae.", "Ofertório"),

        new("Offertorium — In Te Speravi",
            @"In te speravi, Domine:
Dixi: Tu es Deus meus.
In manibus tuis sortes meae.

Benedictus Dominus,
Quoniam mirificavit misericordiam suam mihi
In civitate munita.

Lucis aeternae consortium
Da nobis, Domine.
Et in bonis tuis exsultemus
In aeternum.", "Ofertório"),

        // ── ELEVAÇÃO ──────────────────────────────────────────────────────
        new("Tantum Ergo",
            @"Tantum ergo Sacramentum
Veneremur cernui,
Et antiquum documentum
Novo cedat ritui.
Praestet fides supplementum
Sensuum defectui.

Genitori Genitoque
Laus et jubilatio,
Salus, honor, virtus quoque
Sit et benedictio:
Procedenti ab utroque
Compar sit laudatio. Amen.", "Elevação"),

        new("O Salutaris Hostia",
            @"O salutaris Hostia,
Quae caeli pandis ostium:
Bella premunt hostilia,
Da robur, fer auxilium.

Uni trinoque Domino
Sit sempiterna gloria,
Qui vitam sine termino
Nobis donet in patria. Amen.", "Elevação"),

        new("Panis Angelicus",
            @"Panis angelicus fit panis hominum;
Dat panis caelicus figuris terminum.
O res mirabilis! manducat Dominum
Pauper, servilis et humilis.

Te trina Deitas unaque poscimus,
Sic nos tu visita, sicut te colimus;
Per tuas semitas duc nos quo tendimus,
Ad lucem quam inhabitas. Amen.", "Elevação"),

        // ── SANTO ──────────────────────────────────────────────────────────
        new("Sanctus (Gregoriano)",
            @"Sanctus, Sanctus, Sanctus
Dominus Deus Sabaoth.
Pleni sunt caeli et terra gloria tua.
Hosanna in excelsis.

Benedictus qui venit
In nomine Domini.
Hosanna in excelsis.", "Santo"),

        new("Gloria in Excelsis Deo",
            @"Gloria in excelsis Deo
Et in terra pax hominibus bonae voluntatis.

Laudamus te. Benedicimus te. Adoramus te. Glorificamus te.

Gratias agimus tibi propter magnam gloriam tuam.

Domine Deus, Rex caelestis, Deus Pater omnipotens.
Domine Fili unigenite, Jesu Christe.
Domine Deus, Agnus Dei, Filius Patris.

Qui tollis peccata mundi, miserere nobis.
Qui tollis peccata mundi, suscipe deprecationem nostram.
Qui sedes ad dexteram Patris, miserere nobis.

Quoniam tu solus Sanctus. Tu solus Dominus.
Tu solus Altissimus, Jesu Christe,
Cum Sancto Spiritu in gloria Dei Patris. Amen.", "Santo"),

        // ── SAUDAÇÃO ──────────────────────────────────────────────────────
        new("Pax Domini",
            @"Pax Domini sit semper vobiscum.
Et cum spiritu tuo.

Dona nobis pacem,
Dona nobis pacem tuam.

Non sicut mundus dat,
Pacem tuam da nobis, Domine.
Non turbentur corda nostra
Neque formidet.

Pax Domini sit semper vobiscum.", "Saudação"),

        new("Ubi Caritas",
            @"Ubi caritas et amor, Deus ibi est.
Congregavit nos in unum Christi amor.
Exsultemus et in ipso jucundemur.
Timeamus et amemus Deum vivum.
Et ex corde diligamus nos sincero.

Ubi caritas et amor, Deus ibi est.
Simul ergo cum in unum congregamur,
Ne nos mente dividamur caveamus.
Cessent jurgia maligna, cessent lites.
Et in medio nostri sit Christus Deus.

Ubi caritas et amor, Deus ibi est.", "Saudação"),

        // ── CORDEIRO DE DEUS ───────────────────────────────────────────────
        new("Agnus Dei (Gregoriano)",
            @"Agnus Dei, qui tollis peccata mundi,
Miserere nobis.

Agnus Dei, qui tollis peccata mundi,
Miserere nobis.

Agnus Dei, qui tollis peccata mundi,
Dona nobis pacem.", "Cordeiro de Deus"),

        new("Ecce Agnus Dei",
            @"Ecce Agnus Dei,
Ecce qui tollit peccata mundi.
Beati qui ad cenam Agni vocati sunt.

Agnus Dei, qui tollis peccata mundi,
Miserere nobis.
Agnus Dei, qui tollis peccata mundi,
Dona nobis pacem.", "Cordeiro de Deus"),

        // ── COMUNHÃO ──────────────────────────────────────────────────────
        new("Adoro Te Devote",
            @"Adoro te devote, latens Deitas,
Quae sub his figuris vere latitas:
Tibi se cor meum totum subjicit,
Quia te contemplans totum deficit.

Visus, tactus, gustus in te fallitur,
Sed auditu solo tuto creditur.
Credo quidquid dixit Dei Filius,
Nil hoc verbo veritatis verius.

O memoriale mortis Domini,
Panis vivus vitam praestans homini,
Praesta meae menti de te vivere,
Et te illi semper dulce sapere.", "Comunhão"),

        new("O Sacrum Convivium",
            @"O sacrum convivium,
In quo Christus sumitur,
Recolitur memoria passionis ejus,
Mens impletur gratia,
Et futurae gloriae
Nobis pignus datur. Alleluia.", "Comunhão"),

        new("Ave Verum Corpus",
            @"Ave verum corpus natum
De Maria Virgine,
Vere passum, immolatum
In cruce pro homine:

Cujus latus perforatum
Fluxit aqua et sanguine:
Esto nobis praegustatum
Mortis in examine.

O dulcis, o pie,
O Jesu Fili Mariae,
Miserere mei. Amen.", "Comunhão"),

        new("Gustate et Videte",
            @"Gustate et videte quoniam suavis est Dominus.
Beatus vir qui sperat in eo.

Benedicam Dominum in omni tempore,
Semper laus ejus in ore meo.
In Domino laudabitur anima mea,
Audiant mansueti et laetentur.

Gustate et videte quoniam suavis est Dominus.
Beatus vir qui sperat in eo.", "Comunhão"),

        // ── ACÇÃO DE GRAÇAS ────────────────────────────────────────────────
        new("Benedictus Dominus Deus",
            @"Benedictus Dominus Deus Israel,
Quia visitavit et fecit redemptionem plebis suae.

Et erexit cornu salutis nobis
In domo David pueri sui.

Ad dandam scientiam salutis plebi ejus
In remissionem peccatorum eorum.

Per viscera misericordiae Dei nostri,
In quibus visitavit nos oriens ex alto.
Gloria in excelsis Deo. Amen.", "Acção de Graças"),

        new("Nunc Dimittis",
            @"Nunc dimittis servum tuum, Domine,
Secundum verbum tuum in pace.

Quia viderunt oculi mei salutare tuum,
Quod parasti ante faciem omnium populorum.

Lumen ad revelationem gentium,
Et gloriam plebis tuae Israel.

Gloria Patri et Filio
Et Spiritui Sancto.
Sicut erat in principio, et nunc et semper,
Et in saecula saeculorum. Amen.", "Acção de Graças"),

        new("Te Deum Laudamus",
            @"Te Deum laudamus, te Dominum confitemur.
Te aeternum Patrem, omnis terra veneratur.
Tibi omnes angeli, tibi caeli et universae potestates.
Tibi cherubim et seraphim incessabili voce proclamant:
Sanctus, Sanctus, Sanctus Dominus Deus Sabaoth.

Pleni sunt caeli et terra majestatis gloriae tuae.
Te gloriosus apostolorum chorus,
Te prophetarum laudabilis numerus,
Te martyrum candidatus laudat exercitus.

Tu rex gloriae, Christe.
Tu Patris sempiternus es Filius.
Tu, ad liberandum suscepturus hominem, non horruisti Virginis uterum.", "Acção de Graças"),

        // ── SAÍDA ─────────────────────────────────────────────────────────
        new("Ite Missa Est",
            @"Ite, missa est.
Deo gratias.

In nomine Patris et Filii
Et Spiritus Sancti.
Amen.

Benedicat vos omnipotens Deus,
Pater et Filius et Spiritus Sanctus.
Amen.", "Saída"),

        new("Salve Regina",
            @"Salve, Regina, Mater misericordiae,
Vita, dulcedo et spes nostra, salve.
Ad te clamamus, exsules filii Hevae.
Ad te suspiramus, gementes et flentes
In hac lacrimarum valle.

Eia ergo, Advocata nostra,
Illos tuos misericordes oculos ad nos converte.
Et Jesum, benedictum fructum ventris tui,
Nobis post hoc exsilium ostende.

O clemens, o pia, o dulcis Virgo Maria.", "Saída"),

        new("Alma Redemptoris Mater",
            @"Alma Redemptoris Mater,
Quae pervia caeli porta manes,
Et stella maris, succurre cadenti,
Surgere qui curat, populo.

Tu quae genuisti, natura mirante,
Tuum sanctum Genitorem,
Virgo prius ac posterius,
Gabrielis ab ore sumens illud Ave,
Peccatorum miserere.", "Saída"),

        new("Sub Tuum Praesidium",
            @"Sub tuum praesidium confugimus,
Sancta Dei Genetrix.
Nostras deprecationes ne despicias
In necessitatibus nostris,
Sed a periculis cunctis libera nos semper,
Virgo gloriosa et benedicta.", "Saída"),
    ];
}
