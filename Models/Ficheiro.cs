namespace MissaoBackend.Models;

// Armazenamento de ficheiros (fotos, PDFs) dentro da própria base de dados —
// ao contrário do disco do servidor (WebRootPath), isto sobrevive a
// redeploys, porque a base de dados é persistente e o disco do serviço
// web não é.
public class Ficheiro
{
    public int Id { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
    public byte[] Dados { get; set; } = Array.Empty<byte>();
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
