using System;
using System.Collections.Generic;
using System.Management;
using System.Text;

namespace NetworkSanityManager.MicrosoftDNS
{
     public class DnsProvider
     {
         #region Members
         private ManagementScope Session = null;
         public string Server = null;
         public string User = null;
         private string Password = null;
         private string m_NameSpace = null;
         #endregion

         public DnsProvider(string serverName, string userName, string password)
         {
             this.Server = serverName;
             this.User = userName;
             this.Password = password;
             this.Logon();
         }

         private void Logon()
         {
             this.m_NameSpace = "\\\\" + this.Server + "\\root\\microsoftdns";
             ConnectionOptions con = new ConnectionOptions();
             con.Username = this.User;
             con.Password = this.Password;
             con.Impersonation = ImpersonationLevel.Impersonate;
             this.Session = new ManagementScope(this.NameSpace);
             this.Session.Options = con;
             this.Session.Connect();
         }

         #region Methods
         public void Dispose()
         {
         }

         public void Dispose(ref ManagementClass x)
         {
             if (x != null)
             {
                 x.Dispose();
                 x = null;
             }
         }

         public void Dispose(ref ManagementBaseObject x)
         {
             if (x != null)
             {
                 x.Dispose();
                 x = null;
             }
         }

         public bool DomainExists(string domainName)
         {
             bool retval = false;
             string wql = "";
             wql = "SELECT *";
             wql += " FROM MicrosoftDNS_ATYPE";
             wql += " WHERE OwnerName = '" + domainName + "'";
             ObjectQuery q = new ObjectQuery(wql);
             ManagementObjectSearcher s = new ManagementObjectSearcher(this.Session, q);
             ManagementObjectCollection col = s.Get();
             int total = col.Count;
             foreach (ManagementObject o in col)
             {
                 retval = true;
             }
             return retval;
         }

         public void AddARecord(string domain, string recordName, string ipDestination)
         {
             if (this.DomainExists(recordName + "." + domain))
             {
                 throw new Exception("That record already exists!");
             }
             ManagementClass man = new ManagementClass(this.Session, new ManagementPath("MicrosoftDNS_ATYPE"), null);
             ManagementBaseObject vars = man.GetMethodParameters("CreateInstanceFromPropertyData");
             vars["DnsServerName"] = this.Server;
             vars["ContainerName"] = domain;
             vars["OwnerName"] = recordName + "." + domain;
             vars["IPAddress"] = ipDestination;
             man.InvokeMethod("CreateInstanceFromPropertyData", vars, null);
         }

         public void RemoveARecord(string domain, string aRecord)
         {
             string wql = "";
             wql = "SELECT *";
             wql += " FROM MicrosoftDNS_ATYPE";
             wql += " WHERE OwnerName = '" + aRecord + "." + domain + "'";
             ObjectQuery q = new ObjectQuery(wql);
             ManagementObjectSearcher s = new ManagementObjectSearcher(this.Session, q);
             ManagementObjectCollection col = s.Get();
             int total = col.Count;
             foreach (ManagementObject o in col)
             {
                 o.Delete();
             }
         }
         #endregion

         #region Properties
            public string NameSpace
            {
                get
                {
                    return this.m_NameSpace;
                }
            }

            public bool Enabled
            {
                get
                {
                    bool retval = false;
                    try
                    {
                        SelectQuery wql = new SelectQuery();
                        wql.QueryString = "";
                    }
                    catch
                    { }
                    return retval;
                }
            }

            public ManagementClass Manage(string path)
            {
                ManagementClass retval = new ManagementClass(this.Session, new ManagementPath(path), null);
                return retval;
            }
            #endregion
     }

}
