using System.ComponentModel.DataAnnotations;

namespace MissaoBackend.Models;

public class CatecismoOtc
{
    [Key]
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public string? Slug { get; set; }

    public int CatecismoOtcTopicoId { get; set; }
    public CatecismoOtcTopico? CatecismoOtcTopico { get; set; }
}
