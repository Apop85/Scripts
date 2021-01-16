#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: recognizeNumbers.py
# Project: Deep Learning
#-----
# Created Date: Saturday 16.01.2021, 20:59
# Author: Apop85s
#-----
# Last Modified: Saturday 16.01.2021, 21:45
#-----
# Copyright (c) 2021 Apop85s
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Neurales Netzwerk welches handgeschriebene Nummern erkennen soll. 
####

import matplotlib.pyplot as plt
import tensorflow as tf
import numpy as np
from random import randint as rng

# print(tf.__version__)
mnist = tf.keras.datasets.mnist # Handgeschriebene Zahlen von 0 - 9

(x_train, y_train), (x_test, y_test) = mnist.load_data()

# Werte in x_train und x_test auf Werte zwischen 0 und 1 reduzieren (optional)
x_train = tf.keras.utils.normalize(x_train, axis=1)
x_test = tf.keras.utils.normalize(x_test, axis=1)

# Beispielbild anzeigen
# plt.imshow(x_train[0])
# plt.show()

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
model.add(tf.keras.layers.Dense(10, activation=tf.nn.softmax)) # 10 = Anzahl klassifikationen (0 - 9)

model.compile(optimizer='adam',                         # adam = Defaultoptimizer
              loss='sparse_categorical_crossentropy',   # Fehlerhafte Vorhersagen / (sparse_)categorical_entropy = default / binary_categorical_entropy = default für 2 Klassifikationen
              metrics=['accuracy'])                     # Welche Eigenschaften sollen getrackt werden? Beispiel accuracy für genauigkeit

# Modeltraining
number_of_trainings = 5
model.fit(x_train, y_train, epochs=number_of_trainings)

# Berechne Genauigkeit und Verlust
val_loss, val_acc = model.evaluate(x_test, y_test)
print("Verlust    : {}".format(val_loss))
print("Genauigkeit: {}".format(val_acc))
# Modell auf Disk speichern
model.save("recognizeNumbers.model")

# Lade modell
new_model = tf.keras.models.load_model("recognizeNumbers.model")
# Erstelle ein Prediction-Datensatz aus x_test
predictions = new_model.predict([x_test])
# Teste Zahlenerkennung
for i in range(10):
    # Zufälliges Bild auswählen
    random_choice = rng(0, len(x_test)-1)
    # Wahrscheinlichster Wert auslesen
    highest_weight_value = np.argmax(predictions[random_choice])
    # Ausgabe der wahrscheinlichsten Zahl
    print(f"{highest_weight_value} ", end="")
    # Zeige Bild an
    plt.imshow(x_test[random_choice])
    plt.show()

# input("Enter to exit")