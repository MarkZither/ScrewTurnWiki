
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Text;

namespace ScrewTurn.Wiki {

	public partial class MasterPageSA : System.Web.UI.MasterPage {

		private string currentNamespace = null;
		private string currentWiki = null;

		protected void Page_Load(object sender, EventArgs e) {
			// Try to detect current namespace
			currentNamespace = Tools.DetectCurrentNamespace();
			currentWiki = Tools.DetectCurrentWiki();

			lblStrings.Text = string.Format("<script type=\"text/javascript\">\r\n<!--\r\n__BaseName = \"{0}\";\r\n__ConfirmMessage = \"{1}\";\r\n// -->\r\n</script>",
				CphMasterSA.ClientID + "_", Properties.Messages.ConfirmOperation);

			string nspace = currentNamespace;
			if(string.IsNullOrEmpty(nspace)) nspace = "";
			else nspace += ".";
			lnkMainPage.NavigateUrl = nspace + "Default.aspx";

			if(!Page.IsPostBack) {
				string referrer = Request.UrlReferrer != null ? Request.UrlReferrer.FixHost().ToString() : "";
				if(!string.IsNullOrEmpty(referrer)) {
					lnkPreviousPage.Visible = true;
					lnkPreviousPage.NavigateUrl = referrer;
				}
				else lnkPreviousPage.Visible = false;
			}

			PrintHtmlHead();
			PrintHeader();
			PrintFooter();
		}

		/// <summary>
		/// Prints the HTML head tag.
		/// </summary>
		public void PrintHtmlHead() {
			Literal c = new Literal();
			c.Text = Tools.GetIncludes(currentWiki, Tools.DetectCurrentNamespace()) + "\r\n" + Host.Instance.GetAllHtmlHeadContent(currentWiki);
			Page.Header.Controls.Add(c);
		}
		
		/// <summary>
		/// Prints the header.
		/// </summary>
		public void PrintHeader() {
			string h = FormattingPipeline.FormatWithPhase1And2(currentWiki, Settings.GetProvider(currentWiki).GetMetaDataItem(MetaDataItem.Header, currentNamespace),
					false, FormattingContext.Header, null);

			lblHeaderDiv.Text = FormattingPipeline.FormatWithPhase3(currentWiki, h, FormattingContext.Header, null);
		}

		/// <summary>
		/// Prints the footer.
		/// </summary>
		public void PrintFooter() {
			string f = FormattingPipeline.FormatWithPhase1And2(currentWiki, Settings.GetProvider(currentWiki).GetMetaDataItem(MetaDataItem.Footer, currentNamespace),
					false, FormattingContext.Footer, null);

			lblFooterDiv.Text = FormattingPipeline.FormatWithPhase3(currentWiki, f, FormattingContext.Footer, null);
		}

	}

}
