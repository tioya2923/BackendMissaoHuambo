using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

namespace MissaoBackend.Services;

// A Ndatava não cobra comissão às lojas parceiras. Em vez disso, perto do fim de cada
// mês, este serviço envia um email a cada loja aprovada e ativa a agradecer e a
// convidar (sem qualquer obrigação) para uma doação voluntária, segundo as suas
// possibilidades, para ajudar a manter o serviço — com um link para a página pública
// de apoio. Nunca é uma cobrança nem uma condição para continuar a vender.
public class LembreteApoioService : BackgroundService
{
    private const string ChaveEstado = "UltimoMesLembreteApoio";

    private static readonly string[] MesesPt =
    {
        "janeiro", "fevereiro", "março", "abril", "maio", "junho",
        "julho", "agosto", "setembro", "outubro", "novembro", "dezembro",
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LembreteApoioService> _logger;

    public LembreteApoioService(IServiceScopeFactory scopeFactory, ILogger<LembreteApoioService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Pequeno atraso inicial para deixar a aplicação arrancar por completo.
        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var enviou = await VerificarEEnviarSeNecessarioAsync(db, config, DateTime.UtcNow, stoppingToken);
                if (enviou)
                    _logger.LogInformation("Lembrete mensal de apoio enviado às lojas.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar/enviar o lembrete mensal de apoio.");
            }

            try { await Task.Delay(TimeSpan.FromHours(6), stoppingToken); }
            catch (TaskCanceledException) { return; }
        }
    }

    // Só envia perto do fim do mês (últimos dois dias), e no máximo uma vez por mês —
    // controlado pela chave gravada em ConfiguracaoSistema, para sobreviver a reinícios.
    public static async Task<bool> VerificarEEnviarSeNecessarioAsync(
        AppDbContext db, IConfiguration config, DateTime agora, CancellationToken ct = default)
    {
        var diasNoMes = DateTime.DaysInMonth(agora.Year, agora.Month);
        var pertoDoFimDoMes = agora.Day >= diasNoMes - 1;
        if (!pertoDoFimDoMes) return false;

        var chaveMes = agora.ToString("yyyy-MM");
        var estado = await db.ConfiguracaoSistema.FirstOrDefaultAsync(c => c.Chave == ChaveEstado, ct);
        if (estado?.Valor == chaveMes) return false; // já enviado este mês

        await EnviarParaTodasAsLojasAsync(db, config, agora, ct);

        if (estado == null)
            db.ConfiguracaoSistema.Add(new ConfiguracaoSistema { Chave = ChaveEstado, Valor = chaveMes });
        else
            estado.Valor = chaveMes;

        await db.SaveChangesAsync(ct);
        return true;
    }

    // Envia a todas as lojas aprovadas e ativas, com o resumo de vendas do mês de
    // "referencia". Não depende do estado gravado — usada também pelo endpoint de
    // envio manual, para testar sem esperar pelo fim do mês.
    public static async Task<int> EnviarParaTodasAsLojasAsync(
        AppDbContext db, IConfiguration config, DateTime referencia, CancellationToken ct = default)
    {
        var inicioMes = new DateTime(referencia.Year, referencia.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimMes = inicioMes.AddMonths(1);

        var lojas = await db.Lojas
            .AsNoTracking()
            .Where(l => l.Aprovada && l.Ativa)
            .ToListAsync(ct);

        var enviados = 0;
        foreach (var loja in lojas)
        {
            if (string.IsNullOrWhiteSpace(loja.Email)) continue;

            var vendas = await db.Encomendas
                .AsNoTracking()
                .Where(e => e.LojaId == loja.Id && e.Data >= inicioMes && e.Data < fimMes
                    && e.Estado != EstadoEncomenda.Cancelada)
                .ToListAsync(ct);

            var totalVendido = vendas.Sum(e => e.Total);
            var assunto = $"Obrigado por fazer parte da Ndatava, {loja.Nome}";
            var corpo = MontarEmail(loja.Nome, referencia, totalVendido, vendas.Count, loja.Moeda, config);

            await EmailService.EnviarAsync(config, loja.Email, assunto, corpo);
            enviados++;
        }
        return enviados;
    }

    private static string MontarEmail(
        string nomeLoja, DateTime referencia, decimal totalVendido, int numeroEncomendas, string moeda, IConfiguration config)
    {
        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL")
            ?? config["Frontend:BaseUrl"]
            ?? "https://missao-no-huambo-frontend-b3583f0178f6.herokuapp.com";
        var linkApoiar = $"{frontendUrl.TrimEnd('/')}/apoiar";

        var nomeMes = $"{MesesPt[referencia.Month - 1]} de {referencia.Year}";

        var linhaVendas = numeroEncomendas > 0
            ? $"<p>Em {nomeMes}, a sua loja recebeu <strong>{numeroEncomendas}</strong> " +
              $"encomenda{(numeroEncomendas != 1 ? "s" : "")} através da Ndatava, no valor de " +
              $"<strong>{Moeda.Formatar(totalVendido, moeda)}</strong>.</p>"
            : $"<p>Em {nomeMes} ainda não teve encomendas através da Ndatava — esperamos que o " +
              "próximo mês traga mais movimento à sua loja!</p>";

        return $@"
            <div style='font-family:sans-serif;color:#222;line-height:1.6;max-width:520px;'>
                <h2 style='margin-bottom:4px;'>Obrigado por fazer parte da Ndatava</h2>
                {linhaVendas}
                <p>
                    Lembramos que a Ndatava <strong>não cobra nenhuma comissão nem taxa</strong> sobre
                    as vendas da sua loja — todo o valor das suas encomendas é inteiramente seu.
                </p>
                <p>
                    A manutenção da aplicação (servidor, base de dados, desenvolvimento) tem custos e,
                    por isso, se puder e quiser, gostaríamos de o convidar a fazer uma
                    <strong>doação voluntária</strong>, segundo as suas possibilidades, para nos ajudar
                    a continuar. Não é uma cobrança nem uma condição para continuar a vender — é só um
                    convite, sem qualquer obrigação.
                </p>
                <p style='margin-top:22px;'>
                    <a href='{linkApoiar}'
                       style='background:#181818;color:#ffffff;padding:12px 22px;border-radius:8px;
                              text-decoration:none;font-weight:bold;display:inline-block;'>
                        Conhecer as formas de apoiar
                    </a>
                </p>
                <p style='color:#666;font-size:13px;margin-top:26px;'>
                    Obrigado, {nomeLoja}, por fazer parte desta comunidade.
                </p>
            </div>";
    }
}
