
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.Plugins.SqlCommon;
using System.Data.SqlClient;
using System.Data.Common;
using System.Data.SqlServerCe;

namespace ScrewTurn.Wiki.Plugins.FSProviders {

	/// <summary>
	/// Implements a SQL Server-based users storage provider.
	/// </summary>
	public class SqlCEPagesStorageProvider : SqlPagesStorageProviderBase, IPagesStorageProviderV40 {

		private readonly ComponentInformation info = new ComponentInformation("SQL Server Pages Storage Provider", "Threeplicate Srl", "4.0.1.71", "http://www.screwturn.eu", null);

		private readonly SqlCECommandBuilder commandBuilder = new SqlCECommandBuilder();

		private const int CurrentSchemaVersion = 4000;

		/// <summary>
		/// Gets a new command with an open connection.
		/// </summary>
		/// <param name="connString">The connection string.</param>
		/// <returns>The command.</returns>
		private DbCommand GetCommand(string connString) {
			return commandBuilder.GetCommand(connString, "select current_user", new List<Parameter>()) as SqlCeCommand;
		}

		/// <summary>
		/// Gets a new command builder object.
		/// </summary>
		/// <returns>The command builder.</returns>
		protected override ICommandBuilder GetCommandBuilder() {
			return commandBuilder;
		}

		/// <summary>
		/// Validates a connection string.
		/// </summary>
		/// <param name="connString">The connection string to validate.</param>
		/// <remarks>If the connection string is invalid, the method throws <see cref="T:InvalidConfigurationException"/>.</remarks>
		protected override void ValidateConnectionString(string connString) {
			DbCommand cmd = null;
			try {
				cmd = GetCommand(connString);
			}
			catch(SqlException ex) {
				throw new InvalidConfigurationException("Provided connection string is not valid", ex);
			}
			catch(InvalidOperationException ex) {
				throw new InvalidConfigurationException("Provided connection string is not valid", ex);
			}
			catch(ArgumentException ex) {
				throw new InvalidConfigurationException("Provided connection string is not valid", ex);
			}
			finally {
				try {
					cmd.Connection.Close();
				}
				catch { }
			}
		}

		/// <summary>
		/// Detects whether the database schema exists.
		/// </summary>
		/// <returns><c>true</c> if the schema exists, <c>false</c> otherwise.</returns>
		private bool SchemaExists() {
			DbCommand cmd = GetCommand(connString);
			cmd.CommandText = "select [Version] from [Version] where [Component] = 'Pages'";

			bool exists = false;

			try {
				int version = ExecuteScalar<int>(cmd, -1);
				if(version > CurrentSchemaVersion) throw new InvalidConfigurationException("The version of the database schema is greater than the supported version");
				exists = version != -1;
			}
			catch(DbException) {
				exists = false;
			}
			finally {
				try {
					cmd.Connection.Close();
				}
				catch { }
			}

			return exists;
		}

		/// <summary>
		/// Detects whether the database schema needs to be updated.
		/// </summary>
		/// <returns><c>true</c> if an update is needed, <c>false</c> otherwise.</returns>
		private bool SchemaNeedsUpdate() {
			DbCommand cmd = GetCommand(connString);
			cmd.CommandText = "select [Version] from [Version] where [Component] = 'Pages'";

			bool exists = false;

			try {
				int version = ExecuteScalar<int>(cmd, -1);
				exists = version < CurrentSchemaVersion;
			}
			catch(SqlCeException) {
				exists = false;
			}
			finally {
				try {
					cmd.Connection.Close();
				}
				catch { }
			}

			return exists;
		}

		/// <summary>
		/// Creates the standard database schema.
		/// </summary>
		private void CreateStandardSchema() {
			DbCommand cmd = GetCommand(connString);

			try {
				cmd.CommandText = "create table [Namespace] ([Wiki] nvarchar(100) not null, [Name] nvarchar(100) not null, [DefaultPage] nvarchar(200), constraint [PK_Namespace] primary key ([Wiki], [Name]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [Category]([Wiki] nvarchar(100) not null, [Name] nvarchar(100) not null, [Namespace] nvarchar(100) not null, constraint [FK_Category_Namespace] foreign key ([Wiki], [Namespace]) references [Namespace]([Wiki], [Name])	on delete cascade on update cascade, constraint [PK_Category] primary key ([Wiki], [Name], [Namespace]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [PageContent] ([Wiki] nvarchar(100) not null, [Name] nvarchar(200) not null, [CreationDateTime] datetime not null, [Namespace] nvarchar(100) not null, [Revision] smallint not null, [Title] nvarchar(200) not null, [User] nvarchar(100) not null, [LastModified] datetime not null, [Comment] nvarchar(300), [Content] ntext not null, [Description] nvarchar(200), constraint [FK_Page_Namespace] foreign key ([Wiki], [Namespace]) references [Namespace]([Wiki], [Name]) on delete cascade on update cascade,constraint [PK_PageContent] primary key ([Wiki], [Name], [Namespace], [Revision]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [CategoryBinding] ([Wiki] nvarchar(100) not null, [Namespace] nvarchar(100) not null, [Category] nvarchar(100) not null, [Page] nvarchar(200) not null, constraint [FK_CategoryBinding_Namespace] foreign key ([Wiki], [Namespace]) references [Namespace]([Wiki], [Name]), constraint [FK_CategoryBinding_Category] foreign key ([Wiki], [Category], [Namespace]) references [Category]([Wiki], [Name], [Namespace]) on delete cascade on update cascade, constraint [PK_CategoryBinding] primary key ([Wiki], [Namespace], [Page], [Category]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [PageKeyword] ([Wiki] nvarchar(100) not null, [Page] nvarchar(200) not null, [Namespace] nvarchar(100) not null, [Revision] smallint not null, [Keyword] nvarchar(50) not null, constraint [FK_PageKeyword_PageContent] foreign key ([Wiki], [Page], [Namespace], [Revision]) references [PageContent]([Wiki], [Name], [Namespace], [Revision]) on delete cascade on update cascade, constraint [PK_PageKeyword] primary key ([Wiki], [Page], [Namespace], [Revision], [Keyword]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [Message] ([Wiki] nvarchar(100) not null, [Page] nvarchar(200) not null, [Namespace] nvarchar(100) not null, [Id] smallint not null, [Parent] smallint, [Username] nvarchar(100) not null, [Subject] nvarchar(200) not null, [DateTime] datetime not null, [Body] ntext not null, constraint [PK_Message] primary key ([Wiki], [Page], [Namespace], [Id]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [NavigationPath] ([Wiki] nvarchar(100) not null, [Name] nvarchar(100) not null, [Namespace] nvarchar(100) not null, [Page] nvarchar(200) not null, [Number] smallint not null, constraint [PK_NavigationPath] primary key ([Wiki], [Name], [Namespace], [Page]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [Snippet] ([Wiki] nvarchar(100) not null, [Name] nvarchar(200) not null, [Content] ntext not null, constraint [PK_Snippet] primary key ([Wiki], [Name]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [ContentTemplate] ([Wiki] nvarchar(100) not null, [Name] nvarchar(200) not null, [Content] ntext not null, constraint [PK_ContentTemplate] primary key ([Wiki], [Name]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [IndexDocument] ([Wiki] nvarchar(100) not null, [Id] int not null, [Name] nvarchar(200) not null, [Title] nvarchar(200) not null, [TypeTag] nvarchar(10) not null, [DateTime] datetime not null, constraint [UQ_IndexDocument] unique ([Wiki], [Name]), constraint [PK_IndexDocument] primary key ([Wiki], [Id]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [IndexWord] ([Wiki] nvarchar(100) not null, [Id] int not null, [Text] nvarchar(200) not null, constraint [UQ_IndexWord] unique ([Wiki], [Text]), constraint [PK_IndexWord] primary key ([Wiki], [Id]))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table [IndexWordMapping] ([Wiki] nvarchar(100) not null, [Word] int not null, [Document] int not null, [FirstCharIndex] smallint not null, [WordIndex] smallint not null, [Location] tinyint not null, constraint [FK_IndexWordMapping_IndexWord] foreign key ([Wiki], [Word]) references [IndexWord]([Wiki], [Id]) on delete cascade on update cascade, constraint [FK_IndexWordMapping_IndexDocument] foreign key ([Wiki], [Document]) references [IndexDocument]([Wiki], [Id]) on delete cascade on update cascade, constraint [PK_IndexWordMapping] primary key ([Wiki], [Word], [Document], [FirstCharIndex], [WordIndex], [Location]))";
				cmd.ExecuteNonQuery();
			}
			catch(DbException) { }
			finally {
				cmd.Connection.Close();
			}
		}

		private void InitNamespaceTable(string wikiName) {
			DbCommand cmd = GetCommand(connString);
			try {
				cmd.CommandText = "insert into [Namespace] ([Wiki], [Name], [DefaultPage]) values (@wiki, '', null)";
				cmd.Parameters.Add(new SqlCeParameter("Wiki", wikiName));

				cmd.ExecuteNonQuery();
			}
			catch(DbException) { }
			finally {
				cmd.Connection.Close();
			}
		}

		/// <summary>
		/// Creates or updates the database schema if necessary.
		/// </summary>
		protected override void CreateOrUpdateDatabaseIfNecessary() {
			if(!SchemaExists()) {
				CreateStandardSchema();
			}
			if(SchemaNeedsUpdate()) {
				// Run Update
			}
			foreach(PluginFramework.Wiki wiki in host.GetGlobalSettingsStorageProvider().AllWikis()) {
				InitNamespaceTable(wiki.WikiName);
			}
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public override ComponentInformation Information {
			get { return info; }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public override string ConfigHelpHtml {
			get { return "Connection string format:<br /><code>Data Source=<i>Database Address and Instance</i>;Initial Catalog=<i>Database name</i>;User ID=<i>login</i>;Password=<i>password</i>;</code>"; }
		}
	
	}

}
