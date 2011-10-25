
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using System.IO;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Manages files, directories and attachments.
	/// </summary>
	public static class FilesAndAttachments {

		/// <summary>
		/// Finds the provider that has a file.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="fullName">The full name of the file.</param>
		/// <returns>The provider that has the file, or <c>null</c> if the file could not be found.</returns>
		public static IFilesStorageProviderV40 FindFileProvider(string wiki, string fullName) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(string.IsNullOrEmpty(fullName)) throw new ArgumentException("Full Name cannot be empty", "fullName");

			fullName = NormalizeFullName(fullName);

			foreach(IFilesStorageProviderV40 provider in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
				FileDetails details = provider.GetFileDetails(fullName);
				if(details != null) return provider;
			}

			return null;
		}

		/// <summary>
		/// Stores and indexes a file.
		/// </summary>
		/// <param name="provider">The destination provider.</param>
		/// <param name="fullName">The full name.</param>
		/// <param name="source">The source stream.</param>
		/// <param name="overwrite"><c>true</c> to overwrite the existing file, <c>false</c> otherwise.</param>
		/// <returns><c>true</c> if the file was stored, <c>false</c> otherwise.</returns>
		public static bool StoreFile(IFilesStorageProviderV40 provider, string fullName, Stream source, bool overwrite) {
			if(provider == null) throw new ArgumentNullException("provider");
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty", "fullName");
			if(source == null) throw new ArgumentNullException("source");

			fullName = NormalizeFullName(fullName);

			bool done = provider.StoreFile(fullName, source, overwrite);
			if(!done) return false;

			if(overwrite) SearchClass.UnindexFile(provider.GetType().FullName + "|" + fullName, provider.CurrentWiki);

			// Index the attached file
			string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			if(!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
			string tempFile = Path.Combine(tempDir, Path.GetFileName(fullName));

			source.Seek(0, SeekOrigin.Begin);
			using(FileStream temp = File.Create(tempFile)) {
				source.CopyTo(temp);
			}
			SearchClass.IndexFile(provider.GetType().FullName + "|" + fullName, tempFile, provider.CurrentWiki);
			Directory.Delete(tempDir, true);

			Host.Instance.OnFileActivity(provider.GetType().FullName, fullName, null, FileActivity.FileUploaded);

			return true;
		}

		/// <summary>
		/// Renames and re-indexes a file.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="fullName">The full name of the file to rename.</param>
		/// <param name="newName">The new name of the file (without namespace).</param>
		/// <returns><c>true</c> if the file was renamed, <c>false</c> otherwise.</returns>
		public static bool RenameFile(IFilesStorageProviderV40 provider, string fullName, string newName) {
			if(provider == null) throw new ArgumentNullException("provider");
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty", "fullName");
			if(newName == null) throw new ArgumentNullException("newName");
			if(newName.Length == 0) throw new ArgumentException("New Name cannot be empty", "newName");

			fullName = NormalizeFullName(fullName);
			string newFullName = GetDirectory(fullName) + newName;
			newFullName = NormalizeFullName(newFullName);

			if(newFullName.ToLowerInvariant() == fullName.ToLowerInvariant()) return false;

			bool done = provider.RenameFile(fullName, newFullName);
			if(!done) return false;

			SearchClass.RenameFile(provider.CurrentWiki, provider.GetType().FullName + "|" + fullName, provider.GetType().FullName + "|" + newFullName);

			Host.Instance.OnFileActivity(provider.GetType().FullName, fullName, newFullName, FileActivity.FileRenamed);
				
			return true;
		}

		/// <summary>
		/// Deletes and de-indexes a file.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="fullName">The full name of the file to delete.</param>
		/// <returns><c>true</c> if the file was deleted, <c>false</c> otherwise.</returns>
		public static bool DeleteFile(IFilesStorageProviderV40 provider, string fullName) {
			if(provider == null) throw new ArgumentNullException("provider");
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty", "fullName");

			fullName = NormalizeFullName(fullName);

			bool done = provider.DeleteFile(fullName);
			if(!done) return false;

			SearchClass.UnindexFile(provider.GetType().FullName + "|" + fullName, provider.CurrentWiki);

			Host.Instance.OnFileActivity(provider.GetType().FullName, fullName, null, FileActivity.FileDeleted);

			return true;
		}

		/// <summary>
		/// Finds the provider that has a directory.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="fullPath">The full path of the directory.</param>
		/// <returns>The provider that has the directory, or <c>null</c> if no directory is found.</returns>
		public static IFilesStorageProviderV40 FindDirectoryProvider(string wiki, string fullPath) {
			if(fullPath == null) throw new ArgumentNullException("fullPath");
			if(fullPath.Length == 0) throw new ArgumentException("Full Path cannot be empty");

			fullPath = NormalizeFullPath(fullPath);

			// In order to verify that the full path exists, it is necessary to navigate 
			// from the root down to the specified directory level
			// Example: /my/very/nested/directory/structure/
			// 1. Check that / contains /my/
			// 2. Check that /my/ contains /my/very/
			// 3. ...

			// allLevels contains this:
			// /my/very/nested/directory/structure/
			// /my/very/nested/directory/
			// /my/very/nested/
			// /my/very/
			// /my/
			// /

			string oneLevelUp = fullPath;

			List<string> allLevels = new List<string>(10);
			allLevels.Add(fullPath.ToLowerInvariant());
			while(oneLevelUp != "/") {
				oneLevelUp = UpOneLevel(oneLevelUp);
				allLevels.Add(oneLevelUp.ToLowerInvariant());
			}

			foreach(IFilesStorageProviderV40 provider in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
				bool allLevelsFound = true;

				for(int i = allLevels.Count - 1; i >= 1; i--) {
					string[] dirs = provider.ListDirectories(allLevels[i]);

					string nextLevel =
						(from d in dirs
						 where d.ToLowerInvariant() == allLevels[i - 1]
						 select d).FirstOrDefault();

					if(string.IsNullOrEmpty(nextLevel)) {
						allLevelsFound = false;
						break;
					}
				}

				if(allLevelsFound) return provider;
			}

			return null;
		}

		/// <summary>
		/// Creates a new directory.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="path">The path.</param>
		/// <param name="name">The name of the new directory.</param>
		/// <returns><c>true</c> if the directory was created, <c>false</c> otherwise.</returns>
		public static bool CreateDirectory(IFilesStorageProviderV40 provider, string path, string name) {
			if(provider == null) throw new ArgumentNullException("provider");
			if(path == null) throw new ArgumentNullException("path");
			if(path.Length == 0) path = "/";
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			path = NormalizeFullPath(path);
			name = name.Trim('/', ' ');

			bool done = provider.CreateDirectory(path, name);
			if(!done) return false;

			Host.Instance.OnDirectoryActivity(provider.GetType().FullName, path + name + "/", null, FileActivity.DirectoryCreated);

			return true;
		}

		/// <summary>
		/// Renames a directory and re-indexes all contents.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="fullPath">The full path of the directory to rename.</param>
		/// <param name="newName">The new name of the directory.</param>
		/// <returns><c>true</c> if the directory was renamed, <c>false</c> otherwise.</returns>
		public static bool RenameDirectory(IFilesStorageProviderV40 provider, string fullPath, string newName) {
			if(provider == null) throw new ArgumentNullException("provider");
			if(fullPath == null) throw new ArgumentNullException("fullPath");
			if(fullPath.Length == 0) throw new ArgumentException("Cannot rename the root directory", "fullPath");
			if(newName == null) throw new ArgumentNullException("newName");
			if(newName.Length == 0) throw new ArgumentException("New Name cannot be empty", "newName");

			fullPath = NormalizeFullPath(fullPath);
			string newFullPath = GetDirectory(fullPath) + newName;
			newFullPath = NormalizeFullPath(newFullPath);

			bool done = provider.RenameDirectory(fullPath, newFullPath);
			if(!done) return false;

			MovePermissions(provider, fullPath, newFullPath);
			ReindexDirectory(provider, fullPath, newFullPath);

			Host.Instance.OnDirectoryActivity(provider.GetType().FullName, fullPath, newFullPath, FileActivity.DirectoryRenamed);

			return true;
		}

		/// <summary>
		/// Recursively re-indexes all the contents of the old (renamed) directory to the new one.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="oldDirectory">The old directory.</param>
		/// <param name="newDirectory">The new directory.</param>
		private static void ReindexDirectory(IFilesStorageProviderV40 provider, string oldDirectory, string newDirectory) {
			oldDirectory = NormalizeFullPath(oldDirectory);
			newDirectory = NormalizeFullPath(newDirectory);

			// At this point the directory has been already renamed,
			// thus we must list on the new directory and construct the old name
			// Example: /directory/one/ renamed to /directory/two-two/
			// List on /directory/two-two/
			//     dir1
			//     dir2
			// oldSub = /directory/one/dir1/

			foreach(string sub in provider.ListDirectories(newDirectory)) {
				string oldSub = oldDirectory + sub.Substring(newDirectory.Length);
				ReindexDirectory(provider, oldSub, sub);
			}

			foreach(string file in provider.ListFiles(newDirectory)) {
				string oldFile = oldDirectory + file.Substring(newDirectory.Length);
				SearchClass.RenameFile(provider.CurrentWiki, provider.GetType().FullName + "|" + oldFile, provider.GetType().FullName + "|" + file);
			}
		}

		/// <summary>
		/// Recursively moves permissions from the old (renamed) directory to the new one.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="oldDirectory">The old directory.</param>
		/// <param name="newDirectory">The new directory.</param>
		private static void MovePermissions(IFilesStorageProviderV40 provider, string oldDirectory, string newDirectory) {
			oldDirectory = NormalizeFullPath(oldDirectory);
			newDirectory = NormalizeFullPath(newDirectory);

			// At this point the directory has been already renamed,
			// thus we must list on the new directory and construct the old name
			// Example: /directory/one/ renamed to /directory/two-two/
			// List on /directory/two-two/
			//     dir1
			//     dir2
			// oldSub = /directory/one/dir1/

			foreach(string sub in provider.ListDirectories(newDirectory)) {
				string oldSub = oldDirectory + sub.Substring(newDirectory.Length);
				MovePermissions(provider, oldSub, sub);
			}

			AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(provider.CurrentWiki));
			authWriter.ClearEntriesForDirectory(provider, newDirectory);
			authWriter.ProcessDirectoryRenaming(provider, oldDirectory, newDirectory);
		}

		/// <summary>
		/// Deletes a directory and de-indexes all its contents.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="directory">The directory to delete.</param>
		public static bool DeleteDirectory(IFilesStorageProviderV40 provider, string directory) {
			if(provider == null) throw new ArgumentNullException("provider");
			if(directory == null) throw new ArgumentNullException("directory");
			if(directory.Length == 0 || directory == "/") throw new ArgumentException("Cannot delete the root directory", "directory");

			directory = NormalizeFullPath(directory);

			// This must be done BEFORE deleting the directory, otherwise there wouldn't be a way to list contents
			DeletePermissions(provider, directory);
			DeindexDirectory(provider, directory);

			// Delete Directory
			bool done = provider.DeleteDirectory(directory);
			if(!done) return false;
			
			Host.Instance.OnDirectoryActivity(provider.GetType().FullName, directory, null, FileActivity.DirectoryDeleted);

			return true;
		}

		/// <summary>
		/// De-indexes all contents of a directory.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="directory">The directory.</param>
		private static void DeindexDirectory(IFilesStorageProviderV40 provider, string directory) {
			directory = NormalizeFullPath(directory);

			foreach(string sub in provider.ListDirectories(directory)) {
				DeindexDirectory(provider, sub);
			}

			foreach(string file in provider.ListFiles(directory)) {
				SearchClass.UnindexFile(provider.GetType().FullName + "|" + file, provider.CurrentWiki);
			}
		}

		/// <summary>
		/// Deletes the permissions of a directory.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="directory">The directory.</param>
		private static void DeletePermissions(IFilesStorageProviderV40 provider, string directory) {
			directory = NormalizeFullPath(directory);

			foreach(string sub in provider.ListDirectories(directory)) {
				DeletePermissions(provider, sub);
			}

			AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(provider.CurrentWiki));
			authWriter.ClearEntriesForDirectory(provider, directory);
		}

		/// <summary>
		/// Finds the provider that has a page attachment.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="pageFullName">The page full name.</param>
		/// <param name="attachmentName">The name of the attachment.</param>
		/// <returns>The provider that has the attachment, or <c>null</c> if the attachment could not be found.</returns>
		public static IFilesStorageProviderV40 FindPageAttachmentProvider(string wiki, string pageFullName, string attachmentName) {
			if(pageFullName == null) throw new ArgumentNullException("page");
			if(attachmentName == null) throw new ArgumentNullException("attachmentName");
			if(attachmentName.Length == 0) throw new ArgumentException("Attachment Name cannot be empty", "attachmentName");

			foreach(IFilesStorageProviderV40 provider in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
				FileDetails details = provider.GetPageAttachmentDetails(pageFullName, attachmentName);
				if(details != null) return provider;
			}

			return null;
		}

		/// <summary>
		/// Stores and indexes a page attachment.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="pageFullName">The page full name.</param>
		/// <param name="name">The attachment name.</param>
		/// <param name="source">The source stream.</param>
		/// <param name="overwrite"><c>true</c> to overwrite an existing attachment with the same name, <c>false</c> otherwise.</param>
		/// <returns><c>true</c> if the attachment was stored, <c>false</c> otherwise.</returns>
		public static bool StorePageAttachment(IFilesStorageProviderV40 provider, string pageFullName, string name, Stream source, bool overwrite) {
			if(provider == null) throw new ArgumentNullException("provider");
			if(pageFullName == null) throw new ArgumentNullException("pageFullName");
			if(pageFullName.Length == 0) throw new ArgumentException("Page Full Name cannot be empty", "pageFullName");
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");
			if(source == null) throw new ArgumentNullException("source");

			PageContent page = Pages.FindPage(provider.CurrentWiki, pageFullName);
			if(page == null) return false;

			bool done = provider.StorePageAttachment(pageFullName, name, source, overwrite);
			if(!done) return false;

			if(overwrite) SearchClass.UnindexPageAttachment(name, page);

			// Index the attached file
			string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
			if(!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
			string tempFile = Path.Combine(tempDir, name);

			source.Seek(0, SeekOrigin.Begin);
			using(FileStream temp = File.Create(tempFile)) {
				source.CopyTo(temp);
			}
			SearchClass.IndexPageAttachment(name, tempFile, page);
			Directory.Delete(tempDir, true);

			Host.Instance.OnAttachmentActivity(provider.GetType().FullName, name, pageFullName, null, FileActivity.AttachmentUploaded);

			return true;
		}

		/// <summary>
		/// Renames and re-indexes a page attachment.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="pageFullName">The page full name.</param>
		/// <param name="name">The attachment name.</param>
		/// <param name="newName">The new attachment name.</param>
		/// <returns><c>true</c> if the attachment was rename, <c>false</c> otherwise.</returns>
		public static bool RenamePageAttachment(IFilesStorageProviderV40 provider, string pageFullName, string name, string newName) {
			if(provider == null) throw new ArgumentNullException("provider");
			if(pageFullName == null) throw new ArgumentNullException("pageFullName");
			if(pageFullName.Length == 0) throw new ArgumentException("Page Full Name cannot be empty", "pageFullName");
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");
			if(newName == null) throw new ArgumentNullException("newName");
			if(newName.Length == 0) throw new ArgumentException("New Name cannot be empty", "newName");

			if(name.ToLowerInvariant() == newName.ToLowerInvariant()) return false;

			PageContent page = Pages.FindPage(provider.CurrentWiki, pageFullName);
			if(page == null) return false;
			
			bool done = provider.RenamePageAttachment(pageFullName, name, newName);
			if(!done) return false;

			SearchClass.RenamePageAttachment(page, name, newName);

			Host.Instance.OnAttachmentActivity(provider.GetType().FullName, newName, pageFullName, name, FileActivity.AttachmentRenamed);

			return true;
		}

		/// <summary>
		/// Deletes and de-indexes a page attachment.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="pageFullName">The page full name.</param>
		/// <param name="name">The attachment name.</param>
		/// <returns><c>true</c> if the attachment was deleted, <c>false</c> otherwise.</returns>
		public static bool DeletePageAttachment(IFilesStorageProviderV40 provider, string pageFullName, string name) {
			if(provider == null) throw new ArgumentNullException("provider");
			if(pageFullName == null) throw new ArgumentNullException("pageFullName");
			if(pageFullName.Length == 0) throw new ArgumentException("Page Full Name cannot be empty", "pageFullName");
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			PageContent page = Pages.FindPage(provider.CurrentWiki, pageFullName);
			if(page == null) return false;

			bool done = provider.DeletePageAttachment(pageFullName, name);
			if(!done) return false;

			SearchClass.UnindexPageAttachment(name, page);

			Host.Instance.OnAttachmentActivity(provider.GetType().FullName, name, pageFullName, null, FileActivity.AttachmentDeleted);

			return true;
		}

		/// <summary>
		/// Normalizes a full name.
		/// </summary>
		/// <param name="fullName">The full name.</param>
		/// <returns>The normalized full name.</returns>
		private static string NormalizeFullName(string fullName) {
			if(!fullName.StartsWith("/")) fullName = "/" + fullName;
			return fullName;
		}

		/// <summary>
		/// Normalizes a full path.
		/// </summary>
		/// <param name="fullPath">The full path.</param>
		/// <returns>The normalized full path.</returns>
		private static string NormalizeFullPath(string fullPath) {
			if(fullPath == null) return "/";
			if(!fullPath.StartsWith("/")) fullPath = "/" + fullPath;
			if(!fullPath.EndsWith("/")) fullPath += "/";
			return fullPath;
		}

		/// <summary>
		/// Goes up one level in a directory path.
		/// </summary>
		/// <param name="fullPath">The full path, normalized, different from "/".</param>
		/// <returns>The directory.</returns>
		private static string UpOneLevel(string fullPath) {
			if(fullPath == "/") throw new ArgumentException("Cannot navigate up from the root");

			string temp = fullPath.Trim('/');
			int lastIndex = temp.LastIndexOf("/");

			return "/" + temp.Substring(0, lastIndex + 1);
		}

		/// <summary>
		/// Gets the directory of a path.
		/// </summary>
		/// <param name="fullPath">The full path in normalized form ('/' or '/folder/' or '/folder/file.jpg').</param>
		/// <returns>The directory.</returns>
		private static string GetDirectory(string fullPath) {
			if(fullPath == "/") return "/";

			string temp = fullPath.Trim('/');
			int lastIndex = temp.LastIndexOf("/");

			return "/" + temp.Substring(0, lastIndex + 1);
		}

	}

}
