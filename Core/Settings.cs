
using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Web;
using System.Web.Security;
using System.Web.Configuration;
using System.Text;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	/// <summary>
	/// Allows access to settings and configuration options for a specific wiki.
	/// </summary>
	[System.Security.Permissions.FileIOPermission(System.Security.Permissions.SecurityAction.Assert,
		AllLocalFiles = System.Security.Permissions.FileIOPermissionAccess.PathDiscovery)]
	public static class Settings {
		
		/// <summary>
		/// Gets the settings storage provider.
		/// </summary>
		public static ISettingsStorageProviderV40 GetProvider(string wiki) {
			return Collectors.CollectorsBox.GetSettingsProvider(wiki);
		}

		/// <summary>
		/// Gets the Master Password of the given wiki, used to encrypt the Users data.
		/// </summary>
		public static string GetMasterPassword(string wiki) {
			return SettingsTools.GetString(GetProvider(wiki).GetSetting("MasterPassword"), "");
		}

		/// <summary>
		/// Sets the master password for the given wiki, used to encrypt the Users data.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="newMasterPassword">The new master password.</param>
		public static void SetMasterPassword(string wiki, string newMasterPassword) {
			GetProvider(wiki).SetSetting("MasterPassword", newMasterPassword);
		}

		/// <summary>
		/// Gets the bytes of the MasterPassword.
		/// </summary>
		public static byte[] GetMasterPasswordBytes(string wiki) {
			MD5 md5 = MD5CryptoServiceProvider.Create();
			return md5.ComputeHash(System.Text.UTF8Encoding.UTF8.GetBytes(GetMasterPassword(wiki)));
		}

		/// <summary>
		/// Begins a bulk update session.
		/// </summary>
		public static void BeginBulkUpdate(string wiki) {
			GetProvider(wiki).BeginBulkUpdate();
		}

		/// <summary>
		/// Ends a bulk update session.
		/// </summary>
		public static void EndBulkUpdate(string wiki) {
			GetProvider(wiki).EndBulkUpdate();
		}
		
		#region Basic Settings and Associated Data

		/// <summary>
		/// Gets or sets the main URL of the Wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <returns>The main URL associated with the given wiki.</returns>
		public static string GetMainUrl(string wiki) {
				string s = SettingsTools.GetString(GetProvider(wiki).GetSetting("MainUrl"), "http://www.server.com/");
				if(!s.EndsWith("/")) s += "/";
				return s;
			}

		/// <summary>
		/// Sets the main URL.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="url">The main URL.</param>
		public static void SetMainUrl(string wiki, string url) {
			GetProvider(wiki).SetSetting("MainUrl", url);
		}

		/// <summary>
		/// Gets the main URL of the wiki, defaulting to the current request URL if none is configured manually.
		/// </summary>
		/// <returns>The URL of the wiki.</returns>
		public static Uri GetMainUrlOrDefault(string wiki) {
			Uri mainUrl = new Uri(GetMainUrl(wiki));
			if(mainUrl.Host == "www.server.com") {
				try {
					// STW never uses internal URLs with slashes, so trimming to the last slash should work
					// Example: http://server/wiki/namespace.page.ashx
					string temp = System.Web.HttpContext.Current.Request.Url.ToString();
					mainUrl = new Uri(temp.Substring(0, temp.LastIndexOf("/") + 1));
				}
				catch { }
			}
			return mainUrl;
		}

		/// <summary>
		/// Gets or sets the Title of the Wiki.
		/// </summary>
		public static string GetWikiTitle(string wiki) {
			return SettingsTools.GetString(GetProvider(wiki).GetSetting("WikiTitle"), "ScrewTurn Wiki");
		}

		/// <summary>
		/// Sets the title of the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="newWikiTitle">The new wiki title.</param>
		public static void SetWikiTitle(string wiki, string newWikiTitle) {
			GetProvider(wiki).SetSetting("WikiTitle", newWikiTitle);
		}

		/// <summary>
		/// Gets a value specifying whether the access to the wiki is public or not.
		/// </summary>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static bool GetPublicAccess(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("PublicAccess"), false);
		}

		/// <summary>
		/// Sets if the wiki is public or not.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="publicAccess">if set to <c>true</c> [public access].</param>
		public static void SetPublicAccess(string wiki, bool publicAccess) {
			GetProvider(wiki).SetSetting("PublicAccess", SettingsTools.PrintBool(publicAccess));
		}

		/// <summary>
		/// Gets a value specifying whether the access to the Wiki is private or not.
		/// </summary>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static bool GetPrivateAccess(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("PrivateAccess"), false);
		}

		/// <summary>
		/// Sets if the given wiki is private or not.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="privateAccess">if set to <c>true</c> [private access].</param>
		public static void SetPrivateAccess(string wiki, bool privateAccess) {	
			GetProvider(wiki).SetSetting("PrivateAccess", SettingsTools.PrintBool(privateAccess));
		}

		/// <summary>
		/// Gets a value specifying whether, in Public Access mode, anonymous file management is allowed.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <returns></returns>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static bool GetFileManagementInPublicAccessAllowed(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("FileManagementInPublicAccessAllowed"), false);
		}

		/// <summary>
		/// Sets a value specifying whether, in Public Access mode, anonymous file management is allowed.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="fileManagementInPublicAccessAllowed">if set to <c>true</c> [file management in public access allowed].</param>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static void SetFileManagementInPublicAccessAllowed(string wiki, bool fileManagementInPublicAccessAllowed) {
			GetProvider(wiki).SetSetting("FileManagementInPublicAccessAllowed", SettingsTools.PrintBool(fileManagementInPublicAccessAllowed));
		}

		/// <summary>
		/// Gets a value specifying whether Users can create new accounts or not (in this case Register.aspx won't be available).
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static bool UsersCanRegister(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("UsersCanRegister"), true);
		}

		/// <summary>
		/// Sets a value specifying whether Users can create new accounts or not (in this case Register.aspx won't be available).
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="usersCanRegister">If set to <c>true</c> users can register.</param>
		public static void SetUsersCanRegister(string wiki, bool usersCanRegister) {
			GetProvider(wiki).SetSetting("UsersCanRegister", SettingsTools.PrintBool(usersCanRegister));
		}

		/// <summary>
		/// Gets a value specifying whether to disable the Captcha control in the Registration Page.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static bool GetDisableCaptchaControl(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("DisableCaptchaControl"), false);
		}
		
		/// <summary>
		/// Sets a value specifying whether to disable the Captcha control in the Registration Page.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="disableCaptchaControl">If set to <c>true</c> disable captcha control.</param>
		public static void SetDisableCaptchaControl(string wiki, bool disableCaptchaControl) {
			GetProvider(wiki).SetSetting("DisableCaptchaControl", SettingsTools.PrintBool(disableCaptchaControl));
		}

		/// <summary>
		/// Gets a value indicating whether to enable the "View Page Code" feature.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static bool GetEnableViewPageCodeFeature(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("EnableViewPageCode"), true);
		}
		
		/// <summary>
		/// Sets a value indicating whether to enable the "View Page Code" feature.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="enableViewPageCodeFeature">if set to <c>true</c> enable view page code feature.</param>
		public static void SetEnableViewPageCodeFeature(string wiki, bool enableViewPageCodeFeature) {
			GetProvider(wiki).SetSetting("EnableViewPageCode", SettingsTools.PrintBool(enableViewPageCodeFeature));
		}

		/// <summary>
		/// Gets a value indicating whether to enable the Page Information DIV.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static bool GetEnablePageInfoDiv(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("EnablePageInfoDiv"), true);
		}
		
		/// <summary>
		/// Sets a value indicating whether to enable the Page Information DIV.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="enablePageInfoDiv">If set to <c>true</c> enable page info div.</param>
		public static void SetEnablePageInfoDiv(string wiki, bool enablePageInfoDiv) {
			GetProvider(wiki).SetSetting("EnablePageInfoDiv", SettingsTools.PrintBool(enablePageInfoDiv));
		}

		/// <summary>
		/// Gets a value indicating whether to enable the Page Toolbar in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static bool GetEnablePageToolbar(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("EnablePageToolbar"), true);
		}
		
		/// <summary>
		/// Sets a value indicating whether to enable the Page Toolbar in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="enablePageToolbar">If set to <c>true</c> enables page toolbar.</param>
		public static void SetEnablePageToolbar(string wiki, bool enablePageToolbar) {
			GetProvider(wiki).SetSetting("EnablePageToolbar", SettingsTools.PrintBool(enablePageToolbar));
		}

		/// <summary>
		/// Gets the number of items displayed in pages/users lists for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static int GetListSize(string wiki) {
			return SettingsTools.GetInt(GetProvider(wiki).GetSetting("ListSize"), 50);
		}

		/// <summary>
		/// Sets the number of items displayed in pages/users lists for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="listSize">Size of the list.</param>
		public static void SetListSize(string wiki, int listSize) {
			GetProvider(wiki).SetSetting("ListSize", listSize.ToString());
		}

		/// <summary>
		/// Gets the Account Activation Mode for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static AccountActivationMode GetAccountActivationMode(string wiki) {
			string value = SettingsTools.GetString(GetProvider(wiki).GetSetting("AccountActivationMode"), "EMAIL");
			switch(value.ToLowerInvariant()) {
				case "email":
					return AccountActivationMode.Email;
				case "admin":
					return AccountActivationMode.Administrator;
				case "auto":
					return AccountActivationMode.Auto;
				default:
					return AccountActivationMode.Email;
			}
		}
		
		/// <summary>
		/// Sets the Account Activation Mode for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="accountActivationMode">The account activation mode.</param>
		public static void SetAccountActivationMode(string wiki, AccountActivationMode accountActivationMode) {
			string aa = "";
			switch(accountActivationMode) {
				case AccountActivationMode.Email:
					aa = "EMAIL";
					break;
				case AccountActivationMode.Administrator:
					aa = "ADMIN";
					break;
				case AccountActivationMode.Auto:
					aa = "AUTO";
					break;
				default:
					throw new ArgumentException("Invalid Account Activation Mode.");
			}

			GetProvider(wiki).SetSetting("AccountActivationMode", aa);
		}

		/// <summary>
		/// Gets the page change moderation mode for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static ChangeModerationMode GetModerationMode(string wiki) {
			string value = SettingsTools.GetString(GetProvider(wiki).GetSetting("ChangeModerationMode"),
				ChangeModerationMode.None.ToString());

			return (ChangeModerationMode)Enum.Parse(typeof(ChangeModerationMode), value, true);
		}

		/// <summary>
		/// Sets the page change moderation mode for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="moderationMode">The moderation mode.</param>
		public static void SetModerationMode(string wiki, ChangeModerationMode moderationMode) {
			GetProvider(wiki).SetSetting("ChangeModerationMode", moderationMode.ToString());
		}

		/// <summary>
		/// Gets a value specifying whether or not Users can create new Page.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static bool GetUsersCanCreateNewPages(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("UsersCanCreateNewPages"), true);
		}

		/// <summary>
		/// Gets or sets a value specifying whether or not Users can create new Page.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="usersCanCreateNewPages">if set to <c>true</c> users can create new pages.</param>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static void SetUsersCanCreateNewPages(string wiki, bool usersCanCreateNewPages) {
			GetProvider(wiki).SetSetting("UsersCanCreateNewPages", SettingsTools.PrintBool(usersCanCreateNewPages));
		}

		/// <summary>
		/// Gets a value specifying whether or not users can view uploaded Files for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static bool GetUsersCanViewFiles(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("UsersCanViewFiles"), true);
		}

		/// <summary>
		/// Gets or sets a value specifying whether or not users can view uploaded Files for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="usersCanViewFiles">If set to <c>true</c> users can view files.</param>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static void SetUsersCanViewFiles(string wiki, bool usersCanViewFiles) {
			GetProvider(wiki).SetSetting("UsersCanViewFiles", SettingsTools.PrintBool(usersCanViewFiles));
		}

		/// <summary>
		/// Gets a value specifying whether or not users can upload Files for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <returns></returns>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static bool GetUsersCanUploadFiles(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("UsersCanUploadFiles"), false);
		}

		/// <summary>
		/// Sets a value specifying whether or not users can upload Files for the specified wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="usersCanUploadFiles">If set to <c>true</c> users can upload files.</param>
		/// Deprecated in version 3.0.
		public static void SetUsersCanUploadFiles(string wiki, bool usersCanUploadFiles) {
			GetProvider(wiki).SetSetting("UsersCanUploadFiles", SettingsTools.PrintBool(usersCanUploadFiles));
		}

		/// <summary>
		/// Gets a value specifying whether or not users can delete Files for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static bool GetUsersCanDeleteFiles(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("UsersCanDeleteFiles"), false);
		}
		
		/// <summary>
		/// Sets if users can delete Files for the given wiki or not.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="usersCanDeleteFiles">If set to <c>true</c> users can delete files.</param>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static void SetUsersCanDeleteFiles(string wiki, bool usersCanDeleteFiles) {
			GetProvider(wiki).SetSetting("UsersCanDeleteFiles", SettingsTools.PrintBool(usersCanDeleteFiles));
		}

		/// <summary>
		/// Gets a value specifying whether or not users can create new Categories for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static bool GetUsersCanCreateNewCategories(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("UsersCanCreateNewCategories"), false);
		}

		/// <summary>
		/// Sets if users can create new Categories for the given wiki or not.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="usersCanCreateNewCategories">If set to <c>true</c> users can create new categories.</param>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static void SetUsersCanCreateNewCategories(string wiki, bool usersCanCreateNewCategories) {
			GetProvider(wiki).SetSetting("UsersCanCreateNewCategories", SettingsTools.PrintBool(usersCanCreateNewCategories));
		}

		/// <summary>
		/// Gets a value specifying whether or not users can manage Page Categories for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static bool GetUsersCanManagePageCategories(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("UsersCanManagePageCategories"), false);
		}
			
		/// <summary>
		/// Sets if users can manage Page Categories for the given wiki or not.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="usersCanManagePageCategories">If set to <c>true</c> users can manage page categories.</param>
		/// <remarks>Deprecated in version 3.0.</remarks>
		public static void SetUsersCanManagePageCategories(string wiki, bool usersCanManagePageCategories) {
			GetProvider(wiki).SetSetting("UsersCanManagePageCategories", SettingsTools.PrintBool(usersCanManagePageCategories));
		}

		/// <summary>
		/// Gets the Default Language for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static string GetDefaultLanguage(string wiki) {
			return SettingsTools.GetString(GetProvider(wiki).GetSetting("DefaultLanguage"), "en-US");
		}
		
		/// <summary>
		/// Sets the Default Language for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="defaultLanguage">The default language.</param>
		public static void SetDefaultLanguage(string wiki, string defaultLanguage) {
			GetProvider(wiki).SetSetting("DefaultLanguage", defaultLanguage);
		}

		/// <summary>
		/// Gets the Default Timezone (time delta in minutes) for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static int GetDefaultTimezone(string wiki) {
			string value = SettingsTools.GetString(GetProvider(wiki).GetSetting("DefaultTimezone"), "0");
			return int.Parse(value);
		}
			
		/// <summary>
		/// Sets the Default Timezone (time delta in minutes) for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="timeDelta">The delta in minutes.</param>
		public static void SetDefaultTimezone(string wiki, int timeDelta) {
			GetProvider(wiki).SetSetting("DefaultTimezone", timeDelta.ToString());
		}

		/// <summary>
		/// Gets the DateTime format for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static string GetDateTimeFormat(string wiki) {
			return SettingsTools.GetString(GetProvider(wiki).GetSetting("DateTimeFormat"), "yyyy'/'MM'/'dd' 'HH':'mm");
		}
		
		/// <summary>
		/// Sets the DateTime format for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="dateTimeFormat">The date time format.</param>
		public static void SetDateTimeFormat(string wiki, string dateTimeFormat) {
			GetProvider(wiki).SetSetting("DateTimeFormat", dateTimeFormat);
		}

		/// <summary>
		/// Gets the Defaul Page of the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static string GetDefaultPage(string wiki) {
			return SettingsTools.GetString(GetProvider(wiki).GetSetting("DefaultPage"), "MainPage");
		}
		
		/// <summary>
		/// Sets the Defaul Page of the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="defaultPage">The default page.</param>
		public static void SetDefaultPage(string wiki, string defaultPage) {
			GetProvider(wiki).SetSetting("DefaultPage", defaultPage);
		}

		/// <summary>
		/// Gets the maximum number of recent changes to display with {RecentChanges} special tag for the specified wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static int GetMaxRecentChangesToDisplay(string wiki) {
			return SettingsTools.GetInt(GetProvider(wiki).GetSetting("MaxRecentChangesToDisplay"), 10);
		}

		/// <summary>
		/// Sets the maximum number of recent changes to display with {RecentChanges} special tag for the specified wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="maxRecentChangesToDisplay">The max number of recent changes to display.</param>
		public static void SetMaxRecentChangesToDisplay(string wiki, int maxRecentChangesToDisplay) {
			GetProvider(wiki).SetSetting("MaxRecentChangesToDisplay", maxRecentChangesToDisplay.ToString());
		}

		/// <summary>
		/// Gets a value specifying whether to enable double-click Page editing for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static bool GetEnableDoubleClickEditing(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("EnableDoubleClickEditing"), false);
		}
		
		/// <summary>
		/// Sets if to enable or not double-click Page editing for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="enableDoubleClickEditing">If set to <c>true</c> enable double click editing.</param>
		public static void SetEnableDoubleClickEditing(string wiki, bool enableDoubleClickEditing) {
			GetProvider(wiki).SetSetting("EnableDoubleClickEditing", SettingsTools.PrintBool(enableDoubleClickEditing));
		}

		/// <summary>
		/// Gets a value indicating whether to enable section editing for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static bool GetEnableSectionEditing(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("EnableSectionEditing"), true);
		}
		
		/// <summary>
		/// Sets if to enable or not section editing for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="enableSelectionEditing">If set to <c>true</c> enable selection editing.</param>
		public static void SetEnableSectionEditing(string wiki, bool enableSelectionEditing) {
			GetProvider(wiki).SetSetting("EnableSectionEditing", SettingsTools.PrintBool(enableSelectionEditing));
		}

		/// <summary>
		/// Gets a value indicating whether to display section anchors in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static bool GetEnableSectionAnchors(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("EnableSectionAnchors"), true);
		}
		
		/// <summary>
		/// Sets if to display or not section anchors in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="enableSectionAnchors">If set to <c>true</c> enable section anchors.</param>
		public static void SetEnableSectionAnchors(string wiki, bool enableSectionAnchors) {
			GetProvider(wiki).SetSetting("EnableSectionAnchors", SettingsTools.PrintBool(enableSectionAnchors));
		}

		/// <summary>
		/// Gets a value indicating whether to disable the Breadcrumbs Trail in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static bool GetDisableBreadcrumbsTrail(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("DisableBreadcrumbsTrail"), false);
		}
		
		/// <summary>
		/// Sets if to disable or not the Breadcrumbs Trail in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="disableBreadcrumbsTrail">If set to <c>true</c> disables breadcrumbs trail.</param>
		public static void SetDisableBreadcrumbsTrail(string wiki, bool disableBreadcrumbsTrail) {
			GetProvider(wiki).SetSetting("DisableBreadcrumbsTrail", SettingsTools.PrintBool(disableBreadcrumbsTrail));
		}

		/// <summary>
		/// Gets a value indicating whether the editor should auto-generate page names from the title for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static bool GetAutoGeneratePageNames(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("AutoGeneratePageNames"), true);
		}
		
		/// <summary>
		/// Sets if the editor should auto-generate page names from the title for the given wiki or not.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="autoGeneratePageNames">If set to <c>true</c> auto generates page names.</param>
		public static void SetAutoGeneratePageNames(string wiki, bool autoGeneratePageNames) {
			GetProvider(wiki).SetSetting("AutoGeneratePageNames", SettingsTools.PrintBool(autoGeneratePageNames));
		}

		/// <summary>
		/// Gets a value indicating whether or not to process single line breaks in WikiMarkup for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static bool GetProcessSingleLineBreaks(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("ProcessSingleLineBreaks"), false);
		}
		
		/// <summary>
		/// Sets if to process single line breaks in WikiMarkup for the given wiki or not.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="processSingleLineBreacks">If set to <c>true</c> processes single line breacks.</param>
		public static void SetProcessSingleLineBreaks(string wiki, bool processSingleLineBreacks) {
			GetProvider(wiki).SetSetting("ProcessSingleLineBreaks", SettingsTools.PrintBool(processSingleLineBreacks));
		}

		/// <summary>
		/// Gets the # of Backups that are kept in the specified wiki. Older backups are deleted after Page editing.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <remarks>-1 indicates that no backups are deleted.</remarks>
		public static int GetKeptBackupNumber(string wiki) {
			return SettingsTools.GetInt(GetProvider(wiki).GetSetting("KeptBackupNumber"), -1);
		}

		/// <summary>
		/// Sets the # of Backups that are kept in the specified wiki. Older backups are deleted after Page editing.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="numberOfBackup">The number of backup.</param>
		/// <remarks>-1 indicates that no backups are deleted.</remarks>
		public static void SetKeptBackupNumber(string wiki, int numberOfBackup) {
			GetProvider(wiki).SetSetting("KeptBackupNumber", numberOfBackup.ToString());
		}

		/// <summary>
		/// Gets the name of the theme for the given wiki for the given namespace.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <returns>The name of the theme.</returns>
		public static string GetTheme(string wiki, string nspace) {
			if(!string.IsNullOrEmpty(nspace)) nspace = Pages.FindNamespace(wiki, nspace).Name;
			string propertyName = "Theme" + (!string.IsNullOrEmpty(nspace) ? "-" + nspace : "");
			return SettingsTools.GetString(GetProvider(wiki).GetSetting(propertyName), "standard|Default");
		}

		/// <summary>
		/// Sets the theme for a namespace for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="nspace">The namespace (<c>null</c> for the root).</param>
		/// <param name="theme">The name of the theme.</param>
		public static void SetTheme(string wiki, string nspace, string theme) {
			if(!string.IsNullOrEmpty(nspace)) nspace = Pages.FindNamespace(wiki, nspace).Name;
			string propertyName = "Theme" + (!string.IsNullOrEmpty(nspace) ? "-" + nspace : "");
			GetProvider(wiki).SetSetting(propertyName, theme);
		}

		/// <summary>
		/// Gets the list of allowed file types for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static string[] GetAllowedFileTypes(string wiki) {
			string raw = SettingsTools.GetString(GetProvider(wiki).GetSetting("AllowedFileTypes"), "jpg|jpeg|gif|png|tif|tiff|bmp|svg|htm|html|zip|rar|pdf|txt|doc|xls|ppt|docx|xlsx|pptx");
			return raw.ToLowerInvariant().Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
		}

		/// <summary>
		/// Sets the list of allowed file types for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="allowedFileTypes">The allowed file types.</param>
		public static void SetAllowedFileTypes(string wiki, string[] allowedFileTypes) {
			string res = string.Join("|", allowedFileTypes);
			GetProvider(wiki).SetSetting("AllowedFileTypes", res);
		}

		/// <summary>
		/// Gets the file download count filter mode for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static FileDownloadCountFilterMode GetFileDownloadCountFilterMode(string wiki) {
			string raw = SettingsTools.GetString(GetProvider(wiki).GetSetting("FileDownloadCountFilterMode"), FileDownloadCountFilterMode.CountAll.ToString());
			return (FileDownloadCountFilterMode)Enum.Parse(typeof(FileDownloadCountFilterMode), raw);
		}

		/// <summary>
		/// Sets the file download count filter mode for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="fileDonwloadCountFilterMode">The file donwload count filter mode.</param>
		public static void SetFileDownloadCountFilterMode(string wiki, FileDownloadCountFilterMode fileDonwloadCountFilterMode) {
			GetProvider(wiki).SetSetting("FileDownloadCountFilterMode", fileDonwloadCountFilterMode.ToString());
		}

		/// <summary>
		/// Gets the file download count extension filter for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static string[] GetFileDownloadCountFilter(string wiki) {
			string raw = SettingsTools.GetString(GetProvider(wiki).GetSetting("FileDownloadCountFilter"), "");
			return raw.ToLowerInvariant().Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
		}

		/// <summary>
		/// Sets the file download count extension filter for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="fileDownloadCountFilter">The file download count filter.</param>
		public static void SetFileDownloadCountFilter(string wiki, string[] fileDownloadCountFilter) {
			string res = string.Join("|", fileDownloadCountFilter);
			GetProvider(wiki).SetSetting("FileDownloadCountFilter", res);
		}

		/// <summary>
		/// Gets a value specifying whether script tags are allowed or not in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static bool GetScriptTagsAllowed(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("ScriptTagsAllowed"), false);
		}
		
		/// <summary>
		/// Sets if script tags are allowed or not in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="scriptTagAllowed">If set to <c>true</c> script tag are allowed.</param>
		public static void SetScriptTagsAllowed(string wiki, bool scriptTagAllowed) {
			GetProvider(wiki).SetSetting("ScriptTagsAllowed", SettingsTools.PrintBool(scriptTagAllowed));
		}

		/// <summary>
		/// Gets the IP/Host filter for page editing for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static string GetIpHostFilter(string wiki) {
			return SettingsTools.GetString(GetProvider(wiki).GetSetting("IpHostFilter"), "");
		}

		/// <summary>
		/// Gets the IP/Host filter for page editing for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="ipHostFilter">The ip host filter.</param>
		public static void SetIpHostFilter(string wiki, string ipHostFilter) {
			GetProvider(wiki).SetSetting("IpHostFilter", ipHostFilter);
		}

		/// <summary>
		/// Gets the max number of recent changes to log for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static int GetMaxRecentChanges(string wiki) {
			return SettingsTools.GetInt(GetProvider(wiki).GetSetting("MaxRecentChanges"), 100);
		}
		
		/// <summary>
		/// Sets the max number of recent changes to log for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="maxRecentChanges">The max recent changes.</param>
		public static void SetMaxRecentChanges(string wiki, int maxRecentChanges) {
			GetProvider(wiki).SetSetting("MaxRecentChanges", maxRecentChanges.ToString());
		}

		/// <summary>
		/// Gets the Discussion Permissions for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static string GetDiscussionPermissions(string wiki) {
			return SettingsTools.GetString(GetProvider(wiki).GetSetting("DiscussionPermissions"), "PAGE");
		}

		/// <summary>
		/// Sets the Discussion Permissions for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="discussionPermission">The discussion permission.</param>
		public static void SetDiscussionPermissions(string wiki, string discussionPermission) {
			GetProvider(wiki).SetSetting("DiscussionPermissions", discussionPermission);
		}

		/// <summary>
		/// Gets a value specifying whether or not to disable concurrent editing of Pages for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static bool GetDisableConcurrentEditing(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("DisableConcurrentEditing"), false);
		}

		/// <summary>
		/// Sets if to disable or not concurrent editing of Pages for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="disableConcurrentEditing">If set to <c>true</c> disable concurrent editing.</param>
		public static void SetDisableConcurrentEditing(string wiki, bool disableConcurrentEditing) {
			GetProvider(wiki).SetSetting("DisableConcurrentEditing", SettingsTools.PrintBool(disableConcurrentEditing));
		}

		/// <summary>
		/// Gets a value indicating whether to use the Visual editor as default for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static bool GetUseVisualEditorAsDefault(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("UseVisualEditorAsDefault"), false);
		}

		/// <summary>
		/// Sets if to use or not the Visual editor as default for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="useVisualEditorAsDefault">If set to <c>true</c> use visual editor as default.</param>
		public static void SetUseVisualEditorAsDefault(string wiki, bool useVisualEditorAsDefault) {
			GetProvider(wiki).SetSetting("UseVisualEditorAsDefault", SettingsTools.PrintBool(useVisualEditorAsDefault));
		}

		/// <summary>
		/// Gets the name of the default Administrators group in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static string GetAdministratorsGroup(string wiki) {
			return SettingsTools.GetString(GetProvider(wiki).GetSetting("AdministratorsGroup"), "Administrators");
		}
		
		/// <summary>
		/// Sets the name of the default Administrators group in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="administratorsGroup">The name of the administrators group.</param>
		public static void SetAdministratorsGroup(string wiki, string administratorsGroup) {
			GetProvider(wiki).SetSetting("AdministratorsGroup", administratorsGroup);
		}

		/// <summary>
		/// Gets the name of the default Users group in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static string GetUsersGroup(string wiki) {
			return SettingsTools.GetString(GetProvider(wiki).GetSetting("UsersGroup"), "Users");
		}
		
		/// <summary>
		/// Sets the name of the default Users group in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="usersGroup">The name of the users group.</param>
		public static void SetUsersGroup(string wiki, string usersGroup) {
			GetProvider(wiki).SetSetting("UsersGroup", usersGroup);
		}

		/// <summary>
		/// Gets the name of the default Anonymous users group in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static string GetAnonymousGroup(string wiki) {
			return SettingsTools.GetString(GetProvider(wiki).GetSetting("AnonymousGroup"), "Anonymous");
		}

		/// <summary>
		/// Sets the name of the default Anonymous users group in the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="anonymousGroup">The name of the anonymous group.</param>
		public static void SetAnonymousGroup(string wiki, string anonymousGroup) {
			GetProvider(wiki).SetSetting("AnonymousGroup", anonymousGroup);
		}

		/// <summary>
		/// Gets a value indicating whether to display gravatars or not for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static bool GetDisplayGravatars(string wiki) {
			return SettingsTools.GetBool(GetProvider(wiki).GetSetting("DisplayGravatars"), true);
		}

		/// <summary>
		/// Sets if to display gravatars or not for the given wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="displayGravatar">If set to <c>true</c> displays gravatar.</param>
		public static void SetDisplayGravatars(string wiki, bool displayGravatar) {
			GetProvider(wiki).SetSetting("DisplayGravatars", SettingsTools.PrintBool(displayGravatar));
		}

		/// <summary>
		/// Gets the functioning mode of RSS feeds for the specified wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		public static RssFeedsMode GetRssFeedsMode(string wiki) {
			string value = SettingsTools.GetString(GetProvider(wiki).GetSetting("RssFeedsMode"), RssFeedsMode.Summary.ToString());
			return (RssFeedsMode)Enum.Parse(typeof(RssFeedsMode), value);
		}

		/// <summary>
		/// Sets the functioning mode of RSS feeds for the specified wiki.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="rssFeedMode">The RSS feed mode.</param>
		public static void SetRssFeedsMode(string wiki, RssFeedsMode rssFeedMode) {
			GetProvider(wiki).SetSetting("RssFeedsMode", rssFeedMode.ToString());
		}

		#endregion

		#region Advanced Settings and Associated Data

		/// <summary>
		/// Gets the last page indexing.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <returns>The last page indexing DateTime.</returns>
		public static DateTime GetLastPageIndexing(string wiki) {
			return DateTime.ParseExact(SettingsTools.GetString(GetProvider(wiki).GetSetting("LastPageIndexing"), DateTime.Now.AddYears(-10).ToString("yyyyMMddHHmmss")),
				"yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Sets the last page indexing.
		/// </summary>
		/// <param name="wiki">The wiki.</param>
		/// <param name="lastPageIndexingDateTime">The last page indexing DateTime.</param>
		public static void SetLastPageIndexing(string wiki, DateTime lastPageIndexingDateTime) {
			GetProvider(wiki).SetSetting("LastPageIndexing", lastPageIndexingDateTime.ToString("yyyyMMddHHmmss"));
		}

		#endregion

		/// <summary>
		/// Determines whether a meta-data item is global or namespace-specific.
		/// </summary>
		/// <param name="item">The item to test.</param>
		/// <returns><c>true</c> if the meta-data item is global, <c>false</c> otherwise.</returns>
		public static bool IsMetaDataItemGlobal(MetaDataItem item) {
			int value = (int)item;
			return value < 100; // See MetaDataItem
		}

	}

	/// <summary>
	/// Lists legal RSS feeds function modes.
	/// </summary>
	public enum RssFeedsMode {
		/// <summary>
		/// RSS feeds serve full-text content.
		/// </summary>
		FullText,
		/// <summary>
		/// RSS feeds serve summary content.
		/// </summary>
		Summary,
		/// <summary>
		/// RSS feeds are disabled.
		/// </summary>
		Disabled
	}

	/// <summary>
	/// Lists legal file download filter modes.
	/// </summary>
	public enum FileDownloadCountFilterMode {
		/// <summary>
		/// Counts all downloads.
		/// </summary>
		CountAll,
		/// <summary>
		/// Counts only the specified extensions.
		/// </summary>
		CountSpecifiedExtensions,
		/// <summary>
		/// Excludes the specified extensions.
		/// </summary>
		ExcludeSpecifiedExtensions
	}

	/// <summary>
	/// Lists legal page change moderation mode values.
	/// </summary>
	public enum ChangeModerationMode {
		/// <summary>
		/// Page change moderation is disabled.
		/// </summary>
		None,
		/// <summary>
		/// Anyone who has page editing permissoins but not page management permissions 
		/// can edit pages, but the changes are held in moderation.
		/// </summary>
		RequirePageEditingPermissions,
		/// <summary>
		/// Anyone who has page viewing permissions can edit pages, but the changes are 
		/// held in moderation.
		/// </summary>
		RequirePageViewingPermissions
	}

	/// <summary>
	/// Lists legal account activation mode values.
	/// </summary>
	public enum AccountActivationMode {
		/// <summary>
		/// Users must activate their account via email.
		/// </summary>
		Email,
		/// <summary>
		/// Accounts must be activated by administrators.
		/// </summary>
		Administrator,
		/// <summary>
		/// Accounts are active by default.
		/// </summary>
		Auto
	}

}
