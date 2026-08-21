using System.Linq;

namespace MissaoBackend.Models;

// Catálogo dos métodos de pagamento disponíveis — abrange os países onde a
// Ndatava tem lojas (Angola, Portugal, Brasil, Moçambique, Cabo Verde) mais
// alguns genéricos internacionais. Cada loja escolhe livremente quais aceita
// (um, vários, ou todos) e preenche o "detalhe" respetivo quando aplicável
// (número de telefone, IBAN, referência, etc.). A validação aqui é só "este
// código existe" — qual subconjunto faz sentido para cada moeda é uma decisão
// de interface (ver constants/metodosPagamento nos clientes web e mobile).
public static class MetodoPagamento
{
    // Genéricos — fazem sentido em qualquer país
    public const string Dinheiro = "dinheiro";                            // presencial, na entrega
    public const string TransferenciaBancaria = "transferencia_bancaria"; // IBAN
    public const string CartaoPOS = "cartao_pos";                         // cartão na entrega/levantamento

    // Angola
    public const string MulticaixaExpress = "multicaixa_express";         // EMIS — número de telefone
    public const string ReferenciaMulticaixa = "referencia_multicaixa";   // entidade + referência ATM
    public const string UnitelMoney = "unitel_money";                     // número de telefone
    public const string PayPay = "paypay";                                // número/utilizador PayPay

    // Portugal
    public const string MBWay = "mbway";                                  // número de telemóvel
    public const string ReferenciaMultibanco = "referencia_multibanco";   // entidade + referência ATM

    // Brasil
    public const string Pix = "pix";                                      // chave Pix
    public const string Boleto = "boleto";                                // boleto bancário

    // Moçambique
    public const string MPesa = "mpesa";                                  // Vodacom — número de telefone
    public const string EMola = "emola";                                  // Movitel — número de telefone
    public const string MKesh = "mkesh";                                  // tmcel/Millennium bim — número de telefone

    // Cabo Verde
    public const string Vinti4 = "vinti4";                                // rede interbancária SISP

    // Internacionais
    public const string PayPal = "paypal";
    public const string Wise = "wise";

    public static readonly string[] Todos =
    {
        Dinheiro, TransferenciaBancaria, CartaoPOS,
        MulticaixaExpress, ReferenciaMulticaixa, UnitelMoney, PayPay,
        MBWay, ReferenciaMultibanco,
        Pix, Boleto,
        MPesa, EMola, MKesh,
        Vinti4,
        PayPal, Wise,
    };

    public static bool EhValido(string metodo) => Todos.Contains(metodo);
}
