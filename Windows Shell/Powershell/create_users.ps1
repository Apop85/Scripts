####
# File: create_users.ps1
# Project: Modul 159
#-----
# Created Date: Monday 30.03.2020, 09:25
# Author: Apop85
#-----
# Last Modified: Monday 30.03.2020, 12:49
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Erstellen von AD-Benutzern und Gruppen
####

# Benutzerdefinitionen
# Userdefinition: Benutzername = Vorname, Nachname, Mail, Department, (Gruppe/n)
$usernames = @{ 'ursmei01' = 'Urs', 'Meier', 'urs.meier@creasol.ch', 'CEO', @('CEO'); 
                'viokra01' = 'Viola', 'Krätzer', 'viola.kraezer@creasol.ch', 'Marketing', @('Marketing', 'Administration'); 
                'rolber01' = 'Roland', 'Berger', 'roland.berger@creasol.ch', 'Architecture', @('Architecture'); 
                'hanglu01' = 'Hannah', 'Glühwind', 'hannah.gluehwind@creasol.ch', 'Architecture', @('Architecture'); 
                'karmei01' = 'Karl', 'Meister', 'karl.meister@creasol.ch', 'Architecture', @('Architecture'); 
                'thokur01' = 'Thomas', 'Kurzwell', 'thomas.kurzwell@creasol.ch', 'Accounting', @('Accounting', 'Technics')
            }

# Gruppendefinitionen
$user_groups = @(   'CEO', 'Marketing', 'Architecture', 'Accounting', 
                    'Administration', 'Projects', 'Technics'
                )

# Default-Daten
# Default-Passwort für die neuen Benutzer
$default_password = "P@ssW0rD!"
# Firmenname
$company = "creasol"
# Userstandort
$city = "Zürich"
# Domänenname
$domain = "zh.ch.creasol.ch"
# Pfad für die Gruppe (./creasol/Groups)
$grouppath = "OU=Groups, OU=creasol"
# GroupScope
$GroupScope = "Global"

# Erstelle Gruppen
$counter = 0
$user_keys = $usernames.Keys

foreach ($group in $user_groups) {
    if (@(Get-ADGroup -Filter { SamAccountName -eq $group }).Count -eq 0) {
        # Existiert die Gruppe noch nicht?
        Write-Warning "Group $group does not exist."
        Write-Host "Creating group $group"
        $counter += 1
        
        # DC-Einträge erstellen:
        $dc_output = ""
        $dc_entrys = $domain.Split(".")
        foreach ($sub_dc in $dc_entrys) {
            $dc_output += "DC=$sub_dc, "
        }
        
        # Letztes Komma entfernen.
        $dc_output = $dc_output.trimend(", ")
        
        # Erstelle Gruppe
        New-ADGroup -Name $group -Path "$grouppath ,$dc_output" -GroupScope $GroupScope
    }
}

Write-Host "##########################"
Write-Host "$counter Groups have been added."
Write-Host "##########################"

# Erstelle User
$counter = 0
foreach ($user in $user_keys) {
    # Definiere Departpent
    $department = $usernames.Get_Item($user)[3]
    $groups = $usernames.Get_Item($user)[4]

    if (@(Get-ADUser -Filter { SamAccountName -eq $user }).Count -eq 0) {
        # Existiert der User noch nicht?
        Write-Warning -Message "User $user does not exist."
        Write-Host "Creating user: $user"
        $counter += 1
        # Lese Userdetails aus Hashtable aus
        $vorname = $usernames.Get_Item($user)[0]
        $nachname = $usernames.Get_Item($user)[1]
        $usermail = $usernames.Get_Item($user)[2]
        # Lege neuen Benutzer an
        New-ADUser -SamAccountName $user -Name "$user" -Surname $nachname -GivenName $vorname -UserPrincipalName $usermail -AccountPassword $default_password -Enabled $false -PasswordNeverExpires $false -Company $company -City $city -Department $department
        # Benutzer aktivieren
        Enable-ADAccount -Identity $user
        Write-Host "Adding $user to group $department"
        # Benutzer zu Gruppe hinzufügen
        foreach ($group in $groups) {
            Add-ADGroupMember -Identity $group -Members $user
        }
    } else {
        # Lese Gruppenmitglieder aus 
        foreach ($group in $groups) {
            # Prüfe jede Gruppe bei welcher der User eingetragen werden soll. 
            $group_members = Get-ADGroupMember -Identity $group -Recursive | Select-Object -ExpandProperty Name
            if ($group_members -notcontains $user) {
                # Ist der aktuelle User NICHT in der vorgesehenen Gruppe?
                Write-Warning "User $user already exists but is not in group $group."
                # Benutzer zu Grupper hinzufügen
                Add-ADGroupMember -Identity $department -Members $user
                Write-Host "Adding $user to group $department"
            }
        }
    }
}

Write-Host "##########################"
Write-Host "$counter Users have been added."
Write-Host "##########################"