using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

namespace MissaoBackend.Controllers;

// Permite avisar os utilizadores da app móvel quando há uma versão nova —
// necessário porque, distribuída fora da Play Store (ou mesmo lá, sem usar
// o mecanismo de "in-app updates" do Google), a app não sabe sozinha que
// existe uma versão mais recente. Guarda os valores na tabela genérica
// ConfiguracaoSistema (chave/valor), sem precisar de uma tabela dedicada.
[ApiController]
[Route("api/versao-app")]
public class VersaoAppController : ControllerBase
{
    private readonly AppDbContext _db;
    public VersaoAppController(AppDbContext db) => _db = db;

    private const string ChaveRecomendada = "versao_app_recomendada";
    private const string ChaveMinima = "versao_app_minima";
    private const string ChaveUrl = "versao_app_url_download";
    private const string ChaveMensagem = "versao_app_mensagem";

    public record VersaoAppDto(string? VersaoRecomendada, string? VersaoMinima, string? UrlDownload, string? Mensagem);

    [HttpGet]
    public async Task<ActionResult<VersaoAppDto>> Get()
    {
        var valores = await _db.ConfiguracaoSistema
            .Where(c => c.Chave == ChaveRecomendada || c.Chave == ChaveMinima || c.Chave == ChaveUrl || c.Chave == ChaveMensagem)
            .ToDictionaryAsync(c => c.Chave, c => c.Valor);

        return Ok(new VersaoAppDto(
            valores.GetValueOrDefault(ChaveRecomendada),
            valores.GetValueOrDefault(ChaveMinima),
            valores.GetValueOrDefault(ChaveUrl),
            valores.GetValueOrDefault(ChaveMensagem)
        ));
    }

    [HttpPut]
    [Authorize(Policy = "Gestor")]
    public async Task<IActionResult> Atualizar([FromBody] VersaoAppDto input)
    {
        await DefinirAsync(ChaveRecomendada, input.VersaoRecomendada);
        await DefinirAsync(ChaveMinima, input.VersaoMinima);
        await DefinirAsync(ChaveUrl, input.UrlDownload);
        await DefinirAsync(ChaveMensagem, input.Mensagem);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task DefinirAsync(string chave, string? valor)
    {
        var registo = await _db.ConfiguracaoSistema.FirstOrDefaultAsync(c => c.Chave == chave);
        if (registo == null)
        {
            _db.ConfiguracaoSistema.Add(new ConfiguracaoSistema { Chave = chave, Valor = valor ?? "" });
        }
        else
        {
            registo.Valor = valor ?? "";
        }
    }
}
