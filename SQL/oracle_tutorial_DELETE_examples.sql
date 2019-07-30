/*Update Statement*/
/*https://www.oracletutorial.com/oracle-basics/oracle-delete/*/

DROP TABLE sales PURGE;

CREATE TABLE sales AS
       SELECT order_id,
              item_id,
              product_id,
              quantity,
              unit_price,
              status,
              order_date,
              salesman_id
FROM orders
INNER JOIN order_items USING(order_id);

/*Delete single row*/
DELETE FROM sales
WHERE order_id = 1 AND item_id = 1;

SELECT * FROM sales
WHERE item_id = 1;

/*Delete multiple rows*/
DELETE FROM sales 
WHERE order_id = 1;

/*Delete ALL rows from table*/
--DELETE FROM sales;

select * from sales;
