<?php
    include("menu.php");
    
    function navigation() {
        global $dtm;
        
        $class = "main_nav";
        
        $menu_array = array (
            array("text" => "Home", "url" => "index.php"),
            array("text" => "Fächer", "url" => "faecher.php"),
            array("text" => "Randomfrage", "url" => "random.php"),
            array("text" => "Liste", "url" => "list_all.php"),
            array("text" => "Hinzufügen", "url" => "add_card.php")
        );
        
        return $dtm -> create_menu($menu_array, $class);
    }

    function get_title($titel) {
        $do_titel = $titel." | Raffis Lernkarten";
        
        return $do_titel;
    }
?>