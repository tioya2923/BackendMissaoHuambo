using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;
using Microsoft.EntityFrameworkCore;

namespace MissaoBackend.Seeds;

/// <summary>
/// Ordem da Missa (Omisa) em Otchikwama, a partir do documento fornecido
/// pela missão. Não inclui a secção de cânticos do mesmo documento
/// ("Omaimbilo"), que está explicitamente citada como extraída do livro
/// "Hinos e Salmos" (comp. Medeiros J.) — obra de terceiros.
/// </summary>
public static class CatecismoOtcSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var topico = await db.CatecismoOtcTopicos
            .FirstOrDefaultAsync(t => t.Titulo == "Omisa (Ordem da Missa)");

        if (topico == null)
        {
            topico = new CatecismoOtcTopico
            {
                Titulo = "Omisa (Ordem da Missa)",
                Slug = SlugHelper.Slugify("Omisa (Ordem da Missa)"),
            };
            db.CatecismoOtcTopicos.Add(topico);
            await db.SaveChangesAsync();
            Console.WriteLine("✓ Tópico 'Omisa (Ordem da Missa)' criado (Catecismo Otchikwama).");
        }

        var existingSlugs = (await db.CatecismosOtc
            .Where(c => c.CatecismoOtcTopicoId == topico.Id)
            .Select(c => c.Slug)
            .ToListAsync()).ToHashSet();

        var novos = new List<CatecismoOtc>();
        foreach (var (titulo, texto) in Entradas)
        {
            var slug = SlugHelper.Slugify(titulo);
            if (existingSlugs.Contains(slug)) continue;

            novos.Add(new CatecismoOtc
            {
                Titulo = titulo,
                Texto = texto,
                Slug = slug,
                CatecismoOtcTopicoId = topico.Id,
            });
            existingSlugs.Add(slug);
        }

        if (novos.Count == 0) return;

        db.CatecismosOtc.AddRange(novos);
        await db.SaveChangesAsync();
        Console.WriteLine($"✓ {novos.Count} conteúdos da Ordem da Missa (Otchikwama) adicionados.");
    }

    private static readonly List<(string Titulo, string Texto)> Entradas = new()
    {
        new(@"Orações Iniciais", @"Orações iniciais = Omaindilo opokutameka
Op.: Medina la Khe no l’Omona no l’Omepo Iyapuki.
Ov.: Amen.
Op.: Osali sOmwene wetu Jesu Kristu, ohole ya Khe n’oulinumwe
wOmepo Iyapuki naikale nanye.
Ov.: Nafimanekwe Kalunga ou etuongela mobole ya Kristu.
O.: Vamwameme, tudimbulukeni omatimba etu, opo tufewele okudana
oikumbikwa iyapuki.
                           (silêncio - ediladilo)
Tuliveleni hano ombedi yomatimba etu:
Op.: + Ov.: Ohandidiladila ku Kalunga, adula, aise nokunye,
vamwameme, sasi ondanyona luhapu m’omatil’adilo no mendaka no
moilonga no mokuhawanifa: etimba lange, etimba lange linene. Oso
handindile virgem Maria neandyu nesandu na nye, vamwameme,
muindililenge ku Kalunga, Omwene wetu.
Op.: Kalunga adula aise, netufil’onhenda, netuyavelele omatimba etu, ye
netufikife komwenyo itaupu.
Ov.: Athen."),
        new(@"Kyrie", @"Op.: Mwene, tufil’onhenda Ov.: Mwene, tufil’onhenda
Op.: Kristu, tufil’onhenda Ov.: Kristu, tufil’onhenda
Op.: Mwene, tufil’onhenda Ov.: Mwene, tufil’onhenda"),
        new(@"Glória", @"P.:- Kalunga, natumbalekwe pombadambada.
OV:- Nombili posi kovanu ovaholike vOmwene. Ohatukuhambelele,
ohatukutumbaleke, ohatu linyongamene ku Ove, ohatukufimaneke,
ohatukupandula, moluetumbalo loye linene. Mwene Kalunga, Hamba yo
meulu, Kalunga Tate udula aise. Mwene, Mona wa Kalunga Ewifa, Yesu
Kristu. Mwene Kalunga, Dyona ya Kalunga, Mona wa Kalunga Khe. Ove
ou tokufapo omatimba ounyuni, tufila onhenda. Ove ou tokufapo
omatimba ounyuni, puilikina eindilo letu. Ove ou uli omtumba kolulyo la
Kho, tufila onhenda. Sasi oove auke Omuyapuki, oove auke Omwene,
oove auke Omunene, Yesu Kristu, nOmepo Iyapuki, me tumbaleko la
Kho Kalunga. Amen"),
        new(@"Oração, Leituras e Evangelho", @"Op.: tuindileni . . . . . . Molwomwene wetu Jesu Kristu, Omona woye,
moulimumwe wOmepo Iyapuki.
Ov.: Amen.
Leituras:
Om.: Omukanda wa . . . . . . Ondaka yomwene.
Ov.: Tupanduleni Kalunga.
Evangelho:
Op.: Omwene nakale na nye.
Ov.: Omwene okuli mokati ketu.
Op.: Evangelyu lOmwene wetu Jesu Kristu, lasangwa ku . . .
Ov.: Mwene tumbalekwa.
..............
Op.: Ondaka yekhupifo.
Ov.: Mwene tumbalekwa."),
        new(@"Credo da Missa", @"P:- NDAITAVELA MUKALUNGA UMWE AEKE.
P no OV: Khe adula aise, omusiti wEulu nEdu noinima aise iwetike nai
ihewetike. Ondaitavela mOmwene umwe aeke Yesu Kristu, Omona wa
Kalunga Ewifa adalwa ku Khe ketetekelo loinima aise; Kalunga adya mu
Kalunga, Ouyelele wadya mOuyelele, Kalunga kosili adya mu Kalunga
kosili, adalwa nde inasitwa, osifete na Khe ou asita oinima aise. Nde
moluetu fye ovanu, no moluekhupifo letu, okwadya mEulu nde
tatambula olutu menhono dOmepo Iyapuki medimo la Virgem Maria,
nde teliningi omunu. Moluetu avalelwa komusiyakano, menhono da
Ponsiu Pilatus, afya ndele tapakwa, mefiku letitatu okwanyumuka
movafi ngasi sasangwa, alonda kEulu oku eli omutumba kolulyo la Khe
Kalunga. Otekeuyulula, eyadi etumbaleko oso ahokololife ovanamwenyo
novafi, ndele ou-
[continuação do Credo]
hamba waye itaukakhulapo. Ondaitavela mOmhepo Iyapuki, omuyandyi
wOmwenyo, ou adya mu Khe no mOmona, ou telinyongamenwa ye
tatumbalekwa kumwe na Khe nOmona. Oye apopya mOvakuhunganeki.
Ondaitavela mOngeleya imwe aike Iyapuki, Katolika yOvapostolu.
Ondaitavela mOmbatismu imwe aike yokuyavelela omatimba, ndele
ondateelela enyumuko lomalutu nomwenyo ou taukeuya. Amen.
Oração dos fieis: . . . .
Op.: Omwene nakale na nye.
Ov.: Omwene okuli mokati ketu.
Op.: Vamwameme, tuindileni ku Kalunga Tate adula aise molweindilo
lomwene wetu Jesu Kristu:




Om.: tuindilileni Ongeleya katolika iyapuki, oso Omwene eipe ombili
nou likumwe mokuongela ovanu avese po Altar yaye.
Ov.: Mwene, tuuda.
Op.: Kalunga, Ove epopilo letu, Ove enhono detu, molwonhenda yoye
tuningila esi hatukuidile neitavelo. Molwa Kristu, Omwene wetu.
Ov.: Amen.
Ofertorio:
Op.: Vamwameme, indileni oso okhula yetweni iitavelwe ku Kalunga,
Tate adula aise.
Ov.: Omwene natambule okhula ei pomake oye, sininge efimaneko
netumbaleko ledina laye nouwa wetu no wOngeleya yaYe iyapuki aise.
Oração Eucaristica:
Op.: Omwene nakale na nye.
Ov.: Omwene okuli mokati ketu.
Op.: Omitima naiyeluke.
Ov.: Otweiyelula nokuli kOmwene.
Op.: Tupanduleni Omwene Kalunga ketu.
Ov.: Osetuwapalela, so ositukhupifa."),
        new(@"Prefácio do Advento I", @"Mwene, Tate Omuyapuki, Kalunga ka aluse udula aise. Osilisili
osetuwapalela so ositukhupifa okukupandula aluse na apese, mu Kristu
Omwene wetu, ou euya tete, mounini wounu wetu, awanife ounongo
waluse wohole yoye ye etuyeululile ondyila yekhupifo; nde otekeuyulula
nouyelele wetumbaleko laye oso etupe asise esi etulombwela, esi natango
twateelela neitavelo linene. Molwaso fye neandyu nesandu ohatuingida
pombada hatuimbi etumbaleko loye hatu ti: Muyapuki . . ."),
        new(@"Prefácio do Advento II", @"Mwene, Tate Omuyapuki, Kalunga ka aluse udula aise! Osilisili
osetuwapalela so ositukhupifa okukupandula aluse na apese mu Kristu
Omwene wetu. Oye audifwa kovakhunganeki, ateelelwa ku Virgem Maria
nohole ihaiyelekwa, aingidwa ku Joao Baptista kutya teuya nde esi afika
emuulika mokati kovanu. Oye hetupe osali sokulongikida nehafo
ekumbiko ledalwo laye, oso ahange hatuindile fye hatudana efimaneko
laye.
Molwaso fye neandyu nesandu ohatuingida pombada hatuimbi
etumbaleko loye hatuti: Muyapuki . . .
I.- Dize-se: nas missas do tempo, desde o 1o Dom. do Advento ate ao dia
16 de Dezembro.
II.- Diz-se: desde o dia 17 de Dezembro ate ao dia 24 Dezembro."),
        new(@"Prefácio do Natal", @"Mwene, Tate Omuyapuki, Kalunga ka aluse udula aise! Osilisili
osetuwapalela so ositukhupifa okukupandula aluse na apese, sasi,
mekumbiko lOndaka yoye yaniga omunu, ouyelele mupe wetumbaleko
loye owetuminikila, opo, esi twamona Kalunga nomeso etu, tusiive
okuhala oupuna wo keulu. Molwaso fye neandyu nesandu ohatuingida
pombada hatuimbi etumbaleko loye hatuti: Muyapuki . . ."),
        new(@"Prefácio da Epifania", @"Mwene Tate Omuyapuki, Kalunga ka aluse udula aise! Osilisili
osetuwapalela so ositukhupifa okukupandula aluse
[continuação do Prefácio da Epifania]




na apese, sasi Omona woye Ewifa, molupe lounu wetu, etusitulula
nouyelele woukalunga waye. Molwaso fye neandyu nesandu ohatuingida
pombada hatuimbi etumbaleko loye hatuti: Muyapuki . . .
Natal:
Diz-se nas Missas do Natal do Senhor e durante a oitava, dias feriais
antes da Epifania e apresentação do Senhor.
Epifania:
Diz-se: nas Missas da Epifania e Baptismo do Senhor, e também durante
os dias até á festa do Baptismo."),
        new(@"Prefácio dos Domingos da Quaresma", @"Mwene Tate Omuyapuki, Kalunga ka aluse udula aise! Osilisili
osetuwapalela so ositukhupifa okukupandula aluse na apese mu Kristu
Omwene wetu, sasi, omido adise ohope ovaitaveli voye osali
sokulilongikidila osivilo so Paskwa, mehafo lomutima wakosoka oso,
momaindilo manene no mohole yakola vali natango, vo vamone onele
métotololo lomakumbiko ovakriste, vamone ewanifo losali sovana va
Kalunga. Molwaso fye neandyu nesandu, ohatuingida pombada hatuimbi
etumbaleko loye hatuti: Muyapuki . . ."),
        new(@"Prefácio Ferial da Quaresma", @"Osilisili osetuwapalela so ositukhupifa okukupandula aluse na apese,
Mwene, Tate Omuyapuki, Kalunga ka aluse
[continuação do Prefácio Ferial da Quaresma]
udula aise, ou, ku ava velivela ombedi nava vena ohole, hoyandye
edimepo lomatimba ove hyoandye enhono nondyabi, mu Kristu Omwene
wetu. Muye, fye neandyu nesandu ohatuingida pombada hatuimbi
etumbaleko loye hatuti: Muyapuki . . .


Ferial:
Diz-se: desde a Quarta-feira de Cinzas até ao sabado antes do 5°
Domingo da Quaresma."),
        new(@"Prefácio da Santa Cruz", @"Osilisili osetuwapalela so ositukhupifa okukupandula aluse na apese,
Mwene, Tate Omuyapuki, Kalunga ka aluse udula aise, ou wapaka
ekulilo lovanu mositi sómusiyakano, oso apa padililile efyo padilile-yo
omwenyo, ye ou atele ovanu nomuti wo moparaisu atewe-yo nositi
somusiyakano, mu Kristu Omwene wetu. Moluaso fye neandyu nesandu
ohatuingida pombada hatuimbi etumbaleko loye hatuti: Muyapuki . . ."),
        new(@"Prefácio Pascal", @"Mwene, Tate Omuyapuki, Kalunga ka aluse udula aise! Osilisili
osetuwapalela so ositukhupifa okukufimaneka aluse, naunene mefiku
(oufiku ou . . . efimbo) eli, omu Kristu Paskwa yetu adipawa. Ye oye
Dyona ya kalunga ei yadimapo omatimba ounyuni: ata efyo nefyo laye,
aetulula omwenyo nenyumuko laye. Molwaso fye neandyu nesandu
ohatuingida pombada hatuimbi etumbaleko loye hatuti: Muyapuki . . .
Santa Cruz.
Diz-se: desde a 2 feira Santa; Santa Cruz, Paixão do Senhor,
Preciosissimo Sangue.
Pascal.
Diz-se: desde a Vigilia Pascal até á Vigilia da Ascensáo."),
        new(@"Prefácio da Ascensão", @"Ascensão:
Desde a Ascensão até ao sábado antes do Pentecostes."),
        new(@"Prefácio da Ascensão (II)", @"Mwene, Tate Omuyapuki, Kalunga ka aluse udula aise! Osilisili
osetuwapalela so ositukhupifa okukupandula aluse na apese mu Kristu
Omwene wetu, ou, esi anyumuka, elimonikifa kovanhongwa vaye avese,
nde, moipafi yavo, okwalonda meulu oso eketupe onele moukalunga
waye. Molwaso fye neandyu nesandu ohatuingida pombada hatuimbi
etumbaleko loye hatuti: Muyapuki . . ."),
        new(@"Prefácio do Sagrado Coração de Jesus", @"Mwene, Tate Omuyapuki, Kalunga káluse udula aise! Osilisili
osetuwapalela so ositukhupifa okukupandula aluse napese, sasi
owayandya Omona woye Ewifa atulikwe komusiyakano nde tatuwa
neonga kesalale oso momutima waye watuwa, elimba loupuna wa
Kalunga, mutudile onhenda nosali sinene, nde aluse, monhalawa yohole
yaye nafye, aninge onele yetulumukwo yomwenyo yovadiinini
[continuação — fim do Prefácio do Sagrado Coração]
yo ininge epopilo lekhupifo lava velivela ombedi. Molwaso fye neandyu
nesandu ohatuingida pombada hatuimbi etumbaleko loye hatuti . . .
Muyapuki . . ."),
        new(@"Prefácio de Cristo Rei", @"Osilisili osetuwapalela so ositukhupifa okukupandula aluse na apese.
Mwene, Tate Omuyapuki, Kalunga Ka aluse udula aise, ou nomaadi
ehafo wayapula omuhongi wa aluse, Ohamba younyuni, Yesu Kristu
Omona woye, Omwene wetu; opo, meliyandyo koaltar yomusiyakano,
ngokhulayetotololo-ume losili, awanife ekulilo lovanu, ye, mokupangela
oisitwa aise, ayandye kounene-hamba woye uhena khulilo ouhamba
waluse uhena eengaba: ouhamba wosili no womwenyo, ouhamba
wouyapuki no wosali, ouhamba wouyuki, wohole no wombili. Molwasho

fye neandyu nesandu ohatuingida pombada hatuimbi etumbaelko loye
hatuti: Muyapuki . . ."),
        new(@"Prefácio do Espírito Santo", @"Mwene, Tate Omuyapuki, Kalunga ka aluse udula aise! Osilisili
osetuwapalela so ositukhupifa okukupandula aluse na apese mu Kristu
Omwene wetu. Esi alonda meulu nde akala omutumba kolulyo loye
atuma (nena) kovana voye Omepo Iyapuki ei evalombwelele. Molwaso
ounyuni ause owanyakukwa unene nde kumwe neandyu nesandu,
otauingida pombada etumbaelko loye tauti: Muyapuki . ."),
        new(@"Prefácio da Santíssima Trindade", @"Mwene, Tate Omuyapuki, Kalunga ka aluse udula aise, Osilisili
osetuwapalela so ositukhupifa okukupandula aluse na apese. Ove nye
nOmona woye Ewifa nOmepo Iyapuki omuKalunga umwe aeke,
Omwene umwe aeke, ha moumwe auke womunu umwe aeke ndele o
moutatu w' ""ouli"" umwe auke. Asise wetulikila setumbalo loye
otwesitavela ngasi-yo naanaa sOmona woye no sOmepo Iyapuki.
Mokuhokolola eitavelo letu moukalunga wosili waluse
ohatulinyongamene kovanu vatatu naanaa no k' ""ouli"" wavo umwe auke
no kounene wavo ufike pamwe. Molwaso fye neandyu nesandu
ohatuingida pombada hatuimbi etumbaleko loye hatuti: Muyapuki . . ."),
        new(@"Prefácio dos Domingos do Tempo Comum I", @"Mwene, Tate Omuyapuki, Kalunga kaluse udula aise, osilisili
osetuwapalela so ositukhupifa okukupandula aluse na apese mu Kristu
Omwene wetu. Meyahamo, mefyo nenyumuko, nelondo laye, neuyo
lOmepo Iyapuki, okwaninga oilonga ihakumifa: etukufa momatimba no
mefyo, etueta ketumbaleko loludi lahololwa, l ovahongi-hamba, ovanu
vayapuki, omuhoko wakulilwa, opo, esi twakulilwa moupika welaulu,

tweuya kevaimo louyelele woye, tuudife apese oihakumifa yoye. Molwaso
fye neandyu nesandu ohatuingida pombada hatuimbi etumbaleko loye
hatuti: Muyapuki . . ."),
        new(@"Prefácio dos Domingos do Tempo Comum II", @"Mwene, Tate Omuyapuki, Kalunga ka aluse udula aise! Osilisili
osetuwapalela so ositukhupifa okukupandula aluse na apese mu Kristu
Omwene wetu, sasi aetela ovanu onhenda molwowii wendyila davo,
okwadalwa ku Virgem Maria; mefyo laye komusiyakano okwetukufa
mefyo aluse; menyumuko laye okwetupa omwenyo itaupu. Molwaso fye
neandyu nesandu ohatuingida pombada hatuimbi etumbaleko loye
hatuti. Muyapuki . . ."),
        new(@"Prefácio da Santíssima Eucaristia", @"SSma Eucaristia.
Diz se: na Missa In Coena Domini e na solenidade da SSma Eucaristia.
Mwene, tate Muyapuki, Kalunga ka aluse udula aise! Osilisili
osetuwapalela so ositukhupifa okukupandula aluse napese mu Kristu
Omwene wetu, omuhongi wosili wa aluse, ou, mokuliyandya ku ove
moikhuna yekulilo, aninga okhula yeume lipe nde tati natuininge
mokumudimbuluka, opo, ngenge hatuli ombolo yomwenyo mosivilo
siyapuki omu, tuudife efyo laye fimbo twateelela euyululo laye lafimana.
Molwaso fye neandyu nesandu ohatuingida pombada hatuimbi
etumbaleko loye hatuti: Muyapuki . . ."),
        new(@"Prefácio da Santíssima Virgem", @"Mwene, Tatte Omuyapuki, Kalunga ka aluse udula aise! Osilisili
osetuwapalela so ositukhupifa okukupandula aluse na apese fye
tukufimaneke, takutange fye tukutumbaleke mosivilo (ile: M . . .)


somuyapuki Virgem Maria. Ye, menhono dOmepo Iyapuki, okwaninga
oufimba wOmona woye Ewifa; nde, no inakanifa ouvirgem waye,
okwaeta mounyuni ouyelele Waluse, Yesu Kristu Omwene wetu. Mu Ye
fye neandyu nesandu ohatuingida pombada hatuimbi etumbaleko loye
hatuti: Muyapuki . . .
Ile: M . . . Ekundo, Etalelepo, Edalwo, Elondeko, Epakwilo, Oimakulada
Conceição, Enyemateko, Efimaneko . . . lomuyapuki."),
        new(@"Prefácio de São José", @"Mwene, Tate Omuyapuki, Kalunga ka aluse udula aise! Osilisili
osetuwapalela so ositukhupifa okukupandula aluse na apese,
tukunenepeke, tukutange fye tuingide etumbaleko loye mosivilo (ile:
mefimaneko lomuyapuki) somuyapuki José. Omulumenu omuyuki
wahoolola aninge mwene wa Ina ya Kalunga; omupiya woye omudiinini
alungama, okwaninga omupangeli weumbo leni, oso, nombavi, akoneke
Omona woye Ewifa, asitwa menhono dOmepo Iyapuki, Yesu Kristu
Omwene wetu. Molwaso fye neandyu nesandu ohatuingida pombada
etumbaleko loye hatuimbi hatuti: Muyapuki . . ."),
        new(@"Prefácio dos Apóstolos", @"Apóstolos: Diz-se nas festas dos Apóstolos e Evangelistas.
Mwene, Tate Omuyapuki, Kalunga ka aluse udula aise! Osilisili
osetuwapalela so ositukhupifa okukupandula aluse na apese, sasi Ove,
mufita wa aluse, ihohauluka oufita woye ndele ohoukoncke aluse
nepopilo lovapostolu voye; ngaha Ongeleya otaipangelwa medina loye
kovapangeli venya ngaho wapaka-po, vaya ponele yOmona woye Yesu
Kristu. Mu Ye, fye neandyu nesandu ohatuingida pombada hatuimbi
etumbaleko loye hatuti: Muyapuki . . ."),
        new(@"Prefácio Comum II", @"Mwene, Tate Omuyapuki, Kalunga ka aluse udula aise! Osilisili
osetuwapalela so ositukhupifa okukupandula aluse na apese mu Kristu
Omwene wetu. Mu Ye, owatotolola oinima aise, weipa osali sounene
woye. Op-ne, nande oye Kalunga, okwelininipika, atilasi eohonde yaye
komusiyakano, ndele aeta vali ombili mounyuni ause. Opo-ne
atumbalekwa edule oisitwa aise ndele taningi ofifiya yomwenyo itaupu
wa avese ava havemuudu. Molwaso fye neandyu nesandu ohatuingida
pombada etumbaleko loye hatuti: Muyapuki . . ."),
        new(@"Prefácio dos Defuntos", @"Mwene, Tate Omuyapuki, Kalunga ka aluse udula aise! Osilisili
osetuwapalela so ositukhupifa okukupandula aluse na apese, mu Kristu
Omwene wetu. Mu Ye, eteelelo lenyumuko oletuminikila; nde, nande
meteelelo lefyo tuna ekunyamo, melakelo lomwenyo itaupu otuna ehe-
keleko. Mwene, ku ava vaitavela mu Ove, omwenyo ihau-
[continuação do Prefácio dos Defuntos]
hkulupo ndele ohausitululwa asike; opo-ne ngenge omutumba wo
kombada yedu wakhulapo tavakomona ukwao meulu itaukhulupo.
Molwaso fye neandyu nesandu ohatuingida pombada hatuimbi
etumbaleko loye hatuti; Muyapuki . . .
Santo:
Op. + Ov.: Muyapuki, muyapuki, muyapuki. Mwene Kaunga kounyuni
ause, eulu nedu otaliingida etumbaleko loye. Hosana, hosana
pombadambada. Natumbalekwe ou teuya medina lomwene. Hosana,
hosana pombadambada."),
        new(@"Oração Eucarística II", @"P:- Omwene nakale nanye.
OV:- Omwene okuli mokati ketu.

P:- Omitima naiyeluke.
OV:- Otweiyelula nokuli kOmwene.
P:- Tupanduleni Omwene Kalunga ketu.
OV:- Osetuwapalela so ositukhupifa.
P:- Mwene, Tate Omuyapuki! Osilisili osetuwapalela so ositukhupifa
okukupandula aluse na apese mu Kristu Omona woye. Ye ondaka yoye
wasitifa oinima aise; owemutuma akulile ounyuni, aninga omunu
menhono dOmepo Iyapuki, adalwa ku Virgem Maria. Opo awanife ehalo
loye ye ekukongele omuhoko uyapuki, okwatandaveka
[parte da Oração Eucarística / Consagração]
omaoko nde tafi avalelwa komusiyakano; ngaha okwata efyo ndele aeta
enyumuko. Molwaso fye neandyu nesandu ohatuingida pombada
etumbaleko loye hatuimbi hatuti:
P:- e o Povo: Muyapuki . . .
P:- Mwene, Ove Omuyapuki silisili, Ove ofifiya youyapuki ause! Paka
Omepo Yoye moiyandyua ei opo iyapuke nde taitusitukile olutu +
nohonde yOmwene wetu Yesu Kristu, ou, mefiku eli eliyandya mwene
adipawe, akufa ombolo nde tapandula nde teipambula nde teiyandyele
ovahongwa vaye tati: Tambuleni amuse lyeni: ESI OLUTU LANGE
LAYANDYUA MOLWENI.
Oso-yo, konima youvalelo, okwakufa eholo nde tapandula nde teliyandye
kovahongwa vaye tati: Tambuleni amuse nweni: ELI EHOLO LOHONDE
YANGE, OHONDE YEUME LIPE LA ALUSE, YATILWASI MOLWENI
NO MOLWOVANU AVESE MEYAVELELO LOMATIMBA. Esi siningeni
mokudimbulukange.
P:- Taleni ekumbiko leitavelo.




Ov:- Mwene, efyo loye ohatulitambeke, enyumuko loYe ohatuliingida;
Mwene Yesu ila!
Ile:- Mwene, aluse ngenge hatuli ombolo ei fye hatunu meholo eli,
ohatutambeke efyo loye fimbo twateelela euyululo loye.
Ile:- Mukulili wounyuni, tukhupifa, Ove ou wetukulila nomusiyakano
nenyumuko loye.
P:- Mwene, paife esi hatudana edimbuluko lefyo nenyumuko lOmona
woye, molupandu ohatukupe ombolo yomwenyo neholo lekulilo sasi
[continuação da Oração Eucarística]
waitavela tuuye pomeso Yoye oso tukukalele. Ohatulikwambele ku Ove,
opo, esi twatambula olutu nohonde ya Kristu, Omepo Iyapuki ituninge
osiwana sohole. Mwene, dimbuluka Ongeleya yoye ili mounyuni ause.
Ipa ohole yosili yo nOpapa yetu (N ...), N'ombispu yetu (N...) n'ovahongi
avese. Dimbuluka yo omupipa woye (N...) ou waifana ku Ove: sasi elifa
na Kristu mefyo nakale-yo na Ye menyumuko. Dimbuluka yo vakwetu
avese ava vafya meteelelo lenyumuko na avese ava vafya nale mounyuni
ou, avese vatambula mouhamba woye. Mwene, tufila onhenda atuse, ove
tupa elao lomwenyo itaupu tukakale na Virgem Maria, Ina ya Kalunga,
novapostolu na avese ava vawanifa ehalo loye opo tukaingide efimaneko
loye. Mu Kristu Omwene wetu.
MOLWA KRISTU, NA KRISTU, MU KRISTU, KALUNGA TATE UDULA
AISE, TAMBULA EFIMANEKO ALISE N'ETUMBALEKO ALISE, PAIFE
NO FIYOFIYO, MOULIMUMWE WOMEPO IYAPUKI.
OV:- Amen.

                     PAI NOSSO (TATE YETU)

P:- Tuindileni nelinekelo eindilo eli twalongwa k'Omwene:



Pe OV.- Tate yetu uli meulu, edina loye nalitumbalekwe, ouhamba woye
nauuye kufye, ehalo loye nalilongwe kombada yedu ngasi o meulu. Tupa
nena okulya kwetu kwomafiku aese, tuyavelela omatimba etu ngasi fye
hatuyavelele ava vetusinda. Inoefa tuwile momakumbaelo ndele
tukandula kowii.
[continuação do Pai Nosso / ritos da paz]
P:- Mwene, tukandula kowii ause, pa ounyuni ombili momafiku etu, oso
mekwafelo lonhenda yoye, tukale aluse tuhena etimba fye tuheli
melimbililo lasa, twakeuka neteelelo lehafo euyululo la Kristu Omukulili.
OV:- Ouhamba, nepangelo, netumbaleko oloye aluse fiyo aluse.
P:- Mwene Yesu Kristu, ou walombwela ovahongwa voye toti:
Ohandimufiile ombili, ohandimupe ombili yange; inotala komatimba
etu, ndele-ne tala keitavelo lOngeleya yoye, ove ipa oulimumwe nombili
ngasi waseya, ou u Kalunga na Kho, moulimumwe wOmepo Iyapuki.
OV:- Amen.
P:- Ombili yOmwene naikale aluse nanye.
OV:- Ohole ya Kristu oyetuongela. (Paafaneni ombili mohole ya Xto).
P:- Oulimumwe wolutu nohonde yOmwene wetu Yesu Kristu ou
hatutambula nautufikife k'omwenyo itaupu.
P e OV:- Dyona ya Kalunga ou tokufa-po omatimba ounyuni,
tufil'onhenda. Dyona ya Kalunga, ou tokufa-po omatimba ounyuni,
tufil'onhenda. Dyona ya Kalunga, ou tokufa-po omatimba ounyuni, tupa
ombili.
P:- Mwene Yesu Kristu, Mona wa Kalunga Omunamwenyo, ou wapa
ounyuni omwenyo, molwehalo la Kho nekwafelo lOmepo Iyapuki,
molwefyo loye, molwolutu loye eli liyapuki nohonde, kandulenge




komatimba ange aese no kowii ause, ninga ndiitavele aluse oipango yoye,
ove inoefa tulitukauke fye nAve.
Ile:- Mwene Yesu Kristu, okumunyau yolutu nohonde yoye inayetela-nge
etokolo nekano, nde,
[continuação]
molwonhenda yoye, nayetele-nge epopilo noimbodi yomwenyo wange
nolutu lange."),
        new(@"Comunhão", @"P:- Ovanelao ava vaifanwa kouvalelo wOmwene. Oyei odyona ya
Kalunga, ei taikufa-po omatimba ounyuni.
P e OV:- Mwene, Inandiwana wuye muame, ndele-ne tonga ondaka
imwaike ndikakhupifwe.
P:- Olutu la Kristu.
Om.:- Amen."),
        new(@"Oração Final (Eindilo)", @"P:- Tuindileni . . . MolwOmwene wetu Yesu Kristu, Omona woye,
moulimumwe wOmepo Iyapuki.
OV:- Amen."),
        new(@"Conclusão", @"P:- Omwene nakale nanye.
OV:- Omwene okuli mokati ketu.
P:- Nemuyambeke Kalunga adula aise: Khe, nOmona nOmepo Iyapuki.
OV:- Amen.
P:- Tuyeni nombili ye Omwene netusikule.
OV:- Amen."),
        new(@"O que é a Missa? (Omisa Osike?)", @"Dimbuluka.
Kristu okwati mefimbo esi kwali ena omwêñyo kutya
[continuação]
okwali ena okufya nokunyumuka molwovanu avese. Kristu okwati kutya
okwali ena okutupa olutu laye ngasi oikulya, nohonde yaye ngasi
oikunwa.
Molwaso:
Metine liyapuki okwaifana vapostolu, etuongela posililo ndele aninga
Omisa yotete. Tambuleni . . . lyeni, tambuleni . . . nueni, esi siningeni
mokudimbulukange. Metitano liyapuki okwafya molwetu. Mosalumingu
so Paskwa okwanyumuka movafi.
Molwaso itavela naanaa:
Omisa osidimbulukifo seyahamo nefyo nenyumuko la Kristu. Omisa
osivilo sepata lovakristao, ava veliongela posililo, oso vaude Ondaka
yomwene vo valye oikulya yo keulu."),
        new(@"Miseremini Mei", @"Fileni-nge onheñda. / Fileni-nge onheñda. / Nande nye, okaume kange,
/ Sasi eke l'Omwene / Lakumange!"),
        new(@"Oração para Pedir Chuva", @"Kalunga Omumene, Tate munaenhono adise, oinima aise oili mepangelo
loye; otopangele meulu no kombada yedu. Oisitwa aise nenhono adise
odina okudulika ku Ove.

 OMUNDILO NOMEVA NOIKUNGULU OTAIWANIFA EHALO
                                   LOYE


Eedula otadiloko apa wahala. Eembadi ohadiende meendyila edi
todiulikile, ohosingi oikoo nde tolokifa odula; toipe osilongo simwe nde
toikelele kosilongo sikwao.
Kelela ei ii tayeta oiponga nde yandyea ei iwa yetuwapalela.
Kamatulula omake ouwa woye, uyambeke omaumbo, nomapya etu na
kese soye. Yambeka oilya noimeno aise wiyambeke, ove wikaleke, opo
tukuetele omilongelo yepandulo letu. fye tufimaneke edina loye, aluse
fiyo aluse. Amen."),
        new(@"Oração Coleta", @"Tuindileni: Kalunga, ou watupa omwenyo, omu Ove hatulinyenge fye tuli
kombada yedu, tumina omapya etu odula apumbwa, opo-ne ngenge
twakwafelwa nouwa wo posi ou, natuhokwe nenhono omauwa omwenyo
itaupu. MoluOmwene wetu Yesu Kristu Omona woye, Moulimumwe
wOmepo Iyapuki. Amen."),
        new(@"Sobre as Oblatas", @"Mwene, handuluka, moluoiyandyuwa ei, ndele tukwafela nodula ei
yawanena. MoluOmwene wetu Yesu Kristu."),
        new(@"Oração Depois da Comunhão", @"Tuindileni: Omwene, tupa odula y'okuyandya enhono dipe, ndele
lidinika ulihanifile omeva o kEulu kombada yedu lakukuta.
MoluOmwene wetu Yesu Kristu, Omona woye, moulimumwe wOmepo
Iyapuki."),
        new(@"Oração da Noite (P'Eindilo Longulosi)", @"(Á oração da noite)
M'omake oye, Kalunga, haiyandye omwenyo wange.


M'omake oye, Kalunga, haiyandye omwenyo wange.
Wetukulila, 'Mwene, Kalunga kosili - Haiyandye omwenyo wange.
Tutumbalekeni Se n'Omona n'Omepo Iyapuki.
M'omake oye, Kalunga, haiyandye omwenyo wange."),
        new(@"Ongulosi", @"Ove, esi latya komatango / twaloloka k'oilonga yetu, / hatukupe n'ovanu
avese / oilonga, efudo n'ohole
Oufiku wetupaka m'elaulu, / oudila vasuna k'oihadi, / Mwene, twahala
evatelo loye: / m'ohole Yoye tunangale.
Efiku eli toketuifana / k'evalelwa l'ohole Yoye, / m'ongudu yovahoololwa
voye, / mehafo hatukawanena."),
    };
}
