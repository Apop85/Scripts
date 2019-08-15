<?php
include("./php/get_settings.php");
    $page_content = create_help_content();
    $titel = 'Einstellungen';
    $content = '<p class="content_title">Einstellungen</p><p class="content_text">Ändere Installationspfad, Trennzeichen und Frageeinleitung</p>'.$page_content;
?>

<?php include("php/main.php"); ?>