
using System;
using System.Configuration;
using System.Web;
using System.Collections.Generic;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Contains the Contents.
	/// </summary>
	public static class Content {

		/// <summary>
		/// Reads the Content of a Page.
		/// </summary>
		/// <param name="pageInfo">The Page.</param>
		/// <returns>The Page Content.</returns>
		public static PageContent GetPageContent(PageInfo pageInfo) {
			PageContent	result = pageInfo.Provider.GetContent(pageInfo);

			// result should NEVER be null
			if(result == null) {
				Log.LogEntry("PageContent could not be retrieved for page " + pageInfo.FullName + " - returning empty", EntryType.Error, Log.SystemUsername, pageInfo.Provider.CurrentWiki);
				result = PageContent.GetEmpty(pageInfo);
			}

			return result;
		}

		/// <summary>
		/// Gets the formatted Page Content, properly handling content caching and the Formatting Pipeline.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="page">The Page to get the formatted Content of.</param>
		/// <returns>The formatted content.</returns>
		public static string GetFormattedPageContent(string wiki, PageInfo page) {
			PageContent pg = GetPageContent(page);
			string[] linkedPages;
			string content = FormattingPipeline.FormatWithPhase1And2(wiki, pg.Content, false, FormattingContext.PageContent, page, out linkedPages);
			pg.LinkedPages = linkedPages;
			return FormattingPipeline.FormatWithPhase3(wiki, content, FormattingContext.PageContent, page);
		}

		/// <summary>
		/// Invalidates the cached Content of a Page.
		/// </summary>
		/// <param name="pageInfo">The Page to invalidate the cached content of.</param>
		public static void InvalidatePage(PageInfo pageInfo) {
			Redirections.WipePageOut(pageInfo);
		}

		/// <summary>
		/// Invalidates all the cache Contents.
		/// </summary>
		public static void InvalidateAllPages() {
			Redirections.Clear();
		}

	}

}
