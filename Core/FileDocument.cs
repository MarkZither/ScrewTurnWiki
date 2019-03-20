
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.SearchEngine;

namespace ScrewTurn.Wiki
{

	/// <summary>
	/// Represents a file document.
	/// </summary>
	public class FileDocument : IDocument
	{

		/// <summary>
		/// The file name.
		/// </summary>
		public string FileName;

		/// <summary>
		/// The content of the file;
		/// </summary>
		public string FileContent;

		/// <summary>
		/// The content of the file with search query highlighted.
		/// </summary>
		public string HighlightedFileContent;
	}
}