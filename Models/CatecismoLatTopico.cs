using System.ComponentModel.DataAnnotations;

namespace MissaoBackend.Models;

public class CatecismoLatTopico
{
    [Key]
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Slug { get; set; }

    public List<CatecismoLat> CatecismosLat { get; set; } = new();
}
