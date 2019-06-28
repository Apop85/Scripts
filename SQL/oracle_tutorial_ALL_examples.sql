/*Verwendung des ALL-Operators*/
/*https://www.oracletutorial.com/oracle-basics/oracle-all/*/

/*Return values greater than the maximum list_price of category 1*/
SELECT product_name, list_price
FROM products p1
WHERE p1.list_price > ALL( SELECT p2.list_price
                           FROM products p2
                           WHERE category_id = 1 )
ORDER BY product_name;


/*Find the average list_price of each category*/
SELECT category_id, ROUND(AVG(list_price),2) avg_list_price
FROM products
GROUP BY category_id
ORDER BY category_id;

/*Fin products with list_prices greater than the highest price of the average price list*/
SELECT product_name, list_price
FROM products
WHERE list_price > ALL ( SELECT AVG(list_price)
                         FROM products
                         GROUP BY category_id )
ORDER BY list_price;

--SELECT MAX(list_price) FROM products WHERE category_id = 1                           
