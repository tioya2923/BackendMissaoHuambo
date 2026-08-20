using System.Text.Json.Serialization;

namespace MissaoBackend.Models;

public class ItemEncomenda
{
    public int Id { get; set; }

    public int EncomendaId { get; set; }
    [JsonIgnore]
    public Encomenda? Encomenda { get; set; }

    public int ProdutoId { get; set; }

    // Snapshot do produto no momento da encomenda — protege o histórico
    // caso o produto seja depois alterado, ou preços mudem.
    public string ProdutoNome { get; set; } = string.Empty;
    public decimal PrecoUnitario { get; set; }
    public int Quantidade { get; set; }
}
