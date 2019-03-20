
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.SearchEngine;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki
{

	/// <summary>
	/// Represents a page attachment document.
	/// </summary>
	public class PageAttachmentDocument : IDocument
	{


		/// <summary>
		/// The page fullName.
		/// </summary>
		public string PageFullName;

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
