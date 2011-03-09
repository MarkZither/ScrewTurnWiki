
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;
using ScrewTurn.Wiki.SearchEngine;
using Ionic.Zip;

namespace ScrewTurn.Wiki {
	class ThemeStorageProvider : ProviderBase, IThemeStorageProviderV30 {

		private const string ThemeDirectory = ("Themes");
		/// <summary>
		/// The name of the provider.
		/// </summary>
		public static readonly string ProviderName = "Theme Provider";
		public const string DefaultTheme = "Default";
		private readonly ComponentInformation info =
			new ComponentInformation(ProviderName, "Threeplicate Srl", Settings.WikiVersion, "http://www.screwturn.eu", null);

		private IHostV30 host;

		/// <summary>
		/// Gets the path.
		/// </summary>
		/// <returns>The path generated from hostV30 and ThemeDirectory</returns>
		private string GetPath(string path1, string path2){
			return Path.Combine(path1, path2);
		}

		public List<string> GetListTheme() {
			List<string> listTheme = new List<string>();
			string pathFolders = GetPath(GetDataDirectory(host), ThemeDirectory);

			foreach(string dir in Directory.GetDirectories(pathFolders))
					listTheme.Add(dir.ToString());

			return listTheme;
		}

		public List<string> GetListThemeFiles(string name) {
			List<string> listFiles = new List<string>();
			List<string> Folder = GetListTheme();
			foreach(string dir in Folder){
				if (name == dir.ToString())
					foreach(var file in Directory.GetFiles(name)) {
						listFiles.Add(file);
					}
			}
			return listFiles;
		}

		public bool DeleteTheme(string themename) {
			List<string> ThemeName = new List<string>();
			foreach(var theme in Directory.GetDirectories(Path.Combine(GetDataDirectory(host), ThemeDirectory))) {
				if(themename == theme.ToString()) {
					Directory.Delete(themename);
					return true;
				}
			}
			return false;
		}

		public bool storeTheme(string filename, byte[] assembly) {

			if(filename == null) throw new ArgumentNullException("filename");
			if(filename.Length == 0) throw new ArgumentException("Filename cannot be empty", "filename");
			if(assembly == null) throw new ArgumentNullException("assembly");
			if(assembly.Length == 0) throw new ArgumentException("Assembly cannot be empty", "assembly");
			string targetPath = GetPath(GetPath(GetDataDirectory(host), ThemeDirectory), filename);
			Directory.CreateDirectory(targetPath);
			lock(this) {
				try {
					using(ZipFile zip1 = ZipFile.Read(assembly)) {
						// here, we extract every entry, but we could extract conditionally
						// based on entry name, size, date, checkbox status, etc.  
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
		/// Initializes the Storage Provider.
		/// </summary>
		/// <param name="host">The Host of the Component.</param>
		/// <param name="config">The Configuration data, if any.</param>
		/// <exception cref="ArgumentNullException">If <b>host</b> or <b>config</b> are <c>null</c>.</exception>
		/// <exception cref="InvalidConfigurationException">If <b>config</b> is not valid or is incorrect.</exception>
		public void Init(IHostV30 host, string config) {
			if(host == null) throw new ArgumentNullException("host");
			if(config == null) throw new ArgumentNullException("config");

			this.host = host;

			if(!LocalProvidersTools.CheckWritePermissions(GetDataDirectory(host))) {
				throw new InvalidConfigurationException("Cannot write into the public directory - check permissions");
			}

			if(!Directory.Exists(GetPath(GetDataDirectory(host), ThemeDirectory)))
				Directory.CreateDirectory(GetPath(GetDataDirectory(host), ThemeDirectory));

			if(!Directory.Exists(GetPath(GetDataDirectory(host), ThemeDirectory)))
				Directory.CreateDirectory(GetPath(GetDataDirectory(host), ThemeDirectory));
			
			bool successExtract;
						if(!Directory.Exists(GetPath(GetPath(GetDataDirectory(host), ThemeDirectory), DefaultTheme))) {
				Directory.CreateDirectory(GetPath(GetPath(GetDataDirectory(host), ThemeDirectory), DefaultTheme));
				 successExtract = storeTheme(GetPath(GetPath(GetDataDirectory(host), ThemeDirectory), DefaultTheme), DefaultThemeZip());
			}
		}

		private byte[] DefaultThemeZip() {
			Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ScrewTurn.Wiki.Core.Resources.Default.zip");
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
