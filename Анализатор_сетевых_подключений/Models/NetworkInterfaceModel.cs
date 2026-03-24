using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Анализатор_сетевых_подключений.Models;

public class NetworkInterfaceModel
{
    public string Name { get; }
    public string Description { get; }
    public string MacAddress { get; }
    public string Status { get; }
    public string Speed { get; }
    public string InterfaceType { get; }
    public string IpAddressDisplay { get; }
    public string SubnetMaskDisplay { get; }
    public string GatewayDisplay { get; }

    public NetworkInterfaceModel(NetworkInterface ni)
    {
        Name = ni.Name;
        Description = ni.Description;
        MacAddress = FormatMac(ni.GetPhysicalAddress().ToString());
        Status = ni.OperationalStatus.ToString();
        Speed = FormatSpeed(ni.Speed);
        InterfaceType = ni.NetworkInterfaceType.ToString();

        try
        {
            var ipProps = ni.GetIPProperties();

            var ipv4 = ipProps.UnicastAddresses
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                .ToList();
            var ipv6 = ipProps.UnicastAddresses
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6)
                .ToList();

            var allIps = ipv4.Concat(ipv6).Select(a => a.Address.ToString()).ToList();
            IpAddressDisplay = allIps.Count > 0 ? string.Join(", ", allIps) : "—";

            SubnetMaskDisplay = ipv4.Count > 0
                ? string.Join(", ", ipv4.Select(a => a.IPv4Mask?.ToString() ?? "—"))
                : "—";

            GatewayDisplay = ipProps.GatewayAddresses.Count > 0
                ? string.Join(", ", ipProps.GatewayAddresses.Select(g => g.Address.ToString()))
                : "—";
        }
        catch
        {
            IpAddressDisplay = "—";
            SubnetMaskDisplay = "—";
            GatewayDisplay = "—";
        }
    }

    private static string FormatMac(string mac)
    {
        if (string.IsNullOrEmpty(mac) || mac.All(c => c == '0')) return "—";
        if (mac.Length % 2 != 0) return mac;
        return string.Join(":", Enumerable.Range(0, mac.Length / 2)
            .Select(i => mac.Substring(i * 2, 2)));
    }

    private static string FormatSpeed(long speed)
    {
        if (speed <= 0) return "Неизвестно";
        if (speed >= 1_000_000_000) return $"{speed / 1_000_000_000} Гбит/с";
        if (speed >= 1_000_000) return $"{speed / 1_000_000} Мбит/с";
        if (speed >= 1_000) return $"{speed / 1_000} Кбит/с";
        return $"{speed} бит/с";
    }
}
