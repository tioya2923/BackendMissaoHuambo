using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MissaoBackend.Data;
using MissaoBackend.Services;

namespace MissaoBackend.Controllers
{
    // Permite ao Gestor disparar manualmente o lembrete mensal de apoio às lojas —
    // sobretudo para testar o envio sem ter de esperar pelo fim do mês. O envio
    // automático (LembreteApoioService) continua a acontecer sozinho, uma vez por mês.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "Gestor")]
    public class LembretesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public LembretesController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public record EnviarAgoraRequest(int? Ano, int? Mes);

        // POST /api/lembretes/apoio/enviar-agora — envia já, a todas as lojas aprovadas
        // e ativas, o email de apoio referente ao mês indicado (por defeito, o mês atual).
        // Não depende nem afeta o controlo de "já enviado este mês" do envio automático.
        [HttpPost("apoio/enviar-agora")]
        public async Task<IActionResult> EnviarAgora(EnviarAgoraRequest? req)
        {
            var agora = DateTime.UtcNow;
            var referencia = new DateTime(req?.Ano ?? agora.Year, req?.Mes ?? agora.Month, 1);

            var enviados = await LembreteApoioService.EnviarParaTodasAsLojasAsync(_db, _config, referencia);

            return Ok(new { Enviados = enviados, ReferenteA = referencia.ToString("yyyy-MM") });
        }
    }
}
