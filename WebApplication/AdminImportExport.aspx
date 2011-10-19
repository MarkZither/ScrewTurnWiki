<%@ Page Title="" Language="C#" MasterPageFile="~/Admin.master" AutoEventWireup="true" CodeBehind="AdminImportExport.aspx.cs" Inherits="ScrewTurn.Wiki.AdminImportExport" culture="auto" meta:resourcekey="PageResource1" uiculture="auto" %>

<asp:Content ID="ctnHead" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="ctnConfig" ContentPlaceHolderID="cphAdmin" runat="server">
	<div class="leftaligned">
		<h2 class="sectiontitle"><asp:Literal ID="lblImportExportTitle" runat="server" Text="Import/Export" EnableViewState="False" meta:resourcekey="lblImportExportTitleResource1" /></h2>
	
		<h2 class="separator"><asp:Literal ID="lblGlobalSettings" runat="server" Text="Import and Export Global Settings" EnableViewState="False" meta:resourcekey="lblGlobalSettingsResource1" /></h2>
		<asp:Button ID="btnExportGlobalSettings" runat="server" Text="Export Global Settings" OnClick="btnExportGlobalSettings_Click" meta:resourcekey="btnExportGlobalSettingsResource1" />

		<br /><br />

		<asp:FileUpload ID="upGlobalSettings" runat="server" meta:resourcekey="upGlobalSettingsResource1" />
		<asp:Button ID="btnImportGlobalSettings" runat="server" Text="Import Global Settings" OnClick="btnImportGlobalSettings_Click" meta:resourcekey="btnImportGlobalSettingsResource1" />
		<br />
		<asp:Label ID="lblImportGlobalSettingsResult" runat="server" meta:resourcekey="lblImportGlobalSettingsResultResource1" />

		<br /><br />
		
		<h2 class="separator"><asp:Literal ID="lblMainData" runat="server" Text="Import and Export Wiki-specific Data" EnableViewState="False" meta:resourcekey="lblMainDataResource1" /></h2>

		<asp:Literal ID="lblImportExportStorageProviderDescription" runat="server" EnableViewState="False" Text="Select the wiki you want to work with (import or export)" meta:resourcekey="lblImportExportStorageProviderDescriptionResource1" />
		<br />
		<asp:DropDownList ID="lstWiki" runat="server" OnSelectedIndexChanged="lstWiki_SelectedIndexChanged" AutoPostBack="True" meta:resourcekey="lstWikiResource1" />
		
		<br /><br />

		<h3><asp:Literal ID="lblExportAll" runat="server" EnableViewState="False" Text="Export All Data From All Providers" meta:resourcekey="lblExportAllResource1" /></h3>
		<br />
		<asp:Literal ID="lblRevisions" runat="server" EnableViewState="False" Text="Max number of page revisions to export" meta:resourcekey="lblRevisionsResource1" /><br />
		<asp:TextBox ID="txtRevisions" runat="server" EnableViewState="False" Enabled="False" Text="100" CssClass="configsmallest" meta:resourcekey="txtRevisionsResource1"></asp:TextBox>
		<br /><br />
		<asp:Button ID="btnExportAll" runat="server" Text="Export Data" OnClick="btnExportAll_Click" Enabled="False" meta:resourcekey="btnExportAllResource1" />

		<br /><br />

		<h3><asp:Literal ID="lblImportBackup" runat="server" EnableViewState="False" Text="Import Data Into Selected Providers" meta:resourcekey="lblImportBackupResource1" /></h3>
		<br />
		<asp:Literal ID="lblUploadBackupFile" runat="server" EnableViewState="False" Text="URL of the backup file (examples: 'http://server/data/wiki.zip' or 'file:///D:/Backup.zip')" meta:resourcekey="lblUploadBackupFileResource1" /><br />
		<asp:TextBox ID="txtBackupFileURL" runat="server" EnableViewState="False" Enabled="False" CssClass="configlarge" meta:resourcekey="txtBackupFileURLResource1"></asp:TextBox>

		<br /><br />
		<asp:Literal ID="lblPagesStorageProvider" runat="server" EnableViewState="False" Text="Destination pages storage provider" meta:resourcekey="lblPagesStorageProviderResource1" /><br />
		<asp:DropDownList ID="lstPagesStorageProviders" runat="server" meta:resourcekey="lstPagesStorageProvidersResource1"></asp:DropDownList>
		<br /><br />
		<asp:Literal ID="lblUsersStorageProvider" runat="server" EnableViewState="False" Text="Destination users storage provider" meta:resourcekey="lblUsersStorageProviderResource1" /><br />
		<asp:DropDownList ID="lstUsersStorageProviders" runat="server" meta:resourcekey="lstUsersStorageProvidersResource1"></asp:DropDownList>
		<br /><br />
		<asp:Literal ID="lblFilesStorageProvider" runat="server" EnableViewState="False" Text="Destination files storage provider" meta:resourcekey="lblFilesStorageProviderResource1" /><br />
		<asp:DropDownList ID="lstFilesStorageProviders" runat="server" meta:resourcekey="lstFilesStorageProvidersResource1"></asp:DropDownList>
		
		<br /><br />
		<asp:Button ID="btnImportBackup" runat="server" Text="Import Data" OnClick="btnImportBackup_Click" Enabled="False" meta:resourcekey="btnImportBackupResource1" />
		<br />
		<asp:Label ID="lblImportBackupResult" runat="server" meta:resourcekey="lblImportBackupResultResource1" />
	</div>
	
	<div class="clear"></div>

</asp:Content>
