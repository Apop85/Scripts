
<?php
    function get_all_cards($filename, &$output, &$binary, &$sort_list, $depth=0) {
        if ($sort_list == NULL && $depth == 0) {
            $sort_list = array();
        }
        $depth++;
        if (is_dir($filename) && $filename != "." && $filename != "..") {
            $dir_content = glob("$filename/*");
            foreach ($dir_content as $f) {
                get_all_cards($f, $output, $binary, $sort_list, $depth);
            }
        }
        else {
            if (is_file($filename)){
                if ($binary == 1) {
                    $binary = 0;
                }
                else {
                    $binary = 1;
                }
                $fach = preg_split("/\//", $filename);
                $question = get_answer($filename);

                $sort = "";
                    if ($_SERVER["REQUEST_METHOD"] == "GET") {
                        $sort = $_GET["sort"];

                        // $sort = preg_split("/\-/", $sort);

                        // $current = $sort[1];
                        // $sort = $sort[0];
                        // echo $sort;
                    }
                    
                    $pfad = dirname($filename);
                    $dateiname = basename($filename);
                    if ($sort == "") {
                        $output[] .= create_row($binary, $fach[2], $question[0], $question[1], $pfad, $dateiname);
                        $key = "";
                    }
                    elseif ($sort == "sort_class") {
                        $key = strtolower($fach[2]);
                    }
                    elseif ($sort == "sort_question") {
                        $key = strtolower($question[0]);
                    }
                    elseif ($sort == "sort_points") {
                        $key = strval($question[1]);
                    }
                    elseif ($sort == "sort_path") {
                        $key = strtolower(dirname($filename));
                    }
                    elseif ($sort == "sort_fname") {
                        $key = strtolower(basename($filename));
                    }

                    if ($key != ""){
                        $key = $key." ";
                        if (!isset($sort_list[$key])){
                            $sort_list[$key] = [];
                        }
                        $sort_list[$key][sizeof($sort_list[$key])] = array($fach[2], $question[0], strval($question[1]), $pfad, $dateiname);
                    }
                    
                }
            }
            if ($depth == 1 && sizeof($sort_list) != 0) {
                $output = create_sorted_table($sort_list);
            }
        }

    function create_row($binary, $course, $question, $score, $dir, $file) {
        $file_path = $dir.$file;
        $output = "<tr class='file_list_".$binary." table_item'>
                        <td>".$course."</td>
                        <td>".$question."</td>
                        <td class='score'>".intval($score)."</td>
                        <td>".$dir."</td>
                        <td>".$file."</td>
                        <td class='icon_container'>
                        <button type='image' name='del' class='icon delete' title='Frage Löschen' value='".$file_path."' method='post'></button>
                        <button type='image' name='edit_file' class='icon edit' title='Frage Editieren' value='".$file_path."' method='post'></button>
                    </td></tr>";

        return $output;
    }

    function create_sorted_table($liste){
        $key_list = array_keys($liste);
        if (is_numeric($key_list[0])) {
            ksort($key_list);
        }
        else {
            sort($key_list);
        }
        
        $binary = 1;
        $table = [];

        foreach ($key_list as $key) {
            foreach ($liste[$key] as $values) {
                if ($binary == 1) {
                    $binary = 0;
                }
                else {
                    $binary = 1;
                }
                $fach = $values[0];
                $frage = $values[1];
                $punkte = $values[2];
                $pfad = $values[3];
                $datei = $values[4];

                $new_row = create_row($binary, $fach, $frage, $punkte, $pfad, $datei);
                $table[] .= $new_row;
            }
        }

        return $table;
    }

    function get_answer($file){
        include($file);
        $output = array($q, $s);

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
                $output .= '<div class="dateipfad">'.$answer[0].'</div>';
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