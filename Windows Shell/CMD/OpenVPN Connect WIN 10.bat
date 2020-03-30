rem Script für Windows zum Verbinden mit einem OpenVPN-Server und dem umstellen des DNS-Servers
rem Muss als Administrator ausgeführt werden.
rem ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

@echo OFF

rem ------------------------------------
rem VARIABLEN:
rem Netzwerkname:
set "nname=Ethernet"

rem ------------------------------------
rem VPN-VERBINDUNG:
rem VPN Verbinden

"C:\Program Files\OpenVPN\bin\openvpn-gui.exe" --connect "Profile.ovpn"
timeout 15

rem ------------------------------------
rem DNS-SERVER:
rem DNS-Server ändern.
netsh interface ip set dns "%nname%" static 192.168.178.20

rem ------------------------------------

exit

