using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

namespace MissaoBackend.Seeds;

public static class Calendario2026Seeder
{
    private const string ResourceName = "MissaoBackend.Seeds.Calendario2026.json";
    private const int VersaoActual = 1;
    private const string ChaveVersao = "Calendario2026.Versao";

    public static async Task SeedAsync(AppDbContext db)
    {
        await using var stream = typeof(Calendario2026Seeder).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Recurso do calendário não encontrado: {ResourceName}.");

        var eventos = await JsonSerializer.DeserializeAsync<List<Evento>>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("O calendário de 2026 não contém dados válidos.");

        var inicio = new DateTime(2026, 1, 1);
        var datasEsperadas = Enumerable.Range(0, 365)
            .Select(dia => inicio.AddDays(dia))
            .ToHashSet();
        var datasRecebidas = eventos.Select(evento => evento.Data.Date).ToHashSet();
        var chavesDuplicadas = eventos
            .GroupBy(evento => (evento.Data.Date, evento.Titulo))
            .Any(grupo => grupo.Count() > 1);

        if (eventos.Count != 374 || datasRecebidas.Count != 365
            || !datasEsperadas.SetEquals(datasRecebidas) || chavesDuplicadas)
            throw new InvalidDataException("O calendário de 2026 deve conter os eventos previstos para cada dia do ano.");

        var fim = inicio.AddYears(1);
        var existentes = await db.Eventos
            .Where(evento => evento.Data >= inicio && evento.Data < fim)
            .ToListAsync();
        var porChave = existentes.ToDictionary(evento => (evento.Data.Date, evento.Titulo));
        var registoVersao = await db.ConfiguracaoSistema
            .FirstOrDefaultAsync(configuracao => configuracao.Chave == ChaveVersao);
        var versaoGuardada = int.TryParse(registoVersao?.Valor, out var versao)
            ? versao
            : existentes.Count == 0 ? VersaoActual : 0;
        var aplicarActualizacao = versaoGuardada < VersaoActual;
        var novos = new List<Evento>();
        var actualizados = 0;

        foreach (var evento in eventos)
        {
            if (porChave.TryGetValue((evento.Data.Date, evento.Titulo), out var existente))
            {
                if (aplicarActualizacao && existente.Leituras != evento.Leituras)
                {
                    existente.Leituras = evento.Leituras;
                    actualizados++;
                }

                continue;
            }

            novos.Add(new Evento
            {
                Data = evento.Data,
                Titulo = evento.Titulo,
                Descricao = evento.Descricao,
                Leituras = evento.Leituras,
                Observacoes = evento.Observacoes
            });
        }

        if (novos.Count > 0) db.Eventos.AddRange(novos);

        if (novos.Count > 0 || actualizados > 0)
            await db.SaveChangesAsync();

        if (novos.Count > 0)
            Console.WriteLine($"✓ {novos.Count} eventos do Calendário Litúrgico de 2026 adicionados.");
        if (actualizados > 0)
            Console.WriteLine($"✓ Leituras completas adicionadas a {actualizados} eventos de 2026.");

        if (registoVersao == null)
            db.ConfiguracaoSistema.Add(new ConfiguracaoSistema { Chave = ChaveVersao, Valor = VersaoActual.ToString() });
        else if (registoVersao.Valor != VersaoActual.ToString())
            registoVersao.Valor = VersaoActual.ToString();

        await db.SaveChangesAsync();
    }
}
