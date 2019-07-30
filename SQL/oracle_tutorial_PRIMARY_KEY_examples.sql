/*PRIMARY KEY Beispiele*/
/*https://www.oracletutorial.com/oracle-basics/oracle-primary-key/*/

CREATE TABLE purchase_orders (
    po_nr NUMBER PRIMARY KEY, --> Primary Key verhindert NULL-Einträge, NOT NULL muss also nicht definiert werden
    vendor_id NUMBER NOT NULL,
    po_status NUMBER(1,0) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL
);



/*PRIMARY KEY mit mehreren Zuweisungen*/
CREATE TABLE purchase_order_items (
       po_nr NUMBER NOT NULL,
       item_nr NUMBER NOT NULL,
       product_id NUMBER NOT NULL,
       quantity NUMBER NOT NULL, 
       purchase_unit NUMBER NOT NULL, 
       buy_price NUMBER(9,2) NOT NULL,
       delivery_date DATE,
       PRIMARY KEY (po_nr, item_nr)
);



/*PRIMIARY KEY im nachhinein definieren*/
CREATE TABLE vendors (
             vendor_id NUMBER,
             vendor_name VARCHAR2(255) NOT NULL,
             address VARCHAR(255) NOT NULL);
             
ALTER TABLE vendors
ADD CONSTRAINT pk_vendors PRIMARY KEY (vendor_id);
             
SELECT * FROM vendors;       

/*PRIMARY KEY wieder entfernen*/
ALTER TABLE vendors
DROP CONSTRAINT pk_vendors;

-- ODER

ALTER TABLE vendors DROP PRIMARY KEY;


/*Primary Keys de-/akivieren*/
-- Das Deaktivieren der primary keys kann bei grossen Datensätzen zu einem Performanceboost führen. 
ALTER TABLE purchase_orders
-- DISABLE CONSTRAINT pk_purchase_orders -- Alternative
DISABLE PRIMARY KEY;

ALTER TABLE purchase_orders
-- ENABLE CONSTRAINT pk_purchase_orders -- Alternative
ENABLE PRIMARY KEY;

/*Erstellte Tabellen dieses Beispiels löschen*/
DROP TABLE purchase_orders PURGE;
DROP TABLE purchase_order_items PURGE;       
DROP TABLE vendors PURGE;       
