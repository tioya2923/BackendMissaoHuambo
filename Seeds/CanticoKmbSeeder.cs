using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;
using Microsoft.EntityFrameworkCore;

namespace MissaoBackend.Seeds;

public static class CanticoKmbSeeder
{
    // Mesma taxonomia de tópicos já usada em Umbundu, para manter as secções
    // de cânticos das duas línguas nacionais coerentes entre si.
    private static readonly string[] TopicosBase =
    {
        "Procissão",
        "Entrada",
        "Kyrie",
        "Entronização da Palavra",
        "Aleluia",
        "Oração dos Fiéis",
        "Ofertório",
        "Elevação",
        "Santo",
        "Saudação",
        "Cordeiro de Deus",
        "Comunhão",
        "Acção de Graças",
        "Saída",
        "Advento",
        "Natal",
        "Quaresma",
        "Páscoa",
        "A Jesus Cristo",
        "Ao Espírito Santo",
        "A Nossa Senhora",
        "Aos Santos",
        "Cânticos Interleccionais",
        "Vida Cristã",
    };

    public static async Task SeedAsync(AppDbContext db)
    {
        // 1) Garante que a taxonomia de tópicos existe
        var topicoByNome = (await db.TopicosKmb.ToListAsync())
            .ToDictionary(t => t.Nome.ToLowerInvariant(), t => t.Id);

        var criar = TopicosBase
            .Where(nome => !topicoByNome.ContainsKey(nome.ToLowerInvariant()))
            .Select(nome => new TopicoKmb { Nome = nome, Slug = SlugHelper.Slugify(nome) })
            .ToList();

        if (criar.Count > 0)
        {
            db.TopicosKmb.AddRange(criar);
            await db.SaveChangesAsync();
            foreach (var t in criar) topicoByNome[t.Nome.ToLowerInvariant()] = t.Id;
            Console.WriteLine($"✓ {criar.Count} tópicos criados (Cânticos Kimbundu).");
        }

        // 2) Insere os cânticos que ainda não existem (por slug)
        var existingSlugs = (await db.CanticosKmb.Select(c => c.Slug).ToListAsync()).ToHashSet();

        var novos = GetCanticos()
            .Where(c => topicoByNome.ContainsKey(c.TopicoNome.ToLowerInvariant()))
            .Select(c => new { Data = c, Slug = SlugHelper.Slugify(c.Titulo) })
            .Where(x => !existingSlugs.Contains(x.Slug))
            .Select(x => new CanticoKmb
            {
                Titulo = x.Data.Titulo,
                Slug = x.Slug,
                Letra = x.Data.Letra,
                TopicoId = topicoByNome[x.Data.TopicoNome.ToLowerInvariant()],
            })
            .ToList();

        if (novos.Count == 0) return;

        db.CanticosKmb.AddRange(novos);
        await db.SaveChangesAsync();
        Console.WriteLine($"✓ {novos.Count} cânticos Kimbundu adicionados.");
    }

    private record CanticoData(string Titulo, string Letra, string TopicoNome);

    private static List<CanticoData> GetCanticos() => new()
    {
        new CanticoData("Ave Maria",
            @"Ave Maria! Mu ixi ni diulu
Umukengeji ua ima ioso
Um kitangana kiki kionene
Tukuximana: Avé Maria (2×)

1 - Ngoloxi iza
Iza ni kizomba ixi ioso
Ivunda boxi ni bulu
Ni irmã ioso/izeka: hudi!
Nzumbi jizuela: Eva Maria!

2 - Mu jingeleja ngunga jixika
Jizuela jixi:
Sambenu o anjulu
Tetembua imuika itulu itemuka
Jianju jimba: Ave Maria!

3 - Kuhota kuná kua tufundu tuâ
Asanguluka atulukuka
Tuana tuzuela ni jitata já
Ene oso asamba: Ave Maria

4 - Usuku uiza o ngenji ia kuenda
Uala ni uoma ua irmã ioso ukuata
O tersu, usamba udila:
Ngendese, mama: ave Maria

5 - O jingadiama jizunga o ixi, kala
ni jinzu, kala ni kima, ala ni tuxi, mbambi ni nzala
Abinga, asamba: Ave Maria!

6 - O Mu kizua kietu kia kúfua
Eie, Maria, kaia o madiabu
Atuimanena, tuambate
Ngana, kua nzambi tata
Katé mu diulu", "A Nossa Senhora"),
    };
}
