
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class AccessDenied : BasePage {

		protected void Page_Load(object sender, EventArgs e) {
			string currentWiki = DetectWiki();
			Page.Title = Properties.Messages.AccessDeniedTitle + " - " + Settings.GetWikiTitle(currentWiki);

			string n = Settings.GetProvider(currentWiki).GetMetaDataItem(MetaDataItem.AccessDeniedNotice, null);
			if(!string.IsNullOrEmpty(n)) {
				n = FormattingPipeline.FormatWithPhase1And2(currentWiki, n, false, FormattingContext.Other, null);
			}
			if(!string.IsNullOrEmpty(n)) lblDescription.Text = FormattingPipeline.FormatWithPhase3(currentWiki, n, FormattingContext.Other, null);
		}

	}

}
