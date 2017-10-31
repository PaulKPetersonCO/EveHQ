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

Imports System.ComponentModel
Imports System.Net.Cache
Imports System.Data
Imports EveHQ.EveData
Imports EveHQ.EveApi
Imports EveHQ.Common
Imports EveHQ.Controls
Imports EveHQ.Core.CoreReports
Imports DevComponents.AdvTree
Imports EveHQ.Core
Imports DevComponents.DotNetBar
Imports System.Xml
Imports System.Threading
Imports System.Globalization
Imports System.Net
Imports System.IO
Imports System.Net.Mail
Imports System.Reflection
Imports EveHQ.Core.Requisitions
Imports System.Text
Imports EveHQ.Common.Extensions
Imports System.Net.Http
Imports EveHQ.Core.ItemBrowser
Imports Microsoft.VisualBasic.FileIO
Imports System.Threading.Tasks

Namespace Forms

    Public Class FrmEveHQ

        Dim WithEvents _eveTqWorker As BackgroundWorker = New BackgroundWorker
        Dim WithEvents _skillWorker As BackgroundWorker = New BackgroundWorker
        Dim WithEvents _backupWorker As BackgroundWorker = New BackgroundWorker
        Dim WithEvents _eveHQBackupWorker As BackgroundWorker = New BackgroundWorker

        Private Delegate Sub QueryMyEveServerDelegate()

        Dim _eveHqmlf As New FrmMarketPrices
        Dim _appStartUp As Boolean = True
        Private _eveHQTrayForm As Form = Nothing
        Dim _iconShutdown As Boolean = False
        Dim _saveTrainingBarSize As Boolean = True

#Region "Icon Routines"

        Private Sub EveHQIcon1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles EveStatusIcon.Click
            If _
                Not _
                (TypeOf e Is MouseEventArgs AndAlso
                 (Not TypeOf e Is MouseEventArgs OrElse (TryCast(e, MouseEventArgs).Button = MouseButtons.Right))) Then
                Visible = True
                _saveTrainingBarSize = False
                Select Case HQ.Settings.MainFormWindowState
                    Case FormWindowState.Maximized
                        WindowState = FormWindowState.Maximized
                    Case FormWindowState.Normal
                        HQ.Settings.MainFormLocation = Location
                        HQ.Settings.MainFormSize = Size
                        WindowState = FormWindowState.Normal
                End Select
                _saveTrainingBarSize = True
                ' Set the training bar position, after checking for null!
                If HQ.Settings.TrainingBarDockPosition = eDockSide.None Then
                    HQ.Settings.TrainingBarDockPosition = eDockSide.Bottom
                End If
                If HQ.Settings.DisableTrainingBar = False Then
                    Bar1.DockSide = CType(HQ.Settings.TrainingBarDockPosition, eDockSide)
                    DockContainerItem1.Height = HQ.Settings.TrainingBarHeight
                    DockContainerItem1.Width = HQ.Settings.TrainingBarWidth
                End If
                ShowInTaskbar = True
                Activate()
                If _eveHQTrayForm IsNot Nothing Then
                    _eveHQTrayForm.Close()
                    _eveHQTrayForm = Nothing
                End If
            End If
        End Sub

        Private Sub EveHQIcon1_MouseHover(ByVal sender As Object, ByVal e As EventArgs) Handles EveStatusIcon.MouseHover
            ' Only display the pop up window if the context menu isn't showing
            If Not EveIconMenu.Visible And HQ.Settings.TaskbarIconMode = 1 Then
                _eveHQTrayForm = New FrmToolTrayIconPopup
                _eveHQTrayForm.Show()
            End If
        End Sub

        Private Sub EveHQIcon1_MouseLeave(ByVal sender As Object, ByVal e As EventArgs) Handles EveStatusIcon.MouseLeave
            ' Remove the popup if its showing
            If _eveHQTrayForm IsNot Nothing Then
                _eveHQTrayForm.Close()
                _eveHQTrayForm = Nothing
            End If
        End Sub

#End Region

#Region "Menu Click Routines"

        Private Sub ForceServerCheckToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles ForceServerCheckToolStripMenuItem.Click
            Call GetServerStatus()
        End Sub

        Private Sub HideWhenMinimisedToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles HideWhenMinimisedToolStripMenuItem.Click
            If HideWhenMinimisedToolStripMenuItem.Checked = True Then
                HideWhenMinimisedToolStripMenuItem.Checked = False
                FrmSettings.chkAutoHide.Checked = False
                HQ.Settings.AutoHide = False
            Else
                HideWhenMinimisedToolStripMenuItem.Checked = True
                FrmSettings.chkAutoHide.Checked = True
                HQ.Settings.AutoHide = True
            End If
        End Sub

        Private Sub ctxExit_Click(ByVal sender As Object, ByVal e As EventArgs) Handles ctxExit.Click
            _iconShutdown = True
            Close()
        End Sub

        Private Sub ctxAbout_Click(ByVal sender As Object, ByVal e As EventArgs) Handles ctxAbout.Click
            Using aboutForm As New FrmAbout
                aboutForm.ShowDialog()
            End Using
        End Sub

        Private Sub RestoreWindowToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles RestoreWindowToolStripMenuItem.Click
            ' Restores the window
            Show()
            Select Case HQ.Settings.MainFormWindowState
                Case FormWindowState.Maximized
                    WindowState = FormWindowState.Maximized
                Case FormWindowState.Normal
                    Location = HQ.Settings.MainFormLocation
                    Size = HQ.Settings.MainFormSize
                    WindowState = FormWindowState.Normal
            End Select
        End Sub

#End Region

#Region "Server Status Routines"

        Private Sub GetServerStatus()
            If _eveTqWorker.IsBusy Then
            Else
                _eveTqWorker.RunWorkerAsync()
            End If
        End Sub

        Private Sub tmrEve_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tmrEve.Tick
            tmrEve.Interval = 120000
            Call GetServerStatus()
        End Sub

        Private Sub UpdateEveTime()
            Dim now As DateTime = DateTime.Now.ToUniversalTime()
            Dim fi As DateTimeFormatInfo = CultureInfo.CurrentCulture.DateTimeFormat
            lblEveTime.Text = "EVE Time: " & now.ToString(fi.ShortDatePattern + " HH:mm")
        End Sub

        Private Sub eveTQWorker_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs) Handles _eveTqWorker.DoWork
            ' Defines what work the thread has to do
            Call HQ.MyTqServer.GetServerStatus()
        End Sub

        Private Sub eveTQWorker_RunWorkerCompleted(ByVal sender As Object, ByVal e As RunWorkerCompletedEventArgs) _
            Handles _eveTqWorker.RunWorkerCompleted
            ' Sub raised on the completion of a call to read the Eve TQ data

            ' Check if the server status has changed since the last result and notify user
            If HQ.MyTqServer.Status <> HQ.MyTqServer.LastStatus Then

                ' Depending on server status, set the notify icon text and the statusbar text
                Select Case HQ.MyTqServer.Status
                    Case EveServer.ServerStatus.Down
                        'EveStatusIcon.Text = EveHQ.Core.HQ.myTQServer.StatusText
                        lblTQStatus.Text = HQ.MyTqServer.ServerName & ": Unable to connect to server"
                        If EveStatusIcon IsNot Nothing And My.Resources.EveHQ_offline IsNot Nothing Then
                            EveStatusIcon.Icon = My.Resources.EveHQ_offline
                        End If
                    Case EveServer.ServerStatus.Starting
                        EveStatusIcon.Text = HQ.MyTqServer.StatusText
                        lblTQStatus.Text = HQ.MyTqServer.ServerName & ": " & HQ.MyTqServer.StatusText
                        If EveStatusIcon IsNot Nothing And My.Resources.EveHQ_starting IsNot Nothing Then
                            EveStatusIcon.Icon = My.Resources.EveHQ_starting
                        End If
                    Case EveServer.ServerStatus.Shutting
                        EveStatusIcon.Text = HQ.MyTqServer.StatusText
                        lblTQStatus.Text = HQ.MyTqServer.ServerName & ": " & HQ.MyTqServer.StatusText
                        If EveStatusIcon IsNot Nothing And My.Resources.EveHQ_starting IsNot Nothing Then
                            EveStatusIcon.Icon = My.Resources.EveHQ_starting
                        End If
                    Case EveServer.ServerStatus.Full
                        EveStatusIcon.Text = HQ.MyTqServer.StatusText
                        lblTQStatus.Text = HQ.MyTqServer.ServerName & ": " & HQ.MyTqServer.StatusText
                        If EveStatusIcon IsNot Nothing And My.Resources.EveHQ_online IsNot Nothing Then
                            EveStatusIcon.Icon = My.Resources.EveHQ_online
                        End If
                    Case EveServer.ServerStatus.ProxyDown
                        lblTQStatus.Text = HQ.MyTqServer.ServerName & ": " & HQ.MyTqServer.StatusText
                        EveStatusIcon.Text = HQ.MyTqServer.StatusText
                        If EveStatusIcon IsNot Nothing And My.Resources.EveHQ_offline IsNot Nothing Then
                            EveStatusIcon.Icon = My.Resources.EveHQ_offline
                        End If
                    Case EveServer.ServerStatus.Unknown
                        EveStatusIcon.Text = HQ.MyTqServer.StatusText
                        lblTQStatus.Text = HQ.MyTqServer.ServerName & ": Status unknown"
                        If EveStatusIcon IsNot Nothing And My.Resources.EveHQ IsNot Nothing Then
                            EveStatusIcon.Icon = My.Resources.EveHQ
                        End If
                    Case EveServer.ServerStatus.Up
                        lblTQStatus.Text = HQ.MyTqServer.ServerName & ": Online (" & HQ.MyTqServer.Players & " Players)"
                        If EveStatusIcon IsNot Nothing And My.Resources.EveHQ_online IsNot Nothing Then
                            EveStatusIcon.Icon = My.Resources.EveHQ_online
                        End If
                End Select

                If EveStatusIcon IsNot Nothing Then
                    EveStatusIcon.BalloonTipIcon = ToolTipIcon.Info
                    EveStatusIcon.BalloonTipTitle = HQ.MyTqServer.ServerName & " Status Notification"
                    Select Case HQ.MyTqServer.Status
                        Case EveServer.ServerStatus.Down
                            EveStatusIcon.BalloonTipText = HQ.MyTqServer.ServerName & " is Down"
                        Case EveServer.ServerStatus.Starting
                            EveStatusIcon.BalloonTipText = HQ.MyTqServer.ServerName & " is Starting Up"
                        Case EveServer.ServerStatus.Unknown
                            EveStatusIcon.BalloonTipText = HQ.MyTqServer.ServerName & " status is Unknown"
                        Case EveServer.ServerStatus.Up
                            EveStatusIcon.BalloonTipText = HQ.MyTqServer.ServerName & " is Up"
                    End Select
                    EveStatusIcon.ShowBalloonTip(3000)
                End If
            Else
                ' Report the players regardless
                If HQ.MyTqServer.Status = EveServer.ServerStatus.Up Then
                    lblTQStatus.Text = HQ.MyTqServer.ServerName & ": Online (" & HQ.MyTqServer.Players & " Players)"
                End If
            End If
            ' Update last status
            HQ.MyTqServer.LastStatus = HQ.MyTqServer.Status
        End Sub

#End Region

#Region "Form Opening & Closing & Resizing (+ Icon)"

        Private Sub frmEveHQ_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles Me.FormClosing

            ' Check we aren't updating
            If HQ.EveHQIsUpdating = True Then
                MessageBox.Show(
                    "You can't exit EveHQ while an update is in progress. Please wait until the update has completed and try again.",
                    "Update in Progress", MessageBoxButtons.OK, MessageBoxIcon.Information)
                e.Cancel = True
                Exit Sub
            End If

            Try

                HQ.WriteLogEvent("Shutdown: EveHQ Form Closure request made")

                ' Are we shutting down to restore settings?
                If HQ.RestoredSettings = False Then
                    ' Check if we should minimise rather than exit?
                    If e.CloseReason <> CloseReason.TaskManagerClosing And e.CloseReason <> CloseReason.WindowsShutDown Then
                        If HQ.Settings.MinimiseExit = True And _iconShutdown = False Then
                            WindowState = FormWindowState.Minimized
                            HQ.WriteLogEvent("Shutdown: EveHQ Form Closure aborted due to 'Minimise on Exit' setting")
                            e.Cancel = True
                            Exit Sub
                        Else
                            ' Check if there are updates available
                            If HQ.AppUpdateAvailable = True Then
                                Const Msg As String = "There are pending updates available - these will be installed now."
                                MessageBox.Show(Msg, "Update Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                Call UpdateNow()
                            Else
                                HQ.WriteLogEvent("Shutdown: Calling main shutdown routine")
                                Call ShutdownRoutine()
                            End If
                        End If
                    Else
                        ' Check if there are updates available
                        If HQ.AppUpdateAvailable = True Then
                            Const Msg As String = "There are pending updates available - these will be installed now."
                            MessageBox.Show(Msg, "Update Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                            Call UpdateNow()
                        Else
                            HQ.WriteLogEvent("Shutdown: Calling main shutdown routine")
                            Call ShutdownRoutine()
                        End If
                    End If
                Else
                    ' Close and flush the timer file
                    Trace.Flush()
                End If
            Catch ex As Exception
                MessageBox.Show("An error occured while closing EveHQ: " & ex.Message & "- " & ex.StackTrace,
                                "Error Closing EveHQ", MessageBoxButtons.OK, MessageBoxIcon.Error)

            Finally
                Trace.Flush()
            End Try
        End Sub

        Private Sub frmEveHQ_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load

            Hide()

            ' error handlers for any unhandled error forground or background
            AddHandler Application.ThreadException, AddressOf CatchUiThreadException
            AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf CatchAppDomainUnhandledException

            ' Set global AdvTree settings
            AdvTreeSettings.SelectedScrollIntoViewHorizontal = False

            ' Disable resizing of bar
            _appStartUp = True

            EveStatusIcon.Visible = True

            ' Set Theme Stuff
            UpdateTheme(HQ.Settings.ThemeStyle, HQ.Settings.ThemeTint)
            Dim themeBtn As ButtonItem = CType(btnTheme.SubItems(1), ButtonItem)
            If btnTheme.SubItems.Contains("btn" & HQ.Settings.ThemeStyle.ToString) Then
                themeBtn = CType(btnTheme.SubItems("btn" & HQ.Settings.ThemeStyle.ToString), ButtonItem)
            ElseIf StyleManager.IsMetro(HQ.Settings.ThemeStyle) Then
                themeBtn = CType(btnTheme.SubItems("btnMetro"), ButtonItem)
            End If
            themeBtn.Checked = True

            ' Add the pilot refresh handler
            AddHandler PilotParseFunctions.RefreshPilots, AddressOf RemoteRefreshPilots
            AddHandler G15Lcd.UpdateAPI, AddressOf RemoteUpdate
            AddHandler HQ.ShutDownEveHQ, AddressOf ShutdownRoutine
            AddHandler EveMailEvents.MailUpdateNumbers, AddressOf UpdateEveMailButton

            ' Check if "Hide When Minimised" is active
            HideWhenMinimisedToolStripMenuItem.Checked = HQ.Settings.AutoHide

            'Setup the Modules menu if applicable
            Call SetupModuleMenu()

            ' Update the QAT config if applicable
            If HQ.Settings.QatLayout <> "" Then
                RibbonControl1.QatLayout = HQ.Settings.QatLayout
            End If

            ' Set the tab position
            Select Case HQ.Settings.MdiTabPosition
                Case "Top"
                    tabEveHQMDI.Dock = DockStyle.Top
                    tabEveHQMDI.TabAlignment = eTabStripAlignment.Top
                Case "Bottom"
                    tabEveHQMDI.Dock = DockStyle.Bottom
                    tabEveHQMDI.TabAlignment = eTabStripAlignment.Bottom
            End Select

            ' Check for ribbon status
            RibbonControl1.Expanded = Not HQ.Settings.RibbonMinimised

            ' Close the splash screen
            FrmSplash.Close()

            ' Check if the form needs to be minimised on startup
            If HQ.Settings.AutoMinimise = True Then
                WindowState = FormWindowState.Minimized
                'Me.Show()
            Else
                Select Case HQ.Settings.MainFormWindowState
                    Case FormWindowState.Normal
                        Show()
                        Location = HQ.Settings.MainFormLocation
                        Size = HQ.Settings.MainFormSize
                        WindowState = FormWindowState.Normal
                    Case FormWindowState.Maximized
                        WindowState = FormWindowState.Maximized
                        Show()
                End Select
            End If

            If RibbonControl1.QatPositionedBelowRibbon = True Then
                RibbonControl1.QatPositionedBelowRibbon = False
                RibbonControl1.QatPositionedBelowRibbon = True
            End If

            ' Start the timers
            If HQ.Settings.EnableEveStatus = True Then
                tmrEve.Start()
                lblTQStatus.Text = "Tranquility Status: Not Updated"
            Else
                lblTQStatus.Text = "Tranquility Status: Updates Disabled"
            End If
            tmrSkillUpdate.Start()
            tmrModules.Start()
            If HQ.Settings.EnableAutomaticSave = True Then
                tmrSave.Interval = HQ.Settings.AutomaticSaveTime * 60000
                tmrSave.Start()
            End If

            Call HQ.ReduceMemory()
            tmrMemory.Start()

            ' Update the EveMailNotice button
            Call UpdateEveMailButton()

            ' Update the pilots in the report
            Call UpdateReportPilots()

            ' Set the training bar position, after checking for null!
            If HQ.Settings.DisableTrainingBar = False Then
                If HQ.Settings.TrainingBarDockPosition = eDockSide.None Then
                    HQ.Settings.TrainingBarDockPosition = eDockSide.Bottom
                End If
                Bar1.DockSide = CType(HQ.Settings.TrainingBarDockPosition, eDockSide)
                DockContainerItem1.Height = HQ.Settings.TrainingBarHeight
                DockContainerItem1.Width = HQ.Settings.TrainingBarWidth
            Else
                Bar1.Visible = False
            End If

            _appStartUp = False

            ' Display server message if applicable
            Try
                GetServerMessage()
            Catch ex As Exception
                ' just supress errors from getting the server message.
            End Try
            If HQ.EveHQServerMessage IsNot Nothing Then
                If _
                    HQ.EveHQServerMessage.MessageDate > HQ.Settings.LastMessageDate Or
                    (HQ.EveHQServerMessage.MessageDate = HQ.Settings.LastMessageDate And
                     HQ.Settings.IgnoreLastMessage = False) Then
                    Using newMsg As New FrmEveHQMessage
                        newMsg.lblMessage.Text = HQ.EveHQServerMessage.Message
                        newMsg.lblTitle.Text = HQ.EveHQServerMessage.MessageTitle
                        HQ.Settings.LastMessageDate = HQ.EveHQServerMessage.MessageDate
                        If HQ.EveHQServerMessage.AllowIgnore = False Then
                            newMsg.chkIgnore.Checked = False
                            newMsg.chkIgnore.Enabled = False
                        Else
                            newMsg.chkIgnore.Checked = False
                            newMsg.chkIgnore.Enabled = True
                        End If
                        newMsg.ShowDialog()
                    End Using
                End If
            End If

            ' Check for existing pilots and accounts
            If HQ.Settings.Accounts.Count = 0 And HQ.Settings.Pilots.Count = 0 Then
                Dim wMsg As String = "EveHQ has detected that you have not yet setup any API accounts." & ControlChars.CrLf &
                                     ControlChars.CrLf
                wMsg &= "Would you like to do this now?"
                Dim reply As Integer = MessageBox.Show(wMsg, "Welcome to EveHQ!", MessageBoxButtons.YesNo,
                                                       MessageBoxIcon.Question)
                If reply = DialogResult.No Then
                    wMsg =
                        "You can add API accounts using the 'Manage API Account' button on the ribbon bar or by going into Settings and choosing the Eve Accounts section."
                    MessageBox.Show(wMsg, "API Account Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Else
                    Using eveHQSettings As New FrmSettings
                        eveHQSettings.Tag = "nodeEveAccounts"
                        eveHQSettings.ShowDialog()
                    End Using
                End If
            End If

            ' Start the update check on a new thread
            If HQ.Settings.DisableAutoWebConnections = False Then
                ThreadPool.QueueUserWorkItem(AddressOf CheckForUpdates)
            End If
        End Sub

        Private Sub UpdateTheme(theme As eStyle, tint As Color)
            StyleManager.ChangeStyle(theme, tint)
            If StyleManager.IsMetro(StyleManager.Style) Then
                StyleManager.MetroColorGeneratorParameters = New DevComponents.DotNetBar.Metro.ColorTables.MetroColorGeneratorParameters(HQ.Settings.ThemeCanvas, tint)
                btnCanvasColor.Enabled = True
            Else
                StyleManager.ColorTint = tint
                btnCanvasColor.Enabled = False
            End If
        End Sub

        Private Sub UpdateTint(tint As Color)
            If StyleManager.IsMetro(StyleManager.Style) Then
                StyleManager.MetroColorGeneratorParameters = New DevComponents.DotNetBar.Metro.ColorTables.MetroColorGeneratorParameters(HQ.Settings.ThemeCanvas, tint)
            Else
                StyleManager.ColorTint = tint
            End If
        End Sub

        Private Sub UpdateCanvas(tint As Color)
            If StyleManager.IsMetro(StyleManager.Style) Then
                StyleManager.MetroColorGeneratorParameters = New DevComponents.DotNetBar.Metro.ColorTables.MetroColorGeneratorParameters(tint, HQ.Settings.ThemeTint)
            End If
        End Sub

        Private Sub frmEveHQ_Shown(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Shown
            ' Determine which view to display
            For idx As Integer = 0 To 5
                If (HQ.Settings.StartupForms And CInt(2 ^ idx)) = CInt(2 ^ idx) Then
                    Select Case idx
                        Case 0
                            If HQ.Settings.StartupPilot <> "" Then
                                ' Open the pilot info form
                                Call OpenPilotInfoForm()
                            End If
                        Case 1
                            If HQ.Settings.StartupPilot <> "" Then
                                ' Open the skill training form
                                Call OpenSkillTrainingForm()
                            End If
                        Case 2
                            ' Open the market prices
                            Call OpenMarketPricesForm()
                        Case 3
                            ' Open the dashboard
                            Call OpenDashboard()
                        Case 4
                            ' Open the requisitions form
                            Call OpenRequisitions()
                        Case 5
                            ' Show the pilot summary report form!
                            Dim newReport As New FrmReportViewer
                            Call Reports.GenerateCharSummary()
                            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "PilotSummary.html"))
                            Call DisplayReport(newReport, "Pilot Summary")
                    End Select
                End If
            Next
        End Sub

        Private Sub frmEveHQ_Resize(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Resize
            If HideWhenMinimisedToolStripMenuItem.Checked = True Then
                If WindowState = FormWindowState.Minimized Then
                    Hide()
                Else
                    If ShowInTaskbar = False Then
                        ShowInTaskbar = True
                    End If
                End If
            End If

            ' Save the window position if possible
            If HQ.Settings IsNot Nothing Then
                Select Case WindowState
                    Case FormWindowState.Normal
                        HQ.Settings.MainFormLocation = Location
                        HQ.Settings.MainFormSize = Size
                        HQ.Settings.MainFormWindowState = WindowState
                    Case FormWindowState.Maximized
                        HQ.Settings.MainFormWindowState = WindowState
                End Select
            End If

        End Sub

        Private Sub frmEveHQ_Move(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Move
            ' Save the window position if possible
            If HQ.Settings IsNot Nothing Then
                Select Case WindowState
                    Case FormWindowState.Normal
                        HQ.Settings.MainFormLocation = Location
                        HQ.Settings.MainFormSize = Size
                        HQ.Settings.MainFormWindowState = WindowState
                    Case FormWindowState.Maximized
                        HQ.Settings.MainFormWindowState = WindowState
                End Select
            End If
        End Sub

        Public Sub ShutdownRoutine()

            Try
                ' Check we aren't updating
                If HQ.EveHQIsUpdating = True Then
                    MessageBox.Show(
                        "You can't exit EveHQ while an update is in progress. Please wait until the update has completed and try again.",
                        "Update in Progress", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Exit Sub
                End If

                ' Disable timers
                HQ.WriteLogEvent("Shutdown: Disabling timers...")
                tmrMemory.Stop()
                tmrEve.Stop()
                HQ.WriteLogEvent("Shutdown: Disabled TQ Status timer")
                tmrSkillUpdate.Stop()
                HQ.WriteLogEvent("Shutdown: Disabled Skill Update timer")
                tmrSave.Stop()
                HQ.WriteLogEvent("Shutdown: Disabled Automatic Save timer")

                ' Check if Shutdown Notification is active (only if not shutting down on request on the updater
                If HQ.Settings.ShutdownNotify = True And HQ.UpdateShutDownRequest = False Then
                    HQ.WriteLogEvent("Shutdown: Processing shutdown notifications")
                    Dim accounts As New ArrayList
                    Dim strNotify As String = ""
                    Dim strCharNotify As String = ""
                    For Each cPilot As EveHQPilot In HQ.Settings.Pilots.Values
                        If cPilot.Training = True Then
                            If cPilot.Active = True Then
                                Dim timeLimit As Date = Now.AddSeconds(HQ.Settings.ShutdownNotifyPeriod * 3600)
                                If Core.SkillFunctions.ConvertEveTimeToLocal(cPilot.TrainingEndTime) < timeLimit Then
                                    If cPilot.QueuedSkillTime > 0 Then
                                        If Core.SkillFunctions.ConvertEveTimeToLocal(cPilot.TrainingEndTime.AddSeconds(cPilot.QueuedSkillTime)) < timeLimit Then
                                            strCharNotify &= cPilot.Name & " - " & cPilot.TrainingSkillName &
                                                             " (Skill Queue ends in " &
                                                             SkillFunctions.TimeToString(cPilot.QueuedSkillTime) & ")" &
                                                             ControlChars.CrLf
                                        End If
                                    Else
                                        If cPilot.TrainingCurrentTime > 0 Then
                                            strCharNotify &= cPilot.Name & " - " & cPilot.TrainingSkillName &
                                                             " (Training ends in " &
                                                             SkillFunctions.TimeToString(cPilot.TrainingCurrentTime) & ")" &
                                                             ControlChars.CrLf
                                        Else
                                            strCharNotify &= cPilot.Name & " - " & cPilot.TrainingSkillName &
                                                             " (Training already complete)" & ControlChars.CrLf
                                        End If
                                    End If
                                End If
                            End If
                            accounts.Add(cPilot.Account)
                        End If
                    Next
                    If strCharNotify <> "" Then
                        strCharNotify = "The following pilots have skills due to end within " &
                                        HQ.Settings.ShutdownNotifyPeriod & " hours:" & ControlChars.CrLf &
                                        ControlChars.CrLf & strCharNotify
                        strNotify &= strCharNotify
                    End If
                    ' Check each account to see if something is training.
                    Dim strAccountNotify As String = ""
                    For Each cAccount As EveHQAccount In HQ.Settings.Accounts.Values
                        If cAccount.APIKeyType <> APIKeyTypes.Corporation And cAccount.APIAccountStatus <> APIAccountStatuses.ManualDisabled Then
                            If accounts.Contains(cAccount.UserID) = False Then
                                If cAccount.FriendlyName <> "" Then
                                    strAccountNotify &= cAccount.FriendlyName & " (UserID: " & cAccount.UserID & ")" &
                                                        ControlChars.CrLf
                                Else
                                    strAccountNotify &= "UserID: " & cAccount.UserID & ControlChars.CrLf
                                End If
                            End If
                        End If
                    Next
                    If strAccountNotify <> "" Then
                        strAccountNotify = ControlChars.CrLf &
                                           "The following accounts do not appear to have any skill training:" &
                                           ControlChars.CrLf & ControlChars.CrLf & strAccountNotify
                        strNotify &= strAccountNotify
                    End If
                    If strNotify <> "" Then
                        MessageBox.Show(strNotify, "EveHQ Skill Notification", MessageBoxButtons.OK,
                                        MessageBoxIcon.Information)
                    End If
                End If

                ' Close all the open tabs first
                Dim mainTab As TabStrip = CType(HQ.MainForm.Controls("tabEveHQMDI"), TabStrip)
                If mainTab.Tabs.Count > 0 Then
                    For tab As Integer = mainTab.Tabs.Count - 1 To 0 Step -1
                        HQ.WriteLogEvent("Shutdown: Closing tab: " & mainTab.Tabs(tab).Text)
                        CType(mainTab.Tabs(tab).AttachedControl, Form).Close()
                    Next
                End If

                ' Save the QAT config if applicable
                HQ.WriteLogEvent("Shutdown: Storing ribbon QAT layout")
                HQ.Settings.QatLayout = RibbonControl1.QatLayout

                ' Check for backup warning expiry
                If HQ.UpdateShutDownRequest = True Then
                    If HQ.Settings.EveHqBackupMode = 1 Then
                        Dim backupDate As Date =
                                HQ.Settings.EveHqBackupLast.AddDays(HQ.Settings.EveHqBackupWarnFreq)
                        If backupDate < Now Then
                            Dim timeElapsed As TimeSpan = Now - HQ.Settings.EveHqBackupLast
                            Dim msg As String = "You haven't backed up your EveHQ Settings for " & timeElapsed.Days &
                                                " days. Would you like to do this now?"
                            Dim reply As Integer = MessageBox.Show(msg, "Backup EveHQ Settings?", MessageBoxButtons.YesNo,
                                                                   MessageBoxIcon.Question)
                            If reply = DialogResult.Yes Then
                                HQ.WriteLogEvent("Shutdown: Request to backup EveHQ Settings before update")
                                Call EveHQBackup.BackupEveHQSettings()
                            End If
                        End If
                    End If
                    HQ.WriteLogEvent("Shutdown: Request to save EveHQ Settings before update")
                    Call HQ.Settings.Save()
                Else
                    If HQ.Settings.EveHqBackupMode = 1 Then
                        HQ.WriteLogEvent("Shutdown: Checking EveHQ backup status before exit")
                        Dim backupDate As Date =
                                HQ.Settings.EveHqBackupLast.AddDays(HQ.Settings.EveHqBackupWarnFreq)
                        If backupDate < Now Then
                            Dim timeElapsed As TimeSpan = Now - HQ.Settings.EveHqBackupLast
                            Dim msg As String = "You haven't backed up your EveHQ Settings for " & timeElapsed.Days &
                                                " days. Would you like to do this now?"
                            Dim reply As Integer = MessageBox.Show(msg, "Backup EveHQ Settings?", MessageBoxButtons.YesNo,
                                                                   MessageBoxIcon.Question)
                            If reply = DialogResult.Yes Then
                                HQ.WriteLogEvent("Shutdown: User accepted request to backup EveHQ Settings before exit")
                                Call EveHQBackup.BackupEveHQSettings()
                            Else
                                HQ.WriteLogEvent("Shutdown: User rejected request to backup EveHQ Settings before exit")
                            End If
                        Else
                            HQ.WriteLogEvent("Shutdown: EveHQ backup not required")
                        End If
                    End If
                    HQ.WriteLogEvent("Shutdown: Request to save EveHQ Settings before exit")
                    Call HQ.Settings.Save()
                End If

                ' Remove the icons
                HQ.WriteLogEvent("Shutdown: Dispose of EveHQ icons")
                EveStatusIcon.Visible = False
                iconEveHQMLW.Visible = False
                iconEveHQMLW.Icon = Nothing
                iconEveHQMLW.Dispose()
                EveStatusIcon.Dispose()

                HQ.WriteLogEvent("Shutdown: Shutdown complete")
                ' Close log files

                Trace.Flush()
                ' Trace.Listeners.Remove(HQ.EveHqTracer)

                'End

            Catch e As Exception
                MessageBox.Show(
                    "An error occurred calling the shutdown routine for EveHQ: " & e.Message & " - " & e.StackTrace,
                    "Error Closing EveHQ", MessageBoxButtons.OK, MessageBoxIcon.Error)

            Finally
                Trace.Flush()
            End Try
        End Sub

        Private Sub SaveEverything(ByVal silent As Boolean)
            Dim savesDone As List(Of String) = New List(Of String)()
            ' Save Core data and settings
            HQ.Settings.QatLayout = RibbonControl1.QatLayout
            Call HQ.Settings.Save()
            savesDone.Add("EveHQ Core")

            ' Save Plug-in data and settings
            For Each myPlugIn As EveHQPlugIn In HQ.Plugins.Values
                If myPlugIn.Status = PlugIn.PlugInStatus.Active Then
                    Dim dataSaved As Boolean = myPlugIn.Instance.SaveAll()
                    If dataSaved = True Then
                        savesDone.Add(myPlugIn.Name)
                    End If
                End If
            Next

            ' Report result to user
            If silent = False Then
                If savesDone.Count > 0 Then
                    Dim msg As String = "Data and settings of the following modules have been saved:"
                    For Each moduleName As String In savesDone
                        msg &= vbCrLf & " " & Chr(149) & " " & moduleName
                    Next
                    MessageBox.Show(msg, "Data Saving Finished", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Else
                    MessageBox.Show("No data and settings have been saved.", "Data Saving Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            End If
        End Sub

#End Region

#Region "Skill Display Updater & Notification Routines"

        Private Sub SkillWorker_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs) Handles _skillWorker.DoWork
            For Each tPilot As EveHQPilot In HQ.Settings.Pilots.Values
                If tPilot.Active = True Then
                    tPilot.TrainingCurrentSP = CInt(SkillFunctions.CalcCurrentSkillPoints(tPilot))
                    tPilot.TrainingCurrentTime = SkillFunctions.CalcCurrentSkillTime(tPilot)
                End If
            Next
        End Sub

        Private Sub SkillWorker_RunWorkerCompleted(ByVal sender As Object, ByVal e As RunWorkerCompletedEventArgs) _
            Handles _skillWorker.RunWorkerCompleted

            Call UpdateEveTime()

            Call CheckNotifications()

            Dim ti As TabItem

            ' Update pilot form if open
            ti = HQ.GetMdiTab(FrmPilot.Text)
            If ti IsNot Nothing Then
                Call FrmPilot.UpdateSkillInfo()
            End If

            ' Update training form if open
            ti = HQ.GetMdiTab(FrmTraining.Text)
            If ti IsNot Nothing Then
                Call FrmTraining.UpdateTraining()
            End If

            If FrmSkillDetails.Visible = True Then
                Call FrmSkillDetails.UpdateSkillDetails()
            End If

            Call CheckForCharAPIUpdate()

            Call CheckForMailAPIUpdate()
        End Sub

        Private Sub tmrSkillUpdate_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tmrSkillUpdate.Tick
            If _skillWorker.IsBusy = False Then
                _skillWorker.RunWorkerAsync()
            End If
        End Sub

        Private Sub CheckNotifications()

            'Mitigation for EVEHQ-92 ... null check the pilots collection before trying to use it.
            If HQ.Settings.Pilots Is Nothing Then
                Return
            End If

            ' Only do this if at least one notification is enabled
            If _
                HQ.Settings.NotifyToolTip = True Or HQ.Settings.NotifyDialog = True Or
                HQ.Settings.NotifyEMail = True Or HQ.Settings.NotifySound = True Then
                Dim notifyText As String

                If HQ.Settings.Pilots Is Nothing Or HQ.Settings.Pilots.Count = 0 Then
                    Return ' no pilots not reason to continue.
                End If

                For Each cPilot As EveHQPilot In HQ.Settings.Pilots.Values
                    If cPilot.Active = True And cPilot.Training = True Then
                        notifyText = ""
                        Dim trainingTime As Long = SkillFunctions.CalcCurrentSkillTime(cPilot)
                        ' See if we need to notify about this pilot
                        If trainingTime <= HQ.Settings.NotifyOffset Then
                            If cPilot.TrainingNotifiedEarly = False Then
                                If cPilot.TrainingCurrentTime <= 0 And cPilot.TrainingNotifiedNow = False Then
                                    If HQ.Settings.NotifyNow = True Then
                                        notifyText &= cPilot.Name & " has completed training of " & cPilot.TrainingSkillName &
                                                      " to Level " & cPilot.TrainingSkillLevel & "." & ControlChars.CrLf
                                        cPilot.TrainingNotifiedEarly = True
                                        cPilot.TrainingNotifiedNow = True
                                    End If
                                Else
                                    If HQ.Settings.NotifyEarly = True Then
                                        Dim strTime As String = SkillFunctions.TimeToString(cPilot.TrainingCurrentTime)
                                        strTime = strTime.Replace("s", " seconds").Replace("m", " minutes")
                                        notifyText &= cPilot.Name & " has approximately " & strTime & " before training of " &
                                                      cPilot.TrainingSkillName & " to Level " & cPilot.TrainingSkillLevel &
                                                      " completes." & ControlChars.CrLf
                                        cPilot.TrainingNotifiedEarly = True
                                        cPilot.TrainingNotifiedNow = False
                                    End If
                                End If
                            Else
                                If cPilot.TrainingCurrentTime <= 0 And cPilot.TrainingNotifiedNow = False Then
                                    If HQ.Settings.NotifyNow = True Then
                                        notifyText &= cPilot.Name & " has completed training of " & cPilot.TrainingSkillName &
                                                      " to Level " & cPilot.TrainingSkillLevel & "." & ControlChars.CrLf
                                        cPilot.TrainingNotifiedEarly = True
                                        cPilot.TrainingNotifiedNow = True
                                    End If
                                End If
                            End If

                            ' Show the notifications
                            If notifyText <> "" Then
                                ' If sound is required: Play first as this is automatically put on a separate thread
                                If HQ.Settings.NotifySound = True Then
                                    Try
                                        My.Computer.Audio.Play(HQ.Settings.NotifySoundFile, AudioPlayMode.Background)
                                    Catch ex As Exception
                                    End Try
                                End If
                                ' If tooltip is required:
                                If HQ.Settings.NotifyToolTip = True Then
                                    EveStatusIcon.ShowBalloonTip(3000, "Training Notification", notifyText, ToolTipIcon.Info)
                                End If
                                ' If dialog box is required:
                                If HQ.Settings.NotifyDialog = True Then
                                    MessageBox.Show(notifyText, "Training Notification", MessageBoxButtons.OK,
                                                    MessageBoxIcon.Information)
                                End If
                                ' If email is required:
                                If HQ.Settings.NotifyEMail = True Then
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
                                                            notifyText &= sq.Name & ": " &
                                                                          CType(nq(q), SortedQueueItem).Name
                                                            notifyText &= " (" &
                                                                          SkillFunctions.Roman(
                                                                              CInt(CType(nq(q), SortedQueueItem).FromLevel))
                                                            notifyText &= " to " &
                                                                          SkillFunctions.Roman(
                                                                              CInt(CType(nq(q), SortedQueueItem).FromLevel) +
                                                                              1) & ")" & ControlChars.CrLf
                                                            Exit For
                                                        End If
                                                    Next
                                                End If
                                            Else
                                                If nq.Count > 0 Then
                                                    For q As Integer = 0 To nq.Count - 1
                                                        If CType(nq(q), SortedQueueItem).Done = False Then
                                                            notifyText &= sq.Name & ": " &
                                                                          CType(nq(q), SortedQueueItem).Name
                                                            notifyText &= " (" &
                                                                          SkillFunctions.Roman(
                                                                              CInt(CType(nq(q), SortedQueueItem).FromLevel))
                                                            notifyText &= " to " &
                                                                          SkillFunctions.Roman(
                                                                              CInt(CType(nq(q), SortedQueueItem).FromLevel) +
                                                                              1) & ")" & ControlChars.CrLf
                                                            Exit For
                                                        End If
                                                    Next
                                                End If
                                            End If
                                        Next
                                    End If
                                    Call SendEveHQMail(cPilot, notifyText)
                                End If
                            End If
                        End If
                    End If
                Next
            End If
        End Sub

        Private Sub SendEveHQMail(ByVal cpilot As EveHQPilot, ByVal mailText As String)
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
                Dim eveHQMsg As New MailMessage(HQ.Settings.EmailSenderAddress, HQ.Settings.EMailAddress)
                eveHQMsg.Subject = "Eve Training Notification: " & cpilot.Name & " (" & cpilot.TrainingSkillName & " " &
                                   SkillFunctions.Roman(cpilot.TrainingSkillLevel) & ")"
                eveHQMsg.Body = mailText
                eveHQMail.Send(eveHQMsg)
            Catch ex As Exception
                MessageBox.Show(
                    "The mail notification sending process failed. Please check that the server, port, address, username and password are correct." &
                    ControlChars.CrLf & ControlChars.CrLf & "The error was: " & ex.Message, "EveHQ Email Notification Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End Try
        End Sub

        Private Sub CheckForCharAPIUpdate()
            ' Check for an API update if applicable
            If HQ.Settings.AutoAPI = True Then
                Dim updateRequired As Boolean = False
                For Each cPilot As EveHQPilot In HQ.Settings.Pilots.Values
                    If cPilot.Name <> "" And cPilot.Account <> "" Then
                        Dim cacheCDate As Date = SkillFunctions.ConvertEveTimeToLocal(cPilot.CacheExpirationTime)
                        Dim cacheTDate As Date = SkillFunctions.ConvertEveTimeToLocal(cPilot.TrainingExpirationTime)
                        If cacheCDate < Now Or cacheTDate < Now Then
                            updateRequired = True
                            Exit For
                        Else
                            If cacheCDate < HQ.NextAutoAPITime Then
                                HQ.NextAutoAPITime = cacheCDate
                            End If
                            If cacheTDate < HQ.NextAutoAPITime Then
                                HQ.NextAutoAPITime = cacheTDate
                            End If
                            If HQ.AutoRetryAPITime > HQ.NextAutoAPITime Then
                                HQ.NextAutoAPITime = HQ.AutoRetryAPITime
                            End If
                        End If
                    End If
                Next
                If Now > HQ.AutoRetryAPITime Then
                    If updateRequired = True Then
                        ' Invoke the API Caller
                        HQ.NextAutoAPITime = Now.AddMinutes(60)
                        HQ.AutoRetryAPITime = Now.AddMinutes(5)
                        Call QueryMyEveServer()
                    End If
                    ' Display time until autoAPI download
                    Dim timeLeft As TimeSpan = HQ.NextAutoAPITime - Now
                    lblCharAPITime.Text = SkillFunctions.TimeToString(timeLeft.TotalSeconds, False)
                Else
                    Dim timeLeft As TimeSpan = HQ.NextAutoAPITime - Now
                    Dim timeLeft2 As TimeSpan = HQ.AutoRetryAPITime - Now
                    lblCharAPITime.Text = SkillFunctions.TimeToString(
                        Math.Max(timeLeft.TotalSeconds, timeLeft2.TotalSeconds), False)
                End If
            Else
                lblCharAPITime.Text = ""
            End If
        End Sub

        Private Sub CheckForMailAPIUpdate()
            ' Check if the mail download is in progress
            If EveMailEvents.MailIsBeingProcessed = False Then
                ' Check for an API update if applicable
                If HQ.Settings.AutoMailAPI = True Then
                    If Now > HQ.NextAutoMailAPITime Then
                        ' Invoke the API Caller
                        Call UpdateMailNotifications()
                        ' Display time until autoMailAPI download
                        Dim timeLeft As TimeSpan = HQ.NextAutoMailAPITime - Now
                        lblMailAPITime.Text = SkillFunctions.TimeToString(timeLeft.TotalSeconds, False)
                    Else
                        Dim timeLeft As TimeSpan = HQ.NextAutoMailAPITime - Now
                        lblMailAPITime.Text = SkillFunctions.TimeToString(timeLeft.TotalSeconds, False)
                    End If
                Else
                    lblMailAPITime.Text = ""
                End If
            Else
                lblMailAPITime.Text = "Processing..."
            End If
        End Sub

#End Region

        Private Sub RemoteUpdate()
            Invoke(New QueryMyEveServerDelegate(AddressOf QueryMyEveServer))
        End Sub

        Public Sub QueryMyEveServer()
            If HQ.APIUpdateInProgress = False Then
                HQ.APIUpdateInProgress = True
                btnQueryAPI.Enabled = False
                FrmSettings.btnGetData.Enabled = False
                Dim charThread As New Thread(AddressOf StartCharacterAPIThread)
                charThread.SetApartmentState(ApartmentState.STA) 'Bug EveHQ-118 .. character thread needs to be set to STA
                charThread.IsBackground = True
                charThread.Start()
            Else
                ' Do we want to add a user message here?
                ' Maybe some form of logging to see why this would be happening?
                'MessageBox.Show("A method attempted to run the Character API Update before the previous attempt was completed. The stack trace is: " & ControlChars.CrLf & ControlChars.CrLf & System.Environment.StackTrace, "Character API Routine Duplication", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End Sub

        Public Sub StartCharacterAPIThread(ByVal state As Object)
            Try

                ' If we have accounts to query then get the data for them
                If HQ.Settings.Accounts.Count = 0 Then
                    lblAPIStatus.Text = "API Status: No accounts entered into settings!! (" & Now.ToString & ")"
                    Exit Sub
                Else
                    lblAPIStatus.Text = "API Status: Fetching Character Data..."
                    Invoke(Sub() barStatus.Refresh())
                    ' Clear the current list of pilots
                    HQ.TempPilots.Clear()
                    HQ.TempCorps.Clear()
                    HQ.APIResults.Clear()
                    ' get the details for the account
                    For Each currentAccount As EveHQAccount In HQ.Settings.Accounts.Values
                        If currentAccount.APIAccountStatus <> APIAccountStatuses.ManualDisabled Then
                            lblAPIStatus.Text = "API Status: Updating Account '" & currentAccount.FriendlyName & "' (ID=" &
                                                currentAccount.UserID & ")..."
                            Invoke(Sub() barStatus.Refresh())
                            Call PilotParseFunctions.GetCharactersInAccount(currentAccount)
                        End If
                    Next
                    Call PilotParseFunctions.CopyTempPilotsToMain()
                    Call PilotParseFunctions.CopyTempCorpsToMain()
                End If

                ' Determine API responses and display appropriate message
                Dim allCached As Boolean = True
                Dim containsErrors As Boolean = False
                For Each result As Integer In HQ.APIResults.Values
                    If result <> 1 Then allCached = False
                    Select Case result
                        Case 2, 3, 4, 5, 6, 8, 9
                            containsErrors = True
                        Case Is < 0
                            containsErrors = True
                    End Select
                Next

                ' Display the results
                If containsErrors = True Then
                    lblAPIStatus.Text = "API Status: Last Download - " & Now.ToString &
                                        " (Errors occured - double-click for details)"
                Else
                    If allCached = True Then
                        lblAPIStatus.Text = "API Status: Last Download - " & Now.ToString & " (No new updates)"
                    Else
                        lblAPIStatus.Text = "API Status: Last Download - " & Now.ToString & " (Update successful)"
                    End If
                End If

                If IsHandleCreated Then
                    ' Save the settings
                    Invoke(Sub() HQ.Settings.Save())

                    ' Enable the option again
                    btnQueryAPI.Enabled = True
                    Invoke(Sub() ResetSettingsButton())

                    ' Update data
                    Invoke(Sub() UpdatePilotInfo())
                End If
            Catch e As Exception
                If IsHandleCreated Then
                    Invoke(Sub()
                               CatchGeneralException(e)
                           End Sub)
                End If

            End Try
            ' We've finished our update routine so we can now release the flag
            HQ.APIUpdateInProgress = False
        End Sub

        Public Sub ResetSettingsButton()
            Call FrmSettings.FinaliseAPIServerUpdate()
        End Sub

        Public Sub UpdatePilotInfo()

            ' Setup the Training Status Bar
            Call SetupTrainingStatus()

            ' Update the cbopilots in the pilot form and the training form
            If FrmPilot.IsHandleCreated = True Then
                FrmPilot.UpdatePilots()
            End If
            If FrmTraining.IsHandleCreated = True Then
                FrmTraining.UpdatePilots()
            End If
            If FrmSettings.IsHandleCreated = True Then
                FrmSettings.UpdatePilots()
            End If

            If HQ.Settings.Pilots.Count = 0 Then
                btnViewPilotInfo.Enabled = False
                btnViewSkillTraining.Enabled = False
                If FrmPilot IsNot Nothing Then
                    If FrmPilot.IsHandleCreated = True Then
                        FrmPilot.Close()
                    End If
                End If
                If FrmTraining IsNot Nothing Then
                    If FrmTraining.IsHandleCreated = True Then
                        FrmTraining.Close()
                    End If
                End If
            Else
                btnViewPilotInfo.Enabled = True
                btnViewSkillTraining.Enabled = True
            End If

            ' Update the dashboard if applicable
            If FrmDashboard.IsHandleCreated = True Then
                Call FrmDashboard.UpdateWidgets()
            End If
        End Sub

        Private Sub SetupTrainingStatus()

            If HQ.Settings.DisableTrainingBar = False Then
                ' Setup a collection for sorting
                Dim pilotTrainingTimes As New List(Of PilotSortTrainingTime)
                Dim trainingAccounts As New ArrayList
                Dim disabledAccounts As New ArrayList
                For Each cPilot As EveHQPilot In HQ.Settings.Pilots.Values
                    ' Check for disabled accounts
                    If HQ.Settings.Accounts.ContainsKey(cPilot.Account) Then

                        ' Check for training accounts
                        If cPilot.Training = True Then
                            Dim p As New PilotSortTrainingTime
                            p.Name = cPilot.Name
                            p.TrainingEndTime = cPilot.TrainingEndTime
                            ' Only add active pilots!
                            If cPilot.Active = True Then
                                pilotTrainingTimes.Add(p)
                            End If
                            trainingAccounts.Add(cPilot.Account)
                        End If
                    End If
                Next

                ' Clear old event handlers
                For c As Integer = trainingBlockLayout.Controls.Count - 1 To 0 Step -1
                    Dim cb As CharacterTrainingBlock = CType(trainingBlockLayout.Controls(c), CharacterTrainingBlock)
                    RemoveHandler cb.lblSkill.Click, AddressOf TrainingStatusLabelClick
                    RemoveHandler cb.lblTime.Click, AddressOf TrainingStatusLabelClick
                    RemoveHandler cb.lblQueue.Click, AddressOf TrainingStatusLabelClick
                    RemoveHandler cb.pbPilot.Click, AddressOf PilotPicClick
                    cb.Dispose()
                Next

                ' Initialise the x-location and clear items
                trainingBlockLayout.Controls.Clear()
                Dim startloc As Integer = 0

                ' Add non-training accounts to the training bar
                For Each cAccount As EveHQAccount In HQ.Settings.Accounts.Values
                    If trainingAccounts.Contains(cAccount.UserID) = False Then
                        ' Only add if not a APIv2 corp account
                        If _
                                Not _
                                (cAccount.ApiKeySystem = APIKeySystems.Version2 And
                                 cAccount.APIKeyType = APIKeyTypes.Corporation) Then
                            ' Build a status panel if the account is not manually disabled
                            If cAccount.APIAccountStatus <> APIAccountStatuses.ManualDisabled Then
                                Dim cb As New CharacterTrainingBlock(cAccount.UserID, True)
                                trainingBlockLayout.Controls.Add(cb)
                                If Bar1.DockSide = eDockSide.Bottom Or Bar1.DockSide = eDockSide.Top Then
                                    cb.Left = startloc
                                    cb.BringToFront()
                                    startloc += cb.Width + 20
                                Else
                                    cb.Top = startloc
                                    cb.BringToFront()
                                    startloc += cb.Height + 5
                                End If
                            End If
                        End If
                    End If
                Next

                ' Add training pilots to the training bar
                ' Note: I'm not completely sure why the date objects need to be sorted descending, in order to get the pilots in order of
                ' next to finish training, but it's what is required. It seems counter-intuitive, but don't change this to an ascending order or
                ' you get the pilots in the wrong order (the one with the most training left is first)
                For Each cPilot As PilotSortTrainingTime In pilotTrainingTimes.OrderByDescending(Function(pilot) pilot.TrainingEndTime)
                    Dim cb As New CharacterTrainingBlock(cPilot.Name, False)
                    AddHandler cb.lblSkill.Click, AddressOf TrainingStatusLabelClick
                    AddHandler cb.pbPilot.Click, AddressOf PilotPicClick
                    AddHandler cb.lblTime.Click, AddressOf TrainingStatusLabelClick
                    AddHandler cb.lblQueue.Click, AddressOf TrainingStatusLabelClick
                    trainingBlockLayout.Controls.Add(cb)
                    If Bar1.DockSide = eDockSide.Bottom Or Bar1.DockSide = eDockSide.Top Then

                        cb.Left = startloc
                        cb.BringToFront()
                        startloc += cb.Width + 20
                    Else
                        cb.Top = startloc
                        cb.BringToFront()
                        startloc += cb.Height + 5
                    End If
                Next
            End If
        End Sub

        Public Sub PilotPicClick(ByVal sender As Object, ByVal e As EventArgs)
            Dim selectedPic As PictureBox = CType(sender, PictureBox)
            Call OpenPilotInfoForm()
            If selectedPic.Name <> "" Then
                FrmPilot.DisplayPilotName = selectedPic.Name
            End If
        End Sub

        Public Sub TrainingStatusLabelClick(ByVal sender As Object, ByVal e As EventArgs)
            Dim selectedLabel As LinkLabel = CType(sender, LinkLabel)
            Call OpenSkillTrainingForm()
            If selectedLabel.Name <> "" Then
                FrmTraining.DisplayPilotName = selectedLabel.Name
            End If
        End Sub

        Private Sub EveIconMenu_Opening(ByVal sender As Object, ByVal e As CancelEventArgs) Handles EveIconMenu.Opening

            ' Hide the tooltip form
            If _eveHQTrayForm IsNot Nothing Then
                _eveHQTrayForm.Close()
                _eveHQTrayForm = Nothing
            End If

            If HQ.Settings.EveFolder(1) IsNot Nothing Then
                If My.Computer.FileSystem.FileExists(Path.Combine(HQ.Settings.EveFolder(1), "Eve.exe")) = True Then
                    If HQ.Settings.EveFolderLabel(1) <> "" Then
                        ctxmnuLaunchEve1.Text = "Launch Eve (" & HQ.Settings.EveFolderLabel(1) & ")"
                    End If
                    ctxmnuLaunchEve1.Enabled = True
                Else
                    ctxmnuLaunchEve1.Enabled = False
                End If
            End If

            If HQ.Settings.EveFolder(2) IsNot Nothing Then
                If My.Computer.FileSystem.FileExists(Path.Combine(HQ.Settings.EveFolder(2), "Eve.exe")) = True Then
                    If HQ.Settings.EveFolderLabel(2) <> "" Then
                        ctxmnuLaunchEve2.Text = "Launch Eve (" & HQ.Settings.EveFolderLabel(2) & ")"
                    End If
                    ctxmnuLaunchEve2.Enabled = True
                Else
                    ctxmnuLaunchEve2.Enabled = False
                End If
            End If

            If HQ.Settings.EveFolder(3) IsNot Nothing Then
                If My.Computer.FileSystem.FileExists(Path.Combine(HQ.Settings.EveFolder(3), "Eve.exe")) = True Then
                    If HQ.Settings.EveFolderLabel(3) <> "" Then
                        ctxmnuLaunchEve3.Text = "Launch Eve (" & HQ.Settings.EveFolderLabel(3) & ")"
                    End If
                    ctxmnuLaunchEve3.Enabled = True
                Else
                    ctxmnuLaunchEve3.Enabled = False
                End If
            End If

            If HQ.Settings.EveFolder(4) IsNot Nothing Then
                If My.Computer.FileSystem.FileExists(Path.Combine(HQ.Settings.EveFolder(4), "Eve.exe")) = True Then
                    If HQ.Settings.EveFolderLabel(4) <> "" Then
                        ctxmnuLaunchEve4.Text = "Launch Eve (" & HQ.Settings.EveFolderLabel(4) & ")"
                    End If
                    ctxmnuLaunchEve4.Enabled = True
                Else
                    ctxmnuLaunchEve4.Enabled = False
                End If
            End If
        End Sub

#Region "Backup Worker routines"

        Private Sub tmrBackup_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tmrBackup.Tick
            If (HQ.Settings IsNot Nothing) Then

                If (_backupWorker IsNot Nothing) Then
                    If _backupWorker.IsBusy = False Then
                        If HQ.Settings.BackupAuto = True Then
                            Dim nextBackup As Date = HQ.Settings.BackupStart
                            If HQ.Settings.BackupLast > nextBackup Then
                                nextBackup = HQ.Settings.BackupLast
                            End If
                            nextBackup = DateAdd(DateInterval.Day, HQ.Settings.BackupFreq, nextBackup)
                            If Now >= nextBackup Then
                                _backupWorker.RunWorkerAsync()
                            End If
                        End If
                    End If
                End If

                If (_eveHQBackupWorker IsNot Nothing) Then

                    If _eveHQBackupWorker.IsBusy = False Then
                        If HQ.Settings.EveHqBackupMode = 2 Then
                            Dim nextBackup As Date = HQ.Settings.EveHqBackupStart
                            If HQ.Settings.EveHqBackupLast > nextBackup Then
                                nextBackup = HQ.Settings.EveHqBackupLast
                            End If
                            nextBackup = DateAdd(DateInterval.Day, HQ.Settings.EveHqBackupFreq, nextBackup)
                            If Now >= nextBackup Then
                                _eveHQBackupWorker.RunWorkerAsync()
                            End If
                        End If
                    End If
                End If

            End If
        End Sub

        Private Sub BackupWorker_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs) Handles _backupWorker.DoWork
            Call FrmBackup.BackupEveSettings()
        End Sub

        Private Sub BackupWorker_RunWorkerCompleted(ByVal sender As Object, ByVal e As RunWorkerCompletedEventArgs) _
            Handles _backupWorker.RunWorkerCompleted

            If HQ.Settings.BackupLastResult = -1 Then
                FrmBackup.lblLastBackup.Text = HQ.Settings.BackupLast.ToString
            End If
            Call FrmBackup.CalcNextBackup()
            Call FrmBackup.ScanBackups()
            If HQ.Settings.BackupLastResult = -1 Then
                lblAPIStatus.Text = "Eve Settings Backup Successful: " & HQ.Settings.BackupLast.ToString
            Else
                lblAPIStatus.Text = "Eve Settings Backup Aborted - No Source Folders"
            End If
        End Sub

        Private Sub EveHQBackupWorker_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs) _
            Handles _eveHQBackupWorker.DoWork
            Call EveHQBackup.BackupEveHQSettings()
        End Sub

        Private Sub EveHQBackupWorker_RunWorkerCompleted(ByVal sender As Object, ByVal e As RunWorkerCompletedEventArgs) _
            Handles _eveHQBackupWorker.RunWorkerCompleted
            Call EveHQBackup.CalcNextBackup()
            If FrmBackupEveHQ.IsHandleCreated = True Then
                Call FrmBackupEveHQ.ScanBackups()
            End If
            If HQ.Settings.EveHqBackupLastResult = -1 Then
                lblAPIStatus.Text = "EveHQ Settings Backup Successful: " & HQ.Settings.EveHqBackupLast.ToString
            Else
                lblAPIStatus.Text = "EveHQ Settings Backup Failed!"
            End If
        End Sub

#End Region

#Region "Background Module Loading"

        Private Sub tmrModules_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tmrModules.Tick
            tmrModules.Stop()
            For Each plugInInfo As EveHQPlugIn In HQ.Plugins.Values
                ' Override settings if the remote server says so
                Dim serverOverride As Boolean = False
                If HQ.EveHQServerMessage IsNot Nothing Then
                    If HQ.EveHQServerMessage.DisabledPlugins.ContainsKey(plugInInfo.Name) = True Then
                        If plugInInfo.Version <> "" Then
                            If _
                                CompareVersions(plugInInfo.Version, HQ.EveHQServerMessage.DisabledPlugins(plugInInfo.Name)) =
                                True Then
                                serverOverride = True
                            End If
                        End If
                    End If
                End If
                If serverOverride = False Then
                    If plugInInfo.Available = True And plugInInfo.Disabled = False Then
                        If plugInInfo.RunAtStartup = True Then
                            ThreadPool.QueueUserWorkItem(AddressOf RunModuleStartUps, plugInInfo)
                        End If
                    ElseIf plugInInfo.Available = True And plugInInfo.Disabled = True Then
                        ' Check for initialisation from a parameter
                        If plugInInfo.PostStartupData IsNot Nothing Then
                            Dim msg As String = plugInInfo.Name & " is not configured to run at startup but EveHQ was started with data specifcally for that Plug-In." & ControlChars.CrLf & ControlChars.CrLf
                            msg &= "Would you like to initialise the Plug-in so the data can be viewed?"
                            If MessageBox.Show(msg, "Confirm Load Plug-In", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                                ThreadPool.QueueUserWorkItem(AddressOf RunModuleStartUps, plugInInfo)
                            End If
                        End If
                    End If
                End If
            Next
        End Sub

        Public Sub RunModuleStartUps(ByVal state As Object)
            Dim plugInInfo As EveHQPlugIn = CType(state, EveHQPlugIn)
            Dim myAssembly As Assembly = Assembly.LoadFrom(plugInInfo.FileName)
            Dim t As Type = myAssembly.GetType(plugInInfo.FileType)
            plugInInfo.Instance = CType(Activator.CreateInstance(t), IEveHQPlugIn)
            Dim runPlugIn As IEveHQPlugIn = plugInInfo.Instance
            Dim pluginContainer As ItemContainer = CType(rbPlugins.Items.Item(plugInInfo.Name), ItemContainer)
            Dim loadPlugInButton As ButtonItem = CType(pluginContainer.SubItems("LPI" & plugInInfo.Name), ButtonItem)
            Dim runPlugInButton As ButtonItem = CType(pluginContainer.SubItems("RPI" & plugInInfo.Name), ButtonItem)
            loadPlugInButton.Enabled = False
            runPlugInButton.Text = plugInInfo.Name & ControlChars.CrLf & "Status: Loading..."
            loadPlugInButton.Text = "Loading..."
            runPlugInButton.Refresh()
            plugInInfo.Status = EveHQPlugInStatus.Loading
            Try
                Dim plugInResponse As String
                plugInResponse = runPlugIn.EveHQStartUp().ToString
                If CBool(plugInResponse) = False Then
                    loadPlugInButton.Enabled = True
                    runPlugInButton.Enabled = False
                    loadPlugInButton.Text = "Load Plug-in"
                    runPlugInButton.Text = plugInInfo.Name & ControlChars.CrLf & "Status: Failed"
                    plugInInfo.Status = EveHQPlugInStatus.Failed
                Else
                    Dim hitError As Boolean
                    Do
                        hitError = False
                        Try
                            If runPlugInButton IsNot Nothing Then
                                runPlugInButton.Enabled = True
                            End If
                        Catch e As Exception
                            hitError = True
                        End Try
                    Loop Until hitError = False
                    loadPlugInButton.Enabled = False
                    runPlugInButton.Enabled = True
                    runPlugInButton.Text = plugInInfo.Name & ControlChars.CrLf & "Status: Ready"
                    loadPlugInButton.Text = ""
                    plugInInfo.Status = EveHQPlugInStatus.Active
                End If
                ' Clean up after loading the plugin
                Call HQ.ReduceMemory()
                ' Check if we should open the plug-in by reference to any PostLoadData
                If plugInInfo.PostStartupData IsNot Nothing Then
                    ' Open the Plug-in
                    Dim myDelegate As New OpenPlugInDelegate(AddressOf OpenPlugIn)
                    Invoke(myDelegate, New Object() {plugInInfo.Name})
                End If
            Catch et As ThreadAbortException
                loadPlugInButton.Enabled = True
                runPlugInButton.Enabled = False
            Catch ex As Exception
                MessageBox.Show("Unable to load plugin: " & plugInInfo.Name & ControlChars.CrLf & ex.Message, "Plugin error")

                loadPlugInButton.Enabled = True
                runPlugInButton.Enabled = False
            End Try
            Invoke(Sub() rbPlugins.Refresh())
        End Sub

#End Region

#Region "Plug-in Routines"

        Private Sub SetupModuleMenu()
            If HQ.Plugins.Count <> 0 Then
                ' Clear the Plug-ins ribbon
                rbPlugins.Items.Clear()
                For Each plugInInfo As EveHQPlugIn In HQ.Plugins.Values
                    If plugInInfo.Available = True Then
                        ' Create the plug-in container and orientations
                        Dim pluginContainer As New ItemContainer
                        pluginContainer.Name = plugInInfo.Name
                        pluginContainer.LayoutOrientation = eOrientation.Vertical
                        pluginContainer.MinimumSize = New Size(80, 25)
                        pluginContainer.HorizontalItemAlignment = eHorizontalItemsAlignment.Left
                        pluginContainer.VerticalItemAlignment = eVerticalItemsAlignment.Top

                        ' Create a new plug-in button for the item
                        Dim runPlugInButton As New ButtonItem
                        runPlugInButton.ButtonStyle = eButtonStyle.ImageAndText
                        runPlugInButton.ImagePosition = eImagePosition.Left
                        runPlugInButton.Image = plugInInfo.MenuImage
                        runPlugInButton.ImageFixedSize = New Size(40, 40)
                        runPlugInButton.Name = "RPI" & plugInInfo.Name
                        runPlugInButton.Text = plugInInfo.Name & ControlChars.CrLf & "Status: Not Loaded"

                        ' Add a shiny tooltip
                        Dim stt As New SuperTooltipInfo
                        stt.FooterText = "EveHQ Plug-in: " & plugInInfo.Name
                        stt.BodyText = plugInInfo.Description & ControlChars.CrLf & ControlChars.CrLf
                        stt.BodyText &= "Author: " & plugInInfo.Author
                        stt.Color = eTooltipColor.Yellow
                        stt.BodyImage = CType(My.Resources.Info32, Image)
                        stt.FooterImage = plugInInfo.MenuImage
                        SuperTooltip1.SetSuperTooltip(runPlugInButton, stt)

                        AddHandler runPlugInButton.Click, AddressOf PlugInIconClick
                        pluginContainer.SubItems.Add(runPlugInButton)
                        ' Add a load item for each disabled plug-in
                        Dim loadPlugInButton As New ButtonItem
                        loadPlugInButton.Name = "LPI" & plugInInfo.Name
                        loadPlugInButton.Text = "Load Plug-in"
                        loadPlugInButton.Tooltip = "Load the " & plugInInfo.MainMenuText & " Plug-in"
                        loadPlugInButton.ImageFixedSize = New Size(2, 2)
                        loadPlugInButton.ButtonStyle = eButtonStyle.TextOnlyAlways
                        loadPlugInButton.ImagePosition = eImagePosition.Top
                        loadPlugInButton.CanCustomize = False
                        loadPlugInButton.Size = New Size(20, 40)
                        AddHandler loadPlugInButton.Click, AddressOf LoadPlugin
                        pluginContainer.SubItems.Add(loadPlugInButton)

                        ' Override settings if the remote server says so
                        Dim serverOverride As Boolean = False
                        If HQ.EveHQServerMessage IsNot Nothing Then
                            If HQ.EveHQServerMessage.DisabledPlugins.ContainsKey(plugInInfo.Name) = True Then
                                If CompareVersions(plugInInfo.Version,
                                                    HQ.EveHQServerMessage.DisabledPlugins(plugInInfo.Name)) = True Then
                                    serverOverride = True
                                End If
                            End If
                        End If

                        If serverOverride = False Then
                            If plugInInfo.RunAtStartup = True Then
                                loadPlugInButton.Enabled = True
                                runPlugInButton.Enabled = False
                                plugInInfo.Status = EveHQPlugInStatus.Uninitialised
                            Else
                                If plugInInfo.Disabled = False Then
                                    runPlugInButton.Text = plugInInfo.Name & ControlChars.CrLf & "Status: Ready"
                                    loadPlugInButton.Enabled = False
                                    loadPlugInButton.Text = ""
                                    runPlugInButton.Enabled = True
                                    plugInInfo.Status = EveHQPlugInStatus.Active
                                Else
                                    loadPlugInButton.Enabled = True
                                    runPlugInButton.Enabled = False
                                    plugInInfo.Status = EveHQPlugInStatus.Uninitialised
                                End If
                            End If
                        Else
                            runPlugInButton.Text = plugInInfo.Name & ControlChars.CrLf & "Status: Disabled"
                            runPlugInButton.Tooltip = plugInInfo.MainMenuText &
                                                      " has been disabled remotely due to critical issues!"
                            loadPlugInButton.Enabled = False
                            loadPlugInButton.Text = "Disabled"
                            loadPlugInButton.Tooltip = plugInInfo.MainMenuText &
                                                       " has been disabled remotely due to critical issues!"

                            runPlugInButton.Enabled = False
                            plugInInfo.Status = EveHQPlugInStatus.Uninitialised
                        End If
                        rbPlugins.Items.Add(pluginContainer)
                    Else
                        ' Need anything here if the plug-in is disabled?
                    End If
                Next
                RibbonControl1.RecalcLayout()
            End If
        End Sub

        Private Function CompareVersions(ByVal thisVersion As String, ByVal requiredVersion As String) As Boolean
            Dim requiresUpdate As Boolean = False
            Try
                Dim localVers() As String = thisVersion.Split(CChar("."))
                Dim remoteVers() As String = requiredVersion.Split(CChar("."))
                For ver As Integer = 0 To 3
                    If CInt(remoteVers(ver)) <> CInt(localVers(ver)) Then
                        If CInt(remoteVers(ver)) > CInt(localVers(ver)) Then
                            requiresUpdate = True
                            Exit For
                        Else
                            requiresUpdate = False
                            Exit For
                        End If
                    End If
                Next
                Return requiresUpdate
            Catch ex As Exception
                Return requiresUpdate
            End Try
        End Function

        Private Sub PlugInIconClick(ByVal sender As Object, ByVal e As EventArgs)
            Dim btn As ButtonItem = DirectCast(sender, ButtonItem)
            Dim plugInName As String = btn.Name.Remove(0, 3)
            Dim tp As TabItem = HQ.GetMdiTab(plugInName)
            If tp IsNot Nothing Then
                tabEveHQMDI.SelectedTab = tp
            Else
                Dim myPlugIn As EveHQPlugIn = HQ.Plugins(plugInName)
                Dim plugInForm As Form = myPlugIn.Instance.RunEveHQPlugIn
                Call DisplayChildForm(plugInForm)
            End If
        End Sub

        Private Sub LoadPlugin(ByVal sender As Object, ByVal e As EventArgs)
            Dim pib As ButtonItem = DirectCast(sender, ButtonItem)
            Dim plugInName As String = pib.Name.Remove(0, 3)
            Dim plugInInfo As EveHQPlugIn = HQ.Plugins.Item(plugInName)
            If plugInInfo.RunAtStartup = True Then
                ThreadPool.QueueUserWorkItem(AddressOf RunModuleStartUps, plugInInfo)
            Else
                Dim pluginContainer As ItemContainer = CType(rbPlugins.Items.Item(plugInInfo.Name), ItemContainer)
                Dim loadPlugInButton As ButtonItem = CType(pluginContainer.SubItems("LPI" & plugInInfo.Name), ButtonItem)
                Dim runPlugInButton As ButtonItem = CType(pluginContainer.SubItems("RPI" & plugInInfo.Name), ButtonItem)
                runPlugInButton.Text = plugInInfo.Name & ControlChars.CrLf & "Status: Ready"
                loadPlugInButton.Text = ""
                plugInInfo.Status = EveHQPlugInStatus.Active
                loadPlugInButton.Enabled = False
                runPlugInButton.Enabled = True
            End If
            rbPlugins.Refresh()
        End Sub

        Public Sub LoadAndOpenPlugIn(ByVal state As Object)
            ' Called usually from an instance
            Dim plugInInfo As PlugIn = CType(state, PlugIn)
            Dim myAssembly As Assembly = Assembly.LoadFrom(plugInInfo.FileName)
            Dim t As Type = myAssembly.GetType(plugInInfo.FileType)
            plugInInfo.Instance = CType(Activator.CreateInstance(t), IEveHQPlugIn)
            Dim runPlugIn As IEveHQPlugIn = plugInInfo.Instance
            Dim pluginContainer As ItemContainer = CType(rbPlugins.Items.Item(plugInInfo.Name), ItemContainer)
            Dim loadPlugInButton As ButtonItem = CType(pluginContainer.SubItems("LPI" & plugInInfo.Name), ButtonItem)
            Dim runPlugInButton As ButtonItem = CType(pluginContainer.SubItems("RPI" & plugInInfo.Name), ButtonItem)
            loadPlugInButton.Enabled = False
            runPlugInButton.Enabled = False
            runPlugInButton.Text = plugInInfo.Name & ControlChars.CrLf & "Status: Loading..."
            plugInInfo.Status = EveHQPlugInStatus.Loading
            Try
                Dim plugInResponse As String
                plugInResponse = runPlugIn.EveHQStartUp().ToString
                If CBool(plugInResponse) = False Then
                    loadPlugInButton.Enabled = True
                    runPlugInButton.Enabled = False
                    runPlugInButton.Text = plugInInfo.Name & ControlChars.CrLf & "Status: Failed"
                    plugInInfo.Status = EveHQPlugInStatus.Failed
                Else
                    runPlugInButton.Text = plugInInfo.Name & ControlChars.CrLf & "Status: Ready"
                    plugInInfo.Status = EveHQPlugInStatus.Active
                    loadPlugInButton.Enabled = False
                    runPlugInButton.Enabled = True
                End If
                ' Clean up after loading the plugin
                Call HQ.ReduceMemory()
                ' Open the Plug-in
                Dim myDelegate As New OpenPlugInDelegate(AddressOf OpenPlugIn)
                Invoke(myDelegate, New Object() {plugInInfo.Name})
            Catch ex As Exception
                MessageBox.Show("Unable to load plugin: " & plugInInfo.Name & ControlChars.CrLf & ex.Message, "Plugin error")
                loadPlugInButton.Enabled = True
                runPlugInButton.Enabled = False
            End Try
        End Sub

        Delegate Sub OpenPlugInDelegate(ByVal plugInName As String)

        Private Sub OpenPlugIn(ByVal plugInName As String)
            Dim plugInInfo As EveHQPlugIn = HQ.Plugins(plugInName)
            If plugInInfo.Status = EveHQPlugInStatus.Active Then
                Dim mainTab As TabStrip = CType(HQ.MainForm.Controls("tabEveHQMDI"), TabStrip)
                Dim tp As TabItem = HQ.GetMdiTab(plugInName)
                If tp IsNot Nothing Then
                    mainTab.SelectedTab = tp
                Else
                    Dim plugInForm As Form = plugInInfo.Instance.RunEveHQPlugIn
                    plugInForm.MdiParent = HQ.MainForm
                    plugInForm.Show()
                End If
                plugInInfo.Instance.GetPlugInData(plugInInfo.PostStartupData, 0)
            End If
        End Sub

#End Region

#Region "TabbedMDI Window Routines"

        Public Sub OpenPilotInfoForm()
            Dim tp As TabItem = HQ.GetMdiTab(FrmPilot.Text)
            If tp Is Nothing Then
                Call DisplayChildForm(FrmPilot)
            Else
                tabEveHQMDI.SelectedTab = tp
            End If
        End Sub

        Public Sub OpenSkillTrainingForm()
            Dim tp As TabItem = HQ.GetMdiTab(FrmTraining.Text)
            If tp Is Nothing Then
                Call DisplayChildForm(FrmTraining)
            Else
                tabEveHQMDI.SelectedTab = tp
            End If
        End Sub

        Public Sub OpenEveHQMailForm()
            Dim tp As TabItem = HQ.GetMdiTab(FrmMail.Text)
            If tp Is Nothing Then
                Call DisplayChildForm(FrmMail)
            Else
                tabEveHQMDI.SelectedTab = tp
            End If
        End Sub

        Private Sub OpenBackUpForm()
            Dim tp As TabItem = HQ.GetMdiTab(FrmBackup.Text)
            If tp Is Nothing Then
                Call DisplayChildForm(FrmBackup)
            Else
                tabEveHQMDI.SelectedTab = tp
            End If
        End Sub

        Private Sub OpenEveHQBackUpForm()
            Dim tp As TabItem = HQ.GetMdiTab(FrmBackupEveHQ.Text)
            If tp Is Nothing Then
                Call DisplayChildForm(FrmBackupEveHQ)
            Else
                tabEveHQMDI.SelectedTab = tp
            End If
        End Sub

        Private Sub OpenAPICheckerForm()
            Dim tp As TabItem = HQ.GetMdiTab(FrmAPIChecker.Text)
            If tp Is Nothing Then
                Call DisplayChildForm(FrmAPIChecker)
            Else
                tabEveHQMDI.SelectedTab = tp
            End If
        End Sub

        Private Sub OpenMarketPricesForm()
            Dim tp As TabItem = HQ.GetMdiTab(_eveHqmlf.Text)
            If tp Is Nothing Then
                _eveHqmlf = New FrmMarketPrices
                Call DisplayChildForm(_eveHqmlf)
            Else
                tabEveHQMDI.SelectedTab = tp
            End If
        End Sub

        Private Sub OpenDashboard()
            Dim tp As TabItem = HQ.GetMdiTab(FrmDashboard.Text)
            If tp Is Nothing Then
                Call DisplayChildForm(FrmDashboard)
            Else
                tabEveHQMDI.SelectedTab = tp
            End If
        End Sub

        Private Sub OpenRequisitions()
            Dim tp As TabItem = HQ.GetMdiTab("EveHQ Requisitions")
            If tp Is Nothing Then
                Dim myReq As New FrmRequisitions
                Call DisplayChildForm(myReq)
            Else
                tabEveHQMDI.SelectedTab = tp
            End If
        End Sub

        Private Sub OpenSQLQueryForm()
            Dim tp As TabItem = HQ.GetMdiTab(FrmSQLQuery.Text)
            If tp Is Nothing Then
                Call DisplayChildForm(FrmSQLQuery)
            Else
                tabEveHQMDI.SelectedTab = tp
            End If
        End Sub

        Private Sub OpenInfoHelpForm()
            Dim tp As TabItem = HQ.GetMdiTab(FrmHelp.Text)
            If tp Is Nothing Then
                Call DisplayChildForm(FrmHelp)
            Else
                tabEveHQMDI.SelectedTab = tp
            End If
        End Sub

        Public Sub DisplayReport(ByRef reportForm As FrmReportViewer, ByVal reportText As String)
            reportForm.Text = reportText
            Dim tp As TabItem = HQ.GetMdiTab(reportForm.Text)
            If tp Is Nothing Then
                Call DisplayChildForm(reportForm)
            Else
                tabEveHQMDI.SelectedTab = tp
            End If
        End Sub

#End Region

        Private Sub RemoteRefreshPilots()
            Call UpdatePilotInfo()
        End Sub

        Public Sub DisplayChartReport(ByRef chartForm As FrmChartViewer, ByVal formTitle As String)
            chartForm.Text = formTitle
            Dim tp As TabItem = HQ.GetMdiTab(chartForm.Text)
            If tp Is Nothing Then
                Call DisplayChildForm(chartForm)
            Else
                tabEveHQMDI.SelectedTab = tp
            End If
        End Sub

        Private Sub ctxmnuLaunchEve1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles ctxmnuLaunchEve1.Click
            Call LaunchEveInNormalWindow(1)
        End Sub

        Private Sub ctxmnuLaunchEve2_Click(ByVal sender As Object, ByVal e As EventArgs) Handles ctxmnuLaunchEve2.Click
            Call LaunchEveInNormalWindow(2)
        End Sub

        Private Sub ctxmnuLaunchEve3_Click(ByVal sender As Object, ByVal e As EventArgs) Handles ctxmnuLaunchEve3.Click
            Call LaunchEveInNormalWindow(3)
        End Sub

        Private Sub ctxmnuLaunchEve4_Click(ByVal sender As Object, ByVal e As EventArgs) Handles ctxmnuLaunchEve4.Click
            Call LaunchEveInNormalWindow(4)
        End Sub

        Private Sub LaunchEveInNormalWindow(ByVal folder As Integer)
            WindowState = FormWindowState.Minimized
            Try
                If HQ.Settings.EveFolderLua(folder) = True Then
                    Process.Start(Path.Combine(HQ.Settings.EveFolder(folder), "Eve.exe"), "/LUA:OFF")
                Else
                    Process.Start(Path.Combine(HQ.Settings.EveFolder(folder), "Eve.exe"))
                End If
            Catch ex As Exception
                MessageBox.Show(
                    "Unable to start Eve. Please ensure that the location is correctly specified in the EveHQ settings.",
                    "Error Starting External Process", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Try
        End Sub

        Public Function CacheErrorHandler() As Boolean
            ' Stop the timer from reporting multiple errors
            tmrSkillUpdate.Stop()
            Dim msg As New StringBuilder
            msg.Append("EveHQ has detected that there is an error in the character cache files. ")
            msg.AppendLine(
                "This could be due to a corrupt cache file or a conflict with another skill training application.")
            msg.AppendLine("")
            msg.AppendLine(
                "The issue may be resolved by clearing the EveHQ cache and connecting back to the API. Would you like to do this now?")
            msg.AppendLine("")

            Dim reply As Integer = MessageBox.Show(msg.ToString, "Skill Error", MessageBoxButtons.YesNo,
                                                   MessageBoxIcon.Question)
            If reply = DialogResult.No Then
                ' Don't do anything with the cache but restart the timer
                tmrSkillUpdate.Start()
                Return False
            Else
                ' Close all open forms
                If tabEveHQMDI.Tabs.Count > 0 Then
                    For tab As Integer = tabEveHQMDI.Tabs.Count - 1 To 0
                        Dim tp As TabItem = tabEveHQMDI.Tabs(tab)
                        tabEveHQMDI.Tabs.Remove(tp)
                    Next
                End If

                ' Clear the EveHQ API cache
                Try
                    If My.Computer.FileSystem.DirectoryExists(HQ.ApiCacheFolder) Then
                        My.Computer.FileSystem.DeleteDirectory(HQ.ApiCacheFolder, DeleteDirectoryOption.DeleteAllContents)
                    End If
                Catch e As Exception
                End Try

                ' Recreate the EveHQ API cache folder
                Try
                    If My.Computer.FileSystem.DirectoryExists(HQ.ApiCacheFolder) = False Then
                        My.Computer.FileSystem.CreateDirectory(HQ.ApiCacheFolder)
                    End If
                Catch e As Exception
                End Try

                '' Clear the EveHQ Pilot Data
                Try
                    ' Clear the current list of pilots
                    HQ.TempPilots.Clear()
                    HQ.TempCorps.Clear()
                    HQ.APIResults.Clear()
                Catch ex As Exception
                End Try

                '' Update the pilot lists
                Call UpdatePilotInfo()

                ' Restart the timer
                tmrSkillUpdate.Start()

                ' Call the API
                Call QueryMyEveServer()

                Return True
            End If
        End Function

        Private Sub lblAPIStatus_DoubleClick(ByVal sender As Object, ByVal e As EventArgs) Handles lblAPIStatus.DoubleClick
            If HQ.APIResults.Count > 0 Then
                Using apiStatus As New EveAPIStatusForm
                    apiStatus.ShowDialog()
                End Using
            End If
        End Sub

        Private Sub tmrMemory_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tmrMemory.Tick
            Call HQ.ReduceMemory()
        End Sub

        Private Sub tmrSave_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tmrSave.Tick
            Call SaveEverything(True)
        End Sub

#Region "Update Check & Menu"

        Private Sub CheckForUpdates(ByVal state As Object)
            Trace.TraceInformation("Checking For Updates...")
            Dim currentComponents As New SortedList
            Dim updateXML As XmlDocument = FetchUpdateXML()

            Dim currentVersion As Version
            If updateXML Is Nothing Then
                Exit Sub
            Else
                ' Get a current list of components
                currentComponents.Clear()
                currentVersion = My.Application.Info.Version

                ' Try parsing the update file
                Try
                    Dim updateVersion As XmlNodeList = updateXML.SelectNodes("/eveHQUpdate/version")
                    Dim installerLocation As XmlNodeList = updateXML.SelectNodes("/eveHQUpdate/location")

                    If (IsUpdateAvailable(currentVersion.ToString, updateVersion(0).InnerText)) Then
                        Trace.TraceInformation("Update Available")
                        btnUpdateEveHQ.Enabled = True

                        Invoke(Sub()
                                   Dim reply As Integer =
                                           MessageBox.Show(Me,
                                               "There is an update for EveHQ. Would you like to download the latest version?",
                                               "Update EveHQ?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1)
                                   If reply = DialogResult.No Then
                                       Exit Sub
                                   Else
                                       Invoke(Sub()
                                                  ShowUpdateForm(installerLocation(0).InnerText)
                                              End Sub)
                                   End If
                               End Sub)
                    Else
                        Trace.TraceInformation("No Update Available")
                        If CBool(state) = True Then
                            MessageBox.Show("There is no new EveHQ update currently available.", "No Update Available", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        End If
                    End If
                Catch ex As Exception
                End Try
            End If
        End Sub

        Private Sub ShowUpdateForm(installerUrl As String)
            Dim myUpdater As New NewUpdater(installerUrl, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EveHQ"), New HttpRequestProvider(HQ.ProxyDetails))
            myUpdater.ShowDialog()
        End Sub

        Private Shared Function IsUpdateAvailable(ByVal localVer As String, ByVal remoteVer As String) As Boolean
            If localVer = "Not Used" Then
                Return False
            Else
                If localVer = remoteVer Then
                    Return False
                Else
                    Dim localVers() As String = localVer.Split(CChar("."))
                    Dim remoteVers() As String = remoteVer.Split(CChar("."))
                    Dim requiresUpdate As Boolean = False
                    For ver As Integer = 0 To 3
                        If CInt(remoteVers(ver)) <> CInt(localVers(ver)) Then
                            If CInt(remoteVers(ver)) > CInt(localVers(ver)) Then
                                requiresUpdate = True
                                Exit For
                            Else
                                requiresUpdate = False
                                Exit For
                            End If
                        End If
                    Next
                    Return requiresUpdate
                End If
            End If
        End Function

        Private Function FetchUpdateXML() As XmlDocument

            ' Set a default policy level for the "http:" and "https" schemes.
            Dim updateServer As String = HQ.Settings.UpdateUrl
            Dim remoteURL As String = updateServer & "_evehqupdate.xml"
            Dim updateXML As New XmlDocument
            Trace.TraceInformation("Fetching Update XML document from {0}".FormatInvariant(remoteURL))
            Try
                ' Create the requester
                Dim temp As Uri = Nothing
                If Uri.TryCreate(remoteURL, UriKind.Absolute, temp) = False Then
                    Return Nothing
                End If

                Dim provider As New HttpRequestProvider(HQ.ProxyDetails)

                Dim requestTask As Task(Of HttpResponseMessage) = provider.GetAsync(temp)

                requestTask.Wait()
                If (requestTask.IsFaulted Or requestTask.Exception IsNot Nothing) Then
                    Return Nothing
                End If
                Dim readTask As Task(Of String) = requestTask.Result.Content.ReadAsStringAsync()
                readTask.Wait()
                ' Check response string for any error codes?
                updateXML.LoadXml(readTask.Result)
                Return updateXML
            Catch e As Exception
                Trace.TraceError(e.FormatException())
                Return Nothing
            End Try
        End Function

        Private Sub UpdateNow()
            ' Try and download patchfile
            If String.IsNullOrWhiteSpace(HQ.UpdateLocation) = False Then

                Invoke(Sub()
                           ShowUpdateForm(HQ.UpdateLocation)
                       End Sub)
            End If
        End Sub

#End Region

#Region "Eve Mail Functions"

        Public Sub UpdateEveMailButton()

            Dim strSql As String
            Dim mailData As DataSet
            Dim unreadMail As Integer = 0

            ' Get a list of the mail messages that are unread
            Try
                strSql = "SELECT COUNT(*) FROM eveMail WHERE readMail=0;"
                mailData = CustomDataFunctions.GetCustomData(strSql)
                If mailData IsNot Nothing Then
                    If mailData.Tables(0).Rows.Count > 0 Then
                        unreadMail = CInt(mailData.Tables(0).Rows(0).Item(0))
                    End If
                End If
            Catch ex As Exception
                Dim msg As String = ex.Message & ControlChars.CrLf & ControlChars.CrLf
                If ex.InnerException IsNot Nothing Then
                    msg &= ex.InnerException.Message & ControlChars.CrLf & ex.InnerException.StackTrace
                End If
                MessageBox.Show(msg, "Update EveMail Button Error!", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try

            ' Get a list of the notifications that are unread
            strSql = "SELECT COUNT(*) FROM eveNotifications WHERE readMail=0;"
            mailData = CustomDataFunctions.GetCustomData(strSql)
            Dim unreadNotices As Integer = 0
            If mailData IsNot Nothing Then
                If mailData.Tables(0).Rows.Count > 0 Then
                    unreadNotices = CInt(mailData.Tables(0).Rows(0).Item(0))
                End If
            End If

            lblEveMail.Text = "EveMail: " & unreadMail.ToString & ControlChars.CrLf & "Notices: " & unreadNotices.ToString
            btnEveMail.Tooltip = "View Mail && Notifications" & ControlChars.CrLf & "Unread: " & unreadMail.ToString &
                                 " mails, " & unreadNotices.ToString & " notifications"
        End Sub

        Private Sub UpdateMailNotifications()
            If EveMailEvents.MailIsBeingProcessed = False Then
                ThreadPool.QueueUserWorkItem(AddressOf MailUpdateThread, FrmMail.IsHandleCreated)
            End If
        End Sub

        Private Sub MailUpdateThread(ByVal mailFormOpen As Object)

            ' Set the processing flag
            EveMailEvents.MailIsBeingProcessed = True

            ' Check for the AutoMailAPI flag
            Dim requiresAutoDisable As Boolean = HQ.Settings.AutoMailAPI

            ' Disable the AutoMailAPI flag if required
            If requiresAutoDisable = True Then
                HQ.Settings.AutoMailAPI = False
            End If

            Invoke(New MethodInvoker(AddressOf UpdateMailAPILabelStart))
            EveMailEvents.MailUpdateStart()

            ' Call the main routines!
            Dim myMail As New EveMail
            Call myMail.GetMail()

            Invoke(New MethodInvoker(AddressOf UpdateMailAPILabelEnd))
            EveMailEvents.MailUpdateComplete()

            ' Update the main EveMail button
            Call UpdateEveMailButton()

            ' Set the AutoMailAPI flag if required
            If requiresAutoDisable = True Then
                HQ.Settings.AutoMailAPI = True
            End If

            EveMailEvents.MailIsBeingProcessed = False
        End Sub

        Private Sub UpdateMailAPILabelStart()
            lblMailAPITime.Text = "Processing..."
        End Sub

        Private Sub UpdateMailAPILabelEnd()
            lblMailAPITime.Text = "Updating..."
        End Sub

#End Region

        Public Shared Sub CatchUiThreadException(ByVal sender As Object, ByVal t As ThreadExceptionEventArgs)
            Trace.TraceWarning("Unhandled Exception was caught from AppDomain.")
            CatchGeneralException(t.Exception)
        End Sub

        Public Shared Sub CatchAppDomainUnhandledException(sender As Object, args As UnhandledExceptionEventArgs)
            Trace.TraceWarning("Unhandled Exception was caught from AppDomain.")
            CatchGeneralException(CType(args.ExceptionObject, Exception))
        End Sub

        Public Shared Sub CatchGeneralException(ByRef e As Exception)

            Trace.TraceError(e.FormatException())

            Using myException As New FrmException
                myException.lblVersion.Text = "Version: " & My.Application.Info.Version.ToString
                myException.lblError.Text = e.Message
                Dim trace As New StringBuilder
                trace.AppendLine(e.FormatException)
                trace.AppendLine("")
                trace.AppendLine("========== Plug-ins ==========")
                trace.AppendLine("")
                For Each myPlugIn As EveHQPlugIn In HQ.Plugins.Values
                    If myPlugIn.ShortFileName IsNot Nothing Then
                        trace.AppendLine(myPlugIn.ShortFileName & " (" & myPlugIn.Version & ")")
                    End If
                Next
                trace.AppendLine("")
                trace.AppendLine("")
                trace.AppendLine("========= System Info =========")
                trace.AppendLine("")
                trace.AppendLine("Operating System: " & Environment.OSVersion.ToString)
                trace.AppendLine(".Net Framework Version: " & Environment.Version.ToString)
                trace.AppendLine("EveHQ Location: " & HQ.AppFolder)
                trace.AppendLine("EveHQ Cache Locations: " & HQ.AppDataFolder)
                myException.txtStackTrace.Text = trace.ToString
                Dim result As Integer = myException.ShowDialog()
                If result = DialogResult.Ignore Then
                Else
                    Call FrmEveHQ.ShutdownRoutine()
                End If
            End Using
        End Sub

#Region "Ribbon Button Routines"

        Private Sub btnSave_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnSave.Click
            Call SaveEverything(False)
        End Sub

        Private Sub btnManageAPI_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnManageAPI.Click
            Using eveHQSettings As New FrmSettings
                eveHQSettings.Tag = "nodeEveAccounts"
                eveHQSettings.ShowDialog()
            End Using
        End Sub

        Private Sub btnQueryAPI_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnQueryAPI.Click
            Call QueryMyEveServer()
        End Sub

        Private Sub btnViewPilotInfo_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnViewPilotInfo.Click
            Call OpenPilotInfoForm()
        End Sub

        Private Sub btnViewSkillTraining_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnViewSkillTraining.Click
            Call OpenSkillTrainingForm()
        End Sub

        Private Sub btnViewPrices_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnViewPrices.Click
            Call OpenMarketPricesForm()
        End Sub

        Private Sub btnViewDashboard_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnViewDashboard.Click
            Call OpenDashboard()
        End Sub

        Private Sub btnEveMail_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnEveMail.Click
            Call OpenEveHQMailForm()
        End Sub

        Private Sub btnViewReqs_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnViewReqs.Click
            Call OpenRequisitions()
        End Sub

        Private Sub btnBackupEveHQ_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnBackupEveHQ.Click
            Call OpenEveHQBackUpForm()
        End Sub

        Private Sub btnBackupEve_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnBackupEve.Click
            Call OpenBackUpForm()
        End Sub

        Private Sub btnFileSettings_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnFileSettings.Click
            FrmSettings.ShowDialog()
        End Sub

        Private Sub btnFileExit_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnFileExit.Click
            Close()
        End Sub

        Private Sub btnAPIChecker_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnAPIChecker.Click
            Call OpenAPICheckerForm()
        End Sub

        Private Sub btnOpenCacheFolder_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnOpenCacheFolder.Click
            Try
                Process.Start(HQ.AppDataFolder)
            Catch ex As Exception
                MessageBox.Show("Unable to start Windows Explorer: " & ex.Message, "Error Starting External Process",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Try
        End Sub

        Private Sub btnClearCharacterCache_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnClearCharacterCache.Click
            Const Msg As String = "This will delete the character specific XML files, and get fresh data from the API." &
                                  ControlChars.CrLf & ControlChars.CrLf & "Are you sure you wish to continue?"
            Dim reply As Integer = MessageBox.Show(Msg, "Confirm Delete Cache", MessageBoxButtons.YesNo,
                                                   MessageBoxIcon.Question)
            If reply = DialogResult.Yes Then
                Try
                    ' Close all open forms
                    If tabEveHQMDI.Tabs.Count > 0 Then
                        For tab As Integer = tabEveHQMDI.Tabs.Count - 1 To 0
                            Dim tp As TabItem = tabEveHQMDI.Tabs(tab)
                            tabEveHQMDI.Tabs.Remove(tp)
                        Next
                    End If

                    ' Clear the character XML files
                    Try
                        For Each charFile As String In _
                            My.Computer.FileSystem.GetFiles(HQ.ApiCacheFolder, FileIO.SearchOption.SearchTopLevelOnly,
                                                            "EVEHQAPI_" & APITypes.CharacterSheet.ToString & "*")
                            My.Computer.FileSystem.DeleteFile(charFile)
                        Next
                    Catch ex As Exception
                    End Try

                    ' Clear the skill training XML files
                    Try
                        For Each charFile As String In _
                            My.Computer.FileSystem.GetFiles(HQ.ApiCacheFolder, FileIO.SearchOption.SearchTopLevelOnly,
                                                            "EVEHQAPI_" & APITypes.SkillTraining.ToString & "*")
                            My.Computer.FileSystem.DeleteFile(charFile)
                        Next
                    Catch ex As Exception
                    End Try

                    ' Clear the skill queue XML files
                    Try
                        For Each charFile As String In _
                            My.Computer.FileSystem.GetFiles(HQ.ApiCacheFolder, FileIO.SearchOption.SearchTopLevelOnly,
                                                            "EVEHQAPI_" & APITypes.SkillQueue.ToString & "*")
                            My.Computer.FileSystem.DeleteFile(charFile)
                        Next
                    Catch ex As Exception
                    End Try


                    '' Clear the EveHQ Pilot Data
                    Try
                        ' Clear the current list of pilots
                        HQ.TempPilots.Clear()
                        HQ.TempCorps.Clear()
                        HQ.APIResults.Clear()
                    Catch ex As Exception
                    End Try

                    '' Update the pilot lists
                    Call UpdatePilotInfo()

                    ' Restart the timer
                    tmrSkillUpdate.Start()

                    ' Call the API
                    Call QueryMyEveServer()

                Catch ex As Exception
                    MessageBox.Show(
                        "Error Deleting the EveHQ Cache Folder, please try to delete the following location manually: " &
                        ControlChars.CrLf & ControlChars.CrLf & HQ.ApiCacheFolder, "Error Deleting Cache", MessageBoxButtons.OK,
                        MessageBoxIcon.Information)
                    Trace.TraceError(ex.FormatException())
                End Try
            End If
        End Sub

        Private Sub btnClearImageCache_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnClearImageCache.Click
            Const Msg As String = "This will delete the entire contents of the image cache folder." & ControlChars.CrLf &
                                  ControlChars.CrLf & "Are you sure you wish to continue?"
            Dim reply As Integer = MessageBox.Show(Msg, "Confirm Delete Image Cache", MessageBoxButtons.YesNo,
                                                   MessageBoxIcon.Question)
            If reply = DialogResult.Yes Then
                Try
                    ' Clear the EveHQ image cache
                    Try
                        If My.Computer.FileSystem.DirectoryExists(HQ.ImageCacheFolder) Then
                            My.Computer.FileSystem.DeleteDirectory(HQ.ImageCacheFolder,
                                                                   DeleteDirectoryOption.DeleteAllContents)
                        End If
                    Catch ex As Exception
                    End Try

                    ' Recreate the EveHQ image cache folder
                    Try
                        If My.Computer.FileSystem.DirectoryExists(HQ.ImageCacheFolder) = False Then
                            My.Computer.FileSystem.CreateDirectory(HQ.ImageCacheFolder)
                        End If
                    Catch ex As Exception
                    End Try

                Catch ex As Exception
                    MessageBox.Show(
                        "Error Deleting the EveHQ Image Cache Folder, please try to delete the following location manually: " &
                        ControlChars.CrLf & ControlChars.CrLf & HQ.ImageCacheFolder, "Error Deleting Cache",
                        MessageBoxButtons.OK, MessageBoxIcon.Information)
                End Try
            End If
        End Sub

        Private Sub btnClearAllCache_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnClearAllCache.Click
            Const Msg As String = "This will delete the entire contents of the cache folder, clear the pilot data and reconnect to the API." &
                                  ControlChars.CrLf & ControlChars.CrLf & "Are you sure you wish to continue?"
            Dim reply As Integer = MessageBox.Show(Msg, "Confirm Delete Cache", MessageBoxButtons.YesNo,
                                                   MessageBoxIcon.Question)
            If reply = DialogResult.Yes Then
                Try
                    ' Close all open forms
                    If tabEveHQMDI.Tabs.Count > 0 Then
                        For tab As Integer = tabEveHQMDI.Tabs.Count - 1 To 0
                            Dim tp As TabItem = tabEveHQMDI.Tabs(tab)
                            tabEveHQMDI.Tabs.Remove(tp)
                        Next
                    End If

                    ' Clear the EveHQ API cache
                    Try
                        If My.Computer.FileSystem.DirectoryExists(HQ.ApiCacheFolder) Then
                            My.Computer.FileSystem.DeleteDirectory(HQ.ApiCacheFolder, DeleteDirectoryOption.DeleteAllContents)
                        End If
                    Catch ex As Exception
                    End Try

                    ' Recreate the EveHQ API cache folder
                    Try
                        If My.Computer.FileSystem.DirectoryExists(HQ.ApiCacheFolder) = False Then
                            My.Computer.FileSystem.CreateDirectory(HQ.ApiCacheFolder)
                        End If
                    Catch ex As Exception
                    End Try


                    '' Clear the EveHQ Pilot Data
                    Try
                        ' Clear the current list of pilots
                        HQ.TempPilots.Clear()
                        HQ.TempCorps.Clear()
                        HQ.APIResults.Clear()
                    Catch ex As Exception
                    End Try

                    '' Update the pilot lists
                    Call UpdatePilotInfo()

                    ' Restart the timer
                    tmrSkillUpdate.Start()

                    ' Call the API
                    Call QueryMyEveServer()

                Catch ex As Exception
                    MessageBox.Show(
                        "Error Deleting the EveHQ Cache Folder, please try to delete the following location manually: " &
                        ControlChars.CrLf & ControlChars.CrLf & HQ.ApiCacheFolder, "Error Deleting Cache", MessageBoxButtons.OK,
                        MessageBoxIcon.Information)
                End Try
            End If
        End Sub

        Private Sub btnCheckForUpdates_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnCheckForUpdates.Click
            Call CheckForUpdates(True)
        End Sub

        Private Sub btnUpdateEveHQ_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnUpdateEveHQ.Click
            Call UpdateNow()
            Close()
        End Sub

        Private Sub btnViewHistory_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnViewHistory.Click
            Try
                Process.Start("http://evehq.co/files")
            Catch ex As Exception
                ' Guess the user needs to reset the http protocol in the OS - not much EveHQ can do here!
            End Try
        End Sub

        Private Sub btnAbout_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnAbout.Click
            Using aboutForm As New FrmAbout
                aboutForm.ShowDialog()
            End Using
        End Sub

        Private Sub btnSQLQueryTool_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnSQLQueryTool.Click
            Call OpenSQLQueryForm()
        End Sub

        Private Sub btnInfoHelp_Click(sender As Object, e As EventArgs) Handles btnInfoHelp.Click
            Call OpenInfoHelpForm()
        End Sub

#Region "Ribbon Report Functions"

#Region "Report Options Routines"

        Private Sub UpdateReportPilots()
            cboReportPilot.Items.Clear()
            For Each rPilot As EveHQPilot In HQ.Settings.Pilots.Values
                If rPilot.Active = True Then
                    cboReportPilot.Items.Add(rPilot.Name)
                End If
            Next
            If cboReportPilot.Items.Count > 0 Then
                If cboReportPilot.Items.Contains(HQ.Settings.StartupPilot) = True Then
                    cboReportPilot.SelectedItem = HQ.Settings.StartupPilot
                Else
                    cboReportPilot.SelectedIndex = 0
                End If
            End If
        End Sub

        Private Sub cboReportPilot_SelectedIndexChaged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles cboReportPilot.SelectedIndexChanged
            Call BuildQueueReportsMenu()
        End Sub

        Private Sub cboReportFormat_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles cboReportFormat.SelectedIndexChanged
            ' Get the name of the selected item
            Dim selItem As String = cboReportFormat.SelectedItem.ToString
            ' Cycle through the ribbon bars to hide the non-applicable ones
            For Each rb As RibbonBar In rpReports.Controls
                If rb.Name = "rb" & selItem Or rb.Name = "rbReportOptions" Or rb.Name = "rbStandard" Then
                    rb.Visible = True
                Else
                    rb.Visible = False
                End If
            Next
            rpReports.Refresh()
        End Sub

        Private Sub btnOpenReportFolder_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnOpenReportFolder.Click
            Try
                Process.Start(HQ.ReportFolder)
            Catch ex As Exception
                MessageBox.Show("Unable to start Windows Explorer: " & ex.Message, "Error Starting External Process",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Try
        End Sub

        Public Sub BuildQueueReportsMenu()
            ' Clear option for btnHTMLTrainingQueue
            For Each queueBtn As ButtonItem In btnHTMLTrainingQueue.SubItems
                RemoveHandler queueBtn.Click, AddressOf ReportsMenuHandler
            Next
            ' Clear option for btnHTMLQueueShoppingList
            For Each queueBtn As ButtonItem In btnHTMLQueueShoppingList.SubItems
                RemoveHandler queueBtn.Click, AddressOf ReportsMenuHandler
            Next
            ' Clear option for btnTextTrainingQueue
            For Each queueBtn As ButtonItem In btnTextTrainingQueue.SubItems
                RemoveHandler queueBtn.Click, AddressOf ReportsMenuHandler
            Next
            ' Clear option for btnTextQueueShoppingList
            For Each queueBtn As ButtonItem In btnTextQueueShoppingList.SubItems
                RemoveHandler queueBtn.Click, AddressOf ReportsMenuHandler
            Next
            ' Clear the existing options
            btnHTMLTrainingQueue.SubItems.Clear()
            btnHTMLQueueShoppingList.SubItems.Clear()
            btnTextTrainingQueue.SubItems.Clear()
            btnTextQueueShoppingList.SubItems.Clear()
            ' Rebuild the queue and shopping list options based on the pilot
            If cboReportPilot.SelectedItem IsNot Nothing Then
                If HQ.Settings.Pilots.ContainsKey(cboReportPilot.SelectedItem.ToString) Then
                    Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
                    If rPilot IsNot Nothing Then
                        If rPilot.TrainingQueues IsNot Nothing Then
                            For Each qItem As EveHQSkillQueue In rPilot.TrainingQueues.Values
                                Dim queueBtn As New ButtonItem
                                queueBtn.CanCustomize = False
                                queueBtn.Text = qItem.Name
                                queueBtn.Name = qItem.Name
                                queueBtn.Image = My.Resources.SkillBook16
                                AddHandler queueBtn.Click, AddressOf ReportsMenuHandler
                                btnHTMLTrainingQueue.SubItems.Add(queueBtn)
                                queueBtn = New ButtonItem
                                queueBtn.CanCustomize = False
                                queueBtn.Text = qItem.Name
                                queueBtn.Name = qItem.Name
                                queueBtn.Image = My.Resources.SkillBook16
                                AddHandler queueBtn.Click, AddressOf ReportsMenuHandler
                                btnHTMLQueueShoppingList.SubItems.Add(queueBtn)
                                queueBtn = New ButtonItem
                                queueBtn.CanCustomize = False
                                queueBtn.Text = qItem.Name
                                queueBtn.Name = qItem.Name
                                queueBtn.Image = My.Resources.SkillBook16
                                AddHandler queueBtn.Click, AddressOf ReportsMenuHandler
                                btnTextTrainingQueue.SubItems.Add(queueBtn)
                                queueBtn = New ButtonItem
                                queueBtn.CanCustomize = False
                                queueBtn.Text = qItem.Name
                                queueBtn.Name = qItem.Name
                                queueBtn.Image = My.Resources.SkillBook16
                                AddHandler queueBtn.Click, AddressOf ReportsMenuHandler
                                btnTextQueueShoppingList.SubItems.Add(queueBtn)
                            Next
                        End If
                    End If
                End If
            End If
        End Sub

        Private Sub ReportsMenuHandler(ByVal sender As Object, ByVal e As EventArgs)
            ' Identify queue name
            Dim queueBtn As ButtonItem = CType(sender, ButtonItem)
            Dim queueName As String = queueBtn.Text
            ' Find parent button name
            Dim reportType As String = queueBtn.Parent.Name
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            ' Setup report details
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Dim rQueue As EveHQSkillQueue = rPilot.TrainingQueues(queueName)
            Select Case reportType
                Case "btnHTMLTrainingQueue"
                    Call Reports.GenerateTrainQueue(rPilot, rQueue)
                    newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder,
                                                             "TrainQueue - " & rQueue.Name & " (" & rPilot.Name & ").html"))
                    DisplayReport(newReport, "Training Queue - " & rPilot.Name & " (" & rQueue.Name & ")")
                Case "btnHTMLQueueShoppingList"
                    Call Reports.GenerateShoppingList(rPilot, rQueue)
                    newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder,
                                                             "ShoppingList - " & rQueue.Name & " (" & rPilot.Name & ").html"))
                    DisplayReport(newReport, "Shopping List - " & rPilot.Name & " (" & rQueue.Name & ")")
                Case "btnTextTrainingQueue"
                    Call Reports.GenerateTextTrainQueue(rPilot, rQueue)
                    newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder,
                                                             "TrainQueue - " & rQueue.Name & " (" & rPilot.Name & ").txt"))
                    DisplayReport(newReport, "Training Queue - " & rPilot.Name & " (" & rQueue.Name & ")")
                Case "btnTextQueueShoppingList"
                    Call Reports.GenerateTextShoppingList(rPilot, rQueue)
                    newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder,
                                                             "ShoppingList - " & rQueue.Name & " (" & rPilot.Name & ").txt"))
                    DisplayReport(newReport, "Shopping List - " & rPilot.Name & " (" & rQueue.Name & ")")
            End Select
        End Sub

#End Region

#Region "Standard Reports"

        Private Sub btnStdCharSummary_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnStdCharSummary.Click
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateCharSummary()
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "PilotSummary.html"))
            DisplayReport(newReport, "Pilot Summary")
        End Sub

        Private Sub btnStdSkillLevels_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnStdSkillLevels.Click
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateSPSummary()
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "SPSummary.html"))
            DisplayReport(newReport, "Skill Point Summary")
        End Sub

        Private Sub btnStdAlloyReport_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnStdAlloyReport.Click
            'Dim newReport As New frmReportViewer
            'Call Reports.GenerateAlloyReport()
            'newReport.wbReport.Navigate(Path.Combine(HQ.reportFolder, "AlloyReport.html"))
            'DisplayReport(newReport, "Alloy Composition")
        End Sub

        Private Sub btnStdAsteroidReport_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnStdAsteroidReport.Click
            'Dim newReport As New frmReportViewer
            'Call Reports.GenerateRockReport()
            'newReport.wbReport.Navigate(Path.Combine(HQ.reportFolder, "OreReport.html"))
            'DisplayReport(newReport, "Asteroid Composition")
        End Sub

        Private Sub btnStdIceReport_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnStdIceReport.Click
            'Dim newReport As New frmReportViewer
            'Call Reports.GenerateIceReport()
            'newReport.wbReport.Navigate(Path.Combine(HQ.reportFolder, "IceReport.html"))
            'DisplayReport(newReport, "Ice Composition")
        End Sub

#End Region

#Region "HTML Reports"

        Private Sub btnHTMLCharSheet_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnHTMLCharSheet.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateCharSheet(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "CharSheet (" & rPilot.Name & ").html"))
            DisplayReport(newReport, "Character Sheet - " & rPilot.Name)
        End Sub

        Private Sub btnHTMLTrainingTimes_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnHTMLTrainingTimes.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateTrainingTime(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "TrainTime (" & rPilot.Name & ").html"))
            DisplayReport(newReport, "Training Times - " & rPilot.Name)
        End Sub

        Private Sub btnHTMLTimeToLvl5_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnHTMLTimeToLvl5.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateTimeToLevel5(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "TimeToLevel5 (" & rPilot.Name & ").html"))
            DisplayReport(newReport, "Time To Level 5 - " & rPilot.Name)
        End Sub

        Private Sub btnHTMLSkillLevels_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnHTMLSkillLevels.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateSkillLevels(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "SkillLevels (" & rPilot.Name & ").html"))
            DisplayReport(newReport, "Skill Levels - " & rPilot.Name)
        End Sub

        Private Sub btnHTMLSkillRanks_Click(sender As Object, e As EventArgs) Handles btnHTMLSkillRanks.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateSkillRanks(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "SkillRanks (" & rPilot.Name & ").html"))
            DisplayReport(newReport, "Skill Ranks - " & rPilot.Name)
        End Sub

        Private Sub btnHTMLSkillsAvailable_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnHTMLSkillsAvailable.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateSkillsAvailable(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "SkillsToTrain (" & rPilot.Name & ").html"))
            DisplayReport(newReport, "Skills Available to Train - " & rPilot.Name)
        End Sub

        Private Sub btnHTMLSkillsNotTrained_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnHTMLSkillsNotTrained.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateSkillsNotTrained(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "SkillsNotTrained (" & rPilot.Name & ").html"))
            DisplayReport(newReport, "Skills Not Trained - " & rPilot.Name)
        End Sub

        Private Sub btnHTMLPartiallyTrained_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnHTMLPartiallyTrained.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GeneratePartialSkills(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "PartialSkills (" & rPilot.Name & ").html"))
            DisplayReport(newReport, "Partially Trained Skills - " & rPilot.Name)
        End Sub

        Private Sub btnHTMLSkillsCost_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnHTMLSkillsCost.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateSkillsCost(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "SkillsCost (" & rPilot.Name & ").html"))
            DisplayReport(newReport, "Skills Cost - " & rPilot.Name)
        End Sub

        Private Sub btnHTMLCertGrades1_Click(sender As Object, e As EventArgs) Handles btnHTMLCertGrades1.Click
            Const Grade As Integer = 1
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateCertGradeTimes(rPilot, Grade)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, [Enum].GetName(GetType(CertificateGrade), Grade) & "CertGradeTimes (" & rPilot.Name & ").html"))
            DisplayReport(newReport, [Enum].GetName(GetType(CertificateGrade), Grade) & " Certificate Grade Times - " & rPilot.Name)
        End Sub

        Private Sub btnHTMLCertGrades2_Click(sender As Object, e As EventArgs) Handles btnHTMLCertGrades2.Click
            Const Grade As Integer = 2
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateCertGradeTimes(rPilot, Grade)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, [Enum].GetName(GetType(CertificateGrade), Grade) & "CertGradeTimes (" & rPilot.Name & ").html"))
            DisplayReport(newReport, [Enum].GetName(GetType(CertificateGrade), Grade) & " Certificate Grade Times - " & rPilot.Name)
        End Sub

        Private Sub btnHTMLCertGrades3_Click(sender As Object, e As EventArgs) Handles btnHTMLCertGrades3.Click
            Const Grade As Integer = 3
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateCertGradeTimes(rPilot, Grade)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, [Enum].GetName(GetType(CertificateGrade), Grade) & "CertGradeTimes (" & rPilot.Name & ").html"))
            DisplayReport(newReport, [Enum].GetName(GetType(CertificateGrade), Grade) & " Certificate Grade Times - " & rPilot.Name)
        End Sub

        Private Sub btnHTMLCertGrades4_Click(sender As Object, e As EventArgs) Handles btnHTMLCertGrades4.Click
            Const Grade As Integer = 4
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateCertGradeTimes(rPilot, Grade)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, [Enum].GetName(GetType(CertificateGrade), Grade) & "CertGradeTimes (" & rPilot.Name & ").html"))
            DisplayReport(newReport, [Enum].GetName(GetType(CertificateGrade), Grade) & " Certificate Grade Times - " & rPilot.Name)
        End Sub

        Private Sub btnHTMLCertGrades5_Click(sender As Object, e As EventArgs) Handles btnHTMLCertGrades5.Click
            Const Grade As Integer = 5
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateCertGradeTimes(rPilot, Grade)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, [Enum].GetName(GetType(CertificateGrade), Grade) & "CertGradeTimes (" & rPilot.Name & ").html"))
            DisplayReport(newReport, [Enum].GetName(GetType(CertificateGrade), Grade) & " Certificate Grade Times - " & rPilot.Name)
        End Sub

#End Region

#Region "Text Reports"

        Private Sub btnTextCharacterSheet_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnTextCharacterSheet.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateTextCharSheet(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "CharSheet (" & rPilot.Name & ").txt"))
            DisplayReport(newReport, "Character Sheet - " & rPilot.Name)
        End Sub

        Private Sub btnTextTrainingTimes_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnTextTrainingTimes.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateTextTrainingTime(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "TrainTime (" & rPilot.Name & ").txt"))
            DisplayReport(newReport, "Training Times - " & rPilot.Name)
        End Sub

        Private Sub btnTextTimeToLvl5_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnTextTimeToLvl5.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateTextTimeToLevel5(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "TimeToLevel5 (" & rPilot.Name & ").txt"))
            DisplayReport(newReport, "Time To Level 5 - " & rPilot.Name)
        End Sub

        Private Sub btnTextSkillLevels_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnTextSkillLevels.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateTextSkillLevels(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "SkillLevels (" & rPilot.Name & ").txt"))
            DisplayReport(newReport, "Skill Levels - " & rPilot.Name)
        End Sub

        Private Sub btnTextSkillRanks_Click(sender As Object, e As EventArgs) Handles btnTextSkillRanks.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateTextSkillRanks(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "SkillRanks (" & rPilot.Name & ").txt"))
            DisplayReport(newReport, "Skill Ranks - " & rPilot.Name)
        End Sub

        Private Sub btnTextSkillsAvailable_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnTextSkillsAvailable.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateTextSkillsAvailable(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "SkillsToTrain (" & rPilot.Name & ").txt"))
            DisplayReport(newReport, "Skills Available to Train - " & rPilot.Name)
        End Sub

        Private Sub btnTextSkillsNotTrained_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnTextSkillsNotTrained.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateTextSkillsNotTrained(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "SkillsNotTrained (" & rPilot.Name & ").txt"))
            DisplayReport(newReport, "Skills Not Trained - " & rPilot.Name)
        End Sub

        Private Sub btnTextPartiallyTrained_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnTextPartiallyTrained.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateTextPartialSkills(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "PartialSkills (" & rPilot.Name & ").txt"))
            DisplayReport(newReport, "Partially Trained Skills - " & rPilot.Name)
        End Sub

        Private Sub btnTextSkillsCost_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnTextSkillsCost.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateTextSkillsCost(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "SkillsCost (" & rPilot.Name & ").txt"))
            DisplayReport(newReport, "Skills Cost - " & rPilot.Name)
        End Sub

#End Region

#Region "XML Reports"

        Private Sub btnXMLCharacterXML_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnXMLCharacterXML.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateCharXML(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "CharXML (" & rPilot.Name & ").xml"))
            DisplayReport(newReport, "Imported Character XML - " & rPilot.Name)
        End Sub

        Private Sub btnXMLTrainingXML_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnXMLTrainingXML.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateTrainXML(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "TrainingXML (" & rPilot.Name & ").xml"))
            DisplayReport(newReport, "Imported Training XML - " & rPilot.Name)
        End Sub

        Private Sub btnXMLCurrentCharOld_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnXMLCurrentCharOld.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateCurrentPilotXML_Old(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "CurrentXML - Old (" & rPilot.Name & ").xml"))
            DisplayReport(newReport, "Old Style Character XML - " & rPilot.Name)
        End Sub

        Private Sub btnXMLCurrentCharNew_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnXMLCurrentCharNew.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateCurrentPilotXML_New(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "CurrentXML - New (" & rPilot.Name & ").xml"))
            DisplayReport(newReport, "Current Character XML - " & rPilot.Name)
        End Sub

        Private Sub btnXMLCurrentTrainingOld_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnXMLCurrentTrainingOld.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GenerateCurrentTrainingXML_Old(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "TrainingXML - Old (" & rPilot.Name & ").xml"))
            DisplayReport(newReport, "Old Style Training XML - " & rPilot.Name)
        End Sub

        Private Sub btnXMLECMExport_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnXMLECMExport.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Call Reports.GenerateECMExportReports(rPilot)
        End Sub

#End Region

#Region "PHPBB Reports"

        Private Sub btnPHPBBCharacterSheet_Click(ByVal sender As Object, ByVal e As EventArgs) _
            Handles btnPHPBBCharacterSheet.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newReport As New FrmReportViewer
            Call Reports.GeneratePHPBBCharSheet(rPilot)
            newReport.wbReport.Navigate(Path.Combine(HQ.ReportFolder, "PHPBBCharSheet (" & rPilot.Name & ").txt"))
            DisplayReport(newReport, "PHPBB Character Sheet - " & rPilot.Name)
        End Sub

#End Region

#Region "Chart Reports"

        Private Sub btnChartSkillGroup_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnChartSkillGroup.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newChartForm As New FrmChartViewer
            Call Reports.SkillGroupChart(rPilot, newChartForm.Chart1)
            Call DisplayChartReport(newChartForm, "Skill Group Chart - " & rPilot.Name)
        End Sub

        Private Sub btnChartSkillCost_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnChartSkillCost.Click
            If cboReportPilot.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a pilot before running this report!", "Pilot Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
            Dim rPilot As EveHQPilot = HQ.Settings.Pilots(cboReportPilot.SelectedItem.ToString)
            Dim newChartForm As New FrmChartViewer
            Call Reports.SkillCostChart(rPilot, newChartForm.Chart1)
            Call DisplayChartReport(newChartForm, "Skill Cost Chart - " & rPilot.Name)
        End Sub

#End Region

#End Region

#End Region

#Region "Training Bar Routines"

        Private Sub Bar1_BarDock(ByVal sender As Object, ByVal e As EventArgs) Handles Bar1.BarDock
            HQ.Settings.TrainingBarDockPosition = Bar1.DockSide
            Select Case Bar1.DockSide
                Case eDockSide.Top, eDockSide.Bottom
                    'DockContainerItem1.Height = 75
                    trainingBlockLayout.FlowDirection = FlowDirection.LeftToRight
                Case eDockSide.Left, eDockSide.Right
                    'DockContainerItem1.Width = 320
                    trainingBlockLayout.FlowDirection = FlowDirection.TopDown
            End Select
            Call SetupTrainingStatus()
        End Sub

        Private Sub Bar1_BarUndock(ByVal sender As Object, ByVal e As EventArgs) Handles Bar1.BarUndock
            HQ.Settings.TrainingBarDockPosition = Bar1.DockSide
            trainingBlockLayout.FlowDirection = FlowDirection.LeftToRight
            Call SetupTrainingStatus()
        End Sub

        Private Sub Bar1_SizeChanged(ByVal sender As Object, ByVal e As EventArgs) Handles Bar1.SizeChanged
            If _appStartUp = False And _saveTrainingBarSize = True Then
                HQ.Settings.TrainingBarHeight = DockContainerItem1.Height + 3
                HQ.Settings.TrainingBarWidth = DockContainerItem1.Width
            End If

            ' tailor trainging block layout display to best suit the experience
            If Bar1.Docked And (Bar1.DockSide = eDockSide.Bottom Or Bar1.DockSide = eDockSide.Top) And Bar1.Height < 120 Then
                trainingBlockLayout.Dock = DockStyle.None
            ElseIf Bar1.Docked And (Bar1.DockSide = eDockSide.Left Or Bar1.DockSide = eDockSide.Right) And Bar1.Width < 400 Then
                trainingBlockLayout.Dock = DockStyle.None
            Else
                trainingBlockLayout.Dock = DockStyle.Fill
            End If


        End Sub

#End Region

#Region "Theme Modification and Automatic Color Scheme creation based on the selected color table"

        Private _mColorSelected As Boolean = False

        Private Sub buttonStyleCustom_ExpandChange(ByVal sender As Object, ByVal e As EventArgs) Handles btnCustomTheme.ExpandChange
            If btnCustomTheme.Expanded Then
                ' Remember the starting color scheme to apply if no color is selected during live-preview
                _mColorSelected = False
            Else
                If Not _mColorSelected Then
                    UpdateTint(HQ.Settings.ThemeTint)
                    UpdateTheme(HQ.Settings.ThemeStyle, HQ.Settings.ThemeTint)
                End If
            End If
        End Sub

        Private Sub btnCanvasColor_ExpandChange(sender As Object, e As EventArgs) Handles btnCanvasColor.ExpandChange
            If btnCanvasColor.Expanded Then
            Else
                UpdateCanvas(HQ.Settings.ThemeCanvas)
            End If
        End Sub

        Private Sub buttonStyleCustom_ColorPreview(ByVal sender As Object, ByVal e As ColorPreviewEventArgs) Handles btnCustomTheme.ColorPreview
            Try
                UpdateTint(e.Color)
            Catch ex As Exception
            End Try
        End Sub

        Private Sub buttonStyleCustom_SelectedColorChanged(ByVal sender As Object, ByVal e As EventArgs) Handles btnCustomTheme.SelectedColorChanged
            _mColorSelected = True
            ' Indicate that color was selected for buttonStyleCustom_ExpandChange method
            btnCustomTheme.CommandParameter = btnCustomTheme.SelectedColor
        End Sub

        Private Sub btnCanvasColor_ColorPreview(sender As Object, e As DevComponents.DotNetBar.ColorPreviewEventArgs) Handles btnCanvasColor.ColorPreview
            Try
                UpdateCanvas(e.Color)
            Catch ex As Exception
            End Try
        End Sub

        Private Sub btnCanvasColor_SelectedColorChanged(sender As System.Object, e As EventArgs) Handles btnCanvasColor.SelectedColorChanged
            _mColorSelected = True
            ' Indicate that color was selected for buttonStyleCustom_ExpandChange method
            btnCanvasColor.CommandParameter = btnCanvasColor.SelectedColor
        End Sub

        Private Sub AppCommandTheme_Executed(ByVal sender As Object, ByVal e As EventArgs) Handles AppCommandTheme.Executed
            Dim source As ICommandSource = CType(sender, ICommandSource)
            If typeof(source.CommandParameter) Is String Then
                Dim cs As eStyle = CType([Enum].Parse(GetType(eStyle), source.CommandParameter.ToString()), eStyle)
                ' This is all that is needed to change the color table for all controls on the form
                UpdateTheme(cs, Color.Empty)
                HQ.Settings.ThemeStyle = cs
                HQ.Settings.ThemeSetByUser = True
                UpdateTint(Color.Empty)
                If StyleManager.IsMetro(StyleManager.Style) Then
                    btnCanvasColor.Enabled = True
                    StyleManager.MetroColorGeneratorParameters = New DevComponents.DotNetBar.Metro.ColorTables.MetroColorGeneratorParameters(HQ.Settings.ThemeCanvas, HQ.Settings.ThemeTint)
                    HQ.Settings.ThemeCanvas = StyleManager.MetroColorGeneratorParameters.CanvasColor
                    HQ.Settings.ThemeTint = StyleManager.MetroColorGeneratorParameters.BaseColor
                Else
                    btnCanvasColor.Enabled = False
                    HQ.Settings.ThemeTint = Color.Empty
                End If
            ElseIf typeof(source.CommandParameter) Is Color Then
                Dim tint As Color = CType(source.CommandParameter, Color)
                If CType(source, ColorPickerDropDown).Text = "Canvas Color" Then
                    ' Updating then metro canvas color
                    HQ.Settings.ThemeCanvas = tint
                    HQ.Settings.ThemeSetByUser = True
                    StyleManager.MetroColorGeneratorParameters = New DevComponents.DotNetBar.Metro.ColorTables.MetroColorGeneratorParameters(HQ.Settings.ThemeCanvas, HQ.Settings.ThemeTint)
                Else
                    ' Updating the theme tint or the metro base color
                    If StyleManager.IsMetro(StyleManager.Style) Then
                        HQ.Settings.ThemeTint = tint
                        HQ.Settings.ThemeSetByUser = True
                        StyleManager.MetroColorGeneratorParameters = New DevComponents.DotNetBar.Metro.ColorTables.MetroColorGeneratorParameters(HQ.Settings.ThemeCanvas, HQ.Settings.ThemeTint)
                    Else
                        UpdateTint(tint)
                        HQ.Settings.ThemeTint = StyleManager.ColorTint
                        HQ.Settings.ThemeSetByUser = True
                    End If
                End If
            End If
            Invalidate()
        End Sub

#End Region

        Private Sub RibbonControl1_ExpandedChanged(ByVal sender As Object, ByVal e As EventArgs) _
            Handles RibbonControl1.ExpandedChanged
            HQ.Settings.RibbonMinimised = Not RibbonControl1.Expanded
        End Sub

        Private Sub DisplayChildForm(ByVal childForm As Form)
            If ActiveMdiChild IsNot Nothing Then
                If ActiveMdiChild.WindowState = FormWindowState.Maximized Then
                    ActiveMdiChild.WindowState = FormWindowState.Normal
                End If
            End If
            childForm.MdiParent = Me
            childForm.WindowState = FormWindowState.Maximized
            childForm.Show()
        End Sub

        Private Sub btnIB_Click(sender As Object, e As EventArgs) Handles btnIB.Click
            Using newIB As New FrmIB
                newIB.ShowDialog()
            End Using
        End Sub

        Private Sub GetServerMessage()
            ' Download the message from the server
            Dim msgXML As XmlDocument = FetchMessageXML()
            Try
                If msgXML IsNot Nothing Then
                    Dim newMessage As New EveHQMessage
                    Dim data As XmlNodeList = msgXML.SelectNodes("/eveHQMessage")
                    newMessage.MessageDate = DateTime.Parse(data(0).ChildNodes(0).InnerText, New CultureInfo("en-gb"))
                    newMessage.MessageTitle = data(0).ChildNodes(1).InnerText
                    newMessage.AllowIgnore = CBool(data(0).ChildNodes(2).InnerText)
                    newMessage.Message = data(0).ChildNodes(3).InnerText
                    If data(0).ChildNodes(4).ChildNodes.Count > 0 Then
                        newMessage.DisabledPlugins.Clear()
                        For Each disabledPlugin As XmlNode In data(0).ChildNodes(4).ChildNodes
                            newMessage.DisabledPlugins.Add(disabledPlugin.Attributes.GetNamedItem("name").Value, disabledPlugin.Attributes.GetNamedItem("version").Value)
                        Next
                    End If
                    HQ.EveHQServerMessage = newMessage
                Else
                    HQ.EveHQServerMessage = Nothing
                End If
            Catch e As Exception
                HQ.EveHQServerMessage = Nothing
            End Try
            HQ.WriteLogEvent(" *** Message Finished Loading")
        End Sub

        Private Function FetchMessageXML() As XmlDocument
            ' Set a default policy level for the "http:" and "https" schemes.
            Dim policy As HttpRequestCachePolicy = New HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore)
            Dim updateServer As String = HQ.Settings.UpdateUrl
            Dim remoteURL As String = updateServer & "_message.xml"
            Dim webdata As String
            Dim updateXML As New XmlDocument
            Try
                ' Create the requester
                ServicePointManager.DefaultConnectionLimit = 10
                ServicePointManager.Expect100Continue = False
                ServicePointManager.FindServicePoint(New Uri(remoteURL))
                Dim request As HttpWebRequest = CType(WebRequest.Create(remoteURL), HttpWebRequest)
                request.UserAgent = "EveHQ " & My.Application.Info.Version.ToString
                request.CachePolicy = policy
                request.Timeout = 10000 ' timeout set to 10s
                ' Setup proxy server (if required)
                Call ProxyServerFunctions.SetupWebProxy(request)
                ' Prepare for a response from the server
                Dim response As HttpWebResponse = CType(request.GetResponse(), HttpWebResponse)
                ' Get the stream associated with the response.
                Dim receiveStream As Stream = response.GetResponseStream()
                ' Pipes the stream to a higher level stream reader with the required encoding format.
                Dim readStream As New StreamReader(receiveStream, Encoding.UTF8)
                webdata = readStream.ReadToEnd()
                ' Check response string for any error codes?
                updateXML.LoadXml(webdata)
                Return updateXML
            Catch e As Exception
                Return Nothing
            End Try
        End Function


    End Class
End Namespace