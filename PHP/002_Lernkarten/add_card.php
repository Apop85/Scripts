<?php
    function create_form() {
        $classes = get_class_names("./cards/", []);
        $menu_size = sizeof($classes);
        $dd_menu = "";
        foreach ($classes as $class) {
            $dd_menu .= '<option value="'.$class.'">'.$class.'</option>';
        }
        // CSS Slideshow
        $output =   '<div class="add_manual_form"><form method="post" class="manual_form" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'">
                    <div class="slideshow middle"><div class="slides">
                    <input type="radio" name="r" class="r1" id="r1" checked>
                    <input type="radio" name="r" class="r2" id="r2">
                    <input type="radio" name="r" class="r3" id="r3">
                    <input type="radio" name="r" class="r4" id="r4">
                    

                    <div class="slide s1">
                            
                        <div class="add_titles">Alle Dokumente durchsuchen
                            <div class="add_info_text">
                                <p>Alle Dokumente im Dokumentenordner werden durchsucht und alle noch nicht vorhandenen Karteikarten werden dabei automatisch erstellt</p>
                                <button class="add_button" method="post" name="button" value="search_all" autofocus>Dokumente durchsuchen</button></div>
                    </div></div>

                    <div class="slide">
                        
                        <div class="add_titles">Ordner durchsuchen
                            <div class="add_info_text">
                                <p>Durchsuche einen bestimmten Ordner nach docx Dateien und erstelle entsprechende Karteikarten</p>
                                <input class="add_input" type="text" name="dir_name">
                                <button class="add_button" method="post" name="button" value="search_folder">Ordner durchsuchen</button></div>
                    </div></div>
                    
                    <div class="slide">
                        
                        <div class="add_titles">Datei durchsuchen
                            <div class="add_info_text">
                                <p>Durchsuche eine bestimmte Datei um darin vermerkte Lernkarten zu erstellen</p>
                                <input class="add_input" type="text" name="file_name">
                                <button class="add_button" method="post" name="button" value="search_file">Datei durchsuchen</button></div></div>
                    </div>

                    <div class="slide">
                        
                        <div class="add_titles">Manueller Eintrag
                            <div class="add_info_text">
                                <p>Erstelle manuell einen Eintrag und füge diesen einem Fach hinzu</p>
                                <div class="manual_form man_entry">
                                    <table class="hidden_table"><tr>
                                        <td> Frage:</td><td> <textarea class="add_manual_input" type="text" name="frage"></textarea></td></tr>
                                        <tr><td>Antwort:</td><td> <textarea class="add_manual_input" type="text" name="antwort"></textarea></td>
                                    </tr></table>
                                <div class="add_button_wrapper"><button class="add_button" method="post" name="button" value="add_entry">Hinzufügen</button></div>

                                <div class="drop_down_menu">
                                    <select name="class_option" size="'.$menu_size.'">'.$dd_menu.'</select></div></div></div>

                    </div></div></div></div></form>

                    <div class="navigation">
                        <label for="r1" title="Alles durchsuchen" class="bar doc_alle"></label>
                        <label for="r2" title="Ordner durchsuchen" class="bar doc_dir"></label>
                        <label for="r3" title="Datei durchsuchen" class="bar doc_file"></label>
                        <label for="r4" title="Eintrag erstellen" class="bar doc_man"></label>
                    </div></div>';

        return $output;
    }

    function get_class_names($main, $class_names, $counter = 0){
        $counter++;
        # Suche nach allen Ordnern im aktuellen Ordner
        $dirHandle = opendir($main);
        while($file = readdir($dirHandle)){
            # Alle filtern was "." ".." enthält und kein Ordner ist
            
            if (is_dir($main.$file) && ($file != "." && $file != "..") && !in_array($file, $class_names)) {
                if ($counter == 1) {
                    $class_names[] .= trim($main, "./cards/").$file;
                    $class_names = get_class_names($main.$file.'/', $class_names, $counter);
                }
                else {
                    // Rekursive Ordnersuche
                    $class_names[] .= trim($main, "./cards/").'/'.$file;
                    $class_names = get_class_names($main.$file.'/', $class_names, $counter);
                }
            }
        }
        return $class_names;
    }

    function test_post($data) {
        // Formatiere POST-Strings
        $data = trim($data);
        $data = stripslashes($data);
        $data = htmlspecialchars($data);
        return $data;
    }

    function check_post() {
        // Auslesen der Button_Value
        if ($_SERVER["REQUEST_METHOD"] == "POST") {
            $button_value = test_post($_POST["button"]);

            return $button_value;
        }
        return NULL;
    }

    function get_add_entry_values() {
        // Lese Daten für das manuelle anlegen von Lernkarten aus
        $add_question = $_POST["frage"];
        $add_answer = $_POST["antwort"];
        $add_class = $_POST["class_option"];

        return array($add_question, $add_answer, $add_class);
    }

    function get_file_values() {
        // Auslesen des Verzeichnisses und umformatieren des Strings
        $add_file = $_POST['file_name'];
        $add_file = trim($add_file, '"');
        $add_file = escapeshellarg(addslashes($add_file));

        return $add_file;
    }

    function get_dir_values() {
        // Auslesen des Ordners zum Scannen
        $add_dir = $_POST["dir_name"];
        $add_dir = trim($add_dir, '"');
        $add_dir = escapeshellarg(addslashes($add_dir));

        return $add_dir;
    }

    $message = "";
    $button_value = check_post();
    if ($button_value != NULL) {

        if ($button_value == "search_all") {
            # python find_all ausführen
            $command = escapeshellcmd('python ./py/add_card.py "all"');
            $message = shell_exec($command);
        }
        elseif ($button_value == "search_folder") {
            $add_dir = get_dir_values();
            if ($add_dir != "") {
                # python find_dir ausführen
                $command = escapeshellcmd('python ./py/add_card.py "dir" "'.$add_dir.'"');
                $message = shell_exec($command);
            }
            else {
                $message = '<div class="output_message error">Kein Pfad angegeben</div>';
            }
        }
        elseif ($button_value == "search_file") {
            $add_file = get_file_values();
            if ($add_file != "") {
                # python find_file ausführen
                $command = escapeshellcmd('python ./py/add_card.py "file" "'.$add_file.'"');
                $message = shell_exec($command);    
            }
            else {
                $message = '<div class="output_message error">Kein Dateipfad angegeben</div>';
            }
        }
        elseif ($button_value == "add_entry") {
            // Manuell hinzufügen
            $values = get_add_entry_values();
            $frage = $values[0];
            $antwort = $values[1];
            $fach = $values[2];

            if ($frage != "" && $antwort != "" && $fach != "") {
                $command = escapeshellcmd('python ./py/add_card.py "manadd" "'.$frage.'" "'.$antwort.'" "'.$fach.'"');
                $message = shell_exec($command);
            }
            else {
                $message = '<div class="output_message error">Fehlende Angaben</div>'; 
            }

            
        }
    }
?>

<?php
    $form = create_form();
    $titel = 'Hinzufügen';
    $content = '<p class="content_title">Karteikarten Hinzufügen</p>
                <p class="content_text">Hier lassen sich neue Karteikarten registrieren und zu der Kartei hinzufügen</p>'
                .$form.$message;
?>

<?php include("php/main.php"); ?>