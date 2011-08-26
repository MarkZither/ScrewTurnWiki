
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.IO;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// A SqlStorageProvider utility class;
	/// </summary>
	public interface ISqlStorageProviderUtility {

		/// <summary>
		/// Gets the command builder.
		/// </summary>
		/// <returns>The command builder.</returns>
		ICommandBuilder GetCommandBuilder2();

		/// <summary>
		/// Begins the transaction.
		/// </summary>
		/// <param name="connection">The connection.</param>
		/// <returns>The transaction.</returns>
		DbTransaction BeginTransaction(DbConnection connection);

		/// <summary>
		/// Rollbacks the transaction.
		/// </summary>
		/// <param name="transaction">The transaction.</param>
		void RollbackTransaction(DbTransaction transaction);

		/// <summary>
		/// Commits the transaction.
		/// </summary>
		/// <param name="transaction">The transaction.</param>
		void CommitTransaction(DbTransaction transaction);

		/// <summary>
		/// Closes the connection.
		/// </summary>
		/// <param name="connection">The connection.</param>
		void CloseConnection(DbConnection connection);

		/// <summary>
		/// Executes a reading operation on db.
		/// </summary>
		/// <param name="command">The command to be executed.</param>
		/// <returns>A data reader object.</returns>
		DbDataReader ExecuteReader(DbCommand command);

		/// <summary>
		/// Reads a column containing binary data.
		/// </summary>
		/// <param name="reader">The db data reader.</param>
		/// <param name="column">The column to be read.</param>
		/// <param name="_fileStream">The _file stream.</param>
		/// <returns>The number of bytes read.</returns>
		int ReadBinaryColumn(DbDataReader reader, string column, Stream _fileStream);

		/// <summary>
		/// Closes the reader.
		/// </summary>
		/// <param name="reader">The reader.</param>
		void CloseReader(DbDataReader reader);

		/// <summary>
		/// Closes the reader.
		/// </summary>
		/// <param name="command">The command.</param>
		/// <param name="reader">The reader.</param>
		void CloseReader(DbCommand command, DbDataReader reader);

		/// <summary>
		/// Executes the scalar.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="command">The command.</param>
		/// <param name="defaultValue">The default value.</param>
		/// <param name="close">if set to <c>true</c> [close].</param>
		/// <returns></returns>
		T ExecuteScalar<T>(DbCommand command, T defaultValue, bool close);

		/// <summary>
		/// Executes a non query command.
		/// </summary>
		/// <param name="command">The command.</param>
		/// <param name="close">If set to <c>true</c> close the connection.</param>
		/// <returns>The number of affected rows.</returns>
		int ExecuteNonQuery(DbCommand command, bool close);
	}
}
