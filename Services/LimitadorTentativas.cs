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

public static class OrigemDoPedido
{
    /// <summary>Endereço de quem fez o pedido. Atrás do proxy do Render vem em X-Forwarded-For (o primeiro da lista).</summary>
    public static string Endereco(Microsoft.AspNetCore.Http.HttpContext contexto)
    {
        var enc = contexto.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
        return !string.IsNullOrEmpty(enc) ? enc : contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
    }
}
