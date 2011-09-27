﻿
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Plugins.SqlCommon {

	/// <summary>
	/// Implements a base class for a SQL pages storage provider.
	/// </summary>
	public abstract class SqlPagesStorageProviderBase : SqlStorageProviderBase, IPagesStorageProviderV40 {

		private const int MaxStatementsInBatch = 20;

		private const int FirstRevision = 0;
		private const int CurrentRevision = -1;
		private const int DraftRevision = -100;

		#region IPagesStorageProvider Members

		/// <summary>
		/// Gets a namespace.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="name">The name of the namespace (cannot be <c>null</c> or empty).</param>
		/// <returns>The <see cref="T:NamespaceInfo" />, or <c>null</c> if no namespace is found.</returns>
		private NamespaceInfo GetNamespace(DbTransaction transaction, string name) {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			// select ... from Namespace left join Page on Namespace.DefaultPage = Page.Name where Namespace.Name = <name> and (Namespace.DefaultPage is null or Page.Namespace = <name>)
			string query = queryBuilder.SelectFrom("Namespace", "PageContent", new string[] { "Wiki", "DefaultPage" }, new string[] { "Wiki", "Name" }, Join.LeftJoin, new string[] { "Name", "DefaultPage" }, new string[] { "CreationDateTime" });
			query = queryBuilder.Where(query, "Namespace", "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Namespace", "Name", WhereOperator.Equals, "Name1");
			query = queryBuilder.AndWhere(query, "Namespace", "DefaultPage", WhereOperator.IsNull, null, true, false);
			query = queryBuilder.OrWhere(query, "PageContent", "Namespace", WhereOperator.Equals, "Name2", false, true);

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name1", name));
			parameters.Add(new Parameter(ParameterType.String, "Name2", name));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				NamespaceInfo result = null;

				if(reader.Read()) {
					string realName = reader["Namespace_Name"] as string;
					string page = GetNullableColumn<string>(reader, "Namespace_DefaultPage", null);
					string defaultPageFullName = string.IsNullOrEmpty(page) ? null : NameTools.GetFullName(realName, page);

					result = new NamespaceInfo(realName, this, defaultPageFullName);
				}

				CloseReader(reader);

				return result;
			}
			else return null;
		}

		/// <summary>
		/// Gets a namespace.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="name">The name of the namespace (cannot be <c>null</c> or empty).</param>
		/// <returns>The <see cref="T:NamespaceInfo" />, or <c>null</c> if no namespace is found.</returns>
		private NamespaceInfo GetNamespace(DbConnection connection, string name) {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			// select ... from Namespace left join Page on Namespace.DefaultPage = Page.Name where Namespace.Name = <name> and (Namespace.DefaultPage is null or Page.Namespace = <name>)
			string query = queryBuilder.SelectFrom("Namespace", "PageContent", new string[] { "Wiki", "DefaultPage" }, new string[] { "Wiki", "Name" }, Join.LeftJoin, new string[] { "Name", "DefaultPage" }, new string[] { "CreationDateTime" });
			query = queryBuilder.Where(query, "Namespace", "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Namespace", "Name", WhereOperator.Equals, "Name1");
			query = queryBuilder.AndWhere(query, "Namespace", "DefaultPage", WhereOperator.IsNull, null, true, false);
			query = queryBuilder.OrWhere(query, "PageContent", "Namespace", WhereOperator.Equals, "Name2", false, true);

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name1", name));
			parameters.Add(new Parameter(ParameterType.String, "Name2", name));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				NamespaceInfo result = null;

				if(reader.Read()) {
					string realName = reader["Namespace_Name"] as string;
					string page = GetNullableColumn<string>(reader, "Namespace_DefaultPage", null);
					string defaultPageFullName = string.IsNullOrEmpty(page) ? null : NameTools.GetFullName(realName, page);

					result = new NamespaceInfo(realName, this, defaultPageFullName);
				}

				CloseReader(reader);

				return result;
			}
			else return null;
		}

		/// <summary>
		/// Gets a namespace.
		/// </summary>
		/// <param name="name">The name of the namespace (cannot be <c>null</c> or empty).</param>
		/// <returns>The <see cref="T:NamespaceInfo"/>, or <c>null</c> if no namespace is found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		public NamespaceInfo GetNamespace(string name) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			NamespaceInfo nspace = GetNamespace(connection, name);
			CloseConnection(connection);

			return nspace;
		}

		/// <summary>
		/// Gets all the sub-namespaces.
		/// </summary>
		/// <returns>The sub-namespaces, sorted by name.</returns>
		public NamespaceInfo[] GetNamespaces() {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			// select ... from Namespace left join Page on Namespace.DefaultPage = Page.Name where Namespace.Name <> '' and (Namespace.DefaultPage is null or Page.Namespace <> '')
			string query = queryBuilder.SelectFrom("Namespace", "PageContent", new string[] {"Wiki", "DefaultPage"}, new string[] {"Wiki", "Name"}, Join.LeftJoin, new string[] { "Name", "DefaultPage" }, new string[] { "CreationDateTime" });
			query = queryBuilder.Where(query, "Namespace", "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Namespace", "Name", WhereOperator.NotEquals, "Empty1");
			query = queryBuilder.AndWhere(query, "Namespace", "DefaultPage", WhereOperator.IsNull, null, true, false);
			query = queryBuilder.OrWhere(query, "PageContent", "Namespace", WhereOperator.NotEquals, "Empty2", false, true);
			query = queryBuilder.OrderBy(query, new[] { "Namespace_Name" }, new[] { Ordering.Asc });

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Empty1", ""));
			parameters.Add(new Parameter(ParameterType.String, "Empty2", ""));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<NamespaceInfo> result = new List<NamespaceInfo>(10);

				while(reader.Read()) {
					string realName = reader["Namespace_Name"] as string;
					string page = GetNullableColumn<string>(reader, "Namespace_DefaultPage", null);
					string defaultPageFullName = string.IsNullOrEmpty(page) ? null : NameTools.GetFullName(realName, page);

					// The query returns duplicate entries if the main page of two or more namespaces have the same name
					if(result.Find(n => { return n.Name.Equals(realName); }) == null) {
						result.Add(new NamespaceInfo(realName, this, defaultPageFullName));
					}
				}

				CloseReader(command, reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Adds a new namespace.
		/// </summary>
		/// <param name="name">The name of the namespace.</param>
		/// <returns>The correct <see cref="T:NamespaceInfo"/> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		public NamespaceInfo AddNamespace(string name) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.InsertInto("Namespace", new string[] { "Wiki", "Name" }, new string[] { "Wiki", "Name" });

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			int rows = ExecuteNonQuery(command);

			if(rows == 1) return new NamespaceInfo(name, this, null);
			else return null;
		}

		/// <summary>
		/// Renames a namespace.
		/// </summary>
		/// <param name="nspace">The namespace to rename.</param>
		/// <param name="newName">The new name of the namespace.</param>
		/// <returns>The correct <see cref="T:NamespaceInfo"/> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="nspace"/> or <paramref name="newName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="newName"/> is empty.</exception>
		public NamespaceInfo RenameNamespace(NamespaceInfo nspace, string newName) {
			if(nspace == null) throw new ArgumentNullException("nspace");
			if(newName == null) throw new ArgumentNullException("newName");
			if(newName.Length == 0) throw new ArgumentException("New Name cannot be empty", "newName");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			if(GetNamespace(transaction, nspace.Name) == null) {
				RollbackTransaction(transaction);
				return null;
			}

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.Update("Namespace", new string[] { "Name" }, new string[] { "NewName" });
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "OldName");

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "NewName", newName));
			parameters.Add(new Parameter(ParameterType.String, "OldName", nspace.Name));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows > 0) {
				query = queryBuilder.Update("Message", new string[] { "Namespace" }, new string[] { "NewName" });
				query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
				query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");

				parameters = new List<Parameter>(4);
				parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
				parameters.Add(new Parameter(ParameterType.String, "NewName", newName));
				parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace.Name));

				command = builder.GetCommand(transaction, query, parameters);

				rows = ExecuteNonQuery(command, false);

				query = queryBuilder.Update("NavigationPath", new string[] { "Namespace" }, new string[] { "NewName" });
				query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
				query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");

				parameters = new List<Parameter>(4);
				parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
				parameters.Add(new Parameter(ParameterType.String, "NewName", newName));
				parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace.Name));

				command = builder.GetCommand(transaction, query, parameters);

				rows = ExecuteNonQuery(command, false);

				NamespaceInfo result = GetNamespace(transaction, newName);

				CommitTransaction(transaction);

				return result;
			}
			else {
				RollbackTransaction(transaction);
				return null;
			}
		}

		/// <summary>
		/// Sets the default page of a namespace.
		/// </summary>
		/// <param name="nspace">The namespace of which to set the default page.</param>
		/// <param name="pageFullName">The full name of the page to use as default page, or <c>null</c>.</param>
		/// <returns>The correct <see cref="T:NamespaceInfo"/> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="nspace"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		public NamespaceInfo SetNamespaceDefaultPage(NamespaceInfo nspace, string pageFullName) {
			if(nspace == null) throw new ArgumentNullException("nspace");

			// Namespace existence is verified by the affected rows (should be 1)

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			if(pageFullName != null && GetPage(transaction, pageFullName, CurrentRevision) == null) {
				RollbackTransaction(transaction);
				return null;
			}

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.Update("Namespace", new string[] { "DefaultPage" }, new string[] { "DefaultPage" });
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			if(pageFullName == null) parameters.Add(new Parameter(ParameterType.String, "DefaultPage", DBNull.Value));
			else parameters.Add(new Parameter(ParameterType.String, "DefaultPage", NameTools.GetLocalName(pageFullName)));
			parameters.Add(new Parameter(ParameterType.String, "Name", nspace.Name));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows == 1) {
				CommitTransaction(transaction);
				return new NamespaceInfo(nspace.Name, this, pageFullName);
			}
			else {
				RollbackTransaction(transaction);
				return null;
			}
		}

		/// <summary>
		/// Removes a namespace.
		/// </summary>
		/// <param name="nspace">The namespace to remove.</param>
		/// <returns><c>true</c> if the namespace is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="nspace"/> is <c>null</c>.</exception>
		public bool RemoveNamespace(NamespaceInfo nspace) {
			if(nspace == null) throw new ArgumentNullException("nspace");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("Namespace");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", nspace.Name));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);
			if(rows > 0) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return rows > 0;
		}

		/// <summary>
		/// Determines whether a page is the default page of its namespace.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="pageFullName">The page full name.</param>
		/// <returns><c>true</c> if the page is the default page, <c>false</c> otherwise.</returns>
		private bool IsDefaultPage(DbTransaction transaction, string pageFullName) {
			string nspaceName = NameTools.GetNamespace(pageFullName);
			if(string.IsNullOrEmpty(nspaceName)) return false;

			NamespaceInfo nspace = GetNamespace(transaction, nspaceName);
			if(nspace == null) return false;
			else {
				if(nspace.DefaultPageFullName != null) return nspace.DefaultPageFullName.ToLowerInvariant() == pageFullName.ToLowerInvariant();
				else return false;
			}
		}

		/// <summary>
		/// Moves a page from its namespace into another.
		/// </summary>
		/// <param name="pageFullName">The full name of the page to move.</param>
		/// <param name="destination">The destination namespace (<c>null</c> for the root).</param>
		/// <param name="copyCategories">A value indicating whether to copy the page categories in the destination
		/// namespace, if not already available.</param>
		/// <returns>The correct instance of <see cref="T:PageContent"/>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		public PageContent MovePage(string pageFullName, NamespaceInfo destination, bool copyCategories) {
			if(pageFullName == null) throw new ArgumentNullException("page");

			// Check:
			// 1. Same namespace - ROOT, SUB (explicit check)
			// 2. Destination existence (update query affects 0 rows because it would break a FK)
			// 3. Page existence in target (update query affects 0 rows because it would break a FK)
			// 4. Page is default page of its namespace (explicit check)

			string destinationName = destination != null ? destination.Name : "";
			string sourceName = null;
			string pageName = null;
			NameTools.ExpandFullName(pageFullName, out sourceName, out pageName);
			if(sourceName == null) sourceName = "";

			if(destinationName.ToLowerInvariant() == sourceName.ToLowerInvariant()) return null;

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			if(IsDefaultPage(transaction, pageFullName)) {
				RollbackTransaction(transaction);
				return null;
			}

			PageContent currentContent = GetPage(transaction, pageFullName, CurrentRevision);
			
			CategoryInfo[] currCategories = GetCategories(transaction, sourceName == "" ? null : GetNamespace(transaction, sourceName));

			// Remove bindings
			RebindPage(transaction, pageFullName, new string[0]);

			string[] newCategories = new string[0];

			if(copyCategories) {
				// Retrieve categories for page
				// Copy missing ones in destination

				string lowerPageName = pageFullName.ToLowerInvariant();

				List<string> pageCategories = new List<string>(10);
				foreach(CategoryInfo cat in currCategories) {
					if(Array.Find(cat.Pages, (s) => { return s.ToLowerInvariant() == lowerPageName; }) != null) {
						pageCategories.Add(NameTools.GetLocalName(cat.FullName));
					}
				}

				// Create categories into destination without checking existence (AddCategory will return null)
				string tempName = destinationName == "" ? null : destinationName;
				newCategories = new string[pageCategories.Count];

				for(int i = 0; i < pageCategories.Count; i++) {
					string catName = NameTools.GetFullName(tempName, pageCategories[i]);
					if(GetCategory(transaction, catName) == null) {
						CategoryInfo added = AddCategory(tempName, pageCategories[i]);
					}
					newCategories[i] = catName;
				}
			}

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.Update("PageContent", new string[] { "Namespace" }, new string[] { "Destination" });
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Source");

			List<Parameter> parameters = new List<Parameter>(4);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Destination", destinationName));
			parameters.Add(new Parameter(ParameterType.String, "Name", pageName));
			parameters.Add(new Parameter(ParameterType.String, "Source", sourceName));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows > 0) {
				query = queryBuilder.Update("Message", new string[] { "Namespace" }, new string[] { "Destination" });
				query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
				query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");
				query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");

				parameters = new List<Parameter>(4);
				parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
				parameters.Add(new Parameter(ParameterType.String, "Destination", destinationName));
				parameters.Add(new Parameter(ParameterType.String, "Page", pageName));
				parameters.Add(new Parameter(ParameterType.String, "Namespace", sourceName));

				command = builder.GetCommand(transaction, query, parameters);

				rows = ExecuteNonQuery(command, false);

				query = queryBuilder.Update("NavigationPath", new string[] { "Namespace" }, new string[] { "Destination" });
				query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
				query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");
				query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");

				parameters = new List<Parameter>(4);
				parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
				parameters.Add(new Parameter(ParameterType.String, "Destination", destinationName));
				parameters.Add(new Parameter(ParameterType.String, "Page", pageName));
				parameters.Add(new Parameter(ParameterType.String, "Namespace", sourceName));

				command = builder.GetCommand(transaction, query, parameters);

				rows = ExecuteNonQuery(command, false);

				PageContent newContent = currentContent;
				newContent.FullName = NameTools.GetFullName(destinationName, pageName);

				// Re-bind categories
				if(copyCategories) {
					bool rebound = RebindPage(transaction, newContent.FullName, newCategories);
					if(!rebound) {
						RollbackTransaction(transaction);
						return null;
					}
				}

				CommitTransaction(transaction);

				return newContent;
			}
			else {
				RollbackTransaction(transaction);
				return null;
			}
		}

		/// <summary>
		/// Gets a category.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="fullName">The full name of the category.</param>
		/// <returns>The <see cref="T:CategoryInfo" />, or <c>null</c> if no category is found.</returns>
		private CategoryInfo GetCategory(DbTransaction transaction, string fullName) {
			string nspace = null;
			string name = null;
			NameTools.ExpandFullName(fullName, out nspace, out name);
			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("Category", "CategoryBinding", new string[] { "Wiki", "Name", "Namespace" }, new string[] { "Wiki", "Category", "Namespace" }, Join.LeftJoin,
				new string[] { "Name", "Namespace" }, new string[] { "Page" });
			query = queryBuilder.Where(query, "Category", "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Category", "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.AndWhere(query, "Category", "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(4);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				CategoryInfo result = null;
				List<string> pages = new List<string>(50);

				while(reader.Read()) {
					if(result == null) result = new CategoryInfo(NameTools.GetFullName(reader["Category_Namespace"] as string, reader["Category_Name"] as string), this);

					if(!IsDBNull(reader, "CategoryBinding_Page")) {
						pages.Add(NameTools.GetFullName(reader["Category_Namespace"] as string, reader["CategoryBinding_Page"] as string));
					}
				}

				CloseReader(reader);

				if(result != null) result.Pages = pages.ToArray();

				return result;
			}
			else return null;
		}

		/// <summary>
		/// Gets a category.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="fullName">The full name of the category.</param>
		/// <returns>The <see cref="T:CategoryInfo" />, or <c>null</c> if no category is found.</returns>
		private CategoryInfo GetCategory(DbConnection connection, string fullName) {
			string nspace = null;
			string name = null;
			NameTools.ExpandFullName(fullName, out nspace, out name);
			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("Category", "CategoryBinding", new string[] { "Wiki", "Name", "Namespace" }, new string[] { "Wiki", "Category", "Namespace" }, Join.LeftJoin,
				new string[] { "Name", "Namespace" }, new string[] { "Page" });
			query = queryBuilder.Where(query, "Category", "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Category", "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.AndWhere(query, "Category", "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(4);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				CategoryInfo result = null;
				List<string> pages = new List<string>(50);

				while(reader.Read()) {
					if(result == null) result = new CategoryInfo(NameTools.GetFullName(reader["Category_Namespace"] as string, reader["Category_Name"] as string), this);

					if(!IsDBNull(reader, "CategoryBinding_Page")) {
						pages.Add(NameTools.GetFullName(reader["Category_Namespace"] as string, reader["CategoryBinding_Page"] as string));
					}
				}

				CloseReader(reader);

				if(result != null) result.Pages = pages.ToArray();

				return result;
			}
			else return null;
		}

		/// <summary>
		/// Gets a category.
		/// </summary>
		/// <param name="fullName">The full name of the category.</param>
		/// <returns>The <see cref="T:CategoryInfo"/>, or <c>null</c> if no category is found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		public CategoryInfo GetCategory(string fullName) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("Full Name cannot be empty", "fullName");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			CategoryInfo category = GetCategory(connection, fullName);
			CloseConnection(connection);

			return category;
		}

		/// <summary>
		/// Gets all the Categories in a namespace.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="nspace">The namespace.</param>
		/// <returns>All the Categories in the namespace. The array is not sorted.</returns>
		private CategoryInfo[] GetCategories(DbTransaction transaction, NamespaceInfo nspace) {
			string nspaceName = nspace != null ? nspace.Name : "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("Category", "CategoryBinding", new string[] { "Wiki", "Name", "Namespace" }, new string[] { "Wiki", "Category", "Namespace" }, Join.LeftJoin,
				new string[] { "Name", "Namespace" }, new string[] { "Page" });
			query = queryBuilder.Where(query, "Category", "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Category", "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.OrderBy(query, new string[] { "Category_Name", "CategoryBinding_Page" }, new Ordering[] { Ordering.Asc, Ordering.Asc });

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspaceName));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<CategoryInfo> result = new List<CategoryInfo>(20);
				List<string> pages = new List<string>(50);

				string prevName = "|||";
				string name = null;

				while(reader.Read()) {
					name = reader["Category_Name"] as string;

					if(name != prevName) {
						if(prevName != "|||") {
							result[result.Count - 1].Pages = pages.ToArray();
							pages.Clear();
						}

						result.Add(new CategoryInfo(NameTools.GetFullName(reader["Category_Namespace"] as string, name), this));
					}

					prevName = name;
					if(!IsDBNull(reader, "CategoryBinding_Page")) {
						pages.Add(NameTools.GetFullName(reader["Category_Namespace"] as string, reader["CategoryBinding_Page"] as string));
					}
				}

				CloseReader(reader);

				if(result.Count > 0) result[result.Count - 1].Pages = pages.ToArray();

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Gets all the Categories in a namespace.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="nspace">The namespace.</param>
		/// <returns>All the Categories in the namespace. The array is not sorted.</returns>
		private CategoryInfo[] GetCategories(DbConnection connection, NamespaceInfo nspace) {
			string nspaceName = nspace != null ? nspace.Name : "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("Category", "CategoryBinding", new string[] { "Wiki", "Name", "Namespace" }, new string[] { "Wiki", "Category", "Namespace" }, Join.LeftJoin,
				new string[] { "Name", "Namespace" }, new string[] { "Page" });
			query = queryBuilder.Where(query, "Category", "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Category", "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.OrderBy(query, new string[] { "Category_Name", "CategoryBinding_Page" }, new Ordering[] { Ordering.Asc, Ordering.Asc });

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspaceName));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<CategoryInfo> result = new List<CategoryInfo>(20);
				List<string> pages = new List<string>(50);

				string prevName = "|||";
				string name = null;

				while(reader.Read()) {
					name = reader["Category_Name"] as string;

					if(name != prevName) {
						if(prevName != "|||") {
							result[result.Count - 1].Pages = pages.ToArray();
							pages.Clear();
						}

						result.Add(new CategoryInfo(NameTools.GetFullName(reader["Category_Namespace"] as string, name), this));
					}

					prevName = name;
					if(!IsDBNull(reader, "CategoryBinding_Page")) {
						pages.Add(NameTools.GetFullName(reader["Category_Namespace"] as string, reader["CategoryBinding_Page"] as string));
					}
				}

				CloseReader(reader);

				if(result.Count > 0) result[result.Count - 1].Pages = pages.ToArray();

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Gets all the Categories in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace.</param>
		/// <returns>All the Categories in the namespace, sorted by name.</returns>
		public CategoryInfo[] GetCategories(NamespaceInfo nspace) {
			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			CategoryInfo[] categories = GetCategories(connection, nspace);
			CloseConnection(connection);

			return categories;
		}

		/// <summary>
		/// Gets all the categories of a page.
		/// </summary>
		/// <param name="pageFullName">The page full name.</param>
		/// <returns>The categories, sorted by name.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> is <c>null</c>.</exception>
		public CategoryInfo[] GetCategoriesForPage(string pageFullName) {
			if(pageFullName == null) throw new ArgumentNullException("page");

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string nspace, pageName;
			NameTools.ExpandFullName(pageFullName, out nspace, out pageName);
			if(nspace == null) nspace = "";

			string query = queryBuilder.SelectFrom("Category", "CategoryBinding", new string[] { "Wiki", "Name", "Namespace" }, new string[] { "Wiki", "Category", "Namespace" }, Join.LeftJoin,
				new string[] { "Name", "Namespace" }, new string[] { "Page" });
			query = queryBuilder.Where(query, "CategoryBinding", "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "CategoryBinding", "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.AndWhere(query, "CategoryBinding", "Page", WhereOperator.Equals, "Page");
			query = queryBuilder.OrderBy(query, new[] { "Category_Name" }, new[] { Ordering.Asc });

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
			parameters.Add(new Parameter(ParameterType.String, "Page", pageName));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<CategoryInfo> result = new List<CategoryInfo>(20);
				List<string> pages = new List<string>(50);

				string prevName = "|||";
				string name = null;

				while(reader.Read()) {
					name = reader["Category_Name"] as string;

					if(name != prevName) {
						if(prevName != "|||") {
							result[result.Count - 1].Pages = pages.ToArray();
							pages.Clear();
						}

						result.Add(new CategoryInfo(NameTools.GetFullName(reader["Category_Namespace"] as string, name), this));
					}

					prevName = name;
					if(!IsDBNull(reader, "CategoryBinding_Page")) {
						pages.Add(NameTools.GetFullName(reader["Category_Namespace"] as string, reader["CategoryBinding_Page"] as string));
					}
				}

				CloseReader(command, reader);

				if(result.Count > 0) result[result.Count - 1].Pages = pages.ToArray();

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Adds a Category.
		/// </summary>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Category name.</param>
		/// <returns>The correct CategoryInfo object.</returns>
		/// <remarks>The method should set category's Pages to an empty array.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		public CategoryInfo AddCategory(string nspace, string name) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();

			string query = QueryBuilder.NewQuery(builder).InsertInto("Category", new string[] { "Wiki", "Name", "Namespace" }, new string[] { "Wiki", "Name", "Namespace" });

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			int rows = ExecuteNonQuery(command);

			if(rows == 1) return new CategoryInfo(NameTools.GetFullName(nspace, name), this);
			else return null;
		}

		/// <summary>
		/// Renames a Category.
		/// </summary>
		/// <param name="category">The Category to rename.</param>
		/// <param name="newName">The new Name.</param>
		/// <returns>The correct CategoryInfo object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="category"/> or <paramref name="newName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="newName"/> is empty.</exception>
		public CategoryInfo RenameCategory(CategoryInfo category, string newName) {
			if(category == null) throw new ArgumentNullException("category");
			if(newName == null) throw new ArgumentNullException("newName");
			if(newName.Length == 0) throw new ArgumentException("New Name cannot be empty", "newName");

			string nspace = null;
			string name = null;
			NameTools.ExpandFullName(category.FullName, out nspace, out name);
			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.Update("Category", new string[] { "Name" }, new string[] { "NewName" });
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "OldName");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");

			List<Parameter> parameters = new List<Parameter>(4);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "NewName", newName));
			parameters.Add(new Parameter(ParameterType.String, "OldName", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows > 0) {
				CategoryInfo result = GetCategory(transaction, NameTools.GetFullName(nspace, newName));
				CommitTransaction(transaction);
				return result;
			}
			else {
				RollbackTransaction(transaction);
				return null;
			}
		}

		/// <summary>
		/// Removes a Category.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="category">The Category to remove.</param>
		/// <returns>True if the Category has been removed successfully.</returns>
		private bool RemoveCategory(DbTransaction transaction, CategoryInfo category) {
			string nspace = null;
			string name = null;
			NameTools.ExpandFullName(category.FullName, out nspace, out name);
			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("Category");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			return rows > 0;
		}

		/// <summary>
		/// Removes a Category.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="category">The Category to remove.</param>
		/// <returns>True if the Category has been removed successfully.</returns>
		private bool RemoveCategory(DbConnection connection, CategoryInfo category) {
			string nspace = null;
			string name = null;
			NameTools.ExpandFullName(category.FullName, out nspace, out name);
			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("Category");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			return rows > 0;
		}

		/// <summary>
		/// Removes a Category.
		/// </summary>
		/// <param name="category">The Category to remove.</param>
		/// <returns>True if the Category has been removed successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="category"/> is <c>null</c>.</exception>
		public bool RemoveCategory(CategoryInfo category) {
			if(category == null) throw new ArgumentNullException("category");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			bool removed = RemoveCategory(connection, category);
			CloseConnection(connection);

			return removed;
		}

		/// <summary>
		/// Merges two Categories.
		/// </summary>
		/// <param name="source">The source Category.</param>
		/// <param name="destination">The destination Category.</param>
		/// <returns>The correct <see cref="T:CategoryInfo" /> object.</returns>
		/// <remarks>The destination Category remains, while the source Category is deleted, and all its Pages re-bound 
		/// in the destination Category.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> are <c>null</c>.</exception>
		public CategoryInfo MergeCategories(CategoryInfo source, CategoryInfo destination) {
			if(source == null) throw new ArgumentNullException("source");
			if(destination == null) throw new ArgumentNullException("destination");

			// 1. Check for same namespace
			// 2. Load all pages in source
			// 3. Load all pages in destination
			// 4. Merge lists in memory
			// 5. Delete all destination bindings
			// 6. Delete source cat
			// 7. Insert new bindings stored in memory

			string sourceNs = NameTools.GetNamespace(source.FullName);
			string destinationNs = NameTools.GetNamespace(destination.FullName);
			
			// If one is null and the other not null, fail
			if(sourceNs == null && destinationNs != null || sourceNs != null && destinationNs == null) return null;
			else {
				// Both non-null or both null
				if(sourceNs != null) {
					// Both non-null, check names
					NamespaceInfo tempSource = new NamespaceInfo(sourceNs, this, null);
					NamespaceInfo tempDest = new NamespaceInfo(destinationNs, this, null);
					// Different names, fail
					if(new NamespaceComparer().Compare(tempSource, tempDest) != 0) return null;
				}
				// else both null, OK
			}

			string nspace = sourceNs != null ? sourceNs : "";

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			CategoryInfo actualSource = GetCategory(transaction, source.FullName);
			CategoryInfo actualDestination = GetCategory(transaction, destination.FullName);

			if(actualSource == null) {
				RollbackTransaction(transaction);
				return null;
			}
			if(actualDestination == null) {
				RollbackTransaction(transaction);
				return null;
			}

			string destinationName = NameTools.GetLocalName(actualDestination.FullName);

			string[] mergedPages = MergeArrays(actualSource.Pages, actualDestination.Pages);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("CategoryBinding");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.AndWhere(query, "Category", WhereOperator.Equals, "Category");

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
			parameters.Add(new Parameter(ParameterType.String, "Category", destinationName));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows == -1) {
				RollbackTransaction(transaction);
				return null;
			}

			if(!RemoveCategory(transaction, source)) {
				RollbackTransaction(transaction);
				return null;
			}

			rows = 0;
			foreach(string page in mergedPages) {
				query = queryBuilder.InsertInto("CategoryBinding", new string[] { "Wiki", "Namespace", "Category", "Page" },
					new string[] { "Wiki", "Namespace", "Category", "Page" });

				parameters = new List<Parameter>(4);
				parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
				parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
				parameters.Add(new Parameter(ParameterType.String, "Category", destinationName));
				parameters.Add(new Parameter(ParameterType.String, "Page", NameTools.GetLocalName(page)));

				command = builder.GetCommand(transaction, query, parameters);
				rows += ExecuteNonQuery(command, false);
			}
			if(rows == mergedPages.Length) {
				CommitTransaction(transaction);
				CategoryInfo result = new CategoryInfo(actualDestination.FullName, this);
				result.Pages = mergedPages;
				return result;
			}
			else {
				RollbackTransaction(transaction);
				return null;
			}
		}

		/// <summary>
		/// Merges two arrays of strings.
		/// </summary>
		/// <param name="array1">The first array.</param>
		/// <param name="array2">The second array.</param>
		/// <returns>The merged array.</returns>
		private static string[] MergeArrays(string[] array1, string[] array2) {
			List<string> result = new List<string>(array1.Length + array2.Length);

			// A) BinarySearch is O(log n), but Insert is O(n) (+ QuickSort which is O(n*log n))
			// B) A linear search is O(n), and Add is O(1) (given that the list is already big enough)
			// --> B is faster, even when result approaches a size of array1.Length + array2.Length

			StringComparer comp = StringComparer.OrdinalIgnoreCase;

			result.AddRange(array1);

			foreach(string value in array2) {
				if(result.Find(x => { return comp.Compare(x, value) == 0; }) == null) {
					result.Add(value);
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Gets all the Pages in a namespace.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>All the Pages in the namespace. The array is not sorted.</returns>
		private PageContent[] GetPages(DbTransaction transaction, NamespaceInfo nspace) {
			string nspaceName = nspace != null ? nspace.Name : "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("PageContent", "PageKeyword", new string[] { "Wiki", "Name", "Namespace", "Revision" }, new string[] { "Wiki", "Page", "Namespace", "Revision" }, Join.LeftJoin,
				new string[] { "Name", "CreationDateTime", "Title", "User", "LastModified", "Comment", "Content", "Description" }, new string[] { "Keyword" });
			query = queryBuilder.Where(query, "PageContent", "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "PageContent", "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.AndWhere(query, "PageContent", "Revision", WhereOperator.Equals, "Revision");

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspaceName));
			parameters.Add(new Parameter(ParameterType.Int16, "Revision", (short)CurrentRevision));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<PageContent> result = new List<PageContent>();

				string name = null, title = null, user = null, comment = null, content = null, description = null;
				DateTime dateTime = DateTime.MinValue, creationDateTime = DateTime.MinValue;
				List<string> keywords = new List<string>(10);

				while(reader.Read()) {
					name = reader["PageContent_Name"] as string;
					creationDateTime = new DateTime(((DateTime)reader["PageContent_CreationDateTime"]).Ticks, DateTimeKind.Utc);
					title = reader["PageContent_Title"] as string;
					user = reader["PageContent_User"] as string;
					dateTime = new DateTime(((DateTime)reader["PageContent_LastModified"]).Ticks, DateTimeKind.Utc);
					comment = GetNullableColumn<string>(reader, "PageContent_Comment", "");
					content = reader["PageContent_Content"] as string;
					description = GetNullableColumn<string>(reader, "PageContent_Description", null);

					if(!IsDBNull(reader, "PageKeyword_Keyword")) {
						keywords.Add(reader["PageKeyword_Keyword"] as string);
					}

					result.Add(new PageContent(NameTools.GetFullName(nspaceName, name), this, creationDateTime, title, user, dateTime, comment, content, keywords.ToArray(), description));
				}

				CloseReader(reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Gets all the Pages in a namespace.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>All the Pages in the namespace. The array is not sorted.</returns>
		private PageContent[] GetPages(DbConnection connection, NamespaceInfo nspace) {
			string nspaceName = nspace != null ? nspace.Name : "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("PageContent", "PageKeyword", new string[] { "Wiki", "Name", "Namespace", "Revision" }, new string[] { "Wiki", "Page", "Namespace", "Revision" }, Join.LeftJoin,
				new string[] { "Name", "CreationDateTime", "Title", "User", "LastModified", "Comment", "Content", "Description" }, new string[] { "Keyword" });
			query = queryBuilder.Where(query, "PageContent", "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "PageContent", "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.AndWhere(query, "PageContent", "Revision", WhereOperator.Equals, "Revision");

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspaceName));
			parameters.Add(new Parameter(ParameterType.Int16, "Revision", (short)CurrentRevision));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<PageContent> result = new List<PageContent>();

				string name = null, title = null, user = null, comment = null, content = null, description = null;
				DateTime dateTime = DateTime.MinValue, creationDateTime = DateTime.MinValue;
				List<string> keywords = new List<string>(10);

				while(reader.Read()) {
					name = reader["PageContent_Name"] as string;
					creationDateTime = new DateTime(((DateTime)reader["PageContent_CreationDateTime"]).Ticks, DateTimeKind.Utc);
					title = reader["PageContent_Title"] as string;
					user = reader["PageContent_User"] as string;
					dateTime = new DateTime(((DateTime)reader["PageContent_LastModified"]).Ticks, DateTimeKind.Utc);
					comment = GetNullableColumn<string>(reader, "PageContent_Comment", "");
					content = reader["PageContent_Content"] as string;
					description = GetNullableColumn<string>(reader, "PageContent_Description", null);

					if(!IsDBNull(reader, "PageKeyword_Keyword")) {
						keywords.Add(reader["PageKeyword_Keyword"] as string);
					}

					result.Add(new PageContent(NameTools.GetFullName(nspaceName, name), this, creationDateTime, title, user, dateTime, comment, content, keywords.ToArray(), description));
				}
				
				CloseReader(reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Gets all the Pages in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>All the Pages in the namespace. The array is not sorted.</returns>
		public PageContent[] GetPages(NamespaceInfo nspace) {
			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			PageContent[] pages = GetPages(connection, nspace);
			CloseConnection(connection);

			return pages;
		}

		/// <summary>
		/// Gets all the pages in a namespace that are bound to zero categories.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The pages, sorted by name.</returns>
		public PageContent[] GetUncategorizedPages(NamespaceInfo nspace) {
			string nspaceName = nspace != null ? nspace.Name : "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("PageContent", "CategoryBinding", new string[] { "Wiki", "Namespace", "Name" }, new string[] { "Wiki", "Namespace", "Page" }, Join.LeftJoin, new string[] { "Namespace", "Name", "CreationDateTime", "Title", "User", "LastModified", "Comment", "Content", "Description" }, new string[] { "Page" }, "PageKeyword", new string[] { "Wiki", "Namespace", "Page" }, Join.LeftJoin, new string[] { "Keyword" });
			query = queryBuilder.Where(query, "PageContent", "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "CategoryBinding", "Category", WhereOperator.IsNull, null);
			query = queryBuilder.AndWhere(query, "PageContent", "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.OrderBy(query, new[] { "Name" }, new[] { Ordering.Asc });

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspaceName));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<PageContent> result = new List<PageContent>(100);

				while(reader.Read()) {
					result.Add(new PageContent(NameTools.GetFullName(reader["PageContent_Namespace"] as string, reader["PageContent_Name"] as string),
						this, new DateTime(((DateTime)reader["PageContent_CreationDateTime"]).Ticks, DateTimeKind.Utc), reader["PageContent_Title"] as string, reader["PageContent_User"] as string, new DateTime(((DateTime)reader["PageContent_LastModified"]).Ticks, DateTimeKind.Utc), 
						GetNullableColumn<string>(reader, "PageContent_Comment", ""), reader["PageContent_Content"] as string, new string[0], 
						GetNullableColumn<string>(reader, "PageContent_Description", null)));
				}

				CloseReader(command, reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Gets the content of a specific revision of a page.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="fullName">The page full name.</param>
		/// <param name="revision">The revision.</param>
		/// <returns>The content.</returns>
		private PageContent GetPage(DbConnection connection, string fullName, int revision) {
			// Internal version to work with GetContent, GetBackupContent, GetDraft

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string name, nspace;
			NameTools.ExpandFullName(fullName, out nspace, out name);
			if(nspace == null) nspace = "";

			string query = queryBuilder.SelectFrom("PageContent", "PageKeyword", new string[] { "Wiki", "Name", "Namespace", "Revision" }, new string[] { "Wiki", "Page", "Namespace", "Revision" }, Join.LeftJoin,
				new string[] { "Title", "CreationDateTime", "User", "LastModified", "Comment", "Content", "Description" }, new string[] { "Keyword" });
			query = queryBuilder.Where(query, "PageContent", "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "PageContent", "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "PageContent", "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.AndWhere(query, "PageContent", "Revision", WhereOperator.Equals, "Revision");

			List<Parameter> parameters = new List<Parameter>(4);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
			parameters.Add(new Parameter(ParameterType.Int16, "Revision", (short)revision));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				PageContent result = null;

				string title = null, user = null, comment = null, content = null, description = null;
				DateTime dateTime = DateTime.MinValue, creationDateTime = DateTime.MinValue;
				List<string> keywords = new List<string>(10);

				while(reader.Read()) {
					if(title == null) {
						creationDateTime = new DateTime(((DateTime)reader["PageContent_CreationDateTime"]).Ticks, DateTimeKind.Utc);
						title = reader["PageContent_Title"] as string;
						user = reader["PageContent_User"] as string;
						dateTime = new DateTime(((DateTime)reader["PageContent_LastModified"]).Ticks, DateTimeKind.Utc);
						comment = GetNullableColumn<string>(reader, "PageContent_Comment", "");
						content = reader["PageContent_Content"] as string;
						description = GetNullableColumn<string>(reader, "PageContent_Description", null);
					}

					if(!IsDBNull(reader, "PageKeyword_Keyword")) {
						keywords.Add(reader["PageKeyword_Keyword"] as string);
					}
				}

				if(title != null) {
					result = new PageContent(fullName, this, creationDateTime, title, user, dateTime, comment, content, keywords.ToArray(), description);
				}

				CloseReader(reader);

				return result;
			}
			else return null;
		}

		/// <summary>
		/// Gets the content of a specific revision of a page.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="fullName">The page full name.</param>
		/// <param name="revision">The revision.</param>
		/// <returns>The content.</returns>
		private PageContent GetPage(DbTransaction transaction, string fullName, int revision) {
			// Internal version to work with GetContent, GetBackupContent, GetDraft

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string name, nspace;
			NameTools.ExpandFullName(fullName, out nspace, out name);
			if(nspace == null) nspace = "";

			string query = queryBuilder.SelectFrom("PageContent", "PageKeyword", new string[] { "Wiki", "Name", "Namespace", "Revision" }, new string[] { "Wiki", "Page", "Namespace", "Revision" }, Join.LeftJoin,
				new string[] { "Title", "CreationDateTime", "User", "LastModified", "Comment", "Content", "Description" }, new string[] { "Keyword" });
			query = queryBuilder.Where(query, "PageContent", "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "PageContent", "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "PageContent", "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.AndWhere(query, "PageContent", "Revision", WhereOperator.Equals, "Revision");

			List<Parameter> parameters = new List<Parameter>(4);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
			parameters.Add(new Parameter(ParameterType.Int16, "Revision", (short)revision));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				PageContent result = null;

				string title = null, user = null, comment = null, content = null, description = null;
				DateTime dateTime = DateTime.MinValue, creationDateTime = DateTime.MinValue;
				List<string> keywords = new List<string>(10);

				while(reader.Read()) {
					if(title == null) {
						creationDateTime = new DateTime(((DateTime)reader["PageContent_CreationDateTime"]).Ticks, DateTimeKind.Utc);
						title = reader["PageContent_Title"] as string;
						user = reader["PageContent_User"] as string;
						dateTime = new DateTime(((DateTime)reader["PageContent_LastModified"]).Ticks, DateTimeKind.Utc);
						comment = GetNullableColumn<string>(reader, "PageContent_Comment", "");
						content = reader["PageContent_Content"] as string;
						description = GetNullableColumn<string>(reader, "PageContent_Description", null);
					}

					if(!IsDBNull(reader, "PageKeyword_Keyword")) {
						keywords.Add(reader["PageKeyword_Keyword"] as string);
					}
				}

				if(title != null) {
					result = new PageContent(fullName, this, creationDateTime, title, user, dateTime, comment, content, keywords.ToArray(), description);
				}

				CloseReader(reader);

				return result;
			}
			else return null;
		}

		/// <summary>
		/// Gets the Content of a Page.
		/// </summary>
		/// <param name="fullName">The Page.</param>
		/// <returns>The Page Content object, <c>null</c> if the page does not exist or <paramref name="fullName"/> is <c>null</c>,
		/// or an empty instance if the content could not be retrieved (<seealso cref="PageContent.GetEmpty"/>).</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		public PageContent GetPage(string fullName) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("fullName");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			PageContent content = GetPage(connection, fullName, CurrentRevision);
			CloseConnection(connection);

			return content;
		}

		/// <summary>
		/// Gets the content of a draft of a Page.
		/// </summary>
		/// <param name="fullName">The full name of the page.</param>
		/// <returns>The draft, or <c>null</c> if no draft exists.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		public PageContent GetDraft(string fullName) {
			if(fullName == null) throw new ArgumentNullException("page");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			PageContent content = GetPage(connection, fullName, DraftRevision);
			CloseConnection(connection);

			return content;
		}

		/// <summary>
		/// Deletes a draft of a Page.
		/// </summary>
		/// <param name="fullName">The full name of the page.</param>
		/// <returns><c>true</c> if the draft is deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		public bool DeleteDraft(string fullName) {
			if(fullName == null) throw new ArgumentNullException("page");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			bool deleted = DeleteContent(connection, fullName, DraftRevision);
			CloseConnection(connection);

			return deleted;
		}

		/// <summary>
		/// Gets the Backup/Revision numbers of a Page.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="fullName">The full name of the page to get the Backups of.</param>
		/// <returns>The Backup/Revision numbers.</returns>
		private int[] GetBackups(DbTransaction transaction, string fullName) {
			if(GetPage(transaction, fullName, CurrentRevision) == null) {
				return null;
			}

			string name, nspace;
			NameTools.ExpandFullName(fullName, out nspace, out name);
			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("PageContent", new string[] { "Revision" });
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.AndWhere(query, "Revision", WhereOperator.GreaterThanOrEqualTo, "Revision");
			query = queryBuilder.OrderBy(query, new[] { "Revision" }, new[] { Ordering.Asc });

			List<Parameter> parameters = new List<Parameter>(4);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
			parameters.Add(new Parameter(ParameterType.Int16, "Revision", FirstRevision));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<int> result = new List<int>(100);

				while(reader.Read()) {
					result.Add((short)reader["Revision"]);
				}

				CloseReader(reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Gets the Backup/Revision numbers of a Page.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="fullName">The full name of the page to get the Backups of.</param>
		/// <returns>The Backup/Revision numbers.</returns>
		private int[] GetBackups(DbConnection connection, string fullName) {
			if(GetPage(connection, fullName, CurrentRevision) == null) {
				return null;
			}

			string name, nspace;
			NameTools.ExpandFullName(fullName, out nspace, out name);
			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("PageContent", new string[] { "Revision" });
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.AndWhere(query, "Revision", WhereOperator.GreaterThanOrEqualTo, "Revision");
			query = queryBuilder.OrderBy(query, new[] { "Revision" }, new[] { Ordering.Asc });

			List<Parameter> parameters = new List<Parameter>(4);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
			parameters.Add(new Parameter(ParameterType.Int16, "Revision", FirstRevision));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<int> result = new List<int>(100);

				while(reader.Read()) {
					result.Add((short)reader["Revision"]);
				}

				CloseReader(reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Gets the Backup/Revision numbers of a Page.
		/// </summary>
		/// <param name="fullName">The full name of the page to get the Backups of.</param>
		/// <returns>The Backup/Revision numbers.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		public int[] GetBackups(string fullName) {
			if(fullName == null) throw new ArgumentNullException("page");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			int[] revisions = GetBackups(connection, fullName);
			CloseConnection(connection);

			return revisions;
		}

		/// <summary>
		/// Gets the Content of a Backup of a Page.
		/// </summary>
		/// <param name="fullName">The full name of the page to get the backup of.</param>
		/// <param name="revision">The Backup/Revision number.</param>
		/// <returns>The Page Backup.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="revision"/> is less than zero.</exception>
		public PageContent GetBackupContent(string fullName, int revision) {
			if(fullName == null) throw new ArgumentNullException("page");
			if(revision < 0) throw new ArgumentOutOfRangeException("revision", "Invalid Revision");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			PageContent content = GetPage(connection, fullName, revision);
			CloseConnection(connection);

			return content;
		}

		/// <summary>
		/// Stores the content for a revision.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="content">The content.</param>
		/// <param name="revision">The revision.</param>
		/// <returns><c>true</c> if the content is stored, <c>false</c> otherwise.</returns>
		private bool SetContent(DbTransaction transaction, PageContent content, int revision) {
			string name, nspace;
			NameTools.ExpandFullName(content.FullName, out nspace, out name);
			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.InsertInto("PageContent",
				new string[] { "Wiki", "Name", "Namespace", "Revision", "CreationDateTime", "Title", "User", "LastModified", "Comment", "Content", "Description" },
				new string[] { "Wiki", "Name", "Namespace", "Revision", "CreationDateTime", "Title", "User", "LastModified", "Comment", "Content", "Description" });

			List<Parameter> parameters = new List<Parameter>(10);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
			parameters.Add(new Parameter(ParameterType.Int16, "Revision", revision));
			parameters.Add(new Parameter(ParameterType.DateTime, "CreationDateTime", content.CreationDateTime));
			parameters.Add(new Parameter(ParameterType.String, "Title", content.Title));
			parameters.Add(new Parameter(ParameterType.String, "User", content.User));
			parameters.Add(new Parameter(ParameterType.DateTime, "LastModified", content.LastModified));
			if(!string.IsNullOrEmpty(content.Comment)) parameters.Add(new Parameter(ParameterType.String, "Comment", content.Comment));
			else parameters.Add(new Parameter(ParameterType.String, "Comment", DBNull.Value));
			parameters.Add(new Parameter(ParameterType.String, "Content", content.Content));
			if(!string.IsNullOrEmpty(content.Description)) parameters.Add(new Parameter(ParameterType.String, "Description", content.Description));
			else parameters.Add(new Parameter(ParameterType.String, "Description", DBNull.Value));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows != 1) return false;

			if(content.Keywords.Length > 0) {
				rows = 0;
				foreach(string kw in content.Keywords) {
					query = queryBuilder.InsertInto("PageKeyword", new string[] { "Wiki", "Page", "Namespace", "Revision", "Keyword" },
						new string[] { "Wiki", "Page", "Namespace", "Revision", "Keyword" });

					parameters = new List<Parameter>(5);
					parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
					parameters.Add(new Parameter(ParameterType.String, "Page", name));
					parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
					parameters.Add(new Parameter(ParameterType.Int16, "Revision", revision));
					parameters.Add(new Parameter(ParameterType.String, "Keyword", kw));

					command = builder.GetCommand(transaction, query, parameters);

					rows += ExecuteNonQuery(command, false);

				}
				return rows == content.Keywords.Length;
			}
			else return true;
		}

		/// <summary>
		/// Deletes a revision of a page content.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="pageFullName">The page full name.</param>
		/// <param name="revision">The revision.</param>
		/// <returns><c>true</c> if the content ir deleted, <c>false</c> otherwise.</returns>
		private bool DeleteContent(DbTransaction transaction, string pageFullName, int revision) {
			string name, nspace;
			NameTools.ExpandFullName(pageFullName, out nspace, out name);
			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("PageContent");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.AndWhere(query, "Revision", WhereOperator.Equals, "Revision");

			List<Parameter> parameters = new List<Parameter>(4);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
			parameters.Add(new Parameter(ParameterType.String, "Revision", revision));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			return rows > 0;
		}

		/// <summary>
		/// Deletes a revision of a page content.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="pageFullName">The page full name.</param>
		/// <param name="revision">The revision.</param>
		/// <returns><c>true</c> if the content ir deleted, <c>false</c> otherwise.</returns>
		private bool DeleteContent(DbConnection connection, string pageFullName, int revision) {
			string name, nspace;
			NameTools.ExpandFullName(pageFullName, out nspace, out name);
			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("PageContent");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.AndWhere(query, "Revision", WhereOperator.Equals, "Revision");

			List<Parameter> parameters = new List<Parameter>(4);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
			parameters.Add(new Parameter(ParameterType.String, "Revision", revision));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			return rows > 0;
		}

		/// <summary>
		/// Forces to overwrite or create a Backup.
		/// </summary>
		/// <param name="content">The Backup content.</param>
		/// <param name="revision">The revision.</param>
		/// <returns>True if the Backup has been created successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="content"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="revision"/> is less than zero.</exception>
		public bool SetBackupContent(PageContent content, int revision) {
			if(content == null) throw new ArgumentNullException("content");
			if(revision < 0) throw new ArgumentOutOfRangeException("revision", "Invalid Revision");

			// 1. DeletebBackup, if any
			// 2. Set new content

			if(GetPage(content.FullName) == null) return false;

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);
			
			DeleteContent(transaction, content.FullName, revision);

			bool set = SetContent(transaction, content, revision);

			if(set) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return set;
		}

		/// <summary>
		/// Renames a Page.
		/// </summary>
		/// <param name="fullName">The full name of the page to rename.</param>
		/// <param name="newName">The new Name.</param>
		/// <returns>The correct <see cref="T:PageContent"/> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> or <paramref name="newName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> or <paramref name="newName"/> are empty.</exception>
		public PageContent RenamePage(string fullName, string newName) {
			if(fullName == null) throw new ArgumentNullException("page");
			if(newName == null) throw new ArgumentNullException("newName");
			if(newName.Length == 0) throw new ArgumentException("New Name cannot be empty", "newName");

			// Check
			// 1. Page is default page of its namespace
			// 2. New name already exists

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			PageContent currentPage = GetPage(transaction, fullName, CurrentRevision);

			if(currentPage == null) {
				RollbackTransaction(transaction);
				return null;
			}
			if(IsDefaultPage(transaction, fullName)) {
				RollbackTransaction(transaction);
				return null;
			}
			if(GetPage(transaction, NameTools.GetFullName(NameTools.GetNamespace(fullName), newName), CurrentRevision) != null) {
				RollbackTransaction(transaction);
				return null;
			}

			string nspace, name;
			NameTools.ExpandFullName(fullName, out nspace, out name);
			if(nspace == null) nspace = "";

			CategoryInfo[] currCategories = GetCategories(transaction, nspace == "" ? null : GetNamespace(transaction, nspace));
			string lowerPageName = fullName.ToLowerInvariant();
			List<string> pageCategories = new List<string>(10);
			foreach(CategoryInfo cat in currCategories) {
				if(Array.Find(cat.Pages, (s) => { return s.ToLowerInvariant() == lowerPageName; }) != null) {
					pageCategories.Add(NameTools.GetLocalName(cat.FullName));
				}
			}

			RebindPage(transaction, fullName, new string[0]);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.Update("PageContent", new string[] { "Name" }, new string[] { "NewName" });
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "OldName");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");

			List<Parameter> parameters = new List<Parameter>(4);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "NewName", newName));
			parameters.Add(new Parameter(ParameterType.String, "OldName", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows > 0) {
				PageContent result = currentPage;
				result.FullName = NameTools.GetFullName(nspace, newName);

				RebindPage(transaction, NameTools.GetFullName(nspace, newName), pageCategories.ToArray());

				query = queryBuilder.Update("Message", new string[] { "Page" }, new string[] { "NewName" });
				query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
				query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");
				query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");

				parameters = new List<Parameter>(4);
				parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
				parameters.Add(new Parameter(ParameterType.String, "NewName", newName));
				parameters.Add(new Parameter(ParameterType.String, "Page", name));
				parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));

				command = builder.GetCommand(transaction, query, parameters);

				rows = ExecuteNonQuery(command, false);

				query = queryBuilder.Update("NavigationPath", new string[] { "Page" }, new string[] { "NewName" });
				query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
				query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");
				query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");

				parameters = new List<Parameter>(4);
				parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
				parameters.Add(new Parameter(ParameterType.String, "NewName", newName));
				parameters.Add(new Parameter(ParameterType.String, "Page", name));
				parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));

				command = builder.GetCommand(transaction, query, parameters);

				rows = ExecuteNonQuery(command, false);

				CommitTransaction(transaction);

				return result;
			}
			else {
				RollbackTransaction(transaction);
				return null;
			}
		}

		/// <summary>
		/// Adds a new page content.
		/// </summary>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="pageName">The Page Name.</param>
		/// <param name="creationDateTime">The creation Date/Time.</param>
		/// <param name="title">The Title of the Page.</param>
		/// <param name="username">The Username.</param>
		/// <param name="dateTime">The Date/Time.</param>
		/// <param name="comment">The Comment of the editor, about this revision.</param>
		/// <param name="content">The Page Content.</param>
		/// <param name="keywords">The keywords, usually used for SEO.</param>
		/// <param name="description">The description, usually used for SEO.</param>
		/// <param name="saveMode">The save mode for this modification.</param>
		/// <returns>The correct PageInfo object or null.</returns>
		/// <remarks>This method should <b>not</b> create the content of the Page.</remarks>
		/// <exception cref="ArgumentNullException">If <paramref name="pageName"/>, <paramref name="title"/> <paramref name="username"/> or <paramref name="content"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageName"/>, <paramref name="title"/> or <paramref name="username"/> are empty.</exception>
		public PageContent SetPageContent(string nspace, string pageName, DateTime creationDateTime, string title, string username, DateTime dateTime, string comment, string content,
			string[] keywords, string description, SaveMode saveMode) {
			if(pageName == null) throw new ArgumentNullException("name");
			if(pageName.Length == 0) throw new ArgumentException("Name cannot be empty", "name");
			if(title == null) throw new ArgumentNullException("title");
			if(title.Length == 0) throw new ArgumentException("Title cannot be empty", "title");
			if(username == null) throw new ArgumentNullException("username");
			if(username.Length == 0) throw new ArgumentException("Username cannot be empty", "username");
			if(content == null) throw new ArgumentNullException("content"); // content can be empty

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			string pageFullName = NameTools.GetFullName(nspace, pageName);

			PageContent currentContent = GetPage(transaction, pageFullName, CurrentRevision);

			PageContent pageContent = new PageContent(pageFullName, this, creationDateTime, title, username, dateTime, comment, content, keywords != null ? keywords : new string[0], description);

			switch(saveMode) {
				case SaveMode.Backup:
					// Do backup (if there is something to backup), delete current version (if any), store new version
					Backup(transaction, currentContent);
					DeleteContent(transaction, pageFullName, CurrentRevision);
					bool done1 = SetContent(transaction, pageContent, CurrentRevision);

					if(done1) CommitTransaction(transaction);
					else RollbackTransaction(transaction);

					return pageContent;
				case SaveMode.Normal:
					// Delete current version (if any), store new version
					DeleteContent(transaction, pageFullName, CurrentRevision);
					bool done2 = SetContent(transaction, pageContent, CurrentRevision);

					if(done2) CommitTransaction(transaction);
					else RollbackTransaction(transaction);

					return pageContent;
				case SaveMode.Draft:
					// Delete current draft (if any), store new draft
					DeleteContent(transaction, pageFullName, DraftRevision);
					bool done3 = SetContent(transaction, pageContent, DraftRevision);

					if(done3) CommitTransaction(transaction);
					else RollbackTransaction(transaction);

					return pageContent;
				default:
					RollbackTransaction(transaction);
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Backs up the content of a page.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="page">The page.</param>
		/// <returns><c>true</c> if the backup is performed, <c>false</c> otherwise.</returns>
		private bool Backup(DbTransaction transaction, PageContent page) {
			// Insert a new revision
			int[] backups = GetBackups(transaction, page.FullName);
			if(backups == null) return false;

			int revision = backups.Length > 0 ? backups[backups.Length - 1] + 1 : FirstRevision;
			bool set = SetContent(transaction, page, revision);

			return set;
		}

		/// <summary>
		/// Performs the rollback of a Page to a specified revision.
		/// </summary>
		/// <param name="pageFullName">The full name of the page to rollback.</param>
		/// <param name="revision">The Revision to rollback the Page to.</param>
		/// <returns><c>true</c> if the rollback succeeded, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="revision"/> is less than zero.</exception>
		public bool RollbackPage(string pageFullName, int revision) {
			if(pageFullName == null) throw new ArgumentNullException("page");
			if(revision < 0) throw new ArgumentOutOfRangeException("revision", "Invalid Revision");

			// 1. Load specific revision's content
			// 2. Modify page with loaded content, performing backup

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			PageContent targetContent = GetPage(transaction, pageFullName, revision);

			if(targetContent == null) {
				RollbackTransaction(transaction);
				return false;
			}

			PageContent currentContent = GetPage(transaction, pageFullName, CurrentRevision);

			bool done = Backup(transaction, currentContent);
			if(!done) {
				RollbackTransaction(transaction);
				return false;
			}

			done = DeleteContent(transaction, pageFullName, CurrentRevision);
			if(!done) {
				RollbackTransaction(transaction);
				return false;
			}

			done = SetContent(transaction, targetContent, CurrentRevision);
			if(!done) {
				RollbackTransaction(transaction);
				return false;
			}

			CommitTransaction(transaction);

			return true;
		}

		/// <summary>
		/// Deletes the Backups of a Page, up to a specified revision.
		/// </summary>
		/// <param name="pageFullName">The full name of the page to delete the backups of.</param>
		/// <param name="revision">The newest revision to delete (newer revision are kept) or -1 to delete all the Backups.</param>
		/// <returns><c>true</c> if the deletion succeeded, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="revision"/> is less than -1.</exception>
		public bool DeleteBackups(string pageFullName, int revision) {
			if(pageFullName == null) throw new ArgumentNullException("page");
			if(revision < -1) throw new ArgumentOutOfRangeException("revision", "Invalid Revision");

			// 1. Retrieve target content (revision-1 = first kept revision)
			// 2. Replace the current content (delete, store)
			// 3. Delete all older revisions up to the specified on (included) "N-m...N"
			// 4. Re-number remaining revisions starting from FirstRevision (zero) to revision-1 (don't re-number revs -1, -100)

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			if(GetPage(transaction, pageFullName, CurrentRevision) == null) {
				RollbackTransaction(transaction);
				return false;
			}

			int[] baks = GetBackups(transaction, pageFullName);
			if(baks.Length > 0 && revision > baks[baks.Length - 1]) {
				RollbackTransaction(transaction);
				return true;
			}

			string nspace, name;
			NameTools.ExpandFullName(pageFullName, out nspace, out name);
			if(nspace == null) nspace = "";

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("PageContent");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");
			if(revision != -1) query = queryBuilder.AndWhere(query, "Revision", WhereOperator.LessThanOrEqualTo, "Revision");
			query = queryBuilder.AndWhere(query, "Revision", WhereOperator.GreaterThanOrEqualTo, "FirstRevision");

			List<Parameter> parameters = new List<Parameter>(5);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
			if(revision != -1) parameters.Add(new Parameter(ParameterType.Int16, "Revision", revision));
			parameters.Add(new Parameter(ParameterType.Int16, "FirstRevision", FirstRevision));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows == -1) {
				RollbackTransaction(transaction);
				return false;
			}

			if(revision != -1) {
				int revisionDelta = revision + 1;

				query = queryBuilder.UpdateIncrement("PageContent", "Revision", -revisionDelta);
				query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
				query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");
				query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");
				query = queryBuilder.AndWhere(query, "Revision", WhereOperator.GreaterThanOrEqualTo, "FirstRevision");

				parameters = new List<Parameter>(4);
				parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
				parameters.Add(new Parameter(ParameterType.String, "Name", name));
				parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
				parameters.Add(new Parameter(ParameterType.Int16, "FirstRevision", FirstRevision));

				command = builder.GetCommand(transaction, query, parameters);

				rows = ExecuteNonQuery(command, false);

				if(rows > 0) CommitTransaction(transaction);
				else RollbackTransaction(transaction);

				return rows >= 0;
			}
			else {
				CommitTransaction(transaction);
				return true;
			}
		}

		/// <summary>
		/// Removes a Page.
		/// </summary>
		/// <param name="pageFullName">The full name of the page to remove.</param>
		/// <returns>True if the Page is removed successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		public bool RemovePage(string pageFullName) {
			if(pageFullName == null) throw new ArgumentNullException("page");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			if(IsDefaultPage(transaction, pageFullName)) {
				RollbackTransaction(transaction);
				return false;
			}

			PageContent currentContent = GetPage(transaction, pageFullName, CurrentRevision);
			
			RebindPage(transaction, pageFullName, new string[0]);

			string nspace, name;
			NameTools.ExpandFullName(pageFullName, out nspace, out name);
			if(nspace == null) nspace = "";

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("PageContent");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows > 0) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return rows > 0;
		}

		/// <summary>
		/// Binds a Page with one or more Categories.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="pageFullName">The full name of the page to bind.</param>
		/// <param name="categories">The Categories to bind the Page with.</param>
		/// <returns>True if the binding succeeded.</returns>
		/// <remarks>After a successful operation, the Page is bound with all and only the categories passed as argument.</remarks>
		private bool RebindPage(DbTransaction transaction, string pageFullName, string[] categories) {
			string nspace, name;
			NameTools.ExpandFullName(pageFullName, out nspace, out name);
			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("CategoryBinding");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Page", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows < 0) return false;

			if(categories.Length > 0) {
				rows = 0;
				foreach(string cat in categories) {
					query = queryBuilder.InsertInto("CategoryBinding", new string[] { "Wiki", "Namespace", "Category", "Page" },
						new string[] { "Wiki", "Namespace", "Category", "Page" });

					parameters = new List<Parameter>(4);
					parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
					parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
					parameters.Add(new Parameter(ParameterType.String, "Category", NameTools.GetLocalName(cat)));
					parameters.Add(new Parameter(ParameterType.String, "Page", name));

					command = builder.GetCommand(transaction, query, parameters);

					rows += ExecuteNonQuery(command, false);
				}
				return rows == categories.Length;
			}
			else return true;
		}

		/// <summary>
		/// Binds a Page with one or more Categories.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="pageFullName">The full name of the page to bind.</param>
		/// <param name="categories">The Categories to bind the Page with.</param>
		/// <returns>True if the binding succeeded.</returns>
		/// <remarks>After a successful operation, the Page is bound with all and only the categories passed as argument.</remarks>
		private bool RebindPage(DbConnection connection, string pageFullName, string[] categories) {
			string nspace, name;
			NameTools.ExpandFullName(pageFullName, out nspace, out name);
			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("CategoryBinding");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Page", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows < 0) return false;

			if(categories.Length > 0) {
				rows  = 0;
				foreach(string cat in categories) {
					query = queryBuilder.InsertInto("CategoryBinding", new string[] { "Wiki", "Namespace", "Category", "Page" },
						new string[] { "Wiki", "Namespace", "Category", "Page" });

					parameters = new List<Parameter>(4);
					parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
					parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
					parameters.Add(new Parameter(ParameterType.String, "Category", NameTools.GetLocalName(cat)));
					parameters.Add(new Parameter(ParameterType.String, "Page", name));

					command = builder.GetCommand(connection, query, parameters);

					rows += ExecuteNonQuery(command, false);
				}
				return rows == categories.Length;
			}
			else return true;
		}

		/// <summary>
		/// Binds a Page with one or more Categories.
		/// </summary>
		/// <param name="pageFullName">The full name of the page to bind.</param>
		/// <param name="categories">The Categories to bind the Page with.</param>
		/// <returns>True if the binding succeeded.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> or <paramref name="categories"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		public bool RebindPage(string pageFullName, string[] categories) {
			if(pageFullName == null) throw new ArgumentNullException("page");
			if(categories == null) throw new ArgumentNullException("categories");

			foreach(string cat in categories) {
				if(cat == null) throw new ArgumentNullException("categories");
				if(cat.Length == 0) throw new ArgumentException("Category item cannot be empty", "categories");
			}

			if(GetPage(pageFullName) == null) return false;

			// 1. Delete old bindings
			// 2. Store new bindings

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			bool rebound = RebindPage(connection, pageFullName, categories);
			CloseConnection(connection);

			return rebound;
		}

		/// <summary>
		/// Gets the Page Messages.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="pageFullName">The Page full name.</param>
		/// <returns>The list of the <b>first-level</b> Messages, containing the replies properly nested, sorted by date/time.</returns>
		private Message[] GetMessages(DbTransaction transaction, string pageFullName) {
			if(GetPage(transaction, pageFullName, CurrentRevision) == null) return null;

			// 1. Load all messages in memory in a dictionary id->message
			// 2. Build tree using ParentID

			string nspace, name;
			NameTools.ExpandFullName(pageFullName, out nspace, out name);
			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("Message", new string[] { "Id", "Parent", "Username", "Subject", "DateTime", "Body" });
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.OrderBy(query, new string[] { "DateTime", "Id" }, new Ordering[] { Ordering.Asc, Ordering.Asc });

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Page", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				Dictionary<short, Message> allMessages = new Dictionary<short, Message>(50);
				List<short> ids = new List<short>(50);
				List<short?> parents = new List<short?>(50);

				while(reader.Read()) {
					Message msg = new Message((short)reader["Id"], reader["Username"] as string, reader["Subject"] as string,
						new DateTime(((DateTime)reader["DateTime"]).Ticks, DateTimeKind.Utc), reader["Body"] as string);

					ids.Add((short)msg.ID);

					// Import from V2: parent = -1, otherwise null
					if(!IsDBNull(reader, "Parent")) {
						short par = (short)reader["Parent"];
						if(par >= 0) parents.Add(par);
						else parents.Add(null);
					}
					else parents.Add(null);

					allMessages.Add((short)msg.ID, msg);
				}

				CloseReader(reader);

				// Add messages to their parents and build the top-level messages list
				List<Message> result = new List<Message>(20);

				for(int i = 0; i < ids.Count; i++) {
					short? currentParent = parents[i];
					short currentId = ids[i];

					if(currentParent.HasValue) {
						List<Message> replies = new List<Message>(allMessages[currentParent.Value].Replies);
						replies.Add(allMessages[currentId]);
						allMessages[currentParent.Value].Replies = replies.ToArray();
					}
					else result.Add(allMessages[currentId]);
				}

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Gets the Page Messages.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="pageFullName">The Page full name.</param>
		/// <returns>The list of the <b>first-level</b> Messages, containing the replies properly nested, sorted by date/time.</returns>
		private Message[] GetMessages(DbConnection connection, string pageFullName) {
			if(GetPage(connection, pageFullName, CurrentRevision) == null) return new Message[0];

			// 1. Load all messages in memory in a dictionary id->message
			// 2. Build tree using ParentID

			string nspace, name;
			NameTools.ExpandFullName(pageFullName, out nspace, out name);
			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("Message", new string[] { "Id", "Parent", "Username", "Subject", "DateTime", "Body" });
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.OrderBy(query, new string[] { "DateTime", "Id" }, new Ordering[] { Ordering.Asc, Ordering.Asc });

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Page", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				Dictionary<short, Message> allMessages = new Dictionary<short, Message>(50);
				List<short> ids = new List<short>(50);
				List<short?> parents = new List<short?>(50);

				while(reader.Read()) {
					Message msg = new Message((short)reader["Id"], reader["Username"] as string, reader["Subject"] as string,
						new DateTime(((DateTime)reader["DateTime"]).Ticks, DateTimeKind.Utc), reader["Body"] as string);

					ids.Add((short)msg.ID);

					// Import from V2: parent = -1, otherwise null
					if(!IsDBNull(reader, "Parent")) {
						short par = (short)reader["Parent"];
						if(par >= 0) parents.Add(par);
						else parents.Add(null);
					}
					else parents.Add(null);

					allMessages.Add((short)msg.ID, msg);
				}

				CloseReader(reader);

				// Add messages to their parents and build the top-level messages list
				List<Message> result = new List<Message>(20);

				for(int i = 0; i < ids.Count; i++) {
					short? currentParent = parents[i];
					short currentId = ids[i];

					if(currentParent.HasValue) {
						List<Message> replies = new List<Message>(allMessages[currentParent.Value].Replies);
						replies.Add(allMessages[currentId]);
						allMessages[currentParent.Value].Replies = replies.ToArray();
					}
					else result.Add(allMessages[currentId]);
				}

				return result.ToArray();
			}
			else return new Message[0];
		}

		/// <summary>
		/// Gets the Page Messages.
		/// </summary>
		/// <param name="pageFullName">The page full name.</param>
		/// <returns>The list of the <b>first-level</b> Messages, containing the replies properly nested, sorted by date/time.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		public Message[] GetMessages(string pageFullName) {
			if(pageFullName == null) throw new ArgumentNullException("page");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			Message[] messages = GetMessages(connection, pageFullName);
			CloseConnection(connection);

			return messages;
		}

		/// <summary>
		/// Gets the total number of Messages in a Page Discussion.
		/// </summary>
		/// <param name="pageFullName">The page full name.</param>
		/// <returns>The number of messages.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		public int GetMessageCount(string pageFullName) {
			if(pageFullName == null) throw new ArgumentNullException("page");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			if(GetPage(connection, pageFullName, CurrentRevision) == null) {
				CloseConnection(connection);
				return 0;
			}

			string nspace, name;
			NameTools.ExpandFullName(pageFullName, out nspace, out name);
			if(nspace == null) nspace = "";

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectCountFrom("Message");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Page", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			int count = ExecuteScalar<int>(command, 0);

			return count;
		}

		/// <summary>
		/// Removes all messages for a page and stores the new messages.
		/// </summary>
		/// <param name="pageFullName">The full name of the page.</param>
		/// <param name="messages">The new messages to store.</param>
		/// <returns><c>true</c> if the messages are stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> or <paramref name="messages"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		public bool BulkStoreMessages(string pageFullName, Message[] messages) {
			if(pageFullName == null) throw new ArgumentNullException("page");
			if(messages == null) throw new ArgumentNullException("messages");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			PageContent page = GetPage(transaction, pageFullName, CurrentRevision);

			if(page == null) {
				RollbackTransaction(transaction);
				return false;
			}

			string nspace, name;
			NameTools.ExpandFullName(pageFullName, out nspace, out name);
			if(nspace == null) nspace = "";

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("Message");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Page", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			ExecuteNonQuery(command, false);

			List<Message> allMessages;
			List<int> parents;

			UnTreeMessages(messages, out allMessages, out parents, -1);

			int rowsDone = 0;

			for(int i = 0; i < allMessages.Count; i++) {

				// Execute the batch in smaller chunks

				Message msg = allMessages[i];
				int parent = parents[i];

				query = queryBuilder.InsertInto("Message", new string[] { "Wiki", "Page", "Namespace", "Id", "Parent", "Username", "Subject", "DateTime", "Body" },
					new string[] { "Wiki", "Page", "Namespace", "Id", "Parent", "Username", "Subject", "DateTime", "Body" });

				parameters = new List<Parameter>(9);
				parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
				parameters.Add(new Parameter(ParameterType.String, "Page", name));
				parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
				parameters.Add(new Parameter(ParameterType.Int16, "Id", (short)msg.ID));
				if(parent != -1) parameters.Add(new Parameter(ParameterType.Int16, "Parent", parent));
				else parameters.Add(new Parameter(ParameterType.Int16, "Parent", DBNull.Value));
				parameters.Add(new Parameter(ParameterType.String, "Username", msg.Username));
				parameters.Add(new Parameter(ParameterType.String, "Subject", msg.Subject));
				parameters.Add(new Parameter(ParameterType.DateTime, "DateTime", msg.DateTime));
				parameters.Add(new Parameter(ParameterType.String, "Body", msg.Body));

				command = builder.GetCommand(transaction, query, parameters);

				rowsDone += ExecuteNonQuery(command, false);
			}

			if(rowsDone == allMessages.Count) {
				CommitTransaction(transaction);
				return true;
			}
			else {
				RollbackTransaction(transaction);
				return false;
			}
		}

		/// <summary>
		/// Deconstructs a tree of messages and converts it into a flat list.
		/// </summary>
		/// <param name="messages">The input tree.</param>
		/// <param name="flatList">The resulting flat message list.</param>
		/// <param name="parent">The list of parent IDs.</param>
		/// <param name="parents">The current parent ID.</param>
		private static void UnTreeMessages(Message[] messages, out List<Message> flatList, out List<int> parents, int parent) {
			flatList = new List<Message>(20);
			parents = new List<int>(20);

			flatList.AddRange(messages);
			for(int i = 0; i < messages.Length; i++) {
				parents.Add(parent);
			}

			foreach(Message msg in messages) {
				List<Message> temp;
				List<int> tempParents;

				UnTreeMessages(msg.Replies, out temp, out tempParents, msg.ID);

				flatList.AddRange(temp);
				parents.AddRange(tempParents);
			}
			
		}

		/// <summary>
		/// Adds a new Message to a Page.
		/// </summary>
		/// <param name="pageFullName">The full name of the page.</param>
		/// <param name="username">The Username.</param>
		/// <param name="subject">The Subject.</param>
		/// <param name="dateTime">The Date/Time.</param>
		/// <param name="body">The Body.</param>
		/// <param name="parent">The Parent Message ID, or -1.</param>
		/// <returns>True if the Message is added successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/>, <paramref name="username"/>, <paramref name="subject"/> or <paramref name="body"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="username"/> or <paramref name="subject"/> or <paramref name="pageFullName"/> are empty.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="parent"/> is less than -1.</exception>
		public int AddMessage(string pageFullName, string username, string subject, DateTime dateTime, string body, int parent) {
			if(pageFullName == null) throw new ArgumentNullException("page");
			if(username == null) throw new ArgumentNullException("username");
			if(username.Length == 0) throw new ArgumentException("Username cannot be empty", "username");
			if(subject == null) throw new ArgumentNullException("subject");
			if(subject.Length == 0) throw new ArgumentException("Subject cannot be empty", "subject");
			if(body == null) throw new ArgumentNullException("body"); // body can be empty
			if(parent < -1) throw new ArgumentOutOfRangeException("parent", "Invalid Parent Message ID");

			string nspace, name;
			NameTools.ExpandFullName(pageFullName, out nspace, out name);
			if(nspace == null) nspace = "";

			if(GetPage(pageFullName) == null) {
				return -1;
			}

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			if(parent != -1 && FindMessage(GetMessages(transaction, pageFullName), parent) == null) {
				RollbackTransaction(transaction);
				return -1;
			}

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			short freeId = -1;

			string query = queryBuilder.SelectFrom("Message", new string[] { "Id" });
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.OrderBy(query, new string[] { "Id" }, new Ordering[] { Ordering.Desc });

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Page", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			freeId = ExecuteScalar<short>(command, -1, false);

			if(freeId == -1) freeId = 0;
			else freeId++;

			query = queryBuilder.InsertInto("Message", new string[] { "Wiki", "Page", "Namespace", "Id", "Parent", "Username", "Subject", "DateTime", "Body" },
				new string[] { "Wiki", "Page", "Namespace", "Id", "Parent", "Username", "Subject", "DateTime", "Body" });

			parameters = new List<Parameter>(10);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Page", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
			parameters.Add(new Parameter(ParameterType.Int16, "Id", freeId));
			if(parent != -1) parameters.Add(new Parameter(ParameterType.Int16, "Parent", parent));
			else parameters.Add(new Parameter(ParameterType.Int16, "Parent", DBNull.Value));
			parameters.Add(new Parameter(ParameterType.String, "Username", username));
			parameters.Add(new Parameter(ParameterType.String, "Subject", subject));
			parameters.Add(new Parameter(ParameterType.DateTime, "DateTime", dateTime));
			parameters.Add(new Parameter(ParameterType.String, "Body", body));

			command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows == 1) {
				CommitTransaction(transaction);
				return freeId;
			}
			else {
				RollbackTransaction(transaction);
				return freeId;
			}
		}

		/// <summary>
		/// Finds a Message in a Message tree.
		/// </summary>
		/// <param name="messages">The Message tree.</param>
		/// <param name="id">The ID of the Message to find.</param>
		/// <returns>The Message or null.</returns>
		/// <remarks>The method is recursive.</remarks>
		private static Message FindMessage(IEnumerable<Message> messages, int id) {
			Message result = null;
			foreach(Message msg in messages) {
				if(msg.ID == id) {
					result = msg;
				}
				if(result == null) {
					result = FindMessage(msg.Replies, id);
				}
				if(result != null) break;
			}
			return result;
		}

		/// <summary>
		/// Finds the anchestor/parent of a Message.
		/// </summary>
		/// <param name="messages">The Messages.</param>
		/// <param name="id">The Message ID.</param>
		/// <returns>The anchestor Message or null.</returns>
		private static Message FindAnchestor(IEnumerable<Message> messages, int id) {
			Message result = null;
			foreach(Message msg in messages) {
				for(int k = 0; k < msg.Replies.Length; k++) {
					if(msg.Replies[k].ID == id) {
						result = msg;
						break;
					}
					if(result == null) {
						result = FindAnchestor(msg.Replies, id);
					}
				}
				if(result != null) break;
			}
			return result;
		}

		/// <summary>
		/// Removes a Message.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="pageFullName">The Page.</param>
		/// <param name="id">The ID of the Message to remove.</param>
		/// <param name="removeReplies">A value specifying whether or not to remove the replies.</param>
		/// <returns>True if the Message is removed successfully.</returns>
		private bool RemoveMessage(DbTransaction transaction, string pageFullName, int id, bool removeReplies) {
			string nspace, name;
			NameTools.ExpandFullName(pageFullName, out nspace, out name);
			if(nspace == null) nspace = "";

			Message[] messages = GetMessages(transaction, pageFullName);
			if(messages == null) return false;
			Message message = FindMessage(messages, id);
			if(message == null) return false;
			Message parent = FindAnchestor(messages, id);
			int parentId = parent != null ? parent.ID : -1;

			if(removeReplies) {
				// Recursively remove all replies BEFORE removing parent (depth-first)
				foreach(Message reply in message.Replies) {
					if(!RemoveMessage(transaction, pageFullName, reply.ID, true)) return false;
				}
			}

			// Remove this message
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("Message");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.AndWhere(query, "Id", WhereOperator.Equals, "Id");

			List<Parameter> parameters = new List<Parameter>(4);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Page", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
			parameters.Add(new Parameter(ParameterType.Int16, "Id", (short)id));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(!removeReplies && rows == 1) {
				// Update replies' parent id

				query = queryBuilder.Update("Message", new string[] { "Parent" }, new string[] { "NewParent" });
				query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
				query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");
				query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");
				query = queryBuilder.AndWhere(query, "Parent", WhereOperator.Equals, "OldParent");

				parameters = new List<Parameter>(5);
				parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));;
				if(parentId != -1) parameters.Add(new Parameter(ParameterType.Int16, "NewParent", parentId));
				else parameters.Add(new Parameter(ParameterType.Int16, "NewParent", DBNull.Value));
				parameters.Add(new Parameter(ParameterType.String, "Page", name));
				parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
				parameters.Add(new Parameter(ParameterType.Int16, "OldParent", (short)id));

				command = builder.GetCommand(transaction, query, parameters);

				rows = ExecuteNonQuery(command, false);
			}

			return rows > 0;
		}

		/// <summary>
		/// Removes a Message.
		/// </summary>
		/// <param name="pageFullName">The full name of the page.</param>
		/// <param name="id">The ID of the Message to remove.</param>
		/// <param name="removeReplies">A value specifying whether or not to remove the replies.</param>
		/// <returns>True if the Message is removed successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="id"/> is less than zero.</exception>
		public bool RemoveMessage(string pageFullName, int id, bool removeReplies) {
			if(pageFullName == null) throw new ArgumentNullException("page");
			if(id < 0) throw new ArgumentOutOfRangeException("id", "Invalid ID");

			// 1. If removeReplies, recursively delete all messages with parent == id
			//    Else remove current message, updating all replies' parent id (set to this message's parent or to NULL)
			// 2. If removeReplies, unindex the whole message tree
			//    Else unindex only this message

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			bool done = RemoveMessage(transaction, pageFullName, id, removeReplies);

			if(done) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return done;
		}

		/// <summary>
		/// Modifies a Message.
		/// </summary>
		/// <param name="pageFullName">The Page.</param>
		/// <param name="id">The ID of the Message to modify.</param>
		/// <param name="username">The Username.</param>
		/// <param name="subject">The Subject.</param>
		/// <param name="dateTime">The Date/Time.</param>
		/// <param name="body">The Body.</param>
		/// <returns>True if the Message is modified successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/>, <paramref name="username"/>, <paramref name="subject"/> or <paramref name="body"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="id"/> is less than zero.</exception>
		/// <exception cref="ArgumentException">If <paramref name="username"/> or <paramref name="subject"/> or <paramref name="pageFullName"/> are empty.</exception>
		public bool ModifyMessage(string pageFullName, int id, string username, string subject, DateTime dateTime, string body) {
			if(pageFullName == null) throw new ArgumentNullException("page");
			if(id < 0) throw new ArgumentOutOfRangeException("id", "Invalid Message ID");
			if(username == null) throw new ArgumentNullException("username");
			if(username.Length == 0) throw new ArgumentException("Username cannot be empty", "username");
			if(subject == null) throw new ArgumentNullException("subject");
			if(subject.Length == 0) throw new ArgumentException("Subject cannot be empty", "subject");
			if(body == null) throw new ArgumentNullException("body"); // body can be empty

			string nspace, name;
			NameTools.ExpandFullName(pageFullName, out nspace, out name);
			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			Message[] messages = GetMessages(transaction, pageFullName);
			if(messages == null) {
				RollbackTransaction(transaction);
				return false;
			}
			Message oldMessage = FindMessage(messages, id);

			if(oldMessage == null) {
				RollbackTransaction(transaction);
				return false;
			}

			PageContent page = GetPage(transaction, pageFullName, CurrentRevision);

			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.Update("Message", new string[] { "Username", "Subject", "DateTime", "Body" }, new string[] { "Username", "Subject", "DateTime", "Body" });
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Page", WhereOperator.Equals, "Page");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.AndWhere(query, "Id", WhereOperator.Equals, "Id");

			List<Parameter> parameters = new List<Parameter>(8);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Username", username));
			parameters.Add(new Parameter(ParameterType.String, "Subject", subject));
			parameters.Add(new Parameter(ParameterType.DateTime, "DateTime", dateTime));
			parameters.Add(new Parameter(ParameterType.String, "Body", body));
			parameters.Add(new Parameter(ParameterType.String, "Page", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
			parameters.Add(new Parameter(ParameterType.Int16, "Id", id));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows == 1) {
				CommitTransaction(transaction);
				return true;
			}
			else {
				RollbackTransaction(transaction);
				return false;
			}
		}

		/// <summary>
		/// Gets all the Navigation Paths in a Namespace.
		/// </summary>
		/// <param name="nspace">The Namespace.</param>
		/// <returns>All the Navigation Paths, sorted by name.</returns>
		public NavigationPath[] GetNavigationPaths(NamespaceInfo nspace) {
			string nspaceName = nspace != null ? nspace.Name : "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.SelectFrom("NavigationPath", new string[] { "Name", "Namespace", "Page" });
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");
			query = queryBuilder.OrderBy(query, new string[] { "Namespace", "Name", "Number" }, new Ordering[] { Ordering.Asc, Ordering.Asc, Ordering.Asc });

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspaceName));

			DbCommand command = builder.GetCommand(connString, query, parameters);

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<NavigationPath> result = new List<NavigationPath>(10);

				string prevName = "|||";
				string name;
				string actualNamespace = "";
				List<string> pages = new List<string>(10);

				while(reader.Read()) {
					name = reader["Name"] as string;

					if(name != prevName) {
						actualNamespace = reader["Namespace"] as string;

						if(prevName != "|||") {
							result[result.Count - 1].Pages = pages.ToArray();
							pages.Clear();
						}

						result.Add(new NavigationPath(NameTools.GetFullName(actualNamespace, name), this));
					}

					prevName = name;
					pages.Add(NameTools.GetFullName(actualNamespace, reader["Page"] as string));
				}

				if(result.Count > 0) {
					result[result.Count - 1].Pages = pages.ToArray();
				}

				CloseReader(command, reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Adds a new Navigation Path.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Name of the Path.</param>
		/// <param name="pages">The Pages array.</param>
		/// <returns>The correct <see cref="T:NavigationPath"/> object.</returns>
		private NavigationPath AddNavigationPath(DbTransaction transaction, string nspace, string name, string[] pages) {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			int rows = 0;
			int count = 0;
			foreach(string pageFullName in pages) {
				string query = queryBuilder.InsertInto("NavigationPath", new string[] { "Wiki", "Name", "Namespace", "Page", "Number" },
					new string[] { "Wiki", "Name", "Namespace", "Page", "Number" });

				List<Parameter> parameters = new List<Parameter>(5);
				parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
				parameters.Add(new Parameter(ParameterType.String, "Name", name));
				parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));
				parameters.Add(new Parameter(ParameterType.String, "Page", NameTools.GetLocalName(pageFullName)));
				parameters.Add(new Parameter(ParameterType.Int32, "Number", (short)count));

				DbCommand command = builder.GetCommand(transaction, query, parameters);

				rows += ExecuteNonQuery(command, false);
			}
			if(rows == pages.Length) {
				NavigationPath result = new NavigationPath(NameTools.GetFullName(nspace, name), this);
				result.Pages = pages;
				return result;
			}
			else return null;
		}

		/// <summary>
		/// Adds a new Navigation Path.
		/// </summary>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Name of the Path.</param>
		/// <param name="pages">The full name of the pages array.</param>
		/// <returns>The correct <see cref="T:NavigationPath"/> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> or <paramref name="pages"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> or <paramref name="pages"/> are empty.</exception>
		public NavigationPath AddNavigationPath(string nspace, string name, string[] pages) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");
			if(pages == null) throw new ArgumentNullException("pages");
			if(pages.Length == 0) throw new ArgumentException("Pages cannot be empty");

			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			foreach(string pageFullName in pages) {
				if(pageFullName == null) {
					RollbackTransaction(transaction);
					throw new ArgumentNullException("pages");
				}
				if(GetPage(transaction, pageFullName, CurrentRevision) == null) {
					RollbackTransaction(transaction);
					throw new ArgumentException("Page not found", "pages");
				}
			}

			NavigationPath path = AddNavigationPath(transaction, nspace, name, pages);

			if(path != null) CommitTransaction(transaction);
			else RollbackTransaction(transaction);

			return path;
		}

		/// <summary>
		/// Modifies an existing navigation path.
		/// </summary>
		/// <param name="path">The navigation path to modify.</param>
		/// <param name="pages">The new pages full names array.</param>
		/// <returns>The correct <see cref="T:NavigationPath"/> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="path"/> or <paramref name="pages"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pages"/> is empty.</exception>
		public NavigationPath ModifyNavigationPath(NavigationPath path, string[] pages) {
			if(path == null) throw new ArgumentNullException("path");
			if(pages == null) throw new ArgumentNullException("pages");
			if(pages.Length == 0) throw new ArgumentException("Pages cannot be empty");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			foreach(string pageFullName in pages) {
				if(pageFullName == null) {
					RollbackTransaction(transaction);
					throw new ArgumentNullException("pages");
				}
				if(GetPage(transaction, pageFullName, CurrentRevision) == null) {
					RollbackTransaction(transaction);
					throw new ArgumentException("Page not found", "pages");
				}
			}

			if(RemoveNavigationPath(transaction, path)) {
				string nspace, name;
				NameTools.ExpandFullName(path.FullName, out nspace, out name);
				if(nspace == null) nspace = "";

				NavigationPath result = AddNavigationPath(transaction, nspace, name, pages);

				if(result != null) {
					CommitTransaction(transaction);
					return result;
				}
				else {
					RollbackTransaction(transaction);
					return null;
				}
			}
			else {
				RollbackTransaction(transaction);
				return null;
			}
		}

		/// <summary>
		/// Removes a Navigation Path.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="path">The navigation path to remove.</param>
		/// <returns><c>true</c> if the path is removed, <c>false</c> otherwise.</returns>
		private bool RemoveNavigationPath(DbTransaction transaction, NavigationPath path) {
			string nspace, name;
			NameTools.ExpandFullName(path.FullName, out nspace, out name);
			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("NavigationPath");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			return rows > 0;
		}

		/// <summary>
		/// Removes a Navigation Path.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="path">The navigation path to remove.</param>
		/// <returns><c>true</c> if the path is removed, <c>false</c> otherwise.</returns>
		private bool RemoveNavigationPath(DbConnection connection, NavigationPath path) {
			string nspace, name;
			NameTools.ExpandFullName(path.FullName, out nspace, out name);
			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("NavigationPath");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");
			query = queryBuilder.AndWhere(query, "Namespace", WhereOperator.Equals, "Namespace");

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Namespace", nspace));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			return rows > 0;
		}

		/// <summary>
		/// Removes a Navigation Path.
		/// </summary>
		/// <param name="path">The navigation path to remove.</param>
		/// <returns><c>true</c> if the path is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="path"/> is <c>null</c>.</exception>
		public bool RemoveNavigationPath(NavigationPath path) {
			if(path == null) throw new ArgumentNullException("path");

			string nspace, name;
			NameTools.ExpandFullName(path.FullName, out nspace, out name);
			if(nspace == null) nspace = "";

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			bool removed = RemoveNavigationPath(connection, path);
			CloseConnection(connection);

			return removed;
		}

		/// <summary>
		/// Gets all the snippets.
		/// </summary>
		/// <returns>All the snippets, sorted by name.</returns>
		public Snippet[] GetSnippets() {
			ICommandBuilder builder = GetCommandBuilder();

			QueryBuilder queryBuilder = QueryBuilder.NewQuery(builder);
			string query = queryBuilder.SelectFrom("Snippet");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.OrderBy(query, new[] { "Name" }, new[] { Ordering.Asc });

			DbCommand command = builder.GetCommand(connString, query, new List<Parameter>() { new Parameter(ParameterType.String, "Wiki", wiki) });

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<Snippet> result = new List<Snippet>(10);

				while(reader.Read()) {
					result.Add(new Snippet(reader["Name"] as string, reader["Content"] as string, this));
				}

				CloseReader(command, reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Adds a new snippet.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="name">The name of the snippet.</param>
		/// <param name="content">The content of the snippet.</param>
		/// <returns>The correct <see cref="T:Snippet"/> object.</returns>
		private Snippet AddSnippet(DbTransaction transaction, string name, string content) {
			ICommandBuilder builder = GetCommandBuilder();

			string query = QueryBuilder.NewQuery(builder).InsertInto("Snippet", new string[] { "Wiki", "Name", "Content" }, new string[] { "Wiki", "Name", "Content" });

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Content", content));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows == 1) {
				return new Snippet(name, content, this);
			}
			else return null;
		}

		/// <summary>
		/// Adds a new snippet.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="name">The name of the snippet.</param>
		/// <param name="content">The content of the snippet.</param>
		/// <returns>The correct <see cref="T:Snippet"/> object.</returns>
		private Snippet AddSnippet(DbConnection connection, string name, string content) {
			ICommandBuilder builder = GetCommandBuilder();

			string query = QueryBuilder.NewQuery(builder).InsertInto("Snippet", new string[] { "Wiki", "Name", "Content" }, new string[] { "Wiki", "Name", "Content" });

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Content", content));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows == 1) {
				return new Snippet(name, content, this);
			}
			else return null;
		}

		/// <summary>
		/// Adds a new snippet.
		/// </summary>
		/// <param name="name">The name of the snippet.</param>
		/// <param name="content">The content of the snippet.</param>
		/// <returns>The correct <see cref="T:Snippet"/> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> or <paramref name="content"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is empty.</exception>
		public Snippet AddSnippet(string name, string content) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");
			if(content == null) throw new ArgumentNullException("content"); // content can be empty

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			Snippet snippet = AddSnippet(connection, name, content);
			CloseConnection(connection);

			return snippet;
		}

		/// <summary>
		/// Modifies an existing snippet.
		/// </summary>
		/// <param name="name">The name of the snippet to modify.</param>
		/// <param name="content">The content of the snippet.</param>
		/// <returns>The correct <see cref="T:Snippet"/> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> or <paramref name="content"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is empty.</exception>
		public Snippet ModifySnippet(string name, string content) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");
			if(content == null) throw new ArgumentNullException("content"); // content can be empty

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			if(RemoveSnippet(transaction, name)) {
				Snippet result = AddSnippet(transaction, name, content);

				if(result != null) CommitTransaction(transaction);
				else RollbackTransaction(transaction);

				return result;
			}
			else {
				RollbackTransaction(transaction);
				return null;
			}
		}

		/// <summary>
		/// Removes a new Snippet.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="name">The Name of the Snippet to remove.</param>
		/// <returns><c>true</c> if the snippet is removed, <c>false</c> otherwise.</returns>
		private bool RemoveSnippet(DbTransaction transaction, string name) {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("Snippet");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			return rows == 1;
		}

		/// <summary>
		/// Removes a new Snippet.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="name">The Name of the Snippet to remove.</param>
		/// <returns><c>true</c> if the snippet is removed, <c>false</c> otherwise.</returns>
		private bool RemoveSnippet(DbConnection connection, string name) {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("Snippet");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			return rows == 1;
		}

		/// <summary>
		/// Removes a new Snippet.
		/// </summary>
		/// <param name="name">The Name of the Snippet to remove.</param>
		/// <returns><c>true</c> if the snippet is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is empty.</exception>
		public bool RemoveSnippet(string name) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			bool removed = RemoveSnippet(connection, name);
			CloseConnection(connection);

			return removed;
		}

		/// <summary>
		/// Gets all the content templates.
		/// </summary>
		/// <returns>All the content templates, sorted by name.</returns>
		public ContentTemplate[] GetContentTemplates() {
			ICommandBuilder builder = GetCommandBuilder();

			QueryBuilder queryBuilder = QueryBuilder.NewQuery(builder);
			string query = queryBuilder.SelectFrom("ContentTemplate");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.OrderBy(query, new[] { "Name" }, new[] { Ordering.Asc });

			DbCommand command = builder.GetCommand(connString, query, new List<Parameter>() { new Parameter(ParameterType.String, "Wiki", wiki) });

			DbDataReader reader = ExecuteReader(command);

			if(reader != null) {
				List<ContentTemplate> result = new List<ContentTemplate>(10);

				while(reader.Read()) {
					result.Add(new ContentTemplate(reader["Name"] as string, reader["Content"] as string, this));
				}

				CloseReader(command, reader);

				return result.ToArray();
			}
			else return null;
		}

		/// <summary>
		/// Adds a new content template.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="name">The name of template.</param>
		/// <param name="content">The content of the template.</param>
		/// <returns>The correct <see cref="T:ContentTemplate"/> object.</returns>
		private ContentTemplate AddContentTemplate(DbTransaction transaction, string name, string content) {
			ICommandBuilder builder = GetCommandBuilder();

			string query = QueryBuilder.NewQuery(builder).InsertInto("ContentTemplate", new string[] { "Wiki", "Name", "Content" }, new string[] { "Wiki", "Name", "Content" });

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Content", content));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows == 1) {
				return new ContentTemplate(name, content, this);
			}
			else return null;
		}

		/// <summary>
		/// Adds a new content template.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="name">The name of template.</param>
		/// <param name="content">The content of the template.</param>
		/// <returns>The correct <see cref="T:ContentTemplate"/> object.</returns>
		private ContentTemplate AddContentTemplate(DbConnection connection, string name, string content) {
			ICommandBuilder builder = GetCommandBuilder();

			string query = QueryBuilder.NewQuery(builder).InsertInto("ContentTemplate", new string[] { "Wiki", "Name", "Content" }, new string[] { "Wiki", "Name", "Content" });

			List<Parameter> parameters = new List<Parameter>(3);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));
			parameters.Add(new Parameter(ParameterType.String, "Content", content));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			if(rows == 1) {
				return new ContentTemplate(name, content, this);
			}
			else return null;
		}

		/// <summary>
		/// Adds a new content template.
		/// </summary>
		/// <param name="name">The name of template.</param>
		/// <param name="content">The content of the template.</param>
		/// <returns>The correct <see cref="T:ContentTemplate"/> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> or <paramref name="content"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is empty.</exception>
		public ContentTemplate AddContentTemplate(string name, string content) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");
			if(content == null) throw new ArgumentNullException("content"); // content can be empty

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			ContentTemplate template = AddContentTemplate(connection, name, content);
			CloseConnection(connection);

			return template;
		}

		/// <summary>
		/// Modifies an existing content template.
		/// </summary>
		/// <param name="name">The name of the template to modify.</param>
		/// <param name="content">The content of the template.</param>
		/// <returns>The correct <see cref="T:ContentTemplate"/> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> or <paramref name="content"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is empty.</exception>
		public ContentTemplate ModifyContentTemplate(string name, string content) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");
			if(content == null) throw new ArgumentNullException("content"); // content can be empty
			
			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);
			DbTransaction transaction = BeginTransaction(connection);

			if(RemoveContentTemplate(transaction, name)) {
				ContentTemplate template = AddContentTemplate(transaction, name, content);

				if(template != null) CommitTransaction(transaction);
				else RollbackTransaction(transaction);

				return template;
			}
			else {
				RollbackTransaction(transaction);
				return null;
			}
		}

		/// <summary>
		/// Removes a content template.
		/// </summary>
		/// <param name="transaction">A database transaction.</param>
		/// <param name="name">The name of the template to remove.</param>
		/// <returns><c>true</c> if the template is removed, <c>false</c> otherwise.</returns>
		private bool RemoveContentTemplate(DbTransaction transaction, string name) {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("ContentTemplate");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));

			DbCommand command = builder.GetCommand(transaction, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			return rows == 1;
		}

		/// <summary>
		/// Removes a content template.
		/// </summary>
		/// <param name="connection">A database connection.</param>
		/// <param name="name">The name of the template to remove.</param>
		/// <returns><c>true</c> if the template is removed, <c>false</c> otherwise.</returns>
		private bool RemoveContentTemplate(DbConnection connection, string name) {
			ICommandBuilder builder = GetCommandBuilder();
			QueryBuilder queryBuilder = new QueryBuilder(builder);

			string query = queryBuilder.DeleteFrom("ContentTemplate");
			query = queryBuilder.Where(query, "Wiki", WhereOperator.Equals, "Wiki");
			query = queryBuilder.AndWhere(query, "Name", WhereOperator.Equals, "Name");

			List<Parameter> parameters = new List<Parameter>(2);
			parameters.Add(new Parameter(ParameterType.String, "Wiki", wiki));
			parameters.Add(new Parameter(ParameterType.String, "Name", name));

			DbCommand command = builder.GetCommand(connection, query, parameters);

			int rows = ExecuteNonQuery(command, false);

			return rows == 1;
		}

		/// <summary>
		/// Removes a content template.
		/// </summary>
		/// <param name="name">The name of the template to remove.</param>
		/// <returns><c>true</c> if the template is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is empty.</exception>
		public bool RemoveContentTemplate(string name) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("Name cannot be empty", "name");

			ICommandBuilder builder = GetCommandBuilder();
			DbConnection connection = builder.GetConnection(connString);

			bool removed = RemoveContentTemplate(connection, name);
			CloseConnection(connection);

			return removed;
		}

		#endregion

		#region IStorageProvider Members

		/// <summary>
		/// Gets a value specifying whether the provider is read-only, i.e. it can only provide data and not store it.
		/// </summary>
		public bool ReadOnly {
			get { return false; }
		}

		#endregion

	}

}
