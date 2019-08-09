<?php include("php/get_cards.php"); ?>
<?php
    $titel = 'Kartenliste';
    $message = check_post();
    $cards = get_all_cards("./cards/", "", 0);
    $output_list = "";
    foreach ($cards as $card) {
        $output_list .= $card;
    }
    $content = '<form class="list_form" method="post" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'">
                <p class="content_title">Alle verfügbaren Karten</p>
                <p class="content_text">Hier können Karteikarten bearbeitet oder entfernt werden.</p>
                <table class="list_table"><tr><th class="title_column">Fach</th>
                <th class="title_column">Frage</th><th class="title_column">Punkte</th>
                <th class="title_column">Pfad</th>
                <th class="title_column">Dateiname</th><th class="title_column">Aktion</th>
                </tr>'.$output_list."</table></form>".$message;
?>

<?php include("php/main.php"); ?>