#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: optimizeModel.py
# Project: recognizeShapesOwnData
#-----
# Created Date: Sunday 17.01.2021, 17:32
# Author: Apop85
#-----
# Last Modified: Sunday 17.01.2021, 22:32
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
# Quelle: https://www.youtube.com/watch?v=lV09_8432VA
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
# Laufanalyse-Modul importieren
import analyzeRuns

# Mögliche Density Layers (Minimum: Anzahl Kategorien)
dense_layers = [3]
# Mögliche Layergrössen
# layer_sizes = [8,16,24,32,40,48,56,64,72,80,88,96,104,112,120,128]
layer_sizes = [48,56,72,80,88,96,104,112,120,128]
# Mögliche Convolution layers
conv_layers = [1,2]
# Mögliche Loss-Algorithmen
# https://www.tensorflow.org/api_docs/python/tf/keras/losses
# loss_algorithms = ["sparse_categorical_crossentropy", "categorical_crossentropy", "binary_crossentropy", "categorical_hinge"]
loss_algorithms = ["sparse_categorical_crossentropy"]
# Mögliche Optimizer
# https://www.tensorflow.org/api_docs/python/tf/keras/optimizers
# optimizers = ["adam", "SGD", "Adadelta", "RMSprop"]
optimizers = ["adam"]
# Mögliche Aktivatoren
# https://www.tensorflow.org/api_docs/python/tf/keras/activations/swish
# activators = ["relu", "selu", "sigmoid", "swish"]
activators = ["selu", "swish"]
# Anzahl der durchzuführenden Trainings pro Datensatz
amount_of_trainings = 50
# Bildgrösse des Trainingsdatensets
training_image_size = (30, 30)


# Pfaddefinitionen
log_dir = os.path.join(".", "logs")
model_dir = os.path.join(".", "modelData")
analyzer_exec = os.path.join(".", "analyzeRuns.py")
x_file_dir = os.path.join(model_dir, "X.pickle")
y_file_dir = os.path.join(model_dir, "y.pickle")
x_test_file_dir = os.path.join(model_dir, "X_test.pickle")
y_test_file_dir = os.path.join(model_dir, "y_test.pickle")
error_log_path = os.path.join(log_dir, "error.log")
success_log_path = os.path.join(log_dir, "success.log")

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

def write_to_log(path, message):
    file_writer = open(path, "a+")
    file_writer.write(f"{message}\n")
    file_writer.close()
    
def createTrainingModel(X_data, optimizer, loss_algorithm, activator, dense_layer, layer_size, conv_layer):
    print("Create Training Model ", end="")
    # Modeltypdefinition
    model = Sequential()

    loss_initials = ""
    for item in loss_algorithm.split("_"):
        loss_initials += item[0]
    print(f"{optimizer}-{loss_initials}-{activator}-{conv_layer}-{layer_size}-{dense_layer}...", end="")
    desc = f"{optimizer}-{loss_initials}-{activator}-{conv_layer}-conv-{layer_size}-size-{dense_layer}-dense"

    model.add(Conv2D(layer_size, (3,3), input_shape=X_data.shape[1:]))
    model.add(Activation(activator))
    model.add(MaxPooling2D(pool_size=(2,2)))

    for i in range(conv_layer):
        model.add(Conv2D(layer_size, (3,3)))
        model.add(Activation(activator))
        model.add(MaxPooling2D(pool_size=(2,2)))

    # Konvertieren einer 3D-Map in ein 1D Vektor
    model.add(Flatten())

    for i in range(dense_layer):
        model.add(Dense(layer_size))
        model.add(Activation(activator))

    # --------- Output Layer -----------
    model.add(Dense(dense_layer)) # Anzahl möglicher Outputs Dreieck + Viereck + Kreis = 3
    model.add(Activation("sigmoid"))

    model.compile(optimizer=optimizer,                    # adam = Defaultoptimizer
                loss=loss_algorithm,                      # Fehlerhafte Vorhersagen / (sparse_)categorical_entropy = default / binary_crossentropy = default für 2 Klassifikationen
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

if os.path.exists(x_test_file_dir) and os.path.exists(y_test_file_dir):
    print("Loading Test Data...", end="")
    pickle_in = open(x_test_file_dir, "rb")
    X_test = pickle.load(pickle_in)
    pickle_in.close()
    pickle_in = open(y_test_file_dir, "rb")
    y_test = pickle.load(pickle_in)
    pickle_in.close()
    print("Done")
    test_data = True
else:
    test_data = False

# Iteriere durch mögliche Parametereinstellungen
for optimizer in optimizers:
    for loss_algorithm in loss_algorithms:
        for activator in activators:
            for dense_layer in dense_layers:
                for layer_size in layer_sizes:
                    for conv_layer in conv_layers:
                        timenow = time()
                        log_message = optimizer.center(33) + "|" + loss_algorithm.center(33) + "|" + activator.center(33) + "|" + str(dense_layer).center(33) + "|" + str(layer_size).center(33) + "|" + str(conv_layer).center(33) + "|" 

                        if not os.path.exists(success_log_path):
                            header = "OPTIMIZER".center(33) + "|" + "LOSS_ALG".center(33) + "|" + "ACTIVATOR".center(33) + "|" + "DENSE LAYERS".center(33) + "|" + "LAYER SIZE".center(33) + "|" + "CONVOLUTION LAYERS".center(33) + "|" + "ACCURRACY".center(33) + "|" + "LOSS".center(33) + "\n"
                            header = header + "-"*len(header)+"\n"
                            log_message = header + log_message
                        
                        try:
                            # Trainingsmodell erstellen
                            model, desc = createTrainingModel(X_train, optimizer, loss_algorithm, activator, dense_layer, layer_size, conv_layer)
                            logfile_dir = os.path.join(log_dir, f"optimizeModel-{desc}-{int(timenow)}")
                            # Tensorboard initialisieren
                            tensorboard = TensorBoard(log_dir=logfile_dir)
                            # Modell trainieren
                            model.fit(X_train, y_train, batch_size=32, epochs=amount_of_trainings, validation_split=0.3, callbacks=[tensorboard])
                            # Use test-data if avaiable
                            # if test_data:
                                # val_loss, val_acc = model.evaluate(X_test, y_test)
                            # else:
                            val_loss, val_acc = model.evaluate(X_train, y_train)
                            log_message  += f"{val_acc}".center(33) + "|" + f"{val_loss}".center(33)
                            write_to_log(success_log_path, log_message)
                        except Exception as error_message:
                            log_message = optimizer.center(33) + "|" + loss_algorithm.center(33) + "|" + activator.center(33) + "|" + str(dense_layer).center(33) + "|" + str(layer_size).center(33) + "|" + str(conv_layer).center(33) + "|" 
                            if not os.path.exists(error_log_path):
                                header = "OPTIMIZER".center(33) + "|" + "LOSS_ALG".center(33) + "|" + "ACTIVATOR".center(33) + "|" + "DENSE LAYERS".center(33) + "|" + "LAYER SIZE".center(33) + "|" + "CONVOLUTION LAYERS".center(33) + "|" + "ERROR".center(33) + "\n"
                                header = header + "-"*len(header)+"\n"
                                log_message = header + log_message
                            print(f"Cancelled")
                            error_out = f"Aborted with arguments: {optimizer}-{loss_algorithm}-{activator}-{dense_layer}-{layer_size}-{conv_layer}-{int(timenow)} due to error."
                            log_message += "--".join(str(error_message).split("\n"))
                            write_to_log(error_log_path, log_message)

                            print(error_out)

if os.path.exists(success_log_path):
    # Start log analysis
    analyzeRuns.analyzeRuns(20)