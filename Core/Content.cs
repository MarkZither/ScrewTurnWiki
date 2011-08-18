
using System;
using System.Configuration;
using System.Web;
using System.Collections.Generic;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Contains the Contents.
	/// </summary>
	public static class FormattedContent {

		/// <summary>
		/// Gets the formatted Page Content, properly handling content caching and the Formatting Pipeline.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="page">The Page to get the formatted Content of.</param>
		/// <returns>The formatted content.</returns>
		public static string GetFormattedPageContent(string wiki, PageContent page) {
			string[] linkedPages;
			string content = FormattingPipeline.FormatWithPhase1And2(wiki, page.Content, false, FormattingContext.PageContent, page.FullName, out linkedPages);
			page.LinkedPages = linkedPages;
			return FormattingPipeline.FormatWithPhase3(wiki, content, FormattingContext.PageContent, page.FullName);
		}

	}

}
