
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
	public class SqlServerStorageProviderUtility : SqlClassBase, ISqlStorageProviderUtility {

		private IHostV40 _host;
		private string _wiki;

		private readonly SqlServerCommandBuilder commandBuilder = new SqlServerCommandBuilder();

		/// <summary>
		/// Initializes a new instance of the <see cref="SqlServerStorageProviderUtility"/> class.
		/// </summary>
		/// <param name="host">The host.</param>
		/// <param name="wiki">The wiki.</param>
		public SqlServerStorageProviderUtility(IHostV40 host, string wiki) {
			_host = host;
			_wiki = wiki;
		}

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
		/// Gets the command builder.
		/// </summary>
		/// <returns>The command builder.</returns>
		public ICommandBuilder GetCommandBuilder2() {
			return commandBuilder;
		}

		/// <summary>
		/// Closes the connection.
		/// </summary>
		/// <param name="connection">The connection.</param>
		public void CloseDbConnection(System.Data.Common.DbConnection connection) {
			try {
				connection.Close();
			}
			catch { }
		}

		/// <summary>
		/// Logs an exception.
		/// </summary>
		/// <param name="ex">The exception.</param>
		protected override void LogException(Exception ex) {
			try {
				_host.LogEntry(ex.ToString(), LogEntryType.Error, null, this, _wiki);
			}
			catch { }
		}

		/// <summary>
		/// Closes a connection, swallowing all exceptions.
		/// </summary>
		/// <param name="connection">The connection to close.</param>
		protected override void CloseConnection(System.Data.Common.DbConnection connection) {
			CloseDbConnection(connection);
		}
	}
}