
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.Plugins.SqlCommon;
using System.Data.SqlClient;

namespace ScrewTurn.Wiki.Plugins.SqlServer {

	/// <summary>
	/// Implements a sql server based indexDirectory provider.
	/// </summary>
	public class SqlServerIndexDirectoryProvider : SqlStorageProviderBase, IIndexDirectoryProviderV40 {

		private Lucene.Net.Store.Directory _directory;

		ICommandBuilder commandBuilder = new SqlServerCommandBuilder();

		private const int CurrentSchemaVersion = 4000;

		#region IIndexDirectoryProviderV40 Members

		/// <summary>
		/// Gets the directory object to be used by the Search engine.
		/// </summary>
		/// <returns>The directory object.</returns>
		public Lucene.Net.Store.Directory GetDirectory() {
			return _directory;
		}

		#endregion

		#region IProviderV40 Members

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <param name="wiki">The wiki.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public new void Init(IHostV40 host, string config, string wiki) {
			base.Init(host, config, wiki);

			_directory = new SqlServerDirectory(new SqlServerStorageProviderUtility(), config, "searchindex", wiki);
		}

		/// <summary>
		/// Sets up the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public new void SetUp(IHostV40 host, string config) {
			base.SetUp(host, config);
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public override ComponentInformation Information {
			get { return new ComponentInformation("Sql Server IndexDirectory Provider", "Threeplicate Srl", "", "", ""); }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public override string ConfigHelpHtml {
			get { return ""; }
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public new void Dispose() {
			if(_directory != null) _directory.Close();
		}

		#endregion

		/// <summary>
		/// Gets a new command builder object.
		/// </summary>
		/// <returns>The command builder.</returns>
		protected override ICommandBuilder GetCommandBuilder() {
			return commandBuilder;
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
			cmd.CommandText = "select [Version] from [Version] where [Component] = 'SearchIndex'";

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
			cmd.CommandText = "select [Version] from [Version] where [Component] = 'SearchIndex'";

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
			cmd.CommandText = Properties.Resources.SearchIndexDatabase;

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

	}
}
