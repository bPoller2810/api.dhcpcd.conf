using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace api.dhcpcd.conf
{
    public class DhcpcdConfiguration
    {

        #region private member
        private string[] _readLines;
        #endregion

        #region properties
        public string Interface { get; set; }

        public IPAddress StaticV4 { get; set; }
        public byte V4SubNet { get; set; }

        public IPAddress StaticV6 { get; set; }
        public byte V6SubNet { get; set; }

        public IReadOnlyList<IPAddress> Routers { get; set; }
        public IReadOnlyList<IPAddress> Dns { get; set; }
        #endregion

        #region ctor
        public DhcpcdConfiguration()
        {
            Routers = new List<IPAddress>();
            Dns = new List<IPAddress>();
        }
        #endregion

        #region saving methods
        public string[] GetLines()
        {
            var data = new List<string>();

            data.AddRange(GetDefaultSettings());

            if (HasValidStaticSettings())
            {
                data.Add($"interface {Interface}");
                if (StaticV4 is not null)
                {
                    data.Add($"static ip_address={StaticV4}/{V4SubNet}");
                }
                if (StaticV6 is not null)
                {
                    data.Add($"static ip6_address={StaticV6}/{V6SubNet}");
                }
                data.Add($"static routers={string.Join(" ", Routers)}");
                data.Add($"static domain_name_servers={string.Join(" ", Dns)}");
            }

            return data.ToArray();
        }
        #endregion

        #region private helper
        private IEnumerable<string> GetDefaultSettings()
        {
            return new List<string>
            {
                "hostname",
                "clientid",
                "persistent",
                "option rapid_commit",
                "option domain_name_servers, domain_name, domain_search, host_name",
                "option classless_static_routes",
                "option ntp_servers",
                "option interface_mtu",
                "require dhcp_server_identifier",
                "slaac private",
            };
        }
        private bool HasValidStaticSettings()
        {
            return
                !string.IsNullOrEmpty(Interface)
                && (StaticV4 is not null || StaticV6 is not null)
                && (Routers.Count > 0)
                && (Dns.Count > 0);
        }
        #endregion

        #region creator from file
        public static DhcpcdConfiguration FromFile(string filepath)
        {
            if (string.IsNullOrWhiteSpace(filepath)) { throw new ArgumentNullException(nameof(filepath)); }
            if (!File.Exists(filepath)) { throw new FileNotFoundException("not found", filepath); }
            if (!Path.GetExtension(filepath).Equals(".conf")) { throw new InvalidOperationException("Filetype is wrong"); }

            var configuration = new DhcpcdConfiguration();
            configuration.LoadFromFileContent(File.ReadAllLines(filepath));
            return configuration;
        }

        private void LoadFromFileContent(string[] fileLines)
        {
            _readLines = fileLines;
            foreach (var line in fileLines.Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#") && l.Contains(' ')))
            {
                var clean = line.Trim();//remove stupid whitespaces
                var key = clean.Substring(0, clean.IndexOf(' '));
                var value = clean.Substring(clean.IndexOf(' ')).Trim();
                switch (key)
                {
                    case "interface"://the used interface
                        Interface = value;
                        break;
                    case "static"://static ip settings
                        var ipKey = value.Substring(0, value.IndexOf('='));
                        var valueBlock = value.Substring(value.IndexOf('=') + 1).Trim();
                        switch (ipKey)
                        {
                            case "ip_address":
                                var v4Split = valueBlock.Split('/');
                                if (IPAddress.TryParse(v4Split[0], out var ip) && byte.TryParse(v4Split[1], out var v4Subnet))
                                {
                                    StaticV4 = ip;
                                    V4SubNet = v4Subnet;
                                }
                                break;
                            case "ip6_address":
                                var v6Split = valueBlock.Split('/');
                                if (IPAddress.TryParse(v6Split[0], out var ip6) && byte.TryParse(v6Split[1], out var v6Subnet))
                                {
                                    StaticV6 = ip6;
                                    V6SubNet = v6Subnet;
                                }
                                break;
                            case "routers":
                                var routers = valueBlock.Split(' ');
                                foreach (var router in routers)
                                {
                                    if (IPAddress.TryParse(router, out var routIp))
                                    {
                                        ((List<IPAddress>)Routers).Add(routIp);
                                    }
                                }
                                break;
                            case "domain_name_servers":
                                var dnsSplit = valueBlock.Split(' ');
                                foreach (var dns in dnsSplit)
                                {
                                    if (IPAddress.TryParse(dns, out var dnsIp))
                                    {
                                        ((List<IPAddress>)Dns).Add(dnsIp);
                                    }
                                }
                                break;
                        }
                        break;
                }
            }
        }
        #endregion

    }
}
