<?php include("function.php"); ?>
<!DOCTYPE html>
<html>
    <head>
        <title><?php echo get_title($titel); ?></title>
        <!--STYLESHEET-->
        <link rel="stylesheet" href="styles.css">
    </head>
    <body>
        <div id="wrapper">
            <div id="logo">
                <img id="logo_img" src="https://www.christiane-witt-fengshui.com/wp-content/uploads/2017/03/yin-yang-symbol-705x702.png">
            </div>
            <div id="header">
                <h1>Lernkarten</h1>
                <p>Lernen mit automatisch extrahierten Lernkarten</p>
            </div>
            <div id="menubar">
                <?php echo navigation(); ?>
            </div>