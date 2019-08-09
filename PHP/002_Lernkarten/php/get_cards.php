
<?php
    function get_all_cards($main, $cards, $counter){
        global $cards, $counter;
        $dirHandle = opendir($main);
        while($file = readdir($dirHandle)){
            if(is_dir($main . $file) && $file != '.' && $file != '..'){
                $newdir = $main.$file.'/';
                $cards = get_all_cards($newdir, $cards, $counter);
            }
            else { 
                if ($file != '.' && $file != '..'){
                    if ($counter == 1) {
                        $counter = 0;
                    }
                    else {
                        $counter = 1;
                    }
                    $fach = preg_split("/\//", $main);
                    $question = get_answer($main.$file);

                    $cards[] .= "<tr class='file_list_".$counter."'><th>".$fach[2]."</th>
                                <td>".$question[0]."</td><td class='score'>".$question[1]."</td><td>".$main.'</td><td>'.$file."</td><td class='icon_container'>
                                <button type='image' name='del' class='icon delete' title='Frage Löschen' value='".$main.$file."' method='post'></button>
                                <button type='image' name='edit_file' class='icon edit' title='Frage Editieren' value='".$main.$file."' method='post'></button>
                                </td></tr>";
                }
            
            }
        }
        return $cards;
    }

    function get_answer($file){
        include($file);
        // echo $s;
        $output = array(0 => $q, 1=> $s);

        return $output;
    }

    function get_content($file) {
        include($file);
        return array($q, $a);
    }

    function check_post() {
        if ($_SERVER["REQUEST_METHOD"] == "POST") {
            $file = $_POST["del"];
            $edit_file = $_POST["edit_file"];
            $last_file = $_POST["last_file"];
            $verify = $_POST["verify"];
            $edited_file = $_POST["edited_file"];
            $save_statement = $_POST["save"];
            $edited_question = $_POST["question"];
            $edited_answer = $_POST["answer"];
            $answer = get_answer($file);
            if ($file != "" && $edit_file == "") {
                $output = '<div class="warning"><p>Achtung! Folgendes File wird gelöscht:</p><div class="dateipfad">'.$file.'</div>';
                $output .= '<form method="post" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'">';
                $output .= '<input type="hidden" name="last_file" value="'.$file.'">';
                $output .= '<div class="dateipfad">'.$answer.'</div>';
                $output .= '<div class="warning_menu"><button name="verify" value="0" method="post">Abbrechen</button><button name="verify" value="1" method="post">OK</button></div></form></div>';
                return $output;
            }
            elseif ($verify == 1 && $last_file != "") {
                $command = escapeshellcmd('python ./py/delete.py  "del" "'.$last_file.'"');
                $output = shell_exec($command);
                return $output;
            }
            elseif ($edit_file != "" && $file == ""){
                $file_content = get_content($edit_file);
                $output = '<div class="editor"><p class="edit_title">Folgendes File wird bearbeitet:</p><div class="dateipfad">'.$edit_file.'</div>';
                $output .= '<form method="post" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'">';
                $output .= '<input type="hidden" name="edited_file" value="'.$edit_file.'">';
                $output .= '<div class="edit_form"><p><input type="text" name="question" value="'.$file_content[0].'"></p>';
                $output .= '<p><input type="text" name="answer" value="'.$file_content[1].'"></p>';
                $output .= '<div class="warning_menu"><button name="save" value="1" method="post">OK</button><button name="save" value="0" method="post">Abbrechen</button></div></form>';
                $output .= '</div></div>';
                return $output;
            }
            elseif ($save_statement == 1) {
                if ($edited_answer == "" || $edited_question == "") {
                    $output = '<div class="output_message info">Frage- oder Antwortfeld darf nicht leer sein</div>';
                }
                elseif ($edited_file == "") {
                    $output = '<div class="output_message error">Pfadübergabe fehlgeschlagen. Datei wurde nicht editiert.</div>';
                }
                else {
                    $command = escapeshellcmd('python ./py/edit.py  "edit" "'.$edited_file.'" "'.$edited_question.'" "'.$edited_answer.'"');
                    $output = shell_exec($command);
                }

                return $output;
            }
        }
        return NULL;
    }

?>