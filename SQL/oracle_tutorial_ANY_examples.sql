/*Verwendung des ANY-Operators*/
/*https://www.oracletutorial.com/oracle-basics/oracle-any/*/

/*Return all products with a higher price than the smallest list price of products in category 1*/
SELECT product_name, list_price
FROM products
WHERE list_price > ANY( SELECT list_price
                        FROM products
                        WHERE category_id = 1 )
ORDER BY product_name;


/*Identic function but without ANY-operator*/
SELECT product_name, list_price
FROM products p1
WHERE EXISTS( SELECT list_price
              FROM products p2
              WHERE p1.list_price > p2.list_price )
ORDER BY product_name;

/*Find products with specific prices*/
SELECT product_name, list_price
FROM products
WHERE list_price = ANY( 2200, 2259.99, 2269.99 )
      AND category_id = 1;

--SELECT MIN(list_price) FROM products WHERE category_id = 1
