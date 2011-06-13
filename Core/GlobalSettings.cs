
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web.Configuration;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.Plugins.AzureStorage;
using System.Security.Cryptography;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Allows access to all the ScrewTurn Wiki global settings and configuration options.
	/// </summary>
	public static class GlobalSettings {

		private static string version = null;

		/// <summary>
		/// A value indicating whether the public directory can still be overridden.
		/// </summary>
		internal static bool CanOverridePublicDirectory = true;
		private static string _overriddenPublicDirectory = null;

		/// <summary>
		/// Gets the global settings storage provider.
		/// </summary>
		public static IGlobalSettingsStorageProviderV40 Provider {
			get { return Collectors.CollectorsBox.GlobalSettingsProvider; }
		}

		/// <summary>
		/// Gets the Master Password of the given wiki, used to encrypt the Users data.
		/// </summary>
		public static string GetMasterPassword() {
			return SettingsTools.GetString(Provider.GetSetting("MasterPassword"), "");
		}

		/// <summary>
		/// Sets the master password for the given wiki, used to encrypt the Users data.
		/// </summary>
		/// <param name="newMasterPassword">The new master password.</param>
		public static void SetMasterPassword(string newMasterPassword) {
			Provider.SetSetting("MasterPassword", newMasterPassword);
		}

		/// <summary>
		/// Gets the bytes of the MasterPassword.
		/// </summary>
		public static byte[] GetMasterPasswordBytes() {
			MD5 md5 = MD5CryptoServiceProvider.Create();
			return md5.ComputeHash(System.Text.UTF8Encoding.UTF8.GetBytes(GetMasterPassword()));
		}

		/// <summary>
		/// Gets the name of the Login Cookie.
		/// </summary>
		public static string LoginCookieName {
			get { return "ScrewTurnWikiLogin3"; }
		}

		/// <summary>
		/// Gets the name of the Culture Cookie.
		/// </summary>
		public static string CultureCookieName {
			get { return "ScrewTurnWikiCulture3"; }
		}

		/// <summary>
		/// Gets the version of the Wiki.
		/// </summary>
		public static string WikiVersion {
			get {
				if(version == null) {
					version = typeof(Settings).Assembly.GetName().Version.ToString();
				}

				return version;
			}
		}

		/// <summary>
		/// Overrides the public directory, unless it's too late to do that.
		/// </summary>
		/// <param name="fullPath">The full path.</param>
		internal static void OverridePublicDirectory(string fullPath) {
			if(!CanOverridePublicDirectory) throw new InvalidOperationException("Cannot override public directory - that can only be done during Settings Storage Provider initialization");

			_overriddenPublicDirectory = fullPath;
		}

		/// <summary>
		/// Gets direction of the application
		/// </summary>
		public static string Direction {
			get {
				if(Tools.IsRightToLeftCulture()) return "rtl";
				else return "ltr";
			}
		}

		/// <summary>
		/// Gets the extension used for Pages, including the dot.
		/// </summary>
		public static string PageExtension {
			get { return ".ashx"; }
		}

		/// <summary>
		/// Gets the display name validation regex.
		/// </summary>
		public static string DisplayNameRegex {
			get { return "^[^\\|\\r\\n]*$"; }
		}

		/// <summary>
		/// Gets the Email validation Regex.
		/// </summary>
		public static string EmailRegex {
			get { return @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,5}|[0-9]{1,3})$"; }
		}

		/// <summary>
		/// Gets the WikiTitle validation Regex.
		/// </summary>
		public static string WikiTitleRegex {
			get { return ".+"; }
		}

		/// <summary>
		/// Gets the MainUrl validation Regex.
		/// </summary>
		public static string MainUrlRegex {
			get { return @"^https?\://{1}\S+/$"; }
		}

		/// <summary>
		/// Gets the SMTP Server validation Regex.
		/// </summary>
		public static string SmtpServerRegex {
			get { return @"^[A-Za-z0-9\.\-_]+$"; }
		}

		#region Directories and Files

		/// <summary>
		/// Gets the Root Directory of the Wiki.
		/// </summary>
		public static string RootDirectory {
			get { return System.Web.HttpRuntime.AppDomainAppPath; }
		}

		/// <summary>
		/// Gets the Public Directory of the Wiki.
		/// </summary>
		public static string PublicDirectory {
			get {
				if(!string.IsNullOrEmpty(_overriddenPublicDirectory)) return _overriddenPublicDirectory;

				string pubDirName = PublicDirectoryName;
				if(Path.IsPathRooted(pubDirName)) return pubDirName;
				else {
					string path = Path.Combine(RootDirectory, pubDirName);
					if(!path.EndsWith(Path.DirectorySeparatorChar.ToString())) path += Path.DirectorySeparatorChar;
					return path;
				}
			}
		}

		/// <summary>
		/// Gets the Public Directory Name (without the full Path) of the Wiki.
		/// </summary>
		private static string PublicDirectoryName {
			get {
				string dir = WebConfigurationManager.AppSettings["PublicDirectory"];
				if(string.IsNullOrEmpty(dir)) throw new InvalidConfigurationException("PublicDirectory cannot be empty or null");
				dir = dir.Trim('\\', '/'); // Remove '/' and '\' from head and tail
				if(string.IsNullOrEmpty(dir)) throw new InvalidConfigurationException("PublicDirectory cannot be empty or null");
				else return dir;
			}
		}

		/// <summary>
		/// Gets the Name of the Themes directory.
		/// </summary>
		public static string ThemesDirectoryName {
			get { return "Themes"; }
		}

		/// <summary>
		/// Gets the Themes directory.
		/// </summary>
		public static string ThemesDirectory {
			get { return RootDirectory + ThemesDirectoryName + Path.DirectorySeparatorChar; }
		}

		/// <summary>
		/// Gets the Name of the JavaScript Directory.
		/// </summary>
		public static string JsDirectoryName {
			get { return "JS"; }
		}



		/// <summary>
		/// Gets the JavaScript Directory.
		/// </summary>
		public static string JsDirectory {
			get { return RootDirectory + JsDirectoryName + Path.DirectorySeparatorChar; }
		}

		#endregion

		#region Basic Settings and Associated Data

		/// <summary>
		/// Gets or sets the SMTP Server.
		/// </summary>
		public static string SmtpServer {
			get {
				return SettingsTools.GetString(Provider.GetSetting("SmtpServer"), "smtp.server.com");
			}
			set {
				Provider.SetSetting("SmtpServer", value);
			}
		}

		/// <summary>
		/// Gets or sets the SMTP Server Username.
		/// </summary>
		public static string SmtpUsername {
			get {
				return SettingsTools.GetString(Provider.GetSetting("SmtpUsername"), "");
			}
			set {
				Provider.SetSetting("SmtpUsername", value);
			}
		}

		/// <summary>
		/// Gets or sets the SMTP Server Password.
		/// </summary>
		public static string SmtpPassword {
			get {
				return SettingsTools.GetString(Provider.GetSetting("SmtpPassword"), "");
			}
			set {
				Provider.SetSetting("SmtpPassword", value);
			}
		}

		/// <summary>
		/// Gets or sets the SMTP Server Port.
		/// </summary>
		public static int SmtpPort {
			get {
				return SettingsTools.GetInt(Provider.GetSetting("SmtpPort"), -1);
			}
			set {
				Provider.SetSetting("SmtpPort", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether to enable SSL in SMTP.
		/// </summary>
		public static bool SmtpSsl {
			get {
				return SettingsTools.GetBool(Provider.GetSetting("SmtpSsl"), false);
			}
			set {
				Provider.SetSetting("SmtpSsl", SettingsTools.PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets the Contact Email.
		/// </summary>
		public static string ContactEmail {
			get {
				return SettingsTools.GetString(Provider.GetSetting("ContactEmail"), "info@server.com");
			}
			set {
				Provider.SetSetting("ContactEmail", value);
			}
		}

		/// <summary>
		/// Gets or sets the Sender Email.
		/// </summary>
		public static string SenderEmail {
			get {
				return SettingsTools.GetString(Provider.GetSetting("SenderEmail"), "no-reply@server.com");
			}
			set {
				Provider.SetSetting("SenderEmail", value);
			}
		}

		/// <summary>
		/// Gets or sets the email addresses to send a message to when an error occurs.
		/// </summary>
		public static string[] ErrorsEmails {
			get {
				return SettingsTools.GetString(Provider.GetSetting("ErrorsEmails"), "").Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			}
			set {
				Provider.SetSetting("ErrorsEmails", string.Join("|", value));
			}
		}

		/// <summary>
		/// Gets the correct path to use with Cookies.
		/// </summary>
		public static string CookiePath {
			get {
				string requestUrl = System.Web.HttpContext.Current.Request.RawUrl;
				string virtualDirectory = System.Web.HttpContext.Current.Request.ApplicationPath;
				// We need to convert the case of the virtual directory to that used in the url
				// Return the virtual directory as is if we can't find it in the URL
				if(requestUrl.ToLowerInvariant().Contains(virtualDirectory.ToLowerInvariant())) {
					return requestUrl.Substring(requestUrl.ToLowerInvariant().IndexOf(virtualDirectory.ToLowerInvariant()), virtualDirectory.Length);
				}
				return virtualDirectory;
			}
		}

		/// <summary>
		/// Gets or sets the Type name of the Default Users Provider.
		/// </summary>
		public static string DefaultUsersProvider {
			get {
				return SettingsTools.GetString(Provider.GetSetting("DefaultUsersProvider"), typeof(UsersStorageProvider).ToString());
			}
			set {
				Provider.SetSetting("DefaultUsersProvider", value);
			}
		}

		/// <summary>
		/// Gets or sets the Type name of the Default Pages Provider.
		/// </summary>
		public static string DefaultPagesProvider {
			get {
				return SettingsTools.GetString(Provider.GetSetting("DefaultPagesProvider"), typeof(PagesStorageProvider).ToString());
			}
			set {
				Provider.SetSetting("DefaultPagesProvider", value);
			}
		}

		/// <summary>
		/// Gets or sets the Type name of the Default Files Provider.
		/// </summary>
		public static string DefaultFilesProvider {
			get {
				return SettingsTools.GetString(Provider.GetSetting("DefaultFilesProvider"), typeof(FilesStorageProvider).ToString());
			}
			set {
				Provider.SetSetting("DefaultFilesProvider", value);
			}
		}

		/// <summary>
		/// Gets or sets the Type name of the Default Themes Provider.
		/// </summary>
		public static string DefaultThemesProvider {
			get {
				return SettingsTools.GetString(Provider.GetSetting("DefaultThemesProvider"), typeof(ThemeStorageProvider).ToString());
			}
			set {
				Provider.SetSetting("DefaultThemesProvider", value);
			}
		}

		#endregion

		#region Advanced Settings and Associated Data

		/// <summary>
		/// Gets or sets a value indicating whether to disable the Automatic Version Check.
		/// </summary>
		public static bool DisableAutomaticVersionCheck {
			get {
				return SettingsTools.GetBool(Provider.GetSetting("DisableAutomaticVersionCheck"), false);
			}
			set {
				Provider.SetSetting("DisableAutomaticVersionCheck", SettingsTools.PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets the Max file size for upload (in KB).
		/// </summary>
		public static int MaxFileSize {
			get {
				return SettingsTools.GetInt(Provider.GetSetting("MaxFileSize"), 10240);
			}
			set {
				Provider.SetSetting("MaxFileSize", value.ToString());
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether ViewState compression is enabled or not.
		/// </summary>
		public static bool EnableViewStateCompression {
			get {
				return SettingsTools.GetBool(Provider.GetSetting("EnableViewStateCompression"), false);
			}
			set {
				Provider.SetSetting("EnableViewStateCompression", SettingsTools.PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets a value specifying whether HTTP compression is enabled or not.
		/// </summary>
		public static bool EnableHttpCompression {
			get {
				return SettingsTools.GetBool(Provider.GetSetting("EnableHttpCompression"), false);
			}
			set {
				Provider.SetSetting("EnableHttpCompression", SettingsTools.PrintBool(value));
			}
		}

		/// <summary>
		/// Gets or sets the Username validation Regex.
		/// </summary>
		public static string UsernameRegex {
			get {
				return SettingsTools.GetString(Provider.GetSetting("UsernameRegex"), @"^\w[\w\ !$@%^\.\(\)\-_]{3,25}$");
			}
			set {
				Provider.SetSetting("UsernameRegex", value);
			}
		}

		/// <summary>
		/// Gets or sets the Password validation Regex.
		/// </summary>
		public static string PasswordRegex {
			get {
				return SettingsTools.GetString(Provider.GetSetting("PasswordRegex"), @"^\w[\w~!@#$%^\(\)\[\]\{\}\.,=\-_\ ]{5,25}$");
			}
			set {
				Provider.SetSetting("PasswordRegex", value);
			}
		}

		/// <summary>
		/// Gets or sets the Logging Level.
		/// </summary>
		public static LoggingLevel LoggingLevel {
			get {
				int value = SettingsTools.GetInt(Provider.GetSetting("LoggingLevel"), 3);
				return (LoggingLevel)value;
			}
			set {
				Provider.SetSetting("LoggingLevel", ((int)value).ToString());
			}
		}

		/// <summary>
		/// Gets or sets the Max size of the Log file (KB).
		/// </summary>
		public static int MaxLogSize {
			get {
				return SettingsTools.GetInt(Provider.GetSetting("MaxLogSize"), 256);
			}
			set {
				Provider.SetSetting("MaxLogSize", value.ToString());
			}
		}
		
		#endregion
	}
}
