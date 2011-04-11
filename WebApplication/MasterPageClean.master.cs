﻿
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Text;

namespace ScrewTurn.Wiki {

	public partial class MasterPageClean : System.Web.UI.MasterPage {

		private string currentNamespaces = null;

		protected void Page_Load(object sender, EventArgs e) {
			// Try to detect current namespace
			currentNamespaces = Tools.DetectCurrentNamespace();

			lblStringsClean.Text = string.Format("<script type=\"text/javascript\">\r\n<!--\r\n__BaseName = \"{0}\";\r\n__ConfirmMessage = \"{1}\";\r\n// -->\r\n</script>", 
				CphMasterClean.ClientID + "_", Properties.Messages.ConfirmOperation);

			string nspace = currentNamespaces;
			if(string.IsNullOrEmpty(nspace)) nspace = "";
			else nspace += ".";

			PrintHtmlHead();
			PrintHeader();
			PrintFooter();
		}

		/// <summary>
		/// Prints the HTML head tag.
		/// </summary>
		public void PrintHtmlHead() {
			Literal c = new Literal();
			c.Text = Tools.GetIncludes(Tools.DetectCurrentNamespace());
			Page.Header.Controls.Add(c);
		}
		
		/// <summary>
		/// Prints the header.
		/// </summary>
		public void PrintHeader() {
			string h = FormattingPipeline.FormatWithPhase1And2("{wikititle}",
				false, FormattingContext.Header, null);

			lblHeaderDivClean.Text = "<h1>" + FormattingPipeline.FormatWithPhase3(h, FormattingContext.Header, null) + "</h1>";
		}

		/// <summary>
		/// Prints the footer.
		/// </summary>
		public void PrintFooter() {
			string f = FormattingPipeline.FormatWithPhase1And2(Settings.Provider.GetMetaDataItem(MetaDataItem.Footer, currentNamespaces),
					false, FormattingContext.Footer, null);

			lblFooterDivClean.Text = FormattingPipeline.FormatWithPhase3(f, FormattingContext.Footer, null);
		}

	}
}
