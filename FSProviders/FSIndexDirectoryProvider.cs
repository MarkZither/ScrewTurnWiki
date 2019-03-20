using System;
using System.IO;
using Lucene.Net.Store;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Plugins.FSProviders
{
	public class FSIndexDirectoryProvider : ProviderBase, IIndexDirectoryProviderV30
	{
		private IHostV30 _host;

		private Lucene.Net.Store.Directory _directory;

		private string GetFullPath(string wiki)
		{
			return Path.Combine(GetDataDirectory(_host), "SearchIndex");
		}

		#region IIndexDirectoryProviderV30 Members

		/// <summary>
		/// Gets the directory object to be used by the Search engine.
		/// </summary>
		/// <returns>The directory object.</returns>
		public Lucene.Net.Store.Directory GetDirectory()
		{
			return _directory;
		}

		#endregion

		#region IProviderV30 Members

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void Init(IHostV30 host, string config)
		{
			if(host == null)
			{
				throw new ArgumentNullException("host");
			}

			_host = host;

			DirectoryInfo dirInfo = System.IO.Directory.CreateDirectory(GetFullPath("Index"));

			_directory = FSDirectory.Open(dirInfo);
		}

		/// <summary>
		/// Sets up the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void SetUp(IHostV30 host, string config)
		{
			// Nothing to do.
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information
		{
			get
			{
				return new ComponentInformation("File System IndexDirectory Provider", "Threeplicate Srl", "4.0.5.143", "http://www.screwturn.eu", null);
			}
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml
		{
			get
			{
				return "";
			}
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			if(_directory != null)
			{
				_directory.Dispose();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void Shutdown()
		{
			// Nothing to do.
		}

		#endregion
	}
}
