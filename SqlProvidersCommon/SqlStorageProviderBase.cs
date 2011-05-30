
using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// Implements a base class for a SQL storage provider.
	/// </summary>
	public abstract class SqlStorageProviderBase : SqlClassBase {

		/// <summary>
		/// The connection string.
		/// </summary>
		protected string connString;

		/// <summary>
		/// The host.
		/// </summary>
		protected IHostV30 host;

		/// <summary>
		/// The wiki.
		/// </summary>
		protected string wiki;

		/// <summary>
		/// Gets a new command builder object.
		/// </summary>
		/// <returns>The command builder.</returns>
		protected abstract ICommandBuilder GetCommandBuilder();

		/// <summary>
		/// Logs an exception.
		/// </summary>
		/// <param name="ex">The exception.</param>
		protected override void LogException(Exception ex) {
			try {
				host.LogEntry(ex.ToString(), LogEntryType.Error, null, this);
			}
			catch { }
		}

		#region IProvider Members

		/// <summary>
		/// Validates a connection string.
		/// </summary>
		/// <param name="connString">The connection string to validate.</param>
		/// <remarks>If the connection string is invalid, the method throws <see cref="T:InvalidConfigurationException" />.</remarks>
		protected abstract void ValidateConnectionString(string connString);

		/// <summary>
		/// Creates or updates the database schema if necessary.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		protected abstract void CreateOrUpdateDatabaseIfNecessary(string wiki);

		/// <summary>
		/// Tries to load the configuration from a corresponding v2 provider.
		/// </summary>
		/// <returns>The configuration, or an empty string.</returns>
		protected abstract string TryLoadV2Configuration();

		/// <summary>
		/// Tries to load the configuration of the corresponding settings storage provider.
		/// </summary>
		/// <returns>The configuration, or an empty string.</returns>
		protected abstract string TryLoadSettingsStorageProviderConfiguration();

		/// <summary>
		/// Gets the wiki that has been used to initialize the current instance of the provider.
		/// </summary>
		public string CurrentWiki {
			get { return wiki; }
		}

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <param name="wiki">The wiki.</param>
		/// <exception cref="ArgumentNullException">If <b>host</b> or <b>config</b> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <b>config</b> is not valid or is incorrect.</exception>
		public void Init(IHostV30 host, string config, string wiki) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			this.host = host;
			this.wiki = string.IsNullOrEmpty(wiki) ? "root" : wiki;

			if(config.Length == 0) {
				// Try to load v2 provider configuration
				config = TryLoadV2Configuration();
			}
			
			if(config == null || config.Length == 0) {
				// Try to load Settings Storage Provider configuration
				config = TryLoadSettingsStorageProviderConfiguration();
			}

			if(config == null) config = "";

			ValidateConnectionString(config);

			connString = config;

			CreateOrUpdateDatabaseIfNecessary(wiki);
		}

		/// <summary>
		/// Sets up the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <b>host</b> or <b>config</b> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <b>config</b> is not valid or is incorrect.</exception>
		public void SetUp(IHostV30 host, string config) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");
			
		}

		/// <summary>
		/// Releases resources
		/// </summary>
		public void Dispose() { }

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public abstract ComponentInformation Information {
			get;
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public abstract string ConfigHelpHtml {
			get;
		}

		#endregion

	}

}
