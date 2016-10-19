using System;
using System.Management;

namespace Gaev.LeaderElection.Tests.Utils
{
    /// <summary>
    /// http://stackoverflow.com/questions/25319080/simulate-a-network-failure
    /// </summary>
    public class NetworkConnection
    {
        public static void On()
        {

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_NetworkAdapterConfiguration");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    String Check = Convert.ToString(queryObj["DHCPLeaseObtained"]);
                    if (String.IsNullOrEmpty(Check))
                    {
                    }
                    else
                    {
                        ManagementBaseObject outParams = queryObj.InvokeMethod("RenewDHCPLease", null, null);
                    }
                }
            }
            catch (ManagementException e)
            {
                Console.WriteLine("An error occurred while querying for WMI data: " + e.Message);
            }
        }

        public static void Off()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_NetworkAdapterConfiguration");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    String Check = Convert.ToString(queryObj["DHCPLeaseObtained"]);
                    if (String.IsNullOrEmpty(Check))
                    {
                    }
                    else
                    {
                        ManagementBaseObject outParams = queryObj.InvokeMethod("ReleaseDHCPLease", null, null);
                    }
                }
            }
            catch (ManagementException e)
            {
                Console.WriteLine("An error occurred while querying for WMI data: " + e.Message);
            }
        }
    }
}
