using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using MySql.Data.MySqlClient;

class DekiMigration
{
	const string DEFAULT_CONNECTION_STRING = "Server=localhost; Database=monoproject; User=monoproject; Password=monoproject; CharSet=utf8";
	const string DEFAULT_OUTPUT_FILE = "mono-project-stw.sql";
	const string DEFAULT_DEKI_FILES_DIRECTORY = "/tmp/public_html/files";
	
	static Dictionary <uint, string> users = new Dictionary <uint, string> ();
	
	public static string ConnectionString { get; private set; }
	public static string DekiFilesDirectory { get; private set; }
		
	static void Main (string[] args)
	{
		int argslen = args.Length;
		DekiFilesDirectory = argslen > 0 ? args [1] : DEFAULT_DEKI_FILES_DIRECTORY;
		ConnectionString = argslen > 1 ? args [1] : DEFAULT_CONNECTION_STRING;
		string output_file = argslen > 2 ? args [2] : DEFAULT_OUTPUT_FILE;

		if (!Directory.Exists (DekiFilesDirectory)) {
			Console.Error.WriteLine ("DekiWiki files directory '{0}' does not exist. You can pass the directory as the first parameter.", DekiFilesDirectory);
			Environment.Exit (1);
		}
		
		var conn = new MySqlConnection (ConnectionString);
		conn.Open ();

		var cmd = conn.GetCommand ("SET character_set_client=utf8; SET character_set_database=utf8; SET character_set_server=utf8; SET character_set_results=utf8");
		cmd.ExecuteNonQuery ();
		
		using (var sw = new StreamWriter (output_file, false, Encoding.UTF8)) {
			sw.WriteLine ("/*!40101 SET NAMES utf8 */;");
			sw.WriteLine ("SET character_set_client=utf8; SET character_set_database=utf8; SET character_set_server=utf8; SET character_set_results=utf8;");
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
			int images = 0;
			bool isUpdate;
			DateTime cachedTimeStamp;
			while (reader.Read ()) {
				processed++;
				isUpdate = false;
				try {
					page = new WikiPage (reader, templates);
					if (page.Ignore && !page.IsRedirect) {
						StepProgress ("M", processed == 1);
						ignoredPages.Add (String.Format ("Ignored settings page '{0}'", page.Title));
						continue;
					}
					
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
					sw.WriteLine ("INSERT INTO Snippet (Name,Content) VALUES ('{0}', '{1}');", page.Title, page.Text.Replace ("{TOC}", String.Empty).SqlEncode ());
					continue;
				}
				
				if (page.DekiNameSpace == DekiNamespace.Image) {
					if (images == 0)
						sw.WriteLine ("INSERT INTO Directory (FullPath,Parent) VALUES ('/images/', '/');");
					images++;
					string imagePath = GetImagePath (page.Title);
					if (imagePath == null) {
						StepProgress ("F", processed == 1);
						failedPages.Add (String.Format ("Image '{0}' could not be processed (MD5 hash generation failed).", page.Title));
						continue;
					}

					if (!File.Exists (imagePath)) {
						StepProgress ("F", processed == 1);
						failedPages.Add (String.Format ("File '{0}' for image '{1}' does not exist.", imagePath, page.Title));
						continue;
					}

					var fi = new FileInfo (imagePath);
					sw.Write ("INSERT INTO File (Name,Directory,Size,Downloads,LastModified,Data) " +
						  "VALUES ('{0}', '/', {1}, 0, {2}, '",
						      page.Title.SqlEncodeForName (true), fi.Length, page.LastModified.ToMySqlDateTime ());
					WriteFileData (imagePath, sw);
					sw.WriteLine ("');");
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

					// Special case. Deki is able to handle templates which are redirects to a
					// regular (non-template) page, STW doesn't do that so we need to duplicate this
					// page as a snippet.
					if (page.Title == "Supported_Platforms")
						sw.WriteLine ("INSERT INTO Snippet (Name,Content) VALUES ('{0}', '{1}');", page.Title, page.Text.Replace ("{TOC}", String.Empty).SqlEncode ());
				}
			}
			Console.Error.WriteLine ();
			
			StepInfo ("{0} pages imported, {1} images, {2} ignored, {3} old versions ignored, {4} failed.", imported, images, count - imported, olderVersionIgnored, failedPages.Count);
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

	static string GetImagePath (string imageName)
	{
		if (String.IsNullOrEmpty (imageName))
			return null;		

		string[] files = Directory.GetFiles (DekiFilesDirectory, imageName, SearchOption.AllDirectories);
		return files [0];
	}

	static void WriteFileData (string filePath, StreamWriter sw)
	{
		using (FileStream fs = File.OpenRead (filePath)) {
			byte[] bytes = new byte [1024];
			int bread;
			
			while ((bread = fs.Read (bytes, 0, bytes.Length)) > 0) {
				byte b;
				for (int i = 0; i < bread; i++) {
					b = bytes [i];
					if (b == '\0') {
						sw.Write ("\\0");
					} else if (b == '\\' || b == '\'' || b == '"') {
						sw.Write ('\\');
						sw.Flush ();
						sw.BaseStream.WriteByte (b);
					} else if (b == '\n') {
						sw.Write ("\\n");
					} else if (b == '\r') {
						sw.Write ("\\r");
					} else if (b == 0x1a) {
						sw.Write ("\\Z");
					} else {
						sw.Flush ();
						sw.BaseStream.WriteByte (b);
					}
					sw.Flush ();
				}
			}
		}
	}
	
	static string LookupUser (uint id)
	{
		string ret;

		if (!users.TryGetValue (id, out ret) || String.IsNullOrEmpty (ret))
			return String.Empty;

		return ret;
	}
}
