using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Анализатор_сетевых_подключений.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Анализатор_сетевых_подключений.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<NetworkInterfaceModel> _networkInterfaces = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InterfaceSelected))]
    private NetworkInterfaceModel? _selectedInterface;

    [ObservableProperty]
    private string _urlInput = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUrlAnalysis))]
    private UrlAnalysisModel? _urlAnalysis;

    [ObservableProperty]
    private bool _isAnalyzing;

    [ObservableProperty]
    private ObservableCollection<string> _urlHistory = [];

    [ObservableProperty]
    private string? _selectedHistoryItem;

    public bool InterfaceSelected => SelectedInterface != null;
    public bool HasUrlAnalysis    => UrlAnalysis != null;
    public bool HasHistory        => UrlHistory.Count > 0;

    public MainViewModel()
    {
        LoadInterfaces();
        UrlHistory.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasHistory));
    }

    [RelayCommand]
    private void LoadInterfaces()
    {
        NetworkInterfaces.Clear();
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                NetworkInterfaces.Add(new NetworkInterfaceModel(ni));
        }
        catch { /* ignore on unsupported platforms */ }
    }

    [RelayCommand]
    private async Task AnalyzeUrl()
    {
        var url = UrlInput.Trim();
        if (string.IsNullOrEmpty(url)) return;

        IsAnalyzing = true;
        UrlAnalysis = null;

        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                if (!Uri.TryCreate("https://" + url, UriKind.Absolute, out uri))
                    return;
                UrlInput = uri.ToString();
            }

            var analysis = new UrlAnalysisModel
            {
                OriginalUrl  = url,
                Scheme       = uri.Scheme,
                Host         = uri.Host,
                Port         = uri.IsDefaultPort ? $"{uri.Port} (по умолч.)" : uri.Port.ToString(),
                Path         = string.IsNullOrEmpty(uri.AbsolutePath) ? "/" : uri.AbsolutePath,
                Query        = string.IsNullOrEmpty(uri.Query)    ? "—" : uri.Query,
                Fragment     = string.IsNullOrEmpty(uri.Fragment) ? "—" : uri.Fragment,
                AddressType  = GetAddressType(uri.Host),
            };

            var pingTask = Task.Run(async () =>
            {
                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(uri.Host, 3000);
                    return reply.Status == IPStatus.Success
                        ? $"Доступен ({reply.RoundtripTime} мс)"
                        : $"Недоступен ({reply.Status})";
                }
                catch (Exception ex) { return $"Ошибка: {ex.Message}"; }
            });

            var dnsTask = Task.Run(async () =>
            {
                try
                {
                    var entry = await Dns.GetHostEntryAsync(uri.Host);
                    var ips     = string.Join(", ", entry.AddressList.Select(a => a.ToString()));
                    var aliases = entry.Aliases.Length > 0
                        ? string.Join(", ", entry.Aliases)
                        : "—";
                    return (ips: string.IsNullOrEmpty(ips) ? "—" : ips, aliases);
                }
                catch (Exception ex) { return (ips: $"Ошибка: {ex.Message}", aliases: "—"); }
            });

            await Task.WhenAll(pingTask, dnsTask);

            analysis.PingResult = await pingTask;
            var dns = await dnsTask;
            analysis.DnsResult  = dns.ips;
            analysis.DnsAliases = dns.aliases;

            UrlAnalysis = analysis;

            if (!UrlHistory.Contains(url))
            {
                UrlHistory.Insert(0, url);
                if (UrlHistory.Count > 20)
                    UrlHistory.RemoveAt(UrlHistory.Count - 1);
            }
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    [RelayCommand]
    private void ClearHistory()
    {
        UrlHistory.Clear();
        SelectedHistoryItem = null;
    }

    partial void OnSelectedHistoryItemChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
            UrlInput = value;
    }

    private static string GetAddressType(string host)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host == "127.0.0.1" || host == "::1")
            return "Loopback (локальный хост)";

        if (!IPAddress.TryParse(host, out var ip))
            return "Доменное имя (разрешается через DNS)";

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            if (b[0] == 127)                          return "Loopback (IPv4)";
            if (b[0] == 10)                           return "Частный (класс A, RFC 1918)";
            if (b[0] == 172 && b[1] is >= 16 and <= 31) return "Частный (класс B, RFC 1918)";
            if (b[0] == 192 && b[1] == 168)           return "Частный (класс C, RFC 1918)";
            if (b[0] == 169 && b[1] == 254)           return "Link-Local / APIPA";
            return "Публичный (глобальный)";
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (IPAddress.IsLoopback(ip)) return "IPv6 Loopback (::1)";
            if (ip.IsIPv6LinkLocal)       return "IPv6 Link-Local";
            if (ip.IsIPv6SiteLocal)       return "IPv6 Site-Local";
            return "IPv6 Публичный";
        }

        return "Неизвестный тип";
    }
}
