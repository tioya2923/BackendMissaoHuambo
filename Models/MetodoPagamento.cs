using System.Linq;

namespace MissaoBackend.Models;

// Catálogo fixo dos métodos de pagamento disponíveis em Angola. Cada loja escolhe
// livremente quais aceita (um, vários, ou todos) e preenche o "detalhe" respetivo
// quando aplicável (número de telefone, IBAN, referência, etc.).
public static class MetodoPagamento
{
    public const string Dinheiro = "dinheiro";                            // presencial, na entrega
    public const string MulticaixaExpress = "multicaixa_express";         // EMIS — número de telefone
    public const string ReferenciaMulticaixa = "referencia_multicaixa";   // entidade + referência ATM
    public const string TransferenciaBancaria = "transferencia_bancaria"; // IBAN
    public const string UnitelMoney = "unitel_money";                     // número de telefone
    public const string PayPay = "paypay";                                // número/utilizador PayPay
    public const string CartaoPOS = "cartao_pos";                         // cartão na entrega/levantamento

    public static readonly string[] Todos =
    {
        Dinheiro, MulticaixaExpress, ReferenciaMulticaixa, TransferenciaBancaria, UnitelMoney, PayPay, CartaoPOS,
    };

    public static bool EhValido(string metodo) => Todos.Contains(metodo);
}
