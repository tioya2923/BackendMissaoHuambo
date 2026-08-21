using System.Linq;

namespace MissaoBackend.Models;

// Catálogo das moedas suportadas — cada loja indica a sua ao registar-se, conforme o
// país onde opera, e todos os seus produtos/encomendas usam essa moeda. Não há
// conversão automática entre moedas: cada loja vende na sua própria.
public static class Moeda
{
    public const string AOA = "AOA"; // Kwanza — Angola
    public const string EUR = "EUR"; // Euro — Portugal
    public const string BRL = "BRL"; // Real — Brasil
    public const string MZN = "MZN"; // Metical — Moçambique
    public const string CVE = "CVE"; // Escudo — Cabo Verde
    public const string USD = "USD"; // Dólar — outros países

    public static readonly string[] Todas = { AOA, EUR, BRL, MZN, CVE, USD };

    public static bool EhValida(string moeda) => Todas.Contains(moeda);

    // Formatação simples só para uso em texto simples/HTML gerado no servidor (ex.:
    // emails) — a app (web/mobile) formata os valores do seu lado, com o mesmo critério.
    public static string Formatar(decimal valor, string moeda) => moeda switch
    {
        EUR => $"€{valor:0.00}",
        BRL => $"R$ {valor:0.00}",
        USD => $"${valor:0.00}",
        MZN => $"{valor:0.00} MT",
        CVE => $"{valor:0.00}$",
        _ => $"{valor:0.00} Kz",
    };
}
