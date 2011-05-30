using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

using MySql.Data.MySqlClient;

class DekiMigration
{
	const string DEFAULT_CONNECTION_STRING = "Server=localhost; Database=monoproject; User=monoproject; Password=monoproject; CharSet=utf8";
	const string DEFAULT_OUTPUT_FILE = "mono-project-stw.sql";

	static Dictionary <uint, string> users = new Dictionary <uint, string> ();
	
	public static string ConnectionString { get; private set; }
		
	static void Main (string[] args)
	{
		int argslen = args.Length;
		ConnectionString = argslen > 0 ? args [0] : DEFAULT_CONNECTION_STRING;
		string output_file = argslen > 1 ? args [1] : DEFAULT_OUTPUT_FILE;
		
		var conn = new MySqlConnection (ConnectionString);
		conn.Open ();

		var cmd = conn.GetCommand ("SET character_set_client=utf8; SET character_set_database=utf8; SET character_set_server=utf8; SET character_set_results=utf8");
		cmd.ExecuteNonQuery ();
		
		using (var sw = new StreamWriter (output_file, false, Encoding.UTF8)) {
			Categories (conn, sw);
			Users (conn, sw);
			Pages (conn, sw);
		}
		conn.Close ();
	}
	
	public static void StepStart (string format, params object[] parms)
	{
		Console.Error.Write ("**: ");
		Console.Error.WriteLine (format, parms);
	}

	public static void StepInfo (string format, params object[] parms)
	{
		Console.Error.Write ("  ...");
		Console.Error.WriteLine (format, parms);
	}

	public static void StepProgress (string format, params object[] parms)
	{
		StepProgress (format, false, parms);
	}
	
	public static void StepProgress (string format, bool first, params object[] parms)
	{
		if (first)
			Console.Error.Write ("  ... ");
		Console.Error.Write (format, parms);
	}
	
	static void Categories (MySqlConnection conn, StreamWriter sw)
	{
		StepStart ("Categories");
		sw.WriteLine ("/* Categories: start */");
		var cmd = conn.GetCommand ("SELECT DISTINCT cl_to FROM categorylinks ORDER BY cl_to");
		using (var reader = cmd.ExecuteReader ()) {
			int count = 0;
			while (reader.Read ()) {
				count++;
				sw.WriteLine ("INSERT INTO Category (Name,Namespace) VALUES ('{0}', '');", (reader ["cl_to"] as string).SqlEncode ());
			}
			StepInfo ("{0} categories imported.", count);
		}
		sw.WriteLine ("/* Categories: end */");
		sw.WriteLine ();
	}

	static void Users (MySqlConnection conn, StreamWriter sw)
	{
		StepStart ("Users");
		sw.WriteLine ("/* Users: start */");
		var cmd = conn.GetCommand ("SELECT * FROM user");
		WikiUser user;
		using (var reader = cmd.ExecuteReader ()) {
			int count = 0;
			int admins = 0;
			while (reader.Read ()) {
				user = new WikiUser (reader);
				user.ResolveGroups ();
				users.Add (user.Id, user.Name);
				count++;
				
				sw.WriteLine ("INSERT INTO User (Username,PasswordHash,DisplayName,Email,Active,DateTime) " +
					      "VALUES ('{0}', '{1}', '{2}', '{3}', TRUE, '{4}');",
					      user.Name.SqlEncode (), user.PasswordHash, user.RealName.SqlEncode (), user.Email, user.Touched.ToMySqlDateTime ());
				if (!user.IsAdmin)
					continue;
				sw.WriteLine ("INSERT INTO UserGroupMembership (User,UserGroup) VALUES ('{0}','Administrators');", user.Name.SqlEncode ());
				admins++;
			}
			StepInfo ("{0} users migrated, {1} admins", count, admins);
		}
		
		sw.WriteLine ("/* Users: end */");
		sw.WriteLine ();
	}
	
	static void Pages (MySqlConnection conn, StreamWriter sw)
	{
		var pagesCache = new Dictionary <string, DateTime> (StringComparer.OrdinalIgnoreCase);
		var ignoredPages = new List <string> ();
		var failedPages = new List <string> ();
		StepStart ("Pages");
		sw.WriteLine ("/* Pages: start */");

		var cmd = conn.GetCommand ("SELECT count(*) FROM cur");
		var count = cmd.ExecuteScalar <int> (0);
		StepInfo ("{0} pages to import.", count);
		StepInfo ("Legend: M - MediaWiki internal, I - ignored, F - failed, T - template/snippet, * - successful");

		// Load template names
		var templates = new Dictionary <string, bool> (StringComparer.OrdinalIgnoreCase);
		cmd = conn.GetCommand ("SELECT cur_title FROM cur WHERE cur_namespace=10");

		using (var reader = cmd.ExecuteReader ()) {
			string title;
			while (reader.Read ()) {
				title = reader ["cur_title"] as string;
				if (String.IsNullOrEmpty (title) || templates.ContainsKey (title))
					continue;

				templates [title] = true;
				templates ["Template:" + title] = true;
			}
		}

		var magicWords = new SortedDictionary <string, bool> (StringComparer.Ordinal);
		WikiPage page;
		cmd = conn.GetCommand ("SELECT * FROM cur");
		using (var reader = cmd.ExecuteReader ()) {
			int imported = 0;
			int processed = 0;
			int olderVersionIgnored = 0;
			bool isUpdate;
			DateTime cachedTimeStamp;
			while (reader.Read ()) {
				processed++;
				isUpdate = false;
				try {
					page = new WikiPage (reader, templates);
					if (page.Ignore) {
						StepProgress ("M", processed == 1);
						ignoredPages.Add (String.Format ("Ignored settings page '{0}'", page.Title));
						continue;
					}
					
					// TODO: process images here (page.DekiNameSpace ==
					// DekiNamespace.Image)
					
					page.ResolveCategories ();
					if (pagesCache.TryGetValue (page.Title.SqlEncodeForName () + " (" + page.DekiNameSpace + ")", out cachedTimeStamp)) {
						olderVersionIgnored++;
						StepProgress ("I", processed == 1);
						if (page.LastModified > cachedTimeStamp) {
							ignoredPages.Add (String.Format ("Ignored older version of page '{0}' (Deki ID: {1}, Namespace: {2})", page.Title, page.Id, page.DekiNameSpace));
							isUpdate = true;
						} else {
							olderVersionIgnored++;
							continue;
						}
					} else {
						pagesCache.Add (page.Title.SqlEncodeForName () + " (" + page.DekiNameSpace + ")", page.LastModified);
						imported++;
						StepProgress (page.DekiNameSpace == DekiNamespace.Template ? "T" : "*", processed == 1);
					}

					if (page.MagicWords != null) {
						foreach (string mw in page.MagicWords) {
							if (magicWords.ContainsKey (mw))
								continue;
							magicWords [mw] = true;
						}
					}
				} catch (Exception ex) {
					StepProgress ("F", processed == 1);
					failedPages.Add (String.Format ("Page {0} not processed. Exception '{1}' occurred: {2}", processed, ex.GetType (), ex));
					continue;
				}

				if (page.DekiNameSpace == DekiNamespace.Template) {
					sw.WriteLine ("INSERT INTO Snippet (Name,Content) VALUES ('{0}', '{1}');", page.Title, page.Text.SqlEncode ());
					continue;
				}
				
				if (isUpdate) {
					sw.WriteLine ("UPDATE Page SET Name='{0}', Namespace='', CreationDateTime='{1}' WHERE Name='{0}';",
						      page.Title.SqlEncodeForName (), page.Created.ToMySqlDateTime ());
					sw.WriteLine ("UPDATE PageContent SET Page='{0}', Namespace='', Revision={1}, Title='{2}', User='{3}', " +
						      "LastModified='{4}', Comment='{5}', Content='{6}', Description='{7}' " +
						      "WHERE Page='{0}';",
						      page.Title.SqlEncodeForName (), -1, page.Title.SqlAndWikiEncode (), LookupUser (page.User), page.LastModified.ToMySqlDateTime (),
						      page.Comment.SqlEncode (), page.Text.SqlEncode (), String.Empty);
				} else {
					sw.WriteLine ("INSERT INTO Page (Name,Namespace,CreationDateTime) VALUES ('{0}','','{1}');",
						      page.Title.SqlEncodeForName (), page.Created.ToMySqlDateTime ());
					sw.WriteLine ("INSERT INTO PageContent (Page,Namespace,Revision,Title,User,LastModified,Comment,Content,Description) " +
						      "VALUES ('{0}', '', {1}, '{2}', '{3}', '{4}', '{5}', '{6}', '{7}');",
						      page.Title.SqlEncodeForName (), -1, page.Title.SqlAndWikiEncode (), LookupUser (page.User), page.LastModified.ToMySqlDateTime (),
						      page.Comment.SqlEncode (), page.Text.SqlEncode (), String.Empty);
					sw.WriteLine ("INSERT INTO PageKeyword (Page,Namespace,Revision,Keyword) VALUES ('{0}','',-1,'');",
						      page.Title.SqlEncodeForName ());
				}
			}
			Console.Error.WriteLine ();
			
			StepInfo ("{0} pages imported, {1} ignored, {2} old versions ignored, {3} failed.", imported, count - imported, olderVersionIgnored, failedPages.Count);
			if (failedPages.Count > 0) {
				StepInfo ("Failed pages:");
				foreach (string s in failedPages)
					StepInfo ("  {0}", s);
			}
			
			if (ignoredPages.Count > 0) {
				StepInfo ("Ignored pages:");
				foreach (string s in ignoredPages)
					StepInfo ("  {0}", s);
			}

			if (magicWords.Count > 0) {
				StepInfo ("Magic words used:");
				bool first = true;
				foreach (var kvp in magicWords) {
					StepProgress (kvp.Key.Replace ("{", "{{").Replace ("}", "}}") + " ", first);
					if (first)
						first = false;
				}
				Console.Error.WriteLine ();
			}
		}
		
		sw.WriteLine ("/* Pages: end */");
		sw.WriteLine ();
	}

	static string LookupUser (uint id)
	{
		string ret;

		if (!users.TryGetValue (id, out ret) || String.IsNullOrEmpty (ret))
			return String.Empty;

		return ret;
	}
}
