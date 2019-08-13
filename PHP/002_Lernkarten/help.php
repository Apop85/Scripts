<?php
include("./php/get_help.php");
    $page_content = create_help_content();
    $titel = 'HILFE';
    $content = '<p class="content_title">Hilfestellung</p><p class="content_text">Auf dieser Seite kännen Hilfestellungen zum Betrieb dieser Seite ausgelesen werden</p>'.$page_content;
?>

<?php include("php/main.php"); ?>