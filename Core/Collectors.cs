
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
		/// Contains the configuration strings of each storage provider.
		/// </summary>
		private static Dictionary<string, string> StorageProvidersConfigurations;
		
		/// <summary>
		/// The settings storage provider.
		/// </summary>
		private static Type _settingsProvider;

		/// <summary>
		/// The settings storage provider assembly.
		/// </summary>
		private static System.Reflection.Assembly _settingsProviderAssembly;

		/// <summary>
		/// The global settings storage provider.
		/// </summary>
		private static Type _globalSettingsProvider;

		/// <summary>
		/// The global settings storage provider assembly.
		/// </summary>
		private static System.Reflection.Assembly _globalSettingsProviderAssembly;
		
		/// <summary>
		/// The Users Provider Collector instance.
		/// </summary>
		private static ProviderCollector<IUsersStorageProviderV40> _usersProviderCollector;

		/// <summary>
		/// The Theme Provider Collector istance.
		/// </summary>
		private static ProviderCollector<IThemesStorageProviderV40> _themeProviderCollector;

		/// <summary>
		/// The Pages Provider Collector instance.
		/// </summary>
		private static ProviderCollector<IPagesStorageProviderV40> _pagesProviderCollector;

		/// <summary>
		/// The Files Provider Collector instance.
		/// </summary>
		private static ProviderCollector<IFilesStorageProviderV40> _filesProviderCollector;

		/// <summary>
		/// The Formatter Provider Collector instance.
		/// </summary>
		private static ProviderCollector<IFormatterProviderV40> _formatterProviderCollector;


		/// <summary>
		/// Initializes the collectors.
		/// </summary>
		public static void InitCollectors() {
			StorageProvidersConfigurations = new Dictionary<string, string>();
			_usersProviderCollector = new ProviderCollector<IUsersStorageProviderV40>();
			_pagesProviderCollector = new ProviderCollector<IPagesStorageProviderV40>();
			_filesProviderCollector = new ProviderCollector<IFilesStorageProviderV40>();
			_themeProviderCollector = new ProviderCollector<IThemesStorageProviderV40>();
			_formatterProviderCollector = new ProviderCollector<IFormatterProviderV40>();
		}
		
		/// <summary>
		/// Tries to unload a provider.
		/// </summary>
		/// <param name="typeName">The provider.</param>
		public static void TryUnloadPlugin(string typeName) {
			_formatterProviderCollector.RemoveProvider(typeName);
			collectorsBox = null;
		}

		/// <summary>
		/// Adds the given provider to the appropriate collector.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="assembly">The provider assembly.</param>
		/// <param name="configuration">The provider configuration.</param>
		/// <param name="providerInterface">The provider interface.</param>
		public static void AddProvider(Type provider, System.Reflection.Assembly assembly, string configuration, Type providerInterface) {
			collectorsBox = null;
			if(providerInterface.FullName == typeof(ISettingsStorageProviderV40).FullName) {
				StorageProvidersConfigurations.Add(provider.FullName, configuration);
				_settingsProvider = provider;
				_settingsProviderAssembly = assembly;
			}
			else if(providerInterface.FullName == typeof(IPagesStorageProviderV40).FullName) {
				StorageProvidersConfigurations.Add(provider.FullName, configuration);
				_pagesProviderCollector.AddProvider(provider, assembly);
			}
			else if(providerInterface.FullName == typeof(IThemesStorageProviderV40).FullName) {
				StorageProvidersConfigurations.Add(provider.FullName, configuration);
				_themeProviderCollector.AddProvider(provider, assembly);
			}
			else if(providerInterface.FullName == typeof(IUsersStorageProviderV40).FullName) {
				StorageProvidersConfigurations.Add(provider.FullName, configuration);
				_usersProviderCollector.AddProvider(provider, assembly);
			}
			else if(providerInterface.FullName == typeof(IFilesStorageProviderV40).FullName) {
				StorageProvidersConfigurations.Add(provider.FullName, configuration);
				_filesProviderCollector.AddProvider(provider, assembly);
			}
			else {
				throw new Exception("The provider with the given interface (" + providerInterface.FullName + ") could not be added to a colletor.");
			}
		}

		/// <summary>
		/// Adds the gine plugin (formatter provider) to the appropriate collector..
		/// </summary>
		/// <param name="plugin">The plugin.</param>
		/// <param name="assembly">The assembly.</param>
		public static void AddPlugin(Type plugin, System.Reflection.Assembly assembly) {
			_formatterProviderCollector.AddProvider(plugin, assembly);
			FileNames[plugin.GetType().FullName] = assembly.FullName;
			collectorsBox = null;
		}

		/// <summary>
		/// Set the global settings storage provider.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="assembly">The provider assembly.</param>
		public static void AddGlobalSettingsStorageProvider(Type provider, System.Reflection.Assembly assembly) {
			collectorsBox = null;
			_globalSettingsProvider = provider;
			_globalSettingsProviderAssembly = assembly;
		}

		/// <summary>
		/// Gets the storage provider configuration.
		/// </summary>
		/// <param name="typeName">The Type Name of the Storage Provider.</param>
		/// <returns>The configuration string.</returns>
		public static string GetStorageProviderConfiguration(string typeName) {
			string configuration;
	  		if(StorageProvidersConfigurations.TryGetValue(typeName, out configuration)) return configuration;
			return "";
		}

		/// <summary>
		/// Finds a provider.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="typeName">The provider type name.</param>
		/// <param name="enabled">A value indicating whether the provider is enabled.</param>
		/// <returns>The provider, or <c>null</c>.</returns>
		public static IProviderV40 FindProvider(string wiki, string typeName, out bool enabled) {
			enabled = false;
			IProviderV40 prov = null;

			prov = CollectorsBox.FormatterProviderCollector.GetProvider(typeName, wiki);
			if(prov != null) {
				enabled = Settings.GetProvider(wiki).GetPluginStatus(typeName);
				return prov;
			}

			return null;
		}

	}

}
