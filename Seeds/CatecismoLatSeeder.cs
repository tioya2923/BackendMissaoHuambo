using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;
using Microsoft.EntityFrameworkCore;

namespace MissaoBackend.Seeds;

public static class CatecismoLatSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.CatecismoLatTopicos.AnyAsync())
        {
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

        // Adicionar orações em falta ao tópico Preces Fundamentales
        await AddMissingPrayersAsync(db);
    }

    private static async Task AddMissingPrayersAsync(AppDbContext db)
    {
        var topico = await db.CatecismoLatTopicos
            .FirstOrDefaultAsync(t => t.Titulo == "Preces Fundamentales");
        if (topico == null) return;

        var existingSlugs = (await db.CatecismosLat
            .Where(e => e.CatecismoLatTopicoId == topico.Id)
            .Select(e => e.Slug)
            .ToListAsync()).ToHashSet();

        var novos = GetPrecesAdicionais()
            .Select(p => new { Data = p, Slug = SlugHelper.Slugify(p.Titulo) })
            .Where(x => !existingSlugs.Contains(x.Slug))
            .Select(x => new CatecismoLat
            {
                Titulo = x.Data.Titulo,
                Slug = x.Slug,
                Texto = x.Data.Texto,
                CatecismoLatTopicoId = topico.Id,
            })
            .ToList();

        if (novos.Count == 0) return;

        db.CatecismosLat.AddRange(novos);
        await db.SaveChangesAsync();
        Console.WriteLine($"✓ {novos.Count} orações Latim adicionadas.");
    }

    private record OracaoData(string Titulo, string Texto);

    private static List<OracaoData> GetPrecesAdicionais() =>
    [
        new("Signum Crucis",
            "Per signum Crucis de inimícis nostris líbera nos Deus noster.\nIn nómine Patris, et Fílii, et Spíritus Sancti.\nAmen."),

        new("Actus Contritionis",
            "Deus meus, ex toto corde paénitet me ómnium meórum peccatórum,\neáque detéstor, qui peccándo, non solum poenas a te iuste statútas proméritus sum,\nsed praesértim quia offéndi te, summum bonum, ac dignum qui super ómnia diligáris.\nIdeo firmiter propóno, adiuvánte grátia tua,\nde cétero me non peccatúrum peccandíque occasiónes próximas fugitúrum.\nAmen."),

        new("O Domina Mea",
            "O Domina mea! O Mater mea!\nTibi me totum offero,\natque, ut me tibi probem devotum,\nconsecro tibi hodie oculos meos, aures meas, os meum, cor meum, plane me totum.\nQuoniam itaque tuus sum, o bona Mater,\nserva me, defende me ut rem ac possessionem tuam.\nAmen."),

        new("Memorare",
            "Memorare, o piisima Virgo Maria,\nnon esse auditum a saeculo,\nquemquam ad tua currentem praesidia,\ntua implorantem auxilia,\ntua petentem suffragia esse derelicta.\nNos tali animati confidentia ad te, Virgo Virginum, Mater, currimus;\nad te venimus; coram te gementes peccatores assistimus.\nNoli, Mater Verbi, verba nostra despicere,\nsed audi propitia et exaudi.\nAmen."),

        new("Veni Sancte Spiritus",
            "Veni Sancte Spíritus,\nreple tuórum corda fidélium,\net tu amóris in eis ignem accénde.\nEmítte Spíritum tuum et creabúntur.\nEt renovábis faciem terrae.\nOremus: Deus, qui corda fidélium\nSancti Spíritus illustratióne docuisti,\nda nobis in eódem Spíritu recta sápere,\net de ejus semper consolatióne gaudére.\nPer Christum Dóminum nostrum.\nAmen."),

        new("Angele Dei",
            "Angele Dei, qui custos es mei,\nme, tibi commissum pietate superna,\nhodie illúmina, custódi, rege et gubérna.\nAmen."),

        new("Sancte Michael Archangele",
            "Sancte Michael Archangele,\ndefende nos in praelio,\ncontra nequitias et insidias diaboli esto praesidium.\nImperet illi Deus, supplices deprecamur,\ntuque, Princeps militiae caelestis,\nsatanam aliosque spiritus malignos,\nqui ad perditionem animarum pervagantur in mundo,\ndivina virtute in infernum detrude.\nAmen."),

        new("Domine, Non Sum Dignus",
            "Domine, non sum dignus, ut intres sub tectum meum:\nsed tantum dic verbo, et sanabitur anima mea."),

        new("Anima Christi",
            "Anima Christi, sanctífica me.\nCorpus Christi, salva me.\nSanguis Christi, inebria me.\nAqua láteris Christi, lava me.\nPássio Christi, confórta me.\nO boné Iesu, exáudi me.\nIntra tua vulnera abscónde me.\nNe permíttas me separári a te.\nAb hoste maligno defende me.\nIn hora mortis meae voca me.\nEt iube me veníre ad te,\nut cum Sanctis tuis laudem te\nin saécula saeculórum.\nAmen."),

        new("Oratio Fatimae",
            "Domine Iesu, dimitte nobis debita nostra,\nsalva nos ab igne inferiori,\nperduc in caelum omnes animas,\npraesertim eas, quae misericordiae tuae maxime indigent."),

        new("Requiem Aeternam",
            "Requiem aeternam dona eis, Domine.\nEt lux perpetua luceat eis.\nFidelium animae, per misericordiam Dei,\nrequiescant in pace.\nAmen."),

        new("Actus Fidei",
            "Dómine Deus, firma fide credo et confiteor\nómnia et síngula quae Sancta Ecclésia Cathólica propónit,\nquia tu, Deus, ea ómnia revelásti,\nqui es aetérna véritas et sapiéntia quae nec fállere nec falli potest.\nIn hac fide vívere et mori státuo.\nAmen."),

        new("Actus Spei",
            "Dómine Deus, spero per grátiam tuam\nremissiónem ómnium peccatórum,\net post hanc vitam aetérnam felicitátem me esse consecutúrum:\nquia tu promisísti, qui es infiníte potens, fidélis, benígnus, et miséricors.\nIn hac spe vívere et mori státuo.\nAmen."),

        new("Actus Caritatis",
            "Dómine Deus, amo te super ómnia\net próximum meum propter te,\nquia tu es summum, infinítum, et perfectíssimum bonum,\nomni dilectióne dignum.\nIn hac caritáte vívere et mori státuo.\nAmen."),

        new("Vade Retro Satana",
            "Crux Sacra Sit Mihi Lux.\nNon Draco Sit Mihi Dux.\nVade Retro Sátana,\nNunquam Suade Mihi Vana.\nSunt Mala Quae Libas,\nIpse Venena Bibas."),
    ];

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
