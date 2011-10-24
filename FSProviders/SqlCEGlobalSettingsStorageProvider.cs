
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.Plugins.SqlCommon;
using ScrewTurn.Wiki.PluginFramework;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.Data.Common;
using System.IO;

namespace ScrewTurn.Wiki.Plugins.FSProviders {

	/// <summary>
	/// Implements a SQL Server-based settings storage provider.
	/// </summary>
	public class SqlCEGlobalSettingsStorageProvider : SqlGlobalSettingsStorageProviderBase, IGlobalSettingsStorageProviderV40 {

		private readonly ComponentInformation info = new ComponentInformation("SQL CE Global Settings Storage Provider", "Threeplicate Srl", "4.0.5.143", "http://www.screwturn.eu", null);

		private readonly SqlCECommandBuilder commandBuilder = new SqlCECommandBuilder();

		private const int CurrentSchemaVersion = 4000;

		private const string PluginsDirectory = "Plugins";

		private string _connString = null;

		private string BuildDbConnectionString(IHostV40 host) {
			return "Data Source = '" + Path.Combine(host.GetGlobalSettingValue(GlobalSettingName.PublicDirectory), "ScrewTurnWiki.sdf") + "';";
		}

		private string GetFullPath(string finalChunk) {
			return Path.Combine(Path.Combine(host.GetGlobalSettingValue(GlobalSettingName.PublicDirectory), wiki), finalChunk);
		}

		private string GetFullPathForPlugin(string name) {
			return Path.Combine(GetFullPath(PluginsDirectory), name);
		}

		/// <summary>
		/// Sets up the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <b>host</b> or <b>config</b> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <b>config</b> is not valid or is incorrect.</exception>
		public new void SetUp(IHostV40 host, string config) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			_connString = config.Length == 0 ? BuildDbConnectionString(host) : config;

			base.SetUp(host, _connString);
		}

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <param name="wiki">The wiki.</param>
		/// <exception cref="ArgumentNullException">If <b>host</b> or <b>config</b> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <b>config</b> is not valid or is incorrect.</exception>
		public new void Init(IHostV40 host, string config, string wiki) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			_connString = config.Length == 0 ? BuildDbConnectionString(host) : config;

			base.Init(host, _connString, wiki);

			if(!Directory.Exists(GetFullPath(PluginsDirectory))) {
				Directory.CreateDirectory(GetFullPath(PluginsDirectory));
			}
		}


		/// <summary>
		/// Lists the stored plugin assemblies.
		/// </summary>
		/// <returns></returns>
		public new string[] ListPluginAssemblies() {
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
		public new bool StorePluginAssembly(string filename, byte[] assembly) {
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
		public new byte[] RetrievePluginAssembly(string filename) {
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
		public new bool DeletePluginAssembly(string filename) {
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


		/// <summary>
		/// Gets a new command with an open connection.
		/// </summary>
		/// <param name="connString">The connection string.</param>
		/// <returns>The command.</returns>
		private DbCommand GetCommand(string connString) {
			return commandBuilder.GetCommand(connString, "select current_user", new List<Parameter>()) as SqlCeCommand;
		}

		/// <summary>
		/// Gets a new command builder object.
		/// </summary>
		/// <returns>The command builder.</returns>
		protected override ICommandBuilder GetCommandBuilder() {
			return commandBuilder;
		}

		/// <summary>
		/// Validates a connection string.
		/// </summary>
		/// <param name="connString">The connection string to validate.</param>
		/// <remarks>If the connection string is invalid, the method throws <see cref="T:InvalidConfigurationException"/>.</remarks>
		protected override void ValidateConnectionString(string connString) { }

		// Check if the database with the given name exists, otherwise a new one is created.
		private void CreateDatabaseIfNotExists(string connString) {
			SqlCeConnection connection = new SqlCeConnection(connString);
			string dbFileName = connection.Database;
			if(!Path.IsPathRooted(dbFileName)) {
				dbFileName = Path.Combine(Path.Combine(host.GetGlobalSettingValue(GlobalSettingName.PublicDirectory), dbFileName));
			}
			connString = connString.Replace(connection.Database, dbFileName);
			if(!File.Exists(dbFileName)) {
				// Create database with no tables
				SqlCeEngine engine = new SqlCeEngine(connString);
				engine.CreateDatabase();
				engine.Dispose();
			}
		}

		/// <summary>
		/// Detects whether the database schema exists.
		/// </summary>
		/// <returns><c>true</c> if the schema exists, <c>false</c> otherwise.</returns>
		private bool SchemaExists() {
			DbCommand cmd = GetCommand(_connString);
			cmd.CommandText = "select [Version] from [Version] where [Component] = 'GlobalSettings'";

			bool exists = false;

			try {
				int version = ExecuteScalar<int>(cmd, -1);
				if(version > CurrentSchemaVersion) throw new InvalidConfigurationException("The version of the database schema is greater than the supported version");
				exists = version != -1;
			}
			catch(SqlCeException) {
				exists = false;
			}
			finally {
				try {
					cmd.Connection.Close();
				}
				catch { }
			}

			return exists;
		}

		/// <summary>
		/// Detects whether the database schema needs to be updated.
		/// </summary>
		/// <returns><c>true</c> if an update is needed, <c>false</c> otherwise.</returns>
		private bool SchemaNeedsUpdate() {
			DbCommand cmd = GetCommand(_connString);
			cmd.CommandText = "select [Version] from [Version] where [Component] = 'GlobalSettings'";

			bool exists = false;

			try {
				int version = ExecuteScalar<int>(cmd, -1);
				exists = version < CurrentSchemaVersion;
			}
			catch(SqlCeException) {
				exists = false;
			}
			finally {
				try {
					cmd.Connection.Close();
				}
				catch { }
			}

			return exists;
		}

		/// <summary>
		/// Creates the standard database schema.
		/// </summary>
		private void CreateStandardSchema() {
			DbCommand cmd = GetCommand(_connString);
			try {
				cmd.CommandText = "create table [GlobalSetting] ([Name] nvarchar(100) not null, [Value] nvarchar(4000) not null, constraint [PK_GlobalSetting] primary key ([Name]));";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [Log] ([Id] int not null identity, [DateTime] datetime not null, [EntryType] nchar not null, [User] nvarchar(100) not null, [Message] nvarchar(4000) not null, [Wiki] nvarchar(100), constraint [PK_Log] primary key ([Id]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [PluginAssembly] ([Name] nvarchar(100) not null, [Assembly] image not null, constraint [PK_PluginAssembly] primary key ([Name]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [Version] ([Component] nvarchar(100) not null, [Version] int not null, constraint [PK_Version] primary key ([Component]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "insert into [Version] ([Component], [Version]) values ('GlobalSettings', 4000)";
				cmd.ExecuteNonQuery();
			}
			catch(DbException ex) {
				host.LogEntry("Error while creating database schema. " + ex.Message, LogEntryType.Error, null, this, wiki);
				throw;
			}
			finally {
				cmd.Connection.Close();
			}
		}

		/// <summary>
		/// Creates or updates the database schema if necessary.
		/// </summary>
		protected override void CreateOrUpdateDatabaseIfNecessary() {
			// Check if the SqlCe database exists, otherwise creates a new one.
			CreateDatabaseIfNotExists(_connString);

			if(!SchemaExists()) {
				CreateStandardSchema();
			}
			if(SchemaNeedsUpdate()) {
				// Run minor update batches...
			}
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public override ComponentInformation Information {
			get { return info; }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public override string ConfigHelpHtml {
			get { return ""; }
		}

		/// <summary>
		/// Gets the default users storage provider, when no value is stored in the database.
		/// </summary>
		protected override string DefaultUsersStorageProvider {
			get { return typeof(SqlCEUsersStorageProvider).FullName; }
		}

		/// <summary>
		/// Gets the default pages storage provider, when no value is stored in the database.
		/// </summary>
		protected override string DefaultPagesStorageProvider {
			get { return typeof(SqlCEPagesStorageProvider).FullName; }
		}

		/// <summary>
		/// Gets the default files storage provider, when no value is stored in the database.
		/// </summary>
		protected override string DefaultFilesStorageProvider {
			get { return typeof(FilesStorageProvider).FullName; }
		}

	}

}
