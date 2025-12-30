using System.ComponentModel.DataAnnotations;

namespace MissaoBackend.Models
{
    public class CatecismoUb
    {
        [Key]
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Texto { get; set; }

        public int CatecismoUbTopicoId { get; set; }
        public CatecismoUbTopico? CatecismoUbTopico { get; set; }
    }
}