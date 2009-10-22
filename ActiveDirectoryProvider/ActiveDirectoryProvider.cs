using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Web;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Plugins.ActiveDirectory {

	/// <summary>
	/// Implements a Users Storage Provider for Active Directory.
	/// </summary>
	public class ActiveDirectoryProvider {

        private const string HELP_HELP = "Configuration is set with the following parameters:<br/>domain - The domain name (e.g. DOMAIN or DOMAIN.COM)<br/>server - A domain controller to authenticate against (e.g. MYDC1)<br/>admingroup - The AD group that will have admin access to the Wiki<br/>usergroup The AD group that will have user access to the Wiki<br/>username - An AD account username that has at minimum read access to the domain.<br/>password - The password for the above username.<br/><br/>You will also need to adjust your web.config to set Authentication mode to Windows, and turn on NTLM on the server as well.";
        private IHostV30 m_Host;
        private string m_Server;
        private string m_Domain;
        private string m_SearchRoot;
        private string m_AdminGroup;
        private string m_UserGroup;
        private string m_Username;
        private string m_Password;
        private IUsersStorageProviderV30 m_StorageProvider;

        /// <summary>
        /// Not Implemented
        /// </summary>
        public bool TestAccount(UserInfo user, string password) {
            return false;
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public UserInfo[] GetUsers() {
            return new UserInfo[] { };
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public UserInfo AddUser(string username, string displayName, string password, string email, bool active, DateTime dateTime) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public UserInfo ModifyUser(UserInfo user, string newDisplayName, string newPassword, string newEmail, bool newActive) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public bool RemoveUser(UserInfo user) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public UserGroup[] GetUserGroups() {
            return new UserGroup[] { };
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public UserGroup AddUserGroup(string name, string description) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public UserGroup ModifyUserGroup(UserGroup group, string description) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public bool RemoveUserGroup(UserGroup group) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public UserInfo SetUserMembership(UserInfo user, string[] groups) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tries to log the user in manually
        /// </summary>
        public UserInfo TryManualLogin(string username, string password) {

            var info = m_StorageProvider.TryManualLogin(username, password);

            if (info != null)
                return info;

            try {
                using (var rootEntry = new DirectoryEntry(m_SearchRoot, username, password, AuthenticationTypes.Delegation | AuthenticationTypes.ReadonlyServer | AuthenticationTypes.Secure)) {
                    var nativeObject = rootEntry.NativeObject;
                    return GetUser(username);
                }
            } catch {
                return null;
            }
        }

        /// <summary>
        /// Tries to log the user in automagically
        /// </summary>
        public UserInfo TryAutoLogin(HttpContext context) {

            try {

                if (!context.User.Identity.IsAuthenticated)
                    return null;

                var username = context.User.Identity.Name.Substring(context.User.Identity.Name.IndexOf("\\") + 1);
                return GetUser(username);

            } catch {
                return null;
            }
        }

        /// <summary>
        /// Gets the user info object for the currently logged in user.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public UserInfo GetUser(string username) {
            var user = m_StorageProvider.GetUser(username);
            if (user != null)
                return user;

            using (var rootEntry = new DirectoryEntry(m_SearchRoot, m_Username, m_Password, AuthenticationTypes.Secure)) {
                using (var searcher = new DirectorySearcher(rootEntry) {
                    Filter = string.Format("(&(objectClass=user)(objectCategory=person)(sAMAccountName={0}))", username)
                })
                {
                    searcher.PropertiesToLoad.Add("objectClass");
                    searcher.PropertiesToLoad.Add("member");
                    searcher.PropertiesToLoad.Add("sAMAccountName");
                    searcher.PropertiesToLoad.Add("sn");
                    searcher.PropertiesToLoad.Add("givenName");
                    searcher.PropertiesToLoad.Add("department");
                    searcher.PropertiesToLoad.Add("title");
                    searcher.PropertiesToLoad.Add("mail");
                    searcher.PropertiesToLoad.Add("displayName");

                    DirectoryEntry entry;
                    try {

                        entry = searcher.FindOne().GetDirectoryEntry(); 

                    } catch {

                        return null;

                    }

                    var displayName = string.Empty;
                    var emailAddress = string.Empty;
                    var adminGroup = false;
                    var userGroup = false;

                    var values = entry.Properties["displayName"];
                    if (values != null && values.Count > 0)
                        displayName = values[0].ToString();

                    values = entry.Properties["mail"];
                    if (values != null && values.Count > 0)
                        emailAddress = values[0].ToString();

                    var rootPath = entry.Path;
                    rootPath = rootPath.Substring(0, rootPath.IndexOf("/", 7) + 1);

                    for (var i = 0; i < entry.Properties["memberOf"].Count; i++) {

                        using (var currentEntry = new DirectoryEntry(rootPath + entry.Properties["memberOf"][i])) {

                            if (IsAccountTypeGroup(currentEntry)) {

                                values = currentEntry.Properties["sAMAccountName"];
                                if (values != null && values.Count > 0) {

                                    if (values[0].ToString() == m_AdminGroup)
                                        adminGroup = true;

                                    if (values[0].ToString() == m_UserGroup)
                                        userGroup = true;

                                }

                            }

                        }

                    }


                    var userGroups = new List<string>();

                    if (adminGroup)
                        userGroups.Add("Administrators");
                    if (userGroup)

                        userGroups.Add("Users");

                    user = m_StorageProvider.AddUser(username, displayName, "jdzs98duadj2918j9sdjasd", emailAddress, true, DateTime.Now);
                    user = m_StorageProvider.SetUserMembership(user, userGroups.ToArray());

                    return user;
                }
            }
        }

        /// <summary>
        /// Gets the type of the account.
        /// </summary>
        private static bool IsAccountTypeGroup(DirectoryEntry entry) {

            var objectClass = entry.Properties["objectClass"];

            for (var i = 0; i < objectClass.Count; i++)
                if ((string)objectClass[i] == "group")
                    return true;

            return false;

        }

        /// <summary>
        /// Not Implemented - Passed Directly to the IUsersStorageProviderV30
        /// </summary>
        public UserInfo GetUserByEmail(string email) {

            return m_StorageProvider.GetUserByEmail(email);

        }

        /// <summary>
        /// Not Implemented - Passed Directly to the IUsersStorageProviderV30
        /// </summary>
        public void NotifyCookieLogin(UserInfo user) {

            m_StorageProvider.NotifyCookieLogin(user);

        }

        /// <summary>
        /// Not Implemented - Passed Directly to the IUsersStorageProviderV30
        /// </summary>
        public void NotifyLogout(UserInfo user) {

            m_StorageProvider.NotifyLogout(user);

        }

        /// <summary>
        /// Not Implemented - Passed Directly to the IUsersStorageProviderV30
        /// </summary>
        public bool StoreUserData(UserInfo user, string key, string value) {

            return m_StorageProvider.StoreUserData(user, key, value);

        }

        /// <summary>
        /// Not Implemented - Passed Directly to the IUsersStorageProviderV30
        /// </summary>
        public string RetrieveUserData(UserInfo user, string key) {

            return m_StorageProvider.RetrieveUserData(user, key);

        }

        /// <summary>
        /// Not Implemented - Passed Directly to the IUsersStorageProviderV30
        /// </summary>
        public IDictionary<string, string> RetrieveAllUserData(UserInfo user) {

            return m_StorageProvider.RetrieveAllUserData(user);

        }

        /// <summary>
        /// Not Implemented - Passed Directly to the IUsersStorageProviderV30
        /// </summary>
        public IDictionary<UserInfo, string> GetUsersWithData(string key) {

            return m_StorageProvider.GetUsersWithData(key);

        }

        /// <summary>
        /// True, always, we can't write back to AD
        /// </summary>
        public bool UserAccountsReadOnly {
            get { return true; }
        }

        /// <summary>
        /// No
        /// </summary>
        public bool UserGroupsReadOnly {
            get { return true; }
        }

        /// <summary>
        /// True, always, we can't write back to AD
        /// </summary>
        public bool GroupMembershipReadOnly {
            get { return true; }
        }

        /// <summary>
        /// True, always, we can't write back to AD
        /// </summary>
        public bool UsersDataReadOnly {
            get { return false; }
        }

        /// <summary>
        /// Inits the settings
        /// </summary>
        public void Init(IHostV30 host, string config) {
            m_Host = host;
            var provider = (from a in host.GetUsersStorageProviders(true)
                            where a.Information.Name != this.Information.Name
                            select a).FirstOrDefault();

            if (provider == null)
                throw new InvalidConfigurationException("This provider require an additional active storage provider for storing of active directory user information.");

            m_StorageProvider = provider;
            InitConfig(config);
        }

        /// <summary>
        /// Ignored
        /// </summary>
        public void Shutdown() {
            m_StorageProvider.Shutdown();
        }

        /// <summary>
        /// Plugin Information
        /// TODO return and complete
        /// </summary>
        public ComponentInformation Information {
            get { return new ComponentInformation("ActiveDirectoryProvider", "ScrewTurn Software", "3.0.1.417", "http://www.screwturn.eu", "http://www.screwturn.eu/Version/ActiveDirectoryProvider.txt"); }
        }

        /// <summary>
        /// Plugin Help Text
        /// </summary>
        public string ConfigHelpHtml {
            get { return HELP_HELP; }
        }

        /// <summary>
        /// Configures the plugin based on the configuration settings
        /// </summary>
        /// <param name="config"></param>
        private void InitConfig(string config)
        {
            if (string.IsNullOrEmpty(config))
                throw new InvalidConfigurationException("Config settings must be set before plugin can be used.");

            try {
                var configParts = config.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var configPart in configParts) {
                    var setting = configPart.Substring(0, configPart.IndexOf("="));
                    var value = configPart.Substring(configPart.IndexOf("=") + 1);
                    switch (setting.ToLower().Trim())
                    {
                        case "domain":
                            m_Domain = value;
                            break;
                        case "server":
                            m_Server = value;
                            break;
                        case "admingroup":
                            m_AdminGroup = value;
                            break;
                        case "usergroup":
                            m_UserGroup = value;
                            break;
                        case "username":
                            m_Username = value;
                            break;
                        case "password":
                            m_Password = value;
                            break;
                    }
                }

                var ldap = string.Empty;
                if (!string.IsNullOrEmpty(m_Server))
                    ldap = string.Format("{0}/", m_Server);

                var ldapParts = m_Domain.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                m_SearchRoot = string.Format("LDAP://{0}DC={1}", ldap, string.Join(",DC=", ldapParts));
            } catch (Exception ex) {
                throw new InvalidConfigurationException("The configuration is invalid.", ex);
            }
        }
	}

}
