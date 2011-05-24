using System;

using MySql.Data.MySqlClient;

class WikiUser
{
	public uint Id { get; set; }
	public string Name { get; set; }
	public string RealName { get; set; }
	public string Email { get; set; }
	public DateTime Touched { get; set; }

	public WikiUser (MySqlDataReader reader)
	{
		Id = (uint)reader ["user_id"];
		Name = reader ["user_name"] as string;
		RealName = reader ["user_real_name"] as string;
		Email = reader ["user_email"] as string;
		Touched = (reader ["user_touched"] as string).ParseDekiTime ();
	}
}