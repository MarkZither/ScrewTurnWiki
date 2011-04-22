
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {
	/// <summary>
	/// Contains instances of the Providers Collectors.
	/// </summary>
	public class CollectorsBox : IDisposable, ICollectorsBox {

		/// <summary>
		/// Initializes a new instance of the <see cref="CollectorsBox"/> class.
		/// </summary>
		/// <param name="globalSettingsProvider">The global settings provider Type.</param>
		/// <param name="globalSettingsProviderAssembly">The global settings provider assembly.</param>
		/// <param name="settingsProvider">The settings provider Type.</param>
		/// <param name="settingsProviderAssembly">The settings provider assembly.</param>
		/// <param name="usersProviderCollector">The users provider collector.</param>
		/// <param name="themeProviderCollector">The theme provider collector.</param>
		/// <param name="pagesProviderCollector">The pages provider collector.</param>
		/// <param name="filesProviderCollector">The files provider collector.</param>
		/// <param name="formatterProviderCollector">The formatter provider collector.</param>
		public CollectorsBox(Type globalSettingsProvider,
			System.Reflection.Assembly globalSettingsProviderAssembly,
			Type settingsProvider,
			System.Reflection.Assembly settingsProviderAssembly,
			ProviderCollector<IUsersStorageProviderV30> usersProviderCollector,
			ProviderCollector<IThemeStorageProviderV30> themeProviderCollector,
			ProviderCollector<IPagesStorageProviderV30> pagesProviderCollector,
			ProviderCollector<IFilesStorageProviderV30> filesProviderCollector,
			ProviderCollector<IFormatterProviderV30> formatterProviderCollector) {

			_globalSettingsProviderType = globalSettingsProvider;
			_globalSettingsProviderAssembly = globalSettingsProviderAssembly;
			_settingsProviderType = settingsProvider;
			_settingsProviderAssembly = settingsProviderAssembly;
			_settingsProvider = new Dictionary<string, ISettingsStorageProviderV30>();
			_usersProviderCollector = usersProviderCollector;
			_themeProviderCollector = themeProviderCollector;
			_pagesProviderCollector = pagesProviderCollector;
			_filesProviderCollector = filesProviderCollector;
			_formatterProviderCollector = formatterProviderCollector; ;
		}

		private Type _globalSettingsProviderType;
		private System.Reflection.Assembly _globalSettingsProviderAssembly;
		private IGlobalSettingsStorageProviderV30 _globalSettingsProvider;

		private Type _settingsProviderType;
		private System.Reflection.Assembly _settingsProviderAssembly;
		private Dictionary<string, ISettingsStorageProviderV30> _settingsProvider;

		private ProviderCollector<IUsersStorageProviderV30> _usersProviderCollector;
		private ProviderCollector<IThemeStorageProviderV30> _themeProviderCollector;
		private ProviderCollector<IPagesStorageProviderV30> _pagesProviderCollector;
		private ProviderCollector<IFilesStorageProviderV30> _filesProviderCollector;
		private ProviderCollector<IFormatterProviderV30> _formatterProviderCollector;

		/// <summary>
		/// The global settings storage provider.
		/// </summary>
		/// <returns>The globalSettingsProvider.</returns>
		public IGlobalSettingsStorageProviderV30 GlobalSettingsProvider {
			get {
				if(_globalSettingsProvider == null) {
					_globalSettingsProvider = ProviderLoader.CreateInstance<IGlobalSettingsStorageProviderV30>(_globalSettingsProviderAssembly, _globalSettingsProviderType);
					_globalSettingsProvider.Init(Host.Instance, StartupTools.GetGlobalSettingsStorageProviderConfiguration(), null);
				}
				return _globalSettingsProvider;
			}
		}

		/// <summary>
		/// The settings storage provider.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <returns>The settingsProvider initialized for the given wiki.</returns>
		public ISettingsStorageProviderV30 GetSettingsProvider(string wiki) {
			wiki = wiki != null ? wiki : "-";
			if(!_settingsProvider.ContainsKey(wiki)) {
				_settingsProvider[wiki] = ProviderLoader.CreateInstance<ISettingsStorageProviderV30>(_settingsProviderAssembly, _settingsProviderType);
				_settingsProvider[wiki].Init(Host.Instance, StartupTools.GetSettingsStorageProviderConfiguration(), wiki);
			}
			return _settingsProvider[wiki];
		}

		/// <summary>
		/// The Users Provider Collector instance.
		/// </summary>
		public ProviderCollector<IUsersStorageProviderV30> UsersProviderCollector {
			get {
				return _usersProviderCollector;
			}
		}

		/// <summary>
		/// The Theme Provider Collector istance.
		/// </summary>
		public ProviderCollector<IThemeStorageProviderV30> ThemeProviderCollector {
			get {
				return _themeProviderCollector;
			}
		}

		/// <summary>
		/// The Pages Provider Collector instance.
		/// </summary>
		public ProviderCollector<IPagesStorageProviderV30> PagesProviderCollector {
			get {
				return _pagesProviderCollector;
			}
		}

		/// <summary>
		/// The Files Provider Collector instance.
		/// </summary>
		public ProviderCollector<IFilesStorageProviderV30> FilesProviderCollector {
			get {
				return _filesProviderCollector;
			}
		}

		/// <summary>
		/// The Formatter Provider Collector instance.
		/// </summary>
		public ProviderCollector<IFormatterProviderV30> FormatterProviderCollector {
			get {
				return _formatterProviderCollector;
			}
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose() {
			if(_settingsProvider != null) {
				foreach(var settingsProvider in _settingsProvider.Values) {
					settingsProvider.Dispose();
				}
				_settingsProvider.Clear();
			}
			_usersProviderCollector.Dispose();
			_themeProviderCollector.Dispose();
			_pagesProviderCollector.Dispose();
			_filesProviderCollector.Dispose();
			_formatterProviderCollector.Dispose();
		}

	}
}
