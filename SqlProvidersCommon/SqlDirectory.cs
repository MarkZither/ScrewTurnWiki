using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;
using System.Data.Common;
using System.Data.SqlClient;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// Implements a Lucene.NET Directory object for Sql.
	/// </summary>
	public class SqlDirectory : Directory {

		private string _connString;
		private string _catalog;
		private string _wiki;
		private ISqlStorageProviderUtility _sqlStorageProviderUtility;
		private Directory _cacheDirectory;

		/// <summary>
		/// Initializes a new instance of the <see cref="SqlDirectory"/> class.
		/// </summary>
		/// <param name="sqlStorageProviderUtility">The SQL storage provider utility.</param>
		/// <param name="connString">The connection string.</param>
		/// <param name="catalog">The catalog.</param>
		/// <param name="wiki">The wiki.</param>
		public SqlDirectory(ISqlStorageProviderUtility sqlStorageProviderUtility, string connString, string catalog, string wiki) {
			_sqlStorageProviderUtility = sqlStorageProviderUtility;
			_catalog = catalog.ToLowerInvariant();
			_connString = connString;
			_wiki = wiki;
			_initCacheDirectory();
		}

		private void _initCacheDirectory() {
			string cachePath = System.IO.Path.Combine(Environment.ExpandEnvironmentVariables("%temp%"), "SqlDirectory");
			System.IO.DirectoryInfo azureDir = new System.IO.DirectoryInfo(cachePath);
			if(!azureDir.Exists)
				azureDir.Create();

			string catalogPath = System.IO.Path.Combine(cachePath, _catalog);
			System.IO.DirectoryInfo catalogDir = new System.IO.DirectoryInfo(catalogPath);
			if(!catalogDir.Exists)
				catalogDir.Create();

			_cacheDirectory = FSDirectory.Open(catalogDir);
		}

		/// <summary>
		/// Deletes a file.
		/// </summary>
		/// <param name="name">The name of the file to be deleted.</param>
		public override void DeleteFile(string name) {
			ICommandBuilder builder = _sqlStorageProviderUtility.GetCommandBuilder2();
			DbConnection connection = builder.GetConnection(_connString);
			DbTransaction transaction = _sqlStorageProviderUtility.BeginTransaction(connection);

			if(!FileExists(transaction, name)) {
				_sqlStorageProviderUtility.RollbackTransaction(transaction);
				return;
			}

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("SearchIndex");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", _wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = _sqlStorageProviderUtility.ExecuteNonQuery(command, false);
			if(rows != 1) _sqlStorageProviderUtility.RollbackTransaction(transaction);
			else {
				_sqlStorageProviderUtility.CommitTransaction(transaction);

				if(_cacheDirectory.FileExists(name + ".blob"))
					_cacheDirectory.DeleteFile(name + ".blob");

				if(_cacheDirectory.FileExists(name))
					_cacheDirectory.DeleteFile(name);
			}
		}

		private bool FileExists(DbTransaction transaction, string name) {
			ICommandBuilder builder = _sqlStorageProviderUtility.GetCommandBuilder2();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectCountFrom("SearchIndex");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", _wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int count = _sqlStorageProviderUtility.ExecuteScalar<int>(command, -1, false);

			return count == 1;
		}

		/// <summary>
		/// Check if a file exists.
		/// </summary>
		/// <param name="name">The name of the file.</param>
		/// <returns><c>true</c> if the file exists, <c>false</c> otherwise.</returns>
		public override bool FileExists(string name) {
			ICommandBuilder builder = _sqlStorageProviderUtility.GetCommandBuilder2();
			DbConnection connection = builder.GetConnection(_connString);
			DbTransaction transaction = _sqlStorageProviderUtility.BeginTransaction(connection);

			bool exists = FileExists(transaction, name);

			_sqlStorageProviderUtility.CommitTransaction(transaction);
			return exists;
		}

		/// <summary>
		/// Get the lenght of the file.
		/// </summary>
		/// <param name="name">The name of the file.</param>
		/// <returns>The length of the file.</returns>
		public override long FileLength(string name) {
			ICommandBuilder builder = _sqlStorageProviderUtility.GetCommandBuilder2();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("SearchIndex", new string[] { "Size" });
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", _wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));

			DbCommand command = builder.GetCommand(_connString, query, parameters);

			DbDataReader reader = _sqlStorageProviderUtility.ExecuteReader(command);

			if(reader != null) {
				long fileLength = 0;

				if(reader.Read()) {
					fileLength = (long)reader["Size"];
				}

				_sqlStorageProviderUtility.CloseReader(command, reader);

				return fileLength;
			}
			else return 0;
		}

		/// <summary>
		/// Get last time a file has been modified.
		/// </summary>
		/// <param name="name">The name of the file.</param>
		/// <returns>Last modified time.</returns>
		public override long FileModified(string name) {
			ICommandBuilder builder = _sqlStorageProviderUtility.GetCommandBuilder2();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("SearchIndex", new string[] { "LastModified" });
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", _wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));

			DbCommand command = builder.GetCommand(_connString, query, parameters);

			DbDataReader reader = _sqlStorageProviderUtility.ExecuteReader(command);

			if(reader != null) {
				long lastModified = 0;

				if(reader.Read()) {
					lastModified = ((DateTime)reader["LastModified"]).Ticks;
				}

				_sqlStorageProviderUtility.CloseReader(command, reader);

				return lastModified;
			}
			else return 0;
		}

		/// <summary>
		/// Lists all files.
		/// </summary>
		/// <returns>The list of files name.</returns>
		[Obsolete]
		public override string[] List() {
			ICommandBuilder builder = _sqlStorageProviderUtility.GetCommandBuilder2();
			DbConnection connection = builder.GetConnection(_connString);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("SearchIndex", new string[] { "Name" });
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.OrderBy(query, new[] { "Name" }, new[] { Ordering.Asc });

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", _wiki));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			DbDataReader reader = _sqlStorageProviderUtility.ExecuteReader(command);

			List<string> result = new List<string>(20);
			
			if(reader != null) {

				while(reader.Read()) {
					result.Add(reader["Name"] as string);
				}

				_sqlStorageProviderUtility.CloseReader(command, reader);
			}

			return result.ToArray();
		}

		/// <summary>
		/// Opens the input.
		/// </summary>
		/// <param name="name">The name of the file.</param>
		/// <returns>An implementation of <typeref name="IndexInput"/>.</returns>
		public override IndexInput OpenInput(string name) {
			try {
				return new SqlIndexInput(this, _sqlStorageProviderUtility, _connString, _wiki, name);
			}
			catch {
				throw new System.IO.FileNotFoundException(name);
			}
		}

		/// <summary>
		/// Renames the file.
		/// </summary>
		/// <param name="from">The old name.</param>
		/// <param name="to">The new name.</param>
		[Obsolete]
		public override void RenameFile(string from, string to) {
			ICommandBuilder builder = _sqlStorageProviderUtility.GetCommandBuilder2();
			DbConnection connection = builder.GetConnection(_connString);
			DbTransaction transaction = _sqlStorageProviderUtility.BeginTransaction(connection);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			if(!FileExists(transaction, from)) {
				_sqlStorageProviderUtility.RollbackTransaction(transaction);
				throw new ArgumentException("File does not exist", "from");
			}
			if(FileExists(transaction, to)) {
				_sqlStorageProviderUtility.RollbackTransaction(transaction);
				throw new ArgumentException("File already exists", "to");
			}

			string query = queryBuilder.Update("SearchIndex", new string[] { "Name" }, new string[] { "NewName" });
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "OldName");

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", _wiki));
			parameters.Add(new Parameter(ParameterType.String, "NewName", to));
			parameters.Add(new Parameter(ParameterType.String, "OldName", from));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = _sqlStorageProviderUtility.ExecuteNonQuery(command, false);
			if(rows != 1) _sqlStorageProviderUtility.RollbackTransaction(transaction);
			else {
				_sqlStorageProviderUtility.CommitTransaction(transaction);

				// we delete and force a redownload, since we can't do this in an atomic way
				if(_cacheDirectory.FileExists(from))
					_cacheDirectory.DeleteFile(from);

				// drop old cached data as it's wrong now
				if(_cacheDirectory.FileExists(from + ".blob"))
					_cacheDirectory.DeleteFile(from + ".blob");
			}
		}

		/// <summary>
		/// Touches the file.
		/// </summary>
		/// <param name="name">The name of the file.</param>
		public override void TouchFile(string name) {
			_cacheDirectory.TouchFile(name);
		}

		/// <summary>
		/// Closes the file.
		/// </summary>
		public override void Close() {
			_sqlStorageProviderUtility = null;
		}

		/// <summary>
		/// Creates the output.
		/// </summary>
		/// <param name="name">The name of the file.</param>
		/// <returns>An implementation of <typeref name="IndexOutput"></typeref></returns>
		public override IndexOutput CreateOutput(string name) {
			return new SqlIndexOutput(this, _sqlStorageProviderUtility, _connString, _wiki, name);
		}


		/// <summary>Construct a {@link Lock}.</summary>
		/// <param name="name">The name of the lock file.</param>
		public override Lock MakeLock(string name) {
			return new SqlLock(_sqlStorageProviderUtility, _connString, _wiki, name);
		}

		/// <summary>
		/// Clears the lock.
		/// </summary>
		/// <param name="name">The name of the lock file.</param>
		public override void ClearLock(string name) {
			SqlLock sqlLock = new SqlLock(_sqlStorageProviderUtility, _connString, _wiki, name);
			sqlLock.Release();
		}

		/// <summary>
		/// Gets or sets the cache directory.
		/// </summary>
		/// <value>The cache directory.</value>
		public Directory CacheDirectory {
			get {
				return _cacheDirectory;
			}
			set {
				_cacheDirectory = value;
			}
		}

		/// <summary>
		/// Opens the cached input as stream.
		/// </summary>
		/// <param name="name">The name of the file.</param>
		public StreamInput OpenCachedInputAsStream(string name) {
			return new StreamInput(CacheDirectory.OpenInput(name));
		}

		/// <summary>
		/// Creates the cached output as stream.
		/// </summary>
		/// <param name="name">The name of the file.</param>
		public StreamOutput CreateCachedOutputAsStream(string name) {
			return new StreamOutput(CacheDirectory.CreateOutput(name));
		}
	}

}
