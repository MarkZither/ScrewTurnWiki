
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
		/// The type of the indexed document.
		/// </summary>
		DocumentType,
		/// <summary>
		/// The wiki of a page.
		/// </summary>
		Wiki,
		/// <summary>
		/// The full name of a page.
		/// </summary>
		PageFullName,
		/// <summary>
		/// The title of a document.
		/// </summary>
		Title,
		/// <summary>
		/// The content of a document.
		/// </summary>
		Content,
		/// <summary>
		/// The id of the message.
		/// </summary>
		MessageId,
		/// <summary>
		/// The DateTime of the message.
		/// </summary>
		MessageDateTime
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
				case SearchField.DocumentType:
					return "DocumentType";
				case SearchField.Wiki:
					return "Wiki";
				case SearchField.PageFullName:
					return "PageFullName";
				case SearchField.Title:
					return "Title";
				case SearchField.Content:
					return "Content";
				case SearchField.MessageId:
					return "MessageId";
				case SearchField.MessageDateTime:
					return "MessageDateTime";
				default:
					throw new ArgumentException("The given SearchField is not valid");
			}
		}

	}
}