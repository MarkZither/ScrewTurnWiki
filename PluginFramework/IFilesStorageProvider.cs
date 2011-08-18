
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ScrewTurn.Wiki.PluginFramework {

	/// <summary>
	/// It is the interface that must be implemented in order to create a custom Files Storage Provider for ScrewTurn Wiki.
	/// </summary>
	/// <remarks>A class that implements this interface <b>should not</b> have any kind of data caching. 
	/// All directory paths are specified in a UNIX-like fashion, for example "/my/directory/myfile.jpg". 
	/// All paths must start with '/'. All Directory paths must end with '/'.
	/// All paths are case-insensitive.</remarks>
	public interface IFilesStorageProviderV40 : IStorageProviderV40 {

		/// <summary>
		/// Lists the Files in the specified Directory.
		/// </summary>
		/// <param name="directory">The full directory name, for example "/my/directory". Null, empty or "/" for the root directory.</param>
		/// <returns>The list of Files in the directory.</returns>
		/// <exception cref="ArgumentException">If <paramref name="directory"/> does not exist.</exception>
		string[] ListFiles(string directory);

		/// <summary>
		/// Lists the Directories in the specified directory.
		/// </summary>
		/// <param name="directory">The full directory name, for example "/my/directory". Null, empty or "/" for the root directory.</param>
		/// <returns>The list of Directories in the Directory.</returns>
		/// <exception cref="ArgumentException">If <paramref name="directory"/> does not exist.</exception>
		string[] ListDirectories(string directory);

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
		bool StoreFile(string fullName, Stream sourceStream, bool overwrite);

		/// <summary>
		/// Retrieves a File.
		/// </summary>
		/// <param name="fullName">The full name of the File.</param>
		/// <param name="destinationStream">A Stream object used as <b>destination</b> of a byte stream, 
		/// i.e. the method writes to the Stream the file content.</param>
		/// <returns><c>true</c> if the file is retrieved, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> or <paramref name="destinationStream"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty or <paramref name="destinationStream"/> does not support writing, or if <paramref name="fullName"/> does not exist.</exception>
		bool RetrieveFile(string fullName, Stream destinationStream);

		/// <summary>
		/// Gets the details of a file.
		/// </summary>
		/// <param name="fullName">The full name of the file.</param>
		/// <returns>The details, or <c>null</c> if the file does not exist.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		FileDetails GetFileDetails(string fullName);

		/// <summary>
		/// Deletes a File.
		/// </summary>
		/// <param name="fullName">The full name of the File.</param>
		/// <returns><c>true</c> if the File is deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty or it does not exist.</exception>
		bool DeleteFile(string fullName);

		/// <summary>
		/// Renames or moves a File.
		/// </summary>
		/// <param name="oldFullName">The old full name of the File.</param>
		/// <param name="newFullName">The new full name of the File.</param>
		/// <returns><c>true</c> if the File is renamed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="oldFullName"/> or <paramref name="newFullName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="oldFullName"/> or <paramref name="newFullName"/> are empty, or if the old file does not exist, or if the new file already exist.</exception>
		bool RenameFile(string oldFullName, string newFullName);

		/// <summary>
		/// Creates a new Directory.
		/// </summary>
		/// <param name="path">The path to create the new Directory in.</param>
		/// <param name="name">The name of the new Directory.</param>
		/// <returns><c>true</c> if the Directory is created, <c>false</c> otherwise.</returns>
		/// <remarks>If <b>path</b> is "/my/directory" and <b>name</b> is "newdir", a new directory named "/my/directory/newdir" is created.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="path"/> or <paramref name="name"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty or if the directory does not exist, or if the new directory already exists.</exception>
		bool CreateDirectory(string path, string name);

		/// <summary>
		/// Deletes a Directory and <b>all of its content</b>.
		/// </summary>
		/// <param name="fullPath">The full path of the Directory.</param>
		/// <returns><c>true</c> if the Directory is delete, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullPath"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullPath"/> is empty or if it equals '/' or it does not exist.</exception>
		bool DeleteDirectory(string fullPath);

		/// <summary>
		/// Renames or moves a Directory.
		/// </summary>
		/// <param name="oldFullPath">The old full path of the Directory.</param>
		/// <param name="newFullPath">The new full path of the Directory.</param>
		/// <returns><c>true</c> if the Directory is renamed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="oldFullPath"/> or <paramref name="newFullPath"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="oldFullPath"/> or <paramref name="newFullPath"/> are empty or equal to '/', 
		/// or if the old directory does not exist or the new directory already exists.</exception>
		bool RenameDirectory(string oldFullPath, string newFullPath);

		/// <summary>
		/// The names of the pages with attachments.
		/// </summary>
		/// <returns>The names of the pages with attachments.</returns>
		string[] GetPagesWithAttachments();
		
		/// <summary>
		/// Returns the names of the Attachments of a Page.
		/// </summary>
		/// <param name="pageFullName">The full name of the page that owns the Attachments.</param>
		/// <returns>The names, or an empty list.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		string[] ListPageAttachments(string pageFullName);

		/// <summary>
		/// Stores a Page Attachment.
		/// </summary>
		/// <param name="pageFullName">The Page Info that owns the Attachment.</param>
		/// <param name="name">The name of the Attachment, for example "myfile.jpg".</param>
		/// <param name="sourceStream">A Stream object used as <b>source</b> of a byte stream, 
		/// i.e. the method reads from the Stream and stores the content properly.</param>
		/// <param name="overwrite"><c>true</c> to overwrite an existing Attachment.</param>
		/// <returns><c>true</c> if the Attachment is stored, <c>false</c> otherwise.</returns>
		/// <remarks>If <b>overwrite</b> is <c>false</c> and Attachment already exists, the method returns <c>false</c>.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/>, <paramref name="name"/> or <paramref name="sourceStream"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/>, <paramref name="name"/> are empty or if <paramref name="sourceStream"/> does not support reading.</exception>
		bool StorePageAttachment(string pageFullName, string name, Stream sourceStream, bool overwrite);

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
		bool RetrievePageAttachment(string pageFullName, string name, Stream destinationStream);

		/// <summary>
		/// Gets the details of a page attachment.
		/// </summary>
		/// <param name="pageFullName">The full name of the page that owns the attachment.</param>
		/// <param name="attachmentName">The name of the attachment, for example "myfile.jpg".</param>
		/// <returns>The details of the attachment, or <c>null</c> if the attachment does not exist.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> or <paramref name="attachmentName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> or <paramref name="attachmentName"/> are empty.</exception>
		FileDetails GetPageAttachmentDetails(string pageFullName, string attachmentName);

		/// <summary>
		/// Deletes a Page Attachment.
		/// </summary>
		/// <param name="pageFullName">The Page Info that owns the Attachment.</param>
		/// <param name="attachmentName">The name of the Attachment, for example "myfile.jpg".</param>
		/// <returns><c>true</c> if the Attachment is deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> or <paramref name="attachmentName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> or <paramref name="attachmentName"/> are empty or if the page or attachment do not exist.</exception>
		bool DeletePageAttachment(string pageFullName, string attachmentName);

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
		bool RenamePageAttachment(string pageFullName, string oldName, string newName);

		/// <summary>
		/// Notifies the Provider that a Page has been renamed.
		/// </summary>
		/// <param name="oldPageFullName">The old page full name.</param>
		/// <param name="newPageFullName">The new page full name.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="oldPageFullName"/> or <paramref name="newPageFullName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="oldPageFullName"/> or <paramref name="newPageFullName"/> are empty or if the new page full name is already in use.</exception>
		void NotifyPageRenaming(string oldPageFullName, string newPageFullName);

	}

}
