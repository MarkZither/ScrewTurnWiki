
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Represents a result returned by a search query.
	/// </summary>
	public class SearchResult {

		/// <summary>
		/// The type of the found document.
		/// </summary>
		public DocumentType DocumentType;

		/// <summary>
		/// The found document object.
		/// </summary>
		public IDocument Document;

		/// <summary>
		/// The relevance of the search result.
		/// </summary>
		public float Relevance;

	}
}
