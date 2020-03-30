rem Script zum Wiederherstellen vorheriger DNS-Einstellungen und Trennen der VPN-Verbindung
rem Muss als Administrator ausgeführt werden.
rem ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

@echo OFF

rem ------------------------------------
rem VARIABLEN:
rem Netzwerkname:
set "nname=Ethernet"

rem ------------------------------------
rem VPN-VERBINDUNG:
rem VPN Verbindung trennen
C:\Windows\System32\taskkill.exe /F /IM openvpn.exe

rem ------------------------------------
rem DNS-SERVER:
rem DNS-Server zurücksetzen
netsh interface ip set dns "%nname%" dhcp

rem ------------------------------------

exit
