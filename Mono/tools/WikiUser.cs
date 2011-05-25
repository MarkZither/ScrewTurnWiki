using System;

using MySql.Data.MySqlClient;

class WikiUser
{
	public uint Id { get; set; }
	public string Name { get; set; }
	public string RealName { get; set; }
	public string Email { get; set; }
	public DateTime Touched { get; set; }
	public string PasswordHash { get; set; }
	public bool IsAdmin { get; set; }
	
	public WikiUser (MySqlDataReader reader)
	{
		Id = (uint)reader ["user_id"];
		Name = reader ["user_name"] as string;
		RealName = reader ["user_real_name"] as string;
		Email = reader ["user_email"] as string;
		Touched = (reader ["user_touched"] as string).ParseDekiTime ();
		PasswordHash = (reader ["user_password"] as byte[]).TinyBlobToString ();
		if (String.IsNullOrEmpty (PasswordHash))
			PasswordHash = "invalidvalue";
	}

	public void ResolveGroups ()
	{
		using (var conn = new MySqlConnection (DekiMigration.ConnectionString)) {
			conn.Open ();
			var cmd = conn.GetCommand ("SELECT ur_rights FROM user_rights WHERE ur_user = ?Id", new Tuple <string, object> ("Id", Id));
			using (var reader = cmd.ExecuteReader ()) {
				string rights, trimmed;
				while (reader.Read ()) {
					rights = (reader ["ur_rights"] as byte[]).TinyBlobToString ();
					if (String.IsNullOrEmpty (rights))
						continue;
					
					foreach (string r in rights.Split (',')) {
						trimmed = r.Trim ();
						if (trimmed.Length == 0)
							continue;
						if (String.Compare (trimmed, "sysop", StringComparison.OrdinalIgnoreCase) != 0)
							continue;
						IsAdmin = true;
						break;
					}
				}
			}
		}
	}
}