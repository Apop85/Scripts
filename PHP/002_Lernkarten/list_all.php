<?php include("php/get_cards.php"); ?>
<?php
    $titel = 'Kartenliste';
    $cards = get_all_cards("./cards/", "", 0);
    $output_list = "";
    foreach ($cards as $card) {
        $output_list .= $card;
    }
    $content = '<p class="content_title">Alle verfügbaren Karten</p><p class="content">In dieser Liste findest du alle bisher abgelegten Karten</p><table class="list_table"><tr><th "class=title_column">Fach</th><th "class=title_column">Pfad</th><th "class=title_column">Dateiname</th></tr>'.$output_list."</table>";
?>

<?php include("php/main.php"); ?>