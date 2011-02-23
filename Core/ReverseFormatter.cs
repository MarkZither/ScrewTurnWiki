
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

		private static readonly Regex WebkitDivRegex = new Regex(@"(<div>)((.|\n|\r)*?)(</div>)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex BoldRegex = new Regex(@"(<b>)((.|\n|\r)*?)(</b>)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex ItalicRegex = new Regex(@"(<i>)((.|\n|\r)*?)(</i>)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex UnderlineRegex = new Regex(@"(<u>)((.|\n|\r)*?)(</u>)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex StrikeRegex = new Regex(@"(<strike>)((.|\n|\r)*?)(</strike>)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex H1Regex = new Regex(@"(<h1 class=""?separator""?>)((.|\n|\r)*?)(</h1>)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex H2Regex = new Regex(@"(<h2 class=""?separator""?>)((.|\n|\r)*?)(</h2>)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex H3Regex = new Regex(@"(<h3 class=""?separator""?>)((.|\n|\r)*?)(</h3>)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex H4Regex = new Regex(@"(<h4 class=""?separator""?>)((.|\n|\r)*?)(</h4>)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex PageLinkRegex = new Regex(@"(<a class=""pagelink"" (target=""_blank"" )?href=""((.)*?)\.ashx"" title=""(.)*?"">)((.|\n|\r)*?)(</a>)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex UnknownLinkRegex = new Regex(@"(<a class=""unknownlink"" (target=""_blank"" )?href=""((.)*?)\.ashx"" title=""(.)*?"">)((.|\n|\r)*?)(</a>)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex FileLinkRegex = new Regex(@"(<a class=""internallink"" (target=""_blank"" )?href=""GetFile\.aspx\?(Provider=((.)+?)&amp;)?File=((.)+?)"" title=""((.)+?)"">)((.|\n|\r)+?)(</a>)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex AttachmentLinkRegex = new Regex(@"(<a class=""internallink"" (target=""_blank"" )?href=""GetFile\.aspx\?(Provider=((.)*?)&amp;)?Page=((.)+?)&amp;File=((.)+?)"" title=""((.)+?)"">)((.|\n|\r)+?)(</a>)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex SystemLinkRegex = new Regex(@"<a class=""systemlink"" (target=""_blank"" )?href=""((.)*?)"" title=""((.)*?)"">((.|\n|\r)*?)</a>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex ExternalLinkRegex = new Regex(@"(<a class=""externallink"" href=""((.)*?)"" title=""((.)*?)"" (target=""_blank"")?>)((.|\n|\r)*?)(</a>)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex InternalLinkRegex = new Regex(@"<a class=""internallink"" (target=""_blank"" )?href=""((.)*?)"" title=""((.)*?)"">((.|\n|\r)*?)</a>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex AnchorLinkRegex = new Regex(@"<a href=""#((.)*?)"" class=""internallink"" (target=""_blank"" )?title=""((.)*?)"">((.|\n|\r)*?)</a>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex EmailLinkRegex = new Regex(@"(<a class=""emaillink"" (target=""_blank"" )?href=""mailto:((.)*?)"" title=""(.)*?"">)((.|\n|\r)*?)(</a>)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex AnchorRegex = new Regex(@"<a id=""?(.+?)""?>(.*?)</a>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex ImageLeftRightRegex = new Regex(@"(<div class=""(imageleft|imageright)"">|<table class=""imageauto"" cellpadding=""0"" cellspacing=""0""><tbody><tr><td>)(<a href=""((.)*?)""( target=""(_blank)"")? title="".+?"">)?<img class=""image"" src=""((.)*?)"" alt=""(.)*?"">(</a>)?(<p class=""imagedescription"">((.)*?)</p>)?(</div>|</td></tr></tbody></table>)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex ImageInlineRegex = new Regex(@"(<a( target=""(_blank)"")? href=""((.)*?)"" title="".*?"">)?<img src=""((.)*?)"" alt=""((.)*?)"">(</a>)?", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex HRRegex = new Regex(@"<h1 class=""?separator""?>\s*</h1>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex BoxRegex = new Regex(@"<div class=""?box""?>((.|\n|\r)*?)</div>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex CodeRegex = new Regex(@"<code>((.|\n|\r)*?)</code>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex PreRegex = new Regex(@"<pre>((.|\n|\r)*?)</pre>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex SingleBR = new Regex(@"(?<!<br />)<br />(?!<br />)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex SingleNewLine = new Regex(@"(?<!\\r\\n)\\r\\n(?!\\r\\n)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		// Title=1 - Href=2 - Target=3 - Content=4 --- Href=http://www.server.com/Spaced%20Page.ashx
		private static readonly Regex PageLinkRegexIE = new Regex(@"<a class=pagelink title=\""?(.*?)\""? href=\""(.*?)\""( target=_blank)?>(.*?)</a>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		// Title=1 - Href=2 - Target=3 - Content=4 --- Href=http://www.server.com/Spaced%20Page.ashx
		private static readonly Regex UnknownLinkRegexIE = new Regex(@"<a class=unknownlink title=\""?(.*?)\""? href=\""(.*?)\""( target=_blank)?>(.*?)</a>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		// Title=1 - ProviderGlobal=3 - Provider=4 - Page=6 - File=7 - Target=8 - Content=9
		private static readonly Regex FileOrAttachmentLinkRegexIE = new Regex(@"<a class=internallink title=\""?(.*?)\""? href=\""(.*?)GetFile.aspx\?(Provider=(.*?)&amp;)?(Page=(.+?)&amp;)?File=(.*?)\""( target=_blank)?>(.*?)</a>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		// Title=1 - Href=2 - Target=3 - Content=4 --- Href=http://www.server.com/Register.aspx
		private static readonly Regex SystemLinkRegexIE = new Regex(@"<a class=systemlink title=\""?(.*?)\""? href=\""(.*?)""( target=_blank)?>(.*?)</a>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		// Title=1 - Href=2 - Target=3 - Content=4
		private static readonly Regex ExternalLinkRegexIE = new Regex(@"<a class=externallink title=\""?(.*?)\""? href=\""(.*?)""( target=_blank)?>(.*?)</a>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		// Title=1 - Href=2 - Target=3 - Content=4
		private static readonly Regex InternalLinkRegexIE = new Regex(@"<a class=internallink title=\""?(.*?)\""? href=\""(.*?)""( target=_blank)?>(.*?)</a>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		// AnchorLinkRegexIE would be equal to InternalLinkRegex - no need for it

		// Title=1 - Href=2 - Target=3 - Content=4
		private static readonly Regex EmailLinkRegexIE = new Regex(@"<a class=emaillink title=\""?(.*?)\""? href=\""(.*?)""( target=_blank)?>(.*?)</a>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		// DivClass=1 - A=2 - ATitle=3 - AHref=4 - ATarget=5 - ImageAlt=6 - ImageSrc=7 - P=9 - PContent=10 --- Href/Src=http://www.server.com/Blah.ashx/GetFile.aspx...
		private static readonly Regex ImageLeftRightRegexIE = new Regex(@"<div class=(imageleft|imageright)>(<a title=\""?(.*?)\""? href=\""(.*?)\""( target=_blank)?>)?<img class=image alt=\""?(.*?)\""? src=\""(.*?)\"">(</a>)?(\r\n<p class=imagedescription>(.*?)</p>)?</div>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		// A=1 - ATitle=2 - AHref=3 - ATarget=4 - ImageAlt=5 - ImageSrc=6 - P=8 - PContent=9 --- Href/Src=http://www.server.com/Blah.ashx/GetFile.aspx...
		private static readonly Regex ImageAutoRegexIE = new Regex(@"<table class=imageauto cellspacing=0 cellpadding=0>\r\n<tbody>\r\n<tr>\r\n<td>(<a title=\""?(.*?)\""? href=\""(.*?)\""( target=_blank)?>)?<img class=image alt=\""?(.*?)\""? src=\""(.*?)\"">(</a>)?(\r\n<p class=imagedescription>(.*?)</p>)?</td></tr></tbody></table>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		// A=1 - ATitle=2 - AHref=3 - ATarget=4 - ImageAlt=5 - ImageSrc=6 --- Href/Src=http://www.server.com/Blah.ashx/GetFile.aspx...
		private static readonly Regex ImageInlineRegexIE = new Regex(@"(<a title=\""?(.*?)\"" href=\""(.*?)\""?( target=_blank)?>)?<img alt=\""?(.*?)\""? src=\""(.*?)\"">(</a>)?", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);


		private static List<string> listText= new List<string>();
		//private static string result = "";


		/// <summary>
		/// Processes the image.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <returns></returns>
		private static string processImage(XmlNode node) {
			string result = "";
			if(node.Attributes.Count != 0) {
				foreach(XmlAttribute attName in node.Attributes) {
					if(attName.Name.ToString() == "src") {
						string[] path = attName.Value.ToString().Split('=');
					//result += "|" + processChild(node.ChildNodes);
					result += "{" + "UP(" + path[1].Split('&')[0] + ")}" + path[2];
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Processes the child Image.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		/// <returns></returns>
		private static string processChildImage(XmlNodeList nodes) {
			string image ="";
			string p ="";
			string url = "";
			string result ="";
			foreach(XmlNode node in nodes) {
				if(node.Name.ToLowerInvariant() == "img")
					image += processImage(node);
				else if(node.Name.ToLowerInvariant() == "p") {
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
			result = p+image+ url;
			return result;
		}
		/// <summary>
		/// Processes the child.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		/// <returns></returns>
		private static string processChild(XmlNodeList nodes) {
			string result = "";
			foreach(XmlNode node in nodes) {
				bool anchor = false;
				if(node.NodeType == XmlNodeType.Text) {
					result += node.Value;
					//string result = "";
				}
				else {
					switch(node.Name.ToLowerInvariant()) {
						case "b":
						case "strong":
							result += ("'''" + processChild(node.ChildNodes) + "'''");
							break;
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
							result += ("==" + processChild(node.ChildNodes) + "==");
							break;
						//break;
						case "h2":
							result += ("===" + processChild(node.ChildNodes) + "===");
							break;
						//break;
						case "h3":
							result += ("====" + processChild(node.ChildNodes) + "====");
							break;
						//break;
						case "h4":
							result += ("=====" + processChild(node.ChildNodes) + "=====");
							break;
						case "pre":
							result += ("@@" + node.InnerText.ToString() + "@@");
							break;
						case "code":
							result += ("{{" + processChild(node.ChildNodes) + "}}");
							break;
						case "hr":
						case "hr /":
							result += ("----" + processChild(node.ChildNodes));
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
						case "\n":
						case "br":
							result += ("{br}" + processChild(node.ChildNodes));
							break;
						case "ol":
							result += processChild(node.ChildNodes) + "{br}";
							break;
						case "ul":
							result += processChild(node.ChildNodes) + "{br}";
							break;
						case "table":
							result +=  processChild(node.ChildNodes);
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
						case "li":
							if (node.ParentNode.Name.ToLowerInvariant() == "ol")
								result += ("#" + " "+processChild(node.ChildNodes) + "{br}");
							else if (node.ParentNode.Name.ToLowerInvariant() == "ul")
								result += ("*" + " "+ processChild(node.ChildNodes) + "{br}");
							else if(node.ParentNode.Name.ToLowerInvariant() == "li") 
								if(node.ParentNode.ParentNode.Name.ToLowerInvariant() == "ol")
									result += ("*#" + " " + processChild(node.ChildNodes));
								else if(node.ParentNode.ParentNode.Name.ToLowerInvariant() == "ul")
									result += ("#*" + " " + processChild(node.ChildNodes));
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
							break;
						case "div":
							if(node.Attributes.Count != 0) {
								XmlAttributeCollection attribute = node.Attributes;
								foreach(XmlAttribute attName in attribute) {
									if (attName.Value.ToString() == "box"){
										result += "(((" + processChild(node.ChildNodes) + "))){br}";
									}
									if(attName.Value.ToString() == "imageleft") {
										result += "[imageleft" + processChildImage(node.ChildNodes) + "]{br}";											
									}
									if(attName.Value.ToString() == "imageright")
										result += "[imageleft" + processChildImage(node.ChildNodes) + "]{br}";
									if(attName.Value.ToString() == "imageauto")
										result += "[imageleft" + processChildImage(node.ChildNodes) + "]{br}";
								}
							}
							else
								result += (processChild(node.ChildNodes) + "{br}");
							break;

						case "img":
							if(node.Attributes.Count != 0) {
								XmlAttributeCollection attribute = node.Attributes;
								foreach(XmlAttribute attName in attribute) {
									//if(attName.Name.ToString() == "src") {
									//	string[] path = attName.Value.ToString().Split('=');
										//result += "|" + processChild(node.ChildNodes);
										result += processImage(node);
									//}
								}
							}
							break;
						case "a":
							string link="";
							string target="";
							string title="";
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
									else{
										anchor = true;
										result += "[anchor|#" + attName.Value.ToString().ToLowerInvariant() + "]" + processChild(node.ChildNodes);
										break;
									}
								}
								if(!anchor)
									if(title != link)
										result += "[" + target + link + "|" + processChild(node.ChildNodes) + "]";
									else
										result += "[" + target + link + "|" + "]" + processChild(node.ChildNodes);
							}
						break;

						default:
							result += (node.OuterXml);
							break;
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Froms the HTML.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <returns></returns>
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
			string text = processChild(x.FirstChild.ChildNodes);
			//StringBuilder t = new StringBuilder(html);
			//result = "";
			listText.Clear();
			return text;
		}


		/// <summary>
		/// Reverse formats HTML content into WikiMarkup.
		/// </summary>
		/// <param name="html">The input HTML.</param>
		/// <returns>The corresponding WikiMarkup.</returns>
		public static string ReverseFormatOld(string html) {

			Match match = null;
			StringBuilder buffer = new StringBuilder(html);
			if(!html.EndsWith("\r\n")) buffer.Append("\r\n");

			buffer.Replace("<br>", "<br />");
			buffer.Replace("<BR>", "<br />");

			buffer.Replace("<em>", "<i>");
			buffer.Replace("<EM>", "<i>");
			buffer.Replace("</em>", "</i>");
			buffer.Replace("</EM>", "</i>");
			buffer.Replace("<strong>", "<b>");
			buffer.Replace("<STRONG>", "<b>");
			buffer.Replace("</strong>", "</b>");
			buffer.Replace("</STRONG>", "</b>");
			buffer.Replace("<P>", "<p>");
			buffer.Replace("</P>", "</p>");

			buffer.Replace("&amp;amp;", "&amp;");

			// Escape square brackets, otherwise they're interpreted as links
			buffer.Replace("[", "&#91;");
			buffer.Replace("]", "&#93;");

			// #469: IE seems to randomly add this stuff
			buffer.Replace("<p>&nbsp;</p>\r\n", "<br />");

			buffer.Replace("<p>", "");
			buffer.Replace("</p>", "");

			// Temporarily replace <br /> in <pre> tags
			match = PreRegex.Match(buffer.ToString());
			while(match.Success) {
				Match subMatch = SingleBR.Match(match.Value);
				while(subMatch.Success) {
					buffer.Remove(match.Index + subMatch.Index, subMatch.Length);
					buffer.Insert(match.Index + subMatch.Index, "<br-/>");
					subMatch = SingleBR.Match(match.Value, subMatch.Index + 1);
				}
				match = PreRegex.Match(buffer.ToString(), match.Index + 1);
			}
			buffer.Replace("<br-/>", "\r\n");

			// Code
			match = CodeRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				buffer.Insert(match.Index, "{{" + match.Value.Substring(6, match.Length - 13) + "}}");
				match = CodeRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// Pre
			// Unescape square brackets
			match = PreRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				buffer.Insert(match.Index, "@@" +
					match.Value.Substring(5, match.Length - 11).Replace("&amp;", "&").Replace("&#91;", "[").Replace("&#93;", "]") +
					"@@");
				match = PreRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// WebkitDivRegex
			// Remove all div added by webkit and replace them with \r\n.
			match = WebkitDivRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				buffer.Insert(match.Index, "\r\n" + match.Groups[2].Value);
				match = WebkitDivRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// Bold
			match = BoldRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				buffer.Insert(match.Index, "'''" + match.Groups[2].Value + "'''");
				match = BoldRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// Italic
			match = ItalicRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				buffer.Insert(match.Index, "''" + match.Groups[2].Value + "''");
				match = ItalicRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// Underline
			match = UnderlineRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				buffer.Insert(match.Index, "__" + match.Groups[2].Value + "__");
				match = UnderlineRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// Strike
			match = StrikeRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				buffer.Insert(match.Index, "--" + match.Groups[2].Value + "--");
				match = StrikeRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// Horizontal Ruler
			match = HRRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				buffer.Insert(match.Index, "----");
				match = HRRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// H1
			match = H1Regex.Match(buffer.ToString());
			while(match.Success) {
				char c = buffer[match.Index + match.Length];
				bool addNewLine = false;
				if(buffer[match.Index + match.Length] != '\n') addNewLine = true;
				buffer.Remove(match.Index, match.Length);
				if(addNewLine) buffer.Insert(match.Index, "==" + match.Groups[2].Value + "==\n");
				else buffer.Insert(match.Index, "==" + match.Groups[2].Value + "==");
				match = H1Regex.Match(buffer.ToString(), match.Index + 1);
			}

			// H2
			match = H2Regex.Match(buffer.ToString());
			while(match.Success) {
				bool addNewLine = false;
				if(buffer[match.Index + match.Length] != '\n') addNewLine = true;
				buffer.Remove(match.Index, match.Length);
				if(addNewLine) buffer.Insert(match.Index, "===" + match.Groups[2].Value + "===\n");
				else buffer.Insert(match.Index, "===" + match.Groups[2].Value + "===");
				match = H2Regex.Match(buffer.ToString(), match.Index + 1);
			}

			// H3
			match = H3Regex.Match(buffer.ToString());
			while(match.Success) {
				bool addNewLine = false;
				if(buffer[match.Index + match.Length] != '\n') addNewLine = true;
				buffer.Remove(match.Index, match.Length);
				if(addNewLine) buffer.Insert(match.Index, "====" + match.Groups[2].Value + "====\n");
				else buffer.Insert(match.Index, "====" + match.Groups[2].Value + "====");
				match = H3Regex.Match(buffer.ToString(), match.Index + 1);
			}

			// H4
			match = H4Regex.Match(buffer.ToString());
			while(match.Success) {
				bool addNewLine = false;
				if(buffer[match.Index + match.Length] != '\n') addNewLine = true;
				buffer.Remove(match.Index, match.Length);
				if(addNewLine) buffer.Insert(match.Index, "=====" + match.Groups[2].Value + "=====\n");
				else buffer.Insert(match.Index, "=====" + match.Groups[2].Value + "=====");
				match = H4Regex.Match(buffer.ToString(), match.Index + 1);
			}

			// Lists
			buffer.Replace("<UL>", "<ul>");
			buffer.Replace("</UL>", "</ul>");
			buffer.Replace("<OL>", "<ol>");
			buffer.Replace("</OL>", "</ol>");
			buffer.Replace("<LI>", "<li>");
			buffer.Replace("</LI>", "</li>");
			ProcessLists(buffer);

			// Page Link 
			match = PageLinkRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[";
				if(match.Groups[2].Value == @"target=""_blank"" ") insertion += "^";
				string decoded = UrlDecode(match.Groups[3].Value);
				insertion += (decoded.StartsWith("  ") ? "++" : "") + decoded.Trim();
				if(match.Groups[6].Value != decoded) insertion += "|" + match.Groups[6].Value;
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = PageLinkRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// Page Link IE
			match = PageLinkRegexIE.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[";
				if(match.Groups[3].Value == " target=_blank") insertion += "^";
				string page = match.Groups[2].Value.Substring(match.Groups[2].Value.LastIndexOf("/") + 1);
				page = page.Substring(0, page.Length - 5); // Remove .ashx
				page = UrlDecode(page);
				insertion += page;
				if(match.Groups[4].Value != page) insertion += "|" + match.Groups[4].Value;
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = PageLinkRegexIE.Match(buffer.ToString(), match.Index + 1);
			}

			// Unknown Link
			match = UnknownLinkRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[";
				if(match.Groups[2].Value == @"target=""_blank"" ") insertion += "^";
				string decoded = UrlDecode(match.Groups[3].Value);
				insertion += decoded;
				if(match.Groups[6].Value != decoded) insertion += "|" + match.Groups[6].Value;
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = UnknownLinkRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// Unknown Link IE
			match = UnknownLinkRegexIE.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[";
				if(match.Groups[3].Value == " target=_blank") insertion += "^";
				string page = match.Groups[2].Value.Substring(match.Groups[2].Value.LastIndexOf("/") + 1);
				page = page.Substring(0, page.Length - 5); // Remove .ashx
				page = UrlDecode(page);
				insertion += page;
				if(match.Groups[4].Value != page) insertion += "|" + match.Groups[4].Value;
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = UnknownLinkRegexIE.Match(buffer.ToString(), match.Index + 1);
			}

			// File Link
			match = FileLinkRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[";
				if(match.Groups[2].Value == @"target=""_blank"" ") insertion += "^";
				if(match.Groups[3].Value != "") insertion += "{UP:" + match.Groups[4].Value + "}" + UrlDecode(match.Groups[6].Value);
				else insertion += "{UP}" + UrlDecode(match.Groups[6].Value);
				if(!match.Groups[10].Value.StartsWith("GetFile.aspx") && !match.Groups[10].Value.StartsWith("{UP")) insertion += "|" + match.Groups[10];
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = FileLinkRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// File Link IE
			match = FileOrAttachmentLinkRegexIE.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[";
				if(match.Groups[8].Value == " target=_blank") insertion += "^";
				if(match.Groups[3].Value != "") insertion += "{UP:" + match.Groups[4].Value;
				else insertion += "{UP";
				if(match.Groups[6].Value != "") insertion += "(" + UrlDecode(match.Groups[6].Value) + ")";
				insertion += "}";
				insertion += UrlDecode(match.Groups[7].Value);
				if(!match.Groups[9].Value.StartsWith("GetFile.aspx") && !match.Groups[9].Value.StartsWith("{UP")) insertion += "|" + match.Groups[9].Value;
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = FileOrAttachmentLinkRegexIE.Match(buffer.ToString(), match.Index + 1);
			}

			// Attachment Link
			match = AttachmentLinkRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[";
				if(match.Groups[2].Value == @"target=""_blank"" ") insertion += "^";
				// if the provider is not present "{UP" is added without ":providername"
				insertion += match.Groups[4].Value == "" ? "{UP" : "{UP:" + match.Groups[4].Value;
				insertion += "(" + UrlDecode(match.Groups[6].Value) + ")}" + UrlDecode(match.Groups[8].Value);
				if(!match.Groups[12].Value.StartsWith("GetFile.aspx") && !match.Groups[12].Value.StartsWith("{UP")) insertion += "|" + match.Groups[12];
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = AttachmentLinkRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// External Link
			match = ExternalLinkRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[";
				//if(match.Groups[6].Value == @"target=""_blank""") insertion += "^";
				string url = match.Groups[2].Value;
				if(url.StartsWith(Settings.MainUrl)) url = url.Substring(Settings.MainUrl.Length);
				insertion += url;
				if(match.Groups[7].Value != match.Groups[2].Value && match.Groups[7].Value + "/" != match.Groups[2].Value) insertion += "|" + match.Groups[7].Value;
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = ExternalLinkRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// External Link IE
			match = ExternalLinkRegexIE.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[";
				string url = match.Groups[2].Value;
				if(url.StartsWith(Settings.MainUrl)) url = url.Substring(Settings.MainUrl.Length);
				insertion += url;
				if(match.Groups[4].Value != match.Groups[2].Value.TrimEnd('/')) insertion += "|" + match.Groups[4].Value;
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = ExternalLinkRegexIE.Match(buffer.ToString(), match.Index + 1);
			}

			// Internal Link
			match = InternalLinkRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[";
				if(match.Groups[1].Value == @"target=""_blank""") insertion += "^";
				string url = match.Groups[2].Value;
				if(url.StartsWith(Settings.MainUrl)) url = url.Substring(Settings.MainUrl.Length);
				insertion += url;
				string decoded = UrlDecode(match.Groups[6].Value);
				if(match.Groups[2].Value != decoded) insertion += "|" + decoded;
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = InternalLinkRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// Internal Link IE
			match = InternalLinkRegexIE.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[";
				if(match.Groups[3].Value == " target=_blank") insertion += "^";
				string url = match.Groups[2].Value;
				if(url.StartsWith(Settings.MainUrl)) url = url.Substring(Settings.MainUrl.Length);
				insertion += url;
				string decoded = UrlDecode(match.Groups[4].Value);
				if(decoded != match.Groups[2].Value) insertion += "|" + decoded;
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = InternalLinkRegexIE.Match(buffer.ToString(), match.Index + 1);
			}

			// Anchor Link
			match = AnchorLinkRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[";
				if(match.Groups[3].Value != "") insertion += "^";
				insertion += "#";
				insertion += match.Groups[1].Value;
				string val = match.Groups[6].Value.ToLowerInvariant().Replace("&nbsp;", "");
				if(val != "") insertion += "|" + val;
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = AnchorLinkRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// System Link (.aspx)
			match = SystemLinkRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[";
				if(match.Groups[1].Value == @"target=""_blank""") insertion += "^";
				insertion += match.Groups[2].Value;
				string decoded = UrlDecode(match.Groups[6].Value);
				if(match.Groups[2].Value != decoded) insertion += "|" + decoded;
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = SystemLinkRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// System Link IE
			match = SystemLinkRegexIE.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[";
				if(match.Groups[3].Value == " target=_blank") insertion += "^";
				string url = match.Groups[2].Value.Substring(match.Groups[2].Value.LastIndexOf("/") + 1);
				insertion += url;
				string decoded = UrlDecode(match.Groups[4].Value);
				if(decoded != url) insertion += "|" + decoded;
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = SystemLinkRegexIE.Match(buffer.ToString(), match.Index + 1);
			}

			// Email Link 
			match = EmailLinkRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[";
				if(match.Groups[2].Value == @"target=""_blank"" ") insertion += "^";
				insertion += match.Groups[3].Value;
				string decoded = UrlDecode(match.Groups[6].Value);
				if(decoded != match.Groups[3].Value) insertion += "|" + decoded;
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = EmailLinkRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// Email Link IE
			match = EmailLinkRegexIE.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[";
				insertion += match.Groups[2].Value.Substring(7); // Remove mailto:
				string decoded = UrlDecode(match.Groups[4].Value);
				if(decoded != match.Groups[2].Value.Substring(7)) insertion += "|" + decoded;
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = EmailLinkRegexIE.Match(buffer.ToString(), match.Index + 1);
			}

			// Anchor
			match = AnchorRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				buffer.Insert(match.Index, "[anchor|#" + match.Groups[1].Value + "]");
				match = AnchorRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// Image Left/Right/Auto
			match = ImageLeftRightRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[";
				if(match.Groups[1].Value.StartsWith("<div")) insertion += match.Groups[2].Value + "|";
				else insertion += "imageauto|";
				insertion += match.Groups[13].Value + "|";
				insertion += PrepareImageUrl(match.Groups[8].Value);
				if(match.Groups[3].Value != "") {
					insertion += "|";
					if(match.Groups[6].Value != "") insertion += "^";
					insertion += PrepareLink(match.Groups[4].Value);
				}
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = ImageLeftRightRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// Image Left/Right IE
			match = ImageLeftRightRegexIE.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[";
				insertion += match.Groups[1].Value + "|";
				insertion += match.Groups[10].Value + "|";
				insertion += PrepareImageUrl(match.Groups[7].Value);
				if(match.Groups[4].Value != "") {
					insertion += "|";
					if(match.Groups[5].Value != "") insertion += "^";
					insertion += PrepareLink(match.Groups[4].Value);
				}
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = ImageLeftRightRegexIE.Match(buffer.ToString(), match.Index + 1);
			}

			// Image Auto IE
			match = ImageAutoRegexIE.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[imageauto|";
				insertion += match.Groups[9].Value + "|";
				insertion += PrepareImageUrl(match.Groups[6].Value);
				if(match.Groups[1].Value != "") {
					insertion += "|";
					if(match.Groups[4].Value != "") insertion += "^";
					insertion += PrepareLink(match.Groups[3].Value);
				}
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = ImageAutoRegexIE.Match(buffer.ToString(), match.Index);
			}

			// Image Inline
			match = ImageInlineRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[image|";
				if(match.Groups[8].Value != "Image") insertion += match.Groups[8].Value;
				insertion += "|";
				insertion += PrepareImageUrl(match.Groups[6].Value);
				if(match.Groups[4].Value != "") {
					insertion += "|";
					if(match.Groups[2].Value != "") insertion += "^";
					insertion += PrepareLink(match.Groups[4].Value);
				}
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = ImageInlineRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// Image Inline IE
			match = ImageInlineRegexIE.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				string insertion = "[image|";
				if(match.Groups[5].Value != "Image") insertion += match.Groups[5].Value;
				insertion += "|";
				insertion += PrepareImageUrl(match.Groups[6].Value);
				if(match.Groups[3].Value != "") {
					insertion += "|";
					if(match.Groups[3].Value != "") insertion += "^";
					insertion += PrepareLink(match.Groups[3].Value);
				}
				insertion += "]";
				buffer.Insert(match.Index, insertion);
				match = ImageInlineRegexIE.Match(buffer.ToString(), match.Index + 1);
			}

			// Box
			match = BoxRegex.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				buffer.Insert(match.Index, "(((" + match.Groups[1].Value + ")))");
				match = BoxRegex.Match(buffer.ToString(), match.Index + 1);
			}

			// <br />
			buffer.Replace("<br />", "\r\n");

			// Fix line breaks in IE
			buffer.Replace("\r\n\r\n\r\n=====", "\r\n\r\n=====");
			buffer.Replace("\r\n\r\n\r\n====", "\r\n\r\n====");
			buffer.Replace("\r\n\r\n\r\n===", "\r\n\r\n===");
			buffer.Replace("\r\n\r\n\r\n==", "\r\n\r\n==");
			buffer.Replace("\r\n\r\n\r\n----", "\r\n\r\n----");
			buffer.Replace("\r\n\r\n\r\n* ", "\r\n\r\n* ");
			buffer.Replace("\r\n\r\n\r\n# ", "\r\n\r\n# ");

			match = SingleNewLine.Match(buffer.ToString());
			while(match.Success) {
				buffer.Remove(match.Index, match.Length);
				buffer.Insert(match.Index, "{BR}");
				match = SingleNewLine.Match(buffer.ToString(), match.Index);
			}

			buffer.Replace("&lt;", "<");
			buffer.Replace("&gt;", ">");

			string result = buffer.ToString();

			return result.TrimEnd('\r', '\n');
		}

		/// <summary>
		/// Processes unordered and ordered lists.
		/// </summary>
		/// <param name="sb">The string builder buffer.</param>
		private static void ProcessLists(StringBuilder sb) {
			string temp = null;

			int ulIndex = -1;
			int olIndex = -1;

			int lastIndex = 0;

			do {
				temp = sb.ToString().ToLowerInvariant();

				ulIndex = temp.IndexOf("<ul>", lastIndex);
				olIndex = temp.IndexOf("<ol>", lastIndex);

				if(ulIndex != -1 || olIndex != -1) {
					// 1. Find tag pairs
					// 2. Extract block and remove it from SB
					// 3. Process block and generate WikiMarkup output
					// 4. Insert new markup in SB at original position

					if(ulIndex != -1 && (ulIndex < olIndex || olIndex == -1)) {
						// Find a UL block
						int openIndex, closeIndex;

						if(FindTagsPair(sb, "<ul>", "</ul>", lastIndex, out openIndex, out closeIndex)) {
							string section = sb.ToString().Substring(openIndex, closeIndex - openIndex + 5);
							sb.Remove(openIndex, closeIndex - openIndex + 5);

							string result = ProcessList(false, section);

							sb.Insert(openIndex, result);

							// Skip processed data
							lastIndex = openIndex + result.Length;
						}
						else lastIndex += 4;

						continue;
					}

					if(olIndex != -1 && (olIndex < ulIndex || ulIndex == -1)) {
						// Find a OL block
						int openIndex, closeIndex;

						if(FindTagsPair(sb, "<ol>", "</ol>", lastIndex, out openIndex, out closeIndex)) {
							string section = sb.ToString().Substring(openIndex, closeIndex - openIndex + 5);
							sb.Remove(openIndex, closeIndex - openIndex + 5);

							string result = ProcessList(true, section);

							sb.Insert(openIndex, result);

							// Skip processed data
							lastIndex = openIndex + result.Length;
						}
						else lastIndex += 4;

						continue;
					}
				}

			} while(ulIndex != -1 || olIndex != -1);
		}

		/// <summary>
		/// Processes an unordered or ordered list.
		/// </summary>
		/// <param name="ordered"><c>true</c> for an ordered list, <c>false</c> for an unordered list.</param>
		/// <param name="html">The input HTML.</param>
		/// <returns>The output WikiMarkup.</returns>
		private static string ProcessList(bool ordered, string html) {
			HtmlList list = BuildListTree(ordered, html);

			string wikiMarkup = BuildListWikiMarkup(list, "");

			return wikiMarkup.TrimEnd('\r', '\n');
		}

		/// <summary>
		/// Builds the WikiMarkup for a list.
		/// </summary>
		/// <param name="list">The root list.</param>
		/// <param name="previousBullets">The previous bullets, used at upper levels.</param>
		/// <returns>The WikiMarkup.</returns>
		private static string BuildListWikiMarkup(HtmlList list, string previousBullets) {
			previousBullets = previousBullets + (list.Type == HtmlListType.Ordered ? "#" : "*");

			StringBuilder sb = new StringBuilder(500);

			foreach(HtmlListElement elem in list.Elements) {
				sb.Append(previousBullets);
				sb.Append(" ");
				sb.Append(elem.Text);
				sb.Append("\r\n");

				if(elem.SubList != null) {
					sb.Append(BuildListWikiMarkup(elem.SubList, previousBullets));
				}
			}

			// Remove empty lines in the middle of the list
			string raw = sb.ToString().Replace("\r", "");
			string[] lines = raw.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

			return
				string.Join("\r\n", lines) +
				(raw.EndsWith("\r\n") || raw.EndsWith("\n") ? "\r\n" : "");
		}

		/// <summary>
		/// Builds a list tree.
		/// </summary>
		/// <param name="ordered"><c>true</c> for an ordered list.</param>
		/// <param name="html">The input HTML.</param>
		/// <returns>The list tree.</returns>
		private static HtmlList BuildListTree(bool ordered, string html) {
			string[] tags = new string[] { "<ol>", "<ul>", "<li>", "</li>", "</ul>", "</ol>" };

			// IE seems to add new-lines after some elements
			// \r\n are never added by the Formatter, so it is safe to remove all them
			html = html.Replace("\r", "");
			html = html.Replace("\n", "");

			int index = 0;
			int lastOpenListItemIndex = 0;
			int stringFound;

			HtmlList root = new HtmlList(ordered ? HtmlListType.Ordered : HtmlListType.Unordered);
			HtmlList currentList = root;

			do {
				index = FirstIndexOfAny(html, index, out stringFound, tags);

				if(index != -1) {
					switch(stringFound) {
						case 0: // <ol>
							// Unless at the beginning, start a new sub-list
							if(index != 0) {
								// Set text of current element (sub-lists are added into the previous item)
								if(lastOpenListItemIndex != -1) {
									string text = html.Substring(lastOpenListItemIndex + 4, index - (lastOpenListItemIndex + 4));
									currentList.Elements[currentList.Elements.Count - 1].Text = text;
								}
								currentList.Elements[currentList.Elements.Count - 1].SubList = new HtmlList(HtmlListType.Ordered);
								currentList = currentList.Elements[currentList.Elements.Count - 1].SubList;
							}
							break;
						case 1: // <ul>
							// Unless at the beginning, start a new sub-list
							if(index != 0) {
								// Set text of current element (sub-lists are added into the previous item)
								if(lastOpenListItemIndex != -1) {
									string text = html.Substring(lastOpenListItemIndex + 4, index - (lastOpenListItemIndex + 4));
									currentList.Elements[currentList.Elements.Count - 1].Text = text;
								}
								currentList.Elements[currentList.Elements.Count - 1].SubList = new HtmlList(HtmlListType.Unordered);
								currentList = currentList.Elements[currentList.Elements.Count - 1].SubList;
							}
							break;
						case 2: // <li>
							lastOpenListItemIndex = index;
							currentList.Elements.Add(new HtmlListElement());
							break;
						case 3: // </li>
							// If lastOpenListItemIndex != -1 (i.e. there are no sub-lists) extract item text and set it to the last list element
							// Otherwise, navigate upwards to parent list (if any)
							if(lastOpenListItemIndex != -1) {
								string text = html.Substring(lastOpenListItemIndex + 4, index - (lastOpenListItemIndex + 4));
								currentList.Elements[currentList.Elements.Count - 1].Text = text;
							}
							else {
								currentList = FindAnchestor(root, currentList);
							}
							break;
						case 4: // </ul>
							// Close last open list (nothing to do)
							lastOpenListItemIndex = -1;
							break;
						case 5: // </ol>
							// Close last open list (nothing to do)
							lastOpenListItemIndex = -1;
							break;
						default:
							throw new NotSupportedException();
					}

					index++;
				}
			} while(index != -1);

			return root;
		}

		/// <summary>
		/// Finds the anchestor of a list in a tree.
		/// </summary>
		/// <param name="root">The root of the tree.</param>
		/// <param name="current">The current element.</param>
		/// <returns>The anchestor of <b>current</b>.</returns>
		private static HtmlList FindAnchestor(HtmlList root, HtmlList current) {
			foreach(HtmlListElement elem in root.Elements) {
				if(elem.SubList == current) return root;
				else if(elem.SubList != null) {
					HtmlList temp = FindAnchestor(elem.SubList, current);
					if(temp != null) return temp;
				}
			}
			//return root;
			return null;
		}

		/// <summary>
		/// Finds the index of the first string.
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <param name="startIndex">The start index.</param>
		/// <param name="stringFound">The index (in <b>strings</b>) of the string found.</param>
		/// <param name="strings">The strings to search for.</param>
		/// <returns>The index of the string found in <b>input</b>.</returns>
		private static int FirstIndexOfAny(string input, int startIndex, out int stringFound, params string[] strings) {
			if(startIndex > input.Length) {
				stringFound = -1;
				return -1;
			}

			int[] indices = new int[strings.Length];

			for(int i = 0; i < strings.Length; i++) {
				indices[i] = input.IndexOf(strings[i], startIndex);
			}

			bool nothingFound = true;
			int min = int.MaxValue;
			stringFound = -1;
			for(int i = 0; i < indices.Length; i++) {
				if(indices[i] != -1 && indices[i] < min) {
					nothingFound = false;
					min = indices[i];
					stringFound = i;
				}
			}

			if(nothingFound) return -1;
			else return min;
		}

		/// <summary>
		/// Finds the position of a matched tag pair.
		/// </summary>
		/// <param name="sb">The string builder buffer.</param>
		/// <param name="openTag">The open tag.</param>
		/// <param name="closeTag">The close tag.</param>
		/// <param name="startIndex">The start index.</param>
		/// <param name="openIndex">The open index.</param>
		/// <param name="closeIndex">The (matched/balanced) close index.</param>
		/// <returns><c>true</c> if a tag pair is found, <c>false</c> otherwise.</returns>
		private static bool FindTagsPair(StringBuilder sb, string openTag, string closeTag, int startIndex, out int openIndex, out int closeIndex) {
			// Find indexes for all open and close tags
			// Identify the smallest tag tree

			string text = sb.ToString();

			List<int> openIndexes = new List<int>(10);
			List<int> closeIndexes = new List<int>(10);

			if(startIndex >= sb.Length) {
				openIndex = -1;
				closeIndex = -1;
				return false;
			}

			int currentOpenIndex = startIndex - 1;
			int currentCloseIndex = startIndex - 1;

			do {
				currentOpenIndex = text.IndexOf(openTag, currentOpenIndex + 1);
				if(currentOpenIndex != -1) openIndexes.Add(currentOpenIndex);
			} while(currentOpenIndex != -1);

			// Optimization
			if(openIndexes.Count == 0) {
				openIndex = -1;
				closeIndex = -1;
				return false;
			}

			do {
				currentCloseIndex = text.IndexOf(closeTag, currentCloseIndex + 1);
				if(currentCloseIndex != -1) closeIndexes.Add(currentCloseIndex);
			} while(currentCloseIndex != -1);

			// Optimization
			if(closeIndexes.Count == 0) {
				openIndex = -1;
				closeIndex = -1;
				return false;
			}

			// Condition needed for further processing
			if(openIndexes.Count != closeIndexes.Count) {
				openIndex = -1;
				closeIndex = -1;
				return false;
			}

			// Build a sorted list of tags
			List<Tag> tags = new List<Tag>(openIndexes.Count * 2);
			foreach(int index in openIndexes) {
				tags.Add(new Tag() { Type = TagType.Open, Index = index });
			}
			foreach(int index in closeIndexes) {
				tags.Add(new Tag() { Type = TagType.Close, Index = index });
			}
			tags.Sort((x, y) => { return x.Index.CompareTo(y.Index); });

			// Find shortest closed tree
			int openCount = 0;
			int firstOpenIndex = -1;
			foreach(Tag tag in tags) {
				if(tag.Type == TagType.Open) {
					openCount++;
					if(firstOpenIndex == -1) firstOpenIndex = tag.Index;
				}
				else openCount--;

				if(openCount == 0) {
					openIndex = firstOpenIndex;
					closeIndex = tag.Index;
					return true;
				}
			}

			openIndex = -1;
			closeIndex = -1;
			return false;
		}

		/// <summary>
		/// Prepares a link URL.
		/// </summary>
		/// <param name="rawUrl">The raw URL, as generated by the formatter.</param>
		/// <returns>The prepared link URL, suitable for formatting.</returns>
		private static string PrepareLink(string rawUrl) {
			rawUrl = UrlDecode(rawUrl);
			string mainUrl = GetCurrentRequestMainUrl().ToLowerInvariant();
			if(rawUrl.ToLowerInvariant().StartsWith(mainUrl)) rawUrl = rawUrl.Substring(mainUrl.Length);

			if(rawUrl.ToLowerInvariant().EndsWith(".ashx")) return rawUrl.Substring(0, rawUrl.Length - 5);

			int extensionIndex = rawUrl.ToLowerInvariant().IndexOf(".ashx#");
			if(extensionIndex != -1) {
				return rawUrl.Remove(extensionIndex, 5);
			}

			if(rawUrl.StartsWith("GetFile.aspx")) {
				// Look for File and Provider parameter (v2 and v3)

				string provider, page, file;
				GetProviderAndFileAndPage(rawUrl, out provider, out page, out file);

				if(provider == null && page == null) return "{UP}" + file;
				else if(page != null) {
					return "{UP" + (provider != null ? ":" + provider : "") + "(" + page + ")}" + file;
				}
				else {
					return "{UP" + (provider != null ? ":" + provider : "") + "}" + file;
				}
			}

			return rawUrl;
		}

		/// <summary>
		/// Prepares an image URL.
		/// </summary>
		/// <param name="rawUrl">The raw URL, as generated by the formatter.</param>
		/// <returns>The prepared image URL, suitable for formatting.</returns>
		private static string PrepareImageUrl(string rawUrl) {
			rawUrl = UrlDecode(rawUrl);
			string mainUrl = GetCurrentRequestMainUrl().ToLowerInvariant();
			if(rawUrl.ToLowerInvariant().StartsWith(mainUrl)) rawUrl = rawUrl.Substring(mainUrl.Length);

			if(rawUrl.StartsWith("GetFile.aspx")) {
				// Look for File and Provider parameter (v2 and v3)

				string provider, page, file;
				GetProviderAndFileAndPage(rawUrl, out provider, out page, out file);

				if(provider == null) return "{UP" + (page != null ? "(" + page + ")" : "") + "}" + file;
				else return "{UP:" + provider + (page != null ? "(" + page + ")" : "") + "}" + file;
			}
			else return rawUrl;
		}

		/// <summary>
		/// Gets the current request main URL, such as http://www.server.com/Wiki/.
		/// </summary>
		/// <returns>The URL.</returns>
		private static string GetCurrentRequestMainUrl() {
			string url = HttpContext.Current.Request.Url.FixHost().GetLeftPart(UriPartial.Path);
			if(!url.EndsWith("/")) {
				int index = url.LastIndexOf("/");
				if(index != -1) url = url.Substring(0, index + 1);
			}
			return url;
		}

		/// <summary>
		/// Gets the provider and file of a link or URL.
		/// </summary>
		/// <param name="rawUrl">The raw URL, in the format ...?Provider=PROVIDER[&amp;IsPageAttachment=1&amp;Page=PAGE]&amp;File=FILE.</param>
		/// <param name="provider">The provider, or <c>null</c>.</param>
		/// <param name="page">The page (for attachments), or <c>null</c>.</param>
		/// <param name="file">The file.</param>
		private static void GetProviderAndFileAndPage(string rawUrl, out string provider, out string page, out string file) {
			rawUrl = rawUrl.Substring(rawUrl.IndexOf("?") + 1).Replace("&amp;", "&");

			string[] chunks = rawUrl.Split('&');

			provider = null;
			page = null;
			file = null;

			foreach(string chunk in chunks) {
				if(chunk.StartsWith("Provider=")) {
					provider = chunk.Substring(9);
				}
				if(chunk.StartsWith("File=")) {
					file = chunk.Substring(5);
				}
				if(chunk.StartsWith("Page=")) {
					page = chunk.Substring(5);
				}
			}
		}

		/// <summary>
		/// Decodes a URL-encoded string, even if it was encoded multiple times.
		/// </summary>
		/// <param name="input">The input encoded string.</param>
		/// <returns>The decoded string.</returns>
		/// <remarks>It seems that in some cases URL encoding occurs multiple times,
		/// one on the server and one on the client.</remarks>
		private static string UrlDecode(string input) {
			return Tools.UrlDecode(input);
			//return Tools.UrlDecode(Tools.UrlDecode(input));
		}

	}

	/// <summary>
	/// Represents an open or close tag.
	/// </summary>
	public class Tag {

		/// <summary>
		/// Gets or sets the tag type.
		/// </summary>
		public TagType Type { get; set; }

		/// <summary>
		/// Gets or sets the tag index.
		/// </summary>
		public int Index { get; set; }

	}

	/// <summary>
	/// Lists tag types.
	/// </summary>
	public enum TagType {
		/// <summary>
		/// An open tag.
		/// </summary>
		Open,
		/// <summary>
		/// A close tag.
		/// </summary>
		Close
	}

	/// <summary>
	/// Represents a HTML list.
	/// </summary>
	public class HtmlList {

		/// <summary>
		/// Initializes a new instance of the <see cref="T:HtmlList" /> class.
		/// </summary>
		/// <param name="type">The list type.</param>
		public HtmlList(HtmlListType type) {
			Type = type;
			Elements = new List<HtmlListElement>(10);
		}

		/// <summary>
		/// Gets or sets the list type.
		/// </summary>
		public HtmlListType Type { get; set; }

		/// <summary>
		/// Gets or sets the list elements.
		/// </summary>
		public List<HtmlListElement> Elements { get; set; }

	}

	/// <summary>
	/// Represents a HTML list element.
	/// </summary>
	public class HtmlListElement {

		/// <summary>
		/// Gets or sets the text.
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// Gets or sets the sub-list.
		/// </summary>
		public HtmlList SubList { get; set; }

	}

	/// <summary>
	/// Lists HTML list types.
	/// </summary>
	public enum HtmlListType {
		/// <summary>
		/// An ordered list.
		/// </summary>
		Ordered,
		/// <summary>
		/// An unordered list.
		/// </summary>
		Unordered
	}

}
