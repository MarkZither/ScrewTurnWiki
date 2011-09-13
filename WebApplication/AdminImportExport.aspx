<%@ Page Title="" Language="C#" MasterPageFile="~/Admin.master" AutoEventWireup="true" CodeBehind="AdminImportExport.aspx.cs" Inherits="ScrewTurn.Wiki.AdminImportExport" culture="auto" meta:resourcekey="PageResource1" uiculture="auto" %>

<asp:Content ID="ctnHead" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="ctnConfig" ContentPlaceHolderID="cphAdmin" runat="server">
	<div class="leftaligned">
		<h2 class="sectiontitle"><asp:Literal ID="lblImportExportTitle" runat="server" Text="Import/Export" EnableViewState="False" meta:resourcekey="lblImportExportTitleResource1" /></h2>
	
		<h2 class="separator"><asp:Literal ID="lblGlobalSettings" runat="server" Text="Global Settings" EnableViewState="False" meta:resourcekey="lblGlobalSettingsResource1" /></h2>
		<h4><asp:Literal ID="lblExportGlobalSettings" runat="server" Text="Export Global Settings and related data" EnableViewState="False" meta:resourcekey="lblExportGlobalSettingsResource1" /></h4>
		<asp:Button ID="btnExportGlobalSettings" runat="server" Text="Export" OnClick="btnExportGlobalSettings_Click" meta:resourcekey="btnExportGlobalSettingsResource1" />
		<br />
		<asp:Label ID="lblExportGlobalSettingsResult" runat="server" meta:resourcekey="lblExportGlobalSettingsResultResource1" />

		<br /><br />

		<h4><asp:Literal ID="lblImportGlobalSettings" runat="server" Text="Import Global Settings and relaated data" EnableViewState="False" meta:resourcekey="lblImportGlobalSettingsResource1" /></h4>
		<asp:FileUpload ID="upGlobalSettings" runat="server" meta:resourcekey="upGlobalSettingsResource1" />
		<asp:Button ID="btnImportGlobalSettings" runat="server" Text="Import" OnClick="btnImportGlobalSettings_Click" meta:resourcekey="btnImportGlobalSettingsResource1" />
		<br />
		<asp:Label ID="lblImportGlobalSettingsResult" runat="server" meta:resourcekey="lblImportGlobalSettingsResultResource1" />

		<br /><br />
		
		<h2 class="separator"><asp:Literal ID="lblStorageProviders" runat="server" Text="Storage Providers" EnableViewState="False" meta:resourcekey="lblStorageProvidersResource1" /></h2>
		<h4><asp:Literal ID="lblImportExportStorageProviderDescription" runat="server" EnableViewState="False" meta:resourcekey="lblImportExportStorageProviderDescriptionResource1" Text="Select a wiki you want to import/export data" /></h4>
		<asp:DropDownList ID="lstWiki" runat="server" OnSelectedIndexChanged="lstWiki_SelectedIndexChanged" AutoPostBack="true" meta:resourcekey="lstWikiResource1" />
		
		<br /><br /><br />
		<h4><asp:Literal ID="lblExportAll" runat="server" EnableViewState="False" meta:resourcekey="lblExportAllResource1" Text="Export all data from all installed providers." /></h4>
		<asp:Button ID="btnExportAll" runat="server" Text="Export" OnClick="btnExportAll_Click" meta:resourcekey="btnExportAllResource1" Enabled="false" />

		<br /><br /><br />
		<h4><asp:Literal ID="lblImportBackup" runat="server" EnableViewState="False" meta:resourcekey="lblImportBackupResource1" Text="Import data from a backup file into selected storage providers." /></h4>

		<br />
		<h4><asp:Literal ID="lblUploadBackupFile" runat="server" EnableViewState="False" meta:resourcekey="lblUploadBackupFileResource1" Text="Select a backup file." /></h4>
		<asp:FileUpload ID="upBackup" runat="server" meta:resourcekey="upBackupResource1" Enabled="false" />

		<br /><br />
		<h4><asp:Literal ID="lblSelectStorageProviders" runat="server" EnableViewState="False" meta:resourcekey="lblSelectStorageProvidersResource1" Text="Select storage providers where you want to import data." /></h4>
		<asp:DropDownList ID="lstPagesStorageProviders" runat="server" meta:resourcekey="lstPagesStorageProvidersResource1"></asp:DropDownList>
		<br /><br />
		<asp:DropDownList ID="lstUsersStorageProviders" runat="server" meta:resourcekey="lstUsersStorageProvidersResource1"></asp:DropDownList>
		<br /><br />
		<asp:DropDownList ID="lstFilesStorageProviders" runat="server" meta:resourcekey="lstFilesStorageProvidersResource1"></asp:DropDownList>
		
		<br /><br />
		<asp:Button ID="btnImportBackup" runat="server" Text="Import" OnClick="btnImportBackup_Click" meta:resourcekey="btnImportBackupResource1" Enabled="false" />
	</div>
	
	<div class="clear"></div>

</asp:Content>
