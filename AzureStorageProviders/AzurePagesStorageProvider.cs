
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using Microsoft.WindowsAzure.StorageClient;

namespace ScrewTurn.Wiki.Plugins.AzureStorage {

	/// <summary>
	/// Implements a Pages Storage Provider.
	/// </summary>
	public class AzurePagesStorageProvider :IPagesStorageProviderV40 {

		private IHostV40 _host;
		private string _wiki;

		private TableServiceContext _context;
		private CloudBlobContainer _containerRef;

		private const string CurrentRevision = "CurrentRevision";
		private const string Draft = "Draft";

		#region IPagesStorageProviderV40 Members

		#region Namespaces

		private Dictionary<string, NamespacesEntity> _namespaces;
		private List<NamespacesEntity> _namespacesList;

		private List<NamespacesEntity> GetNamespacesEntities(string wiki) {
			if(_namespacesList == null) {
				var query = (from e in _context.CreateQuery<NamespacesEntity>(NamespacesTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(wiki)
							 select e).AsTableServiceQuery();
				_namespacesList = QueryHelper<NamespacesEntity>.All(query).ToList<NamespacesEntity>();

				_namespaces = new Dictionary<string, NamespacesEntity>();
				foreach(var entity in _namespacesList) {
					_namespaces[entity.RowKey] = entity;
				}
			}
			return _namespacesList;
		}

		private NamespacesEntity GetNamespacesEntity(string wiki, string namespaceName) {
			if(_namespaces == null) _namespaces = new Dictionary<string,NamespacesEntity>();
			
			if(!_namespaces.ContainsKey(namespaceName)) {	
				var query = (from e in _context.CreateQuery<NamespacesEntity>(NamespacesTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(_wiki) && e.RowKey.Equals(namespaceName)
							 select e).AsTableServiceQuery();
				var entity = QueryHelper<NamespacesEntity>.FirstOrDefault(query);
				if(entity == null) return null;

				_namespaces[namespaceName] = entity;
			}

			return _namespaces[namespaceName];
		}

		// Invalidate namespaces temporary cache
		private void InvalidateNamespacesTempCache() {
			_namespaces = null;
			_namespacesList = null;
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
				return new NamespaceInfo(entity.RowKey, this, entity.DefaultPageFullName);
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
				var entities = GetNamespacesEntities(_wiki);
				if(entities == null) return null;

				List<NamespaceInfo> namespaces = new List<NamespaceInfo>(entities.Count);
				foreach(NamespacesEntity entity in entities) {
					namespaces.Add(new NamespaceInfo(entity.RowKey, this, entity.DefaultPageFullName));
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

				// Invalidate local cache.
				InvalidateNamespacesTempCache();

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
				List<PagesContentsEntity> pagesInNamespace = GetAllPagesContentsEntities(_wiki, nspace.Name);

				int count = 0;
				foreach(PagesContentsEntity entity in pagesInNamespace) {
					string newPageFullName = NameTools.GetFullName(newName, NameTools.GetLocalName(entity.PageFullName));
					IList<PagesContentsEntity> allRevisions = GetPagesContentEntities(_wiki, entity.PageFullName);
					foreach(PagesContentsEntity revision in allRevisions) {
						PagesContentsEntity newPage = new PagesContentsEntity() {
							PartitionKey = revision.PartitionKey,
							RowKey = newPageFullName + "|" + revision.Revision,
							PageFullName = newPageFullName,
							Revision = revision.Revision,
							Namespace = newName,
							CreationDateTime = revision.CreationDateTime,
							Comment = revision.Comment,
							Content = revision.Content,
							Description = revision.Description,
							Keywords = revision.Keywords,
							LastModified = revision.LastModified,
							Title = revision.Title,
							User = revision.User,
							BlobReference = revision.BlobReference
						};
						_context.DeleteObject(revision);
						_context.AddObject(PagesContentsTable, newPage);
						count = count + 2;
						if(count >= 98) {
							_context.SaveChangesStandard();
							count = 0;
						}
					}
				}
				if(count > 0) _context.SaveChangesStandard();

				foreach(PagesContentsEntity entity in pagesInNamespace) {
					string newPageFullName = NameTools.GetFullName(newName, NameTools.GetLocalName(entity.PageFullName));
					IList<MessageEntity> messages = GetMessagesEntities(_wiki, entity.PageFullName);
					count = 0;
					foreach(MessageEntity message in messages) {
						MessageEntity newMessage = new MessageEntity() {
							PartitionKey = _wiki + "|" + newPageFullName,
							RowKey = message.RowKey,
							Body = message.Body,
							DateTime = message.DateTime,
							ParetnId = message.ParetnId,
							Subject = message.Subject,
							Username = message.Username
						};
						_context.AddObject(MessagesTable, newMessage);
						count++;
						if(count >= 99) {
							_context.SaveChangesStandard();
							count = 0;
						}
					}
					if(count > 0) _context.SaveChangesStandard();

					count = 0;
					foreach(MessageEntity message in messages) {
						_context.DeleteObject(message);
						count++;
						if(count >= 99) {
							_context.SaveChangesStandard();
							count = 0;
						}
					}
					if(count > 0) _context.SaveChangesStandard();
				}

				// Invalidate temporaty page content cache
				InvalidatePagesTempCache();

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

				// Invalidate local cache.
				InvalidateNamespacesTempCache();

				return new NamespaceInfo(newName, this, newNamespaceEntity.DefaultPageFullName);
			}
			catch(Exception ex) {
				throw ex;
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

			try {
				var entity = GetNamespacesEntity(_wiki, nspace.Name);
				// If namespace does not exists return null
				if(entity == null) return null;

				// If the page does not exists return null
				if(pageFullName != null && GetPage(pageFullName) == null) return null;

				entity.DefaultPageFullName = pageFullName != null ? pageFullName : null;
				_context.UpdateObject(entity);
				_context.SaveChangesStandard();

				// Invalidate local cache.
				InvalidateNamespacesTempCache();

				nspace.DefaultPageFullName = pageFullName;
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
					RemovePage(pageInNamespace.FullName);
				}

				_context.DeleteObject(nspaceEntity);
				_context.SaveChangesStandard();

				// Invalidate local cache.
				InvalidateNamespacesTempCache();

				return true;
			}
			catch(Exception ex) {
				throw ex;
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
			if(pageFullName == null) throw new ArgumentNullException("pageFullName");
			if(pageFullName.Length == 0) throw new ArgumentException("pageFullName");

			try {
				List<PagesContentsEntity> oldPagesContentsEntities = GetPagesContentEntities(_wiki, pageFullName);
				if(oldPagesContentsEntities == null || oldPagesContentsEntities.Count == 0) return null;

				string destinationName = destination != null ? destination.Name : null;

				string currentNsName = NameTools.GetNamespace(pageFullName);
				NamespaceInfo currentNs = currentNsName != null ? GetNamespace(currentNsName) : null;
				NamespaceComparer nsComp = new NamespaceComparer();
				if((currentNs == null && destination == null) || nsComp.Compare(currentNs, destination) == 0) return null;

				if(GetPage(NameTools.GetFullName(destinationName, NameTools.GetLocalName(pageFullName))) != null) return null;
				if(destination != null && GetNamespacesEntity(_wiki, destinationName) == null) return null;

				if(currentNs != null && currentNs.DefaultPageFullName != null) {
					// Cannot move the default page
					if(currentNs.DefaultPageFullName.ToLowerInvariant() == pageFullName.ToLowerInvariant()) return null;
				}

				string newPageFullName = NameTools.GetFullName(destinationName, NameTools.GetLocalName(pageFullName));

				List<CategoryInfo> pageCategories = GetCategoriesForPage(pageFullName).ToList();
				foreach(CategoryInfo category in pageCategories) {
					CategoriesEntity categoryEntity = GetCategoriesEntity(_wiki, category.FullName);
					categoryEntity.PagesFullNames = string.Join("|", categoryEntity.PagesFullNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList().Remove(pageFullName));
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
				
				PageContent page = null;

				// Update all page versions with values from the new namespace
				int count = 0;
				foreach(PagesContentsEntity entity in oldPagesContentsEntities) {
					PagesContentsEntity newPageEntity = new PagesContentsEntity() {
						PartitionKey = entity.PartitionKey,
						RowKey = newPageFullName + "|" + entity.Revision,
						Comment = entity.Comment,
						Content = entity.Content,
						CreationDateTime = entity.CreationDateTime,
						Description = entity.Description,
						Keywords = entity.Keywords,
						LastModified = entity.LastModified,
						Namespace = destinationName,
						PageFullName = newPageFullName,
						Revision = entity.Revision,
						Title = entity.Title,
						User = entity.User,
						BlobReference = entity.BlobReference,
						BigContent = entity.BigContent
					};
					if(entity.Revision == CurrentRevision) {
						page = BuildPageContent(newPageEntity);
					}
					_context.DeleteObject(entity);
					_context.AddObject(PagesContentsTable, newPageEntity);
					count = count + 2;
					if(count >= 98) {
						_context.SaveChangesStandard();
						count = 0;
					}
				}
				if(count > 0) _context.SaveChangesStandard();

				IList<MessageEntity> messagesEntities = GetMessagesEntities(_wiki, pageFullName);
				count = 0;
				foreach(MessageEntity message in messagesEntities) {
					MessageEntity newMessage = new MessageEntity() {
						PartitionKey = _wiki + "|" + newPageFullName,
						RowKey = message.RowKey,
						Body = message.Body,
						DateTime = message.DateTime,
						ParetnId = message.ParetnId,
						Subject = message.Subject,
						Username = message.Username
					};
					_context.AddObject(MessagesTable, newMessage);
					count++;
					if(count >= 99) {
						_context.SaveChangesStandard();
						count = 0;
					}
				}
				if(count > 0) _context.SaveChangesStandard();

				count = 0;
				foreach(MessageEntity message in messagesEntities) {
					_context.DeleteObject(message);
					count++;
					if(count >= 99) {
						_context.SaveChangesStandard();
						count = 0;
					}
				}
				if(count > 0) _context.SaveChangesStandard();

				// Invalidate pageContent temporary cache 
				InvalidatePagesTempCache();

				return page;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		#endregion

		#region Categories

		private CategoriesEntity GetCategoriesEntity(string wiki, string categoryFullName) {
			var query = (from e in _context.CreateQuery<CategoriesEntity>(CategoriesTable).AsTableServiceQuery()
						 where e.PartitionKey.Equals(_wiki) && e.RowKey.Equals(categoryFullName)
						 select e).AsTableServiceQuery();
			return QueryHelper<CategoriesEntity>.FirstOrDefault(query);
		}

		private List<CategoriesEntity> GetCategoriesEntities(string wiki) {
			var query = (from e in _context.CreateQuery<CategoriesEntity>(CategoriesTable).AsTableServiceQuery()
						 where e.PartitionKey.Equals(_wiki)
						 select e).AsTableServiceQuery();
			return QueryHelper<CategoriesEntity>.All(query).ToList();
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
		/// <param name="pageFullName">The full name of the page.</param>
		/// <returns>The categories, sorted by name.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		public CategoryInfo[] GetCategoriesForPage(string pageFullName) {
			if(pageFullName == null) throw new ArgumentNullException("page");

			try {
				var categoriesEntities = GetCategoriesEntities(_wiki);

				List<CategoryInfo> categories = new List<CategoryInfo>();
				foreach(CategoriesEntity categoryEntity in categoriesEntities) {
					if(categoryEntity.PagesFullNames != null && categoryEntity.PagesFullNames.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries).ToList<string>().Contains(pageFullName)) {
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

		#endregion

		#region Pages

		// wiki -> (pageFullName|revision -> pageContentEntity)
		private Dictionary<string, Dictionary<string, PagesContentsEntity>> _pagesContentCache;

		// A list containing the wiki|FullName of the pages for wich all versions have been retrieved
		private List<string> allVersionRetrieved = new List<string>();

		// if true allPagesContent has been called and doesn't need to be called again
		private bool allPagesContentRetrieved = false;

		private PagesContentsEntity GetPagesContentsEntityByRevision(string wiki, string pageFullName, string revision) {
			if(_pagesContentCache == null) _pagesContentCache = new Dictionary<string, Dictionary<string, PagesContentsEntity>>();
			if(!_pagesContentCache.ContainsKey(wiki)) _pagesContentCache[wiki] = new Dictionary<string, PagesContentsEntity>();

			if(!_pagesContentCache[wiki].ContainsKey(pageFullName + "|" + revision)) {
				var query = (from e in _context.CreateQuery<PagesContentsEntity>(PagesContentsTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(wiki) && e.RowKey.Equals(pageFullName + "|" + revision)
							 select e).AsTableServiceQuery();
				var entity = QueryHelper<PagesContentsEntity>.FirstOrDefault(query);
				if(entity == null) {
					return null;
				}
				else {
					if(entity.BlobReference != null) {
						CloudBlob blob = _containerRef.GetBlobReference(entity.BlobReference);
						entity.BigContent = blob.DownloadText();
					}
					_pagesContentCache[wiki][pageFullName + "|" + revision] = entity;
				}
			}
			return _pagesContentCache[wiki][pageFullName + "|" + revision];
		}

		private List<PagesContentsEntity> GetPagesContentEntities(string wiki, string pageFullName) {
			if(!allVersionRetrieved.Contains(wiki + "|" + pageFullName)) {
				// pagesContents not present in temp local cache retrieve from table storage
				if(_pagesContentCache == null) _pagesContentCache = new Dictionary<string, Dictionary<string, PagesContentsEntity>>();
				if(!_pagesContentCache.ContainsKey(wiki)) _pagesContentCache[wiki] = new Dictionary<string, PagesContentsEntity>();
				allVersionRetrieved.Add(wiki + "|" + pageFullName);

				var query = (from e in _context.CreateQuery<PagesContentsEntity>(PagesContentsTable).AsTableServiceQuery()
							 where e.PartitionKey.Equals(wiki) && e.PageFullName.Equals(pageFullName)
							 select e).AsTableServiceQuery();
				var entities = QueryHelper<PagesContentsEntity>.All(query);

				foreach(PagesContentsEntity entity in entities) {
					if(entity.BlobReference != null) {
						CloudBlob blob = _containerRef.GetBlobReference(entity.BlobReference);
						entity.BigContent = blob.DownloadText();
					}
					_pagesContentCache[wiki][entity.RowKey] = entity;
				}
			}

			return (from p in _pagesContentCache[wiki].Values
					where p.PageFullName == pageFullName
					select p).ToList();
		}

		private List<PagesContentsEntity> GetAllPagesContentsEntities(string wiki, string nspace) {
			if(!(allPagesContentRetrieved && _pagesContentCache != null && _pagesContentCache.ContainsKey(wiki))) {
				// pagesContents not present in temp local cache retrieve from table storage
				if(_pagesContentCache == null) _pagesContentCache = new Dictionary<string, Dictionary<string, PagesContentsEntity>>();
				if(!_pagesContentCache.ContainsKey(wiki)) _pagesContentCache[wiki] = new Dictionary<string, PagesContentsEntity>();
				allPagesContentRetrieved = true;

				CloudTableQuery<PagesContentsEntity> query = (from e in _context.CreateQuery<PagesContentsEntity>(PagesContentsTable).AsTableServiceQuery()
															  where e.PartitionKey.Equals(wiki) && e.Revision.Equals(CurrentRevision)
															  select e).AsTableServiceQuery();

				var entities = QueryHelper<PagesContentsEntity>.All(query);

				foreach(PagesContentsEntity entity in entities) {
					if(entity.BlobReference != null) {
						CloudBlob blob = _containerRef.GetBlobReference(entity.BlobReference);
						entity.BigContent = blob.DownloadText();
					}
					_pagesContentCache[wiki][entity.RowKey] = entity;
				}
			}

			return (from p in _pagesContentCache[wiki].Values
					where p.Namespace == nspace && p.Revision == CurrentRevision
					select p).ToList();
		}

		// Invalidate temporary pages local cache
		private void InvalidatePagesTempCache() {
			_pagesContentCache = null;
			allPagesContentRetrieved = false;
			allVersionRetrieved = new List<string>();
		}

		private void BuildPageContentEntity(PagesContentsEntity entity, string pageFullName, string revision, DateTime creationLocalDateTime, string title,
			string username, DateTime lastModificationLocalDateTime, string comment, string content, string[] keywords, string description) {

			BuildPageContentEntity(entity, pageFullName, revision, creationLocalDateTime.ToUniversalTime(), title, username, lastModificationLocalDateTime.ToUniversalTime(),
				comment, content, keywords != null ? string.Join("|", keywords) : null, description);
		}

		private void BuildPageContentEntity(PagesContentsEntity entity, string pageFullname, string revision, DateTime creationUniversalDateTime, string title,
			string username, DateTime lastModificationUniversalDateTime, string comment, string content, string keywords, string description) {

			if(entity == null) entity = new PagesContentsEntity();

			entity.PartitionKey = _wiki;
			entity.RowKey = pageFullname + "|" + revision;
			entity.Comment = comment;
			entity.CreationDateTime = creationUniversalDateTime;
			entity.Description = description;
			entity.Keywords = keywords;
			entity.LastModified = lastModificationUniversalDateTime;
			entity.Namespace = NameTools.GetNamespace(pageFullname);
			entity.PageFullName = pageFullname;
			entity.Revision = revision;
			entity.Title = title;
			entity.User = username;

			int count = Encoding.UTF8.GetByteCount(content);

			if(Encoding.UTF8.GetByteCount(content) > 60 * 1024) {
				string blobName = Guid.NewGuid().ToString("N");
				CloudBlob blob = _containerRef.GetBlobReference(blobName);
				blob.UploadText(content);
				entity.BlobReference = blobName;
				entity.BigContent = content;
			}
			else {
				entity.Content = content;
			}
		}

		private void DeleteOldBlobReference(string blobReference) {
			_containerRef.GetBlobReference(blobReference).DeleteIfExists();
		}

		private PageContent BuildPageContent(PagesContentsEntity entity) {
			PageContent page = new PageContent(
				entity.PageFullName,
				this,
				entity.CreationDateTime.ToLocalTime(),
				entity.Title,
				entity.User,
				entity.LastModified.ToLocalTime(),
				string.IsNullOrEmpty(entity.Comment) ? "" : entity.Comment,
				entity.BlobReference != null ? entity.BigContent : entity.Content,
				entity.Keywords == null ? new string[0] : entity.Keywords.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries),
				string.IsNullOrEmpty(entity.Description) ? null : entity.Description);

			return page;
		}

		/// <summary>
		/// Gets a page.
		/// </summary>
		/// <param name="fullName">The full name of the page.</param>
		/// <returns>The <see cref="T:PageContent"/>, or <c>null</c> if no page is found.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		public PageContent GetPage(string fullName) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("fullName");

			try {
				var entity = GetPagesContentsEntityByRevision(_wiki, fullName, CurrentRevision);
				if(entity == null) return null;

				return BuildPageContent(entity);
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
		public PageContent[] GetPages(NamespaceInfo nspace) {
			try {
				var entities = GetAllPagesContentsEntities(_wiki, nspace == null ? null : nspace.Name);

				return (from e in entities
						select BuildPageContent(e)).ToArray();
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
		public PageContent[] GetUncategorizedPages(NamespaceInfo nspace) {
			lock(this) {
				PageContent[] pages = GetPages(nspace);
				CategoryInfo[] categories = GetCategories(nspace);

				return (from p in pages
						where !(from c in categories
								where c.Pages.Contains(p.FullName)
								select c).Any()
						select p).ToArray();

				//List<PageContent> result = new List<PageContent>(pages.Length);

				//foreach(PageContent p in pages) {
				//    bool found = false;
				//    foreach(CategoryInfo c in categories) {
				//        foreach(string name in c.Pages) {
				//            if(StringComparer.OrdinalIgnoreCase.Compare(name, p.FullName) == 0) {
				//                found = true;
				//                break;
				//            }
				//        }
				//    }
				//    if(!found) result.Add(p);
				//}

				//return result.ToArray();
			}
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

			try {
				// Find the associated draft PageContent; if not found return an empty PageContent
				var pageContentEntity = GetPagesContentsEntityByRevision(_wiki, fullName, Draft);
				if(pageContentEntity == null) return null;

				return BuildPageContent(pageContentEntity);
			}
			catch(Exception ex) {
				throw ex;
			}
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

			try {
				// Find the associated draft PageContent; if not found return an empty PageContent
				var pageContentEntity = GetPagesContentsEntityByRevision(_wiki, fullName, Draft);
				if(pageContentEntity == null) return false;

				if(pageContentEntity.BlobReference != null) DeleteOldBlobReference(pageContentEntity.BlobReference);
				_context.DeleteObject(pageContentEntity);
				_context.SaveChangesStandard();

				// Invalidate pagesContentCache
				InvalidatePagesTempCache();

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		/// <summary>
		/// Gets the Backup/Revision numbers of a Page.
		/// </summary>
		/// <param name="fullName">The full name of the page to get the Backups of.</param>
		/// <returns>The Backup/Revision numbers.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="fullName"/> is empty.</exception>
		public int[] GetBackups(string fullName) {
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("fullName");

			try {
				// Find the PageContent; if not found return null
				var pageContentEntities = GetPagesContentEntities(_wiki, fullName);
				if(pageContentEntities == null || pageContentEntities.Count == 0) return null;

				List<int> revisions = new List<int>(pageContentEntities.Count);
				foreach(PagesContentsEntity pageContentEntity in pageContentEntities) {
					if(pageContentEntity.RowKey != CurrentRevision && pageContentEntity.RowKey != Draft) {
						int rev;
						if(int.TryParse(pageContentEntity.Revision, out rev)) revisions.Add(rev);
					}
				}
				revisions.Sort();
				return revisions.ToArray();
			}
			catch(Exception ex) {
				throw ex;
			}
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
			if(revision < 0) throw new ArgumentOutOfRangeException("revision");

			try {
				// Find the PageContent; if not found return null
				var pageContentEntity = GetPagesContentsEntityByRevision(_wiki, fullName, revision.ToString());
				if(pageContentEntity == null) return null;

				return BuildPageContent(pageContentEntity);
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
				// Find the Page; if not found return null
				if(GetPage(content.FullName) == null) return false;

				var pageContentEntity = GetPagesContentsEntityByRevision(_wiki, content.FullName, revision.ToString());
				if(pageContentEntity != null) {
					_context.UpdateObject(pageContentEntity);
					if(pageContentEntity.BlobReference != null) {
						DeleteOldBlobReference(pageContentEntity.BlobReference);
					}
				}
				else {
					pageContentEntity = new PagesContentsEntity();
					_context.AddObject(PagesContentsTable, pageContentEntity);
				}
				BuildPageContentEntity(pageContentEntity, content.FullName, revision.ToString(), content.CreationDateTime, content.Title, content.User, content.LastModified, content.Comment, content.Content, content.Keywords, content.Description);
				
				_context.SaveChangesStandard();

				// Invalidate pagesContetnCache
				InvalidatePagesTempCache();

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
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
			if(fullName == null) throw new ArgumentNullException("fullName");
			if(fullName.Length == 0) throw new ArgumentException("fullName");
			if(newName == null) throw new ArgumentNullException("newName");
			if(newName.Length == 0) throw new ArgumentException("newName");

			try {
				List<PagesContentsEntity> allOldRevisions = GetPagesContentEntities(_wiki, fullName);
				if(allOldRevisions == null || allOldRevisions.Count == 0) return null; // Inexistent source page

				NamespaceInfo currentNs = NameTools.GetNamespace(fullName) != null ? GetNamespace(NameTools.GetNamespace(fullName)) : null;
				if(currentNs != null && currentNs.DefaultPageFullName != null) {
					// Cannot rename the default page
					if(currentNs.DefaultPageFullName.ToLowerInvariant() == fullName.ToLowerInvariant()) return null;
				}

				string newPageFullName = NameTools.GetFullName(NameTools.GetNamespace(fullName), newName);

				if(GetPage(newPageFullName) != null) return null;

				CategoryInfo[] categoriesInfo = GetCategoriesForPage(fullName);
				foreach(CategoryInfo categoryInfo in categoriesInfo) {
					CategoriesEntity categoryEntity = GetCategoriesEntity(_wiki, categoryInfo.FullName);
					string[] oldNamesCategoryPages = categoryEntity.PagesFullNames != null ? categoryEntity.PagesFullNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries) : new string[0];
					List<string> newNamesCategoryPages = new List<string>(oldNamesCategoryPages.Length);
					foreach(string oldName in oldNamesCategoryPages) {
						newNamesCategoryPages.Add(NameTools.GetFullName(NameTools.GetNamespace(fullName), newName));
					}
					categoryEntity.PagesFullNames = string.Join("|", newNamesCategoryPages);
					_context.UpdateObject(categoryEntity);
				}
				_context.SaveChangesStandard();

				List<NavigationPathEntity> navigationPathEntities = GetNavigationPathEntities(_wiki, NameTools.GetNamespace(fullName));
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

				PageContent currentContent = GetPage(fullName);

				int count = 0;
				foreach(PagesContentsEntity entity in allOldRevisions) {
					PagesContentsEntity newEntity = new PagesContentsEntity() {
						PartitionKey = entity.PartitionKey,
						RowKey = newPageFullName + "|" + entity.Revision,
						PageFullName = newPageFullName,
						Namespace = entity.Namespace,
						Revision = entity.Revision,
						CreationDateTime = entity.CreationDateTime,
						Title = entity.Title,
						User = entity.User,
						LastModified = entity.LastModified,
						Comment = entity.Comment,
						Content = entity.Content,
						Keywords = entity.Keywords,
						Description = entity.Description,
						BlobReference = entity.BlobReference
					};
					_context.DeleteObject(entity);
					_context.AddObject(PagesContentsTable, newEntity);
					count = count + 2;
					if(count >= 98) {
						_context.SaveChangesStandard();
						count = 0;
					}
				}
				if(count > 0) _context.SaveChangesStandard();

				IList<MessageEntity> messagesEntities = GetMessagesEntities(_wiki, fullName);
				count = 0;
				foreach(MessageEntity message in messagesEntities) {
					MessageEntity newMessage = new MessageEntity() {
						PartitionKey = _wiki + "|" + newPageFullName,
						RowKey = message.RowKey,
						Body = message.Body,
						DateTime = message.DateTime,
						ParetnId = message.ParetnId,
						Subject = message.Subject,
						Username = message.Username
					};
					_context.AddObject(MessagesTable, newMessage);
					count++;
					if(count >= 99) {
						_context.SaveChangesStandard();
						count = 0;
					}
				}
				if(count > 0) _context.SaveChangesStandard();

				count = 0;
				foreach(MessageEntity message in messagesEntities) {
					_context.DeleteObject(message);
					count++;
					if(count >= 99) {
						_context.SaveChangesStandard();
						count = 0;
					}
				}
				if(count > 0) _context.SaveChangesStandard();

				PageContent newCurrentContent = new PageContent(
					newPageFullName,
					this,
					currentContent.CreationDateTime,
					currentContent.Title,
					currentContent.User,
					currentContent.LastModified,
					currentContent.Comment,
					currentContent.Content,
					currentContent.Keywords,
					currentContent.Description);
				
				// Invalidate local cache
				InvalidatePagesTempCache();

				return newCurrentContent;
			}
			catch(Exception ex) {
				throw ex;
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

			if(pageName == null) throw new ArgumentNullException("pageName");
			if(pageName.Length == 0) throw new ArgumentException("pageName");
			if(title == null) throw new ArgumentNullException("title");
			if(username == null) throw new ArgumentNullException("username");
			if(content == null) throw new ArgumentNullException("content");
			if(title.Length == 0) throw new ArgumentException("title");
			if(username.Length == 0) throw new ArgumentException("username");

			try {
				switch(saveMode) {
					case SaveMode.Draft:
						// Find the "Draft" PageContent if present
						var draftPageContentEntity = GetPagesContentsEntityByRevision(_wiki, pageName, Draft);
						if(draftPageContentEntity == null) {
							draftPageContentEntity = new PagesContentsEntity();
							_context.AddObject(PagesContentsTable, draftPageContentEntity);
						}
						else {
							_context.UpdateObject(draftPageContentEntity);
							if(draftPageContentEntity.BlobReference != null) {
								DeleteOldBlobReference(draftPageContentEntity.BlobReference);
							}
						}
						BuildPageContentEntity(draftPageContentEntity, NameTools.GetFullName(nspace, pageName), Draft, creationDateTime, title, username, dateTime, comment, content, keywords, description);

						_context.SaveChangesStandard();

						// Invalidate pagesContetnCache
						InvalidatePagesTempCache();

						return BuildPageContent(draftPageContentEntity);
					default:
						// Find the "CurrentRevision" PageContent if present
						var currentPageContentEntity = GetPagesContentsEntityByRevision(_wiki, NameTools.GetFullName(nspace, pageName), CurrentRevision);

						// If currentPageContent is not found the page has never been saved and has no content to backup
						if(currentPageContentEntity != null) {
							if(saveMode == SaveMode.Backup) {
								int[] backupsRevisions = GetBackups(NameTools.GetFullName(nspace, pageName));
								int rev = (backupsRevisions.Length > 0 ? backupsRevisions[backupsRevisions.Length - 1] + 1 : 0);
								PagesContentsEntity backupPageContentEntity = new PagesContentsEntity();
								BuildPageContentEntity(backupPageContentEntity, currentPageContentEntity.PageFullName, rev.ToString(), currentPageContentEntity.CreationDateTime, currentPageContentEntity.Title, currentPageContentEntity.User, currentPageContentEntity.LastModified, currentPageContentEntity.Comment, currentPageContentEntity.BlobReference != null ? currentPageContentEntity.BigContent : currentPageContentEntity.Content, currentPageContentEntity.Keywords, currentPageContentEntity.Description);

								backupPageContentEntity.BlobReference = currentPageContentEntity.BlobReference;
								_context.AddObject(PagesContentsTable, backupPageContentEntity);
								_context.SaveChangesStandard();
							}
							else {
								if(currentPageContentEntity.BlobReference != null) DeleteOldBlobReference(currentPageContentEntity.BlobReference);
							}

							_context.UpdateObject(currentPageContentEntity);
						}
						else {
							currentPageContentEntity = new PagesContentsEntity();
							_context.AddObject(PagesContentsTable, currentPageContentEntity);
						}
						BuildPageContentEntity(currentPageContentEntity, NameTools.GetFullName(nspace, pageName), CurrentRevision, creationDateTime, title, username, dateTime, comment, content, keywords, description);

						_context.SaveChangesStandard();

						PageContent newPageContent = BuildPageContent(currentPageContentEntity);

						// Invalidate pagesContetnCache
						InvalidatePagesTempCache();

						return newPageContent;
				}
			}
			catch(Exception ex) {
				throw ex;
			}
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
			if(revision < 0) throw new ArgumentOutOfRangeException("revision");

			PageContent currentContent = GetPage(pageFullName);
			if(currentContent == null) return false;
			
			// Get the pageContet at the specified revision
			PageContent rollbackPageContent = GetBackupContent(pageFullName, revision);
			// If rollbackPageContent is null, the page does not exists and return false
			if(rollbackPageContent == null) return false;

			// Save a new version of the page with the content at the given revision
			PageContent newPageContent = SetPageContent(NameTools.GetNamespace(pageFullName), NameTools.GetLocalName(pageFullName), rollbackPageContent.CreationDateTime, rollbackPageContent.Title, rollbackPageContent.User, rollbackPageContent.LastModified, rollbackPageContent.Comment, rollbackPageContent.Content, rollbackPageContent.Keywords, rollbackPageContent.Description, SaveMode.Backup);

			// Invalidate pagesContetnCache
			InvalidatePagesTempCache();

			return newPageContent != null;
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
			if(pageFullName == null) throw new ArgumentNullException("pageFullName");
			if(pageFullName.Length == 0) throw new ArgumentException("pageFullName");
			if(revision < -1) throw new ArgumentOutOfRangeException("revision");

			try {
				// Find the PageContent; if not found return false
				var pageContentQuery = (from e in _context.CreateQuery<PagesContentsEntity>(PagesContentsTable).AsTableServiceQuery()
										where e.PartitionKey.Equals(_wiki) && e.PageFullName.Equals(pageFullName) && !e.Revision.Equals(CurrentRevision) && !e.Revision.Equals(Draft)
										select e).AsTableServiceQuery();
				var pageContentEntities = QueryHelper<PagesContentsEntity>.All(pageContentQuery);
				if(pageContentEntities == null || pageContentEntities.Count == 0) return false;

				var pageContentEntityWithSpecifiedRevision = pageContentEntities.Any(c => c.Revision == revision.ToString());

				int idx = -1;
				if(revision != -1) {
					idx = pageContentEntityWithSpecifiedRevision ? revision : -1;
				}
				else {
					idx = pageContentEntities.Max((c) => { return int.Parse(c.Revision); });
				}


				// Operations
				// - Delete old beckups, from 0 to revision
				// - Rename newer backups starting from 0
				for(int i = 0; i <= idx; i++) {
					var pageContentEntity = pageContentEntities.First(c => c.Revision == i.ToString());
					_context.DeleteObject(pageContentEntity);
					if(pageContentEntity.BlobReference != null) DeleteOldBlobReference(pageContentEntity.BlobReference);
				}
				_context.SaveChangesStandard();

				if(revision != -1) {
					for(int i = revision + 1; i < pageContentEntities.Count; i++) {
						var pageContentEntity = pageContentEntities.First(c => c.Revision == i.ToString());
						if(pageContentEntity.BlobReference != null) pageContentEntity.BigContent = _containerRef.GetBlobReference(pageContentEntity.BlobReference).DownloadText();
						var newPageContentEntity = new PagesContentsEntity();
						BuildPageContentEntity(newPageContentEntity, pageFullName, (int.Parse(pageContentEntity.Revision) - revision - 1).ToString(), pageContentEntity.CreationDateTime, pageContentEntity.Title, pageContentEntity.User, pageContentEntity.LastModified, pageContentEntity.Comment, pageContentEntity.BlobReference != null ? pageContentEntity.BigContent : pageContentEntity.Content, pageContentEntity.Keywords, pageContentEntity.Description);
						
						newPageContentEntity.BlobReference = pageContentEntity.BlobReference;
						_context.DeleteObject(pageContentEntity);
						
						_context.AddObject(PagesContentsTable, newPageContentEntity);
						_context.SaveChangesStandard();
					}
				}

				// Invalidate pagesContetnCache
				InvalidatePagesTempCache();

				return true;
			}
			catch(Exception ex) {
				throw ex;
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
			if(pageFullName == null) throw new ArgumentNullException("pageFullName");
			if(pageFullName.Length == 0) throw new ArgumentException("pageFullName");
			
			try {
				NamespaceInfo currentNs = NameTools.GetNamespace(pageFullName) != null ? GetNamespace(NameTools.GetNamespace(pageFullName)) : null;
				if(currentNs != null && currentNs.DefaultPageFullName != null) {
					// Cannot remove the default page
					if(currentNs.DefaultPageFullName.ToLowerInvariant() == pageFullName.ToLowerInvariant()) return false;
				}

				// Find the PageContent; if not found return false
				var pageContentEntities = GetPagesContentEntities(_wiki, pageFullName);
				if(pageContentEntities == null || pageContentEntities.Count == 0) return false;

				// Delete all the pageContent including draft and backups
				bool deleteExecuted = false;
				foreach(PagesContentsEntity pageContentEntity in pageContentEntities) {
					if(pageContentEntity.BlobReference != null) DeleteOldBlobReference(pageContentEntity.BlobReference);
					_context.DeleteObject(pageContentEntity);
					deleteExecuted = true;
				}
				_context.SaveChangesStandard();

				// Invalidate pagesContetnCache
				InvalidatePagesTempCache();

				return deleteExecuted;
			}
			catch(Exception ex) {
				throw ex;
			}
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

			try {
				if(GetPage(pageFullName) == null) return false;

				var categoriesEntities = GetCategoriesEntities(_wiki);

				foreach(CategoriesEntity entity in categoriesEntities) {
					List<string> categoryPages = entity.PagesFullNames != null ? entity.PagesFullNames.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries).ToList<string>() : new List<string>();
					if(categoryPages.Contains(pageFullName)) {
						categoryPages.Remove(pageFullName);
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
					categoryPages.Add(pageFullName);
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

		#endregion

		#region Messages

		private MessageEntity GetMessageEntity(string wiki, string pageFullName, int messageId) {
			var messagesQuery = (from e in _context.CreateQuery<MessageEntity>(MessagesTable).AsTableServiceQuery()
								 where e.PartitionKey.Equals(wiki + "|" + pageFullName) && e.RowKey.Equals(messageId.ToString())
								 select e).AsTableServiceQuery();
			return QueryHelper<MessageEntity>.FirstOrDefault(messagesQuery);
		}

		private IList<MessageEntity> GetMessagesEntities(string wiki, string pageFullName) {
			var messagesQuery = (from e in _context.CreateQuery<MessageEntity>(MessagesTable).AsTableServiceQuery()
								 where e.PartitionKey.Equals(wiki + "|" + pageFullName)
								 select e).AsTableServiceQuery();
			return QueryHelper<MessageEntity>.All(messagesQuery);
		}

		private MessageEntity BuildMessageEntity(string wiki, string pageFullName, int messageId, string username, string subject, DateTime dateTime, string body, int parent) {
			return new MessageEntity() {
				PartitionKey = wiki + "|" + pageFullName,
				RowKey = messageId.ToString(),
				Username = username,
				Subject = subject,
				DateTime = dateTime.ToUniversalTime(),
				Body = body,
				ParetnId = parent.ToString()
			};
		}
		
		private int GetNextMessageId(string wiki, string pageFullName) {
			IList<MessageEntity> messagesEntities = GetMessagesEntities(wiki, pageFullName);
			if(messagesEntities.Count == 0) return 0;
			return messagesEntities.Max<MessageEntity>(m => int.Parse(m.RowKey)) + 1;
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

			try {
				var messagesEntities = GetMessagesEntities(_wiki, pageFullName);

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
		/// <param name="pageFullName">The page full name.</param>
		/// <returns>The number of messages.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="pageFullName"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pageFullName"/> is empty.</exception>
		public int GetMessageCount(string pageFullName) {
			if(pageFullName == null) throw new ArgumentNullException("page");

			try {
				var messagesEntities = GetMessagesEntities(_wiki, pageFullName);
				return messagesEntities.Count;
			}
			catch(Exception ex) {
				throw ex;
			}
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

			try {
				PageContent page = GetPage(pageFullName);
				if(page == null) return false;

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
				var oldMessagesIds = GetMessagesEntities(_wiki, pageFullName).Select(m => m.RowKey);
				foreach(string oldMessageId in oldMessagesIds) {
					RemoveMessage(pageFullName, int.Parse(oldMessageId), true);
				}

				foreach(Message message in messages) {
					_context.AddObject(MessagesTable, BuildMessageEntity(_wiki, pageFullName, message.ID, message.Username, message.Subject, message.DateTime, message.Body, -1));
					foreach(Message reply in message.Replies) {
						var replayEntity = BuildMessageEntity(_wiki, pageFullName, reply.ID, reply.Username, reply.Subject, reply.DateTime, reply.Body, message.ID);
						_context.AddObject(MessagesTable, replayEntity);
					}
				}
				_context.SaveChangesStandard();

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
		public bool AddMessage(string pageFullName, string username, string subject, DateTime dateTime, string body, int parent) {
			if(pageFullName == null) throw new ArgumentNullException("page");
			if(username == null) throw new ArgumentNullException("username");
			if(subject == null) throw new ArgumentNullException("subject");
			if(body == null) throw new ArgumentNullException("body");
			if(username.Length == 0) throw new ArgumentException("username");
			if(subject.Length == 0) throw new ArgumentException("subject");
			if(parent < -1) throw new ArgumentOutOfRangeException("parent");

			try {
				// If page does not exists return false
				PageContent page = GetPage(pageFullName);
				if(page == null) return false;

				if(parent != -1) {
					// If the given parent message with the given id does not exists return false;
					var parentMessageEntity = GetMessageEntity(_wiki, pageFullName, parent);
					if(parentMessageEntity == null) return false;
				}

				var messageEntity = BuildMessageEntity(_wiki, pageFullName, GetNextMessageId(_wiki, pageFullName), username, subject, dateTime, body, parent);
				_context.AddObject(MessagesTable, messageEntity);
				_context.SaveChangesStandard();

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
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
			if(id < 0) throw new ArgumentOutOfRangeException("id");

			try {
				PageContent page = GetPage(pageFullName);
				if(page == null) return false;

				var messageEntity = GetMessageEntity(_wiki, pageFullName, id);
				if(messageEntity == null) return false;

				_context.DeleteObject(messageEntity);

				var messagesEntities = GetMessagesEntities(_wiki, pageFullName);
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

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
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
			if(username == null) throw new ArgumentNullException("username");
			if(subject == null) throw new ArgumentNullException("subject");
			if(body == null) throw new ArgumentNullException("body");
			if(id < 0) throw new ArgumentOutOfRangeException("id");
			if(username.Length == 0) throw new ArgumentException("username");
			if(subject.Length == 0) throw new ArgumentException("subject");

			try {
				PageContent page = GetPage(pageFullName);
				if(page == null) return false;

				var messageEntity = GetMessageEntity(_wiki, pageFullName, id);
				if(messageEntity == null) return false;

				messageEntity.Username = username;
				messageEntity.Subject = subject;
				messageEntity.DateTime = dateTime.ToUniversalTime();
				messageEntity.Body = body;

				_context.UpdateObject(messageEntity);
				_context.SaveChangesStandard();

				return true;
			}
			catch(Exception ex) {
				throw ex;
			}
		}

		#endregion

		#region NavigationPaths

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
		/// <param name="pages">The full name of the pages array.</param>
		/// <returns>The correct <see cref="T:NavigationPath"/> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> or <paramref name="pages"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="name"/> or <paramref name="pages"/> are empty.</exception>
		public NavigationPath AddNavigationPath(string nspace, string name, string[] pages) {
			if(name == null) throw new ArgumentNullException("name");
			if(pages == null) throw new ArgumentNullException("pages");
			if(name.Length == 0) throw new ArgumentException("name");
			if(pages.Length == 0) throw new ArgumentException("pages");

			try {
				if(GetNavigationPathEntity(_wiki, NameTools.GetFullName(nspace, name)) != null) return null;

				NavigationPath navigationPath = new NavigationPath(NameTools.GetFullName(nspace, name), this);
				List<string> pagesNames = new List<string>(pages.Length);
				foreach(string page in pages) {
					if(page == null) throw new ArgumentNullException();
					if(GetPage(page) == null) throw new ArgumentException();
					pagesNames.Add(page);
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
		/// <param name="pages">The new pages full names array.</param>
		/// <returns>The correct <see cref="T:NavigationPath"/> object.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="path"/> or <paramref name="pages"/> are <c>null</c>.</exception>
		/// <exception cref="ArgumentException">If <paramref name="pages"/> is empty.</exception>
		public NavigationPath ModifyNavigationPath(NavigationPath path, string[] pages) {
			if(path == null) throw new ArgumentNullException("path");
			if(pages == null) throw new ArgumentNullException("pages");
			if(pages.Length == 0) throw new ArgumentException("pages");

			try {
				NavigationPathEntity navigationPathEntity = GetNavigationPathEntity(_wiki, path.FullName);
				if(navigationPathEntity == null) return null;

				List<string> pagesNames = new List<string>(pages.Length);
				foreach(string page in pages) {
					if(page == null) throw new ArgumentNullException("pages", "A page element cannot be null");
					if(GetPage(page) == null) throw new ArgumentException("Page not found", "pages");
					pagesNames.Add(page);
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

		#endregion

		#region Snippets

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

		#endregion

		#region ContentTemplates

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

		#endregion

		#region IStorageProviderV40 Members

		/// <summary>
		/// Gets a value specifying whether the provider is read-only, i.e. it can only provide data and not store it.
		/// </summary>
		public bool ReadOnly {
			get { return false; }
		}

		#endregion

		#region IProviderV40 Members


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

			if(config == "") config = Config.GetConnectionString();

			_host = host;
			_wiki = string.IsNullOrEmpty(wiki) ? "root" : wiki.ToLowerInvariant();
			
			_context = TableStorage.GetContext(config);
			_containerRef = TableStorage.StorageAccount(config).CreateCloudBlobClient().GetContainerReference(_wiki + "-pages");
			_containerRef.CreateIfNotExist();
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

			if(config == "") config = Config.GetConnectionString();

			TableStorage.CreateTable(config, NamespacesTable);
			TableStorage.CreateTable(config, CategoriesTable);
			TableStorage.CreateTable(config, PagesInfoTable);
			TableStorage.CreateTable(config, PagesContentsTable);
			TableStorage.CreateTable(config, MessagesTable);
			TableStorage.CreateTable(config, NavigationPathsTable);
			TableStorage.CreateTable(config, SnippetsTable);
			TableStorage.CreateTable(config, ContentTemplatesTable);
			TableStorage.CreateTable(config, IndexWordMappingTable);
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return new ComponentInformation("Azure Table Storage Pages Storage Provider", "Threeplicate Srl", "4.0.1.71", "http://www.screwturn.eu", null); }
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml {
			get { return ""; }
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
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

	internal class PagesContentsEntity : TableServiceEntity {
		// PartitionKey = wiki
		// RowKey = pageFullName|revision

		public string Revision { get; set; }
		public string PageFullName { get; set; }
		public string Namespace { get; set; }
		public DateTime CreationDateTime { get; set; }
		public string Title { get; set; }
		public string User { get; set; }
		public DateTime LastModified { get; set; }
		public string Comment { get; set; }
		public string Content { get; set; }
		public string Description { get; set; }
		public string Keywords { get; set; }    // Separated by '|' (pipe)
		public string BlobReference { get; set; }    // Contains the name of the blob where the content is stored for long pages
													 // (if null all the content is stored in the Content property)
		internal string BigContent { get; set; }    // Not stored on TableStorage
	}

	internal class MessageEntity : TableServiceEntity {
		// PartitionKey = wiki|pageFullName
		// RowKey = messageId

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
	
}
