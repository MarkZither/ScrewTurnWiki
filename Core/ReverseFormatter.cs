
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


		/// <summary>
		/// Processes ordered or unordered lists and sublists.
		/// </summary>
		/// <param name="nodes">The nodes to process.</param>
		/// <param name="marker">The marker to prepend list items with.</param>
		/// <returns>Valid WikiMarkup text for the list structure.</returns>
		private static string ProcessList(XmlNodeList nodes, string marker) {
			string result = "";
			string ul = "*";
			string ol = "#";
			foreach(XmlNode node in nodes) {
				string text = "";
				if(node.Name == "li") {
					foreach(XmlNode child in node.ChildNodes) {
						if(child.Name != "ol" && child.Name != "ul") {
							StringReader a = new StringReader(child.OuterXml);
							XmlDocument n = FromHtml((TextReader)a);
							text += ProcessChild(n.ChildNodes);
						}
					}
					result += marker + " " + text;
					if(!result.EndsWith("\r\n")) result += "\r\n";
					foreach(XmlNode child in node.ChildNodes) {
						if(child.Name.ToString() == "ol") {
							result += ProcessList(child.ChildNodes, marker + ol);
						}
						if(child.Name.ToString() == "ul") {
							result += ProcessList(child.ChildNodes, marker + ul);
						}
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Extracts the URL of an image.
		/// </summary>
		/// <param name="node">The node containing image tag data.</param>
		/// <returns>The image path in WikiMarkup.</returns>
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

		/// <summary>
		/// Processes an image when found in a greater structure.
		/// </summary>
		/// <param name="nodes">Nodes of the structure.</param>
		/// <returns>The correct WikiMarkup for the image.</returns>
		private static string ProcessChildImage(XmlNodeList nodes) {
			string image = "";
			string p = "";
			string url = "";
			string result = "";
			bool hasDescription = false;
			foreach(XmlNode node in nodes) {
				if(node.Name.ToLowerInvariant() == "img")
					image += ProcessImage(node);
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
							if(attName.Value.ToString() == "_blank")
								target += "^";
							if(attName.Name.ToString() == "href")
								link += attName.Value.ToString();
						}
					}
					image += ProcessImage(node.LastChild);
					url = "|" + target + link;
				}
			}
			if(!hasDescription) p = "||";
			result = p + image + url;
			return result;
		}


		/// <summary>
		/// Processes an image when found in a table (usually for 'imageauto' tags).
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		/// <returns>The WikiMarkup for an 'imageauto' tag.</returns>
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
									if(attName.Value.ToString() == "_blank")
										target += "^";
									if(attName.Name.ToString() == "href")
										link += attName.Value.ToString();
									if(attName.Name.ToString() == "title")
										title += attName.Value.ToString();
								}
							}
							result += ProcessTableImage(node.ChildNodes) + "|" + target + link;
						}
						break;
				}
			}
			return result;
		}

		/// <summary>
		/// Processes a table structure.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		/// <returns>The WikiMarkup for the table.</returns>
		private static string ProcessTable(XmlNodeList nodes) {
			string result = "";

			foreach(XmlNode node in nodes) {
				switch(node.Name.ToLowerInvariant()) {
					case "thead":
						result += ProcessTable(node.ChildNodes);
						break;
					case "th":
						result += "! " + ProcessChild(node.ChildNodes) + "\r\n";
						break;
					case "caption":
						result += "|+ " + ProcessChild(node.ChildNodes) + "\r\n";
						break;
					case "tbody":
						result += ProcessTable(node.ChildNodes) + "";
						break;
					case "tr":
						string style = "";
						foreach(XmlAttribute attr in node.Attributes)
							if(attr.Name.ToLowerInvariant() == "style") style += "style=\"" + attr.Value.ToString() + "\" ";

						result += "|- " + style + "\r\n" + ProcessTable(node.ChildNodes);
						//else result += processTable(node.ChildNodes);
						break;
					case "td":
						string styleTd = "";
						if(node.Attributes.Count != 0) {
							foreach(XmlAttribute attr in node.Attributes)
								styleTd += " " + attr.Name + "=\"" + attr.Value.ToString() + "\" ";
							result += "| " + styleTd + " | " + ProcessChild(node.ChildNodes) + "\r\n";
						}
						else result += "| " + ProcessChild(node.ChildNodes) + "\r\n";
						break;
				}

			}
			return result;
		}
		/// <summary>
		/// Recursively processes XML nodes.
		/// </summary>
		/// <param name="nodes">The XML nodes.</param>
		/// <returns>The correspondent WikiMarkup.</returns>
		private static string ProcessChild(XmlNodeList nodes) {
			string result = "";
			foreach(XmlNode node in nodes) {
				bool anchor = false;
				if(node.NodeType == XmlNodeType.Text) {
					result += node.Value;
				}
				else if(node.NodeType != XmlNodeType.Whitespace) {
					switch(node.Name.ToLowerInvariant()) {
						case "html":
							result += ProcessChild(node.ChildNodes);
							break;
						case "b":
						case "strong":
							result += "'''" + ProcessChild(node.ChildNodes) + "'''";
							break;
						case "strike":
						case "s":
							result += "--" + ProcessChild(node.ChildNodes) + "--";
							break;
						case "em":
						case "i":
							result += "''" + ProcessChild(node.ChildNodes) + "''";
							break;
						case "u":
							result += "__" + ProcessChild(node.ChildNodes) + "__";
							break;
						case "h1":
							if(node.HasChildNodes) {
								if(node.FirstChild.NodeType == XmlNodeType.Whitespace) result += "----\r\n" + ProcessChild(node.ChildNodes);
								else result += "==" + ProcessChild(node.ChildNodes) + "==\r\n";
							}
							else result += "----\r\n";
							break;
						case "h2":
							result += "===" + ProcessChild(node.ChildNodes) + "===\r\n";
							break;
						case "h3":
							result += "====" + ProcessChild(node.ChildNodes) + "====\r\n";
							break;
						case "h4":
							result += "=====" + ProcessChild(node.ChildNodes) + "=====\r\n";
							break;
						case "pre":
							result += "@@" + node.InnerText.ToString() + "@@";
							break;
						case "code":
							result += "{{" + ProcessChild(node.ChildNodes) + "}}";
							break;
						case "hr":
						case "hr /":
							result += "\r\n== ==\r\n" + ProcessChild(node.ChildNodes);
							break;
						case "span":
							if(node.Attributes.Count != 0) {
								XmlAttributeCollection attribute = node.Attributes;
								foreach(XmlAttribute attName in attribute) {
									if(attName.Value.ToString() == "italic") {
										result += "''" + ProcessChild(node.ChildNodes) + "''";
									}
								}
							}
							break;
						case "br":
							result += "\r\n" + ProcessChild(node.ChildNodes);
							break;
						case "table":
							bool isImage = false;
							string image = "";
							string tableStyle = "";

							foreach(XmlAttribute attName in node.Attributes) {
								if(attName.Value.ToString() == "imageauto") {
									isImage = true;
									image += "[imageauto|" + ProcessTableImage(node.ChildNodes) + "]\r\n";
								}
								else tableStyle += attName.Name + "=\"" + attName.Value.ToString() + "\" ";
							}
							if(isImage) {
								result += image;
								isImage = false;
								break;
							}
							else result += "{| " + tableStyle + "\r\n" + ProcessTable(node.ChildNodes) + "|}\r\n";
							break;
						case "ol":
							if(node.ParentNode != null) {
								if(node.ParentNode.Name.ToLowerInvariant() != "td") result += ProcessList(node.ChildNodes, "#");
								else result += node.OuterXml.ToString();
							}
							else result += ProcessList(node.ChildNodes, "#");
							break;
						case "ul":
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
							if(node.Attributes.Count != 0) {
								XmlAttributeCollection attribute = node.Attributes;
								foreach(XmlAttribute attName in attribute) {
									if(attName.Value.ToString() == "imagedescription") result += "";
								}
							}
							else result += ProcessChild(node.ChildNodes) + "{BR}\r\n";
							break;
						case "div":
							if(node.Attributes.Count != 0) {
								XmlAttributeCollection attribute = node.Attributes;
								foreach(XmlAttribute attName in attribute) {
									if(attName.Value.ToString() == "box") {
										result += "(((" + ProcessChild(node.ChildNodes) + ")))\r\n";
									}
									if(attName.Value.ToString() == "imageleft") {
										result += "[imageleft" + ProcessChildImage(node.ChildNodes) + "]\r\n";
									}
									if(attName.Value.ToString() == "imageright") {
										result += "[imageright" + ProcessChildImage(node.ChildNodes) + "]\r\n";
									}
									if(attName.Value.ToString() == "image") {
										result += "[image" + ProcessChildImage(node.ChildNodes) + "]\r\n";
									}
									if(attName.Value.ToString() == "indent") {
										result += ": " + ProcessChild(node.ChildNodes) + "\r\n";
									}
								}
							}
							else result += ProcessChild(node.ChildNodes) + "\r\n";
							break;
						case "img":
							string description = "";
							bool hasClass = false;
							bool isLink = false;
							if(node.ParentNode != null) {
								if(node.ParentNode.Name.ToLowerInvariant().ToString() == "a") isLink = true;
							}
							if(node.Attributes.Count != 0) {
								foreach(XmlAttribute attName in node.Attributes) {
									if(attName.Name.ToString() == "alt") description = attName.Value.ToString();
									if(attName.Name.ToString() == "class") hasClass = true;
								}
							}
							if(!hasClass && !isLink) result += "[image|" + description + "|" + ProcessImage(node) + "]\r\n";
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
							if(node.FirstChild != null) {
								if(node.FirstChild.Name.ToLowerInvariant() == "img") childImg = true;
							}
							if(node.ParentNode.Name.ToLowerInvariant() == "td") isTable = true;
							if(node.Attributes.Count != 0) {
								XmlAttributeCollection attribute = node.Attributes;
								foreach(XmlAttribute attName in attribute) {
									if(attName.Name.ToString() != "id".ToLowerInvariant()) {
										if(attName.Value.ToString() == "_blank") target += "^";
										if(attName.Name.ToString() == "href") link += attName.Value.ToString();
										if(attName.Name.ToString() == "title") title += attName.Value.ToString();
										if(attName.Value.ToString() == "systemlink") isInternalLink = true;
									}
									else {
										anchor = true;
										result += "[anchor|#" + attName.Value.ToString().ToLowerInvariant() + "]" + ProcessChild(node.ChildNodes);
										break;
									}
								}
								if(isInternalLink) {
									string[] splittedLink = link.Split('=');
									link = "c:" + splittedLink[1];
								}

								if(!anchor && !isTable && !childImg) {
									if(title != link) result += "[" + target + link + "|" + ProcessChild(node.ChildNodes) + "]";
									else result += "[" + target + link + "|" + ProcessChild(node.ChildNodes) + "]";
								}
								if(!anchor && !childImg && isTable) result += "[" + target + link + "|" + ProcessChild(node.ChildNodes) + "]";
								if(!anchor && childImg && !isTable) result += ProcessChild(node.ChildNodes) + "|" + target + link + "]\r\n";
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

		/// <summary>
		/// Generates a valid XML document from a piece of a arbitrary HTML.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <returns>The valid XML document.</returns>
		private static XmlDocument FromHtml(TextReader reader) {
			Sgml.SgmlReader sgmlReader = new Sgml.SgmlReader();
			sgmlReader.DocType = "HTML";
			sgmlReader.WhitespaceHandling = WhitespaceHandling.All;

			sgmlReader.CaseFolding = Sgml.CaseFolding.ToLower;
			sgmlReader.InputStream = reader;

			XmlDocument doc = new XmlDocument();
			doc.PreserveWhitespace = true;
			doc.XmlResolver = null;
			doc.Load(sgmlReader);
			return doc;
		}

		/// <summary>
		/// Reverse-formats HTML content into WikiMarkup.
		/// </summary>
		/// <param name="html">The input HTML.</param>
		/// <returns>The corresponding WikiMarkup.</returns>
		public static string ReverseFormat(string html) {
			StringReader strReader = new StringReader(html);
			XmlDocument x = FromHtml((TextReader)strReader);
			return ProcessChild(x.FirstChild.ChildNodes);
		}

	}

}
