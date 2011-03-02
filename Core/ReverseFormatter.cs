
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
		/// Processes order or unorder lists and sublists.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		/// <param name="marker">The marker.</param>
		/// <returns>Valid WikiMarkUp text for the lists</returns>
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
						if(child.Name.ToString() == "ol"){
							result += processList(child.ChildNodes, marker + ol);
						}
						if (child.Name.ToString() == "ul") {
							result += processList(child.ChildNodes, marker + ul);
						}
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Processes the image.
		/// </summary>
		/// <param name="node">The node contenent fileName of the image.</param>
		/// <returns>The correct path for wikimarup and image</returns>
		private static string processImage(XmlNode node) {
			string result = "";
			if(node.Attributes.Count != 0) {
				foreach(XmlAttribute attName in node.Attributes) {
					if((attName.Name == "src") || (attName.Value.ToString() == "Image")) {
						string[] path = attName.Value.ToString().Split('=');
						if(path.Length > 2)
							result += "{" + "UP(" + path[1].Split('&')[0] + ")}" + path[2];
						else result += "{UP}" + path[path.Length - 1];
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Processes the child Image.
		/// </summary>
		/// <param name="nodes">Nodelist from an image.</param>
		/// <returns>The correct WikiMarkup for the images </returns>
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
					image += processImage(node.LastChild);
					url = "|" + target + link;
				}
			}
			if (!hasDescription)
				p = "||";
			result = p + image + url;
			return result;
		}

		private static string processCode(string text) {
			string result = "";
			result = text;
			return result;
		}
		/// <summary>
		/// Processes the child.
		/// </summary>
		/// <param name="nodes">A XmlNodeList .</param>
		/// <returns>The corrispondent WikiMarkup Text</returns>
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
						//break;
						case "h1":
							if(node.HasChildNodes)
								result += ("\r\n==" + processChild(node.ChildNodes) + "==\r\n");
							else
								result += ("\r\n== ==\r\n");
							break;
						//break;
						case "h2":
							result += ("\r\n===" + processChild(node.ChildNodes) + "===\r\n");
							break;
						//break;
						case "h3":
							result += ("\r\n====" + processChild(node.ChildNodes) + "====\r\n");
							break;
						//break;
						case "h4":
							result += ("\r\n=====" + processChild(node.ChildNodes) + "=====\r\n");
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
						case "\t":
							result += (":" + processChild(node.ChildNodes));
							break;
						case "éé":
							result += ("~~~~" + processChild(node.ChildNodes));
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
							foreach(XmlAttribute attName in node.Attributes) {
								if(attName.Value.ToString() == "imageauto") {
									isImage = true;
									image += "[imageauto|" + processChild(node.ChildNodes) + "]\r\n";
								}
							}
							if(isImage) {
								result += image;
								isImage = false;
								break;
							}
							else result += processChild(node.ChildNodes);
							break;
						case "tbody":
							result += processChild(node.ChildNodes);
							break;
						case "tr":
							result += processChild(node.ChildNodes);
							break;
						case "td":
								result += processChild(node.ChildNodes);
							break;
						case "ol":
							result += processList(node.ChildNodes, "#");
							break;
						case "ul":
							result += processList(node.ChildNodes, "*");
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
										result += "\r\n" + "(((" + processChild(node.ChildNodes) + ")))\r\n";
									}
									if(attName.Value.ToString() == "imageleft") {
										result += "\r\n" + "[imageleft" + processChildImage(node.ChildNodes) + "]\r\n";
									}
									if(attName.Value.ToString() == "imageright")
										result += "\r\n" + "[imageright" + processChildImage(node.ChildNodes) + "]\r\n";
									if(attName.Value.ToString() == "image")
										result += "\r\n" + "[image" + processChildImage(node.ChildNodes) + "]\r\n";
								}
							}
							else
								result += "\r\n" + (processChild(node.ChildNodes) + "\r\n");
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
							bool childImg = false;
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
									}
									else {
										anchor = true;
										result += "[anchor|#" + attName.Value.ToString().ToLowerInvariant() + "]" + processChild(node.ChildNodes);
										break;
									}
								}

								if((!anchor) && (!isTable) && (!childImg))
									if(title != link)
										result += "[" + target + link + "|" + processChild(node.ChildNodes) + "]";
									else
										result += "[" + target + link + "|" + processChild(node.ChildNodes) + "]";
								if((!anchor) && (isTable))
									result += processChild(node.ChildNodes) + "|" + target + link;
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

		/// <summary>
		/// Froms the HTML.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <returns>valid XML Document</returns>
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
			return processChild(x.FirstChild.ChildNodes);
		}
	}
}