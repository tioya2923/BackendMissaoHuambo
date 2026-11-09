namespace MissaoBackend.Models;

// Código de 6 dígitos enviado por email para redefinir a palavra-passe de uma conta de utilizador.
// Guarda-se só o hash do código, com validade curta, poucas tentativas e uso único.
public class RecuperacaoSenha
{
    public int Id { get; set; }
    public int UtilizadorId { get; set; }
    public string CodigoHash { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime ExpiraEm { get; set; }
    public int Tentativas { get; set; }
    public bool Usado { get; set; }
}
