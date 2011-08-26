
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;
using System.IO;
using System.Data.Common;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// Implement a Lucene.NET IndexOutput for SqlServer.
	/// </summary>
	public class SqlIndexOutput : IndexOutput {

		private const int MaxFileSize = 52428800; // 50 MB

		private SqlServerDirectory _sqlServerDirectory;
		private ISqlStorageProviderUtility _sqlStorageProviderUtility;
		private IndexOutput _indexOutput;
		private string _connString;
		private string _wiki;
		private string _fileName;

		private Lucene.Net.Store.Directory CacheDirectory { get { return _sqlServerDirectory.CacheDirectory; } }

		/// <summary>
		/// Initializes a new instance of the <see cref="SqlIndexOutput"/> class.
		/// </summary>
		/// <param name="sqlServerDirectory">The SQL server directory.</param>
		/// <param name="sqlStorageProviderUtility">The SQL storage provider utility.</param>
		/// <param name="connString">The connection string.</param>
		/// <param name="wiki">The wiki.</param>
		/// <param name="fileName">The name of the file.</param>
		public SqlIndexOutput(SqlServerDirectory sqlServerDirectory, ISqlStorageProviderUtility sqlStorageProviderUtility, string connString, string wiki, string fileName) {
			_sqlServerDirectory = sqlServerDirectory;
			_sqlStorageProviderUtility = sqlStorageProviderUtility;
			_connString = connString;
			_wiki = wiki;
			_fileName = fileName;

			// create the local cache one we will operate against...
			_indexOutput = CacheDirectory.CreateOutput(_fileName);
		}

		/// <summary>
		/// Closes the sql index output.
		/// </summary>
		public override void Close() {
			string fileName = _fileName;

			// make sure it's all written out
			_indexOutput.Flush();

			long originalLength = _indexOutput.Length();
			_indexOutput.Close();

			Stream fileStream = new StreamInput(CacheDirectory.OpenInput(fileName));

			try {
				// push the file stream up to the db.
				ICommandBuilder builder = _sqlStorageProviderUtility.GetCommandBuilder2();
				DbConnection connection = builder.GetConnection(_connString);
				DbTransaction transaction = _sqlStorageProviderUtility.BeginTransaction(connection);

				QueryBuilder queryBuilder = new QueryBuilder(builder);

				bool fileExists = FileExists(transaction, _wiki, _fileName);

				// To achieve decent performance, an UPDATE query is issued if the file exists,
				// otherwise an INSERT query is issued

				string query;
				List<Parameter> parameters;

				byte[] fileData = null;
				int size = Tools.ReadStream(fileStream, ref fileData, MaxFileSize);
				if(size < 0) {
					_sqlStorageProviderUtility.RollbackTransaction(transaction);
					throw new ArgumentException("Source Stream contains too much data", "sourceStream");
				}

				long filemodlong = CacheDirectory.FileModified(fileName);
				DateTime dt = new DateTime(filemodlong).ToUniversalTime();

				if(fileExists) {
					query = queryBuilder.Update("SearchIndex", new string[] { "Size", "LastModified", "Data" }, new string[] { "Size", "LastModified", "Data" });
					query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
					query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");

					parameters = new List<Parameter>(5);
					parameters.Add(new Parameter(ParameterType.String, "Wiki", _wiki));
					parameters.Add(new Parameter(ParameterType.Int64, "Size", (long)originalLength));
					parameters.Add(new Parameter(ParameterType.DateTime, "LastModified", DateTime.Now.ToUniversalTime()));
					parameters.Add(new Parameter(ParameterType.ByteArray, "Data", fileData));
					parameters.Add(new Parameter(ParameterType.String, "Name", _fileName));
				}
				else {
					query = queryBuilder.InsertInto("SearchIndex", new string[] { "Wiki", "Name", "Size", "LastModified", "Data" },
						new string[] { "Wiki", "Name", "Size", "LastModified", "Data" });

					parameters = new List<Parameter>(5);
					parameters.Add(new Parameter(ParameterType.String, "Wiki", _wiki));
					parameters.Add(new Parameter(ParameterType.String, "Name", _fileName));
					parameters.Add(new Parameter(ParameterType.Int64, "Size", (long)originalLength));
					parameters.Add(new Parameter(ParameterType.DateTime, "LastModified", DateTime.Now.ToUniversalTime()));
					parameters.Add(new Parameter(ParameterType.ByteArray, "Data", fileData));
				}

				DbCommand command = builder.GetCommand(transaction, query, parameters);

				int rows = _sqlStorageProviderUtility.ExecuteNonQuery(command, false);
				if(rows == 1) _sqlStorageProviderUtility.CommitTransaction(transaction);
				else _sqlStorageProviderUtility.RollbackTransaction(transaction);
			}
			finally {
				fileStream.Dispose();
			}

			// clean up
			_indexOutput = null;
			GC.SuppressFinalize(this);
		}

		private bool FileExists(DbTransaction transaction, string wiki, string name) {
			ICommandBuilder builder = _sqlStorageProviderUtility.GetCommandBuilder2();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectCountFrom("SearchIndex");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int count = _sqlStorageProviderUtility.ExecuteScalar<int>(command, -1, false);

			return count == 1;
		}

		/// <summary>
		/// Flushes the sql index output.
		/// </summary>
		public override void Flush() {
			_indexOutput.Flush();
		}

		/// <summary>
		/// Gets the file pointer.
		/// </summary>
		/// <returns>A pointer to the file.</returns>
		public override long GetFilePointer() {
			return _indexOutput.GetFilePointer();
		}

		/// <summary>
		/// The length of the index output file.
		/// </summary>
		public override long Length() {
			return _indexOutput.Length();
		}

		/// <summary>
		/// Seeks to the specified position.
		/// </summary>
		/// <param name="pos">The position.</param>
		public override void Seek(long pos) {
			_indexOutput.Seek(pos);
		}

		/// <summary>
		/// Writes the byte.
		/// </summary>
		/// <param name="b">The byte to be written.</param>
		public override void WriteByte(byte b) {
			_indexOutput.WriteByte(b);
		}

		/// <summary>
		/// Writes the given array of bytes.
		/// </summary>
		/// <param name="b">The array of bytes to be written.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="length">The length.</param>
		public override void WriteBytes(byte[] b, int offset, int length) {
			_indexOutput.WriteBytes(b, offset, length);
		}
	}
}
