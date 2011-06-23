using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using MySql.Data.MySqlClient;
	
class WikiPage
{
	static readonly Regex CategoryLinkRegex = new Regex (@"(\[\[Category:.+?\]\])|(\[Category:.+?\])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
	static readonly Regex LinkRegex = new Regex (@"(\[\[.+?\]\])|(\[.+?\])", RegexOptions.Compiled);
	static readonly Regex UrlRegex = new Regex (@"\w.+://", RegexOptions.Compiled);
	static readonly Regex DashesRegex = new Regex (@"[-]{2}", RegexOptions.Compiled);
	static readonly Regex HRegex = new Regex (@"^={1,4}.+?={1,4}\n?", RegexOptions.Compiled | RegexOptions.Multiline);
	static readonly Regex MagicWordRegex = new Regex (@"\{\{.+?\}\}", RegexOptions.Compiled | RegexOptions.Singleline);
	static readonly Regex ImageRegex = new Regex (@"\[\[Image:.+?\]\]", RegexOptions.Compiled);

	static readonly SortedList <string, string> codeHighlightBlocks = new SortedList <string, string> () {
		{"<csharp>", "\n@@ csharp\n"},
		{"</csharp>", "\n@@\n"},
		{"<bash>", "\n@@ bash\n"},
		{"</bash>", "\n@@\n"},
		{"<xml>", "\n@@ xml\n"},
		{"</xml>", "\n@@\n"}
	};

	static readonly SortedList <string, string> otherSimpleReplacements = new SortedList <string, string> () {
		{"[Mono:About", "[About_Mono"},
		{"[Mono:Runtime:Documentation:ThreadSafety", "[Project_Mono_Runtime_Documentation_ThreadSafety"},
		{"[Mono:Runtime:Documentation:MemoryManagement", "[Project_Mono_Runtime_Documentation_MemoryMangement"},
		{"[Mono:Runtime:Documentation:GenericSharing", "[Project_Mono_Runtime_Documentation_GenericSharing"},
		{"[Mono:Runtime:Documentation:Generics", "[Project_Mono_Runtime_Documentation_Generics"},
		{"[Mono:Runtime:Documentation:RegisterAllocation", "[Project_Mono_Runtime_Documentation_RegisterAllocation"},
		{"[Mono:Runtime:Documentation:SoftDebugger", "[Project_Mono_Runtime_Documentation_SoftDebugger"},
		{"[Mono:Runtime:Documentation:mono-llvm.diff", "[Project_Mono_Runtime_Documentation_mono-llvm.diff"},
		{"[Mono:Runtime:Documentation:LLVM", "[Project_Mono_Runtime_Documentation_LLVM"},
		{"[Mono:Runtime:Documentation:XDEBUG", "[Project_Mono_Runtime_Documentation_XDEBUG"},
		{"[Mono:Runtime:Documentation:MiniPorting", "[Project_Mono_Runtime_Documentation_MiniPorting"},
		{"[Mono:Runtime:Documentation:AOT", "[Project_Mono_Runtime_Documentation_AOT"},
		{"[Mono:Runtime:Documentation:Trampolines", "[Project_Mono_Runtime_Documentation_Trampolines"},
		{"[Mono:Runtime:Documentation", "[Project_Mono_Runtime_Documentation"},
		{"[Mono:", "[Project_Mono_"}, // For pages in namespace Project (4)
		{"[Talk:", "[Talk_"},
		{"[User:", "[User_"},
		{"[Help:", "[Help_"},
		{"[Mono_Runtime", "[Runtime"},
		{"#REDIRECT", ">>> "},
		{"#redirect", ">>> "},
		{"{{:Supported Platforms}}", "{s:Supported_Platforms}"}
	};
	
	public uint Id { get; set; }
	public byte NameSpace { get; set; }
	public DekiNamespace DekiNameSpace { get; set; }
	public string Title { get; set; }
	public string Text { get; internal set; }
	public string Comment { get; set; }
	public uint User { get; set; }
	public DateTime LastModified { get; set; }
	public DateTime Created { get; set; }
	public List <string> Categories { get; set; }
	public List <string> MagicWords { get; set; }
	public bool Ignore {
		get {
			return DekiNameSpace == DekiNamespace.Mediawiki ||
				DekiNameSpace == DekiNamespace.Mediawiki_talk ||
				DekiNameSpace == DekiNamespace.Category ||
				DekiNameSpace == DekiNamespace.Category_talk;
		}
	}
	public bool IsRedirect { get; set; }
	
	public WikiPage (MySqlDataReader reader, Dictionary <string, bool> templates)
	{
		Id = (uint)reader ["cur_id"];
		NameSpace = (byte)reader ["cur_namespace"];
		if (NameSpace >= (sbyte)DekiNamespace.FIRST && NameSpace <= (sbyte)DekiNamespace.LAST)
			DekiNameSpace = ((DekiNamespace) NameSpace);
		else
			DekiNameSpace = DekiNamespace.Invalid;

		string titlePrefix = String.Empty;
		switch (DekiNameSpace) {
			case DekiNamespace.Talk:
			case DekiNamespace.User:
			case DekiNamespace.User_talk:
			case DekiNamespace.Help:
				titlePrefix = DekiNameSpace.ToString () + "_";
				break;
				
			case DekiNamespace.Project:
				titlePrefix = "Project_Mono_";
				break;
				
		}
		Title = titlePrefix + reader ["cur_title"] as string;
		Comment = (reader ["cur_comment"] as byte[]).TinyBlobToString ();
		User = (uint)reader ["cur_user"];
		LastModified = (reader ["cur_touched"] as string).ParseDekiTime ();
		Created = (reader ["cur_timestamp"] as string).ParseDekiTime ();
		IsRedirect = ((byte)reader ["cur_is_redirect"]) == 1;
		Text = FixupContent (reader ["cur_text"] as string, templates);
	}

	public void ResolveCategories ()
	{
		using (var mconn = new MySqlConnection (DekiMigration.ConnectionString)) {
			mconn.Open ();
			var cmd = mconn.GetCommand ("SELECT DISTINCT cl_to FROM categorylinks WHERE cl_from = ?Id", new Tuple <string, object> ("Id", Id));
			using (var reader = cmd.ExecuteReader ()) {
				if (Categories == null)
					Categories = new List <string> ();

				while (reader.Read ())
					Categories.Add (reader ["cl_to"] as string);
				Categories.Sort ();
			}
		}
	}

	static char[] linkReplaceChars = {':', '&', ' ', '.'};
	string FixupString (string s, out string original)
	{
		original = null;
		if (String.IsNullOrEmpty (s))
			return s;
		if (s.IndexOfAny (linkReplaceChars) == -1)
			return s;
		original = s;
		foreach (char ch in linkReplaceChars)
			s = s.Replace (ch, '_');

		return s;
	}
	
	string FixupContent (string content, Dictionary <string, bool> templates)
	{
		if (String.IsNullOrEmpty (content))
			return String.Empty;

		string ret = CategoryLinkRegex.Replace (content, String.Empty, -1);
		ret = DashesRegex.Replace (ret, "<nowiki>--</nowiki>", -1);
		var sb = new StringBuilder (ret);

		if (DekiNameSpace != DekiNamespace.Template) {
			if (ret.IndexOf ("__NOTOC__", StringComparison.OrdinalIgnoreCase) == -1) {
				if (HRegex.IsMatch (ret)) {
					if (ret.IndexOf ("__TOC__") != -1)
						sb.Replace ("__TOC__", "{TOC}");
					else
						sb.Insert (0, "{TOC}\n");
				}
			} else {
				sb.Replace ("__NOTOC__", String.Empty);
			}
		}
		
		foreach (var kvp in otherSimpleReplacements)
			sb.Replace (kvp.Key, kvp.Value);

		Match match = ImageRegex.Match (sb.ToString ());
		string value, tmp;
		string[] fields;
		int newStart = 0;
		WikiImage image;
		while (match.Success) {
			value = match.Value.TrimStart ('[').TrimEnd (']').Replace ("Image:", String.Empty);
			image = new WikiImage (value);
			if (!image.Valid)
				continue;
			tmp = image.ToString ();
			sb.Remove (match.Index, match.Length);
			sb.Insert (match.Index, tmp);
			newStart = match.Index + tmp.Length;
			match = ImageRegex.Match (sb.ToString (), newStart);
		}
		
		match = LinkRegex.Match (sb.ToString ());
		newStart = 0;
		while (match.Success) {
			value = match.Value;
			if (value.Equals ("[]", StringComparison.Ordinal) || value.Equals ("[[]]", StringComparison.Ordinal) || value.Equals ("[[]", StringComparison.Ordinal)) {
				match = LinkRegex.Match (sb.ToString (), match.Index + match.Length);
				continue;
			}
			
			if (value.StartsWith ("[[", StringComparison.Ordinal))
				value = value.Substring (2, match.Length - 4).Trim ();
			else
				value = value.Substring (1, match.Length - 2).Trim ();
			
			if (value.IndexOf ('|') != -1) {
				fields = value.Split ('|');
				fields [0] = FixupString (fields [0].Trim (), out tmp);
				sb.Remove (match.Index, match.Length);
				if (IsRedirect)
					tmp = String.Format ("{0}", fields [0]);
				else
					tmp = String.Format ("[{0}]", String.Join ("|", fields));
				newStart = match.Index + tmp.Length;
				sb.Insert (match.Index, tmp);
			} else if (value.IndexOf (' ') != -1) {
				// Might be a special case of [url://link text] or [SomeOther text]
				// If the part before the first ' ' is an URL, we do nothing to it and just put the
				// entire text that follows it as the link name.
				// If the first part is not an URL, we replace invalid characters in it and make the
				// original text the new link's name
				sb.Remove (match.Index, match.Length);
				fields = value.Split (' ');
				if (!UrlRegex.IsMatch (fields [0])) {
					value = FixupString (value, out tmp);
					if (!String.IsNullOrEmpty (tmp))
						tmp = String.Format (IsRedirect ? "{0}" : "[{0}|{1}]", value, tmp);
					else
						tmp = IsRedirect ? value : "[" + value + "]";
				} else
					  tmp = String.Format (IsRedirect ? "{0}" : "[{0}|{1}]", fields [0], String.Join (" ", fields, 1, fields.Length - 1));
				newStart = match.Index + tmp.Length;
				sb.Insert (match.Index, tmp);
			} else {
				//if (value.IndexOfAny (linkReplaceChars) != -1) {
					tmp = String.Format (IsRedirect ? "{0}" : "[{0}|{1}]", FixupString (value.Trim (), out tmp), value);
					sb.Remove (match.Index, match.Length);
					sb.Insert (match.Index, tmp);
					newStart = match.Index + tmp.Length;
				//} else 
				//	newStart = match.Index + match.Length;
			}
			
			match = LinkRegex.Match (sb.ToString (), newStart);
		}

		match = HRegex.Match (sb.ToString ());
		newStart = 0;
		int equalcount;
		while (match.Success) {
			value = match.Value.Trim ();
			equalcount = 0;
			for (int i = 0; i < value.Length; i++) {
				if (value [i] != '=')
					break;
				equalcount++;
			}
			
			if (equalcount > 4)
				tmp = "\n";
			else
				tmp = "\n=";

			tmp += value;
			equalcount = 0;
			for (int i = value.Length - 1; i >= 0; i--) {
				if (value [i] != '=')
					break;
				equalcount++;
			}
			if (equalcount <= 4)
				tmp += "=";
			
			sb.Remove (match.Index, match.Length);
			sb.Insert (match.Index, tmp);
			newStart = match.Index + tmp.Length;

			match = HRegex.Match (sb.ToString (), newStart);
		}
		
		foreach (var kvp in codeHighlightBlocks)
			sb.Replace (kvp.Key, kvp.Value);

		MagicWords = new List <string> ();
		match = MagicWordRegex.Match (sb.ToString ());
		while (match.Success) {
			value = match.Value.TrimStart ('{').TrimEnd ('}').Replace (" ", "_");
			if (!templates.ContainsKey (value)) {
				MagicWords.Add (value);
				newStart = match.Index + match.Length;
			} else {
				if (value.StartsWith ("Template:", StringComparison.OrdinalIgnoreCase))
					value = value.Substring (9);
				else if (value.StartsWith (":"))
					value = value.Substring (1);
				
				tmp = "{s:" + value + "}";
				sb.Remove (match.Index, match.Length);
				sb.Insert (match.Index, tmp);
				newStart = match.Index + tmp.Length;
			}
			
			match = MagicWordRegex.Match (sb.ToString (), newStart);
		}
		
		return sb.ToString ();
	}	
}
