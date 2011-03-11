
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.SearchEngine;
using Ionic.Zip;

namespace ScrewTurn.Wiki {
	/// <summary>
	/// Implements the methods to view, add or delete Themes
	/// </summary>
	public class ThemeStorageProvider : ProviderBase, IThemeStorageProviderV30 {

		private const string ThemeDirectory = ("Themes");
		/// <summary>
		/// The name of the provider.
		/// </summary>
		public static readonly string ProviderName = "Theme Provider";
		private const string DefaultTheme = "Default";
		private readonly ComponentInformation info =
			new ComponentInformation(ProviderName, "Threeplicate Srl", Settings.WikiVersion, "http://www.screwturn.eu", null);

		private IHostV30 host;

		/// <summary>
		/// Gets the path.
		/// </summary>
		/// <returns>The path generated from hostV30 and ThemeDirectory</returns>
		private string GetPath(string path1, string path2) {
			return Path.Combine(path1, path2);
		}

		/// <summary>
		/// Retrives the lists of avaiable themes.
		/// </summary>
		/// <returns>A list of theme names.</returns>
		public List<string> ListThemes() {
			List<string> listTheme = new List<string>();
			string pathFolders = GetPath(GetDataDirectory(host), ThemeDirectory);

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
			string path = GetPath(GetPath(GetDataDirectory(host), ThemeDirectory), themeName);
			string[] files;
			if(!String.IsNullOrEmpty(path))
				files = Directory.GetFiles(path, searchPattern);
			else return null;

			for(int i = 0; i < files.Length; i++) {
				files[i] = GetRelativePath(files[i]);
			}
			return new List<string>(files);
		}

		private string GetRelativePath(string file) {
			DirectoryInfo publicPath = new DirectoryInfo(GetDataDirectory(host));
			return file.Substring(file.IndexOf(publicPath.Name));
		}

		/// <summary>
		/// Deletes the theme with the given name.
		/// </summary>
		/// <param name="themeName">The name of the theme to be deleted.</param>
		/// <returns><c>true</c> if the theme is removed, <c>false</c> otherwise.</returns>
		public bool DeleteTheme(string themeName) {
				Directory.Delete(GetPath(GetPath(GetDataDirectory(host), ThemeDirectory), themeName), true);
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
			string targetPath = GetPath(GetPath(GetDataDirectory(host), ThemeDirectory), themeName);

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
			return GetRelativePath(GetPath(GetPath(GetDataDirectory(host), ThemeDirectory), themeName)) + "/";
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
		/// Initializes the Theme Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <b>host</b> or <b>config</b> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <b>config</b> is not valid or is incorrect.</exception>
		public void Init(IHostV30 host, string config) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");
			string[] files;
			this.host = host;

			if(!LocalProvidersTools.CheckWritePermissions(GetDataDirectory(host))) {
				throw new InvalidConfigurationException("Cannot write into the public directory - check permissions");
			}

			if(!Directory.Exists(GetPath(GetDataDirectory(host), ThemeDirectory)))
				Directory.CreateDirectory(GetPath(GetDataDirectory(host), ThemeDirectory));

			bool successExtract;
			if(!Directory.Exists(GetPath(GetPath(GetDataDirectory(host), ThemeDirectory), DefaultTheme))) {
				Directory.CreateDirectory(GetPath(GetPath(GetDataDirectory(host), ThemeDirectory), DefaultTheme));
				successExtract = StoreTheme(GetPath(GetPath(GetDataDirectory(host), ThemeDirectory), DefaultTheme), DefaultThemeZip());
			}
			else {
				files = Directory.GetFiles(GetPath(GetPath(GetDataDirectory(host), ThemeDirectory), DefaultTheme));
				if(files.Length == 0) {
					Directory.Delete(GetPath(GetDataDirectory(host), ThemeDirectory), true);
					successExtract = StoreTheme(GetPath(GetPath(GetDataDirectory(host), ThemeDirectory), DefaultTheme), DefaultThemeZip());
				}
			}
		}

		/// <summary>
		/// Extrats the defaults the theme zip.
		/// </summary>
		/// <returns>The file contenent in the resources.</returns>
		private byte[] DefaultThemeZip() {
			string[] resourceList = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames();
			Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceList[0]);
			byte[] buffer = new byte[stream.Length];
			stream.Read(buffer, 0, buffer.Length);

			return buffer;
		}

		/// <summary>
		/// Method invoked on shutdown.
		/// </summary>
		/// <remarks>This method might not be invoked in some cases.</remarks>
		public void Shutdown() { }

	}


}
