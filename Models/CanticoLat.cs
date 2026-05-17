using System.Text.Json.Serialization;

namespace MissaoBackend.Models;

public class CanticoLat
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Letra { get; set; } = string.Empty;
    public string? PdfUrl { get; set; }

    public int TopicoId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public TopicoLat? Topico { get; set; }
}
