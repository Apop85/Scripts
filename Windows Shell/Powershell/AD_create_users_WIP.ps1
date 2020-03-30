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


# ____ ___                    __                                      __   
# |    |   \______ ___________|  | ______   ____ ________ ____ _______/  |_ 
# |    |   /  ___// __ \_  __ \  |/ /  _ \ /    \\___   // __ \\____ \   __\
# |    |  /\___ \\  ___/|  | \/    <  <_> )   |  \/    /\  ___/|  |_> >  |  
# |______//____  >\___  >__|  |__|_ \____/|___|  /_____ \\___  >   __/|__|  
#              \/     \/           \/          \/      \/    \/|__|          
# ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Benutzername ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
# Zu verwendende Buchstaben vom Vornamen (0 für alle):
$username_name_length = 3
# Zu verwendende Buchstaben vom Nachnamen (0 für alle)
$username_surname_length = 3
# Sollen die Zeichen angepasst werden? (0 = Keine Änderung, 1 = Alles klein, 2 = alles gross)
$username_rule = 1
# Soll der Benutzername nummeriert werden? (0 = nein, 1 = eine Stelle, 2 = zweistellig, 3 = dreistellig)
$username_iterate = 2
# Sollen Sonderzeichen ausgewechselt wereden? (0 = nein, 1 = ja)
$username_replace = 1

# ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Mailadresse ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
# Zu verwendende Buchstaben vom Vornamen (0 für alle)
$usermail_name_length = 0
# Zu verwendende Buchstaben vom Nachnamen (0 für alle)
$usermail_surname_length = 0
# Trennzeichen zwischen Vor- und Nachname ("" für keines)
$usermail_seperator = "."
# Falls andere Mail-Domain als der definierte Domainname verwendet werden soll ("" für Default-Domainname)
$usermail_domain = "creasol.ch"
# Sollen Sonderzeichen ausgewechselt werden? (0 = nein, 1 = ja)
$usermail_replace = 1


# ________          _____             .__   __            ________          __                 
# \______ \   _____/ ____\____   __ __|  |_/  |_          \______ \ _____ _/  |_  ____   ____  
#  |    |  \_/ __ \   __\\__  \ |  |  \  |\   __\  ______  |    |  \\__  \\   __\/ __ \ /    \ 
#  |    `   \  ___/|  |   / __ \|  |  /  |_|  |   /_____/  |    `   \/ __ \|  | \  ___/|   |  \
# /_______  /\___  >__|  (____  /____/|____/__|           /_______  (____  /__|  \___  >___|  /
#         \/     \/           \/                                  \/     \/          \/     \/
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

# __________                      __                       
# \______   \ ____   ____  __ ___/  |_________ ___________ 
#  |    |  _// __ \ /    \|  |  \   __\___   // __ \_  __ \
#  |    |   \  ___/|   |  \  |  /|  |  /    /\  ___/|  | \/
#  |______  /\___  >___|  /____/ |__| /_____ \\___  >__|   
#         \/     \/     \/                  \/    \/     
# Userdefinition: Benutzer = Department, (Gruppe/n)
$usernames = @{ 'Urs Meier' = 'CEO', @('CEO'); 
                'Viola Krätzer' = 'Marketing', @('Marketing', 'Administration'); 
                'Roland Berger' = 'Architecture', @('Architecture'); 
                'Hannah Glühwind' = 'Architecture', @('Architecture'); 
                'Karl Meister' = 'Architecture', @('Architecture'); 
                'Thomas Kurzwell' = 'Accounting', @('Accounting', 'Technics')
            }

#   ________                                         
#  /  _____/______ __ ________ ______   ____   ____  
# /   \  __\_  __ \  |  \____ \\____ \_/ __ \ /    \ 
# \    \_\  \  | \/  |  /  |_> >  |_> >  ___/|   |  \
#  \______  /__|  |____/|   __/|   __/ \___  >___|  /
#         \/            |__|   |__|        \/     \/
# Gruppendefinitionen
$user_groups = @(   'CEO', 'Marketing', 'Architecture', 'Accounting', 
                    'Administration', 'Projects', 'Technics'
                )

###########################################################################################################
################################ AB HIER KEINE ÄNDERUNGEN MEHR VORNEHMEN ##################################
###########################################################################################################

#   ______           _    _   _                        
#  |  ____|         | |  | | (_)                       
#  | |__ _   _ _ __ | | _| |_ _  ___  _ __   ___ _ __  
#  |  __| | | | '_ \| |/ / __| |/ _ \| '_ \ / _ \ '_ \ 
#  | |  | |_| | | | |   <| |_| | (_) | | | |  __/ | | |
#  |_|   \__,_|_| |_|_|\_\\__|_|\___/|_| |_|\___|_| |_| 
function CutToLength {
    # Funktion zum zuschneiden der Namen anhand der Namenskonverntion
    param (
        [string]$name,
        [int]$length
    )
    # Schneide übergebenen Namen auf gewünschte Länge zu
    if ($length -ge 0) {
        return $name.SubString(0, $length)
    } else {
        return $name
    }
    
}

function ReplaceSpecialChars {
    param (
        [string]$value
    )
    # Umlautcharaktere und ihre Ersetzungen
    $special_characters_to_replace = @(@("ä", "ae"), @("ö", "oe"), @("ü", "ue"), @("Ä", "Ae"), @("Ö", "Oe"), @("Ü", "Ue"))
    # Ersetze Spezialzeichen
    foreach ($character_set in $special_characters_to_replace) {
        # Prüfe ob Spezialcharakter vorkommt.
        if ($value -contains $character_set[0]) {
            $value = $value.Replace($character_set[0], $character_set[1])
        }
    }
    return $value
}

function AddZero {
    # Fülle Zahl mit vorangehenden Nullen auf
    param (
        [int]$number,
        [int]$amount
    )
    
    # Konvertiere Integer zu String
    $number = $number.ToString()
    
    while ($number.Length() -ne $amount) {
        # Füge Nullen hinzu, bis Anzahl erreicht
        $number = "0" + $number
    }

    # Gebe Zahl zurück
    return $number
}

#  ______          _       _ _         _____                                   
# |  ____|        | |     | | |       / ____|                                  
# | |__   _ __ ___| |_ ___| | | ___  | |  __ _ __ _   _ _ __  _ __   ___ _ __  
# |  __| | '__/ __| __/ _ \ | |/ _ \ | | |_ | '__| | | | '_ \| '_ \ / _ \ '_ \ 
# | |____| |  \__ \ ||  __/ | |  __/ | |__| | |  | |_| | |_) | |_) |  __/ | | |
# |______|_|  |___/\__\___|_|_|\___|  \_____|_|   \__,_| .__/| .__/ \___|_| |_|
#                                                      | |   | |               
#                                                      |_|   |_|              
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


#  ______          _       _ _        _    _               
# |  ____|        | |     | | |      | |  | |              
# | |__   _ __ ___| |_ ___| | | ___  | |  | |___  ___ _ __ 
# |  __| | '__/ __| __/ _ \ | |/ _ \ | |  | / __|/ _ \ '__|
# | |____| |  \__ \ ||  __/ | |  __/ | |__| \__ \  __/ |   
# |______|_|  |___/\__\___|_|_|\___|  \____/|___/\___|_|  
$counter = 0
$usercounter = 1
foreach ($user in $user_keys) {
    # Lese Informationen aus Hash-Table aus
    $name_array = $user.Split(" ")
    $name = $name_array[0]
    $surname = $name_array[1]
    $department = $usernames.Get_Item($user)[0]
    $groups = $usernames.Get_Item($user)[1]

    # Falls vorgegeben, ersetze Umlaute
    if ($username_replace -eq 1) {
        $username_name = ReplaceSpecialChars -value $name
        $username_surname = ReplaceSpecialChars -value $surname
    } else {
        $username_name = $name
        $username_surname = $surname
    }
    # Namen entsprechend Vorgabe zuschneiden
    $username_name = CutToLength -name $username_name -length $username_name_length
    $username_surname = CutToLength -name $username_surname -length $username_surname_length
    # Benutzername zusammenstellen
    $username = $username_name + $username_surname


    # Manipuliere Benutzername falls erwünscht.
    if ($username_rule -eq 1) {
        # Gross- in Kleinbuchstaben umwandlen
        $username = $username.ToLower()
    } elseif ($username_rule -eq 2) {
        # Klein- in Grossbuchstaben umwandlen
        $username = $username.ToUpper()
    }
    
    # Iteriere, falls gewünscht, bis User noch nicht vorhanden ist
    if ($username_iterate -ne 0) {
        while ($true) {
            # Fülle Iteration mit Nullen auf
            $usercount = AddZero -number $usercounter -amount $username_iterate
            $username_to_check = $username + $usercount
            # Prüfe ob Benutzer bereits existiert
            if (@(Get-ADUser -Filter { SamAccountName -eq $username_to_check }).Count -eq 0) {
                # Falls Benutzer noch nicht existiert breche aus Loop aus
                $username = $username_to_check
                break
            }
            $usercounter += 1    
        }
    }

    # Entferne Sonderzeichen, falls gewünscht
    if ($usermail_replace -eq 1) {
        $usermail_name = ReplaceSpecialChars -value $name
        $usermail_surname = ReplaceSpecialChars -value $surname
    } else {
        $usermail_name = $name
        $usermail_surname = $surname
    }
    # Name und Vorname für die Mailadresse zurechtschneiden
    $usermail_name = CutToLength -name $usermail_name -length $usermail_name_length
    $usermail_surname = CutToLength -name $usermail_surname -length $usermail_surname_length

    # Prüfe ob Standard- oder Alternativdomain gewählt wurde
    if ($usermail_domain -eq "") {
        $usermail_domain = $domain
    }

    # Setze 1. Teil der Mailadresse zusammen
    $usermail = $usermail_name + $usermail_seperator + $usermail_surname
    # Setze Mailadresse zusammen
    $usermail = $usermail + "@" + $usermail_domain

    $counter += 1
    # Existiert der User noch nicht?
    Write-Warning -Message "User $user does not exist."
    Write-Host "Creating user: $user"
    # Lege neuen Benutzer an
    New-ADUser -SamAccountName $user -Name "$user" -Surname $surname -GivenName $name -UserPrincipalName $usermail -AccountPassword $default_password -Enabled $false -PasswordNeverExpires $false -Company $company -City $city -Department $department
    # Benutzer aktivieren
    Enable-ADAccount -Identity $user
    Write-Host "Adding $user to group $department"
    # Benutzer zu Gruppe hinzufügen
    foreach ($group in $groups) {
        Add-ADGroupMember -Identity $group -Members $user
    }
} 

Write-Host "##########################"
Write-Host "$counter Users have been added."
Write-Host "##########################"