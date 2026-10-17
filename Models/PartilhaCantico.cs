namespace MissaoBackend.Models;

// Cântico enviado por um utilizador a outro dentro da app (uma linha por destinatário).
public class PartilhaCantico
{
    public int Id { get; set; }
    public int RemetenteId { get; set; }
    public int DestinatarioId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Letra { get; set; } = string.Empty;
    public string? Momento { get; set; }
    public int? AudioId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? LidoEm { get; set; }
}

// Gravação de voz anexada a uma partilha. Só o dono a pode anexar e só destinatários a podem ouvir.
public class AudioPartilha
{
    public int Id { get; set; }
    public int DonoId { get; set; }
    public string ContentType { get; set; } = "audio/mp4";
    public byte[] Dados { get; set; } = Array.Empty<byte>();
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public class BloqueioUtilizador
{
    public int Id { get; set; }
    public int UtilizadorId { get; set; }
    public int BloqueadoId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// Ligação entre dois utilizadores que se encontraram na pesquisa. Começa como pedido (Aceite = false)
// e só depois de o destinatário aceitar passa a contacto, nos dois sentidos.
public class ContactoUtilizador
{
    public int Id { get; set; }
    public int SolicitanteId { get; set; }
    public int DestinatarioId { get; set; }
    public bool Aceite { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// Denúncia de uma partilha. Guarda uma cópia do conteúdo, para o gestor poder avaliar
// mesmo que o destinatário entretanto apague a partilha.
public class DenunciaPartilha
{
    public int Id { get; set; }
    public int? DenuncianteId { get; set; }
    public int? RemetenteId { get; set; }
    public string RemetenteNome { get; set; } = string.Empty;
    public string RemetenteCodigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Letra { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
