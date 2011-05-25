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
	
	public uint Id { get; set; }
	public string NameSpace { get; set; }
	public string Title { get; set; }
	public string Text { get; internal set; }
	public string Comment { get; set; }
	public uint User { get; set; }
	public DateTime LastModified { get; set; }
	public DateTime Created { get; set; }
	public List <string> Categories { get; set; }
	public bool Ignore {
		get { return ShouldIgnore (); }
	}
	
	public WikiPage (MySqlDataReader reader)
	{
		Id = (uint)reader ["cur_id"];
		NameSpace = reader ["cur_namespace"] as string;
		Title = reader ["cur_title"] as string;
		Text = FixupContent (reader ["cur_text"] as string);
		Comment = (reader ["cur_comment"] as byte[]).TinyBlobToString ();
		User = (uint)reader ["cur_user"];
		LastModified = (reader ["cur_touched"] as string).ParseDekiTime ();
		Created = (reader ["cur_timestamp"] as string).ParseDekiTime ();
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

	string FixupContent (string content)
	{
		if (String.IsNullOrEmpty (content))
			return String.Empty;
		
		string ret = CategoryLinkRegex.Replace (content, String.Empty, -1);
		ret = DashesRegex.Replace (ret, "<nowiki>--</nowiki>", -1);
		var sb = new StringBuilder (ret);

		if (ret.IndexOf ("__NOTOC__", StringComparison.OrdinalIgnoreCase) == -1)
			sb.Insert (0, "{TOC}\n");
		
		Match match = LinkRegex.Match (sb.ToString ());
		string value, tmp;
		string[] fields;
		int newStart = 0;
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
				fields [0] = fields [0].Replace (":", "-").Replace ("&", "-");
				sb.Remove (match.Index, match.Length);
				tmp = String.Format ("[{0}]", String.Join ("|", fields));
				newStart = match.Index + tmp.Length;
				sb.Insert (match.Index, tmp);
			} else if (value.IndexOf (' ') != -1) {
				// Might be a special case of [url://link text] or [SomeOther text]
				// We replace the first ' ' with '|' and remove invalid characters
				// before it unless it's an url
				sb.Remove (match.Index, match.Length);
				fields = value.Split (' ');
				if (!UrlRegex.IsMatch (fields [0]))
					fields [0] = fields [0].Replace (":", "-").Replace ("&", "-");
				tmp = String.Format ("[{0}|{1}]", fields [0], String.Join (" ", fields, 1, fields.Length - 1));
				newStart = match.Index + tmp.Length;
				sb.Insert (match.Index, tmp);
			} else
				  newStart = match.Index + match.Length;
			match = LinkRegex.Match (sb.ToString (), newStart);
		}
		
		return sb.ToString ();
	}
	
	bool ShouldIgnore ()
	{
		string title = Title;
		if (String.IsNullOrEmpty (title))
			return true;

		return (
			User == 0 || // SysOp
			title.StartsWith ("Sitesettings-", StringComparison.OrdinalIgnoreCase) ||
			title.StartsWith ("Accesskey-", StringComparison.OrdinalIgnoreCase)
		);
	}
}
