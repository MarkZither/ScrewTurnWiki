
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.IO;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Implements reverse formatting methods (HTML-&gt;WikiMarkup).
	/// </summary>
	public static class ReverseFormatter {

		private static string ProcessList(XmlNodeList nodes, string marker) {
			string result = "";
			string ul = "*";
			string ol = "#";
			foreach(XmlNode node in nodes) {
				string text = "";
				if(node.Name == "li") {
					foreach(XmlNode child in node.ChildNodes) {
						if(child.Name != "ol" && child.Name != "ul") {
							TextReader reader = new StringReader(child.OuterXml);
							XmlDocument n = FromHTML(reader);
							text += ProcessChild(n.ChildNodes);
						}
					}
					XmlAttribute styleAttribute = node.Attributes["style"];
					if(styleAttribute != null) {
						if(styleAttribute.Value.Contains("bold")) {
							text = "'''" + text + "'''";
						}
						if(styleAttribute.Value.Contains("italic")) {
							text = "''" + text + "''";
						}
						if(styleAttribute.Value.Contains("underline")) {
							text = "__" + text + "__";
						}
					}
					result += marker + " " + text;
					if(!result.EndsWith("\n")) result += "\n";
					foreach(XmlNode child in node.ChildNodes) {
						if(child.Name.ToString() == "ol") result += ProcessList(child.ChildNodes, marker + ol);
						if(child.Name.ToString() == "ul") result += ProcessList(child.ChildNodes, marker + ul);
					}
				}
			}
			return result;
		}

		private static string ProcessImage(XmlNode node) {
			string result = "";
			if(node.Attributes.Count != 0) {
				foreach(XmlAttribute attName in node.Attributes) {
					if(attName.Name == "src") {
						string[] path = attName.Value.ToString().Split('=');
						if(path.Length > 2) result += "{" + "UP(" + path[1].Split('&')[0] + ")}" + path[2];
						else result += "{UP}" + path[path.Length - 1];
					}
				}
			}
			return result;
		}

		private static string ProcessLink(string link) {
			string subLink = "";
			string[] links = link.Split('=');
			if(links[0] == "GetFile.aspx?File") {
				subLink += "{UP}";
				for(int i = 1; i < links.Length - 1; i++) {
					subLink += links[i] + "=";
				}
				subLink += links[links.Length - 1];
				link = subLink;
			}
			return link;
		}

		private static string ProcessChildImage(XmlNodeList nodes) {
			string image = "";
			string p = "";
			string url = "";
			string result = "";
			bool hasDescription = false;
			foreach(XmlNode node in nodes) {
				if(node.Name.ToLowerInvariant() == "img") image += ProcessImage(node);
				else if(node.Name.ToLowerInvariant() == "p") {
					hasDescription = true;
					p += "|" + ProcessChild(node.ChildNodes) + "|";
				}
				else if(node.Name.ToLowerInvariant() == "a") {
					string link = "";
					string target = "";
					if(node.Attributes.Count != 0) {
						XmlAttributeCollection attribute = node.Attributes;
						foreach(XmlAttribute attName in attribute) {
							if(attName.Value.ToString() == "_blank") target += "^";
							if(attName.Name.ToString() == "href") link += attName.Value.ToString();
						}
					}
					link = ProcessLink(link);
					image += ProcessImage(node.LastChild);
					url = "|" + target + link;
				}
			}
			if(!hasDescription) p = "||";
			result = p + image + url;
			return result;
		}
		
		private static string ProcessTableImage(XmlNodeList nodes) {
			string result = "";
			foreach(XmlNode node in nodes) {
				switch(node.Name.ToLowerInvariant()) {
					case "tbody":
						result += ProcessTableImage(node.ChildNodes);
						break;
					case "tr":
						result += ProcessTableImage(node.ChildNodes);
						break;
					case "td":
						string image = "";
						string aref = "";
						string p = "";
						bool hasLink = false;
						if(node.FirstChild.Name.ToLowerInvariant() == "img") image += ProcessTableImage(node.ChildNodes);
						if(node.FirstChild.Name.ToLowerInvariant() == "a") {
							hasLink = true;
							aref += ProcessTableImage(node.ChildNodes);
						}
						if(node.LastChild.Name.ToLowerInvariant() == "p") p += node.LastChild.InnerText.ToString();
						if(!hasLink) result += p + image;
						else result += p + aref;
						break;
					case "img":
						result += "|" + ProcessImage(node);
						break;
					case "a":
						string link = "";
						string target = "";
						string title = "";
						if(node.Attributes.Count != 0) {
							XmlAttributeCollection attribute = node.Attributes;
							foreach(XmlAttribute attName in attribute) {
								if(attName.Name.ToString() != "id".ToLowerInvariant()) {
									if(attName.Value.ToString() == "_blank") target += "^";
									if(attName.Name.ToString() == "href") link += attName.Value.ToString();
									if(attName.Name.ToString() == "title") title += attName.Value.ToString();
								}
								link = ProcessLink(link);
							}
							result += ProcessTableImage(node.ChildNodes) + "|" + target + link;
						}
						break;
				}
			}
			return result;
		}

		private static string ProcessTable(XmlNodeList nodes) {
			string result = "";
			foreach(XmlNode node in nodes) {
				switch(node.Name.ToLowerInvariant()) {
					case "thead":
						result += ProcessTable(node.ChildNodes);
						break;
					case "th":
						result += "! " + ProcessChild(node.ChildNodes) + "\n";
						break;
					case "caption":
						result += "|+ " + ProcessChild(node.ChildNodes) + "\n";
						break;
					case "tbody":
						result += ProcessTable(node.ChildNodes) + "";
						break;
					case "tr":
						string style = "";
						foreach(XmlAttribute attr in node.Attributes) {
							if(attr.Name.ToLowerInvariant() == "style") style += "style=\"" + attr.Value.ToString() + "\" ";
						}
						result += "|- " + style + "\n" + ProcessTable(node.ChildNodes);
						break;
					case "td":
						string styleTd = "";
						if(node.Attributes.Count != 0) {
							foreach(XmlAttribute attr in node.Attributes) {
								styleTd += " " + attr.Name + "=\"" + attr.Value.ToString() + "\" ";
							}
							result += "| " + styleTd + " | " + ProcessChild(node.ChildNodes) + "\n";
						}
						else result += "| " + ProcessChild(node.ChildNodes) + "\n";
						break;
				}
			}
			return result;
		}

		private static string ProcessChild(XmlNodeList nodes) {
			string result = "";
			foreach(XmlNode node in nodes) {
				bool anchor = false;
				if(node.NodeType == XmlNodeType.Text) result += node.Value.TrimStart('\n');
				else if(node.NodeType != XmlNodeType.Whitespace) {
					switch(node.Name.ToLowerInvariant()) {
						case "html":
							result += ProcessChild(node.ChildNodes);
							break;
						case "b":
						case "strong":
							result += node.HasChildNodes ? "'''" + ProcessChild(node.ChildNodes) + "'''" : "";
							break;
						case "strike":
						case "s":
							result += node.HasChildNodes ? "--" + ProcessChild(node.ChildNodes) + "--" : "";
							break;
						case "em":
						case "i":
							result += node.HasChildNodes ? "''" + ProcessChild(node.ChildNodes) + "''" : "";
							break;
						case "u":
							result += node.HasChildNodes ? "__" + ProcessChild(node.ChildNodes) + "__" : "";
							break;
						case "h1":
							if(node.HasChildNodes) {
								if(node.FirstChild.NodeType == XmlNodeType.Whitespace) result += "----\n" + ProcessChild(node.ChildNodes);
								else result += "==" + ProcessChild(node.ChildNodes) + "==\n";
							}
							else result += "----\n";
							break;
						case "h2":
							result += "===" + ProcessChild(node.ChildNodes) + "===\n";
							break;
						case "h3":
							result += "====" + ProcessChild(node.ChildNodes) + "====\n";
							break;
						case "h4":
							result += "=====" + ProcessChild(node.ChildNodes) + "=====\n";
							break;
						case "pre":
							result += node.HasChildNodes ? "@@" + node.InnerText.ToString() + "@@" : "";
							break;
						case "code":
							result += node.HasChildNodes ? "{{" + ProcessChild(node.ChildNodes) + "}}" : "";
							break;
						case "hr":
						case "hr /":
							result += "\n== ==\n" + ProcessChild(node.ChildNodes);
							break;
						case "span":
							if(node.Attributes["style"] != null) {
								if(node.Attributes["style"].Value.Replace(" ", "").Contains("font-weight:normal")) {
									result += ProcessChild(node.ChildNodes);
								}
							}
							if(node.Attributes.Count > 0) {
								XmlAttributeCollection attributeCollection = node.Attributes;
								foreach(XmlAttribute attribute in attributeCollection) {
									if(attribute.Value == "italic") result += "''" + ProcessChild(node.ChildNodes) + "''";
								}
							}
							break;
						case "br":
							if(node.PreviousSibling != null && node.PreviousSibling.Name == "br") {
								result += "\n";
							}
							else {
								result += Settings.ProcessSingleLineBreaks ? "\n" : "\n\n";
							}
							break;
						case "table":
							string tableStyle = "";

							if(node.Attributes["class"] != null && node.Attributes["class"].Value.Contains("imageauto")) {
								result += "[imageauto|" + ProcessTableImage(node.ChildNodes) + "]";
							}
							else {
								foreach(XmlAttribute attName in node.Attributes) {
									tableStyle += attName.Name + "=\"" + attName.Value + "\" ";
								}
								result += "{| " + tableStyle + "\n" + ProcessTable(node.ChildNodes) + "|}\n";
							}
							break;
						case "ol":
							if(node.PreviousSibling != null) {
								result += "\n";
							}
							if(node.ParentNode != null) {
								if(node.ParentNode.Name.ToLowerInvariant() != "td") result += ProcessList(node.ChildNodes, "#");
								else result += node.OuterXml.ToString();
							}
							else result += ProcessList(node.ChildNodes, "#");
							break;
						case "ul":
							if(node.PreviousSibling != null) {
								result += "\n";
							}
							if(node.ParentNode != null) {
								if(node.ParentNode.Name.ToLowerInvariant() != "td") result += ProcessList(node.ChildNodes, "*");
								else result += node.OuterXml.ToString();
							}
							else result += ProcessList(node.ChildNodes, "*");
							break;
						case "sup":
							result += "<sup>" + ProcessChild(node.ChildNodes) + "</sup>";
							break;
						case "sub":
							result += "<sub>" + ProcessChild(node.ChildNodes) + "</sub>";
							break;
						case "p":
							if(node.Attributes["class"] != null && node.Attributes["class"].Value.Contains("imagedescription")) continue;
							else result += ProcessChild(node.ChildNodes) + "\n" + (Settings.ProcessSingleLineBreaks ? "" : "\n");
							break;
						case "div":
							if(node.Attributes["class"] != null) {
								if(node.Attributes["class"].Value.Contains("box")) result += node.HasChildNodes ? "(((" + ProcessChild(node.ChildNodes) + ")))" : "";
								else if(node.Attributes["class"].Value.Contains("imageleft")) result += "[imageleft" + ProcessChildImage(node.ChildNodes) + "]";
								else if(node.Attributes["class"].Value.Contains("imageright")) result += "[imageright" + ProcessChildImage(node.ChildNodes) + "]";
								else if(node.Attributes["class"].Value.Contains("image")) result += "[image" + ProcessChildImage(node.ChildNodes) + "]";
								else if(node.Attributes["class"].Value.Contains("indent")) result += ": " + ProcessChild(node.ChildNodes) + "\n";
							}
							else {
								if(node.PreviousSibling != null && node.PreviousSibling.Name != "div") {
									result += Settings.ProcessSingleLineBreaks ? "\n" : "\n\n";
								}
								if(node.FirstChild != null && node.FirstChild.Name == "br") {
									node.RemoveChild(node.FirstChild);
								}
								if(node.HasChildNodes) {
									result += ProcessChild(node.ChildNodes);
									result += Settings.ProcessSingleLineBreaks ? "\n" : "\n\n";
								}
							}
							break;
						case "img":
							string description = "";
							bool hasClass = false;
							bool isLink = false;
							if(node.ParentNode != null && node.ParentNode.Name.ToLowerInvariant().ToString() == "a") isLink = true;
							if(node.Attributes.Count != 0) {
								foreach(XmlAttribute attName in node.Attributes) {
									if(attName.Name.ToString() == "alt") description = attName.Value.ToString();
									if(attName.Name.ToString() == "class") hasClass = true;
								}
							}
							if(!hasClass && !isLink) result += "[image|" + description + "|" + ProcessImage(node) + "]\n";
							else if(!hasClass && isLink) result += "[image|" + description + "|" + ProcessImage(node);
							else result += description + "|" + ProcessImage(node);
							break;
						case "a":
							bool isTable = false;
							string link = "";
							string target = "";
							string title = "";
							bool isInternalLink = false;
							bool childImg = false;
							bool isUnknowLink = false;
							if(node.FirstChild != null && node.FirstChild.Name == "img") childImg = true;
							if(node.ParentNode.Name == "td") isTable = true;
							if(node.Attributes.Count != 0) {
								XmlAttributeCollection attribute = node.Attributes;
								foreach(XmlAttribute attName in attribute) {
									if(attName.Name != "id".ToLowerInvariant()) {
										if(attName.Value == "_blank") target += "^";
										if(attName.Name == "href") link += attName.Value.ToString();
										if(attName.Name == "title") title += attName.Value.ToString();
										if(attName.Value == "SystemLink".ToLowerInvariant()) isInternalLink = true;
										if(attName.Value == "unknownlink") isUnknowLink = true;
									}
									else {
										anchor = true;
										result += "[anchor|#" + attName.Value + "]" + ProcessChild(node.ChildNodes);
										break;
									}
								}
								if(isInternalLink) {
									string[] splittedLink = link.Split('=');
									link = "c:" + splittedLink[1];
								}
								else link = ProcessLink(link);
								if(!anchor && !isTable && !childImg && !isUnknowLink)
									if(title != link) result += "[" + target + link + "|" + ProcessChild(node.ChildNodes) + "]";
									else result += "[" + target + link + "|" + ProcessChild(node.ChildNodes) + "]";
								if(!anchor && !childImg && isTable) result += "[" + target + link + "|" + ProcessChild(node.ChildNodes) + "]";
								if(!anchor && childImg && !isTable) result += ProcessChild(node.ChildNodes) + "|" + target + link + "]";
							}
							break;
						default:
							result += node.OuterXml;
							break;
					}
				}
				else result += "";
			}
			return result;
		}

		private static XmlDocument FromHTML(TextReader reader) {
			// setup SgmlReader
			Sgml.SgmlReader sgmlReader = new Sgml.SgmlReader();
			sgmlReader.DocType = "HTML";
			sgmlReader.WhitespaceHandling = WhitespaceHandling.None;

			sgmlReader.CaseFolding = Sgml.CaseFolding.ToLower;
			sgmlReader.InputStream = reader;

			// create document
			XmlDocument doc = new XmlDocument();
			doc.PreserveWhitespace = true;
			doc.XmlResolver = null;
			doc.Load(sgmlReader);
			return doc;
		}

		/// <summary>
		/// Reverse formats HTML content into WikiMarkup.
		/// </summary>
		/// <param name="html">The input HTML.</param>
		/// <returns>The corresponding WikiMarkup.</returns>
		public static string ReverseFormat(string html) {
			StringReader strReader = new StringReader(html);
			XmlDocument x = FromHTML((TextReader)strReader);
			if(x != null && x.HasChildNodes && x.FirstChild.HasChildNodes) return ProcessChild(x.FirstChild.ChildNodes);
			else return "";
		}
	}
}