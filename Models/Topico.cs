using System.Text.Json.Serialization;

namespace MissaoBackend.Models;

public class Topico
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public int IdiomaId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Idioma? Idioma { get; set; }
}
