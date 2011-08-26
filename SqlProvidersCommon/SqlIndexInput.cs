
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;
using System.Data.Common;
using System.IO;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// Implement a Lucene.NET IndexInput for SqlServer.
	/// </summary>
	public class SqlIndexInput : IndexInput {

		private SqlServerDirectory _sqlServerDirectory;
		private ISqlStorageProviderUtility _sqlStorageProviderUtility;

		private IndexInput _indexInput;
		private Lucene.Net.Store.Directory CacheDirectory { get { return _sqlServerDirectory.CacheDirectory; } }

		/// <summary>
		/// Initializes a new instance of the <see cref="SqlIndexInput"/> class.
		/// </summary>
		/// <param name="sqlServerDirectory">The Sql Server Directory object.</param>
		/// <param name="sqlStorageProviderUtility">The SQL storage provider utility.</param>
		/// <param name="connString">The connection string.</param>
		/// <param name="wiki">The wiki.</param>
		/// <param name="name">The name of the file.</param>
		public SqlIndexInput(SqlServerDirectory sqlServerDirectory, ISqlStorageProviderUtility sqlStorageProviderUtility, string connString, string wiki, string name) {
			_sqlServerDirectory = sqlServerDirectory;
			_sqlStorageProviderUtility = sqlStorageProviderUtility;

			bool fFileNeeded = false;
			if(!CacheDirectory.FileExists(name)) {
				fFileNeeded = true;
			}

			if(fFileNeeded) {
				StreamOutput fileStream = _sqlServerDirectory.CreateCachedOutputAsStream(name);

				ICommandBuilder builder = _sqlStorageProviderUtility.GetCommandBuilder2();
				DbConnection connection = builder.GetConnection(connString);
				DbTransaction transaction = _sqlStorageProviderUtility.BeginTransaction(connection);

				if(!FileExists(transaction, wiki, name)) {
					_sqlStorageProviderUtility.RollbackTransaction(transaction);
					_sqlStorageProviderUtility.CloseConnection(connection);
					throw new ArgumentException("File does not exist", "fullName");
				}

				QueryBuilder queryBuilder = new QueryBuilder(builder);

				string query = queryBuilder.SelectFrom("SearchIndex", new string[] { "Size", "Data" });
				query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
				query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");

				List<Parameter> parameters = new List<Parameter>(2);
				parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
				parameters.Add(new Parameter(ParameterType.String, "Name", name));

				DbCommand command = builder.GetCommand(transaction, query, parameters);

				DbDataReader reader = _sqlStorageProviderUtility.ExecuteReader(command);

				if(reader != null) {
					bool done = false;

					if(reader.Read()) {
						int read = _sqlStorageProviderUtility.ReadBinaryColumn(reader, "Data", fileStream);
						done = (long)read == (long)reader["Size"];
					}

					_sqlStorageProviderUtility.CloseReader(reader);

					if(!done) {
						_sqlStorageProviderUtility.RollbackTransaction(transaction);
					}
				}
				else {
					_sqlStorageProviderUtility.RollbackTransaction(transaction);
				}

				_sqlStorageProviderUtility.CommitTransaction(transaction);

				fileStream.Flush();
				fileStream.Close();

				// and open it as an input 
				_indexInput = CacheDirectory.OpenInput(name);
			}
			else {
				_indexInput = CacheDirectory.OpenInput(name);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SqlIndexInput"/> class.
		/// </summary>
		/// <param name="cloneInput">The clone input.</param>
		public SqlIndexInput(SqlIndexInput cloneInput)
		{
			try	{
				_sqlServerDirectory = cloneInput._sqlServerDirectory;
				_sqlStorageProviderUtility = cloneInput._sqlStorageProviderUtility;
				_indexInput = cloneInput._indexInput.Clone() as IndexInput;
			}
			catch (Exception)
			{
				// sometimes we get access denied on the 2nd stream...but not always. I haven't tracked it down yet
				// but this covers our tail until I do
			}
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
		/// Closes the file.
		/// </summary>
		public override void Close() {
			_indexInput.Close();
			_indexInput = null;
			_sqlServerDirectory = null;
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Gets the file pointer.
		/// </summary>
		/// <returns>the file pointer.</returns>
		public override long GetFilePointer() {
			return _indexInput.GetFilePointer();
		}

		/// <summary>
		/// The Length of the file.
		/// </summary>
		/// <returns>The length of the file.</returns>
		public override long Length() {
			return _indexInput.Length();
		}

		/// <summary>
		/// Reads the byte.
		/// </summary>
		/// <returns>The read byte.</returns>
		public override byte ReadByte() {
			return _indexInput.ReadByte();
			
		}

		/// <summary>
		/// Reads an array of bytes.
		/// </summary>
		/// <param name="b">The buffer array.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="len">The number of bytes to read.</param>
		public override void ReadBytes(byte[] b, int offset, int len) {
			_indexInput.ReadBytes(b, offset, len);
		}

		/// <summary>
		/// Set the position within the current stream.
		/// </summary>
		/// <param name="pos">The posision.</param>
		public override void Seek(long pos) {
			_indexInput.Seek(pos);
		}

		/// <summary>
		/// Clones the inden input.
		/// </summary>
		/// <returns></returns>
		public override System.Object Clone() {
			IndexInput clone = null;
			try {
				SqlIndexInput input = new SqlIndexInput(this);
				clone = (IndexInput)input;
			}
			catch(System.Exception) {
			}
			return clone;
		}

	}
}
