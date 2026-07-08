namespace TipMolde.Models;

public sealed class ProjetoDto : ProjetoBaseDto
{
    public string ResumoDisplay => $"{NomeProjetoDisplay} | {TipoProjetoDisplay}";
}
