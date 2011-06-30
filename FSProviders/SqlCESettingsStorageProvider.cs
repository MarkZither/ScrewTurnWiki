
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.Plugins.SqlCommon;
using ScrewTurn.Wiki.PluginFramework;
using System.Data.SqlClient;
using System.Data.Common;
using System.Data.SqlServerCe;

namespace ScrewTurn.Wiki.Plugins.FSProviders {

	/// <summary>
	/// Implements a SQL Server-based settings storage provider.
	/// </summary>
	public class SqlCESettingsStorageProvider : SqlSettingsStorageProviderBase {

		private readonly ComponentInformation info = new ComponentInformation("SQL Server Settings Storage Provider", "Threeplicate Srl", "4.0.1.71", "http://www.screwturn.eu", null);

		private readonly SqlCECommandBuilder commandBuilder = new SqlCECommandBuilder();

		private const int CurrentSchemaVersion = 4000;

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
		protected override void ValidateConnectionString(string connString) {
			DbCommand cmd = null;
			try {
				cmd = GetCommand(connString);
			}
			catch(SqlException ex) {
				throw new InvalidConfigurationException("Provided connection string is not valid", ex);
			}
			catch(InvalidOperationException ex) {
				throw new InvalidConfigurationException("Provided connection string is not valid", ex);
			}
			catch(ArgumentException ex) {
				throw new InvalidConfigurationException("Provided connection string is not valid", ex);
			}
			finally {
				try {
					cmd.Connection.Close();
				}
				catch { }
			}
		}

		/// <summary>
		/// Detects whether the database schema exists.
		/// </summary>
		/// <returns><c>true</c> if the schema exists, <c>false</c> otherwise.</returns>
		private bool SchemaExists() {
			DbCommand cmd = GetCommand(connString);
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
			DbCommand cmd = GetCommand(connString);
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
			DbCommand cmd = GetCommand(connString);
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
			}
			catch(DbException) { }
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
			get { return "Connection string format:<br /><code>Data Source=<i>Database Address and Instance</i>;Initial Catalog=<i>Database name</i>;User ID=<i>login</i>;Password=<i>password</i>;</code>"; }
		}

	}

}
