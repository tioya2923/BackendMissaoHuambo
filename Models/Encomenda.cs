using System.Text.Json.Serialization;

namespace MissaoBackend.Models;

// Estados possíveis de uma encomenda, do pedido à entrega
public static class EstadoEncomenda
{
    public const string Pendente = "Pendente";
    public const string Confirmada = "Confirmada";
    public const string Enviada = "Enviada";
    public const string Cancelada = "Cancelada";
}

public class Encomenda
{
    public int Id { get; set; }
    public DateTime Data { get; set; } = DateTime.UtcNow;

    public string NomeCliente { get; set; } = string.Empty;
    public string Contacto { get; set; } = string.Empty;
    public string? Morada { get; set; }
    public string? Observacoes { get; set; }

    public string Estado { get; set; } = EstadoEncomenda.Pendente;
    public decimal Total { get; set; }

    // Comissão da plataforma, calculada e fixada no momento da compra (a percentagem
    // da loja pode mudar depois, mas não altera encomendas já criadas).
    public decimal PercentualComissaoAplicado { get; set; }
    public decimal ValorComissao { get; set; }

    // Cada encomenda pertence a uma única loja. Quando o carrinho do
    // comprador tem artigos de várias lojas, o checkout cria uma
    // Encomenda por loja, todas partilhando o mesmo GrupoId.
    public int LojaId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Loja? Loja { get; set; }

    public Guid GrupoId { get; set; }

    public List<ItemEncomenda> Itens { get; set; } = new();
}
