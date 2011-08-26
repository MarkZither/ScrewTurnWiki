
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.Plugins.SqlCommon;
using System.Data.SqlClient;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Plugins.SqlServer {

	/// <summary>
	/// An implementation of ISqlServerStorageProviderUtility for SqlServer.
	/// </summary>
	public class SqlServerStorageProviderUtility : SqlStorageProviderBase, ISqlStorageProviderUtility {
		
		private readonly ComponentInformation info = new ComponentInformation("SQL Server Files Storage Provider", "Threeplicate Srl", "4.0.1.71", "http://www.screwturn.eu", null);

		private readonly SqlServerCommandBuilder commandBuilder = new SqlServerCommandBuilder();

		private const int CurrentSchemaVersion = 4000;

		/// <summary>
		/// Begins a transaction.
		/// </summary>
		/// <param name="connection">The connection.</param>
		/// <returns>The transaction.</returns>
		public new System.Data.Common.DbTransaction BeginTransaction(System.Data.Common.DbConnection connection) {
			return base.BeginTransaction(connection);
		}

		/// <summary>
		/// Rolls back a transaction.
		/// </summary>
		/// <param name="transaction">The transaction.</param>
		public new void RollbackTransaction(System.Data.Common.DbTransaction transaction) {
			base.RollbackTransaction(transaction);
		}

		/// <summary>
		/// Commits a transaction.
		/// </summary>
		/// <param name="transaction">The transaction.</param>
		public new void CommitTransaction(System.Data.Common.DbTransaction transaction) {
			base.CommitTransaction(transaction);
		}

		/// <summary>
		/// Executes a reader command, leaving the connection open.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <returns>The data reader, or <c>null</c> if the command fails.</returns>
		public new System.Data.Common.DbDataReader ExecuteReader(System.Data.Common.DbCommand command) {
			return base.ExecuteReader(command);
		}

		/// <summary>
		/// Reads a column containing binary data.
		/// </summary>
		/// <param name="reader">The db data reader.</param>
		/// <param name="column">The column to be read.</param>
		/// <param name="_fileStream">The _file stream.</param>
		/// <returns>The number of bytes read.</returns>
		public new int ReadBinaryColumn(System.Data.Common.DbDataReader reader, string column, System.IO.Stream _fileStream) {
			return base.ReadBinaryColumn(reader, column, _fileStream);
		}

		/// <summary>
		/// Closes a reader.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public new void CloseReader(System.Data.Common.DbDataReader reader) {
			base.CloseReader(reader);
		}

		/// <summary>
		/// Closes a reader, a command and the associated connection.
		/// </summary>
		/// <param name="command">The command.</param>
		/// <param name="reader">The reader.</param>
		public new void CloseReader(System.Data.Common.DbCommand command, System.Data.Common.DbDataReader reader) {
			base.CloseReader(command, reader);
		}

		/// <summary>
		/// Executes a scalar command, then closes the connection.
		/// </summary>
		/// <typeparam name="T">The return type.</typeparam>
		/// <param name="command">The command to execute.</param>
		/// <param name="defaultValue">The default value of the return value, to use when the command fails.</param>
		/// <param name="close">A value indicating whether to close the connection after execution.</param>
		/// <returns>The result.</returns>
		public new T ExecuteScalar<T>(System.Data.Common.DbCommand command, T defaultValue, bool close) {
			return base.ExecuteScalar<T>(command, defaultValue, close);
		}

		/// <summary>
		/// Executes a non-query command, then closes the connection if requested.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <param name="close">A value indicating whether to close the connection after execution.</param>
		/// <returns>The rows affected (-1 if the command failed).</returns>
		public new int ExecuteNonQuery(System.Data.Common.DbCommand command, bool close) {
			return base.ExecuteNonQuery(command, close);
		}

		/// <summary>
		/// Gets a new command with an open connection.
		/// </summary>
		/// <param name="connString">The connection string.</param>
		/// <returns>The command.</returns>
		private SqlCommand GetCommand(string connString) {
			return commandBuilder.GetCommand(connString, "select current_user", new List<Parameter>()) as SqlCommand;
		}

		/// <summary>
		/// Validates a connection string.
		/// </summary>
		/// <param name="connString">The connection string to validate.</param>
		protected override void ValidateConnectionString(string connString) {
			SqlCommand cmd = null;
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
			SqlCommand cmd = GetCommand(connString);
			cmd.CommandText = "select [Version] from [Version] where [Component] = 'Files'";

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
			SqlCommand cmd = GetCommand(connString);
			cmd.CommandText = "select [Version] from [Version] where [Component] = 'Files'";

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
			SqlCommand cmd = GetCommand(connString);
			cmd.CommandText = Properties.Resources.FilesDatabase;

			cmd.ExecuteNonQuery();

			cmd.Connection.Close();
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

		/// <summary>
		/// Closes the database connection.
		/// </summary>
		/// <param name="connection">The connection.</param>
		public new void CloseConnection(System.Data.Common.DbConnection connection) {
			try {
				connection.Close();
			}
			catch { }
		}

		/// <summary>
		/// Gets the command builder.
		/// </summary>
		/// <returns>The command builder.</returns>
		protected override ICommandBuilder GetCommandBuilder() {
			return commandBuilder;
		}

		/// <summary>
		/// Gets the command builder.
		/// </summary>
		/// <returns>The command builder.</returns>
		public ICommandBuilder GetCommandBuilder2() {
			return commandBuilder;
		}

	}
}
