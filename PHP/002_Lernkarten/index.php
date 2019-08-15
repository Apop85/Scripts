<?php
    function read_ini_file() {
        $command = escapeshellcmd('python ./py/conf.py "get_content"');
        $output = shell_exec($command);
        $output = htmlentities($output);  
        $output = preg_split("/\?/", $output);

        return $output;
    }
    // include("./php/get_help.php");

    $ini_values = read_ini_file();
    
    $command = escapeshellcmd('python -c "import os; print(1) if (os.path.exists(\''.$ini_values[0].'\') and os.path.exists(\''.$ini_values[1].'\')) else print(0)"');
    $setup_switch = shell_exec($command);
    global $setup_switch;
    // echo $setup_switch;

    if ($setup_switch == 0) {
        include("./php/get_settings.php");
        $titel = "SETUP";
        $content = '<p class="content_title">SETUP</p>
                    <p class="content_text">Dies scheint das erste mal zu sein dass du diese Seite öffnest 
                    oder dass der Installationspfad geändert wurde. Bitte hinterlege die benötigten 
                    Informationen um die volle Funktionalität zu nutzen.</p>'
                    .create_help_content();
    } else {
        $titel = 'HOME';
        $content = '<p class="content_title">Digitale Lernkarten</p><p class="content_text">Auf dieser Seite können Lernkarten angelegt werden welche dann geübt werden können. Diese Werden nach Fächern oder Fortschritt sortiert. Wähle im Menü wie du weiter fortfahren möchtest</p>';
    }
?>

<?php include("php/main.php"); ?>