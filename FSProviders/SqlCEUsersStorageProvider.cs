
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.Plugins.SqlCommon;
using ScrewTurn.Wiki.PluginFramework;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.Data.Common;

namespace ScrewTurn.Wiki.Plugins.FSProviders {

	/// <summary>
	/// Implements a SQL CE-based users storage provider.
	/// </summary>
	public class SqlCEUsersStorageProvider : SqlUsersStorageProviderBase, IUsersStorageProviderV40 {

		private readonly ComponentInformation info = new ComponentInformation("SQL Server Users Storage Provider", "Threeplicate Srl", "4.0.1.71", "http://www.screwturn.eu", null);

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
			cmd.CommandText = "select [Version] from [Version] where [Component] = 'Users'";

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
			cmd.CommandText = "select [Version] from [Version] where [Component] = 'Users'";

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
				cmd.CommandText = "create table [User] ([Wiki] nvarchar(100) not null, [Username] nvarchar(100) not null, [PasswordHash] nvarchar(100) not null, [DisplayName] nvarchar(150), [Email] nvarchar(100) not null, [Active] bit not null, [DateTime] datetime not null, constraint [PK_User] primary key ([Wiki], [Username]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [UserGroup] ([Wiki] nvarchar(100) not null, [Name] nvarchar(100) not null, [Description] nvarchar(150), constraint [PK_UserGroup] primary key ([Wiki], [Name]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [UserGroupMembership] ([Wiki] nvarchar(100) not null, [User] nvarchar(100) not null, [UserGroup] nvarchar(100) not null, constraint [FK_UserGroupMembership_User] foreign key ([Wiki], [User]) references [User]([Wiki], [UserName]) on delete cascade on update cascade, constraint [FK_UserGroupMembership_UserGroup] foreign key ([Wiki], [UserGroup]) references [UserGroup]([Wiki], [Name]) on delete cascade on update cascade, constraint [PK_UserGroupMembership] primary key ([Wiki], [User], [UserGroup]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [UserData] ([Wiki] nvarchar(100) not null, [User] nvarchar(100) not null, [Key] nvarchar(100) not null, [Data] nvarchar(4000) not null, constraint [FK_UserData_User] foreign key ([Wiki], [User]) references [User]([Wiki], [UserName]) on delete cascade on update cascade, constraint [PK_UserData] primary key ([Wiki], [User], [Key]))";
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
