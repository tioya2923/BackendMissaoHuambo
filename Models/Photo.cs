using System;

namespace MissaoBackend.Models
{
    public class Photo
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string DescricaoCurta { get; set; } = string.Empty;
        public string DescricaoLonga { get; set; } = string.Empty;
        public DateTime DataUpload { get; set; } = DateTime.UtcNow;
    }
}