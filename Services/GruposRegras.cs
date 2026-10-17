using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

namespace MissaoBackend.Services;

// Regras partilhadas entre os controladores de grupos, partilhas e utilizadores.
public static class GruposRegras
{
    /// <summary>Apaga as gravações que já não estão ligadas a nenhuma partilha nem publicação.</summary>
    public static async Task LimparAudiosSemUso(AppDbContext db, List<int> audioIds)
    {
        if (audioIds.Count == 0) return;
        var semUso = await db.AudiosPartilha
            .Where(a => audioIds.Contains(a.Id)
                && !db.PartilhasCanticos.Any(p => p.AudioId == a.Id)
                && !db.PublicacoesAudio.Any(p => p.AudioId == a.Id))
            .ToListAsync();
        if (semUso.Count == 0) return;
        db.AudiosPartilha.RemoveRange(semUso);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Tira uma pessoa de um grupo. Se era a única administradora, passa a administração ao membro
    /// mais antigo; se não sobrar ninguém, o grupo é apagado.
    /// </summary>
    public static async Task RetirarMembro(AppDbContext db, int grupoId, int utilizadorId)
    {
        var membro = await db.MembrosGrupo.FirstOrDefaultAsync(m => m.GrupoId == grupoId && m.UtilizadorId == utilizadorId);
        if (membro == null) return;
        var eraAdmin = membro.Administrador && membro.Estado == EstadoMembro.Activo;
        db.MembrosGrupo.Remove(membro);
        await db.SaveChangesAsync();

        // As publicações da pessoa saem com ela.
        var minhas = await db.PublicacoesGrupo.Where(p => p.GrupoId == grupoId && p.AutorId == utilizadorId).Select(p => p.Id).ToListAsync();
        var audios = minhas.Count == 0 ? new List<int>() : await db.PublicacoesAudio.Where(a => minhas.Contains(a.PublicacaoId)).Select(a => a.AudioId).ToListAsync();
        if (minhas.Count > 0)
        {
            db.PublicacoesGrupo.RemoveRange(await db.PublicacoesGrupo.Where(p => minhas.Contains(p.Id)).ToListAsync());
            await db.SaveChangesAsync();
        }

        if (eraAdmin)
        {
            var outrosAdmins = await db.MembrosGrupo.AnyAsync(m => m.GrupoId == grupoId && m.Estado == EstadoMembro.Activo && m.Administrador);
            if (!outrosAdmins)
            {
                var proximo = await db.MembrosGrupo.Where(m => m.GrupoId == grupoId && m.Estado == EstadoMembro.Activo)
                    .OrderBy(m => m.EntrouEm ?? m.CriadoEm).ThenBy(m => m.Id).FirstOrDefaultAsync();
                if (proximo != null) { proximo.Administrador = true; await db.SaveChangesAsync(); }
                else audios.AddRange(await ApagarGrupo(db, grupoId));
            }
        }
        await LimparAudiosSemUso(db, audios.Distinct().ToList());
    }

    /// <summary>Apaga o grupo (as publicações e ligações saem em cascata) e devolve as gravações que ficam sem uso.</summary>
    public static async Task<List<int>> ApagarGrupo(AppDbContext db, int grupoId)
    {
        var audios = await (from a in db.PublicacoesAudio
                            join p in db.PublicacoesGrupo on a.PublicacaoId equals p.Id
                            where p.GrupoId == grupoId
                            select a.AudioId).Distinct().ToListAsync();
        var g = await db.Grupos.FindAsync(grupoId);
        if (g != null) { db.Grupos.Remove(g); await db.SaveChangesAsync(); }
        return audios;
    }

    /// <summary>Antes de eliminar uma conta: entrega a administração dos grupos que ficariam sem administrador.</summary>
    public static async Task PrepararEliminacaoConta(AppDbContext db, int utilizadorId)
    {
        var grupos = await db.MembrosGrupo.Where(m => m.UtilizadorId == utilizadorId).Select(m => m.GrupoId).ToListAsync();
        foreach (var gid in grupos) await RetirarMembro(db, gid, utilizadorId);
    }
}
