<?php
    function read_stats() {
        // Lese allgemeine Statistiken aus
        include("stats.php");
        return array($r, $f);
    }

    function get_files($ROOT, &$files) {
        // Rekursive Suche nach Files
        $dir_handler = opendir($ROOT);
        while($file = readdir($dir_handler)){
            if (is_dir($ROOT . $file . '/') && $file != "." && $file != "..") {
                $new_dir = $ROOT.$file.'/';
                get_files($new_dir, $files);
            } elseif (is_file($ROOT.$file)) {
                $files[] .= $ROOT.$file;
            }
        }
    }

    function get_class_name($file) {
        // Fach aus Dateipfad extrahieren
        $class = str_replace("./cards/", "", $file);
        $class = preg_split("/\//", $class);
        $class = $class[0];
        return $class;
    }

    function analyze_data($ROOT ="./cards/") {
        // Sammle Statistiken von allen auffindbaren Karteikarten
        $files = [];
        get_files($ROOT, $files);
        $file_amount = sizeof($files);
        $total_score = 0;
        $classes = [];
        foreach ($files as $file) {
            $file_data = read_data($file);
            $score_delta = $file_data[0];
            $file_class = get_class_name($file);
            $right_answers = $file_data[1];
            $false_answers = $file_data[2];
            if (!key_exists($file_class, $classes)) {
                $classes[$file_class] = [];
                $classes[$file_class]["ra"] = 0;
                $classes[$file_class]["fa"] = 0;
            } 

            // Erstelle Array sortiert nach Fächern sortiert
            $classes[$file_class]["ra"] = $classes[$file_class]["ra"] + $right_answers;
            $classes[$file_class]["fa"] = $classes[$file_class]["fa"] + $false_answers;


            $total_score = $total_score + $score_delta;
        }

        // Berechne durchschnittliche Punktezahl
        $avg = round(($total_score/$file_amount), 1, PHP_ROUND_HALF_UP);

        return array($file_amount, $total_score, $avg, $classes);
    }

    function read_data($file) {
        // Lese Statistik aus Karteikarte aus
        include($file);
        return array($s, $ra, $fa);
    }

    function create_table($table_array) {
        // Erstelle Balkendiagramme
        $keys = array_keys($table_array);
        $class_amount = sizeof($keys);
        $column_width = round(100/$class_amount, 2);
        $output = "";
        foreach ($keys as $key) {
            // Lese Statistiken der einzelnen Fächer aus
            $right_answers = $table_array[$key]["ra"];
            $false_answers = $table_array[$key]["fa"];
            $total_score = $right_answers + $false_answers;

            if ($right_answers != 0) {
                $percent_true = round($right_answers*(80/$total_score), 0, PHP_ROUND_HALF_UP);
                $percent_true_abs = round($right_answers*(100/$total_score), 0, PHP_ROUND_HALF_UP);
            } else {
                $percent_true = 0;
                $percent_true_abs = 0;
            }

            if ($false_answers != 0) {
                $percent_false = round($false_answers*(80/$total_score), 0, PHP_ROUND_HALF_UP);
                $percent_false_abs = round($false_answers*(100/$total_score), 0, PHP_ROUND_HALF_UP);
            } else {
                $percent_false = 0;
                $percent_false_abs = 0;
            }


            $output .= '<div class="class_stats" style="min-width: '.$column_width.'%;">
                            <div class="stats_true progress-bar" data="'.$right_answers.'" style="height: '.$percent_true.'%;">
                                <p class="stats_prgbar_percent">'.$percent_true_abs.'%</p>
                            </div>
                            <div class="stats_false progress-bar" data="'.$false_answers.'" style="height: '.$percent_false.'%;">
                                <p class="stats_prgbar_percent">'.$percent_false_abs.'%</p>
                            </div>
                        </div>';
        }
       
        return $output;
    }

    function get_bottom_table($table_array) {
        // Erstelle Beschreibung unterhalb der Balkendiagramme
        $keys = array_keys($table_array);
        $class_amount = sizeof($keys);
        $column_width = round(100/$class_amount, 2);
        $output = "";
        foreach ($keys as $class) {
            $output .= '<div class="stats_class_title" style="min-width: '.$column_width.'%;">'.$class.'</div>';
        }
        return $output;
    }

    function get_content() {
        // Erstelle Inhalt der Seite
        $stats_content = read_stats();
        $question_total = $stats_content[0] + $stats_content[1];

        // Verhindere Division by Zero Fehler 
        if ($question_total != 0) {
            $percent_true = round($stats_content[0]*(90/$question_total), 0, PHP_ROUND_HALF_UP);
            $percent_true_abs = round($stats_content[0]*(100/$question_total), 0, PHP_ROUND_HALF_UP);
        } else {
            $percent_true = 0;
            $percent_true_abs = 0;
        }
        
        if ($question_total != 0) {
            $percent_false = round($stats_content[1]*(90/$question_total), 0, PHP_ROUND_HALF_UP);
            $percent_false_abs = round($stats_content[1]*(100/$question_total), 0, PHP_ROUND_HALF_UP);
        } else {
            $percent_false = 0;
            $percent_false_abs = 0;
        }

        // Sammle alle verfügbaren Statistiken
        $file_data = analyze_data();

        $file_amount = $file_data[0];
        $total_score = $file_data[1];
        $avg = $file_data[2];
        $classes = $file_data[3];

        if (sizeof($classes) <= 4){
            // Erstelle einzelne Reihe für Balkendiagramme
            $table_content = create_table($classes);
            $bottom_content = get_bottom_table($classes);
            $table_content .= '</div><div class="stats_info_box">'.$bottom_content.'</div>';
        } else {
            // Erstelle mehrere Reihen für die Balkendiagramme 
            $classes_keys = array_keys($classes);
            $chunked_table_content = array_chunk($classes_keys, 4);
            $table_content = "";

            $new_array = [];
            $counter = 0;
            foreach ($chunked_table_content as $chunk) {
                foreach ($chunk as $class) {
                    $new_array[$counter][$class] = $classes[$class];
                }
                $counter++;
            }
            foreach ($new_array as $key) {
                // Erstelle Beschreibung unterhalb der Balkendiagramme
                $table_content .= create_table($key);
                $bottom_content = get_bottom_table($key);
                if ($key != $new_array[sizeof($new_array)-1]) {
                    $table_content .= '</div><div class="stats_info_box">'.$bottom_content.'</div><div class="stats_inner_part">';
                } else {
                    $table_content .= '</div><div class="stats_info_box">'.$bottom_content.'</div>';
                }
            }
        }

        $output =  '<div class="stats_outer">
                        <div class="stats_outer_part"><p class="stats_part_title">Auswertung Antworten</p>
                            <div class="stats_inner_part">
                                <div class="stats_true progress-bar" data="'.$stats_content[0].'" style="height: '.$percent_true.'%;">
                                <p class="stats_prgbar_percent">'.$percent_true_abs.'%</p>
                                </div>
                                <div class="stats_false progress-bar" data="'.$stats_content[1].'" style="height: '.$percent_false.'%;">
                                <p class="stats_prgbar_percent">'.$percent_false_abs.'%</p>
                                </div>
                                <div class="stats_legend_box">
                                <div class="stats_legend">
                                    <p class="stat_legend_title">Legende</p>
                                    <p class="stat_legend_info"><img src="./img/box.png" class="stats_box stats_true">Richtig</p>
                                    <p class="stat_legend_info"><img src="./img/box.png" class="stats_box stats_false"> Falsch</p>
                                    <p class="stat_legend_info"><img src="./img/box.png" class="stats_box stats_hidden"> Total:'.$question_total.'</p>
                                </div>
                                </div>
                            </div>
                        </div>
                        <div class="stats_outer_part"><p class="stats_part_title">Auswertung Punktzahlen</p>
                            <div class="stats_inner_part" style="display: block;">
                            <table class="score_table">
                                <tr class="hidden_border">
                                    <td class="stat_score">Anzahl Fragen:</td>
                                    <td class="stat_score_int">'.$file_amount.'</td>
                                </tr>
                                <tr class="hidden_border">
                                    <td class="stat_score">Punkte Total:</td>
                                    <td class="stat_score_int">'.$total_score.'</td>
                                </tr>
                                <tr class="hidden_border">
                                    <td class="stat_score">Durchschnitt:</td>
                                    <td class="stat_score_int">'.$avg.'</td>
                                </tr>
                            </table>
                            </div>
                        </div>
                    </div>
                    
                    <div class="stats_outer">
                        <div class="stats_outer_part" style="width: 100%;"><p class="stats_part_title">Leistungen nach Fächern</p>
                            <div class="stats_inner_part">
                               '.$table_content.'
                            
                        </div>
                    </div>';
        
        return $output;
    }
?>