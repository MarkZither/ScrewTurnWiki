
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using Microsoft.WindowsAzure.StorageClient;
using System.IO;

namespace ScrewTurn.Wiki.Plugins.AzureStorage {

	/// <summary>
	/// Implements a Files Storage Provider based on Azure Storage.
	/// </summary>
	public class AzureFilesStorageProvider : IFilesStorageProviderV40 {

		private IHostV40 _host;
		private string _wiki;
		private CloudBlobClient _client;

		#region IFilesStorageProviderV40 Members

		private string BuildNameForBlobStorage(string name) {
			return !string.IsNullOrEmpty(name) ? name.Trim('/') : "";
		}

		/// <summary>
		/// Check if a blob with the given full name (ignoring case) exists.
		/// </summary>
		/// <param name="containerName">The blob container where to search.</param>
		/// <param name="blobName">Full name of the blob.</param>
		/// <returns>The name of the blob with case as saved in blob storage.</returns>
		private string BlobExists(string containerName, string blobName) {
			// If is the roor return true
			if(blobName == "") return "";
			try {
				var containerRef = _client.GetContainerReference(containerName);
				var blobRef = containerRef.GetBlobReference(blobName);
				blobRef.FetchAttributes();
				blobName = blobRef.Uri.PathAndQuery;
				return blobName.Substring(blobName.IndexOf(blobRef.Container.Name) + blobRef.Container.Name.Length + 1);
			}
			catch(StorageClientException e) {
				if(e.ErrorCode == StorageErrorCode.ResourceNotFound) {
					string[] allBlobs = ListFilesForInternalUse(containerName, "", false, true);
					return allBlobs.FirstOrDefault(b => b.ToLowerInvariant().TrimStart('/') == blobName.ToLowerInvariant());
				}
				else {
					throw;
				}
			}
		}

		private string[] ListFilesForInternalUse(string containerName, string directory, bool forCopy, bool flatBlob) {
			try {
				var containerRef = _client.GetContainerReference(containerName);
				IEnumerable<IListBlobItem> blobItems = null;
				BlobRequestOptions options = new BlobRequestOptions();
				options.UseFlatBlobListing = flatBlob;

				if(BuildNameForBlobStorage(directory) != "") {
					if(BlobExists(containerName, BuildNameForBlobStorage(directory)) == null) throw new ArgumentException("Directory does not exist", "directory");
					var directoryRef = containerRef.GetDirectoryReference(BuildNameForBlobStorage(directory));
					blobItems = directoryRef.ListBlobs(options);
				}
				else {
					blobItems = containerRef.ListBlobs(options);
				}
				List<string> files = new List<string>();
				foreach(IListBlobItem blobItem in blobItems) {
					string blobName = blobItem.Uri.PathAndQuery;
					blobName = blobName.Substring(blobName.IndexOf(blobItem.Container.Name) + blobItem.Container.Name.Length);
					if(!forCopy) blobName = blobName.EndsWith("/.stw.dat") ? blobName.Remove(blobName.IndexOf("/.stw.dat")) : blobName;
					files.Add(blobName.TrimStart('/'));
				}
				return files.ToArray();
			}
			catch(StorageClientException ex) {
				if(ex.ErrorCode == StorageErrorCode.BlobNotFound) throw new ArgumentException("Directory does not exist", "directory");
				throw ex;
			}
		}

		/// <summary>
		/// Lists the Files in the specified Directory.
		/// </summary>
		/// <param name="directory">The full directory name, for example "/my/directory". Null, empty or "/" for the root directory.</param>
		/// <returns>The list of Files in the directory.</returns>
		/// <exception cref="ArgumentException">If <paramref name="directory"/> does not exist.</exception>
		public string[] ListFiles(string directory) {
			try {
				var containerRef = _client.GetContainerReference(_wiki);
				IEnumerable<IListBlobItem> blobItems = null;
				BlobRequestOptions options = new BlobRequestOptions();
				options.UseFlatBlobListing = false;

				if(BuildNameForBlobStorage(directory) != "") {
					if(BlobExists(_wiki, BuildNameForBlobStorage(directory)) == null) throw new ArgumentException("Directory does not exist", "directory");
					var directoryRef = containerRef.GetDirectoryReference(BuildNameForBlobStorage(directory));
					blobItems = directoryRef.ListBlobs(options);
				}
				else {
					blobItems = containerRef.ListBlobs(options);
				}
				List<string> files = new List<string>();
				foreach(IListBlobItem blobItem in blobItems) {
					string blobName = blobItem.Uri.PathAndQuery;
					blobName = blobName.Substring(blobName.IndexOf(blobItem.Container.Name) + blobItem.Container.Name.Length);
					blobName = blobName.EndsWith(".stw.dat") ? blobName.Remove(blobName.IndexOf(".stw.dat")) : blobName;
					if(!blobName.EndsWith("/")) files.Add(blobName);
				}
				return files.ToArray();
			}
			catch(StorageClientException ex) {
				if(ex.ErrorCode == StorageErrorCode.BlobNotFound) throw new ArgumentException("Directory does not exist", "directory");
				throw ex;
			}
		}

		/// <summary>
		/// Lists the Directories in the specified directory.
		/// </summary>
		/// <param name="directory">The full directory name, for example "/my/directory". Null, empty or "/" for the root directory.</param>
		/// <returns>The list of Directories in the Directory.</returns>
		/// <exception cref="ArgumentException">If <paramref name="directory"/> does not exist.</exception>
		public string[] ListDirectories(string directory) {
			try {
				string directoryName = BlobExists(_wiki, BuildNameForBlobStorage(directory));
				if(directoryName == null) throw new ArgumentException("Directory does not exist", "directory");

				string[] blobs = ListFilesForInternalUse(_wiki, directoryName, false, false);
				List<string> directories = new List<string>();
				foreach(string blob in blobs) {
					if(blob.EndsWith("/")) directories.Add("/" + blob);
				}
				return directories.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Stores a file.
		/// </summary>
		/// <param name="fullName">The full name of the file.</param>
		/// <param name="sourceStream">A Stream object used as <b>source</b> of a byte stream,
		/// i.e. the method reads from the Stream and stores the content properly.</param>
		/// <param name="overwrite"><c>true</c> to overwrite an existing file.</param>
		/// <returns><c>true</c> if the File is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> or <paramref name="sourceStream"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty or <paramref name="sourceStream"/> does not support reading.</exception>
		public bool StoreFile(string fullName, System.IO.Stream sourceStream, bool overwrite) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty", "fullName");
			if(sourceStream == null) throw new ArgumentNullException("sourceStream");
			if(!sourceStream.CanRead) throw new ArgumentException("Cannot read from Source Stream", "sourceStream");

			try {
				string blobName = BlobExists(_wiki, BuildNameForBlobStorage(fullName));
				if(!overwrite && blobName != null) return false;

				var containerRef = _client.GetContainerReference(_wiki);

				var blobRef = containerRef.GetBlockBlobReference(BuildNameForBlobStorage(fullName));
				blobRef.UploadFromStream(sourceStream);

				return true;
			}
			catch(StorageClientException ex) {
				if(ex.ErrorCode == StorageErrorCode.BlobAlreadyExists) return false;
				throw ex;
			}
		}

		/// <summary>
		/// Retrieves a File.
		/// </summary>
		/// <param name="fullName">The full name of the File.</param>
		/// <param name="destinationStream">A Stream object used as <b>destination</b> of a byte stream,
		/// i.e. the method writes to the Stream the file content.</param>
		/// <returns><c>true</c> if the file is retrieved, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> or <paramref name="destinationStream"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty or <paramref name="destinationStream"/> does not support writing, or if <paramref name="fullName"/> does not exist.</exception>
		public bool RetrieveFile(string fullName, System.IO.Stream destinationStream) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty", "fullName");
			if(destinationStream == null) throw new ArgumentNullException("destinationStream");
			if(!destinationStream.CanWrite) throw new ArgumentException("Cannot write into Destination Stream", "destinationStream");

			try {
				var containerRef = _client.GetContainerReference(_wiki);
				string blobName = BlobExists(_wiki, BuildNameForBlobStorage(fullName));
				if(blobName == null) throw new ArgumentException("File does not exist", "fullName");

				var blobRef = containerRef.GetBlobReference(blobName);
				blobRef.DownloadToStream(destinationStream);

				return true;
			}
			catch(StorageClientException ex) {
				if(ex.ErrorCode == StorageErrorCode.BlobNotFound) return false;
				throw ex;
			}
		}

		/// <summary>
		/// Gets the details of a file.
		/// </summary>
		/// <param name="fullName">The full name of the file.</param>
		/// <returns>The details, or <c>null</c> if the file does not exist.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		public FileDetails GetFileDetails(string fullName) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty", "fullName");

			try {
				var blobName = BlobExists(_wiki, BuildNameForBlobStorage(fullName));
				if(blobName == null) return null;
				var containerRef = _client.GetContainerReference(_wiki);
				var blobRef = containerRef.GetBlobReference(blobName);
				blobRef.FetchAttributes();

				return new FileDetails(blobRef.Properties.Length, blobRef.Properties.LastModifiedUtc.ToLocalTime());
			}
			catch(StorageClientException ex) {
				if(ex.ErrorCode == StorageErrorCode.BlobNotFound) return null;
				throw ex;
			}
		}

		/// <summary>
		/// Deletes a File.
		/// </summary>
		/// <param name="fullName">The full name of the File.</param>
		/// <returns><c>true</c> if the File is deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty or it does not exist.</exception>
		public bool DeleteFile(string fullName) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty", "fullName");

			try {
				var containerRef = _client.GetContainerReference(_wiki);
				string blobName = BlobExists(_wiki, BuildNameForBlobStorage(fullName));
				if(blobName == null) throw new ArgumentException("File does not exist", "fullName");

				var blobRef = containerRef.GetBlobReference(blobName);
				bool deleted = blobRef.DeleteIfExists();

				return deleted;
			}
			catch(StorageClientException ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Renames or moves a File.
		/// </summary>
		/// <param name="oldFullName">The old full name of the File.</param>
		/// <param name="newFullName">The new full name of the File.</param>
		/// <returns><c>true</c> if the File is renamed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="oldFullName"/> or <paramref name="newFullName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="oldFullName"/> or <paramref name="newFullName"/> are empty, or if the old file does not exist, or if the new file already exist.</exception>
		public bool RenameFile(string oldFullName, string newFullName) {
			if(oldFullName == null) throw new ArgumentNullException("oldFullName");
			if(oldFullName.Length == 0) throw new ArgumentException("Old Full Name cannot be empty", "oldFullName");
			if(newFullName == null) throw new ArgumentNullException("newFullName");
			if(newFullName.Length == 0) throw new ArgumentException("New Full Name cannot be empty", "newFullName");

			try {
				var containerRef = _client.GetContainerReference(_wiki);
				string oldBlobName = BlobExists(_wiki, BuildNameForBlobStorage(oldFullName));
				if(oldBlobName == null) throw new ArgumentException("Old File does not exist", "oldFullName");
				var oldBlobRef = containerRef.GetBlobReference(oldBlobName);
				string newBlobName = BlobExists(_wiki, BuildNameForBlobStorage(newFullName));
				if(newBlobName != null) throw new ArgumentException("New File already exists", "newFullName");
				var newBlobRef = containerRef.GetBlobReference(BuildNameForBlobStorage(newFullName));

				newBlobRef.CopyFromBlob(oldBlobRef);
				oldBlobRef.Delete();

				return true;
			}
			catch(StorageClientException ex) {
				if(ex.ErrorCode == StorageErrorCode.BlobNotFound) throw new ArgumentException("Old File does not exist", "oldFullName");
				if(ex.ErrorCode == StorageErrorCode.BlobAlreadyExists) throw new ArgumentException("New File already exists", "newFullName");
				throw ex;
			}
		}

		/// <summary>
		/// Creates a new Directory.
		/// </summary>
		/// <param name="path">The path to create the new Directory in.</param>
		/// <param name="name">The name of the new Directory.</param>
		/// <returns><c>true</c> if the Directory is created, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="path"/> or <paramref name="name"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty or if the directory does not exist, or if the new directory already exists.</exception>
		public bool CreateDirectory(string path, string name) {
			if(path == null) throw new ArgumentNullException("path");
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			try {
				var containerRef = _client.GetContainerReference(_wiki);
				if(BlobExists(_wiki, BuildNameForBlobStorage(path)) == null) throw new ArgumentException("Directory does not exist", "path");
				if(BlobExists(_wiki, BuildNameForBlobStorage(Path.Combine(path, name))) != null) throw new ArgumentException("Directory already exists", "name");
				CloudBlobDirectory directoryRef = containerRef.GetDirectoryReference(BuildNameForBlobStorage(Path.Combine(path, name)));
				directoryRef.GetBlobReference(".stw.dat").UploadText("");

				return true;
			}
			catch(StorageClientException ex) {
				if(ex.ErrorCode == StorageErrorCode.BlobAlreadyExists) throw new ArgumentException("Directory already exists", "name");
				if(ex.ErrorCode == StorageErrorCode.BlobNotFound) throw new ArgumentException("Directory does not exist", "path");
				throw ex;
			}
		}

		/// <summary>
		/// Deletes a Directory and <b>all of its content</b>.
		/// </summary>
		/// <param name="fullPath">The full path of the Directory.</param>
		/// <returns><c>true</c> if the Directory is delete, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullPath"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullPath"/> is empty or if it equals '/' or it does not exist.</exception>
		public bool DeleteDirectory(string fullPath) {
			if(fullPath == null) throw new ArgumentNullException("fullPath");
			if(fullPath.Length == 0) throw new ArgumentException("Full Path cannot be empty", "fullPath");
			if(fullPath == "/") throw new ArgumentException("Cannot delete the root directory", "fullPath");

			try {
				string directoryName = BlobExists(_wiki, BuildNameForBlobStorage(fullPath));
				if(directoryName == null) throw new ArgumentException("Directory does not exist", "fullPath");

				var containerRef = _client.GetContainerReference(_wiki);
				string[] blobs = ListFilesForInternalUse(_wiki, fullPath, true, true);
				foreach(string blob in blobs) {
					var blobRef = containerRef.GetBlobReference(blob);
					blobRef.Delete();
				}

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Renames or moves a Directory.
		/// </summary>
		/// <param name="oldFullPath">The old full path of the Directory.</param>
		/// <param name="newFullPath">The new full path of the Directory.</param>
		/// <returns><c>true</c> if the Directory is renamed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="oldFullPath"/> or <paramref name="newFullPath"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="oldFullPath"/> or <paramref name="newFullPath"/> are empty or equal to '/',
		/// or if the old directory does not exist or the new directory already exists.</exception>
		public bool RenameDirectory(string oldFullPath, string newFullPath) {
			if(oldFullPath == null) throw new ArgumentNullException("oldFullPath");
			if(oldFullPath.Length == 0) throw new ArgumentException("Old Full Path cannot be empty", "oldFullPath");
			if(oldFullPath == "/") throw new ArgumentException("Cannot rename the root directory", "oldFullPath");
			if(newFullPath == null) throw new ArgumentNullException("newFullPath");
			if(newFullPath.Length == 0) throw new ArgumentException("New Full Path cannot be empty", "newFullPath");
			if(newFullPath == "/") throw new ArgumentException("Cannot rename directory to the root directory", "newFullPath");

			try {
				string oldDirectoryName = BlobExists(_wiki, BuildNameForBlobStorage(oldFullPath));
				if(oldDirectoryName == null) throw new ArgumentException("Directory does not exist", "oldFullPath");
				string newDirectoryName = BlobExists(_wiki, BuildNameForBlobStorage(newFullPath));
				if(newDirectoryName != null) throw new ArgumentException("Directory already exists", "newFullPath");
				newDirectoryName = BuildNameForBlobStorage(newFullPath);

				var containerRef = _client.GetContainerReference(_wiki);
				string[] blobs = ListFilesForInternalUse(_wiki, oldFullPath, true, true);
				foreach(string blob in blobs) {
					string newBlobName = blob.Substring(blob.IndexOf(oldDirectoryName) + oldDirectoryName.Length);
					newBlobName = newDirectoryName + newBlobName;
					var newBlobReference = containerRef.GetBlobReference(newBlobName);
					var oldBlobReference = containerRef.GetBlobReference(blob);
					newBlobReference.CopyFromBlob(oldBlobReference);
					oldBlobReference.Delete();
				}

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// The the names of the pages with attachments.
		/// </summary>
		/// <returns>The names of the pages with attachments.</returns>
		public string[] GetPagesWithAttachments() {
			try {
				string[] blobs = ListFilesForInternalUse(_wiki + "-attachments", "", false, false);
				List<string> directories = new List<string>();
				foreach(string blob in blobs) {
					if(blob.EndsWith("/")) directories.Add(blob.Trim('/'));
				}
				return directories.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Returns the names of the Attachments of a Page.
		/// </summary>
		/// <param name="pageFullName">The full name of the page that owns the Attachments.</param>
		/// <returns>The names, or an empty list.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		public string[] ListPageAttachments(string pageFullName) {
			if(pageFullName == null) throw new ArgumentNullException("pageFullName");
			if(pageFullName.Length == 0) throw new ArgumentException("pageFullName");

			try {
				string dirName = BlobExists(_wiki + "-attachments", BuildNameForBlobStorage(pageFullName));
				if(dirName == null) return new string[0];

				string[] blobs = ListFilesForInternalUse(_wiki + "-attachments", dirName, false, true);
				List<string> directories = new List<string>();
				foreach(string blob in blobs) {
					string blobName = blob.Substring(blob.IndexOf(dirName) + dirName.Length);
					if(blobName != "") directories.Add(blobName.TrimStart('/'));
				}
				return directories.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Stores a Page Attachment.
		/// </summary>
		/// <param name="pageFullName">The Page Info that owns the Attachment.</param>
		/// <param name="name">The name of the Attachment, for example "myfile.jpg".</param>
		/// <param name="sourceStream">A Stream object used as <b>source</b> of a byte stream,
		/// i.e. the method reads from the Stream and stores the content properly.</param>
		/// <param name="overwrite"><c>true</c> to overwrite an existing Attachment.</param>
		/// <returns><c>true</c> if the Attachment is stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/>, <paramref name="name"/> or <paramref name="sourceStream"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/>, <paramref name="name"/> are empty or if <paramref name="sourceStream"/> does not support reading.</exception>
		public bool StorePageAttachment(string pageFullName, string name, System.IO.Stream sourceStream, bool overwrite) {
			if(pageFullName == null) throw new ArgumentNullException("pageFullName");
			if(pageFullName.Length == 0) throw new ArgumentException("pageFullName");
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");
			if(sourceStream == null) throw new ArgumentNullException("sourceStream");
			if(!sourceStream.CanRead) throw new ArgumentException("Cannot read from Source Stream", "sourceStream");

			try {
				var containerRef = _client.GetContainerReference(_wiki + "-attachments");

				if(BlobExists(_wiki + "-attachments", BuildNameForBlobStorage(pageFullName)) == null) {
					CloudBlobDirectory directoryRef = containerRef.GetDirectoryReference(BuildNameForBlobStorage(pageFullName));
					directoryRef.GetBlobReference(".stw.dat").UploadText("");
				}

				string blobName = BlobExists(_wiki + "-attachments", BuildNameForBlobStorage(pageFullName) + "/" + BuildNameForBlobStorage(name));
				if(!overwrite && blobName != null) return false;

				blobName = blobName != null ? blobName : BuildNameForBlobStorage(pageFullName) + "/" + BuildNameForBlobStorage(name);

				var blobRef = containerRef.GetBlockBlobReference(blobName);
				blobRef.UploadFromStream(sourceStream);

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Retrieves a Page Attachment.
		/// </summary>
		/// <param name="pageFullName">The Page Info that owns the Attachment.</param>
		/// <param name="name">The name of the Attachment, for example "myfile.jpg".</param>
		/// <param name="destinationStream">A Stream object used as <b>destination</b> of a byte stream,
		/// i.e. the method writes to the Stream the file content.</param>
		/// <returns><c>true</c> if the Attachment is retrieved, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/>, <paramref name="name"/> or <paramref name="destinationStream"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> or <paramref name="name"/> are empty or if <paramref name="destinationStream"/> does not support writing,
		/// or if the page does not have attachments or if the attachment does not exist.</exception>
		public bool RetrievePageAttachment(string pageFullName, string name, System.IO.Stream destinationStream) {
			if(pageFullName == null) throw new ArgumentNullException("pageFullName");
			if(pageFullName.Length == 0) throw new ArgumentException("pageFullName");
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");
			if(destinationStream == null) throw new ArgumentNullException("destinationStream");
			if(!destinationStream.CanWrite) throw new ArgumentException("Cannot write into Destination Stream", "destinationStream");

			try {
				var containerRef = _client.GetContainerReference(_wiki + "-attachments");
				string dirName = BlobExists(_wiki + "-attachments", BuildNameForBlobStorage(pageFullName));
				if(dirName == null) throw new ArgumentException("No attachments for Page", "pageInfo");

				string blobName = BlobExists(_wiki + "-attachments", dirName.TrimEnd('/') + "/" + BuildNameForBlobStorage(name));
				if(dirName == null) throw new ArgumentException("Attachment does not exist", "name");

				var blobRef = containerRef.GetBlobReference(blobName);
				blobRef.DownloadToStream(destinationStream);

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets the details of a page attachment.
		/// </summary>
		/// <param name="pageFullName">The full name of the page that owns the attachment.</param>
		/// <param name="attachmentName">The name of the attachment, for example "myfile.jpg".</param>
		/// <returns>The details of the attachment, or <c>null</c> if the attachment does not exist.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> or <paramref name="attachmentName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> or <paramref name="attachmentName"/> are empty.</exception>
		public FileDetails GetPageAttachmentDetails(string pageFullName, string attachmentName) {
			if(pageFullName == null) throw new ArgumentNullException("pageInfo");
			if(attachmentName == null) throw new ArgumentNullException("name");
			if(attachmentName.Length == 0) throw new ArgumentException("Name cannot be empty");

			try {
				var blobName = BlobExists(_wiki + "-attachments", BuildNameForBlobStorage(pageFullName) + "/" + BuildNameForBlobStorage(attachmentName));
				if(blobName == null) return null;
				var containerRef = _client.GetContainerReference(_wiki + "-attachments");
				var blobRef = containerRef.GetBlobReference(blobName);
				blobRef.FetchAttributes();

				return new FileDetails(blobRef.Properties.Length, blobRef.Properties.LastModifiedUtc.ToLocalTime());
			}
			catch(StorageClientException ex) {
				if(ex.ErrorCode == StorageErrorCode.BlobNotFound) return null;
				throw ex;
			}
		}

		/// <summary>
		/// Deletes a Page Attachment.
		/// </summary>
		/// <param name="pageFullName">The Page Info that owns the Attachment.</param>
		/// <param name="attachmentName">The name of the Attachment, for example "myfile.jpg".</param>
		/// <returns><c>true</c> if the Attachment is deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> or <paramref name="attachmentName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> or <paramref name="attachmentName"/> are empty or if the page or attachment do not exist.</exception>
		public bool DeletePageAttachment(string pageFullName, string attachmentName) {
			if(pageFullName == null) throw new ArgumentNullException("pageFullName");
			if(pageFullName.Length == 0) throw new ArgumentException("pageFullName");
			if(attachmentName == null) throw new ArgumentNullException("name");
			if(attachmentName.Length == 0) throw new ArgumentException("Name cannot be empty");

			try {
				var containerRef = _client.GetContainerReference(_wiki + "-attachments");
				string blobName = BlobExists(_wiki + "-attachments", BuildNameForBlobStorage(pageFullName) + "/" + BuildNameForBlobStorage(attachmentName));
				if(blobName == null) throw new ArgumentException("File does not exist", "fullName");

				var blobRef = containerRef.GetBlobReference(blobName);
				bool deleted = blobRef.DeleteIfExists();

				return deleted;
			}
			catch(StorageClientException ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Renames a Page Attachment.
		/// </summary>
		/// <param name="pageFullName">The Page Info that owns the Attachment.</param>
		/// <param name="oldName">The old name of the Attachment.</param>
		/// <param name="newName">The new name of the Attachment.</param>
		/// <returns><c>true</c> if the Attachment is renamed, false otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/>, <paramref name="oldName"/> or <paramref name="newName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/>, <paramref name="oldName"/> or <paramref name="newName"/> are empty,
		/// or if the page or old attachment do not exist, or the new attachment name already exists.</exception>
		public bool RenamePageAttachment(string pageFullName, string oldName, string newName) {
			if(pageFullName == null) throw new ArgumentNullException("pageFullName");
			if(pageFullName.Length == 0) throw new ArgumentException("pageFullName");
			if(oldName == null) throw new ArgumentNullException("oldName");
			if(oldName.Length == 0) throw new ArgumentException("Old Name cannot be empty", "oldName");
			if(newName == null) throw new ArgumentNullException("newName");
			if(newName.Length == 0) throw new ArgumentException("New Name cannot be empty", "newName");

			try {
				var containerRef = _client.GetContainerReference(_wiki + "-attachments");
				string oldBlobName = BlobExists(_wiki + "-attachments", BuildNameForBlobStorage(pageFullName) + "/" + BuildNameForBlobStorage(oldName));
				if(oldBlobName == null) throw new ArgumentException("Old File does not exist", "oldFullName");
				var oldBlobRef = containerRef.GetBlobReference(oldBlobName);
				string newBlobName = BlobExists(_wiki + "-attachments", BuildNameForBlobStorage(pageFullName) + "/" + BuildNameForBlobStorage(newName));
				if(newBlobName != null) throw new ArgumentException("New File already exists", "newFullName");
				var newBlobRef = containerRef.GetBlobReference(BuildNameForBlobStorage(pageFullName) + "/" + BuildNameForBlobStorage(newName));

				newBlobRef.CopyFromBlob(oldBlobRef);
				oldBlobRef.Delete();
				return true;
			}
			catch(StorageClientException ex) {
				if(ex.ErrorCode == StorageErrorCode.BlobNotFound) throw new ArgumentException("Old File does not exist", "oldFullName");
				if(ex.ErrorCode == StorageErrorCode.BlobAlreadyExists) throw new ArgumentException("New File already exists", "newFullName");
				throw ex;
			}
		}

		/// <summary>
		/// Notifies the Provider that a Page has been renamed.
		/// </summary>
		/// <param name="oldPageFullName">The old page full name.</param>
		/// <param name="newPageFullName">The new page full name.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="oldPageFullName"/> or <paramref name="newPageFullName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="oldPageFullName"/> or <paramref name="newPageFullName"/> are empty or if the new page full name is already in use.</exception>
		public void NotifyPageRenaming(string oldPageFullName, string newPageFullName) {
			if(oldPageFullName == null) throw new ArgumentNullException("oldPageFullName");
			if(oldPageFullName.Length == 0) throw new ArgumentException("oldPageFullName");
			if(newPageFullName == null) throw new ArgumentNullException("newPageFullName");
			if(newPageFullName.Length == 0) throw new ArgumentException("newPageFullName");

			try {
				string oldDirectoryName = BlobExists(_wiki + "-attachments", BuildNameForBlobStorage(oldPageFullName));
				if(oldDirectoryName == null) return;
				string newDirectoryName = BlobExists(_wiki + "-attachments", BuildNameForBlobStorage(newPageFullName));
				if(newDirectoryName != null) throw new ArgumentException("New Page already exists", "newPage");
				newDirectoryName = BuildNameForBlobStorage(newPageFullName);

				var containerRef = _client.GetContainerReference(_wiki + "-attachments");
				string[] blobs = ListFilesForInternalUse(_wiki + "-attachments", oldPageFullName, true, true);
				foreach(string blob in blobs) {
					string newBlobName = blob.Substring(blob.IndexOf(oldDirectoryName) + oldDirectoryName.Length);
					newBlobName = newDirectoryName + newBlobName;
					var newBlobReference = containerRef.GetBlobReference(newBlobName);
					var oldBlobReference = containerRef.GetBlobReference(blob);
					newBlobReference.CopyFromBlob(oldBlobReference);
					oldBlobReference.Delete();
				}
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		#endregion

		#region IStorageProviderV40 Members

		/// <summary>
		/// Gets a value specifying whether the provider is read-only, i.e. it can only provide data and not store it.
		/// </summary>
		public bool ReadOnly {
			get { return false; }
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
			if(config == null) throw new ArgumentNullException("config");

			if(config == "") config = Config.GetConnectionString();

			_host = host;
			_wiki = string.IsNullOrEmpty(wiki) ? "root" : wiki.ToLowerInvariant();

			_client = TableStorage.StorageAccount(config).CreateCloudBlobClient();
			
			CloudBlobContainer containerRef = _client.GetContainerReference(_wiki);
			containerRef.CreateIfNotExist();

			containerRef = _client.GetContainerReference(_wiki + "-attachments");
			containerRef.CreateIfNotExist();
		}

		/// <summary>
		/// Sets up the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void SetUp(IHostV40 host, string config) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			if(config == "") config = Config.GetConnectionString();

			_host = host;
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return new ComponentInformation("Azure Blob Storage Files Storage Provider", "Threeplicate Srl", "4.0.1.71", "http://www.screwturn.eu", null); }
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
			// Nothing todo
		}

		#endregion
	}

	internal class FileRetrievalStatEntity : TableServiceEntity {
		// PartitionKey = wiki
		// RowKey = filename

		public int Count { get; set; }
	}
}
