using System;

namespace MissaoBackend.Models
{
    public class Photo
    {
        public int Id { get; set; }
        public required string Url { get; set; }
        public required string DescricaoCurta { get; set; }
        public required string DescricaoLonga { get; set; }
        public DateTime DataUpload { get; set; } = DateTime.UtcNow;
    }
}