using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Novell.Directory.Ldap;
using System.Net;

namespace Hugin.Models
{
    public class LdapAuthenticator
    {
        private string host;
        private int port;
        private string basestr;
        private string id;
        private Func<LdapEntry, (string, string, string)> getEmailAndNames;

        public LdapAuthenticator(string host, int port, string basestr, string id, Func<LdapEntry, (string, string, string)> getEmailAndNames)
        {
            this.host = host;
            this.port = port;
            this.basestr = basestr;
            this.id = id;
            this.getEmailAndNames = getEmailAndNames;
        }


        public (bool, string, string, string) Authenticate(string account, string password)
        {
            var host = Environment.GetEnvironmentVariable("LDAP_HOST");
            if(string.IsNullOrWhiteSpace(host))
            {
                return (false, null, null, null);
            }
            var lc = new LdapConnection();
            lc.UserDefinedServerCertValidationDelegate += (sender, certificate, chain, sslPolicyErrors) => true;  // Ignore cert. error
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
                lc.SecureSocketLayer = true;
                lc.Connect(this.host, this.port);
                var dn = string.Format("{0}={1},{2}", this.id, account, this.basestr);
                lc.Bind(LdapConnection.LdapV3, dn, password);
                var (name, ename, email) = getEmailAndNames(lc.Read(dn));
                return (true, name, ename, email);
            }
            catch(Exception)
            {
                return (false, null, null, null);
            }
            finally
            {
                lc.Disconnect();
            }
        }
    }
}
