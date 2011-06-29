
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements a Local Files Storage Provider.
	/// </summary>
	public class FilesStorageProvider : ProviderBase, IFilesStorageProviderV40 {

		private readonly ComponentInformation info = new ComponentInformation("Local Files Provider",
			"Threeplicate Srl", "4.0.2.76", "http://www.screwturn.eu", null);

		// The following strings MUST terminate with DirectorySeparatorPath in order to properly work
		// in BuildFullPath method
		private readonly string UploadDirectory = "Upload" + Path.DirectorySeparatorChar;
		private readonly string AttachmentsDirectory = "Attachments" + Path.DirectorySeparatorChar;

		private const string FileDownloadsFile = "FileDownloads.cs";
		private const string AttachmentDownloadsFile = "AttachmentDownloads.cs";

		// 16 KB buffer used in the StreamCopy method
		// 16 KB seems to be the best break-even between performance and memory usage
		private const int BufferSize = 16384;

		private IHostV40 host;
		private string wiki;

		private string GetFullPath(string finalChunk) {
			return Path.Combine(Path.Combine(GetDataDirectory(host), wiki), finalChunk);
		}

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
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void Init(IHostV40 host, string config, string wiki) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			this.host = host;
			this.wiki = string.IsNullOrEmpty(wiki) ? "root" : wiki;

			// Create directories, if needed
			if(!Directory.Exists(GetFullPath(""))) {
				Directory.CreateDirectory(GetFullPath(""));
			}

			if(!Directory.Exists(GetFullPath(UploadDirectory))) {
				Directory.CreateDirectory(GetFullPath(UploadDirectory));
			}
			if(!Directory.Exists(GetFullPath(AttachmentsDirectory))) {
				Directory.CreateDirectory(GetFullPath(AttachmentsDirectory));
			}
			if(!File.Exists(GetFullPath(FileDownloadsFile))) {
				File.Create(GetFullPath(FileDownloadsFile)).Close();
			}
			if(!File.Exists(GetFullPath(AttachmentDownloadsFile))) {
				File.Create(GetFullPath(AttachmentDownloadsFile)).Close();
			}
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

			this.host = host;

			if(!LocalProvidersTools.CheckWritePermissions(GetDataDirectory(host))) {
				throw new InvalidConfigurationException("Cannot write into the public directory - check permissions");
			}
		}

		void IDisposable.Dispose() {
			// Nothing to do
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return info; }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml {
			get { return null; }
		}

		/// <summary>
		/// Gets a value specifying whether the provider is read-only, i.e. it can only provide data and not store it.
		/// </summary>
		public bool ReadOnly {
			get { return false; }
		}

		/// <summary>
		/// Checks the path.
		/// </summary>
		/// <param name="path">The path to be checked.</param>
		/// <param name="begin">The expected beginning of the path.</param>
		/// <exception cref="InvalidOperationException">If <paramref name="path"/> does not begin with <paramref name="begin"/> or contains "\.." or "..\".</exception>
		private string CheckPath(string path, string begin) {
			if(!path.StartsWith(begin) || path.Contains(Path.DirectorySeparatorChar + "..") || path.Contains(".." + Path.DirectorySeparatorChar))
				throw new InvalidOperationException();
			return path;
		}

		/// <summary>
		/// Builds a full path from a provider-specific partial path.
		/// </summary>
		/// <param name="partialPath">The partial path.</param>
		/// <returns>The full path.</returns>
		/// <remarks>For example: if <b>partialPath</b> is "/my/directory", the method returns 
		/// "C:\Inetpub\wwwroot\Wiki\public\Upload\my\directory", assuming the Wiki resides in "C:\Inetpub\wwwroot\Wiki".</remarks>
		private string BuildFullPath(string partialPath) {
			if(partialPath == null) partialPath = "";
			partialPath = partialPath.Replace("/", Path.DirectorySeparatorChar.ToString()).TrimStart(Path.DirectorySeparatorChar);
			string up = Path.Combine(GetDataDirectory(host), Path.Combine(wiki, UploadDirectory));			
			return CheckPath(Path.Combine(up, partialPath), up); // partialPath CANNOT start with "\" -> Path.Combine does not work
		}

		/// <summary>
		/// Builds a full path from a provider-specific partial path.
		/// </summary>
		/// <param name="partialPath">The partial path.</param>
		/// <returns>The full path.</returns>
		/// <remarks>For example: if <b>partialPath</b> is "/my/directory", the method returns 
		/// "C:\Inetpub\wwwroot\Wiki\public\Attachments\my\directory", assuming the Wiki resides in "C:\Inetpub\wwwroot\Wiki".</remarks>
		private string BuildFullPathForAttachments(string partialPath) {
			if(partialPath == null) partialPath = "";
			partialPath = partialPath.Replace("/", Path.DirectorySeparatorChar.ToString()).TrimStart(Path.DirectorySeparatorChar);
			string up = Path.Combine(GetDataDirectory(host), Path.Combine(wiki, AttachmentsDirectory));
			return CheckPath(Path.Combine(up, partialPath), up); // partialPath CANNOT start with "\" -> Path.Combine does not work
		}

		/// <summary>
		/// Lists the Files in the specified Directory.
		/// </summary>
		/// <param name="directory">The full directory name, for example "/my/directory". Null, empty or "/" for the root directory.</param>
		/// <returns>The list of Files in the directory.</returns>
		/// <exception cref="ArgumentException">If <paramref name="directory"/> does not exist.</exception>
		public string[] ListFiles(string directory) {
			string d = BuildFullPath(directory);
			if(!Directory.Exists(d)) throw new ArgumentException("Directory does not exist", "directory");

			string[] temp = Directory.GetFiles(d);
			
			// Result must be transformed in the form /my/dir/file.ext
			List<string> res = new List<string>(temp.Length);
			string root = GetFullPath(UploadDirectory);
			foreach(string s in temp) {
				// root = C:\blah\ - ends with '\'
				res.Add(s.Substring(root.Length - 1).Replace(Path.DirectorySeparatorChar, '/'));
			}

			return res.ToArray();
		}

		/// <summary>
		/// Lists the Directories in the specified directory.
		/// </summary>
		/// <param name="directory">The full directory name, for example "/my/directory". Null, empty or "/" for the root directory.</param>
		/// <returns>The list of Directories in the Directory.</returns>
		/// <exception cref="ArgumentException">If <paramref name="directory"/> does not exist.</exception>
		public string[] ListDirectories(string directory) {
			string d = BuildFullPath(directory);
			if(!Directory.Exists(d)) throw new ArgumentException("Directory does not exist", "directory");

			string[] temp = Directory.GetDirectories(d);

			// Result must be transformed in the form /my/dir
			List<string> res = new List<string>(temp.Length);
			string root = GetFullPath(UploadDirectory);
			foreach(string s in temp) {
				// root = C:\blah\ - ends with '\'
				res.Add(s.Substring(root.Length - 1).Replace(Path.DirectorySeparatorChar, '/') + "/");
			}

			return res.ToArray();
		}

		/// <summary>
		/// Copies data from a Stream to another.
		/// </summary>
		/// <param name="source">The Source stream.</param>
		/// <param name="destination">The destination Stream.</param>
		private static void StreamCopy(Stream source, Stream destination) {
			byte[] buff = new byte[BufferSize];
			int copied = 0;
			do {
				copied = source.Read(buff, 0, buff.Length);
				if(copied > 0) {
					destination.Write(buff, 0, copied);
				}
			} while(copied > 0);
		}

		/// <summary>
		/// Stores a file.
		/// </summary>
		/// <param name="fullName">The full name of the file.</param>
		/// <param name="sourceStream">A Stream object used as <b>source</b> of a byte stream, 
		/// i.e. the method reads from the Stream and stores the content properly.</param>
		/// <param name="overwrite"><c>true</c> to overwrite an existing file.</param>
		/// <returns><c>true</c> if the File is stored, <c>false</c> otherwise.</returns>
		/// <remarks>If <b>overwrite</b> is <c>false</c> and File already exists, the method returns <c>false</c>.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> or <paramref name="sourceStream"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty or <paramref name="sourceStream"/> does not support reading.</exception>
		public bool StoreFile(string fullName, Stream sourceStream, bool overwrite) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty", "fullName");
			if(sourceStream == null) throw new ArgumentNullException("sourceStream");
			if(!sourceStream.CanRead) throw new ArgumentException("Cannot read from Source Stream", "sourceStream");

			string filename = BuildFullPath(fullName);

			// Abort if the file already exists and overwrite is false
			if(File.Exists(filename) && !overwrite) return false;

			FileStream fs = null;

			bool done = false;

			try {
				fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);

				// StreamCopy content (throws exception in case of error)
				StreamCopy(sourceStream, fs);

				done = true;
			}
			catch(IOException) {
				done = false;
			}
			finally {
				try {
					fs.Close();
				}
				catch { }
			}

			return done;
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
		public bool RetrieveFile(string fullName, Stream destinationStream) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty", "fullName");
			if(destinationStream == null) throw new ArgumentNullException("destinationStream");
			if(!destinationStream.CanWrite) throw new ArgumentException("Cannot write into Destination Stream", "destinationStream");

			string filename = BuildFullPath(fullName);

			if(!File.Exists(filename)) throw new ArgumentException("File does not exist", "fullName");

			FileStream fs = null;

			bool done = false;

			try {
				fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);

				// StreamCopy content (throws exception in case of error)
				StreamCopy(fs, destinationStream);

				done = true;
			}
			catch(IOException) {
				done = false;
			}
			finally {
				try {
					fs.Close();
				}
				catch { }
			}

			return done;
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

			string n = BuildFullPath(fullName);

			if(!File.Exists(n)) return null;

			FileInfo fi = new FileInfo(n);

			return new FileDetails(fi.Length, fi.LastWriteTime);
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

			string n = BuildFullPath(fullName);

			if(!File.Exists(n)) throw new ArgumentException("File does not exist", "fullName");

			try {
				File.Delete(n);
				return true;
			}
			catch(IOException) {
				return false;
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

			string oldFilename = BuildFullPath(oldFullName);
			string newFilename = BuildFullPath(newFullName);

			if(!File.Exists(oldFilename)) throw new ArgumentException("Old File does not exist", "oldFullName");
			if(File.Exists(newFilename)) throw new ArgumentException("New File already exists", "newFullName");

			try {
				File.Move(oldFilename, newFilename);
				return true;
			}
			catch(IOException) {
				return false;
			}
		}

		/// <summary>
		/// Creates a new Directory.
		/// </summary>
		/// <param name="path">The path to create the new Directory in.</param>
		/// <param name="name">The name of the new Directory.</param>
		/// <returns><c>true</c> if the Directory is created, <c>false</c> otherwise.</returns>
		/// <remarks>If <b>path</b> is "/my/directory" and <b>name</b> is "newdir", a new directory named "/my/directory/newdir" is created.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="path"/> or <paramref name="name"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty or if the directory does not exist, or if the new directory already exists.</exception>
		public bool CreateDirectory(string path, string name) {
			if(path == null) throw new ArgumentNullException("path");
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			if(!Directory.Exists(BuildFullPath(path))) throw new ArgumentException("Directory does not exist", "path");

			string partialPath = path + (!path.EndsWith("/") ? "/" : "") + name;
			string d = BuildFullPath(partialPath);

			if(Directory.Exists(d)) throw new ArgumentException("Directory already exists", "name");

			try {
				Directory.CreateDirectory(d);
				return true;
			}
			catch(IOException) {
				return false;
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

			string d = BuildFullPath(fullPath);

			if(!Directory.Exists(d)) throw new ArgumentException("Directory does not exist", "fullPath");

			try {
				Directory.Delete(d, true);
				// Make sure tht fullPath ends with "/" so that the method does not clear wrong items
				if(!fullPath.EndsWith("/")) fullPath += "/";
				return true;
			}
			catch(IOException) {
				return false;
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

			string olddir = BuildFullPath(oldFullPath);
			string newdir = BuildFullPath(newFullPath);

			if(!Directory.Exists(olddir)) throw new ArgumentException("Directory does not exist", "oldFullPath");
			if(Directory.Exists(newdir)) throw new ArgumentException("Directory already exists", "newFullPath");

			try {
				Directory.Move(olddir, newdir);
				// Make sure that oldFullPath and newFullPath end with "/" so that the method does not rename wrong items
				if(!oldFullPath.EndsWith("/")) oldFullPath += "/";
				if(!newFullPath.EndsWith("/")) newFullPath += "/";
				return true;
			}
			catch(IOException) {
				return false;
			}
		}

		/// <summary>
		/// Gets the name of the Directory containing the Attachments of a Page.
		/// </summary>
		/// <param name="pageFullName">The page full name.</param>
		/// <returns>The name of the Directory (not the full path) that contains the Attachments of the specified Page.</returns>
		private string GetPageAttachmentDirectory(string pageFullName) {
			// Use the Hash to avoid problems with special chars and the like
			// Using the hash prevents GetPageWithAttachments to work
			//return Hash.Compute(pageInfo.FullName);

			return pageFullName;
		}

		/// <summary>
		/// The the names of the pages with attachments.
		/// </summary>
		/// <returns>The names of the pages with attachments.</returns>
		public string[] GetPagesWithAttachments() {
			string[] directories = Directory.GetDirectories(GetFullPath(AttachmentsDirectory));
			string[] result = new string[directories.Length];
			for(int i = 0; i < result.Length; i++) {
				result[i] = Path.GetFileName(directories[i]);
			}
			return result;
		}

		/// <summary>
		/// Returns the names of the Attachments of a Page.
		/// </summary>
		/// <param name="pageFullName">The full name of the page that owns the Attachments.</param>
		/// <returns>The names, or an empty list.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		public string[] ListPageAttachments(string pageFullName) {
			if(pageFullName == null) throw new ArgumentNullException("pageInfo");

			string dir = BuildFullPathForAttachments(GetPageAttachmentDirectory(pageFullName));

			if(!Directory.Exists(dir)) return new string[0];

			string[] files = Directory.GetFiles(dir);

			// Result must contain only the filename, not the full path
			List<string> result = new List<string>(files.Length);
			foreach(string f in files) {
				result.Add(Path.GetFileName(f));
			}
			return result.ToArray();
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
		public bool StorePageAttachment(string pageFullName, string name, Stream sourceStream, bool overwrite) {
			if(pageFullName == null) throw new ArgumentNullException("pageInfo");
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");
			if(sourceStream == null) throw new ArgumentNullException("sourceStream");
			if(!sourceStream.CanRead) throw new ArgumentException("Cannot read from Source Stream", "sourceStream");

			string filename = BuildFullPathForAttachments(GetPageAttachmentDirectory(pageFullName) + "/" + name);

			if(!Directory.Exists(Path.GetDirectoryName(filename))) {
				try {
					Directory.CreateDirectory(Path.GetDirectoryName(filename));
				}
				catch(IOException) {
					// Cannot create attachments dir
					return false;
				}
			}

			if(File.Exists(filename) && !overwrite) return false;

			FileStream fs = null;

			bool done = false;

			try {
				fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);

				// StreamCopy content (throws exception in case of error)
				StreamCopy(sourceStream, fs);

				done = true;
			}
			catch(IOException) {
				return false;
			}
			finally {
				try {
					fs.Close();
				}
				catch { }
			}

			return done;
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
		public bool RetrievePageAttachment(string pageFullName, string name, Stream destinationStream) {
			if(pageFullName == null) throw new ArgumentNullException("pageInfo");
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");
			if(destinationStream == null) throw new ArgumentNullException("destinationStream");
			if(!destinationStream.CanWrite) throw new ArgumentException("Cannot write into Destination Stream", "destinationStream");

			string d = GetPageAttachmentDirectory(pageFullName);
			if(!Directory.Exists(BuildFullPathForAttachments(d))) throw new ArgumentException("No attachments for Page", "pageInfo");

			string filename = BuildFullPathForAttachments(d + "/" + name);
			if(!File.Exists(filename)) throw new ArgumentException("Attachment does not exist", "name");

			FileStream fs = null;

			bool done = false;

			try {
				fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);

				// StreamCopy content (throws exception in case of error)
				StreamCopy(fs, destinationStream);

				done = true;
			}
			catch(IOException) {
				done = false;
			}
			finally {
				try {
					fs.Close();
				}
				catch { }
			}

			return done;
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

			string d = GetPageAttachmentDirectory(pageFullName);
			if(!Directory.Exists(BuildFullPathForAttachments(d))) return null;

			string filename = BuildFullPathForAttachments(d + "/" + attachmentName);
			if(!File.Exists(filename)) return null;

			FileInfo fi = new FileInfo(filename);

			return new FileDetails(fi.Length, fi.LastWriteTime);
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
			if(pageFullName == null) throw new ArgumentNullException("pageInfo");
			if(attachmentName == null) throw new ArgumentNullException("name");
			if(attachmentName.Length == 0) throw new ArgumentException("Name cannot be empty");

			string d = GetPageAttachmentDirectory(pageFullName);
			if(!Directory.Exists(BuildFullPathForAttachments(d))) throw new ArgumentException("Page does not exist", "pageInfo");

			string filename = BuildFullPathForAttachments(d + "/" + attachmentName);
			if(!File.Exists(filename)) throw new ArgumentException("Attachment does not exist", "name");

			try {
				File.Delete(filename);
				return true;
			}
			catch(IOException) {
				return false;
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
			if(pageFullName == null) throw new ArgumentNullException("pageInfo");
			if(oldName == null) throw new ArgumentNullException("oldName");
			if(oldName.Length == 0) throw new ArgumentException("Old Name cannot be empty", "oldName");
			if(newName == null) throw new ArgumentNullException("newName");
			if(newName.Length == 0) throw new ArgumentException("New Name cannot be empty", "newName");

			string d = GetPageAttachmentDirectory(pageFullName);
			if(!Directory.Exists(BuildFullPathForAttachments(d))) throw new ArgumentException("Page does not exist", "pageInfo");

			string oldFilename = BuildFullPathForAttachments(d + "/" + oldName);
			if(!File.Exists(oldFilename)) throw new ArgumentException("Attachment does not exist", "oldName");

			string newFilename = BuildFullPathForAttachments(d + "/" + newName);
			if(File.Exists(newFilename)) throw new ArgumentException("Attachment already exists", "newName");

			try {
				File.Move(oldFilename, newFilename);
				return true;
			}
			catch(IOException) {
				return false;
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
			if(oldPageFullName == null) throw new ArgumentNullException("oldPage");
			if(newPageFullName == null) throw new ArgumentNullException("newPage");

			string oldName = GetPageAttachmentDirectory(oldPageFullName);
			string newName = GetPageAttachmentDirectory(newPageFullName);

			string oldDir = BuildFullPathForAttachments(oldName);
			string newDir = BuildFullPathForAttachments(newName);

			if(!Directory.Exists(oldDir)) return; // Nothing to do
			if(Directory.Exists(newDir)) throw new ArgumentException("New Page already exists", "newPage");

			try {
				Directory.Move(oldDir, newDir);
			}
			catch(IOException) { }
		}

	}

}
