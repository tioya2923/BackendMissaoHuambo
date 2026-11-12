using MissaoBackend.Data;
using MissaoBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace MissaoBackend.Services;

public enum TipoFicheiro { Imagem, Pdf }

// Guarda ficheiros (fotos, PDFs) na base de dados, em vez do disco do
// servidor — o disco de um serviço web no Render não é persistente entre
// deploys, mas a base de dados é. Devolve sempre um URL relativo
// "/api/ficheiros/{id}", igual em formato ao que já era usado antes com
// ficheiros em disco, para não ser preciso mudar nada no frontend/mobile.
//
// Segurança: o tipo do ficheiro nunca é o que o cliente diz (nome ou Content-Type):
// é detectado pelos primeiros bytes. Só são aceites JPEG, PNG, WEBP e PDF — assim
// ninguém consegue alojar HTML/JavaScript no domínio da Ndatava.
public class ArmazenamentoService
{
    public const long MaxImagemBytes = 5 * 1024 * 1024;   // 5 MB
    public const long MaxPdfBytes = 20 * 1024 * 1024;     // 20 MB

    private readonly AppDbContext _db;
    public ArmazenamentoService(AppDbContext db) => _db = db;

    /// <summary>Detecta o tipo real a partir dos bytes iniciais; null se não for um formato aceite.</summary>
    public static string? DetectarTipo(ReadOnlySpan<byte> b, TipoFicheiro esperado)
    {
        if (esperado == TipoFicheiro.Pdf)
            return b.Length >= 5 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46 && b[4] == 0x2D
                ? "application/pdf" : null;

        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "image/jpeg";
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47
            && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A) return "image/png";
        if (b.Length >= 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46
            && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return "image/webp";
        return null;
    }

    /// <summary>
    /// Valida (tamanho e formato real) e guarda. Devolve o URL, ou uma mensagem de erro para o utilizador.
    /// </summary>
    public async Task<(string? Url, string? Erro)> GuardarValidadoAsync(IFormFile ficheiro, TipoFicheiro tipo)
    {
        var maximo = tipo == TipoFicheiro.Pdf ? MaxPdfBytes : MaxImagemBytes;
        if (ficheiro.Length == 0) return (null, "Ficheiro inválido.");
        if (ficheiro.Length > maximo)
            return (null, tipo == TipoFicheiro.Pdf ? "O PDF não pode exceder 20 MB." : "A imagem não pode exceder 5 MB.");

        using var ms = new MemoryStream();
        await ficheiro.CopyToAsync(ms);
        var dados = ms.ToArray();

        var contentType = DetectarTipo(dados, tipo);
        if (contentType == null)
            return (null, tipo == TipoFicheiro.Pdf
                ? "O ficheiro não é um PDF válido."
                : "Apenas imagens JPG, PNG ou WEBP válidas são aceites.");

        var registo = new Ficheiro { ContentType = contentType, Dados = dados };
        _db.Ficheiros.Add(registo);
        await _db.SaveChangesAsync();

        return ($"/api/ficheiros/{registo.Id}", null);
    }

    // Apaga o ficheiro anterior, se a URL guardada apontar para um
    // registo nesta tabela (evita acumular ficheiros órfãos quando alguém
    // substitui a foto/PDF ou apaga a conta).
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
