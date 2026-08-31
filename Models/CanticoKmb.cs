using System.Text.Json.Serialization;

namespace MissaoBackend.Models;

public class CanticoKmb
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Letra { get; set; } = string.Empty;
    public string? Autor { get; set; }
    public string? PdfUrl { get; set; }

    public int TopicoId { get; set; }

    // Ignora no POST/PUT, mas permite mostrar no GET
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public TopicoKmb? Topico { get; set; }

    // Código amigável para o frontend (mapeia para o Slug)
    public string Codigo => Slug;
}
