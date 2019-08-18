
<?php
    function get_classes($main, $class_names=[]){
        # Suche nach allen Ordnern im aktuellen Ordner
        global $class_names;
        $dirHandle = opendir($main);
        $class = substr_replace($main, '', 0, 8);
        while($file = readdir($dirHandle)){
            # Alle filtern die "." ".." oder ".php" enthalten - Durchsuche rekursiv
            if(is_dir($main . $file) && $file != '.' && $file != '..' && substr($file, -4) != '.php' && substr($file, -1) != '/' && $file != ""){
                $newdir = $main . $file;
                get_classes($newdir, $class_names);
            }
            elseif (($file != '.' && $file != '..' && in_array($class, $class_names) != TRUE && $class != "") ||  $class_names == NULL){
                if ($class != "") {
                    $class_names[] = $class;         
                }
            }
        }
        
        return $class_names;
    }


    function create_question($quest_array, $main, $post_value) {
        $div_intro_1 = "<div class='flip-card'>";
        $div_intro_2 = "<div class='flip-card-inner'>";
        $div_outro = '</div></div>';
        $form_0 = '<form method="post" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'">';
        $form_1 = '<p class="frage">'.$quest_array[0]."</p>";
        $form_2 = '<input class="antwort" autocomplete="off" type="text" name="answer" autofocus><button class="submit_button" method="post" name="button_value" value="'.$main.'">GO</button>';
        $form_3 = '<input type="hidden" name="true_answer" value="'.$quest_array[1].'">';
        $form_4 = '<input type="hidden" name="button" value="'.substr($main, 8).'">';
        $form_5 = '<input type="hidden" name="last_file" value="'.$quest_array[2].'">';
        $form_6 = '<input type="hidden" name="q_file" value="'.$quest_array[3].'">';
        $form_7 = '</form>';
        $form = $form_0.$form_1.$form_2.$form_3.$form_4.$form_5.$form_6;
        $output = "";
        if ($post_value == "check") {
            $answer = strtolower(test_input($_POST["answer"]));
            $last_answer = strtolower(test_input($_POST["true_answer"]));
            $last_file = test_input($_POST["last_file"]);
            $q_file = test_input($_POST["q_file"]);
            
            if ($last_answer == $answer && $answer != "") {
                # Richtige Antwort
                $front_1 = "<div class='flip-card-front'>";
                $front_2 = '<div class="true_value"><p>Diese Antwort war richtig</p></div>';
                $front_3 = '</div>';
                $back_1 = '<div class="flip-card-back">';
                $back_2 = $form;
                $back_3 = '</div>';
                $command = escapeshellcmd('python ./py/update_score.py  "update" "1" "'.$q_file.'"');
                $output = shell_exec($command);
            }
            else {
                # Falsche Antwort
                $front_1 = "<div class='flip-card-front'>";
                $front_2 = '<div class="false_value"><p>Diese Antwort war falsch. Die richtige Antwort lautet:</p><div class="correct">'.$last_answer.'</div>Siehe '.$last_file.'</a></div>';
                $front_3 = '</div>';
                $back_1 = '<div class="flip-card-back">';
                $back_2 = $form;
                $back_3 = '</div>';
                $antwortfeld = '<div class="false_value">Diese Antwort war falsch!! Die korrekte Antwort lautet:</br><div class="correct">'.$test.'</div>Siehe '.$card[2].'</div>';
                $command = escapeshellcmd('python ./py/update_score.py  "update" "-1" "'.$q_file.'"');
                $output = shell_exec($command);
            }
            $div_intro = $div_intro_1.$div_intro_2;
            
            $front = $front_1.$front_2.$front_3;
            $back = $back_1.$back_2.$back_3;
            
            return $div_intro.$front.$back.$div_outro.$output;
        } else {
            # Wenn erste Frage
            $div_intro_2 = "<div class='flip-card-static'>";
            $front_1 = '<div class="flip-card-front-static">';
            $front_2 = $form;
            $front_3 = '</div>';
            $back_1 = "";
            $back_2 = "";
            $back_3 = "";
            $output = "";

            $div_intro = $div_intro_1.$div_intro_2;
            
            $front = $front_1.$front_2.$front_3;
            $back = $back_1.$back_2.$back_3;

            return $div_intro.$front.$back.$div_outro.$output;
        }
        
        return "<div class='flip-card'></div>";
    }

    function get_cards($main) {
        $file_array = scandir($main);
        $output = [];
        foreach ($file_array as $file) {
            if (substr($file, -4) == ".php") {
                $output[] = $file;
            }
        }

        $random = mt_rand(0, sizeof($output)-1);
        $random_card = $main.$output[$random];
        include ($random_card);
        return array($q,$a,$f,$main.$output[$random]);
    }

    function create_buttons($folder_array, $post_value) {
        # Erstelle navigationsbuttons
        $init_form = '<form class="form" method="post" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'">';
        $output = $init_form;
        if (is_dir('./cards/'.$post_value)) {
            foreach ($folder_array as $folder) {
                $back = './cards/'.$post_value;
                $back = preg_split("/\//", $back);
                $back[sizeof($back)-1] = "";
                $back = join("/", $back);
                $back = str_replace("./cards/", "", $back);
                $back = substr($back, 0, -1);

                if (substr($folder, -1) == '/' && sizeof($folder_array) > 1) {
                    $output .= "<button class='class_button' method='post' name='button_value' value='".$back."'>Zurück</button>";
                }
                elseif (sizeof($folder_array) > 1) {
                    $output .= "<button class='class_button' method='post' name='button_value' value='".$folder."'>".$folder."</button>";
                }
                else {
                    $output .= "<button class='class_button' method='post' name='button_value' value='".$back."'>Zurück</button>";
                }
            }
        }
        elseif ($post_value == "start") {
            foreach ($folder_array as $folder) {
                $output .= "<button class='class_button' method='post' name='button_value' value='".$folder."'>".$folder."</button>";
            }
        }
        $output .= '</form>';
        return $output;
    }

    function get_post() {
        if ($_SERVER["REQUEST_METHOD"] == "POST") {
            $btn_value = $_POST['button_value'];
            if ($btn_value == "") {
                $btn_value = "start";
            } elseif (array_key_exists("q_file", $_POST)) {
                $btn_value = "check";
            }
            return $btn_value;
        } else {
            return "start";
        }
    }

    function test_input($data) {
        $data = trim($data);
        $data = stripslashes($data);
        $data = htmlspecialchars($data);
        return $data;
    }

     
    $post_value = get_post();

    $main = './cards/';
    if ($post_value == "check") {
        $main = $_POST["q_file"];
        $main = preg_split("/\//", $main);
        $main[sizeof($main)-1] = "";
        $main = join("/", $main);
        $post_value = "check";
        // echo $main;
    }
    
    if (!is_dir($main.$post_value) && $post_value != "check") {
        $classes = get_classes($main);
        $question = "";
    } elseif ($post_value == "check") {
        $classes = get_classes($main);
        $question_array = get_cards($main);
        $question = create_question($question_array, $main, $post_value);
        $post_value = preg_split("/\//", $main);
        $post_value = $post_value[sizeof($post_value)-1];
    } else {
        $classes = get_classes($main.$post_value.'/');
        $question_array = get_cards($main.$post_value.'/');
        $question = create_question($question_array, $main.$post_value.'/', $post_value);
    }

    $buttons = create_buttons($classes, $post_value);
?>