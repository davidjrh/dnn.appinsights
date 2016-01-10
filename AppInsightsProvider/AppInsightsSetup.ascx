<%@ Control Language="C#" CodeBehind="AppInsightsSetup.ascx.cs" Inherits="DotNetNuke.Monitoring.AppInsights.AppInsightsSetup" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>

<div class="dnnForm" id="form-appinsights">
    <asp:Label runat="server" CssClass="dnnFormMessage dnnFormInfo" ResourceKey="Intro" />    
    <fieldset>
        <div class="dnnFormItem">
            <dnn:Label runat="server" ControlName="chkEnabled" ResourceKey="lblEnabled" />
            <asp:CheckBox runat="server" ID="chkEnabled" AutoPostBack="True" />
        </div>
        
        <div class="dnnFormItem">
            <dnn:Label runat="server" ControlName="txtInstrumentationKey" ResourceKey="lblInstrumentationKey" />
            <asp:TextBox runat="server" ID="txtInstrumentationKey" CssClass="dnnFormRequired" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtInstrumentationKey" Id="rqInstrumentationKey"
                CssClass="dnnFormMessage dnnFormError" ResourceKey="txtInstrumentationKey.Required" ValidationGroup="AppInsights" />
        </div>
    </fieldset>
    <ul class="dnnActions dnnClear">
        <li><asp:LinkButton runat="server" CssClass="dnnPrimaryAction" ResourceKey="lblSave" OnClick="lnkSave_OnClick" /></li>
    </ul>
</div>
