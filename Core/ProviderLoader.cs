
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using ScrewTurn.Wiki.PluginFramework;
using System.Globalization;
using System.Web.Configuration;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Loads providers from assemblies.
	/// </summary>
	public static class ProviderLoader {

		// These must be const because they are used in switch constructs
		internal const string UsersProviderInterfaceName = "ScrewTurn.Wiki.PluginFramework.IUsersStorageProviderV30";
		internal const string PagesProviderInterfaceName = "ScrewTurn.Wiki.PluginFramework.IPagesStorageProviderV30";
		internal const string FilesProviderInterfaceName = "ScrewTurn.Wiki.PluginFramework.IFilesStorageProviderV30";
		internal const string FormatterProviderInterfaceName = "ScrewTurn.Wiki.PluginFramework.IFormatterProviderV30";

		internal static string SettingsStorageProviderAssemblyName = "";

		/// <summary>
		/// Verifies the read-only/read-write constraints of providers.
		/// </summary>
		/// <typeparam name="T">The type of the provider.</typeparam>
		/// <param name="provider">The provider.</param>
		/// <exception cref="T:ProviderConstraintException">Thrown when a constraint is not fulfilled.</exception>
		private static void VerifyConstraints<T>(T provider) {
			if(typeof(T) == typeof(IUsersStorageProviderV30)) {
				// If the provider allows to write user accounts data, then group membership must be writeable too
				IUsersStorageProviderV30 actualInstance = (IUsersStorageProviderV30)provider;
				if(!actualInstance.UserAccountsReadOnly && actualInstance.GroupMembershipReadOnly) {
					throw new ProviderConstraintException("If UserAccountsReadOnly is false, then also GroupMembershipReadOnly must be false");
				}
			}
		}

		/// <summary>
		/// Creates an instance of a type implementing a provider interface.
		/// </summary>
		/// <typeparam name="T">The provider interface type.</typeparam>
		/// <param name="asm">The assembly that contains the type.</param>
		/// <param name="type">The type to create an instance of.</param>
		/// <returns>The instance, or <c>null</c>.</returns>
		public static T CreateInstance<T>(Assembly asm, Type type) where T : class, IProviderV30 {
			T instance;
			try {
				instance = asm.CreateInstance(type.ToString()) as T;
				return instance;
			}
			catch {
				Log.LogEntry("Unable to create instance of " + type.ToString(), EntryType.Error, Log.SystemUsername);
				throw;
			}
		}

		/// <summary>
		/// Try to setup a provider.
		/// </summary>
		/// <typeparam name="T">The type of the provider, which must implement <b>IProvider</b>.</typeparam>
		/// <param name="provider">The provider to setup.</param>
		/// <param name="configuration">The configuration string.</param>
		public static void SetUp<T>(Type provider, string configuration) where T : class, IProviderV30 {
			T providerInstance = ProviderLoader.CreateInstance<T>(Assembly.GetAssembly(provider), provider);
			providerInstance.SetUp(Host.Instance, configuration);

			// Verify constraints
			VerifyConstraints<T>(providerInstance);

			// Dispose the provider
			providerInstance.Dispose();
		}

		/// <summary>
		/// Tries to inizialize a provider.
		/// </summary>
		/// <typeparam name="T">The type of the provider, which must implement <b>IProvider</b>.</typeparam>
		/// <param name="instance">The provider instance to initialize.</param>
		/// <param name="configuration">The configuration string.</param>
		/// <param name="wiki">The wiki that needs the provider.</param>
		public static void Initialize<T>(T instance, string configuration, string wiki) where T : class, IProviderV30 {
			bool enabled = !IsDisabled(instance.GetType().FullName);
			try {
				if(enabled) {
					instance.Init(Host.Instance, configuration, wiki);
				}
			}
			catch(InvalidConfigurationException) {
				// Disable Provider
				enabled = false;
				Log.LogEntry("Unable to load provider " + instance.Information.Name + " (configuration rejected), disabling it", EntryType.Error, Log.SystemUsername);
				SaveStatus(instance.GetType().FullName, false);
			}
			catch {
				// Disable Provider
				enabled = false;
				Log.LogEntry("Unable to load provider " + instance.Information.Name + " (unknown error), disabling it", EntryType.Error, Log.SystemUsername);
				SaveStatus(instance.GetType().FullName, false);
				throw; // Exception is rethrown because it's not a normal condition
			}

			Log.LogEntry("Provider " + instance.Information.Name + " loaded (" + (enabled ? "Enabled" : "Disabled") + ")", EntryType.General, Log.SystemUsername);
		}

		/// <summary>
		/// Loads the Configuration data of a Provider.
		/// </summary>
		/// <param name="typeName">The Type Name of the Provider.</param>
		/// <returns>The Configuration, if available, otherwise an empty string.</returns>
		public static string LoadConfiguration(string typeName) {
			return GlobalSettings.Provider.GetPluginConfiguration(typeName);
		}

		/// <summary>
		/// Saves the Configuration data of a Provider.
		/// </summary>
		/// <param name="typeName">The Type Name of the Provider.</param>
		/// <param name="config">The Configuration data to save.</param>
		public static void SaveConfiguration(string typeName, string config) {
			GlobalSettings.Provider.SetPluginConfiguration(typeName, config);
		}

		/// <summary>
		/// Saves the Status of a Provider.
		/// </summary>
		/// <param name="typeName">The Type Name of the Provider.</param>
		/// <param name="enabled">A value specifying whether or not the Provider is enabled.</param>
		public static void SaveStatus(string typeName, bool enabled) {
			GlobalSettings.Provider.SetPluginStatus(typeName, enabled);
		}

		/// <summary>
		/// Returns a value specifying whether or not a Provider is disabled.
		/// </summary>
		/// <param name="typeName">The Type Name of the Provider.</param>
		/// <returns>True if the Provider is disabled.</returns>
		public static bool IsDisabled(string typeName) {
			return !GlobalSettings.Provider.GetPluginStatus(typeName);
		}

		/// <summary>
		/// Loads Formatter Providers from an assembly.
		/// </summary>
		/// <param name="assembly">The path of the Assembly to load the Providers from.</param>
		public static int LoadFormatterProvidersFromAuto(string assembly) {
			Type[] forms;
			LoadFormatterProvidersFromAssembly(assembly, out forms);

			int count = 0;

			// Setup and add to the Collectors
			for(int i = 0; i < forms.Length; i++) {
				SetUp<IFormatterProviderV30>(forms[i], LoadConfiguration(forms[i].FullName));
				Collectors.AddProvider(forms[i], Assembly.GetAssembly(forms[i]), typeof(IFormatterProviderV30));
				count++;
			}

			return count;
		}

		/// <summary>
		/// Loads Providers from an assembly.
		/// </summary>
		/// <param name="assembly">The path of the Assembly to load the Providers from.</param>
		/// <param name="formatters">The Formatter Providers.</param>
		/// <remarks>The Components returned are <b>not</b> initialized.</remarks>
		public static void LoadFormatterProvidersFromAssembly(string assembly, out Type[] formatters) {

			Assembly asm = null;
			try {
				//asm = Assembly.LoadFile(assembly);
				// This way the DLL is not locked and can be deleted at runtime
				asm = Assembly.Load(LoadAssemblyFromProvider(Path.GetFileName(assembly)));
			}
			catch {
				formatters = new Type[0];

				Log.LogEntry("Unable to load assembly " + Path.GetFileNameWithoutExtension(assembly), EntryType.Error, Log.SystemUsername);
				return;
			}

			Type[] types = null;

			try {
				types = asm.GetTypes();
			}
			catch(ReflectionTypeLoadException) {
				formatters = new Type[0];

				Log.LogEntry("Unable to load providers from (probably v2) assembly " + Path.GetFileNameWithoutExtension(assembly), EntryType.Error, Log.SystemUsername);
				return;
			}

			List<Type> frs = new List<Type>();
			
			Type[] interfaces;
			for(int i = 0; i < types.Length; i++) {
				// Avoid to load abstract classes as they cannot be instantiated
				if(types[i].IsAbstract) continue;

				interfaces = types[i].GetInterfaces();
				foreach(Type iface in interfaces) {
					if(iface == typeof(IFormatterProviderV30)) {
						frs.Add(types[i]);
						Collectors.FileNames[types[i].FullName] = assembly;
					}
				}
			}

			formatters = frs.ToArray();
		}

		/// <summary>
		/// Loads the content of an assembly from disk.
		/// </summary>
		/// <param name="assembly">The assembly file full path.</param>
		/// <returns>The content of the assembly, in a byte array form.</returns>
		private static byte[] LoadAssemblyFromDisk(string assembly) {
			return File.ReadAllBytes(assembly);
		}

		/// <summary>
		/// Loads the content of an assembly from the settings provider.
		/// </summary>
		/// <param name="assemblyName">The name of the assembly, such as "Assembly.dll".</param>
		/// <returns>The content fo the assembly.</returns>
		private static byte[] LoadAssemblyFromProvider(string assemblyName) {
			return GlobalSettings.Provider.RetrievePluginAssembly(assemblyName);
		}

		/// <summary>
		/// Loads the proper Global Setting Storage Provider, given its name.
		/// </summary>
		/// <param name="name">The fully qualified name (such as "Namespace.ProviderClass, MyAssembly"), or <c>null</c>/<b>String.Empty</b>/"<b>default</b>" for the default provider.</param>
		/// <returns>The global settings storage provider.</returns>
		public static IGlobalSettingsStorageProviderV30 LoadGlobalSettingsStorageProvider(string name) {
			if(name == null || name.Length == 0 || string.Compare(name, "default", true, CultureInfo.InvariantCulture) == 0) {
				return new GlobalSettingsStorageProvider();
			}

			IGlobalSettingsStorageProviderV30 result = null;

			Exception inner = null;

			if(name.Contains(",")) {
				string[] fields = name.Split(',');
				if(fields.Length == 2) {
					fields[0] = fields[0].Trim(' ', '"');
					fields[1] = fields[1].Trim(' ', '"');
					try {
						// assemblyName should be an absolute path or a relative path in bin or public\Plugins

						Assembly asm;
						Type t;
						string assemblyName = fields[1];
						if(!assemblyName.ToLowerInvariant().EndsWith(".dll")) assemblyName += ".dll";

						if(File.Exists(assemblyName)) {
							asm = Assembly.Load(LoadAssemblyFromDisk(assemblyName));
							t = asm.GetType(fields[0]);
							SettingsStorageProviderAssemblyName = Path.GetFileName(assemblyName);
						}
						else {
							string tentativePluginsPath = null;
							try {
								// Settings.PublicDirectory is only available when running the web app
								tentativePluginsPath = Path.Combine(GlobalSettings.PublicDirectory, "Plugins");
								tentativePluginsPath = Path.Combine(tentativePluginsPath, assemblyName);
							}
							catch { }

							if(!string.IsNullOrEmpty(tentativePluginsPath) && File.Exists(tentativePluginsPath)) {
								asm = Assembly.Load(LoadAssemblyFromDisk(tentativePluginsPath));
								t = asm.GetType(fields[0]);
								SettingsStorageProviderAssemblyName = Path.GetFileName(tentativePluginsPath);
							}
							else {
								// Trim .dll
								t = Type.GetType(fields[0] + "," + assemblyName.Substring(0, assemblyName.Length - 4), true, true);
								SettingsStorageProviderAssemblyName = assemblyName;
							}
						}

						result = t.GetConstructor(new Type[0]).Invoke(new object[0]) as IGlobalSettingsStorageProviderV30;
					}
					catch(Exception ex) {
						inner = ex;
						result = null;
					}
				}
			}

			if(result == null) throw new ArgumentException("Could not load the specified Global Settings Storage Provider", inner);
			else return result;
		}

		/// <summary>
		/// Loads the proper Setting Storage Provider, given its name.
		/// </summary>
		/// <param name="name">The fully qualified name (such as "Namespace.ProviderClass, MyAssembly"), or <c>null</c>/<b>String.Empty</b>/"<b>default</b>" for the default provider.</param>
		/// <returns>The global settings storage provider.</returns>
		public static ISettingsStorageProviderV30 LoadSettingsStorageProvider(string name) {
			if(name == null || name.Length == 0 || string.Compare(name, "default", true, CultureInfo.InvariantCulture) == 0) {
				return new SettingsStorageProvider();
			}

			ISettingsStorageProviderV30 result = null;

			Exception inner = null;

			if(name.Contains(",")) {
				string[] fields = name.Split(',');
				if(fields.Length == 2) {
					fields[0] = fields[0].Trim(' ', '"');
					fields[1] = fields[1].Trim(' ', '"');
					try {
						// assemblyName should be an absolute path or a relative path in bin or public\Plugins

						Assembly asm;
						Type t;
						string assemblyName = fields[1];
						if(!assemblyName.ToLowerInvariant().EndsWith(".dll")) assemblyName += ".dll";

						if(File.Exists(assemblyName)) {
							asm = Assembly.Load(LoadAssemblyFromDisk(assemblyName));
							t = asm.GetType(fields[0]);
							SettingsStorageProviderAssemblyName = Path.GetFileName(assemblyName);
						}
						else {
							string tentativePluginsPath = null;
							try {
								// Settings.PublicDirectory is only available when running the web app
								tentativePluginsPath = Path.Combine(GlobalSettings.PublicDirectory, "Plugins");
								tentativePluginsPath = Path.Combine(tentativePluginsPath, assemblyName);
							}
							catch { }

							if(!string.IsNullOrEmpty(tentativePluginsPath) && File.Exists(tentativePluginsPath)) {
								asm = Assembly.Load(LoadAssemblyFromDisk(tentativePluginsPath));
								t = asm.GetType(fields[0]);
								SettingsStorageProviderAssemblyName = Path.GetFileName(tentativePluginsPath);
							}
							else {
								// Trim .dll
								t = Type.GetType(fields[0] + "," + assemblyName.Substring(0, assemblyName.Length - 4), true, true);
								SettingsStorageProviderAssemblyName = assemblyName;
							}
						}

						result = t.GetConstructor(new Type[0]).Invoke(new object[0]) as ISettingsStorageProviderV30;
					}
					catch(Exception ex) {
						inner = ex;
						result = null;
					}
				}
			}

			if(result == null) throw new ArgumentException("Could not load the specified Global Settings Storage Provider", inner);
			else return result;
		}

		/// <summary>
		/// Loads the storage providers.
		/// </summary>
		/// <typeparam name="T">The provider interface type</typeparam>
		/// <param name="storageProviders">A list of StorageProvider.</param>
		public static void LoadStorageProviders<T>(List<StorageProvider> storageProviders) where T : class, IProviderV30 {
			foreach(StorageProvider storageProvider in storageProviders) {
				try {
					// assemblyName should be an absolute path or a relative path in bin or public\Plugins
					Assembly asm = null;
					Type t;
					string assemblyName = storageProvider.AssemblyName;
					if(!assemblyName.ToLowerInvariant().EndsWith(".dll")) assemblyName += ".dll";

					if(File.Exists(assemblyName)) {
						asm = Assembly.Load(LoadAssemblyFromDisk(assemblyName));
						t = asm.GetType(storageProvider.TypeName);
						SettingsStorageProviderAssemblyName = Path.GetFileName(assemblyName);
					}
					else {
						string tentativePluginsPath = null;
						try {
							// Settings.PublicDirectory is only available when running the web app
							tentativePluginsPath = Path.Combine(GlobalSettings.PublicDirectory, "Plugins");
							tentativePluginsPath = Path.Combine(tentativePluginsPath, assemblyName);
						}
						catch { }

						if(!string.IsNullOrEmpty(tentativePluginsPath) && File.Exists(tentativePluginsPath)) {
							asm = Assembly.Load(LoadAssemblyFromDisk(tentativePluginsPath));
							t = asm.GetType(storageProvider.TypeName);
							SettingsStorageProviderAssemblyName = Path.GetFileName(tentativePluginsPath);
						}
						else {
							// Trim .dll
							t = Type.GetType(storageProvider.TypeName + "," + assemblyName.Substring(0, assemblyName.Length - 4), true, true);
							SettingsStorageProviderAssemblyName = assemblyName;
						}
					}

					Collectors.AddProvider(t, asm, typeof(T));
					SetUp<T>(t, storageProvider.ConfigurationString);
				}
				catch(Exception ex) {
					throw new ArgumentException("Could not load the provider with name: " + storageProvider, ex);
				}
			}
		}

		/// <summary>
		/// Loads all formatter providers from dlls.
		/// </summary>
		public static void LoadAllFormatterProviders() {
			string[] pluginAssemblies = GlobalSettings.Provider.ListPluginAssemblies();

			List<Type> forms = new List<Type>(2);

			for(int i = 0; i < pluginAssemblies.Length; i++) {
				Type[] f;
				LoadFormatterProvidersFromAssembly(pluginAssemblies[i], out f);
				forms.AddRange(f);
			}

			// Add to the Collectors and Setup
			for(int i = 0; i < forms.Count; i++) {
				SetUp<IFormatterProviderV30>(forms[i], LoadConfiguration(forms[i].FullName));
				Collectors.AddProvider(forms[i], Assembly.GetAssembly(forms[i]), typeof(IFormatterProviderV30));
			}
		}

		/// <summary>
		/// Tries to change a provider's configuration.
		/// </summary>
		/// <param name="typeName">The provider.</param>
		/// <param name="configuration">The new configuration.</param>
		/// <param name="error">The error message, if any.</param>
		/// <returns><c>true</c> if the configuration is saved, <c>false</c> if the provider rejected it.</returns>
		public static bool TryChangeConfiguration(string typeName, string configuration, out string error) {
			error = null;

			//bool enabled, canDisable;
			//IProviderV30 provider = Collectors.FindProvider(typeName, out enabled, out canDisable);

			//try {
			//    provider.Init(Host.Instance, configuration);
			//}
			//catch(InvalidConfigurationException icex) {
			//    error = icex.Message;
			//    return false;
			//}

			SaveConfiguration(typeName, configuration);
			return true;
		}

		/// <summary>
		/// Unloads a provider from memory.
		/// </summary>
		/// <param name="typeName">The provider to unload.</param>
		public static void UnloadProvider(string typeName) {
			Collectors.TryUnload(typeName);
		}

	}

	/// <summary>
	/// Defines an exception thrown when a constraint is not fulfilled by a provider.
	/// </summary>
	public class ProviderConstraintException : Exception {

		/// <summary>
		/// Initializes a new instance of the <see cref="T:ProviderConstraintException" /> class.
		/// </summary>
		/// <param name="message">The message.</param>
		public ProviderConstraintException(string message)
			: base(message) { }

	}

}
