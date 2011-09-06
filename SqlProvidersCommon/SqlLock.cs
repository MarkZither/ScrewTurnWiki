
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;
using System.Data.Common;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// A Lock implementation for Sql.
	/// </summary>
	public class SqlLock : Lock{

		private ISqlStorageProviderUtility _sqlStorageProviderUtility;
		private string _connString;
		private string _wiki;
		private string _name;

		/// <summary>
		/// Initializes a new instance of the <see cref="SqlLock"/> class.
		/// </summary>
		/// <param name="sqlStorageProviderUtility">The SQL storage provider utility.</param>
		/// <param name="connString">The connection string.</param>
		/// <param name="wiki">The wiki.</param>
		/// <param name="name">The name of the lock.</param>
		public SqlLock(ISqlStorageProviderUtility sqlStorageProviderUtility, string connString, string wiki, string name) {
			_sqlStorageProviderUtility = sqlStorageProviderUtility;
			_connString = connString;
			_wiki = wiki;
			_name = name;
		}

		/// <summary>
		/// Determines whether this instance is locked.
		/// </summary>
		/// <returns><c>true</c> if this instance is locked; otherwise, <c>false</c>.</returns>
		public override bool IsLocked() {
			ICommandBuilder builder = _sqlStorageProviderUtility.GetCommandBuilder2();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("SearchIndexLock");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", _wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", _name));

			DbCommand command = builder.GetCommand(_connString, query, parameters);

			DbDataReader reader = _sqlStorageProviderUtility.ExecuteReader(command);

			if(reader != null) {
				string result = null;

				if(reader.Read()) {
					result = reader["Value"] as string;
				}

				_sqlStorageProviderUtility.CloseReader(command, reader);

				return result == "locked";
			}
			else return false;
		}

		/// <summary>
		/// Obtains the lock.
		/// </summary>
		/// <returns><c>true</c> if the lock has been obtained, <c>false</c> otherwise.</returns>
		public override bool Obtain() {
			ICommandBuilder builder = _sqlStorageProviderUtility.GetCommandBuilder2();
			DbConnection connection = builder.GetConnection(_connString);
			DbTransaction transaction = _sqlStorageProviderUtility.BeginTransaction(connection);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("SearchIndexLock");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", _wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", _name));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = _sqlStorageProviderUtility.ExecuteNonQuery(command, false);

			if(rows != 0) {
				_sqlStorageProviderUtility.RollbackTransaction(transaction);
				return false; // Deletion command failed (0 is the only accepted value)
			}

			query = queryBuilder.InsertInto("SearchIndexLock",
				new string[] { "wiki", "Name", "Value" }, new string[] { "Wiki", "Name", "Value" });
			parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", _wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", _name));
			parameters.Add(new Parameter(ParameterType.String, "Value", "locked"));

			command = builder.GetCommand(transaction, query, parameters);

			rows = _sqlStorageProviderUtility.ExecuteNonQuery(command, false);

			if(rows == 1) _sqlStorageProviderUtility.CommitTransaction(transaction);
			else _sqlStorageProviderUtility.RollbackTransaction(transaction);

			return rows == 1;
		}

		/// <summary>
		/// Releases the lock.
		/// </summary>
		public override void Release() {
			ICommandBuilder builder = _sqlStorageProviderUtility.GetCommandBuilder2();
			DbConnection connection = builder.GetConnection(_connString);
			DbTransaction transaction = _sqlStorageProviderUtility.BeginTransaction(connection);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("SearchIndexLock");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", _wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", _name));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = _sqlStorageProviderUtility.ExecuteNonQuery(command, false);

			if(rows == 1) _sqlStorageProviderUtility.CommitTransaction(transaction);
			else _sqlStorageProviderUtility.RollbackTransaction(transaction);
		}
	}
}
