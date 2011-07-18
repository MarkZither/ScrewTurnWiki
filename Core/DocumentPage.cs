
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// An implementation of IDocument for a page.
	/// </summary>
	public class DocumentPage : IDocument {

		/// <summary>
		/// The wiki.
		/// </summary>
		public string Wiki;

		/// <summary>
		/// The full name of the page.
		/// </summary>
		public string PageFullName;

		/// <summary>
		/// The title of the page with search query highlighted.
		/// </summary>
		public string HighlightedTitle;

		/// <summary>
		/// The content of the page with search query highlighted.
		/// </summary>
		public string HighlightedContent;

	}

}
