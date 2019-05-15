namespace NetworkSanityManager.LibreNMS
{
    public struct LibreNMSInputModel
    {
        public string Hostname { get; set; }
        public string Community { get; set; }
        public int SnmpVersion { get; set; }

        public string GetVersionString()
        {
            switch (SnmpVersion)
            {
                case 1:
                    return "v1";

                case 2:
                    return "v2c";

                default:
                    throw new System.Exception("LibreNMS Exporter hit an unsupported snmpVersion on a device.");
            }
        }
    }
}