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
				sw.WriteLine ("INSERT INTO Category ('Name','Namespace') VALUES ('{0}', '');", (reader ["cl_to"] as string).SqlEncode ());
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
			while (reader.Read ()) {
				user = new WikiUser (reader);
				users.Add (user.Id, user.Name);
				count++;
				
				sw.WriteLine ("INSERT INTO User ('Username', 'PasswordHash', 'DisplayName','Email','Active','DateTime') " +
					      "VALUES ('{0}', '', '{1}', '{2}', t, '{3}')",
					      user.Name.SqlEncode (), user.RealName.SqlEncode (), user.Email, user.Touched.ToUniversalTime ());
			}
			StepProgress ("{0} users migrated", count);
		}
		
		sw.WriteLine ("/* Users: end */");
		sw.WriteLine ();
	}
	
	static void Pages (MySqlConnection conn, StreamWriter sw)
	{
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
			while (reader.Read ()) {
				processed++;
				try {
					page = new WikiPage (reader);
					page.ResolveCategories ();
					imported++;
				} catch (Exception ex) {
					StepProgress ("Page {0} not processed. Exception '{1}' occurred: {2}", processed, ex.GetType (), ex.Message);
					continue;
				}

				sw.WriteLine ("INSERT INTO Page ('Name','Namespace','CreationDate') VALUES ('{0}','{1}','{2}')",
					      page.Title.SqlEncode (), page.NameSpace.SqlEncode (), page.Created.ToUniversalTime ());
				sw.WriteLine ("INSERT INTO PageContent ('Page','Namespace','Revision','Title','User','LastModified','Comment','Content','Description') " +
					      "VALUES ('{0}', '{1}', {2}, '{3}, '{4}', '{5}', '{6}', '{7}', '{8}')",
					      page.Title.SqlEncode (), page.NameSpace.SqlEncode (), -1, page.Title.SqlAndWikiEncode (), LookupUser (page.User), page.LastModified.ToUniversalTime (),
					      page.Comment.SqlEncode (), page.Text.SqlEncode (), String.Empty);
			}

			StepProgress ("{0} pages imported, {1} not imported.", imported, count - imported);
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
