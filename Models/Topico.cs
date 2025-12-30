using System.Text.Json.Serialization;

namespace MissaoBackend.Models;

public class Topico
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
