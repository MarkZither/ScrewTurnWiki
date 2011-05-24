
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using Microsoft.WindowsAzure.StorageClient;

namespace ScrewTurn.Wiki.Plugins.AzureStorage {

	public class AzureGlobalSettingsStorageProvider :IGlobalSettingsStorageProviderV30 {

		private IHostV30 _host;

		private TableServiceContext _context;
		private CloudBlobClient _client;

		#region IGlobalSettingsStorageProviderV30 Members

		private Dictionary<string, string> _settingsDictionary;

		// Cache of settings as dictionary settingKey -> settingValue
		private Dictionary<string, string> _settings {
			get {
				if(_settingsDictionary == null) {
					_settingsDictionary = new Dictionary<string, string>();
					IList<GlobalSettingsEntity> settingsEntities = GetGlobalSettingsEntities();
					foreach(GlobalSettingsEntity settingsEntity in settingsEntities) {
						_settingsDictionary.Add(settingsEntity.RowKey, settingsEntity.Value + "");
					}
				}
				return _settingsDictionary;
			}
		}

		private IList<GlobalSettingsEntity> GetGlobalSettingsEntities() {
			var query = (from e in _context.CreateQuery<GlobalSettingsEntity>(GlobalSettingsTable).AsTableServiceQuery()
						 where e.PartitionKey.Equals("0")
						 select e).AsTableServiceQuery();
			return QueryHelper<GlobalSettingsEntity>.All(query);
		}

		private GlobalSettingsEntity GetGlobalSettingsEntity(string settingName) {
			var query = (from e in _context.CreateQuery<GlobalSettingsEntity>(GlobalSettingsTable).AsTableServiceQuery()
						 where e.PartitionKey.Equals("0") && e.RowKey.Equals(settingName)
						 select e).AsTableServiceQuery();
			return QueryHelper<GlobalSettingsEntity>.FirstOrDefault(query);
		}

		/// <summary>
		/// Retrieves the value of a Setting.
		/// </summary>
		/// <param name="name">The name of the Setting.</param>
		/// <returns>The value of the Setting, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		public string GetSetting(string name) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("name");

			try {
				string val = null;
				if(_settings.TryGetValue(name, out val)) return val;
				else return null;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets the all the setting values.
		/// </summary>
		/// <returns>All the settings.</returns>
		public IDictionary<string, string> GetAllSettings() {
			return _settings;
		}

		/// <summary>
		/// Stores the value of a Setting.
		/// </summary>
		/// <param name="name">The name of the Setting.</param>
		/// <param name="value">The value of the Setting. Value cannot contain CR and LF characters, which will be removed.</param>
		/// <returns>True if the Setting is stored, false otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		public bool SetSetting(string name, string value) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("name");

			_settingsDictionary = null;

			// Nulls are converted to empty strings
			if(value == null) value = "";

			GlobalSettingsEntity settingsEntity = GetGlobalSettingsEntity(name);

			if(settingsEntity == null) {
				settingsEntity = new GlobalSettingsEntity() {
					PartitionKey = "0",
					RowKey = name,
					Value = value
				};
				_context.AddObject(GlobalSettingsTable, settingsEntity);
			}
			else {
				settingsEntity.Value = value;
				_context.UpdateObject(settingsEntity);
			}
			_context.SaveChangesStandard();
			return true;
		}

		/// <summary>
		/// Alls the wikis.
		/// </summary>
		/// <returns>A list of wiki identifiers.</returns>
		public IList<PluginFramework.Wiki> AllWikis() {
			return new List<ScrewTurn.Wiki.PluginFramework.Wiki>() { new ScrewTurn.Wiki.PluginFramework.Wiki("", new List<string>() {"testonazure.cloudapp.net"}),
																	 new ScrewTurn.Wiki.PluginFramework.Wiki("sample1", new List<string>() {"sample1.threeplicate.com"}),
																	 new ScrewTurn.Wiki.PluginFramework.Wiki("sample2", new List<string>() {"sample2.threeplicate.com"})};
		}

		/// <summary>
		/// Extracts the name of the wiki from the given host.
		/// </summary>
		/// <param name="host">The host.</param>
		/// <returns>The name of the wiki</returns>
		public string ExtractWikiName(string host) {
			switch(host) {
				case "sample1.threeplicate.com":
					return "sample1";
				case "sample2.threeplicate.com":
					return "sample2";
				default:
					return "";
			}
		}

		/// <summary>
		/// Lists the stored plugin assemblies.
		/// </summary>
		public string[] ListPluginAssemblies() {
			var containerRef = _client.GetContainerReference(AssembliesContainer);
			var blobs = containerRef.ListBlobs();

			List<string> assemblies = new List<string>();
			foreach(var blob in blobs) {
				string blobName = blob.Uri.PathAndQuery;
				blobName = blobName.Substring(blobName.IndexOf(blob.Container.Name) + blob.Container.Name.Length);
				assemblies.Add(blobName.Trim('/'));
			}
			return assemblies.ToArray();
		}

		/// <summary>
		/// Stores a plugin's assembly, overwriting existing ones if present.
		/// </summary>
		/// <param name="filename">The file name of the assembly, such as "Assembly.dll".</param>
		/// <param name="assembly">The assembly content.</param>
		/// <returns><c>true</c> if the assembly is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="filename"/> or <paramref name="assembly"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="filename"/> or <paramref name="assembly"/> are empty.</exception>
		public bool StorePluginAssembly(string filename, byte[] assembly) {
			if(filename == null) throw new ArgumentNullException("filename");
			if(filename.Length == 0) throw new ArgumentException("Filename cannot be empty", "filename");
			if(assembly == null) throw new ArgumentNullException("assembly");
			if(assembly.Length == 0) throw new ArgumentException("Assembly cannot be empty", "assembly");

			var containerRef = _client.GetContainerReference(AssembliesContainer);
			var blobRef = containerRef.GetBlobReference(filename);
			blobRef.UploadByteArray(assembly);
			return true;
		}

		/// <summary>
		/// Retrieves a plugin's assembly.
		/// </summary>
		/// <param name="filename">The file name of the assembly.</param>
		/// <returns>The assembly content, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="filename"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="filename"/> is empty.</exception>
		public byte[] RetrievePluginAssembly(string filename) {
			if(filename == null) throw new ArgumentNullException("filename");
			if(filename.Length == 0) throw new ArgumentException("Filename cannot be empty", "filename");

			try {
				var containerRef = _client.GetContainerReference(AssembliesContainer);
				var blobRef = containerRef.GetBlobReference(filename);
				byte[] assembly = blobRef.DownloadByteArray();
				return assembly;
			}
			catch(StorageClientException ex) {
				if(ex.ErrorCode == StorageErrorCode.BlobNotFound) return null;
				throw ex;
			}
		}

		/// <summary>
		/// Removes a plugin's assembly.
		/// </summary>
		/// <param name="filename">The file name of the assembly to remove, such as "Assembly.dll".</param>
		/// <returns><c>true</c> if the assembly is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="filename"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="filename"/> is empty.</exception>
		public bool DeletePluginAssembly(string filename) {
			if(filename == null) throw new ArgumentNullException(filename);
			if(filename.Length == 0) throw new ArgumentException("Filename cannot be empty", "filename");

			try {
				var containerRef = _client.GetContainerReference(AssembliesContainer);
				var blobRef = containerRef.GetBlobReference(filename);
				return blobRef.DeleteIfExists();
			}
			catch(StorageClientException ex) {
				if(ex.ErrorCode == StorageErrorCode.BlobNotFound) return false;
				throw ex;
			}
		}

		private const int EstimatedLogEntrySize = 100; // bytes

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
		/// <exception cref="ArgumentNullException">If <paramref name="message"/> or <paramref name="user"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="message"/> or <paramref name="user"/> are empty.</exception>
		public void LogEntry(string message, EntryType entryType, string user) {
			if(message == null) throw new ArgumentNullException("message");
			if(message.Length == 0) throw new ArgumentException("Message cannot be empty", "message");
			if(user == null) throw new ArgumentNullException("user");
			if(user.Length == 0) throw new ArgumentException("User cannot be empty", "user");

			DateTime dateTime = DateTime.UtcNow;

			LogEntity logEntity = new LogEntity() {
				PartitionKey = "0",
				RowKey = dateTime.Ticks + "-" + Guid.NewGuid().ToString("N"),
				Type = EntryTypeToString(entryType),
				DateTime = dateTime,
				User = user,
				Message = message
			};
			_context.AddObject(LogsTable, logEntity);
			_context.SaveChangesStandard();

			int logSize = LogSize;
			if(logSize > int.Parse(_host.GetGlobalSettingValue(GlobalSettingName.MaxLogSize))) {
				CutLog((int)(logSize * 0.75));
			}
		}

		/// <summary>
		/// Reduces the size of the Log to the specified size (or less).
		/// </summary>
		/// <param name="size">The size to shrink the log to (in bytes).</param>
		private void CutLog(int size) {
			size = size * 1024;
			var query = (from e in _context.CreateQuery<LogEntity>(LogsTable).AsTableServiceQuery()
						 where e.PartitionKey.Equals("0")
						 select e).AsTableServiceQuery();
			LogEntity[] logEntities = QueryHelper<LogEntity>.All(query).ToArray();
			int estimatedSize = logEntities.Length * EstimatedLogEntrySize;

			if(size < estimatedSize) {
				int difference = estimatedSize - size;
				int entriesToDelete = difference / EstimatedLogEntrySize;
				// Add 10% to avoid 1-by-1 deletion when adding new entries
				entriesToDelete += entriesToDelete / 10;

				if(entriesToDelete > 0) {

					int count = 0;
					for(int i = 0; i < entriesToDelete; i++) {
						_context.DeleteObject(logEntities[i]);
						if(count > 98) {
							_context.SaveChangesStandard();
							count = 0;
						}
						else {
							count++;
						}
					}
					_context.SaveChangesStandard();
				}
			}
		}

		/// <summary>
		/// Gets all the Log Entries, sorted by date/time (oldest to newest).
		/// </summary>
		/// <returns>The Log Entries.</returns>
		public LogEntry[] GetLogEntries() {
			var query = (from e in _context.CreateQuery<LogEntity>(LogsTable).AsTableServiceQuery()
						 where e.PartitionKey.Equals("0")
						 select e).Take((int)(int.Parse(_host.GetGlobalSettingValue(GlobalSettingName.MaxLogSize)) * 9)).AsTableServiceQuery();
			IList<LogEntity> logEntities = QueryHelper<LogEntity>.All(query);

			List<LogEntry> logEntries = new List<LogEntry>();
			foreach(LogEntity entity in logEntities) {
				logEntries.Add(new LogEntry(EntryTypeParse(entity.Type), entity.DateTime.ToLocalTime(), entity.Message, entity.User));
			}
			return logEntries.ToArray();
		}

		/// <summary>
		/// Clear the Log.
		/// </summary>
		public void ClearLog() {
			var query = (from e in _context.CreateQuery<LogEntity>(LogsTable).AsTableServiceQuery()
						 where e.PartitionKey.Equals("0")
						 select e).AsTableServiceQuery();
			IList<LogEntity> logEntities = QueryHelper<LogEntity>.All(query);
			int count = 0;
			foreach(LogEntity logEntity in logEntities) {
				_context.DeleteObject(logEntity);
				if(count > 98) {
					_context.SaveChangesStandard();
					count = 0;
				}
				else {
					count++;
				}
			}
			_context.SaveChangesStandard();
		}

		public int LogSize {
			get {
				var query = (from e in _context.CreateQuery<LogEntity>(LogsTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals("0")
							 select e).AsTableServiceQuery();
				IList<LogEntity> logEntities = QueryHelper<LogEntity>.All(query);
				int estimatedSize = logEntities.Count * EstimatedLogEntrySize;

				return estimatedSize / 1024;
			}
		}

		#endregion

		#region IProviderV30 Members

		/// <summary>
		/// The global settings table name.
		/// </summary>
		public static readonly string GlobalSettingsTable = "GlobalSettings";

		/// <summary>
		/// The Logs table name.
		/// </summary>
		public static readonly string LogsTable = "Logs";

		/// <summary>
		/// The assembly blob container name.
		/// </summary>
		public static readonly string AssembliesContainer = "assemblies";

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <param name="wiki">The wiki.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void Init(IHostV30 host, string config, string wiki) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			_host = host;

			string[] connectionStrings = config.Split(new char[] { '|' });
			if(connectionStrings == null || connectionStrings.Length != 2) throw new InvalidConfigurationException("The given connections string is invalid.");

			_context = TableStorage.GetContext(connectionStrings[0], connectionStrings[1]);

			_client = TableStorage.StorageAccount(connectionStrings[0], connectionStrings[1]).CreateCloudBlobClient();
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

			string[] connectionStrings = config.Split(new char[] { '|' });
			if(connectionStrings == null || connectionStrings.Length != 2) throw new InvalidConfigurationException("The given connections string is invalid.");

			TableStorage.CreateTable(connectionStrings[0], connectionStrings[1], GlobalSettingsTable);
			TableStorage.CreateTable(connectionStrings[0], connectionStrings[1], LogsTable);

			_client = TableStorage.StorageAccount(connectionStrings[0], connectionStrings[1]).CreateCloudBlobClient();

			CloudBlobContainer containerRef = _client.GetContainerReference(AssembliesContainer);
			containerRef.CreateIfNotExist();
		}

		public ComponentInformation Information {
			get { return new ComponentInformation("AzureTableStorage Settings Provider", "Threeplicate Srl", "0.1", "", ""); }
		}

		public string ConfigHelpHtml {
			get { return ""; }
		}

		#endregion

		#region IDisposable Members

		public void Dispose() {
			// Nothing todo
		}

		#endregion
	}

	internal class GlobalSettingsEntity : TableServiceEntity {
		// PartitionKey = "0"
		// RowKey = settingKey

		public string Value { get; set; }
	}

	internal class LogEntity : TableServiceEntity {
		// PartitionKey = "0"
		// RowKey = remainder (seconds) + salt

		public string Type { get; set; }
		public DateTime DateTime { get; set; }
		public string Message { get; set; }
		public string User { get; set; }
	}

}
