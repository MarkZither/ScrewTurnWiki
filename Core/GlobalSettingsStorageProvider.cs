
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using System.IO;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements a Global Settings Storage Provider against local text pluginAssemblies.
	/// </summary>
	public class GlobalSettingsStorageProvider : ProviderBase, IGlobalSettingsStorageProviderV30 {

		private IHostV30 host;
		private string wiki;

		private const string ConfigFile = "GlobalConfig.cs";
		private const string LogFile = "Log.cs";

		private bool bulkUpdating = false;
		private Dictionary<string, string> configData = null;

		private const string PluginsDirectory = "Plugins";

		private const int EstimatedLogEntrySize = 60; // bytes

		/// <summary>
		/// The name of the provider.
		/// </summary>
		private static readonly string ProviderName = "Local Global Settings Provider";

		private readonly ComponentInformation info =
			new ComponentInformation(ProviderName, "Threeplicate Srl", GlobalSettings.WikiVersion, "http://www.screwturn.eu", null);

		private string GetFullPath(string name) {
			return Path.Combine(GetDataDirectory(host), name);
		}

		private string GetFullPathForPlugin(string name) {
			return Path.Combine(Path.Combine(GetDataDirectory(host), PluginsDirectory), name);
		}

		/// <summary>
		/// Gets the wiki that has been used to initialize the current instance of the provider.
		/// </summary>
		public string CurrentWiki {
			get { return wiki; }
		}

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <param name="wiki">The wiki (can be null).</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void Init(IHostV30 host, string config, string wiki) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			this.host = host;
			this.wiki = string.IsNullOrEmpty(wiki) ? "root" : wiki;

			LoadConfig();
		}

		/// <summary>
		/// Sets up the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void SetUp(IHostV30 host, string config) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			this.host = host;

			if(!LocalProvidersTools.CheckWritePermissions(GetDataDirectory(host))) {
				throw new InvalidConfigurationException("Cannot write into the public directory - check permissions");
			}

			if(!File.Exists(GetFullPath(ConfigFile))) {
				File.Create(GetFullPath(ConfigFile)).Close();
			}

			if(!File.Exists(GetFullPath(LogFile))) {
				File.Create(GetFullPath(LogFile)).Close();
			}

			if(!Directory.Exists(GetFullPath(PluginsDirectory))) {
				Directory.CreateDirectory(GetFullPath(PluginsDirectory));
			}

			LoadConfig();
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return info; }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml {
			get { return null; }
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose() {
			// Nothing to-do
		}

		#region IGlobalSettingsStorageProviderV30 Members

		/// <summary>
		/// Retrieves the value of a Setting.
		/// </summary>
		/// <param name="name">The name of the Setting.</param>
		/// <returns>The value of the Setting, or null.</returns>
		/// <exception cref="ArgumentNullException">If <b>name</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>name</b> is empty.</exception>
		public string GetSetting(string name) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			lock(this) {
				string val = null;
				if(configData.TryGetValue(name, out val)) return val;
				else return null;
			}
		}

		/// <summary>
		/// Stores the value of a Setting.
		/// </summary>
		/// <param name="name">The name of the Setting.</param>
		/// <param name="value">The value of the Setting. Value cannot contain CR and LF characters, which will be removed.</param>
		/// <returns>True if the Setting is stored, false otherwise.</returns>
		/// <remarks>This method stores the Value immediately.</remarks>
		/// <exception cref="ArgumentNullException">If <b>name</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>name</b> is empty.</exception>
		public bool SetSetting(string name, string value) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			// Nulls are converted to empty strings
			if(value == null) value = "";

			// Store, then if not bulkUpdating, dump
			lock(this) {
				configData[name] = value;
				if(!bulkUpdating) DumpConfig();

				return true;
			}
		}

		/// <summary>
		/// Gets the all the setting values.
		/// </summary>
		/// <returns>All the settings.</returns>
		public IDictionary<string, string> GetAllSettings() {
			lock(this) {
				Dictionary<string, string> result = new Dictionary<string, string>(configData.Count);
				foreach(KeyValuePair<string, string> pair in configData) {
					result.Add(pair.Key, pair.Value);
				}
				return result;
			}
		}

		/// <summary>
		/// Loads configuration settings from disk.
		/// </summary>
		private void LoadConfig() {
			// This method should not call Log.*(...)
			lock(this) {
				configData = new Dictionary<string, string>(30);
				string data = File.ReadAllText(GetFullPath(ConfigFile), System.Text.UTF8Encoding.UTF8);

				data = data.Replace("\r", "");

				string[] lines = data.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

				string[] fields;
				for(int i = 0; i < lines.Length; i++) {
					lines[i] = lines[i].Trim();

					// Skip comments
					if(lines[i].StartsWith("#")) continue;

					fields = new string[2];
					int idx = lines[i].IndexOf("=");
					if(idx < 0) continue;

					try {
						// Extract key
						fields[0] = lines[i].Substring(0, idx).Trim();
					}
					catch {
						// Unexpected string format
						continue;
					}

					try {
						// Extract value
						fields[1] = lines[i].Substring(idx + 1).Trim();
					}
					catch {
						// Blank/invalid value?
						fields[1] = "";
					}

					configData.Add(fields[0], fields[1]);
				}
			}
		}

		/// <summary>
		/// Dumps settings on disk.
		/// </summary>
		private void DumpConfig() {
			lock(this) {
				StringBuilder buffer = new StringBuilder(4096);

				string[] keys = new string[configData.Keys.Count];
				configData.Keys.CopyTo(keys, 0);
				for(int i = 0; i < keys.Length; i++) {
					buffer.AppendFormat("{0} = {1}\r\n", keys[i], configData[keys[i]]);
				}
				File.WriteAllText(GetFullPath(ConfigFile), buffer.ToString());
			}
		}

		/// <summary>
		/// Starts a Bulk update of the Settings so that a bulk of settings can be set before storing them.
		/// </summary>
		public void BeginBulkUpdate() {
			lock(this) {
				bulkUpdating = true;
			}
		}

		/// <summary>
		/// Ends a Bulk update of the Settings and stores the settings.
		/// </summary>
		public void EndBulkUpdate() {
			lock(this) {
				bulkUpdating = false;
				DumpConfig();
			}
		}

		/// <summary>
		/// Alls the wikis.
		/// </summary>
		/// <returns>A list of wiki identifiers.</returns>
		public IList<ScrewTurn.Wiki.PluginFramework.Wiki> AllWikis() {
			return new List<ScrewTurn.Wiki.PluginFramework.Wiki>() { new ScrewTurn.Wiki.PluginFramework.Wiki("", new List<string>() {"localhost"}),
																	 new ScrewTurn.Wiki.PluginFramework.Wiki("x", new List<string>() {"wiki1.acme.com"}),
																	 new ScrewTurn.Wiki.PluginFramework.Wiki("y", new List<string>() {"wiki2.acme.com"})};
		}

		/// <summary>
		/// Extracts the name of the wiki from the given host.
		/// </summary>
		/// <param name="host">The host.</param>
		/// <returns>The name of the wiki</returns>
		public string ExtractWikiName(string host) {
			switch(host) {
				case "wiki1.acme.com":
					return "x";
				case "wiki2.acme.com":
					return "y";
				default:
					return "";
			}
		}

		/// <summary>
		/// Lists the stored plugin assemblies.
		/// </summary>
		/// <returns></returns>
		public string[] ListPluginAssemblies() {
			lock(this) {
				string[] files = Directory.GetFiles(GetFullPath(PluginsDirectory), "*.dll");
				string[] result = new string[files.Length];
				for(int i = 0; i < files.Length; i++) result[i] = Path.GetFileName(files[i]);
				return result;
			}
		}

		/// <summary>
		/// Stores a plugin's assembly, overwriting existing ones if present.
		/// </summary>
		/// <param name="filename">The file name of the assembly, such as "Assembly.dll".</param>
		/// <param name="assembly">The assembly content.</param>
		/// <returns><c>true</c> if the assembly is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>filename</b> or <b>assembly</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>filename</b> or <b>assembly</b> are empty.</exception>
		public bool StorePluginAssembly(string filename, byte[] assembly) {
			if(filename == null) throw new ArgumentNullException("filename");
			if(filename.Length == 0) throw new ArgumentException("Filename cannot be empty", "filename");
			if(assembly == null) throw new ArgumentNullException("assembly");
			if(assembly.Length == 0) throw new ArgumentException("Assembly cannot be empty", "assembly");

			lock(this) {
				try {
					File.WriteAllBytes(GetFullPathForPlugin(filename), assembly);
				}
				catch(IOException) {
					return false;
				}
				return true;
			}
		}

		/// <summary>
		/// Retrieves a plugin's assembly.
		/// </summary>
		/// <param name="filename">The file name of the assembly.</param>
		/// <returns>The assembly content, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <b>filename</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>filename</b> is empty.</exception>
		public byte[] RetrievePluginAssembly(string filename) {
			if(filename == null) throw new ArgumentNullException("filename");
			if(filename.Length == 0) throw new ArgumentException("Filename cannot be empty", "filename");

			if(!File.Exists(GetFullPathForPlugin(filename))) return null;

			lock(this) {
				try {
					return File.ReadAllBytes(GetFullPathForPlugin(filename));
				}
				catch(IOException) {
					return null;
				}
			}
		}

		/// <summary>
		/// Removes a plugin's assembly.
		/// </summary>
		/// <param name="filename">The file name of the assembly to remove, such as "Assembly.dll".</param>
		/// <returns><c>true</c> if the assembly is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>filename</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>filename</b> is empty.</exception>
		public bool DeletePluginAssembly(string filename) {
			if(filename == null) throw new ArgumentNullException(filename);
			if(filename.Length == 0) throw new ArgumentException("Filename cannot be empty", "filename");

			lock(this) {
				string fullName = GetFullPathForPlugin(filename);
				if(!File.Exists(fullName)) return false;
				try {
					File.Delete(fullName);
					return true;
				}
				catch(IOException) {
					return false;
				}
			}
		}

		#endregion

		/// <summary>
		/// Sanitizes a stiring from all unfriendly characters.
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <returns>The sanitized result.</returns>
		private static string Sanitize(string input) {
			StringBuilder sb = new StringBuilder(input);
			sb.Replace("|", "{PIPE}");
			sb.Replace("\r", "");
			sb.Replace("\n", "{BR}");
			sb.Replace("<", "&lt;");
			sb.Replace(">", "&gt;");
			return sb.ToString();
		}

		/// <summary>
		/// Re-sanitizes a string from all unfriendly characters.
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <returns>The sanitized result.</returns>
		private static string Resanitize(string input) {
			StringBuilder sb = new StringBuilder(input);
			sb.Replace("<", "&lt;");
			sb.Replace(">", "&gt;");
			sb.Replace("{BR}", "\n");
			sb.Replace("{PIPE}", "|");
			return sb.ToString();
		}

		/// <summary>
		/// Converts an <see cref="T:EntryType" /> to a string.
		/// </summary>
		/// <param name="type">The entry type.</param>
		/// <returns>The corresponding string.</returns>
		private static string EntryTypeToString(EntryType type) {
			switch(type) {
				case EntryType.General:
					return "G";
				case EntryType.Warning:
					return "W";
				case EntryType.Error:
					return "E";
				default:
					return "G";
			}
		}

		/// <summary>
		/// Converts an entry type string to an <see cref="T:EntryType" />.
		/// </summary>
		/// <param name="value">The string.</param>
		/// <returns>The <see cref="T:EntryType" />.</returns>
		private static EntryType EntryTypeParse(string value) {
			switch(value) {
				case "G":
					return EntryType.General;
				case "W":
					return EntryType.Warning;
				case "E":
					return EntryType.Error;
				default:
					return EntryType.General;
			}
		}

		/// <summary>
		/// Records a message to the System Log.
		/// </summary>
		/// <param name="message">The Log Message.</param>
		/// <param name="entryType">The Type of the Entry.</param>
		/// <param name="user">The User.</param>
		/// <remarks>This method <b>should not</b> write messages to the Log using the method IHost.LogEntry.
		/// This method should also never throw exceptions (except for parameter validation).</remarks>
		/// <exception cref="ArgumentNullException">If <b>message</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>message</b> or <b>user</b> are empty.</exception>
		public void LogEntry(string message, EntryType entryType, string user) {
			if(message == null) throw new ArgumentNullException("message");
			if(message.Length == 0) throw new ArgumentException("Message cannot be empty", "message");
			if(user == null) throw new ArgumentNullException("user");
			if(user.Length == 0) throw new ArgumentException("User cannot be empty", "user");

			lock(this) {
				message = Sanitize(message);
				user = Sanitize(user);
				LoggingLevel level = (LoggingLevel)Enum.Parse(typeof(LoggingLevel), host.GetGlobalSettingValue(GlobalSettingName.LoggingLevel));
				switch(level) {
					case LoggingLevel.AllMessages:
						break;
					case LoggingLevel.WarningsAndErrors:
						if(entryType != EntryType.Error && entryType != EntryType.Warning) return;
						break;
					case LoggingLevel.ErrorsOnly:
						if(entryType != EntryType.Error) return;
						break;
					case LoggingLevel.DisableLog:
						return;
					default:
						break;
				}

				FileStream fs = null;
				try {
					fs = new FileStream(GetFullPath(LogFile), FileMode.Append, FileAccess.Write, FileShare.None);
				}
				catch {
					return;
				}

				StreamWriter sw = new StreamWriter(fs, System.Text.UTF8Encoding.UTF8);
				// Type | DateTime | Message | User
				try {
					sw.Write(EntryTypeToString(entryType) + "|" + string.Format("{0:yyyy'/'MM'/'dd' 'HH':'mm':'ss}", DateTime.Now) + "|" + message + "|" + user + "\r\n");
				}
				catch { }
				finally {
					try {
						sw.Close();
					}
					catch { }
				}

				try {
					FileInfo fi = new FileInfo(GetFullPath(LogFile));
					if(fi.Length > (long)(int.Parse(host.GetGlobalSettingValue(GlobalSettingName.MaxLogSize)) * 1024)) {
						CutLog((int)(fi.Length * 0.75));
					}
				}
				catch { }
			}
		}

		/// <summary>
		/// Reduces the size of the Log to the specified size (or less).
		/// </summary>
		/// <param name="size">The size to shrink the log to (in bytes).</param>
		private void CutLog(int size) {
			lock(this) {
				// Contains the log messages from oldest to newest, and reverse the list
				List<LogEntry> entries = new List<LogEntry>(GetLogEntries());
				entries.Reverse();

				FileInfo fi = new FileInfo(GetFullPath(LogFile));
				int difference = (int)(fi.Length - size);
				int removeEntries = difference / EstimatedLogEntrySize * 2; // Double the number of removed entries in order to reduce the # of times Cut is needed
				int preserve = entries.Count - removeEntries; // The number of entries to be preserved

				// Copy the entries to preserve in a temp list
				List<LogEntry> toStore = new List<LogEntry>();
				for(int i = 0; i < preserve; i++) {
					toStore.Add(entries[i]);
				}

				toStore.Sort((a, b) => a.DateTime.CompareTo(b.DateTime));

				StringBuilder sb = new StringBuilder();
				// Type | DateTime | Message | User
				foreach(LogEntry e in toStore) {
					sb.Append(EntryTypeToString(e.EntryType));
					sb.Append("|");
					sb.Append(e.DateTime.ToString("yyyy'/'MM'/'dd' 'HH':'mm':'ss"));
					sb.Append("|");
					sb.Append(e.Message);
					sb.Append("|");
					sb.Append(e.User);
					sb.Append("\r\n");
				}

				FileStream fs = null;
				try {
					fs = new FileStream(GetFullPath(LogFile), FileMode.Create, FileAccess.Write, FileShare.None);
				}
				catch(Exception ex) {
					throw new IOException("Unable to open the file: " + LogFile, ex);
				}

				StreamWriter sw = new StreamWriter(fs, System.Text.UTF8Encoding.UTF8);
				// Type | DateTime | Message | User
				try {
					sw.Write(sb.ToString());
				}
				catch { }
				sw.Close();
			}
		}

		/// <summary>
		/// Gets all the Log Entries, sorted by date/time (oldest to newest).
		/// </summary>
		/// <remarks>The Log Entries.</remarks>
		public LogEntry[] GetLogEntries() {
			lock(this) {
				string content = File.ReadAllText(GetFullPath(LogFile)).Replace("\r", "");
				List<LogEntry> result = new List<LogEntry>(50);
				string[] lines = content.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
				string[] fields;
				for(int i = 0; i < lines.Length; i++) {
					fields = lines[i].Split('|');
					try {
						// Try/catch to avoid problems with corrupted file (raw method)
						result.Add(new LogEntry(EntryTypeParse(fields[0]), DateTime.Parse(fields[1]), Resanitize(fields[2]), Resanitize(fields[3])));
					}
					catch { }
				}

				result.Sort((a, b) => a.DateTime.CompareTo(b.DateTime));

				return result.ToArray();
			}
		}

		/// <summary>
		/// Clear the Log.
		/// </summary>
		public void ClearLog() {
			lock(this) {
				FileStream fs = null;
				try {
					fs = new FileStream(GetFullPath(LogFile), FileMode.Create, FileAccess.Write, FileShare.None);
				}
				catch(Exception ex) {
					throw new IOException("Unable to access the file: " + LogFile, ex);
				}
				fs.Close();
			}
		}

		/// <summary>
		/// Gets the current size of the Log, in KB.
		/// </summary>
		public int LogSize {
			get {
				lock(this) {
					FileInfo fi = new FileInfo(GetFullPath(LogFile));
					return (int)(fi.Length / 1024);
				}
			}
		}
	}
}
