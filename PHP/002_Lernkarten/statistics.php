<?php
    include("php/create_stats.php");

    $output = get_content();
    $titel="Statistiken";
    $content = '<p class="content_title">Lernstatistiken</p><p class="content_text">Statistiken über die Qualität der gegebenen Antworten</p>'.$output;
?>
<?php include("php/main.php"); ?>