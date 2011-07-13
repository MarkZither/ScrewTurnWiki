
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
		/// The wiki to witch belongs the found document.
		/// </summary>
		public string Wiki;

		/// <summary>
		/// The full name of the foud document.
		/// </summary>
		public string PageFullName;

		/// <summary>
		/// The title of the found document.
		/// </summary>
		public string Title;

		/// <summary>
		/// The content of the found document.
		/// </summary>
		public string Content;

	}
}
