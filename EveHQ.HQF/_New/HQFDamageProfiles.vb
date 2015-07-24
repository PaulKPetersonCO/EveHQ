﻿'==============================================================================
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
Imports System.Windows.Forms
Imports Newtonsoft.Json

Public Class HQFDamageProfiles
    Public Shared ProfileList As New SortedList(Of String, HQFDamageProfile)

    Public Shared Sub Reset()
        ProfileList.Clear()
        Dim profileFile As String = My.Resources.DamageProfiles.ToString
        Dim profiles() As String = profileFile.Split(ControlChars.CrLf.ToCharArray)
        Dim profileData() As String
        For Each profile As String In profiles
            If profile.Trim <> "" Then
                profileData = profile.Split(",".ToCharArray)
                Dim newProfile As New HQFDamageProfile
                newProfile.Name = profileData(0)
                newProfile.Type = 0
                newProfile.EM = CDbl(profileData(1)) / 100
                newProfile.Explosive = CDbl(profileData(2)) / 100
                newProfile.Kinetic = CDbl(profileData(3)) / 100
                newProfile.Thermal = CDbl(profileData(4)) / 100
                newProfile.DPS = 0
                newProfile.Fitting = ""
                newProfile.Pilot = ""
                ProfileList.Add(newProfile.Name, newProfile)
            End If
        Next
        Save()
    End Sub

    Public Shared Sub Load()

        ' Check for the profiles file so we can load it
        If My.Computer.FileSystem.FileExists(Path.Combine(PluginSettings.HQFFolder, "HQFDamageProfiles.json")) = True Then
            Try
                Using s As New StreamReader(Path.Combine(PluginSettings.HQFFolder, "HQFDamageProfiles.json"))
                    Dim json As String = s.ReadToEnd
                    ProfileList = JsonConvert.DeserializeObject(Of SortedList(Of String, HQFDamageProfile))(json)
                End Using
            Catch ex As Exception
                MessageBox.Show("There was a problem reading the Damage Profiles data file. It appears to be corrupt. A new file will be created, however any customizations to the current one are lost.", "Error Loading Damage Profiles", MessageBoxButtons.OK, MessageBoxIcon.Error)
                ' Need to create the profiles file and the standard custom profile (omni-damage)
                Reset()
            End Try
        Else
            ' Need to create the profiles file and the standard custom profile (omni-damage)
            Call Reset()
        End If

    End Sub

    Public Shared Sub Save()

        ' Create a JSON string for writing
        Dim json As String = JsonConvert.SerializeObject(ProfileList, Formatting.Indented)

        ' Write the JSON version of the settings
        Try
            Using s As New StreamWriter(Path.Combine(PluginSettings.HQFFolder, "HQFDamageProfiles.json"), False)
                s.Write(json)
                s.Flush()
            End Using
        Catch e As Exception
            ' TODO: Need to determine a good system for handling all file saving operations
        End Try

    End Sub
End Class