
using System;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Contains the interface of the Providers Collectors Box.
	/// </summary>
	public interface ICollectorsBox {

		/// <summary>
		/// Gets the settings provider.
		/// </summary>
		ISettingsStorageProviderV30 SettingsProvider { get; }

		/// <summary>
		/// Gets the disabled files provider collector.
		/// </summary>
		ProviderCollector<IFilesStorageProviderV30> DisabledFilesProviderCollector { get; }

		/// <summary>
		/// Gets the disabled formatter provider collector.
		/// </summary>
		ProviderCollector<IFormatterProviderV30> DisabledFormatterProviderCollector { get; }

		/// <summary>
		/// Gets the disabled pages provider collector.
		/// </summary>
		ProviderCollector<IPagesStorageProviderV30> DisabledPagesProviderCollector { get; }

		/// <summary>
		/// Gets the disabled theme provider collector.
		/// </summary>
		ProviderCollector<IThemeStorageProviderV30> DisabledThemeProviderCollector { get; }

		/// <summary>
		/// Gets the disabled users provider collector.
		/// </summary>
		ProviderCollector<IUsersStorageProviderV30> DisabledUsersProviderCollector { get; }

		/// <summary>
		/// Gets the files provider collector.
		/// </summary>
		ProviderCollector<IFilesStorageProviderV30> FilesProviderCollector { get; }

		/// <summary>
		/// Gets the formatter provider collector.
		/// </summary>
		ProviderCollector<IFormatterProviderV30> FormatterProviderCollector { get; }

		/// <summary>
		/// Gets the pages provider collector.
		/// </summary>
		ProviderCollector<IPagesStorageProviderV30> PagesProviderCollector { get; }

		/// <summary>
		/// Gets the theme provider collector.
		/// </summary>
		ProviderCollector<IThemeStorageProviderV30> ThemeProviderCollector { get; }

		/// <summary>
		/// Gets the users provider collector.
		/// </summary>
		ProviderCollector<IUsersStorageProviderV30> UsersProviderCollector { get; }

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		void Dispose();
		
	}
}
