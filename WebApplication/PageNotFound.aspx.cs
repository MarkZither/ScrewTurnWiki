
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Text;

namespace ScrewTurn.Wiki {

	public partial class PageNotFound : BasePage {

		private string currentWiki = null;

		protected void Page_Load(object sender, EventArgs e) {
			currentWiki = DetectWiki();
			Page.Title = Properties.Messages.PageNotFoundTitle + " - " + Settings.GetWikiTitle(currentWiki);

			if(Request["Page"] != null) {
				lblDescription.Text = lblDescription.Text.Replace("##PAGENAME##", Request["Page"]);
			}
			else {
				UrlTools.Redirect(UrlTools.BuildUrl(currentWiki, "Default.aspx"));
			}

			//PrintSearchResults();
		}

		///// <summary>
		///// Prints the results of the automatic search.
		///// </summary>
		//private void PrintSearchResults() {
		//    StringBuilder sb = new StringBuilder(1000);

		//    PageContent[] results = SearchTools.SearchSimilarPages(Request["Page"], DetectNamespace(), currentWiki);
		//    if(results.Length > 0) {
		//        sb.Append("<p>");
		//        sb.Append(Properties.Messages.WereYouLookingFor);
		//        sb.Append("</p>");
		//        sb.Append("<ul>");
		//        for(int i = 0; i < results.Length; i++) {
		//            sb.Append(@"<li><a href=""");
		//            UrlTools.BuildUrl(currentWiki, sb, Tools.UrlEncode(results[i].FullName), GlobalSettings.PageExtension);
		//            sb.Append(@""">");
		//            sb.Append(FormattingPipeline.PrepareTitle(currentWiki, results[i].Title, false, FormattingContext.PageContent, results[i].FullName));
		//            sb.Append("</a></li>");
		//        }
		//        sb.Append("</ul>");
		//    }
		//    else {
		//        sb.Append("<p>");
		//        sb.Append(Properties.Messages.NoSimilarPages);
		//        sb.Append("</p>");
		//    }
		//    sb.Append(@"<br /><p>");
		//    sb.Append(Properties.Messages.YouCanAlso);
		//    sb.Append(@" <a href=""");
		//    UrlTools.BuildUrl(currentWiki, sb, "Search.aspx?Query=", Tools.UrlEncode(Request["Page"]));
		//    sb.Append(@""">");
		//    sb.Append(Properties.Messages.PerformASearch);
		//    sb.Append("</a> ");
		//    sb.Append(Properties.Messages.Or);
		//    sb.Append(@" <a href=""");
		//    UrlTools.BuildUrl(currentWiki, sb, "Edit.aspx?Page=", Tools.UrlEncode(Request["Page"]));
		//    sb.Append(@"""><b>");
		//    sb.Append(Properties.Messages.CreateThePage);
		//    sb.Append("</b></a> (");
		//    sb.Append(Properties.Messages.CouldRequireLogin);
		//    sb.Append(").</p>");

		//    lblSearchResults.Text = sb.ToString();
		//}

	}

}
