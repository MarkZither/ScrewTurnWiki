
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class MasterPage : System.Web.UI.MasterPage {

		private string currentNamespace = null;
		private PageInfo currentPage = null;

		protected void Page_Load(object sender, EventArgs e) {
			// Try to detect current namespace and page
			currentNamespace = Tools.DetectCurrentNamespace();
			currentPage = Tools.DetectCurrentPageInfo(true);

			lblStrings.Text = string.Format("<script type=\"text/javascript\">\r\n<!--\r\n__BaseName = \"{0}\";\r\n__ConfirmMessage = \"{1}\";\r\n// -->\r\n</script>",
				CphMaster.ClientID + "_", Properties.Messages.ConfirmOperation);

			PrintHtmlHead();
			PrintHeader();
			PrintSidebar();
			PrintFooter();
			PrintPageHeaderAndFooter();
		}

		/// <summary>
		/// Prints the page header and page footer.
		/// </summary>
		public void PrintPageHeaderAndFooter() {
			string h = Settings.Provider.GetMetaDataItem(MetaDataItem.PageHeader, currentNamespace);
			h = @"<div id=""PageInternalHeaderDiv"">" + FormattingPipeline.FormatWithPhase1And2(h, false, FormattingContext.PageHeader, currentPage) + "</div>";

			lblPageHeaderDiv.Text = FormattingPipeline.FormatWithPhase3(h, FormattingContext.PageHeader, currentPage);

			h = Settings.Provider.GetMetaDataItem(MetaDataItem.PageFooter, currentNamespace);
			h = @"<div id=""PageInternalFooterDiv"">" + FormattingPipeline.FormatWithPhase1And2(h, false, FormattingContext.PageFooter, currentPage) + "</div>";

			lblPageFooterDiv.Text = FormattingPipeline.FormatWithPhase3(h, FormattingContext.PageFooter, currentPage);
		}

		/// <summary>
		/// Prints the HTML head tag.
		/// </summary>
		public void PrintHtmlHead() {
			StringBuilder sb = new StringBuilder(100);

			if(Settings.RssFeedsMode != RssFeedsMode.Disabled) {
				sb.AppendFormat(@"<link rel=""alternate"" title=""{0}"" href=""{1}######______NAMESPACE______######RSS.aspx"" type=""application/rss+xml"" />",
					Settings.WikiTitle, Settings.MainUrl);
				sb.Append("\n");
				sb.AppendFormat(@"<link rel=""alternate"" title=""{0}"" href=""{1}######______NAMESPACE______######RSS.aspx?Discuss=1"" type=""application/rss+xml"" />",
					Settings.WikiTitle + " - Discussions", Settings.MainUrl);
				sb.Append("\n");
			}

			sb.Append("######______INCLUDES______######");


			// Use a Control to allow 3rd party plugins to programmatically access the Page header
			string nspace = currentNamespace;
			if(nspace == null) nspace = "";
			else if(nspace.Length > 0) nspace += ".";

			Literal c = new Literal();
			c.Text = sb.ToString().Replace("######______INCLUDES______######", Tools.GetIncludes(currentNamespace)).Replace("######______NAMESPACE______######", nspace);
			Page.Header.Controls.Add(c);
		}

		/// <summary>
		/// Prints the header.
		/// </summary>
		public void PrintHeader() {
			string h = FormattingPipeline.FormatWithPhase1And2(Settings.Provider.GetMetaDataItem(MetaDataItem.Header, currentNamespace),
				false, FormattingContext.Header, currentPage);

			lblHeaderDiv.Text = FormattingPipeline.FormatWithPhase3(h, FormattingContext.Header, currentPage);
		}

		/// <summary>
		/// Prints the sidebar.
		/// </summary>
		public void PrintSidebar() {
			string s = FormattingPipeline.FormatWithPhase1And2(Settings.Provider.GetMetaDataItem(MetaDataItem.Sidebar, currentNamespace),
				false, FormattingContext.Sidebar, currentPage);

			lblSidebarDiv.Text = FormattingPipeline.FormatWithPhase3(s, FormattingContext.Sidebar, currentPage);
		}

		/// <summary>
		/// Prints the footer.
		/// </summary>
		public void PrintFooter() {
			string f = FormattingPipeline.FormatWithPhase1And2(Settings.Provider.GetMetaDataItem(MetaDataItem.Footer, currentNamespace),
				false, FormattingContext.Footer, currentPage);

			lblFooterDiv.Text = FormattingPipeline.FormatWithPhase3(f, FormattingContext.Footer, currentPage);
		}

	}

}
