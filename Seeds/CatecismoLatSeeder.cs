using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;
using Microsoft.EntityFrameworkCore;

namespace MissaoBackend.Seeds;

public static class CatecismoLatSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.CatecismoLatTopicos.AnyAsync()) return;

        var topicos = GetTopicos();
        foreach (var t in topicos)
        {
            t.Slug = SlugHelper.Slugify(t.Titulo);
            foreach (var e in t.CatecismosLat)
                e.Slug = SlugHelper.Slugify(e.Titulo);
        }

        db.CatecismoLatTopicos.AddRange(topicos);
        await db.SaveChangesAsync();
        Console.WriteLine($"✓ {topicos.Count} tópicos de catecismo Latim adicionados.");
    }

    private static List<CatecismoLatTopico> GetTopicos() =>
    [
        new CatecismoLatTopico
        {
            Titulo = "Preces Fundamentales",
            CatecismosLat =
            [
                new CatecismoLat
                {
                    Titulo = "Pater Noster",
                    Texto = "Pater noster, qui es in caelis,\nsanctificetur nomen tuum.\nAdveniat regnum tuum.\nFiat voluntas tua, sicut in caelo et in terra.\nPanem nostrum cotidianum da nobis hodie.\nEt dimitte nobis debita nostra,\nsicut et nos dimittimus debitoribus nostris.\nEt ne nos inducas in tentationem,\nsed libera nos a malo.\nAmen."
                },
                new CatecismoLat
                {
                    Titulo = "Ave Maria",
                    Texto = "Ave Maria, gratia plena,\nDominus tecum.\nBenedicta tu in mulieribus,\net benedictus fructus ventris tui, Iesus.\nSancta Maria, Mater Dei,\nora pro nobis peccatoribus,\nnunc et in hora mortis nostrae.\nAmen."
                },
                new CatecismoLat
                {
                    Titulo = "Gloria Patri",
                    Texto = "Gloria Patri et Filio\net Spiritui Sancto.\nSicut erat in principio,\net nunc et semper,\net in saecula saeculorum.\nAmen."
                },
                new CatecismoLat
                {
                    Titulo = "Symbolum Apostolorum",
                    Texto = "Credo in Deum Patrem omnipotentem,\nCreatorem caeli et terrae.\nEt in Iesum Christum, Filium eius unicum, Dominum nostrum:\nqui conceptus est de Spiritu Sancto,\nnatus ex Maria Virgine,\npassus sub Pontio Pilato,\ncrucifixus, mortuus, et sepultus;\ndescendit ad inferos;\ntertia die resurrexit a mortuis;\nascendit ad caelos;\nsedet ad dexteram Dei Patris omnipotentis;\ninde venturus est iudicare vivos et mortuos.\nCredo in Spiritum Sanctum,\nsanctam Ecclesiam catholicam,\nsanctorum communionem,\nremissionem peccatorum,\ncarnis resurrectionem,\nuitam aeternam.\nAmen."
                },
                new CatecismoLat
                {
                    Titulo = "Symbolum Nicaenum",
                    Texto = "Credo in unum Deum,\nPatrem omnipotentem,\nFactorem caeli et terrae,\nvisibilium omnium et invisibilium.\nEt in unum Dominum Iesum Christum,\nFilium Dei unigenitum,\net ex Patre natum ante omnia saecula.\nDeum de Deo, Lumen de Lumine,\nDeum verum de Deo vero,\ngenitum, non factum,\nconsubstantialem Patri;\nper quem omnia facta sunt.\nQui propter nos homines\net propter nostram salutem\ndescendit de caelis.\nEt incarnatus est de Spiritu Sancto\nex Maria Virgine,\net homo factus est.\nCrucifixus etiam pro nobis sub Pontio Pilato;\npassus et sepultus est.\nEt resurrexit tertia die,\nsecundum Scripturas.\nEt ascendit in caelum,\nsedet ad dexteram Patris.\nEt iterum venturus est cum gloria\niudicare vivos et mortuos;\ncuius regni non erit finis.\nEt in Spiritum Sanctum, Dominum et vivificantem,\nqui ex Patre Filioque procedit.\nQui cum Patre et Filio simul adoratur et conglorificatur;\nqui locutus est per prophetas.\nEt unam, sanctam, catholicam\net apostolicam Ecclesiam.\nConfiteor unum baptisma\nin remissionem peccatorum.\nEt expecto resurrectionem mortuorum,\net vitam venturi saeculi.\nAmen."
                },
                new CatecismoLat
                {
                    Titulo = "Confiteor",
                    Texto = "Confiteor Deo omnipotenti\net vobis, fratres,\nquia peccavi nimis\ncogitatio, verbo, opere et omissione:\nmea culpa, mea culpa,\nmea maxima culpa.\nIdeo precor beatam Mariam semper Virginem,\nomnes Angelos et Sanctos,\net vos, fratres,\norare pro me\nad Dominum Deum nostrum."
                },
                new CatecismoLat
                {
                    Titulo = "Sanctus",
                    Texto = "Sanctus, Sanctus, Sanctus\nDominus Deus Sabaoth.\nPleni sunt caeli et terra gloria tua.\nHosanna in excelsis.\nBenedictus qui venit in nomine Domini.\nHosanna in excelsis."
                },
                new CatecismoLat
                {
                    Titulo = "Agnus Dei",
                    Texto = "Agnus Dei, qui tollis peccata mundi,\nmiserere nobis.\nAgnus Dei, qui tollis peccata mundi,\nmiserere nobis.\nAgnus Dei, qui tollis peccata mundi,\ndona nobis pacem."
                },
                new CatecismoLat
                {
                    Titulo = "Salve Regina",
                    Texto = "Salve, Regina, Mater misericordiae,\nvita, dulcedo et spes nostra, salve.\nAd te clamamus, exsules filii Hevae.\nAd te suspiramus, gementes et flentes\nin hac lacrimarum valle.\nEia ergo, Advocata nostra,\nillos tuos misericordes oculos ad nos converte.\nEt Iesum, benedictum fructum ventris tui,\nnobis post hoc exsilium ostende.\nO clemens, o pia, o dulcis Virgo Maria."
                },
                new CatecismoLat
                {
                    Titulo = "Sub Tuum Praesidium",
                    Texto = "Sub tuum praesidium confugimus,\nSancta Dei Genetrix.\nNostras deprecationes ne despicias\nin necessitatibus nostris,\nsed a periculis cunctis libera nos semper,\nVirgo gloriosa et benedicta."
                },
            ]
        },
        new CatecismoLatTopico
        {
            Titulo = "Compendium Fidei",
            CatecismosLat =
            [
                new CatecismoLat
                {
                    Titulo = "Quis est homo?",
                    Texto = "Homo est creatura a Deo ad imaginem et similitudinem suam condita, ad cognoscendum, amandum et serviendum Deo in hac vita et in altera cum eo feliciter vivendum."
                },
                new CatecismoLat
                {
                    Titulo = "Cur homo creatus est?",
                    Texto = "Deus hominem creavit ut eum cognosceret, amaret et ei serviret, atque per haec ad vitam aeternam perveniret. Ad hoc finem Deus hominem in statu iustitiae originalis constituit."
                },
                new CatecismoLat
                {
                    Titulo = "Quid est Deus?",
                    Texto = "Deus est Ens perfectissimum, aeternum, omnipotens, omnisciens, iustissimus, misericordissimus — Creator et Dominus omnium rerum. Ipse dixit Moysi: 'Ego sum qui sum.'"
                },
                new CatecismoLat
                {
                    Titulo = "Quid est fides?",
                    Texto = "Fides est virtus theologica qua credimus vera esse omnia quae Deus revelavit et Ecclesia nobis proponit credenda, quia Deus, qui neque falli neque fallere potest, haec revelavit."
                },
                new CatecismoLat
                {
                    Titulo = "Quid est spes?",
                    Texto = "Spes est virtus theologica qua beatitudinem aeternam et gratiam ad eam obtinendam a Deo expectamus, innixis promissionibus Christi et auxilio Spiritus Sancti."
                },
                new CatecismoLat
                {
                    Titulo = "Quid est caritas?",
                    Texto = "Caritas est virtus theologica qua Deum super omnia propter se ipsum diligimus, et proximum nostrum sicut nos ipsos propter amorem Dei."
                },
                new CatecismoLat
                {
                    Titulo = "Quid est peccatum?",
                    Texto = "Peccatum est dictum, factum vel desiderium contra legem aeternam Dei. Est offensa in Deum, ruptura communionis cum Eo. Distinguuntur peccata mortalia et venialia."
                },
                new CatecismoLat
                {
                    Titulo = "Quid est sacramentum?",
                    Texto = "Sacramentum est signum efficax gratiae, a Christo institutum et Ecclesiae commissum, per quod vita divina nobis confertur. Septem sunt sacramenta Novae Legis."
                },
                new CatecismoLat
                {
                    Titulo = "Quae sunt septem sacramenta?",
                    Texto = "Septem sacramenta sunt: Baptismus, Confirmatio, Eucharistia, Paenitentia, Unctio Infirmorum, Ordo, et Matrimonium. Omnia a Christo instituta sunt et gratiam conferunt."
                },
                new CatecismoLat
                {
                    Titulo = "Quid est Eucharistia?",
                    Texto = "Eucharistia est sacramentum in quo sub speciebus panis et vini vere, realiter et substantialiter continetur Corpus et Sanguis, una cum Anima et Divinitate Domini nostri Iesu Christi — et per consequens Christus totus."
                },
                new CatecismoLat
                {
                    Titulo = "Quid est oratio?",
                    Texto = "Oratio est elevatio mentis et cordis ad Deum, sive ad petendum bona, sive ad reddendum ei gratias et laudes. Optima omnium orationum est Pater Noster, a Christo ipso tradita."
                },
                new CatecismoLat
                {
                    Titulo = "Quot sunt praecepta Decalogi?",
                    Texto = "Praecepta Decalogi decem sunt:\n1. Non habebis deos alienos coram me.\n2. Non assumes nomen Domini Dei tui in vanum.\n3. Memento ut diem sabbati sanctifices.\n4. Honora patrem tuum et matrem tuam.\n5. Non occides.\n6. Non moechaberis.\n7. Non furtum facies.\n8. Non loqueris contra proximum tuum falsum testimonium.\n9. Non concupisces uxorem proximi tui.\n10. Non concupisces domum proximi tui."
                },
            ]
        },
    ];
}
