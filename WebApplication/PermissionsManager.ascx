<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="PermissionsManager.ascx.cs" Inherits="ScrewTurn.Wiki.PermissionsManager" %>

<%@ Register TagPrefix="st" TagName="AclActionsSelector" Src="~/AclActionsSelector.ascx" %>

<div id="PermissionsManagerDiv">
	
	<div id="SubjectsDiv">
		<asp:Literal ID="lblSubjects" runat="server" Text="User Accounts and User Groups" EnableViewState="False" meta:resourcekey="lblSubjectsResource1" /><br />
		<asp:ListBox ID="lstSubjects" runat="server" AutoPostBack="True" OnSelectedIndexChanged="lstSubjects_SelectedIndexChanged" CssClass="listbox" meta:resourcekey="lstSubjectsResource1" />	
	</div>
	
	<div id="SubjectsManagementDiv">
		<asp:Button ID="btnRemove" runat="server" Text="Remove" ToolTip="Remove entries for the selected subject"
			Enabled="False" OnClick="btnRemove_Click" meta:resourcekey="btnRemoveResource1" />
		<br /><br />
		
		<asp:TextBox ID="txtNewSubject" runat="server" CssClass="textbox" meta:resourcekey="txtNewSubjectResource1" />
		<asp:Button ID="btnSearch" runat="server" Text="Search" ToolTip="Search for a User or Group to add" OnClick="btnSearch_Click" meta:resourcekey="btnSearchResource1" /><br />
		<asp:DropDownList ID="lstFoundSubjects" runat="server" CssClass="dropdown" meta:resourcekey="lstFoundSubjectsResource1" />
		<asp:Button ID="btnAdd" runat="server" Text="Add" ToolTip="Add the selected subject" Enabled="False" OnClick="btnAdd_Click" meta:resourcekey="btnAddResource1" /><br />
		<asp:Label ID="lblAddResult" runat="server" meta:resourcekey="lblAddResultResource1" />
	</div>
	
	<div class="clear"></div>
	
	<div id="AclSelectorDiv">
		<h3><asp:Literal ID="lblPermissionsFor" runat="server" Text="Permissions for:" EnableViewState="False" meta:resourcekey="lblPermissionsForResource1" />
		<asp:Literal ID="lblSelectedSubject" runat="server" meta:resourcekey="lblSelectedSubjectResource1" /></h3>
		
		<st:AclActionsSelector ID="aclActionsSelector" runat="server" />
	
	</div>

	<div id="InternalButtonsDiv">
		<asp:Button ID="btnSave" runat="server" Text="Save Permissions" ToolTip="Save this Subject's permissions" Enabled="False" OnClick="btnSave_Click" meta:resourcekey="btnSaveResource1" />
		<asp:Label ID="lblSaveResult" runat="server" meta:resourcekey="lblSaveResultResource1" />
	</div>

</div>
