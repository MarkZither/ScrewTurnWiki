
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
		/// Retrives the lists of avaiable themes.
		/// </summary>
		/// <returns>A list of theme names.</returns>
		public static List<String> ListThemes(string provider) {
			List<string> result = new List<string>();
			if(provider == "standard") {
				// Populate standard themes
				string[] themes = Tools.AvailableThemes;
				foreach(string theme in themes) {
					result.Add(theme);
				}
			}
			else {
				foreach(IThemeStorageProviderV30 prov in Collectors.ThemeProviderCollector.AllProviders) {
					if(prov.ToString() == provider) result.AddRange(prov.ListThemes(provider));
				}
			}
			return result;
		}

		/// <summary>
		/// Deletes the theme with the given name.
		/// </summary>
		/// <param name="themeName">The name of the theme to be deleted.</param>
		/// <returns><c>true</c> if the theme is removed, <c>false</c> otherwise.</returns>
		public static bool DeleteTheme(string themeName) {
			IThemeStorageProviderV30 themeDeleteProvider = Collectors.ThemeProviderCollector.GetProvider(Settings.DefaultThemeProvider);
			if(!IsThemeInUse(themeName)) 
				return themeDeleteProvider.DeleteTheme(themeName);
				else return false;
		}

		private static bool IsThemeInUse(string themeName) {
			List<NamespaceInfo> namespaces = Pages.GetNamespaces();
			bool result = false;

			if(themeName == Settings.GetTheme(null)) return true;
			foreach(NamespaceInfo ns in namespaces) {
				if(themeName == Settings.GetTheme(ns.Name)) return true;
			}
			return result;
		}

		/// <summary>
		/// Retrieves all files present in the selected theme.
		/// </summary>
		/// <param name="themeName">The name of the selected theme.</param>
		/// <param name="searchPattern">The search string to match against the name of files.</param>
		/// <returns>The list of files matching the searchPattern.</returns>
		public static List<string> ListThemeFiles(string themeName, string searchPattern) {
			string provider = "";
			string theme = "";
			string[] values = themeName.Split('|');
			string path = "";
			provider = values[0];
			theme = values[values.Length-1];
			if(values.Length > 1) {

				if(provider == "standard") {
					foreach(string s in ListThemes(provider)) {
						if(s == theme) {
							path = Path.Combine(Settings.ThemesDirectory, s);
							string[] files;
							if(!String.IsNullOrEmpty(path) && Directory.Exists(path))
								files = Directory.GetFiles(path, searchPattern);
							else return null;
							for(int i = 0; i < files.Length; i++) {
								files[i] = "Themes/" + GetRelativePath(files[i], path).Replace(Path.DirectorySeparatorChar.ToString(), "/");
							}
							return new List<string>(files);
						}
					}
				}
				else {
					IThemeStorageProviderV30 themeListFileProvider = Collectors.ThemeProviderCollector.GetProvider(Settings.DefaultThemeProvider);
					List<string> lists = themeListFileProvider.ListThemeFiles(theme, searchPattern);
					if((lists == null) || (lists.Count == 0)) return null;
					else return themeListFileProvider.ListThemeFiles(theme, searchPattern);
				}
			}
			return null;
		}

		private static string GetRelativePath(string file, string directory) {
			DirectoryInfo publicPath = new DirectoryInfo(directory);
			return file.Substring(file.IndexOf(publicPath.Name)).Replace(Path.DirectorySeparatorChar.ToString(), "/");
		}

		/// <summary>
		/// Stores the theme.
		/// </summary>
		/// <param name="themeName">The name of the theme.</param>
		/// <param name="zipFile">The zipFile conteining the theme.</param>
		/// <returns><c>true</c> if the theme is saved, <c>false</c> otherwise.</returns>
		public static bool StoreTheme(string themeName, byte[] zipFile) {
			IThemeStorageProviderV30 themeStorageProvider = Collectors.ThemeProviderCollector.GetProvider(Settings.DefaultThemeProvider);
			return themeStorageProvider.StoreTheme(themeName, zipFile);
		}

		/// <summary>
		/// Gets the relative path of the theme with the given name.
		/// </summary>
		/// <param name="themeName">The name of the theme.</param>
		/// <returns>The relative path of the theme.</returns>
		public static string GetThemePath(string themeName) {
			string[] values = themeName.Split('|');
			string provider = values[0];
			string theme = values[values.Length-1];
			if(provider == "standard") {
				return "Themes/" + GetRelativePath(Path.Combine(Settings.ThemesDirectory, theme), theme) + "/";
			}
			IThemeStorageProviderV30 themePathProvider = Collectors.ThemeProviderCollector.GetProvider(Settings.DefaultThemeProvider);
			return themePathProvider.GetThemePath(theme);
		}
	}
}
