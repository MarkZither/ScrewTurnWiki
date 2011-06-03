using System;
using System.Text;
using System.Text.RegularExpressions;

class WikiImage
{
	static readonly Regex BorderRegex = new Regex (@"border$", RegexOptions.Compiled);
	static readonly Regex TypeRegex = new Regex (@"thumb(nail=.+|=.+|nail)?$|frame(d|less)?$", RegexOptions.Compiled);
	static readonly Regex LocationRegex = new Regex (@"right|left|center|none", RegexOptions.Compiled);
	static readonly Regex AlignmentRegex = new Regex (@"baseline|middle|sub|super|text-top|text-bottom|top|bottom", RegexOptions.Compiled);
	static readonly Regex LinkRegex = new Regex (@"(\[\[.+?\]\])|(\[.+?\])", RegexOptions.Compiled);
	static readonly Regex SizeRegex = new Regex (@"([0-9.]*x?[0-9.]+px)|upright(=[0-9.]+)", RegexOptions.Compiled);
	
	public string Name { get; set; }
	public string Type { get; set; }
	public bool Border { get; set; }
	public string Location { get; set; }
	public string Alignment { get; set; }
	public string Caption { get; set; }
	public string Link { get; set; }
	public string LinkTail { get; set; }
	public string Size { get; set; }
	
	public bool Valid {
		get {
			return !String.IsNullOrEmpty (Name);
		}
	}
	
	public WikiImage (string dekiImage)
	{
		if (String.IsNullOrEmpty (dekiImage))
			return;
		
		Parse (dekiImage);
	}

	void Parse (string dekiImage)
	{
		string[] parts = dekiImage.Split ('|');
		string trimmed;
		string value;
		
		Name = parts [0];
		if (parts.Length == 1)
			return;

		Match match;
		string captionTail = String.Empty;
		for (int i = 1; i < parts.Length; i++) {
			trimmed = parts [i].Trim ();
			if (String.IsNullOrEmpty (trimmed))
				continue;

			match = TypeRegex.Match (trimmed);
			if (match.Success) {
				Type = trimmed;
				continue;
			}

			match = BorderRegex.Match (trimmed);
			if (match.Success) {
				Border = true;
				continue;
			}

			match = LocationRegex.Match (trimmed);
			if (match.Success) {
				Location = trimmed;
				continue;
			}

			match = AlignmentRegex.Match (trimmed);
			if (match.Success) {
				Alignment = trimmed;
				continue;
			}

			match = LinkRegex.Match (trimmed);
			if (match.Success) {
				value = match.Value;
				Link = value;
				if (trimmed.Length > value.Length)
					captionTail += trimmed.Substring (match.Index + match.Length);
				continue;
			}

			match = SizeRegex.Match (trimmed);
			if (match.Success) {
				Size = trimmed;
				continue;
			}
			
			if (String.IsNullOrEmpty (Caption))
				Caption = trimmed;
			else
				Caption += trimmed;
		}
		
		LinkTail = captionTail;
	}

	public override string ToString ()
	{
		if (!Valid)
			return String.Empty;

		var sb = new StringBuilder ("[");
		switch (Location) {
			case "left":
				sb.Append ("imageleft");
				break;

			case "right":
				sb.Append ("imageright");
				break;

			default:
				sb.Append ("image");
				break;
		}

		string link = Link;
		string linkCaption = null;
		if (!String.IsNullOrEmpty (link)) {
			link = link.TrimStart ('[').TrimEnd (']');
			string[] parts = link.Split (' ');
			link = parts [0].Trim ();
			if (parts.Length > 1)
				linkCaption = String.Join (" ", parts, 1, parts.Length - 1);
		}
		
		sb.Append ('|');
		if (!String.IsNullOrEmpty (Caption))
			sb.Append (Caption);
		else if (!String.IsNullOrEmpty (linkCaption)) {
			sb.Append (linkCaption);
			if (!String.IsNullOrEmpty (LinkTail))
				sb.Append (LinkTail);
		}
		
		sb.Append ("|{UP}");
		sb.Append (Name);
		if (!String.IsNullOrEmpty (link)) {
			sb.Append ('|');
			sb.Append (link);
		}
		
		sb.Append (']');

		return sb.ToString ();
	}
}
