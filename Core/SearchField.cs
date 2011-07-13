
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScrewTurn.Wiki {
	/// <summary>
	/// Lists legal values for search fields.
	/// </summary>
	public enum SearchField {
		/// <summary>
		/// The id of a page.
		/// </summary>
		/// <remarks>Composed by wiki|pagefullname</remarks>
		Id,
		/// <summary>
		/// The wiki of a page.
		/// </summary>
		Wiki,
		/// <summary>
		/// The full name of a page.
		/// </summary>
		PageFullName,
		/// <summary>
		/// The title of a page.
		/// </summary>
		PageTitle,
		/// <summary>
		/// The content of a page.
		/// </summary>
		PageContent
	}

	/// <summary>
	/// Extensions methods for <see cref="SearchField"/>.
	/// </summary>
	public static class SearchFieldExtensions {

		/// <summary>
		/// Convert a <see cref="SearchField"/> into the corresponding string.
		/// </summary>
		/// <param name="field">The field.</param>
		/// <returns></returns>
		public static string AsString(this SearchField field) {
			switch(field) {
				case SearchField.Id:
					return "Id";
				case SearchField.Wiki:
					return "Wiki";
				case SearchField.PageFullName:
					return "PageFullName";
				case SearchField.PageTitle:
					return "Title";
				case SearchField.PageContent:
					return "Content";
				default:
					throw new ArgumentException("The given SearchField is not valid");
			}
		}

	}
}