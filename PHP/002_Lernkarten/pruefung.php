<?php
    include("php/gen_test.php");
    $post_method = check_post_method();
    
    $page_content = get_page_content($post_method);

    $titel = 'Prüfung';
    $content = '<p class="content_title">Prüfung generieren</p>
                <p class="content_text">Hier kann eine Prüfung aufgrund verschiedener Optionen generiert werden.</p>
                '.$page_content;
?>

<?php include("php/main.php"); ?>
