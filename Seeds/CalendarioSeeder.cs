using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

namespace MissaoBackend.Seeds;

public static class CalendarioSeeder
{
    private const string ResourceName = "MissaoBackend.Seeds.Calendario2027.json";

    public static async Task SeedAsync(AppDbContext db)
    {
        await using var stream = typeof(CalendarioSeeder).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Recurso do calendário não encontrado: {ResourceName}.");

        var eventos = await JsonSerializer.DeserializeAsync<List<Evento>>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("O calendário de 2027 não contém dados válidos.");

        var inicio = new DateTime(2027, 1, 1);
        var datasEsperadas = Enumerable.Range(0, 365)
            .Select(dia => inicio.AddDays(dia))
            .ToHashSet();
        var datasRecebidas = eventos.Select(evento => evento.Data.Date).ToHashSet();

        if (eventos.Count != 365 || !datasEsperadas.SetEquals(datasRecebidas))
            throw new InvalidDataException("O calendário de 2027 deve conter exactamente um registo para cada dia do ano.");

        var fim = inicio.AddYears(1);
        var datasExistentes = (await db.Eventos
                .Where(evento => evento.Data >= inicio && evento.Data < fim)
                .Select(evento => evento.Data)
                .ToListAsync())
            .Select(data => data.Date)
            .ToHashSet();

        var novos = eventos
            .Where(evento => !datasExistentes.Contains(evento.Data.Date))
            .ToList();

        if (novos.Count == 0) return;

        db.Eventos.AddRange(novos);
        await db.SaveChangesAsync();
        Console.WriteLine($"✓ {novos.Count} eventos do Calendário Litúrgico de 2027 adicionados.");
    }
}