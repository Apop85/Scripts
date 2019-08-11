<?php include("php/get_cards.php"); ?>
<?php
    $file_array = array();
    $output =[];
    $binary = 1;

    $titel = 'Kartenliste';
    $message = check_post();
    $cards = get_all_cards("./cards", $output, $binary, $file_array);
    $cards = $cards[0];
    $output_list = "";
    foreach ($output as $card) {
        $output_list .= $card;
    }
    $content = '
                <p class="content_title">Alle verfügbaren Karten</p>
                <p class="content_text">Hier können Karteikarten bearbeitet oder entfernt werden.</p>
                <table class="list_table"><tr>
                    <th class="title_column"><form class="none" method="get" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'"><button class="sort_button" name="sort" method="get" value="sort_class">Fach</button></form></th>
                    <th class="title_column"><form class="none" method="get" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'"><button class="sort_button" name="sort" method="get" value="sort_question">Frage</button></form></th>
                    <th class="title_column"><form class="none" method="get" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'"><button class="sort_button" name="sort" method="get" value="sort_points">Punkte</button></form></th>
                    <th class="title_column"><form class="none" method="get" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'"><button class="sort_button" name="sort" method="get" value="sort_path">Pfad</button></form></th>
                    <th class="title_column"><form class="none" method="get" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'"><button class="sort_button" name="sort" method="get" value="sort_fname">Dateiname</button></form></th>
                    <th class="title_column">Aktion</th>
                </tr><form class="list_form" method="post" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'">'.$output_list."</form></table>".$message;
?>

<?php include("php/main.php"); ?>