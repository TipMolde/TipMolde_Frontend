using Microsoft.Maui.Storage;
using System.Text;
using TipMolde.Models;

namespace TipMolde.Services;

public sealed class MoldePdfService
{
    public async Task<string> GenerateCicloVidaPdfAsync(
        string numero,
        string nome,
        string descricao,
        string tipoPedido,
        int numeroCavidades,
        MoldeCicloVidaDashboardDto dashboard)
    {
        var fileName = $"ciclo-vida-{SanitizeFileName(string.IsNullOrWhiteSpace(numero) ? dashboard.MoldeId.ToString() : numero)}.pdf";
        var filePath = Path.Combine(FileSystem.Current.AppDataDirectory, fileName);
        var pdfBytes = BuildPdf(
            numero,
            nome,
            descricao,
            tipoPedido,
            numeroCavidades,
            dashboard);

        await File.WriteAllBytesAsync(filePath, pdfBytes);
        return filePath;
    }

    private static byte[] BuildPdf(
        string numero,
        string nome,
        string descricao,
        string tipoPedido,
        int numeroCavidades,
        MoldeCicloVidaDashboardDto dashboard)
    {
        var distribuicaoTotal = dashboard.Maquinacao + dashboard.Erosao + dashboard.Montagem + dashboard.MaterialPendente;
        var linhas = new[]
        {
            "BT",
            "/F1 20 Tf",
            "40 800 Td",
            $"({Escape("Relatorio do Ciclo de Vida do Molde")}) Tj",
            "/F1 12 Tf",
            "0 -30 Td",
            $"({Escape($"Numero: {numero}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Nome: {nome}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Tipo de pedido: {tipoPedido}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Numero de cavidades: {numeroCavidades}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Descricao: {descricao}")}) Tj",
            "0 -30 Td",
            $"({Escape($"Total de pecas: {dashboard.TotalPecas}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Percentagem de conclusao: {dashboard.PercentagemConclusao:0.##}%")}) Tj",
            "0 -18 Td",
            $"({Escape($"Maquinacao: {dashboard.Maquinacao} ({FormatPercent(dashboard.Maquinacao, distribuicaoTotal)})")}) Tj",
            "0 -18 Td",
            $"({Escape($"Erosao: {dashboard.Erosao} ({FormatPercent(dashboard.Erosao, distribuicaoTotal)})")}) Tj",
            "0 -18 Td",
            $"({Escape($"Montagem: {dashboard.Montagem} ({FormatPercent(dashboard.Montagem, distribuicaoTotal)})")}) Tj",
            "0 -18 Td",
            $"({Escape($"Material pendente: {dashboard.MaterialPendente} ({FormatPercent(dashboard.MaterialPendente, distribuicaoTotal)})")}) Tj",
            "0 -18 Td",
            $"({Escape($"Em trabalho: {dashboard.EmTrabalho}")}) Tj",
            "0 -18 Td",
            $"({Escape($"Concluidas: {dashboard.Concluidas}")}) Tj",
            "0 -30 Td",
            $"({Escape($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}")}) Tj",
            "ET"
        };

        var contentStream = string.Join('\n', linhas);
        var contentBytes = Encoding.ASCII.GetBytes(contentStream);

        var objects = new List<byte[]>
        {
            Encoding.ASCII.GetBytes("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"),
            Encoding.ASCII.GetBytes("2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n"),
            Encoding.ASCII.GetBytes("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>\nendobj\n"),
            Encoding.ASCII.GetBytes($"4 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n{contentStream}\nendstream\nendobj\n"),
            Encoding.ASCII.GetBytes("5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n")
        };

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        writer.Write(Encoding.ASCII.GetBytes("%PDF-1.4\n"));

        var offsets = new List<long> { 0 };

        foreach (var obj in objects)
        {
            offsets.Add(stream.Position);
            writer.Write(obj);
        }

        var xrefPosition = stream.Position;
        writer.Write(Encoding.ASCII.GetBytes($"xref\n0 {offsets.Count}\n"));
        writer.Write(Encoding.ASCII.GetBytes("0000000000 65535 f \n"));

        foreach (var offset in offsets.Skip(1))
            writer.Write(Encoding.ASCII.GetBytes($"{offset:D10} 00000 n \n"));

        writer.Write(Encoding.ASCII.GetBytes(
            $"trailer\n<< /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xrefPosition}\n%%EOF"));

        writer.Flush();
        return stream.ToArray();
    }

    private static string FormatPercent(int value, int total)
    {
        if (total <= 0)
            return "0%";

        var percent = (decimal)value / total * 100m;
        return $"{percent:0.#}%";
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Where(character => !invalidChars.Contains(character)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "molde" : sanitized.Trim();
    }

    private static string Escape(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("\r", " ")
            .Replace("\n", " ");
    }
}
