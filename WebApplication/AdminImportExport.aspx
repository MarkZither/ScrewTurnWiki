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
		
		<br /><br />
		<asp:Button ID="btnExportAll" runat="server" Text="Export" OnClick="btnExportAll_Click" meta:resourcekey="btnExportAllResource1" />

		<br /><br />
		<asp:FileUpload ID="upBackup" runat="server" meta:resourcekey="upBackupResource1" />

		<br /><br />
		<div id="StorageProvidersListContainerDiv">
			<%--<asp:Repeater ID="rptStorageProviders" runat="server" OnDataBinding="rptStorageProviders_DataBinding" OnItemCommand="rptStorageProviders_ItemCommand" Visible="false" >
				<HeaderTemplate>
					<table cellpadding="0" cellspacing="0" class="generic">
						<thead>
						<tr class="tableheader">
							<th><asp:Literal ID="lblName" runat="server" EnableViewState="False" meta:resourcekey="lblNameResource1" Text="Name" /></th>
							<th>&nbsp;</th>
							<th>&nbsp;</th>
						</tr>
						</thead>
						<tbody>
				</HeaderTemplate>
				<ItemTemplate>
					<tr class='tablerow'>
						<td><%# Eval("Provider") %></td>
						<td><asp:LinkButton ID="btnExport" runat="server" Text="Export" ToolTip="Export data from this storage provider for the selected wiki" CommandName="Export" CommandArgument='<%# Eval("ProviderInterface") + "|" + Eval("ProviderType") %>'
							meta:resourcekey="btnExportResource1" /></td>
						<td><asp:LinkButton ID="btnImport" runat="server" Text="Import" ToolTip="Import data into this storage provider for the selected wiki" CommandName="Import" CommandArgument='<%# Eval("ProviderInterface") + "|" + Eval("ProviderType") %>'
							meta:resourcekey="btnImportResource1" /></td>
					</tr>
				</ItemTemplate>
				<AlternatingItemTemplate>
					<tr class='tablerowalternate'>
						<td><%# Eval("Provider") %></td>
						<td><asp:LinkButton ID="btnExport" runat="server" Text="Export" ToolTip="Export data from this storage provider for the selected wiki" CommandName="Export" CommandArgument='<%# Eval("ProviderInterface") + "|" + Eval("ProviderType") %>'
							meta:resourcekey="btnExportResource1" /></td>
						<td><asp:LinkButton ID="btnImport" runat="server" Text="Import" ToolTip="Import data into this storage provider for the selected wiki" CommandName="Import" CommandArgument='<%# Eval("ProviderInterface") + "|" + Eval("ProviderType") %>'
							meta:resourcekey="btnImportResource1" /></td>
					</tr>
				</AlternatingItemTemplate>
				<FooterTemplate>
					</tbody>
					</table>
				</FooterTemplate>
			</asp:Repeater>--%>

			
		<asp:Button ID="btnImportBackup" runat="server" Text="Import" OnClick="btnImportBackup_Click" meta:resourcekey="btnImportBackupResource1" />
		</div>
	</div>
	
	<div class="clear"></div>

</asp:Content>
