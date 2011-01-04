
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements a base class for local file-based data providers.
	/// </summary>
	public abstract class ProviderBase {

		private object _syncLock = new object();

		private bool _dataDirectoryAlreadyRead = false;
		private string _dataDirectory = null;

		/// <summary>
		/// Sets the data directory.
		/// </summary>
		/// <param name="dataDirectory">The data directory.</param>
		protected void SetDataDirectory(string dataDirectory) {
			lock(_syncLock) {
				if(_dataDirectoryAlreadyRead) throw new InvalidOperationException("Cannot set data directory when it's already been read");
				_dataDirectory = dataDirectory;
			}
		}

		/// <summary>
		/// Gets the data directory.
		/// </summary>
		/// <param name="host">The host object.</param>
		/// <returns>The data directory.</returns>
		protected string GetDataDirectory(IHostV30 host) {
			lock(_syncLock) {
				_dataDirectoryAlreadyRead = true;
				if(string.IsNullOrEmpty(_dataDirectory)) return host.GetSettingValue(SettingName.PublicDirectory);
				else return _dataDirectory;
			}
		}

	}

}
