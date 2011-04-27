
using System;
using System.IO;
using System.Resources;
using System.Security.Principal;
using System.Web.Configuration;
using ScrewTurn.Wiki.PluginFramework;
using System.Reflection;
using System.Configuration;
using System.Collections.Generic;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Provides tools for starting and shutting down the wiki engine.
	/// </summary>
	public static class StartupTools {

		/// <summary>
		/// Gets the Settings Storage Provider configuration string from web.config.
		/// </summary>
		/// <returns>The configuration string.</returns>
		public static string GetSettingsStorageProviderConfiguration() {
			string config = WebConfigurationManager.AppSettings["SettingsStorageProviderConfig"];
			if(config != null) return config;
			else return "";
		}

		/// <summary>
		/// Gets the Global Settings Storage Provider configuration string from web.config.
		/// </summary>
		/// <returns>The configuration string.</returns>
		public static string GetGlobalSettingsStorageProviderConfiguration() {
			string config = WebConfigurationManager.AppSettings["GlobalSettingsStorageProviderConfig"];
			if(config != null) return config;
			else return "";
		}

		/// <summary>
		/// Updates the DLLs into the settings storage provider, if appropriate.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="settingsProviderAsmName">The file name of the assembly that contains the current Settings Storage Provider.</param>
		private static void UpdateDllsIntoSettingsProvider(IGlobalSettingsStorageProviderV30 provider, string settingsProviderAsmName) {
			// Look into public\Plugins (hardcoded)
			string fullPath = Path.Combine(GlobalSettings.PublicDirectory, "Plugins");

			if(!Directory.Exists(fullPath)) return;

			string[] dlls = Directory.GetFiles(fullPath, "*.dll");
			string[] installedDlls = provider.ListPluginAssemblies();

			foreach(string dll in dlls) {
				bool found = false;
				string filename = Path.GetFileName(dll);
				foreach(string instDll in installedDlls) {
					if(instDll.ToLowerInvariant() == filename.ToLowerInvariant()) {
						found = true;
						break;
					}
				}

				if(!found && filename.ToLowerInvariant() == settingsProviderAsmName.ToLowerInvariant()) {
					found = true;
				}

				if(found) {
					// Update DLL
					provider.StorePluginAssembly(filename, File.ReadAllBytes(dll));
				}
			}
		}

		/// <summary>
		/// Performs all needed startup operations.
		/// </summary>
		public static void Startup() {
			// Load Host
			Host.Instance = new Host();

			// Initialize MimeTypes
			MimeTypes.Init();

			GlobalSettings.CanOverridePublicDirectory = false;

			// Initialize Collectors
			Collectors.InitCollectors();
			Collectors.FileNames = new System.Collections.Generic.Dictionary<string, string>(10);

			// Load Global Config
			IGlobalSettingsStorageProviderV30 globalSettingsStorageProvider = ProviderLoader.LoadGlobalSettingsStorageProvider(WebConfigurationManager.AppSettings["GlobalSettingsStorageProvider"]);
			Collectors.AddGlobalSettingsStorageProvider(globalSettingsStorageProvider.GetType(), Assembly.GetAssembly(globalSettingsStorageProvider.GetType()));
			globalSettingsStorageProvider.SetUp(Host.Instance, GetGlobalSettingsStorageProviderConfiguration());
			globalSettingsStorageProvider.Dispose();

			// Add StorageProviders, from WebConfig, to Collectors and Setup them
			// Load SettingsStorageProvider
			ISettingsStorageProviderV30 settingsStorageProvider = ProviderLoader.LoadSettingsStorageProvider(WebConfigurationManager.AppSettings["SettingsStorageProvider"]);
			if(!(settingsStorageProvider is SettingsStorageProvider)) {
				// Update DLLs from public\Plugins
				UpdateDllsIntoSettingsProvider(Collectors.CollectorsBox.GlobalSettingsProvider, ProviderLoader.SettingsStorageProviderAssemblyName);
			}
			Collectors.AddProvider(settingsStorageProvider.GetType(), Assembly.GetAssembly(settingsStorageProvider.GetType()), typeof(ISettingsStorageProviderV30));
			// SetUp SettingsStorageProvider
			settingsStorageProvider.SetUp(Host.Instance, GetSettingsStorageProviderConfiguration());
			settingsStorageProvider.Dispose();

			ProviderLoader.LoadStorageProviders<IFilesStorageProviderV30>((List<StorageProvider>)WebConfigurationManager.GetWebApplicationSection("storageProviders/filesProviders"));
			ProviderLoader.LoadStorageProviders<IThemeStorageProviderV30>((List<StorageProvider>)WebConfigurationManager.GetWebApplicationSection("storageProviders/themesProviders"));
			ProviderLoader.LoadStorageProviders<IUsersStorageProviderV30>((List<StorageProvider>)WebConfigurationManager.GetWebApplicationSection("storageProviders/usersProviders"));

			ProviderLoader.LoadStorageProviders<IPagesStorageProviderV30>((List<StorageProvider>)WebConfigurationManager.GetWebApplicationSection("storageProviders/pagesProviders"));

			ProviderLoader.LoadAllFormatterProviders();


			foreach(string wiki in Collectors.CollectorsBox.GlobalSettingsProvider.AllWikis()) {
				ISettingsStorageProviderV30 ssp = Collectors.CollectorsBox.GetSettingsProvider(wiki);
				if(ssp.IsFirstApplicationStart()) {
					if(ssp.GetMetaDataItem(MetaDataItem.AccountActivationMessage, null) == "")
						ssp.SetMetaDataItem(MetaDataItem.AccountActivationMessage, null, Defaults.AccountActivationMessageContent);
					if(ssp.GetMetaDataItem(MetaDataItem.EditNotice, null) == "")
						ssp.SetMetaDataItem(MetaDataItem.EditNotice, null, Defaults.EditNoticeContent);
					if(ssp.GetMetaDataItem(MetaDataItem.Footer, null) == "")
						ssp.SetMetaDataItem(MetaDataItem.Footer, null, Defaults.FooterContent);
					if(ssp.GetMetaDataItem(MetaDataItem.Header, null) == "")
						ssp.SetMetaDataItem(MetaDataItem.Header, null, Defaults.HeaderContent);
					if(ssp.GetMetaDataItem(MetaDataItem.PasswordResetProcedureMessage, null) == "")
						ssp.SetMetaDataItem(MetaDataItem.PasswordResetProcedureMessage, null, Defaults.PasswordResetProcedureMessageContent);
					if(ssp.GetMetaDataItem(MetaDataItem.Sidebar, null) == "")
						ssp.SetMetaDataItem(MetaDataItem.Sidebar, null, Defaults.SidebarContent);
					if(ssp.GetMetaDataItem(MetaDataItem.PageChangeMessage, null) == "")
						ssp.SetMetaDataItem(MetaDataItem.PageChangeMessage, null, Defaults.PageChangeMessage);
					if(ssp.GetMetaDataItem(MetaDataItem.DiscussionChangeMessage, null) == "")
						ssp.SetMetaDataItem(MetaDataItem.DiscussionChangeMessage, null, Defaults.DiscussionChangeMessage);
					if(ssp.GetMetaDataItem(MetaDataItem.ApproveDraftMessage, null) == "") {
						ssp.SetMetaDataItem(MetaDataItem.ApproveDraftMessage, null, Defaults.ApproveDraftMessage);
					}
				}

				bool groupsCreated = VerifyAndCreateDefaultGroups(wiki);

				if(groupsCreated) {
					// It is necessary to set default permissions for file management
					UserGroup administratorsGroup = Users.FindUserGroup(wiki, Settings.GetAdministratorsGroup(wiki));
					UserGroup anonymousGroup = Users.FindUserGroup(wiki, Settings.GetAnonymousGroup(wiki));
					UserGroup usersGroup = Users.FindUserGroup(wiki, Settings.GetUsersGroup(wiki));

					SetAdministratorsGroupDefaultPermissions(wiki, administratorsGroup);
					SetUsersGroupDefaultPermissions(wiki, usersGroup);
					SetAnonymousGroupDefaultPermissions(wiki, anonymousGroup);
				}

				// Create the Main Page, if needed
				if(Pages.FindPage(wiki, Settings.GetDefaultPage(wiki)) == null) CreateMainPage(wiki);

				Log.LogEntry("Wiki " + wiki + " is ready", EntryType.General, Log.SystemUsername);
			}

			System.Threading.ThreadPool.QueueUserWorkItem(state => {
				using(((WindowsIdentity)state).Impersonate()) {
					foreach(string wiki in GlobalSettings.Provider.AllWikis()) {
						if((DateTime.Now - Settings.GetLastPageIndexing(wiki)).TotalDays > 7) {
							Settings.SetLastPageIndexing(wiki, DateTime.Now);
							System.Threading.Thread.Sleep(10000);
							using(MemoryStream ms = new MemoryStream()) {
								using(StreamWriter wr = new System.IO.StreamWriter(ms)) {
									System.Web.HttpContext.Current = new System.Web.HttpContext(new System.Web.Hosting.SimpleWorkerRequest("", "", wr));
									foreach(var provider in Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(wiki)) {
										if(!provider.ReadOnly) {
											Log.LogEntry("Starting automatic rebuilding index for provider: " + provider.Information.Name, EntryType.General, Log.SystemUsername);
											provider.RebuildIndex();
											Log.LogEntry("Finished automatic rebuilding index for provider: " + provider.Information.Name, EntryType.General, Log.SystemUsername);
										}
									}
								}
							}
						}
					}
				}
			}, WindowsIdentity.GetCurrent());

			Log.LogEntry("ScrewTurn Wiki is ready", EntryType.General, Log.SystemUsername);

		}

		/// <summary>
		/// Verifies the existence of the default user groups and creates them if necessary, for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <returns><c>true</c> if the groups were created, <c>false</c> otherwise.</returns>
		private static bool VerifyAndCreateDefaultGroups(string wiki) {
			UserGroup administratorsGroup = Users.FindUserGroup(wiki, Settings.GetAdministratorsGroup(wiki));
			UserGroup anonymousGroup = Users.FindUserGroup(wiki, Settings.GetAnonymousGroup(wiki));
			UserGroup usersGroup = Users.FindUserGroup(wiki, Settings.GetUsersGroup(wiki));

			// Create default groups if they don't exist already, initializing permissions

			bool aGroupWasCreated = false;

			if(administratorsGroup == null) {
				Users.AddUserGroup(wiki, Settings.GetAdministratorsGroup(wiki), "Built-in Administrators");
				administratorsGroup = Users.FindUserGroup(wiki, Settings.GetAdministratorsGroup(wiki));

				aGroupWasCreated = true;
			}

			if(usersGroup == null) {
				Users.AddUserGroup(wiki, Settings.GetUsersGroup(wiki), "Built-in Users");
				usersGroup = Users.FindUserGroup(wiki, Settings.GetUsersGroup(wiki));

				aGroupWasCreated = true;
			}

			if(anonymousGroup == null) {
				Users.AddUserGroup(wiki, Settings.GetAnonymousGroup(wiki), "Built-in Anonymous Users");
				anonymousGroup = Users.FindUserGroup(wiki, Settings.GetAnonymousGroup(wiki));

				aGroupWasCreated = true;
			}

			if(aGroupWasCreated) {
				ImportPageDiscussionPermissions(wiki);
			}

			return aGroupWasCreated;
		}

		/// <summary>
		/// Creates the main page for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		private static void CreateMainPage(string wiki) {
			Pages.CreatePage(wiki, null as string, Settings.GetDefaultPage(wiki));
			Pages.ModifyPage(wiki, Pages.FindPage(wiki, Settings.GetDefaultPage(wiki)), "Main Page", Log.SystemUsername,
				DateTime.Now, "", Defaults.MainPageContent, null, null, SaveMode.Normal);
		}

		/// <summary>
		/// Performs shutdown operations, such as shutting-down Providers.
		/// </summary>
		public static void Shutdown() {
			Collectors.CollectorsBox.Dispose();
		}

		/// <summary>
		/// Sets the default permissions for the administrators group, properly importing version 2.0 values.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="administrators">The administrators group.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		public static bool SetAdministratorsGroupDefaultPermissions(string wiki, UserGroup administrators) {
			// Administrators can do any operation
			AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(wiki));

			return authWriter.SetPermissionForGlobals(AuthStatus.Grant, Actions.FullControl, administrators);

			// Settings.ConfigVisibleToAdmins is not imported on purpose
		}

		/// <summary>
		/// Sets the default permissions for the users group in the given wiki, properly importing version 2.0 values.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="users">The users group.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		public static bool SetUsersGroupDefaultPermissions(string wiki, UserGroup users) {
			bool done = true;

			// Set namespace-related permissions
			AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(wiki));
			if(Settings.GetUsersCanCreateNewPages(wiki)) {
				done &= authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.CreatePages, users);
			}
			else done &= authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.ModifyPages, users);
			done &= authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.PostDiscussion, users);
			if(Settings.GetUsersCanCreateNewCategories(wiki) || Settings.GetUsersCanManagePageCategories(wiki)) {
				done &= authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.ManageCategories, users);
			}

			done &= SetupFileManagementPermissions(wiki, users);

			return done;
		}

		/// <summary>
		/// Sets the default permissions for the anonymous users group in the given wiki, properly importing version 2.0 values.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="anonymous">The anonymous users group.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		public static bool SetAnonymousGroupDefaultPermissions(string wiki, UserGroup anonymous) {
			bool done = true;

			AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(wiki));

			// Properly import Private/Public Mode wiki
			if(Settings.GetPrivateAccess(wiki)) {
				// Nothing to do, because without any explicit grant, Anonymous users cannot do anything
			}
			else if(Settings.GetPublicAccess(wiki)) {
				// Public access, allow modification and propagate file management permissions if they were allowed for anonymous users
				done &= authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.ModifyPages, anonymous);
				done &= authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.DownloadAttachments, anonymous);
				if(Settings.GetUsersCanCreateNewPages(wiki)) {
					done &= authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.CreatePages, anonymous);
				}
				if(Settings.GetUsersCanCreateNewCategories(wiki) || Settings.GetUsersCanManagePageCategories(wiki)) {
					done &= authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.ManageCategories, anonymous);
				}
				if(Settings.GetFileManagementInPublicAccessAllowed(wiki)) {
					SetupFileManagementPermissions(wiki, anonymous);
				}
			}
			else {
				// Standard configuration, only allow read permissions
				done &= authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.ReadPages, anonymous);
				done &= authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.ReadDiscussion, anonymous);
				done &= authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.DownloadAttachments, anonymous);

				foreach(IFilesStorageProviderV30 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
					done &= authWriter.SetPermissionForDirectory(AuthStatus.Grant, prov, "/", Actions.ForDirectories.DownloadFiles, anonymous);
				}
			}

			return done;
		}

		/// <summary>
		/// Sets file management permissions for the users or anonymous users group in the given wiki, importing version 2.0 values.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="group">The group.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		private static bool SetupFileManagementPermissions(string wiki, UserGroup group) {
			bool done = true;

			AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(wiki));

			if(Settings.GetUsersCanViewFiles(wiki)) {
				done &= authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.DownloadAttachments, group);
				foreach(IFilesStorageProviderV30 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
					done &= authWriter.SetPermissionForDirectory(AuthStatus.Grant, prov, "/", Actions.ForDirectories.DownloadFiles, group);
				}
			}
			if(Settings.GetUsersCanUploadFiles(wiki)) {
				done &= authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.UploadAttachments, group);
				foreach(IFilesStorageProviderV30 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
					done &= authWriter.SetPermissionForDirectory(AuthStatus.Grant, prov, "/", Actions.ForDirectories.UploadFiles, group);
					done &= authWriter.SetPermissionForDirectory(AuthStatus.Grant, prov, "/", Actions.ForDirectories.CreateDirectories, group);
				}
			}
			if(Settings.GetUsersCanDeleteFiles(wiki)) {
				done &= authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.DeleteAttachments, group);
				foreach(IFilesStorageProviderV30 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(wiki)) {
					done &= authWriter.SetPermissionForDirectory(AuthStatus.Grant, prov, "/", Actions.ForDirectories.DeleteFiles, group);
					done &= authWriter.SetPermissionForDirectory(AuthStatus.Grant, prov, "/", Actions.ForDirectories.DeleteDirectories, group);
				}
			}

			return done;
		}

		/// <summary>
		/// Imports version 2.0 page discussion settings and properly propagates them to user groups and single pages, when needed, for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
		private static bool ImportPageDiscussionPermissions(string wiki) {
			// Notes
			// Who can read pages, can read discussions
			// Who can modify pages, can post messages and read discussions
			// Who can manage pages, can manage discussions and post messages

			// Possible values: page|normal|locked|public
			string value = Settings.GetDiscussionPermissions(wiki).ToLowerInvariant();

			UserGroup usersGroup = Users.FindUserGroup(wiki, Settings.GetUsersGroup(wiki));
			UserGroup anonymousGroup = Users.FindUserGroup(wiki, Settings.GetAnonymousGroup(wiki));

			bool done = true;

			AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(wiki));

			switch(value) {
				case "page":
					// Nothing to do
					break;
				case "normal":
					// Allow Users to post messages
					done &= authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.PostDiscussion, usersGroup);
					break;
				case "locked":
					// Deny Users to post messages
					done &= authWriter.SetPermissionForNamespace(AuthStatus.Deny, null, Actions.ForNamespaces.PostDiscussion, usersGroup);
					break;
				case "public":
					// Allow Users and Anonymous Users to post messages
					done &= authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.PostDiscussion, usersGroup);
					done &= authWriter.SetPermissionForNamespace(AuthStatus.Grant, null, Actions.ForNamespaces.PostDiscussion, anonymousGroup);
					break;
			}

			return true;
		}

	}

}
