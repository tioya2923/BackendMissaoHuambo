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

    public List<ItemEncomenda> Itens { get; set; } = new();
}
