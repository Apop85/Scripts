<?php
    include("php/add_files.php");

   
?>

<?php
    $form = create_form();
    $titel = 'Hinzufügen';
    $content = '<p class="content_title">Karteikarten Hinzufügen</p>
                <p class="content_text">Hier lassen sich neue Karteikarten registrieren und zu der Kartei hinzufügen</p>'
                .$form.$message;
?>

<?php include("php/main.php"); ?>