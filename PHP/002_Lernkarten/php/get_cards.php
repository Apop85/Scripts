
<?php
    function get_all_cards($filename, &$output, &$binary, &$sort_list, $arrangement, $depth=0) {
        # Rekursive suche nach Frage-Files
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
                    if ($_SERVER["REQUEST_METHOD"] == "GET" && key_exists("sort", $_GET)) {
                        $sort = $_GET["sort"];
                        if (key_exists("last_sort", $_GET) && key_exists("arrangement", $_GET)) {
                            $last_sort = get_last_sort();
                            $arrangement = $last_sort[1];
                            $last_sort = $last_sort[0];
                            // echo $arrangement;
                        }
                    }
                    
                    $pfad = dirname($filename);
                    $dateiname = basename($filename);
                    # Erstelle Array aus allen Daten
                    if ($sort == "") {
                        $output[] .= create_row($binary, $fach[2], $question[0], $question[1], $pfad, $dateiname);
                        $key = "";
                    }
                    elseif ($sort == "sort_class") {
                        // Fächer als Keys
                        $key = strtolower($fach[2]);
                    }
                    elseif ($sort == "sort_question") {
                        // Fragen als Keys
                        $key = strtolower($question[0]);
                    }
                    elseif ($sort == "sort_points") {
                        // Punktezahl als Keys
                        $key = strval($question[1]);
                    }
                    elseif ($sort == "sort_path") {
                        // Dateipfad als Keys
                        $key = strtolower(dirname($filename));
                    }
                    elseif ($sort == "sort_fname") {
                        // Dateiname als Keys
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
                $output = create_sorted_table($sort_list, $arrangement);
            }
        }
    
    function get_sort_order($value) {
        if ($value == 1){
            return 0;
        } else {
            return 1;
        }
    }

    function get_last_sort() {
        if (key_exists("last_sort", $_GET) && key_exists("sort", $_GET) && key_exists("arrangement", $_GET)) {
            $last_sort = $_GET["last_sort"];
            $sort = $_GET["sort"];
            $arrangement = $_GET["arrangement"];
            // Prüfe ob letzte Sortierung auf- oder absteigend war
            if ($last_sort == $sort) {
                $arrangement = get_sort_order($arrangement);
            } else {
                $arrangement = 0;
            }
            
            if ($last_sort == "NONE") {
                return array($_GET["sort"], 0);
            } else {
                return array($_GET["sort"], $arrangement);
            }

        } else {
            return array("NONE", 0);
        }
    }

    function create_row($binary, $course, $question, $score, $dir, $file) {
        // Erstelle Reihe der Tabelle
        $file_path = $dir.'/'.$file;
        $output = "<tr class='file_list_".$binary." table_item'>
                        <td>".$course."</td>
                        <td>".$question."</td>
                        <td class='score'>".intval($score)."</td>
                        <td>".$dir."</td>
                        <td>".$file."</td>
                        <td class='icon_container'><form class='list_form' method='post' action='".htmlspecialchars($_SERVER['PHP_SELF'])."'>
                        <button type='image' name='button_value' class='icon delete' title='Frage Löschen' value='delete_file' method='post'></button>
                        <button type='image' name='button_value' class='icon edit' title='Frage Editieren' value='edit_file' method='post'></button>
                        <input type='hidden' name='file_path' value='".$file_path."'>
                        </form>
                    </td></tr>";

        return $output;
    }

    function create_sorted_table($liste, $order){
        // Sortiere das Array nach den gegebenen Keys
        $key_list = array_keys($liste);
        $first_key = $key_list[0][0];
        if ($order == 0) {
            // Sortiere aufsteigend
            if (is_numeric($first_key)) {
                natsort($key_list);
            }
            else {
                sort($key_list);
            }
        } else {
            // Sortiere absteigend
            if (is_numeric($first_key)) {
                natsort($key_list);
                $key_list = array_reverse($key_list);
            }
            else {
                rsort($key_list);
            }
        }
        
        $binary = 1;
        $table = [];

        // Erstelle Tabelleninhalte
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
        // Lese Frage und Punktzahl aus Frage-File aus
        include($file);
        $output = array($q, $s);

        return $output;
    }

    function get_content($file) {
        // Lese Frage und Antwort aus Frage-File aus
        include($file);
        return array($q, $a);
    }

    function check_post() {
        // Prüfe ob Method = Post
        if ($_SERVER["REQUEST_METHOD"] == "POST") {
            $post_value = $_POST["button_value"];
            if ($post_value == "0") {
                return NULL;
            }
            if ($post_value == "delete_file" || $post_value == "edit_file") {
                $file = $_POST["file_path"];
                $answer = get_answer($file);
            } elseif ($post_value == "del" || $post_value == "save") {
                $last_file = $_POST["last_file"];
            }

            // Erstelle Popup
            if ($post_value == "delete_file") {
                // File-Löschen Popup
                $output =   '<div class="warning">
                                <p>Achtung! Folgendes File wird gelöscht:</p>
                                <div class="dateipfad">'.$file.'</div>
                                <form method="post" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'">
                                    <input type="hidden" name="last_file" value="'.$file.'">
                                    <div class="dateipfad">'.$answer[0].'</div>
                                    <div class="warning_menu">
                                        <button name="button_value" value="del" method="post">OK</button>
                                        <button name="button_value" value="0" method="post">Abbrechen</button>
                                    </div>
                                </form>
                            </div>';
                return $output;
            }
            elseif ($post_value == "del") {
                // Lösche File endgültig
                
                $command = escapeshellcmd('python ./py/delete.py  "del" "'.$last_file.'"');
                $output = shell_exec($command);
                return $output;
            }
            elseif ($post_value == "edit_file"){
                // Editiere eine Frage
                $file_content = get_content($file);
                $output =   '<div class="editor">
                                <p class="edit_title">Folgendes File wird bearbeitet:</p>
                                <div class="dateipfad">'.$file.'</div>
                                    <div class="edit_form">
                                    <form method="post" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'">
                                        <input type="hidden" name="last_file" value="'.$file.'">
                                        <p><input type="text" name="question" value="'.$file_content[0].'"></p>
                                        <p><input type="text" name="answer" value="'.$file_content[1].'"></p>
                                        <div class="warning_menu">
                                            <button name="button_value" value="save" method="post">OK</button>
                                            <button name="button_value" value="0" method="post">Abbrechen</button>
                                        </div>
                                    </form>
                                </div>
                            </div>';
                return $output;
            }
            elseif ($post_value == "save") {
                // Frage Speichern

                $edited_question = $_POST["question"];
                $edited_answer = $_POST["answer"];
                if ($edited_answer == "" || $edited_question == "") {
                    $output = '<div class="output_message info">Frage- oder Antwortfeld darf nicht leer sein</div>';
                }
                elseif ($last_file == "") {
                    $output = '<div class="output_message error">Pfadübergabe fehlgeschlagen. Datei wurde nicht editiert.</div>';
                }
                else {
                    $command = escapeshellcmd('python ./py/edit.py  "edit" "'.$last_file.'" "'.$edited_question.'" "'.$edited_answer.'"');
                    $output = shell_exec($command);
                }

                return $output;
            }
        }
        return NULL;
    }

?>