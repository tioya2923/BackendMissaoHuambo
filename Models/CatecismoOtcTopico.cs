using System.ComponentModel.DataAnnotations;

namespace MissaoBackend.Models;

public class CatecismoOtcTopico
{
    [Key]
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Slug { get; set; }

    public List<CatecismoOtc> CatecismosOtc { get; set; } = new();
}
