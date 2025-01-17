using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace ArchSitePlan.Licensing
{
    internal static class MachineCodeHelpers
    {
        // Method to get MAC Address on Windows and macOS
        public static string GetMacAddress()
        {
            // Retrieve the MAC address based on the platform
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetMacAddressWindows();
            
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GetMacAddressMacOS();
            
            else
                throw new PlatformNotSupportedException("Only Windows and macOS are supported.");
        }

        // Method to get MAC address on Windows
        private static string GetMacAddressWindows()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.NetworkInterfaceType != NetworkInterfaceType.Loopback && nic.OperationalStatus == OperationalStatus.Up);

            foreach (var adapter in networkInterfaces)
            {
                var macAddress = adapter.GetPhysicalAddress();
                if (macAddress != null && !string.IsNullOrEmpty(macAddress.ToString()))
                {
                    return macAddress.ToString();
                }
            }

            return "MAC address not found.";
        }

        // Method to get MAC address on macOS
        private static string GetMacAddressMacOS()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.NetworkInterfaceType != NetworkInterfaceType.Loopback && nic.OperationalStatus == OperationalStatus.Up);

            foreach (var adapter in networkInterfaces)
            {
                var macAddress = adapter.GetPhysicalAddress();
                if (macAddress != null && !string.IsNullOrEmpty(macAddress.ToString()))
                {
                    return macAddress.ToString();
                }
            }

            return "MAC address not found.";
        }
    }
}
