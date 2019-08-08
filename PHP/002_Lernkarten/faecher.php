<?php
    function get_classes($main, $class_names){
        # Suche nach allen Ordnern im aktuellen Ordner
        global $class_names;
        $dirHandle = opendir($main);
        while($file = readdir($dirHandle)){
            # Alle filtern die "." ".." oder ".php" enthalten - Durchsuche rekursiv
            if(is_dir($main . $file) && $file != '.' && $file != '..' && substr($file, -4) != '.php' && substr($file, -1) != '/'){
                $newdir = $main . $file;
                get_classes($newdir, $class_names);
            }
            elseif ($file != '.' && $file != '..' && in_array(substr_replace($main, '', 0, 8), $class_names) != TRUE){
                    $class_names[] = substr_replace($main, '', 0, 8);                
            }
        }
        return $class_names;
    }

    
    function create_buttons($folder_array) {
        # Erstelle navigationsbuttons
        $init_form = '<form class="form" method="post" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'">';
        $output = $init_form;
        if ($_SERVER["REQUEST_METHOD"] == "POST") {
            foreach ($folder_array as $folder) {
                if (substr($folder, -1) == '/' && sizeof($folder_array) > 1) {
                    $output .= "<button class='class_button' method='post' name='button' value=''>".$folder."</button>";
                }
                elseif (sizeof($folder_array) > 1) {
                    $output .= "<button class='class_button' method='post' name='button' value='".$folder."'>".$folder."</button>";
                }
                else {
                    $output .= "<button class='class_button' method='post' name='button' value=''>".$folder."</button>";
                }
            }
        }
        else {
            foreach ($folder_array as $folder) {
                $output .= "<button class='class_button' method='post' name='button' value='".$folder."'>".$folder."</button>";
            }
        }
        $output .= '</form>';
        return $output;
    }

    function get_post_value(){
        if ($_SERVER["REQUEST_METHOD"] == "POST") {
            $value = test_input($_POST["button"]);
            return $value;
        }
        return NULL;
    }
    
    function check_main($main){
        if ($_SERVER["REQUEST_METHOD"] == "POST") {
            $value = test_input($_POST["button"]);
            if (substr($value, -1) == "/") {
                return $main.$value;
            }
            elseif ($value != "") {
                return $main.$value."/";
            }
            else {
                return $main;
            }
        }
        return $main;
    }
    
    function test_input($data) {
        $data = trim($data);
        $data = stripslashes($data);
        $data = htmlspecialchars($data);
        return $data;
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
        return array($q,$a,$f);
    }
    
    
    function create_question($quest_array, $main) {
        $div_intro_1 = "<div class='flip-card'>";
        $div_intro_2 = "<div class='flip-card-inner'>";
        $div_outro = '</div></div>';
        $form_0 = '<form method="post" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'">';
        $form_1 = '<p class="frage">'.$quest_array[0]."</p>";
        $form_2 = '<input class="antwort" type="text" name="answer" autofocus><button class="submit_button" method="post">GO</button>';
        $form_3 = '<input type="hidden" name="true_answer" value="'.$quest_array[1].'">';
        $form_4 = '<input type="hidden" name="button" value="'.substr($main, 8).'">';
        $form_5 = '<input type="hidden" name="last_file" value="'.$quest_array[2].'">';
        $form_6 = '</form>';
        $form = $form_0.$form_1.$form_2.$form_3.$form_4.$form_5.$form_6;
        if ($_SERVER["REQUEST_METHOD"] == "POST" && sizeof($quest_array) == 3 && $main != "./cards/") {
            $answer = test_input($_POST["answer"]);
            $last_answer = test_input($_POST["true_answer"]);
            $last_file = test_input($_POST["last_file"]);
            
            if ($last_answer == $answer && $answer != "") {
                # Richtige Antwort
                $front_1 = "<div class='flip-card-front'>";
                $front_2 = '<div class="true_value"><p>Diese Antwort war richtig</p></div>';
                $front_3 = '</div>';
                $back_1 = '<div class="flip-card-back">';
                $back_2 = $form;
                $back_3 = '</div>';
            }
            elseif ($last_answer == "") {
                # Wenn erste Frage
                $div_intro_2 = "<div class='flip-card-static'>";
                $front_1 = '<div class="flip-card-front-static">';
                $front_2 = $form;
                $front_3 = '</div>';
                $back_1 = "";
                $back_2 = "";
                $back_3 = "";
            }
            else {
                # Falsche Antwort
                $front_1 = "<div class='flip-card-front'>";
                $front_2 = '<div class="false_value"><p>Diese Antwort war falsch. Die richtige Antwort lautet:</p><div class="correct">'.$last_answer.'</div>Siehe '.$last_file.'</a></div>';
                $front_3 = '</div>';
                $back_1 = '<div class="flip-card-back">';
                $back_2 = $form;
                $back_3 = '</div>';
            }
            $div_intro = $div_intro_1.$div_intro_2;
            
            $front = $front_1.$front_2.$front_3;
            $back = $back_1.$back_2.$back_3;
            
            return $div_intro.$front.$back.$div_outro;
        }
        
        return "<div class='flip-card'></div>";
    }
    
    $main = "./cards/";
    $emptyarray = [];
    $post_value = get_post_value();
    $main = check_main($main);
    $cards = get_cards($main);

    $classes = get_classes($main, $emptyarray);
    $buttons = create_buttons($classes);
    $question = create_question($cards, $main);
?>

<?php
    $titel = "Schulfächer";
    $content = '<p class="content_title">Schulfächer mit verfügbaren Lernkarten</p><p class="content_text">Hier kann man sich Fragen zu unterschiedlichen Fächern stellen lassen</p>'.$buttons.$question;
?>
<?php include("php/main.php"); ?>