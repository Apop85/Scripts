<?php
    if (!class_exists("CreateMenu")) {
        // Funktioniert noch bis hier
        class CreateMenu {
            public function create_menu($menu_array, $class) {
                $nav = "<div class='".$class."'>\n";

                foreach ($menu_array as $item) {
                    $nav .= "                   <button class='menu_button' onclick='window.location.href =\"".$item['url']."\"'>".$item['text']."</button>\n";
                }

                $nav .= "</div>";

                return $nav;
            }

            
        }
    }
    $dtm = new CreateMenu;
?>