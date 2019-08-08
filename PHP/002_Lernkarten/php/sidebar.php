<?php
    include("count_cards.php");
    $card_amount = count_cards("./cards/", []);
    $classes_amount = count_classes("./cards/", []);
    $total_cls = get_class_list($classes_amount[1]);
    $total_sub = get_sub_total($classes_amount[1], $card_amount[1])
?>
            <div id="infoleiste">
                    <h3>Infoleiste</h3>
                <ul>
                    <li class="info_text">Total: <?php echo $card_amount[0]; ?><?php echo $total_sub; ?></li>
                </ul><ul>
                    <li class="info_text">Fächer: <?php echo $classes_amount[0]; echo $total_cls; ?></li>
                </ul>
            </div>
