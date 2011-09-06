
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using System.Web;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Manages navigation Breadcrumbs.
	/// </summary>
	public class BreadcrumbsManager {

		private const int MaxPages = 10;
		private const string CookieName = "ScrewTurnWikiBreadcrumbs3";
		private const string CookieValue = "B";

		private List<string> pages;
		private string wiki;

		/// <summary>
		/// Gets the cookie.
		/// </summary>
		/// <returns>The cookie, or <c>null</c>.</returns>
		private HttpCookie GetCookie() {
			if(HttpContext.Current.Request != null) {
				HttpCookie cookie = HttpContext.Current.Request.Cookies[CookieName];
				return cookie;
			}
			else return null;
		}

		/// <summary>
		/// Initializes a new instance of the <b>BreadcrumbsManager</b> class for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public BreadcrumbsManager(string wiki) {
			this.wiki = wiki;
			pages = new List<string>(MaxPages);

			HttpCookie cookie = GetCookie();
			if(cookie != null && !string.IsNullOrEmpty(cookie.Values[CookieValue + "|" + wiki])) {
				try {
					pages.AddRange(cookie.Values[CookieValue + "|" + wiki].Split('|'));
				}
				catch { }
			}
		}

		/// <summary>
		/// Updates the cookie.
		/// </summary>
		private void UpdateCookie() {
			HttpCookie cookie = GetCookie();
			if(cookie == null) {
				cookie = new HttpCookie(CookieName);
			}
			cookie.Path = GlobalSettings.CookiePath;

			StringBuilder sb = new StringBuilder(MaxPages * 20);

			sb.Append(string.Join("|", pages));

			cookie.Values[CookieValue + "|" + wiki] = sb.ToString();
			if(HttpContext.Current.Response != null) {
				HttpContext.Current.Response.Cookies.Set(cookie);
			}
			if(HttpContext.Current.Request != null) {
				HttpContext.Current.Request.Cookies.Set(cookie);
			}
		}

		/// <summary>
		/// Adds a Page to the Breadcrumbs trail.
		/// </summary>
		/// <param name="pageFullName">The full name of the page to add.</param>
		public void AddPage(string pageFullName) {
			lock(this) {
				int index = FindPage(pageFullName);
				if(index != -1) pages.RemoveAt(index);
				pages.Add(pageFullName);
				if(pages.Count > MaxPages) pages.RemoveRange(0, pages.Count - MaxPages);

				UpdateCookie();
			}
		}

		/// <summary>
		/// Finds a page by name.
		/// </summary>
		/// <param name="pageFullName">The page full name.</param>
		/// <returns>The index in the collection.</returns>
		private int FindPage(string pageFullName) {
			lock(this) {
				if(pages == null || pages.Count == 0) return -1;

				PageNameComparer comp = new PageNameComparer();
				for(int i = 0; i < pages.Count; i++) {
					if(pages[i] == pageFullName) return i;
				}

				return -1;
			}
		}

		/// <summary>
		/// Removes a Page from the Breadcrumbs trail.
		/// </summary>
		/// <param name="pageFullName">The full name of the page to remove.</param>
		public void RemovePage(string pageFullName) {
			lock(this) {
				int index = FindPage(pageFullName);
				if(index >= 0) pages.RemoveAt(index);

				UpdateCookie();
			}
		}

		/// <summary>
		/// Clears the Breadcrumbs trail.
		/// </summary>
		public void Clear() {
			lock(this) {
				pages.Clear();

				UpdateCookie();
			}
		}

		/// <summary>
		/// Gets all the Pages in the trail that still exist.
		/// </summary>
		public string[] GetAllPages() {
			lock(this) {
				return pages.ToArray();
			}
		}

	}

}
