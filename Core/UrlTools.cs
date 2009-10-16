
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Text;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements useful URL-handling tools.
	/// </summary>
	public static class UrlTools {

		/// <summary>
		/// Properly routes the current virtual request to a physical ASP.NET page.
		/// </summary>
		public static void RouteCurrentRequest() {
			// Extract the physical page name, e.g. MainPage, Edit or Category
			string pageName = Path.GetFileNameWithoutExtension(HttpContext.Current.Request.PhysicalPath);
			// Exctract the extension, e.g. .ashx or .aspx
			string ext = Path.GetExtension(HttpContext.Current.Request.PhysicalPath).ToLowerInvariant();
			// Remove trailing dot, .ashx -> ashx
			if(ext.Length > 0) ext = ext.Substring(1);

			// IIS7+Integrated Pipeline handles all requests through the ASP.NET engine
			// All non-interesting files are not processed, such as GIF, CSS, etc.
			if(ext != "ashx" && ext != "aspx") return;

			// Extract the current namespace, if any
			string nspace = GetCurrentNamespace();
			if(!string.IsNullOrEmpty(nspace)) pageName = pageName.Substring(nspace.Length + 1); // Trim Namespace. from pageName

			//HttpContext.Current.Response.Filter =
			//	new ScrewTurn.Wiki.RelativeUrlFilterStream(HttpContext.Current.Response.Filter, nspace);

			string queryString = ""; // Empty or begins with ampersand, not question mark
			try {
				// This might throw exceptions if 3rd-party modules interfer with the request pipeline
				queryString = HttpContext.Current.Request.Url.Query.Replace("?", "&");
			}
			catch { }

			if(ext.Equals("ashx")) {
				// Content page requested, process it via Default.aspx
				if(!queryString.Contains("NS=")) {
					HttpContext.Current.RewritePath("~/Default.aspx?Page=" + Tools.UrlEncode(pageName) + "&NS=" + Tools.UrlEncode(nspace) + queryString);
				}
				else {
					HttpContext.Current.RewritePath("~/Default.aspx?Page=" + Tools.UrlEncode(pageName) + queryString);
				}
			}
			else if(ext.Equals("aspx")) {
				// System page requested, redirect to the root of the application
				// For example: http://www.server.com/Namespace.Edit.aspx?Page=MainPage -> http://www.server.com/Edit.aspx?Page=MainPage&NS=Namespace
				if(!string.IsNullOrEmpty(nspace)) { // Needed to avoid infinite loops
					if(!queryString.Contains("NS=")) {
						HttpContext.Current.RewritePath("~/" + Tools.UrlEncode(pageName) + "." + ext + "?NS=" + Tools.UrlEncode(nspace) + queryString);
					}
					else {
						if(queryString.Length > 1) queryString = "?" + queryString.Substring(1);
						HttpContext.Current.RewritePath("~/" + Tools.UrlEncode(pageName) + "." + ext + queryString);
					}
				}
			}
			// else nothing to do
		}

		/// <summary>
		/// Extracts the current namespace from the URL, such as <i>/App/Namespace.Edit.aspx</i>.
		/// </summary>
		/// <returns>The current namespace, or an empty string. <c>null</c> if the URL format is not specified.</returns>
		public static string GetCurrentNamespace() {
			string filename = Path.GetFileNameWithoutExtension(HttpContext.Current.Request.Path); // e.g. MainPage or Edit or Namespace.MainPage or Namespace.Edit
			
			// Use dot to split the filename
			string[] fields = filename.Split('.');

			if(fields.Length != 1 && fields.Length != 2) return null; // Unrecognized format
			if(fields.Length == 1) return ""; // Just page name
			else return fields[0]; // Namespace.Page
		}

		/// <summary>
		/// Redirects the current response to the specified URL, properly appending the current namespace if any.
		/// </summary>
		/// <param name="target">The target URL.</param>
		public static void Redirect(string target) {
			Redirect(target, true);
		}

		/// <summary>
		/// Redirects the current response to the specified URL, appending the current namespace if requested.
		/// </summary>
		/// <param name="target">The target URL.</param>
		/// <param name="addNamespace">A value indicating whether to add the namespace.</param>
		public static void Redirect(string target, bool addNamespace) {
			string nspace = HttpContext.Current.Request["NS"];
			if(nspace == null || nspace.Length == 0 || !addNamespace) HttpContext.Current.Response.Redirect(target);
			else HttpContext.Current.Response.Redirect(target + (target.Contains("?") ? "&" : "?") + "NS=" + Tools.UrlEncode(nspace));
		}

		/// <summary>
		/// Builds a URL properly prepending the namespace to the URL.
		/// </summary>
		/// <param name="chunks">The chunks used to build the URL.</param>
		/// <returns>The complete URL.</returns>
		public static string BuildUrl(params string[] chunks) {
			if(chunks == null) throw new ArgumentNullException("chunks");
			if(chunks.Length == 0) return ""; // Shortcut

			StringBuilder temp = new StringBuilder(chunks.Length * 10);
			foreach(string chunk in chunks) {
				temp.Append(chunk);
			}

			string tempString = temp.ToString();

			if(tempString.StartsWith("++")) return tempString.Substring(2);

			string nspace = HttpContext.Current.Request["NS"];
			if(string.IsNullOrEmpty(nspace)) nspace = null;
			if(nspace == null) nspace = GetCurrentNamespace();
			if(string.IsNullOrEmpty(nspace)) nspace = null;

			if(nspace != null) {
				string tempStringLower = tempString.ToLowerInvariant();
				if((tempStringLower.Contains(".ashx") || tempStringLower.Contains(".aspx")) && !tempString.StartsWith(Tools.UrlEncode(nspace) + ".")) temp.Insert(0, nspace + ".");
			}

			return temp.ToString();
		}

		/// <summary>
		/// Builds a URL properly appendind the <b>NS</b> parameter if appropriate.
		/// </summary>
		/// <param name="destination">The destination <see cref="T:StringBuilder"/>.</param>
		/// <param name="chunks">The chunks to append.</param>
		public static void BuildUrl(StringBuilder destination, params string[] chunks) {
			if(destination == null) throw new ArgumentNullException("destination");

			destination.Append(BuildUrl(chunks));
		}

		/// <summary>
		/// Redirects to the default page of the current namespace.
		/// </summary>
		public static void RedirectHome() {
			Redirect(BuildUrl(Settings.DefaultPage, Settings.PageExtension));
		}

	}

}
