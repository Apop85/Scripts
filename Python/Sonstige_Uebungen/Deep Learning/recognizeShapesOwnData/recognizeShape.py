#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: recognizeShape.py
# Project: recognizeShapesOwnData
#-----
# Created Date: Saturday 16.01.2021, 23:03
# Author: Apop85
#-----
# Last Modified: Sunday 17.01.2021, 03:14
#-----
# Copyright (c) 2021 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description:  Neurales Netzwerk zum erkennen von einfachen Formen mit eigenem Datensatz
#               Das Script erstellt selbstständig die zum Training und zum Testen verwendeten Bilder
#               Möchte man mit einem neuen Datenset arbeiten kann man debug auf True stellen oder die 
#               entsprechenden Ordner löschen. 
# Quelle: https://www.youtube.com/watch?v=j-3vuBynnOE
####

import os
# Unterdrücke Debug-Nachrichten
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3' 
# Random-Module
from random import randint as rng
from random import shuffle
# Pillow-Modul zum erstellen der Bilder
from PIL import Image, ImageDraw
# Math-Modul zur berechnung der Eckpunkte der Figuren
import math
# Tensorflow-Modul zum training und testen des neuronalen Netzwerks
import tensorflow as tf
# Numpy-Modul für die statistische Analyse
import numpy as np
# Pyplot-Modul zum Anzeigen der Bilder
import matplotlib.pyplot as plt
# opencv-Modul zum umwandlen der Bildgrösse
import cv2
# pickle-Modul zum speichern der Trainingsdaten
import pickle

# Pfaddefinitionen
example_file_path = os.path.join(".", "trainingData")
model_data_path = os.path.join(".", "modelData")
test_data_path = os.path.join(".", "testData")
training_model_path = os.path.join(model_data_path, "trainingModel")
pickle_X_train_path = os.path.join(model_data_path, "X.pickle")
pickle_y_train_path = os.path.join(model_data_path, "y.pickle")
pickle_X_test_path = os.path.join(model_data_path, "X_test.pickle")
pickle_y_test_path = os.path.join(model_data_path, "y_test.pickle")

# Mögliche Kategorien
categories = ["square", "triangle", "circle"]
# Ausgangsbildgrösse
image_size = (64,64)
# Reduzierte Bildgrösse
target_image_size = 30
# Anzahl zu erstellenden Trainingsbilder pro Kategorie
training_data_amount = 3500
# Anzahl der zu testenden Bilder pro Kategorie
test_images_amount = 100
# Anzahl Trainings mit den Trainingbilder
number_of_trainings = 200

# Alle Bilder, Modelle und Daten neu erstellen
debug = False

# Wechsle zum Scriptpfad
os.chdir(os.path.dirname(__file__))

def createTrainingData(path, image_size, amount, purpose="Training"):
    # Funkntion zum erstellen von Trainingsbildern
    # Input:
    #   - path = string         | Bilderpfad
    #   - image_size = tuple    | (x, y) -> Bildgrösse
    #   - purpose = string      | Zur Ausgabe bei der Verarbeitung
    print(f"Create {amount*3} {purpose} Images...", end="")
    # Vierecke erstellen
    for i in range(amount):
        # Zufällige Hintergrundfarbe bestimmen
        random_background_color = (rng(0,255), rng(0,255), rng(0,255))
        # Neue Bilddatei erstellen
        image = Image.new('RGB', image_size, random_background_color)
        # Erstelle Bild
        draw = ImageDraw.Draw(image)

        # Bestimme Mittelpunkt der Figur
        square_center = (image_size[0] // 2, image_size[1] // 2)
        square_length = rng(10,45)

        # Bestimme Eckpunkte
        square_vertices = (
            (square_center[0] + square_length / 2, square_center[1] + square_length / 2),
            (square_center[0] + square_length / 2, square_center[1] - square_length / 2),
            (square_center[0] - square_length / 2, square_center[1] - square_length / 2),
            (square_center[0] - square_length / 2, square_center[1] + square_length / 2)
        )

        # Rotiere Figur
        square_vertices = [rotated_about(x,y, square_center[0], square_center[1], math.radians(rng(0,90))) for x,y in square_vertices]


        # Zufällige Formfarbe bestimmen
        random_square_color = random_background_color
        color_difference = 0
        while color_difference < 155:
            random_square_color = (rng(0,255),rng(0,255),rng(0,255))
            color_difference = random_background_color[0] + random_square_color[1] + random_square_color[2] - random_background_color[0] - random_background_color[1] - random_background_color[2]
            if color_difference < 0:
                color_difference *= -1
        # Polygon auf Bild zeichnen
        draw.polygon(square_vertices, fill=random_square_color)
        # Bild speichern
        image.save(path + "\\square_{}.png".format(i))

    # Dreiecke erstellen
    for i in range(amount):
        # Zufällige Hintergrundfarbe bestimmen
        random_background_color = (rng(0,255), rng(0,255), rng(0,255))

        # Neue Bilddatei erstellen
        image = Image.new('RGB', image_size, random_background_color)
        # Erstelle Bild
        draw = ImageDraw.Draw(image)

        # Bestimme Mittelpunkt der Figur
        triangle_center = (image_size[0] // 2, image_size[1] // 2)
        triangle_length = rng(10,45)

        # Bestimme Eckpunkte
        triangle_vertices = (
            (triangle_center[0] + triangle_length / 2, triangle_center[1] + triangle_length / 2),
            (triangle_center[0] + triangle_length / 2, triangle_center[1] - triangle_length / 2),
            (triangle_center[0] - triangle_length / 2, triangle_center[1] - triangle_length / 2)
        )

        # Rotiere Figur
        triangle_vertices = [rotated_about(x,y, triangle_center[0], triangle_center[1], math.radians(rng(0,90))) for x,y in triangle_vertices]

        # Zufällige Formfarbe bestimmen
        random_triangle_color = random_background_color
        color_difference = 0
        while color_difference < 155:
            random_triangle_color = (rng(0,255),rng(0,255),rng(0,255))
            color_difference = random_background_color[0] + random_triangle_color[1] + random_triangle_color[2] - random_background_color[0] - random_background_color[1] - random_background_color[2]
            if color_difference < 0:
                color_difference *= -1
        # Polygon auf Bild zeichnen
        draw.polygon(triangle_vertices, fill=random_triangle_color)
        # Bild speichern
        image.save(path + "\\triangle_{}.png".format(i))

    # Kreise erstellen
    for i in range(amount):
        # Zufällige Hintergrundfarbe bestimmen
        random_background_color = (rng(0,255), rng(0,255), rng(0,255))
        # Neue Bilddatei erstellen
        image = Image.new('RGB', image_size, random_background_color)
        # Erstelle Bild
        draw = ImageDraw.Draw(image)

        # Bestimme die Eckpunkte des Kreises
        circle_top_left = [rng(1,image_size[0]), rng(1, image_size[1])]
        delta = [0, 0]
        while delta[0] < 10 or delta[1] < 10:
            circle_bottom_right = [rng(1,image_size[0]), rng(1, image_size[1])]
            delta[0] = circle_bottom_right[0] - circle_top_left[0]
            delta[1] = circle_bottom_right[1] - circle_top_left[1]
            if delta[0] < 0:
                delta[0] *= -1
            if delta[1] < 0:
                delta[1] *= -1
            if circle_top_left[0] > circle_bottom_right[0]:
                circle_top_left[0], circle_bottom_right[0] = circle_bottom_right[0], circle_top_left[0]
            if circle_top_left[1] > circle_bottom_right[1]:
                circle_top_left[1], circle_bottom_right[1] = circle_bottom_right[1], circle_top_left[1]

        circle_top_left = (circle_top_left[0], circle_top_left[1])
        circle_bottom_right = (circle_bottom_right[0], circle_bottom_right[1])
        
        # Zufällige Formfarbe bestimmen
        random_circle_color = random_background_color
        color_difference = 0
        while color_difference < 155:
            random_circle_color = (rng(0,255),rng(0,255),rng(0,255))
            color_difference = random_background_color[0] + random_circle_color[1] + random_circle_color[2] - random_background_color[0] - random_background_color[1] - random_background_color[2]
            if color_difference < 0:
                color_difference *= -1
        # Kreis zeichnen
        draw.ellipse((circle_top_left, circle_bottom_right), fill=random_circle_color)
        # Bild speichern
        image.save(path + "\\circle_{}.png".format(i))
    print("Done\n")


def distance(ax, ay, bx, by):
    #finds the straight-line distance between two points
    return math.sqrt((by - ay)**2 + (bx - ax)**2)

def rotated_about(ax, ay, bx, by, angle):
    #rotates point `A` about point `B` by `angle` radians clockwise.
    radius = distance(ax,ay,bx,by)
    angle += math.atan2(ay-by, ax-bx)
    return (
        round(bx + radius * math.cos(angle)),
        round(by + radius * math.sin(angle))
    )

def createDataStructure(path, training_data=[]):
    # Funktion zum erstellen einer für tensorflow verwendbaren Datenstruktur
    # Inputs:
    #   - path = string     | Pfad zu den Bilddateien
    for image in os.listdir(path):
        # Klasse aus Dateiname auslesen
        class_num = categories.index(image.split("_")[0])
        try:
            filepath = os.path.join(path, image)
            # Bild einlesen
            image_array = cv2.imread(filepath, cv2.IMREAD_GRAYSCALE)
            # Bildgrösse anpassen
            new_array = cv2.resize(image_array, (target_image_size, target_image_size))
            # Add to image array
            training_data.append([new_array, class_num])
        except Exception as error_message:
            print(f"Can't open image {image} --> {error_message}")
    return training_data

# Erstelle Trainingspfad
if not os.path.exists(example_file_path):
    os.mkdir(example_file_path)
    createTrainingData(example_file_path, image_size, training_data_amount)
elif debug:
    createTrainingData(example_file_path, image_size, training_data_amount)

# Erstelle Modell-Pfad
if not os.path.exists(model_data_path):
    os.mkdir(model_data_path)

# Prüfe ob Trainingsdaten geladen werden können
if not os.path.exists(pickle_X_train_path) or not os.path.exists(pickle_y_train_path) or debug:
    # Erstelle Trainingsdaten
    print("Create Training Data...", end="")
    training_data = createDataStructure(example_file_path)
    shuffle(training_data)

    X_train = []
    y_train = []

    # Trenne Aufgabe und Lösung
    for features,label in training_data:
        X_train.append(features)
        y_train.append(label)

    # Normalisieren der Werte zwischen 0 und 1
    X_train = tf.keras.utils.normalize(X_train, axis=1)
    # Wandle Array in numpy-Array um

    X_train = np.array(X_train).reshape(-1, target_image_size, target_image_size, 1)
    X_train = np.asarray(X_train)
    y_train = np.asarray(y_train)

    print("Done")
    print(f"Training data: {len(training_data)}")

    # Speichere Trainingsdaten
    print("Save Training Data...", end="")
    pickle_out = open(pickle_X_train_path, "wb")
    pickle.dump(X_train, pickle_out)
    pickle_out.close()
    pickle_out = open(pickle_y_train_path, "wb")
    pickle.dump(y_train, pickle_out)
    pickle_out.close()
    print("Done")
else:
    # Lade Trainingsdaten
    print("Load Training Data...", end="")
    pickle_in = open(pickle_X_train_path, "rb")
    X_train = pickle.load(pickle_in)
    pickle_in.close()
    pickle_in = open(pickle_y_train_path, "rb")
    y_train = pickle.load(pickle_in)
    pickle_in.close()
    print("Done")


if not os.path.exists(training_model_path) or debug:
    # Trainingsmodell festlegen
    model = tf.keras.models.Sequential()
    # Definition der Eingabelayer
    model.add(tf.keras.layers.Flatten())
    # activation: Was lässt das Neuron auslösen 
    #   relu = lineares gleichrichten -> Default Aktivierungsfunktion
    #   softmax = Wahrscheinlichkeitsverteilung (Wie wahrscheinlich ist die Zahl auf dem Bild eine 1,2,3...) -> Default Outputfunkt 
    model.add(tf.keras.layers.Dense(128, activation=tf.nn.relu)) # 64 + 64 --> Anzahl Neuronen --> Unsichtbar
    model.add(tf.keras.layers.Dense(128, activation=tf.nn.relu)) # 64 + 64 --> Anzahl Neuronen --> Unsichtbar
    # Outputlayer
    model.add(tf.keras.layers.Dense(3, activation=tf.nn.softmax)) # 3 = Anzahl klassifikationen (0 - 9)

    model.compile(optimizer='adam',                       # adam = Defaultoptimizer
                loss='sparse_categorical_crossentropy',   # Fehlerhafte Vorhersagen / (sparse_)categorical_entropy = default / binary_crossentropy = default für 2 Klassifikationen
                metrics=['accuracy'])                     # Welche Eigenschaften sollen getrackt werden? Beispiel accuracy für genauigkeit

    # Modeltraining
    model.fit(X_train, y_train, epochs=number_of_trainings)

    # Modell auf Disk speichern
    print("Saving model...", end="")
    model.save(training_model_path)
    print("Done")
else:
    print("Loading model...", end="")
    model = tf.keras.models.load_model(training_model_path)
    print("Done")

# Berechne Genauigkeit und Verlust
val_loss, val_acc = model.evaluate(X_train, y_train)
print("\nVerlust    : {}".format(val_loss))
print("Genauigkeit: {}\n".format(val_acc))

# Erstelle Testbildpfad
if not os.path.exists(test_data_path):
    os.mkdir(test_data_path)

# Lösche vorhandene Testfiles
for file in os.listdir(test_data_path):
    os.remove(os.path.join(test_data_path, file))

# Testbilder erstellen
createTrainingData(test_data_path, image_size, test_images_amount, purpose="Testing")

# Testdaten erstellen
X_test = createDataStructure(test_data_path)
shuffle(X_test)

X_new = []
y_test = []
# Trennen in Bild und Lösung
for image, class_num in X_test:
    X_new.append(image)
    y_test.append(class_num)
X_test = X_new
# Normalisieren der Werte zwischen 0 und 1
X_test = tf.keras.utils.normalize(X_test, axis=1)

# Speichern der Testdaten
print("Save testdataset...", end="")
pickle_out = open(pickle_X_test_path, "wb")
pickle.dump(X_test, pickle_out)
pickle_out.close()
pickle_out = open(pickle_y_test_path, "wb")
pickle.dump(y_test, pickle_out)
pickle_out.close()
print("Done")



# Umwandlen in numpy-array
X_test = np.array(X_test).reshape(-1, target_image_size, target_image_size, 1)
X_test = np.asarray(X_test)

# Testen der Formerkennung
predictions = model.predict([X_test])
for i in range(len(X_test)):
    # Wahrscheinlichster Wert auslesen
    highest_weight_value = np.argmax(predictions[i])
    # Ausgabe der wahrscheinlichsten Zahl
    print(f"{categories[highest_weight_value]} -> ", end="")
    if highest_weight_value == y_test[i]:
        print(True)
    else:
        print(f"False -> {categories[y_test[i]]}")
    # Zeige Bild an
    plt.imshow(X_test[i])
    plt.show()