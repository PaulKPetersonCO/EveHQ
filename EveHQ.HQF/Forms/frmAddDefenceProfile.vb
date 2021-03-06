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

Imports System.Windows.Forms
Imports EveHQ.Core

Namespace Forms

    Public Class FrmAddDefenceProfile

        Dim _startUp As Boolean = True

        Private Sub cboProfileType_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles cboProfileType.SelectedIndexChanged
            Select Case cboProfileType.SelectedIndex
                Case 0 ' Manual
                    lblFittingName.Enabled = False : cboFittingName.Enabled = False
                    lblPilotName.Enabled = False : cboPilotName.Enabled = False
                    lblEmDamage.Enabled = True
                    lblEXDamage.Enabled = True
                    lblKIDamage.Enabled = True
                    lblTHDamage.Enabled = True
                    txtSEM.Enabled = True
                    txtSEx.Enabled = True
                    txtSKi.Enabled = True
                    txtSTh.Enabled = True
                    txtAEM.Enabled = True
                    txtAEx.Enabled = True
                    txtAKi.Enabled = True
                    txtATh.Enabled = True
                    txtHEM.Enabled = True
                    txtHEx.Enabled = True
                    txtHKi.Enabled = True
                    txtHTh.Enabled = True
                    lblResistInfo.Enabled = True
                Case 1 ' Fitting
                    lblFittingName.Enabled = True : cboFittingName.Enabled = True
                    lblPilotName.Enabled = True : cboPilotName.Enabled = True
                    lblEmDamage.Enabled = False
                    lblEXDamage.Enabled = False
                    lblKIDamage.Enabled = False
                    lblTHDamage.Enabled = False
                    txtSEM.Enabled = False
                    txtSEx.Enabled = False
                    txtSKi.Enabled = False
                    txtSTh.Enabled = False
                    txtAEM.Enabled = False
                    txtAEx.Enabled = False
                    txtAKi.Enabled = False
                    txtATh.Enabled = False
                    txtHEM.Enabled = False
                    txtHEx.Enabled = False
                    txtHKi.Enabled = False
                    txtHTh.Enabled = False
                    lblResistInfo.Enabled = False
                    Call GetFittingDetails()
            End Select
        End Sub

        Private Sub btnCancel_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnCancel.Click
            Close()
        End Sub

        Private Sub btnAccept_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnAccept.Click
            ' Check we have a profile name
            If txtProfileName.Text.Trim = "" Then
                MessageBox.Show("You need to enter a valid profile name. Please try again.", "Profile Name Required", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            Else
                ' Check the name isn't in use
                If HQFDamageProfiles.ProfileList.ContainsKey(txtProfileName.Text.Trim) And txtProfileName.Text.Trim <> txtProfileName.Tag.ToString.Trim Then
                    MessageBox.Show("This Profile name is in use. Please select another.", "Duplicate Profile Name Found", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Exit Sub
                Else
                    ' Check we have all the required info
                    Select Case cboProfileType.SelectedIndex
                        Case 0 ' Manual
                            If IsNumeric(txtSEM.Text) = False Or IsNumeric(txtSEx.Text) = False Or IsNumeric(txtSKi.Text) = False Or IsNumeric(txtSTh.Text) = False Then
                                MessageBox.Show("Shield Resist amounts contain non numeric characters. Please try again.", "Resistance Value Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                Exit Sub
                            End If
                            If IsNumeric(txtAEM.Text) = False Or IsNumeric(txtAEx.Text) = False Or IsNumeric(txtAKi.Text) = False Or IsNumeric(txtATh.Text) = False Then
                                MessageBox.Show("Armor Resist amounts contain non numeric characters. Please try again.", "Resistance Value Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                Exit Sub
                            End If
                            If IsNumeric(txtHEM.Text) = False Or IsNumeric(txtHEx.Text) = False Or IsNumeric(txtHKi.Text) = False Or IsNumeric(txtHTh.Text) = False Then
                                MessageBox.Show("Hull Resist amounts contain non numeric characters. Please try again.", "Resistance Value Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                Exit Sub
                            End If

                            If CDbl(txtSEM.Text) < 0 Or CDbl(txtSEx.Text) < 0 Or CDbl(txtSKi.Text) < 0 Or CDbl(txtSTh.Text) < 0 Or CDbl(txtSEM.Text) > 100 Or CDbl(txtSEx.Text) > 100 Or CDbl(txtSKi.Text) > 100 Or CDbl(txtSTh.Text) > 100 Then
                                MessageBox.Show("Shield Resist amounts must be greater than or equal to zero and less than 100. Please try again.", "Damage Value Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                Exit Sub
                            End If
                            If CDbl(txtAEM.Text) < 0 Or CDbl(txtAEx.Text) < 0 Or CDbl(txtAKi.Text) < 0 Or CDbl(txtATh.Text) < 0 Or CDbl(txtAEM.Text) > 100 Or CDbl(txtAEx.Text) > 100 Or CDbl(txtAKi.Text) > 100 Or CDbl(txtATh.Text) > 100 Then
                                MessageBox.Show("Armor Resist amounts must be greater than or equal to zero and less than 100. Please try again.", "Damage Value Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                Exit Sub
                            End If
                            If CDbl(txtHEM.Text) < 0 Or CDbl(txtHEx.Text) < 0 Or CDbl(txtHKi.Text) < 0 Or CDbl(txtHTh.Text) < 0 Or CDbl(txtHEM.Text) > 100 Or CDbl(txtHEx.Text) > 100 Or CDbl(txtHKi.Text) > 100 Or CDbl(txtHTh.Text) > 100 Then
                                MessageBox.Show("Hull Resist amounts must be greater than or equal to zero and less than 100. Please try again.", "Damage Value Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                Exit Sub
                            End If
                        Case 1 ' Fitting
                            If cboFittingName.SelectedItem Is Nothing Then
                                MessageBox.Show("A valid Fitting needs to be selected. Please try again.", "Fitting Required", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                Exit Sub
                            End If
                            If cboPilotName.SelectedItem Is Nothing Then
                                MessageBox.Show("A Pilot needs to be selected. Please try again.", "Pilot Required", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                Exit Sub
                            End If
                    End Select
                    ' Assume all is OK so lets save the information
                    Dim newProfile As New HQFDefenceProfile
                    newProfile.Name = txtProfileName.Text.Trim
                    newProfile.Type = CType(cboProfileType.SelectedIndex, ProfileTypes)
                    newProfile.SEm = CDbl(txtSEM.Text)
                    newProfile.SExplosive = CDbl(txtSEx.Text)
                    newProfile.SKinetic = CDbl(txtSKi.Text)
                    newProfile.SThermal = CDbl(txtSTh.Text)
                    newProfile.AEm = CDbl(txtAEM.Text)
                    newProfile.AExplosive = CDbl(txtAEx.Text)
                    newProfile.AKinetic = CDbl(txtAKi.Text)
                    newProfile.AThermal = CDbl(txtATh.Text)
                    newProfile.HEm = CDbl(txtHEM.Text)
                    newProfile.HExplosive = CDbl(txtHEx.Text)
                    newProfile.HKinetic = CDbl(txtHKi.Text)
                    newProfile.HThermal = CDbl(txtHTh.Text)
                    Select Case newProfile.Type
                        Case ProfileTypes.Manual  ' Manual
                            newProfile.Fitting = "" : newProfile.Pilot = ""
                        Case ProfileTypes.Fitting  ' Fitting
                            newProfile.Fitting = cboFittingName.SelectedItem.ToString : newProfile.Pilot = cboPilotName.SelectedItem.ToString
                    End Select
                    ' If Editing, delete the old profile
                    If Tag.ToString <> "Add" Then
                        HQFDefenceProfiles.ProfileList.Remove(txtProfileName.Tag.ToString)
                    End If
                    ' Add the profile
                    HQFDefenceProfiles.ProfileList.Add(newProfile.Name, newProfile)
                    Call HQFDefenceProfiles.Save()
                    Close()
                End If
            End If
        End Sub

        Private Sub frmAddDamageProfile_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
            ' Load details into the combo boxes
            cboFittingName.BeginUpdate()
            cboFittingName.Items.Clear()
            For Each fitting As String In Fittings.FittingList.Keys
                cboFittingName.Items.Add(fitting)
            Next
            cboFittingName.EndUpdate()

            cboPilotName.BeginUpdate()
            cboPilotName.Items.Clear()
            For Each cPilot As EveHQPilot In HQ.Settings.Pilots.Values
                cboPilotName.Items.Add(cPilot.Name)
            Next
            cboPilotName.EndUpdate()

            If Tag.ToString = "Add" Then
                cboProfileType.SelectedIndex = 0
                txtProfileName.Tag = ""
                txtSEM.Text = CDbl(0).ToString("N2")
                txtSEx.Text = CDbl(0).ToString("N2")
                txtSKi.Text = CDbl(0).ToString("N2")
                txtSTh.Text = CDbl(0).ToString("N2")
                txtAEM.Text = CDbl(0).ToString("N2")
                txtAEx.Text = CDbl(0).ToString("N2")
                txtAKi.Text = CDbl(0).ToString("N2")
                txtATh.Text = CDbl(0).ToString("N2")
                txtHEM.Text = CDbl(0).ToString("N2")
                txtHEx.Text = CDbl(0).ToString("N2")
                txtHKi.Text = CDbl(0).ToString("N2")
                txtHTh.Text = CDbl(0).ToString("N2")
                _startUp = False
            Else
                Dim editProfile As HQFDefenceProfile = CType(Tag, HQFDefenceProfile)
                txtProfileName.Tag = editProfile.Name
                ' Populate the bits and pieces with the relevant data
                txtProfileName.Text = editProfile.Name
                Select Case editProfile.Type
                    Case ProfileTypes.Manual  ' Manual
                        txtSEM.Text = editProfile.SEm.ToString("N2")
                        txtSEx.Text = editProfile.SExplosive.ToString("N2")
                        txtSKi.Text = editProfile.SKinetic.ToString("N2")
                        txtSTh.Text = editProfile.SThermal.ToString("N2")
                        txtAEM.Text = editProfile.AEm.ToString("N2")
                        txtAEx.Text = editProfile.AExplosive.ToString("N2")
                        txtAKi.Text = editProfile.AKinetic.ToString("N2")
                        txtATh.Text = editProfile.AThermal.ToString("N2")
                        txtHEM.Text = editProfile.HEm.ToString("N2")
                        txtHEx.Text = editProfile.HExplosive.ToString("N2")
                        txtHKi.Text = editProfile.HKinetic.ToString("N2")
                        txtHTh.Text = editProfile.HThermal.ToString("N2")
                    Case ProfileTypes.Fitting  ' Fitting
                        cboFittingName.SelectedItem = editProfile.Fitting
                        cboPilotName.SelectedItem = editProfile.Pilot
                End Select
                ' End the startup and select the right combo index to trigger an update to the form visibility
                _startUp = False
                cboProfileType.SelectedIndex = editProfile.Type
            End If

        End Sub

        Private Sub cboFittingName_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles cboFittingName.SelectedIndexChanged
            Call GetFittingDetails()
        End Sub

        Private Sub cboPilotName_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles cboPilotName.SelectedIndexChanged
            Call GetFittingDetails()
        End Sub

        Private Sub GetFittingDetails()
            ' Check if we have a fitting and a pilot and if so, generate some data
            If _startUp = False Then
                If cboFittingName.SelectedItem IsNot Nothing And cboPilotName.SelectedItem IsNot Nothing Then
                    ' Let's try and generate a fitting and get some damage info
                    Dim shipFit As String = cboFittingName.SelectedItem.ToString
                    Dim pPilot As FittingPilot = FittingPilots.HQFPilots(cboPilotName.SelectedItem.ToString)
                    Dim newFit As Fitting = Fittings.FittingList(shipFit).Clone
                    newFit.UpdateBaseShipFromFitting()
                    newFit.PilotName = pPilot.PilotName
                    newFit.ApplyFitting(BuildType.BuildEverything)
                    Dim profileShip As Ship = newFit.FittedShip
                    ' Place details of ship damage and DPS into text boxes
                    txtSEM.Text = profileShip.ShieldEMResist.ToString("N2")
                    txtSEx.Text = profileShip.ShieldExResist.ToString("N2")
                    txtSKi.Text = profileShip.ShieldKiResist.ToString("N2")
                    txtSTh.Text = profileShip.ShieldThResist.ToString("N2")
                    txtAEM.Text = profileShip.ArmorEMResist.ToString("N2")
                    txtAEx.Text = profileShip.ArmorExResist.ToString("N2")
                    txtAKi.Text = profileShip.ArmorKiResist.ToString("N2")
                    txtATh.Text = profileShip.ArmorThResist.ToString("N2")
                    txtHEM.Text = profileShip.StructureEMResist.ToString("N2")
                    txtHEx.Text = profileShip.StructureExResist.ToString("N2")
                    txtHKi.Text = profileShip.StructureKiResist.ToString("N2")
                    txtHTh.Text = profileShip.StructureThResist.ToString("N2")
                Else
                    txtSEM.Text = CDbl(0).ToString("N2")
                    txtSEx.Text = CDbl(0).ToString("N2")
                    txtSKi.Text = CDbl(0).ToString("N2")
                    txtSTh.Text = CDbl(0).ToString("N2")
                    txtAEM.Text = CDbl(0).ToString("N2")
                    txtAEx.Text = CDbl(0).ToString("N2")
                    txtAKi.Text = CDbl(0).ToString("N2")
                    txtATh.Text = CDbl(0).ToString("N2")
                    txtHEM.Text = CDbl(0).ToString("N2")
                    txtHEx.Text = CDbl(0).ToString("N2")
                    txtHKi.Text = CDbl(0).ToString("N2")
                    txtHTh.Text = CDbl(0).ToString("N2")
                End If
            End If
        End Sub

    End Class
End NameSpace