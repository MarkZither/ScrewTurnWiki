
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
			ProviderCollector<IUsersStorageProviderV40> usersProviderCollector,
			ProviderCollector<IThemeStorageProviderV40> themeProviderCollector,
			ProviderCollector<IPagesStorageProviderV40> pagesProviderCollector,
			ProviderCollector<IFilesStorageProviderV40> filesProviderCollector,
			ProviderCollector<IFormatterProviderV40> formatterProviderCollector) {

			_globalSettingsProviderType = globalSettingsProvider;
			_globalSettingsProviderAssembly = globalSettingsProviderAssembly;
			_settingsProviderType = settingsProvider;
			_settingsProviderAssembly = settingsProviderAssembly;
			_settingsProvider = new Dictionary<string, ISettingsStorageProviderV40>();
			_usersProviderCollector = usersProviderCollector;
			_themeProviderCollector = themeProviderCollector;
			_pagesProviderCollector = pagesProviderCollector;
			_filesProviderCollector = filesProviderCollector;
			_formatterProviderCollector = formatterProviderCollector; ;
		}

		private Type _globalSettingsProviderType;
		private System.Reflection.Assembly _globalSettingsProviderAssembly;
		private IGlobalSettingsStorageProviderV40 _globalSettingsProvider;

		private Type _settingsProviderType;
		private System.Reflection.Assembly _settingsProviderAssembly;
		private Dictionary<string, ISettingsStorageProviderV40> _settingsProvider;

		private ProviderCollector<IUsersStorageProviderV40> _usersProviderCollector;
		private ProviderCollector<IThemeStorageProviderV40> _themeProviderCollector;
		private ProviderCollector<IPagesStorageProviderV40> _pagesProviderCollector;
		private ProviderCollector<IFilesStorageProviderV40> _filesProviderCollector;
		private ProviderCollector<IFormatterProviderV40> _formatterProviderCollector;

		/// <summary>
		/// The global settings storage provider.
		/// </summary>
		/// <returns>The globalSettingsProvider.</returns>
		public IGlobalSettingsStorageProviderV40 GlobalSettingsProvider {
			get {
				if(_globalSettingsProvider == null) {
					_globalSettingsProvider = ProviderLoader.CreateInstance<IGlobalSettingsStorageProviderV40>(_globalSettingsProviderAssembly, _globalSettingsProviderType);
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
		public ISettingsStorageProviderV40 GetSettingsProvider(string wiki) {
			string wikiKey = wiki != null ? wiki : "-";
			if(!_settingsProvider.ContainsKey(wikiKey)) {
				_settingsProvider[wikiKey] = ProviderLoader.CreateInstance<ISettingsStorageProviderV40>(_settingsProviderAssembly, _settingsProviderType);
				_settingsProvider[wikiKey].Init(Host.Instance, ProviderLoader.LoadStorageProviderConfiguration(_settingsProviderType.FullName), wiki);
			}
			return _settingsProvider[wikiKey];
		}

		/// <summary>
		/// The Users Provider Collector instance.
		/// </summary>
		public ProviderCollector<IUsersStorageProviderV40> UsersProviderCollector {
			get {
				return _usersProviderCollector;
			}
		}

		/// <summary>
		/// The Theme Provider Collector istance.
		/// </summary>
		public ProviderCollector<IThemeStorageProviderV40> ThemeProviderCollector {
			get {
				return _themeProviderCollector;
			}
		}

		/// <summary>
		/// The Pages Provider Collector instance.
		/// </summary>
		public ProviderCollector<IPagesStorageProviderV40> PagesProviderCollector {
			get {
				return _pagesProviderCollector;
			}
		}

		/// <summary>
		/// The Files Provider Collector instance.
		/// </summary>
		public ProviderCollector<IFilesStorageProviderV40> FilesProviderCollector {
			get {
				return _filesProviderCollector;
			}
		}

		/// <summary>
		/// The Formatter Provider Collector instance.
		/// </summary>
		public ProviderCollector<IFormatterProviderV40> FormatterProviderCollector {
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
