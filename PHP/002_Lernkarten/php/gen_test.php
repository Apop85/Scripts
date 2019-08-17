<?php
    function check_post_method() {
        // Prüfe Post-Status
        if ($_SERVER["REQUEST_METHOD"] == "POST") {
            if ($_POST["submit_value"] == "0") {
                return "NONE";
            }
            return "POST";
        } elseif ($_SERVER["REQUEST_METHOD"] == "SUBMIT") {
            return "SUBMIT";
        } else {
            return "NONE";
        }
    }

    function collect_classes($folder, &$faecher, $depth=0) {
        // Sammle Ordnernamen im root-pfad
        $depth++;
        $dir_content = glob("$folder/*");
        foreach ($dir_content as $file) {
            if (is_dir($file) && $file != "." && $file != "..") {
                $faecher[] .= $file;
                collect_classes($file, $faecher, $depth);
            }
        }
    }

    function collect_files($folder, &$files) {
        // Rekursive Suche nach Dateien
        $dir_content = glob("$folder/*");
        foreach ($dir_content as $file) {
            if (is_dir($file) && $file != "." && $file != "..") {
                collect_files($file, $files);
            } elseif (is_file($file)) {
                $files[] .= $file;
            }
        }
    }

    function gen_content($classes) {
        // Menü für die Fächerauswahl generieren
        $faecher = '<select name="fach[]" multiple>';
        foreach ($classes as $class) {
            $faecher .= '<option value="'.$class.'">'.str_replace("./cards/", "", $class).'</option>';
        }
        $faecher .= '<option value="ALLE">ALLE</option></select>';

        return $faecher;
    }

    function get_max_questions($filename, $counter=0, $depth=0) {
        // Maximal mögliche Anzahl Fragen ermitteln
        $depth++;
        $dir_content = glob("$filename/*");
        foreach ($dir_content as $file) {
            if (is_file($file)) {
                $counter++;
            }
            elseif (is_dir($file) && $file != "." && $file != ".."){
                $counter = get_max_questions($file, $counter, $depth);
            }
        }
        if ($depth == 1) {
            $output = "<select name='max_question'>";
            $output .= create_num_dd($counter);
            $output .= '</select>';

            $counter = $output;
        }

        return $counter;
    }

    function create_num_dd($amount) {
        // Numerisches Dropdown-Menü erstellen
        $start_value = $amount;
        $output = "";
        for ($i=1; $i <= $amount; $i++) {
            $output .= '<option value="'.$start_value.'">'.$start_value.'</option>';
            $start_value--;
        }

        return $output;
    }

    function get_page_content($post_method) {
        // Generiere Seite anhand des post_value
        $output =   '<form method="post" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'">
                    <div class="pruefungsformular">';

        if ($post_method == "NONE") {
            $classes = [];
            $faecher = "";
            
            collect_classes("./cards", $classes, $faecher);
            
            $faecher = gen_content($classes);
            
            $output .= '<p class="exam_title">Fächer auswählen</p>
                        <div class="exam_option">'.$faecher.'</div>
                        <button name="submit_value" class="exam_button" value="1" method="post">OK</button>';
            
        } elseif ($post_method == "POST") {
            $submit_value = $_POST["submit_value"];
            if ($submit_value == "1") {
                // EIngangsseite generieren (Fachauswahl)
                
                if (!key_exists("faecher_value", $_POST)) {
                    $fach = $_POST["fach"];
                    $faecher = "";
                    foreach ($fach as $f) {
                        $sub_dir = preg_split("/\//", $f);
                        unset($sub_dir[sizeof($sub_dir)-1]);
                        $sub_dir = join("/", $sub_dir);
                        if (!in_array($sub_dir, $fach) || $sub_dir == "./cards") {
                            $faecher .= $f."?";
                        }
                    }
                } else {
                    $faecher = $_POST["faecher_value"];
                }

                $dd_max_points = '<select name="max_score">';
                $dd_max_points .= create_num_dd(10);
                $dd_max_points .= '<option value="ALLE">ALLE</option>';
                $dd_max_points .= '</select>';
                
                $output .=  '<p class="exam_title">Maximal erreichte Punktezahl der Fragen</p>
                            <div class="exam_option"><p>Maximale Punktezahl der Prüfungsfrage: 
                            '.$dd_max_points.'</p></div>
                            <button name="submit_value" class="exam_button" value="0" method="post">Zurück</button>
                            <button name="submit_value" class="exam_button" value="2" method="post">OK</button>
                            <input type="hidden" value="'.$faecher.'", name="faecher">';
                
            } elseif ($submit_value == "2") {
                // Seite zur Wahl der Anzahl Prüfungsfragen generieren

                $faecher = $_POST["faecher"];
                $max_score = $_POST["max_score"];

                if ($max_score == "ALLE") {
                    $max_score = 999999;
                }

                $directories = preg_split("/\?/", $faecher);
                if (!in_array("ALLE", $directories)) {
                    unset($directories[sizeof($directories)-1]);
                    $exam_files = [];
                    foreach ($directories as $dir) {
                        collect_files($dir, $exam_files);
                    }

                    $max_score = intval($max_score);
                    $final_files = [];
                    foreach ($exam_files as $qfile) {
                        $score = intval(get_file_score($qfile));
                        if ($score <= $max_score) {
                            $final_files[] .= $qfile;
                        }
                    }
                    // echo $faecher;
                } else {
                    $final_files = [];
                    $exam_files = [];
                    collect_files("./cards/", $exam_files);
                    foreach ($exam_files as $qfile) {
                        $score = intval(get_file_score($qfile));
                        if ($score <= $max_score) {
                            $final_files[] .= $qfile;
                        }
                    }

                }


                $exam_files = join("?", $final_files);
                
                $dd_max_amount = create_num_dd(sizeof($final_files));
                $output .= '<p class="exam_title">Anzahl Prüfungsfragen</p>
                            <div class="exam_option"><p>Anzahl Fragen: <select name="max_question">'
                            .$dd_max_amount.'</select></p></div>
                            <button name="submit_value" class="exam_button" value="1" method="post">Zurück</button>
                            <button name="submit_value" class="exam_button" value="3" method="post">OK</button>
                            <input type="hidden" name="exam_files" value="'.$exam_files.'">
                            <input type="hidden" name="faecher_value" value="'.$faecher.'">
                            <input type="hidden" name="max_score" value="'.$max_score.'">';

            } elseif ($submit_value == "3") {
                // Erstelle Prüfungsbogen
                $exam_files = $_POST["exam_files"];
                $max_score = $_POST["max_score"];
                $max_amount = $_POST["max_question"];

                $files = preg_split("/\?/", $exam_files);
                $question_file_array = [];

                while (sizeof($question_file_array) < $max_amount || sizeof($question_file_array) == 1) {
                    // Rearrangiere die Fragen nach Zufallsprinzip
                    $random = rand(0, sizeof($files)-1);
                    if (!in_array($files[$random], $question_file_array) && $files[$random] != "") {
                        $question_file_array[] .= $files[$random];
                    }
                    if (sizeof($files) == sizeof($question_file_array)) {
                        break;
                    }    
                }

                $pruefungsbogen = "";
                
                foreach ($question_file_array as $file) {
                    // Generiere Prüfungsbogen
                    $pruefungsbogen .= create_exam_question($file);
                }
                
                $output .=  '<div class="exam"><p class="exam_title">Prüfung</p><div class="exam_form">'.$pruefungsbogen.'</div>
                            <input type="hidden" name="file_array" value='.join("?", $question_file_array).'>
                            <button name="submit_value" class="pruefung_button" value="check" method="post">Auswerten</button>
                            </div>';

            } elseif ($submit_value == "check") {
                // Generiere die Prüfungsergebnissseite
                $question_file_array = $_POST["file_array"];
                $question_file_array = preg_split("/\?/", $question_file_array);
                
                $total_count = 0;
                foreach ($question_file_array as $file) {
                    $file_id = basename($file);
                    $file_id = str_replace(".php", "", $file_id);

                    $answer = $_POST[basename($file_id)];

                    $total_count = $total_count + get_score($answer, $file);
                }

                $percent_right = round((100/sizeof($question_file_array)*$total_count), 2);
                $total_false = sizeof($question_file_array)-$total_count;
                if ($percent_right != 0) {
                    $note = round(1+(5/(100/$percent_right)), 2);
                } else {
                    $note = 1;
                }

                $total_questions = sizeof($question_file_array);

                $output .=  '<div class="exam_title">Prüfungsergebnisse</div>
                            <div class="exam_result"><p>Dein Resultat:</p>
                            <table class="exam_resuts">
                            <tr><td class="exam_explain">Richtige Antworten</td><td class="exam_score">'.$total_count.'</td></tr>
                            <tr><td class="exam_explain">Falsche Antworten</td><td class="exam_score">'.$total_false.'</td></tr>
                            <tr><td class="exam_explain mid_result">Total</td><td class="exam_score mid_result">'.$total_questions.'</td></tr>
                            <tr><td class="exam_explain">Total richtig</td><td class="exam_score">'.$percent_right.'%</td></tr>
                            <tr><td class="exam_explain end_result">Note:</td><td class="exam_score end_result">'.$note.'</td></tr>
                            </table></div><button class="end_of_exam" name="submit_value" value="0" method="post">OK</button>';
            }
        } 

        $output .= '</div></form>';
        return $output;
    }

    function create_exam_question($file) {
        // Fragen auslesen und Ausgangs-Datei hinterlegen 
        include($file);
        ##### EVT UNNÖTIG
        $file_id = basename($file);
        $file_id = str_replace(".php", "", $file_id);
        $output = '<div class="exam_form_content"><div class="exam_question">'.$q.'</div><div class="exam_input"><input class="exam_answer" type="text" name="'.$file_id.'"></div></div>';
        return $output;
    }

    function get_score($answer, $file) {
        // Auswerten der Antwort
        include($file);
        if (strtolower($answer) == strtolower($a)) {
            $command = escapeshellcmd('python ./py/update_score.py  "update" "1" "'.$file.'"');
            shell_exec($command);
            return 1;
        } else {
            $command = escapeshellcmd('python ./py/update_score.py  "update" "-1" "'.$file.'"');
            shell_exec($command);
            return 0;
        }
    }

    function get_file_score($file) {
        include($file);
        return $s;
    }
?>