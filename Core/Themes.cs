
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ScrewTurn.Wiki.PluginFramework;
using System.Web;
using System.Globalization;
using System.IO;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Allows access to the themes.
	/// </summary>
	public static class Themes {

		/// <summary>
		/// Retrives the lists of avaiable themes in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="provider">The provider.</param>
		/// <returns>A list of theme names.</returns>
		public static List<String> ListThemes(string wiki, string provider) {
			List<string> result = new List<string>();
			if(provider == "standard") {
				// Populate standard themes
				string[] dirs = Directory.GetDirectories(GlobalSettings.ThemesDirectory);
				for(int i = 0; i < dirs.Length; i++) {
					//if(dirs[i].EndsWith("\\")) dirs[i] = dirs[i].Substring(0, dirs[i].Length - 1);
					dirs[i] = dirs[i].TrimEnd(Path.DirectorySeparatorChar);
					result.Add(dirs[i].Substring(dirs[i].LastIndexOf(Path.DirectorySeparatorChar) + 1));
				}
			}
			else {
				foreach(IThemesStorageProviderV40 prov in Collectors.CollectorsBox.ThemesProviderCollector.GetAllProviders(wiki)) {
					if(prov.ToString() == provider) result.AddRange(prov.ListThemes());
				}
			}
			return result;
		}

		/// <summary>
		/// Deletes the theme with the given name from the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="themeName">The name of the theme to be deleted.</param>
		/// <returns><c>true</c> if the theme is removed, <c>false</c> otherwise.</returns>
		public static bool DeleteTheme(string wiki, string themeName) {
			foreach(IThemesStorageProviderV40 themeDeleteProvider in Collectors.CollectorsBox.ThemesProviderCollector.GetAllProviders(wiki)) {
				if(!IsThemeInUse(wiki, themeName)) {
					string[] theme = themeName.Split('|');
					if (theme[0] == themeDeleteProvider.ToString())	return themeDeleteProvider.DeleteTheme(theme[theme.Length-1]);
				}
			}
			return false;
		}

		private static bool IsThemeInUse(string wiki, string themeName) {
			List<NamespaceInfo> namespaces = Pages.GetNamespaces(wiki);
			bool result = false;

			if(themeName == Settings.GetTheme(wiki, null)) return true;
			foreach(NamespaceInfo ns in namespaces) {
				if(themeName == Settings.GetTheme(wiki, ns.Name)) return true;
			}
			return result;
		}

		/// <summary>
		/// Retrieves all files present in the selected theme.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="themeName">The name of the selected theme.</param>
		/// <param name="searchPattern">The search string to match against the name of files.</param>
		/// <returns>The list of files matching the searchPattern.</returns>
		public static List<string> ListThemeFiles(string wiki, string themeName, string searchPattern) {
			string provider = "";
			string theme = "";
			string[] values = themeName.Split('|');
			string path = "";
			provider = values[0];
			theme = values[values.Length-1];
			if(values.Length > 1) {
				if(provider == "standard") {
					foreach(string s in ListThemes(wiki, provider)) {
						if(s == theme) {
							path = Path.Combine(GlobalSettings.ThemesDirectory, s);
							List<string> files = new List<string>();
							if(!String.IsNullOrEmpty(path) && Directory.Exists(path)) {
								foreach(string file in Directory.GetFiles(path)) {
									if(file.Contains(searchPattern)) files.Add(file);
								}
							}
							else {
								return null;
							}
							for(int i = 0; i < files.Count; i++) {
								files[i] = "Themes/" + GetRelativePath(files[i], path).Replace(Path.DirectorySeparatorChar.ToString(), "/");
							}
							return new List<string>(files);
						}
					}
				}
				else {
					IThemesStorageProviderV40 themeListFileProvider = Collectors.CollectorsBox.ThemesProviderCollector.GetProvider(provider, wiki);
					if(themeListFileProvider == null) return null;
					List<string> lists = themeListFileProvider.ListThemeFiles(theme, searchPattern);
					if((lists == null) || (lists.Count == 0)) return null;
					else return lists;
				}
			}
			return null;
		}

		private static string GetRelativePath(string file, string directory) {
			DirectoryInfo publicPath = new DirectoryInfo(directory);
			return file.Substring(file.IndexOf(publicPath.Name)).Replace(Path.DirectorySeparatorChar.ToString(), "/");
		}

		/// <summary>
		/// Stores the theme for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="themeName">The name of the theme.</param>
		/// <param name="zipFile">The zipFile conteining the theme.</param>
		/// <returns><c>true</c> if the theme is saved, <c>false</c> otherwise.</returns>
		public static bool StoreTheme(string wiki, string themeName, byte[] zipFile) {
			string[] values = themeName.Split('|');
			string provider = values[0];
			string theme = values[values.Length - 1];
			IThemesStorageProviderV40 themeStorageProvider = Collectors.CollectorsBox.ThemesProviderCollector.GetProvider(provider, wiki);
			return themeStorageProvider.StoreTheme(theme, zipFile);
		}

		/// <summary>
		/// Gets the relative path of the theme with the given name for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="themeName">The name of the theme.</param>
		/// <returns>The relative path of the theme.</returns>
		public static string GetThemePath(string wiki, string themeName) {
			string[] values = themeName.Split('|');
			string provider = values[0];
			string theme = values[values.Length-1];
			if(provider == "standard") return "Themes/" + GetRelativePath(Path.Combine(GlobalSettings.ThemesDirectory, theme), theme) + "/";
			IThemesStorageProviderV40 themePathProvider = Collectors.CollectorsBox.ThemesProviderCollector.GetProvider(provider, wiki);
			return themePathProvider.GetThemePath(theme);
		}
	}
}
