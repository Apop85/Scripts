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

#  _______  _______ ___________________________ _        _______  _______ 
# (  ____ \(  ____ \\__   __/\__   __/\__   __/( (    /|(  ____ \(  ____ \
# | (    \/| (    \/   ) (      ) (      ) (   |  \  ( || (    \/| (    \/
# | (_____ | (__       | |      | |      | |   |   \ | || |      | (_____ 
# (_____  )|  __)      | |      | |      | |   | (\ \) || | ____ (_____  )
#       ) || (         | |      | |      | |   | | \   || | \_  )      ) |
# /\____) || (____/\   | |      | |   ___) (___| )  \  || (___) |/\____) |
# \_______)(_______/   )_(      )_(   \_______/|/    )_)(_______)\_______)
# Bildgrösse des Trainingsdatensets
training_image_size = (30, 30)
# Mögliche Density Layers (Minimum: Anzahl Kategorien)
dense_layers = [0,1,2,3]
# Mögliche Layergrössen
# layer_sizes = [8,16,24,32,40,48,56,64,72,80,88,96,104,112,120,128]
layer_sizes = [32,64,128]
# Mögliche Convolution layers
# conv_layers = [0,1,2]
conv_layers = [0,1,2]
# Mögliche Loss-Algorithmen
# https://www.tensorflow.org/api_docs/python/tf/keras/losses
# loss_algorithms = ["sparse_categorical_crossentropy", "categorical_crossentropy", "binary_crossentropy", "categorical_hinge"]
loss_algorithms = ["sparse_categorical_crossentropy"]
# Mögliche Optimizer
# https://www.tensorflow.org/api_docs/python/tf/keras/optimizers
# optimizers = ["adam", "SGD", "Adadelta", "RMSprop"]
optimizers = ["adam"]
# Mögliche Conv2D Aktivatoren
# https://www.tensorflow.org/api_docs/python/tf/keras/activations
# activators = ["sigmoid", "relu", "selu", , "swish", "softmax", "softplus", "softsign"]
activators = ["relu", "selu"]
# Entscheidungsaktivatoren
# https://www.tensorflow.org/api_docs/python/tf/keras/activations
# decision_activators = ["sigmoid", "relu", "selu", , "swish", "softmax", "softplus", "softsign"]
decision_activators = ["sigmoid"]
# Dense Aktivatoren
# https://www.tensorflow.org/api_docs/python/tf/keras/activations
# decision_activators = ["sigmoid", "relu", "selu", , "swish", "softmax", "softplus", "softsign"]
dense_activators = ["softmax", "softplus"]
# Matrixgrösse
kernel_sizes = [2,3,4]
# Anzahl der durchzuführenden Trainings pro Datensatz
amount_of_trainings = 50
# Anzahl der zur Validierung zu verwendenden Daten in %
validation_percent = 20
# Stapelgrösse der zu trainierenden Daten
batch_size = 64

#  ____   __   ____  _   _  ___ 
# (  _ \ /__\ (_  _)( )_( )/ __)
#  )___//(__)\  )(   ) _ ( \__ \
# (__) (__)(__)(__) (_) (_)(___/
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






#  _______           _        _______ __________________ _______  _        _______ 
# (  ____ \|\     /|( (    /|(  ____ \\__   __/\__   __/(  ___  )( (    /|(  ____ \
# | (    \/| )   ( ||  \  ( || (    \/   ) (      ) (   | (   ) ||  \  ( || (    \/
# | (__    | |   | ||   \ | || |         | |      | |   | |   | ||   \ | || (_____ 
# |  __)   | |   | || (\ \) || |         | |      | |   | |   | || (\ \) |(_____  )
# | (      | |   | || | \   || |         | |      | |   | |   | || | \   |      ) |
# | )      | (___) || )  \  || (____/\   | |   ___) (___| (___) || )  \  |/\____) |
# |/       (_______)|/    )_)(_______/   )_(   \_______/(_______)|/    )_)\_______)
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
    # Funktion um einen String in ein Logfile zu schreiben
    # Inputs:
    #   path = string | Pfad zum Logfile
    #   message = string | Nachricht
    file_writer = open(path, "a+")
    file_writer.write(f"{message}\n")
    file_writer.close()
    
def createTrainingModel(X_data, optimizer, loss_algorithm, activator, dense_layer, layer_size, conv_layer, categories, decision_activator, dense_activator, kernel_size):
    # Funktion zum erstellen eines Trainingmodels
    # Inputs:
    #   X_data = numpy array        |  Trainingsdaten
    #   optimizer = string          |  Verwendeter Optimizer 
    #   loss_algorithm = string     | Algorithmus für die Verlustrate
    #   activator = string          | Aktivierungsalgorithmus
    #   dense_layer = integer       | Anzahl Denselayer
    #   layer_size = integer        | Grösse des Layers
    #   conv_layer = integer        | Anzahl Convolutionlayer
    #   categories                  | Anzahl möglicher Antworten/Kategorien
    #   decision_activator = string | Entscheidungsaktivator
    #   dense_activator = string    | Aktivator für die Dense layer
    #   kernel_size = integer       | Kernelgrösse

    print("Create Training Model ", end="")
    # Modeltypdefinition
    model = Sequential()

    # Erstelle Modellbeschreibung
    loss_initials = ""
    for item in loss_algorithm.split("_"):
        loss_initials += item[0]
    print(f"{optimizer}-{kernel_size}-{decision_activator}-{loss_initials}-{activator}-{conv_layer}-{layer_size}-{dense_layer}-{dense_activator}...", end="")
    desc = f"{optimizer}-{kernel_size}-{decision_activator}-{loss_initials}-{activator}-{conv_layer}-conv-{layer_size}-size-{dense_layer}-dense-{dense_activator}"

    # Initiallayer hinzufügen
    model.add(Conv2D(layer_size, (kernel_size,kernel_size), input_shape=X_data.shape[1:]))
    model.add(Activation(activator))
    model.add(MaxPooling2D(pool_size=(2,2)))

    # Convolutionlayer hinzufügen
    for i in range(conv_layer):
        model.add(Conv2D(layer_size, (kernel_size,kernel_size)))
        model.add(Activation(activator))
        model.add(MaxPooling2D(pool_size=(2,2)))

    # Konvertieren einer 3D-Map in ein 1D Vektor
    model.add(Flatten())

    # Denselayer hinzufügen
    for i in range(dense_layer):
        model.add(Dense(layer_size))
        model.add(Activation(dense_activator))

    # Entscheidungslayer hinzufügen
    model.add(Dense(categories)) # Anzahl möglicher Outputs Dreieck + Viereck + Kreis = 3
    model.add(Activation(decision_activator))

    # Kompilliere Modell
    model.compile(optimizer=optimizer,                    # adam = Defaultoptimizer
                loss=loss_algorithm,                      # Fehlerhafte Vorhersagen / (sparse_)categorical_entropy = default / binary_crossentropy = default für 2 Klassifikationen
                metrics=['accuracy'])                     # Welche Eigenschaften sollen getrackt werden? Beispiel accuracy für genauigkeit

    print("Done")
    return model, desc


# _________ _       ___________________________ _______  _       _________ _______  _______ 
# \__   __/( (    /|\__   __/\__   __/\__   __/(  ___  )( \      \__   __// ___   )(  ____ \
#    ) (   |  \  ( |   ) (      ) (      ) (   | (   ) || (         ) (   \/   )  || (    \/
#    | |   |   \ | |   | |      | |      | |   | (___) || |         | |       /   )| (__    
#    | |   | (\ \) |   | |      | |      | |   |  ___  || |         | |      /   / |  __)   
#    | |   | | \   |   | |      | |      | |   | (   ) || |         | |     /   /  | (      
# ___) (___| )  \  |___) (___   | |   ___) (___| )   ( || (____/\___) (___ /   (_/\| (____/\
# \_______/|/    )_)\_______/   )_(   \_______/|/     \|(_______/\_______/(_______/(_______/
# Change working directory into script directory
os.chdir(os.path.dirname(__file__))

# Erstelle log-pfad
if not os.path.exists(log_dir):
    os.mkdir(log_dir)

validation_percent /= 100

#   _                     _       _       _        
#  | |                   | |     | |     | |       
#  | |     ___   __ _  __| |   __| | __ _| |_ __ _ 
#  | |    / _ \ / _` |/ _` |  / _` |/ _` | __/ _` |
#  | |___| (_) | (_| | (_| | | (_| | (_| | || (_| |
#  |______\___/ \__,_|\__,_|  \__,_|\__,_|\__\__,_|
# Gespeicherte Trainingsdaten laden
print("Loading Training Data...", end="")
X_train = loadPickleData(x_file_dir)
y_train = loadPickleData(y_file_dir)
# Anzahl Kategorien auslesen
amount_of_categories = max(y_train) + 1
print("Done")

# Check if test-data can be loaded
if os.path.exists(x_test_file_dir) and os.path.exists(y_test_file_dir):
    # Load test data
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





#   __  __           _      _     _ _                 _   _             
#  |  \/  |         | |    | |   (_) |               | | (_)            
#  | \  / | ___   __| | ___| |    _| |_ ___ _ __ __ _| |_ _  ___  _ __  
#  | |\/| |/ _ \ / _` |/ _ \ |   | | __/ _ \ '__/ _` | __| |/ _ \| '_ \ 
#  | |  | | (_) | (_| |  __/ |   | | ||  __/ | | (_| | |_| | (_) | | | |
#  |_|  |_|\___/ \__,_|\___|_|   |_|\__\___|_|  \__,_|\__|_|\___/|_| |_|
# Iteriere durch mögliche Parametereinstellungen
for optimizer in optimizers:
    for kernel_size in kernel_sizes:
        for decision_activator in decision_activators:
            for loss_algorithm in loss_algorithms:
                for activator in activators:
                    for dense_layer in dense_layers:
                        for layer_size in layer_sizes:
                            for conv_layer in conv_layers:
                                for dense_activator in dense_activators:
                                    timenow = time()

                                    # Log-Nachricht erstellen
                                    log_message = optimizer.center(33) + "|" + (str(kernel_size)+"x"+str(kernel_size)).center(33) + "|" + decision_activator.center(33) + "|" + loss_algorithm.center(33) + "|" + activator.center(33) + "|" + str(dense_layer).center(33) + "|" + dense_activator.center(33) + "|" + str(layer_size).center(33) + "|" + str(conv_layer).center(33) + "|" 
                                    if not os.path.exists(success_log_path):
                                        # Wenn Logfile noch nicht existiert header hinzufügen
                                        header = "OPTIMIZER".center(33) + "|" + "KERNEL SIZE".center(33) + "|" + "DECISION ACTIVATOR".center(33) + "|" + "LOSS_ALG".center(33) + "|" + "ACTIVATOR".center(33) + "|" + "DENSE LAYERS".center(33) + "|" + "DENSE ACTIVATOR".center(33) + "|" + "LAYER SIZE".center(33) + "|" + "CONVOLUTION LAYERS".center(33) + "|" + "ACCURRACY".center(33) + "|" + "LOSS".center(33) + "\n"
                                        header = header + "-"*len(header)+"\n"
                                        log_message = header + log_message
                                    
                                    try:
                                        # Trainingsmodell erstellen
                                        model, desc = createTrainingModel(X_train, optimizer, loss_algorithm, activator, dense_layer, layer_size, conv_layer, amount_of_categories, decision_activator, dense_activator, kernel_size)
                                        logfile_dir = os.path.join(log_dir, f"optimizeModel-{desc}-{int(timenow)}")
                                        # Tensorboard initialisieren
                                        tensorboard = TensorBoard(log_dir=logfile_dir)
                                        # Modell trainieren
                                        model.fit(X_train, y_train, batch_size=batch_size, epochs=amount_of_trainings, validation_split=validation_percent, callbacks=[tensorboard])
                                        # Wenn möglich seperate testdaten verwenden
                                        if test_data:
                                            val_loss, val_acc = model.evaluate(X_test, y_test)
                                        else:
                                            val_loss, val_acc = model.evaluate(X_train, y_train)
                                        log_message  += f"{val_acc}".center(33) + "|" + f"{val_loss}".center(33)
                                        write_to_log(success_log_path, log_message)
                                    except Exception as error_message:
                                        # Errorzusammenfassung generieren
                                        log_message = optimizer.center(33) + "|" + (str(kernel_size)+"x"+str(kernel_size)).center(33) + "|" + decision_activator.center(33) + "|" + loss_algorithm.center(33) + "|" + activator.center(33) + "|" + str(dense_layer).center(33) + "|" + dense_activator.center(33) + "|" + str(layer_size).center(33) + "|" + str(conv_layer).center(33) + "|" 
                                        if not os.path.exists(error_log_path):
                                            # Wenn Logfile noch nicht existiert header hinzufügen
                                            header = "OPTIMIZER".center(33) + "|" + "KERNEL SIZE".center(33) + "|" + "DECISION ACTIVATOR".center(33) + "|" + "LOSS_ALG".center(33) + "|" + "ACTIVATOR".center(33) + "|" + "DENSE LAYERS".center(33) + "|" + "DENSE ACTIVATOR".center(33) + "|" + "LAYER SIZE".center(33) + "|" + "CONVOLUTION LAYERS".center(33) + "|" + "ERROR".center(33) + "\n"
                                            header = header + "-"*len(header)+"\n"
                                            log_message = header + log_message
                                        print(f"Cancelled")
                                        error_out = f"Aborted with arguments: {optimizer}-{decision_activator}-{loss_algorithm}-{activator}-{dense_layer}-{layer_size}-{conv_layer}-{int(timenow)} due to error."
                                        log_message += "--".join(str(error_message).split("\n"))
                                        write_to_log(error_log_path, log_message)

                                        print(error_out)


#  __    _____  ___      __    _  _    __    __   _  _  ___  ____  ___ 
# (  )  (  _  )/ __)    /__\  ( \( )  /__\  (  ) ( \/ )/ __)(_  _)/ __)
#  )(__  )(_)(( (_-.   /(__)\  )  (  /(__)\  )(__ \  / \__ \ _)(_ \__ \
# (____)(_____)\___/  (__)(__)(_)\_)(__)(__)(____)(__) (___/(____)(___/         
if os.path.exists(success_log_path):
    # Start log analysis
    analyzeRuns.analyzeRuns(20)