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

Imports System.Windows.Forms
Imports EveHQ.Common
Imports DevComponents.DotNetBar
Imports EveHQ.NewEveApi
Imports EveHQ.Market
Imports System.IO
Imports EveHQ.Common.Logging
Imports EveHQ.Common.Extensions
Imports EveHQ.Market.MarketServices

Public Class HQ
    Private Declare Auto Function SetProcessWorkingSetSize Lib "kernel32.dll" (ByVal procHandle As IntPtr, ByVal min As Int32, ByVal max As Int32) As Boolean

    Public Shared MainForm As Form
    Private Shared tempPilots1 As New SortedList(Of String, EveHQPilot)
    Public Shared TempCorps As New SortedList(Of String, Corporation)
    Public Shared MyTqServer As EveServer = New EveServer
    Public Shared SkillListName As New Dictionary(Of String, EveSkill) ' SkillName, EveSkill
    Public Shared SkillListID As New SortedList(Of Integer, EveSkill) ' SkillID, EveSkill
    Public Shared SkillGroups As New Dictionary(Of String, SkillGroup)
    Public Shared IsUsingLocalFolders As Boolean = False
    Public Shared IsSplashFormDisabled As Boolean = False
    Private Shared _appDataFolder As String = ""
    Public Shared AppFolder As String = ""
    Public Shared ApiCacheFolder As String = ""
    Public Shared StaticDataFolder As String = ""
    Public Shared ImageCacheFolder As String = ""
    Public Shared ReportFolder As String = ""
    Public Shared BackupFolder As String = ""
    Public Shared EveHQBackupFolder As String = ""
    Public Shared ItemDBConnectionString As String = ""
    Public Shared EveHQDataConnectionString As String = ""
    Public Shared DataError As String = ""
    Public Shared IGBActive As Boolean = False
    Public Shared APIResults As New SortedList
    Public Shared APIErrors As New SortedList
    Public Shared LastAutoAPIResult As Boolean = True
    Public Shared NextAutoAPITime As DateTime = Now.AddMinutes(60)
    Public Shared AutoRetryAPITime As DateTime = Now.AddMinutes(5) ' Minimum retry time if an error occurs
    Public Shared EveHqlcd As New G15Lcd
    Public Shared IsG15LcdActive As Boolean = False
    Public Shared LcdPilot As String = ""
    Public Shared LcdCharMode As Integer = 0
    Public Shared CustomPriceList As New SortedList(Of Integer, Double) ' TypeID, Price
    Public Shared APIUpdateAvailable As Boolean = False
    Public Shared AppUpdateAvailable As Boolean = False
    Public Shared NextAutoMailAPITime As DateTime = Now
    Public Shared Widgets As New SortedList(Of String, String)
    Public Shared Event ShutDownEveHQ()
    Public Shared UpdateShutDownRequest As Boolean = False
    Public Shared RemoteProxy As New RemoteProxyServer
    Public Shared APIUpdateInProgress As Boolean = False
    Public Shared EveHQServerMessage As EveHQMessage
    Public Shared RestoredSettings As Boolean = False
    Public Shared BcAppKey As String = "B23079B49E1FCBB9C224C9D9CC591DF9904C193F"
    'Public Shared EveHqapiServerInfo As New APIServerInfo
    Public Shared EveHQIsUpdating As Boolean = False
    Private Shared _marketStatDataProvider As IMarketStatDataProvider
    Private Shared _marketOrderDataProvider As IMarketOrderDataProvider
    Private Shared ReadOnly MarketCacheProcessorMinTime As DateTime = DateTime.Now.AddHours(-1)
    Private Shared marketDataReceivers As IEnumerable(Of IMarketDataReceiver)
    Private Shared _tickerItemList As New List(Of Integer)
    Private Shared _loggingStream As Stream
    Private Shared _eveHqTracer As EveHQTraceLogger
    Private Shared _proxyDetails As WebProxyDetails
    Private Shared _apiProvider As NewEveApi.EveAPI
    Private Shared _updateLocation As String
    Private Shared _plugins As Dictionary(Of String, EveHQPlugIn)
    Private Shared _eveCentralProvider As EveCentralMarketDataProvider
    Private Shared _eveHqProvider As IMarketStatDataProvider
	Private Shared _ccpMarketDataProvider As CcpMarketDataProvider
	Private Shared _locations As Locations


	Shared Sub New()
    End Sub

    Public Shared Property Settings As New EveHQSettings

    Public Shared Property EveHQLogTimer As New Stopwatch

    Public Shared Property Plugins() As Dictionary(Of String, EveHQPlugIn)
        Get
            If _plugins Is Nothing Then
                _plugins = New Dictionary(Of String, EveHQPlugIn)
            End If
            Return _plugins
        End Get
        Set(ByVal value As Dictionary(Of String, EveHQPlugIn))
            _plugins = value
        End Set
    End Property

    'Public Shared Property EveHqSettings As EveSettings

    Shared Property StartShutdownEveHQ() As Boolean
        Get
        End Get
        Set(ByVal value As Boolean)
            If value = True Then
                RaiseEvent ShutDownEveHQ()
            End If
        End Set
    End Property

    Public Shared Property AppDataFolder As String
        Get
            Return _appDataFolder
        End Get
        Set(value As String)
            _appDataFolder = value
        End Set
    End Property



    Public Shared Property MarketStatDataProvider As IMarketStatDataProvider
        Get
            If _marketStatDataProvider Is Nothing Then
                ' Initialize based on settings
                If (Settings.MarketDataProvider = EveCentralMarketDataProvider.Name) Then
					_marketStatDataProvider = GetEveCentralMarketInstance()
                Else If (Settings.MarketDataProvider = FuzzworkMarketStatDataProvider.Name)
					_marketStatDataProvider = GetFuzzworkMarketStatDataProvider()
				Else 
					_marketStatDataProvider = GetCcpMarketStatDataProvider()
                End If
            End If
            Return _marketStatDataProvider
        End Get
        Set
            _marketStatDataProvider = value
        End Set
    End Property

	Public Shared Property TickerItemList As List(Of Integer)
        Get
            If (_tickerItemList.Count = 0) Then
                'Add place holder mineral types only
                _tickerItemList.Add(34)
                _tickerItemList.Add(35)
                _tickerItemList.Add(36)
                _tickerItemList.Add(37)
                _tickerItemList.Add(38)
                _tickerItemList.Add(39)
                _tickerItemList.Add(40)
                _tickerItemList.Add(11399)
            End If
            Return _tickerItemList
        End Get
        Set(value As List(Of Integer))
            _tickerItemList = value
        End Set
    End Property

    Public Shared ReadOnly Property MarketOrderDataProvider As IMarketOrderDataProvider
        Get
	        If (Settings.MarketDataProvider = EveCentralMarketDataProvider.Name) Then
                _marketOrderDataProvider =  GetEveCentralMarketInstance()
			Else If (Settings.MarketDataProvider = CcpMarketDataProvider.Name)
				_marketOrderDataProvider =  GetCcpMarketStatDataProvider()
			Else
				_marketOrderDataProvider =  new StabMarketOrderDataProvider()
            End If
            Return _marketOrderDataProvider
        End Get
	End Property

    Public Shared Property LoggingStream As Stream
        Get
            Return _loggingStream
        End Get
        Set(value As Stream)
            _loggingStream = value
        End Set
    End Property

    Public Shared Property EveHqTracer As EveHQTraceLogger
        Get
            Return _eveHqTracer
        End Get
        Set(value As EveHQTraceLogger)
            _eveHqTracer = value
        End Set
    End Property

    Public Shared ReadOnly Property ProxyDetails As WebProxyDetails
        Get
            If (Settings.ProxyRequired) Then

                If _proxyDetails Is Nothing Then
                    _proxyDetails = New WebProxyDetails()
                    _proxyDetails.ProxyPassword = Settings.ProxyPassword
                    Dim scheme As String
                    If Settings.ProxyServer.StartsWith("http://") Then
                        scheme = String.Empty
                    Else
                        scheme = "http://"
                    End If
                    _proxyDetails.ProxyServerAddress = New Uri(scheme + Settings.ProxyServer)
                    _proxyDetails.ProxyUserName = Settings.ProxyUsername
                    _proxyDetails.UseBasicAuth = Settings.ProxyUseBasic
                    _proxyDetails.UseDefaultCredential = Settings.ProxyUseDefault
                End If

                Return _proxyDetails
            End If
            Return Nothing
        End Get
    End Property



    Public Shared Property UpdateLocation As String
        Get
            Return _updateLocation
        End Get
        Set(value As String)
            _updateLocation = value
        End Set
    End Property

    Public Shared ReadOnly Property ApiProvider As NewEveApi.EveAPI
        Get
            If _apiProvider Is Nothing Then
                _apiProvider = New NewEveApi.EveAPI(ApiCacheFolder, New HttpRequestProvider(ProxyDetails))
            End If
            Return _apiProvider
        End Get
    End Property

    Public Shared ReadOnly Property Locations As Locations
        Get
            If _locations Is Nothing Then
	            _locations = new Locations(ApiProvider.StructureName, AddressOf WriteLogEvent)
            End If
            Return _locations
        End Get
    End Property



    Public Shared Property TempPilots As SortedList(Of String, EveHQPilot)
        Get
            Return tempPilots1
        End Get
        Set(value As SortedList(Of String, EveHQPilot))
            tempPilots1 = value
        End Set
    End Property


    Public Shared Sub ReduceMemory()
        GC.Collect()
        GC.WaitForPendingFinalizers()
        Try
            If (Environment.OSVersion.Platform = PlatformID.Win32NT) Then
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1)
            End If
        Catch ex As Exception
            ' Catch potential errors from remote loading routines
        End Try
    End Sub

    Public Shared Sub WriteLogEvent(ByVal eventText As String)
        Dim ts As TimeSpan = EveHQLogTimer.Elapsed
        ' Format and display the TimeSpan value.
        Dim elapsedTime As String = String.Format("{0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds)
        eventText = "[" & elapsedTime & "]" & " " & eventText
        Try
            Trace.WriteLine(eventText, "Information")
        Catch e As Exception
            ' Don't bother reporting this
        End Try
    End Sub

    Public Shared Function GetMdiTab(ByVal tabName As String) As TabItem
        Dim mainTab As TabStrip = CType(MainForm.Controls("tabEveHQMDI"), TabStrip)
        If mainTab IsNot Nothing Then
            For Each tp As TabItem In mainTab.Tabs
                If tp.Text = tabName Then
                    Return tp
                End If
            Next
        End If
        Return Nothing
    End Function


    Public Shared Function GetEveCentralMarketInstance() As EveCentralMarketDataProvider
        If _eveCentralProvider Is Nothing Then
            If (Settings.ProxyRequired) Then

                _eveCentralProvider = New EveCentralMarketDataProvider(Path.Combine(AppDataFolder, "MarketCache\EveCentral"), New HttpRequestProvider(ProxyDetails))
            Else
                _eveCentralProvider = New EveCentralMarketDataProvider(Path.Combine(AppDataFolder, "MarketCache\EveCentral"), New HttpRequestProvider(Nothing))
            End If
        End If
        Return _eveCentralProvider
    End Function

	Public Shared Function GetFuzzworkMarketStatDataProvider() As IMarketStatDataProvider
        If _eveHqProvider Is Nothing Then
            If (Settings.ProxyRequired) Then
                _eveHqProvider = New FuzzworkMarketStatDataProvider(
					Path.Combine(AppDataFolder, "MarketCache\Fuzzwork"), New HttpRequestProvider(ProxyDetails), New SupportedMarket())
            Else
                _eveHqProvider = New FuzzworkMarketStatDataProvider(
					Path.Combine(AppDataFolder, "MarketCache\Fuzzwork"), New HttpRequestProvider(Nothing), New SupportedMarket())
            End If
        End If
        Return _eveHqProvider
	End Function
	Public Shared Function GetCcpMarketStatDataProvider() As CcpMarketDataProvider
        If _eveHqProvider Is Nothing Then
            If (Settings.ProxyRequired) Then
	            _ccpMarketDataProvider = New CcpMarketDataProvider(
					Path.Combine(AppDataFolder, "MarketCache\Ccp"), New HttpRequestProvider(ProxyDetails), New SupportedMarket(), Locations)
            Else
	            _ccpMarketDataProvider = New CcpMarketDataProvider(
					Path.Combine(AppDataFolder, "MarketCache\Ccp"), New HttpRequestProvider(Nothing), New SupportedMarket(), Locations)
            End If
        End If
        Return _ccpMarketDataProvider
	End Function
End Class

Class ListViewItemComparerA
    Implements IComparer
    Private ReadOnly _col As Integer

    Public Sub New()
        _col = 0
    End Sub

    Public Sub New(ByVal column As Integer)
        _col = column
    End Sub

    Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer _
        Implements IComparer.Compare
        Return [String].Compare(CType(x, ListViewItem).SubItems(_col).Text, CType(y, ListViewItem).SubItems(_col).Text)
    End Function
End Class

Class ListViewItemComparerD
    Implements IComparer
    Private ReadOnly _col As Integer

    Public Sub New()
        _col = 0
    End Sub

    Public Sub New(ByVal column As Integer)
        _col = column
    End Sub

    Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer _
        Implements IComparer.Compare
        Return [String].Compare(CType(y, ListViewItem).SubItems(_col).Text, CType(x, ListViewItem).SubItems(_col).Text)
    End Function
End Class

Public Class ListViewItemComparerText
    Implements IComparer
    Private ReadOnly _col As Integer
    Private ReadOnly _order As SortOrder

    Public Sub New()
        _col = 0
        _order = SortOrder.Ascending
    End Sub

    Public Sub New(ByVal column As Integer, ByVal order As SortOrder)
        _col = column
        _order = order
    End Sub

    Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements IComparer.Compare
        Dim returnVal As Integer = -1

        Try
            If _
                IsNumeric(CType(x, ListViewItem).SubItems(_col).Text) = True And
                IsNumeric(CType(y, ListViewItem).SubItems(_col).Text) = True Then
                ' Parse the two objects passed as a parameter as a DateTime.
                Dim firstNum As Double = CDbl((CType(x, ListViewItem).SubItems(_col).Text))
                Dim secondNum As Double = CDbl((CType(y, ListViewItem).SubItems(_col).Text))
                ' Compare the two numbers
                returnVal = Decimal.Compare(CDec(firstNum), CDec(secondNum))
                ' If neither compared object has a valid date format, 
                ' compare as a string.
            Else
                returnVal = [String].Compare(CType(x, ListViewItem).SubItems(_col).Text,
                                             CType(y, ListViewItem).SubItems(_col).Text)
            End If
        Catch
            ' Compare the two items as a string.
        End Try

        ' Determine whether the sort order is descending.
        If _order = SortOrder.Descending Then
            ' Invert the value returned by String.Compare.
            returnVal *= -1
        End If
        Return returnVal
    End Function
End Class

Public Class ListViewItemComparerName
    Implements IComparer
    Private ReadOnly _col As Integer
    Private ReadOnly _order As SortOrder

    Public Sub New()
        _col = 0
        _order = SortOrder.Ascending
    End Sub

    Public Sub New(ByVal column As Integer, ByVal order As SortOrder)
        _col = column
        _order = order
    End Sub

    Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements IComparer.Compare
        Dim returnVal As Integer = -1

        Try
            If _
                IsNumeric(CType(x, ListViewItem).SubItems(_col).Name) = True And
                IsNumeric(CType(y, ListViewItem).SubItems(_col).Name) = True Then
                ' Parse the two objects passed as a parameter as a DateTime.
                Dim firstNum As Double = CDbl((CType(x, ListViewItem).SubItems(_col).Name))
                Dim secondNum As Double = CDbl((CType(y, ListViewItem).SubItems(_col).Name))
                ' Compare the two numbers
                returnVal = Decimal.Compare(CDec(firstNum), CDec(secondNum))
                ' If neither compared object has a valid date format, 
                ' compare as a string.
            Else
                returnVal = [String].Compare(CType(x, ListViewItem).SubItems(_col).Name,
                                             CType(y, ListViewItem).SubItems(_col).Name)
            End If
        Catch
            ' Compare the two items as a string.
        End Try

        ' Determine whether the sort order is descending.
        If _order = SortOrder.Descending Then
            ' Invert the value returned by String.Compare.
            returnVal *= -1
        End If
        Return returnVal
    End Function
End Class
