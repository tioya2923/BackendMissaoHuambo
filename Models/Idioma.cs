namespace MissaoBackend.Models;

/// <summary>
/// Um idioma disponível na app (Português, Umbundu, Latim, Kimbundu, Otchikwanyama, ...).
/// Cânticos, tópicos de cânticos, catecismo/orações e tópicos de catecismo referenciam
/// um Idioma através de IdiomaId, em vez de existir uma tabela por idioma.
/// </summary>
public class Idioma
{
    public int Id { get; set; }

    /// <summary>Código curto e estável, usado pela app e pelo admin (ex: "pt", "umb", "lat", "kmb", "otc").</summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Nome apresentado ao utilizador (ex: "Português", "Umbundu").</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Ordem de apresentação nos seletores de idioma.</summary>
    public int Ordem { get; set; }

    /// <summary>Permite esconder um idioma sem apagar o seu conteúdo.</summary>
    public bool Ativo { get; set; } = true;
}
