//
//  Authors:
//    Marek Habersack <grendel@twistedcode.net>
//
//  (C) 2011 Marek Habersack
//
//  Redistribution and use in source and binary forms, with or without modification, are permitted
//  provided that the following conditions are met:
//
//     * Redistributions of source code must retain the above copyright notice, this list of
//       conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of
//       conditions and the following disclaimer in the documentation and/or other materials
//       provided with the distribution.
//     * Neither the name of Marek Habersack nor the names of its contributors may be used to
//       endorse or promote products derived from this software without specific prior written
//       permission.
//
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
//  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
//  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
//  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
//  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//

using System;
using System.IO;
using System.Text;

namespace MediaWikiCompat
{
	public class Table 
	{
		static string[] headerCells = {"!!"};
		static string[] tableCells = {"||"};
		
		// Ugly, naive code, but the simplest to implement
		// - No support for nested tables!
		public static string Parse (string input)
		{
			if (String.IsNullOrEmpty (input))
				return input;
			
			if (input.IndexOf ("<nowiki>|</nowiki>", StringComparison.Ordinal) != -1)
				input = input.Replace ("<nowiki>|</nowiki>", "&#x007c;");
			input = input.Replace ("\r", String.Empty);
			
			var sb = new StringBuilder ();
			string[] lines = input.Split ('\n');
			string trimmed;
			bool inTable = false, beforeFirstRow = true, inRow = false, inCell = false, inHeader = false;
			
			foreach (string line in lines) {
				trimmed = line.Trim ();
				if (trimmed.StartsWith ("{|", StringComparison.Ordinal)) {
					inTable = true;
					beforeFirstRow = true;
					inRow = false;
					inCell = false;
					inHeader = false;
					sb.Append ("<table");
					if (trimmed.Length > 2) {
						sb.Append (' ');
						sb.Append (trimmed.Substring (2));
					}
					sb.Append (">");
					continue;
				}

				if (!inTable) {
					sb.AppendLine (line);
					continue;
				}

				if (trimmed.StartsWith ("|}", StringComparison.Ordinal)) {
					if (inCell)
						sb.Append ("</td>");
					if (inHeader)
						sb.Append ("</th>");
					if (inRow)
						sb.Append ("</tr>");
					sb.Append ("</table>");
					inTable = false;
					inHeader = false;
					inRow = false;
					inCell = false;
				} else if (trimmed.StartsWith ("|-", StringComparison.Ordinal)) {
					inRow = true;
					if (inCell) {
						sb.Append ("</td>");
						inCell = false;
					}
					if (inHeader) {
						sb.Append ("</th>");
						inHeader = false;
					}
					if (beforeFirstRow)
						beforeFirstRow = false;
					else 
						sb.Append ("</tr>");
					sb.Append ("<tr");
					if (trimmed.Length > 2) {
						sb.Append (' ');
						sb.Append (trimmed.Substring (2));
					}
					sb.Append (">");
				} else if (beforeFirstRow && trimmed.StartsWith ("|+")) {
					AppendTagWithAttributes (sb, "caption", trimmed.Substring (2));
					sb.Append ("</caption>");
				} else if (trimmed.StartsWith ("!", StringComparison.Ordinal)) {
					if (inHeader)
						sb.Append ("</th>");
					inHeader = true;
					string[] headers = trimmed.Substring (1).Split (headerCells, StringSplitOptions.None);
					int nheaders = headers.Length;
					for (int i = 0; i < nheaders; i++) {
						AppendTagWithAttributes (sb, "th", headers [i]);
						if (i < nheaders - 1)
							sb.Append ("</th>");
					}
				} else if (trimmed.StartsWith ("|", StringComparison.Ordinal)) {
					if (inCell)
						sb.Append ("</td>");
					inCell = true;
					string[] cells = trimmed.Substring (1).Split (tableCells, StringSplitOptions.None);
					int ncells = cells.Length;
					for (int i = 0; i < ncells; i++) {
						AppendTagWithAttributes (sb, "td", cells [i]);
						if (i < ncells - 1)
							sb.Append ("</td>");
					}
				} else {
					sb.AppendLine ();
					sb.Append (line);
				}
			}

			return sb.ToString ();
		}

		static void AppendTagWithAttributes (StringBuilder sb, string tagName, string line)
		{
			sb.Append ("<" + tagName);
			int pipeIndex = line.IndexOf ('|');
			int lineLen = line.Length;
			
			if (pipeIndex == -1 || (pipeIndex < lineLen - 1 && line [pipeIndex + 1] == '|')) {
				sb.Append ('>');
				sb.Append (line);
				return;
			}
			sb.Append (' ');
			sb.Append (line.Substring (0, pipeIndex));
			sb.Append ('>');
			sb.Append (line.Substring (pipeIndex + 1));
		}
#if TEST
		static void Main (string[] args)
		{
			string parsed = Parse (File.ReadAllText (args [0]));
			Console.WriteLine (parsed);
		}
#endif
	}
}
