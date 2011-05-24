using System;
using System.Collections.Generic;

using MySql.Data.MySqlClient;

class WikiPage
{
	public uint Id { get; set; }
	public string NameSpace { get; set; }
	public string Title { get; set; }
	public string Text { get; internal set; }
	public string Comment { get; set; }
	public uint User { get; set; }
	public DateTime LastModified { get; set; }
	public DateTime Created { get; set; }
	public List <string> Categories { get; set; }
	
	public WikiPage (MySqlDataReader reader)
	{
		Id = (uint)reader ["cur_id"];
		NameSpace = reader ["cur_namespace"] as string;
		Title = reader ["cur_title"] as string;
		Text = reader ["cur_text"] as string;
		Comment = reader ["cur_comment"] as string;
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
}
