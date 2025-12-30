using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MissaoBackend.Models;

public class Evento
{
    public int Id { get; set; }

    [Required]
    [JsonPropertyName("data")]
    public DateTime Data { get; set; }

    [JsonPropertyName("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [JsonPropertyName("descricao")]
    public string Descricao { get; set; } = string.Empty;

    [JsonPropertyName("leituras")]
    public string Leituras { get; set; } = string.Empty;

    [JsonPropertyName("observacoes")]
    public string Observacoes { get; set; } = string.Empty;

}
