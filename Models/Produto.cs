namespace MissaoBackend.Models;

public class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal Preco { get; set; }
    public string? ImagemUrl { get; set; }
    public string? Categoria { get; set; }
    public bool Disponivel { get; set; } = true;
    public int Ordem { get; set; } = 0;
}
