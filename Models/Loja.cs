using System.Text.Json.Serialization;

namespace MissaoBackend.Models;

public class Loja
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [JsonIgnore]
    public string Password { get; set; } = string.Empty;

    public string? Descricao { get; set; }
    public string? Telefone { get; set; }
    public string? Morada { get; set; }
    public string? Categoria { get; set; }

    // Coordenadas usadas para calcular a distância até ao comprador
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    // Métodos de pagamento que a loja aceita (dinheiro, Multicaixa Express, IBAN, etc.)
    // — cada um com o seu detalhe (nº de telefone, IBAN, referência...). A loja escolhe
    // livremente quais aceita. Mostrados ao comprador na confirmação da encomenda.
    public List<FormaPagamentoLoja> FormasPagamento { get; set; } = new();

    // Instruções adicionais em texto livre (ex.: horário de levantamento, nome do
    // titular da conta) — complementam as formas de pagamento estruturadas acima.
    public string? InfoPagamento { get; set; }

    // Percentagem que a plataforma retém de cada encomenda desta loja (comissão).
    // Aplicada e guardada em cada Encomenda no momento da compra; o Gestor pode
    // ajustá-la por loja ao moderar. O acerto de contas com a loja é feito à parte
    // (fora da app), com base no total de comissão acumulado.
    public decimal PercentualComissao { get; set; } = 10m;

    // Uma loja nova só aparece nas pesquisas depois de aprovada pelo administrador
    public bool Aprovada { get; set; } = false;

    // A própria loja pode pausar-se (deixa de aparecer) sem precisar de eliminar a conta
    public bool Ativa { get; set; } = true;

    public DateTime DataRegisto { get; set; } = DateTime.UtcNow;
}
