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

Imports System.IO
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Windows.Forms
Imports System.ComponentModel
Imports EveHQ.EveData
Imports EveHQ.Prism.Classes
Imports EveHQ.Prism.BPCalc
Imports Newtonsoft.Json
Imports System.Xml
Imports System.Globalization
Imports EveHQ.Core
Imports EveHQ.Prism
Imports EveHQ.HQF

''' <summary>
''' Converts the old settings format into the new one
''' </summary>
''' <remarks></remarks>
Public Class FileConverter

    Private ReadOnly _settingsFolder As String
    Private _newSettings As EveHQSettings

    Private ReadOnly _worker As BackgroundWorker

    Public Sub New(worker As BackgroundWorker, settingsFolder As String)
        _worker = worker
        _settingsFolder = settingsFolder
        _newSettings = New EveHQSettings
    End Sub

#Region "Public Methods"

    Public Sub Convert()

        ' Convert the Core settings
        ConvertCoreSettings(_settingsFolder)

        ' Convert the Prism settings
        ConvertPrismSettings(_settingsFolder)

        ' Convert the HQF settings
        ConvertHQFSettings(_settingsFolder)

    End Sub

#End Region

#Region "Core Data/Settings Conversion Methods"

    Private Sub ConvertCoreSettings(settingsFolder As String)

        Dim oldSettings As New EveSettings
        _newSettings = New EveHQSettings

        ' Load in the old EveHQ Settings
        _worker.ReportProgress(0, "Attempting to load original EveHQ Core Settings file...")
        If My.Computer.FileSystem.FileExists(Path.Combine(settingsFolder, "EveHQSettings.bin")) = True Then
            Try
                Using s As New FileStream(Path.Combine(settingsFolder, "EveHQSettings.bin"), FileMode.Open)
                    Dim f As BinaryFormatter = New BinaryFormatter
                    oldSettings = CType(f.Deserialize(s), EveSettings)
                End Using
            Catch e As Exception
                _worker.ReportProgress(0, "Error loading EveHQ Settings file: " & e.Message)
            End Try

            _worker.ReportProgress(0, "Core Settings Conversion Step 1/8: Loading training queues...")
            LoadTraining(oldSettings, Path.Combine(settingsFolder, "Data"))

            _worker.ReportProgress(0, "Core Settings Conversion Step 2/8: Converting main settings...")
            ConvertMainSettings(oldSettings)

            _worker.ReportProgress(0, "Core Settings Conversion Step 3/8: Converting API accounts...")
            ConvertAccounts(oldSettings)

            _worker.ReportProgress(0, "Core Settings Conversion Step 4/8: Converting characters...")
            ConvertPilots(oldSettings)

            _worker.ReportProgress(0, "Core Settings Conversion Step 5/8: Converting plug-in settings...")
            ConvertPlugins(oldSettings)

            _worker.ReportProgress(0, "Core Settings Conversion Step 6/8: Converting dashboard configuration...")
            ConvertDashboard(oldSettings)

            'Dim startTime, endTime As DateTime
            'Dim timeTaken As TimeSpan

            'startTime = Now
            Dim json As String = JsonConvert.SerializeObject(_newSettings, Newtonsoft.Json.Formatting.Indented)
            'endTime = Now
            'timeTaken = (endTime - startTime)

            _worker.ReportProgress(0, "Core Settings Conversion Step 7/8: Saving new settings file...")
            ' Write a JSON version of the settings
            Try
                Using s As New StreamWriter(Path.Combine(settingsFolder, "EveHQSettings.json"), False)
                    s.Write(json)
                    s.Flush()
                End Using
            Catch e As Exception
                _worker.ReportProgress(0, "Error writing new settings file: " & e.Message)
            End Try

            ' Rename the old settings file
            _worker.ReportProgress(0, "Core Settings Conversion Step 8/8: Archiving old settings...")
            Try
                My.Computer.FileSystem.RenameFile(Path.Combine(settingsFolder, "EveHQSettings.bin"), "OldEveHQSettings.bin")
            Catch e As Exception
                _worker.ReportProgress(0, "Error archiving old settings file: " & e.Message)
            End Try
            'MessageBox.Show("Successfully converted settings in " & timeTaken.TotalMilliseconds.ToString("N2") & "ms", "Settings conversion complete", MessageBoxButtons.OK, MessageBoxIcon.Information)

        End If

    End Sub

    Private Sub LoadTraining(oldSettings As EveSettings, skillXMLFolder As String)
        Dim currentPilot As Pilot
        Dim xmlDoc As New XmlDocument
        Dim tFileName As String

        Dim trainingList, queueList As XmlNodeList
        Dim trainingDetails, queueDetails As XmlNode

        Dim obsoleteSkills() As String =
                {"Analytical Mind", "Clarity", "Eidetic Memory", "Empathy", "Focus", "Instant Recall", "Iron Will",
                 "Learning", "Logic", "Presence", "Spatial Awareness"}
        Dim obsoleteList As New List(Of String)(obsoleteSkills)

        For Each currentPilot In oldSettings.Pilots
            currentPilot.ActiveQueue = New SkillQueue
            'currentPilot.ActiveQueue.Queue.Clear()
            currentPilot.TrainingQueues = New SortedList
            currentPilot.TrainingQueues.Clear()
            currentPilot.PrimaryQueue = ""

            tFileName = "Q_" & currentPilot.Name & ".xml"
            If My.Computer.FileSystem.FileExists(Path.Combine(skillXMLFolder, tFileName)) = True Then
                Try
                    xmlDoc.Load(Path.Combine(skillXMLFolder, tFileName))

                    ' Get the pilot details
                    trainingList = xmlDoc.SelectNodes("/training/skill")

                    If trainingList.Count > 0 Then
                        ' Using version prior to 1.3
                        ' Start a new SkillQueue class (using "primary" as the default name)
                        Dim newQ As New SkillQueue
                        newQ.Name = "Primary"
                        newQ.IncCurrentTraining = True
                        newQ.Primary = True
                        For Each trainingDetails In trainingList
                            Dim myskill As SkillQueueItem = New SkillQueueItem
                            myskill.Name = trainingDetails.ChildNodes(0).InnerText
                            myskill.FromLevel = CInt(trainingDetails.ChildNodes(1).InnerText)
                            myskill.ToLevel = CInt(trainingDetails.ChildNodes(2).InnerText)
                            myskill.Pos = CInt(trainingDetails.ChildNodes(3).InnerText)
                            Dim keyName As String = myskill.Name & myskill.FromLevel & myskill.ToLevel
                            currentPilot.ActiveQueue.Queue.Add(myskill, keyName)
                        Next
                        newQ.Queue = currentPilot.ActiveQueue.Queue
                        currentPilot.PrimaryQueue = newQ.Name
                        currentPilot.TrainingQueues.Add(newQ.Name, newQ)
                    Else
                        ' Try for the post 1.3 version
                        ' Get version
                        Dim rootNode As XmlNode = xmlDoc.SelectSingleNode("/training")
                        Dim version As Double = 0
                        Dim culture As CultureInfo = New CultureInfo("en-GB")
                        If rootNode.Attributes.Count > 0 Then
                            version = Double.Parse(rootNode.Attributes("version").Value, NumberStyles.Any, culture)
                        End If
                        queueList = xmlDoc.SelectNodes("/training/queue")
                        If queueList.Count > 0 Then
                            For Each queueDetails In queueList
                                Dim newQ As New SkillQueue
                                newQ.Name = Net.WebUtility.HtmlDecode(queueDetails.Attributes("name").Value)
                                newQ.IncCurrentTraining = CBool(queueDetails.Attributes("ICT").Value)
                                newQ.Primary = CBool(queueDetails.Attributes("primary").Value)
                                If newQ.Primary = True Then
                                    If currentPilot.PrimaryQueue <> "" Then
                                        ' There can be only one!
                                        newQ.Primary = False
                                    Else
                                        currentPilot.PrimaryQueue = newQ.Name
                                    End If
                                End If
                                If queueDetails.ChildNodes.Count > 0 Then
                                    ' Using version prior to 1.3
                                    ' Start a new SkillQueue class (using "primary" as the default name)
                                    For Each trainingDetails In queueDetails.ChildNodes
                                        If obsoleteList.Contains(trainingDetails.ChildNodes(0).InnerText) = False Then
                                            Dim myskill As SkillQueueItem = New SkillQueueItem
                                            myskill.Name = trainingDetails.ChildNodes(0).InnerText
                                            ' Adjust for the 1.9 version
                                            If version < 1.9 Then
                                                If myskill.Name = "Astrometric Triangulation" Then
                                                    myskill.Name = "Astrometric Acquisition"
                                                End If
                                                If myskill.Name = "Signal Acquisition" Then
                                                    myskill.Name = "Astrometric Triangulation"
                                                End If
                                            End If
                                            Try
                                                myskill.FromLevel = CInt(trainingDetails.ChildNodes(1).InnerText)
                                                myskill.ToLevel = CInt(trainingDetails.ChildNodes(2).InnerText)
                                                myskill.Pos = CInt(trainingDetails.ChildNodes(3).InnerText)
                                                myskill.Notes =
                                                    Net.WebUtility.HtmlDecode(trainingDetails.ChildNodes(4).InnerText)
                                            Catch e As Exception
                                                ' We don't have the required info
                                            End Try
                                            Dim keyName As String = myskill.Name & myskill.FromLevel & myskill.ToLevel
                                            If newQ.Queue.Contains(keyName) = False Then
                                                If myskill.ToLevel > myskill.FromLevel Then
                                                    newQ.Queue.Add(myskill, keyName) _
                                                    ' Multi queue method
                                                End If
                                            End If
                                        End If
                                    Next
                                End If
                                currentPilot.TrainingQueues.Add(newQ.Name, newQ)
                            Next
                        End If
                    End If

                    ' Iterate through the relevant nodes

                Catch e As Exception
                    _worker.ReportProgress(0, "Error loading training for " & currentPilot.Name & ": " & e.Message)
                    MessageBox.Show("Error importing Training data for " & currentPilot.Name & "." & ControlChars.CrLf &
                        "The error reported was " & e.Message, "Training Data Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Error)
                End Try
            End If
        Next
    End Sub

    Private Sub ConvertMainSettings(oldSettings As EveSettings)

        Try
            _newSettings.MarketDataProvider = oldSettings.MarketDataProvider
            _newSettings.MaxUpdateThreads = oldSettings.MaxUpdateThreads
            _newSettings.MarketDataSource = oldSettings.MarketDataSource
            _newSettings.Corporations = oldSettings.Corporations
            _newSettings.PriceGroups = oldSettings.PriceGroups
            _newSettings.SkillQueuePanelWidth = oldSettings.SkillQueuePanelWidth
            _newSettings.AccountTimeLimit = oldSettings.AccountTimeLimit
            _newSettings.NotifyAccountTime = oldSettings.NotifyAccountTime
            _newSettings.NotifyInsuffClone = oldSettings.NotifyInsuffClone
            _newSettings.StartWithPrimaryQueue = oldSettings.StartWithPrimaryQueue
            _newSettings.IgnoreLastMessage = oldSettings.IgnoreLastMessage
            _newSettings.LastMessageDate = oldSettings.LastMessageDate
            _newSettings.DisableTrainingBar = oldSettings.DisableTrainingBar
            _newSettings.EnableAutomaticSave = oldSettings.EnableAutomaticSave
            _newSettings.AutomaticSaveTime = oldSettings.AutomaticSaveTime
            _newSettings.RibbonMinimised = oldSettings.RibbonMinimised
            _newSettings.ThemeSetByUser = oldSettings.ThemeSetByUser
            _newSettings.ThemeTint = oldSettings.ThemeTint
            _newSettings.ThemeStyle = oldSettings.ThemeStyle
            _newSettings.SQLQueries = oldSettings.SQLQueries
            _newSettings.BackupBeforeUpdate = oldSettings.BackupBeforeUpdate
            _newSettings.QatLayout = oldSettings.QATLayout
            _newSettings.NotifyEveNotification = oldSettings.NotifyEveNotification
            _newSettings.NotifyEveMail = oldSettings.NotifyEveMail
            _newSettings.AutoMailAPI = oldSettings.AutoMailAPI
            _newSettings.EveHqBackupWarnFreq = oldSettings.EveHQBackupWarnFreq
            _newSettings.EveHqBackupMode = oldSettings.EveHQBackupMode
            _newSettings.EveHqBackupStart = oldSettings.EveHQBackupStart
            _newSettings.EveHqBackupFreq = oldSettings.EveHQBackupFreq
            _newSettings.EveHqBackupLast = oldSettings.EveHQBackupLast
            _newSettings.EveHqBackupLastResult = oldSettings.EveHQBackupLastResult
            _newSettings.IbShowAllItems = oldSettings.IBShowAllItems
            _newSettings.EmailSenderAddress = oldSettings.EmailSenderAddress
            _newSettings.UserQueueColumns = oldSettings.UserQueueColumns
            _newSettings.StandardQueueColumns = oldSettings.StandardQueueColumns
            _newSettings.DBTickerLocation = oldSettings.DBTickerLocation
            _newSettings.DBTicker = oldSettings.DBTicker
            _newSettings.CsvSeparatorChar = oldSettings.CSVSeparatorChar
            _newSettings.DisableVisualStyles = oldSettings.DisableVisualStyles
            _newSettings.DisableAutoWebConnections = oldSettings.DisableAutoWebConnections
            _newSettings.TrainingBarHeight = oldSettings.TrainingBarHeight
            _newSettings.TrainingBarWidth = oldSettings.TrainingBarWidth
            _newSettings.TrainingBarDockPosition = oldSettings.TrainingBarDockPosition
            _newSettings.MdiTabPosition = oldSettings.MDITabPosition
            _newSettings.MarketRegionList = oldSettings.MarketRegionList
            _newSettings.IgnoreBuyOrderLimit = oldSettings.IgnoreBuyOrderLimit
            _newSettings.IgnoreSellOrderLimit = oldSettings.IgnoreSellOrderLimit
            For i As Integer = 0 To 11
                _newSettings.PriceCriteria(i) = oldSettings.PriceCriteria(i)
            Next
            _newSettings.MarketLogUpdateData = oldSettings.MarketLogUpdateData
            _newSettings.MarketLogUpdatePrice = oldSettings.MarketLogUpdatePrice
            _newSettings.MarketLogPopupConfirm = oldSettings.MarketLogPopupConfirm
            _newSettings.MarketLogToolTipConfirm = oldSettings.MarketLogToolTipConfirm
            _newSettings.IgnoreBuyOrders = oldSettings.IgnoreBuyOrders
            _newSettings.IgnoreSellOrders = oldSettings.IgnoreSellOrders
            _newSettings.DBTimeout = oldSettings.DBTimeout
            _newSettings.PilotSkillHighlightColor = oldSettings.PilotSkillHighlightColor
            _newSettings.PilotSkillTextColor = oldSettings.PilotSkillTextColor
            _newSettings.PilotGroupTextColor = oldSettings.PilotGroupTextColor
            _newSettings.PilotGroupBackgroundColor = oldSettings.PilotGroupBackgroundColor
            _newSettings.ErrorReportingEmail = oldSettings.ErrorReportingEmail
            _newSettings.ErrorReportingName = oldSettings.ErrorReportingName
            _newSettings.ErrorReportingEnabled = oldSettings.ErrorReportingEnabled
            _newSettings.TaskbarIconMode = oldSettings.TaskbarIconMode
            _newSettings.EcmDefaultLocation = oldSettings.ECMDefaultLocation
            _newSettings.APIFileExtension = oldSettings.APIFileExtension
            _newSettings.UseAppDirectoryForDB = oldSettings.UseAppDirectoryForDB
            _newSettings.OmitCurrentSkill = oldSettings.OmitCurrentSkill
            _newSettings.UpdateUrl = oldSettings.UpdateURL
            _newSettings.UseCcpapiBackup = oldSettings.UseCCPAPIBackup
            _newSettings.UseApirs = oldSettings.UseAPIRS
            _newSettings.ApirsAddress = oldSettings.APIRSAddress
            _newSettings.CcpapiServerAddress = oldSettings.CCPAPIServerAddress
            For i As Integer = 1 To 4
                _newSettings.EveFolderLabel(i) = oldSettings.EveFolderLabel(i)
            Next
            _newSettings.PilotCurrentTrainSkillColor = oldSettings.PilotCurrentTrainSkillColor
            _newSettings.PilotPartTrainedSkillColor = oldSettings.PilotPartTrainedSkillColor
            _newSettings.PilotLevel5SkillColor = oldSettings.PilotLevel5SkillColor
            _newSettings.PilotStandardSkillColor = oldSettings.PilotStandardSkillColor
            _newSettings.PanelHighlightColor = oldSettings.PanelHighlightColor
            _newSettings.PanelTextColor = oldSettings.PanelTextColor
            _newSettings.PanelRightColor = oldSettings.PanelRightColor
            _newSettings.PanelLeftColor = oldSettings.PanelLeftColor
            _newSettings.PanelBottomRightColor = oldSettings.PanelBottomRightColor
            _newSettings.PanelTopLeftColor = oldSettings.PanelTopLeftColor
            _newSettings.PanelOutlineColor = oldSettings.PanelOutlineColor
            _newSettings.PanelBackgroundColor = oldSettings.PanelBackgroundColor
            _newSettings.LastMarketPriceUpdate = oldSettings.LastMarketPriceUpdate
            _newSettings.LastFactionPriceUpdate = oldSettings.LastFactionPriceUpdate
            For i As Integer = 1 To 4
                _newSettings.EveFolderLua(i) = oldSettings.EveFolderLUA(i)
            Next
            _newSettings.CycleG15Time = oldSettings.CycleG15Time
            _newSettings.CycleG15Pilots = oldSettings.CycleG15Pilots
            _newSettings.ActivateG15 = oldSettings.ActivateG15
            _newSettings.AutoAPI = oldSettings.AutoAPI
            _newSettings.MainFormWindowState = CType(oldSettings.MainFormPosition(4), FormWindowState)
            _newSettings.MainFormLocation = New Point(oldSettings.MainFormPosition(0), oldSettings.MainFormPosition(1))
            _newSettings.MainFormSize = New Size(oldSettings.MainFormPosition(2), oldSettings.MainFormPosition(3))
            _newSettings.DeleteSkills = oldSettings.DeleteSkills
            _newSettings.PartialTrainColor = oldSettings.PartialTrainColor
            _newSettings.ReadySkillColor = oldSettings.ReadySkillColor
            _newSettings.IsPreReqColor = oldSettings.IsPreReqColor
            _newSettings.HasPreReqColor = oldSettings.HasPreReqColor
            _newSettings.BothPreReqColor = oldSettings.BothPreReqColor
            _newSettings.DtClashColor = oldSettings.DTClashColor
            _newSettings.ColorHighlightQueuePreReq = oldSettings.ColorHighlightQueuePreReq
            _newSettings.ColorHighlightQueueTraining = oldSettings.ColorHighlightQueueTraining
            _newSettings.ColorHighlightPilotTraining = oldSettings.ColorHighlightPilotTraining
            _newSettings.ContinueTraining = oldSettings.ContinueTraining
            _newSettings.EMailPassword = oldSettings.EMailPassword
            _newSettings.EMailUsername = oldSettings.EMailUsername
            _newSettings.UseSsl = oldSettings.UseSSL
            _newSettings.UseSmtpAuth = oldSettings.UseSMTPAuth
            _newSettings.EMailAddress = oldSettings.EMailAddress
            _newSettings.EMailPort = oldSettings.EMailPort
            _newSettings.EMailServer = oldSettings.EMailServer
            _newSettings.NotifySoundFile = oldSettings.NotifySoundFile
            _newSettings.NotifyOffset = oldSettings.NotifyOffset
            _newSettings.NotifyEarly = oldSettings.NotifyEarly
            _newSettings.NotifyNow = oldSettings.NotifyNow
            _newSettings.NotifySound = oldSettings.NotifySound
            _newSettings.NotifyEMail = oldSettings.NotifyEMail
            _newSettings.NotifyDialog = oldSettings.NotifyDialog
            _newSettings.NotifyToolTip = oldSettings.NotifyToolTip
            _newSettings.ShutdownNotifyPeriod = oldSettings.ShutdownNotifyPeriod
            _newSettings.ShutdownNotify = oldSettings.ShutdownNotify
            _newSettings.ServerOffset = oldSettings.ServerOffset
            _newSettings.EnableEveStatus = oldSettings.EnableEveStatus
            _newSettings.ProxyUseDefault = oldSettings.ProxyUseDefault
            _newSettings.ProxyUseBasic = oldSettings.ProxyUseBasic
            _newSettings.ProxyPassword = oldSettings.ProxyPassword
            _newSettings.ProxyUsername = oldSettings.ProxyUsername
            _newSettings.ProxyPort = oldSettings.ProxyPort
            _newSettings.ProxyServer = oldSettings.ProxyServer
            _newSettings.ProxyRequired = oldSettings.ProxyRequired
            _newSettings.AutoHide = oldSettings.AutoHide
            _newSettings.AutoStart = oldSettings.AutoStart
            _newSettings.AutoCheck = oldSettings.AutoCheck
            _newSettings.MinimiseExit = oldSettings.MinimiseExit
            _newSettings.AutoMinimise = oldSettings.AutoMinimise
            _newSettings.StartupPilot = oldSettings.StartupPilot
            _newSettings.StartupForms = 0
            Select Case oldSettings.StartupView
                Case "EveHQ Dashboard"
                    _newSettings.StartupForms = 8
                Case "Pilot Information"
                    _newSettings.StartupForms = 1
                Case "Pilot Summary Report"
                    _newSettings.StartupForms = 32
                Case "Skill Training"
                    _newSettings.StartupForms = 2
            End Select
            For i As Integer = 1 To 4
                _newSettings.EveFolder(i) = oldSettings.EveFolder(i)
            Next
            _newSettings.BackupAuto = oldSettings.BackupAuto
            _newSettings.BackupStart = oldSettings.BackupStart
            _newSettings.BackupFreq = oldSettings.BackupFreq
            _newSettings.BackupLast = oldSettings.BackupLast
            _newSettings.BackupLastResult = oldSettings.BackupLastResult
            _newSettings.QColumnsSet = oldSettings.QColumnsSet
            For i As Integer = 0 To 20
                For j As Integer = 0 To 1
                    _newSettings.QColumns(i, j) = oldSettings.QColumns(i, j)
                Next
            Next
            _newSettings.MarketRegions = new List(Of Integer)(oldSettings.MarketRegions.First())
            _newSettings.MarketSystem = oldSettings.MarketSystem
            _newSettings.MarketUseRegionMarket = oldSettings.MarketUseRegionMarket
            _newSettings.MarketDefaultMetric = oldSettings.MarketDefaultMetric
            _newSettings.MarketStatOverrides = oldSettings.MarketStatOverrides
            _newSettings.MarketDefaultTransactionType = oldSettings.MarketDefaultTransactionType
        Catch e As Exception
            _worker.ReportProgress(0, "Error converting main settings: " & e.Message)
        End Try
    End Sub

    Private Sub ConvertAccounts(ByVal oldSettings As EveSettings)
        Try
            _newSettings.Accounts.Clear()
            For Each account As EveAccount In oldSettings.Accounts
                Dim newAccount As New EveHQAccount
                newAccount.AccessMask = account.AccessMask
                newAccount.APIAccountStatus = account.APIAccountStatus
                newAccount.APIKey = account.APIKey
                newAccount.ApiKeyExpiryDate = account.APIKeyExpiryDate
                newAccount.ApiKeySystem = account.APIKeySystem
                newAccount.APIKeyType = account.APIKeyType
                newAccount.Characters.Clear()
                For Each name As String In account.Characters
                    newAccount.Characters.Add(name)
                Next
                newAccount.CorpApiAccountKey = account.CorpAPIAccountKey
                newAccount.CreateDate = account.CreateDate
                newAccount.FailedAttempts = account.FailedAttempts
                newAccount.FriendlyName = account.FriendlyName
                newAccount.LastAccountStatusCheck = account.LastAccountStatusCheck
                newAccount.LogonCount = account.LogonCount
                newAccount.LogonMinutes = account.LogonMinutes
                newAccount.PaidUntil = account.PaidUntil
                newAccount.UserID = account.userID
                _newSettings.Accounts.Add(newAccount.UserID, newAccount)
            Next
        Catch e As Exception
            _worker.ReportProgress(0, "Error converting API account settings: " & e.Message)
        End Try
    End Sub

    Private Sub ConvertPilots(ByVal oldSettings As EveSettings)

        Try
            _newSettings.Pilots.Clear()
            For Each pilot As Pilot In oldSettings.Pilots
                Dim newPilot As New EveHQPilot

                newPilot.Name = pilot.Name
                newPilot.ID = pilot.ID
                newPilot.Account = pilot.Account
                newPilot.AccountPosition = pilot.AccountPosition
                newPilot.Race = pilot.Race
                newPilot.Blood = pilot.Blood
                newPilot.Gender = pilot.Gender
                newPilot.Corp = pilot.Corp
                newPilot.CorpID = pilot.CorpID
                newPilot.Isk = pilot.Isk
                newPilot.SkillPoints = pilot.SkillPoints
                newPilot.Training = pilot.Training
                newPilot.TrainingStartTime = pilot.TrainingStartTime
                newPilot.TrainingStartTimeActual = pilot.TrainingStartTimeActual
                newPilot.TrainingEndTime = pilot.TrainingEndTime
                newPilot.TrainingEndTimeActual = pilot.TrainingEndTimeActual
                If IsNumeric(pilot.TrainingSkillID) Then
                    newPilot.TrainingSkillID = CInt(pilot.TrainingSkillID)
                Else
                    newPilot.TrainingSkillID = 0
                End If
                newPilot.TrainingSkillName = pilot.TrainingSkillName
                newPilot.TrainingStartSP = pilot.TrainingStartSP
                newPilot.TrainingEndSP = pilot.TrainingEndSP
                newPilot.TrainingCurrentSP = pilot.TrainingCurrentSP
                newPilot.TrainingCurrentTime = pilot.TrainingCurrentTime
                newPilot.TrainingSkillLevel = pilot.TrainingSkillLevel
                newPilot.TrainingNotifiedNow = pilot.TrainingNotifiedNow
                newPilot.TrainingNotifiedEarly = pilot.TrainingNotifiedEarly
                newPilot.CAtt = pilot.CAtt
                newPilot.IntAtt = pilot.IAtt
                newPilot.MAtt = pilot.MAtt
                newPilot.PAtt = pilot.PAtt
                newPilot.WAtt = pilot.WAtt
                newPilot.CImplant = pilot.CImplant
                newPilot.IntImplant = pilot.IImplant
                newPilot.MImplant = pilot.MImplant
                newPilot.PImplant = pilot.PImplant
                newPilot.WImplant = pilot.WImplant
                newPilot.CImplantA = pilot.CImplantA
                newPilot.IntImplantA = pilot.IImplantA
                newPilot.MImplantA = pilot.MImplantA
                newPilot.PImplantA = pilot.PImplantA
                newPilot.WImplantA = pilot.WImplantA
                newPilot.CImplantM = pilot.CImplantM
                newPilot.IntImplantM = pilot.IImplantM
                newPilot.MImplantM = pilot.MImplantM
                newPilot.PImplantM = pilot.PImplantM
                newPilot.WImplantM = pilot.WImplantM
                newPilot.UseManualImplants = pilot.UseManualImplants
                newPilot.CAttT = pilot.CAttT
                newPilot.IntAttT = pilot.IAttT
                newPilot.MAttT = pilot.MAttT
                newPilot.PAttT = pilot.PAttT
                newPilot.WAttT = pilot.WAttT
                ConvertPilotSkills(pilot, newPilot)
                ConvertPilotQueuedSkills(pilot, newPilot)
                newPilot.QueuedSkillTime = pilot.QueuedSkillTime
                newPilot.QualifiedCertificates.Clear()
                'For Each c As String In pilot.Certificates
                '    newPilot.Certificates.Add(CInt(c))
                'Next

                ConvertPilotCertificates(newPilot)

                newPilot.PrimaryQueue = pilot.PrimaryQueue
                ConvertTrainingQueues(pilot, newPilot)
                newPilot.ActiveQueue = ConvertPilotSkillQueue(pilot.ActiveQueue)
                newPilot.ActiveQueueName = pilot.ActiveQueueName
                newPilot.CacheFileTime = pilot.CacheFileTime
                newPilot.CacheExpirationTime = pilot.CacheExpirationTime
                newPilot.TrainingFileTime = pilot.TrainingFileTime
                newPilot.TrainingExpirationTime = pilot.TrainingExpirationTime
                newPilot.Updated = pilot.Updated
                newPilot.LastUpdate = pilot.LastUpdate
                newPilot.Active = pilot.Active
                For index As Integer = 0 To 53
                    newPilot.KeySkills(CType(index, KeySkill)) = CInt(pilot.KeySkills(index))
                Next
                newPilot.Standings = pilot.Standings
                newPilot.CorpRoles = pilot.CorpRoles
                _newSettings.Pilots.Add(newPilot.Name, newPilot)
            Next
        Catch e As Exception
            _worker.ReportProgress(0, "Error converting pilot settings: " & e.Message)
        End Try
    End Sub

    Private Sub ConvertPilotSkills(ByVal pilot As Pilot, ByVal newPilot As EveHQPilot)
        newPilot.PilotSkills.Clear()
        For Each oldskill As PilotSkill In pilot.PilotSkills
            Dim newSkill As New EveHQPilotSkill
            newSkill.ID = CInt(oldskill.ID)
            newSkill.Name = oldskill.Name
            newSkill.GroupID = CInt(oldskill.GroupID)
            newSkill.Rank = oldskill.Rank
            newSkill.SP = oldskill.SP
            newSkill.Level = oldskill.Level
            newPilot.PilotSkills.Add(newSkill.Name, newSkill)
        Next
    End Sub
    Private Sub ConvertPilotCertificates(ByRef pilot As EveHQPilot)
        pilot.QualifiedCertificates.Clear()

        Dim qualifiedCerts As New Dictionary(Of Integer, CertificateGrade)

        For Each cert In StaticData.Certificates.Values
            For Each grade In cert.GradesAndSkills.Keys
                If CheckPilotSkillsForCertGrade(cert.GradesAndSkills(grade), pilot) Then
                    If qualifiedCerts.ContainsKey(cert.Id) Then
                        qualifiedCerts(cert.Id) = grade
                    Else
                        qualifiedCerts.Add(cert.Id, grade)
                    End If
                End If
            Next
        Next

        ' take the collection of qualified certs and apply them to the pilot
        For Each certId In qualifiedCerts.Keys
            pilot.QualifiedCertificates.Add(certId, qualifiedCerts(certId))
        Next
    End Sub

    Private Shared Function CheckPilotSkillsForCertGrade(ByRef reqSkills As SortedList(Of Integer, Integer), ByRef pilot As EveHQPilot) As Boolean
        Dim qualifications As New SortedList(Of Integer, Boolean)
        For Each skill In reqSkills.Keys
            Dim pSkill As New EveHQPilotSkill
            qualifications.Add(skill, False)
            If pilot.PilotSkills.TryGetValue(skill.ToString(), pSkill) Then
                If pSkill.Rank >= reqSkills(skill) Then
                    qualifications(skill) = True
                End If
            End If
        Next
        Return qualifications.Values.All(Function(q) q = True)
    End Function

    
    Private Sub ConvertPilotQueuedSkills(ByVal pilot As Pilot, ByVal newPilot As EveHQPilot)
        newPilot.QueuedSkills.Clear()
        For Each oldskill As PilotQueuedSkill In pilot.QueuedSkills.Values
            Dim newSkill As New EveHQPilotQueuedSkill
            newSkill.Position = oldskill.Position
            newSkill.SkillID = oldskill.SkillID
            newSkill.Level = oldskill.Level
            newSkill.StartSP = oldskill.StartSP
            newSkill.EndSP = oldskill.EndSP
            newSkill.StartTime = oldskill.StartTime
            newSkill.EndTime = oldskill.EndTime
            newPilot.QueuedSkills.Add(newSkill.Position, newSkill)
        Next
    End Sub

    Private Sub ConvertTrainingQueues(ByVal pilot As Pilot, ByVal newPilot As EveHQPilot)
        newPilot.TrainingQueues.Clear()
        For Each oldQueue As SkillQueue In pilot.TrainingQueues.Values
            newPilot.TrainingQueues.Add(oldQueue.Name, ConvertPilotSkillQueue(oldQueue))
        Next
    End Sub

    Private Function ConvertPilotSkillQueue(oldQueue As SkillQueue) As EveHQSkillQueue
        Dim newQueue As New EveHQSkillQueue
        newQueue.Name = oldQueue.Name
        newQueue.IncCurrentTraining = oldQueue.IncCurrentTraining
        newQueue.Primary = oldQueue.Primary
        newQueue.QueueSkills = oldQueue.QueueSkills
        newQueue.QueueTime = oldQueue.QueueTime
        newQueue.Queue.Clear()
        For Each oldQueueItem As SkillQueueItem In oldQueue.Queue
            Dim newQueueItem As New EveHQSkillQueueItem
            newQueueItem.Name = oldQueueItem.Name
            newQueueItem.FromLevel = oldQueueItem.FromLevel
            newQueueItem.ToLevel = oldQueueItem.ToLevel
            newQueueItem.Pos = oldQueueItem.Pos
            newQueueItem.Priority = oldQueueItem.Priority
            newQueueItem.Notes = oldQueueItem.Notes
            newQueue.Queue.Add(newQueueItem.Key, newQueueItem)
        Next
        Return newQueue
    End Function

    Private Sub ConvertPlugins(ByVal oldSettings As EveSettings)
        Try
            _newSettings.Plugins.Clear()
            For Each plugin As PlugIn In oldSettings.Plugins.Values
                Dim newPlugin As New EveHQPlugInConfig
                newPlugin.Name = plugin.Name
                newPlugin.Disabled = plugin.Disabled
                _newSettings.Plugins.Add(newPlugin.Name, newPlugin)
            Next
        Catch e As Exception
            _worker.ReportProgress(0, "Error converting plug-in settings: " & e.Message)
        End Try
    End Sub

    Private Sub ConvertDashboard(ByVal oldSettings As EveSettings)
        _newSettings.DashboardConfiguration.Clear()
        For Each config As SortedList(Of String, Object) In oldSettings.DashboardConfiguration
            Try
                Dim newConfig As New SortedList(Of String, Object)
                For Each configProperty As String In config.Keys
                    Select Case configProperty
                        Case "ControlLocation", "ControlSize"
                            newConfig.Add(configProperty, config(configProperty).ToString)
                        Case Else
                            newConfig.Add(configProperty, config(configProperty))
                    End Select
                Next
                _newSettings.DashboardConfiguration.Add(newConfig)
            Catch e As Exception
                _worker.ReportProgress(0, "Error converting dashboard widget: " & e.Message)
            End Try
        Next
    End Sub

#End Region

#Region "Prism Data/Settings Conversion Methods"

    Private Sub ConvertPrismSettings(settingsFolder As String)

        Dim prismFolder As String = Path.Combine(settingsFolder, "Prism")

        _worker.ReportProgress(0, "Prism Settings Conversion Step 1/4: Converting settings...")
        ConvertSettings(prismFolder)

        _worker.ReportProgress(0, "Prism Settings Conversion Step 2/4: Converting blueprints...")
        ConvertBlueprintAssets(prismFolder)

        _worker.ReportProgress(0, "Prism Settings Conversion Step 3/4: Converting production jobs...")
        ConvertProductionJobs(prismFolder)

        _worker.ReportProgress(0, "Prism Settings Conversion Step 4/4: Converting batch jobs...")
        ConvertBatchJobs(prismFolder)

    End Sub

    Private Sub ConvertSettings(prismFolder As String)

        Try
            Dim oldSettings As Prism.Settings

            If My.Computer.FileSystem.FileExists(Path.Combine(prismFolder, "PrismSettings.bin")) = True Then
                Using s As New FileStream(Path.Combine(prismFolder, "PrismSettings.bin"), FileMode.Open)
                    Dim f As BinaryFormatter = New BinaryFormatter
                    oldSettings = CType(f.Deserialize(s), Prism.Settings)
                End Using

                Dim newSettings As New PrismSettings

                newSettings.CorpReps = oldSettings.CorpReps
                newSettings.SlotNameWidth = oldSettings.SlotNameWidth
                newSettings.UserSlotColumns = oldSettings.UserSlotColumns
                newSettings.StandardSlotColumns = oldSettings.StandardSlotColumns
                newSettings.DefaultBPCalcAssetOwner = oldSettings.DefaultBPCalcAssetOwner
                newSettings.DefaultBPCalcManufacturer = oldSettings.DefaultBPCalcManufacturer
                newSettings.DefaultBPOwner = oldSettings.DefaultBPOwner
                newSettings.DefaultCharacter = oldSettings.DefaultCharacter
                newSettings.BPCCosts.Clear()
                For Each bpc As BPCCostInfo In oldSettings.BPCCosts.Values
                    Dim newBPC As New BlueprintCopyCostInfo(CInt(bpc.ID), bpc.MaxRunCost, bpc.MinRunCost)
                    newSettings.BPCCosts.Add(newBPC.ID, newBPC)
                Next
                newSettings.LabRunningCost = oldSettings.LabRunningCost
                newSettings.LabInstallCost = oldSettings.LabInstallCost
                newSettings.FactoryRunningCost = oldSettings.FactoryRunningCost
                newSettings.FactoryInstallCost = oldSettings.FactoryInstallCost

                Dim json As String = JsonConvert.SerializeObject(newSettings, Newtonsoft.Json.Formatting.Indented)

                ' Write a JSON version of the settings
                Try
                    Using s As New StreamWriter(Path.Combine(prismFolder, "PrismSettings.json"), False)
                        s.Write(json)
                        s.Flush()
                    End Using
                Catch e As Exception
                End Try

                ' Rename the old settings file
                My.Computer.FileSystem.RenameFile(Path.Combine(prismFolder, "PrismSettings.bin"), "OldPrismSettings.bin")

            End If
        Catch e As Exception
            _worker.ReportProgress(0, "Error converting Prism main settings: " & e.Message)
        End Try

    End Sub

    Private Sub ConvertBlueprintAssets(prismFolder As String)

        Try

            Dim ownerBlueprints As SortedList(Of String, SortedList(Of String, BlueprintAsset))

            If My.Computer.FileSystem.FileExists(Path.Combine(prismFolder, "OwnerBlueprints.bin")) = True Then
                Using s As New FileStream(Path.Combine(prismFolder, "OwnerBlueprints.bin"), FileMode.Open)
                    Dim f As BinaryFormatter = New BinaryFormatter
                    ownerBlueprints = CType(f.Deserialize(s), SortedList(Of String, SortedList(Of String, BlueprintAsset)))
                End Using

                Dim json As String = JsonConvert.SerializeObject(ownerBlueprints, Newtonsoft.Json.Formatting.Indented)

                ' Write a JSON version of the settings
                Try
                    Using s As New StreamWriter(Path.Combine(prismFolder, "OwnerBlueprints.json"), False)
                        s.Write(json)
                        s.Flush()
                    End Using
                Catch e As Exception
                End Try

                ' Rename the old settings file
                My.Computer.FileSystem.RenameFile(Path.Combine(prismFolder, "OwnerBlueprints.bin"), "OldOwnerBlueprints.bin")

            End If

        Catch e As Exception
            _worker.ReportProgress(0, "Error converting Prism blueprint assets: " & e.Message)
        End Try

    End Sub

    Private Sub ConvertProductionJobs(prismFolder As String)

        Try
            Dim oldJobs As SortedList(Of String, ProductionJob)

            If My.Computer.FileSystem.FileExists(Path.Combine(prismFolder, "ProductionJobs.bin")) = True Then
                Using s As New FileStream(Path.Combine(prismFolder, "ProductionJobs.bin"), FileMode.Open)
                    Dim f As BinaryFormatter = New BinaryFormatter
                    oldJobs = CType(f.Deserialize(s), SortedList(Of String, ProductionJob))
                End Using

                ' Load up some static data for the conversion
                StaticData.LoadCoreCacheForConversion(Path.Combine(Application.StartupPath, "StaticData"))

                ' Convert the old jobs into the new format
                Dim newJobs As New SortedList(Of String, Job)
                For Each job As ProductionJob In oldJobs.Values
                    newJobs.Add(job.JobName, ConvertProductionJob(job))
                Next

                ' Create the JSON string
                Dim json As String = JsonConvert.SerializeObject(newJobs, Newtonsoft.Json.Formatting.Indented)

                ' Write a JSON version of the settings
                Try
                    Using s As New StreamWriter(Path.Combine(prismFolder, "ProductionJobs.json"), False)
                        s.Write(json)
                        s.Flush()
                    End Using
                Catch e As Exception
                End Try

                ' Rename the old settings file
                My.Computer.FileSystem.RenameFile(Path.Combine(prismFolder, "ProductionJobs.bin"), "OldProductionJobs.bin")

            End If

        Catch e As Exception
            _worker.ReportProgress(0, "Error converting Prism production jobs: " & e.Message)
        End Try

    End Sub

    Private Function ConvertProductionJob(oldJob As ProductionJob) As Job
        If oldJob Is Nothing Then
            Return Nothing
        Else
            Dim newJob As New Job
            newJob.JobName = oldJob.JobName
            newJob.BlueprintId = oldJob.BPID
            newJob.TypeID = oldJob.TypeID
            newJob.TypeName = oldJob.TypeName
            newJob.PerfectUnits = oldJob.PerfectUnits
            newJob.WasteUnits = oldJob.WasteUnits
            newJob.Runs = oldJob.Runs
            newJob.Manufacturer = oldJob.Manufacturer
            newJob.BlueprintOwner = oldJob.BPOwner
            newJob.PESkill = oldJob.PESkill
            newJob.IndSkill = oldJob.IndSkill
            newJob.ProdImplant = oldJob.ProdImplant
            newJob.OverridingME = oldJob.OverridingME
            newJob.OverridingPE = oldJob.OverridingPE
            newJob.StartTime = oldJob.StartTime
            newJob.RunTime = oldJob.RunTime
            newJob.Cost = oldJob.Cost
            newJob.HasInventionJob = oldJob.HasInventionJob
            newJob.ProduceSubJob = oldJob.ProduceSubJob
            newJob.InventionJob = ConvertInventionJob(oldJob.InventionJob)
            If oldJob.AssemblyArray Is Nothing Then
                newJob.AssemblyArray = Nothing
            Else
                newJob.AssemblyArray = StaticData.AssemblyArrays(oldJob.AssemblyArray.ID)
            End If
            For Each key As String In oldJob.SubJobMEs.Keys
                newJob.SubJobMEs.Add(CInt(key), oldJob.SubJobMEs(key))
            Next
            If StaticData.Blueprints.ContainsKey(oldJob.CurrentBP.ID) Then
                newJob.CurrentBlueprint = OwnedBlueprint.CopyFromBlueprint(StaticData.Blueprints(oldJob.CurrentBP.ID))
                newJob.CurrentBlueprint.MELevel = oldJob.CurrentBP.MELevel
                newJob.CurrentBlueprint.PELevel = oldJob.CurrentBP.PELevel
                newJob.CurrentBlueprint.Runs = oldJob.CurrentBP.Runs
                newJob.CurrentBlueprint.AssetId = oldJob.CurrentBP.AssetID
            Else
                newJob.CurrentBlueprint = Nothing
            End If
            For Each resource As Object In oldJob.RequiredResources.Values
                If typeof(resource) Is RequiredResource Then
                    Dim rResource As RequiredResource = CType(resource, RequiredResource)
                    Dim newResource As New JobResource
                    newResource.TypeID = rResource.TypeID
                    newResource.TypeName = rResource.TypeName
                    newResource.TypeGroup = rResource.TypeGroup
                    newResource.TypeCategory = rResource.TypeCategory
                    newResource.PerfectUnits = rResource.PerfectUnits
                    newResource.BaseUnits = rResource.BaseUnits
                    newResource.WasteUnits = rResource.WasteUnits
                    newJob.Resources.Add(newResource.TypeID, newResource)
                Else
                    ' This is another production job
                    Dim subJob As ProductionJob = CType(resource, ProductionJob)
                    newJob.SubJobs.Add(subJob.TypeID, ConvertProductionJob(subJob))
                End If
            Next

            Return newJob
        End If

    End Function

    Private Function ConvertInventionJob(oldJob As Prism.InventionJob) As BPCalc.InventionJob

        If oldJob Is Nothing Then

            Return Nothing

        Else

            Dim newJob As New BPCalc.InventionJob
            newJob.InventedBpid = oldJob.InventedBpid
            newJob.BaseChance = oldJob.BaseChance
            newJob.DecryptorUsed = ConvertDecryptor(oldJob.DecryptorUsed)
            newJob.OverrideBpcRuns = oldJob.OverrideBpcRuns
            newJob.BpcRuns = oldJob.BpcRuns
            newJob.OverrideEncSkill = oldJob.OverrideEncSkill
            newJob.OverrideDcSkill1 = oldJob.OverrideDcSkill1
            newJob.OverrideDcSkill2 = oldJob.OverrideDcSkill2
            newJob.EncryptionSkill = oldJob.EncryptionSkill
            newJob.DatacoreSkill1 = oldJob.DatacoreSkill1
            newJob.DatacoreSkill2 = oldJob.DatacoreSkill2
            newJob.ProductionJob = ConvertProductionJob(oldJob.ProductionJob)
            Return newJob

        End If

    End Function

    Private Function ConvertDecryptor(oldDecryptor As Prism.Decryptor) As BPCalc.Decryptor
        If oldDecryptor Is Nothing Then
            Return Nothing
        Else
            Dim newDecryptor As New BPCalc.Decryptor
            newDecryptor.GroupID = oldDecryptor.GroupID
            newDecryptor.ID = CInt(oldDecryptor.ID)
            newDecryptor.MEMod = oldDecryptor.MEMod
            newDecryptor.Name = oldDecryptor.Name
            newDecryptor.PEMod = oldDecryptor.PEMod
            newDecryptor.ProbMod = oldDecryptor.ProbMod
            newDecryptor.RunMod = oldDecryptor.RunMod
            Return newDecryptor
        End If
    End Function

    Private Sub ConvertBatchJobs(prismFolder As String)

        Try
            Dim oldJobs As SortedList(Of String, BatchJob)

            If My.Computer.FileSystem.FileExists(Path.Combine(prismFolder, "BatchJobs.bin")) = True Then
                Using s As New FileStream(Path.Combine(prismFolder, "BatchJobs.bin"), FileMode.Open)
                    Dim f As BinaryFormatter = New BinaryFormatter
                    oldJobs = CType(f.Deserialize(s), SortedList(Of String, BatchJob))
                End Using

                Dim json As String = JsonConvert.SerializeObject(oldJobs, Newtonsoft.Json.Formatting.Indented)

                ' Write a JSON version of the settings
                Try
                    Using s As New StreamWriter(Path.Combine(prismFolder, "BatchJobs.json"), False)
                        s.Write(json)
                        s.Flush()
                    End Using
                Catch e As Exception
                End Try

                ' Rename the old settings file
                My.Computer.FileSystem.RenameFile(Path.Combine(prismFolder, "BatchJobs.bin"), "OldBatchJobs.bin")

            End If

        Catch e As Exception
            _worker.ReportProgress(0, "Error converting Prism batch jobs: " & e.Message)
        End Try

    End Sub

#End Region

#Region "HQF Data/Settings Conversion Methods"

    Private Sub ConvertHQFSettings(settingsFolder As String)

        Dim hqfFolder As String = Path.Combine(settingsFolder, "HQF")

        _worker.ReportProgress(0, "HQF Settings Conversion Step 1/7: Converting settings...")
        ConvertMainHQFSettings(hqfFolder)

        _worker.ReportProgress(0, "HQF Settings Conversion Step 2/7: Converting defence profiles...")
        ConvertDefenceProfiles(hqfFolder)

        _worker.ReportProgress(0, "HQF Settings Conversion Step 3/7: Converting damage profiles...")
        ConvertDamageProfiles(hqfFolder)

        _worker.ReportProgress(0, "HQF Settings Conversion Step 4/7: Converting fittings...")
        ConvertSavedFittings(hqfFolder)

        _worker.ReportProgress(0, "HQF Settings Conversion Step 5/7: Converting pilots...")
        ConvertPilots(hqfFolder)

        _worker.ReportProgress(0, "HQF Settings Conversion Step 6/7: Converting custom ship classes...")
        ConvertCustomShipClasses(hqfFolder)

        _worker.ReportProgress(0, "HQF Settings Conversion Step 7/7: Converting custom ships...")
        ConvertCustomShips(hqfFolder)

    End Sub

    Private Sub ConvertMainHQFSettings(hqfFolder As String)

        Try

            Dim oldSettings As HQF.Settings

            ' Check for the fittings file so we can load it
            If My.Computer.FileSystem.FileExists(Path.Combine(hqfFolder, "HQFSettings.bin")) = True Then
                Using s As New FileStream(Path.Combine(hqfFolder, "HQFSettings.bin"), FileMode.Open)
                    Dim f As BinaryFormatter = New BinaryFormatter
                    oldSettings = CType(f.Deserialize(s), HQF.Settings)
                End Using

                Dim newSettings As New PluginSettings

                newSettings.HiSlotColour = oldSettings.HiSlotColour
                newSettings.MidSlotColour = oldSettings.MidSlotColour
                newSettings.LowSlotColour = oldSettings.LowSlotColour
                newSettings.RigSlotColour = oldSettings.RigSlotColour
                newSettings.SubSlotColour = oldSettings.SubSlotColour
                newSettings.ServiceModSlotColour = oldSettings.ServiceModSlotColour
                newSettings.DefaultPilot = oldSettings.DefaultPilot
                newSettings.RestoreLastSession = oldSettings.RestoreLastSession
                newSettings.LastPriceUpdate = oldSettings.LastPriceUpdate
                newSettings.ModuleFilter = oldSettings.ModuleFilter
                newSettings.AutoUpdateHQFSkills = oldSettings.AutoUpdateHQFSkills
                newSettings.OpenFittingList = oldSettings.OpenFittingList
                newSettings.ShowPerformanceData = oldSettings.ShowPerformanceData
                newSettings.CloseInfoPanel = oldSettings.CloseInfoPanel
                newSettings.CapRechargeConstant = oldSettings.CapRechargeConstant
                newSettings.ShieldRechargeConstant = oldSettings.ShieldRechargeConstant
                newSettings.StandardSlotColumns = oldSettings.StandardSlotColumns
                newSettings.UserSlotColumns = oldSettings.UserSlotColumns
                newSettings.Favourites = oldSettings.Favourites
                newSettings.MruLimit = oldSettings.MRULimit
                newSettings.MruModules = oldSettings.MRUModules
                newSettings.ShipPanelWidth = oldSettings.ShipPanelWidth
                newSettings.ModPanelWidth = oldSettings.ModPanelWidth
                newSettings.ShipSplitterWidth = oldSettings.ShipSplitterWidth
                newSettings.ModSplitterWidth = oldSettings.ModSplitterWidth
                newSettings.MissileRangeConstant = oldSettings.MissileRangeConstant
                newSettings.IncludeCapReloadTime = oldSettings.IncludeCapReloadTime
                newSettings.IncludeAmmoReloadTime = oldSettings.IncludeAmmoReloadTime
                newSettings.UseLastPilot = oldSettings.UseLastPilot
                newSettings.StorageBayHeight = oldSettings.StorageBayHeight
                newSettings.SlotNameWidth = oldSettings.SlotNameWidth
                For Each ig As ImplantGroup In oldSettings.ImplantGroups.Values
                    Dim ic As New ImplantCollection(True)
                    ic.GroupName = ig.GroupName
                    For slot As Integer = 0 To 10
                        ic.ImplantName(slot) = ig.ImplantName(slot)
                    Next
                    newSettings.ImplantGroups.Add(ic.GroupName, ic)
                Next
                newSettings.ModuleListColWidths = oldSettings.ModuleListColWidths
                newSettings.IgnoredAttributeColumns = oldSettings.IgnoredAttributeColumns
                newSettings.SortedAttributeColumn = oldSettings.SortedAttributeColumn
                newSettings.MetaVariationsFormSize = oldSettings.MetaVariationsFormSize
                newSettings.DefensePanelIsCollapsed = oldSettings.DefensePanelIsCollapsed
                newSettings.CapacitorPanelIsCollapsed = oldSettings.CapacitorPanelIsCollapsed
                newSettings.DamagePanelIsCollapsed = oldSettings.DamagePanelIsCollapsed
                newSettings.TargetingPanelIsCollapsed = oldSettings.TargetingPanelIsCollapsed
                newSettings.PropulsionPanelIsCollapsed = oldSettings.PropulsionPanelIsCollapsed
                newSettings.CargoPanelIsCollapsed = oldSettings.CargoPanelIsCollapsed
                newSettings.SortedModuleListInfo = oldSettings.SortedModuleListInfo
                newSettings.AutoResizeColumns = oldSettings.AutoResizeColumns

                ' Create a JSON string for writing
                Dim json As String = JsonConvert.SerializeObject(newSettings, Newtonsoft.Json.Formatting.Indented)

                ' Write the JSON version of the settings
                Try
                    Using s As New StreamWriter(Path.Combine(hqfFolder, "HQFSettings.json"), False)
                        s.Write(json)
                        s.Flush()
                    End Using
                Catch e As Exception
                End Try

                ' Rename the old file
                My.Computer.FileSystem.RenameFile(Path.Combine(hqfFolder, "HQFSettings.bin"), "OldHQFSettings.bin")

            End If

        Catch e As Exception
            _worker.ReportProgress(0, "Error converting HQF main settings: " & e.Message)
        End Try


    End Sub

    Private Sub ConvertDefenceProfiles(hqfFolder As String)

        Try

            Dim oldProfiles As SortedList

            ' Check for the profiles file so we can load it
            If My.Computer.FileSystem.FileExists(Path.Combine(hqfFolder, "HQFDefenceProfiles.bin")) = True Then
                Using s As New FileStream(Path.Combine(hqfFolder, "HQFDefenceProfiles.bin"), FileMode.Open)
                    Dim f As BinaryFormatter = New BinaryFormatter
                    oldProfiles = CType(f.Deserialize(s), SortedList)
                End Using

                Dim newProfiles As New SortedList(Of String, HQFDefenceProfile)
                For Each profile As DefenceProfile In oldProfiles.Values
                    Dim newProfile As New HQFDefenceProfile
                    newProfile.Name = profile.Name
                    newProfile.Type = CType(profile.Type, ProfileTypes)
                    newProfile.SEm = profile.SEM
                    newProfile.SExplosive = profile.SExplosive
                    newProfile.SKinetic = profile.SKinetic
                    newProfile.SThermal = profile.SThermal
                    newProfile.AEm = profile.AEM
                    newProfile.AExplosive = profile.AExplosive
                    newProfile.AKinetic = profile.AKinetic
                    newProfile.AThermal = profile.AThermal
                    newProfile.HEm = profile.HEM
                    newProfile.HExplosive = profile.HExplosive
                    newProfile.HKinetic = profile.HKinetic
                    newProfile.HThermal = profile.HThermal
                    newProfile.DPS = profile.DPS
                    newProfile.Fitting = profile.Fitting
                    newProfile.Pilot = profile.Pilot
                    newProfiles.Add(newProfile.Name, newProfile)
                Next

                ' Create a JSON string for writing
                Dim json As String = JsonConvert.SerializeObject(newProfiles, Newtonsoft.Json.Formatting.Indented)

                ' Write the JSON version of the settings
                Try
                    Using s As New StreamWriter(Path.Combine(hqfFolder, "HQFDefenceProfiles.json"), False)
                        s.Write(json)
                        s.Flush()
                    End Using
                Catch e As Exception
                End Try

                ' Rename the old settings file
                My.Computer.FileSystem.RenameFile(Path.Combine(hqfFolder, "HQFDefenceProfiles.bin"), "OldHQFDefenceProfiles.bin")

            End If

        Catch e As Exception
            _worker.ReportProgress(0, "Error converting HQF defence profiles: " & e.Message)
        End Try

    End Sub

    Private Sub ConvertDamageProfiles(hqfFolder As String)

        Try

            Dim oldProfiles As SortedList

            ' Check for the profiles file so we can load it
            If My.Computer.FileSystem.FileExists(Path.Combine(hqfFolder, "HQFProfiles.bin")) = True Then
                Using s As New FileStream(Path.Combine(hqfFolder, "HQFProfiles.bin"), FileMode.Open)
                    Dim f As BinaryFormatter = New BinaryFormatter
                    oldProfiles = CType(f.Deserialize(s), SortedList)
                End Using

                Dim newProfiles As New SortedList(Of String, HQFDamageProfile)
                For Each profile As DamageProfile In oldProfiles.Values
                    Dim newProfile As New HQFDamageProfile
                    newProfile.Name = profile.Name
                    newProfile.Type = CType(profile.Type, ProfileTypes)
                    newProfile.EM = profile.EM
                    newProfile.Explosive = profile.Explosive
                    newProfile.Kinetic = profile.Kinetic
                    newProfile.Thermal = profile.Thermal
                    newProfile.DPS = profile.DPS
                    newProfile.Fitting = profile.Fitting
                    newProfile.Pilot = profile.Pilot
                    newProfiles.Add(newProfile.Name, newProfile)
                Next

                ' Create a JSON string for writing
                Dim json As String = JsonConvert.SerializeObject(newProfiles, Newtonsoft.Json.Formatting.Indented)

                ' Write the JSON version of the settings
                Try
                    Using s As New StreamWriter(Path.Combine(hqfFolder, "HQFDamageProfiles.json"), False)
                        s.Write(json)
                        s.Flush()
                    End Using
                Catch e As Exception
                End Try

                ' Rename the old settings file
                My.Computer.FileSystem.RenameFile(Path.Combine(hqfFolder, "HQFProfiles.bin"), "OldHQFProfiles.bin")

            End If

        Catch e As Exception
            _worker.ReportProgress(0, "Error converting HQF damage profiles: " & e.Message)
        End Try


    End Sub

    Private Sub ConvertSavedFittings(hqfFolder As String)

        Try

            Dim fittings As SortedList(Of String, SavedFitting)

            ' Check for the fittings file so we can load it
            If My.Computer.FileSystem.FileExists(Path.Combine(hqfFolder, "Fittings.bin")) = True Then
                Using s As New FileStream(Path.Combine(hqfFolder, "Fittings.bin"), FileMode.Open)
                    Dim f As BinaryFormatter = New BinaryFormatter
                    fittings = CType(f.Deserialize(s), SortedList(Of String, SavedFitting))
                End Using

                ' Create a JSON string for writing
                Dim json As String = JsonConvert.SerializeObject(fittings, Newtonsoft.Json.Formatting.Indented)

                ' Write the JSON version of the settings
                Try
                    Using s As New StreamWriter(Path.Combine(hqfFolder, "Fittings.json"), False)
                        s.Write(json)
                        s.Flush()
                    End Using
                Catch e As Exception
                End Try

                ' Rename the old fittings file
                My.Computer.FileSystem.RenameFile(Path.Combine(hqfFolder, "Fittings.bin"), "OldFittings.bin")

            End If

        Catch e As Exception
            _worker.ReportProgress(0, "Error converting HQF fittings: " & e.Message)
        End Try


    End Sub

    Private Sub ConvertPilots(hqfFolder As String)

        Try

            Dim oldPilots As New SortedList

            If My.Computer.FileSystem.FileExists(Path.Combine(hqfFolder, "HQFPilotSettings.bin")) = True Then
                Try
                    Using s As New FileStream(Path.Combine(hqfFolder, "HQFPilotSettings.bin"), FileMode.Open)
                        Dim f As BinaryFormatter = New BinaryFormatter
                        oldPilots = CType(f.Deserialize(s), SortedList)
                    End Using
                Catch ex As Exception
                End Try


                Dim newPilots As New SortedList(Of String, FittingPilot)
                For Each pilot As HQFPilot In oldPilots.Values
                    newPilots.Add(pilot.PilotName, ConvertPilot(pilot))
                Next

                ' Create a JSON string for writing
                Dim json As String = JsonConvert.SerializeObject(newPilots, Newtonsoft.Json.Formatting.Indented)

                ' Write the JSON version of the settings
                Try
                    Using s As New StreamWriter(Path.Combine(hqfFolder, "HQFPilotSettings.json"), False)
                        s.Write(json)
                        s.Flush()
                    End Using
                Catch e As Exception
                End Try

                ' Rename the old fittings file
                My.Computer.FileSystem.RenameFile(Path.Combine(hqfFolder, "HQFPilotSettings.bin"), "OldHQFPilotSettings.bin")

            End If

        Catch e As Exception
            _worker.ReportProgress(0, "Error converting HQF pilots: " & e.Message)
        End Try

    End Sub

    Private Function ConvertPilot(oldPilot As HQFPilot) As FittingPilot
        Dim newPilot As New FittingPilot
        newPilot.PilotName = oldPilot.PilotName
        newPilot.SkillSet = New Dictionary(Of String, FittingSkill)
        For Each skill As HQFSkill In oldPilot.SkillSet
            Dim newSkill As New FittingSkill
            newSkill.ID = CInt(skill.ID)
            newSkill.Name = skill.Name
            newSkill.Level = skill.Level
            newPilot.SkillSet.Add(newSkill.Name, newSkill)
        Next
        For implant As Integer = 0 To 10
            newPilot.ImplantName(implant) = oldPilot.ImplantName(implant)
        Next
        Return newPilot
    End Function

    Private Sub ConvertCustomShipClasses(hqfFolder As String)

        Try
            Dim custom As SortedList(Of String, CustomShipClass)

            ' Check for the fittings file so we can load it
            If My.Computer.FileSystem.FileExists(Path.Combine(hqfFolder, "CustomShipClasses.bin")) = True Then
                Using s As New FileStream(Path.Combine(hqfFolder, "CustomShipClasses.bin"), FileMode.Open)
                    Dim f As BinaryFormatter = New BinaryFormatter
                    custom = CType(f.Deserialize(s), SortedList(Of String, CustomShipClass))
                End Using

                ' Create a JSON string for writing
                Dim json As String = JsonConvert.SerializeObject(custom, Newtonsoft.Json.Formatting.Indented)

                ' Write the JSON version of the settings
                Try
                    Using s As New StreamWriter(Path.Combine(hqfFolder, "CustomShipClasses.json"), False)
                        s.Write(json)
                        s.Flush()
                    End Using
                Catch e As Exception
                End Try

                ' Rename the old file
                My.Computer.FileSystem.RenameFile(Path.Combine(hqfFolder, "CustomShipClasses.bin"), "OldCustomShipClasses.bin")

            End If

        Catch e As Exception
            _worker.ReportProgress(0, "Error converting HQF custom ship classes: " & e.Message)
        End Try


    End Sub

    Private Sub ConvertCustomShips(hqfFolder As String)

        Try

            Dim custom As SortedList(Of String, CustomShip)

            ' Check for the fittings file so we can load it
            If My.Computer.FileSystem.FileExists(Path.Combine(hqfFolder, "CustomShips.bin")) = True Then
                Using s As New FileStream(Path.Combine(hqfFolder, "CustomShips.bin"), FileMode.Open)
                    Dim f As BinaryFormatter = New BinaryFormatter
                    custom = CType(f.Deserialize(s), SortedList(Of String, CustomShip))
                End Using

                ' Create a JSON string for writing
                Dim json As String = JsonConvert.SerializeObject(custom, Newtonsoft.Json.Formatting.Indented)

                ' Write the JSON version of the settings
                Try
                    Using s As New StreamWriter(Path.Combine(hqfFolder, "CustomShips.json"), False)
                        s.Write(json)
                        s.Flush()
                    End Using
                Catch e As Exception
                End Try

                ' Rename the old file
                My.Computer.FileSystem.RenameFile(Path.Combine(hqfFolder, "CustomShips.bin"), "OldCustomShips.bin")

            End If

        Catch e As Exception
            _worker.ReportProgress(0, "Error converting HQF custom ships: " & e.Message)
        End Try

    End Sub

#End Region

End Class
