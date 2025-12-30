using System;

namespace MissaoBackend.Models
{
    public class Photo
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public string DescricaoCurta { get; set; }
        public string DescricaoLonga { get; set; }
        public DateTime DataUpload { get; set; } = DateTime.UtcNow;
    }
}