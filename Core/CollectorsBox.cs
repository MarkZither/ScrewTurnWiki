
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {
	/// <summary>
	/// Contains instances of the Providers Collectors.
	/// </summary>
	public class CollectorsBox :IDisposable {

		/// <summary>
		/// Initializes a new instance of the <see cref="CollectorsBox"/> class.
		/// </summary>
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
		public CollectorsBox(ProviderCollector<IUsersStorageProviderV30> usersProviderCollector,
			ProviderCollector<IThemeStorageProviderV30> themeProviderCollector,
			ProviderCollector<IPagesStorageProviderV30> pagesProviderCollector,
			ProviderCollector<IFilesStorageProviderV30> filesProviderCollector,
			ProviderCollector<IFormatterProviderV30> formatterProviderCollector,
			ProviderCollector<IUsersStorageProviderV30> disabledUsersProviderCollector,
			ProviderCollector<IThemeStorageProviderV30> disabledThemeProviderCollector,
			ProviderCollector<IPagesStorageProviderV30> disabledPagesProviderCollector,
			ProviderCollector<IFilesStorageProviderV30> disabledFilesProviderCollector,
			ProviderCollector<IFormatterProviderV30> disabledFormatterProviderCollector) {
		
			UsersProviderCollector = usersProviderCollector;
			ThemeProviderCollector = themeProviderCollector;
			PagesProviderCollector = pagesProviderCollector;
			FilesProviderCollector = filesProviderCollector;
			FormatterProviderCollector = formatterProviderCollector;
			DisabledUsersProviderCollector = disabledUsersProviderCollector;
			DisabledThemeProviderCollector = disabledThemeProviderCollector;
			DisabledPagesProviderCollector = disabledPagesProviderCollector;
			DisabledFilesProviderCollector = disabledFilesProviderCollector;
			DisabledFormatterProviderCollector = disabledFormatterProviderCollector;
		}

		/// <summary>
		/// The Users Provider Collector instance.
		/// </summary>
		public ProviderCollector<IUsersStorageProviderV30> UsersProviderCollector;

		/// <summary>
		/// The Theme Provider Collector istance.
		/// </summary>
		public ProviderCollector<IThemeStorageProviderV30> ThemeProviderCollector;

		/// <summary>
		/// The Pages Provider Collector instance.
		/// </summary>
		public ProviderCollector<IPagesStorageProviderV30> PagesProviderCollector;

		/// <summary>
		/// The Files Provider Collector instance.
		/// </summary>
		public ProviderCollector<IFilesStorageProviderV30> FilesProviderCollector;

		/// <summary>
		/// The Formatter Provider Collector instance.
		/// </summary>
		public ProviderCollector<IFormatterProviderV30> FormatterProviderCollector;
		
		/// <summary>
		/// The Disabled Users Provider Collector instance.
		/// </summary>
		public ProviderCollector<IUsersStorageProviderV30> DisabledUsersProviderCollector;

		/// <summary>
		/// The Disabled Files Provider Collector instance.
		/// </summary>
		public ProviderCollector<IFilesStorageProviderV30> DisabledFilesProviderCollector;

		/// <summary>
		/// The Disabled Pages Provider Collector instance.
		/// </summary>
		public ProviderCollector<IPagesStorageProviderV30> DisabledPagesProviderCollector;

		/// <summary>
		/// The Disabled Theme Provider Collector instance.
		/// </summary>
		public ProviderCollector<IThemeStorageProviderV30> DisabledThemeProviderCollector;

		/// <summary>
		/// The Disabled Formatter Provider Collector instance.
		/// </summary>
		public ProviderCollector<IFormatterProviderV30> DisabledFormatterProviderCollector;
		

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose() {
			UsersProviderCollector.Dispose();
			ThemeProviderCollector.Dispose();
			PagesProviderCollector.Dispose();
			FilesProviderCollector.Dispose();
			FormatterProviderCollector.Dispose();
			DisabledUsersProviderCollector.Dispose();
			DisabledThemeProviderCollector.Dispose();
			DisabledPagesProviderCollector.Dispose();
			DisabledFilesProviderCollector.Dispose();
			DisabledFormatterProviderCollector.Dispose();
		}
	}
}
