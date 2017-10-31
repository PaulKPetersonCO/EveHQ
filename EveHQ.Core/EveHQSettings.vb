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

Imports System.Drawing
Imports DevComponents.DotNetBar
Imports System.Windows.Forms
Imports EveHQ.Market
Imports EveHQ.Common.Extensions
Imports System.Globalization
Imports System.IO
Imports Newtonsoft.Json
Imports System.Text

''' <summary>
''' Class for the new EveHQ settings.
''' </summary>
''' <remarks></remarks>
<Serializable()>
Public Class EveHQSettings

#Region "Private Fields"
    Private _marketSystem As Integer
    Private _marketDataProvider As String
    Private _maxUpdateThreads As Integer
    Private _corporations As SortedList(Of String, Corporation)
    Private _priceGroups As SortedList(Of String, PriceGroup)
    Private _skillQueuePanelWidth As Integer
    Private _automaticSaveTime As Integer
    Private _sqlQueries As SortedList(Of String, String)
    Private _emailSenderAddress As String
    Private _userQueueColumns As ArrayList
    Private _standardQueueColumns As ArrayList
    Private _dashboardConfiguration As New List(Of SortedList(Of String, Object))
    Private _csvSeparatorChar As String
    Private _marketRegionList As ArrayList
    Private _priceCriteria(11) As Boolean
    Private _ccpApiServerAddress As String
	<JsonProperty>
    Private _eveFolderLabel(4) As String
	<JsonProperty>
    Private _eveFolderLua(4) As Boolean
    Private _igbAllowedData As SortedList(Of String, Boolean)
	<JsonProperty>
    Private _eveFolder(4) As String
    Private _qColumns(20, 1) As String
    Private _marketStatOverrides As Dictionary(Of Integer, ItemMarketOverride)
    Private _marketRegions As List(Of Integer)
    Private _pilots As Dictionary(Of String, EveHQPilot)
    Private _accounts As Dictionary(Of String, EveHQAccount)
    Private _plugins As Dictionary(Of String, EveHQPlugInConfig)
#End Region

#Region "Constructors"

    Public Sub New()

        ' Initialise new settings
        Call InitialiseSettings()

    End Sub

#End Region

#Region "Private Constants"

    Private Const OfficialApiLocation As String = "https://api.eveonline.com"

#End Region

#Region "Public Properties"

    Public Property MarketDataProvider As String
        Get
            If _marketDataProvider.IsNullOrWhiteSpace() = True Then
                _marketDataProvider = MarketProvider.EveCentral.ToString()
            End If
            Return _marketDataProvider
        End Get
        Set(value As String)
            _marketDataProvider = value
        End Set
    End Property
    Public Property MaxUpdateThreads As Integer
        Get
            If _maxUpdateThreads = 0 Then _maxUpdateThreads = 5
            Return _maxUpdateThreads
        End Get
        Set(value As Integer)
            _maxUpdateThreads = value
        End Set
    End Property
    Public Property MarketDataSource As MarketSite
    Public Property Corporations As SortedList(Of String, Corporation)
        Get
            If _corporations Is Nothing Then
                _corporations = New SortedList(Of String, Corporation)
            End If
            Return _corporations
        End Get
        Set(value As SortedList(Of String, Corporation))
            _corporations = value
        End Set
    End Property
    Public Property PriceGroups As SortedList(Of String, PriceGroup)
        Get
            If _priceGroups Is Nothing Then
                _priceGroups = New SortedList(Of String, PriceGroup)
            End If
            Return _priceGroups
        End Get
        Set(ByVal value As SortedList(Of String, PriceGroup))
            _priceGroups = value
        End Set
    End Property
    Public Property SkillQueuePanelWidth() As Integer
        Get
            If _skillQueuePanelWidth = 0 Then
                _skillQueuePanelWidth = 440
            End If
            Return _skillQueuePanelWidth
        End Get
        Set(ByVal value As Integer)
            _skillQueuePanelWidth = value
        End Set
    End Property
    Public Property AccountTimeLimit() As Integer
    Public Property NotifyAccountTime() As Boolean
    Public Property NotifyInsuffClone() As Boolean
    Public Property StartWithPrimaryQueue() As Boolean
    Public Property IgnoreLastMessage() As Boolean
    Public Property LastMessageDate() As Date
    Public Property DisableTrainingBar() As Boolean
    Public Property EnableAutomaticSave() As Boolean
    Public Property AutomaticSaveTime() As Integer
        Get
            If _automaticSaveTime < 0 Then _automaticSaveTime = 60
            Return _automaticSaveTime
        End Get
        Set(ByVal value As Integer)
            _automaticSaveTime = value
        End Set
    End Property
    Public Property RibbonMinimised() As Boolean
    Public Property ThemeSetByUser() As Boolean
    Public Property ThemeCanvas As Color
    Public Property ThemeTint() As Color
    Public Property ThemeStyle() As eStyle
    Public Property SQLQueries() As SortedList(Of String, String)
        Get
            If _sqlQueries Is Nothing Then
                _sqlQueries = New SortedList(Of String, String)
            End If
            Return _sqlQueries
        End Get
        Set(ByVal value As SortedList(Of String, String))
            _sqlQueries = value
        End Set
    End Property
    Public Property BackupBeforeUpdate() As Boolean
    Public Property QatLayout() As String
    Public Property NotifyEveNotification() As Boolean
    Public Property NotifyEveMail() As Boolean
    Public Property AutoMailAPI() As Boolean
    Public Property EveHqBackupWarnFreq() As Integer
    Public Property EveHqBackupMode() As Integer
    Public Property EveHqBackupStart() As Date
    Public Property EveHqBackupFreq() As Integer
    Public Property EveHqBackupLast() As Date
    Public Property EveHqBackupLastResult() As Integer
    Public Property IbShowAllItems() As Boolean
    Public Property EmailSenderAddress() As String
        Get
            If _emailSenderAddress Is Nothing Then
                _emailSenderAddress = "contact.evehq@gmail.com"
            End If
            Return _emailSenderAddress
        End Get
        Set(ByVal value As String)
            If value IsNot Nothing Then
                _emailSenderAddress = value
            End If
        End Set
    End Property
    Public Property UserQueueColumns() As ArrayList
        Get
            If _userQueueColumns Is Nothing Then
                _userQueueColumns = New ArrayList
            End If
            Return _userQueueColumns
        End Get
        Set(ByVal value As ArrayList)
            _userQueueColumns = value
        End Set
    End Property
    Public Property StandardQueueColumns() As ArrayList
        Get
            If _standardQueueColumns Is Nothing Then
                _standardQueueColumns = New ArrayList
            End If
            Return _standardQueueColumns
        End Get
        Set(ByVal value As ArrayList)
            _standardQueueColumns = value
        End Set
    End Property
    Public Property DBTickerLocation() As String
    Public Property DBTicker() As Boolean
    Public Property DashboardConfiguration() As List(Of SortedList(Of String, Object))
        Get
            If _dashboardConfiguration Is Nothing Then
                _dashboardConfiguration = New List(Of SortedList(Of String, Object))
            End If
            Return _dashboardConfiguration
        End Get
        Set(ByVal value As List(Of SortedList(Of String, Object)))
            _dashboardConfiguration = value
        End Set
    End Property
    Public Property CsvSeparatorChar() As String
        Get
            If _csvSeparatorChar Is Nothing Then
                _csvSeparatorChar = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator
            End If
            Return _csvSeparatorChar
        End Get
        Set(ByVal value As String)
            _csvSeparatorChar = value
        End Set
    End Property
    Public Property DisableVisualStyles() As Boolean
    Public Property DisableAutoWebConnections() As Boolean
    Public Property TrainingBarHeight() As Integer
    Public Property TrainingBarWidth() As Integer
    Public Property TrainingBarDockPosition() As Integer
    Public Property MdiTabPosition() As String
    Public Property MarketRegionList() As ArrayList
        Get
            If _marketRegionList Is Nothing Then
                _marketRegionList = New ArrayList
            End If
            Return _marketRegionList
        End Get
        Set(ByVal value As ArrayList)
            _marketRegionList = value
        End Set
    End Property
    Public Property IgnoreBuyOrderLimit() As Double
    Public Property IgnoreSellOrderLimit() As Double
    Public Property PriceCriteria(ByVal index As Integer) As Boolean
        Get
            If _priceCriteria Is Nothing Then
                ReDim _priceCriteria(11)
            End If
            Return _priceCriteria(index)
        End Get
        Set(ByVal value As Boolean)
            _priceCriteria(index) = value
        End Set
    End Property
    Public Property MarketLogUpdateData() As Boolean
    Public Property MarketLogUpdatePrice() As Boolean
    Public Property MarketLogPopupConfirm() As Boolean
    Public Property MarketLogToolTipConfirm() As Boolean
    Public Property IgnoreBuyOrders() As Boolean
    Public Property IgnoreSellOrders() As Boolean
    Public Property CustomDBFileName As String
    Public Property DBTimeout() As Integer
    Public Property PilotSkillHighlightColor() As Long
    Public Property PilotSkillTextColor() As Long
    Public Property PilotGroupTextColor() As Long
    Public Property PilotGroupBackgroundColor() As Long
    Public Property ErrorReportingEmail() As String
    Public Property ErrorReportingName() As String
    Public Property ErrorReportingEnabled() As Boolean
    Public Property TaskbarIconMode() As Integer
    Public Property EcmDefaultLocation() As String
    Public Property APIFileExtension() As String
    Public Property UseAppDirectoryForDB() As Boolean
    Public Property OmitCurrentSkill() As Boolean
    Public Property UpdateUrl() As String
    Public Property UseCcpapiBackup() As Boolean
    Public Property UseApirs() As Boolean
    Public Property ApirsAddress() As String
    Public Property CcpapiServerAddress() As String
        Get
            'Bug #75: Broken API update due to CCP disabling HTTP endpoint
            'Resolution: Now the code will check and forcibly update the api location to https 
            'Any time it is retrieved or saved.
            _ccpApiServerAddress = ForceHttpsOnCcpEndpoints(_ccpApiServerAddress)
            Return _ccpApiServerAddress
        End Get
        Set(ByVal value As String)
            _ccpApiServerAddress = ForceHttpsOnCcpEndpoints(value)
        End Set
    End Property
    Public Property EveFolderLabel(ByVal index As Integer) As String
        Get
            If _eveFolderLabel Is Nothing Then
                ReDim _eveFolderLabel(4)
            End If
            If index < 1 Or index > 4 Then
                MessageBox.Show("Eve Folder Label index must be in the range 1 to 4", "Eve Folder Label Get Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return "0"
            Else
                Return _eveFolderLabel(index)
            End If
        End Get
        Set(ByVal value As String)
            If index < 1 Or index > 4 Then
                MessageBox.Show("Eve Folder Label index must be in the range 1 to 4", "Eve Folder Label Set Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                _eveFolderLabel(index) = value
            End If
        End Set
    End Property
    Public Property PilotCurrentTrainSkillColor() As Long
    Public Property PilotPartTrainedSkillColor() As Long
    Public Property PilotLevel5SkillColor() As Long
    Public Property PilotStandardSkillColor() As Long
    Public Property PanelHighlightColor() As Long
    Public Property PanelTextColor() As Long
    Public Property PanelRightColor() As Long
    Public Property PanelLeftColor() As Long
    Public Property PanelBottomRightColor() As Long
    Public Property PanelTopLeftColor() As Long
    Public Property PanelOutlineColor() As Long
    Public Property PanelBackgroundColor() As Long
    Public Property LastMarketPriceUpdate() As DateTime
    Public Property LastFactionPriceUpdate() As DateTime
    Public Property EveFolderLua(ByVal index As Integer) As Boolean
        Get
            If _eveFolderLua Is Nothing Then
                ReDim _eveFolderLua(4)
            End If
            If index < 1 Or index > 4 Then
                MessageBox.Show("Eve Folder LUA index must be in the range 1 to 4", "Eve Folder Get Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return False
            Else
                Return _eveFolderLua(index)
            End If
        End Get
        Set(ByVal value As Boolean)
            If index < 1 Or index > 4 Then
                MessageBox.Show("Eve Folder LUA index must be in the range 1 to 4", "Eve Folder Set Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                _eveFolderLua(index) = value
            End If
        End Set
    End Property
    Public Property CycleG15Time() As Integer
    Public Property CycleG15Pilots() As Boolean
    Public Property ActivateG15() As Boolean
    Public Property AutoAPI() As Boolean
    Public Property MainFormWindowState As FormWindowState
    Public Property MainFormLocation As Point
    Public Property MainFormSize As Size
    'Public Property MainFormPosition(ByVal index As Integer) As Integer
    '    Get
    '        If _mainFormPosition Is Nothing Then
    '            ReDim _mainFormPosition(4)
    '        End If
    '        If index < 0 Or index > 4 Then
    '            MessageBox.Show("Eve Main Form Position index must be in the range 0 to 4",
    '                            "Eve Main Form Position Get Error", MessageBoxButtons.OK, MessageBoxIcon.Information)
    '            Return 0
    '        Else
    '            Return _mainFormPosition(index)
    '        End If
    '    End Get
    '    Set(ByVal value As Integer)
    '        If index < 0 Or index > 4 Then
    '            MessageBox.Show("Eve Main Form Position index must be in the range 0 to 4",
    '                            "Eve Main Form Position Set Error", MessageBoxButtons.OK, MessageBoxIcon.Information)
    '        Else
    '            _mainFormPosition(index) = value
    '        End If
    '    End Set
    'End Property
    Public Property DeleteSkills() As Boolean
    Public Property PartialTrainColor() As Long
    Public Property ReadySkillColor() As Long
    Public Property IsPreReqColor() As Long
    Public Property HasPreReqColor() As Long
    Public Property BothPreReqColor() As Long
    Public Property DtClashColor() As Long
    Public Property ColorHighlightQueuePreReq() As String
    Public Property ColorHighlightQueueTraining() As String
    Public Property ColorHighlightPilotTraining() As String
    Public Property ContinueTraining() As Boolean
    Public Property EMailPassword() As String
    Public Property EMailUsername() As String
    Public Property UseSsl() As Boolean
    Public Property UseSmtpAuth() As Boolean
    Public Property EMailAddress() As String
    Public Property EMailPort() As Integer
    Public Property EMailServer() As String
    Public Property NotifySoundFile() As String
    Public Property NotifyOffset() As Integer
    Public Property NotifyEarly() As Boolean
    Public Property NotifyNow() As Boolean
    Public Property NotifySound() As Boolean
    Public Property NotifyEMail() As Boolean
    Public Property NotifyDialog() As Boolean
    Public Property NotifyToolTip() As Boolean
    Public Property ShutdownNotifyPeriod() As Integer
    Public Property ShutdownNotify() As Boolean
    Public Property ServerOffset() As Integer
    Public Property EnableEveStatus() As Boolean
    Public Property ProxyUseDefault() As Boolean
    Public Property ProxyUseBasic() As Boolean
    Public Property ProxyPassword() As String
    Public Property ProxyUsername() As String
    Public Property ProxyPort() As Integer
    Public Property ProxyServer() As String
    Public Property ProxyRequired() As Boolean
    Public Property IgbPort() As Integer
    Public Property IgbAutoStart() As Boolean
    Public Property IgbFullMode() As Boolean
    Public Property IgbAllowedData() As SortedList(Of String, Boolean)
        Get
            If _igbAllowedData Is Nothing Then
                _igbAllowedData = New SortedList(Of String, Boolean)
            End If
            Return _igbAllowedData
        End Get
        Set(ByVal value As SortedList(Of String, Boolean))
            _igbAllowedData = value
        End Set
    End Property
    Public Property AutoHide() As Boolean
    Public Property AutoStart() As Boolean
    Public Property AutoCheck() As Boolean
    Public Property MinimiseExit() As Boolean
    Public Property AutoMinimise() As Boolean
    Public Property StartupPilot() As String
    Public Property StartupForms As Integer
    Public Property EveFolder(ByVal index As Integer) As String
        Get
            If _eveFolder Is Nothing Then
                ReDim _eveFolder(4)
            End If
            If index < 1 Or index > 4 Then
                MessageBox.Show("Eve Folder index must be in the range 1 to 4", "Eve Folder Get Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return "0"
            Else
                Return _eveFolder(index)
            End If
        End Get
        Set(ByVal value As String)
            If index < 1 Or index > 4 Then
                MessageBox.Show("Eve Folder index must be in the range 1 to 4", "Eve Folder Set Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                _eveFolder(index) = value
            End If
        End Set
    End Property
    Public Property BackupAuto() As Boolean
    Public Property BackupStart() As Date
    Public Property BackupFreq() As Integer
    Public Property BackupLast() As Date
    Public Property BackupLastResult() As Integer
    Public Property QColumnsSet() As Boolean
    Public Property QColumns(ByVal col As Integer, ByVal ref As Integer) As String
        Get
            If _qColumns Is Nothing Then
                ReDim _qColumns(20, 1)
            End If
            Return _qColumns(col, ref)
        End Get
        Set(ByVal value As String)
            If _qColumns.GetUpperBound(0) < 19 Then
                ReDim _qColumns(20, 1)
            End If
            _qColumns(col, ref) = value
        End Set
    End Property
    Public Property Accounts() As Dictionary(Of String, EveHQAccount)
        Get
            If _accounts Is Nothing Then
                _accounts = New Dictionary(Of String, EveHQAccount)
            End If
            Return _accounts
        End Get
        Set(ByVal value As Dictionary(Of String, EveHQAccount))
            _accounts = value
        End Set
    End Property
    Public Property Plugins() As Dictionary(Of String, EveHQPlugInConfig)
        Get
            If _plugins Is Nothing Then
                _plugins = New Dictionary(Of String, EveHQPlugInConfig)
            End If
            Return _plugins
        End Get
        Set(ByVal value As Dictionary(Of String, EveHQPlugInConfig))
            _plugins = value
        End Set
    End Property
    Public Property Pilots() As Dictionary(Of String, EveHQPilot)
        Get
            If _pilots Is Nothing Then
                _pilots = New Dictionary(Of String, EveHQPilot)
            End If
            Return _pilots
        End Get
        Set(ByVal value As Dictionary(Of String, EveHQPilot))
            _pilots = value
        End Set
    End Property
    Public Property MarketRegions As List(Of Integer)
        Get
            If (_marketRegions Is Nothing) Then
                _marketRegions = New List(Of Integer)
                _marketRegions.Add(10000002) ' The Forge... safe default.
            End If

            Return _marketRegions
        End Get
        Set(value As List(Of Integer))
            _marketRegions = value
        End Set
    End Property
    Public Property MarketSystem As Integer
        Get
            If _marketSystem = 0 Then
                _marketSystem = 30000142
            End If
            Return _marketSystem
        End Get
        Set(value As Integer)
            _marketSystem = value
        End Set
    End Property
    Public Property MarketUseRegionMarket As Boolean
    Public Property MarketDefaultMetric As MarketMetric

    Public Property MarketStatOverrides As Dictionary(Of Integer, ItemMarketOverride)
        Get
            If (_marketStatOverrides Is Nothing) Then
                _marketStatOverrides = New Dictionary(Of Integer, ItemMarketOverride)
            End If

            Return _marketStatOverrides
        End Get
        Set(value As Dictionary(Of Integer, ItemMarketOverride))
            _marketStatOverrides = value
        End Set
    End Property
    Public Property MarketDefaultTransactionType As MarketTransactionKind
    Public Property EveQueueDisplayLength As Integer = 1
#End Region

    Private Sub InitialiseSettings()
        ' Initialise settings that are non-default
        IgbPort = 26001
        AutoHide = True
        BackupStart = Now
        BackupFreq = 1
        BackupLast = New DateTime(1999, 1, 1)
        EnableAutomaticSave = True
        AutomaticSaveTime = 15
        ProxyUseDefault = True
        ShutdownNotifyPeriod = 8
        EMailPort = 25
        IsPreReqColor = Color.LightSteelBlue.ToArgb
        HasPreReqColor = Color.White.ToArgb
        BothPreReqColor = Color.White.ToArgb
        DtClashColor = Color.Red.ToArgb
        ReadySkillColor = Color.White.ToArgb
        PartialTrainColor = Color.White.ToArgb
        CycleG15Time = 15
        PanelBackgroundColor = Color.Navy.ToArgb
        PanelOutlineColor = Color.SteelBlue.ToArgb
        PanelTopLeftColor = Color.LightSteelBlue.ToArgb
        PanelBottomRightColor = Color.LightSteelBlue.ToArgb
        PanelLeftColor = Color.RoyalBlue.ToArgb
        PanelRightColor = Color.LightSteelBlue.ToArgb
        PanelTextColor = Color.Black.ToArgb
        PanelHighlightColor = Color.LightSteelBlue.ToArgb
        PilotStandardSkillColor = Color.White.ToArgb
        PilotLevel5SkillColor = Color.Thistle.ToArgb
        PilotPartTrainedSkillColor = Color.Gold.ToArgb
        PilotCurrentTrainSkillColor = Color.LimeGreen.ToArgb
        CcpapiServerAddress = OfficialApiLocation
        UpdateUrl = "http://evehq.co/update/"
        APIFileExtension = "aspx"
        PilotGroupBackgroundColor = Color.DimGray.ToArgb
        PilotGroupTextColor = Color.White.ToArgb
        PilotSkillTextColor = Color.Black.ToArgb
        PilotSkillHighlightColor = Color.DodgerBlue.ToArgb
        DBTimeout = 30
        IgnoreBuyOrders = True
        IgnoreSellOrderLimit = 1000
        IgnoreBuyOrderLimit = 1
        MdiTabPosition = "Top"
        TrainingBarHeight = 54
        TrainingBarWidth = 100
        CsvSeparatorChar = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator
        DBTickerLocation = "Bottom"
        EmailSenderAddress = "contact.evehq@gmail.com"
        EveHqBackupStart = Now
        EveHqBackupFreq = 1
        EveHqBackupLast = New DateTime(1999, 1, 1)
        EveHqBackupWarnFreq = 1
        ThemeStyle = eStyle.Office2007Black
        ThemeTint = Color.Empty
        LastMessageDate = New DateTime(1999, 1, 1)
        AccountTimeLimit = 168
        SkillQueuePanelWidth = 440
        MarketSystem = 30000142 'Safe Default of Jita
        MaxUpdateThreads = 5
        MainFormWindowState = FormWindowState.Maximized
        StartupPilot = ""
        StartupForms = 0

    End Sub

    ''' <summary>
    '''     Validates that when "official" CCP api endpoints are used, the http scheme is forced to https.
    '''     Also the older eve-online.com domain will be changed to eveonline.com
    ''' </summary>
    ''' <remarks></remarks>
    Private Shared Function ForceHttpsOnCcpEndpoints(endpoint As String) As String
        Const OldApi1 As String = "http://api.eveonline.com"
        Const OldApi2 As String = "http://api.eve-online.com"
        Const BadApi As String = "https://api.eve-online.com" 'this https endpoint isn't supported anymore

        Dim normalizedEndpoint As String = endpoint.ToLowerInvariant()

        If (normalizedEndpoint = OldApi1 Or normalizedEndpoint = OldApi2 Or normalizedEndpoint = BadApi) Then
            normalizedEndpoint = OfficialApiLocation
        End If

        Return normalizedEndpoint
    End Function

   Public Shared Sub ResetColumns()
        HQ.Settings.QColumns(0, 0) = "Name"
        HQ.Settings.QColumns(0, 1) = CStr(True)
        HQ.Settings.QColumns(1, 0) = "Curr"
        HQ.Settings.QColumns(1, 1) = CStr(True)
        HQ.Settings.QColumns(2, 0) = "From"
        HQ.Settings.QColumns(2, 1) = CStr(True)
        HQ.Settings.QColumns(3, 0) = "Tole"
        HQ.Settings.QColumns(3, 1) = CStr(True)
        HQ.Settings.QColumns(4, 0) = "Perc"
        HQ.Settings.QColumns(4, 1) = CStr(True)
        HQ.Settings.QColumns(5, 0) = "Trai"
        HQ.Settings.QColumns(5, 1) = CStr(True)
        HQ.Settings.QColumns(6, 0) = "Comp"
        HQ.Settings.QColumns(6, 1) = CStr(True)
        HQ.Settings.QColumns(7, 0) = "Date"
        HQ.Settings.QColumns(7, 1) = CStr(True)
        HQ.Settings.QColumns(8, 0) = "Rank"
        HQ.Settings.QColumns(8, 1) = CStr(False)
        HQ.Settings.QColumns(9, 0) = "PAtt"
        HQ.Settings.QColumns(9, 1) = CStr(False)
        HQ.Settings.QColumns(10, 0) = "SAtt"
        HQ.Settings.QColumns(10, 1) = CStr(False)
        HQ.Settings.QColumns(11, 0) = "SPRH"
        HQ.Settings.QColumns(11, 1) = CStr(False)
        HQ.Settings.QColumns(12, 0) = "SPRD"
        HQ.Settings.QColumns(12, 1) = CStr(False)
        HQ.Settings.QColumns(13, 0) = "SPRW"
        HQ.Settings.QColumns(13, 1) = CStr(False)
        HQ.Settings.QColumns(14, 0) = "SPRM"
        HQ.Settings.QColumns(14, 1) = CStr(False)
        HQ.Settings.QColumns(15, 0) = "SPRY"
        HQ.Settings.QColumns(15, 1) = CStr(False)
        HQ.Settings.QColumns(16, 0) = "SPAd"
        HQ.Settings.QColumns(16, 1) = CStr(False)
        HQ.Settings.QColumns(17, 0) = "SPTo"
        HQ.Settings.QColumns(17, 1) = CStr(False)
        HQ.Settings.QColumns(18, 0) = "Note"
        HQ.Settings.QColumns(18, 1) = CStr(False)
        HQ.Settings.QColumns(19, 0) = "Prio"
        HQ.Settings.QColumns(19, 1) = CStr(False)
        HQ.Settings.QColumnsSet = True
    End Sub

    Public Sub Save()

        Dim fileName As String = Path.Combine(HQ.AppDataFolder, "EveHQSettings.json")
        HQ.WriteLogEvent("Settings: Saving EveHQ settings to " & fileName)

        ' Convert the current settings to a JSON formatted string
        Dim json As String = JsonConvert.SerializeObject(Me, Formatting.Indented)

        ' Write the JSON string to the file
        Try
            Using s As New StreamWriter(fileName, False)
                s.Write(json)
                s.Flush()
            End Using
        Catch e As Exception
            HQ.WriteLogEvent("Settings: Error saving EveHQ settings to " & Path.Combine(HQ.AppDataFolder, "EveHQSettings.bin - " & e.Message))
        End Try

        ' Update the Proxy Server settings
        Call InitialiseRemoteProxyServer()

        ' Set Global APIServerInfo
        'HQ.EveHqapiServerInfo = New APIServerInfo(HQ.Settings.CcpapiServerAddress, HQ.Settings.ApirsAddress,
        '                                          HQ.Settings.UseApirs, HQ.Settings.UseCcpapiBackup)

    End Sub

    Public Shared Function Load(showRawData As Boolean) As Boolean

        If My.Computer.FileSystem.FileExists(Path.Combine(HQ.AppDataFolder, "EveHQSettings.json")) = True Then
            Try
                Using s As New StreamReader(Path.Combine(HQ.AppDataFolder, "EveHQSettings.json"))
                    Dim json As String = s.ReadToEnd
                    HQ.Settings = JsonConvert.DeserializeObject(Of EveHQSettings)(json)
                End Using

            Catch ex As Exception
                Trace.TraceError(ex.FormatException())
                Dim msg As String = "There was an error trying to load the settings file and it appears that this file is corrupt." & ControlChars.CrLf & ControlChars.CrLf
                msg &= "The error was: " & ex.Message & ControlChars.CrLf & ControlChars.CrLf
                msg &= "Stacktrace: " & ex.StackTrace & ControlChars.CrLf & ControlChars.CrLf
                msg &= "EveHQ will copy this file to 'EveHQSettings.bad' and delete the original file and re-initialise the settings. This means you will need to re-enter your API information but your production and fittings data should be intact and available once the API data has been downloaded. You can attempt to reload the old settings by renaming the 'EveHQSettings.bad' file to 'EveHQSettings.bin', however if the issue continues the bad file will be useful to the EveHQ team for debugging purposes" & ControlChars.CrLf & ControlChars.CrLf
                msg &= "Press OK to reset the settings." & ControlChars.CrLf
                MessageBox.Show(msg, "Invalid Settings file detected", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Try
                    My.Computer.FileSystem.CopyFile(Path.Combine(HQ.AppDataFolder, "EveHQSettings.json"), Path.Combine(HQ.AppDataFolder, "EveHQSettings.bad"), True)
                Catch e As Exception
                    MessageBox.Show("Unable to delete the EveHQSettings.json file. Please delete this manually before proceeding", "Delete File Error", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Application.Exit()
                End Try
                Return False
            End Try
        Else
            ' Create a new settings file
            HQ.Settings = New EveHQSettings
        End If

        If HQ.Settings Is Nothing Then
            MessageBox.Show("There was an issue loading the settings file: It was empty. Please delete the EveHQSettigns.json file manually and restore from backup.")
            Return False
        End If

        If showRawData = False Then

            ' Reset the update URL to a temp location
            If HQ.Settings.UpdateUrl <> "http://evehq.co/update/" Then
                HQ.Settings.UpdateUrl = "http://evehq.co/update/"
            End If

            ' Set the Custom database connection
            Try
                If CustomDataFunctions.SetEveHQDataConnectionString() = False Then
                    Return False
                End If
            Catch ex As Exception
                Dim msg As New StringBuilder
                msg.AppendLine("Error: " & ex.Message)
                msg.AppendLine("")
                msg.AppendLine("An error occurred trying to set the custom database connection string. This could be down to a missing database library file.")
                MessageBox.Show(msg.ToString, "Error Initialising Database", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Try

            '  Setup queue columns etc
            Call InitialiseQueueColumns()
            Call InitialiseUserColumns()
            Call InitialiseRemoteProxyServer()
            If HQ.Settings.QColumns(0, 0) Is Nothing Then
                Call ResetColumns()
            End If

            ' Set Theme stuff
            If HQ.Settings.ThemeSetByUser = False Then
                HQ.Settings.ThemeStyle = eStyle.Office2007Black
                HQ.Settings.ThemeTint = Color.Empty
            End If

            ' Set Global APIServerInfo
            'HQ.EveHQAPIServerInfo = New APIServerInfo(HQ.Settings.CcpapiServerAddress, HQ.Settings.ApirsAddress, HQ.Settings.UseApirs, HQ.Settings.UseCcpapiBackup)

        End If

        Return True

    End Function

    Public Shared Sub InitialiseRemoteProxyServer()
        HQ.RemoteProxy.ProxyRequired = HQ.Settings.ProxyRequired
        HQ.RemoteProxy.ProxyServer = HQ.Settings.ProxyServer
        HQ.RemoteProxy.ProxyPort = HQ.Settings.ProxyPort
        HQ.RemoteProxy.UseDefaultCredentials = HQ.Settings.ProxyUseDefault
        HQ.RemoteProxy.ProxyUsername = HQ.Settings.ProxyUsername
        HQ.RemoteProxy.ProxyPassword = HQ.Settings.ProxyPassword
        HQ.RemoteProxy.UseBasicAuthentication = HQ.Settings.ProxyUseBasic
    End Sub

    Public Shared Sub InitialiseQueueColumns()
        HQ.Settings.StandardQueueColumns.Clear()
        Dim newItem As ListViewItem
        'newItem = New ListViewItem
        'newItem.Name = "Name"
        'newItem.Text = "Skill Name"
        'newItem.Checked = True
        'EveHQ.Core.HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "Current"
        newItem.Text = "Cur Lvl"
        newItem.Checked = True
        HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "From"
        newItem.Text = "From Lvl"
        newItem.Checked = True
        HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "To"
        newItem.Text = "To Lvl"
        newItem.Checked = True
        HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "Percent"
        newItem.Text = "%"
        newItem.Checked = True
        HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "TrainTime"
        newItem.Text = "Training Time"
        newItem.Checked = True
        HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "TimeToComplete"
        newItem.Text = "Time To Complete"
        newItem.Checked = True
        HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "DateEnded"
        newItem.Text = "Date Completed"
        newItem.Checked = True
        HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "Rank"
        newItem.Text = "Rank"
        newItem.Checked = False
        HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "PAtt"
        newItem.Text = "Pri Att"
        newItem.Checked = False
        HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "SAtt"
        newItem.Text = "Sec Att"
        newItem.Checked = False
        HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "SPHour"
        newItem.Text = "SP /hour"
        newItem.Checked = False
        HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "SPDay"
        newItem.Text = "SP /day"
        newItem.Checked = False
        HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "SPWeek"
        newItem.Text = "SP /week"
        newItem.Checked = False
        HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "SPMonth"
        newItem.Text = "SP /month"
        newItem.Checked = False
        HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "SPYear"
        newItem.Text = "SP /year"
        newItem.Checked = False
        HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "SPAdded"
        newItem.Text = "SP Added"
        newItem.Checked = False
        HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "SPTotal"
        newItem.Text = "SP Total"
        newItem.Checked = False
        HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "Notes"
        newItem.Text = "Notes"
        newItem.Checked = False
        HQ.Settings.StandardQueueColumns.Add(newItem)
        newItem = New ListViewItem
        newItem.Name = "Priority"
        newItem.Text = "Priority"
        newItem.Checked = False
        HQ.Settings.StandardQueueColumns.Add(newItem)
    End Sub

    Public Shared Sub InitialiseUserColumns()
        If HQ.Settings.UserQueueColumns.Count = 0 Then
            ' Add preset items
            HQ.Settings.UserQueueColumns.Add("Current1")
            HQ.Settings.UserQueueColumns.Add("From1")
            HQ.Settings.UserQueueColumns.Add("To1")
            HQ.Settings.UserQueueColumns.Add("Percent1")
            HQ.Settings.UserQueueColumns.Add("TrainTime1")
            HQ.Settings.UserQueueColumns.Add("TimeToComplete1")
            HQ.Settings.UserQueueColumns.Add("DateEnded1")
        End If
        ' Check if the standard columns have changed and we need to add columns
        If HQ.Settings.UserQueueColumns.Count <> HQ.Settings.StandardQueueColumns.Count Then
            For Each slotItem As ListViewItem In HQ.Settings.StandardQueueColumns
                If _
                    HQ.Settings.UserQueueColumns.Contains(slotItem.Name & "0") = False And
                    HQ.Settings.UserQueueColumns.Contains(slotItem.Name & "1") = False Then
                    HQ.Settings.UserQueueColumns.Add(slotItem.Name & "0")
                End If
            Next
        End If
    End Sub

End Class

