using System.Text.Json.Serialization;

namespace MissaoBackend.Models;

// Um método de pagamento que uma loja aceita, com o respetivo detalhe (número de
// telefone Multicaixa Express/Unitel Money/PayPay, IBAN, referência ATM, etc.).
// "Dinheiro" e "Cartão POS" normalmente não precisam de detalhe.
public class FormaPagamentoLoja
{
    public int Id { get; set; }

    public int LojaId { get; set; }
    [JsonIgnore]
    public Loja? Loja { get; set; }

    public string Metodo { get; set; } = string.Empty;
    public string? Detalhe { get; set; }
}
