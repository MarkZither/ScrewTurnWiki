
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using Microsoft.WindowsAzure.StorageClient;
using Ionic.Zip;
using System.IO;

namespace ScrewTurn.Wiki.Plugins.AzureStorage {

	/// <summary>
	/// Implements the methods to view, add or delete Themes
	/// </summary>
	public class AzureThemeStorageProvider :IThemeStorageProviderV30 {

		private IHostV30 _host;
		private string _wiki;
		private CloudBlobClient _client;

		#region IThemeStorageProviderV30 Members

		private List<string> ListFilesForInternalUse(string directory, bool flatBlob) {
			try {
				var containerRef = _client.GetContainerReference(_wiki + "-themes");
				IEnumerable<IListBlobItem> blobItems = null;
				BlobRequestOptions options = new BlobRequestOptions();
				options.UseFlatBlobListing = flatBlob;

				if(directory == "") {
					blobItems = containerRef.ListBlobs(options);
				}
				else {
					var directoryRef = containerRef.GetDirectoryReference(directory);
					blobItems = directoryRef.ListBlobs(options);
				}
				
				List<string> files = new List<string>();
				foreach(IListBlobItem blobItem in blobItems) {
					string blobName = blobItem.Uri.AbsoluteUri;
					files.Add(blobName);
				}
				return files;
			}
			catch(StorageClientException ex) {
				if(ex.ErrorCode == StorageErrorCode.BlobNotFound) throw new ArgumentException("Directory does not exist", "directory");
				throw ex;
			}
		}

		/// <summary>
		/// Retrives the lists of avaiable themes.
		/// </summary>
		/// <returns>A list of theme names.</returns>
		public List<string> ListThemes() {
			try {
				return ListFilesForInternalUse("", false);
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Retrieves all files present in the selected theme.
		/// </summary>
		/// <param name="themeName">The name of the selected theme.</param>
		/// <param name="searchPattern">The search string to match against the name of files.</param>
		/// <returns>The list of files matching the searchPattern.</returns>
		public List<string> ListThemeFiles(string themeName, string searchPattern) {
			try {
				List<string> files = ListFilesForInternalUse(themeName, true);

				List<string> matchingFiles = new List<string>();
				foreach(string file in files) {
					if(file.Contains(searchPattern)) matchingFiles.Add(file);
				}
				return matchingFiles;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Stores the theme.
		/// </summary>
		/// <param name="themeName">The name of the theme.</param>
		/// <param name="zipFile">The zipFile conteining the theme.</param>
		/// <returns><c>true</c> if the theme is saved, <c>false</c> otherwise.</returns>
		public bool StoreTheme(string themeName, byte[] zipFile) {
			if(themeName == null) throw new ArgumentNullException("themeName");
			if(themeName.Length == 0) throw new ArgumentException("themeName cannot be empty", "themeName");
			if(zipFile == null) throw new ArgumentNullException("zipFile");
			if(zipFile.Length == 0) throw new ArgumentException("zipFile cannot be empty", "zipFile");

			var containerRef = _client.GetContainerReference(_wiki + "-themes");
			var directoryRef = containerRef.GetDirectoryReference(themeName);

			try {
				using(ZipFile zip = ZipFile.Read(zipFile)) {
					foreach(ZipEntry e in zip) {
						using(MemoryStream stream = new MemoryStream()) {
							e.Extract(stream);
							stream.Seek(0, SeekOrigin.Begin);
							var blob = directoryRef.GetBlobReference(e.FileName);
							BlobRequestOptions options = new BlobRequestOptions();
							MimeTypes.Init();
							string extension = Path.GetExtension(e.FileName).Trim('.');
							if(MimeTypes.Types.ContainsKey(extension)) {
								blob.Properties.ContentType = MimeTypes.Types[extension];
							}
							blob.UploadFromStream(stream);
						}
					}
				}
			}
			catch(IOException) {
				return false;
			}
			return true;
		}

		/// <summary>
		/// Deletes the theme with the given name.
		/// </summary>
		/// <param name="themeName">The name of the theme to be deleted.</param>
		/// <returns><c>true</c> if the theme is removed, <c>false</c> otherwise.</returns>
		public bool DeleteTheme(string themeName) {
			var containerRef = _client.GetContainerReference(_wiki + "-themes");
			var directoryRef = containerRef.GetDirectoryReference(themeName);
			BlobRequestOptions options = new BlobRequestOptions();
			options.UseFlatBlobListing = true;
			var blobs = directoryRef.ListBlobs(options);
			foreach(var blob in blobs) {
				_client.GetBlobReference(blob.Uri.AbsoluteUri).Delete();
			}
			return true;
		}

		/// <summary>
		/// Gets the relative path of the theme with the given name.
		/// </summary>
		/// <param name="themeName">The name of the theme.</param>
		/// <returns>The relative path of the theme.</returns>
		public string GetThemePath(string themeName) {
			var containerRef = _client.GetContainerReference(_wiki + "-themes");
			var directoryRef = containerRef.GetDirectoryReference(themeName);
			return directoryRef.Uri.AbsoluteUri;
		}

		#endregion

		#region IProviderV30 Members

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <param name="wiki">The wiki.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void Init(IHostV30 host, string config, string wiki) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			_host = host;
			_wiki = string.IsNullOrEmpty(wiki) ? "root" : wiki.ToLowerInvariant();

			string[] connectionStrings = config.Split(new char[] { '|' });
			if(connectionStrings == null || connectionStrings.Length != 2) throw new InvalidConfigurationException("The given connections string is invalid.");

			_client = TableStorage.StorageAccount(connectionStrings[0], connectionStrings[1]).CreateCloudBlobClient();

			CloudBlobContainer containerRef = _client.GetContainerReference(_wiki + "-themes");
			BlobContainerPermissions permissions = new BlobContainerPermissions();
			permissions.PublicAccess = BlobContainerPublicAccessType.Blob;
			containerRef.CreateIfNotExist();
			containerRef.SetPermissions(permissions);
		}

		/// <summary>
		/// Sets up the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void SetUp(IHostV30 host, string config) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			// Nothing todo
		}

		public ComponentInformation Information {
			get { return new ComponentInformation("Azure Blob Storage Theme Storage Provider", "Threeplicate Srl", _host.GetGlobalSettingValue(GlobalSettingName.WikiVersion), "", ""); }
		}

		public string ConfigHelpHtml {
			get { return ""; }
		}

		#endregion

		#region IDisposable Members

		public void Dispose() {
			// Nothing todo
		}

		#endregion
	}
}
