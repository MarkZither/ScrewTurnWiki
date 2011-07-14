
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.Plugins.SqlCommon;
using ScrewTurn.Wiki.PluginFramework;
using System.Data.SqlClient;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.IO;

namespace ScrewTurn.Wiki.Plugins.FSProviders {

	/// <summary>
	/// Implements a SQL Server-based settings storage provider.
	/// </summary>
	public class SqlCESettingsStorageProvider : SqlSettingsStorageProviderBase, ISettingsStorageProviderV40 {

		private readonly ComponentInformation info = new ComponentInformation("SQL CE Settings Storage Provider", "Threeplicate Srl", "4.0.2.76", "http://www.screwturn.eu", null);

		private readonly SqlCECommandBuilder commandBuilder = new SqlCECommandBuilder();

		private const int CurrentSchemaVersion = 4000;

		private string _connString = null;

		private string BuildDbConnectionString(IHostV40 host) {
			return "Data Source = '" + host.GetGlobalSettingValue(GlobalSettingName.PublicDirectory) + "ScrewTurnWiki.sdf';";
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
		public new void Init(IHostV40 host, string config, string wiki) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			_connString = config.Length == 0 ? BuildDbConnectionString(host) : config;

			base.Init(host, _connString, wiki);
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
			cmd.CommandText = "select [Version] from [Version] where [Component] = 'Settings'";

			bool exists = false;

			try {
				int version = ExecuteScalar<int>(cmd, -1);
				if(version > CurrentSchemaVersion) throw new InvalidConfigurationException("The version of the database schema is greater than the supported version");
				exists = version != -1;
			}
			catch(SqlException) {
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
			cmd.CommandText = "select [Version] from [Version] where [Component] = 'Settings'";

			bool exists = false;

			try {
				int version = ExecuteScalar<int>(cmd, -1);
				exists = version < CurrentSchemaVersion;
			}
			catch(SqlException) {
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
				cmd.CommandText = "create table [Setting] ([Wiki] nvarchar(100) not null, [Name] nvarchar(100) not null, [Value] nvarchar(4000) not null, constraint [PK_Setting] primary key ([Wiki], [Name]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [MetaDataItem] ([Wiki] nvarchar(100) not null, [Name] nvarchar(100) not null, [Tag] nvarchar(100) not null, [Data] nvarchar(4000) not null, constraint [PK_MetaDataItem] primary key ([Wiki], [Name], [Tag]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [RecentChange] ([Id] int not null identity, [Wiki] nvarchar(100) not null, [Page] nvarchar(200) not null, [Title] nvarchar(200) not null, [MessageSubject] nvarchar(200), [DateTime] datetime not null, [User] nvarchar(100) not null, [Change] nchar not null, [Description] nvarchar(4000), constraint [PK_RecentChange] primary key ([Id]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [PluginStatus] ([Wiki] nvarchar(100) not null, [Name] nvarchar(150) not null, [Enabled] bit not null, [Configuration] nvarchar(4000) not null, constraint [PK_PluginStatus] primary key ([Wiki], [Name]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [OutgoingLink] ([Wiki] nvarchar(100) not null, [Source] nvarchar(100) not null, [Destination] nvarchar(100) not null, constraint [PK_OutgoingLink] primary key ([Wiki], [Source], [Destination]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [AclEntry] ([Wiki] nvarchar(100) not null, [Resource] nvarchar(200) not null, [Action] nvarchar(50) not null, [Subject] nvarchar(100) not null, [Value] nchar not null, constraint [PK_AclEntry] primary key ([Wiki], [Resource], [Action], [Subject]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "insert into [Version] ([Component], [Version]) values ('Settings', 4000)";
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

	}

}
