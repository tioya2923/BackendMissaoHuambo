namespace MissaoBackend.Models;

// Código de 6 dígitos enviado por email para redefinir a palavra-passe de uma loja (área do vendedor).
// Funciona como o das contas de utilizador: só se guarda o hash, com validade curta, poucas tentativas e uso único.
public class RecuperacaoSenhaLoja
{
    public int Id { get; set; }
    public int LojaId { get; set; }
    public string CodigoHash { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime ExpiraEm { get; set; }
    public int Tentativas { get; set; }
    public bool Usado { get; set; }
}
