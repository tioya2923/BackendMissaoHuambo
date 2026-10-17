namespace MissaoBackend.Models;

public class Utilizador
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FotoUrl { get; set; }

    // Código curto (ex.: NDT-7K4PQ) que o utilizador dá a quem lhe quer enviar cânticos.
    public string? Codigo { get; set; }

    // Datas de sacramentos serializadas como "DD/MM/YYYY"
    public string? Nascimento { get; set; }
    public string? Baptismo { get; set; }
    public string? Comunhao { get; set; }
    public string? Crisma { get; set; }
    public string? Casamento { get; set; }
    public string? Ordem { get; set; }

    // Comunidade
    public string? Diocese { get; set; }
    public string? Paroquia { get; set; }
}
