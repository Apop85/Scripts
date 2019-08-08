
<?php
    function get_all_cards($main, $cards, $counter){
        global $cards, $counter;
        $dirHandle = opendir($main);
        while($file = readdir($dirHandle)){
            if(is_dir($main . $file) && $file != '.' && $file != '..'){
                $newdir = $main.$file.'/';
                $cards = get_all_cards($newdir, $cards, $counter);
            }
            else { 
                if ($file != '.' && $file != '..'){
                    if ($counter == 1) {
                        $counter = 0;
                    }
                    else {
                        $counter = 1;
                    }
                    $fach = preg_split("/\//", $main);
                    // echo $fach[2];
                    $cards[] .= "<tr class='file_list_".$counter."'><th>".$fach[2]."</td><td>".$main.'</td><td>'.$file."</td></tr>";
                }
            
            }
        }
        return $cards;
    }
?>