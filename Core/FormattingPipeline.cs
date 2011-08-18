
using System;
using System.Collections.Generic;
using ScrewTurn.Wiki.PluginFramework;
using System.Web;
using System.Threading;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Contains methods for formatting content using a pipeline paradigm.
	/// </summary>
	public static class FormattingPipeline {

		/// <summary>
		/// Gets the formatter providers list sorted by priority.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <returns>The list.</returns>
		private static IList<IFormatterProviderV40> GetSortedFormatters(string wiki) {
			List<IFormatterProviderV40> providers = new List<IFormatterProviderV40>(Collectors.CollectorsBox.FormatterProviderCollector.GetAllProviders(wiki));

			// Sort by priority, then by name
			providers.Sort((x, y) => {
				int preliminaryResult = x.ExecutionPriority.CompareTo(y.ExecutionPriority);
				if(preliminaryResult != 0) return preliminaryResult;
				else return x.Information.Name.CompareTo(y.Information.Name);
			});

			return providers;
		}

		/// <summary>
		/// Performs the Phases 1 and 2 of the formatting process.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="raw">The raw WikiMarkup to format.</param>
		/// <param name="forIndexing">A value indicating whether the formatting is being done for content indexing.</param>
		/// <param name="context">The formatting context.</param>
		/// <param name="currentPageFullName">The full name of the current Page, if any.</param>
		/// <returns>The formatted content.</returns>
		public static string FormatWithPhase1And2(string wiki, string raw, bool forIndexing, FormattingContext context, string currentPageFullName) {
			string[] tempLinks;
			return FormatWithPhase1And2(wiki, raw, forIndexing, context, currentPageFullName, out tempLinks);
		}

		/// <summary>
		/// Performs the Phases 1 and 2 of the formatting process.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="raw">The raw WikiMarkup to format.</param>
		/// <param name="forIndexing">A value indicating whether the formatting is being done for content indexing.</param>
		/// <param name="context">The formatting context.</param>
		/// <param name="currentPageFullName">The full name of the current Page, if any.</param>
		/// <param name="linkedPages">The Pages linked by the current Page.</param>
		/// <returns>The formatted content.</returns>
		public static string FormatWithPhase1And2(string wiki, string raw, bool forIndexing, FormattingContext context, string currentPageFullName, out string[] linkedPages) {
			ContextInformation info = null;
			string username = SessionFacade.CurrentUsername;
			info = new ContextInformation(forIndexing, false, context, currentPageFullName, System.Threading.Thread.CurrentThread.CurrentCulture.Name, HttpContext.Current,
				username, SessionFacade.GetCurrentGroupNames(wiki));

			IList<IFormatterProviderV40> providers = GetSortedFormatters(wiki);

			// Phase 1
			foreach(IFormatterProviderV40 provider in providers) {
				if(provider.PerformPhase1) {
					try {
						raw = provider.Format(raw, info, FormattingPhase.Phase1);
					}
					catch(Exception ex) {
						if(!(ex is ThreadAbortException)) { // Consider Response.End()
							Log.LogEntry("Provider " + provider.Information.Name + " failed to perform Phase1 (silently resuming from next provider): " + ex.ToString(), EntryType.Error, Log.SystemUsername, provider.CurrentWiki);
						}
					}
				}
			}

			raw = Formatter.Format(wiki, raw, forIndexing, context, currentPageFullName, out linkedPages);

			// Phase 2
			foreach(IFormatterProviderV40 provider in providers) {
				if(provider.PerformPhase2) {
					try {
						raw = provider.Format(raw, info, FormattingPhase.Phase2);
					}
					catch(Exception ex) {
						if(!(ex is ThreadAbortException)) { // Consider Response.End()
							Log.LogEntry("Provider " + provider.Information.Name + " failed to perform Phase2 (silently resuming from next provider): " + ex.ToString(), EntryType.Error, Log.SystemUsername, provider.CurrentWiki);
						}
					}
				}
			}

			return raw;
		}

		/// <summary>
		/// Performs the Phase 3 of the formatting process.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="raw">The raw WikiMarkup to format.</param>
		/// <param name="context">The formatting context.</param>
		/// <param name="currentPageFullName">The full name of the current Page, if any.</param>
		/// <returns>The formatted content.</returns>
		public static string FormatWithPhase3(string wiki, string raw, FormattingContext context, string currentPageFullName) {
			raw = Formatter.FormatPhase3(wiki, raw, context, currentPageFullName);

			ContextInformation info = null;
			string username = SessionFacade.CurrentUsername;
			info = new ContextInformation(false, false, context, currentPageFullName, System.Threading.Thread.CurrentThread.CurrentCulture.Name, HttpContext.Current,
				username, SessionFacade.GetCurrentGroupNames(wiki));

			// Phase 3
			foreach(IFormatterProviderV40 provider in GetSortedFormatters(wiki)) {
				if(provider.PerformPhase3) {
					try {
						raw = provider.Format(raw, info, FormattingPhase.Phase3);
					}
					catch(Exception ex) {
						if(!(ex is ThreadAbortException)) { // Consider Response.End()
							Log.LogEntry("Provider " + provider.Information.Name + " failed to perform Phase3 (silently resuming from next provider): " + ex.ToString(), EntryType.Error, Log.SystemUsername, provider.CurrentWiki);
						}
					}
				}
			}

			return raw;
		}

		/// <summary>
		/// Prepares the title of an item for display.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="title">The input title.</param>
		/// <param name="forIndexing">A value indicating whether the formatting is being done for content indexing.</param>
		/// <param name="context">The context information.</param>
		/// <param name="currentPageFullName">The full name of the current page, if any.</param>
		/// <returns>The prepared title, properly sanitized.</returns>
		public static string PrepareTitle(string wiki, string title, bool forIndexing, FormattingContext context, string currentPageFullName) {
			string temp = title;
			ContextInformation info = new ContextInformation(forIndexing, false, context, currentPageFullName, System.Threading.Thread.CurrentThread.CurrentCulture.Name,
				HttpContext.Current, SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(wiki));

			foreach(IFormatterProviderV40 prov in GetSortedFormatters(wiki)) {
				temp = prov.PrepareTitle(temp, info);
			}

			return PrepareItemTitle(temp);
		}

		/// <summary>
		/// Prepares the title of an item for safe display.
		/// </summary>
		/// <param name="title">The title.</param>
		/// <returns>The sanitized title.</returns>
		private static string PrepareItemTitle(string title) {
			return Formatter.StripHtml(title)
				.Replace("\"", "&quot;")
				.Replace("'", "&#39;")
				.Replace("<", "&lt;").Replace(">", "&gt;")
				.Replace("[", "&#91;").Replace("]", "&#93;"); // This avoid endless loops in Formatter
		}

	}

}
