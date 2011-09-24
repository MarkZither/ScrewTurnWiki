
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using ScrewTurn.Wiki.PluginFramework;
using System.Text;

namespace ScrewTurn.Wiki {

	public partial class Sitemap : System.Web.UI.Page {

		protected void Page_Load(object sender, EventArgs e) {
			string currentWiki = Tools.DetectCurrentWiki();

			Response.ClearContent();
			Response.ContentType = "text/xml;charset=UTF-8";
			Response.ContentEncoding = System.Text.UTF8Encoding.UTF8;

			string mainUrl = Settings.GetMainUrl(currentWiki);
			string rootDefault = Settings.GetDefaultPage(currentWiki).ToLowerInvariant();

			using(XmlWriter writer = XmlWriter.Create(Response.OutputStream)) {
				writer.WriteStartDocument();

				writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
				writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
				writer.WriteAttributeString("xsi", "schemaLocation", null, "http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/09/sitemap.xsd");

				string user = SessionFacade.GetCurrentUsername();
				string[] groups = SessionFacade.GetCurrentGroupNames(currentWiki);


				AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));

				foreach(PageContent page in Pages.GetPages(currentWiki, null)) {
					if(authChecker.CheckActionForPage(page.FullName, Actions.ForPages.ReadPage, user, groups)) {
						WritePage(mainUrl, page.FullName, page.FullName.ToLowerInvariant() == rootDefault, writer);
					}
				}
				foreach(NamespaceInfo nspace in Pages.GetNamespaces(currentWiki)) {
					string nspaceDefault = nspace.DefaultPageFullName.ToLowerInvariant();

					foreach(PageContent page in Pages.GetPages(currentWiki, nspace)) {
						if(authChecker.CheckActionForPage(page.FullName, Actions.ForPages.ReadPage, user, groups)) {
							WritePage(mainUrl, page.FullName, page.FullName.ToLowerInvariant() == nspaceDefault, writer);
						}
					}
				}

				writer.WriteEndElement();
				writer.WriteEndDocument();
			}
		}

		/// <summary>
		/// Writes a page to the output XML writer.
		/// </summary>
		/// <param name="mainUrl">The main wiki URL.</param>
		/// <param name="pageFullName">The page full name.</param>
		/// <param name="isDefault">A value indicating whether the page is the default of its namespace.</param>
		/// <param name="writer">The writer.</param>
		private void WritePage(string mainUrl, string pageFullName, bool isDefault, XmlWriter writer) {
			writer.WriteStartElement("url");
			writer.WriteElementString("loc", mainUrl + Tools.UrlEncode(pageFullName) + GlobalSettings.PageExtension);
			writer.WriteElementString("priority", isDefault ? "0.75" : "0.5");
			writer.WriteElementString("changefreq", "daily");
			writer.WriteEndElement();
		}

	}

}
