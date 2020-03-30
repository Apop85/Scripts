####
# File: create_users.ps1
# Project: Modul 159
#-----
# Created Date: Monday 30.03.2020, 09:25
# Author: Apop85
#-----
# Last Modified: Mon Mar 30 2020
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Erstellen von AD-Benutzern und Gruppen
####

# Benutzerdefinitionen
$usernames = @{ 'ursmei01' = 'Urs', 'Meier', 'urs.meier@creasol.ch', 'CEO'; 
                'viokra01' = 'Viola', 'Krätzer', 'viola.kraezer@creasol.ch', 'Marketing'; 
                'rolber01' = 'Roland', 'Berger', 'roland.berger@creasol.ch', 'Architecture'; 
                'hanglu01' = 'Hannah', 'Glühwind', 'hannah.gluehwind@creasol.ch', 'Architecture'; 
                'karmei01' = 'Karl', 'Meister', 'karl.meister@creasol.ch', 'Architecture'; 
                'thokur01' = 'Thomas', 'Kurzwell', 'thomas.kurzwell@creasol.ch', 'Accounting'
            }

# Gruppendefinitionen
$user_groups = @(   'CEO', 'Marketing', 'Architecture', 'Accounting', 
                    'Administration', 'Projects'
                )

# Default-Daten
$user_keys = $usernames.Keys
$default_password = "Zli.1234"
$company = "creasol"
$city = "Zürich"

# Erstelle Gruppen
$counter = 0
foreach ($group in $user_groups) {
    if (@(Get-ADGroup -Filter { SamAccountName -eq $group }).Count -eq 0) {
        # Existiert die Gruppe noch nicht?
        Write-Warning "Group $group does not exist."
        Write-Host "Creating group $group"
        $counter += 1
        # Erstelle Gruppe
        New-ADGroup -Name $group -Path "CN=Users, OU=Groups, OU=creasol ,DC=zh, DC=ch, DC=creasol, DC=ch" -GroupScope "Global"
    }
}

Write-Host "##########################"
Write-Host "$counter Groups have been added."
Write-Host "##########################"

# Erstelle User
foreach ($user in $user_keys) {
    # Definiere Departpent
    $department = $usernames.Get_Item($user)[3]
    $counter = 0
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
        Add-ADGroupMember -Identity $department -Members $user
    } elseif ($members -notcontains $user) {
        # Ist der aktuelle User NICHT in der vorgesehenen Gruppe?
        Write-Warning "User $user already exists but is not in group $group."
        # Benutzer zu Grupper hinzufügen
        Add-ADGroupMember -Identity $department -Members $user
        Write-Host "Adding $user to group $department"
    }
}

Write-Host "##########################"
Write-Host "$counter Users have been added."
Write-Host "##########################"