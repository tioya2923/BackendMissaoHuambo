namespace MissaoBackend.Models;

public class FormaApoio
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Ativo { get; set; } = true;
    public int Ordem { get; set; } = 0;
}
