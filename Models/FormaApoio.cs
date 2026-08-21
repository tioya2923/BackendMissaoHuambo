namespace MissaoBackend.Models;

public class FormaApoio
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Ativo { get; set; } = true;
    public int Ordem { get; set; } = 0;

    // Moeda em que esta forma de apoio recebe (AOA, EUR, BRL, MZN, CVE, USD) —
    // permite ter, por exemplo, um IBAN para quem apoia a partir de Portugal
    // e uma referência Multicaixa para quem apoia a partir de Angola.
    public string Moeda { get; set; } = MissaoBackend.Models.Moeda.AOA;
}
