
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements the <b>IHost</b> interface.
	/// </summary>
	public class Host : IHostV40 {

		private static Host instance;

		/// <summary>
		/// Gets or sets the singleton instance of the <see cref="Host"/> object.
		/// </summary>
		public static Host Instance {
			get {
				if(instance == null) throw new InvalidOperationException("Host.Instance is null");
				return instance;
			}
			set { instance = value; }
		}

		private Dictionary<string, CustomToolbarItem> _customSpecialTags;
		private Dictionary<string, RequestHandlerRegistryEntry> _requestHandlers;
		private Dictionary<string, HtmlHeadContentItem> _htmlHeadContent;

		/// <summary>
		/// Initializes a new instance of the <see cref="Host"/> class.
		/// </summary>
		public Host() {
			_customSpecialTags = new Dictionary<string, CustomToolbarItem>(5);
			_requestHandlers = new Dictionary<string, RequestHandlerRegistryEntry>(5);
			_htmlHeadContent = new Dictionary<string, HtmlHeadContentItem>(5);
		}

		/// <summary>
		/// Gets the current wiki.
		/// </summary>
		/// <returns>The current wiki.</returns>
		public string GetCurrentWiki() {
			return Tools.DetectCurrentWiki();
		}

		/// <summary>
		/// Gets the special tags added by providers.
		/// </summary>
		/// <param name="wiki">The current wiki.</param>
		/// <returns>The special tags.</returns>
		public Dictionary<string, CustomToolbarItem> GetCustomSpecialTags(string wiki) {
			lock(_customSpecialTags) {
				Dictionary<string, CustomToolbarItem> result = new Dictionary<string, CustomToolbarItem>(_customSpecialTags.Count);

				foreach(var pair in _customSpecialTags) {
					// Storage Providers are now allowed to inject custom buttons in the editor
					if(Settings.GetProvider(wiki).GetPluginStatus(pair.Value.CallerType.FullName)) {
						result.Add(pair.Key, pair.Value);
					}
				}

				return result;
			}
		}

		/// <summary>
		/// Gets the request handlers added by providers.
		/// </summary>
		/// <param name="wiki">The current wiki.</param>
		/// <returns>The request handlers.</returns>
		public Dictionary<string, RequestHandlerRegistryEntry> GetRequestHandlers(string wiki) {
			lock(_requestHandlers) {
				Dictionary<string, RequestHandlerRegistryEntry> result = new Dictionary<string, RequestHandlerRegistryEntry>(_requestHandlers.Count);

				foreach(var pair in _requestHandlers) {
					if(Settings.GetProvider(wiki).GetPluginStatus(pair.Value.CallerType.FullName)) {
						result.Add(pair.Key, pair.Value);
					}
				}

				return result;
			}
		}

		/// <summary>
		/// Gets all the HTML head content.
		/// </summary>
		/// <param name="wiki">The current wiki.</param>
		/// <returns>The content.</returns>
		public string GetAllHtmlHeadContent(string wiki) {
			StringBuilder buffer = new StringBuilder(1024);
			
			lock(_htmlHeadContent) {
				foreach(HtmlHeadContentItem s in _htmlHeadContent.Values) {
					if(Settings.GetProvider(wiki).GetPluginStatus(s.CallerType.FullName)) {
						buffer.AppendLine(s.Content);
					}
				}
			}

			return buffer.ToString();
		}

		/// <summary>
		/// Gets the global setting value.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>The global settings' value.</returns>
		public string GetGlobalSettingValue(GlobalSettingName name) {
			switch(name) {
				case GlobalSettingName.ContactEmail:
					return GlobalSettings.ContactEmail;
				case GlobalSettingName.PublicDirectory:
					return GlobalSettings.PublicDirectory;
				case GlobalSettingName.SenderEmail:
					return GlobalSettings.SenderEmail;
				case GlobalSettingName.UsernameRegex:
					return GlobalSettings.UsernameRegex;
				case GlobalSettingName.PasswordRegex:
					return GlobalSettings.PasswordRegex;
				case GlobalSettingName.EmailRegex:
					return GlobalSettings.EmailRegex;
				case GlobalSettingName.MainUrlRegex:
					return GlobalSettings.MainUrlRegex;
				case GlobalSettingName.DisableAutomaticVersionCheck:
					return GlobalSettings.DisableAutomaticVersionCheck.ToString();
				case GlobalSettingName.EnableHttpCompression:
					return GlobalSettings.EnableHttpCompression.ToString();
				case GlobalSettingName.EnableViewStateCompression:
					return GlobalSettings.EnableViewStateCompression.ToString();
				case GlobalSettingName.MaxFileSize:
					return GlobalSettings.MaxFileSize.ToString();
				case GlobalSettingName.PageExtension:
					return GlobalSettings.PageExtension;
				case GlobalSettingName.WikiVersion:
					return GlobalSettings.WikiVersion;
				case GlobalSettingName.DefaultPagesStorageProvider:
					return GlobalSettings.DefaultPagesProvider;
				case GlobalSettingName.DefaultUsersStorageProvider:
					return GlobalSettings.DefaultUsersProvider;
				case GlobalSettingName.DefaultFilesStorageProvider:
					return GlobalSettings.DefaultFilesProvider;
				case GlobalSettingName.LoggingLevel:
					return GlobalSettings.LoggingLevel.ToString();
				case GlobalSettingName.MaxLogSize:
					return GlobalSettings.MaxLogSize.ToString();
			}
			return "";
		}

		/// <summary>
		/// Gets the values of the Settings in the given wiki.
		/// </summary>
		/// <param name="name">The Setting's Name.</param>
		/// <returns>The Setting's value.</returns>
		public string GetSettingValue(SettingName name) {
			string wiki = GetCurrentWiki();

			switch(name) {
				case SettingName.MainUrl:
					return Settings.GetMainUrl(wiki);
				case SettingName.DateTimeFormat:
					return Settings.GetDateTimeFormat(wiki);
				case SettingName.DefaultLanguage:
					return Settings.GetDefaultLanguage(wiki);
				case SettingName.RootNamespaceDefaultPage:
					return Settings.GetDefaultPage(wiki);
				case SettingName.DefaultTimeZone:
					return Settings.GetDefaultTimezone(wiki).ToString();
				case SettingName.WikiTitle:
					return Settings.GetWikiTitle(wiki);
				case SettingName.UsersCanRegister:
					return Settings.UsersCanRegister(wiki).ToString();
				case SettingName.EnableDoubleClickEditing:
					return Settings.GetEnableDoubleClickEditing(wiki).ToString();
				case SettingName.ProcessSingleLineBreaks:
					return Settings.GetProcessSingleLineBreaks(wiki).ToString();
				case SettingName.AccountActivationMode:
					return Settings.GetAccountActivationMode(wiki).ToString();
				case SettingName.AllowedFileTypes:
					StringBuilder sb = new StringBuilder(50);
					foreach(string s in Settings.GetAllowedFileTypes(wiki)) sb.Append(s + ",");
					return sb.ToString().TrimEnd(',');
				case SettingName.DisableBreadcrumbsTrail:
					return Settings.GetDisableBreadcrumbsTrail(wiki).ToString();
				case SettingName.DisableCaptchaControl:
					return Settings.GetDisableCaptchaControl(wiki).ToString();
				case SettingName.DisableConcurrentEditing:
					return Settings.GetDisableConcurrentEditing(wiki).ToString();
				case SettingName.ScriptTagsAllowed:
					return Settings.GetScriptTagsAllowed(wiki).ToString();
				case SettingName.MaxRecentChanges:
					return Settings.GetMaxRecentChanges(wiki).ToString();
				case SettingName.EditingSessionTimeout:
					return Collisions.EditingSessionTimeout.ToString();
				case SettingName.AdministratorsGroup:
					return Settings.GetAdministratorsGroup(wiki);
				case SettingName.UsersGroup:
					return Settings.GetUsersGroup(wiki);
				case SettingName.AnonymousGroup:
					return Settings.GetAnonymousGroup(wiki);
				case SettingName.ChangeModerationMode:
					return Settings.GetModerationMode(wiki).ToString();
				case SettingName.RootNamespaceTheme:
					return Settings.GetTheme(wiki, null);
				case SettingName.ListSize:
					return Settings.GetListSize(wiki).ToString();
			}
			return "";
		}

		/// <summary>
		/// Gets the list of the Users.
		/// </summary>
		/// <returns>The users.</returns>
		public UserInfo[] GetUsers() {
			string wiki = GetCurrentWiki();

			return Users.GetUsers(wiki).ToArray();
		}

		/// <summary>
		/// Finds a user by username, properly handling Users Storage Providers.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <returns>The <see cref="T:UserInfo"/>, or <c>null</c> if no users are found.</returns>
		/// <exception cref="ArgumentNullException">If <b>username</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>username</b> is empty.</exception>
		public UserInfo FindUser(string username) {
			if(username == null) throw new ArgumentNullException("username");
			if(username.Length == 0) throw new ArgumentException("Username cannot be empty", "username");

			string wiki = GetCurrentWiki();

			return Users.FindUser(wiki, username);
		}

		/// <summary>
		/// Gets the authenticated user in the current session, if any.
		/// </summary>
		/// <returns>The authenticated user, or <c>null</c> if no user is authenticated.</returns>
		/// <remarks>If the built-it <i>admin</i> user is authenticated, the returned user 
		/// has <i>admin</i> as Username.</remarks>
		public UserInfo GetCurrentUser() {
			string wiki = GetCurrentWiki();

			return SessionFacade.GetCurrentUser(wiki);
		}

		/// <summary>
		/// Gets the list of the user groups.
		/// </summary>
		/// <returns>The groups.</returns>
		public UserGroup[] GetUserGroups() {
			string wiki = GetCurrentWiki();

			return Users.GetUserGroups(wiki).ToArray();
		}

		/// <summary>
		/// Finds a user group by name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>The <see cref="T:UserGroup "/>, or <c>null</c> if no groups are found.</returns>
		/// <exception cref="ArgumentNullException">If <b>name</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>name</b> is empty.</exception>
		public UserGroup FindUserGroup(string name) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			string wiki = GetCurrentWiki();

			return Users.FindUserGroup(wiki, name);
		}

		/// <summary>
		/// Checks whether an action is allowed for a global resource in the given wiki.
		/// </summary>
		/// <param name="action">The action (see <see cref="Actions.ForGlobals"/> class)</param>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>action</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>action</b> is empty.</exception>
		public bool CheckActionForGlobals(string action, UserInfo user) {
			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");

			string wiki = GetCurrentWiki();

			var temp = user != null ? user : Users.GetAnonymousAccount(wiki);
			
			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(wiki));
			return authChecker.CheckActionForGlobals(action, temp.Username, temp.Groups);
		}

		/// <summary>
		/// Checks whether an action is allowed for a namespace in the given wiki.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <param name="action">The action (see <see cref="Actions.ForNamespaces"/> class)</param>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>action</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>action</b> is empty.</exception>
		public bool CheckActionForNamespace(NamespaceInfo nspace, string action, UserInfo user) {
			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");

			string wiki = GetCurrentWiki();

			var temp = user != null ? user : Users.GetAnonymousAccount(wiki);
			
			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(wiki));
			return authChecker.CheckActionForNamespace(nspace, action, temp.Username, temp.Groups);
		}

		/// <summary>
		/// Checks whether an action is allowed for a page in the given page.
		/// </summary>
		/// <param name="pageFullName">The page full name.</param>
		/// <param name="action">The action (see <see cref="Actions.ForPages"/> class)</param>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b>, <b>action</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>action</b> is empty.</exception>
		public bool CheckActionForPage(string pageFullName, string action, UserInfo user) {
			if(pageFullName == null) throw new ArgumentNullException("page");
			if(pageFullName.Length == 0) throw new ArgumentException("Page cannot be empty", "page");
			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");

			string wiki = GetCurrentWiki();

			var temp = user != null ? user : Users.GetAnonymousAccount(wiki);
			
			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(wiki));
			return authChecker.CheckActionForPage(pageFullName, action, temp.Username, temp.Groups);
		}

		/// <summary>
		/// Checks whether an action is allowed for a directory in the given wiki.
		/// </summary>
		/// <param name="directory">The directory.</param>
		/// <param name="action">The action (see <see cref="Actions.ForDirectories"/>).</param>
		/// <param name="user">The user.</param>
		/// <returns><c>true</c> if the action is allowed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>directory</b>, <b>action</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>action</b> is empty.</exception>
		public bool CheckActionForDirectory(StDirectoryInfo directory, string action, UserInfo user) {
			if(directory == null) throw new ArgumentNullException("directory");
			if(action == null) throw new ArgumentNullException("action");
			if(action.Length == 0) throw new ArgumentException("Action cannot be empty", "action");

			string wiki = GetCurrentWiki();

			var temp = user != null ? user : Users.GetAnonymousAccount(wiki);
			
			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(wiki));
			return authChecker.CheckActionForDirectory(directory.Provider, directory.FullPath, action, temp.Username, temp.Groups);
		}

		/// <summary>
		/// Gets the theme in use for a namespace in a wiki.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The theme.</returns>
		public string GetTheme(NamespaceInfo nspace) {
			string wiki = GetCurrentWiki();

			return Settings.GetTheme(wiki, nspace != null ? nspace.Name : null);
		}

		/// <summary>
		/// Gets the list of the namespaces.
		/// </summary>
		/// <returns>The namespaces.</returns>
		public NamespaceInfo[] GetNamespaces() {
			string wiki = GetCurrentWiki();

			return Pages.GetNamespaces(wiki).ToArray();
		}

		/// <summary>
		/// Finds a namespace by name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>The <see cref="T:NamespaceInfo"/>, or <c>null</c> if no namespaces are found.</returns>
		public NamespaceInfo FindNamespace(string name) {
			string wiki = GetCurrentWiki();

			return Pages.FindNamespace(wiki, name);
		}

		/// <summary>
		/// Gets the list of the Wiki Pages in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The pages.</returns>
		public PageContent[] GetPages(NamespaceInfo nspace) {
			string wiki = GetCurrentWiki();

			return Pages.GetPages(wiki, nspace).ToArray();
		}

		/// <summary>
		/// Gets the List of Categories in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The categories.</returns>
		public CategoryInfo[] GetCategories(NamespaceInfo nspace) {
			string wiki = GetCurrentWiki();

			return Pages.GetCategories(wiki, nspace).ToArray();
		}

		/// <summary>
		/// Gets the list of Snippets.
		/// </summary>
		/// <returns>The snippets.</returns>
		public Snippet[] GetSnippets() {
			string wiki = GetCurrentWiki();

			return Snippets.GetSnippets(wiki).ToArray();
		}

		/// <summary>
		/// Gets the list of Navigation Paths in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The navigation paths.</returns>
		public NavigationPath[] GetNavigationPaths(NamespaceInfo nspace) {
			string wiki = GetCurrentWiki();

			return NavigationPaths.GetNavigationPaths(wiki, nspace).ToArray();
		}

		/// <summary>
		/// Gets the Categories of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The Categories.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> is <c>null</c>.</exception>
		public CategoryInfo[] GetCategoriesPerPage(PageContent page) {
			if(page == null) throw new ArgumentNullException("page");

			return Pages.GetCategoriesForPage(page);
		}

		/// <summary>
		/// Gets a Wiki Page.
		/// </summary>
		/// <param name="fullName">The full Name of the Page.</param>
		/// <returns>The Wiki Page or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <b>fullName</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>fullName</b> is empty.</exception>
		public PageContent FindPage(string fullName) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty");

			string wiki = GetCurrentWiki();

			return Pages.FindPage(wiki, fullName);
		}

		/// <summary>
		/// Gets the Backup/Revision numbers of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The Backup/Revision numbers.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> is <c>null</c>.</exception>
		public int[] GetBackups(PageContent page) {
			if(page == null) throw new ArgumentNullException("page");

			return Pages.GetBackups(page).ToArray();
		}

		/// <summary>
		/// Gets the Content of a Page Backup.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="revision">The revision.</param>
		/// <returns>The Backup Content.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <b>revision</b> is less than zero.</exception>
		public PageContent GetBackupContent(PageContent page, int revision) {
			if(page == null) throw new ArgumentNullException("page");
			if(revision < 0) throw new ArgumentOutOfRangeException("revision", "Revision must be greater than or equal to zero");

			return Pages.GetBackupContent(page, revision);
		}

		/// <summary>
		/// Gets the formatted content of a Wiki Page.
		/// </summary>
		/// <param name="pageFullName">The page full name.</param>
		/// <returns>The formatted content.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> is <c>null</c>.</exception>
		public string GetFormattedContent(string pageFullName) {
			if(pageFullName == null) throw new ArgumentNullException("page");

			string wiki = GetCurrentWiki();

			PageContent pageContent = Pages.FindPage(wiki, pageFullName);
			if(pageContent == null) return null;
			return Formatter.Format(wiki, pageContent.Content, false, FormattingContext.PageContent, pageFullName);
		}

		/// <summary>
		/// Formats a block of WikiMarkup, using the built-in formatter only.
		/// </summary>
		/// <param name="raw">The block of WikiMarkup.</param>
		/// <returns>The formatted content.</returns>
		/// <exception cref="ArgumentNullException">If <b>raw</b> is <c>null</c>.</exception>
		public string Format(string raw) {
			if(raw == null) throw new ArgumentNullException("raw");

			string wiki = GetCurrentWiki();

			return Formatter.Format(wiki, raw, false, FormattingContext.Unknown, null);
		}

		/// <summary>
		/// Lists directories in a directory.
		/// </summary>
		/// <param name="directory">The directory (<c>null</c> for the root, first invocation).</param>
		/// <returns>The directories.</returns>
		public StDirectoryInfo[] ListDirectories(StDirectoryInfo directory) {
			string wiki = GetCurrentWiki();

			List<StDirectoryInfo> result = new List<StDirectoryInfo>(20);

			if(directory == null) {
				foreach(IFilesStorageProviderV40 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
					string[] dirs = prov.ListDirectories(null);

					foreach(string dir in dirs) {
						result.Add(new StDirectoryInfo(dir, prov));
					}
				}
			}
			else {
				string[] dirs = directory.Provider.ListDirectories(directory.FullPath);

				foreach(string dir in dirs) {
					result.Add(new StDirectoryInfo(dir, directory.Provider));
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Lists files in a directory.
		/// </summary>
		/// <param name="directory">The directory (<c>null</c> for the root, first invocation).</param>
		/// <returns>The files.</returns>
		public StFileInfo[] ListFiles(StDirectoryInfo directory) {
			string wiki = GetCurrentWiki();

			List<StFileInfo> result = new List<StFileInfo>(20);

			if(directory == null) {
				foreach(IFilesStorageProviderV40 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
					string[] files = prov.ListFiles(null);

					foreach(string file in files) {
						FileDetails details = prov.GetFileDetails(file);
						result.Add(new StFileInfo(details.Size, details.LastModified, file, prov));
					}
				}
			}
			else {
				string[] files = directory.Provider.ListFiles(directory.FullPath);

				foreach(string file in files) {
					FileDetails details = directory.Provider.GetFileDetails(file);
					result.Add(new StFileInfo(details.Size, details.LastModified, file, directory.Provider));
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Lists page attachments.
		/// </summary>
		/// <param name="pageFullName">The page full name.</param>
		/// <returns>The attachments.</returns>
		/// <exception cref="ArgumentNullException">If <b>page</b> is <c>null</c>.</exception>
		public StFileInfo[] ListPageAttachments(string pageFullName) {
			if(pageFullName == null) throw new ArgumentNullException("page");
			if(pageFullName.Length == 0) throw new ArgumentException("page");

			string wiki = GetCurrentWiki();

			List<StFileInfo> result = new List<StFileInfo>(10);
			foreach(IFilesStorageProviderV40 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
				string[] attachments = prov.ListPageAttachments(pageFullName);

				foreach(string attn in attachments) {
					FileDetails details = prov.GetPageAttachmentDetails(pageFullName, attn);
					result.Add(new StFileInfo(details.Size, details.LastModified, attn, prov));
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Sends an Email.
		/// </summary>
		/// <param name="recipient">The Recipient Email address.</param>
		/// <param name="sender">The Sender's Email address.</param>
		/// <param name="subject">The Subject.</param>
		/// <param name="body">The Body.</param>
		/// <param name="html">True if the message is HTML.</param>
		/// <returns>True if the message has been sent successfully.</returns>
		/// <exception cref="ArgumentNullException">If <b>recipient</b>, <b>sender</b>, <b>subject</b> or <b>body</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>recipient</b>, <b>sender</b>, <b>subject</b> or <b>body</b> are empty.</exception>
		public bool SendEmail(string recipient, string sender, string subject, string body, bool html) {
			if(recipient == null) throw new ArgumentNullException("recipient");
			if(recipient.Length == 0) throw new ArgumentException("Recipient cannot be empty");
			if(sender == null) throw new ArgumentNullException("sender");
			if(sender.Length == 0) throw new ArgumentException("Sender cannot be empty");
			if(subject == null) throw new ArgumentNullException("subject");
			if(subject.Length == 0) throw new ArgumentException("Subject cannot be empty", "subject");
			if(body == null) throw new ArgumentNullException("body");
			if(body.Length == 0) throw new ArgumentException("Body cannot be empty", "body");

			try {
				EmailTools.AsyncSendEmail(recipient, sender, subject, body, html);
				return true;
			}
			catch {
				return false;
			}
		}

		/// <summary>
		/// Logs a new message.
		/// </summary>
		/// <param name="message">The Message.</param>
		/// <param name="entryType">The Entry Type.</param>
		/// <param name="user">The user, or <c>null</c>. If <c>null</c>, the system will log "PluginName+System".</param>
		/// <param name="caller">The Component that calls the method. The caller cannot be null.</param>
		/// <param name="wiki">The wiki or <c>null</c> for application-wide events.</param>
		/// <exception cref="ArgumentNullException">If <b>message</b> or <b>caller</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>message is empty.</b></exception>
		public void LogEntry(string message, LogEntryType entryType, string user, object caller, string wiki = null) {
			if(message == null) throw new ArgumentNullException("message");
			if(message.Length == 0) throw new ArgumentException("Message cannot be empty");
			if(caller == null) throw new ArgumentNullException("caller");

			// Prevent spoofing
			string currentWiki = GetCurrentWiki();
			if(wiki != null && wiki != currentWiki) wiki = currentWiki;

			EntryType t = EntryType.General;
			switch(entryType) {
				case LogEntryType.General:
					t = EntryType.General;
					break;
				case LogEntryType.Warning:
					t = EntryType.Warning;
					break;
				case LogEntryType.Error:
					t = EntryType.Error;
					break;
			}
			string name = "?";
			if(user == null) {
				if(caller is IUsersStorageProviderV40) name = ((IUsersStorageProviderV40)caller).Information.Name;
				else if(caller is IPagesStorageProviderV40) name = ((IPagesStorageProviderV40)caller).Information.Name;
				else if(caller is IFormatterProviderV40) name = ((IFormatterProviderV40)caller).Information.Name;
				else if(caller is ISettingsStorageProviderV40) name = ((ISettingsStorageProviderV40)caller).Information.Name;
				else if(caller is IGlobalSettingsStorageProviderV40) name = ((IGlobalSettingsStorageProviderV40)caller).Information.Name;
				else if(caller is IFilesStorageProviderV40) name = ((IFilesStorageProviderV40)caller).Information.Name;
				name += "+" + Log.SystemUsername;
			}
			else name = user;
			Log.LogEntry(message, t, name, wiki);
		}

		/// <summary>
		/// Changes the language of the current user.
		/// </summary>
		/// <param name="language">The language code.</param>
		public void ChangeCurrentUserLanguage(string language) {
			string wiki = GetCurrentWiki();

			string timezoneId = Preferences.LoadTimezoneFromCookie() ?? Settings.GetDefaultTimezone(wiki);
			if(SessionFacade.LoginKey == null || SessionFacade.CurrentUsername == "admin") Preferences.SavePreferencesInCookie(language, timezoneId);
			else Preferences.SavePreferencesInUserData(wiki, language, timezoneId);
		}

		/// <summary>
		/// Aligns a Date and Time object to the User's Time Zone preferences.
		/// </summary>
		/// <param name="dt">The Date/Time to align.</param>
		/// <returns>The aligned Date/Time.</returns>
		/// <remarks>The method takes care of daylight saving settings.</remarks>
		public DateTime AlignDateTimeWithPreferences(DateTime dt) {
			string wiki = GetCurrentWiki();

			return Preferences.AlignWithTimezone(wiki, dt);
		}

		/// <summary>
		/// Adds an item in the Editing Toolbar.
		/// </summary>
		/// <param name="caller">The caller.</param>
		/// <param name="item">The item to add.</param>
		/// <param name="text">The text of the item showed in the toolbar.</param>
		/// <param name="value">The value of the item, placed in the content: if <b>item</b> is <b>ToolbarItem.SpecialTagWrap</b>, separate start and end tag with a pipe.</param>
		/// <returns>A token to use to remove the toolbar item with <see cref="RemoveToolbarItem"/>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="text"/> or <paramref name="value"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="text"/> or <paramref name="value"/> are empty, or if they contain single or double quotes,
		/// or if <paramref name="value"/> does not contain a pipe when <paramref name="item"/> is <b>SpecialTagWrap</b>.</exception>
		public string AddToolbarItem(IProviderV40 caller, ToolbarItem item, string text, string value) {
			if(caller == null) throw new ArgumentNullException("caller");
			if(text == null) throw new ArgumentNullException("text");
			if(text.Length == 0) throw new ArgumentException("Text cannot be empty", "text");
			if(text.Contains("\"") || text.Contains("'")) throw new ArgumentException("Text cannot contain single or double quotes", "text");
			if(value == null) throw new ArgumentNullException("value");
			if(value.Length == 0) throw new ArgumentException("Value cannot be empty", "value");
			if(value.Contains("\"") || value.Contains("'")) throw new ArgumentException("Value cannot contain single or double quotes", "value");

			if(item == ToolbarItem.SpecialTagWrap && !value.Contains("|")) throw new ArgumentException("Invalid value for a SpecialTagWrap (pipe not found)", "value");

			string token = Guid.NewGuid().ToString("N");

			lock(_customSpecialTags) {
				_customSpecialTags.Add(text, new CustomToolbarItem() {
					CallerType = caller.GetType(),
					Item = item,
					Text = text,
					Token = token,
					Value = value
				});
			}

			return token;
		}

		/// <summary>
		/// Removes a toolbar item.
		/// </summary>
		/// <param name="token">The token returned by <see cref="AddToolbarItem"/>.</param>
		public void RemoveToolbarItem(string token) {
			if(token == null) throw new ArgumentNullException("token");
			if(token.Length == 0) throw new ArgumentException("Token cannot be empty", "token");

			lock(_customSpecialTags) {
				string toRemove = null;
				foreach(var pair in _customSpecialTags) {
					if(pair.Value.Token == token) {
						toRemove = pair.Key;
						break;
					}
				}

				if(toRemove != null) _customSpecialTags.Remove(toRemove);
			}
		}

		/// <summary>
		/// Gets the default provider of the specified type.
		/// </summary>
		/// <param name="providerType">The type of the provider (
		/// <see cref="T:IPagesStorageProviderV40" />, 
		/// <see cref="T:IUsersStorageProviderV40" />, 
		/// <see cref="T:IFilesStorageProviderV40" />,
		/// <see cref="T:IThemesStorageProviderV40" />.</param>
		/// <returns>The Full type name of the default provider of the specified type or <c>null</c>.</returns>
		public string GetDefaultProvider(Type providerType) {
			switch(providerType.FullName) {
				case ProviderLoader.UsersProviderInterfaceName:
					return GlobalSettings.DefaultUsersProvider;
				case ProviderLoader.PagesProviderInterfaceName:
					return GlobalSettings.DefaultPagesProvider;
				case ProviderLoader.FilesProviderInterfaceName:
					return GlobalSettings.DefaultFilesProvider;
				case ProviderLoader.ThemesProviderInterfaceName:
					return GlobalSettings.DefaultThemesProvider;
				default:
					return null;
			}
		}

		/// <summary>
		/// Gets the pages storage providers.
		/// </summary>
		/// <returns>The providers.</returns>
		public IPagesStorageProviderV40[] GetPagesStorageProviders() {
			string wiki = GetCurrentWiki();

			List<IPagesStorageProviderV40> pagesStorageProviders = new List<IPagesStorageProviderV40>();
			foreach(IPagesStorageProviderV40 pagesStorageProvider in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
				pagesStorageProviders.Add(pagesStorageProvider);
			}
			return pagesStorageProviders.ToArray();
		}

		/// <summary>
		/// Gets the users storage providers.
		/// </summary>
		/// <returns>The providers.</returns>
		public IUsersStorageProviderV40[] GetUsersStorageProviders() {
			string wiki = GetCurrentWiki();

			List<IUsersStorageProviderV40> usersStorageProviders = new List<IUsersStorageProviderV40>();
			foreach(IUsersStorageProviderV40 userStorageProvider in Collectors.CollectorsBox.UsersProviderCollector.GetAllProviders(wiki)) {
				usersStorageProviders.Add(userStorageProvider);
			}
			return usersStorageProviders.ToArray();
		}

		/// <summary>
		/// Gets the files storage providers.
		/// </summary>
		/// <returns>The providers.</returns>
		public IFilesStorageProviderV40[] GetFilesStorageProviders() {
			string wiki = GetCurrentWiki();

			List<IFilesStorageProviderV40> filesStorageProviders = new List<IFilesStorageProviderV40>();
			foreach(IFilesStorageProviderV40 filesStorageProvider in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
				filesStorageProviders.Add(filesStorageProvider);
			}
			return filesStorageProviders.ToArray();
		}

		/// <summary>
		/// Gets the theme providers.
		/// </summary>
		/// <returns>The providers.</returns>
		public IThemesStorageProviderV40[] GetThemesProviders() {
			string wiki = GetCurrentWiki();

			List<IThemesStorageProviderV40> themesStorageProviders = new List<IThemesStorageProviderV40>();
			foreach(IThemesStorageProviderV40 themesStorageProvider in Collectors.CollectorsBox.ThemesProviderCollector.GetAllProviders(wiki)) {
				themesStorageProviders.Add(themesStorageProvider);
			}
			return themesStorageProviders.ToArray();
		}

		/// <summary>
		/// Gets the formatter providers, either enabled or disabled.
		/// </summary>
		/// <param name="enabled"><c>true</c> to get enabled providers, <c>false</c> to get disabled providers.</param>
		/// <returns>The providers.</returns>
		public IFormatterProviderV40[] GetFormatterProviders(bool enabled) {
			string wiki = GetCurrentWiki();

			List<IFormatterProviderV40> formatterProviders = new List<IFormatterProviderV40>();
			foreach(IFormatterProviderV40 formatterProvider in Collectors.CollectorsBox.FormatterProviderCollector.GetAllProviders(wiki)) {
				if(enabled == Settings.GetProvider(wiki).GetPluginStatus(formatterProvider.GetType().FullName)) {
					formatterProviders.Add(formatterProvider);
				}
			}
			return formatterProviders.ToArray();
		}

		/// <summary>
		/// Gets the current settings storage provider.
		/// </summary>
		/// <returns>The global settings storage provider.</returns>
		public ISettingsStorageProviderV40 GetSettingsStorageProvider() {
			string wiki = GetCurrentWiki();

			return Collectors.CollectorsBox.GetSettingsProvider(wiki);
		}

		/// <summary>
		/// Gets the current global settings storage provider.
		/// </summary>
		/// <returns>The global settings storage provider.</returns>
		public IGlobalSettingsStorageProviderV40 GetGlobalSettingsStorageProvider() {
			return Collectors.CollectorsBox.GlobalSettingsProvider;
		}

		/// <summary>
		/// Gets the configuration of a storage provider.
		/// </summary>
		/// <param name="providerTypeName">The type name of the provider, such as 'Vendor.Namespace.Provider'.</param>
		/// <returns>The configuration (can be empty or <c>null</c>).</returns>
		/// <exception cref="ArgumentNullException">If <b>providerTypeName</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>providerTypeName</b> is empty.</exception>
		public string GetProviderConfiguration(string providerTypeName) {
			if(providerTypeName == null) throw new ArgumentNullException("providerTypeName");
			if(providerTypeName.Length == 0) throw new ArgumentException("Provider Type Name cannot be empty", "providerTypeName");

			return ProviderLoader.LoadStorageProviderConfiguration(providerTypeName);
		}

		/// <summary>
		/// Gets the configuration of a plugin (formatter provider).
		/// </summary>
		/// <param name="providerTypeName">The type name of the provider, such as 'Vendor.Namespace.Provider'.</param>
		/// <returns>The configuration (can be empty or <c>null</c>).</returns>
		/// <exception cref="ArgumentNullException">If <b>providerTypeName</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>providerTypeName</b> is empty.</exception>
		public string GetPluginConfiguration(string providerTypeName) {
			if(providerTypeName == null) throw new ArgumentNullException("providerTypeName");
			if(providerTypeName.Length == 0) throw new ArgumentException("Provider Type Name cannot be empty", "providerTypeName");

			string wiki = GetCurrentWiki();

			return ProviderLoader.LoadPluginConfiguration(wiki, providerTypeName);
		}

		/// <summary>
		/// Sets the configuration of a provider.
		/// </summary>
		/// <param name="provider">The provider of which to set the configuration.</param>
		/// <param name="configuration">The configuration to set.</param>
		/// <returns><c>true</c> if the configuration is set, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>provider</b> is <c>null</c>.</exception>
		public bool SetPluginConfiguration(IProviderV40 provider, string configuration) {
			if(provider == null) throw new ArgumentNullException("provider");
			if(configuration == null) configuration = "";

			string wiki = GetCurrentWiki();

			ProviderLoader.SavePluginConfiguration(wiki, provider.GetType().FullName, configuration);

			return true;
		}

		/// <summary>
		/// Overrides the public directory.
		/// </summary>
		/// <param name="fullPath">The new full path of the public directory.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="fullPath"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullPath"/> is empty.</exception>
		/// <exception cref="InvalidOperationException">If it's too late to override the public directory.</exception>
		public void OverridePublicDirectory(string fullPath) {
			if(fullPath == null) throw new ArgumentNullException("fullPath");
			if(fullPath == "") throw new ArgumentException("Full Path cannot be empty", "fullPath");

			GlobalSettings.OverridePublicDirectory(fullPath);
		}

		/// <summary>
		/// Event fired whenever an activity is performed on a User Account.
		/// </summary>
		public event EventHandler<UserAccountActivityEventArgs> UserAccountActivity;

		/// <summary>
		/// Fires the UserAccountActivity event.
		/// </summary>
		/// <param name="user">The user the activity refers to.</param>
		/// <param name="activity">The activity.</param>
		public void OnUserAccountActivity(UserInfo user, UserAccountActivity activity) {
			if(UserAccountActivity != null) {
				UserAccountActivity(this, new UserAccountActivityEventArgs(user, activity));
			}
		}

		/// <summary>
		/// Event fired whenever an activity is performed on a user group.
		/// </summary>
		public event EventHandler<UserGroupActivityEventArgs> UserGroupActivity;

		/// <summary>
		/// Fires the UserGroupActivity event.
		/// </summary>
		/// <param name="group">The group the activity refers to.</param>
		/// <param name="activity">The activity.</param>
		public void OnUserGroupActivity(UserGroup group, UserGroupActivity activity) {
			if(UserGroupActivity != null) {
				UserGroupActivity(this, new UserGroupActivityEventArgs(group, activity));
			}
		}

		/// <summary>
		/// Event fired whenever an activity is performed on a namespace.
		/// </summary>
		public event EventHandler<NamespaceActivityEventArgs> NamespaceActivity;

		/// <summary>
		/// Fires the NamespaceActivity event.
		/// </summary>
		/// <param name="nspace">The namespace the activity refers to.</param>
		/// <param name="nspaceOldName">The old name of the renamed namespace, or <c>null</c>.</param>
		/// <param name="activity">The activity.</param>
		public void OnNamespaceActivity(NamespaceInfo nspace, string nspaceOldName, NamespaceActivity activity) {
			if(NamespaceActivity != null) {
				NamespaceActivity(this, new NamespaceActivityEventArgs(nspace, nspaceOldName, activity));
			}
		}

		/// <summary>
		/// Even fired whenever an activity is performed on a Page.
		/// </summary>
		public event EventHandler<PageActivityEventArgs> PageActivity;

		/// <summary>
		/// Fires the PageActivity event.
		/// </summary>
		/// <param name="pageFullName">The full name of the page the activity refers to.</param>
		/// <param name="pageOldName">The old name of the renamed page, or <c>null</c>.</param>
		/// <param name="author">The author of the activity.</param>
		/// <param name="activity">The activity.</param>
		public void OnPageActivity(string pageFullName, string pageOldName, string author, PageActivity activity) {
			if(PageActivity != null) {
				PageActivity(this, new PageActivityEventArgs(pageFullName, pageOldName, author, activity));
			}
		}

		/// <summary>
		/// Event fired whenever an activity is performed on a file, directory or attachment.
		/// </summary>
		public event EventHandler<FileActivityEventArgs> FileActivity;

		/// <summary>
		/// Fires the FileActivity event.
		/// </summary>
		/// <param name="provider">The provider that handles the file.</param>
		/// <param name="file">The name of the file that changed.</param>
		/// <param name="oldFileName">The old name of the renamed file, if any.</param>
		/// <param name="activity">The activity.</param>
		public void OnFileActivity(string provider, string file, string oldFileName, FileActivity activity) {
			string wiki = GetCurrentWiki();

			if(FileActivity != null) {
				IFilesStorageProviderV40 prov = Collectors.CollectorsBox.FilesProviderCollector.GetProvider(provider, wiki);

				FileActivity(this, new FileActivityEventArgs(
					new StFileInfo(prov.GetFileDetails(file), file, prov), oldFileName, null, null, null, activity));
			}
		}

		/// <summary>
		/// Fires the FileActivity event.
		/// </summary>
		/// <param name="provider">The provider that handles the attachment.</param>
		/// <param name="attachment">The old name of the renamed attachment, if any.</param>
		/// <param name="page">The page that owns the attachment.</param>
		/// <param name="oldAttachmentName">The old name of the renamed attachment, if any.</param>
		/// <param name="activity">The activity.</param>
		public void OnAttachmentActivity(string provider, string attachment, string page, string oldAttachmentName, FileActivity activity) {
			string wiki = GetCurrentWiki();

			if(FileActivity != null) {
				IFilesStorageProviderV40 prov = Collectors.CollectorsBox.FilesProviderCollector.GetProvider(provider, wiki);
				
				FileActivity(this, new FileActivityEventArgs(
					new StFileInfo(prov.GetPageAttachmentDetails(page, attachment), attachment, prov), oldAttachmentName, null, null, page, activity));
			}
		}

		/// <summary>
		/// Fires the FileActivity event.
		/// </summary>
		/// <param name="provider">The provider that handles the directory.</param>
		/// <param name="directory">The directory that changed.</param>
		/// <param name="oldDirectoryName">The old name of the renamed directory, if any.</param>
		/// <param name="activity">The activity.</param>
		public void OnDirectoryActivity(string provider, string directory, string oldDirectoryName, FileActivity activity) {
			string wiki = GetCurrentWiki();

			if(FileActivity != null) {
				IFilesStorageProviderV40 prov = Collectors.CollectorsBox.FilesProviderCollector.GetProvider(provider, wiki);

				FileActivity(this, new FileActivityEventArgs(
					null, null, new StDirectoryInfo(directory, prov), oldDirectoryName, null, activity));
			}
		}

		/// <summary>
		/// Registers a HTTP request handler (typically during the SetUp phase of a provider).
		/// </summary>
		/// <param name="caller">The caller.</param>
		/// <param name="urlRegex">The regular expression used to filter URLs to decide whether or not a request must be handled.</param>
		/// <param name="methods">The HTTP request methods to consider, such as GET and POST.</param>
		/// <returns>A token to use to unregister the handler (<see cref="UnregisterRequestHandler"/>).</returns>
		/// <remarks>The <paramref name="urlRegex"/> will be treated as culture-invariant and case-insensitive.</remarks>
		public string RegisterRequestHandler(IProviderV40 caller, string urlRegex, string[] methods) {
			if(caller == null) throw new ArgumentNullException("caller");
			if(urlRegex == null) throw new ArgumentNullException("urlRegex");
			if(urlRegex.Length == 0) throw new ArgumentException("Regex cannot be empty", "urlRegex");
			if(methods == null) throw new ArgumentNullException("methods");
			if(methods.Length == 0) throw new ArgumentException("At least one method is required", "methods");

			string token = Guid.NewGuid().ToString("N");

			lock(_requestHandlers) {
				_requestHandlers.Add(token, new RequestHandlerRegistryEntry() {
					CallerType = caller.GetType(),
					Token = token,
					UrlRegex = new Regex(urlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
					Methods = methods
				});
			}

			return token;
		}

		/// <summary>
		/// Unregisters a request handler.
		/// </summary>
		/// <param name="token">The token returned by <see cref="RegisterRequestHandler"/>.</param>
		public void UnregisterRequestHandler(string token) {
			if(token == null) throw new ArgumentNullException("token");
			if(token.Length == 0) throw new ArgumentException("Token cannot be empty", "token");

			lock(_requestHandlers) {
				_requestHandlers.Remove(token);
			}
		}

		/// <summary>
		/// Allows to inject content into the HTML head, like scripts and css (typically during the SetUp phase of a provider).
		/// </summary>
		/// <param name="caller">The caller.</param>
		/// <param name="content">The content to inject.</param>
		/// <returns>A token to use to remove the injected content.</returns>
		public string AddHtmlHeadContent(IProviderV40 caller, string content) {
			if(caller == null) throw new ArgumentNullException("caller");
			if(content == null) throw new ArgumentNullException("content");
			if(content.Length == 0) throw new ArgumentException("Content cannot be empty", "content");

			string token = Guid.NewGuid().ToString("N");

			lock(_htmlHeadContent) {
				_htmlHeadContent.Add(token, new HtmlHeadContentItem() {
					CallerType = caller.GetType(),
					Token = token,
					Content = content
				});
			}

			return token;
		}

		/// <summary>
		/// Removes injected HTML head content.
		/// </summary>
		/// <param name="token">The token returned by <see cref="AddHtmlHeadContent"/>.</param>
		public void RemoveHtmlHeadContent(string token) {
			if(token == null) throw new ArgumentNullException("token");
			if(token.Length == 0) throw new ArgumentException("Token cannot be empty", "token");

			lock(_htmlHeadContent) {
				_htmlHeadContent.Remove(token);
			}
		}

	}

	/// <summary>
	/// Represents a request handler.
	/// </summary>
	public class RequestHandlerRegistryEntry {

		/// <summary>
		/// Gets or sets the caller type.
		/// </summary>
		public Type CallerType { get; set; }

		/// <summary>
		/// Get or sets the unique token assigned to the handler.
		/// </summary>
		public string Token { get; set; }

		/// <summary>
		/// Gets or sets the URL regex.
		/// </summary>
		public Regex UrlRegex { get; set; }

		/// <summary>
		/// Gets or sets the methods.
		/// </summary>
		public string[] Methods { get; set; }

	}

	/// <summary>
	/// Represents custom content to be injected in the HTML head.
	/// </summary>
	public class HtmlHeadContentItem {

		/// <summary>
		/// Gets or sets the caller type.
		/// </summary>
		public Type CallerType { get; set; }

		/// <summary>
		/// Gets or sets the unique token assigned to the content.
		/// </summary>
		public string Token { get; set; }

		/// <summary>
		/// Gets or sets the content.
		/// </summary>
		public string Content { get; set; }

	}

	/// <summary>
	/// Represents a custom toolbar item.
	/// </summary>
	public class CustomToolbarItem {

		/// <summary>
		/// Gets or sets the caller type.
		/// </summary>
		public Type CallerType { get; set; }

		/// <summary>
		/// Gets or sets the unique token assigned to the content.
		/// </summary>
		public string Token { get; set; }

		/// <summary>
		/// Gets or sets the item.
		/// </summary>
		public ToolbarItem Item { get; set; }

		/// <summary>
		/// Gets the text.
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		public string Value { get; set; }

	}

}
