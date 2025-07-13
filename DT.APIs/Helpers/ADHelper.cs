using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace DT.APIs.Helpers
{
    public class ADHelper : IDisposable
    {
        private readonly IConfiguration _configuration;
        private PrincipalContext _principalContext;

        public ADHelper(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            InitPrincipalContext();
        }

        private void InitPrincipalContext()
        {
            if (_principalContext == null)
            {
                string domain = GetConfigValue("ActiveDirectorySettings:Domain");
                string user = GetConfigValue("ActiveDirectorySettings:User");
                string password = GetConfigValue("ActiveDirectorySettings:Password");

                _principalContext = new PrincipalContext(ContextType.Domain, domain, user, password);
            }


        }

        public void Dispose()
        {
            _principalContext?.Dispose();
        }
        private string GetConfigValue(string key)
        {
            return _configuration[key] ?? throw new ArgumentNullException($"Config key {key} is missing.");
        }

        public List<ADUserModel> FindUsers(string searchKey)
        {
            var adUsers = new List<ADUserModel>();
            searchKey = (searchKey ?? "").Trim();

            if (searchKey.Length >= 3)
            {


                // Search by sAMAccountName (username)
                using (var user = new UserPrincipal(_principalContext))
                {
                    user.SamAccountName = searchKey + "*";
                    AddUsersFromSearch(adUsers, user, _principalContext);
                }

                // Search by DisplayName
                using (var user = new UserPrincipal(_principalContext))
                {
                    user.DisplayName = searchKey + "*";
                    AddUsersFromSearch(adUsers, user, _principalContext);
                }

            }

            return adUsers.DistinctBy(u => u.UserName).Take(50).ToList();
        }

        private void AddUsersFromSearch(List<ADUserModel> adUsers, UserPrincipal user, PrincipalContext context)
        {
            using (var searcher = new PrincipalSearcher(user))
            {
                foreach (var result in searcher.FindAll())
                {
                    if (result.GetUnderlyingObject() is DirectoryEntry de)
                    {
                        var userObj = MapToLocalObject(de);

                        if (!adUsers.Any(a => a.UserName == userObj.UserName))
                        {
                            adUsers.Add(userObj);
                        }
                    }
                }
            }
        }



        private IEnumerable<ADUserModel> SearchUsersByAttribute(PrincipalContext context, string attributeName, string searchKey, string wildcard)
        {
            var user = new UserPrincipal(context);
            user.GetType().GetProperty(attributeName)?.SetValue(user, $"{searchKey}{wildcard ?? "*"}");

            using (var searcher = new PrincipalSearcher(user))
            {
                foreach (var result in searcher.FindAll())
                {
                    if (result.GetUnderlyingObject() is DirectoryEntry de)
                        yield return MapToLocalObject(de);
                }
            }
        }

        private ADUserModel MapToLocalObject(DirectoryEntry de)
        {
            var adUser = new ADUserModel
            {
                FirstName = (string)de.Properties["givenName"].Value,
                MiddleName = (string)de.Properties["initials"].Value,
                LastName = (string)de.Properties["sn"].Value,
                DisplayName = (string)de.Properties["displayName"].Value,
                UserName = (string)de.Properties["sAMAccountName"].Value,
                Email = (string)de.Properties["mail"].Value,
                FullEmail = (string)de.Properties["userPrincipalName"].Value,
                Mobile = (string)de.Properties["mobile"].Value,
                Company = (string)de.Properties["company"].Value,
                Title = (string)de.Properties["title"].Value
            };

            var imgArr = (byte[])de.Properties["thumbnailPhoto"].Value;
            if (imgArr != null)
            {
                adUser.PhotoBase64 = Convert.ToBase64String(imgArr);
                adUser.PhotoBinary = imgArr;
            }

            return adUser;
        }

        public bool Authenticate(string userName, string password)
        {
            try
            {
                using (var entry = new DirectoryEntry($"LDAP://{GetConfigValue("ActiveDirectorySettings:Domain")}", userName, password))
                {
                    _ = entry.NativeObject;
                    return true;
                }
            }
            catch (DirectoryServicesCOMException)
            {
                return false;
            }
        }

        public ADUserDetails AuthenticateUser(string userName, string password)
        {
            if (Authenticate(userName, password))
            {
                using (var entry = new DirectoryEntry($"LDAP://{GetConfigValue("ActiveDirectorySettings:Domain")}", userName, password))
                {
                    return GetADUserDetails(entry, userName);
                }
            }
            return null;
        }

        public ADUserDetails GetADUserDetails(DirectoryEntry entry, string userName)
        {
            using (var searcher = new DirectorySearcher(entry))
            {
                searcher.Filter = $"(&(objectClass=user)(objectCategory=person)(SAMAccountName={userName}))";
                var result = searcher.FindOne();

                return result != null ? ADUserDetails.GetUser(result.Properties) : null;
            }
        }

        public ADUserDetails GetUserDetailsByUsername(string userName)
        {
            string domain = GetConfigValue("ActiveDirectorySettings:Domain");
            string user = GetConfigValue("ActiveDirectorySettings:User");

            // Use _principalContext to authenticate with the service account
            using (var context = new PrincipalContext(ContextType.Domain, domain, user))
            {
                // Authenticate against the domain, and retrieve the user
                var userPrincipal = new UserPrincipal(context) { SamAccountName = userName };
                using (var searcher = new PrincipalSearcher(userPrincipal))
                {
                    var result = searcher.FindOne();
                    if (result != null && result.GetUnderlyingObject() is DirectoryEntry de)
                    {
                        return GetADUserDetails(de, userName);
                    }
                }
            }
            return null; // Return null if no user is found
        }



        public void UpdateUserProperty(string userName, string password, string propertyName, object value)
        {
            using (var entry = new DirectoryEntry($"LDAP://{GetConfigValue("ActiveDirectorySettings:Domain")}", userName, password))
            {
                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = $"(&(objectClass=user)(objectCategory=person)(SAMAccountName={userName}))";
                    var result = searcher.FindOne();
                    if (result == null) throw new Exception("User not found.");

                    using (var user = new DirectoryEntry(result.Path, userName, password))
                    {
                        user.Properties[propertyName].Value = value;
                        user.CommitChanges();
                    }
                }
            }
        }

        public void UpdateMobile(string userName, string password, string mobile) =>
            UpdateUserProperty(userName, password, "mobile", mobile);

        public void UpdateTitle(string userName, string password, string title) =>
            UpdateUserProperty(userName, password, "title", title);

        public byte[] GetUserPicture(string userName)
        {

            var user = new UserPrincipal(_principalContext) { SamAccountName = userName };
            using (var searcher = new PrincipalSearcher(user))
            {
                var result = searcher.FindOne();
                return result?.GetUnderlyingObject() is DirectoryEntry de ? (byte[])de.Properties["thumbnailPhoto"].Value : null;
            }

        }
    }

    public class ADUserDetails
    {
        public string Department { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string LoginName { get; set; }
        public string LoginNameWithDomain { get; set; }
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string HomePhone { get; set; }
        public string Extension { get; set; }
        public string Mobile { get; set; }
        public string Fax { get; set; }
        public string EmailAddress { get; set; }
        public string Title { get; set; }
        public string Company { get; set; }
        public string Manager { get; set; }
        public string ManagerName { get; set; }
        public bool Enabled { get; set; }
        public byte[] Photo { get; set; }

        private ADUserDetails(ResultPropertyCollection propertyCollection)
        {
            //var json=SerializeProperties(propertyCollection);

            string domainAddress;
            string domainName;
            FirstName = (string)GetProperty(propertyCollection, ADProperties.FIRSTNAME);
            MiddleName = (string)GetProperty(propertyCollection, ADProperties.MIDDLENAME);
            LastName = (string)GetProperty(propertyCollection, ADProperties.LASTNAME);
            LoginName = (string)GetProperty(propertyCollection, ADProperties.LOGINNAME);
            StreetAddress = (string)GetProperty(propertyCollection, ADProperties.STREETADDRESS);
            City = (string)GetProperty(propertyCollection, ADProperties.CITY);
            State = (string)GetProperty(propertyCollection, ADProperties.STATE);
            PostalCode = (string)GetProperty(propertyCollection, ADProperties.POSTALCODE);
            Country = (string)GetProperty(propertyCollection, ADProperties.COUNTRY);
            Company = (string)GetProperty(propertyCollection, ADProperties.COMPANY);
            Department = (string)GetProperty(propertyCollection, ADProperties.DEPARTMENT);
            HomePhone = (string)GetProperty(propertyCollection, ADProperties.HOMEPHONE);
            Extension = (string)GetProperty(propertyCollection, ADProperties.EXTENSION);
            Mobile = (string)GetProperty(propertyCollection, ADProperties.MOBILE);
            Fax = (string)GetProperty(propertyCollection, ADProperties.FAX);
            EmailAddress = (string)GetProperty(propertyCollection, ADProperties.EMAILADDRESS);
            Title = (string)GetProperty(propertyCollection, ADProperties.TITLE);
            Manager = (string)GetProperty(propertyCollection, ADProperties.MANAGER);
            Enabled = Convert.ToBoolean(GetProperty(propertyCollection, ADProperties.MSRTCSIPUSERENABLED) ?? false);
            Photo = (byte[])GetProperty(propertyCollection, ADProperties.PHOTO);
            string userPrincipalName = GetProperty(propertyCollection, ADProperties.USERPRINCIPALNAME).ToString();
            if (!string.IsNullOrEmpty(userPrincipalName))
                domainAddress = userPrincipalName.Split('@')[1];
            else
                domainAddress = string.Empty;

            if (!string.IsNullOrEmpty(domainAddress))
                domainName = domainAddress.Split('.').First();
            else
            {
                domainName = string.Empty;
            }
            LoginNameWithDomain = string.Format(@"{0}\{1}", domainName, LoginName);
            if (!string.IsNullOrEmpty(Manager))
            {
                string[] managerArray = Manager.Split(',');
                ManagerName = managerArray[0].Replace("CN=", "");
            }
        }

        private static object GetProperty(ResultPropertyCollection properties, string propertyName)
        {
            if (properties.Contains(propertyName) && properties[propertyName].Count > 0)
            {
                return properties[propertyName][0] ?? null;
            }
            return null;
        }

        // Method for ResultPropertyCollection
        public static ADUserDetails GetUser(ResultPropertyCollection propertyCollection)
        {
            return new ADUserDetails(propertyCollection);
        }

        // Method for SearchResult
        public static ADUserDetails GetUser(SearchResult searchResult)
        {
            return GetUser(searchResult.Properties);
        }

        // Method for SearchResultCollection
        public static List<ADUserDetails> GetUsers(SearchResultCollection searchResults)
        {
            var users = new List<ADUserDetails>();
            foreach (SearchResult result in searchResults)
            {
                users.Add(GetUser(result));
            }
            return users;
        }

        // Method to serialize ResultPropertyCollection to JSON for debugging
        public static string SerializeProperties(ResultPropertyCollection propertyCollection)
        {
            var dict = new Dictionary<string, object>();

            foreach (string propertyName in propertyCollection.PropertyNames)
            {
                dict[propertyName] = propertyCollection[propertyName].Count > 0
                    ? propertyCollection[propertyName].Cast<object>().ToList()
                    : null;
            }

            return JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    public static class ADProperties
    {
        public const String OBJECTCLASS = "objectClass";
        public const String CONTAINERNAME = "cn";
        public const String LASTNAME = "sn";
        public const String COUNTRYNOTATION = "c";
        public const String CITY = "l";
        public const String STATE = "st";
        public const String TITLE = "title";
        public const String POSTALCODE = "postalCode";
        public const String PHYSICALDELIVERYOFFICENAME = "physicalDeliveryOfficeName";
        public const String FIRSTNAME = "givenName";
        public const String MIDDLENAME = "initials";
        public const String DISTINGUISHEDNAME = "distinguishedName";
        public const String INSTANCETYPE = "instanceType";
        public const String WHENCREATED = "whenCreated";
        public const String WHENCHANGED = "whenChanged";
        public const String DISPLAYNAME = "displayName";
        public const String USNCREATED = "uSNCreated";
        public const String MEMBEROF = "memberOf";
        public const String USNCHANGED = "uSNChanged";
        public const String COUNTRY = "co";
        public const String DEPARTMENT = "extensionAttribute5"; //"department";
        public const String COMPANY = "company";
        public const String PROXYADDRESSES = "proxyAddresses";
        public const String STREETADDRESS = "streetAddress";
        public const String DIRECTREPORTS = "directReports";
        public const String NAME = "name";
        public const String OBJECTGUID = "objectGUID";
        public const String USERACCOUNTCONTROL = "userAccountControl";
        public const String BADPWDCOUNT = "badPwdCount";
        public const String CODEPAGE = "codePage";
        public const String COUNTRYCODE = "countryCode";
        public const String BADPASSWORDTIME = "badPasswordTime";
        public const String LASTLOGOFF = "lastLogoff";
        public const String LASTLOGON = "lastLogon";
        public const String PWDLASTSET = "pwdLastSet";
        public const String PRIMARYGROUPID = "primaryGroupID";
        public const String OBJECTSID = "objectSid";
        public const String ADMINCOUNT = "adminCount";
        public const String ACCOUNTEXPIRES = "accountExpires";
        public const String LOGONCOUNT = "logonCount";
        public const String LOGINNAME = "sAMAccountName";
        public const String SAMACCOUNTTYPE = "sAMAccountType";
        public const String SHOWINADDRESSBOOK = "showInAddressBook";
        public const String LEGACYEXCHANGEDN = "legacyExchangeDN";
        public const String USERPRINCIPALNAME = "userPrincipalName";
        public const String EXTENSION = "ipPhone";
        public const String SERVICEPRINCIPALNAME = "servicePrincipalName";
        public const String OBJECTCATEGORY = "objectCategory";
        public const String DSCOREPROPAGATIONDATA = "dSCorePropagationData";
        public const String LASTLOGONTIMESTAMP = "lastLogonTimestamp";
        public const String EMAILADDRESS = "mail";
        public const String MANAGER = "manager";
        public const String MOBILE = "mobile";
        public const String PAGER = "pager";
        public const String FAX = "facsimileTelephoneNumber";
        public const String HOMEPHONE = "homePhone";
        public const String MSEXCHUSERACCOUNTCONTROL = "msExchUserAccountControl";
        public const String MDBUSEDEFAULTS = "mDBUseDefaults";
        public const String MSEXCHMAILBOXSECURITYDESCRIPTOR = "msExchMailboxSecurityDescriptor";
        public const String HOMEMDB = "homeMDB";
        public const String MSEXCHPOLICIESINCLUDED = "msExchPoliciesIncluded";
        public const String HOMEMTA = "homeMTA";
        public const String MSEXCHRECIPIENTTYPEDETAILS = "msExchRecipientTypeDetails";
        public const String MAILNICKNAME = "mailNickname";
        public const String MSEXCHHOMESERVERNAME = "msExchHomeServerName";
        public const String MSEXCHVERSION = "msExchVersion";
        public const String MSEXCHRECIPIENTDISPLAYTYPE = "msExchRecipientDisplayType";
        public const String MSEXCHMAILBOXGUID = "msExchMailboxGuid";
        public const String NTSECURITYDESCRIPTOR = "nTSecurityDescriptor";
        public const String MSRTCSIPUSERENABLED = "msrtcsip-userenabled";
        public const String PHOTO = "thumbnailPhoto";
    }




}
