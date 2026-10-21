using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

namespace MissaoBackend.Seeds;

public static class CalendarioSeeder
{
    private const string ResourceName = "MissaoBackend.Seeds.Calendario2027.json";

    // Sobe quando o ficheiro do calendário de 2027 é corrigido: os dias já guardados são então
    // actualizados uma única vez (depois disso, edições feitas pelo administrador são respeitadas).
    private const int VersaoActual = 2;
    private const string ChaveVersao = "Calendario2027.Versao";

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
        var existentes = await db.Eventos
            .Where(evento => evento.Data >= inicio && evento.Data < fim)
            .ToListAsync();
        var datasExistentes = existentes.Select(evento => evento.Data.Date).ToHashSet();

        var novos = eventos
            .Where(evento => !datasExistentes.Contains(evento.Data.Date))
            .ToList();

        if (novos.Count > 0)
        {
            db.Eventos.AddRange(novos);
            await db.SaveChangesAsync();
            Console.WriteLine($"✓ {novos.Count} eventos do Calendário Litúrgico de 2027 adicionados.");
        }

        // Correcções ao ficheiro: aplica-as uma vez aos dias que já estavam guardados.
        var registoVersao = await db.ConfiguracaoSistema.FirstOrDefaultAsync(c => c.Chave == ChaveVersao);
        var versaoGuardada = int.TryParse(registoVersao?.Valor, out var v) ? v : (existentes.Count == 0 ? VersaoActual : 1);
        if (versaoGuardada < VersaoActual)
        {
            var porData = eventos.ToDictionary(e => e.Data.Date);
            var actualizados = 0;
            foreach (var existente in existentes)
            {
                if (!porData.TryGetValue(existente.Data.Date, out var correcto)) continue;
                if (existente.Titulo == correcto.Titulo && existente.Descricao == correcto.Descricao
                    && existente.Leituras == correcto.Leituras && existente.Observacoes == correcto.Observacoes) continue;
                existente.Titulo = correcto.Titulo;
                existente.Descricao = correcto.Descricao;
                existente.Leituras = correcto.Leituras;
                existente.Observacoes = correcto.Observacoes;
                actualizados++;
            }
            if (actualizados > 0) Console.WriteLine($"✓ {actualizados} dias do Calendário Litúrgico de 2027 corrigidos.");
        }

        if (registoVersao == null)
            db.ConfiguracaoSistema.Add(new ConfiguracaoSistema { Chave = ChaveVersao, Valor = VersaoActual.ToString() });
        else if (registoVersao.Valor != VersaoActual.ToString())
            registoVersao.Valor = VersaoActual.ToString();
        await db.SaveChangesAsync();
    }
}
