
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Contains private instances of the Providers Collectors accessible through the CollectorsBox ThreadStatic property.
	/// </summary>
	public static class Collectors {

		[ThreadStatic]
		private static ICollectorsBox collectorsBox;

		/// <summary>
		/// <c>true</c> if the collectorsBox has been instantiated
		/// </summary>
		[ThreadStatic]
		public static bool CollectorsBoxUsed = false;

		/// <summary>
		/// Gets the collectors box.
		/// </summary>
		public static ICollectorsBox CollectorsBox {
			get {
				if(collectorsBox == null) {
					collectorsBox = new CollectorsBox(_globalSettingsProvider,
													  _globalSettingsProviderAssembly,
													  _settingsProvider,
													  _settingsProviderAssembly,
													  _usersProviderCollector.Clone(),
													  _themeProviderCollector.Clone(),
													  _pagesProviderCollector.Clone(),
													  _filesProviderCollector.Clone(),
													  _formatterProviderCollector.Clone());
					CollectorsBoxUsed = true;
				}
				return collectorsBox;
			}
			set {
				collectorsBox = value;
			}
		}

		// The following static instances are "almost" thread-safe because they are set at startup and never changed
		// The ProviderCollector generic class is fully thread-safe

		/// <summary>
		/// Contains the file names of the DLLs containing each provider (provider->file).
		/// </summary>
		public static Dictionary<string, string> FileNames;
		
		/// <summary>
		/// The settings storage provider.
		/// </summary>
		private static Type _settingsProvider;

		/// <summary>
		/// The settings storage provider assembly.
		/// </summary>
		private static System.Reflection.Assembly _settingsProviderAssembly;

		/// <summary>
		/// The settings storage provider.
		/// </summary>
		private static Type _globalSettingsProvider;

		/// <summary>
		/// The settings storage provider assembly.
		/// </summary>
		private static System.Reflection.Assembly _globalSettingsProviderAssembly;

		/// <summary>
		/// The Users Provider Collector instance.
		/// </summary>
		private static ProviderCollector<IUsersStorageProviderV30> _usersProviderCollector;

		/// <summary>
		/// The Theme Provider Collector istance.
		/// </summary>
		private static ProviderCollector<IThemeStorageProviderV30> _themeProviderCollector;

		/// <summary>
		/// The Pages Provider Collector instance.
		/// </summary>
		private static ProviderCollector<IPagesStorageProviderV30> _pagesProviderCollector;

		/// <summary>
		/// The Files Provider Collector instance.
		/// </summary>
		private static ProviderCollector<IFilesStorageProviderV30> _filesProviderCollector;

		/// <summary>
		/// The Formatter Provider Collector instance.
		/// </summary>
		private static ProviderCollector<IFormatterProviderV30> _formatterProviderCollector;


		/// <summary>
		/// Initializes the collectors.
		/// </summary>
		public static void InitCollectors() {
			_usersProviderCollector = new ProviderCollector<IUsersStorageProviderV30>();
			_pagesProviderCollector = new ProviderCollector<IPagesStorageProviderV30>();
			_filesProviderCollector = new ProviderCollector<IFilesStorageProviderV30>();
			_themeProviderCollector = new ProviderCollector<IThemeStorageProviderV30>();
			_formatterProviderCollector = new ProviderCollector<IFormatterProviderV30>();
		}
		
		/// <summary>
		/// Tries to unload a provider.
		/// </summary>
		/// <param name="typeName">The provider.</param>
		public static void TryUnload(string typeName) {
			_formatterProviderCollector.RemoveProvider(typeName);
		}
		
		/// <summary>
		/// Adds the given provider to the appropriate collector.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="assembly">The assembly.</param>
		/// <param name="providerInterface">The provider interface.</param>
		public static void AddProvider(Type provider, System.Reflection.Assembly assembly, Type providerInterface) {
			collectorsBox = null;
			if(providerInterface.FullName == typeof(ISettingsStorageProviderV30).FullName) {
				_settingsProvider = provider;
				_settingsProviderAssembly = assembly;
			}
			if(providerInterface.FullName == typeof(IPagesStorageProviderV30).FullName) {
				_pagesProviderCollector.AddProvider(provider, assembly);
			}
			else if(providerInterface.FullName == typeof(IThemeStorageProviderV30).FullName) {
				_themeProviderCollector.AddProvider(provider, assembly);
			}
			else if(providerInterface.FullName == typeof(IUsersStorageProviderV30).FullName) {
				_usersProviderCollector.AddProvider(provider, assembly);
			}
			else if(providerInterface.FullName == typeof(IFilesStorageProviderV30).FullName) {
				_filesProviderCollector.AddProvider(provider, assembly);
			}
			else if(providerInterface.FullName == typeof(IFormatterProviderV30).FullName) {
				_formatterProviderCollector.AddProvider(provider, assembly);
			}
		}

		/// <summary>
		/// Set the global settings storage provider.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="assembly">The assembly.</param>
		public static void AddGlobalSettingsStorageProvider(Type provider, System.Reflection.Assembly assembly) {
			collectorsBox = null;
			_globalSettingsProvider = provider;
			_globalSettingsProviderAssembly = assembly;
		}
	}

}
