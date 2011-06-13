﻿
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ScrewTurn.Wiki;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.SearchEngine;
using Ionic.Zip;

namespace ScrewTurn.Wiki {
	/// <summary>
	/// Implements the methods to view, add or delete Themes
	/// </summary>
	public class ThemesStorageProvider : ProviderBase, IThemesStorageProviderV40 {

		private const string ThemeDirectory = ("Themes");
		/// <summary>
		/// The name of the provider.
		/// </summary>
		public static readonly string ProviderName = "Local Theme Provider";
		private const string DefaultTheme = "Default";
		private readonly ComponentInformation info =
			new ComponentInformation(ProviderName, "Threeplicate Srl", GlobalSettings.WikiVersion, "http://www.screwturn.eu", null);
		private IHostV40 host;
		private string wiki;

		private string GetMasterDirectory() {
			return Path.Combine(GetDataDirectory(host), wiki);
		}

		/// <summary>
		/// Gets the path.
		/// </summary>
		/// <returns>The path generated from host and ThemeDirectory</returns>
		private string GetPath(string path1, string path2) {
			return Path.Combine(path1, path2);
		}

		/// <summary>
		/// Retrives the lists of avaiable themes.
		/// </summary>
		/// <returns>A list of theme names.</returns>
		public List<string> ListThemes() {
			List<string> listTheme = new List<string>();
			string parent = GetMasterDirectory();
			string pathFolders = GetPath(parent, ThemeDirectory);
			foreach(string dir in Directory.GetDirectories(pathFolders)) {
				DirectoryInfo themeName = new DirectoryInfo(dir);
				listTheme.Add(themeName.Name);
			}
			return listTheme;
		}

		/// <summary>
		/// Retrieves all files present in the selected theme.
		/// </summary>
		/// <param name="themeName">The name of the selected theme.</param>
		/// <param name="searchPattern">The search string to match against the name of files.</param>
		/// <returns>The list of files matching the searchPattern.</returns>
		public List<string> ListThemeFiles(string themeName, string searchPattern) {
			string parent = GetMasterDirectory();
			string path = GetPath(GetPath(parent, ThemeDirectory), themeName);
			string[] files;
			if(!String.IsNullOrEmpty(path) && Directory.Exists(path))
				files = Directory.GetFiles(path, "*" + searchPattern);
			else return null;

			for(int i = 0; i < files.Length; i++) {
				files[i] = GetRelativePath(files[i]).Replace(Path.DirectorySeparatorChar.ToString(),"/");
			}
			return new List<string>(files);
		}

		private string GetRelativePath(string file) {
			string parent = GetMasterDirectory();
			DirectoryInfo publicPath = new DirectoryInfo(parent);
			return file.Substring(file.IndexOf(publicPath.Name)).Replace(Path.DirectorySeparatorChar.ToString(),"/");
		}

		/// <summary>
		/// Deletes the theme with the given name.
		/// </summary>
		/// <param name="themeName">The name of the theme to be deleted.</param>
		/// <returns><c>true</c> if the theme is removed, <c>false</c> otherwise.</returns>
		public bool DeleteTheme(string themeName) {
			string parent = GetMasterDirectory();
			Directory.Delete(GetPath(GetPath(parent, ThemeDirectory), themeName), true);
				return true;
		}

		/// <summary>
		/// Stores the theme.
		/// </summary>
		/// <param name="themeName">The name of the theme.</param>
		/// <param name="zipFile">The zipFile conteining the theme.</param>
		/// <returns><c>true</c> if the theme is saved, <c>false</c> otherwise.</returns>
		public bool StoreTheme(string themeName, byte[] zipFile) {
			if(themeName == null) throw new ArgumentNullException("filename");
			if(themeName.Length == 0) throw new ArgumentException("Filename cannot be empty", "filename");
			if(zipFile == null) throw new ArgumentNullException("assembly");
			if(zipFile.Length == 0) throw new ArgumentException("Assembly cannot be empty", "assembly");
			string parent = GetMasterDirectory();
			//parent = parent.Replace("public\\", "");
			string targetPath = GetPath(GetPath(parent, ThemeDirectory), themeName);

			Directory.CreateDirectory(targetPath);
			try {
				using(ZipFile zip1 = ZipFile.Read(zipFile)) {
					foreach(ZipEntry e in zip1) {
						e.Extract(targetPath, ExtractExistingFileAction.OverwriteSilently);
					}
				}
			}
			catch(IOException) {
				return false;
			}
			return true;
		}

		/// <summary>
		/// Gets the relative path of the theme with the given name.
		/// </summary>
		/// <param name="themeName">The name of the theme.</param>
		/// <returns>The relative path of the theme.</returns>
		public string GetThemePath(string themeName) {
			string parent = GetMasterDirectory();
			//parent = parent.Replace("public\\", "");
			return GetRelativePath(GetPath(GetPath(parent, ThemeDirectory), themeName)) + "/";
		}

		/// <summary>
		/// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
		/// </summary>
		public string ConfigHelpHtml {
			get { return null; }
		}

		/// <summary>
		/// Gets the Information about the Provider.
		/// </summary>
		public ComponentInformation Information {
			get { return info; }
		}

		/// <summary>
		/// Gets a value specifying whether the provider is read-only, i.e. it can only provide data and not store it.
		/// </summary>
		public bool ReadOnly {
			get { return false; }
		}

		/// <summary>
		/// Gets the wiki that has been used to initialize the current instance of the provider.
		/// </summary>
		public string CurrentWiki {
			get { return wiki; }
		}

		/// <summary>
		/// Initializes the Theme Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <param name="wiki">The wiki.</param>
		/// <exception cref="ArgumentNullException">If <b>host</b> or <b>config</b> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <b>config</b> is not valid or is incorrect.</exception>
		public void Init(IHostV40 host, string config, string wiki) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");
			
			this.host = host;
			this.wiki = wiki;

			if(!LocalProvidersTools.CheckWritePermissions(GetMasterDirectory())) {
				throw new InvalidConfigurationException("Cannot write into the public directory - check permissions");
			}

			string pathFolders = GetPath(GetMasterDirectory(), ThemeDirectory);
			if(!Directory.Exists(pathFolders)) {
				Directory.CreateDirectory(pathFolders);
			}
		}

		/// <summary>
		/// Sets up the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <b>host</b> or <b>config</b> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <b>config</b> is not valid or is incorrect.</exception>
		public void SetUp(IHostV40 host, string config) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

		}

		void IDisposable.Dispose() { }

	}
}
