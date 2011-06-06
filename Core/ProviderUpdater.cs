
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using System.Net;
using System.IO;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Handles the updating of providers.
	/// </summary>
	public class ProviderUpdater {

		private List<string> visitedUrls;

		private IGlobalSettingsStorageProviderV30 globalSettingsProvider;
		private List<IProviderV30> providers;
		private Dictionary<string, string> fileNamesForProviders;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:ProviderUpdater" /> class.
		/// </summary>
		/// <param name="globalSettingsProvider">The settings storage provider.</param>
		/// <param name="fileNamesForProviders">A provider->file dictionary.</param>
		/// <param name="providers">The providers to update.</param>
		public ProviderUpdater(IGlobalSettingsStorageProviderV30 globalSettingsProvider,
			Dictionary<string, string> fileNamesForProviders,
			params IProviderV30[][] providers) {

			if(globalSettingsProvider == null) throw new ArgumentNullException("settingsProvider");
			if(fileNamesForProviders == null) throw new ArgumentNullException("fileNamesForProviders");
			if(providers == null) throw new ArgumentNullException("providers");
			if(providers.Length == 0) throw new ArgumentException("Providers cannot be empty", "providers");

			this.globalSettingsProvider = globalSettingsProvider;
			this.fileNamesForProviders = fileNamesForProviders;

			this.providers = new List<IProviderV30>(20);
			foreach(IProviderV30[] group in providers) {
				this.providers.AddRange(group);
			}

			visitedUrls = new List<string>(10);
		}

		/// <summary>
		/// Updates all the providers.
		/// </summary>
		/// <returns>The number of updated DLLs.</returns>
		public int UpdateAll() {
			Log.LogEntry("Starting automatic providers update", EntryType.General, Log.SystemUsername, null);

			int updatedDlls = 0;

			foreach(IProviderV30 prov in providers) {
				if(string.IsNullOrEmpty(prov.Information.UpdateUrl)) continue;

				string newVersion;
				string newDllUrl;
				UpdateStatus status = Tools.GetUpdateStatus(prov.Information.UpdateUrl,
					prov.Information.Version, out newVersion, out newDllUrl);

				if(status == UpdateStatus.NewVersionFound && !string.IsNullOrEmpty(newDllUrl)) {
					// Update is possible

					// Case insensitive check
					if(!visitedUrls.Contains(newDllUrl.ToLowerInvariant())) {
						string dllName = null;

						if(!fileNamesForProviders.TryGetValue(prov.GetType().FullName, out dllName)) {
							Log.LogEntry("Could not determine DLL name for provider " + prov.GetType().FullName, EntryType.Error, Log.SystemUsername, null);
							continue;
						}

						// Download DLL and install
						if(DownloadAndUpdateDll(prov, newDllUrl, dllName)) {
							visitedUrls.Add(newDllUrl.ToLowerInvariant());
							updatedDlls++;
						}
					}
					else {
						// Skip DLL (already updated)
						Log.LogEntry("Skipping provider " + prov.GetType().FullName + ": DLL already updated", EntryType.General, Log.SystemUsername, null);
					}
				}
			}

			Log.LogEntry("Automatic providers update completed: updated " + updatedDlls.ToString() + " DLLs", EntryType.General, Log.SystemUsername, null);

			return updatedDlls;
		}

		/// <summary>
		/// Downloads and updates a DLL.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="url">The URL of the new DLL.</param>
		/// <param name="filename">The file name of the DLL.</param>
		private bool DownloadAndUpdateDll(IProviderV30 provider, string url, string filename) {
			try {
				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();

				if(response.StatusCode != HttpStatusCode.OK) {
					Log.LogEntry("Update failed for provider " + provider.GetType().FullName + ": Status Code=" + response.StatusCode.ToString(), EntryType.Error, Log.SystemUsername, null);
					response.Close();
					return false;
				}

				BinaryReader reader = new BinaryReader(response.GetResponseStream());
				byte[] content = reader.ReadBytes((int)response.ContentLength);
				reader.Close();

				bool done = globalSettingsProvider.StorePluginAssembly(filename, content);
				if(done) Log.LogEntry("Provider " + provider.GetType().FullName + " updated", EntryType.General, Log.SystemUsername, null);
				else Log.LogEntry("Update failed for provider " + provider.GetType().FullName + ": could not store assembly", EntryType.Error, Log.SystemUsername, null);

				return done;
			}
			catch(Exception ex) {
				Log.LogEntry("Update failed for provider " + provider.GetType().FullName + ": " + ex.ToString(), EntryType.Error, Log.SystemUsername, null);
				return false;
			}
		}

	}

}
