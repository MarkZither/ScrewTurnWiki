
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using Microsoft.WindowsAzure.StorageClient;
using ScrewTurn.Wiki.SearchEngine;

namespace ScrewTurn.Wiki.Plugins.AzureStorage {

	/// <summary>
	/// Implements a Pages Storage Provider.
	/// </summary>
	public class AzurePagesStorageProvider :IPagesStorageProviderV40 {

		private IHostV40 _host;
		private string _wiki;

		private TableServiceContext _context;

		private IIndex index;

		private const string CurrentRevision = "CurrentRevision";
		private const string Draft = "Draft";

		#region IPagesStorageProviderV30 Members

		private NamespacesEntity GetNamespacesEntity(string wiki, string namespaceName) {
			var query = (from e in _context.CreateQuery<NamespacesEntity>(NamespacesTable).AsTableServiceQuery()
						 where e.PartitionKey.Equals(_wiki) && e.RowKey.Equals(namespaceName)
						 select e).AsTableServiceQuery();
			return QueryHelper<NamespacesEntity>.FirstOrDefault(query);
		}

		/// <summary>
		/// Gets a namespace.
		/// </summary>
		/// <param name="name">The name of the namespace.</param>
		/// <returns>The <see cref="T:NamespaceInfo"/>, or <c>null</c> if no namespace is found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		public NamespaceInfo GetNamespace(string name) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("name");
			
			try {
				var entity = GetNamespacesEntity(_wiki, name);
				if(entity == null) return null;
				return new NamespaceInfo(entity.RowKey, this, entity.DefaultPageFullName == null ? null : GetPage(entity.DefaultPageFullName));
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets all the sub-namespaces.
		/// </summary>
		/// <returns>The sub-namespaces, sorted by name.</returns>
		public NamespaceInfo[] GetNamespaces() {
			try {
				var query = (from e in _context.CreateQuery<NamespacesEntity>(NamespacesTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki)
							 select e).AsTableServiceQuery();
				var entities = QueryHelper<NamespacesEntity>.All(query);
				if(entities == null) return null;

				List<NamespaceInfo> namespaces = new List<NamespaceInfo>(entities.Count);
				foreach(NamespacesEntity entity in entities) {
					namespaces.Add(new NamespaceInfo(entity.RowKey, this, entity.DefaultPageFullName == null ? null : GetPage(entity.DefaultPageFullName)));
				}
				return namespaces.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
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
			if(name.Length == 0) throw new ArgumentException("name");

			try {
				// if the namespace is already present return null
				if(GetNamespacesEntity(_wiki, name) != null) return null;

				NamespacesEntity namespacesEntity = new NamespacesEntity() {
					PartitionKey = _wiki,
					RowKey = name,
					DefaultPageFullName = null
				};
				_context.AddObject(NamespacesTable, namespacesEntity);
				_context.SaveChangesStandard();
				return new NamespaceInfo(name, this, null);
			}
			catch(Exception ex) {
				throw ex;
			}
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
			if(newName.Length == 0) throw new ArgumentException("newName");

			try {
				var oldNamespaceEntity = GetNamespacesEntity(_wiki, nspace.Name);
				if(oldNamespaceEntity == null) return null;

				if(GetNamespacesEntity(_wiki, newName) != null) return null;

				// Move all pages from old namespace to the new one
				List<PagesInfoEntity> pagesInNamespace = GetPagesInfoEntities(_wiki, nspace.Name);

				// Unindex pages
				foreach(PagesInfoEntity pageInfoEntity in pagesInNamespace) {
					PageInfo page = new PageInfo(pageInfoEntity.RowKey, this, pageInfoEntity.CreationDateTime.ToLocalTime());
					PageContent content = GetContent(page);
					if(content != null) {
						UnindexPage(content);
					}
					Message[] messages = GetMessages(page);
					if(messages != null) {
						foreach(Message msg in messages) {
							UnindexMessageTree(page, msg);
						}
					}
				}
	
				foreach(PagesInfoEntity pageInfoEntity in pagesInNamespace) {
					PagesInfoEntity newPage = new PagesInfoEntity() {
						PartitionKey = pageInfoEntity.PartitionKey,
						RowKey = NameTools.GetFullName(newName, NameTools.GetLocalName(pageInfoEntity.RowKey)),
						CreationDateTime = pageInfoEntity.CreationDateTime,
						Namespace = newName,
						PageId = pageInfoEntity.PageId
					};
					_context.DeleteObject(pageInfoEntity);
					_context.AddObject(PagesInfoTable, newPage);
				}
				_context.SaveChangesStandard();

				// Index pages
				foreach(PagesInfoEntity pageInfoEntity in pagesInNamespace) {
					PageInfo page = new PageInfo(NameTools.GetFullName(newName, NameTools.GetLocalName(pageInfoEntity.RowKey)), this, pageInfoEntity.CreationDateTime.ToLocalTime());
					PageContent content = GetContent(page);
					if(content != null) {
						IndexPage(content);
					}
					Message[] messages = GetMessages(page);
					if(messages != null) {
						foreach(Message msg in messages) {
							IndexMessageTree(page, msg);
						}
					}
				}

				// Invalidate pagesInfoCache
				_pagesInfoCache = null;

				List<CategoriesEntity> categoryEntities = GetCategoriesEntities(_wiki, nspace.Name);
				foreach(CategoriesEntity categoryEntity in categoryEntities) {
					CategoriesEntity newCategoryEntity = new CategoriesEntity() {
						PartitionKey = categoryEntity.PartitionKey,
						RowKey = NameTools.GetFullName(newName, NameTools.GetLocalName(categoryEntity.RowKey)),
						Namespace = newName
					};
					string[] oldNamesCategoryPages = categoryEntity.PagesFullNames != null ? categoryEntity.PagesFullNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries) : new string[0];
					List<string> newNamesCategoryPages = new List<string>(oldNamesCategoryPages.Length);
					foreach(string oldName in oldNamesCategoryPages) {
						newNamesCategoryPages.Add(NameTools.GetFullName(newName, NameTools.GetLocalName(oldName)));
					}
					newCategoryEntity.PagesFullNames = string.Join("|", newNamesCategoryPages);
					_context.AddObject(CategoriesTable, newCategoryEntity);
					_context.DeleteObject(categoryEntity);
				}
				_context.SaveChangesStandard();

				List<NavigationPathEntity> navigationPathEntities = GetNavigationPathEntities(_wiki, nspace.Name);
				foreach(NavigationPathEntity navigationPathEntity in navigationPathEntities) {
					NavigationPathEntity newNavigationPathEntity = new NavigationPathEntity() {
						PartitionKey = navigationPathEntity.PartitionKey,
						RowKey = NameTools.GetFullName(newName, NameTools.GetLocalName(navigationPathEntity.RowKey)),
						Namespace = newName
					};
					string[] oldNamesNavigationPathPages = navigationPathEntity.PagesFullNames != null ? navigationPathEntity.PagesFullNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries) : new string[0];
					List<string> newNamesNavigationPathPages = new List<string>(oldNamesNavigationPathPages.Length);
					foreach(string oldName in oldNamesNavigationPathPages) {
						newNamesNavigationPathPages.Add(NameTools.GetFullName(newName, NameTools.GetLocalName(oldName)));
					}
					newNavigationPathEntity.PagesFullNames = string.Join("|", newNamesNavigationPathPages);
					_context.AddObject(CategoriesTable, newNavigationPathEntity);
					_context.DeleteObject(navigationPathEntity);
				}
				_context.SaveChangesStandard();

				NamespacesEntity newNamespaceEntity = new NamespacesEntity();
				newNamespaceEntity.PartitionKey = _wiki;
				newNamespaceEntity.RowKey = newName;
				newNamespaceEntity.DefaultPageFullName = NameTools.GetFullName(newName, NameTools.GetLocalName(oldNamespaceEntity.DefaultPageFullName));
				_context.AddObject(NamespacesTable, newNamespaceEntity);
				_context.DeleteObject(oldNamespaceEntity);
				_context.SaveChangesStandard();

				return new NamespaceInfo(newName, this, GetPage(newNamespaceEntity.DefaultPageFullName));
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Sets the default page of a namespace.
		/// </summary>
		/// <param name="nspace">The namespace of which to set the default page.</param>
		/// <param name="page">The page to use as default page, or <c>null</c>.</param>
		/// <returns>The correct <see cref="T:NamespaceInfo"/> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="nspace"/> is <c>null</c>.</exception>
		public NamespaceInfo SetNamespaceDefaultPage(NamespaceInfo nspace, PageInfo page) {
			if(nspace == null) throw new ArgumentNullException("nspace");

			try {
				var entity = GetNamespacesEntity(_wiki, nspace.Name);
				// If namespace does not exists return null
				if(entity == null) return null;

				// If the page does not exists return null
				if(page != null && GetPagesInfoEntity(_wiki, page.FullName) == null) return null;

				entity.DefaultPageFullName = page != null ? page.FullName : null;
				_context.UpdateObject(entity);
				_context.SaveChangesStandard();

				nspace.DefaultPage = page;
				return nspace;
			}
			catch(Exception ex) {
				throw ex;
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

			try {
				var nspaceEntity = GetNamespacesEntity(_wiki, nspace.Name);
				if(nspaceEntity == null) return false;

				var pagesInNamespace = GetPages(nspace);
				foreach(var pageInNamespace in pagesInNamespace) {
					RemovePage(pageInNamespace);
				}

				_context.DeleteObject(nspaceEntity);
				_context.SaveChangesStandard();
				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Moves a page from its namespace into another.
		/// </summary>
		/// <param name="page">The page to move.</param>
		/// <param name="destination">The destination namespace (<c>null</c> for the root).</param>
		/// <param name="copyCategories">A value indicating whether to copy the page categories in the destination
		/// namespace, if not already available.</param>
		/// <returns>The correct instance of <see cref="T:PageInfo"/>.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		public PageInfo MovePage(PageInfo page, NamespaceInfo destination, bool copyCategories) {
			if(page == null) throw new ArgumentNullException("page");

			try {
				PagesInfoEntity oldPageInfoEntity = GetPagesInfoEntity(_wiki, page.FullName);
				if(oldPageInfoEntity == null) return null;

				string destinationName = destination != null ? destination.Name : null;

				string currentNsName = NameTools.GetNamespace(page.FullName);
				NamespaceInfo currentNs = currentNsName != null ? GetNamespace(currentNsName) : null;
				NamespaceComparer nsComp = new NamespaceComparer();
				if((currentNs == null && destination == null) || nsComp.Compare(currentNs, destination) == 0) return null;

				if(GetPagesInfoEntity(_wiki, NameTools.GetFullName(destinationName, NameTools.GetLocalName(page.FullName))) != null) return null;
				if(destination != null && GetNamespacesEntity(_wiki, destinationName) == null) return null;

				if(currentNs != null && currentNs.DefaultPage != null) {
					// Cannot move the default page
					if(new PageNameComparer().Compare(currentNs.DefaultPage, page) == 0) return null;
				}

				string newPageFullName = NameTools.GetFullName(destinationName, NameTools.GetLocalName(page.FullName));

				List<CategoryInfo> pageCategories = GetCategoriesForPage(page).ToList();
				foreach(CategoryInfo category in pageCategories) {
					CategoriesEntity categoryEntity = GetCategoriesEntity(_wiki, category.FullName);
					categoryEntity.PagesFullNames = string.Join("|", categoryEntity.PagesFullNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList().Remove(oldPageInfoEntity.RowKey));
					_context.UpdateObject(categoryEntity);
					if(copyCategories) {
						string newCategoryFullName = NameTools.GetFullName(destinationName, NameTools.GetLocalName(category.FullName));
						CategoriesEntity newCategoryEntity = GetCategoriesEntity(_wiki, newCategoryFullName);
						if(newCategoryEntity == null) {
							newCategoryEntity = new CategoriesEntity() {
								PartitionKey = _wiki,
								RowKey = newCategoryFullName,
								Namespace = destinationName,
								PagesFullNames = newPageFullName
							};
							_context.AddObject(CategoriesTable, newCategoryEntity);
						}
						else {
							newCategoryEntity.PagesFullNames = newCategoryEntity.PagesFullNames + "|" + newPageFullName;
							_context.UpdateObject(newCategoryEntity);
						}
					}
				}
				_context.SaveChangesStandard();

				// Unindex page with the old fullName
				PageContent pageContent = GetContent(page);
				UnindexPage(pageContent);
				Message[] messages = GetMessages(page);
				foreach(Message msg in messages) {
					UnindexMessageTree(page, msg);
				}

				// Update the pageInfo with values from the new namespace
				var newPageInfoEntity = new PagesInfoEntity();
				newPageInfoEntity.PartitionKey = _wiki;
				newPageInfoEntity.RowKey = newPageFullName;
				newPageInfoEntity.Namespace = destinationName;
				newPageInfoEntity.PageId = oldPageInfoEntity.PageId;
				newPageInfoEntity.CreationDateTime = oldPageInfoEntity.CreationDateTime;
				_context.AddObject(PagesInfoTable, newPageInfoEntity);
				_context.DeleteObject(oldPageInfoEntity);
				_context.SaveChangesStandard();

				// Invalidate pageInfoCache
				_pagesInfoCache = null;

				PageInfo newPage = new PageInfo(newPageFullName, this, page.CreationDateTime);

				// Index page with the new fullName
				IndexPage(new PageContent(newPage,
					pageContent.Title,
					pageContent.User,
					pageContent.LastModified.ToUniversalTime(),
					pageContent.Comment, pageContent.Content,
					pageContent.Keywords,
					pageContent.Description));
				foreach(Message msg in messages) {
					IndexMessageTree(newPage, msg);
				}

				return newPage;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		private CategoriesEntity GetCategoriesEntity(string wiki, string categoryFullName) {
			var query = (from e in _context.CreateQuery<CategoriesEntity>(CategoriesTable).AsTableServiceQuery()
						 where e.PartitionKey.Equals(_wiki) && e.RowKey.Equals(categoryFullName)
						 select e).AsTableServiceQuery();
			return QueryHelper<CategoriesEntity>.FirstOrDefault(query);
		}

		private List<CategoriesEntity> GetCategoriesEntities(string wiki, string namespaceName) {
			var query = (from e in _context.CreateQuery<CategoriesEntity>(CategoriesTable).AsTableServiceQuery()
						 where e.PartitionKey.Equals(_wiki)
						 select e).AsTableServiceQuery();
			var entities = QueryHelper<CategoriesEntity>.All(query);

			List<CategoriesEntity> categoriesEntities = new List<CategoriesEntity>();
			foreach(CategoriesEntity entity in entities) {
				if(string.IsNullOrEmpty(namespaceName) && string.IsNullOrEmpty(entity.Namespace) || !string.IsNullOrEmpty(namespaceName) && namespaceName == entity.Namespace) {
					categoriesEntities.Add(entity);
				}
			}
			return categoriesEntities;
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
			if(fullName.Length == 0) throw new ArgumentException("fullName");

			try {
				var entity = GetCategoriesEntity(_wiki, fullName);
				if(entity == null) return null;

				CategoryInfo categoryInfo = new CategoryInfo(entity.RowKey, this);
				categoryInfo.Pages = entity.PagesFullNames != null ? entity.PagesFullNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries) : new string[0];

				return categoryInfo;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets all the Categories in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace.</param>
		/// <returns>All the Categories in the namespace, sorted by name.</returns>
		public CategoryInfo[] GetCategories(NamespaceInfo nspace) {
			try {
				var entities = GetCategoriesEntities(_wiki, nspace != null ? nspace.Name : null);

				List<CategoryInfo> categories = new List<CategoryInfo>();
				foreach(CategoriesEntity entity in entities) {
					CategoryInfo category = new CategoryInfo(entity.RowKey, this);
					category.Pages = entity.PagesFullNames != null ? entity.PagesFullNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries) : new string[0];
					categories.Add(category);
				}
				return categories.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets all the categories of a page.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <returns>The categories, sorted by name.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		public CategoryInfo[] GetCategoriesForPage(PageInfo page) {
			if(page == null) throw new ArgumentNullException("page");

			try {
				var pageInfoEntity = GetPagesInfoEntity(_wiki, page.FullName);
				var query = (from e in _context.CreateQuery<CategoriesEntity>(CategoriesTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki)
							 select e).AsTableServiceQuery();
				var categoriesEntities = QueryHelper<CategoriesEntity>.All(query);

				List<CategoryInfo> categories = new List<CategoryInfo>();
				foreach(CategoriesEntity categoryEntity in categoriesEntities) {
					if(categoryEntity.PagesFullNames != null && categoryEntity.PagesFullNames.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries).ToList<string>().Contains(pageInfoEntity.RowKey)) {
						CategoryInfo categoryInfo = new CategoryInfo(categoryEntity.RowKey, this);
						categoryInfo.Pages = categoryEntity.PagesFullNames.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
						categories.Add(categoryInfo);
					}
				}

				return categories.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Adds a Category.
		/// </summary>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Category name.</param>
		/// <returns>The correct CategoryInfo object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		public CategoryInfo AddCategory(string nspace, string name) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("name");

			string categoryFullName = NameTools.GetFullName(nspace, name);
			try {
				// If the category is alrady present return null
				if(GetCategoriesEntity(_wiki, categoryFullName) != null) return null;

				CategoriesEntity categoriesEntity = new CategoriesEntity() {
					PartitionKey = _wiki,
					RowKey = categoryFullName,
					Namespace = nspace
				};
				_context.AddObject(CategoriesTable, categoriesEntity);
				_context.SaveChangesStandard();
				return new CategoryInfo(categoryFullName, this);
			}
			catch(Exception ex) {
				throw ex;
			}
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
			if(newName.Length == 0) throw new ArgumentException("newName");

			try {
				var oldCategoryEntity = GetCategoriesEntity(_wiki, category.FullName);
				if(oldCategoryEntity == null) return null;

				CategoryInfo newCategoryInfo = new CategoryInfo(NameTools.GetFullName(NameTools.GetNamespace(category.FullName), newName), this);
				newCategoryInfo.Pages = oldCategoryEntity.PagesFullNames != null ? oldCategoryEntity.PagesFullNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries) : new string[0];
				if(GetCategory(newCategoryInfo.FullName) != null) return null;

				CategoriesEntity newCategoryEntity = new CategoriesEntity() {
					PartitionKey = oldCategoryEntity.PartitionKey,
					RowKey = newCategoryInfo.FullName,
					Namespace = oldCategoryEntity.Namespace,
					PagesFullNames = oldCategoryEntity.PagesFullNames
				};
				_context.AddObject(CategoriesTable, newCategoryEntity);
				_context.DeleteObject(oldCategoryEntity);
				_context.SaveChangesStandard();

				return newCategoryInfo;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Removes a Category.
		/// </summary>
		/// <param name="category">The Category to remove.</param>
		/// <returns>
		/// True if the Category has been removed successfully.
		/// </returns>
		/// <exception cref="ArgumentNullException">If <paramref name="category"/> is <c>null</c>.</exception>
		public bool RemoveCategory(CategoryInfo category) {
			if(category == null) throw new ArgumentNullException("category");

			try {
				var entity = GetCategoriesEntity(_wiki, category.FullName);
				if(entity == null) return false;

				_context.DeleteObject(entity);
				_context.SaveChangesStandard();
				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Merges two Categories.
		/// </summary>
		/// <param name="source">The source Category.</param>
		/// <param name="destination">The destination Category.</param>
		/// <returns>The correct <see cref="T:CategoryInfo"/> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="destination"/> are <c>null</c>.</exception>
		public CategoryInfo MergeCategories(CategoryInfo source, CategoryInfo destination) {
			if(source == null) throw new ArgumentNullException("source");
			if(destination == null) throw new ArgumentNullException("destination");

			try {
				NamespaceInfo sourceNs = NameTools.GetNamespace(source.FullName) != null ? GetNamespace(NameTools.GetNamespace(source.FullName)) : null;
				NamespaceInfo destinationNs = NameTools.GetNamespace(destination.FullName) != null ? GetNamespace(NameTools.GetNamespace(destination.FullName)) : null;
				NamespaceComparer nsComp = new NamespaceComparer();
				if(!(sourceNs == null && destinationNs == null) && nsComp.Compare(sourceNs, destinationNs) != 0) {
					// Different namespaces
					return null;
				}

				CategoriesEntity sourceCategoryEntity = GetCategoriesEntity(_wiki, source.FullName);
				CategoriesEntity destinationCategoryEntity = GetCategoriesEntity(_wiki, destination.FullName);

				if(sourceCategoryEntity == null || destinationCategoryEntity == null) return null;

				List<string> pagesIds = destinationCategoryEntity.PagesFullNames != null ? new List<string>(destinationCategoryEntity.PagesFullNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)) : new List<string>();
				if(sourceCategoryEntity.PagesFullNames != null) {
					pagesIds.AddRange(sourceCategoryEntity.PagesFullNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
				}

				destinationCategoryEntity.PagesFullNames = string.Join("|", pagesIds);

				_context.UpdateObject(destinationCategoryEntity);
				_context.DeleteObject(sourceCategoryEntity);

				_context.SaveChangesStandard();

				List<string> pagesNames = new List<string>(destination.Pages);
				pagesNames.AddRange(source.Pages);
				destination.Pages = pagesNames.ToArray();
				return destination;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		private bool alwaysGenerateDocument = false;

		private IList<IndexWordMappingEntity> GetIndexWordMappingEntities(string wiki) {
			var query = (from e in _context.CreateQuery<IndexWordMappingEntity>(IndexWordMappingTable).AsTableServiceQuery()
						 where e.PartitionKey.Equals(wiki)
						 select e).AsTableServiceQuery();
			return QueryHelper<IndexWordMappingEntity>.All(query);
		}

		private IList<IndexWordMappingEntity> GetIndexWordMappingEntitiesByDocumentName(string wiki, string documentName) {
			var query = (from e in _context.CreateQuery<IndexWordMappingEntity>(IndexWordMappingTable).AsTableServiceQuery()
						 where e.PartitionKey.Equals(wiki) && e.DocumentName.Equals(documentName)
						 select e).AsTableServiceQuery();
			return QueryHelper<IndexWordMappingEntity>.All(query);
		}

		private IList<IndexWordMappingEntity> GetIndexWordMappingEntitiesByWord(string wiki, string word) {
			var query = (from e in _context.CreateQuery<IndexWordMappingEntity>(IndexWordMappingTable).AsTableServiceQuery()
						 where e.PartitionKey.Equals(wiki) && e.Word.Equals(word)
						 select e).AsTableServiceQuery();
			return QueryHelper<IndexWordMappingEntity>.All(query);
		}

		private IList<IndexWordMappingEntity> GetIndexWordMappingEntitiesByTypeTag(string wiki, string typeTag) {
			var query = (from e in _context.CreateQuery<IndexWordMappingEntity>(IndexWordMappingTable).AsTableServiceQuery()
						 where e.PartitionKey.Equals(wiki) && e.TypeTag.Equals(typeTag)
						 select e).AsTableServiceQuery();
			return QueryHelper<IndexWordMappingEntity>.All(query);
		}

		/// <summary>
		/// Sets test flags (to be used only for tests).
		/// </summary>
		/// <param name="alwaysGenerateDocument">A value indicating whether to always generate a result when resolving a document, 
		/// even when the page does not exist.</param>
		public void SetFlags(bool alwaysGenerateDocument) {
			this.alwaysGenerateDocument = alwaysGenerateDocument;
		}

		/// <summary>
		/// Gets a word fetcher.
		/// </summary>
		/// <returns>The word fetcher.</returns>
		private IWordFetcher GetWordFetcher() {
			return new AzureWordFetcher(TryFindWord);
		}

		/// <summary>
		/// Gets the search index (only used for testing purposes).
		/// </summary>
		public IIndex Index {
			get { return index; }
		}

		/// <summary>
		/// Performs a search in the index.
		/// </summary>
		/// <param name="parameters">The search parameters.</param>
		/// <returns>The results.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="parameters"/> is <c>null</c>.</exception>
		public SearchResultCollection PerformSearch(SearchParameters parameters) {
			if(parameters == null) throw new ArgumentNullException("parameters");

			return index.Search(parameters);
		}

		/// <summary>
		/// Rebuilds the search index.
		/// </summary>
		public void RebuildIndex() {
			index.Clear(null);

			List<NamespaceInfo> allNamespaces = new List<NamespaceInfo>(GetNamespaces());
			allNamespaces.Add(null);

			int indexedElements = 0;

			foreach(NamespaceInfo nspace in allNamespaces) {
				foreach(PageInfo page in GetPages(nspace)) {
					IndexPage(GetContent(page));
					indexedElements++;

					foreach(Message msg in GetMessages(page)) {
						IndexMessageTree(page, msg);
						indexedElements++;
					}
				}
			}
		}

		/// <summary>
		/// Deletes all data associated to a document.
		/// </summary>
		/// <param name="document">The document.</param>
		private void DeleteDataForDocument(IDocument document) {
			// 1. Delete all data related to a document
			// 2. Delete all words that have no more mappings

			IList<IndexWordMappingEntity> indexWordMappingEntitites = GetIndexWordMappingEntitiesByDocumentName(_wiki, document.Name);

			foreach(IndexWordMappingEntity indexWordMappingEntity in indexWordMappingEntitites) {
				_context.DeleteObject(indexWordMappingEntity);
			}
			_context.SaveChangesStandard();
		}

		/// <summary>
		/// Saves data for a new document.
		/// </summary>
		/// <param name="document">The document.</param>
		/// <param name="content">The content words.</param>
		/// <param name="title">The title words.</param>
		/// <param name="keywords">The keywords.</param>
		/// <returns>The number of stored occurrences.</returns>
		private int SaveDataForDocument(IDocument document, WordInfo[] content, WordInfo[] title, WordInfo[] keywords) {
			// 1. Insert document
			// 2. Insert all new words
			// 3. Load all word IDs
			// 4. Insert mappings

			List<WordInfo> allWords = new List<WordInfo>(content.Length + title.Length + keywords.Length);
			allWords.AddRange(content);
			allWords.AddRange(title);
			allWords.AddRange(keywords);

			foreach(WordInfo word in allWords) {
				IndexWordMappingEntity indexWordMappingEntity = new IndexWordMappingEntity() {
					PartitionKey = _wiki,
					RowKey = document.Name + "|" + word.Text + "|" + word.Location + "|" + word.WordIndex,
					Word = word.Text,
					DocumentName = document.Name,
					FirstCharIndex = word.FirstCharIndex,
					Location = word.Location.Location,
					DocumentTitle = document.Title,
					TypeTag = document.TypeTag,
					DateTime = document.DateTime
				};
				_context.AddObject(IndexWordMappingTable, indexWordMappingEntity);
			}
			_context.SaveChangesStandard();
			return allWords.Count;
		}
		
		/// <summary>
		/// Tries to load all data related to a word from the database.
		/// </summary>
		/// <param name="text">The word text.</param>
		/// <param name="word">The returned word.</param>
		/// <returns><c>true</c> if the word is found, <c>false</c> otherwise.</returns>
		private bool TryFindWord(string text, out Word word) {
			// 1. Find word - if not found, return
			// 2. Read all raw word mappings
			// 3. Read all documents (unique)
			// 4. Build result data structure

			// Read all raw mappings
			IList<IndexWordMappingEntity> indexWordMappingEntitites = GetIndexWordMappingEntitiesByWord(_wiki, text);

			if(indexWordMappingEntitites == null || indexWordMappingEntitites.Count <= 0) {
				word = null;
				return false;
			}

			Dictionary<string, IDocument> documents = new Dictionary<string, IDocument>(64);
			foreach(IndexWordMappingEntity indexWordMappingEntity in indexWordMappingEntitites) {
				string documentName = indexWordMappingEntity.DocumentName;
				if(documents.ContainsKey(documentName)) continue;

				DumpedDocument dumpedDoc = new DumpedDocument(1,
						documentName, indexWordMappingEntity.DocumentTitle,
						indexWordMappingEntity.TypeTag,
						indexWordMappingEntity.DateTime.ToLocalTime());

				IDocument document = BuildDocument(dumpedDoc);

				if(document != null) documents.Add(documentName, document);
			}

			OccurrenceDictionary occurrences = new OccurrenceDictionary(indexWordMappingEntitites.Count);
			foreach(IndexWordMappingEntity indexWordMappingEntity in indexWordMappingEntitites) {
				if(!occurrences.ContainsKey(documents[indexWordMappingEntity.DocumentName])) {
					occurrences.Add(documents[indexWordMappingEntity.DocumentName], new SortedBasicWordInfoSet(2));
				}

				occurrences[documents[indexWordMappingEntity.DocumentName]].Add(new BasicWordInfo(
					(ushort)indexWordMappingEntity.FirstCharIndex, ushort.Parse(indexWordMappingEntity.RowKey.Split(new char[] {'|'})[3]), WordLocation.GetInstance((byte)indexWordMappingEntity.Location)));
			}

			word = new Word(1, text, occurrences);
			return true;
		}

		/// <summary>
		/// Handles the construction of an <see cref="T:IDocument" /> for the search engine.
		/// </summary>
		/// <param name="dumpedDocument">The input dumped document.</param>
		/// <returns>The resulting <see cref="T:IDocument" />.</returns>
		private IDocument BuildDocument(DumpedDocument dumpedDocument) {
			if(alwaysGenerateDocument) {
				return new DummyDocument() {
					ID = dumpedDocument.ID,
					Name = dumpedDocument.Name,
					Title = dumpedDocument.Title,
					TypeTag = dumpedDocument.TypeTag,
					DateTime = dumpedDocument.DateTime
				};
			}

			if(dumpedDocument.TypeTag == PageDocument.StandardTypeTag) {
				string pageName = PageDocument.GetPageName(dumpedDocument.Name);

				PageInfo page = GetPage(pageName);

				if(page == null) return null;
				else return new PageDocument(page, dumpedDocument, TokenizeContent);
			}
			else if(dumpedDocument.TypeTag == MessageDocument.StandardTypeTag) {
				string pageFullName;
				int id;
				MessageDocument.GetMessageDetails(dumpedDocument.Name, out pageFullName, out id);

				PageInfo page = GetPage(pageFullName);
				if(page == null) return null;
				else return new MessageDocument(page, id, dumpedDocument, TokenizeContent);
			}
			else return null;
		}

		// Extremely dirty way for testing the search engine in combination with alwaysGenerateDocument
		private class DummyDocument : IDocument {

			public uint ID { get; set; }

			public string Name { get; set; }

			public string Title { get; set; }

			public string TypeTag { get; set; }

			public DateTime DateTime { get; set; }

			public WordInfo[] Tokenize(string content) {
				return ScrewTurn.Wiki.SearchEngine.Tools.Tokenize(content);
			}
		}

		/// <summary>
		/// Gets some statistics about the search engine index.
		/// </summary>
		/// <param name="documentCount">The total number of documents.</param>
		/// <param name="wordCount">The total number of unique words.</param>
		/// <param name="occurrenceCount">The total number of word-document occurrences.</param>
		/// <param name="size">The approximated size, in bytes, of the search engine index.</param>
		public void GetIndexStats(out int documentCount, out int wordCount, out int occurrenceCount, out long size) {
			documentCount = index.TotalDocuments;
			wordCount = index.TotalWords;
			occurrenceCount = index.TotalOccurrences;
			size = GetSize();
		}

		/// <summary>
		/// Gets the approximate size, in bytes, of the search engine index.
		/// </summary>
		private long GetSize() {
			// 1. Size of documents: 8 + 2*20 + 2*30 + 2*1 + 8 = 118 bytes
			// 2. Size of words: 8 + 2*8 = 24 bytes
			// 3. Size of mappings: 8 + 8 + 2 + 2 + 1 = 21 bytes
			// 4. Size = Size * 2

			long size = 0;

			int rows = GetCount(IndexElementType.Documents);

			if(rows == -1) return 0;
			size += rows * 118;

			rows = GetCount(IndexElementType.Words);

			if(rows == -1) return 0;
			size += rows * 24;

			rows = GetCount(IndexElementType.Occurrences);

			if(rows == -1) return 0;
			size += rows * 21;
			
			return size * 2;
		}

		/// <summary>
		/// Gets the number of elements in the index.
		/// </summary>
		/// <param name="element">The type of elements.</param>
		/// <returns>The number of elements.</returns>
		private int GetCount(IndexElementType element) {
			int count = 0;

			IList<IndexWordMappingEntity> indexWordMappingEntities = GetIndexWordMappingEntities(_wiki);

			if(element == IndexElementType.Documents) count = indexWordMappingEntities.Distinct<IndexWordMappingEntity>(new IndexWordMappingEntityComparerByDocumentName()).Count();
			else if(element == IndexElementType.Words) count = indexWordMappingEntities.Distinct<IndexWordMappingEntity>(new IndexWordMappingEntityComparerByWord()).Count();
			else if(element == IndexElementType.Occurrences) count = indexWordMappingEntities.Count;
			else throw new NotSupportedException("Unsupported element type");

			return count;
		}

		/// <summary>
		/// Clears the index.
		/// </summary>
		/// <param name="state">A state object passed from the index.</param>
		private void ClearIndex() {
			IList<IndexWordMappingEntity> indexWordMappingEntities = GetIndexWordMappingEntities(_wiki);

			int count = 0;
			foreach(IndexWordMappingEntity indexWordMappingEntity in indexWordMappingEntities) {
				_context.DeleteObject(indexWordMappingEntity);
				if(count > 98) {
					_context.SaveChangesStandard();
					count = 0;
				}
				else {
					count++;
				}
			}
			_context.SaveChangesStandard();
		}

		public bool IsIndexCorrupted {
			get { return false; }
		}

		/// <summary>
		/// Indexes a page.
		/// </summary>
		/// <param name="content">The page content.</param>
		/// <param name="transaction">The current transaction.</param>
		/// <returns>The number of indexed words, including duplicates.</returns>
		private int IndexPage(PageContent content) {
			try {
				string documentName = PageDocument.GetDocumentName(content.PageInfo);

				DumpedDocument ddoc = new DumpedDocument(0, documentName, _host.PrepareTitleForIndexing(_wiki, content.PageInfo, content.Title),
					PageDocument.StandardTypeTag, content.LastModified);

				// Store the document
				// The content should always be prepared using IHost.PrepareForSearchEngineIndexing()
				int count = index.StoreDocument(new PageDocument(content.PageInfo, ddoc, TokenizeContent),
					content.Keywords, _host.PrepareContentForIndexing(_wiki, content.PageInfo, content.Content), null);

				if(count == 0 && content.Content.Length > 0) {
					_host.LogEntry("Indexed 0 words for page " + content.PageInfo.FullName + ": possible index corruption. Please report this error to the developers",
						LogEntryType.Warning, null, this, _wiki);
				}

				return count;
			}
			catch(Exception ex) {
				_host.LogEntry("Page indexing error for " + content.PageInfo.FullName + " (skipping page): " + ex.ToString(), LogEntryType.Error, null, this, _wiki);
				return 0;
			}
		}

		/// <summary>
		/// Removes a page from the search engine index.
		/// </summary>
		/// <param name="content">The content of the page to remove.</param>
		/// <param name="transaction">The current transaction.</param>
		private void UnindexPage(PageContent content) {
			string documentName = PageDocument.GetDocumentName(content.PageInfo);

			DumpedDocument ddoc = new DumpedDocument(0, documentName, _host.PrepareTitleForIndexing(_wiki, content.PageInfo, content.Title),
				PageDocument.StandardTypeTag, content.LastModified);
			index.RemoveDocument(new PageDocument(content.PageInfo, ddoc, TokenizeContent), null);
		}

		/// <summary>
		/// Indexes a message tree.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="root">The root message.</param>
		/// <param name="transaction">The current transaction.</param>
		private void IndexMessageTree(PageInfo page, Message root) {
			IndexMessage(page, root.ID, root.Subject, root.DateTime, root.Body);
			foreach(Message reply in root.Replies) {
				IndexMessageTree(page, reply);
			}
		}

		/// <summary>
		/// Indexes a message.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="id">The message ID.</param>
		/// <param name="subject">The subject.</param>
		/// <param name="dateTime">The date/time.</param>
		/// <param name="body">The body.</param>
		/// <param name="transaction">The current transaction.</param>
		/// <returns>The number of indexed words, including duplicates.</returns>
		private int IndexMessage(PageInfo page, int id, string subject, DateTime dateTime, string body) {
			try {
				// Trim "RE:" to avoid polluting the search engine index
				if(subject.ToLowerInvariant().StartsWith("re:") && subject.Length > 3) subject = subject.Substring(3).Trim();

				string documentName = MessageDocument.GetDocumentName(page, id);

				DumpedDocument ddoc = new DumpedDocument(0, documentName, _host.PrepareTitleForIndexing(_wiki, null, subject),
					MessageDocument.StandardTypeTag, dateTime);

				// Store the document
				// The content should always be prepared using IHost.PrepareForSearchEngineIndexing()
				int count = index.StoreDocument(new MessageDocument(page, id, ddoc, TokenizeContent), null,
					_host.PrepareContentForIndexing(_wiki, null, body), null);

				if(count == 0 && body.Length > 0) {
					_host.LogEntry("Indexed 0 words for message " + page.FullName + ":" + id.ToString() + ": possible index corruption. Please report this error to the developers",
						LogEntryType.Warning, null, this, _wiki);
				}

				return count;
			}
			catch(Exception ex) {
				_host.LogEntry("Message indexing error for " + page.FullName + ":" + id.ToString() + " (skipping message): " + ex.ToString(), LogEntryType.Error, null, this, _wiki);
				return 0;
			}
		}

		/// <summary>
		/// Removes a message from the search engine index.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="id">The message ID.</param>
		/// <param name="subject">The subject.</param>
		/// <param name="dateTime">The date/time.</param>
		/// <param name="body">The body.</param>
		/// <param name="transaction">The current transaction.</param>
		/// <returns>The number of indexed words, including duplicates.</returns>
		private void UnindexMessage(PageInfo page, int id, string subject, DateTime dateTime, string body) {
			// Trim "RE:" to avoid polluting the search engine index
			if(subject.ToLowerInvariant().StartsWith("re:") && subject.Length > 3) subject = subject.Substring(3).Trim();

			string documentName = MessageDocument.GetDocumentName(page, id);

			DumpedDocument ddoc = new DumpedDocument(0, documentName, _host.PrepareTitleForIndexing(_wiki, null, subject),
				MessageDocument.StandardTypeTag, DateTime.Now);
			index.RemoveDocument(new MessageDocument(page, id, ddoc, TokenizeContent), null);
		}

		/// <summary>
		/// Removes a message tree from the search engine index.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="root">The tree root.</param>
		/// <param name="transaction">The current transaction.</param>
		private void UnindexMessageTree(PageInfo page, Message root) {
			UnindexMessage(page, root.ID, root.Subject, root.DateTime, root.Body);
			foreach(Message reply in root.Replies) {
				UnindexMessageTree(page, reply);
			}
		}

		/// <summary>
		/// Tokenizes page content.
		/// </summary>
		/// <param name="content">The content to tokenize.</param>
		/// <returns>The tokenized words.</returns>
		private static WordInfo[] TokenizeContent(string content) {
			WordInfo[] words = SearchEngine.Tools.Tokenize(content);
			return words;
		}

		private Dictionary<string, PagesInfoEntity> _pagesInfoCache;

		private PagesInfoEntity GetPagesInfoEntity(string wiki, string pageFullName) {
			if(_pagesInfoCache == null) _pagesInfoCache = new Dictionary<string, PagesInfoEntity>();

			if(!_pagesInfoCache.ContainsKey(pageFullName)) {
				var query = (from e in _context.CreateQuery<PagesInfoEntity>(PagesInfoTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(wiki) && e.RowKey.Equals(pageFullName)
							 select e).AsTableServiceQuery();
				var entity = QueryHelper<PagesInfoEntity>.FirstOrDefault(query);
				if(entity == null) return null;
				_pagesInfoCache[pageFullName] = entity;
				
			}
			return _pagesInfoCache[pageFullName];
		}

		private List<PagesInfoEntity> GetPagesInfoEntities(string wiki, string namespaceName) {
			var query = (from e in _context.CreateQuery<PagesInfoEntity>(PagesInfoTable).AsTableServiceQuery()
						 where e.PartitionKey.Equals(wiki)
						 select e).AsTableServiceQuery();
			var entities = QueryHelper<PagesInfoEntity>.All(query);

			List<PagesInfoEntity> pagesInfoEntities = new List<PagesInfoEntity>();
			foreach(PagesInfoEntity entity in entities) {
				if(namespaceName == null && entity.Namespace == null || namespaceName != null && namespaceName == entity.Namespace) {
					pagesInfoEntities.Add(entity);
				}
			}
			return pagesInfoEntities;
		}

		/// <summary>
		/// Gets a page.
		/// </summary>
		/// <param name="fullName">The full name of the page.</param>
		/// <returns>The <see cref="T:PageInfo"/>, or <c>null</c> if no page is found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		public PageInfo GetPage(string fullName) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("fullName");

			try {
				var entity = GetPagesInfoEntity(_wiki, fullName);
				if(entity == null) return null;

				return new PageInfo(entity.RowKey, this, entity.CreationDateTime.ToLocalTime());
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets all the Pages in a namespace.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>All the Pages in the namespace, sorted by name.</returns>
		public PageInfo[] GetPages(NamespaceInfo nspace) {
			try {
				var entities = GetPagesInfoEntities(_wiki, nspace == null ? null : nspace.Name);

				List<PageInfo> pagesInfo = new List<PageInfo>(entities.Count);
				foreach(PagesInfoEntity entity in entities) {
						pagesInfo.Add(new PageInfo(entity.RowKey, this, entity.CreationDateTime.ToLocalTime()));
				}
				return pagesInfo.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets all the pages in a namespace that are bound to zero categories.
		/// </summary>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The pages, sorted by name.</returns>
		public PageInfo[] GetUncategorizedPages(NamespaceInfo nspace) {
			lock(this) {
				PageInfo[] pages = GetPages(nspace);
				CategoryInfo[] categories = GetCategories(nspace);

				List<PageInfo> result = new List<PageInfo>(pages.Length);

				foreach(PageInfo p in pages) {
					bool found = false;
					foreach(CategoryInfo c in categories) {
						foreach(string name in c.Pages) {
							if(StringComparer.OrdinalIgnoreCase.Compare(name, p.FullName) == 0) {
								found = true;
								break;
							}
						}
					}
					if(!found) result.Add(p);
				}

				return result.ToArray();
			}
		}

		// pageId -> (revision -> pageContentEntity)
		private Dictionary<string, Dictionary<string, PagesContentsEntity>> _pagesContentCache;

		// if true allPagesContent has been called and doesn't need to be called again
		private bool allPagesContentRetireved = false;

		private PagesContentsEntity GetPagesContentsEntityByRevision(string pageId, string revision) {
			if(_pagesContentCache == null) _pagesContentCache = new Dictionary<string, Dictionary<string, PagesContentsEntity>>();

			if(!_pagesContentCache.ContainsKey(pageId)) _pagesContentCache[pageId] = new Dictionary<string, PagesContentsEntity>();
			
			if(!_pagesContentCache[pageId].ContainsKey(revision)) {
				var query = (from e in _context.CreateQuery<PagesContentsEntity>(PagesContentsTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(pageId) && e.RowKey.Equals(revision)
							 select e).AsTableServiceQuery();
				var entity = QueryHelper<PagesContentsEntity>.FirstOrDefault(query);
				if(entity == null) {
					return null;
				}
				else {
					_pagesContentCache[pageId][revision] = entity;
				}
			}
			return _pagesContentCache[pageId][revision];
		}

		private List<PagesContentsEntity> GetPagesContentEntities(string pageId) {
			if(!(allPagesContentRetireved && _pagesContentCache != null && _pagesContentCache.ContainsKey(pageId))) {
				// pagesContents not present in temp local cache retrieve from table storage
				if(_pagesContentCache == null) _pagesContentCache = new Dictionary<string, Dictionary<string, PagesContentsEntity>>();
				_pagesContentCache[pageId] = new Dictionary<string, PagesContentsEntity>();
				allPagesContentRetireved = true;

				var query = (from e in _context.CreateQuery<PagesContentsEntity>(PagesContentsTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(pageId)
							 select e).AsTableServiceQuery();
				var entities = QueryHelper<PagesContentsEntity>.All(query);

				foreach(PagesContentsEntity entity in entities) {
					if(entity != null) _pagesContentCache[pageId][entity.RowKey] = entity;
				}
			}

			List<PagesContentsEntity> pagesContentsEntities = new List<PagesContentsEntity>();
			foreach(var item in _pagesContentCache[pageId]) {
				pagesContentsEntities.Add(item.Value);
			}
			return pagesContentsEntities;
		}

		/// <summary>
		/// Gets the Content of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>
		/// The Page Content object, <c>null</c> if the page does not exist or <paramref name="page"/> is <c>null</c>,
		/// or an empty instance if the content could not be retrieved (<seealso cref="PageContent.GetEmpty"/>).
		/// </returns>
		public PageContent GetContent(PageInfo page) {
			if(page == null) return null;

			try {
				// Find the PageInfo; if not found return null
				var entity = GetPagesInfoEntity(_wiki, page.FullName);
				if(entity == null) return null;

				// Find the associated PageContent; if not found return an empty PageContent
				var pageContentEntity = GetPagesContentsEntityByRevision(entity.PageId, CurrentRevision);
				if(pageContentEntity == null) return null;

				return new PageContent(page, pageContentEntity.Title, pageContentEntity.User, pageContentEntity.LastModified.ToLocalTime(), string.IsNullOrEmpty(pageContentEntity.Comment) ? "" : pageContentEntity.Comment, pageContentEntity.Content, pageContentEntity.Keywords == null ? new string[0] : pageContentEntity.Keywords.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries), string.IsNullOrEmpty(pageContentEntity.Description) ? null : pageContentEntity.Description);
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets the content of a draft of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The draft, or <c>null</c> if no draft exists.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		public PageContent GetDraft(PageInfo page) {
			if(page == null) throw new ArgumentNullException("page");

			try {
				// Find the PageInfo; if not found return null
				var entity = GetPagesInfoEntity(_wiki, page.FullName);
				if(entity == null) return null;

				// Find the associated draft PageContent; if not found return an empty PageContent
				var pageContentEntity = GetPagesContentsEntityByRevision(entity.PageId, Draft);
				if(pageContentEntity == null) return null;

				return new PageContent(page, pageContentEntity.Title, pageContentEntity.User, pageContentEntity.LastModified.ToLocalTime(), string.IsNullOrEmpty(pageContentEntity.Comment) ? "" : pageContentEntity.Comment, pageContentEntity.Content, pageContentEntity.Keywords == null ? new string[0] : pageContentEntity.Keywords.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries), string.IsNullOrEmpty(pageContentEntity.Description) ? null : pageContentEntity.Description);
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Deletes a draft of a Page.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <returns><c>true</c> if the draft is deleted, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		public bool DeleteDraft(PageInfo page) {
			if(page == null) throw new ArgumentNullException("page");

			try {
				// Find the PageInfo; if not found return null
				var entity = GetPagesInfoEntity(_wiki, page.FullName);
				if(entity == null) return false;

				// Find the associated draft PageContent; if not found return an empty PageContent
				var pageContentEntity = GetPagesContentsEntityByRevision(entity.PageId, Draft);
				if(pageContentEntity == null) return false;

				_context.DeleteObject(pageContentEntity);
				_context.SaveChangesStandard();

				// Invalidate pagesContentCache
				_pagesContentCache = null;
				allPagesContentRetireved = false;

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets the Backup/Revision numbers of a Page.
		/// </summary>
		/// <param name="page">The Page to get the Backups of.</param>
		/// <returns>The Backup/Revision numbers.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		public int[] GetBackups(PageInfo page) {
			if(page == null) throw new ArgumentNullException("page");

			try {
				// Find the PageInfo; if not found return null
				var entity = GetPagesInfoEntity(_wiki, page.FullName);
				if(entity == null) return null;

				// Find the associated PageContent
				var pageContentEntities = GetPagesContentEntities(entity.PageId);
				if(pageContentEntities == null) return null;

				List<int> revisions = new List<int>(pageContentEntities.Count);
				foreach(PagesContentsEntity pageContentEntity in pageContentEntities) {
					if(pageContentEntity.RowKey != CurrentRevision && pageContentEntity.RowKey != Draft) {
						int rev;
						if(int.TryParse(pageContentEntity.RowKey,out rev)) revisions.Add(rev);
					}
				}
				return revisions.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets the Content of a Backup of a Page.
		/// </summary>
		/// <param name="page">The Page to get the backup of.</param>
		/// <param name="revision">The Backup/Revision number.</param>
		/// <returns>The Page Backup.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="revision"/> is less than zero.</exception>
		public PageContent GetBackupContent(PageInfo page, int revision) {
			if(page == null) throw new ArgumentNullException("page");
			if(revision < 0) throw new ArgumentOutOfRangeException("revision");

			try {
				// Find the PageInfo; if not found return null
				var entity = GetPagesInfoEntity(_wiki, page.FullName);
				if(entity == null) return null;

				// Find the associated PageContent
				var pageContentEntity = GetPagesContentsEntityByRevision(entity.PageId, revision.ToString());
				if(pageContentEntity == null) return null;

				return new PageContent(page, pageContentEntity.Title, pageContentEntity.User, pageContentEntity.LastModified.ToLocalTime(), string.IsNullOrEmpty(pageContentEntity.Comment) ? "" : pageContentEntity.Comment, pageContentEntity.Content, pageContentEntity.Keywords == null ? new string[0] : pageContentEntity.Keywords.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries), string.IsNullOrEmpty(pageContentEntity.Description) ? null : pageContentEntity.Description);
			}
			catch(Exception ex) {
				throw ex;
			}
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
			if(revision < 0) throw new ArgumentOutOfRangeException("revision");

			try {
				// Find the PageInfo; if not found return null
				var entity = GetPagesInfoEntity(_wiki, content.PageInfo.FullName);
				if(entity == null) return false;

				// Find the associated PageContent
				var pageContentEntity = GetPagesContentsEntityByRevision(entity.PageId, revision.ToString());
				if(pageContentEntity != null) {
					pageContentEntity.Title = content.Title;
					pageContentEntity.User = content.User;
					pageContentEntity.LastModified = content.LastModified.ToUniversalTime();
					pageContentEntity.Comment = content.Comment;
					pageContentEntity.Content = content.Content;
					pageContentEntity.Keywords = string.Join("|", content.Keywords);
					pageContentEntity.Description = content.Description;
					_context.UpdateObject(pageContentEntity);
					_context.SaveChangesStandard();

					// Invalidate pagesContetnCache
					_pagesContentCache = null;
					allPagesContentRetireved = false;

					return true;
				}
				else {
					pageContentEntity = new PagesContentsEntity() {
						PartitionKey = entity.PageId,
						RowKey = revision.ToString(),
						Title = content.Title,
						User = content.User,
						LastModified = content.LastModified.ToUniversalTime(),
						Comment = content.Comment,
						Content = content.Content,
						Keywords = string.Join("|", content.Keywords),
						Description = content.Description
					};
					_context.AddObject(PagesContentsTable, pageContentEntity);
					_context.SaveChangesStandard();

					// Invalidate pagesContetnCache
					_pagesContentCache = null;
					allPagesContentRetireved = false;

					return true;
				}
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Adds a Page.
		/// </summary>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Page Name.</param>
		/// <param name="creationDateTime">The creation Date/Time.</param>
		/// <returns>The correct PageInfo object or null.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> is empty.</exception>
		public PageInfo AddPage(string nspace, string name, DateTime creationDateTime) {
			if(name == null) throw new ArgumentNullException("name");
			if(name.Length == 0) throw new ArgumentException("name");

			try {
				if(GetPagesInfoEntity(_wiki, NameTools.GetFullName(nspace, name)) != null) return null;
				PagesInfoEntity pageInfoEntity = new PagesInfoEntity() {
					PartitionKey = _wiki,
					RowKey = NameTools.GetFullName(nspace, name),
					Namespace = nspace,
					PageId = Guid.NewGuid().ToString("N"),
					CreationDateTime = creationDateTime.ToUniversalTime()
				};

				_context.AddObject(PagesInfoTable, pageInfoEntity);
				_context.SaveChangesStandard();

				// Invalidate pagesInfoCache
				_pagesInfoCache = null;

				return new PageInfo(NameTools.GetFullName(nspace, name), this, creationDateTime);
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Renames a Page.
		/// </summary>
		/// <param name="page">The Page to rename.</param>
		/// <param name="newName">The new Name.</param>
		/// <returns>The correct <see cref="T:PageInfo"/> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> or <paramref name="newName"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="newName"/> is empty.</exception>
		public PageInfo RenamePage(PageInfo page, string newName) {
			if(page == null) throw new ArgumentNullException("page");
			if(newName == null) throw new ArgumentNullException("newName");
			if(newName.Length == 0) throw new ArgumentException("newName");

			try {
				var oldPageEntity = GetPagesInfoEntity(_wiki, page.FullName);
				if(oldPageEntity == null) return null;

				NamespaceInfo currentNs = NameTools.GetNamespace(page.FullName) != null ? GetNamespace(NameTools.GetNamespace(page.FullName)) : null;
				if(currentNs != null && currentNs.DefaultPage != null) {
					// Cannot rename the default page
					if(new PageNameComparer().Compare(currentNs.DefaultPage, page) == 0) return null;
				}

				string newPageFullName = NameTools.GetFullName(NameTools.GetNamespace(page.FullName), newName);

				if(GetPagesInfoEntity(_wiki, newPageFullName) != null) return null;

				CategoryInfo[] categoriesInfo = GetCategoriesForPage(page);
				foreach(CategoryInfo categoryInfo in categoriesInfo) {
					CategoriesEntity categoryEntity = GetCategoriesEntity(_wiki, categoryInfo.FullName);
					string[] oldNamesCategoryPages = categoryEntity.PagesFullNames != null ? categoryEntity.PagesFullNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries) : new string[0];
					List<string> newNamesCategoryPages = new List<string>(oldNamesCategoryPages.Length);
					foreach(string oldName in oldNamesCategoryPages) {
						newNamesCategoryPages.Add(NameTools.GetFullName(NameTools.GetNamespace(page.FullName), newName));
					}
					categoryEntity.PagesFullNames = string.Join("|", newNamesCategoryPages);
					_context.UpdateObject(categoryEntity);
				}
				_context.SaveChangesStandard();

				List<NavigationPathEntity> navigationPathEntities = GetNavigationPathEntities(_wiki, NameTools.GetNamespace(page.FullName));
				foreach(NavigationPathEntity navigationPathEntity in navigationPathEntities) {
					string[] oldNamesNavigationPathPages = navigationPathEntity.PagesFullNames != null ? navigationPathEntity.PagesFullNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries) : new string[0];
					List<string> newNamesNavigationPathPages = new List<string>(oldNamesNavigationPathPages.Length);
					foreach(string oldName in oldNamesNavigationPathPages) {
						newNamesNavigationPathPages.Add(NameTools.GetFullName(newName, NameTools.GetLocalName(oldName)));
					}
					navigationPathEntity.PagesFullNames = string.Join("|", newNamesNavigationPathPages);
					_context.UpdateObject(navigationPathEntity);
				}
				_context.SaveChangesStandard();

				// Unindex page
				PageContent currentContent = GetContent(page);
				UnindexPage(currentContent);
				Message[] messages = GetMessages(page);
				foreach(Message msg in messages) {
					UnindexMessageTree(page, msg);
				}

				PagesInfoEntity newPage = new PagesInfoEntity() {
					PartitionKey = oldPageEntity.PartitionKey,
					RowKey = newPageFullName,
					Namespace = oldPageEntity.Namespace,
					CreationDateTime = oldPageEntity.CreationDateTime,
					PageId = oldPageEntity.PageId
				};
				_context.AddObject(PagesInfoTable, newPage);
				_context.DeleteObject(oldPageEntity);
				_context.SaveChangesStandard();

				PageInfo newPageInfo = new PageInfo(newPageFullName, this, newPage.CreationDateTime.ToLocalTime());

				// Index page
				IndexPage(new PageContent(
					newPageInfo,
					currentContent.Title,
					currentContent.User,
					currentContent.LastModified,
					currentContent.Comment,
					currentContent.Content,
					currentContent.Keywords,
					currentContent.Description));
				foreach(Message msg in messages) {
					IndexMessageTree(newPageInfo, msg);
				}

				// Invalidate pagesInfoCache
				_pagesInfoCache = null;

				return newPageInfo;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Modifies the Content of a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="title">The Title of the Page.</param>
		/// <param name="username">The Username.</param>
		/// <param name="dateTime">The Date/Time.</param>
		/// <param name="comment">The Comment of the editor, about this revision.</param>
		/// <param name="content">The Page Content.</param>
		/// <param name="keywords">The keywords, usually used for SEO.</param>
		/// <param name="description">The description, usually used for SEO.</param>
		/// <param name="saveMode">The save mode for this modification.</param>
		/// <returns><c>true</c> if the Page has been modified successfully, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/>, <paramref name="title"/>
		/// 	<paramref name="username"/> or <paramref name="content"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="title"/> or <paramref name="username"/> are empty.</exception>
		public bool ModifyPage(PageInfo page, string title, string username, DateTime dateTime, string comment, string content, string[] keywords, string description, SaveMode saveMode) {
			if(page == null) throw new ArgumentNullException("page");
			if(title == null) throw new ArgumentNullException("title");
			if(username == null) throw new ArgumentNullException("username");
			if(content == null) throw new ArgumentNullException("content");
			if(title.Length == 0) throw new ArgumentException("title");
			if(username.Length == 0) throw new ArgumentException("username");

			try {
				// Find the PageInfo; if not found return false
				var entity = GetPagesInfoEntity(_wiki, page.FullName);
				if(entity == null) return false;

				switch(saveMode) {
					case SaveMode.Draft:
						bool newDraft = false;
						// Find the "Draft" PageContent if present
						var draftPageContentEntity = GetPagesContentsEntityByRevision(entity.PageId, Draft);
						if(draftPageContentEntity == null) {
							newDraft = true;
							draftPageContentEntity = new PagesContentsEntity();
							draftPageContentEntity.PartitionKey = entity.PageId;
							draftPageContentEntity.RowKey = Draft;
						}
						draftPageContentEntity.Title = title;
						draftPageContentEntity.User = username;
						draftPageContentEntity.LastModified = dateTime.ToUniversalTime();
						draftPageContentEntity.Comment = comment;
						draftPageContentEntity.Content = content;
						draftPageContentEntity.Keywords = string.Join("|", keywords);
						draftPageContentEntity.Description = description;
						if(newDraft) {
							_context.AddObject(PagesContentsTable, draftPageContentEntity);
						}
						else {
							_context.UpdateObject(draftPageContentEntity);
						}
						_context.SaveChangesStandard();
						break;
					default:
						bool update = false;
						// Find the "CurrentRevision" PageContent if present
						var currentPageContentEntity = GetPagesContentsEntityByRevision(entity.PageId, CurrentRevision);

						// If currentPageContent is not found the page has never been saved and has no backups
						if(currentPageContentEntity != null) {
							update = true;
							if(saveMode == SaveMode.Backup) {
								int[] backupsRevisions = GetBackups(page);
								int rev = (backupsRevisions.Length > 0 ? backupsRevisions[backupsRevisions.Length - 1] + 1 : 0);
								PagesContentsEntity backupPageContentEntity = new PagesContentsEntity() {
									PartitionKey = currentPageContentEntity.PartitionKey,
									RowKey = rev.ToString(),
									Comment = currentPageContentEntity.Comment,
									Content = currentPageContentEntity.Content,
									Description = currentPageContentEntity.Description,
									Keywords = currentPageContentEntity.Keywords,
									LastModified = currentPageContentEntity.LastModified,
									Title = currentPageContentEntity.Title,
									User = currentPageContentEntity.User
								};
								_context.AddObject(PagesContentsTable, backupPageContentEntity);
								_context.SaveChangesStandard();
							}
						}
						else {
							currentPageContentEntity = new PagesContentsEntity();
						}
						currentPageContentEntity.PartitionKey = entity.PageId;
						currentPageContentEntity.RowKey = CurrentRevision;
						currentPageContentEntity.Title = title;
						currentPageContentEntity.User = username;
						currentPageContentEntity.LastModified = dateTime.ToUniversalTime();
						currentPageContentEntity.Comment = comment;
						currentPageContentEntity.Content = content;
						currentPageContentEntity.Keywords = keywords != null ? string.Join("|", keywords) : null;
						currentPageContentEntity.Description = description;

						if(update) {
							_context.UpdateObject(currentPageContentEntity);
						}
						else {
							_context.AddObject(PagesContentsTable, currentPageContentEntity);
						}
						_context.SaveChangesStandard();

						IndexPage(new PageContent(
								new PageInfo(entity.RowKey, this, entity.CreationDateTime.ToLocalTime()),
								currentPageContentEntity.Title,
								currentPageContentEntity.User,
								currentPageContentEntity.LastModified.ToLocalTime(),
								currentPageContentEntity.Comment,
								currentPageContentEntity.Content,
								currentPageContentEntity.Keywords != null ? currentPageContentEntity.Keywords.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries) : null,
								currentPageContentEntity.Description));
						break;
				}

				// Invalidate pagesContetnCache
				_pagesContentCache = null;
				allPagesContentRetireved = false;

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Performs the rollback of a Page to a specified revision.
		/// </summary>
		/// <param name="page">The Page to rollback.</param>
		/// <param name="revision">The Revision to rollback the Page to.</param>
		/// <returns><c>true</c> if the rollback succeeded, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="revision"/> is less than zero.</exception>
		public bool RollbackPage(PageInfo page, int revision) {
			if(page == null) throw new ArgumentNullException("page");
			if(revision < 0) throw new ArgumentOutOfRangeException("revision");

			PageContent currentContent = GetContent(page);
			if(currentContent == null) return false;
			
			// Unindex the current page content
			UnindexPage(currentContent);

			// Get the pageContet at the specified revision
			PageContent rollbackPageContent = GetBackupContent(page, revision);
			// If rollbackPageContent is null, the page does not exists and return false
			if(rollbackPageContent == null) return false;

			// Save a new version of the page with the content at the given revision
			bool success = ModifyPage(page, rollbackPageContent.Title, rollbackPageContent.User, rollbackPageContent.LastModified, rollbackPageContent.Comment, rollbackPageContent.Content, rollbackPageContent.Keywords, rollbackPageContent.Description, SaveMode.Backup);

			if(success) {
				IndexPage(rollbackPageContent);
			}

			// Invalidate pagesContetnCache
			_pagesContentCache = null;
			allPagesContentRetireved = false;

			return success;
		}

		/// <summary>
		/// Deletes the Backups of a Page, up to a specified revision.
		/// </summary>
		/// <param name="page">The Page to delete the backups of.</param>
		/// <param name="revision">The newest revision to delete (newer revision are kept) or -1 to delete all the Backups.</param>
		/// <returns><c>true</c> if the deletion succeeded, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="revision"/> is less than -1.</exception>
		public bool DeleteBackups(PageInfo page, int revision) {
			if(page == null) throw new ArgumentNullException("page");
			if(revision < -1) throw new ArgumentOutOfRangeException("revision");

			try {
				// Find the PageInfo; if not found return false
				var entity = GetPagesInfoEntity(_wiki, page.FullName);
				if(entity == null) return false;

				// Find the associated PageContent
				var pageContentQuery = (from e in _context.CreateQuery<PagesContentsEntity>(PagesContentsTable).AsTableServiceQuery()
										where e.PartitionKey.Equals(entity.PageId) && e.RowKey != CurrentRevision && e.RowKey != Draft
										select e).AsTableServiceQuery();
				var pageContentEntities = QueryHelper<PagesContentsEntity>.All(pageContentQuery);
				if(pageContentEntities == null) return false;

				var pageContentEntityWithSpecifiedRevision = pageContentEntities.Any(c => c.RowKey == revision.ToString());

				int idx = -1;
				if(revision != -1) {
					idx = pageContentEntityWithSpecifiedRevision ? revision : -1;
				}
				else {
					idx = pageContentEntities.Max((c) => { return int.Parse(c.RowKey); });
				}


				// Operations
				// - Delete old beckups, from 0 to revision
				// - Rename newer backups starting from 0

				for(int i = 0; i <= idx; i++) {
					var pageContentEntity = pageContentEntities.First(c => c.RowKey == i.ToString());
					_context.DeleteObject(pageContentEntity);
				}
				_context.SaveChangesStandard();

				if(revision != -1) {
					for(int i = revision + 1; i < pageContentEntities.Count; i++) {
						var pageContentEntity = pageContentEntities.First(c => c.RowKey == i.ToString());
						var newPageContentEntity = new PagesContentsEntity() {
							PartitionKey = pageContentEntity.PartitionKey,
							RowKey = (int.Parse(pageContentEntity.RowKey) - revision - 1).ToString(),
							Comment = pageContentEntity.Comment,
							Content = pageContentEntity.Content,
							Description = pageContentEntity.Description,
							Keywords = pageContentEntity.Keywords,
							LastModified = pageContentEntity.LastModified,
							Title = pageContentEntity.Title,
							User = pageContentEntity.User
						};
						_context.DeleteObject(pageContentEntity);
						_context.AddObject(PagesContentsTable, newPageContentEntity);
						_context.SaveChangesStandard();
					}
				}

				// Invalidate pagesContetnCache
				_pagesContentCache = null;
				allPagesContentRetireved = false;

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Removes a Page.
		/// </summary>
		/// <param name="page">The Page to remove.</param>
		/// <returns>True if the Page is removed successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		public bool RemovePage(PageInfo page) {
			if(page == null) throw new ArgumentNullException("page");
			
			try {
				// Find the PageInfo; if not found return false
				var entity = GetPagesInfoEntity(_wiki, page.FullName);
				if(entity == null) return false;

				// Find the associated PageContent
				var pageContentEntities = GetPagesContentEntities(entity.PageId);
				if(pageContentEntities == null) return false;

				// Delete all the pageContent including draft and backups
				bool deleteExecuted = false;
				foreach(PagesContentsEntity pageContentEntity in pageContentEntities) {
					if(pageContentEntity.RowKey == CurrentRevision) {
						PageContent currentContent = new PageContent(page, pageContentEntity.Title, pageContentEntity.User, pageContentEntity.LastModified.ToLocalTime(),
							pageContentEntity.Comment,
							pageContentEntity.Content,
							pageContentEntity.Keywords != null ? pageContentEntity.Keywords.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries) : null,
							pageContentEntity.Description);
						_context.SaveChangesStandard();
						UnindexPage(currentContent);
						foreach(Message msg in GetMessages(page)) {
							UnindexMessageTree(page, msg);
						}
					}
					_context.DeleteObject(pageContentEntity);
					deleteExecuted = true;
				}
				_context.SaveChangesStandard();

				// Delete the PageInfo entity
				_context.DeleteObject(entity);
				_context.SaveChangesStandard();

				// Invalidate pagesInfoCache
				_pagesInfoCache = null;

				// Invalidate pagesContetnCache
				_pagesContentCache = null;
				allPagesContentRetireved = false;

				return deleteExecuted;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Binds a Page with one or more Categories.
		/// </summary>
		/// <param name="page">The Page to bind.</param>
		/// <param name="categories">The Categories to bind the Page with.</param>
		/// <returns>True if the binding succeeded.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> or <paramref name="categories"/> are <c>null</c>.</exception>
		public bool RebindPage(PageInfo page, string[] categories) {
			if(page == null) throw new ArgumentNullException("page");
			if(categories == null) throw new ArgumentNullException("categories");

			try {
				var pageInfoEntity = GetPagesInfoEntity(_wiki, page.FullName);
				// If the page does not exists retur false
				if(pageInfoEntity == null) return false;

				var query = (from e in _context.CreateQuery<CategoriesEntity>(CategoriesTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki)
							 select e).AsTableServiceQuery();
				var categoriesEntities = QueryHelper<CategoriesEntity>.All(query);

				foreach(CategoriesEntity entity in categoriesEntities) {
					List<string> categoryPages = entity.PagesFullNames != null ? entity.PagesFullNames.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries).ToList<string>() : new List<string>();
					if(categoryPages.Contains(pageInfoEntity.RowKey)) {
						categoryPages.Remove(pageInfoEntity.RowKey);
						entity.PagesFullNames = string.Join("|", categoryPages);
						_context.UpdateObject(entity);
					}
				}
				foreach(string category in categories) {
					if(category == null) throw new ArgumentNullException("categories", "A category name cannot be null");
					if(category.Length == 0) throw new ArgumentException("A category name cannot be empty", "categories");

					CategoriesEntity categoryEntity = categoriesEntities.FirstOrDefault((e) => { return e.RowKey == category; });
					// If the category does not exists return false
					if(categoryEntity == null) return false;

					List<string> categoryPages = categoryEntity.PagesFullNames != null ? categoryEntity.PagesFullNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList<string>() : new List<string>();
					categoryPages.Add(pageInfoEntity.RowKey);
					categoryEntity.PagesFullNames = string.Join("|", categoryPages);
					_context.UpdateObject(categoryEntity);
				}
				_context.SaveChangesStandard();
				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		private MessageEntity GetMessageEntity(string pageId, int messageId) {
			var messagesQuery = (from e in _context.CreateQuery<MessageEntity>(MessagesTable).AsTableServiceQuery()
								 where e.PartitionKey.Equals(pageId) && e.RowKey.Equals(messageId.ToString())
								 select e).AsTableServiceQuery();
			return QueryHelper<MessageEntity>.FirstOrDefault(messagesQuery);
		}

		private IList<MessageEntity> GetMessagesEntities(string pageId) {
			var messagesQuery = (from e in _context.CreateQuery<MessageEntity>(MessagesTable).AsTableServiceQuery()
								 where e.PartitionKey.Equals(pageId)
								 select e).AsTableServiceQuery();
			return QueryHelper<MessageEntity>.All(messagesQuery);
		}

		private MessageEntity BuildMessageEntity(string pageId, int messageId, string username, string subject, DateTime dateTime, string body, int parent) {
			return new MessageEntity() {
				PartitionKey = pageId,
				RowKey = messageId.ToString(),
				Username = username,
				Subject = subject,
				DateTime = dateTime.ToUniversalTime(),
				Body = body,
				ParetnId = parent.ToString()
			};
		}
		
		private int GetNextMessageId(string pageId) {
			IList<MessageEntity> messagesEntities = GetMessagesEntities(pageId);
			if(messagesEntities.Count == 0) return 0;
			return messagesEntities.Max<MessageEntity>(m => int.Parse(m.RowKey)) + 1;
		}
		
		/// <summary>
		/// Gets the Page Messages.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The list of the <b>first-level</b> Messages, containing the replies properly nested, sorted by date/time.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		public Message[] GetMessages(PageInfo page) {
			if(page == null) throw new ArgumentNullException("page");

			try {
				var pageInfoEntity = GetPagesInfoEntity(_wiki, page.FullName);
				// If the page does not exists return null
				if(pageInfoEntity == null) return null;

				var messagesEntities = GetMessagesEntities(pageInfoEntity.PageId);

				List<Message> messages = new List<Message>(messagesEntities.Count);
				foreach(MessageEntity messageEntity in messagesEntities) {
					if(messageEntity.ParetnId == "-1") {
						messages.Add(new Message(int.Parse(messageEntity.RowKey), messageEntity.Username, messageEntity.Subject, messageEntity.DateTime.ToLocalTime(), messageEntity.Body));
					}
				}
				foreach(MessageEntity messageEntity in messagesEntities) {
					if(messageEntity.ParetnId != "-1") {
						Message parentMessage = messages.Find(m => m.ID.ToString() == messageEntity.ParetnId);
						List<Message> replies = parentMessage.Replies.ToList();
						replies.Add(new Message(int.Parse(messageEntity.RowKey), messageEntity.Username, messageEntity.Subject, messageEntity.DateTime.ToLocalTime(), messageEntity.Body));
						parentMessage.Replies = replies.ToArray();
					}
				}
				return messages.OrderBy(m => m.DateTime).ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets the total number of Messages in a Page Discussion.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <returns>The number of messages.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		public int GetMessageCount(PageInfo page) {
			if(page == null) throw new ArgumentNullException("page");

			try {
				var pageInfoEntity = GetPagesInfoEntity(_wiki, page.FullName);
				// If the page does not exists return -1
				if(pageInfoEntity == null) return -1;

				var messagesQuery = (from e in _context.CreateQuery<MessageEntity>(MessagesTable).AsTableServiceQuery()
									 where e.PartitionKey.Equals(pageInfoEntity.PageId)
									 select e).AsTableServiceQuery();
				var messagesEntities = QueryHelper<MessageEntity>.All(messagesQuery);
				return messagesEntities.Count;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Removes all messages for a page and stores the new messages.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="messages">The new messages to store.</param>
		/// <returns><c>true</c> if the messages are stored, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> or <paramref name="messages"/> are <c>null</c>.</exception>
		public bool BulkStoreMessages(PageInfo page, Message[] messages) {
			if(page == null) throw new ArgumentNullException("page");
			if(messages == null) throw new ArgumentNullException("messages");

			try {
				var pageInfoEntity = GetPagesInfoEntity(_wiki, page.FullName);
				if(pageInfoEntity == null) return false;

				foreach(Message msg in GetMessages(page)) {
					UnindexMessageTree(page, msg);
				}

				// Validate IDs by using a dictionary as a way of validation
				try {
					Dictionary<int, byte> ids = new Dictionary<int, byte>(50);
					foreach(Message msg in messages) {
						AddAllIds(ids, msg);
					}
				}
				catch(ArgumentException) {
					return false;
				}

				// Remove all messages for the given page
				var oldMessagesIds = GetMessagesEntities(pageInfoEntity.PageId).Select(m => m.RowKey);
				foreach(string oldMessageId in oldMessagesIds) {
					RemoveMessage(page, int.Parse(oldMessageId), true);
				}

				foreach(Message message in messages) {
					_context.AddObject(MessagesTable, BuildMessageEntity(pageInfoEntity.PageId, message.ID, message.Username, message.Subject, message.DateTime, message.Body, -1));
					foreach(Message reply in message.Replies) {
						var replayEntity = BuildMessageEntity(pageInfoEntity.PageId, reply.ID, reply.Username, reply.Subject, reply.DateTime, reply.Body, message.ID);
						_context.AddObject(MessagesTable, replayEntity);
					}
				}
				_context.SaveChangesStandard();

				foreach(Message msg in messages) {
					IndexMessageTree(page, msg);
				}

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		private static void AddAllIds(Dictionary<int, byte> dictionary, Message msg) {
			dictionary.Add(msg.ID, 0);
			foreach(Message m in msg.Replies) {
				AddAllIds(dictionary, m);
			}
		}

		/// <summary>
		/// Adds a new Message to a Page.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="username">The Username.</param>
		/// <param name="subject">The Subject.</param>
		/// <param name="dateTime">The Date/Time.</param>
		/// <param name="body">The Body.</param>
		/// <param name="parent">The Parent Message ID, or -1.</param>
		/// <returns>True if the Message is added successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/>, <paramref name="username"/>, <paramref name="subject"/> or <paramref name="body"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="username"/> or <paramref name="subject"/> are empty.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="parent"/> is less than -1.</exception>
		public bool AddMessage(PageInfo page, string username, string subject, DateTime dateTime, string body, int parent) {
			if(page == null) throw new ArgumentNullException("page");
			if(username == null) throw new ArgumentNullException("username");
			if(subject == null) throw new ArgumentNullException("subject");
			if(body == null) throw new ArgumentNullException("body");
			if(username.Length == 0) throw new ArgumentException("username");
			if(subject.Length == 0) throw new ArgumentException("subject");
			if(parent < -1) throw new ArgumentOutOfRangeException("parent");

			try {
				var pageInfoEntity = GetPagesInfoEntity(_wiki, page.FullName);
				// If page does not exists return false
				if(pageInfoEntity == null) return false;

				if(parent != -1) {
					// If the given parent message with the given id does not exists return false;
					var parentMessageEntity = GetMessageEntity(pageInfoEntity.PageId, parent);
					if(parentMessageEntity == null) return false;
				}

				var messageEntity = BuildMessageEntity(pageInfoEntity.PageId, GetNextMessageId(pageInfoEntity.PageId), username, subject, dateTime, body, parent);
				_context.AddObject(MessagesTable, messageEntity);
				_context.SaveChangesStandard();

				IndexMessage(page, int.Parse(messageEntity.RowKey), subject, dateTime, body);

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Removes a Message.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="id">The ID of the Message to remove.</param>
		/// <param name="removeReplies">A value specifying whether or not to remove the replies.</param>
		/// <returns>True if the Message is removed successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="id"/> is less than zero.</exception>
		public bool RemoveMessage(PageInfo page, int id, bool removeReplies) {
			if(page == null) throw new ArgumentNullException("page");
			if(id < 0) throw new ArgumentOutOfRangeException("id");

			try {
				var pageInfoEntity = GetPagesInfoEntity(_wiki, page.FullName);
				if(pageInfoEntity == null) return false;

				var messageEntity = GetMessageEntity(pageInfoEntity.PageId, id);
				if(messageEntity == null) return false;

				UnindexMessage(page, int.Parse(messageEntity.RowKey), messageEntity.Subject, messageEntity.DateTime.ToLocalTime(), messageEntity.Body);
				_context.DeleteObject(messageEntity);

				var messagesEntities = GetMessagesEntities(pageInfoEntity.PageId);
				List<MessageEntity> messagesToBeUnindexed = new List<MessageEntity>();
				foreach(var entity in messagesEntities) {
					if(entity.ParetnId == id.ToString()) {
						if(removeReplies) {
							messagesToBeUnindexed.Add(entity);
							_context.DeleteObject(entity);
						}
						else {
							entity.ParetnId = messageEntity.ParetnId;
							_context.UpdateObject(entity);
						}
					}
				}
				_context.SaveChangesStandard();

				// Unindex messages previously marked to be unindexed
				foreach(var entity in messagesToBeUnindexed) {
					UnindexMessage(page, int.Parse(entity.RowKey), entity.Subject, entity.DateTime.ToLocalTime(), entity.Body);
				}

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Modifies a Message.
		/// </summary>
		/// <param name="page">The Page.</param>
		/// <param name="id">The ID of the Message to modify.</param>
		/// <param name="username">The Username.</param>
		/// <param name="subject">The Subject.</param>
		/// <param name="dateTime">The Date/Time.</param>
		/// <param name="body">The Body.</param>
		/// <returns>True if the Message is modified successfully.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="page"/>, <paramref name="username"/>, <paramref name="subject"/> or <paramref name="body"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="id"/> is less than zero.</exception>
		/// <exception cref="ArgumentException">If <paramref name="username"/> or <paramref name="subject"/> are empty.</exception>
		public bool ModifyMessage(PageInfo page, int id, string username, string subject, DateTime dateTime, string body) {
			if(page == null) throw new ArgumentNullException("page");
			if(username == null) throw new ArgumentNullException("username");
			if(subject == null) throw new ArgumentNullException("subject");
			if(body == null) throw new ArgumentNullException("body");
			if(id < 0) throw new ArgumentOutOfRangeException("id");
			if(username.Length == 0) throw new ArgumentException("username");
			if(subject.Length == 0) throw new ArgumentException("subject");

			try {
				var pageInfoEntity = GetPagesInfoEntity(_wiki, page.FullName);
				if(pageInfoEntity == null) return false;

				var messageEntity = GetMessageEntity(pageInfoEntity.PageId, id);
				if(messageEntity == null) return false;

				UnindexMessage(page, int.Parse(messageEntity.RowKey), messageEntity.Subject, messageEntity.DateTime.ToLocalTime(), messageEntity.Body);

				messageEntity.Username = username;
				messageEntity.Subject = subject;
				messageEntity.DateTime = dateTime.ToUniversalTime();
				messageEntity.Body = body;

				_context.UpdateObject(messageEntity);
				_context.SaveChangesStandard();

				IndexMessage(page, int.Parse(messageEntity.RowKey), messageEntity.Subject, messageEntity.DateTime.ToLocalTime(), messageEntity.Body);

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		private NavigationPathEntity GetNavigationPathEntity(string wiki, string fullName) {
			var messagesQuery = (from e in _context.CreateQuery<NavigationPathEntity>(NavigationPathsTable).AsTableServiceQuery()
								 where e.PartitionKey.Equals(wiki) && e.RowKey.Equals(fullName)
								 select e).AsTableServiceQuery();
			return QueryHelper<NavigationPathEntity>.FirstOrDefault(messagesQuery);
		}

		private List<NavigationPathEntity> GetNavigationPathEntities(string wiki, string nspaceName) {
			var query = (from e in _context.CreateQuery<NavigationPathEntity>(NavigationPathsTable).AsTableServiceQuery()
						 where e.PartitionKey.Equals(_wiki)
						 select e).AsTableServiceQuery();
			var entities = QueryHelper<NavigationPathEntity>.All(query);

			List<NavigationPathEntity> navigationPathEntities = new List<NavigationPathEntity>();
			foreach(NavigationPathEntity entity in entities) {
				if(nspaceName == null && entity.Namespace == null || nspaceName != null && nspaceName == entity.Namespace) {
					navigationPathEntities.Add(entity);
				}
			}
			return navigationPathEntities;
		}

		/// <summary>
		/// Gets all the Navigation Paths in a Namespace.
		/// </summary>
		/// <param name="nspace">The Namespace.</param>
		/// <returns>All the Navigation Paths, sorted by name.</returns>
		public NavigationPath[] GetNavigationPaths(NamespaceInfo nspace) {
			try {
				var entities = GetNavigationPathEntities(_wiki, nspace != null ? nspace.Name : null);

				List<NavigationPath> navigationPaths = new List<NavigationPath>(entities.Count);
				foreach(NavigationPathEntity entity in entities) {
						NavigationPath navigationPath = new NavigationPath(entity.RowKey, this);
						navigationPath.Pages = entity.PagesFullNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
						navigationPaths.Add(navigationPath);
				}
				return navigationPaths.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Adds a new Navigation Path.
		/// </summary>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Name of the Path.</param>
		/// <param name="pages">The Pages array.</param>
		/// <returns>The correct <see cref="T:NavigationPath"/> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> or <paramref name="pages"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> or <paramref name="pages"/> are empty.</exception>
		public NavigationPath AddNavigationPath(string nspace, string name, PageInfo[] pages) {
			if(name == null) throw new ArgumentNullException("name");
			if(pages == null) throw new ArgumentNullException("pages");
			if(name.Length == 0) throw new ArgumentException("name");
			if(pages.Length == 0) throw new ArgumentException("pages");

			try {
				if(GetNavigationPathEntity(_wiki, NameTools.GetFullName(nspace, name)) != null) return null;

				NavigationPath navigationPath = new NavigationPath(NameTools.GetFullName(nspace, name), this);
				List<string> pagesNames = new List<string>(pages.Length);
				foreach(PageInfo page in pages) {
					if(page == null) throw new ArgumentNullException();
					PagesInfoEntity pageInfoEntity = GetPagesInfoEntity(_wiki, page.FullName);
					if(pageInfoEntity == null) throw new ArgumentException();
					pagesNames.Add(page.FullName);
				}
				NavigationPathEntity navigationPathEntity = new NavigationPathEntity();
				navigationPathEntity.PartitionKey = _wiki;
				navigationPathEntity.RowKey = NameTools.GetFullName(nspace, name);
				navigationPathEntity.Namespace = nspace;
				navigationPathEntity.PagesFullNames = string.Join("|", pagesNames);

				_context.AddObject(NavigationPathsTable, navigationPathEntity);
				_context.SaveChangesStandard();

				navigationPath.Pages = pagesNames.ToArray();

				return navigationPath;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Modifies an existing navigation path.
		/// </summary>
		/// <param name="path">The navigation path to modify.</param>
		/// <param name="pages">The new pages array.</param>
		/// <returns>The correct <see cref="T:NavigationPath"/> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="path"/> or <paramref name="pages"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pages"/> is empty.</exception>
		public NavigationPath ModifyNavigationPath(NavigationPath path, PageInfo[] pages) {
			if(path == null) throw new ArgumentNullException("path");
			if(pages == null) throw new ArgumentNullException("pages");
			if(pages.Length == 0) throw new ArgumentException("pages");

			try {
				NavigationPathEntity navigationPathEntity = GetNavigationPathEntity(_wiki, path.FullName);
				if(navigationPathEntity == null) return null;

				List<string> pagesNames = new List<string>(pages.Length);
				foreach(PageInfo page in pages) {
					if(page == null) throw new ArgumentNullException("pages", "A page element cannot be null");
					if(GetPagesInfoEntity(_wiki, page.FullName) == null) throw new ArgumentException("Page not found", "pages");
					pagesNames.Add(page.FullName);
				}
				navigationPathEntity.PagesFullNames = string.Join("|", pagesNames);

				_context.UpdateObject(navigationPathEntity);
				_context.SaveChangesStandard();

				path.Pages = pagesNames.ToArray();

				return path;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Removes a Navigation Path.
		/// </summary>
		/// <param name="path">The navigation path to remove.</param>
		/// <returns><c>true</c> if the path is removed, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="path"/> is <c>null</c>.</exception>
		public bool RemoveNavigationPath(NavigationPath path) {
			if(path == null) throw new ArgumentNullException("path");

			try {
				NavigationPathEntity navigationPathEntity = GetNavigationPathEntity(_wiki, path.FullName);
				if(navigationPathEntity == null) return false;

				_context.DeleteObject(navigationPathEntity);
				_context.SaveChangesStandard();
				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		private SnippetEntity GetSnippetEntity(string wiki, string snippetName) {
			var messagesQuery = (from e in _context.CreateQuery<SnippetEntity>(SnippetsTable).AsTableServiceQuery()
								 where e.PartitionKey.Equals(wiki) && e.RowKey.Equals(snippetName)
								 select e).AsTableServiceQuery();
			return QueryHelper<SnippetEntity>.FirstOrDefault(messagesQuery);
		}

		/// <summary>
		/// Gets all the snippets.
		/// </summary>
		/// <returns>All the snippets, sorted by name.</returns>
		public Snippet[] GetSnippets() {
			try {
				var query = (from e in _context.CreateQuery<SnippetEntity>(SnippetsTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki)
							 select e).AsTableServiceQuery();
				var entities = QueryHelper<SnippetEntity>.All(query);

				List<Snippet> snippets = new List<Snippet>(entities.Count);
				foreach(SnippetEntity snippetEnity in entities) {
					snippets.Add(new Snippet(snippetEnity.RowKey, snippetEnity.Content, this));
				}
				return snippets.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
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
			if(content == null) throw new ArgumentNullException("content");
			if(name.Length == 0) throw new ArgumentException("name");

			try {
				if(GetSnippetEntity(_wiki, name) != null) return null;
				SnippetEntity snippetEntity = new SnippetEntity() {
					PartitionKey = _wiki,
					RowKey = name,
					Content = content
				};
				_context.AddObject(SnippetsTable, snippetEntity);
				_context.SaveChangesStandard();

				return new Snippet(name, content, this);
			}
			catch(Exception ex) {
				throw ex;
			}
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
			if(content == null) throw new ArgumentNullException("content");
			if(name.Length == 0) throw new ArgumentException("name");

			try {
				SnippetEntity snippetEntity = GetSnippetEntity(_wiki, name);
				if(snippetEntity == null) return null;

				snippetEntity.RowKey = name;
				snippetEntity.Content = content;

				_context.UpdateObject(snippetEntity);
				_context.SaveChangesStandard();

				return new Snippet(name, content, this);
			}
			catch(Exception ex) {
				throw ex;
			}
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
			if(name.Length == 0) throw new ArgumentException("name");

			try {
				SnippetEntity snippetEntity = GetSnippetEntity(_wiki, name);
				if(snippetEntity == null) return false;

				_context.DeleteObject(snippetEntity);
				_context.SaveChangesStandard();

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		private ContentTemplateEntity GetContentTemplateEntity(string wiki, string contentTemplateName) {
			var messagesQuery = (from e in _context.CreateQuery<ContentTemplateEntity>(ContentTemplatesTable).AsTableServiceQuery()
								 where e.PartitionKey.Equals(wiki) && e.RowKey.Equals(contentTemplateName)
								 select e).AsTableServiceQuery();
			return QueryHelper<ContentTemplateEntity>.FirstOrDefault(messagesQuery);
		}

		/// <summary>
		/// Gets all the content templates.
		/// </summary>
		/// <returns>All the content templates, sorted by name.</returns>
		public ContentTemplate[] GetContentTemplates() {
			try {
				var query = (from e in _context.CreateQuery<ContentTemplateEntity>(ContentTemplatesTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki)
							 select e).AsTableServiceQuery();
				var entities = QueryHelper<ContentTemplateEntity>.All(query);

				List<ContentTemplate> contentTemplates = new List<ContentTemplate>(entities.Count);
				foreach(ContentTemplateEntity contentTemplateEnity in entities) {
					contentTemplates.Add(new ContentTemplate(contentTemplateEnity.RowKey, contentTemplateEnity.Content, this));
				}
				return contentTemplates.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
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
			if(content == null) throw new ArgumentNullException("content");
			if(name.Length == 0) throw new ArgumentException("name");

			try {
				// If the contentTemplate is alrady present return null
				if(GetContentTemplateEntity(_wiki, name) != null) return null;

				ContentTemplateEntity contentTemplateEntity = new ContentTemplateEntity() {
					PartitionKey = _wiki,
					RowKey = name,
					Content = content
				};
				_context.AddObject(ContentTemplatesTable, contentTemplateEntity);
				_context.SaveChangesStandard();

				return new ContentTemplate(name, content, this);
			}
			catch(Exception ex) {
				throw ex;
			}
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
			if(content == null) throw new ArgumentNullException("content");
			if(name.Length == 0) throw new ArgumentException("name");

			try {
				ContentTemplateEntity contentTemplateEntity = GetContentTemplateEntity(_wiki, name);
				if(contentTemplateEntity == null) return null;

				contentTemplateEntity.RowKey = name;
				contentTemplateEntity.Content = content;

				_context.UpdateObject(contentTemplateEntity);
				_context.SaveChangesStandard();

				return new ContentTemplate(name, content, this);
			}
			catch(Exception ex) {
				throw ex;
			}
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
			if(name.Length == 0) throw new ArgumentException("name");

			try {
				ContentTemplateEntity contentTemplateEntity = GetContentTemplateEntity(_wiki, name);
				if(contentTemplateEntity == null) return false;

				_context.DeleteObject(contentTemplateEntity);
				_context.SaveChangesStandard();

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		#endregion

		#region IStorageProviderV30 Members

		public bool ReadOnly {
			get { return false; }
		}

		#endregion

		#region IProviderV30 Members


		/// <summary>
		/// The namespaces table name.
		/// </summary>
		public static readonly string NamespacesTable = "Namespaces";

		/// <summary>
		/// The categories table name.
		/// </summary>
		public static readonly string CategoriesTable = "Categories";

		/// <summary>
		/// The page info table name.
		/// </summary>
		public static readonly string PagesInfoTable = "PagesInfo";

		/// <summary>
		/// The page content table name.
		/// </summary>
		public static readonly string PagesContentsTable = "PagesContents";

		/// <summary>
		/// The messages table name.
		/// </summary>
		public static readonly string MessagesTable = "Messages";

		/// <summary>
		/// The navigation paths table name.
		/// </summary>
		public static readonly string NavigationPathsTable = "NavigationPaths";

		/// <summary>
		/// The snippets table name.
		/// </summary>
		public static readonly string SnippetsTable = "Snippets";

		/// <summary>
		/// The contentTemplates table name.
		/// </summary>
		public static readonly string ContentTemplatesTable = "ContentTemplates";

		/// <summary>
		/// The IndexWordMapping table name.
		/// </summary>
		public static readonly string IndexWordMappingTable = "IndexWordMapping";

		/// <summary>
		/// Gets the wiki that has been used to initialize the current instance of the provider.
		/// </summary>
		public string CurrentWiki {
			get { return _wiki; }
		}

		/// <summary>
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <param name="wiki">The wiki.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void Init(IHostV40 host, string config, string wiki) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			_host = host;
			_wiki = string.IsNullOrEmpty(wiki) ? "root" : wiki.ToLowerInvariant();
			string[] connectionStrings = config.Split(new char[] { '|' });
			if(connectionStrings == null || connectionStrings.Length != 2) throw new InvalidConfigurationException("The given connections string is invalid.");

			_context = TableStorage.GetContext(connectionStrings[0], connectionStrings[1]);

			index = new AzureIndex(new IndexConnector(GetWordFetcher, GetSize, GetCount, ClearIndex, DeleteDataForDocument, SaveDataForDocument, TryFindWord));
		}

		/// <summary>
		/// Sets up the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="host"/> or <paramref name="config"/> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <paramref name="config"/> is not valid or is incorrect.</exception>
		public void SetUp(IHostV40 host, string config) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			string[] connectionStrings = config.Split(new char[] { '|' });
			if(connectionStrings == null || connectionStrings.Length != 2) throw new InvalidConfigurationException("The given connections string is invalid.");

			TableStorage.CreateTable(connectionStrings[0], connectionStrings[1], NamespacesTable);
			TableStorage.CreateTable(connectionStrings[0], connectionStrings[1], CategoriesTable);
			TableStorage.CreateTable(connectionStrings[0], connectionStrings[1], PagesInfoTable);
			TableStorage.CreateTable(connectionStrings[0], connectionStrings[1], PagesContentsTable);
			TableStorage.CreateTable(connectionStrings[0], connectionStrings[1], MessagesTable);
			TableStorage.CreateTable(connectionStrings[0], connectionStrings[1], NavigationPathsTable);
			TableStorage.CreateTable(connectionStrings[0], connectionStrings[1], SnippetsTable);
			TableStorage.CreateTable(connectionStrings[0], connectionStrings[1], ContentTemplatesTable);
			TableStorage.CreateTable(connectionStrings[0], connectionStrings[1], IndexWordMappingTable);
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return new ComponentInformation("Azure Table Storage Pages Storage Provider", "Threeplicate Srl", _host.GetGlobalSettingValue(GlobalSettingName.WikiVersion), "", ""); }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml {
			get { return ""; }
		}

		#endregion

		#region IDisposable Members

		public void Dispose() {
			// Nothing todo
		}

		#endregion
	}

	internal class NamespacesEntity : TableServiceEntity {
		// PartitionKey = wiki
		// RowKey = namespaceName

		public string DefaultPageFullName { get; set; }
	}

	internal class CategoriesEntity : TableServiceEntity {
		// PartitionKey = wiki
		// RowKey = categoryFullName

		public string Namespace { get; set; }
		public string PagesFullNames { get; set; }
	}

	internal class PagesInfoEntity : TableServiceEntity {
		// PartitionKey = wiki
		// RowKey = pageFullName

		public string Namespace { get; set; }
		public DateTime CreationDateTime { get; set; }
		public string PageId { get; set; }
	}

	internal class PagesContentsEntity : TableServiceEntity {
		// PartitionKey = pageId
		// RowKey = revision

		public string Title { get; set; }
		public string User { get; set; }
		public DateTime LastModified { get; set; }
		public string Comment { get; set; }
		public string Content { get; set; }
		public string Description { get; set; }
		public string Keywords { get; set; }    // Separated by '|' (pipe)
	}

	internal class MessageEntity : TableServiceEntity {
		// PartitionKey = pageId
		// RowKey = GUID

		public string Username { get; set; }
		public string Subject { get; set; }
		public DateTime DateTime { get; set; }
		public string Body { get; set; }
		public string ParetnId { get; set; }   // "-1" if the message has no parent
	}

	internal class NavigationPathEntity : TableServiceEntity {
		// PartitionKey = wiki
		// RowKey = fullName

		public string Namespace { get; set; }
		public string PagesFullNames { get; set; }    // Separated by '|' (pipe)
	}

	internal class SnippetEntity : TableServiceEntity {
		// PartitionKey = wiki
		// RowKey = snippetName

		public string Content { get; set; }
	}

	internal class ContentTemplateEntity : TableServiceEntity {
		// PartitionKey = wiki
		// RowKey = snippetName

		public string Content { get; set; }
	}

	internal class IndexWordMappingEntity : TableServiceEntity {
		// PartitionKey = wiki
		// RowKey = documentName|word|location|wordIndex

		public string Word { get; set; }
		public string DocumentName { get; set; }
		public int FirstCharIndex { get; set; }
		public int Location { get; set; }
		public string DocumentTitle { get; set; }
		public string TypeTag { get; set; }
		public DateTime DateTime { get; set; }
	}

	internal class IndexWordMappingEntityComparerByDocumentName : IEqualityComparer<IndexWordMappingEntity> {

		public bool Equals(IndexWordMappingEntity x, IndexWordMappingEntity y) {
			//Check whether the compared objects reference the same data.
			if(Object.ReferenceEquals(x, y)) return true;

			//Check whether any of the compared objects is null.
			if(Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
				return false;

			//Check whether the entities DocumentName property are equal.
			return x.DocumentName == y.DocumentName;
		}

		public int GetHashCode(IndexWordMappingEntity entity) {
			//Check whether the object is null
			if(Object.ReferenceEquals(entity, null)) return 0;

			//Get hash code for the DocumentName field if it is not null.
			int hashDocumentName = entity.DocumentName == null ? 0 : entity.DocumentName.GetHashCode();

			return hashDocumentName;
		}

	}

	internal class IndexWordMappingEntityComparerByWord : IEqualityComparer<IndexWordMappingEntity> {

		public bool Equals(IndexWordMappingEntity x, IndexWordMappingEntity y) {
			//Check whether the compared objects reference the same data.
			if(Object.ReferenceEquals(x, y)) return true;

			//Check whether any of the compared objects is null.
			if(Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
				return false;

			//Check whether the entities DocumentName property are equal.
			return x.Word == y.Word;
		}

		public int GetHashCode(IndexWordMappingEntity entity) {
			//Check whether the object is null
			if(Object.ReferenceEquals(entity, null)) return 0;

			//Get hash code for the DocumentName field if it is not null.
			int hashWord = entity.Word == null ? 0 : entity.Word.GetHashCode();

			return hashWord;
		}

	}

}
