using MissaoBackend.Data;
using MissaoBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace MissaoBackend.Services;

// Guarda ficheiros (fotos, PDFs) na base de dados, em vez do disco do
// servidor — o disco de um serviço web no Render não é persistente entre
// deploys, mas a base de dados é. Devolve sempre um URL relativo
// "/api/ficheiros/{id}", igual em formato ao que já era usado antes com
// ficheiros em disco, para não ser preciso mudar nada no frontend/mobile.
public class ArmazenamentoService
{
    private readonly AppDbContext _db;
    public ArmazenamentoService(AppDbContext db) => _db = db;

    public async Task<string> GuardarAsync(IFormFile ficheiro)
    {
        using var ms = new MemoryStream();
        await ficheiro.CopyToAsync(ms);

        var registo = new Ficheiro
        {
            ContentType = string.IsNullOrWhiteSpace(ficheiro.ContentType) ? "application/octet-stream" : ficheiro.ContentType,
            Dados = ms.ToArray(),
        };
        _db.Ficheiros.Add(registo);
        await _db.SaveChangesAsync();

        return $"/api/ficheiros/{registo.Id}";
    }

    // Apaga o ficheiro anterior, se a URL guardada apontar para um
    // registo nesta tabela (evita acumular ficheiros órfãos quando alguém
    // substitui a foto/PDF).
    public async Task ApagarSeForNossoAsync(string? urlAnterior)
    {
        if (string.IsNullOrWhiteSpace(urlAnterior)) return;
        const string prefixo = "/api/ficheiros/";
        if (!urlAnterior.StartsWith(prefixo)) return;
        if (!int.TryParse(urlAnterior[prefixo.Length..], out var id)) return;

        var registo = await _db.Ficheiros.FirstOrDefaultAsync(f => f.Id == id);
        if (registo != null)
        {
            _db.Ficheiros.Remove(registo);
            await _db.SaveChangesAsync();
        }
    }
}
