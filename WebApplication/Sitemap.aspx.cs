
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class Sitemap : System.Web.UI.Page {

		protected void Page_Load(object sender, EventArgs e) {
			Response.ClearContent();
			Response.ContentType = "text/xml;charset=UTF-8";
			Response.ContentEncoding = System.Text.UTF8Encoding.UTF8;

			string mainUrl = Settings.MainUrl;

			using(XmlWriter writer = XmlWriter.Create(Response.OutputStream)) {
				writer.WriteStartDocument();

				writer.WriteStartElement("urlset");
				writer.WriteAttributeString("schemaLocation", "xsi", "http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/09/sitemap.xsd");

				foreach(PageInfo page in Pages.GetPages(null)) {
					WritePage(mainUrl, page, writer);
				}
				foreach(NamespaceInfo nspace in Pages.GetNamespaces()) {
					foreach(PageInfo page in Pages.GetPages(nspace)) {
						WritePage(mainUrl, page, writer);
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
		/// <param name="page">The page.</param>
		/// <param name="writer">The writer.</param>
		private void WritePage(string mainUrl, PageInfo page, XmlWriter writer) {
			writer.WriteStartElement("url");
			writer.WriteElementString("loc", mainUrl + Tools.UrlEncode(page.FullName) + Settings.PageExtension);
			writer.WriteElementString("priority", "0.5");
			writer.WriteElementString("changefreq", "daily");
			writer.WriteEndElement();
		}

	}

}
