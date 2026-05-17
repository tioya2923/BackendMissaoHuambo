using System.ComponentModel.DataAnnotations;

namespace MissaoBackend.Models;

public class CatecismoLat
{
    [Key]
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public string? Slug { get; set; }

    public int CatecismoLatTopicoId { get; set; }
    public CatecismoLatTopico? CatecismoLatTopico { get; set; }
}
