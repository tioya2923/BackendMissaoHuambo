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

// ── Grupos (coral, oração, pastoral…) ─────────────────────────────────────────

public static class EstadoMembro
{
    public const int Convidado = 0;   // um administrador convidou; falta a pessoa aceitar
    public const int Pedido = 1;      // a pessoa pediu para entrar com o código; falta um administrador aprovar
    public const int Activo = 2;
}

public class Grupo
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = "outro";
    public string? Descricao { get; set; }
    public string? Paroquia { get; set; }
    // Código para pedir entrada (ex.: GRP-4K7PQ). Quem o usa só entra depois de um administrador aprovar.
    public string Codigo { get; set; } = string.Empty;
    public bool CodigoActivo { get; set; } = true;
    // Se verdadeiro, qualquer membro pode publicar cânticos e orações (avisos e programas são só dos administradores).
    public bool MembrosPublicam { get; set; }
    public int? CriadoPorId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public class MembroGrupo
{
    public int Id { get; set; }
    public int GrupoId { get; set; }
    public int UtilizadorId { get; set; }
    public bool Administrador { get; set; }
    public int Estado { get; set; } = EstadoMembro.Activo;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? EntrouEm { get; set; }
    // Última publicação vista no mural: o que tiver id maior e não for da própria pessoa conta como novo.
    public int UltimaLeituraId { get; set; }
}

public static class TipoPublicacao
{
    public const string Aviso = "aviso";
    public const string Cantico = "cantico";
    public const string Oracao = "oracao";
    public const string Programa = "programa";
    public static readonly string[] Todos = { Aviso, Cantico, Oracao, Programa };
}

public class PublicacaoGrupo
{
    public int Id { get; set; }
    public int GrupoId { get; set; }
    public int AutorId { get; set; }
    public string Tipo { get; set; } = TipoPublicacao.Aviso;
    public string Titulo { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public string? Momento { get; set; }
    // Programa: lista de cânticos por momento da Missa (JSON), com letra dos cânticos próprios.
    public string? ItensJson { get; set; }
    public bool Fixada { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? EditadoEm { get; set; }
}

public class PublicacaoAudio
{
    public int Id { get; set; }
    public int PublicacaoId { get; set; }
    public int AudioId { get; set; }
}

// Quem já viu um aviso (para o administrador saber quem leu).
public class VistoPublicacao
{
    public int Id { get; set; }
    public int PublicacaoId { get; set; }
    public int UtilizadorId { get; set; }
    public DateTime VistoEm { get; set; } = DateTime.UtcNow;
}
