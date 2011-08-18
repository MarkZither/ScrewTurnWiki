
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Manages navigation paths.
	/// </summary>
	public static class NavigationPaths {

		/// <summary>
		/// Gets the list of the Navigation Paths.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <returns>The navigation paths, sorted by name.</returns>
		public static List<NavigationPath> GetAllNavigationPaths(string wiki) {
			List<NavigationPath> allPaths = new List<NavigationPath>(30);

			// Retrieve paths from every Pages provider
			foreach(IPagesStorageProviderV40 provider in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
				allPaths.AddRange(provider.GetNavigationPaths(null));
				foreach(NamespaceInfo nspace in provider.GetNamespaces()) {
					allPaths.AddRange(provider.GetNavigationPaths(nspace));
				}
			}

			allPaths.Sort(new NavigationPathComparer());

			return allPaths;
		}

		/// <summary>
		/// Gets the list of the Navigation Paths in a namespace.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The namespace.</param>
		/// <returns>The navigation paths, sorted by name.</returns>
		public static List<NavigationPath> GetNavigationPaths(string wiki, NamespaceInfo nspace) {
			List<NavigationPath> allPaths = new List<NavigationPath>(30);

			// Retrieve paths from every Pages provider
			foreach(IPagesStorageProviderV40 provider in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
				allPaths.AddRange(provider.GetNavigationPaths(nspace));
			}

			allPaths.Sort(new NavigationPathComparer());

			return allPaths;
		}

		/// <summary>
		/// Finds a Navigation Path's Name.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="name">The Name.</param>
		/// <returns>True if the Navigation Path exists.</returns>
		public static bool Exists(string wiki, string name) {
			return Find(wiki, name) != null;
		}

		/// <summary>
		/// Finds and returns a Path.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="fullName">The full name.</param>
		/// <returns>The correct <see cref="T:NavigationPath"/> object or <c>null</c> if no path is found.</returns>
		public static NavigationPath Find(string wiki, string fullName) {
			List<NavigationPath> allPaths = GetAllNavigationPaths(wiki);
			int idx = allPaths.BinarySearch(new NavigationPath(fullName, null), new NavigationPathComparer());
			if(idx >= 0) return allPaths[idx];
			else return null;
		}

		/// <summary>
		/// Adds a new Navigation Path.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The target namespace (<c>null</c> for the root).</param>
		/// <param name="name">The Name.</param>
		/// <param name="pages">The full name of the pages.</param>
		/// <param name="provider">The Provider to use for the new Navigation Path, or <c>null</c> for the default provider.</param>
		/// <returns>True if the Path is added successfully.</returns>
		public static bool AddNavigationPath(string wiki, NamespaceInfo nspace, string name, List<string> pages, IPagesStorageProviderV40 provider) {
			string namespaceName = nspace != null ? nspace.Name : null;
			string fullName = NameTools.GetFullName(namespaceName, name);

			if(Exists(wiki, fullName)) return false;

			if(provider == null) provider = Collectors.CollectorsBox.PagesProviderCollector.GetProvider(GlobalSettings.DefaultPagesProvider, wiki);

			NavigationPath newPath = provider.AddNavigationPath(namespaceName, name, pages.ToArray());
			if(newPath != null) Log.LogEntry("Navigation Path " + fullName + " added", EntryType.General, Log.SystemUsername, wiki);
			else Log.LogEntry("Creation failed for Navigation Path " + fullName, EntryType.Error, Log.SystemUsername, wiki);
			return newPath != null;
		}

		/// <summary>
		/// Removes a Navigation Path.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="fullName">The full name of the path to remove.</param>
		/// <returns><c>true</c> if the path is removed, <c>false</c> otherwise.</returns>
		public static bool RemoveNavigationPath(string wiki, string fullName) {
			NavigationPath path = Find(wiki, fullName);
			if(path == null) return false;

			bool done = path.Provider.RemoveNavigationPath(path);
			if(done) Log.LogEntry("Navigation Path " + fullName + " removed", EntryType.General, Log.SystemUsername, wiki);
			else Log.LogEntry("Deletion failed for Navigation Path " + fullName, EntryType.Error, Log.SystemUsername, wiki);
			return done;
		}

		/// <summary>
		/// Modifies a Navigation Path.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="fullName">The full name of the path to modify.</param>
		/// <param name="pages">The list of pages full names.</param>
		/// <returns><c>true</c> if the path is modified, <c>false</c> otherwise.</returns>
		public static bool ModifyNavigationPath(string wiki, string fullName, List<string> pages) {
			NavigationPath path = Find(wiki, fullName);
			if(path == null) return false;

			NavigationPath newPath = path.Provider.ModifyNavigationPath(path, pages.ToArray());
			if(newPath != null) Log.LogEntry("Navigation Path " + fullName + " modified", EntryType.General, Log.SystemUsername, wiki);
			else Log.LogEntry("Modification failed for Navigation Path " + fullName, EntryType.Error, Log.SystemUsername, wiki);
			return newPath != null;
		}

		/// <summary>
		/// Finds all the Navigation Paths that include a Page.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="pageFullName">The full name of the page.</param>
		/// <returns>The list of Navigation Paths.</returns>
		public static string[] PathsPerPage(string wiki, string pageFullName) {
			NamespaceInfo pageNamespace = Pages.FindNamespace(wiki, NameTools.GetNamespace(pageFullName));

			List<string> result = new List<string>(10);
			List<NavigationPath> allPaths = GetNavigationPaths(wiki, pageNamespace);

			for(int i = 0; i < allPaths.Count; i++) {
				List<string> pages = new List<string>(allPaths[i].Pages);
				if(pages.Contains(pageFullName)) {
					result.Add(allPaths[i].FullName);
				}
			}
			return result.ToArray();
		}

	}

}
