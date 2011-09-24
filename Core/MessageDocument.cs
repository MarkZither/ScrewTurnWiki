
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// An implementation of IDocument for a message.
	/// </summary>
	public class MessageDocument : IDocument {

		/// <summary>
		/// The wiki.
		/// </summary>
		public string Wiki;

		/// <summary>
		/// The message page full name.
		/// </summary>
		public string PageFullName;

		/// <summary>
		/// The subject of the message.
		/// </summary>
		public string Subject;

		/// <summary>
		/// The body of the message
		/// </summary>
		public string Body;

		/// <summary>
		/// The body of the message with search query highlighted.
		/// </summary>
		public string HighlightedBody;

		/// <summary>
		/// The DateTime of the message.
		/// </summary>
		public DateTime DateTime;
	}

}
