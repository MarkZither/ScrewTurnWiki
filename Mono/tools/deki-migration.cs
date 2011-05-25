using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

using MySql.Data.MySqlClient;

class DekiMigration
{
	const string DEFAULT_CONNECTION_STRING = "Server=localhost; Database=monoproject; User=monoproject; Password=monoproject";
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

	public static void StepProgress (string format, params object[] parms)
	{
		Console.Error.Write ("  ...");
		Console.Error.WriteLine (format, parms);
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
			StepProgress ("{0} categories imported.", count);
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
			StepProgress ("{0} users migrated, {1} admins", count, admins);
		}
		
		sw.WriteLine ("/* Users: end */");
		sw.WriteLine ();
	}
	
	static void Pages (MySqlConnection conn, StreamWriter sw)
	{
		var pagesCache = new Dictionary <string, DateTime> (StringComparer.OrdinalIgnoreCase);
		var ignoredPages = new List <string> ();
		StepStart ("Pages");
		sw.WriteLine ("/* Pages: start */");

		var cmd = conn.GetCommand ("SELECT count(*) FROM cur");
		var count = cmd.ExecuteScalar <int> (0);
		StepProgress ("{0} pages to import.", count);

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
					page = new WikiPage (reader);
					if (page.Ignore) {
						ignoredPages.Add (String.Format ("Ignored settings page '{0}'", page.Title));
						continue;
					}
					
					page.ResolveCategories ();
					if (pagesCache.TryGetValue (page.Title.SqlEncodeForName () + " (" + page.NameSpace + ")", out cachedTimeStamp)) {
						olderVersionIgnored++;
						if (page.LastModified > cachedTimeStamp) {
							ignoredPages.Add (String.Format ("Ignored older version of page '{0}' (Deki ID: {1})", page.Title, page.Id));
							isUpdate = true;
						} else {
							olderVersionIgnored++;
							continue;
						}
					} else {
						pagesCache.Add (page.Title.SqlEncodeForName () + " (" + page.NameSpace + ")", page.LastModified);
						imported++;
					}
				} catch (Exception ex) {
					StepProgress ("Page {0} not processed. Exception '{1}' occurred: {2}", processed, ex.GetType (), ex);
					continue;
				}

				if (isUpdate) {
					sw.WriteLine ("UPDATE Page SET Name='{0}', Namespace='{1}', CreationDateTime='{2}' WHERE Name='{0}';",
						      page.Title.SqlEncodeForName (), page.NameSpace.SqlEncode (), page.Created.ToMySqlDateTime ());
					sw.WriteLine ("UPDATE PageContent SET Page='{0}', Namespace='{1}', Revision={2}, Title='{3}', User='{4}', " +
						      "LastModified='{5}', Comment='{6}', Content='{7}', Description='{8}' " +
						      "WHERE Page='{0}';",
						      page.Title.SqlEncodeForName (), page.NameSpace.SqlEncode (), -1, page.Title.SqlAndWikiEncode (), LookupUser (page.User), page.LastModified.ToMySqlDateTime (),
						      page.Comment.SqlEncode (), page.Text.SqlEncode (), String.Empty);
				} else {
					sw.WriteLine ("INSERT INTO Page (Name,Namespace,CreationDateTime) VALUES ('{0}','{1}','{2}');",
						      page.Title.SqlEncodeForName (), page.NameSpace.SqlEncode (), page.Created.ToMySqlDateTime ());
					sw.WriteLine ("INSERT INTO PageContent (Page,Namespace,Revision,Title,User,LastModified,Comment,Content,Description) " +
						      "VALUES ('{0}', '{1}', {2}, '{3}', '{4}', '{5}', '{6}', '{7}', '{8}');",
						      page.Title.SqlEncodeForName (), page.NameSpace.SqlEncode (), -1, page.Title.SqlAndWikiEncode (), LookupUser (page.User), page.LastModified.ToMySqlDateTime (),
						      page.Comment.SqlEncode (), page.Text.SqlEncode (), String.Empty);
					sw.WriteLine ("INSERT INTO PageKeyword (Page,Namespace,Revision,Keyword) VALUES ('{0}','{1}',-1,'');",
						      page.Title.SqlEncodeForName (), page.NameSpace.SqlEncode ());
				}
			}

			StepProgress ("{0} pages imported, {1} not imported, {2} old versions ignored.", imported, count - imported, olderVersionIgnored);
			if (ignoredPages.Count > 0) {
				StepProgress ("Ignored pages:");
				foreach (string s in ignoredPages)
					StepProgress ("  {0}", s);
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
