
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ScrewTurn.Wiki.PluginFramework
{

	/// <summary>
	/// It is the interface that must be implemented in order to create a custom Files Storage Provider for ScrewTurn Wiki.
	/// </summary>
	/// <remarks>A class that implements this interface <b>should not</b> have any kind of data caching. 
	/// All directory paths are specified in a UNIX-like fashion, for example "/my/directory/myfile.jpg". 
	/// All paths must start with '/'. All Directory paths must end with '/'.
	/// All paths are case-insensitive.</remarks>
	public interface IVersionedFilesStorageProviderV30 : IStorageProviderV30
	{

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
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> os <paramref name="sourceStream"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty or <paramref name="sourceStream"/> does not support reading.</exception>
		bool StoreFile(string fullName, Stream sourceStream, bool overwrite);

		/// <summary>
		/// Retrieves a File.
		/// </summary>
		/// <param name="fullName">The full name of the File.</param>
		/// <param name="destinationStream">A Stream object used as <b>destination</b> of a byte stream, 
		/// i.e. the method writes to the Stream the file content.</param>
		/// <param name="countHit">A value indicating whether or not to count this retrieval in the statistics.</param>
		/// <returns><c>true</c> if the file is retrieved, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> os <paramref name="destinationStream"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty or <paramref name="destinationStream"/> does not support writing, or if <paramref name="fullName"/> does not exist.</exception>
		bool RetrieveFile(string fullName, Stream destinationStream, bool countHit);

		/// <summary>
		/// Sets the number of times a file was retrieved.
		/// </summary>
		/// <param name="fullName">The full name of the file.</param>
		/// <param name="count">The count to set.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/> is less than zero.</exception>
		void SetFileRetrievalCount(string fullName, int count);

		/// <summary>
		/// Gets the details of a file.
		/// </summary>
		/// <param name="fullName">The full name of the file.</param>
		/// <returns>The details, or <c>null</c> if the file does not exist.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		FileDetails GetFileDetails(string fullName);

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
		/// Does a full checkout of all repos.
		/// </summary>
		/// <returns><c>true</c> if the repo is retrieved, <c>false</c> otherwise.</returns>
		bool CheckoutRepos(string repoDir);

		/// <summary>
		/// Count of number of repos configured.
		/// </summary>
		/// <returns>Count of number of repos configured</returns>
		int CountRepos();
	}
}
