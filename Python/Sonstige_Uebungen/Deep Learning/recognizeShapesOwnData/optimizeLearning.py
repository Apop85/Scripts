#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: optimizeLearning.py
# Project: NN
#-----
# Created Date: Sunday 17.01.2021, 11:25
# Author: Apop85
#-----
# Last Modified: Sunday 17.01.2021, 13:24
#-----
# Copyright (c) 2021 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description:  Mit diesem Script kann versucht werden die effizienz des neueronalen Netzwerk zu verbessern
#               Benötigt werden folgende Daten:
#               --> Trainingsmodell im Ordner "modelData"
#               --> X.pickle im Ordner "modelData"
#               --> y.pickle im Ordner "modelData"
#               Starten des WebUIs --> cmd --> tensorboard --logdir=<PFAD ZUM LOG-ORDNER>
# Quelle: https://www.youtube.com/watch?v=BqgTU7_cBnk
####

import os
# Unterdrücke Debug-Nachrichten
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3' 
# Tensorflow-Module für das Trainieren des neuronalen Netzwerks
import tensorflow as tf
from tensorflow.keras.datasets import cifar10
from tensorflow.keras.preprocessing.image import ImageDataGenerator
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import Dense, Dropout, Activation, Flatten
from tensorflow.keras.layers import Conv2D, MaxPooling2D
# Tensorboard-Modul zur Anzeige der Daten auf einer Web-UI
from tensorflow.keras.callbacks import TensorBoard
# Pickle-Modul zum Laden des Trainingssets
import pickle
# Time-Module um zeitlich getrennte Logs zu erstellen
from time import time

# Anzahl der durchzuführenden Trainings pro Datensatz
amount_of_trainings = 50
# Bildgrösse des Trainingsdatensets
training_image_size = (30, 30)
# Anzahl verfügbarer Trainingsmodelle:
avaiable_models = 4
# Definiere verwendetes Trainingsmodell
#   model_switch = 0 => Originalmodell (2x128 default)
#   model_switch = 1 => Angepasstes Modell 1 - 64x3
#   model_switch = 2 => Angepasstes Modell 2 - 64x2
#   model_switch = 3 => Angepasstes Modell 3 - 64x4
#   model_switch = -1 => Alle Modelle
used_model = -1

# Pfaddefinitionen
log_dir = os.path.join(".", "logs")
model_dir = os.path.join(".", "modelData")
x_file_dir = os.path.join(model_dir, "X.pickle")
y_file_dir = os.path.join(model_dir, "y.pickle")

# Change working directory into script directory
os.chdir(os.path.dirname(__file__))

def loadPickleData(path):
    # Funktion um pickle-Daten einzulesen
    # Input:
    #   path = string | Pfad zu einer Datei
    if os.path.exists(path):
        pickle_in = open(path, "rb")
        data = pickle.load(pickle_in)
        pickle_in.close()
        return data
    else:
        raise Exception(f"Path {path} is invalid!")

def createTrainingModel(X_data, model_switch=0):
    print("Create Training Model ", end="")
    # Modeltypdefinition
    model = Sequential()

    if model_switch == 0:
        # ############################################################################################
        # ################################# MODELL 1 - ORIGINAL ######################################
        # ############################################################################################
        print("(2x128 - default)...", end="")
        desc = "2x128 - default"

        # Konvertieren einer 3D-Map in ein 1D Vektor
        model.add(Flatten())


        # activation: Was lässt das Neuron auslösen 
        #   relu = lineares gleichrichten -> Default Aktivierungsfunktion
        #   softmax = Wahrscheinlichkeitsverteilung (Wie wahrscheinlich ist die Zahl auf dem Bild eine 1,2,3...) -> Default Outputfunkt 
        model.add(Dense(128, activation=tf.nn.relu)) # 64 + 64 --> Anzahl Neuronen --> Unsichtbar
        model.add(Dense(128, activation=tf.nn.relu)) # 64 + 64 --> Anzahl Neuronen --> Unsichtbar
        # Outputlayer
        model.add(Dense(3, activation=tf.nn.softmax)) # 3 = Anzahl klassifikationen (0 - 9)

        model.compile(optimizer='adam',                       # adam = Defaultoptimizer
                    loss='sparse_categorical_crossentropy',   # Fehlerhafte Vorhersagen / (sparse_)categorical_entropy = default / binary_crossentropy = default für 2 Klassifikationen
                    metrics=['accuracy'])                     # Welche Eigenschaften sollen getrackt werden? Beispiel accuracy für genauigkeit
    
    elif model_switch == 1:
        # ############################################################################################
        # ################################# MODELL 2 - ANGEPASST 1 ###################################
        # ############################################################################################
        print("(3x64)...", end="")
        desc = "3x64"

        # Neuronale Layer
        # ------------ Layer 1 -------------
        model.add(Conv2D(64, (3,3), input_shape=X_data.shape[1:]))
        model.add(Activation("relu"))
        model.add(MaxPooling2D(pool_size=(2,2)))

        # ------------ Layer 2 -------------
        model.add(Conv2D(64, (3,3)))
        model.add(Activation("relu"))
        model.add(MaxPooling2D(pool_size=(2,2)))

        # Konvertieren einer 3D-Map in ein 1D Vektor
        model.add(Flatten())

        # ------------ Layer 3 -------------
        model.add(Dense(64))
        model.add(Activation("relu"))

        # --------- Output Layer -----------
        model.add(Dense(3)) # Anzahl möglicher Outputs Dreieck + Viereck + Kreis = 3
        model.add(Activation("sigmoid"))

        model.compile(optimizer='adam',                       # adam = Defaultoptimizer
                    loss='sparse_categorical_crossentropy',   # Fehlerhafte Vorhersagen / (sparse_)categorical_entropy = default / binary_crossentropy = default für 2 Klassifikationen
                    metrics=['accuracy'])                     # Welche Eigenschaften sollen getrackt werden? Beispiel accuracy für genauigkeit
    elif model_switch == 2:
        # ############################################################################################
        # ################################# MODELL 3 - ANGEPASST 2 ###################################
        # ############################################################################################
        print("(2x64)...", end="")
        desc = "2x64"

        # Neuronale Layer
        # ------------ Layer 1 -------------
        model.add(Conv2D(64, (3,3), input_shape=X_data.shape[1:]))
        model.add(Activation("relu"))
        model.add(MaxPooling2D(pool_size=(2,2)))

        # ------------ Layer 2 -------------
        model.add(Conv2D(64, (3,3)))
        model.add(Activation("relu"))
        model.add(MaxPooling2D(pool_size=(2,2)))

        # Konvertieren einer 3D-Map in ein 1D Vektor
        model.add(Flatten())

        # --------- Output Layer -----------
        model.add(Dense(3)) # Anzahl möglicher Outputs Dreieck + Viereck + Kreis = 3
        model.add(Activation("sigmoid"))

        model.compile(optimizer='adam',                       # adam = Defaultoptimizer
                    loss='sparse_categorical_crossentropy',   # Fehlerhafte Vorhersagen / (sparse_)categorical_entropy = default / binary_crossentropy = default für 2 Klassifikationen
                    metrics=['accuracy'])                     # Welche Eigenschaften sollen getrackt werden? Beispiel accuracy für genauigkeit
    
    elif model_switch == 3:
        # ############################################################################################
        # ################################# MODELL 4 - ANGEPASST 3 ###################################
        # ############################################################################################
        print("(4x64)...", end="")
        desc = "4x64"

        # Neuronale Layer
        # ------------ Layer 1 -------------
        model.add(Conv2D(64, (3,3), input_shape=X_data.shape[1:]))
        model.add(Activation("relu"))
        model.add(MaxPooling2D(pool_size=(2,2)))

        # ------------ Layer 2 -------------
        model.add(Conv2D(64, (3,3)))
        model.add(Activation("relu"))
        model.add(MaxPooling2D(pool_size=(2,2)))

        # Konvertieren einer 3D-Map in ein 1D Vektor
        model.add(Flatten())

        # ------------ Layer 3 -------------
        model.add(Dense(64))
        model.add(Activation("relu"))
        
        # ------------ Layer 4 -------------
        model.add(Dense(64))
        model.add(Activation("relu"))

        # --------- Output Layer -----------
        model.add(Dense(3)) # Anzahl möglicher Outputs Dreieck + Viereck + Kreis = 3
        model.add(Activation("sigmoid"))

        model.compile(optimizer='adam',                       # adam = Defaultoptimizer
                    loss='sparse_categorical_crossentropy',   # Fehlerhafte Vorhersagen / (sparse_)categorical_entropy = default / binary_crossentropy = default für 2 Klassifikationen
                    metrics=['accuracy'])                     # Welche Eigenschaften sollen getrackt werden? Beispiel accuracy für genauigkeit


    print("Done")
    return model, desc

# Gespeicherte Trainingsdaten laden
print("Loading Training Data...", end="")
X_train = loadPickleData(x_file_dir)
# X_train = tf.keras.utils.normalize(X_train)
y_train = loadPickleData(y_file_dir)
print("Done")

# Erstelle log-pfad
if not os.path.exists(log_dir):
    os.mkdir(log_dir)

if used_model == -1:
    for used_model in range(avaiable_models):
        # Trainingsmodell erstellen
        model, desc = createTrainingModel(X_train, model_switch=used_model)
        logfile_dir = os.path.join(log_dir, f"Squares-Triangles-Circles-CNN-{desc}-{int(time())}")
        # Tensorboard initialisieren
        tensorboard = TensorBoard(log_dir=logfile_dir)

        # Modell trainieren
        model.fit(X_train, y_train, batch_size=32, epochs=amount_of_trainings, validation_split=0.3, callbacks=[tensorboard])
else:
    # Trainingsmodell erstellen
    model, desc = createTrainingModel(X_train, model_switch=used_model)
    logfile_dir = os.path.join(log_dir, f"Squares-Triangles-Circles-CNN-{desc}-{int(time())}")
    # Tensorboard initialisieren
    tensorboard = TensorBoard(log_dir=logfile_dir)

    # Modell trainieren
    model.fit(X_train, y_train, batch_size=32, epochs=amount_of_trainings, validation_split=0.3, callbacks=[tensorboard])