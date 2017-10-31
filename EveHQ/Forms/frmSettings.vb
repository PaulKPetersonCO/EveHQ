'==============================================================================
'
' EveHQ - An Eve-Online™ character assistance application
' Copyright © 2005-2015  EveHQ Development Team
'
' This file is part of EveHQ.
'
' The source code for EveHQ is free and you may redistribute 
' it and/or modify it under the terms of the MIT License. 
'
' Refer to the NOTICES file in the root folder of EVEHQ source
' project for details of 3rd party components that are covered
' under their own, separate licenses.
'
' EveHQ is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the MIT 
' license below for details.
'
' ------------------------------------------------------------------------------
'
' The MIT License (MIT)
'
' Copyright © 2005-2015  EveHQ Development Team
'
' Permission is hereby granted, free of charge, to any person obtaining a copy
' of this software and associated documentation files (the "Software"), to deal
' in the Software without restriction, including without limitation the rights
' to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
' copies of the Software, and to permit persons to whom the Software is
' furnished to do so, subject to the following conditions:
'
' The above copyright notice and this permission notice shall be included in
' all copies or substantial portions of the Software.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
' IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
' FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
' AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
' LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
' OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
' THE SOFTWARE.
'
' ==============================================================================

Imports EveHQ.EveData
Imports EveHQ.Controls.DBConfigs
Imports EveHQ.Controls.DBControls
Imports EveHQ.Controls
Imports DevComponents.DotNetBar
Imports EveHQ.Core
Imports DevComponents.AdvTree
Imports EveHQ.Market
Imports System.Net.Mail
Imports System.IO
Imports EveHQ.Market.MarketServices
Imports EveHQ.Common.Extensions
Imports Microsoft.Win32
Imports System.Windows.Forms.VisualStyles
Imports System.Net
Imports System.Reflection

Namespace Forms

    Public Class FrmSettings
        Dim _redrawColumns As Boolean = False
        Dim _startup As Boolean = True
        Public Property DoNotRecalculatePilots As Boolean = False

#Region "Form Opening & Closing"

        Private Sub frmSettings_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles Me.FormClosing
            Call SaveMarketSettings()
            Call HQ.Settings.Save()
            If DoNotRecalculatePilots = False Then
                If FrmTraining.IsHandleCreated = True And _redrawColumns = True Then
                    _redrawColumns = False
                    Call FrmTraining.RefreshAllTrainingQueues()
                End If
                Call frmEveHQ.UpdatePilotInfo()
            End If
        End Sub

        Private Sub btnClose_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnClose.Click
            Close()
        End Sub

        Private Sub frmSettings_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
            ' Set the startup flag
            _startup = True

            Call UpdateGeneralSettings()
            Call UpdateColourOptions()
            Call UpdateEveServerSettings()
            Call UpdateAccounts()
            Call UpdatePilots()
            Call UpdateEveFolderOptions()
            Call UpdateViewPilots()
            Call UpdateProxyOptions()
            Call UpdatePlugIns()
            Call UpdateNotificationOptions()
            Call UpdateTrainingQueueOptions()
            Call UpdateG15Options()
            Call UpdateTaskBarIconOptions()
            Call UpdateDashboardOptions()
            Call UpdateMarketSettings()

            ' Set the flag to indicate end of the startup
            _startup = False

            ' Switch to the right tab
            tvwSettings.Select()
            If Tag IsNot Nothing Then
                If Tag.ToString = "" Then
                    tvwSettings.SelectedNode = tvwSettings.Nodes("nodeGeneral")
                Else
                    If tvwSettings.Nodes.ContainsKey(Tag.ToString) = True Then
                        tvwSettings.SelectedNode = tvwSettings.Nodes(Tag.ToString)
                    Else
                        tvwSettings.SelectedNode = tvwSettings.Nodes("nodeGeneral")
                    End If
                End If
            Else
                tvwSettings.SelectedNode = tvwSettings.Nodes("nodeGeneral")
            End If
        End Sub

#End Region

#Region "Treeview Routines"

        Private Sub tvwSettings_AfterSelect(ByVal sender As Object, ByVal e As TreeViewEventArgs) _
            Handles tvwSettings.AfterSelect
            Dim nodeName As String = e.Node.Name
            Dim gbName As String = nodeName.TrimStart("node".ToCharArray)
            gbName = "gb" & gbName

            For Each setControl As Control In panelSettings.Controls
                If _
                    setControl.Name = "gpNav" Or setControl.Name = "tvwSettings" Or setControl.Name = "btnClose" Or
                    setControl.Name = gbName Then
                    panelSettings.Controls(gbName).Top = 12
                    panelSettings.Controls(gbName).Left = 195
                    panelSettings.Controls(gbName).Width = 700
                    panelSettings.Controls(gbName).Height = 500
                    panelSettings.Controls(gbName).Visible = True
                Else
                    setControl.Visible = False
                End If
            Next
        End Sub

        Private Sub tvwSettings_NodeMouseClick(ByVal sender As Object, ByVal e As TreeNodeMouseClickEventArgs) _
            Handles tvwSettings.NodeMouseClick
            tvwSettings.SelectedNode = e.Node
        End Sub

#End Region

#Region "General Settings"

        Private Sub UpdateGeneralSettings()
            chkAutoHide.Checked = HQ.Settings.AutoHide
            chkAutoRun.Checked = HQ.Settings.AutoStart
            chkAutoMinimise.Checked = HQ.Settings.AutoMinimise
            chkMinimiseOnExit.Checked = HQ.Settings.MinimiseExit
            chkDisableAutoConnections.Checked = HQ.Settings.DisableAutoWebConnections
            chkDisableTrainingBar.Checked = HQ.Settings.DisableTrainingBar
            chkEnableAutomaticSave.Checked = HQ.Settings.EnableAutomaticSave
            nudAutomaticSaveTime.Enabled = HQ.Settings.EnableAutomaticSave
            nudAutomaticSaveTime.Value = HQ.Settings.AutomaticSaveTime
            For idx As Integer = 0 To 5
                If (HQ.Settings.StartupForms And CInt(2 ^ idx)) = CInt(2 ^ idx) Then
                    Select Case idx
                        Case 0
                            chkViewPilotInfo.Checked = True
                        Case 1
                            chkViewSkillTraining.Checked = True
                        Case 2
                            chkViewMarketPrices.Checked = True
                        Case 3
                            chkViewDashboard.Checked = True
                        Case 4
                            chkViewRequisitions.Checked = True
                        Case 5
                            chkViewPilotSummary.Checked = True
                    End Select
                End If
            Next
            txtUpdateLocation.Text = HQ.Settings.UpdateUrl
            txtUpdateLocation.Enabled = False
            chkErrorReporting.Checked = HQ.Settings.ErrorReportingEnabled
            txtErrorRepName.Text = HQ.Settings.ErrorReportingName
            txtErrorRepEmail.Text = HQ.Settings.ErrorReportingEmail
            If HQ.Settings.MdiTabPosition IsNot Nothing Then
                cboMDITabPosition.SelectedItem = HQ.Settings.MdiTabPosition
            Else
                cboMDITabPosition.SelectedItem = "Top"
            End If
        End Sub

        Private Sub chkAutoHide_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkAutoHide.CheckedChanged
            HQ.Settings.AutoHide = chkAutoHide.Checked
        End Sub

        Private Sub chkAutoRun_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkAutoRun.CheckedChanged
            If chkAutoRun.Checked = True Then
                Dim regKey As RegistryKey =
                        Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", True)
                If HQ.IsUsingLocalFolders = False Then
                    regKey.SetValue(Application.ProductName, """" & Application.ExecutablePath.ToString & """")
                Else
                    regKey.SetValue(Application.ProductName, """" & Application.ExecutablePath.ToString & """" & " /local")
                End If
                HQ.Settings.AutoStart = True
            Else
                Dim regKey As RegistryKey =
                        Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", True)
                regKey.DeleteValue(Application.ProductName, False)
                HQ.Settings.AutoStart = False
            End If
        End Sub

        Private Sub chkAutoMinimise_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkAutoMinimise.CheckedChanged
            HQ.Settings.AutoMinimise = chkAutoMinimise.Checked
        End Sub

        Private Sub chkMinimiseOnExit_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkMinimiseOnExit.CheckedChanged
            HQ.Settings.MinimiseExit = chkMinimiseOnExit.Checked
        End Sub

        Private Sub chkDisableAutoConnections_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkDisableAutoConnections.CheckedChanged
            HQ.Settings.DisableAutoWebConnections = chkDisableAutoConnections.Checked
            If HQ.Settings.DisableAutoWebConnections = True Then
                chkAutoAPI.Checked = False
                chkAutoAPI.Enabled = False
                chkAutoMailAPI.Checked = False
                chkAutoMailAPI.Enabled = False
            Else
                chkAutoAPI.Enabled = True
                chkAutoMailAPI.Enabled = True
            End If
        End Sub

        Private Sub chkDisableTrainingBar_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkDisableTrainingBar.CheckedChanged
            HQ.Settings.DisableTrainingBar = chkDisableTrainingBar.Checked
            If HQ.Settings.DisableTrainingBar = False Then
                If HQ.Settings.TrainingBarDockPosition = eDockSide.None Then
                    HQ.Settings.TrainingBarDockPosition = eDockSide.Bottom
                End If
                frmEveHQ.Bar1.DockSide = CType(HQ.Settings.TrainingBarDockPosition, eDockSide)
                frmEveHQ.DockContainerItem1.Height = HQ.Settings.TrainingBarHeight
                frmEveHQ.DockContainerItem1.Width = HQ.Settings.TrainingBarWidth
            Else
                frmEveHQ.Bar1.Visible = False
                ' Clear old event handlers and controls
                For c As Integer = FrmEveHQ.trainingBlockLayout.Controls.Count - 1 To 0 Step -1
                    Dim cb As CharacterTrainingBlock = CType(FrmEveHQ.trainingBlockLayout.Controls(c), CharacterTrainingBlock)
                    RemoveHandler cb.lblSkill.Click, AddressOf FrmEveHQ.TrainingStatusLabelClick
                    RemoveHandler cb.lblTime.Click, AddressOf FrmEveHQ.TrainingStatusLabelClick
                    RemoveHandler cb.lblQueue.Click, AddressOf FrmEveHQ.TrainingStatusLabelClick
                    cb.Dispose()
                Next
                ' Clear items - just to make sure
                FrmEveHQ.trainingBlockLayout.Controls.Clear()
            End If
        End Sub

        Private Sub chkEnableAutomaticSave_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) Handles chkEnableAutomaticSave.CheckedChanged
            HQ.Settings.EnableAutomaticSave = chkEnableAutomaticSave.Checked
            nudAutomaticSaveTime.Enabled = chkEnableAutomaticSave.Checked
            If chkEnableAutomaticSave.Checked = True Then
                frmEveHQ.tmrSave.Start()
            Else
                frmEveHQ.tmrSave.Stop()
            End If
        End Sub

        Private Sub nudAutomaticSaveTime_Click(ByVal sender As Object, ByVal e As EventArgs) Handles nudAutomaticSaveTime.Click
            HQ.Settings.AutomaticSaveTime = CInt(nudAutomaticSaveTime.Value)
            frmEveHQ.tmrSave.Interval = CInt(nudAutomaticSaveTime.Value) * 60000
        End Sub

        Private Sub nudAutomaticSaveTime_HandleDestroyed(ByVal sender As Object, ByVal e As EventArgs) Handles nudAutomaticSaveTime.HandleDestroyed
            HQ.Settings.AutomaticSaveTime = CInt(nudAutomaticSaveTime.Value)
            frmEveHQ.tmrSave.Interval = CInt(nudAutomaticSaveTime.Value) * 60000
        End Sub

        Private Sub nudAutomaticSaveTime_KeyUp(ByVal sender As Object, ByVal e As KeyEventArgs) Handles nudAutomaticSaveTime.KeyUp
            HQ.Settings.AutomaticSaveTime = CInt(nudAutomaticSaveTime.Value)
            frmEveHQ.tmrSave.Interval = CInt(nudAutomaticSaveTime.Value) * 60000
        End Sub

        Private Sub cboStartupPilot_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles cboStartupPilot.SelectedIndexChanged
            HQ.Settings.StartupPilot = CStr(cboStartupPilot.SelectedItem)
        End Sub

        Private Sub UpdateViewPilots()
            cboStartupPilot.Items.Clear()
            For Each myPilot As EveHQPilot In HQ.Settings.Pilots.Values
                If myPilot.Active = True Then
                    cboStartupPilot.Items.Add(myPilot.Name)
                End If
            Next
            If cboStartupPilot.Items.Contains(HQ.Settings.StartupPilot) = False Then
                If cboStartupPilot.Items.Count > 0 Then
                    cboStartupPilot.SelectedIndex = 0
                End If
            Else
                cboStartupPilot.SelectedItem = HQ.Settings.StartupPilot
            End If
        End Sub

        Private Sub lblUpdateLocation_DoubleClick(ByVal sender As Object, ByVal e As EventArgs) _
            Handles lblUpdateLocation.DoubleClick
            txtUpdateLocation.Enabled = True
        End Sub

        Private Sub txtUpdateLocation_TextChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles txtUpdateLocation.TextChanged
            HQ.Settings.UpdateUrl = txtUpdateLocation.Text
        End Sub

        Private Sub chkErrorReporting_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkErrorReporting.CheckedChanged
            HQ.Settings.ErrorReportingEnabled = chkErrorReporting.Checked
            If HQ.Settings.ErrorReportingEnabled = True Then
                lblErrorRepEmail.Enabled = True
                lblErrorRepName.Enabled = True
                txtErrorRepEmail.Enabled = True
                txtErrorRepName.Enabled = True
            Else
                lblErrorRepEmail.Enabled = False
                lblErrorRepName.Enabled = False
                txtErrorRepEmail.Enabled = False
                txtErrorRepName.Enabled = False
            End If
        End Sub

        Private Sub txtErrorRepName_TextChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles txtErrorRepName.TextChanged
            HQ.Settings.ErrorReportingName = txtErrorRepName.Text
        End Sub

        Private Sub txtErrorRepEmail_TextChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles txtErrorRepEmail.TextChanged
            HQ.Settings.ErrorReportingEmail = txtErrorRepEmail.Text
        End Sub

        Private Sub cboMDITabPosition_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles cboMDITabPosition.SelectedIndexChanged
            HQ.Settings.MdiTabPosition = cboMDITabPosition.SelectedItem.ToString
            Select Case HQ.Settings.MdiTabPosition
                Case "Top"
                    frmEveHQ.tabEveHQMDI.Dock = DockStyle.Top
                    frmEveHQ.tabEveHQMDI.TabAlignment = eTabStripAlignment.Top
                Case "Bottom"
                    frmEveHQ.tabEveHQMDI.Dock = DockStyle.Bottom
                    frmEveHQ.tabEveHQMDI.TabAlignment = eTabStripAlignment.Bottom
            End Select
        End Sub

        Private Sub chkViewPilotInfo_CheckedChanged(sender As System.Object, e As EventArgs) Handles chkViewPilotInfo.CheckedChanged, chkViewSkillTraining.CheckedChanged, chkViewMarketPrices.CheckedChanged, chkViewDashboard.CheckedChanged, chkViewRequisitions.CheckedChanged, chkViewPilotSummary.CheckedChanged
            If _startup = False Then
                Dim chkbox As CheckBox = CType(sender, CheckBox)
                Dim value As Integer = CInt(chkbox.Tag)
                If chkbox.Checked = True Then
                    HQ.Settings.StartupForms += value
                Else
                    HQ.Settings.StartupForms -= value
                End If
            End If
        End Sub

#End Region

#Region "Colour Settings"

        Private Sub UpdateColourOptions()
            ' Update the pilot colours
            Call UpdatePBPilotColours()
            chkDisableVisualStyles.Checked = HQ.Settings.DisableVisualStyles
            txtCSVSeparator.Text = HQ.Settings.CsvSeparatorChar
        End Sub

        Private Sub UpdatePbPilotColours()
            pbPilotCurrent.BackColor = Color.FromArgb(CInt(HQ.Settings.PilotCurrentTrainSkillColor))
            pbPilotLevel5.BackColor = Color.FromArgb(CInt(HQ.Settings.PilotLevel5SkillColor))
            pbPilotPartial.BackColor = Color.FromArgb(CInt(HQ.Settings.PilotPartTrainedSkillColor))
            pbPilotStandard.BackColor = Color.FromArgb(CInt(HQ.Settings.PilotStandardSkillColor))
            pbPilotGroupBG.BackColor = Color.FromArgb(CInt(HQ.Settings.PilotGroupBackgroundColor))
            pbPilotGroupText.BackColor = Color.FromArgb(CInt(HQ.Settings.PilotGroupTextColor))
            pbPilotSkillText.BackColor = Color.FromArgb(CInt(HQ.Settings.PilotSkillTextColor))
            pbPilotSkillHighlight.BackColor = Color.FromArgb(CInt(HQ.Settings.PilotSkillHighlightColor))
        End Sub

        Private Sub pbPilotColours_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles pbPilotGroupBG.Click, pbPilotGroupText.Click, pbPilotSkillText.Click, pbPilotSkillHighlight.Click,
                    pbPilotCurrent.Click, pbPilotLevel5.Click, pbPilotPartial.Click, pbPilotStandard.Click
            Dim thisPb As PictureBox = CType(sender, PictureBox)
            Dim dlgResult As Integer
            With cd1
                .AllowFullOpen = True
                .AnyColor = True
                .FullOpen = True
                Select Case thisPb.Name
                    Case "pbPilotCurrent"
                        .Color = Color.FromArgb(CInt(HQ.Settings.PilotCurrentTrainSkillColor))
                    Case "pbPilotLevel5"
                        .Color = Color.FromArgb(CInt(HQ.Settings.PilotLevel5SkillColor))
                    Case "pbPilotPartial"
                        .Color = Color.FromArgb(CInt(HQ.Settings.PilotPartTrainedSkillColor))
                    Case "pbPilotStandard"
                        .Color = Color.FromArgb(CInt(HQ.Settings.PilotStandardSkillColor))
                    Case "pbPilotGroupBG"
                        .Color = Color.FromArgb(CInt(HQ.Settings.PilotGroupBackgroundColor))
                    Case "pbPilotGroupText"
                        .Color = Color.FromArgb(CInt(HQ.Settings.PilotGroupTextColor))
                    Case "pbPilotSkillText"
                        .Color = Color.FromArgb(CInt(HQ.Settings.PilotSkillTextColor))
                    Case "pbPilotSkillHighlight"
                        .Color = Color.FromArgb(CInt(HQ.Settings.PilotSkillHighlightColor))
                End Select
                dlgResult = .ShowDialog()
            End With
            If dlgResult = DialogResult.Cancel Then
                Exit Sub
            Else
                thisPb.BackColor = cd1.Color
                Select Case thisPb.Name
                    Case "pbPilotCurrent"
                        HQ.Settings.PilotCurrentTrainSkillColor = cd1.Color.ToArgb
                    Case "pbPilotLevel5"
                        HQ.Settings.PilotLevel5SkillColor = cd1.Color.ToArgb
                    Case "pbPilotPartial"
                        HQ.Settings.PilotPartTrainedSkillColor = cd1.Color.ToArgb
                    Case "pbPilotStandard"
                        HQ.Settings.PilotStandardSkillColor = cd1.Color.ToArgb
                    Case "pbPilotGroupBG"
                        HQ.Settings.PilotGroupBackgroundColor = cd1.Color.ToArgb
                    Case "pbPilotGroupText"
                        HQ.Settings.PilotGroupTextColor = cd1.Color.ToArgb
                    Case "pbPilotSkillText"
                        HQ.Settings.PilotSkillTextColor = cd1.Color.ToArgb
                    Case "pbPilotSkillHighlight"
                        HQ.Settings.PilotSkillHighlightColor = cd1.Color.ToArgb
                End Select
                ' Update the colours
                FrmPilot.adtSkills.Refresh()
            End If
        End Sub

        Private Sub btnResetPilotColours_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnResetPilotColours.Click
            ' Resets the panel colours to the default values
            HQ.Settings.PilotCurrentTrainSkillColor = Color.LimeGreen.ToArgb
            HQ.Settings.PilotLevel5SkillColor = Color.Thistle.ToArgb
            HQ.Settings.PilotPartTrainedSkillColor = Color.Gold.ToArgb
            HQ.Settings.PilotStandardSkillColor = Color.White.ToArgb
            HQ.Settings.PilotGroupBackgroundColor = Color.DimGray.ToArgb
            HQ.Settings.PilotGroupTextColor = Color.White.ToArgb
            HQ.Settings.PilotSkillTextColor = Color.Black.ToArgb
            HQ.Settings.PilotSkillHighlightColor = Color.DodgerBlue.ToArgb
            ' Update the colours
            FrmPilot.adtSkills.Refresh()
            ' Update the PBPilot Colours
            Call UpdatePBPilotColours()
        End Sub

        Private Sub chkDisableVisualStyles_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkDisableVisualStyles.CheckedChanged
            HQ.Settings.DisableVisualStyles = chkDisableVisualStyles.Checked
            If chkDisableVisualStyles.Checked = True Then
                Application.VisualStyleState = VisualStyleState.NoneEnabled
            Else
                Application.VisualStyleState = VisualStyleState.ClientAndNonClientAreasEnabled
            End If
        End Sub

        Private Sub txtCSVSeparator_TextChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles txtCSVSeparator.TextChanged
            HQ.Settings.CsvSeparatorChar = txtCSVSeparator.Text
        End Sub

#End Region

#Region "Eve Accounts Settings"

        Private Sub btnAddAccount_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnAddAccount.Click
            ' Clear the text boxes
            Using myAccounts As FrmModifyEveAccounts = New FrmModifyEveAccounts
                With myAccounts
                    .Tag = "Add"
                    .txtUserIDV2.Text = ""
                    .txtUserIDV2.Enabled = True
                    .txtAPIKeyV2.Text = ""
                    .txtAPIKeyV2.Enabled = True
                    .txtAccountNameV2.Text = ""
                    .txtAccountNameV2.Enabled = True
                    .btnAcceptV2.Text = "OK"
                    .Text = "Add New Account"
                    .ShowDialog()
                End With
                UpdateAccounts()
            End Using
        End Sub

        Private Sub btnEditAccount_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnEditAccount.Click
            Call EditAccount()
        End Sub

        Private Sub EditAccount()
            ' Check for some selection on the listview
            If adtAccounts.SelectedNodes.Count = 0 Then
                MessageBox.Show("Please select an account to edit!", "Cannot Edit", MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation)
                adtAccounts.Select()
            Else
                Using myAccounts As FrmModifyEveAccounts = New FrmModifyEveAccounts
                    With myAccounts
                        ' Load the account details into the text boxes
                        Dim selAccount As EveHQAccount = HQ.Settings.Accounts(adtAccounts.SelectedNodes(0).Name)
                        Select Case selAccount.ApiKeySystem
                            Case APIKeySystems.Version2
                                .txtUserIDV2.Text = selAccount.UserID
                                .txtUserIDV2.Enabled = False
                                .txtAPIKeyV2.Text = selAccount.APIKey
                                .txtAPIKeyV2.Enabled = True
                                .txtAccountNameV2.Text = selAccount.FriendlyName
                                .txtAccountNameV2.Enabled = True
                                .lblAPIKeyTypeV2.Text = selAccount.APIKeyType.ToString
                                .lblAPIAccessMask.Text = selAccount.AccessMask.ToString
                                .btnAcceptV2.Text = "OK"
                        End Select
                        .Tag = "Edit"
                        .Text = "Edit '" & selAccount.FriendlyName & "' Account Details"
                        ' Disable the username text box (cannot edit this by design!!)
                        .ShowDialog()
                    End With
                    UpdateAccounts()
                End Using
            End If
        End Sub

        Private Sub btnDeleteAccount_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnDeleteAccount.Click
            ' Check for some selection on the listview
            If adtAccounts.SelectedNodes.Count = 0 Then
                MessageBox.Show("Please select an account to delete!", "Cannot Delete Account", MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation)
                adtAccounts.Select()
            Else
                Dim selAccount As String = adtAccounts.SelectedNodes(0).Name
                Dim selAccountName As String = adtAccounts.SelectedNodes(0).Text
                ' Get the list of pilots that are affected
                Dim strPilots As String = ""
                For Each dPilot As EveHQPilot In HQ.Settings.Pilots.Values
                    If dPilot.Account = selAccount Then
                        strPilots &= dPilot.Name & ControlChars.CrLf
                    End If
                Next
                If strPilots = "" Then strPilots = "<none>"
                ' Confirm deletion
                Dim msg As String = ""
                If strPilots = "<none>" Then
                    msg &= "Deleting the '" & selAccountName & "' account will not delete any of your existing pilots." &
                           ControlChars.CrLf & ControlChars.CrLf
                Else
                    msg &= "Deleting the '" & selAccountName & "' account will delete the following pilots:" &
                           ControlChars.CrLf & strPilots & ControlChars.CrLf
                End If
                msg &= "Are you sure you wish to delete the account '" & selAccountName & "'?"
                Dim confirm As Integer = MessageBox.Show(msg, "Confirm Delete?", MessageBoxButtons.YesNo,
                                                         MessageBoxIcon.Question)
                If confirm = vbYes Then
                    ' Delete the account from the accounts collection
                    HQ.Settings.Accounts.Remove(adtAccounts.SelectedNodes(0).Name)
                    ' Remove the item from the list
                    adtAccounts.Nodes.Remove(adtAccounts.SelectedNodes(0))
                    ' Remove the pilots
                    Dim removeList As New List(Of String)
                    For Each dPilot As EveHQPilot In HQ.Settings.Pilots.Values
                        If dPilot.Account = selAccount Then
                            removeList.Add(dPilot.Name)
                        End If
                    Next
                    For Each pilotName As String In removeList
                        HQ.Settings.Pilots.Remove(pilotName)
                    Next
                    Call frmEveHQ.UpdatePilotInfo()
                    Call UpdatePilots()
                Else
                    adtAccounts.Select()
                    Exit Sub
                End If
            End If
        End Sub

        Private Sub btnDisableAccount_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnDisableAccount.Click
            ' Check for some selection on the listview
            If adtAccounts.SelectedNodes.Count = 0 Then
                MessageBox.Show("Please select an account to toggle the status of!", "Cannot Set Account Status",
                                MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                adtAccounts.Select()
            Else
                Dim si As Node = adtAccounts.SelectedNodes(0)
                Dim userID As String = si.Name
                Dim cAccount As EveHQAccount = HQ.Settings.Accounts(userID)
                Select Case cAccount.APIAccountStatus
                    Case APIAccountStatuses.Active, APIAccountStatuses.Alpha
                        cAccount.APIAccountStatus = APIAccountStatuses.ManualDisabled
                        btnDisableAccount.Text = "Enable Account"
                    Case APIAccountStatuses.ManualDisabled
                        If cAccount.PaidUntil < Now Then
                            ' Account is alpha state
                            cAccount.APIAccountStatus = APIAccountStatuses.Alpha
                        Else
                            cAccount.APIAccountStatus = APIAccountStatuses.Active
                        End If
                        btnDisableAccount.Text = "Disable Account"
                End Select
                si.Cells(4).Text = cAccount.APIAccountStatus.ToString
            End If
        End Sub

        Private Sub btnGetData_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnGetData.Click
            Call frmEveHQ.QueryMyEveServer()
        End Sub

        Public Sub UpdateAccounts()
            adtAccounts.BeginUpdate()
            adtAccounts.Nodes.Clear()
            For Each newAccount As EveHQAccount In HQ.Settings.Accounts.Values
                Dim newLine As New Node
                newLine.Name = newAccount.UserID
                newLine.Text = newAccount.FriendlyName
                newLine.Cells.Add(New Cell(newAccount.ApiKeySystem.ToString))
                newLine.Cells.Add(New Cell(newAccount.UserID))
                newLine.Cells.Add(New Cell(newAccount.APIKeyType.ToString))
                newLine.Cells.Add(New Cell(newAccount.APIAccountStatus.ToString))
                Select Case newAccount.APIKeyType
                    Case APIKeyTypes.Unknown, APIKeyTypes.Limited
                        Dim ttt As String = ""
                        ttt &= "Account Creation Date: <Unknown>" & ControlChars.CrLf
                        ttt &= "Account Expiry Date: <Unknown>" & ControlChars.CrLf
                        ttt &= "Logon Count: <Unknown>" & ControlChars.CrLf
                        ttt &= "Time Online: <Unknown>"
                        Dim sti As New SuperTooltipInfo("Account Name: " & newAccount.FriendlyName,
                                                     "Eve API Account Information", ttt, Nothing, My.Resources.Info32,
                                                     eTooltipColor.Yellow)
                        STT.SetSuperTooltip(newLine, sti)
                    Case APIKeyTypes.Full, APIKeyTypes.Character, APIKeyTypes.Corporation
                        Dim ttt As String = ""
                        ttt &= "Account Creation Date: " & newAccount.CreateDate.ToString & ControlChars.CrLf
                        ttt &= "Account Expiry Date: " & newAccount.PaidUntil.ToString & ControlChars.CrLf
                        ttt &= "Logon Count: " & newAccount.LogonCount.ToString("N0") & ControlChars.CrLf
                        ttt &= "Time Online: " & SkillFunctions.TimeToString(newAccount.LogonMinutes * 60, False)
                        Dim sti As New SuperTooltipInfo("Account Name: " & newAccount.FriendlyName,
                                                     "Eve API Account Information", ttt, Nothing, My.Resources.Info32,
                                                     eTooltipColor.Yellow)
                        STT.SetSuperTooltip(newLine, sti)
                End Select
                adtAccounts.Nodes.Add(newLine)
            Next
            adtAccounts.EndUpdate()
            If HQ.Settings.Accounts.Count = 0 Then
                frmEveHQ.btnQueryAPI.Enabled = False
            Else
                frmEveHQ.btnQueryAPI.Enabled = True
            End If
        End Sub

        Private Sub btnCheckAPIKeys_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnCheckAPIKeys.Click
            ' Checks the API keys types by reference to the account status API
            For Each cAccount As EveHQAccount In HQ.Settings.Accounts.Values
                Call cAccount.CheckAPIKey()
            Next
            Call UpdateAccounts()
        End Sub

        Private Sub adtAccounts_NodeDoubleClick(ByVal sender As Object, ByVal e As TreeNodeMouseEventArgs) _
            Handles adtAccounts.NodeDoubleClick
            Call EditAccount()
        End Sub

        Private Sub adtAccounts_SelectionChanged(ByVal sender As Object, ByVal e As EventArgs) Handles adtAccounts.SelectionChanged
            If adtAccounts.SelectedNodes.Count = 1 Then
                Dim si As Node = adtAccounts.SelectedNodes(0)
                Dim userID As String = si.Name
                Dim cAccount As EveHQAccount = HQ.Settings.Accounts(userID)
                Select Case cAccount.APIAccountStatus
                    Case APIAccountStatuses.Active, APIAccountStatuses.Alpha
                        btnDisableAccount.Text = "Disable Account"
                        btnDisableAccount.Enabled = True
                    Case APIAccountStatuses.ManualDisabled
                        btnDisableAccount.Text = "Enable Account"
                        btnDisableAccount.Enabled = True
                End Select
            Else
                btnDisableAccount.Text = "Disable Account"
                btnDisableAccount.Enabled = False
            End If
        End Sub

#End Region

#Region "Plug-ins Settings"

        Public Sub UpdatePlugIns()
            lvwPlugins.Items.Clear()
            For Each plugin As EveHQPlugIn In HQ.Plugins.Values
                Dim item As New ListViewItem
                item.Name = plugin.Name
                item.Text = plugin.Name & " (v" & plugin.Version & ")"
                item.SubItems.Add("")
                item.Checked = Not plugin.Disabled
                UpdatePluginStatus(plugin, item)
                lvwPlugins.Items.Add(item)
            Next
        End Sub

        Private Sub UpdatePluginStatus(plugin As EveHQPlugIn, item As ListViewItem)
            If plugin.Disabled = True Then
                Dim status As String = ""
                Select Case plugin.Status
                    Case EveHQPlugInStatus.Uninitialised
                        status = "Uninitialised"
                    Case EveHQPlugInStatus.Loading
                        status = "Loading"
                    Case EveHQPlugInStatus.Failed
                        status = "Failed"
                    Case EveHQPlugInStatus.Active
                        status = "Active"
                End Select
                item.SubItems(1).Text = ("Disabled" & " (" & status & ")")
            Else
                Dim status As String = ""
                Select Case plugin.Status
                    Case EveHQPlugInStatus.Uninitialised
                        status = "Uninitialised"
                    Case EveHQPlugInStatus.Loading
                        status = "Loading"
                    Case EveHQPlugInStatus.Failed
                        status = "Failed"
                    Case EveHQPlugInStatus.Active
                        status = "Active"
                End Select
                item.SubItems(1).Text = ("Enabled" & " (" & status & ")")
            End If
            If plugin.Available = False Then
                item.SubItems(1).Text = "Unavailable"
            End If
        End Sub

        Private Sub btnTidyPlugins_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnTidyPlugins.Click
            Dim removePlugIns As New ArrayList
            For Each newPlugIn As EveHQPlugIn In HQ.Plugins.Values
                If newPlugIn.Available = False Then
                    removePlugIns.Add(newPlugIn.Name)
                End If
            Next
            For Each plugin As String In removePlugIns
                HQ.Settings.Plugins.Remove(plugin)
            Next
            Call UpdatePlugIns()
        End Sub

        Private Sub btnRefreshPlugins_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnRefreshPlugins.Click
            Call UpdatePlugIns()
        End Sub

        Private Sub lvwPlugins_ItemChecked(ByVal sender As Object, ByVal e As ItemCheckedEventArgs) Handles lvwPlugins.ItemChecked
            Dim pluginName As String = e.Item.Name
            Dim plugin As EveHQPlugIn = HQ.Plugins(pluginName)
            plugin.Disabled = Not e.Item.Checked
            If HQ.Settings.Plugins.ContainsKey(pluginName) Then
                HQ.Settings.Plugins(pluginName).Disabled = Not e.Item.Checked
            Else
                HQ.Settings.Plugins.Add(pluginName, New EveHQPlugInConfig With
                                                   {.Name = pluginName,
                                                    .Disabled = Not e.Item.Checked})
            End If
            UpdatePluginStatus(plugin, e.Item)
        End Sub

#End Region

#Region "Eve Pilots Settings"

        Private Sub btnAddPilot_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnAddPilot.Click
            ' Clear the text boxes
            Using myPilots As FrmModifyEvePilots = New FrmModifyEvePilots
                With myPilots
                    .txtPilotName.Text = ""
                    .txtPilotName.Enabled = True
                    .txtPilotID.Text = ""
                    .txtPilotID.Enabled = True
                    .Text = "Add New Pilot"
                    .ShowDialog()
                End With
                UpdatePilots()
            End Using
            Call FrmEveHQ.UpdatePilotInfo()
        End Sub

        Private Sub btnAddPilotFromXML_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnAddPilotFromXML.Click
            Call PilotParseFunctions.LoadPilotFromXML()
            Call frmEveHQ.UpdatePilotInfo()
            Call UpdatePilots()
        End Sub

        Private Sub btnCreateBlankPilot_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnCreateBlankPilot.Click
            Using newCharForm As New FrmCharCreate
                newCharForm.ShowDialog()
                Call UpdatePilots()
            End Using
        End Sub

        Public Sub UpdatePilots()
            lvwPilots.Items.Clear()
            For Each newPilot As EveHQPilot In HQ.Settings.Pilots.Values
                Dim newLine As New ListViewItem
                newLine.Text = newPilot.Name
                newLine.SubItems.Add(newPilot.ID)

                If newPilot.Account <> "" And HQ.Settings.Accounts.ContainsKey(newPilot.Account) Then
                    newLine.SubItems.Add(HQ.Settings.Accounts(newPilot.Account).FriendlyName)
                Else
                    If newPilot.Account <> "" Then
                        newLine.SubItems.Add(newPilot.Account)
                    Else
                        newLine.SubItems.Add("-")
                    End If
                End If

                If newPilot.Active = True Then
                    newLine.Checked = True
                Else
                    newLine.Checked = False
                End If
                lvwPilots.Items.Add(newLine)
            Next
        End Sub

        Private Sub btnDeletePilot_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnDeletePilot.Click
            ' Check for some selection on the listview
            If lvwPilots.SelectedItems.Count = 0 Then
                MessageBox.Show("Please select a pilot to delete!", "Cannot Delete Pilot", MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation)
                lvwPilots.Select()
            Else
                Dim selPilot As String = lvwPilots.SelectedItems(0).Text
                Dim selAccount As String = lvwPilots.SelectedItems(0).SubItems(2).Text
                ' Check if the pilot is linked to an account - and therefore cannot be deleted
                If selAccount <> "" And selAccount <> "0" Then
                    Dim msg As String = ""
                    msg &= "You cannot delete pilot '" & selPilot & "' as it is currently associated with account '" &
                           selAccount & "'."
                    MessageBox.Show(msg, "Cannot Delete Pilot", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Exit Sub
                End If
                ' Confirm deletion
                Dim strMsg As String = "Are you sure you wish to delete pilot '" & selPilot & "'?"
                Dim confirm As Integer = MessageBox.Show(strMsg, "Confirm Pilot Delete?", MessageBoxButtons.YesNo,
                                                         MessageBoxIcon.Question)
                If confirm = vbYes Then
                    ' Delete the account from the accounts collection
                    HQ.Settings.Pilots.Remove(selPilot)
                    ' Update the settings view
                    Call UpdatePilots()
                    ' Update the list of pilots in the main form
                    Call frmEveHQ.UpdatePilotInfo()
                Else
                    lvwPilots.Select()
                    Exit Sub
                End If
            End If
        End Sub

        Private Sub lvwPilots_ColumnClick(ByVal sender As Object, ByVal e As ColumnClickEventArgs) _
            Handles lvwPilots.ColumnClick
            If CInt(lvwPilots.Tag) = e.Column Then
                lvwPilots.ListViewItemSorter = New ListViewItemComparerText(e.Column, SortOrder.Ascending)
                lvwPilots.Tag = -1
            Else
                lvwPilots.ListViewItemSorter = New ListViewItemComparerText(e.Column, SortOrder.Descending)
                lvwPilots.Tag = e.Column
            End If
            ' Call the sort method to manually sort.
            lvwPilots.Sort()
        End Sub

        Private Sub lvwPilots_ItemCheck(ByVal sender As Object, ByVal e As ItemCheckEventArgs) Handles lvwPilots.ItemCheck
            Dim pilotIndex As Integer = e.Index
            Dim newpilot As EveHQPilot
            newpilot = HQ.Settings.Pilots(lvwPilots.Items(pilotIndex).Text)
            If lvwPilots.Items(pilotIndex).Checked = False Then
                newpilot.Active = True
            Else
                newpilot.Active = False
            End If
        End Sub

#End Region

#Region "Training Queue Options"

        Private Sub chkDeleteCompletedSkills_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) Handles chkDeleteCompletedSkills.CheckedChanged
            HQ.Settings.DeleteSkills = chkDeleteCompletedSkills.Checked
        End Sub

        Private Sub chkStartWithPrimaryQueue_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) Handles chkStartWithPrimaryQueue.CheckedChanged
            HQ.Settings.StartWithPrimaryQueue = chkStartWithPrimaryQueue.Checked
        End Sub

        Private Sub nudEveQueueDisplayLength_ValueChanged(sender As Object, e As EventArgs) Handles nudEveQueueDisplayLength.ValueChanged
            HQ.Settings.EveQueueDisplayLength = nudEveQueueDisplayLength.Value.ToInt32
        End Sub

        Private Sub UpdateTrainingQueueOptions()
            ' Add the Queue columns
            Call RedrawQueueColumnList()
            chkDeleteCompletedSkills.Checked = HQ.Settings.DeleteSkills
            chkStartWithPrimaryQueue.Checked = HQ.Settings.StartWithPrimaryQueue
            nudEveQueueDisplayLength.Value = HQ.Settings.EveQueueDisplayLength
            Dim prereqColor As Color = Color.FromArgb(CInt(HQ.Settings.IsPreReqColor))
            pbIsPreReqColour.BackColor = prereqColor
            Dim hColor As Color = Color.FromArgb(CInt(HQ.Settings.HasPreReqColor))
            pbHasPreReqColour.BackColor = hColor
            Dim bColor As Color = Color.FromArgb(CInt(HQ.Settings.BothPreReqColor))
            pbBothPreReqColour.BackColor = bColor
            Dim cColor As Color = Color.FromArgb(CInt(HQ.Settings.DtClashColor))
            pbDowntimeClashColour.BackColor = cColor
            Dim rColor As Color = Color.FromArgb(CInt(HQ.Settings.ReadySkillColor))
            pbReadySkillColour.BackColor = rColor
            Dim pColor As Color = Color.FromArgb(CInt(HQ.Settings.PartialTrainColor))
            pbPartiallyTrainedColour.BackColor = pColor
        End Sub

        Private Sub pbIsPreReqColour_Click(ByVal sender As Object, ByVal e As EventArgs) Handles pbIsPreReqColour.Click
            Dim dlgResult As Integer
            With cd1
                .AllowFullOpen = True
                .AnyColor = True
                .FullOpen = True
                dlgResult = .ShowDialog()
            End With
            If dlgResult = DialogResult.Cancel Then
                Exit Sub
            Else
                pbIsPreReqColour.BackColor = cd1.Color
                HQ.Settings.IsPreReqColor = cd1.Color.ToArgb
                _redrawColumns = True
            End If
        End Sub

        Private Sub pbHasPreReqColour_Click(ByVal sender As Object, ByVal e As EventArgs) Handles pbHasPreReqColour.Click
            Dim dlgResult As Integer
            With cd1
                .AllowFullOpen = True
                .AnyColor = True
                .FullOpen = True
                dlgResult = .ShowDialog()
            End With
            If dlgResult = DialogResult.Cancel Then
                Exit Sub
            Else
                pbHasPreReqColour.BackColor = cd1.Color
                HQ.Settings.HasPreReqColor = cd1.Color.ToArgb
                _redrawColumns = True
            End If
        End Sub

        Private Sub pbBothPreReqColour_Click(ByVal sender As Object, ByVal e As EventArgs) Handles pbBothPreReqColour.Click
            Dim dlgResult As Integer
            With cd1
                .AllowFullOpen = True
                .AnyColor = True
                .FullOpen = True
                dlgResult = .ShowDialog()
            End With
            If dlgResult = DialogResult.Cancel Then
                Exit Sub
            Else
                pbBothPreReqColour.BackColor = cd1.Color
                HQ.Settings.BothPreReqColor = cd1.Color.ToArgb
                _redrawColumns = True
            End If
        End Sub

        Private Sub pbDowntimeClashColour_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles pbDowntimeClashColour.Click
            Dim dlgResult As Integer
            With cd1
                .AllowFullOpen = True
                .AnyColor = True
                .FullOpen = True
                dlgResult = .ShowDialog()
            End With
            If dlgResult = DialogResult.Cancel Then
                Exit Sub
            Else
                pbDowntimeClashColour.BackColor = cd1.Color
                HQ.Settings.DtClashColor = cd1.Color.ToArgb
                _redrawColumns = True
            End If
        End Sub

        Private Sub pbReadySkillColour_Click(ByVal sender As Object, ByVal e As EventArgs) Handles pbReadySkillColour.Click
            Dim dlgResult As Integer
            With cd1
                .AllowFullOpen = True
                .AnyColor = True
                .FullOpen = True
                dlgResult = .ShowDialog()
            End With
            If dlgResult = DialogResult.Cancel Then
                Exit Sub
            Else
                pbReadySkillColour.BackColor = cd1.Color
                HQ.Settings.ReadySkillColor = cd1.Color.ToArgb
                _redrawColumns = True
            End If
        End Sub

        Private Sub pbPartiallyTrainedColour_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles pbPartiallyTrainedColour.Click
            Dim dlgResult As Integer
            With cd1
                .AllowFullOpen = True
                .AnyColor = True
                .FullOpen = True
                dlgResult = .ShowDialog()
            End With
            If dlgResult = DialogResult.Cancel Then
                Exit Sub
            Else
                pbPartiallyTrainedColour.BackColor = cd1.Color
                HQ.Settings.PartialTrainColor = cd1.Color.ToArgb
                _redrawColumns = True
            End If
        End Sub

        Private Sub RedrawQueueColumnList()
            ' Setup the listview
            Dim newCol As ListViewItem
            lvwColumns.BeginUpdate()
            lvwColumns.Items.Clear()
            For Each slot As String In HQ.Settings.UserQueueColumns
                For Each stdSlot As ListViewItem In HQ.Settings.StandardQueueColumns
                    If slot.Substring(0, Len(slot) - 1) = stdSlot.Name Then
                        newCol = CType(stdSlot.Clone, ListViewItem)
                        newCol.Name = stdSlot.Name
                        If slot.EndsWith("0", StringComparison.Ordinal) = True Then
                            newCol.Checked = False
                        Else
                            newCol.Checked = True
                        End If
                        lvwColumns.Items.Add(newCol)
                    End If
                Next
            Next
            lvwColumns.EndUpdate()
        End Sub

        Private Sub btnMoveUp_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnMoveUp.Click
            ' Check we have something selected
            If lvwColumns.SelectedItems.Count = 0 Then
                MessageBox.Show("Please select an item before trying it move it!", "Item Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            ' Save the selected item
            ' Get the slot name of the item selected
            Dim slotName As String = lvwColumns.SelectedItems(0).Name
            Dim selName As String = slotName
            ' Find the index in the user column list
            Dim idx As Integer = HQ.Settings.UserQueueColumns.IndexOf(slotName & "0")
            If idx = -1 Then
                idx = HQ.Settings.UserQueueColumns.IndexOf(slotName & "1")
            End If
            ' Switch with the one above if the index is not zero
            If idx <> 0 Then
                slotName = CStr(HQ.Settings.UserQueueColumns(idx - 1))
                HQ.Settings.UserQueueColumns(idx - 1) = HQ.Settings.UserQueueColumns(idx)
                HQ.Settings.UserQueueColumns(idx) = slotName
                ' Redraw the list
                _redrawColumns = True
                Call RedrawQueueColumnList()
                _redrawColumns = False
                lvwColumns.Items(selName).Selected = True
                lvwColumns.Select()
            End If
        End Sub

        Private Sub btnMoveDown_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnMoveDown.Click
            ' Check we have something selected
            If lvwColumns.SelectedItems.Count = 0 Then
                MessageBox.Show("Please select an item before trying it move it!", "Item Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            ' Get the slot name of the item selected
            Dim slotName As String = lvwColumns.SelectedItems(0).Name
            Dim selName As String = slotName
            ' Find the index in the user column list
            Dim idx As Integer = HQ.Settings.UserQueueColumns.IndexOf(slotName & "0")
            If idx = -1 Then
                idx = HQ.Settings.UserQueueColumns.IndexOf(slotName & "1")
            End If
            ' Switch with the one above if the index is not the last
            If idx <> HQ.Settings.UserQueueColumns.Count - 1 Then
                slotName = CStr(HQ.Settings.UserQueueColumns(idx + 1))
                HQ.Settings.UserQueueColumns(idx + 1) = HQ.Settings.UserQueueColumns(idx)
                HQ.Settings.UserQueueColumns(idx) = slotName
                ' Redraw the list
                _redrawColumns = True
                Call RedrawQueueColumnList()
                _redrawColumns = False
                lvwColumns.Items(selName).Selected = True
                lvwColumns.Select()
            End If
        End Sub

        Private Sub lvwColumns_ItemChecked(ByVal sender As Object, ByVal e As ItemCheckedEventArgs) Handles lvwColumns.ItemChecked
            If _redrawColumns = False Then
                ' Get the slot name of the ticked item
                Dim slotName As String = e.Item.Name
                ' Find the index in the user column list
                Dim idx As Integer = HQ.Settings.UserQueueColumns.IndexOf(slotName & "0")
                If idx = -1 Then
                    idx = HQ.Settings.UserQueueColumns.IndexOf(slotName & "1")
                End If
                If e.Item.Checked = False Then
                    HQ.Settings.UserQueueColumns(idx) = slotName & "0"
                Else
                    HQ.Settings.UserQueueColumns(idx) = slotName & "1"
                End If
            End If
        End Sub

#End Region

#Region "Proxy Server Options"

        Private Sub UpdateProxyOptions()
            chkUseProxy.Checked = HQ.Settings.ProxyRequired
            txtProxyUsername.Text = HQ.Settings.ProxyUsername
            txtProxyPassword.Text = HQ.Settings.ProxyPassword
            txtProxyServer.Text = HQ.Settings.ProxyServer
            If HQ.Settings.ProxyUseDefault = True Then
                radUseDefaultCreds.Checked = True
            Else
                radUseSpecifiedCreds.Checked = True
            End If
            chkProxyUseBasic.Checked = HQ.Settings.ProxyUseBasic
        End Sub

        Private Sub chkUseProxy_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkUseProxy.CheckedChanged
            If chkUseProxy.Checked = True Then
                gbProxyServerInfo.Visible = True
                HQ.Settings.ProxyRequired = True
            Else
                gbProxyServerInfo.Visible = False
                HQ.Settings.ProxyRequired = False
            End If
        End Sub

        Private Sub chkProxyUseBasic_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkProxyUseBasic.CheckedChanged
            HQ.Settings.ProxyUseBasic = chkProxyUseBasic.Checked
        End Sub

        Private Sub radUseDefaultCreds_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles radUseDefaultCreds.CheckedChanged
            If radUseDefaultCreds.Checked = True Then
                lblProxyUsername.Enabled = False
                lblProxyPassword.Enabled = False
                txtProxyUsername.Enabled = False
                txtProxyPassword.Enabled = False
                chkProxyUseBasic.Enabled = False
                If _startup = False Then
                    HQ.Settings.ProxyUseDefault = True
                End If
            End If
        End Sub

        Private Sub radUseSpecifiedCreds_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles radUseSpecifiedCreds.CheckedChanged
            If radUseSpecifiedCreds.Checked = True Then
                lblProxyUsername.Enabled = True
                lblProxyPassword.Enabled = True
                txtProxyUsername.Enabled = True
                txtProxyPassword.Enabled = True
                chkProxyUseBasic.Enabled = True
                If _startup = False Then
                    HQ.Settings.ProxyUseDefault = False
                End If
            End If
        End Sub

        Private Sub txtProxyServer_TextChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles txtProxyServer.TextChanged
            HQ.Settings.ProxyServer = txtProxyServer.Text
        End Sub

        Private Sub txtProxyUsername_TextChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles txtProxyUsername.TextChanged
            HQ.Settings.ProxyUsername = txtProxyUsername.Text
        End Sub

        Private Sub txtProxyPassword_TextChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles txtProxyPassword.TextChanged
            HQ.Settings.ProxyPassword = txtProxyPassword.Text
        End Sub

#End Region

#Region "EveAPI & Server Settings"

        Private Sub UpdateEveServerSettings()
            chkEnableEveStatus.Checked = HQ.Settings.EnableEveStatus
            chkAutoAPI.Checked = HQ.Settings.AutoAPI
            chkAutoMailAPI.Checked = HQ.Settings.AutoMailAPI
            txtCCPAPIServer.Text = HQ.Settings.CcpapiServerAddress
            txtAPIRSServer.Text = HQ.Settings.ApirsAddress
            chkUseAPIRSServer.Checked = HQ.Settings.UseApirs
            chkUseCCPBackup.Checked = HQ.Settings.UseCcpapiBackup
            If HQ.Settings.UseApirs = False Then
                chkUseCCPBackup.Enabled = False
                txtAPIRSServer.Enabled = False
            Else
                chkUseCCPBackup.Enabled = True
                txtAPIRSServer.Enabled = True
            End If
            txtAPIFileExtension.Text = HQ.Settings.APIFileExtension
            trackServerOffset.Value = HQ.Settings.ServerOffset
            Dim offset As String = SkillFunctions.TimeToStringAll(HQ.Settings.ServerOffset)
            lblCurrentOffset.Text = "Current Offset: " & offset
        End Sub

        Private Sub trackServerOffset_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles trackServerOffset.ValueChanged
            HQ.Settings.ServerOffset = trackServerOffset.Value
            For Each newPilot As EveHQPilot In HQ.Settings.Pilots.Values
                newPilot.TrainingEndTime = newPilot.TrainingEndTimeActual.AddSeconds(HQ.Settings.ServerOffset)
                newPilot.TrainingStartTime = newPilot.TrainingStartTimeActual.AddSeconds(HQ.Settings.ServerOffset)
            Next
            Dim offset As String = SkillFunctions.TimeToStringAll(trackServerOffset.Value)
            lblCurrentOffset.Text = "Current Offset: " & offset
        End Sub

        Private Sub chkEnableEveStatus_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkEnableEveStatus.CheckedChanged
            If chkEnableEveStatus.Checked = True Then
                frmEveHQ.lblTQStatus.Text = "Tranquility Status: Unknown"
                HQ.Settings.EnableEveStatus = True
                frmEveHQ.tmrEve.Interval = 100
                frmEveHQ.tmrEve.Start()
            Else
                HQ.Settings.EnableEveStatus = False
                frmEveHQ.EveStatusIcon.Icon = My.Resources.EveHQ
                frmEveHQ.EveStatusIcon.Text = "EveHQ"
                frmEveHQ.tmrEve.Stop()
                frmEveHQ.lblTQStatus.Text = "Tranquility Status: Updates Disabled"
            End If
        End Sub

        Private Sub chkAutoAPI_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkAutoAPI.CheckedChanged
            If chkAutoAPI.Checked = True Then
                HQ.Settings.AutoAPI = True
                HQ.NextAutoAPITime = Now.AddMinutes(60)
            Else
                HQ.Settings.AutoAPI = False
            End If
        End Sub

        Private Sub chkAutoMailAPI_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkAutoMailAPI.CheckedChanged
            If chkAutoMailAPI.Checked = True Then
                HQ.Settings.AutoMailAPI = True
            Else
                HQ.Settings.AutoMailAPI = False
            End If
        End Sub

        Private Sub chkUseAPIRSServer_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkUseAPIRSServer.CheckedChanged
            If chkUseAPIRSServer.Checked = True Then
                HQ.Settings.UseApirs = True
                chkUseCCPBackup.Enabled = True
                txtAPIRSServer.Enabled = True
            Else
                HQ.Settings.UseApirs = False
                chkUseCCPBackup.Enabled = False
                txtAPIRSServer.Enabled = False
            End If
        End Sub

        Private Sub chkUseCCPBackup_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkUseCCPBackup.CheckedChanged
            If chkUseCCPBackup.Checked = True Then
                HQ.Settings.UseCcpapiBackup = True
            Else
                HQ.Settings.UseCcpapiBackup = False
            End If
        End Sub

        Private Sub txtCCPAPIServer_TextChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles txtCCPAPIServer.TextChanged
            HQ.Settings.CcpapiServerAddress = txtCCPAPIServer.Text
        End Sub

        Private Sub txtAPIRSServer_TextChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles txtAPIRSServer.TextChanged
            HQ.Settings.ApirsAddress = txtAPIRSServer.Text
        End Sub

        Private Sub txtAPIFileExtension_TextChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles txtAPIFileExtension.TextChanged
            HQ.Settings.APIFileExtension = txtAPIFileExtension.Text
        End Sub

#End Region

#Region "Notification Options"

        Public Sub UpdateNotificationOptions()
            chkShutdownNotify.Checked = HQ.Settings.ShutdownNotify
            chkNotifyToolTip.Checked = HQ.Settings.NotifyToolTip
            chkNotifyDialog.Checked = HQ.Settings.NotifyDialog
            chkNotifyNow.Checked = HQ.Settings.NotifyNow
            chkNotifyEarly.Checked = HQ.Settings.NotifyEarly
            chkNotifyEmail.Checked = HQ.Settings.NotifyEMail
            chkNotifyEveMail.Checked = HQ.Settings.NotifyEveMail
            chkNotifyNotification.Checked = HQ.Settings.NotifyEveNotification
            If HQ.Settings.NotifySound = True Then
                chkNotifySound.Checked = True
                btnSelectSoundFile.Enabled = True
                btnSoundTest.Enabled = True
            Else
                chkNotifySound.Checked = False
                btnSelectSoundFile.Enabled = False
                btnSoundTest.Enabled = False
            End If
            lblSoundFile.Text = HQ.Settings.NotifySoundFile
            If HQ.Settings.UseSmtpAuth = True Then
                chkSMTPAuthentication.Checked = True
                lblEmailUsername.Enabled = True
                lblEmailPassword.Enabled = True
                txtEmailUsername.Enabled = True
                txtEmailPassword.Enabled = True
            Else
                chkSMTPAuthentication.Checked = False
                lblEmailUsername.Enabled = False
                lblEmailPassword.Enabled = False
                txtEmailUsername.Enabled = False
                txtEmailPassword.Enabled = False
            End If
            chkUseSSL.Checked = HQ.Settings.UseSsl
            txtSMTPServer.Text = HQ.Settings.EMailServer
            txtSMTPPort.Text = CStr(HQ.Settings.EMailPort)
            txtEmailAddress.Text = HQ.Settings.EMailAddress
            txtEmailUsername.Text = HQ.Settings.EMailUsername
            txtEmailPassword.Text = HQ.Settings.EMailPassword
            txtSenderAddress.Text = HQ.Settings.EmailSenderAddress
            sldNotifyOffset.Value = HQ.Settings.NotifyOffset
            Dim offset As String = SkillFunctions.TimeToStringAll(sldNotifyOffset.Value)
            lblNotifyOffset.Text = "Early Notification Offset: " & offset
            nudShutdownNotifyPeriod.Value = HQ.Settings.ShutdownNotifyPeriod
            chkIgnoreLastMessage.Checked = HQ.Settings.IgnoreLastMessage
            chkNotifyAccountTime.Checked = HQ.Settings.NotifyAccountTime
            chkNotifyInsuffClone.Checked = HQ.Settings.NotifyInsuffClone
            nudAccountTimeLimit.Enabled = HQ.Settings.NotifyAccountTime
            nudAccountTimeLimit.Value = HQ.Settings.AccountTimeLimit
        End Sub

        Private Sub chkShutdownNotify_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkShutdownNotify.CheckedChanged
            If chkShutdownNotify.Checked = True Then
                HQ.Settings.ShutdownNotify = True
            Else
                HQ.Settings.ShutdownNotify = False
            End If
        End Sub

        Private Sub nudShutdownNotifyPeriod_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles nudShutdownNotifyPeriod.Click
            HQ.Settings.ShutdownNotifyPeriod = CInt(nudShutdownNotifyPeriod.Value)
        End Sub

        Private Sub nudShutdownNotifyPeriod_HandleDestroyed(ByVal sender As Object, ByVal e As EventArgs) _
            Handles nudShutdownNotifyPeriod.HandleDestroyed
            HQ.Settings.ShutdownNotifyPeriod = CInt(nudShutdownNotifyPeriod.Value)
        End Sub

        Private Sub nudShutdownNotifyPeriod_KeyUp(ByVal sender As Object, ByVal e As KeyEventArgs) _
            Handles nudShutdownNotifyPeriod.KeyUp
            HQ.Settings.ShutdownNotifyPeriod = CInt(nudShutdownNotifyPeriod.Value)
        End Sub

        Private Sub chkNotifyNow_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkNotifyNow.CheckedChanged
            HQ.Settings.NotifyNow = chkNotifyNow.Checked
        End Sub

        Private Sub chkNotifyEarly_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkNotifyEarly.CheckedChanged
            HQ.Settings.NotifyEarly = chkNotifyEarly.Checked
        End Sub

        Private Sub chkNotifyToolTip_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkNotifyToolTip.CheckedChanged
            HQ.Settings.NotifyToolTip = chkNotifyToolTip.Checked
        End Sub

        Private Sub chkNotifyDialog_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkNotifyDialog.CheckedChanged
            HQ.Settings.NotifyDialog = chkNotifyDialog.Checked
        End Sub

        Private Sub chkNotifyEmail_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkNotifyEmail.CheckedChanged
            HQ.Settings.NotifyEMail = chkNotifyEmail.Checked
        End Sub

        Private Sub chkNotifySound_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkNotifySound.CheckedChanged
            If chkNotifySound.Checked = True Then
                HQ.Settings.NotifySound = True
                btnSelectSoundFile.Enabled = True
                btnSoundTest.Enabled = True
            Else
                HQ.Settings.NotifySound = False
                btnSelectSoundFile.Enabled = False
                btnSoundTest.Enabled = False
            End If
        End Sub

        Private Sub chkNotifyEveMail_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkNotifyEveMail.CheckedChanged
            HQ.Settings.NotifyEveMail = chkNotifyEveMail.Checked
        End Sub

        Private Sub chkNotifyNotification_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkNotifyNotification.CheckedChanged
            HQ.Settings.NotifyEveNotification = chkNotifyNotification.Checked
        End Sub

        Private Sub sldNotifyOffset_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles sldNotifyOffset.ValueChanged
            HQ.Settings.NotifyOffset = sldNotifyOffset.Value
            Dim offset As String = SkillFunctions.TimeToStringAll(sldNotifyOffset.Value)
            lblNotifyOffset.Text = "Early Notification Offset: " & offset
        End Sub

        Private Sub chkSMTPAuthentication_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkSMTPAuthentication.CheckedChanged
            If chkSMTPAuthentication.Checked = True Then
                HQ.Settings.UseSmtpAuth = True
                lblEmailUsername.Enabled = True
                lblEmailPassword.Enabled = True
                txtEmailUsername.Enabled = True
                txtEmailPassword.Enabled = True
            Else
                HQ.Settings.UseSmtpAuth = False
                lblEmailUsername.Enabled = False
                lblEmailPassword.Enabled = False
                txtEmailUsername.Enabled = False
                txtEmailPassword.Enabled = False
            End If
        End Sub

        Private Sub chkUseSSL_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) Handles chkUseSSL.CheckedChanged
            HQ.Settings.UseSsl = chkUseSSL.Checked
        End Sub

        Private Sub txtSMTPServer_TextChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles txtSMTPServer.TextChanged
            HQ.Settings.EMailServer = txtSMTPServer.Text
        End Sub

        Private Sub txtEmailAddress_TextChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles txtEmailAddress.TextChanged
            HQ.Settings.EMailAddress = txtEmailAddress.Text
        End Sub

        Private Sub txtEmailUsername_TextChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles txtEmailUsername.TextChanged
            HQ.Settings.EMailUsername = txtEmailUsername.Text
        End Sub

        Private Sub txtEmailPassword_TextChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles txtEmailPassword.TextChanged
            HQ.Settings.EMailPassword = txtEmailPassword.Text
        End Sub

        Private Sub txtSenderAddress_TextChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles txtSenderAddress.TextChanged
            HQ.Settings.EmailSenderAddress = txtSenderAddress.Text
        End Sub

        Private Sub btnTestEmail_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnTestEmail.Click

            ' Only do this if at least one notification is enabled
            Dim notifyText As String
            For Each cPilot As EveHQPilot In HQ.Settings.Pilots.Values
                If cPilot.Active = True And cPilot.Training = True Then
                    notifyText = ""
                    SkillFunctions.CalcCurrentSkillTime(cPilot)
                    Dim strTime As String = SkillFunctions.TimeToString(cPilot.TrainingCurrentTime)
                    strTime = strTime.Replace("s", " seconds").Replace("m", " minutes")
                    notifyText &= cPilot.Name & " has approximately " & strTime & " before training of " &
                                  cPilot.TrainingSkillName & " to Level " & cPilot.TrainingSkillLevel & " completes." &
                                  ControlChars.CrLf
                    cPilot.TrainingNotifiedEarly = True
                    cPilot.TrainingNotifiedNow = False
                    ' Show the notifications
                    If notifyText <> "" Then
                        ' Expand the details with some additional information
                        If cPilot.QueuedSkills.Count > 0 Then
                            notifyText &= ControlChars.CrLf
                            notifyText &= "Next skill in Eve skill queue: " &
                                          SkillFunctions.SkillIDToName(cPilot.QueuedSkills.Values(0).SkillID) & " " &
                                          SkillFunctions.Roman(cPilot.QueuedSkills.Values(0).Level)
                            notifyText &= ControlChars.CrLf
                        Else
                            notifyText &= ControlChars.CrLf
                            notifyText &= "Next skill in Eve skill queue: No skill queued"
                            notifyText &= ControlChars.CrLf
                        End If
                        If cPilot.TrainingQueues.Count > 0 Then
                            notifyText &= ControlChars.CrLf
                            notifyText &= "EveHQ Skill Queue Info: " & ControlChars.CrLf
                            For Each sq As EveHQSkillQueue In cPilot.TrainingQueues.Values
                                Dim nq As ArrayList = SkillQueueFunctions.BuildQueue(cPilot, sq, False, True)
                                If sq.IncCurrentTraining = True Then
                                    If nq.Count > 1 Then
                                        For q As Integer = 1 To nq.Count - 1
                                            If CType(nq(q), SortedQueueItem).Done = False Then
                                                notifyText &= sq.Name & ": " & CType(nq(q), SortedQueueItem).Name
                                                notifyText &= " (" &
                                                              SkillFunctions.Roman(
                                                                  CInt(CType(nq(q), SortedQueueItem).FromLevel))
                                                notifyText &= " to " &
                                                              SkillFunctions.Roman(
                                                                  CInt(CType(nq(q), SortedQueueItem).FromLevel) + 1) & ")" &
                                                              ControlChars.CrLf
                                                Exit For
                                            End If
                                        Next
                                    End If
                                Else
                                    If nq.Count > 0 Then
                                        For q As Integer = 0 To nq.Count - 1
                                            If CType(nq(q), SortedQueueItem).Done = False Then
                                                notifyText &= sq.Name & ": " & CType(nq(q), SortedQueueItem).Name
                                                notifyText &= " (" &
                                                              SkillFunctions.Roman(
                                                                  CInt(CType(nq(q), SortedQueueItem).FromLevel))
                                                notifyText &= " to " &
                                                              SkillFunctions.Roman(
                                                                  CInt(CType(nq(q), SortedQueueItem).FromLevel) + 1) & ")" &
                                                              ControlChars.CrLf
                                                Exit For
                                            End If
                                        Next
                                    End If
                                End If
                            Next
                        End If
                        Dim eveHQMail As New SmtpClient
                        Try
                            eveHQMail.Host = HQ.Settings.EMailServer
                            eveHQMail.Port = HQ.Settings.EMailPort
                            eveHQMail.EnableSsl = HQ.Settings.UseSsl
                            If HQ.Settings.UseSmtpAuth = True Then
                                Dim newCredentials As New NetworkCredential
                                newCredentials.UserName = HQ.Settings.EMailUsername
                                newCredentials.Password = HQ.Settings.EMailPassword
                                eveHQMail.Credentials = newCredentials
                            End If
                            Dim recList As String =
                                    HQ.Settings.EMailAddress.Replace(ControlChars.CrLf, "").Replace(" ", "").Replace(
                                        ";", ",")
                            Dim eveHQMsg As New MailMessage(HQ.Settings.EmailSenderAddress, recList)
                            eveHQMsg.Subject = "Eve Training Notification: " & cPilot.Name & " (" & cPilot.TrainingSkillName &
                                               " " & SkillFunctions.Roman(cPilot.TrainingSkillLevel) & ")"
                            eveHQMsg.Body = notifyText
                            eveHQMail.Send(eveHQMsg)
                            MessageBox.Show("Test message sent successfully. Please check your inbox for confirmation.",
                                            "EveHQ Test Email Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                            ' Exit after the first mail
                            Exit Sub
                        Catch ex As Exception
                            MessageBox.Show(
                                "The mail sending process failed. Please check that the server, address, username and password are correct." &
                                ControlChars.CrLf & ControlChars.CrLf & "The error was: " & ex.Message,
                                "EveHQ Test Email Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                            ' Exit after the first mail
                            Exit Sub
                        End Try
                    End If
                End If
            Next
        End Sub

        Private Sub btnSelectSoundFile_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnSelectSoundFile.Click
            With ofd1
                .Title = "Please select a valid .wav file"
                .FileName = ""
                .InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                .Filter = "Wave files (*.wav)|*.wav|All files (*.*)|*.*"
                .FilterIndex = 1
                .RestoreDirectory = True
                If .ShowDialog() = DialogResult.OK Then
                    lblSoundFile.Text = .FileName
                End If
                HQ.Settings.NotifySoundFile = .FileName
            End With
        End Sub

        Private Sub btnSoundTest_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnSoundTest.Click
            Try
                My.Computer.Audio.Play(HQ.Settings.NotifySoundFile, AudioPlayMode.Background)
            Catch ex As Exception
                MessageBox.Show("Unable to play sound file." & ControlChars.CrLf & "Error: " & ex.Message,
                                "Error with Wave File", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub

        Private Sub txtSMTPPort_TextChanged(ByVal sender As Object, ByVal e As EventArgs) Handles txtSMTPPort.TextChanged
            If IsNumeric(txtSMTPPort.Text) = True Then
                HQ.Settings.EMailPort = CInt(txtSMTPPort.Text)
            Else
                HQ.Settings.EMailPort = 0
            End If
        End Sub

        Private Sub chkIgnoreLastMessage_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkIgnoreLastMessage.CheckedChanged
            HQ.Settings.IgnoreLastMessage = chkIgnoreLastMessage.Checked
        End Sub

        Private Sub chkNotifyInsuffClone_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) Handles chkNotifyInsuffClone.CheckedChanged
            HQ.Settings.NotifyInsuffClone = chkNotifyInsuffClone.Checked
        End Sub

        Private Sub chkNotifyAccountTime_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkNotifyAccountTime.CheckedChanged
            HQ.Settings.NotifyAccountTime = chkNotifyAccountTime.Checked
            nudAccountTimeLimit.Enabled = chkNotifyAccountTime.Checked
        End Sub

        Private Sub nudAccountTimeLimit_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles nudAccountTimeLimit.Click
            HQ.Settings.AccountTimeLimit = CInt(nudAccountTimeLimit.Value)
        End Sub

        Private Sub nudAccountTimeLimit_HandleDestroyed(ByVal sender As Object, ByVal e As EventArgs) _
            Handles nudAccountTimeLimit.HandleDestroyed
            HQ.Settings.AccountTimeLimit = CInt(nudAccountTimeLimit.Value)
        End Sub

        Private Sub nudAccountTimeLimit_KeyUp(ByVal sender As Object, ByVal e As KeyEventArgs) _
            Handles nudAccountTimeLimit.KeyUp
            HQ.Settings.AccountTimeLimit = CInt(nudAccountTimeLimit.Value)
        End Sub

#End Region

#Region "G15 Routines"

        Private Sub UpdateG15Options()
            If HQ.Settings.ActivateG15 = True Then
                chkActivateG15.Checked = True
            Else
                chkActivateG15.Checked = False
            End If
            If HQ.Settings.CycleG15Pilots = True Then
                chkCyclePilots.Checked = True
            Else
                chkCyclePilots.Checked = False
            End If
            nudCycleTime.Value = HQ.Settings.CycleG15Time
        End Sub

        Private Sub chkActivateG15_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkActivateG15.CheckedChanged
            If chkActivateG15.Checked = True Then
                If HQ.Settings.ActivateG15 = False Then
                    HQ.Settings.ActivateG15 = True
                    'Init the LCD
                    Try
                        G15Lcd.InitLcd()
                        ' Check if the LCD will cycle chars
                        If HQ.IsG15LCDActive = True And HQ.Settings.CycleG15Pilots = True Then
                            G15Lcd.TmrLcdChar.Interval = (1000 * HQ.Settings.CycleG15Time)
                            G15Lcd.TmrLcdChar.Enabled = True
                        End If
                    Catch ex As Exception
                        MessageBox.Show(
                            "Unable to start G15 Display. Please ensure you have the keyboard and drivers correctly installed. The error was: " &
                            ex.Message, "Error Starting G15", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        chkActivateG15.Checked = False
                    End Try
                End If
            Else
                If HQ.Settings.ActivateG15 = True Then
                    HQ.Settings.ActivateG15 = False
                    ' Close the LCD
                    Try
                        G15Lcd.CloseLcd()
                    Catch ex As Exception
                        MessageBox.Show("Unable to close G15 Display: " & ex.Message, "Error Closing G15",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information)
                    End Try
                End If
            End If
        End Sub

        Private Sub chkCyclePilots_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkCyclePilots.CheckedChanged
            If chkCyclePilots.Checked = True Then
                HQ.Settings.CycleG15Pilots = True
                G15Lcd.TmrLcdChar.Interval = (1000 * HQ.Settings.CycleG15Time)
                If HQ.Settings.ActivateG15 = True Then
                    G15Lcd.TmrLcdChar.Enabled = True
                End If
            Else
                HQ.Settings.CycleG15Pilots = False
                G15Lcd.TmrLcdChar.Enabled = False
            End If
        End Sub

        Private Sub nudCycleTime_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles nudCycleTime.ValueChanged
            HQ.Settings.CycleG15Time = CInt(nudCycleTime.Value)
            If HQ.Settings.CycleG15Time > 0 Then
                G15Lcd.TmrLcdChar.Interval = CInt((nudCycleTime.Value * 1000))
            Else
                G15Lcd.TmrLcdChar.Interval = CInt(1000)
            End If
        End Sub

#End Region

#Region "Eve Folder Options"

        Private Sub UpdateEveFolderOptions()
            Dim lblEveDir As Label
            Dim chkLua As CheckBox
            Dim gbFolderHost As GroupBox
            Dim txtFName As TextBox
            For folder As Integer = 1 To 4
                gbFolderHost = CType(gbEveFolders.Controls("gbLocation" & CStr(folder).Trim), GroupBox)
                lblEveDir = CType(gbFolderHost.Controls("lblEveDir" & CStr(folder).Trim), Label)
                chkLua = CType(gbFolderHost.Controls("chkLUA" & CStr(folder).Trim), CheckBox)
                txtFName = CType(gbFolderHost.Controls("txtFriendlyName" & CStr(folder).Trim), TextBox)
                lblEveDir.Text = HQ.Settings.EveFolder(folder)
                If My.Computer.FileSystem.DirectoryExists(HQ.Settings.EveFolder(folder)) = True Then
                    chkLua.Enabled = True
                    txtFName.Enabled = True
                    txtFName.Text = HQ.Settings.EveFolderLabel(folder)
                Else
                    chkLua.Enabled = False
                    txtFName.Enabled = False
                    txtFName.Text = ""
                End If
                If HQ.Settings.EveFolderLua(folder) = True Then
                    chkLua.Checked = True
                Else
                    chkLua.Checked = False
                    If chkLua.Enabled = True Then
                        'lblCacheSize.Text &= " (shared)"
                    End If
                End If

            Next
        End Sub

        Private Sub BtnEveDirClick(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnEveDir1.Click, btnEveDir2.Click, btnEveDir3.Click, btnEveDir4.Click
            Dim btnEveDir As Button
            btnEveDir = CType(sender, Button)
            Dim folder As Integer = CInt(btnEveDir.Name.Remove(0, 9))
            With fbd1
                .Description = "Please select the folder where the Eve executable is located..."
                .ShowNewFolderButton = False
                .RootFolder = Environment.SpecialFolder.Desktop
                .ShowDialog()
                Dim gbFolderHost As GroupBox = CType(gbEveFolders.Controls("gbLocation" & CStr(folder).Trim), GroupBox)
                Dim lblEveDir As Label = CType(gbFolderHost.Controls("lblEveDir" & CStr(folder).Trim), Label)
                If My.Computer.FileSystem.FileExists(Path.Combine(.SelectedPath, "eve.exe")) = False Then
                    MessageBox.Show("This folder does not contain the Eve.exe file.", "Directory Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error)
                Else
                    lblEveDir.Text = .SelectedPath
                    HQ.Settings.EveFolder(folder) = .SelectedPath
                    Dim chkLUA As CheckBox = CType(gbFolderHost.Controls("chkLUA" & CStr(folder).Trim), CheckBox)
                    chkLUA.Enabled = True
                    Dim txtFName As TextBox = CType(gbFolderHost.Controls("txtFriendlyName" & CStr(folder).Trim), TextBox)
                    txtFName.Enabled = True
                    If chkLUA.Checked = False Then
                        'lblCacheSize.Text &= " (shared)"
                    End If
                End If
            End With
        End Sub

        Private Sub BtnClearClick(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnClear1.Click, btnClear2.Click, btnClear3.Click, btnClear4.Click
            Dim btnEveDir As Button
            btnEveDir = CType(sender, Button)
            Dim folder As Integer = CInt(btnEveDir.Name.Remove(0, 8))
            Dim gbFolderHost As GroupBox = CType(gbEveFolders.Controls("gbLocation" & CStr(folder).Trim), GroupBox)
            Dim lblEveDir As Label = CType(gbFolderHost.Controls("lblEveDir" & CStr(folder).Trim), Label)
            lblEveDir.Text = ""
            Dim chkLUA As CheckBox = CType(gbFolderHost.Controls("chkLUA" & CStr(folder).Trim), CheckBox)
            chkLUA.Checked = False
            chkLUA.Enabled = False
            Dim lblCacheSize As Label = CType(gbFolderHost.Controls("lblCacheSize" & CStr(folder).Trim), Label)
            lblCacheSize.Text = ""
            HQ.Settings.EveFolder(folder) = ""
        End Sub

        Private Sub chkLUA_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkLUA1.CheckedChanged, chkLUA2.CheckedChanged, chkLUA3.CheckedChanged, chkLUA4.CheckedChanged
            Dim chkLUA As CheckBox
            chkLUA = CType(sender, CheckBox)
            Dim folder As Integer = CInt(chkLUA.Name.Remove(0, 6))
            Call CheckLUA(chkLUA, folder)
            Dim gbFolderHost As GroupBox = CType(gbEveFolders.Controls("gbLocation" & CStr(folder).Trim), GroupBox)
            Dim lblCacheSize As Label = CType(gbFolderHost.Controls("lblCacheSize" & CStr(folder).Trim), Label)
            If chkLUA.Checked = False Then
                lblCacheSize.Text &= " (shared)"
            End If
        End Sub

        Private Sub CheckLUA(ByVal chkLUA As CheckBox, ByVal folder As Integer)
            ' If selected, check the program files directory for the settings, otherwise check the user directory
            If chkLUA.Checked = True Then
                HQ.Settings.EveFolderLua(folder) = True
                ' Check program files
                If _startup = False Then
                    Dim cacheDir As String = Path.Combine(HQ.Settings.EveFolder(folder), "cache")
                    Dim prefsFile As String = Path.Combine(cacheDir, "prefs.ini")
                    If My.Computer.FileSystem.DirectoryExists(cacheDir) = True And
                        My.Computer.FileSystem.FileExists(prefsFile) = True Then
                        MessageBox.Show("Confirmed /LUA:off active on this folder.", "LUA Result", MessageBoxButtons.OK,
                                        MessageBoxIcon.Information)
                    Else
                        MessageBox.Show("Warning: /LUA:off does not appear active on this folder.", "LUA Result",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information)
                    End If
                End If
            Else
                HQ.Settings.EveFolderLua(folder) = False
                ' Check the application directory for the user
                If _startup = False Then
                    Dim eveAppFolder As String = HQ.Settings.EveFolder(folder)
                    eveAppFolder = eveAppFolder.Replace("\", "_").Replace(":", "").Replace(" ", "_").ToLower
                    eveAppFolder &= "_tranquility"
                    Dim eveDir As String =
                            Path.Combine(
                                Path.Combine(
                                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                                 "CCP"), "Eve"), eveAppFolder)
                    Dim cacheDir As String = Path.Combine(eveDir, "cache")
                    Dim settingsDir As String = Path.Combine(eveDir, "settings")
                    Dim prefsFile As String = Path.Combine(settingsDir, "prefs.ini")
                    If My.Computer.FileSystem.DirectoryExists(cacheDir) = True And
                        My.Computer.FileSystem.FileExists(prefsFile) = True Then
                        MessageBox.Show("Confirmed shared settings are active.", "LUA Result", MessageBoxButtons.OK,
                                        MessageBoxIcon.Information)
                    Else
                        MessageBox.Show("No shared settings found.", "LUA Result", MessageBoxButtons.OK,
                                        MessageBoxIcon.Information)
                    End If
                End If
            End If
        End Sub

        Private Sub txtFriendlyName_TextChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles txtFriendlyName1.TextChanged, txtFriendlyName2.TextChanged, txtFriendlyName3.TextChanged,
                    txtFriendlyName4.TextChanged
            Dim txtFName As TextBox = CType(sender, TextBox)
            Dim idx As Integer = CInt(txtFName.Name.Substring(txtFName.Name.Length - 1, 1))
            HQ.Settings.EveFolderLabel(idx) = txtFName.Text
            Dim gbFolderHost As GroupBox = CType(gbEveFolders.Controls("gbLocation" & CStr(idx).Trim), GroupBox)
            If HQ.Settings.EveFolderLabel(idx) <> "" Then
                gbFolderHost.Text = "Eve Location " & idx & " (" & HQ.Settings.EveFolderLabel(idx) & ")"
            Else
                gbFolderHost.Text = "Eve Location " & idx
            End If
        End Sub

        'Private Function CheckCacheSize(ByVal folder As Integer) As Long
        '    Dim cacheDir As String = ""
        '    If EveHQ.Core.HQ.Settings.EveFolderLUA(folder) = True Then
        '        cacheDir = EveHQ.Core.HQ.Settings.EveFolder(folder) & "\cache"
        '    Else
        '        cacheDir = (Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) & "\CCP\EVE\cache").Replace("\\", "\")
        '    End If
        '    If My.Computer.FileSystem.DirectoryExists(cacheDir) = True Then
        '        Dim dirInfo As DirectoryInfo = My.Computer.FileSystem.GetDirectoryInfo(cacheDir)
        '        Dim dirSize As Long = DirectorySize(dirInfo)
        '        Return dirSize
        '    Else
        '        Return 0
        '    End If
        'End Function

        'Public Function DirectorySize(ByVal dirInfo As System.IO.DirectoryInfo) As Long
        '    Dim total As Long = 0
        '    Dim file As System.IO.FileInfo
        '    For Each file In dirInfo.GetFiles()
        '        total += file.Length
        '    Next
        '    Dim dir As System.IO.DirectoryInfo
        '    For Each dir In dirInfo.GetDirectories()
        '        total += DirectorySize(dir)
        '    Next
        '    Return total
        'End Function

#End Region

#Region "Taskbar Icon Options"

        Private Sub UpdateTaskBarIconOptions()
            cboTaskbarIconMode.SelectedIndex = HQ.Settings.TaskbarIconMode
        End Sub

        Private Sub cboTaskbarIconMode_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles cboTaskbarIconMode.SelectedIndexChanged
            HQ.Settings.TaskbarIconMode = cboTaskbarIconMode.SelectedIndex
            Select Case HQ.Settings.TaskbarIconMode
                Case 0 ' Simple
                    Select Case HQ.myTQServer.Status
                        Case EveServer.ServerStatus.Down
                            frmEveHQ.EveStatusIcon.Text = HQ.myTQServer.StatusText
                        Case EveServer.ServerStatus.Starting
                            frmEveHQ.EveStatusIcon.Text = HQ.myTQServer.StatusText
                        Case EveServer.ServerStatus.Shutting
                            frmEveHQ.EveStatusIcon.Text = HQ.myTQServer.StatusText
                        Case EveServer.ServerStatus.Full
                            frmEveHQ.EveStatusIcon.Text = HQ.myTQServer.StatusText
                        Case EveServer.ServerStatus.ProxyDown
                            frmEveHQ.EveStatusIcon.Text = HQ.myTQServer.StatusText
                        Case EveServer.ServerStatus.Unknown
                            frmEveHQ.EveStatusIcon.Text = HQ.myTQServer.StatusText
                        Case EveServer.ServerStatus.Up
                            Dim msg As String = HQ.myTQServer.ServerName & ":" & vbCrLf
                            msg = msg & "Version: " & HQ.myTQServer.Version & vbCrLf
                            msg = msg & "Players: " & HQ.myTQServer.Players
                            If msg.Length > 50 Then
                                frmEveHQ.EveStatusIcon.Text = HQ.myTQServer.ServerName & ":" & vbCrLf &
                                                              "Server currently initialising"
                            Else
                                frmEveHQ.EveStatusIcon.Text = msg
                            End If
                    End Select
                Case 1 ' Enhanced

            End Select
        End Sub

#End Region

#Region "Dashboard Options"

        Private Sub UpdateDashboardOptions()
            ' Update the dashboard colours
            Call UpdateWidgetNames()
            Call UpdateWidgets()
            Call UpdateDBOptions()
        End Sub

        Private Sub UpdateWidgetNames()
            cboWidgets.BeginUpdate()
            cboWidgets.Items.Clear()
            For Each cWidget As String In HQ.Widgets.Keys
                cboWidgets.Items.Add(cWidget)
            Next
            cboWidgets.EndUpdate()
        End Sub

        Private Sub UpdateWidgets()
            lvWidgets.BeginUpdate()
            lvWidgets.Items.Clear()
            For Each config As SortedList(Of String, Object) In HQ.Settings.DashboardConfiguration
                If config.ContainsKey("ControlConfigInfo") = False Then
                    config.Add("ControlConfigInfo", "<Not Configurable>")
                End If
                Try
                    Dim newWidgetLvi As New ListViewItem
                    newWidgetLvi.Text = CStr(config("ControlName"))
                    newWidgetLvi.SubItems.Add(CStr(config("ControlConfigInfo")))
                    lvWidgets.Items.Add(newWidgetLvi)
                Catch e As Exception
                    ' presumably it's an old Widget, therefore discount it
                End Try
            Next
            lvWidgets.EndUpdate()
        End Sub

        Private Sub UpdateDBOptions()
            chkShowPriceTicker.Checked = HQ.Settings.DBTicker
            If HQ.Settings.DBTickerLocation = "" Then
                HQ.Settings.DBTickerLocation = "Bottom"
            End If
            cboTickerLocation.SelectedItem = HQ.Settings.DBTickerLocation
        End Sub

        Private Sub btnAddWidget_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnAddWidget.Click
            ' Check we have a selected widget type
            If cboWidgets.SelectedItem IsNot Nothing Then
                Dim widgetName As String = cboWidgets.SelectedItem.ToString
                ' Determine the type of control to add
                Dim className As String = HQ.Widgets(widgetName)
                Dim myType As Type = Assembly.GetExecutingAssembly.GetType(className)
                Dim newWidget As Object = Activator.CreateInstance(myType)
                Dim pi As PropertyInfo = myType.GetProperty("ControlConfigForm")
                Dim myConfigFormName As String = pi.GetValue(newWidget, Nothing).ToString
                If myConfigFormName <> "" Then
                    Dim configType As Type = Assembly.GetExecutingAssembly.GetType(myConfigFormName)
                    Using configForm As Form = CType(Activator.CreateInstance(configType), Form)
                        Dim fi As PropertyInfo = configForm.GetType().GetProperty("DBWidget")
                        fi.SetValue(configForm, newWidget, Nothing)
                        configForm.ShowDialog()
                        If configForm.DialogResult = DialogResult.OK Then
                            ' Save the Widget
                            Dim ci As PropertyInfo = myType.GetProperty("ControlConfiguration")
                            Dim myConfig As SortedList(Of String, Object) = CType(ci.GetValue(newWidget, Nothing), 
                                                                                  SortedList(Of String, Object))
                            HQ.Settings.DashboardConfiguration.Add(myConfig)
                            Call UpdateWidgets()
                            ' Update the dashboard
                            FrmDashboard.UpdateWidgets()
                        Else
                            ' Process Aborted
                            MessageBox.Show("Widget configuration aborted - information not saved.", "Addition Cancelled",
                                            MessageBoxButtons.OK, MessageBoxIcon.Information)
                        End If
                    End Using
                Else
                    ' Save the Widget
                    Dim ci As PropertyInfo = myType.GetProperty("ControlConfiguration")
                    Dim myConfig As SortedList(Of String, Object) = CType(ci.GetValue(newWidget, Nothing), 
                                                                          SortedList(Of String, Object))
                    HQ.Settings.DashboardConfiguration.Add(myConfig)
                    Call UpdateWidgets()
                    ' Update the dashboard
                    FrmDashboard.UpdateWidgets()
                End If
            Else
                ' Need a widget type before proceeding
                MessageBox.Show("Please select a Widget type before proceeding.", "Widget Type Required",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
        End Sub

        Private Sub btnRemoveWidget_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnRemoveWidget.Click
            ' Check for an item selection
            If lvWidgets.SelectedItems.Count > 0 Then
                Dim index As Integer = lvWidgets.SelectedItems(0).Index
                HQ.Settings.DashboardConfiguration.RemoveAt(index)
                lvWidgets.SelectedItems(0).Remove()
                ' Update the dashboard
                frmDashboard.UpdateWidgets()
            Else
                MessageBox.Show("Please select a Widget to remove before proceeding.", "Widget Selection Required",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
        End Sub

        Private Sub lvWidgets_DoubleClick(ByVal sender As Object, ByVal e As EventArgs) Handles lvWidgets.DoubleClick
            ' Edit a widget
            If lvWidgets.SelectedItems.Count > 0 Then
                Dim index As Integer = lvWidgets.SelectedItems(0).Index
                Dim widgetName As String = lvWidgets.SelectedItems(0).Text
                ' Determine the type of control to add
                Select Case widgetName
                    Case "Pilot Information"
                        Dim newWidget As New DBCPilotInfo
                        newWidget.ControlConfiguration = HQ.Settings.DashboardConfiguration.Item(index)
                        Using newWidgetConfig As New DBCPilotInfoConfig
                            newWidgetConfig.DBWidget = newWidget
                            newWidgetConfig.ShowDialog()
                            lvWidgets.Items(index).SubItems(1).Text = "Default Pilot: " & CStr(newWidget.ControlConfiguration("DefaultPilotName"))
                        End Using
                    Case "Skill Queue Information"
                        Dim newWidget As New DBCSkillQueueInfo
                        newWidget.ControlConfiguration = HQ.Settings.DashboardConfiguration.Item(index)
                        Using newWidgetConfig As New DBCSkillQueueInfoConfig
                            newWidgetConfig.DBWidget = newWidget
                            newWidgetConfig.ShowDialog()
                            If CBool(newWidget.ControlConfiguration("EveQueue")) = True Then
                                lvWidgets.Items(index).SubItems(1).Text = "Default Pilot: " &
                                                                          CStr(newWidget.ControlConfiguration("DefaultPilotName")) &
                                                                          ", Eve Queue"
                            Else
                                lvWidgets.Items(index).SubItems(1).Text = "Default Pilot: " &
                                                                          CStr(newWidget.ControlConfiguration("DefaultPilotName")) &
                                                                          ", EveHQ Queue (" &
                                                                          CStr(newWidget.ControlConfiguration("DefaultQueueName")) &
                                                                          ")"
                            End If
                        End Using
                End Select
                ' Update the dashboard
                frmDashboard.UpdateWidgets()
            End If
        End Sub

        Private Sub chkShowPriceTicker_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles chkShowPriceTicker.CheckedChanged
            HQ.Settings.DBTicker = chkShowPriceTicker.Checked
            frmDashboard.ticker1.Visible = HQ.Settings.DBTicker
        End Sub

        Private Sub cboTickerLocation_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles cboTickerLocation.SelectedIndexChanged
            HQ.Settings.DBTickerLocation = cboTickerLocation.SelectedItem.ToString
            If frmDashboard IsNot Nothing Then
                Try
                    Select Case HQ.Settings.DBTickerLocation
                        Case "Top"
                            frmDashboard.ticker1.Dock = DockStyle.Top
                        Case "Bottom"
                            frmDashboard.ticker1.Dock = DockStyle.Bottom
                    End Select

                Catch ex As Exception
                    ' just supressing any errors for now... 
                End Try
            End If
        End Sub

#End Region

#Region "Market Data Settings"

        Private Sub UpdateMarketSettings()
            UpdateMarketProviderList()
            UpdateDefaultMetrics()
            UpdateDataSourceList()
            UpdateItemOverrideControls()

        End Sub

        Private Sub UpdateMarketProviderList()

            ' Clear the list of providers before we add items
            _marketDataProvider.Items.Clear()
            _marketDataProvider.Items.Add(FuzzworkMarketStatDataProvider.Name)
            _marketDataProvider.Items.Add(EveCentralMarketDataProvider.Name)


            ' Set selected to the current setting.
            _marketDataProvider.SelectedItem = HQ.MarketStatDataProvider.ProviderName
        End Sub

        Private Sub UpdateDefaultMetrics()

            Select Case HQ.Settings.MarketDefaultTransactionType
                Case MarketTransactionKind.Buy
                    _defaultBuy.Checked = True
                Case Else
                    _defaultSell.Checked = True
            End Select


            Dim metric As String = HQ.Settings.MarketDefaultMetric.ToString()
            If _useMiniumPrice.Text = metric Then
                _useMiniumPrice.Checked = True
            End If

            If _useMaximumPrice.Text = metric Then
                _useMaximumPrice.Checked = True
            End If

            If _useAveragePrice.Text = metric Then
                _useAveragePrice.Checked = True
            End If

            If _useMedianPrice.Text = metric Then
                _useMedianPrice.Checked = True
            End If

            If _usePercentile.Text = metric Then
                _usePercentile.Checked = True
            End If
        End Sub

        Private Sub UpdateDataSourceList()

            If (HQ.MarketStatDataProvider Is Nothing) Then
                Return
            End If

            If StaticData.Regions Is Nothing Then
                Return
            End If

            If _regionList IsNot Nothing Then
                If _regionList.Items IsNot Nothing Then
                    _regionList.Items.Clear()
                End If

                If (HQ.MarketStatDataProvider.LimitedRegionSelection = False) Then
                    For Each regionName As String In StaticData.Regions.Values.OrderBy(Function(r As String) As String
                                                                                           Return r
                                                                                       End Function)
                        _regionList.Items.Add(regionName)
                    Next
                Else
                    For Each regionId As Integer In HQ.MarketStatDataProvider.SupportedRegions
                        If StaticData.Regions.ContainsKey(regionId) Then
                            _regionList.Items.Add(StaticData.Regions(regionId))
                        End If
                    Next
                End If
            End If


            If _systemList IsNot Nothing Then
                If _systemList.Items IsNot Nothing Then
                    _systemList.Items.Clear()
                End If

                If (HQ.MarketStatDataProvider.LimitedSystemSelection = False) Then
                    For Each system As SolarSystem In StaticData.SolarSystems.Values.OrderBy(Function(r As SolarSystem) As String
                                                                                                 Return r.Name
                                                                                             End Function)
                        _systemList.Items.Add(system.Name)
                    Next
                Else
                    For Each systemId As Integer In HQ.MarketStatDataProvider.SupportedSystems
                        If StaticData.SolarSystems.ContainsKey(systemId) Then
                            _systemList.Items.Add(StaticData.SolarSystems(systemId).Name)
                        End If
                    Next
                End If
            End If

            If (_systemList IsNot Nothing And _regionList IsNot Nothing) Then
                If HQ.Settings.MarketUseRegionMarket = True Then
                    _useRegionData.Checked = True
                    _useSystemPrice.Checked = False
                    _regionList.Enabled = True
                    _systemList.Enabled = False

                    'Get the selected regions from settings and find them in the collection
                    For Each regionID As Integer In HQ.Settings.MarketRegions
                        If StaticData.Regions.ContainsKey(regionID) Then
                            _regionList.SelectedItems.Add(StaticData.Regions(regionID))
                        End If
                    Next

                Else
                    _useSystemPrice.Checked = True
                    _useRegionData.Checked = False
                    _regionList.Enabled = False
                    _systemList.Enabled = True

                    'Find the select system based on id
                    Dim marketSystem As SolarSystem = (From system In StaticData.SolarSystems Where system.Value.Id = HQ.Settings.MarketSystem Select system.Value).FirstOrDefault()
                    If marketSystem IsNot Nothing Then
                        _systemList.SelectedItem = marketSystem.Name
                    End If
                End If
            End If
        End Sub

        Private Sub OnUseSystemPriceChecked(ByVal sender As Object, ByVal e As EventArgs) Handles _useSystemPrice.Click
            _regionList.Enabled = False
            _systemList.Enabled = True
        End Sub

        Private Sub OnUseRegionPriceChecked(ByVal sender As Object, ByVal e As EventArgs) Handles _useRegionData.Click
            _regionList.Enabled = True
            _systemList.Enabled = False
        End Sub

        Private Sub SaveMarketSettings()
            If _marketDataProvider.SelectedItem Is Nothing Then
                HQ.Settings.MarketDataProvider = MarketProvider.EveCentral.ToString()
            Else
                HQ.Settings.MarketDataProvider = _marketDataProvider.SelectedItem.ToString()
            End If
            HQ.MarketStatDataProvider = Nothing 'this will cause an update on next use.
            If _useMiniumPrice.Checked Then
                HQ.Settings.MarketDefaultMetric = MarketMetric.Minimum
            End If

            If _useMaximumPrice.Checked Then
                HQ.Settings.MarketDefaultMetric = MarketMetric.Maximum
            End If

            If _useAveragePrice.Checked Then
                HQ.Settings.MarketDefaultMetric = MarketMetric.Average
            End If

            If _useMedianPrice.Checked Then
                HQ.Settings.MarketDefaultMetric = MarketMetric.Median
            End If

            If _usePercentile.Checked Then
                HQ.Settings.MarketDefaultMetric = MarketMetric.Percentile
            End If

            If (_defaultBuy.Checked) Then
                HQ.Settings.MarketDefaultTransactionType = MarketTransactionKind.Buy
            Else
                HQ.Settings.MarketDefaultTransactionType = MarketTransactionKind.Sell

            End If

            If _useRegionData.Checked Then
                HQ.Settings.MarketUseRegionMarket = True
            Else
                HQ.Settings.MarketUseRegionMarket = False
            End If

            If _systemList.SelectedItem IsNot Nothing Then
                HQ.Settings.MarketSystem = (From s In StaticData.SolarSystems.Values Where s.Name = _systemList.SelectedItem.ToString Select s.Id).FirstOrDefault
            End If

            If _regionList.SelectedItems IsNot Nothing Then
                If _regionList.SelectedItems.Count > 0 Then
                    HQ.Settings.MarketRegions.Clear()
                    For Each regionName As String In _regionList.SelectedItems
                        For Each regionId As Integer In StaticData.Regions.Keys
                            If regionName = StaticData.Regions(regionId) Then
                                HQ.Settings.MarketRegions.Add(regionId)
                            End If
                        Next
                    Next
                End If
            End If
        End Sub


        Private Sub UpdateItemOverrideControls()
            If (_itemOverrideItemList.Items.Count = 0) Then
                _itemOverrideItemList.Items.AddRange((From item In StaticData.Types.Values Where item.MarketGroupId <> 0 Select item.Name).ToArray())
            End If

            'set radio buttons to defaults
            SetOverrideMetricRadioButton(HQ.Settings.MarketDefaultMetric)

            SetOverrideTransactionTypeRadioButton(HQ.Settings.MarketDefaultTransactionType)

            UpdateActiveOverrides()
        End Sub

        Private Sub SetOverrideMetricRadioButton(ByVal metric As MarketMetric)
            Select Case metric
                Case MarketMetric.Average
                    _itemOverrideAvgPrice.Checked = True
                Case MarketMetric.Maximum
                    _itemOverrideMaxPrice.Checked = True
                Case MarketMetric.Median
                    _itemOverrideMedianPrice.Checked = True
                Case MarketMetric.Minimum
                    _itemOverrideMinPrice.Checked = True
                Case MarketMetric.Percentile
                    _itemOverridePercentPrice.Checked = True
                Case MarketMetric.Default
                    _itemOverrideMinPrice.Checked = True

            End Select
        End Sub

        Private Sub SetOverrideTransactionTypeRadioButton(ByVal type As MarketTransactionKind)
            Select Case type
                Case MarketTransactionKind.All
                    _itemOverrideAllOrders.Checked = True
                Case MarketTransactionKind.Buy
                    _itemOverrideBuyOrders.Checked = True
                Case MarketTransactionKind.Sell
                    _itemOverrideSellOrders.Checked = True
                Case MarketTransactionKind.Default
                    _itemOverrideSellOrders.Checked = True
            End Select
        End Sub

        Private Sub OnOverrideItemListIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles _itemOverrideItemList.SelectedIndexChanged

            Const Item As String = ""
            Dim itemId As Integer
            Dim activeStat As MarketMetric = HQ.Settings.MarketDefaultMetric
            Dim activeTransactionType As MarketTransactionKind = HQ.Settings.MarketDefaultTransactionType
            If StaticData.TypeNames.ContainsKey(_itemOverrideItemList.SelectedItem.ToString) And Integer.TryParse(Item, itemId) Then

                ' see if the item is in the override list, otherwise set values to default
                Dim itemOverride As New ItemMarketOverride
                If HQ.Settings.MarketStatOverrides.Count > 0 And HQ.Settings.MarketStatOverrides.TryGetValue(itemId, itemOverride) Then
                    If (itemOverride IsNot Nothing) Then
                        activeStat = itemOverride.MarketStat
                        activeTransactionType = itemOverride.TransactionType
                    End If
                End If

            End If

            SetOverrideMetricRadioButton(activeStat)

            SetOverrideTransactionTypeRadioButton(activeTransactionType)

        End Sub

        Private Sub OnAddUpdateItemOverrideClick(ByVal sender As Object, ByVal e As EventArgs) Handles _itemOverrideAddOverride.Click
            Dim itemID As Integer

            If _itemOverrideItemList Is Nothing Then
                Return
            End If

            If _itemOverrideItemList.SelectedItem Is Nothing Then
                Return
            End If

            If (StaticData.TypeNames.ContainsKey(_itemOverrideItemList.SelectedItem.ToString) = False) Then
                Return 'not a real item
            End If

            itemID = StaticData.TypeNames(_itemOverrideItemList.SelectedItem.ToString)

            Dim override As New ItemMarketOverride()
            override.ItemId = itemID

            If (_itemOverrideAllOrders.Checked) Then
                override.TransactionType = MarketTransactionKind.All
            ElseIf _itemOverrideBuyOrders.Checked Then
                override.TransactionType = MarketTransactionKind.Buy
            Else
                override.TransactionType = MarketTransactionKind.Sell
            End If

            If (_itemOverrideAvgPrice.Checked) Then
                override.MarketStat = MarketMetric.Average
            ElseIf _itemOverrideMaxPrice.Checked Then
                override.MarketStat = MarketMetric.Maximum
            ElseIf _itemOverrideMedianPrice.Checked Then
                override.MarketStat = MarketMetric.Median
            ElseIf _itemOverridePercentPrice.Checked Then
                override.MarketStat = MarketMetric.Percentile
            Else
                override.MarketStat = MarketMetric.Minimum
            End If

            Dim existing As New ItemMarketOverride
            If HQ.Settings.MarketStatOverrides.TryGetValue(override.ItemId, existing) Then
                HQ.Settings.MarketStatOverrides(override.ItemId) = override
            Else
                HQ.Settings.MarketStatOverrides.Add(override.ItemId, override)
            End If

            UpdateActiveOverrides()
        End Sub

        Private Sub OnOverrideGridNodeClick(ByVal sender As Object, ByVal e As TreeNodeMouseEventArgs) Handles _itemOverridesActiveGrid.NodeClick

            If (e.Node IsNot Nothing) Then
                _itemOverrideItemList.SelectedItem = e.Node.Text
                Dim itemID As Integer = StaticData.TypeNames(e.Node.Text)
                Dim o As ItemMarketOverride = HQ.Settings.MarketStatOverrides(itemID)
                Select Case o.MarketStat
                    Case MarketMetric.Average
                        _itemOverrideAvgPrice.Select()
                    Case MarketMetric.Maximum
                        _itemOverrideMaxPrice.Select()
                    Case MarketMetric.Median
                        _itemOverrideMedianPrice.Select()
                    Case MarketMetric.Minimum
                        _itemOverrideMinPrice.Select()
                    Case MarketMetric.Percentile
                        _itemOverridePercentPrice.Select()
                End Select
                Select Case o.TransactionType
                    Case MarketTransactionKind.All
                        _itemOverrideAllOrders.Select()
                    Case MarketTransactionKind.Buy
                        _itemOverrideBuyOrders.Select()
                    Case MarketTransactionKind.Sell
                        _itemOverrideSellOrders.Select()
                End Select
            End If

        End Sub

        Private Sub UpdateActiveOverrides()
            _itemOverridesActiveGrid.Nodes.Clear()

            If HQ.Settings.MarketStatOverrides Is Nothing Then
                Return
            End If

            If StaticData.Types Is Nothing Then
                Return
            End If

            If (StaticData.Types.Count = 0) Then
                Return
            End If

            For Each override As ItemMarketOverride In HQ.Settings.MarketStatOverrides.Values
                Dim node As New Node()
                node.Text = StaticData.Types(CInt(override.ItemId.ToInvariantString)).Name
                node.Cells.Add(New Cell(override.ItemId.ToInvariantString))
                node.Cells.Add(New Cell(override.TransactionType.ToString))
                node.Cells.Add(New Cell(override.MarketStat.ToString))

                _itemOverridesActiveGrid.Nodes.Add(node)
            Next

        End Sub

        Private Sub OnRemoveOverrideClick(ByVal sender As Object, ByVal e As EventArgs) Handles _itemOverrideRemoveOverride.Click
            Dim itemID As Integer

            If _itemOverrideItemList Is Nothing Then
                Return
            End If

            If _itemOverrideItemList.SelectedItem Is Nothing Then
                Return
            End If

            If StaticData.TypeNames.ContainsKey(_itemOverrideItemList.SelectedItem.ToString) = False Then
                Return 'not a real item
            End If

            itemID = StaticData.TypeNames(_itemOverrideItemList.SelectedItem.ToString)

            HQ.Settings.MarketStatOverrides.Remove(itemID)
            UpdateActiveOverrides()

        End Sub

        Private Sub OnMarketProviderChanged(ByVal sender As Object, ByVal e As EventArgs) Handles _marketDataProvider.SelectedIndexChanged
            ChangeMarketProvider(_marketDataProvider.SelectedItem.ToString())
            UpdateDataSourceList()
        End Sub

        Private Sub ChangeMarketProvider(ByVal providerName As String)
            If providerName = EveCentralMarketDataProvider.Name Then
                HQ.MarketStatDataProvider = HQ.GetEveCentralMarketInstance
            Else
                HQ.MarketStatDataProvider = HQ.GetFuzzworkMarketStatDataProvider
            End If
        End Sub

#End Region
        
        Public Sub FinaliseAPIServerUpdate()
            btnGetData.Enabled = True
            Call UpdatePilots()
        End Sub

       
    End Class
End Namespace