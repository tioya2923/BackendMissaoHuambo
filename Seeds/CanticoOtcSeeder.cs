using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;

namespace MissaoBackend.Seeds;

public static class CanticoOtcSeeder
{
    private const string ResourceName = "MissaoBackend.Seeds.oshikwanhama.txt";
    private const string TopicoNome = "Cânticos";

    public static async Task SeedAsync(AppDbContext db)
    {
        var idiomaId = await db.Idiomas
            .Where(idioma => idioma.Codigo == "otc")
            .Select(idioma => (int?)idioma.Id)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("O idioma Oshikwanhama (otc) não existe.");

        var topicoSlug = SlugHelper.Slugify(TopicoNome);
        var topico = await db.Topicos
            .FirstOrDefaultAsync(item => item.IdiomaId == idiomaId && item.Slug == topicoSlug);

        if (topico is null)
        {
            topico = new Topico
            {
                Nome = TopicoNome,
                Slug = topicoSlug,
                IdiomaId = idiomaId,
            };
            db.Topicos.Add(topico);
            await db.SaveChangesAsync();
        }

        var existingSlugs = (await db.Canticos
                .Where(cantico => cantico.IdiomaId == idiomaId)
                .Select(cantico => cantico.Slug)
                .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var canticos = ReadCanticos();
        var slugCounts = canticos
            .GroupBy(cantico => SlugHelper.Slugify(cantico.Titulo), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Count(), StringComparer.OrdinalIgnoreCase);
        var novos = new List<Cantico>();

        foreach (var cantico in canticos)
        {
            var baseSlug = SlugHelper.Slugify(cantico.Titulo);
            var slug = slugCounts[baseSlug] > 1 ? $"{baseSlug}-{cantico.Numero}" : baseSlug;
            if (!existingSlugs.Add(slug)) continue;

            novos.Add(new Cantico
            {
                Titulo = cantico.Titulo,
                Slug = slug,
                Letra = cantico.Letra,
                TopicoId = topico.Id,
                IdiomaId = idiomaId,
            });
        }

        if (novos.Count == 0) return;

        db.Canticos.AddRange(novos);
        await db.SaveChangesAsync();
        Console.WriteLine($"✓ {novos.Count} cânticos Oshikwanhama adicionados.");
    }

    private static IReadOnlyList<CanticoData> ReadCanticos()
    {
        using var stream = typeof(CanticoOtcSeeder).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Recurso de cânticos não encontrado: {ResourceName}.");
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        var headers = Regex.Matches(text, @"(?m)^(?<number>\d{1,3})\.\t+(?<title>.+)$");

        if (headers.Count == 0)
            throw new InvalidDataException("O ficheiro de cânticos Oshikwanhama não contém títulos numerados.");

        var canticos = new List<CanticoData>(headers.Count);
        for (var index = 0; index < headers.Count; index++)
        {
            var header = headers[index];
            var expectedNumber = index + 1;
            if (!int.TryParse(header.Groups["number"].Value, out var number) || number != expectedNumber)
                throw new InvalidDataException($"Numeração inesperada no cântico Oshikwanhama {expectedNumber}.");

            var contentStart = header.Index + header.Length;
            if (contentStart < text.Length && text[contentStart] == '\r') contentStart++;
            if (contentStart < text.Length && text[contentStart] == '\n') contentStart++;

            var contentEnd = index + 1 < headers.Count ? headers[index + 1].Index : text.Length;
            var letra = text[contentStart..contentEnd].Trim();
            var titulo = header.Groups["title"].Value.Trim();
            if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(letra))
                throw new InvalidDataException($"O cântico Oshikwanhama {number} está sem título ou letra.");

            canticos.Add(new CanticoData(number, titulo, letra));
        }

        return canticos;
    }

    private sealed record CanticoData(int Numero, string Titulo, string Letra);
}
