
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// An implementation of IDocument for a page attachment.
	/// </summary>
	public class PageAttachmentDocument :IDocument {

		/// <summary>
		/// The wiki.
		/// </summary>
		public string Wiki;

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
