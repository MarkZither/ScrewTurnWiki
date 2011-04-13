
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
		/// <c>true</c> if the collectorsBox has been used
		/// </summary>
		[ThreadStatic]
		public static bool CollectorsBoxUsed = false;

		/// <summary>
		/// Gets the collectors box.
		/// </summary>
		public static ICollectorsBox CollectorsBox {
			get {
				if(collectorsBox == null) {
					collectorsBox = new CollectorsBox(SettingsProvider,
													  SettingsProviderAssembly,
													  UsersProviderCollector.Clone(),
													  ThemeProviderCollector.Clone(),
													  PagesProviderCollector.Clone(),
													  FilesProviderCollector.Clone(),
													  FormatterProviderCollector.Clone(),
													  DisabledUsersProviderCollector.Clone(),
													  DisabledThemeProviderCollector.Clone(),
													  DisabledPagesProviderCollector.Clone(),
													  DisabledFilesProviderCollector.Clone(),
													  DisabledFormatterProviderCollector.Clone());
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
		private static Type SettingsProvider;

		/// <summary>
		/// The settings storage provider assembly.
		/// </summary>
		private static System.Reflection.Assembly SettingsProviderAssembly;

		/// <summary>
		/// The Users Provider Collector instance.
		/// </summary>
		private static ProviderCollector<IUsersStorageProviderV30> UsersProviderCollector;

		/// <summary>
		/// The Theme Provider Collector istance.
		/// </summary>
		private static ProviderCollector<IThemeStorageProviderV30> ThemeProviderCollector;

		/// <summary>
		/// The Pages Provider Collector instance.
		/// </summary>
		private static ProviderCollector<IPagesStorageProviderV30> PagesProviderCollector;

		/// <summary>
		/// The Files Provider Collector instance.
		/// </summary>
		private static ProviderCollector<IFilesStorageProviderV30> FilesProviderCollector;

		/// <summary>
		/// The Formatter Provider Collector instance.
		/// </summary>
		private static ProviderCollector<IFormatterProviderV30> FormatterProviderCollector;

		/// <summary>
		/// The Disabled Users Provider Collector instance.
		/// </summary>
		private static ProviderCollector<IUsersStorageProviderV30> DisabledUsersProviderCollector;

		/// <summary>
		/// The Disabled Files Provider Collector instance.
		/// </summary>
		private static ProviderCollector<IFilesStorageProviderV30> DisabledFilesProviderCollector;

		/// <summary>
		/// The Disabled Pages Provider Collector instance.
		/// </summary>
		private static ProviderCollector<IPagesStorageProviderV30> DisabledPagesProviderCollector;

		/// <summary>
		/// The Disabled Theme Provider Collector instance.
		/// </summary>
		private static ProviderCollector<IThemeStorageProviderV30> DisabledThemeProviderCollector;

		/// <summary>
		/// The Disabled Formatter Provider Collector instance.
		/// </summary>
		private static ProviderCollector<IFormatterProviderV30> DisabledFormatterProviderCollector;

		/// <summary>
		/// Initializes the collectors.
		/// </summary>
		public static void InitCollectors() {
			UsersProviderCollector = new ProviderCollector<IUsersStorageProviderV30>();
			PagesProviderCollector = new ProviderCollector<IPagesStorageProviderV30>();
			FilesProviderCollector = new ProviderCollector<IFilesStorageProviderV30>();
			ThemeProviderCollector = new ProviderCollector<IThemeStorageProviderV30>();
			FormatterProviderCollector = new ProviderCollector<IFormatterProviderV30>();
			DisabledUsersProviderCollector = new ProviderCollector<IUsersStorageProviderV30>();
			DisabledPagesProviderCollector = new ProviderCollector<IPagesStorageProviderV30>();
			DisabledFilesProviderCollector = new ProviderCollector<IFilesStorageProviderV30>();
			DisabledThemeProviderCollector = new ProviderCollector<IThemeStorageProviderV30>();
			DisabledFormatterProviderCollector = new ProviderCollector<IFormatterProviderV30>();
		}

		/// <summary>
		/// Finds a provider.
		/// </summary>
		/// <param name="typeName">The provider type name.</param>
		/// <param name="enabled">A value indicating whether the provider is enabled.</param>
		/// <param name="canDisable">A value indicating whether the provider can be disabled.</param>
		/// <returns>The provider, or <c>null</c>.</returns>
		public static IProviderV30 FindProvider(string typeName, out bool enabled, out bool canDisable) {
			enabled = true;
			canDisable = true;
			IProviderV30 prov = null;

			prov = PagesProviderCollector.GetProvider(typeName);
			canDisable = typeName != Settings.DefaultPagesProvider;
			if(prov == null) {
				prov = DisabledPagesProviderCollector.GetProvider(typeName);
				if(prov != null) enabled = false;
			}
			if(prov != null) return prov;

			prov = UsersProviderCollector.GetProvider(typeName);
			canDisable = typeName != Settings.DefaultUsersProvider;
			if(prov == null) {
				prov = DisabledUsersProviderCollector.GetProvider(typeName);
				if(prov != null) enabled = false;
			}
			if(prov != null) return prov;

			prov = FilesProviderCollector.GetProvider(typeName);
			canDisable = typeName != Settings.DefaultFilesProvider;
			if(prov == null) {
				prov = DisabledFilesProviderCollector.GetProvider(typeName);
				if(prov != null) enabled = false;
			}
			if(prov != null) return prov;

			prov = ThemeProviderCollector.GetProvider(typeName);
			canDisable = true;
			if(prov == null) {
				prov = DisabledThemeProviderCollector.GetProvider(typeName);
				if(prov != null) enabled = false;
			}
			if(prov != null) return prov;

			prov = FormatterProviderCollector.GetProvider(typeName);
			if(prov == null) {
				prov = DisabledFormatterProviderCollector.GetProvider(typeName);
				if(prov != null) enabled = false;
			}
			if(prov != null) return prov;

			return null;
		}

		/// <summary>
		/// Tries to unload a provider.
		/// </summary>
		/// <param name="typeName">The provider.</param>
		public static void TryUnload(string typeName) {
			bool enabled, canDisable;
			IProviderV30 prov = FindProvider(typeName, out enabled, out canDisable);

			PagesProviderCollector.RemoveProvider(prov.GetType());
			DisabledPagesProviderCollector.RemoveProvider(prov.GetType());

			UsersProviderCollector.RemoveProvider(prov.GetType());
			DisabledUsersProviderCollector.RemoveProvider(prov.GetType());

			FilesProviderCollector.RemoveProvider(prov.GetType());
			DisabledFilesProviderCollector.RemoveProvider(prov.GetType());

			FormatterProviderCollector.RemoveProvider(prov.GetType());
			DisabledFormatterProviderCollector.RemoveProvider(prov.GetType());
		}

		/// <summary>
		/// Tries to disable a provider.
		/// </summary>
		/// <param name="typeName">The provider.</param>
		public static void TryDisable(string typeName) {
			IProviderV30 prov = null;

			prov = PagesProviderCollector.GetProvider(typeName);
			if(prov != null) {
				DisabledPagesProviderCollector.AddProvider(prov.GetType(), PagesProviderCollector.GetAssembly(prov.GetType()));
				PagesProviderCollector.RemoveProvider(prov.GetType());
				return;
			}

			prov = UsersProviderCollector.GetProvider(typeName);
			if(prov != null) {
				DisabledUsersProviderCollector.AddProvider(prov.GetType(), UsersProviderCollector.GetAssembly(prov.GetType()));
				UsersProviderCollector.RemoveProvider(prov.GetType());
				return;
			}

			prov = FilesProviderCollector.GetProvider(typeName);
			if(prov != null) {
				DisabledFilesProviderCollector.AddProvider(prov.GetType(), FilesProviderCollector.GetAssembly(prov.GetType()));
				FilesProviderCollector.RemoveProvider(prov.GetType());
				return;
			}

			prov = FormatterProviderCollector.GetProvider(typeName);
			if(prov != null) {
				DisabledFormatterProviderCollector.AddProvider(prov.GetType(), FormatterProviderCollector.GetAssembly(prov.GetType()));
				FormatterProviderCollector.RemoveProvider(prov.GetType());
				return;
			}
		}

		/// <summary>
		/// Tries to enable a provider.
		/// </summary>
		/// <param name="typeName">The provider.</param>
		public static void TryEnable(string typeName) {
			IProviderV30 prov = null;

			prov = DisabledPagesProviderCollector.GetProvider(typeName);
			if(prov != null) {
				PagesProviderCollector.AddProvider(prov.GetType(), DisabledPagesProviderCollector.GetAssembly(prov.GetType()));
				DisabledPagesProviderCollector.RemoveProvider(prov.GetType());
				return;
			}

			prov = DisabledUsersProviderCollector.GetProvider(typeName);
			if(prov != null) {
				UsersProviderCollector.AddProvider(prov.GetType(), DisabledUsersProviderCollector.GetAssembly(prov.GetType()));
				DisabledUsersProviderCollector.RemoveProvider(prov.GetType());
				return;
			}

			prov = DisabledFilesProviderCollector.GetProvider(typeName);
			if(prov != null) {
				FilesProviderCollector.AddProvider(prov.GetType(), DisabledFilesProviderCollector.GetAssembly(prov.GetType()));
				DisabledFilesProviderCollector.RemoveProvider(prov.GetType());
				return;
			}

			prov = DisabledFormatterProviderCollector.GetProvider(typeName);
			if(prov != null) {
				FormatterProviderCollector.AddProvider(prov.GetType(), DisabledFormatterProviderCollector.GetAssembly(prov.GetType()));
				DisabledFormatterProviderCollector.RemoveProvider(prov.GetType());
				return;
			}
		}

		/// <summary>
		/// Gets the names of all known providers, both enabled and disabled.
		/// </summary>
		/// <returns>The names of the known providers.</returns>
		public static string[] GetAllProviders() {
			List<string> result = new List<string>(20);

			foreach(IProviderV30 prov in PagesProviderCollector.AllProviders) result.Add(prov.GetType().FullName);
			foreach(IProviderV30 prov in DisabledPagesProviderCollector.AllProviders) result.Add(prov.GetType().FullName);

			foreach(IProviderV30 prov in ThemeProviderCollector.AllProviders) result.Add(prov.GetType().FullName);
			foreach(IProviderV30 prov in DisabledThemeProviderCollector.AllProviders) result.Add(prov.GetType().FullName);

			foreach(IProviderV30 prov in UsersProviderCollector.AllProviders) result.Add(prov.GetType().FullName);
			foreach(IProviderV30 prov in DisabledUsersProviderCollector.AllProviders) result.Add(prov.GetType().FullName);

			foreach(IProviderV30 prov in FilesProviderCollector.AllProviders) result.Add(prov.GetType().FullName);
			foreach(IProviderV30 prov in DisabledFilesProviderCollector.AllProviders) result.Add(prov.GetType().FullName);

			foreach(IProviderV30 prov in FormatterProviderCollector.AllProviders) result.Add(prov.GetType().FullName);
			foreach(IProviderV30 prov in DisabledFormatterProviderCollector.AllProviders) result.Add(prov.GetType().FullName);

			return result.ToArray();
		}

		/// <summary>
		/// Adds the given provider to the appropriate collector.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="assembly">The assembly.</param>
		/// <param name="providerInterface">The provider interface.</param>
		/// <param name="enabled">if set to <c>true</c> enabled.</param>
		public static void AddProvider(Type provider, System.Reflection.Assembly assembly, Type providerInterface, bool enabled) {
			collectorsBox = null;
			if(providerInterface.FullName == typeof(IPagesStorageProviderV30).FullName) {
				if(enabled) {
					PagesProviderCollector.AddProvider(provider, assembly);
				}
				else {
					DisabledPagesProviderCollector.AddProvider(provider, assembly);
				}
			}
			else if(providerInterface.FullName == typeof(IThemeStorageProviderV30).FullName) {
				if(enabled) {
					ThemeProviderCollector.AddProvider(provider, assembly);
				}
				else {
					DisabledThemeProviderCollector.AddProvider(provider, assembly);
				}
			}
			else if(providerInterface.FullName == typeof(IUsersStorageProviderV30).FullName) {
				if(enabled) {
					UsersProviderCollector.AddProvider(provider, assembly);
				}
				else {
					DisabledUsersProviderCollector.AddProvider(provider, assembly);
				}
			}
			else if(providerInterface.FullName == typeof(IFilesStorageProviderV30).FullName) {
				if(enabled) {
					FilesProviderCollector.AddProvider(provider, assembly);
				}
				else {
					DisabledFilesProviderCollector.AddProvider(provider, assembly);
				}
			}
			else if(providerInterface.FullName == typeof(IFormatterProviderV30).FullName) {
				if(enabled) {
					FormatterProviderCollector.AddProvider(provider, assembly);
				}
				else {
					DisabledFormatterProviderCollector.AddProvider(provider, assembly);
				}
			}
		}

		/// <summary>
		/// Sets the settings storage provider.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="assembly">The assembly.</param>
		public static void AddSettingsProvider(Type provider, System.Reflection.Assembly assembly) {
			SettingsProvider = provider;
			SettingsProviderAssembly = assembly;
		}
	}

}
