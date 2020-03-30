@echo off & setlocal 
rem Dies ist ein Fan-Basiertes Textadventure zum Survivalspiel ARK Survival evolved. Es ist nicht fertiggestellt, 
rem kann jedoch grundliegend schon gespielt werden. 

:ROOT
if "%delay%==" set /a delay=4
if NOT exist %appdata%\ARKsta mkdir %appdata%\ARKsta >NUL
if NOT exist %appdata%\ARKsta\save.sv copy NUL %appdata%\ARKsta\save.sv >NUL
if NOT exist %appdata%\ARKsta\inv.inv copy NUL %appdata%\ARKsta\inv.inv >NUL
TITLE ~~~~~~~~~~ARK~~~~SURVIVAL~~~~TEXT~~~~ADVENTURE~~~~~~~~~~
cls
echo.                           
echo.                                                                          
echo.                                                                          
echo              `@@@@@         '@@@@@@@@@'       #@@@`     :@@@@            
echo              #@@@@@.        @@@@@@@@@@@@.     @@@@`     @@@@:            
echo              @@@@@@#        @@@@@@@@@@@@@`    @@@@`    @@@@@             
echo             `@@@@@@@        @@@@`    @@@@#    @@@@`   +@@@@              
echo             #@@#'@@@,       @@@@`    `@@@@    @@@@`  `@@@@`              
echo             @@@' @@@@       @@@@`     @@@@    @@@@`  @@@@:               
echo            .@@@  @@@@       @@@@`    `@@@@    @@@@` #@@@@                
echo            @@@@  '@@@:      @@@@`    .@@@@    @@@@``@@@@                 
echo            @@@+  `@@@@      @@@@`   .@@@@,    @@@@`@@@@                  
echo           :@@@    @@@@      @@@@@@@@@@@@#     @@@@@@@@#                  
echo           @@@@    +@@@'     @@@@@@@@@@@       @@@@.@@@@`                 
echo           @@@+     @@@@     @@@@@@@@@@@       @@@@`#@@@@                 
echo          ;@@@      @@@@     @@@@`  @@@@@      @@@@` @@@@#                
echo          @@@@@@@@@@@@@@+    @@@@`   @@@@#     @@@@` .@@@@`               
echo          @@@@@@@@@@@@@@@    @@@@`    @@@@     @@@@`  @@@@@               
echo         '@@@@@@@@@@@@@@@`   @@@@`    @@@@'    @@@@`   @@@@#              
echo         @@@@        #@@@#   @@@@`    :@@@@    @@@@`   .@@@@`             
echo        `@@@#        `@@@@   @@@@`     @@@@.   @@@@`    @@@@@             
echo        '@@@,         @@@@.  @@@@`     #@@@@   @@@@`     @@@@#            
echo        @@@@          #@@@#  @@@@`      @@@@   @@@@`     :@@@@            
echo.                                                                          
echo.
ping -n %delay% localhost >NUL
cls
echo.                                                                          
echo                  @# #:  @ #@@  @   @`@;#  @' `@  .@                      
echo                 @ ; #:  @ #, @ @` ;+`# @  @  @+@ .@                      
echo                 @+  #:  @ #, @ ;+ @ `@ @  @ @  @ .@                      
echo                 @@  #:  @ #@@.  @ @ `@ @+:@ @+ @ .#                      
echo                   @`+# `@ #,`@  @:' `@  @@  @@@@'.@                      
echo                `@@@  @@@` #, @` ;@  `@  @#  @ `@ .@@#                    
echo.                                                                          
echo.                                                                          
echo.                                                                          
echo.                                                                          
echo.                                                                          
echo.                                                                          
echo.                                                                          
echo    #@@@##@@+#' .@@@@@`   @@  #@@` ;#  ;'#@@+ @  #.@@@@`@  .# @@@  @@@    
echo      @  #,   @ @  .@     @@  @, @+ @  @ #,   +@ #. .@  @  .# @ ;@ @      
echo      @  #@@` #@#  .#    `++' #,  @ @ `@ #@@` #@ #. .@  @  .@ @ '@ @@@    
echo      @  #,   ;@+  .@    @  @ #,  @ ;+:' @,  `# ##. .@  @  .# @@@  @      
echo      @  #,   @ @  .@    @@@@ #, ;# `@@  #,  `# @'. .@  @` #+ @ #+ @      
echo      @  #@@##' '# .#   .#  #+#@@@   @@  #@@#`# `@. .@  `@@@  @ `@ @@@`   
echo.                                                                          
echo.                                                                          
echo.
ping -n 4 localhost >NUL
cls

:STARTMENU
cls
echo                              ,@   @@@. `#  @              
echo                              @@,  @`.@ .# @#              
echo                             `@##  @``@ .#`@               
echo                             ;#.@  @`:@ .#@`               
echo                             @. @  @`@# .@#+               
echo                             @@@@: @`:@ .# @.              
echo                            .#  .# @``@ .# @@              
echo                            ;,   @ @` ':`# `@              
echo.     
echo                 @@@..@  '@ @@@@ @#  ;@`@ @`  ;: `@#  .#   
echo                 @   .@  '@ @` @:;@  @+`@ ;' `@  ;#@  .#   
echo                 @`  .@  '@ @` @:`@  @``@ `@ `@  @:@: .#   
echo                 @@@ .@  '@ @@@#  @  @ `@  @ ;@  @ ## .#   
echo                   @#.@  '@ @``@  .+;, `@  @+@, `@@@@ .#   
echo                   ##`@  @# @` @,  @@  `@  ;@@  ;,  @ .#   
echo                .@@@` ;@@@ `@` @#  @@  `@  `@# `@   +:.@@@ 
echo.     
echo                             TEXT ADVENTURE
echo.
echo                             1. Neues Spiel
echo                             2. Spiel Laden
echo                             3. Optionen
echo                             4. Spieldaten entfernen [GEHT NICHT]
echo                             5. Beenden
set "memory=STARTMENU"
set /p eingabe=
if %eingabe% == 1 goto NEWGAME
if %eingabe% == 2 goto LOADGAME
if %eingabe% == 3 goto OPTIONEN
if %eingabe% == 4 goto STARTMENU
if %eingabe% == 5 exit
if "%eingabe%" == "debug" goto NEWGAME
set "error=Ungueltige Eingabe"
goto ERROR



:GAMEOVER
cls
echo. 
echo.                                                             
echo.                                                             
echo   @@@@+  :@#   @@`  '@@ +@@@:    @@@@, @:  `@ @@@@ ;@@@@    
echo  @+      @.@   @'#  @.@ '@      @'  :@ #@  @# @:   ;@  @`   
echo  @       @ @:  @ @ `@.@ '@     `@    @ `@  @  @:   ;@ '@    
echo  @ `@@+ ;# ;@  @ @,@'.@ '@@@   `@    @  @,`@  @@@@ ;@@@     
echo  @.  @+ @@@@@  @ ;@@ .@ '#      @.  `@  ###+  @:   ;@ '@    
echo  ;@, @+ @   @' @  @@ .@ +@      #@  @+   @@   @:   ;@  @`   
echo    @@# :@   '@ @  @: .@ :@@@;     @@     @@   @@@@ ;@  @#   
echo.
echo.
echo.
echo                  { [DANKE FUERS SPIELEN] }
echo.
echo.
echo.
echo.
echo.
ping -n 5 localhost >NUL
if exist %appdata%\ARKsta\save.sv del %appdata%\ARKsta.sv /q/f >NUL
goto ROOT



REM SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!
REM SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!
REM SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!SZENEN!!

:NEWGAME
cls        
if exist %appdata%\ARKsta\save.sv del %appdata%\ARKsta\save.sv /q/f && copy NUL %appdata%\ARKsta\save.sv
if exist %appdata%\ARKsta\save.sv del %appdata%\ARKsta\inv.inv /q/f && copy NUL %appdata%\ARKsta\inv.inv
set /a hunger=100
set /a trinken=100
set /a ausdauer=100
set /a counter=0
set /a leben=100
set /a waffe=1
set /a search=0
if "%eingabe%" == "debug" goto DEBUG
goto NEWGAME1

:NEWGAME1
cls          
echo.
echo.
echo.                                              
echo `'::+;':#+;+++'':.                                                             
echo `''+++''#++'++'''` ::                                                          
echo `'.++#+'+;'':;;+'';::`                       Leichtes  Kopfweh und leichter 
echo :':++#+';'''';;+'++;': ;::';` ,,`            Schwindel. Es ist hell und die 
echo `+'+++'+'''+';;;'';'''.'''';,:;;:            Hitze, Kaum zu ertragen...
echo '+'++';+''++'+''''''';;:;'''''';:::::        Wo bin ich bloss? 
echo ++:+++'''+#++++'++''''''''++++';'+';:                          
echo .+:#+++'+'++#+#++++++'+''''+++'''++'':`      Warum zittert der Boden? 
echo :#,++++++++'++++++++#++++'++++'''++'';;:     Und was sind das fuer Geraeusche? 
echo '',+++'+++++'++#;+++++++#'''#++'++++'';;:                                 
echo ++;+++'++++++++#+'++##+++#++++++;+++++'';                             
echo '''++++++#++++++++++#+#++###++++';;;;;,.     Was willst du machen?
echo '+;''+:'';'++'+;:++#++#+####++'++'';         1. Augen oeffnen
echo +++''#;'''''++;;;'+++#''++##+''#++';:        2. Tief durchatmen
echo +''':++:;'++'+':'+++++#++''+'+`    `:        3. Horchen
echo ':@#@;;;;;#@'#;:+++';;;+,,+;                 5. Inventar
echo ';##@+#@@@#+++''+++:'+;'+;'+,                0. Stats
echo +'#@#+@+###+'#++#++,.:+#'#+'', `     ;::,.`                                    
echo +'+++++++';'#+##++#';,#+#####+++'. +++'''++.                                   
echo +;+++:,,,,';'';'#+##+#++;.,:;''+':.',++++++.                                   
set "memory=NEWGAME1"
set /p eingabe=
if %eingabe% == 1 goto AUGENAUF
if %eingabe% == 2 goto ATMEN
if %eingabe% == 3 goto HORCHEN
if %eingabe% == 5 goto INVENTAR
if %eingabe% == 0 goto STATS
if "%eingabe%" == "debug" goto DEBUG
set "error=Ungültige Eingabe"
goto ERROR



:AUGENAUF
set "dice="
set "gerausch="
set "wasdenn="
set "wiedenn="
set "warumdenn="
cls
echo        :::                       
echo         ,;:` ` ``            Ich oeffne die Augen
echo         ,.:,,,`;``           Das Licht blendet mich
echo         :;;':::,,,,`             
echo     `` ';'';;';:,.,,         Langsam erkenne ich den Ozean.
echo    ,;'#`,#,'+'''''. .        Blau schimmernd und kristallklar.
echo   ,,++++;+,+##+#.            Es scheint keine Zivilisation hier
echo   `',.,:#'+#@'#':            zu geben. Kein Lebenszeichen von
echo    .,:''''+##,. ; :`         anderen Menschen.
echo    .  ,:'#'#;,.;+::,             
echo   `';   :@';,;;:;'+;,`           
echo  ,'+;#+#':;,;;:,.,::,            
echo  ;:#''+',;:@`.`.`   .,       Ich erblicke einige Kreaturen jedoch
echo .,;#,```  :@         `       kann ich keine davon richtig benennen.
echo `.:`      ,@'                Sie sehen aehnlich aus wie ein Huhn,
echo ``        ;@:                aber irgendwie duemmer...
echo           ;@                 Sie scheinen nicht wirklich zu wissen
echo           :@                 was sie machen. Ich denke wenn ich
echo           .@                 Hunger kriege koennte das eine 
echo           ,@                 gute Nahrungsquelle sein. 
echo           .@                     
echo           ,@                 1. Kreatur betrachten    3. Umsehen
echo           .@                 2. Weiter gehen          5. Inventar
set "memory=AUGENAUF"
set /p eingabe=
if %eingabe% == 1 goto DODO
if %eingabe% == 2 set "search=" && goto STRAND
if %eingabe% == 3 goto SEARCH
if %eingabe% == 5 goto INVENTAR
if %eingabe% == 0 goto STATS
if "%eingabe%" == "debug" goto DEBUG
set "error=Ungültige Eingabe"
goto ERROR


:STRAND
cls
set "danger=" && set "dan="
set "ger="
if %ausdauer% LEQ 98 set /a ausdauer=%ausdauer%+2
set /a hunger=%hunger%-5
set /a trinken=%trinken%-10
set /a search=0
if %hunger% LEQ 0 (set /a leben=%leben%-9 && set "dan=HUNGER!! ") else (set "dan=")
if %trinken% LEQ 0 (set /a leben=%leben%-9 && set "ger=DURST!! ") else (set "ger=")
if %hunger% LEQ 0 set /a hunger=0
if %trinken% LEQ 0 set /a trinken=0
set "danger=%dan%%ger%"
if %leben% LEQ 0 goto GAMEOVER
:STRAND1
if %hunger% LEQ 0 (set "dan=HUNGER!! ") else (set "dan=")
if %trinken% LEQ 0 (set "ger=DURST!! ") else (set "ger=")
set /a leben=%leben%
set /a ausdauer=%ausdauer%
cls
set "memory=STRAND1"
set "sea=true"
if exist %appdata%%\ARKsta\save.sv del %appdata%\ARKsta\save.sv /q/f >NUL
copy NUL %appdata%\ARKsta\save.sv >NUL
>>%appdata%\ARKsta\save.sv echo memory.STRAND1
>>%appdata%\ARKsta\save.sv echo leben.%leben%                                 
>>%appdata%\ARKsta\save.sv echo ausdauer.%ausdauer%
>>%appdata%\ARKsta\save.sv echo durst.%trinken%
>>%appdata%\ARKsta\save.sv echo hunger.%hunger%
cls
echo.
echo   Ich bin endlich unten am Strand angekommen
echo   und Schaue über Das Meer, vieleicht könnte
echo   ich einen Fisch fangen? Ich sollte mich jedoch
echo   vorsehen. Man weiss nie was da lauert..?
echo   Um mich rum ist nur Sand. Vieleicht sollte ich
echo   mich wieder auf dem weg machen.
echo   Links von mir geht es in einen Wald und
echo   gerade aus komme ich zu einer Klippe
echo.
echo   1. Gehe in den Wald   5. Inventar
echo   2. Gehe zur Klippe    0. Stats
echo   3. Umsehen
echo.     
echo.    %danger%
echo                               ``                                 ``
echo  ``  ``          ``       ` `     ``  ``          ``       ` `     
echo ,,,:,,,,::``..,.,,,.:,,:;  ,,,,.,.,,,:,,,,::``..,.,,,.:,,:;  ,,,,.,.
echo ,,,,,,,,,,,,,,,,,,,,,,,,,,,,.,.,,.,,,,,,,,,,,,,,,,,,,,,,,,,,,,.,.,,.
echo ,,,,,,,,,,,,,,,,,,,,,,,,,,,,,...,.,,,,,,,,,__-----------__,,,,,...,.
echo ,,,,,,,,__----------__,,,,,,,,,,,.,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,.
echo ,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,__------__,,,,,,,,,,,,,,,,,,,,,,,,,,,,
echo @@@@@;;;;;;;;;;;;;;;;@@@@@@@@@;;;;;;,,;;;;;;;;;;;;;;@@@@@@;;;;;;;;;;
echo @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@;;;;@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
echo @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
set /p eingabe=
if %eingabe% == 0 goto STATS
if %eingabe% == 1 goto WALD
if %eingabe% == 2 goto CLIFF
if %eingabe% == 3 goto SEARCHSTORY
if %eingabe% == 5 goto INVENTAR
if "%eingabe%" == "debug" goto DEBUG
goto STRAND1



:CLIFF
cls
set "danger="
if %ausdauer% LEQ 98 set /a ausdauer=%ausdauer%+2
set /a hunger=%hunger%-5
set /a trinken=%trinken%-10
set /a search=0
if %hunger% LEQ 0 (set /a leben=%leben%-9 && set "dan=HUNGER!! ") else (set "dan=")
if %trinken% LEQ 0 (set /a leben=%leben%-9 && set "ger=DURST!! ") else (set "ger=")
if %hunger% LEQ 0 set /a hunger=0
if %trinken% LEQ 0 set /a trinken=0
set "danger=%dan%%ger%"
if %leben% LEQ 0 goto GAMEOVER
:CLIFF1
if %hunger% LEQ 0 (set "dan=HUNGER!! ") else (set "dan=")
if %trinken% LEQ 0 (set "ger=DURST!! ") else (set "ger=")
set /a leben=%leben%
set /a ausdauer=%ausdauer%
set "memory=CLIFF1"
set "sea=false"
if exist %appdata%%\ARKsta\save.sv del %appdata%\ARKsta\save.sv /q/f >NUL
copy NUL %appdata%\ARKsta\save.sv >NUL
>>%appdata%\ARKsta\save.sv echo memory.CLIFF1
>>%appdata%\ARKsta\save.sv echo leben.%leben%                                 
>>%appdata%\ARKsta\save.sv echo ausdauer.%ausdauer%
>>%appdata%\ARKsta\save.sv echo durst.%trinken%
>>%appdata%\ARKsta\save.sv echo hunger.%hunger%
cls
echo                                Ich stehe hoch auf einer Klippe. Der Wind
echo                                Zieht mir um das Gesicht und das leise 
echo                                Kreischen von Voegeln ist zu hoeren.
echo ###`                           
echo ###++#+#                       Im Sturzflug stechen sie in den Ozean um
echo #@#@@@+++                      Fische zu fangen. Ihre Nester scheinen
echo +++##@###                      sie jedoch nicht in der Naehe zu haben.
echo ###@#@@#+++                    Sie fliegen mit ihrer Beute weiter 
echo #@@+##@##@+##+;`               Landinwaerts. Ganz in der Naehe sitzt
echo ##+#####+##+++#+#+:            einer dieser Voegel.
echo @###+#########@@.              
echo #@@##+@@++#######              Tief unten im Wasser erkennt man 
echo @#+@@#@@@@##+++''              gigantische Schatten in der Form eines
echo @@@@#@@@@####+@@:              Hais. Die schiere groesse der Kreaturen
echo #######'####@####.             und die Vielzahl die da unten im Wasser
echo ##@#+######@####+              schwimmen machen mir Angst. Ich habe
echo ##@#####+#+@#++#,              nichts womit ich mich gegen solche  
echo ########+###++##               Bestien wehren koennte.
echo @###++@#+##@##+##              
echo #@##@@##+######@#+             %danger%
echo +##@@@####+@++###';            
echo @####@##'+++##@@###            1. Zum Strand           4. Umsehen
echo @##+#@######+#+####.           2. Zum Berg             5. Inventar
echo #####+@++#+#'#++@##`           3. Vogel betrachten     0. Stats
set /p eingabe=
if %eingabe% == 0 goto STATS
if %eingabe% == 1 goto STRAND
if %eingabe% == 3 goto BIRD
if %eingabe% == 4 goto SEARCHSTORY
if %eingabe% == 5 goto INVENTAR
if "%eingabe%" == "debug" goto DEBUG
goto CLIFF1


:WALD
cls
set "danger="
if %ausdauer% LEQ 98 set /a ausdauer=%ausdauer%+2
set /a hunger=%hunger%-5
set /a trinken=%trinken%-10
set /a search=0
if %hunger% LEQ 0 (set /a leben=%leben%-9 && set "dan=HUNGER!! ") else (set "dan=")
if %trinken% LEQ 0 (set /a leben=%leben%-9 && set "ger=DURST!! ") else (set "ger=")
if %hunger% LEQ 0 set /a hunger=0
if %trinken% LEQ 0 set /a trinken=0
set "danger=%dan%%ger%"
if %leben% LEQ 0 goto GAMEOVER
:WALD1
if %hunger% LEQ 0 (set "dan=HUNGER!! ") else (set "dan=")
if %trinken% LEQ 0 (set "ger=DURST!! ") else (set "ger=")
set /a leben=%leben%
set /a ausdauer=%ausdauer%
set "memory=WALD1"
set "sea=false"
if exist %appdata%%\ARKsta\save.sv del %appdata%\ARKsta\save.sv /q/f >NUL
copy NUL %appdata%\ARKsta\save.sv >NUL
>>%appdata%\ARKsta\save.sv echo memory.WALD1
>>%appdata%\ARKsta\save.sv echo leben.%leben%                                 
>>%appdata%\ARKsta\save.sv echo ausdauer.%ausdauer%
>>%appdata%\ARKsta\save.sv echo durst.%trinken%
>>%appdata%\ARKsta\save.sv echo hunger.%hunger%
cls
echo.
echo.
echo                               Die Baeume und Buesche um mich
echo             ..                herum lassen kaum Sonnelicht zu.
echo         `'@@@@@#:`            Es ist schwer etwas zu sehen.
echo       #@@@@@@@@@@@@.,         
echo      ;@@@@@@@@@@@@@@@#`       Was erwartet mich in diesem Wald?
echo     `+@@@@@@@@@@@@@@@@#:      
echo     +##@@@@@@@@@@@@@@@@,      Weit vor mir scheint der Wald
echo     #@@@@@@@@@@@@@@@@@@;      bereits zu enden.
echo    `@@@@@@@@@@@@@@@@@@@@#`    Allerdings koennte ich immer noch
echo    `@@@@@@@@@@@@@@@@@@@@;     zurueck zum Strand und auf Hilfe
echo     '@@@@@@@@@@@@#+@@#`       warten.
echo         `:@':#@`  `.          
echo              +@`              Im Busch vor mir raschelt es     
echo              #@`              ploetzlich...Sollte ich nachsehen?
echo              +@`              
echo              #@`              1. Im Busch nachsehen   4. Umsehen
echo              +@`              2. Wald verlassen       5. Inventar
echo              #@`              3. Zurueck zum Strand   0. Stats
echo             `@@,		                     
echo.
echo.                                           %danger%
set /p eingabe=
if %eingabe% == 0 goto STATS
if %eingabe% == 1 goto BUSHSTORY
if %eingabe% == 2 goto WALDANIM
if %eingabe% == 3 goto STRAND
if %eingabe% == 4 goto SEARCHSTORY
if %eingabe% == 5 goto INVENTAR
if "%eingabe%" == "debug" goto DEBUG
goto WALD1

:LICHTUNG
cls
set "danger="
if %ausdauer% LEQ 98 set /a ausdauer=%ausdauer%+2
set /a hunger=%hunger%-5
set /a trinken=%trinken%-10
set /a search=0
if %hunger% LEQ 0 (set /a leben=%leben%-9 && set "dan=HUNGER!! ") else (set "dan=")
if %trinken% LEQ 0 (set /a leben=%leben%-9 && set "ger=DURST!! ") else (set "ger=")
if %hunger% LEQ 0 set /a hunger=0
if %trinken% LEQ 0 set /a trinken=0
set "danger=%dan%%ger%"
if %leben% LEQ 0 goto GAMEOVER
:LICHTUNG1
if %hunger% LEQ 0 (set "dan=HUNGER!! ") else (set "dan=")
if %trinken% LEQ 0 (set "ger=DURST!! ") else (set "ger=")
set /a leben=%leben%
set /a ausdauer=%ausdauer%
set "memory=LICHTUNG1"
set "sea=false"
if exist %appdata%%\ARKsta\save.sv del %appdata%\ARKsta\save.sv /q/f >NUL
copy NUL %appdata%\ARKsta\save.sv >NUL
>>%appdata%\ARKsta\save.sv echo memory.WALD1
>>%appdata%\ARKsta\save.sv echo leben.%leben%                                 
>>%appdata%\ARKsta\save.sv echo ausdauer.%ausdauer%
>>%appdata%\ARKsta\save.sv echo durst.%trinken%
>>%appdata%\ARKsta\save.sv echo hunger.%hunger%
cls
echo.
echo        Es ist nur eine Lichtung -.- Ich hätte doch nicht in den
echo        Wald gehen sollen. Wenn ich schon einmal hier bin, dann kann
echo        ich auch gleich eine Pause machen...Oder vielleicht doch nicht? 
echo        Ich höre wie die Bäume in der Nähe umknicken. GOTT SEI DANK ist 
echo        das hier kein Dinosaurier Spiel, sonst würde gleich ein Dino kommen.
echo                                  %error%    
echo       `'@@@@@#:`                                                  `'@@@@@#:` 
echo      #@@@@@@@@@@@@.,         Aber es beuntuhigt mich             #@@@@@@@@@@@@.
echo    ;@@@@@@@@@@@@@@@#`        dennoch...                        ;@@@@@@@@@@@@@@@
echo   `+@@@@@@@@@@@@@@@@#:                                       :@@@@@@@@@@@@@@@@@
echo   +##@@@@@@@@@@@@@@@@,                                       ,@@@@@@@@@@@@@@@@@
echo __#@@@@@@@@@@@@@@@@@@;_______________________________________@@@@@@@@@@@@@@@@@@
echo  `@@@@@@@@@@@@@@@@@@@@#`                                     @@@@@@@@@@@@@@@@@@
echo  `@@@@@@@@@@@@@@@@@@@@;                                      :@@@@@@@@@@@@@@@@@
echo   '@@@@@@@@@@@@#+@@#`                                        ¨@@@@@@@@@@@@@@#+@
echo       `:@':#@`  `.                                                `:@':#@`  `.
echo            +@`                                                        +@` 
echo            #@`        1. Pause machen         4. Umsehen              #@`   
echo            +@`        2. Weiter gehen         5. Inventar             +@`
echo            #@`        3. Zurueck              0. Stats                #@`
echo           `@@,                                                        @@,
set /p eingabe=
if %eingabe% == 0 goto STATS
if %eingabe% == 1 goto BUSHSTORY
if %eingabe% == 2 goto WALDANIM
if %eingabe% == 3 goto WALD
if %eingabe% == 4 goto SEARCHSTORY
if %eingabe% == 5 goto INVENTAR
if "%eingabe%" == "debug" goto DEBUG
goto LICHTUNG1


REM MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!
REM MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!
REM MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!MENU!!

:LOADGAME
for /f "tokens=1-2 delims=." %%i in ('findstr "memory" "%appdata%\ARKsta\save.sv"') do set "type=%%i"& set "memory=%%j"
if "%memory%" == "" set "error=Kein Savegame gefunden!" && goto ERROR
for /f "tokens=1-2 delims=." %%i in ('findstr "leben" "%appdata%\ARKsta\save.sv"') do set "type=%%i"& set "leben=%%j"
for /f "tokens=1-2 delims=." %%i in ('findstr "ausdauer" "%appdata%\ARKsta\save.sv"') do set "type=%%i"& set "ausdauer=%%j"
for /f "tokens=1-2 delims=." %%i in ('findstr "durst" "%appdata%\ARKsta\save.sv"') do set "type=%%i"& set "trinken=%%j"
for /f "tokens=1-2 delims=." %%i in ('findstr "hunger" "%appdata%\ARKsta\save.sv"') do set "type=%%i"& set "hunger=%%j"
set "error=Savegame wird geladen!" && goto ERROR

:ERROR
cls
echo.                                            
echo.
echo                          #@  @@` @;  @@@    @@@      
echo                          #@  @;# @:  @`   ,@   @'     
echo                          #@  @;@ @:  @`   '@   @#     
echo                          #@  @:#+@:  @@@. #@   @#     
echo                          #@  @:`@@;  @    '@   @'     
echo                          #@  @: ;@;  @     @, ,@      
echo                          #@  @:  @,  @      @@@       
echo.
echo.
echo.
echo.
echo.
echo.
echo                        %error%
echo. 
echo.
echo.
echo.
echo.
ping -n 5 localhost >NUL
goto %memory%


:STATS
cls
echo.
echo                       @@ #@@@#  @'  @@@@@`.@@    
echo                      @  .  @   .@@   ;@:  @ .    
echo                      @    `@   #`@   ;@: `@      
echo                      @@`  `@   @ @   '@:  @@     
echo                       @@, `@   @ ;:  ;@:  `@@    
echo                        `# `@  .@@@@  ;@:    #.   
echo                      ; '' `@  #`  @. ;@: .  @    
echo                      @@@   @  @   @+ ;@:  @@.    
echo. 
echo                          Hunger=%hunger% 
echo                          Durst=%trinken% 
echo                          Ausdauer=%ausdauer%
echo                          Leben=%leben% 
echo.
echo.
echo.
echo.
echo.
echo.
pause
goto %memory%



:OPTIONEN
cls
echo.
echo.
echo.
echo   [Grafikkarte]      BrainMX 980 Ti 120TB 
echo.
echo   [Aufloesung]       RealImaginationHD
echo.
echo   [FPS-Lock]         Unlimited
echo.
echo   [Soundkarte]       Dolby Surreal 9.1
echo.
echo   [Detailstufe]      ULTRA
echo. 
echo   [Untertitel]       Aus
echo.
echo.  [Textfarbe]        Weiss (1), Rot (2), Gruen (3), Gelb (4)
echo.
echo.
echo.
set /p eingabe=
if %eingabe% == 1 color 0F
if %eingabe% == 2 color 0C
if %eingabe% == 3 color 0A
if %eingabe% == 4 color 0E
goto %memory%

:DEBUG
set /a hunger=100
set /a trinken=100
set /a ausdauer=100
set /a counter=0
set /a leben=100
set /a waffe=1
set /a search=0
cls
echo.
echo.
echo.
echo.
echo.
echo.
echo           Waehle Szene aus:
echo.
echo.
echo   1. Anfang       2. Aufwachen     3. Strand      4. Klippe      5. Wald
echo   6. Lichtung
echo   7. Kampf Dodo   8. Kampf Vogel   9. Kampf Raptor  0. Stats verstellen
echo.
echo.
echo.
echo.
echo.
set /p eingabe= 
if %eingabe% == 1 goto NEWGAME1
if %eingabe% == 2 goto AUGENAUF
if %eingabe% == 3 goto STRAND
if %eingabe% == 4 goto CLIFF
if %eingabe% == 5 goto WALD
if %eingabe% == 7 goto DODO
if %eingabe% == 8 goto BIRD
if %eingabe% == 9 goto RAPTOR
if %eingabe% == 6 goto LICHTUNG
if %eingabe% == 0 goto SETSTAT
if "%eingabe%" == "debug" goto DEBUG 

goto %memory%


:SETSTAT
echo.
echo.
echo.
echo.
echo.
echo                Welchen Stat möchtest du aendern?
echo.
echo                1. Hunger            2. Durst
echo                3. Ausdauer          3. Lebenspunkte
echo.
echo.
echo.
set /p eingabe2=
echo                Bitte Zahl eingeben:
set /p eingabe=
if %eingabe2% == 1 if %eingabe% LEQ 999 set /a hunger=%eingabe% && goto STATS
if %eingabe2% == 2 if %eingabe% LEQ 999 set /a trinken=%eingabe% && goto STATS
if %eingabe2% == 3 if %eingabe% LEQ 999 set /a ausdauer=%eingabe% && goto STATS
if %eingabe2% == 4 if %eingabe% LEQ 999 set /a leben=%eingabe% && goto STATS
goto %memory%









REM INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!
REM INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!
REM INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!INVENTAR!!


:INVENTAR
cls
echo.
for /f "tokens=1-3 delims=." %%i in ('findstr "food" "%appdata%\ARKsta\inv.inv"') do set "type=%%i"& set "name=%%j"& set "plushp=%%k"& echo %%j
if "%type%" == "" set "error=Dein Inventar ist leer!"& goto ERROR
echo.
echo.
echo.
echo.
echo              1. Essen        2. Trinken     3.Verlassen    
echo.
echo.
echo.
set /p eingabe=
if %eingabe% == 1 goto GETFOOD
if %eingabe% == 2 goto GETDRINK
if %eingabe% == 3 goto %memory%
set "error=Ungültige Eingabe!" && goto ERROR



:GETFOOD
cls
set /a counter=0
echo.
echo                          Was moechtest du Essen?
for /f "tokens=1-3 delims=." %%i in ('findstr "food" "%appdata%\ARKsta\inv.inv"') do set "type=%%i"& set "name=%%j"& set "plushp=%%k"& echo %%j
if "%type%" == "" set "error=Du hast nichts zu Essen!"& goto ERROR
echo.
echo.
echo.
echo.
echo                          (Korrekten Name eingeben)
set /p eingabe=
set "type="
set "name="
set "plushp="
for /f "tokens=1-3 delims=." %%i in ('findstr "%eingabe%" "%appdata%\ARKsta\inv.inv"') do set "type=%%i"& set "name=%%j"& set "plushp=%%k"
if "%type%" NEQ "" (echo %type%.%name%.%plushp%) else (echo ERROR)
if "%type%" == "" set "error=Sowas hast du nicht!" & goto ERROR
set /a atmp=%hunger%+%plushp%
if %atmp% GEQ 101 set "error=Ich bin satt!" && goto ERROR
if "%type%" NEQ "" goto GETFOOD2
goto %memory%


:GETFOOD2
if exist %temp%\ARK.tmp del %temp%\ARK.tmp /q/f >NUL
for /f "tokens=1-3 delims=." %%i in ('findstr "%eingabe%" "%appdata%\ARKsta\inv.inv"') do set "type=%%i"& set "name=%%j"& set "plushp=%%k"
set /a hunger=%hunger%+%plushp%
set "schreiben="
for /f "usebackq tokens=1-3 delims=." %%i in ("%appdata%\ARKsta\inv.inv") do (
    if "%eingabe%" NEQ "%%j" (
        >>%temp%\ARK.tmp echo %%i.%%j.%%k
    ) else (
        if defined schreiben (>>%temp%\ARK.tmp echo %%i.%%j.%%k) else (set "schreiben=true")
    )
)
if exist %appdata%\ARKsta\inv.inv del %appdata%\ARKsta\inv.inv >NUL
if not exist %appdata%\ARKsta\inv.inv copy %tmp%\ARK.tmp %appdata%\ARKsta\inv.inv >NUL
set "error=Ich habe schon etwas weniger Hunger."
goto %memory%

:GETDRINK
if %trinken% LEQ 90 if "%sea%" == "true" set /a trinken=%trinken%+10 && set "error=Ich fuehle wie der Durst gestillt wird" && goto ERROR
if %trinken% GEQ 91 set "error=Ich habe keinen Durst!" && goto ERROR 
if "%sea%" == "true" goto ERROR

if "%sea%" NEQ "true" set "error=Keine Wasserquelle in der naehe!" && goto ERROR


REM RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!
REM RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!
REM RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!


:SEARCH
if %search% GEQ 3 set "error=Hier ist nichts mehr!" && goto ERROR
set /a dice=%random%*10/32768+1
if %dice% GEQ 5 goto SEARCH
set /a search=%search%+1
if %dice% == 1 set "error=Du hast einen Apfel gefunden!" && >>%appdata%\ARKsta\inv.inv echo food.Apfel.5 && goto ERROR
if %dice% == 2 set "error=Ich kann nichts sehen. Da ist nichts." && goto ERROR
if %dice% == 3 set "error=Hmm hier muss es doch was geben!" && goto ERROR
if %dice% == 4 set "error=Ja da ist ein Apfel!! Den nehm ich mir!" && >>%appdata%\ARKsta\inv.inv echo food.Apfel.5 && goto ERROR
if %dice% == 5 set "error=Ein Apfel!" && >>%appdata%\ARKsta\inv.inv echo food.Apfel.5 && goto ERROR


:SEARCHSTORY
if "%search%" == "" set /a search=0
if %search% GEQ 3 set "error=Hier ist nichts mehr!" && goto ERROR
set /a dice=%random%*100/32768+1
if %dice% GEQ 26 goto SEARCHSTORY
set /a search=%search%+1
if %dice% == 1 set "error=Du hast einen Apfel gefunden!" && >>%appdata%\ARKsta\inv.inv echo food.Apfel.5 && goto ERROR
if %dice% == 2 set "error=Ich kann nichts sehen. Da ist nichts." && goto ERROR
if %dice% == 3 set "error=Hmm hier muss es doch was geben!" && goto ERROR
if %dice% == 4 set "error=Im Gebuesch? Nein da ist nichts..." && goto ERROR
if %dice% == 5 set "error=Ein Stein! Den kann ich als Waffe nutzen!" && >>%appdata%\ARKsta\inv.inv echo waep.Stein.3.3 && goto ERROR
if %dice% == 6 set "error=Nach was suche ich ueberhaubt?" && goto ERROR
if %dice% == 7 set "error=Oh je! Wo bin ich denn da reingetreten?!" && goto ERROR
if %dice% == 8 set "error=Huch ein Apfel! Den nehm ich!" && >>%appdata%\ARKsta\inv.inv echo food.Apfel.5 && goto ERROR
if %dice% == 9 set "error=EINE KNARRE!! Aber nur ein Schuss!" >>%appdata%\ARKsta\inv.inv echo waep.KNARRE.50.1 && goto ERROR
if %dice% == 10 set "error=Oh oh! was ist denn das??" && goto RANDOMFIGHT
if %dice% == 11 set "error=Ein Apfel! Sowas kann man immer brauchen!" && >>%appdata%\ARKsta\inv.inv echo food.Apfel.5 && goto ERROR
if %dice% == 12 goto DODO
if %dice% == 13 set "error=AH!! Ich habe mich geschnitten!" && set /a leben=%leben%-5 && goto ERROR
if %dice% == 14 set "error=Ein Stein! Den kann ich als Waffe nutzen!" && >>%appdata%\ARKsta\inv.inv echo waep.Stein.3.3 && goto ERROR
if %dice% == 15 set "error=Da ist nichts!" && goto ERROR
if %dice% == 16 set "error=Hmm eine seltsame Beere! Mal probieren." && set /a leben=%leben%+5 && goto ERROR
if %dice% == 17 set "error=Da liegt ein Skelett... Es ist nicht menschlich..." && goto ERROR
if %dice% == 18 set "error=Ein Stein! Den kann ich als Waffe nutzen!" 
if %dice% == 19 set "error=Da starrt mich was aus dem Gebuesch an!" && goto ERROR
if %dice% == 20 set "error=Ich dachte ich haette hier was gesehen!" && goto ERROR
if %dice% == 21 set "error=Ein stueck Fleisch? Warum auch nicht?" && >>%appdata%\ARKsta\inv.inv echo food.Fleisch.10 && goto ERROR
if %dice% == 22 set "error=Hier? Nein! Da? Nein!" && goto ERROR
if %dice% == 23 set "error=Da muss doch was zu finden sein!" && goto ERROR
if %dice% == 24 set "error=Ein Speer! Sehr nuetzlich!" && >>%appdata%\ARKsta\inv.inv echo waep.Speer.6.5 && goto ERROR && goto ERROR
if %dice% == 25 set "error=Ein Speer! Kebaptime!" && >>%appdata%\ARKsta\inv.inv echo waep.Speer.6.5 && goto ERROR && goto ERROR
goto %memory%


:RANDOMFIGHT
cls
set /a dice=%random%*10/32768+1
if %dice% GEQ 5 goto RANDOMFIGHT
if %dice% LEQ 3 goto DODO
if %dice% == 4 goto BIRD
if %dice% == 5 goto RAPTOR
goto %memory%

:BUSHSTORY
set /a dice=%random%*10/32768+1
if %dice% GEQ 4 goto BUSHSTORY
if %dice% LEQ 2 goto DODO
if %dice% == 3 goto SEARCHSTORY
REM if %dice% == 3 goto 


REM STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!
REM STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!
REM STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!STORY!!

:ATMEN
cls
echo.
echo.
echo.
echo.
echo.
echo.
echo. 
echo                     Du nimmst einen tiefen Atemzug und der
echo                     der Geruch von Meeresluft und exotischen 
echo                     Blumen dringt in meine Nase. 
echo                     Die Gerueche sind jedoch nicht so wie in
echo                     meiner Erinnerung. Ich kenne sie nicht.
echo.
echo.
echo.
echo.
echo.
echo.
pause
goto %memory%

:HORCHEN
set /a dice=%random%*10/32768+1
if %dice% GEQ 7 goto HORCHEN
if %dice% == 1 set "gerausch=ein Heulen." && set "wasdenn=Es scheint ein Tier zu sein." 
if %dice% == 1 set "wiedenn=Ich sollte mich vorsehen." && set "warumdenn=Es könnte mich angreiffen!"
if %dice% == 2 set "gerausch=einen Schrei!" && set "wasdenn=Dieses Geraeusch macht mir Angst!" 
if %dice% == 2 set "wiedenn=Was koennte das wohl sein?" && set "warumdenn=Sowas hab ich noch nie gehoert!"
if %dice% == 3 set "gerausch=ein Stampfen." && set "wasdenn=Dieses Tier muss riesig sein!" 
if %dice% == 3 set "wiedenn=Ich hoffe es ist nicht feindlich!" && set "warumdenn=Der ganze Boden zittert!"
if %dice% == 4 set "gerausch=ein Wasserfall." && set "wasdenn=Dieses geräusch ist beruhigend." 
if %dice% == 4 set "wiedenn=Ich koennte mal da hin gehen." && set "warumdenn=Wo Wasser ist gibt es Leben."
if %dice% == 5 set "gerausch=ein Knacken!" && set "wasdenn=Es klingt wie berstende Knochen!" 
if %dice% == 5 set "wiedenn=Ich hoffe es ist nicht mehr hungrig!" && set "warumdenn=Ich sollte mich vorsehen!"
if %dice% == 6 set "gerausch=nichts" && set "wasdenn=Diese Ruhe kann Taeuschen!" 
if %dice% == 6 set "wiedenn=Ich sollte nochmals hinhoeren!" && set "warumdenn=Es ist zu ruhig!"
cls
echo.
echo.
echo.
echo.
echo.
echo.
echo. 
echo                     Ich höre das Rauschen von Wellen 
echo                     und Rascheln von Blaettern. 
echo                     In der Ferne hoere ich %gerausch%
echo                     %wasdenn%
echo                     %wiedenn%
echo                     %warumdenn%
echo.
echo.
echo.
echo.
echo.
pause
set "dice="
set "gerausch="
set "wasdenn="
set "wiedenn="
set "warumdenn="
goto %memory%


REM KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!
REM KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!
REM KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!KREATUREN!!


:DODO
set "dodoleben="
set /a dodoleben=6
set /a dice=%random%*10/32768+1
if %dice% GEQ 7 goto DODO
if %dice% == 1 set "gerausch=Die Kreatur ist unbeeindruckt und sucht Koerner." && set "wasdenn=Sie stellt sich dabei sehr ungeschickt an." 
if %dice% == 1 set "wiedenn=Die Augen der Tiere scheint Leer." && set "warumdenn=Als könnte man den Hohlraum dahinter sehen."
if %dice% == 2 set "gerausch=Die Kreatur schaut dich Fragend an." && set "wasdenn=Sie scheint sich zu fragen was ich wohl bin?" 
if %dice% == 2 set "wiedenn=Scheu ist sie nicht. Sie rennt nicht davon" && set "warumdenn=Ich könnte sie Toeten um Nahrung zu erhalten"
if %dice% == 3 set "gerausch=Die Kreatur gibt gackernde Geraeusche von sich." && set "wasdenn=Die anderen Kreaturen antworten ihm nicht." 
if %dice% == 3 set "wiedenn=Es scheint ein Aussenseiter zu sein." && set "warumdenn=Sollte ich es toeten?"
if %dice% == 4 set "gerausch=Ach du... die Kreatur schieint zu" && set "wasdenn=Dumm zu sein um ueberhaubt ueberleben" 
if %dice% == 4 set "wiedenn=zu Koennen" && set "warumdenn=Sie faellt dauernd hin!"
if %dice% == 5 set "gerausch=Die Kreatur schaut zu dir." && set "wasdenn=Sie kann sich wohl nicht entscheiden" 
if %dice% == 5 set "wiedenn=Sie strauchelt und faellt hin" && set "warumdenn=Die gelegenheit zuzuschlagen!"
if %dice% == 6 set "gerausch=Was zum.. soll dass denn darstellen?" && set "wasdenn=Und das Ding stinkt!" 
if %dice% == 6 set "wiedenn=Soll ich dich essen? Soll ich es lassen?" && set "warumdenn=Einfach lächerlich, diese Kreatur!"
:DODOVS
cls
echo      ;:,`             [LEBEN - KREATUR] 
echo  :'`..';::                   %dodoleben%
echo `#+,`,,;'.            
echo .#:,:;;+';            
echo     :.#:;'            %gerausch%
echo      :;::             %wasdenn%
echo      '`+:             %wiedenn%
echo     ;+;'              %warumdenn%
echo    .+:#:   `   `';:;  
echo    :''':`.''+;;,`'..' 
echo    '++'+'++'';+'+`;:: 
echo    '#+++'':+;;#'#;`#` 
echo    ;##;##;,,+';'#'.': 
echo    :#';+,.,::#+'#' ,` 
echo     '+#'#,.,``+#+     
echo      '#++#:`:+##'     1. Angriff
echo       '+###:@##+      2. Zurueck
echo        :+#####''      
echo         ,;'+###;      
echo         .;, `''       
echo        `,:  :+        
echo     .:':,`  ''        
echo          '++'+        
echo          ##;          
set /p eingabe=
if %eingabe% == 1 goto DODOATTACK
if %eingabe% == 2 goto %memory%
set "error=Ungültige Eingabe"
goto ERROR

:DODOATTACK
cls
if "%waffe%" == "" set /a waffe=0
set /a dice=%random%*10/32768+1
if %dice% GEQ 7 goto DODOATTACK
set /a dice2=%random%*10/32768+1
if %dice2% GEQ 7 goto DODOATTACK
if %dice% == 1 set /a dodoleben=%dodoleben%-%waffe%-1
if %dice% == 1 set "gerausch=Fast daneben geschlagen!" && set "wasdenn=Wenigstens habe ich getroffen!"
if %dice% == 2 set /a dodoleben=%dodoleben%-%waffe%-1
if %dice% == 2 set "gerausch=Fast daneben geschlagen!" && set "wasdenn=Na warte!!"
if %dice% == 3 set "gerausch=DANEBEN! Mist!" && set "wasdenn=MPFH!"
if %dice% == 4 set /a dodoleben=%dodoleben%-%waffe%-2 && set /a ausdauer=%ausdauer%-1
if %dice% == 4 set "gerausch=Uh der war nicht schlecht!" && set "wasdenn=Ich krieg dich!"
if %dice% == 5 set "gerausch=Er ist wendiger als er aussieht!" && set "wasdenn=Aber ich krieg dich noch! Komm her!"
if %dice% == 6 set /a dodoleben=%dodoleben%-%waffe%-4 && set /a ausdauer=%ausdauer%-2
if %dice% == 6 set "gerausch=IN YA FACE!" && set "wasdenn=Der war gut!"
if %dice2% == 1 set /a dodoleben=%dodoleben%-1
if %dice2% == 1 set "wiedenn=Sie greifft an! Aber es ist umgefallen" && set "warumdenn=und selber schaden genommen!"
if %dice2% == 2 set /a leben=%leben%-0
if %dice2% == 2 set "wiedenn=Sie greifft an! Rennt aber auf die falsche" && set "warumdenn=Seite! Wo willst du hin?!"
if %dice2% == 3 set /a leben=%leben%-1
if %dice2% == 3 set "wiedenn=Sie greifft an! AUA!" && set "warumdenn=Mein Fuss!! Naaa Warte!"
if %dice2% == 4 set /a leben=%leben%-2
if %dice2% == 4 set "wiedenn=Sie greifft an! OMG sie hat mir" && set "warumdenn=in meinen Finger gebissen!"
if %dice2% == 5 set /a dodoleben=%dodoleben%-1
if %dice2% == 5 set "wiedenn=Sie greifft an! Nein doch nicht!" && set "warumdenn=Sie rennt direkt in einen Stein"
if %dice2% == 6 set /a leben=%leben%-0
if %dice2% == 6 set "wiedenn=Sie greifft an! Daneben!" && set "warumdenn=Probiers nochmal!"
if %leben% LEQ 0 goto GAMEOVER
if %dodoleben% LEQ 0 goto DODOWIN
cls
echo      ;:,`             [LEBEN - KREATUR] 
echo  :'`..';::               %dodoleben%
echo `#+,`,,;'.            
echo .#:,:;;+';            
echo     :.#:;'            %gerausch%
echo      :;::             %wasdenn%
echo      '`+:             %wiedenn%
echo     ;+;'              %warumdenn%
echo    .+:#:   `   `';:;  
echo    :''':`.''+;;,`'..' 
echo    '++'+'++'';+'+`;:: 
echo    '#+++'':+;;#'#;`#` 
echo    ;##;##;,,+';'#'.': 
echo    :#';+,.,::#+'#' ,` 
echo     '+#'#,.,``+#+     
echo      '#++#:`:+##'     1. Angriff
echo       '+###:@##+      2. Zurueck
echo        :+#####''      
echo         ,;'+###;      
echo         .;, `''       [LEBEN] %leben%
echo        `,:  :+        [AUSDAUER] %ausdauer%
echo     .:':,`  ''        
echo          '++'+        
echo          ##;       
set /p eingabe=
if %eingabe% == 1 goto DODOATTACK
if %eingabe% == 2 goto %memory%
set "error=Ungueltige Eingabe"
goto ERROR

:DODOWIN
cls
echo.
echo.
echo.
echo.
echo.
echo.
echo      Du hast die Kreatur besiegt!
set "error=Du hast etwas Fleisch gesammelt"
>>%appdata%\ARKsta\inv.inv echo food.Fleisch.10
ping -n 2 localhost >NUL
goto ERROR 



:RAPTOR
cls
if %eingabe% NEQ 2 set "error="
set /a raptorleben=50
set /a dice=%random%*10/32768+1
if %dice% GEQ 7 goto RAPTOR
if %dice% == 1 set "gerausch=Er scheint Hungrig zu sein!" && set "wasdenn=Er laesst mich nicht aus den Augen" 
if %dice% == 1 set "wiedenn=Flucht oder Kampf?" && set "warumdenn=Das ist hier die Frage!"
if %dice% == 2 set "gerausch=Grosse Zaehne, grosser Mund," && set "wasdenn=gieriege Augen und noch einen Fetzen" 
if %dice% == 2 set "wiedenn=Fleisch im Maul..." && set "warumdenn=Ich sollte mich ihm nicht ohne Waffe stellen!"
if %dice% == 3 set "gerausch=Etwas in mir sagt mir...:" && set "wasdenn=LAAAAAAAAAAAAUF!" 
if %dice% == 3 set "wiedenn=Bin ich schon stark genug?" && set "warumdenn=Er sieht sehr Aggressiv aus!"
if %dice% == 4 set "gerausch=Blutunterlaufene Augen und spitzen Zaehnen." && set "wasdenn=Er hat es ganz klar auf mich abgesehen!" 
if %dice% == 4 set "wiedenn=Was soll ich bloss tun?" && set "warumdenn=Kaempfen? Fluechten?"
if %dice% == 5 set "gerausch=Knurrend sieht er mich an" && set "wasdenn=und beobachtet jede meiner Bewegung." 
if %dice% == 5 set "wiedenn=Er scheint bereit zu sein um" && set "warumdenn=Anzugreiffen! Das bin ich auch!"
if %dice% == 6 set "gerausch=Was haengt ihm bloss aus dem Maul?" && set "wasdenn=Ach du... Das ist ein MENSCHLICHER ARM!" 
if %dice% == 6 set "wiedenn=Das heisst es hat... oder hatte" && set "warumdenn=zumindest mal noch andere Menschen"
:RAPTORVS
cls
echo              @@@@@@@@          
echo          @@@@@@@@@@@@@@@@@@@
echo         @@@@@@@@@@@@@@@@@@@@@@ [LEBEN - KREATUR] 
echo        @@@@@@@@@@@@@@@@@@@@@@        %raptorleben%
echo        @@@@@@@@@@@@@@@@@@@@
echo        @@@@@@@@@@     
echo       @@@@@@@@         
echo      @@@@@@@@      %gerausch%
echo @@@@@@@@@@@@       %wasdenn%
echo @@@@@@@@@@@@       %wiedenn%
echo @@@@@@@@@@@        %warumdenn%
echo @@@@@@@@@@             
echo @@@@@@@@@@             
echo @@@@@@@@               
echo @@@@@@@                1. Kaempfen
echo @@@@@@@                2. Fliehen
echo @@@@@@@                
echo @@@@@@@                [LEBEN] %leben%
echo @   @@@@            [AUSDAUER] %ausdauer%
echo @    @@@@              
echo     @@@@@@             
echo    @@@ @@              
echo      @@@@              
echo     @@@@               
set /p eingabe=
if %eingabe% == 1 goto RAPTORFIGHT
:RAPTORFIGHT
if %eingabe% == 2 set /a dice=%random%*10/32768+1
if %eingabe% == 2 (if %dice% GEQ 6 (goto RAPTORFIGHT))
if %eingabe% == 2 (if %dice% == 5 (goto %memory%)) 
if "%waffe%" == "" set /a waffe=0
set /a dice=%random%*10/32768+1
if %dice% GEQ 7 goto RAPTORFIGHT
set /a dice2=%random%*10/32768+1
if %dice2% GEQ 7 goto RAPTORFIGHT
if %eingabe% == 2 set "error=Ich konnte nicht Fluechten".
if %dice% == 1 set /a raptorleben=%raptorleben%-%waffe%-1
if %dice% == 1 set "gerausch=Fast daneben geschlagen!" && set "wasdenn=Wenigstens habe ich getroffen!"
if %dice% == 2 set /a raptorleben=%raptorleben%-%waffe%-1
if %dice% == 2 set "gerausch=Beinahe daneben geschlagen!" && set "wasdenn=Na warte!!"
if %dice% == 3 set "gerausch=DANEBEN! Mist!" && set "wasdenn=Er hat mich erwischt!" && set /a leben=%leben%-5
if %dice% == 4 set /a raptorleben=%raptorleben%-%waffe%-2 && set /a ausdauer=%ausdauer%-1
if %dice% == 4 set "gerausch=Uh der war nicht schlecht!" && set "wasdenn=Ich krieg dich!"
if %dice% == 5 set "gerausch=Der ist schnell!" && set "wasdenn=Aber ich krieg dich noch! Komm her!"
if %dice% == 6 set /a raptorleben=%raptorleben%-%waffe%-4 && set /a ausdauer=%ausdauer%-2
if %dice% == 6 set "gerausch=IN YA FACE!" && set "wasdenn=Der war gut!"
if %dice2% == 1 set /a raptorleben=%raptorleben%-8
if %dice2% == 1 set "wiedenn=Sie greifft an! Ich konnte ihm aber grade" && set "warumdenn=noch ein Bein stellen!"
if %dice2% == 2 set /a leben=%leben%-0
if %dice2% == 2 set "wiedenn=Sie greifft an! Verbeisst sich jedoch in" && set "warumdenn=einem Ast! Das war knapp!"
if %dice2% == 3 set /a leben=%leben%-20
if %dice2% == 3 set "wiedenn=Sie greifft an! AUA!" && set "warumdenn=Er hat mir ein Stück Fleisch raus gerissen!"
if %dice2% == 4 set /a leben=%leben%-15
if %dice2% == 4 set "wiedenn=Sie greifft an! OMG sie hat meine" && set "warumdenn=Hand erwischt!!"
if %dice2% == 5 set /a leben=%leben%-10
if %dice2% == 5 set "wiedenn=Sie greifft an! Er huepft auf mich und" && set "warumdenn=Trampelt mich nieder!"
if %dice2% == 6 set /a leben=%leben%-35
if %dice2% == 6 set "wiedenn=MIST! Er hat mich erwischt!" && set "warumdenn=Ich sollte fliehen!!"
if %leben% LEQ 0 goto GAMEOVER
if %raptorleben% LEQ 0 set "error=Du hast die Kreatur besiegt und erhaelst Raptorfleisch!  && >>%appdata%\ARKsta\inv.inv echo food.Raptorfleisch.30
if %raptorleben% LEQ 0 set goto ERROR
goto RAPTORVS
goto %memory%



:BIRD
cls
set "weiterleitung="
if %eingabe% NEQ 2 set "error="
set /a birdleben=21
set /a dice=%random%*10/32768+1
if %dice% GEQ 7 goto BIRD
if %dice% == 1 set "gerausch=Als die Kreatur mich erblickt" && set "wasdenn=faengt sie lauthals an zu" 
if %dice% == 1 set "wiedenn=kraechtzen. Sie baeumt sich auf." && set "warumdenn=Sie versucht mir wohl zu drohen!"
if %dice% == 2 set "gerausch=Ich schleiche mich an sie ran" && set "wasdenn=waehrend sie einen Fisch ver-" 
if %dice% == 2 set "wiedenn=schlingt. Obwohl sie mich jetzt" && set "warumdenn=bemerkt hat fliegt sie nicht davon."
if %dice% == 3 set "gerausch=Die Kreatur scheint mich nicht" && set "wasdenn=zu bemerken. Ich koennte" 
if %dice% == 3 set "wiedenn=das jetzt Ausnuetzen und mich" && set "warumdenn=auf sie stuerzen!"
if %dice% == 4 set "gerausch=Als die Kreatur mich erblickt" && set "wasdenn=wendet sie ihren Koerper zu mir und" 
if %dice% == 4 set "wiedenn=legt den Kopf auf den Boden. Sie" && set "warumdenn=scheint vertraut mit Menschen..."
if %dice% == 5 set "gerausch=Ich verstecke mich im Gebuesch und" && set "wasdenn=beobachte sie. Die Haut der Kreatur" 
if %dice% == 5 set "wiedenn=scheint als ob sie aus Leder waere." && set "warumdenn=Dunkel, Faltig und Trocken."
if %dice% == 6 set "gerausch=Als ich an die Kreatur ran trete," && set "wasdenn=erschreckt sie sich so sehr, dass" 
if %dice% == 6 set "wiedenn=sie gleich die Fluegel ausbreitet" && set "warumdenn=und davon fliegt." 
if %dice% == 6 goto BIRDFLY
:BIRDVS
cls
echo               '                                 
echo                ;    ;                           
echo                ;;    ;';                    [LEBEN - KREATUR] 
echo                 ;';    ;:;;                        %birdleben%
echo :                ;''      '':'                 
echo  :::             ;';'        '::''          %gerausch%
echo     ;::           ''''+      #':+++''+      %wasdenn%
echo      :;;;         '''''+   '''#'+' ':+''+;' %wiedenn%
echo        :;;::      '+++#++ ;'+'';;;          %warumdenn%
echo         :;;;;:     #';'+'++;'   ;;'             
echo          +;;;:,; ++';+#++++'      :;+       %error%
echo            ,'+;:'''+'+'#''+;         ;          
echo              ':'';';;;'#,++++          '    1. Kaempfen
echo             ;'#;'';'';+'  ++++              2. Packen
echo              +++.':';,:'   +++'             3. Zurueck
echo            '+  ;++:;:,;'     ++                 
echo          '    '     +;;;:      +            [LEBEN] %leben%
echo         '    '        ''''      +        [AUSDAUER] %ausdauer%
echo         '+  .          :'++      +              
echo               '          '+;                    
echo                              #                  
set /p eingabe=
if %eingabe% == 1 goto BIRDFIGHT
if %eingabe% == 2 goto BIRDFLY
if %eingabe% == 3 goto %memory%
:BIRDFIGHT
if "%waffe%" == "" set /a waffe=0
set /a dice=%random%*10/32768+1
if %dice% GEQ 7 goto BIRDFIGHT
set /a dice2=%random%*10/32768+1
if %dice2% GEQ 7 goto BIRDFIGHT
if %eingabe% == 2 set "error=Ich konnte nicht Fluechten".
if %dice% == 1 set /a birdleben=%birdleben%-%waffe%-1
if %dice% == 1 set "gerausch=Fast daneben geschlagen!" && set "wasdenn=Wenigstens habe ich getroffen!"
if %dice% == 2 set /a birdleben=%birdleben%-%waffe%-1
if %dice% == 2 set "gerausch=Beinahe daneben geschlagen!" && set "wasdenn=Na warte!!"
if %dice% == 3 set "gerausch=DANEBEN! Mist!" && set "wasdenn=Er hat mich erwischt!" && set /a leben=%leben%-5
if %dice% == 4 set /a raptorleben=%birdleben%-%waffe%-2 && set /a ausdauer=%ausdauer%-1
if %dice% == 4 set "gerausch=Uh der war nicht schlecht!" && set "wasdenn=Ich krieg dich!"
if %dice% == 5 set "gerausch=Der ist schnell!" && set "wasdenn=Aber ich krieg dich noch! Komm her!"
if %dice% == 6 set /a raptorleben=%birdleben%-%waffe%-4 && set /a ausdauer=%ausdauer%-2
if %dice% == 6 set "gerausch=IN YA FACE!" && set "wasdenn=Der war gut!"
if %dice2% == 1 set /a raptorleben=%birdleben%-8
if %dice2% == 1 set "wiedenn=Sie greifft an! Ich konnte ihm aber grade" && set "warumdenn=noch ein Bein stellen!"
if %dice2% == 2 set /a leben=%leben%-0
if %dice2% == 2 set "wiedenn=Sie greifft an! Verbeisst sich jedoch in" && set "warumdenn=einem Ast! Das war knapp!"
if %dice2% == 3 set /a leben=%leben%-8
if %dice2% == 3 set "wiedenn=Sie greifft an! AUA!" && set "warumdenn=Er hat mir ein Stück Fleisch raus gerissen!"
if %dice2% == 4 set /a leben=%leben%-10
if %dice2% == 4 set "wiedenn=Sie greifft an! OMG sie hat meine" && set "warumdenn=Hand erwischt!!"
if %dice2% == 5 set /a leben=%leben%-3
if %dice2% == 5 set "wiedenn=Sie greifft an! Er huepft auf mich und" && set "warumdenn=Trampelt mich nieder!"
if %dice2% == 6 set /a leben=%leben%-15
if %dice2% == 6 set "wiedenn=MIST! Er hat mich erwischt!" && set "warumdenn=Ich sollte fliehen!!"
if %leben% LEQ 0 goto GAMEOVER
if %birdleben% LEQ 0 set "error=Du hast die Kreatur besiegt und erhaelst Vogelfleisch!  && >>%appdata%\ARKsta\inv.inv echo food.Vogelfleisch.15
if %birdleben% LEQ 0 set goto ERROR
goto BIRDVS
goto %memory%


:BIRDFLY
if %eingabe% == 2 (set /a dice=%random%*10/32768+1)
if %eingabe% == 2 (if %dice% GEQ 4 goto BIRDFLY)
if %birdleben% NEQ 21 set "error=Der Vogel ist zu schwach zum fliegen." goto BIRDFIGHT
if %dice% == 1 set "gerausch=" && set "wasdenn=" 
if %dice% == 1 set "wiedenn=" && set "warumdenn="
REM if %dice% == 1 set "memory="
if %dice% == 2 set "gerausch=" && set "wasdenn=" 
if %dice% == 2 set "wiedenn=" && set "warumdenn="
REM if %dice% == 2 set "memory="
if %dice% == 3 set "gerausch=" && set "wasdenn=" 
if %dice% == 3 set "wiedenn=" && set "warumdenn="
REM if %dice% == 3 set "memory="
echo.
echo                             `;';;;;;;':`       
echo                  ,     .+';;;;;':              
echo                .:     ;';;;;;'                 
echo            ,;:;:      ;;;;;'`                  
echo           .;:. ;.    :';;;:                    
echo                 ;:;;;;';;;                     
echo                  ';;;;;;;'                     
echo           .,;'''''''''',';;`                   
echo           ';;;'';,,:'';.      `                
echo          ;;;'            :                     
echo          .;;                                   
echo           ';                                   
echo            .                                   
echo.
echo            %gerausch%
echo            %wasdenn%
echo            %wiedenn%
echo            %warumdenn%
echo.
echo.
pause
goto %memory%

REM ANIMATIONEN!!!ANIMATIONEN!!!ANIMATIONEN!!!ANIMATIONEN!!!ANIMATIONEN!!!ANIMATIONEN!!!ANIMATIONEN!!!ANIMATIONEN!!!
REM ANIMATIONEN!!!ANIMATIONEN!!!ANIMATIONEN!!!ANIMATIONEN!!!ANIMATIONEN!!!ANIMATIONEN!!!ANIMATIONEN!!!ANIMATIONEN!!!
REM ANIMATIONEN!!!ANIMATIONEN!!!ANIMATIONEN!!!ANIMATIONEN!!!ANIMATIONEN!!!ANIMATIONEN!!!ANIMATIONEN!!!ANIMATIONEN!!!

:WALDANIM
cls
echo                                ..                ..     
echo                            `'@@@@@#:`        `'@@@@@#:`   
echo                           #@@@@@@@@@@@@.,   #@@@@@@@@@@@@.,    
echo                         ;@@@@@@@@@@@@@@@#`;@@@@@@@@@@@@@@@#`      
echo                        `+@@@@@@@@@@@@@@@@#:@@@@@@@@@@@@@@@@#:      
echo                        +##@@@@@@@@@@@@@@@@,#@@@@@@@@@@@@@@@@,
echo                        #@@@@@@@@@@@@@@@@@@;@@@@@@@@@@@@@@@@@;    
echo                       `@@@@@@@@@@@@@@@@@@@@#`@@@@@@@@@@@@@@@@#`  
echo                       `@@@@@@@@@@@@@@@@@@@@;@@@@@@@@@@@@@@@@;   
echo                        '@@@@@@@@@@@@#+@@#`@@@@@@@@@@@#+@@#` 
echo                            `:@':#@`  `.   `:@':#@`  `.  
echo ________________________________+@`____________+@`___________________________
echo                                 #@`            #@`   
echo                                 +@`            +@`
echo                                 #@`            #@`
echo                                `@@,            @@,
ping -n 2 localhost >NUL
cls
echo.
echo                             ..                      ..     
echo                         `'@@@@@#:`              `'@@@@@#:`   
echo                        #@@@@@@@@@@@@.,         #@@@@@@@@@@@@.,    
echo                      ;@@@@@@@@@@@@@@@#`      ;@@@@@@@@@@@@@@@#`      
echo                     `+@@@@@@@@@@@@@@@@#:   :@@@@@@@@@@@@@@@@@@#:      
echo                     +##@@@@@@@@@@@@@@@@,   ,@@@@@@@@@@@@@@@@@@@@,
echo                     #@@@@@@@@@@@@@@@@@@;   @@@@@@@@@@@@@@@@@@@@@;    
echo                    `@@@@@@@@@@@@@@@@@@@@#` @@@@@@@@@@@@@@@@@@@@@#`  
echo                    `@@@@@@@@@@@@@@@@@@@@;  :@@@@@@@@@@@@@@@@@@@;   
echo                     '@@@@@@@@@@@@#+@@#`    ^@@@@@@@@@@@@@@#+@@#` 
echo ________________________`:@':#@`__`.____________`:@':#@`__`._________________  
echo                              +@`                    +@`
echo                              #@`                    #@`   
echo                              +@`                    +@`
echo                              #@`                    #@`
echo                             `@@,                    @@,
ping -n 2 localhost >NUL
cls
echo.
echo.
echo                          ..                            ..     
echo                      `'@@@@@#:`                    `'@@@@@#:`   
echo                     #@@@@@@@@@@@@.,              #@@@@@@@@@@@@.,    
echo                   ;@@@@@@@@@@@@@@@#`           ;@@@@@@@@@@@@@@@#`      
echo                  `+@@@@@@@@@@@@@@@@#:         :@@@@@@@@@@@@@@@@@@#:      
echo                  +##@@@@@@@@@@@@@@@@,         ,@@@@@@@@@@@@@@@@@@@@,
echo                  #@@@@@@@@@@@@@@@@@@;         @@@@@@@@@@@@@@@@@@@@@;    
echo                 `@@@@@@@@@@@@@@@@@@@@#`       @@@@@@@@@@@@@@@@@@@@@#`  
echo                 `@@@@@@@@@@@@@@@@@@@@;        :@@@@@@@@@@@@@@@@@@@;   
echo ___________________'@@@@@@@@@@@@#+@@#`________¨@@@@@@@@@@@@@@#+@@#`__________ 
echo                      `:@':#@`  `.                  `:@':#@`  `.  
echo                           +@`                          +@`
echo                           #@`                          #@`   
echo                           +@`                          +@`
echo                           #@`                          #@`
echo                          `@@,                          @@,
ping -n 2 localhost >NUL
cls
echo.
echo.
echo.
echo                      ..                                    ..     
echo                  `'@@@@@#:`                            `'@@@@@#:`   
echo                 #@@@@@@@@@@@@.,                       #@@@@@@@@@@@@., 
echo               ;@@@@@@@@@@@@@@@#`                    ;@@@@@@@@@@@@@@@#` 
echo              `+@@@@@@@@@@@@@@@@#:                 :@@@@@@@@@@@@@@@@@@#:
echo              +##@@@@@@@@@@@@@@@@,                 ,@@@@@@@@@@@@@@@@@@@@,
echo              #@@@@@@@@@@@@@@@@@@;                 @@@@@@@@@@@@@@@@@@@@@;
echo             `@@@@@@@@@@@@@@@@@@@@#`               @@@@@@@@@@@@@@@@@@@@@#`  
echo ____________`@@@@@@@@@@@@@@@@@@@@;________________:@@@@@@@@@@@@@@@@@@@;_____
echo              '@@@@@@@@@@@@#+@@#`                  ¨@@@@@@@@@@@@@@#+@@#` 
echo                  `:@':#@`  `.                          `:@':#@`  `.  
echo                       +@`                                  +@`
echo                       #@`                                  #@`   
echo                       +@`                                  +@`
echo                       #@`                                  #@`
echo                      `@@,                                  @@,
ping -n 2 localhost >NUL
cls
echo.
echo.
echo.
echo.
echo                 ..                                              ..     
echo             `'@@@@@#:`                                      `'@@@@@#:`   
echo            #@@@@@@@@@@@@.,                                 #@@@@@@@@@@@@.,
echo          ;@@@@@@@@@@@@@@@#`                              ;@@@@@@@@@@@@@@@#` 
echo         `+@@@@@@@@@@@@@@@@#:                           :@@@@@@@@@@@@@@@@@@#:  
echo         +##@@@@@@@@@@@@@@@@,                           ,@@@@@@@@@@@@@@@@@@@@,
echo         #@@@@@@@@@@@@@@@@@@;                           @@@@@@@@@@@@@@@@@@@@@;
echo _______`@@@@@@@@@@@@@@@@@@@@#`_________________________@@@@@@@@@@@@@@@@@@@@@#`_
echo        `@@@@@@@@@@@@@@@@@@@@;                          :@@@@@@@@@@@@@@@@@@@;   
echo         '@@@@@@@@@@@@#+@@#`                            ¨@@@@@@@@@@@@@@#+@@#` 
echo             `:@':#@`  `.                                    `:@':#@`  `.  
echo                  +@`                                            +@`
echo                  #@`                                            #@`   
echo                  +@`                                            +@`
echo                  #@`                                            #@`
echo                 `@@,                                            @@,
ping -n 2 localhost >NUL
goto LICHTUNG
