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

Imports System.Globalization
Imports System.Xml
Imports System.Threading
Imports System.Runtime.Serialization.Formatters.Binary
Imports EveHQ.EveData
Imports EveHQ.Core
Imports System.IO
Imports EveHQ.Common.Logging
Imports System.Reflection
Imports System.Windows.Forms.VisualStyles
Imports System.Text
Imports EveHQ.Common.Extensions
Imports SearchOption = Microsoft.VisualBasic.FileIO.SearchOption

Namespace Forms

    Public Class FrmSplash

        Dim _isLocal As Boolean = False
        Dim _showSplash As Boolean = True
        Dim _showSettings As Boolean = False
        ReadOnly _plugInLoading As New SortedList(Of String, String)
        Dim _pluginsLoaded As Boolean = False
        Dim _widgetsLoaded As Boolean = False
        Dim _itemsLoaded As Boolean = False

        Private Sub frmSplash_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load

            ' Force primary thread into UK english
            Application.CurrentCulture = CultureInfo.GetCultureInfo("en-GB")
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-GB")
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB")

            ' Check for any commandline parameters that we need to account for
            For Each param As String In Environment.GetCommandLineArgs
                If param = "/wait" Then
                    Thread.Sleep(2000)
                End If
                If param = "/local" Then
                    _isLocal = True
                    HQ.IsUsingLocalFolders = True
                End If
                If param = "/nosplash" Then
                    _showSplash = False
                    HQ.IsSplashFormDisabled = True
                End If
                If param = "/settings" Then
                    _showSettings = True
                End If
            Next

            ' Set the application folder
            lblStatus.Text = "> Setting application directory..."
            lblStatus.Refresh()
            HQ.AppFolder = Application.StartupPath

            ' Check for existence of an application folder in the application directory
            If _isLocal = False Then
                lblStatus.Text = "> Checking app data directory..."
                lblStatus.Refresh()
                HQ.AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EveHQ")
                If My.Computer.FileSystem.DirectoryExists(HQ.AppDataFolder) = False Then
                    ' Create the folder if it doesn't exist
                    My.Computer.FileSystem.CreateDirectory(HQ.AppDataFolder)
                End If
            Else
                HQ.AppDataFolder = HQ.AppFolder
            End If

            ' Configure trace listener
            HQ.LoggingStream = New FileStream(Path.Combine(HQ.AppDataFolder, "EveHQ.log"), FileMode.Create, FileAccess.Write, FileShare.Read)
            HQ.EveHqTracer = New EveHQTraceLogger(HQ.LoggingStream)
            Trace.Listeners.Add(HQ.EveHqTracer)
            HQ.EveHQLogTimer.Start()
            HQ.WriteLogEvent("Start of EveHQ Event Log: " & Now.ToString)
            If Stopwatch.IsHighResolution Then
                HQ.WriteLogEvent("Operations timed using the system's high-resolution performance counter.")
            Else
                HQ.WriteLogEvent("Operations timed using the DateTime class.")
            End If
            HQ.WriteLogEvent("***** Start: EveHQ Startup Routine via Splash Screen *****")

            ' Show the settings form only, then quit
            If _showSettings = True Then
                Dim failedShowSettings As Boolean = EveHQSettings.Load(True)
                If failedShowSettings = True Then
                    FrmSettings.ShowDialog()
                    ' Remove the icons
                    FrmEveHQ.EveStatusIcon.Visible = False : FrmEveHQ.iconEveHQMLW.Visible = False
                    FrmEveHQ.EveStatusIcon.Icon = Nothing : FrmEveHQ.iconEveHQMLW.Icon = Nothing
                    FrmEveHQ.EveStatusIcon.Dispose() : FrmEveHQ.iconEveHQMLW.Dispose()
                Else
                    MessageBox.Show("Unable to load and display settings. Check log file for errors.", "Error displaying settings.", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
                End
            End If

            ' Check for existence of a static data folder in the application data directory
            HQ.WriteLogEvent("Start: Set static data directory")
            HQ.StaticDataFolder = Path.Combine(Application.StartupPath, "StaticData")
            If My.Computer.FileSystem.DirectoryExists(HQ.StaticDataFolder) = False Then
                MessageBox.Show("Unable to find static data folder. EveHQ will now quit", "Static Data Folder Required", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End
            End If
            HQ.WriteLogEvent("End: Set static data directory ({0})".FormatInvariant(HQ.StaticDataFolder))

            ' Load static data
            ThreadPool.QueueUserWorkItem(AddressOf LoadItemData)

            ' Insert the version number to the splash screen
            HQ.WriteLogEvent("Start: Insert version info into splash screen")
            lblVersion.Text = "Version " & FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion
            lblDate.Text = "Released: " & My.Application.Info.Trademark
            HQ.WriteLogEvent("End: Insert version info into splash screen")
            HQ.WriteLogEvent("Start: Show Splash Screen")
            If _showSplash = True And _showSettings = False Then
                Show()
                Refresh()
            End If
            HQ.WriteLogEvent("End: Show Splash Screen")

            ' Delete any .old files left over from the last update
            HQ.WriteLogEvent("Start: Old update file check")
            Try
                For Each newFile As String In My.Computer.FileSystem.GetFiles(Application.StartupPath, SearchOption.SearchTopLevelOnly, "*.old")
                    Dim nfi As New FileInfo(newFile)
                    My.Computer.FileSystem.DeleteFile(nfi.FullName)
                Next
            Catch ex As Exception
                MessageBox.Show("Unable to delete update files, please delete any .old files manually that exist in the installation folder.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Try
            HQ.WriteLogEvent("End: Old update file check")

            ' Check for existence of the API cache folder in the application data directory
            HQ.WriteLogEvent("Start: Set API cache directory")
            lblStatus.Text = "> Checking cache directory..."
            lblStatus.Refresh()
            If _isLocal = False Then
                HQ.ApiCacheFolder = Path.Combine(HQ.AppDataFolder, "ApiCache")
            Else
                HQ.ApiCacheFolder = Path.Combine(Application.StartupPath, "ApiCache")
            End If
            If My.Computer.FileSystem.DirectoryExists(HQ.ApiCacheFolder) = False Then
                ' Create the folder if it doesn't exist
                My.Computer.FileSystem.CreateDirectory(HQ.ApiCacheFolder)
            End If
            HQ.WriteLogEvent("End: Set API cache directory")

            ' Check for existence of the image cache folder in the application data directory
            HQ.WriteLogEvent("Start: Set image cache directory")
            lblStatus.Text = "> Checking image cache directory..."
            lblStatus.Refresh()
            If _isLocal = False Then
                HQ.ImageCacheFolder = Path.Combine(HQ.AppDataFolder, "ImageCache")
            Else
                HQ.ImageCacheFolder = Path.Combine(Application.StartupPath, "ImageCache")
            End If
            If My.Computer.FileSystem.DirectoryExists(HQ.ImageCacheFolder) = False Then
                ' Create the folder if it doesn't exist
                My.Computer.FileSystem.CreateDirectory(HQ.ImageCacheFolder)
            End If
            HQ.WriteLogEvent("End: Set image cache directory")

            ' Check for existence of a report folder in the application documents directory
            HQ.WriteLogEvent("Start: Set report directory")
            lblStatus.Text = "> Checking report folder..."
            lblStatus.Refresh()
            If _isLocal = False Then
                HQ.ReportFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EveHQ")
                HQ.ReportFolder = Path.Combine(HQ.ReportFolder, "Reports")
            Else
                HQ.ReportFolder = Path.Combine(Application.StartupPath, "Reports")
            End If
            If My.Computer.FileSystem.DirectoryExists(HQ.ReportFolder) = False Then
                ' Create the folder if it doesn't exist
                My.Computer.FileSystem.CreateDirectory(HQ.ReportFolder)
            End If
            HQ.WriteLogEvent("End: Set report directory")

            ' Check for existence of a backup folder
            HQ.WriteLogEvent("Start: Set Eve backup directory")
            lblStatus.Text = "> Checking backup directory..."
            lblStatus.Refresh()
            If _isLocal = False Then
                HQ.BackupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EveHQ")
                HQ.BackupFolder = Path.Combine(HQ.BackupFolder, "Backups")
            Else
                HQ.BackupFolder = Path.Combine(Application.StartupPath, "Backups")
            End If
            If My.Computer.FileSystem.DirectoryExists(HQ.BackupFolder) = False Then
                ' Create the folder if it doesn't exist
                My.Computer.FileSystem.CreateDirectory(HQ.BackupFolder)
            End If
            HQ.WriteLogEvent("End: Set Eve backup directory")

            ' Check for existence of an EveHQ backup folder
            HQ.WriteLogEvent("Start: Set EveHQ backup directory")
            lblStatus.Text = "> Checking EveHQ backup directory..."
            lblStatus.Refresh()
            If _isLocal = False Then
                HQ.EveHQBackupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EveHQ")
                HQ.EveHQBackupFolder = Path.Combine(HQ.EveHQBackupFolder, "EveHQBackups")
            Else
                HQ.EveHQBackupFolder = Path.Combine(Application.StartupPath, "EveHQBackups")
            End If
            If My.Computer.FileSystem.DirectoryExists(HQ.EveHQBackupFolder) = False Then
                ' Create the folder if it doesn't exist
                My.Computer.FileSystem.CreateDirectory(HQ.EveHQBackupFolder)
            End If
            HQ.WriteLogEvent("End: Set EveHQ backup directory")

            ' Convert the settings if relevant
            ' Relevance of settings conversion is based on existence of EveHQSettings.bin file - may want to add other conditions
            HQ.WriteLogEvent("Start: Checking settings files")
            If My.Computer.FileSystem.FileExists(Path.Combine(HQ.AppDataFolder, "EveHQSettings.bin")) = True Then
                HQ.WriteLogEvent("Start: Converting settings files")
                lblStatus.Text = "> Converting EveHQ settings..."
                lblStatus.Refresh()
                ConvertSettings(_isLocal, HQ.AppDataFolder)
                HQ.WriteLogEvent("End: Converting settings files")
            End If

            HQ.WriteLogEvent("End: Checking settings files")

            ' Load user settings - this is needed to work out data connection type & update requirements
            HQ.WriteLogEvent("Start: Loading settings")
            lblStatus.Text = "> Loading settings..."
            lblStatus.Refresh()
            EveHQSettings.Load(False)
            HQ.WriteLogEvent("End: Loading settings")

            ' Check for Widgets
            HQ.WriteLogEvent("Start: Enumerate widgets")
            lblStatus.Text = "> Enumerating Widgets..."
            lblStatus.Refresh()
            ThreadPool.QueueUserWorkItem(AddressOf EnumerateWidgets)
            HQ.WriteLogEvent("End: Enumerate widgets")

            ' Determine the visual style
            HQ.WriteLogEvent("Start: Process Visual Styles")
            If HQ.Settings.DisableVisualStyles = True Then
                Application.VisualStyleState = VisualStyleState.NoneEnabled
            Else
                Application.VisualStyleState = VisualStyleState.ClientAndNonClientAreasEnabled
            End If
            HQ.WriteLogEvent("End: Process Visual Styles")

            ' Start the G15 if applicable
            HQ.WriteLogEvent("Start: Activate G15")
            If HQ.Settings.ActivateG15 = True Then
                'Init the LCD
                Try
                    G15Lcd.InitLcd()
                Catch ex As Exception
                End Try
                If HQ.IsG15LcdActive = False Then
                    MessageBox.Show("Unable to start G15 Display. Please ensure you have the keyboard and drivers correctly installed.", "Error Starting G15", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Else
                    ' Check if the LCD will cycle chars
                    If HQ.Settings.CycleG15Pilots = True Then
                        G15Lcd.TmrLcdChar.Interval = (1000 * HQ.Settings.CycleG15Time)
                        G15Lcd.TmrLcdChar.Enabled = True
                    End If
                End If
            End If
            HQ.LcdPilot = HQ.Settings.StartupPilot
            HQ.WriteLogEvent("End: Activate G15")

            ' Force DBDataDirectory location if using SQL CE
            HQ.WriteLogEvent("Start: Set data directory")
            HQ.Settings.CustomDBFileName = Path.Combine(HQ.AppDataFolder, "EveHQData.db3")
            Call CustomDataFunctions.SetEveHQDataConnectionString()
            HQ.WriteLogEvent("End: Set data directory")

            ' Check for new database
            HQ.WriteLogEvent("Start: Check custom database")
            lblStatus.Text = "> Checking custom data..."
            lblStatus.Refresh()
            If My.Computer.FileSystem.FileExists(HQ.Settings.CustomDBFileName) = False Then
                ' Looks like it hasn't been set so let's create it
                If CustomDataFunctions.CreateCustomDB = False Then
                    MessageBox.Show("There was an error creating the EveHQ database. The error was: " & ControlChars.CrLf & ControlChars.CrLf & HQ.DataError, "Error Creating Database", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Else
                    'MessageBox.Show("Database created successfully!", "Database Creation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            Else
                ' Looks like the file exists, but let's test a connection to make sure 
                If CustomDataFunctions.CheckCustomDatabaseConnection(True) = False Then
                    ' Looks like it fails so let's recreate it - but inform the user
                    Dim msg As String = "EveHQ has detected that although the the new storage database is initialised, it cannot connect to it." & ControlChars.CrLf
                    msg &= "This database will be used to store EveHQ specific data such as market prices and financial data." & ControlChars.CrLf
                    msg &= "Defaults will be setup that you can amend later via the Database Settings. Click OK to create the new database."
                    MessageBox.Show(msg, "EveHQ Custom Database Initialisation", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End If
            HQ.WriteLogEvent("End: Check custom database")

            ' Check for modules
            HQ.WriteLogEvent("Start: Load Plug-ins")
            lblStatus.Text = "> Loading modules..."
            lblStatus.Refresh()
            ThreadPool.QueueUserWorkItem(AddressOf LoadModules)
            HQ.WriteLogEvent("End: Load Plug-ins")

            'Set the servers to their server details
            HQ.WriteLogEvent("Start: Set Eve Server details")
            lblStatus.Text = "> Setting Eve Server details..."
            lblStatus.Refresh()
            HQ.MyTqServer.Server = 0
            HQ.WriteLogEvent("End: Set Eve Server details")

            ' Update the pilot account info
            HQ.WriteLogEvent("Start: Check key skill information")
            If PilotParseFunctions.LoadKeySkills() = False Then
                Const Msg As String = "There was an error parsing your character skill data. This will be reset. Please connect to the API to download the latest data."
                MessageBox.Show(Msg, "Error Parsing Pilot Skills", MessageBoxButtons.OK, MessageBoxIcon.Information)
                For Each rPilot As EveHQPilot In HQ.Settings.Pilots.Values
                    rPilot.PilotSkills = New Dictionary(Of String, EveHQPilotSkill)
                    rPilot.SkillPoints = 0
                Next
            End If
            'Call frmSettings.UpdateAccounts()
            'Call frmEveHQ.UpdatePilotInfo(True)
            HQ.WriteLogEvent("End: Check key skill information")

            ' Load the API Errors
            HQ.WriteLogEvent("Start: Load ErrorAPI")
            Dim errorXML As New XmlDocument
            errorXML.LoadXml(My.Resources.Errors)
            Dim errList As XmlNodeList = errorXML.SelectNodes("/eveapi/result/rowset/row")
            If errList.Count <> 0 Then
                HQ.APIErrors.Clear()
                For Each errNode As XmlNode In errList
                    HQ.APIErrors.Add(errNode.Attributes.GetNamedItem("errorCode").Value, errNode.Attributes.GetNamedItem("errorText").Value)
                Next
            End If
            HQ.WriteLogEvent("End: Load ErrorAPI")

            ' Check for additional database tables in the custom database
            HQ.WriteLogEvent("Start: Check custom database tables")
            lblStatus.Text = "> Checking custom database tables..."
            lblStatus.Refresh()
            Call CustomDataFunctions.CheckCoreDBTables()
            HQ.WriteLogEvent("End: Check custom database tables")

            ' Load price data
            'Core.HQ.WriteLogEvent("Start: Load market prices")
            'Core.CustomDataFunctions.LoadMarketPricesFromDB()
            'Core.HQ.WriteLogEvent("End: Load market prices")
            HQ.WriteLogEvent("Start: Load custom prices")
            CustomDataFunctions.LoadCustomPricesFromDB()
            HQ.WriteLogEvent("End: Load custom prices")

            ' Show the main form
            HQ.WriteLogEvent("Start: Initialise main form")
            lblStatus.Text = "> Initialising EveHQ..."
            lblStatus.Refresh()
            G15Lcd.SplashFlag = False
            HQ.MainForm = FrmEveHQ
            HQ.WriteLogEvent("End: Initialise main form")

            ' Await all loading
            HQ.WriteLogEvent("Start: Awaiting final data loading")
            Do
            Loop Until _pluginsLoaded = True And _widgetsLoaded = True And _itemsLoaded = True
            HQ.WriteLogEvent("End: Awaiting final data loading")

            ' Perform additional skill loading
            HQ.WriteLogEvent(" *** Loading Eve skill data...")
            Call SkillFunctions.LoadEveSkillData()
            HQ.WriteLogEvent(" *** Checking missing skills...")
            Call PilotParseFunctions.CheckMissingTrainingSkills()

            HQ.WriteLogEvent("***** End: EveHQ Startup Routine via Splash Screen *****")
            FrmEveHQ.Show()

        End Sub

        Private Sub ConvertSettings(useLocalSwitch As Boolean, settingsFolder As String)

            Dim oldSettings As EveSettings
            Dim arguments As New StringBuilder
            Dim externalApp As New ProcessStartInfo
            externalApp.FileName = Path.Combine(Application.StartupPath, "EveHQ.SettingsConverter.exe")

            ' Load up the old settings file to see if we can grab some info
            If My.Computer.FileSystem.FileExists(Path.Combine(settingsFolder, "EveHQSettings.bin")) = True Then
                Using s As New FileStream(Path.Combine(settingsFolder, "EveHQSettings.bin"), FileMode.Open)
                    Dim f As New BinaryFormatter
                    oldSettings = CType(f.Deserialize(s), EveSettings)
                End Using

                ' Build the command line arguments
                If useLocalSwitch = True Then
                    arguments.Append("/local")
                End If
                arguments.Append(" /dbformat;" & CStr(oldSettings.DBFormat))
                arguments.Append(" /dbserver;" & oldSettings.DBServer)
                arguments.Append(" /dbname;" & oldSettings.DBServer)
                If oldSettings.DBSQLSecurity = True Then
                    arguments.Append(" /dbsqlsec;1")
                Else
                    arguments.Append(" /dbsqlsec;0")
                End If
                arguments.Append(" /dbusername;" & oldSettings.DBUsername)
                arguments.Append(" /dbpassword;" & oldSettings.DBPassword)
            End If
            externalApp.Arguments = arguments.ToString

            ' Start the process and wait until it has finished
            If My.Computer.FileSystem.FileExists(externalApp.FileName) Then
                Using newProcess As Process = Process.Start(externalApp)
                    newProcess.WaitForExit()
                End Using
            End If

        End Sub

        Private Sub LoadItemData(state As Object)

            ' Load data from the core cache
            If (StaticData.LoadCoreCache(HQ.StaticDataFolder)) Then
                ' Finalise skill loading
                HQ.WriteLogEvent(" *** Items Finished Loading")
                _itemsLoaded = True
            Else
                MessageBox.Show("There was an error loading item data. The details of the error are in the EveHQ.log file.\r\n EveHQ will now exit.")
                _itemsLoaded = False
                Application.Exit()
            End If
        End Sub

        Private Sub LoadModules(state As Object)
            For Each filename As String In My.Computer.FileSystem.GetFiles(Application.StartupPath, SearchOption.SearchTopLevelOnly, "*.dll")
                If IsDotNetAssembly(filename) = True Then
                    Try
                        Dim myAssembly As Assembly = Assembly.LoadFrom(filename)
                        Dim types() As Type = myAssembly.GetTypes
                        For Each t As Type In types
                            If t.IsPublic = True Then
                                If t.GetInterface("EveHQ.Core.IEveHQPlugIn") IsNot Nothing Then
                                    Dim myPlugIn As IEveHQPlugIn = CType(Activator.CreateInstance(t), IEveHQPlugIn)
                                    Dim eveHQPlugIn As EveHQPlugIn = myPlugIn.GetEveHQPlugInInfo
                                    eveHQPlugIn.FileName = filename
                                    Dim fi As New FileInfo(filename)
                                    eveHQPlugIn.ShortFileName = fi.Name
                                    eveHQPlugIn.FileType = t.FullName
                                    eveHQPlugIn.Version = myAssembly.GetName.Version.ToString
                                    eveHQPlugIn.Instance = myPlugIn
                                    ' Get status of plug-ins from settings (should already exist!)
                                    If HQ.Settings.Plugins.ContainsKey(eveHQPlugIn.Name) = True Then
                                        Dim userPlugIn As EveHQPlugInConfig = HQ.Settings.Plugins(eveHQPlugIn.Name)
                                        eveHQPlugIn.Disabled = userPlugIn.Disabled
                                        eveHQPlugIn.Available = True
                                    Else
                                        ' If not listed, it must be new
                                        eveHQPlugIn.Disabled = False
                                        eveHQPlugIn.Available = True
                                    End If
                                    ' Check for opening parameters
                                    If _plugInLoading.ContainsKey(eveHQPlugIn.Name) = True Then
                                        eveHQPlugIn.PostStartupData = _plugInLoading(eveHQPlugIn.Name)
                                    End If
                                    eveHQPlugIn.Status = EveHQPlugInStatus.Uninitialised
                                    HQ.Plugins.Add(eveHQPlugIn.Name, eveHQPlugIn)
                                End If
                            End If
                        Next
                    Catch bife As BadImageFormatException
                        'Ignore non .Net dlls (ones without manifests i.e. the G15 lglcd.dll) i.e. don't error
                    Catch rtle As ReflectionTypeLoadException
                        ' Assume it's a bad/old version and ignore it
                    Catch e As Exception
                        MessageBox.Show("Error loading module: " & filename & ControlChars.CrLf & e.Message.ToString, "Module Error!", MessageBoxButtons.OK, MessageBoxIcon.Stop)
                    End Try
                End If
            Next
            HQ.WriteLogEvent(" *** Plug-ins Finished Loading")
            _pluginsLoaded = True
        End Sub

        Private Sub EnumerateWidgets(state As Object)
            Dim myAssembly As Assembly = Assembly.GetExecutingAssembly()
            For Each myType As Type In myAssembly.GetTypes
                If myType.BaseType.Name = "Widget" Then
                    ' Get the control name based on the property of an instance of the control
                    Dim myInstance As Control = CType(Activator.CreateInstance(myType), Control)
                    Dim pi As PropertyInfo = myType.GetProperty("ControlName")
                    Dim myControlName As String = pi.GetValue(myInstance, Nothing).ToString
                    HQ.Widgets.Add(myControlName, myType.FullName)
                    myInstance.Dispose()
                End If
            Next
            HQ.WriteLogEvent(" *** Widgets Finished Loading")
            _widgetsLoaded = True
        End Sub

        Private Function IsDotNetAssembly(ByVal fileName As String) As Boolean
            'private bool IsDotNetAssembly(string fileName)
            Using fs As New FileStream(fileName, FileMode.Open, FileAccess.Read)
                Try
                    Using br As New BinaryReader(fs)
                        Try
                            fs.Position = &H3C ' PE Header start offset
                            Dim headerOffset As UInteger = br.ReadUInt32
                            fs.Position = headerOffset + &H18
                            Dim magicNumber As UInt16 = br.ReadUInt16()
                            Dim dictionaryOffset As Integer
                            Select Case magicNumber
                                Case &H10B ' 32 bit
                                    dictionaryOffset = &H60
                                Case &H20B ' 64 bit
                                    dictionaryOffset = &H70
                                Case Else
                                    Throw New Exception("Invalid Image Format")
                            End Select
                            ' Position to RVA 15
                            fs.Position = headerOffset + &H18 + dictionaryOffset + &H70
                            ' Read the value
                            Dim rva15Value As UInt32 = br.ReadUInt32()
                            Return rva15Value <> 0
                        Catch ex As Exception
                        Finally
                            br.Close()
                        End Try
                    End Using
                Catch ex As Exception
                Finally
                    fs.Close()
                End Try
            End Using
        End Function

    End Class
End Namespace