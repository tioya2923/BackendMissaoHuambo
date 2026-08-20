using System.Text.Json.Serialization;

namespace MissaoBackend.Models;

public class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal Preco { get; set; }

    // Se definido (e menor que o Preco), o artigo está em promoção: mostra-se o
    // preço original riscado e este como o preço a pagar.
    public decimal? PrecoPromocional { get; set; }

    // Destaca o artigo numa secção própria na loja (definido pela própria loja).
    public bool EmDestaque { get; set; } = false;

    public string? ImagemUrl { get; set; }
    public string? Categoria { get; set; }
    public bool Disponivel { get; set; } = true;
    public int Ordem { get; set; } = 0;

    public int LojaId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Loja? Loja { get; set; }
}
