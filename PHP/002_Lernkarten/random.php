<?php
    function get_random_card() {
        # Zufällige Karte auswählen
        $emptyarray = [];
        $cards = get_cards('./cards/', $emptyarray);
        $random = mt_rand(0, sizeof($cards)-1);
        include($cards[$random]);
        return array($q,$a,$f,$cards[$random]);
    }
    
    function get_cards($main, $cards){
        # Alle Karten aller Fächer Sammeln
        global $cards;
        $dirHandle = opendir($main);
        while($file = readdir($dirHandle)){
            if(is_dir($main . $file) && $file != '.' && $file != '..'){
                $newdir = $main.$file.'/';
                get_cards($newdir, $cards);
            }
            else { 
                if ($file != '.' && $file != '..'){
                    $cards[] = $main.$file;
                }
                
            }
        }
        return $cards;
    }
    get_random_card();
    
    function check_answer($card) {
        # Prüfe Antwort
        if ($_SERVER["REQUEST_METHOD"] == "POST") {
            $antwort = strtolower(test_input($_POST["answer"]));
            $test = strtolower(test_input($_POST["true_answer"]));
            $last_file = test_input($_POST["last_file"]);
            
            if ($antwort == $test){
                $antwortfeld = '<div class="true_value">Diese Antwort war richtig!!</div>';
                $command = escapeshellcmd('python ./py/update_score.py  "update" "1" "'.$last_file.'"');
                $output = shell_exec($command);
                return $antwortfeld;
            }
            else {
                $antwortfeld = '<div class="false_value">Diese Antwort war falsch!! Die korrekte Antwort lautet:</br><div class="correct">'.$test.'</div>Siehe '.$card[2].'</div>';
                $command = escapeshellcmd('python ./py/update_score.py  "update" "-1" "'.$last_file.'"');
                $output = shell_exec($command);
                return $antwortfeld;
            }
        }
    }
    
    function test_input($data) {
        $data = trim($data);
        $data = stripslashes($data);
        $data = htmlspecialchars($data);
        return $data;
    }
    ?>

<?php
    $init_form = '<form method="post" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'">';
    $titel = 'Zufallsfrage';
    $card = get_random_card();
    $won = check_answer($card);
    $header = '<p class="content_title">Eine beliebige Zufallsfrage</p><p class="content_text">Eine Zufällige Frage aus der Kartei wird ausgewählt und ausgegeben.</p>';
    $validation = '<div class="flip-card-front">'.$won.'</div>';
    $button = "<p><input class='antwort' type='text' name='answer' autofocus><button class='submit_button' method='post'>GO</button></p></div>";
    $footer = '</div></div>';
    $true_answer = '<input type="hidden" name="true_answer" value="'.$card[1].'"><input type="hidden" name="last_file" value="'.$card[3].'">';
    if ($_SERVER["REQUEST_METHOD"] == "POST") {
        $divs = '<div class="flip-card"><div class="flip-card-inner">';
        $frage = '<div class="flip-card-back"><p class="frage">'.$card[0].'</p>';
        $content = $header.$divs.$frage.$init_form.$button.$validation.$footer.$true_answer.'</form>';
    }
    else {
        $divs = '<div class="flip-card"><div class="flip-card-static">';
        $frage = '<div class="flip-card-b-static"><p class="frage">'.$card[0].'</p>';
        $content = $header.$divs.$frage.$init_form.$button.$footer.$true_answer.'</form>';
    }
?>

<?php include("php/main.php"); ?>


