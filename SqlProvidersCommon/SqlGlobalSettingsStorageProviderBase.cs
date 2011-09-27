
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// Implements a base class for a SQL global settings storage provider.
	/// </summary>
	public abstract class SqlGlobalSettingsStorageProviderBase : SqlStorageProviderBase, IGlobalSettingsStorageProviderV40 {

		private const int EstimatedLogEntrySize = 100; // bytes
		private const int MaxAssemblySize = 5242880; // 5 MB
		private const int MaxParametersInQuery = 50;

		/// <summary>
		/// Gets the default users storage provider, when no value is stored in the database.
		/// </summary>
		protected abstract string DefaultUsersStorageProvider { get; }

		/// <summary>
		/// Gets the default pages storage provider, when no value is stored in the database.
		/// </summary>
		protected abstract string DefaultPagesStorageProvider { get; }

		/// <summary>
		/// Gets the default files storage provider, when no value is stored in the database.
		/// </summary>
		protected abstract string DefaultFilesStorageProvider { get; }

		#region IGlobalSettingsStorageProvider Members

		/// <summary>
		/// Retrieves the value of a Setting.
		/// </summary>
		/// <param name="name">The name of the Setting.</param>
		/// <returns>The value of the Setting, or null.</returns>
		/// <exception cref="ArgumentNullException">If <b>name</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>name</b> is empty.</exception>
		public string GetSetting(string name) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("GlobalSetting");
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Name", name));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				string result = null;

				if(reader.Read()) {
					result = reader["Value"] as string;
				}

				CloseReader(command, reader);

				// HACK: this allows to correctly initialize a fully SQL-based wiki instance without any user intervention
				if(string.IsNullOrEmpty(result)) {
					if(name == "DefaultUsersProvider") result = DefaultUsersStorageProvider;
					if(name == "DefaultPagesProvider") result = DefaultPagesStorageProvider;
					if(name == "DefaultFilesProvider") result = DefaultFilesStorageProvider;
				}

				return result;
			}
			else return null;
		}

		/// <summary>
		/// Stores the value of a Setting.
		/// </summary>
		/// <param name="name">The name of the Setting.</param>
		/// <param name="value">The value of the Setting. Value cannot contain CR and LF characters, which will be removed.</param>
		/// <returns>True if the Setting is stored, false otherwise.</returns>
		/// <remarks>This method stores the Value immediately.</remarks>
		public bool SetSetting(string name, string value) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			// 1. Delete old value, if any
			// 2. Store new value

			// Nulls are converted to empty strings
			if(value == null) value = "";

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("GlobalSetting");
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Name", name));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows == -1) {
				RollbackTransaction(transaction);
				return false; // Deletion command failed (0-1 are OK)
			}

			query = queryBuilder.InsertInto("GlobalSetting",
				new string[] { "Name", "Value" }, new string[] { "Name", "Value" });
			parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Value", value));

			command = builder.GetCommand(transaction, query, parameters);

			rows = ExecuteNonQuery(command, false);

			if(rows == 1) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return rows == 1;
		}

		/// <summary>
		/// Gets the all the setting values.
		/// </summary>
		/// <returns>All the settings.</returns>
		public IDictionary<string, string> GetAllSettings() {
			ICommandBuilder builder = GetCommandBuilder();

			// Sorting order is not relevant
			string query = QueryBuilder.NewQuery(builder).SelectFrom("GlobalSetting");

			DbCommand command = builder.GetCommand(connString, query, new List<Parameter>());

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				Dictionary<string, string> result = new Dictionary<string, string>(50);

				while(reader.Read()) {
					result.Add(reader["Name"] as string, reader["Value"] as string);
				}

				CloseReader(command, reader);

				return result;
			}
			else return null;
		}

		/// <summary>
		/// Starts a Bulk update of the Settings so that a bulk of settings can be set before storing them.
		/// </summary>
		public void BeginBulkUpdate() {
			// Do nothing - currently not supported
		}

		/// <summary>
		/// Ends a Bulk update of the Settings and stores the settings.
		/// </summary>
		public void EndBulkUpdate() {
			// Do nothing - currently not supported
		}

		/// <summary>
		/// Converts an <see cref="T:EntryType" /> to its character representation.
		/// </summary>
		/// <param name="type">The <see cref="T:EntryType" />.</param>
		/// <returns>Th haracter representation.</returns>
		private static char EntryTypeToChar(EntryType type) {
			switch(type) {
				case EntryType.Error:
					return 'E';
				case EntryType.Warning:
					return 'W';
				case EntryType.General:
					return 'G';
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Converts the character representation of an <see cref="T:EntryType" /> back to the enumeration value.
		/// </summary>
		/// <param name="c">The character representation.</param>
		/// <returns>The<see cref="T:EntryType" />.</returns>
		private static EntryType EntryTypeFromChar(char c) {
			switch(char.ToUpperInvariant(c)) {
				case 'E':
					return EntryType.Error;
				case 'W':
					return EntryType.Warning;
				case 'G':
					return EntryType.General;
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Sanitizes a stiring from all unfriendly characters.
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <returns>The sanitized result.</returns>
		private static string Sanitize(string input) {
			StringBuilder sb = new StringBuilder(input);
			sb.Replace("<", "&lt;");
			sb.Replace(">", "&gt;");
			return sb.ToString();
		}

		/// <summary>
		/// Records a message to the System Log.
		/// </summary>
		/// <param name="message">The Log Message.</param>
		/// <param name="entryType">The Type of the Entry.</param>
		/// <param name="user">The User.</param>
		/// <param name="wiki">The wiki, <c>null</c> if is an application level log.</param>
		/// <remarks>This method <b>should not</b> write messages to the Log using the method IHost.LogEntry.
		/// This method should also never throw exceptions (except for parameter validation).</remarks>
		/// <exception cref="ArgumentNullException">If <b>message</b> or <b>user</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>message</b> or <b>user</b> are empty.</exception>
		public void LogEntry(string message, EntryType entryType, string user, string wiki) {
			if(message == null) throw new ArgumentNullException("message");
			if(message.Length == 0) throw new ArgumentException("Message cannot be empty", "message");
			if(user == null) throw new ArgumentNullException("user");
			if(user.Length == 0) throw new ArgumentException("User cannot be empty", "user");

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.InsertInto("Log",
				new string[] { "DateTime", "EntryType", "User", "Message", "Wiki" }, new string[] { "DateTime", "EntryType", "User", "Message", "Wiki" });

			List<Parameter> parameters = new List<Parameter>(5);
			parameters.Add(new Parameter(ParameterType.DateTime, "DateTime", DateTime.UtcNow));
			parameters.Add(new Parameter(ParameterType.Char, "EntryType", EntryTypeToChar(entryType)));
			parameters.Add(new Parameter(ParameterType.String, "User", Sanitize(user)));
			parameters.Add(new Parameter(ParameterType.String, "Message", Sanitize(message)));
			parameters.Add(new Parameter(ParameterType.String, "Wiki", string.IsNullOrEmpty(wiki) ? "" : Sanitize(wiki)));

			try {
				DbCommand command = builder.GetCommand(connString, query, parameters);

				int rows = ExecuteNonQuery(command, true, false);

				// No transaction - accurate log sizing is not really a concern

				if(rows > -1) {
					int logSize = LogSize;
					if(logSize > int.Parse(host.GetGlobalSettingValue(GlobalSettingName.MaxLogSize))) {
						CutLog((int)(logSize * 0.75));
					}
				}
			}
			catch { }
		}

		/// <summary>
		/// Reduces the size of the Log to the specified size (or less).
		/// </summary>
		/// <param name="size">The size to shrink the log to (in bytes).</param>
		private void CutLog(int size) {
			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectCountFrom("Log");

			DbCommand command = builder.GetCommand(transaction, query, new List<Parameter>());

			int rows = ExecuteScalar<int>(command, -1, false);

			if(rows == -1) {
				RollbackTransaction(transaction);
				return;
			}

			int estimatedSize = rows * EstimatedLogEntrySize;

			if(size < estimatedSize) {

				int difference = estimatedSize - size;
				int entriesToDelete = difference / EstimatedLogEntrySize;
				// Add 10% to avoid 1-by-1 deletion when adding new entries
				entriesToDelete += entriesToDelete / 10;

				if(entriesToDelete > 0) {
					// This code is not optimized, but it surely works in most DBMS
					query = queryBuilder.SelectFrom("Log", new string[] { "Id" });
					query = queryBuilder.OrderBy(query, new string[] { "Id" }, new Ordering[] { Ordering.Asc });

					command = builder.GetCommand(transaction, query, new List<Parameter>());

					DbDataReader reader = ExecuteReader(command);

					List<int> ids = new List<int>(entriesToDelete);

					if(reader != null) {
						while(reader.Read() && ids.Count < entriesToDelete) {
							ids.Add((int)reader["Id"]);
						}

						CloseReader(reader);
					}

					if(ids.Count > 0) {
						// Given that the IDs to delete can be many, the query is split in many chunks, each one deleting 50 items
						// This works-around the problem of too many parameters in a RPC call of SQL Server
						// See also CutRecentChangesIfNecessary

						for(int chunk = 0; chunk <= ids.Count / MaxParametersInQuery; chunk++) {
							query = queryBuilder.DeleteFrom("Log");
							List<string> parms = new List<string>(MaxParametersInQuery);
							List<Parameter> parameters = new List<Parameter>(MaxParametersInQuery);

							for(int i = chunk * MaxParametersInQuery; i < Math.Min(ids.Count, (chunk + 1) * MaxParametersInQuery); i++) {
								parms.Add("P" + i.ToString());
								parameters.Add(new Parameter(ParameterType.Int32, parms[parms.Count - 1], ids[i]));
							}

							query = queryBuilder.WhereIn(query, "Id", parms.ToArray());

							command = builder.GetCommand(transaction, query, parameters);

							if(ExecuteNonQuery(command, false) < 0) {
								RollbackTransaction(transaction);
								return;
							}
						}
					}

					CommitTransaction(transaction);
				}
			}
		}

		/// <summary>
		/// Gets all the Log Entries, sorted by date/time (oldest to newest).
		/// </summary>
		/// <returns>The Log Entries.</returns>
		public LogEntry[] GetLogEntries() {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("Log", new string[] { "DateTime", "EntryType", "User", "Message", "Wiki" });
			query = queryBuilder.OrderBy(query, new string[] { "DateTime" }, new Ordering[] { Ordering.Asc });

			DbCommand command = builder.GetCommand(connString, query, new List<Parameter>());

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<LogEntry> result = new List<LogEntry>(100);

				while(reader.Read()) {
					result.Add(new LogEntry(EntryTypeFromChar((reader["EntryType"] as string)[0]),
						new DateTime(((DateTime)reader["DateTime"]).Ticks, DateTimeKind.Utc), reader["Message"] as string, reader["User"] as string, reader["Wiki"] as string));
				}

				CloseReader(command, reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Clear the Log.
		/// </summary>
		public void ClearLog() {
			ICommandBuilder builder = GetCommandBuilder();

			string query = QueryBuilder.NewQuery(builder).DeleteFrom("Log");

			DbCommand command = builder.GetCommand(connString, query, new List<Parameter>());

			ExecuteNonQuery(command);
		}

		/// <summary>
		/// Gets the current size of the Log, in KB.
		/// </summary>
		public int LogSize {
			get {
				ICommandBuilder builder = GetCommandBuilder();
				QueryBuilder queryBuilder = new QueryBuilder(builder);

				string query = queryBuilder.SelectCountFrom("Log");

				DbCommand command = builder.GetCommand(connString, query, new List<Parameter>());

				int rows = ExecuteScalar<int>(command, -1);

				if(rows == -1) return 0;

				int estimatedSize = rows * EstimatedLogEntrySize;

				return estimatedSize / 1024;
			}
		}

		/// <summary>
		/// Lists the stored plugin assemblies.
		/// </summary>
		public string[] ListPluginAssemblies() {
			ICommandBuilder builder = GetCommandBuilder();

			// Sort order is not relevant
			string query = QueryBuilder.NewQuery(builder).SelectFrom("PluginAssembly", new string[] { "Name" });

			DbCommand command = builder.GetCommand(connString, query, new List<Parameter>());

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<string> result = new List<string>(10);

				while(reader.Read()) {
					result.Add(reader["Name"] as string);
				}

				CloseReader(command, reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Stores a plugin's assembly, overwriting existing ones if present.
		/// </summary>
		/// <param name="filename">The file name of the assembly, such as "Assembly.dll".</param>
		/// <param name="assembly">The assembly content.</param>
		/// <returns><c>true</c> if the assembly is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>filename</b> or <b>assembly</b> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>filename</b> or <b>assembly</b> are empty.</exception>
		public bool StorePluginAssembly(string filename, byte[] assembly) {
			if(filename == null) throw new ArgumentNullException("filename");
			if(filename.Length == 0) throw new ArgumentException("Filename cannot be empty", "filename");
			if(assembly == null) throw new ArgumentNullException("assembly");
			if(assembly.Length == 0) throw new ArgumentException("Assembly cannot be empty", "assembly");
			if(assembly.Length > MaxAssemblySize) throw new ArgumentException("Assembly is too big", "assembly");

			// 1. Delete old plugin assembly, if any
			// 2. Store new assembly

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			DeletePluginAssembly(transaction, filename);

			string query = QueryBuilder.NewQuery(builder).InsertInto("PluginAssembly", new string[] { "Name", "Assembly" }, new string[] { "Name", "Assembly" });

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Name", filename));
			parameters.Add(new Parameter(ParameterType.ByteArray, "Assembly", assembly));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows == 1) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return rows == 1;
		}

		/// <summary>
		/// Retrieves a plugin's assembly.
		/// </summary>
		/// <param name="filename">The file name of the assembly.</param>
		/// <returns>The assembly content, or <c>null</c>.</returns>
		/// <exception cref="ArgumentNullException">If <b>filename</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>filename</b> is empty.</exception>
		public byte[] RetrievePluginAssembly(string filename) {
			if(filename == null) throw new ArgumentNullException("filename");
			if(filename.Length == 0) throw new ArgumentException("Filename cannot be empty", "filename");

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("PluginAssembly", new string[] { "Assembly" });
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Name", filename));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				byte[] result = null;

				if(reader.Read()) {
					result = GetBinaryColumn(reader, "Assembly", MaxAssemblySize);
				}

				CloseReader(command, reader);

				return result;
			}
			else return null;
		}

		/// <summary>
		/// Removes a plugin's assembly.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="filename">The file name of the assembly to remove, such as "Assembly.dll".</param>
		/// <returns><c>true</c> if the assembly is removed, <c>false</c> otherwise.</returns>
		private bool DeletePluginAssembly(DbTransaction transaction, string filename) {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("PluginAssembly");
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Name", filename));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			return rows == 1;
		}

		/// <summary>
		/// Removes a plugin's assembly.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="filename">The file name of the assembly to remove, such as "Assembly.dll".</param>
		/// <returns><c>true</c> if the assembly is removed, <c>false</c> otherwise.</returns>
		private bool DeletePluginAssembly(DbConnection connection, string filename) {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("PluginAssembly");
			query = queryBuilder.Where(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(1);
			parameters.Add(new Parameter(ParameterType.String, "Name", filename));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			return rows == 1;
		}

		/// <summary>
		/// Removes a plugin's assembly.
		/// </summary>
		/// <param name="filename">The file name of the assembly to remove, such as "Assembly.dll".</param>
		/// <returns><c>true</c> if the assembly is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <b>filename</b> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <b>filename</b> is empty.</exception>
		public bool DeletePluginAssembly(string filename) {
			if(filename == null) throw new ArgumentNullException(filename);
			if(filename.Length == 0) throw new ArgumentException("Filename cannot be empty", "filename");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			bool deleted = DeletePluginAssembly(connection, filename);
			CloseConnection(connection);

			return deleted;
		}

		private IList<PluginFramework.Wiki> GetWikiList() {
			return (List<PluginFramework.Wiki>)System.Web.Configuration.WebConfigurationManager.GetWebApplicationSection("wikiList");
		}

		/// <summary>
		/// Alls the wikis.
		/// </summary>
		/// <returns>A list of wiki identifiers.</returns>
		public PluginFramework.Wiki[] GetAllWikis() {
			return GetWikiList().ToArray();
		}

		/// <summary>
		/// Extracts the name of the wiki from the given host.
		/// </summary>
		/// <param name="host">The host.</param>
		/// <returns>The name of the wiki.</returns>
		/// <exception cref="WikiNotFoundException">If no wiki is found corresponding to the given host.</exception>
		public string ExtractWikiName(string host) {
			foreach(PluginFramework.Wiki wiki in GetWikiList()) {
				if(wiki.Hosts.Contains(host)) return wiki.WikiName;
			}
			throw new WikiNotFoundException("The given host: " + host + " does not correspond to any wiki.");
		}

		#endregion

	}

}
