/*CASE ausdruck*/
/*https://www.oracletutorial.com/oracle-basics/oracle-case/*/

SELECT product_name, list_price,

CASE category_id
  WHEN 1
    THEN ROUND(list_price*0.05,2) -- CPU
  WHEN 2
    THEN ROUND(list_price*0.1,2) -- GPU
  ELSE
    ROUND(list_price*0.08,2) -- sonstige Kategorien
END discount

FROM products
ORDER BY product_name;

SELECT product_name, list_price,
CASE
  WHEN list_price > 0 AND list_price < 600
    THEN 'Mass'
  WHEN list_price >= 600 AND list_price < 1000
    THEN 'Economy'
  WHEN list_price >= 1000 AND list_price < 2000
    THEN 'Luxury'
  ELSE
    'Grand Luxury'
END product_group
FROM products
WHERE category_id = 1 
ORDER BY product_name;

/*Case Statement in einer Order By klausel*/
SELECT * FROM locations
WHERE country_id in ('US','CA','UK')
ORDER BY country_id,
CASE country_id 
      WHEN 'US'
        THEN state
        ELSE city
      END;
      
/*Case Statement in einer Having klausel*/
SELECT product_name, category_id, COUNT(product_id)
FROM order_items
INNER JOIN products USING(product_id)
GROUP BY product_name, category_id
HAVING COUNT(CASE
               WHEN category_id = 1
                 THEN product_id
                 ELSE NULL
                END) > 5
OR
       COUNT(CASE
               WHEN category_id = 2
                 THEN product_id
                 ELSE NULL
               END) > 2
ORDER BY product_name;

/*Case statement innerhalb eines Update statements*/
SELECT product_name, list_price, standard_cost, 
       ROUND((list_price-standard_cost)*100/list_price,2) gross_margin
FROM products
WHERE ROUND((list_price-standard_cost)*100/list_price,2) >= 12;

UPDATE products
SET list_price = CASE
                   WHEN ROUND((list_price-standard_cost)*100/list_price,2) < 12
                   THEN (standard_cost) * 12
                 END
WHERE ROUND((list_price-standard_cost)*100/list_price,2) < 12;
               
