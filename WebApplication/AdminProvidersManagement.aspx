<%@ Page Title="" Language="C#" MasterPageFile="~/Admin.master" AutoEventWireup="true" CodeBehind="AdminProvidersManagement.aspx.cs" Inherits="ScrewTurn.Wiki.AdminProvidersManagement" culture="auto" meta:resourcekey="PageResource1" uiculture="auto" %>

<asp:Content ID="ctnHead" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="ctnConfig" ContentPlaceHolderID="cphAdmin" runat="server">
	<div class="leftaligned">
		<h2 class="sectiontitle"><asp:Literal ID="lblPluginsManagementTitle" runat="server" Text="Providers Management" EnableViewState="False" meta:resourcekey="lblPluginsManagementTitleResource1" /></h2>
	
		<asp:Literal ID="lblPlugins" runat="server" Text="Plugins (Formatter Providers)" EnableViewState="False" meta:resourcekey="lblPluginsResource1" />:
		<br />
		
		<div id="ProvidersListContainerDiv">
			<asp:Repeater ID="rptProviders" runat="server" OnDataBinding="rptProviders_DataBinding">
				<HeaderTemplate>
					<table cellpadding="0" cellspacing="0" class="generic">
						<thead>
						<tr class="tableheader">
							<th><asp:Literal ID="lblName" runat="server" EnableViewState="False" meta:resourcekey="lblNameResource1" Text="Name" /></th>
							<th><asp:Literal ID="lblVersion" runat="server" EnableViewState="False" meta:resourcekey="lblVersionResource1" Text="Ver." /></th>
							<th><asp:Literal ID="lblAuthor" runat="server" EnableViewState="False" meta:resourcekey="lblAuthorResource1" Text="Author" /></th>
							<th><asp:Literal ID="lblUpdateStatus" runat="server" EnableViewState="false" meta:resourcekey="lblUpdateStatusResource1" Text="Update Status" /></th>
						</tr>
						</thead>
						<tbody>
				</HeaderTemplate>
				<ItemTemplate>
					<tr class='tablerow<%# Eval("AdditionalClass") %>'>
						<td><%# Eval("Name") %></td>
						<td><%# Eval("Version") %></td>
						<td><a href='<%# Eval("AuthorUrl") %>' target="_blank"><%# Eval("Author") %></a></td>
						<td><%# Eval("UpdateStatus") %></td>
					</tr>
				</ItemTemplate>
				<AlternatingItemTemplate>
					<tr class='tablerowalternate<%# Eval("AdditionalClass") %>'>
						<td><%# Eval("Name") %></td>
						<td><%# Eval("Version") %></td>
						<td><a href='<%# Eval("AuthorUrl") %>' target="_blank"><%# Eval("Author") %></a></td>
						<td><%# Eval("UpdateStatus") %></td>
					</tr>
				</AlternatingItemTemplate>
				<FooterTemplate>
					</tbody>
					</table>
				</FooterTemplate>
			</asp:Repeater>
		</div>

		<div id="ProvidersUpdateDiv">
		
			<script type="text/javascript">
			<!--
				function __ShowUpdateProgress() {
					if(RequestConfirm()) {
						document.getElementById("ProvidersUpdateProgress").style["display"] = "";
						return true;
					}
					else return false;
				}

				function __HideUpdateProgress() {
					document.getElementById("ProvidersUpdateProgress").style["display"] = "none";
				}
			// -->
			</script>
			
			<asp:Button ID="btnAutoUpdateProviders" runat="server" Text="Auto-update Providers" ToolTip="Automatically update all installed providers, of all types"
				OnClick="btnAutoUpdateProviders_Click" meta:resourcekey="btnAutoUpdateProvidersResource1" Visible="false" Enabled="true" />
			<span id="ProvidersUpdateProgress" style="display: none;">
				<img src="Images/Wait.gif" alt="..." />
			</span>
			<asp:Label ID="lblAutoUpdateResult" runat="server" />
			
		</div>
		<br /><br />

		<h2 class="separator"><asp:Literal ID="lblUploadProviders" runat="server" Text="Providers DLLs Management" EnableViewState="False" meta:resourcekey="lblUploadProvidersResource1" /></h2>
		<div id="DllUploadContainerDiv">
			<asp:Literal ID="lblUploadNewDll" runat="server" Text="Upload new DLL" EnableViewState="False" meta:resourcekey="lblUploadNewDllResource1" /><br />
			<asp:FileUpload ID="upDll" runat="server" meta:resourcekey="upDllResource1" />
			<asp:Button ID="btnUpload" runat="server" Text="Upload" OnClick="btnUpload_Click"
				meta:resourcekey="btnUploadResource1" />
			<span id="UploadProgressSpan" style="display: none;"><img src="Images/Wait.gif" alt="Uploading..." /></span><br />
			<asp:Label ID="lblUploadResult" runat="server" meta:resourcekey="lblUploadResultResource1" />
		</div>
		<div id="DllsListContainerDiv">
			<asp:Literal ID="lblRemoveDll" runat="server" Text="Remove DLL" EnableViewState="False" meta:resourcekey="lblRemoveDllResource1" /><br />
			<asp:DropDownList ID="lstDlls" runat="server" 
				OnSelectedIndexChanged="lstDlls_SelectedIndexChanged" 
				meta:resourcekey="lstDllsResource1" AutoPostBack="True" />
			<asp:Button ID="btnDeleteDll" runat="server" Text="Delete" OnClick="btnDeleteDll_Click" Enabled="False" meta:resourcekey="btnDeleteDllResource1" />
			<br />
			<asp:Label ID="lblDllResult" runat="server" meta:resourcekey="lblDllResultResource1" />
		</div>
	
		<div id="DllNoticeDiv">
			<small>
				<asp:Literal ID="lblUploadInfo" runat="server" EnableViewState="False" Text="<b>Note</b>: removing a DLL won't disable the plugins it contains until the application is restarted. Uploading a new DLL will automatically load the plugins is contains." meta:resourcekey="lblUploadInfoResource1" />
			</small>
		</div>
	
		<div class="clear"></div>
	</div>
	
	<div class="clear"></div>

</asp:Content>
