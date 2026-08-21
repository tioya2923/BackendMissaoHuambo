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

    // Moeda em que a loja vende (todos os seus produtos e encomendas). Escolhida pela
    // própria loja no registo, conforme o país onde opera — não há conversão automática.
    public string Moeda { get; set; } = MissaoBackend.Models.Moeda.AOA;

    // Métodos de pagamento que a loja aceita (dinheiro, Multicaixa Express, IBAN, etc.)
    // — cada um com o seu detalhe (nº de telefone, IBAN, referência...). A loja escolhe
    // livremente quais aceita. Mostrados ao comprador na confirmação da encomenda.
    public List<FormaPagamentoLoja> FormasPagamento { get; set; } = new();

    // Instruções adicionais em texto livre (ex.: horário de levantamento, nome do
    // titular da conta) — complementam as formas de pagamento estruturadas acima.
    public string? InfoPagamento { get; set; }

    // Histórico: a Ndatava chegou a cobrar comissão às lojas, mas deixou de o fazer —
    // agora a manutenção do serviço é sustentada por doações voluntárias (ver
    // LembreteApoioService). Este campo já não é usado no cálculo de nenhuma encomenda
    // (fica sempre a 0m no momento da compra); mantido só para não partir o esquema.
    public decimal PercentualComissao { get; set; } = 0m;

    // Uma loja nova só aparece nas pesquisas depois de aprovada pelo administrador
    public bool Aprovada { get; set; } = false;

    // A própria loja pode pausar-se (deixa de aparecer) sem precisar de eliminar a conta
    public bool Ativa { get; set; } = true;

    public DateTime DataRegisto { get; set; } = DateTime.UtcNow;
}
