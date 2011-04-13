
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {
	/// <summary>
	/// Contains instances of the Providers Collectors.
	/// </summary>
	public class CollectorsBox : IDisposable, ScrewTurn.Wiki.ICollectorsBox {

		/// <summary>
		/// Initializes a new instance of the <see cref="CollectorsBox"/> class.
		/// </summary>
		/// <param name="settingsProvider">The settings provider Type.</param>
		/// <param name="settingsProviderAssembly">The settings provider assembly.</param>
		/// <param name="usersProviderCollector">The users provider collector.</param>
		/// <param name="themeProviderCollector">The theme provider collector.</param>
		/// <param name="pagesProviderCollector">The pages provider collector.</param>
		/// <param name="filesProviderCollector">The files provider collector.</param>
		/// <param name="formatterProviderCollector">The formatter provider collector.</param>
		/// <param name="disabledUsersProviderCollector">The disabled users provider collector.</param>
		/// <param name="disabledThemeProviderCollector">The disabled theme provider collector.</param>
		/// <param name="disabledPagesProviderCollector">The disabled pages provider collector.</param>
		/// <param name="disabledFilesProviderCollector">The disabled files provider collector.</param>
		/// <param name="disabledFormatterProviderCollector">The disabled formatter provider collector.</param>
		public CollectorsBox(Type settingsProvider,
			System.Reflection.Assembly settingsProviderAssembly,
			ProviderCollector<IUsersStorageProviderV30> usersProviderCollector,
			ProviderCollector<IThemeStorageProviderV30> themeProviderCollector,
			ProviderCollector<IPagesStorageProviderV30> pagesProviderCollector,
			ProviderCollector<IFilesStorageProviderV30> filesProviderCollector,
			ProviderCollector<IFormatterProviderV30> formatterProviderCollector,
			ProviderCollector<IUsersStorageProviderV30> disabledUsersProviderCollector,
			ProviderCollector<IThemeStorageProviderV30> disabledThemeProviderCollector,
			ProviderCollector<IPagesStorageProviderV30> disabledPagesProviderCollector,
			ProviderCollector<IFilesStorageProviderV30> disabledFilesProviderCollector,
			ProviderCollector<IFormatterProviderV30> disabledFormatterProviderCollector) {
		
			_settingsProviderType = settingsProvider;
			_settingsProviderAssembly = settingsProviderAssembly;
			_usersProviderCollector = usersProviderCollector;
			_themeProviderCollector = themeProviderCollector;
			_pagesProviderCollector = pagesProviderCollector;
			_filesProviderCollector = filesProviderCollector;
			_formatterProviderCollector = formatterProviderCollector;
			_disabledUsersProviderCollector = disabledUsersProviderCollector;
			_disabledThemeProviderCollector = disabledThemeProviderCollector;
			_disabledPagesProviderCollector = disabledPagesProviderCollector;
			_disabledFilesProviderCollector = disabledFilesProviderCollector;
			_disabledFormatterProviderCollector = disabledFormatterProviderCollector;
		}

		private Type _settingsProviderType;
		private System.Reflection.Assembly _settingsProviderAssembly;
		private ISettingsStorageProviderV30 _settingsProvider;

		private ProviderCollector<IUsersStorageProviderV30> _usersProviderCollector;
		private ProviderCollector<IThemeStorageProviderV30> _themeProviderCollector;
		private ProviderCollector<IPagesStorageProviderV30> _pagesProviderCollector;
		private ProviderCollector<IFilesStorageProviderV30> _filesProviderCollector;
		private ProviderCollector<IFormatterProviderV30> _formatterProviderCollector;
		private ProviderCollector<IUsersStorageProviderV30> _disabledUsersProviderCollector;
		private ProviderCollector<IFilesStorageProviderV30> _disabledFilesProviderCollector;
		private ProviderCollector<IPagesStorageProviderV30> _disabledPagesProviderCollector;
		private ProviderCollector<IThemeStorageProviderV30> _disabledThemeProviderCollector;
		private ProviderCollector<IFormatterProviderV30> _disabledFormatterProviderCollector;
		
		/// <summary>
		/// The settings storage provider.
		/// </summary>
		public ISettingsStorageProviderV30 SettingsProvider {
			get {
				if(_settingsProvider == null) {
					_settingsProvider = ProviderLoader.CreateInstance<ISettingsStorageProviderV30>(_settingsProviderAssembly, _settingsProviderType);
					_settingsProvider.Init(Host.Instance, StartupTools.GetSettingsStorageProviderConfiguration());
				}
				return _settingsProvider;
			}
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
		/// The Disabled Users Provider Collector instance.
		/// </summary>
		public ProviderCollector<IUsersStorageProviderV30> DisabledUsersProviderCollector {
			get {
				return _disabledUsersProviderCollector;
			}
		}

		/// <summary>
		/// The Disabled Files Provider Collector instance.
		/// </summary>
		public ProviderCollector<IFilesStorageProviderV30> DisabledFilesProviderCollector {
			get {
				return _disabledFilesProviderCollector;
			}
		}

		/// <summary>
		/// The Disabled Pages Provider Collector instance.
		/// </summary>
		public ProviderCollector<IPagesStorageProviderV30> DisabledPagesProviderCollector {
			get {
				return _disabledPagesProviderCollector;
			}
		}

		/// <summary>
		/// The Disabled Theme Provider Collector instance.
		/// </summary>
		public ProviderCollector<IThemeStorageProviderV30> DisabledThemeProviderCollector {
			get {
				return _disabledThemeProviderCollector;
			}
		}

		/// <summary>
		/// The Disabled Formatter Provider Collector instance.
		/// </summary>
		public ProviderCollector<IFormatterProviderV30> DisabledFormatterProviderCollector {
			get {
				return _disabledFormatterProviderCollector;
			}
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose() {
			if(_settingsProvider != null) _settingsProvider.Dispose();
			_usersProviderCollector.Dispose();
			_themeProviderCollector.Dispose();
			_pagesProviderCollector.Dispose();
			_filesProviderCollector.Dispose();
			_formatterProviderCollector.Dispose();
			_disabledUsersProviderCollector.Dispose();
			_disabledThemeProviderCollector.Dispose();
			_disabledPagesProviderCollector.Dispose();
			_disabledFilesProviderCollector.Dispose();
			_disabledFormatterProviderCollector.Dispose();
		}
	}
}
