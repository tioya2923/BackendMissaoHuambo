namespace MissaoBackend.Models;

public class TopicoKmb
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    // RELAÇÃO: um tópico tem vários cânticos
    public List<CanticoKmb> Canticos { get; set; } = new();
}
