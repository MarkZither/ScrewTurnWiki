
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Manages information about Page Redirections.
	/// </summary>
	public static class Redirections {

		/// <summary>
		/// Adds a new Redirection.
		/// </summary>
		/// <param name="sourcePageFullName">The source Page full name.</param>
		/// <param name="destinationPageFullName">The destination Page full name.</param>
		/// <returns>True if the Redirection is added, false otherwise.</returns>
		/// <remarks>The method prevents circular and multi-level redirection.</remarks>
		public static void AddRedirection(string sourcePageFullName, string destinationPageFullName) {
			if(sourcePageFullName == null) throw new ArgumentNullException("source");
			if(destinationPageFullName == null) throw new ArgumentNullException("destination");

		}

		/// <summary>
		/// Gets the destination Page.
		/// </summary>
		/// <param name="pageFullName">The source Page full name.</param>
		/// <returns>The destination Page, or null.</returns>
		public static PageContent GetDestination(string pageFullName) {
			if(pageFullName == null) throw new ArgumentNullException("page");

			return null;
		}

		/// <summary>
		/// Removes any occurrence of a Page from the redirection table, both on sources and destinations.
		/// </summary>
		/// <param name="pageFullName">The full name of the page to wipe-out.</param>
		/// <remarks>This method is useful when removing a Page.</remarks>
		public static void WipePageOut(string pageFullName) {
			if(pageFullName == null) throw new ArgumentNullException("page");
		}

		/// <summary>
		/// Clears the Redirection table.
		/// </summary>
		public static void Clear() { }

	}

}
