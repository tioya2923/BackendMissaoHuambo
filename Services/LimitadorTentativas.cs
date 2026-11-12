using System.Collections.Concurrent;

namespace MissaoBackend.Services;

/// <summary>
/// Limite simples, em memória, de tentativas numa janela de tempo (por exemplo, por email ou por endereço).
/// Serve para travar adivinhação de palavras-passe e criação massiva de contas. Reinicia com o servidor,
/// o que é aceitável para este fim.
/// </summary>
public class LimitadorTentativas
{
    private readonly int _maximo;
    private readonly TimeSpan _janela;
    private readonly ConcurrentDictionary<string, List<DateTime>> _registos = new();
    private DateTime _ultimaLimpeza = DateTime.UtcNow;

    public LimitadorTentativas(int maximo, TimeSpan janela)
    {
        _maximo = maximo;
        _janela = janela;
    }

    /// <summary>Regista uma tentativa. Devolve falso se a chave já atingiu o máximo na janela.</summary>
    public bool Permitir(string chave)
    {
        var agora = DateTime.UtcNow;
        LimparSeNecessario(agora);
        var lista = _registos.GetOrAdd(chave, _ => new List<DateTime>());
        lock (lista)
        {
            lista.RemoveAll(t => agora - t > _janela);
            if (lista.Count >= _maximo) return false;
            lista.Add(agora);
            return true;
        }
    }

    /// <summary>Só regista (sem bloquear): usado para contar falhas de início de sessão.</summary>
    public void Registar(string chave)
    {
        var lista = _registos.GetOrAdd(chave, _ => new List<DateTime>());
        lock (lista) lista.Add(DateTime.UtcNow);
    }

    /// <summary>Indica se a chave já atingiu o máximo, sem registar nova tentativa.</summary>
    public bool Excedeu(string chave)
    {
        if (!_registos.TryGetValue(chave, out var lista)) return false;
        var agora = DateTime.UtcNow;
        lock (lista)
        {
            lista.RemoveAll(t => agora - t > _janela);
            return lista.Count >= _maximo;
        }
    }

    public void Limpar(string chave) => _registos.TryRemove(chave, out _);

    private void LimparSeNecessario(DateTime agora)
    {
        if (agora - _ultimaLimpeza < TimeSpan.FromMinutes(10)) return;
        _ultimaLimpeza = agora;
        foreach (var par in _registos)
        {
            lock (par.Value)
            {
                par.Value.RemoveAll(t => agora - t > _janela);
                if (par.Value.Count == 0) _registos.TryRemove(par.Key, out _);
            }
        }
    }
}

/// <summary>
/// Travão ao início de sessão. A falha conta por e-mail + endereço (8 em 15 min), para que um terceiro não consiga
/// bloquear a conta de outra pessoa só errando a palavra-passe; e há um teto mais alto só por e-mail (40 em 15 min)
/// contra tentativas repartidas por muitos endereços.
/// </summary>
public class ProtecaoLogin
{
    private readonly LimitadorTentativas _porEmailEEndereco = new(8, TimeSpan.FromMinutes(15));
    private readonly LimitadorTentativas _porEmail = new(40, TimeSpan.FromMinutes(15));

    public bool Bloqueado(string email, string endereco) =>
        _porEmailEEndereco.Excedeu(email + "|" + endereco) || _porEmail.Excedeu(email);

    public void Falhou(string email, string endereco)
    {
        _porEmailEEndereco.Registar(email + "|" + endereco);
        _porEmail.Registar(email);
    }

    public void Sucesso(string email, string endereco)
    {
        _porEmailEEndereco.Limpar(email + "|" + endereco);
        _porEmail.Limpar(email);
    }

    /// <summary>Depois de redefinir a palavra-passe por código: levanta o bloqueio global do e-mail.</summary>
    public void LimparEmail(string email) => _porEmail.Limpar(email);
}

public static class OrigemDoPedido
{
    /// <summary>
    /// Endereço de quem fez o pedido. O primeiro valor de X-Forwarded-For é escrito pelo próprio cliente (qualquer um
    /// pode enviar um inventado), por isso não serve para limites. Em produção o tráfego passa pelo Cloudflare do Render,
    /// que escreve sempre CF-Connecting-IP com o endereço real; sem esse cabeçalho (ex.: testes locais) usa-se a ligação direta.
    /// </summary>
    public static string Endereco(Microsoft.AspNetCore.Http.HttpContext contexto)
    {
        var cf = contexto.Request.Headers["CF-Connecting-IP"].ToString().Trim();
        if (!string.IsNullOrEmpty(cf) && cf.Length <= 45 && System.Net.IPAddress.TryParse(cf, out _)) return cf;
        return contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
    }
}
