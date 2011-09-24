
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
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class Category : BasePage {

		private NamespaceInfo currentNamespace = null;

		protected void Page_Load(object sender, EventArgs e) {
			string currentWiki = DetectWiki();
			Page.Title = Properties.Messages.CategoryTitle + " - " + Settings.GetWikiTitle(currentWiki);

			LoginTools.VerifyReadPermissionsForCurrentNamespace(currentWiki);

			currentNamespace = DetectNamespaceInfo();

			PrintCat(currentWiki);
		}

		public void PrintCat(string currentWiki) {
			StringBuilder sb = new StringBuilder();
			sb.Append("<ul>");
			sb.Append(@"<li><a href=""");
			UrlTools.BuildUrl(currentWiki, sb, "AllPages.aspx?Cat=-");
			sb.Append(@""">");
			sb.Append(Properties.Messages.UncategorizedPages);
			sb.Append("</a> (");
			sb.Append(Pages.GetUncategorizedPages(currentWiki, currentNamespace).Length.ToString());
			sb.Append(")");
			sb.Append(@" - <small><a href=""");
			UrlTools.BuildUrl(currentWiki, sb, "RSS.aspx?Category=-");
			sb.Append(@""" title=""");
			sb.Append(Properties.Messages.RssForThisCategory);
			sb.Append(@""">RSS</a> - <a href=""");
			UrlTools.BuildUrl(currentWiki, sb, "RSS.aspx?Discuss=1&amp;Category=-");
			sb.Append(@""" title=""");
			sb.Append(Properties.Messages.RssForThisCategoryDiscussion);
			sb.Append(@""">");
			sb.Append(Properties.Messages.DiscussionsRss);
			sb.Append("</a>");
			sb.Append("</small>");
			sb.Append("</li></ul><br />");

			sb.Append("<ul>");

			List<CategoryInfo> categories = Pages.GetCategories(currentWiki, currentNamespace);

			for(int i = 0; i < categories.Count; i++) {
				if(categories[i].Pages.Length > 0) {
					sb.Append(@"<li>");
					sb.Append(@"<a href=""");
					UrlTools.BuildUrl(currentWiki, sb, "AllPages.aspx?Cat=", Tools.UrlEncode(categories[i].FullName));
					sb.Append(@""">");
					sb.Append(NameTools.GetLocalName(categories[i].FullName));
					sb.Append("</a> (");
					sb.Append(categories[i].Pages.Length.ToString());
					sb.Append(")");
					sb.Append(@" - <small><a href=""");
					UrlTools.BuildUrl(currentWiki, sb, "RSS.aspx?Category=", Tools.UrlEncode(categories[i].FullName));
					sb.Append(@""" title=""");
					sb.Append(Properties.Messages.RssForThisCategory);
					sb.Append(@""">RSS</a> - <a href=""");
					UrlTools.BuildUrl(currentWiki, sb, "RSS.aspx?Discuss=1&amp;Category=", Tools.UrlEncode(categories[i].FullName));
					sb.Append(@""" title=""");
					sb.Append(Properties.Messages.RssForThisCategoryDiscussion);
					sb.Append(@""">");
					sb.Append(Properties.Messages.DiscussionsRss);
					sb.Append("</a>");
					sb.Append("</small>");
					sb.Append("</li>");
				}
				else {
					sb.Append(@"<li><i>");
					sb.Append(NameTools.GetLocalName(categories[i].FullName));
					sb.Append("</i></li>");
				}
			}
			sb.Append("</ul>");
			lblCatList.Text = sb.ToString();
		}

	}

}
