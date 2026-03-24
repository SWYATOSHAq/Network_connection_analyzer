namespace Анализатор_сетевых_подключений.Models;

public class UrlAnalysisModel
{
    public string OriginalUrl { get; init; } = string.Empty;
    public string Scheme     { get; init; } = string.Empty;
    public string Host       { get; init; } = string.Empty;
    public string Port       { get; init; } = string.Empty;
    public string Path       { get; init; } = string.Empty;
    public string Query      { get; init; } = string.Empty;
    public string Fragment   { get; init; } = string.Empty;
    public string AddressType { get; init; } = string.Empty;

    public string PingResult  { get; set; } = "Проверяется...";
    public string DnsResult   { get; set; } = "Запрос...";
    public string DnsAliases  { get; set; } = "—";
}
