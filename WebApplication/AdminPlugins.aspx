<%@ Page Title="" Language="C#" MasterPageFile="~/Admin.master" AutoEventWireup="true" CodeBehind="AdminPlugins.aspx.cs" Inherits="ScrewTurn.Wiki.AdminPlugins" culture="auto" meta:resourcekey="PageResource1" uiculture="auto" %>

<%@ Register TagPrefix="st" TagName="ProviderSelector" Src="~/ProviderSelector.ascx" %>

<asp:Content ID="ctnHead" ContentPlaceHolderID="head" runat="server">
</asp:Content>


<asp:Content ID="ctnProviders" ContentPlaceHolderID="cphAdmin" runat="server">
	<div class="leftaligned">
		<script type="text/javascript">
		<!--
			function __ShowUploadProgress() {
				document.getElementById("UploadProgressSpan").style["display"] = "";
				return true;
			}
			function __HideUploadProgress() {
				document.getElementById("UploadProgressSpan").style["display"] = "none";
				return true;
			}
		// -->
		</script>

		<h2 class="sectiontitle"><asp:Literal ID="lblPluginsConfiguration" runat="server" Text="Plugins Configuration" EnableViewState="False" meta:resourcekey="lblPluginsConfigurationResource1" /></h2>

		<asp:Panel ID="pnlList" runat="server" meta:resourcekey="pnlListResource1" >
			<asp:Literal ID="lblPlugins" runat="server" Text="Plugins (Formatter Providers)" EnableViewState="False" meta:resourcekey="lblPluginResource1" />
			<br />
		
			<div id="ProvidersListContainerDiv">
				<asp:Repeater ID="rptProviders" runat="server"
					OnDataBinding="rptProviders_DataBinding" OnItemCommand="rptProviders_ItemCommand">
					<HeaderTemplate>
						<table cellpadding="0" cellspacing="0" class="generic">
							<thead>
							<tr class="tableheader">
								<th><asp:Literal ID="lblName" runat="server" EnableViewState="False" meta:resourcekey="lblNameResource1" Text="Name" /></th>
								<th><asp:Literal ID="lblVersion" runat="server" EnableViewState="False" meta:resourcekey="lblVersionResource1" Text="Ver." /></th>
								<th><asp:Literal ID="lblAuthor" runat="server" EnableViewState="False" meta:resourcekey="lblAuthorResource1" Text="Author" /></th>
								<th><asp:Literal ID="lblUpdateStatus" runat="server" EnableViewState="false" meta:resourcekey="lblUpdateStatusResource1" Text="Update Status" /></th>
								<th>&nbsp;</th>
								<th>&nbsp;</th>
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
							<td>
								<asp:LinkButton ID="btnEnable" runat="server" CommandArgument='<%# Eval("TypeName") %>' CommandName="Enable" meta:resourcekey="btnEnableResource1" Text="Enable" ToolTip="Enable the Provider" Visible='<%# Eval("Disabled") %>' />
								<asp:LinkButton ID="btnDisable" runat="server" CommandArgument='<%# Eval("TypeName") %>' CommandName="Disable" meta:resourcekey="btnDisableResource1" Text="Disable" ToolTip="Disable the Provider" Visible='<%# Eval("Enabled") %>' />
							</td>
							<td><asp:LinkButton ID="btnSelect" runat="server" CommandArgument='<%# Eval("TypeName") %>' CommandName="Select" meta:resourcekey="btnSelectResource1" Text="Select" /></td>
						</tr>
					</ItemTemplate>
					<AlternatingItemTemplate>
						<tr class='tablerowalternate<%# Eval("AdditionalClass") %>'>
							<td><%# Eval("Name") %></td>
							<td><%# Eval("Version") %></td>
							<td><a href='<%# Eval("AuthorUrl") %>' target="_blank"><%# Eval("Author") %></a></td>
							<td><%# Eval("UpdateStatus") %></td>
							<td>
								<asp:LinkButton ID="btnEnable" runat="server" CommandArgument='<%# Eval("TypeName") %>' CommandName="Enable" meta:resourcekey="btnEnableResource1" Text="Enable" ToolTip="Enable the Provider" Visible='<%# Eval("Disabled") %>' />
								<asp:LinkButton ID="btnDisable" runat="server" CommandArgument='<%# Eval("TypeName") %>' CommandName="Disable" meta:resourcekey="btnDisableResource1" Text="Disable" ToolTip="Disable the Provider" Visible='<%# Eval("Enabled") %>' />
							</td>
							<td><asp:LinkButton ID="btnSelect" runat="server" Text="Select" CommandName="Select" CommandArgument='<%# Eval("TypeName") %>' meta:resourcekey="btnSelectResource1" /></td>
						</tr>
					</AlternatingItemTemplate>
					<FooterTemplate>
						</tbody>
						</table>
					</FooterTemplate>
				</asp:Repeater>
			</div>
		
			<asp:Panel ID="pnlProviderDetails" runat="server" Visible="False" meta:resourcekey="pnlProviderDetailsResource1">
				<div id="EditProviderDiv">
					<h3><asp:Literal ID="lblProviderName" runat="server" meta:resourcekey="lblProviderNameResource1" /></h3>
					<b><asp:Literal ID="lblProviderDll" runat="server" meta:resourcekey="lblProviderDllResource1" /></b>
					<br /><br />
				
					<asp:Literal ID="lblConfigurationStringTitle" runat="server" Text="Configuration String" EnableViewState="False" meta:resourcekey="lblConfigurationStringTitleResource1" />
					&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
					<a href="#" onclick="javascript:document.getElementById('ProviderConfigHelpDiv').style['display'] = ''; return false;">
					<asp:Literal ID="lblConfigHelp" runat="server" Text="Help" EnableViewState="False" meta:resourcekey="lblConfigHelpResource1" /></a>
					<br />
					<asp:TextBox ID="txtConfigurationString" runat="server" TextMode="MultiLine" CssClass="config" meta:resourcekey="txtConfigurationStringResource1" />
					<br />
					<asp:Button ID="btnSave" runat="server" Text="Save" ToolTip="Save the Configuration String" OnClick="btnSave_Click" meta:resourcekey="btnSaveResource1" />
					<asp:Button ID="btnCancel" runat="server" Text="Cancel" ToolTip="Deselect the Provider" OnClick="btnCancel_Click" meta:resourcekey="btnCancelResource1" />
					
					<div id="ProviderConfigHelpDiv" style="display: none;">
						<asp:Label ID="lblProviderConfigHelp" runat="server" meta:resourcekey="lblProviderConfigHelpResource1" />
					</div>
				</div>
			</asp:Panel>
		</asp:Panel>
		<div class="clear"></div>
		<asp:Label ID="lblResult" runat="server" meta:resourcekey="lblResultResource1" />
		<asp:HiddenField ID="txtCurrentProvider" runat="server" />
	</div>

	<div class="clear"></div>
</asp:Content>
