namespace MissaoBackend.Models;

// Pequena tabela chave/valor para guardar estado simples da aplicação — por exemplo,
// qual foi o último mês em que o lembrete de apoio foi enviado às lojas — sem precisar
// de criar uma tabela dedicada para cada coisa deste género.
public class ConfiguracaoSistema
{
    public int Id { get; set; }
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
}
