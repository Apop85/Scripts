

<?php
    function count_cards($main, $cards_am){
        global $cards_am;
        $dirHandle = opendir($main);
        while($file = readdir($dirHandle)){
            if(is_dir($main . $file) && $file != '.' && $file != '..'){
                $newdir = $main . $file.'/';
                count_cards($newdir, $cards_am);
            }
            else { 
                if ($file != '.' && $file != '..'){
                    $cards_am[] .= $main.$file;
                }
            
            }
        }
        if (is_array($cards_am)) {
            return array(sizeof($cards_am), $cards_am);
        } else {
            return array(0, []);
        }
    }

    function count_classes($main, $class_names){
        # Suche nach allen Ordnern im aktuellen Ordner
        $dirHandle = opendir($main);
        while($file = readdir($dirHandle)){
            # Alle filtern was "." ".." enthält und kein Ordner ist
            if (is_dir($main.$file) && ($file != "." && $file != "..") && !in_array($file, $class_names)) {
                $class_names[] .= $file;
            }
        }
        return array(sizeof($class_names), $class_names);
    }

    function get_class_list($some_array) {
    # Erstelle das Fächer Untermenü der Seitenleiste
        $output = "<ul>";
        foreach ($some_array as $value) {
            if ($value[-1] != '/') {
                $output .= "<li>".$value.'</li>';
            }
        }
        $output .= "</ul>";
        return $output;
    }

    function get_sub_total($classes, $cards){
        # Erstelle das Totel Untermenü der Seitenleiste
        $output = "<ul>";
        foreach ($classes as $class) {
            $class_list=[];
            foreach ($cards as $card) {
                if (preg_split ("/\//", $card)[2] == $class) {
                    $class_list[] .= $card;
                }
            }
            $output .= "<li>".$class.": ".sizeof($class_list)."</li>";
        }
        $output .= "</ul>";
        return $output;
    }
?>