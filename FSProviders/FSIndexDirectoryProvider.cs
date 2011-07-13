
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using Lucene.Net.Store;
using System.IO;

namespace ScrewTurn.Wiki.Plugins.FSProviders {

	/// <summary>
	/// Implements a file system based indexDirectory provider.
	/// </summary>
	public class FSIndexDirectoryProvider : ProviderBase, IIndexDirectoryProviderV40 {

		private IHostV40 _host;
		private string _wiki;

		private Lucene.Net.Store.Directory _directory;

		private string GetFullPath(string wiki) {
			return Path.Combine(Path.Combine(GetDataDirectory(_host), wiki), "searchIndex");
		}

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
		/// Gets the wiki that has been used to initialize the current instance of the provider.
		/// </summary>
		public string CurrentWiki {
			get { return _wiki; }
		}

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <param name="wiki">The wiki.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void Init(IHostV40 host, string config, string wiki) {
			if(host == null) throw new ArgumentNullException("host");

			_host = host;
			_wiki = string.IsNullOrEmpty(wiki) ? "root" : wiki;

			DirectoryInfo dirInfo = System.IO.Directory.CreateDirectory(GetFullPath(_wiki));

			_directory = FSDirectory.Open(dirInfo);
		}

		/// <summary>
		/// Sets up the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void SetUp(IHostV40 host, string config) {
			// Nothing to do.
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return new ComponentInformation("File System IndexDirectory Provider", "Threeplicate Srl", "", "", ""); }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml {
			get { return ""; }
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose() {
			if(_directory != null) _directory.Close();
		}

		#endregion
	}
}
