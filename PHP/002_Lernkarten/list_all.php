<?php include("php/get_cards.php"); ?>
<?php
    $file_array = array();
    $output =[];
    $binary = 1;

    $titel = 'Kartenliste';
    $message = check_post();
    $last_sort = get_last_sort();
    $arrangement = $last_sort[1];
    $last_sort = $last_sort[0];
    $cards = get_all_cards("./cards", $output, $binary, $file_array, $arrangement);
    $cards = $cards[0];
    $output_list = "";
    foreach ($output as $card) {
        $output_list .= $card;
    }
    // echo $arrangement." + ".$last_sort;
    // $output_list .= "</form>";

    $content = '
                <p class="content_title">Alle verfügbaren Karten</p>
                <p class="content_text">Hier können Karteikarten bearbeitet oder entfernt werden.</p>
                <table class="list_table"><tr>
                    <th class="title_column"><form class="none" method="get" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'">
                        <button class="sort_button" name="sort" method="get" value="sort_class">Fach</button>
                        <input type="hidden" name="last_sort" value="'.$last_sort.'">
                        <input type="hidden" name="arrangement" value="'.$arrangement.'">
                        </form></th>
                    <th class="title_column"><form class="none" method="get" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'">
                        <button class="sort_button" name="sort" method="get" value="sort_question">Frage</button>
                        <input type="hidden" name="last_sort" value="'.$last_sort.'">
                        <input type="hidden" name="arrangement" value="'.$arrangement.'">
                        </form></th>
                    <th class="title_column"><form class="none" method="get" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'">
                        <button class="sort_button" name="sort" method="get" value="sort_points">Punkte</button>
                        <input type="hidden" name="last_sort" value="'.$last_sort.'">
                        <input type="hidden" name="arrangement" value="'.$arrangement.'">
                        </form></th>
                    <th class="title_column"><form class="none" method="get" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'">
                        <button class="sort_button" name="sort" method="get" value="sort_path">Pfad</button>
                        <input type="hidden" name="last_sort" value="'.$last_sort.'">
                        <input type="hidden" name="arrangement" value="'.$arrangement.'">
                        </form></th>
                    <th class="title_column"><form class="none" method="get" action="'.htmlspecialchars($_SERVER["PHP_SELF"]).'">
                        <button class="sort_button" name="sort" method="get" value="sort_fname">Dateiname</button>
                        <input type="hidden" name="last_sort" value="'.$last_sort.'">
                        <input type="hidden" name="arrangement" value="'.$arrangement.'">
                        </form></th>
                    <th class="title_column">Aktion</th>
                </tr>'.$output_list."</table>".$message;
?>

<?php include("php/main.php"); ?>