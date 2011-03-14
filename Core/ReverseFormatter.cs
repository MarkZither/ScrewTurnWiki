
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

		private static string processList(XmlNodeList nodes, string marker) {
			string result = "";
			string ul = "*";
			string ol = "#";
			foreach(XmlNode node in nodes) {
				string text = "";
				if(node.Name == "li") {
					foreach(XmlNode child in node.ChildNodes) {
						if(child.Name != "ol" && child.Name != "ul") {
							StringReader a = new StringReader(child.OuterXml);
							XmlDocument n = FromHTML((TextReader)a);
							text += processChild(n.ChildNodes);
						}
					}
					result += marker + " " + text;
					if(!result.EndsWith("\r\n")) result += "\r\n";
					foreach(XmlNode child in node.ChildNodes) {
						if(child.Name.ToString() == "ol") {
							result += processList(child.ChildNodes, marker + ol);
						}
						if(child.Name.ToString() == "ul") {
							result += processList(child.ChildNodes, marker + ul);
						}
					}
				}
			}
			return result;
		}

		private static string processImage(XmlNode node) {
			string result = "";
			if(node.Attributes.Count != 0) {
				foreach(XmlAttribute attName in node.Attributes) {
					if(attName.Name == "src") {
						string[] path = attName.Value.ToString().Split('=');
						if(path.Length > 2)
							result += "{" + "UP(" + path[1].Split('&')[0] + ")}" + path[2];
						else result += "{UP}" + path[path.Length - 1];
					}
				}
			}
			return result;
		}

		private static string processLink(string link) {
			string subLink = "";
			string[] links = link.Split('=');
			if(links[0] == "GetFile.aspx?File") {
				subLink += "{UP}";
				for(int i = 1; i < links.Length - 1; i++)
					subLink += links[i] + "=";
				subLink += links[links.Length - 1];
				link = subLink;
			}
			return link;
		}

		private static string processChildImage(XmlNodeList nodes) {
			string image = "";
			string p = "";
			string url = "";
			string result = "";
			bool hasDescription = false;
			foreach(XmlNode node in nodes) {
				if(node.Name.ToLowerInvariant() == "img")
					image += processImage(node);
				else if(node.Name.ToLowerInvariant() == "p") {
					hasDescription = true;
					p += "|" + processChild(node.ChildNodes) + "|";
				}
				else if(node.Name.ToLowerInvariant() == "a") {
					string link = "";
					string target = "";

					if(node.Attributes.Count != 0) {
						XmlAttributeCollection attribute = node.Attributes;
						foreach(XmlAttribute attName in attribute) {
							if(attName.Value.ToString() == "_blank")
								target += "^";
							if(attName.Name.ToString() == "href")
								link += attName.Value.ToString();
						}
					}
					link = processLink(link);

					image += processImage(node.LastChild);
					url = "|" + target + link;
				}
			}
			if(!hasDescription)
				p = "||";
			result = p + image + url;
			return result;
		}
		
		private static string processTableImage(XmlNodeList nodes) {
			string result = "";
			foreach(XmlNode node in nodes) {
				switch(node.Name.ToLowerInvariant()) {
					case "tbody":
						result += processTableImage(node.ChildNodes);
						break;
					case "tr":
						result += processTableImage(node.ChildNodes);
						break;
					case "td":
						string image = "";
						string aref = "";
						string p = "";
						bool hasLink = false;
						if(node.FirstChild.Name.ToLowerInvariant() == "img") image += processTableImage(node.ChildNodes);
						if(node.FirstChild.Name.ToLowerInvariant() == "a") {
							hasLink = true;
							aref += processTableImage(node.ChildNodes);
						}
						if(node.LastChild.Name.ToLowerInvariant() == "p") p += node.LastChild.InnerText.ToString();
						if(!hasLink) result += p + image;
						else result += p + aref;
						break;
					case "img":
						result += "|" + processImage(node);
						break;
					case "a":
						string link = "";
						string target = "";
						string title = "";
						if(node.Attributes.Count != 0) {
							XmlAttributeCollection attribute = node.Attributes;
							foreach(XmlAttribute attName in attribute) {
								if(attName.Name.ToString() != "id".ToLowerInvariant()) {
									if(attName.Value.ToString() == "_blank")
										target += "^";
									if(attName.Name.ToString() == "href")
										link += attName.Value.ToString();
									if(attName.Name.ToString() == "title")
										title += attName.Value.ToString();
								}
								link = processLink(link);
							}
							result += processTableImage(node.ChildNodes) + "|" + target + link;
						}
						break;
				}
			}
			return result;
		}

		private static string processCode(string text) {
			string result = "";
			result = text;
			return result;
		}

		private static string processTable(XmlNodeList nodes) {
			string result = "";

			foreach(XmlNode node in nodes) {
				switch(node.Name.ToLowerInvariant()) {
					case "thead":
						result += processTable(node.ChildNodes);
						break;
					case "th":
						result += "! " + processChild(node.ChildNodes) + "\r\n";
						break;
					case "caption":
						result += "|+ " + processChild(node.ChildNodes) + "\r\n";
						break;
					case "tbody":
						result += processTable(node.ChildNodes) + "";
						break;
					case "tr":
						string style = "";
						foreach(XmlAttribute attr in node.Attributes)
							if(attr.Name.ToLowerInvariant() == "style") style += "style=\"" + attr.Value.ToString() + "\" ";

						result += "|- " + style + "\r\n" + processTable(node.ChildNodes);
						break;
					case "td":
						string styleTd = "";
						if(node.Attributes.Count != 0) {
							foreach(XmlAttribute attr in node.Attributes)
								styleTd += " " + attr.Name + "=\"" + attr.Value.ToString() + "\" ";
							result += "| " + styleTd + " | " + processChild(node.ChildNodes) + "\r\n";
						}
						else result += "| " + processChild(node.ChildNodes) + "\r\n";
						break;
				}

			}
			return result;
		}

		private static string processChild(XmlNodeList nodes) {
			string result = "";
			foreach(XmlNode node in nodes) {
				bool anchor = false;
				if(node.NodeType == XmlNodeType.Text) {
					result += node.Value;
				}
				else if(node.NodeType != XmlNodeType.Whitespace) {
					switch(node.Name.ToLowerInvariant()) {
						case "html":
							result += processChild(node.ChildNodes);
							break;
						case "b":
						case "strong":
							result += ("'''" + processChild(node.ChildNodes) + "'''");
							break;
						case "strike":
						case "s":
							result += ("--" + processChild(node.ChildNodes) + "--");
							break;
						case "em":
						case "i":
							result += ("''" + processChild(node.ChildNodes) + "''");
							break;
						case "u":
							result += ("__" + processChild(node.ChildNodes) + "__");
							break;
						case "h1":
							if(node.HasChildNodes) {
								if(node.FirstChild.NodeType == XmlNodeType.Whitespace) result += ("----\r\n" + processChild(node.ChildNodes));
								else result += ("==" + processChild(node.ChildNodes) + "==\r\n");
							}
							else result += ("----\r\n");
							break;
						case "h2":
							result += ("===" + processChild(node.ChildNodes) + "===\r\n");
							break;
						case "h3":
							result += ("====" + processChild(node.ChildNodes) + "====\r\n");
							break;
						case "h4":
							result += ("=====" + processChild(node.ChildNodes) + "=====\r\n");
							break;
						case "pre":
							result += ("@@" + processCode(node.InnerText.ToString()) + "@@");
							break;
						case "code":
							result += ("{{" + processChild(node.ChildNodes) + "}}");
							break;
						case "hr":
						case "hr /":
							result += ("\r\n== ==\r\n" + processChild(node.ChildNodes));
							break;
						case "span":
							if(node.Attributes.Count != 0) {
								XmlAttributeCollection attribute = node.Attributes;
								foreach(XmlAttribute attName in attribute) {
									if(attName.Value.ToString() == "italic")
										result += "''" + processChild(node.ChildNodes) + "''";
								}
							}
							break;
						case "br":
							result += ("\r\n" + processChild(node.ChildNodes));
							break;
						case "table":
							bool isImage = false;
							string image = "";
							string tableStyle = "";

							foreach(XmlAttribute attName in node.Attributes) {
								if(attName.Value.ToString() == "imageauto") {
									isImage = true;
									image += "[imageauto|" + processTableImage(node.ChildNodes) + "]\r\n";
								}
								else tableStyle += attName.Name + "=\"" + attName.Value.ToString() + "\" ";
							}
							if(isImage) {
								result += image;
								isImage = false;
								break;
							}
							else result += "{| " + tableStyle + "\r\n" + processTable(node.ChildNodes) + "|}\r\n";
							break;
						case "ol":
							if(node.ParentNode != null) {
								if(node.ParentNode.Name.ToLowerInvariant() != "td") result += processList(node.ChildNodes, "#");
								else result += node.OuterXml.ToString();
							}
							else result += processList(node.ChildNodes, "#");
							break;
						case "ul":
							if(node.ParentNode != null) {
								if(node.ParentNode.Name.ToLowerInvariant() != "td") result += processList(node.ChildNodes, "*");
								else result += node.OuterXml.ToString();
							}
							else result += processList(node.ChildNodes, "*");
							break;
						case "sup":
							result += ("<sup>" + processChild(node.ChildNodes) + "</sup>");
							break;
						case "sub":
							result += ("<sub>" + processChild(node.ChildNodes) + "</sub>");
							break;
						case "p":
							if(node.Attributes.Count != 0) {
								XmlAttributeCollection attribute = node.Attributes;
								foreach(XmlAttribute attName in attribute) {
									if(attName.Value.ToString() == "imagedescription")
										result += "";
								}
							}
							else result += processChild(node.ChildNodes) + "{BR}\r\n";
							break;
						case "div":
							if(node.Attributes.Count != 0) {
								XmlAttributeCollection attribute = node.Attributes;
								foreach(XmlAttribute attName in attribute) {
									if(attName.Value.ToString() == "box") {
										result += "(((" + processChild(node.ChildNodes) + ")))\r\n";
									}
									if(attName.Value.ToString() == "imageleft") {
										result += "[imageleft" + processChildImage(node.ChildNodes) + "]\r\n";
									}
									if(attName.Value.ToString() == "imageright")
										result += "[imageright" + processChildImage(node.ChildNodes) + "]\r\n";
									if(attName.Value.ToString() == "image")
										result += "[image" + processChildImage(node.ChildNodes) + "]\r\n";
									if(attName.Value.ToString() == "indent") result += ": " + processChild(node.ChildNodes) + "\r\n";
								}
							}
							else
								result += (processChild(node.ChildNodes) + "\r\n");
							break;

						case "img":
							string description = "";
							bool hasClass = false;
							bool isLink = false;
							if(node.ParentNode != null)
								if(node.ParentNode.Name.ToLowerInvariant().ToString() == "a")
									isLink = true;
							if(node.Attributes.Count != 0) {
								foreach(XmlAttribute attName in node.Attributes) {
									if(attName.Name.ToString() == "alt")
										description = attName.Value.ToString();
									//description = attName.Value.ToString();
									if(attName.Name.ToString() == "class")
										hasClass = true;
								}
							}
							if((!hasClass) && (!isLink))
								result += "[image|" + description + "|" + processImage(node) + "]\r\n";
							else if((!hasClass) && (isLink))
								result += "[image|" + description + "|" + processImage(node);
							else
								result += description + "|" + processImage(node);
							break;

						case "a":
							bool isTable = false;
							string link = "";
							string target = "";
							string title = "";
							bool isInternalLink = false;
							bool childImg = false;
							bool isUnknowLink = false;
							if(node.FirstChild != null) {
								if(node.FirstChild.Name.ToLowerInvariant() == "img")
									childImg = true;
							}
							if(node.ParentNode.Name.ToLowerInvariant() == "td")
								isTable = true;
							if(node.Attributes.Count != 0) {
								XmlAttributeCollection attribute = node.Attributes;
								foreach(XmlAttribute attName in attribute) {
									if(attName.Name.ToString() != "id".ToLowerInvariant()) {
										if(attName.Value.ToString() == "_blank")
											target += "^";
										if(attName.Name.ToString() == "href")
											link += attName.Value.ToString();
										if(attName.Name.ToString() == "title")
											title += attName.Value.ToString();
										if(attName.Value.ToString() == "SystemLink".ToLowerInvariant())
											isInternalLink = true;
										if(attName.Value.ToString().ToLowerInvariant() == "unknownlink")
											isUnknowLink = true;
									}
									else {
										anchor = true;
										result += "[anchor|#" + attName.Value.ToString().ToLowerInvariant() + "]" + processChild(node.ChildNodes);
										break;
									}


								}
								if(isInternalLink) {
									string[] splittedLink = link.Split('=');
									link = "c:" + splittedLink[1];
								}
								else
									link = processLink(link);

								if((!anchor) && (!isTable) && (!childImg) && (!isUnknowLink))
									if(title != link)
										result += "[" + target + link + "|" + processChild(node.ChildNodes) + "]";
									else
										result += "[" + target + link + "|" + processChild(node.ChildNodes) + "]";
								if((!anchor) && (!childImg) && (isTable))
									result += "[" + target + link + "|" + processChild(node.ChildNodes) + "]";
								if((!anchor) && (childImg) && (!isTable))
									result += processChild(node.ChildNodes) + "|" + target + link + "]\r\n";
							}
							break;

						default:
							result += (node.OuterXml);
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
			sgmlReader.WhitespaceHandling = WhitespaceHandling.All;

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
			if(x != null) return processChild(x.FirstChild.ChildNodes);
			else return "";
		}
	}
}