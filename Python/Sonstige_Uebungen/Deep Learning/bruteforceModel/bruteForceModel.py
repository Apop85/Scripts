#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: bruteForceModel.py
# Project: NN
#-----
# Created Date: Wednesday 20.01.2021, 19:52
# Author: Apop85 
# #-----
# Last Modified: Wednesday 20.01.2021, 19:52
#-----
# Copyright (c) 2021 Apop85 
# # This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description:  Script zum bruteforcen der optimalen Einstellungen für ein vorhandenes Trainingsmodell
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
# Zeitmodul zur Zeitmessung
from time import time

#  _______  _______ ___________________________ _        _______  _______ 
# (  ____ \(  ____ \\__   __/\__   __/\__   __/( (    /|(  ____ \(  ____ \
# | (    \/| (    \/   ) (      ) (      ) (   |  \  ( || (    \/| (    \/
# | (_____ | (__       | |      | |      | |   |   \ | || |      | (_____ 
# (_____  )|  __)      | |      | |      | |   | (\ \) || | ____ (_____  )
#       ) || (         | |      | |      | |   | | \   || | \_  )      ) |
# /\____) || (____/\   | |      | |   ___) (___| )  \  || (___) |/\____) |
# \_______)(_______/   )_(      )_(   \_______/|/    )_)(_______)\_______)
# Modelname - Beeinflusst Name der Tensorflow- und Script-Logs
model_name = "bruteforceModel"
# Anzahl der durchzuführenden Trainings pro Datensatz
amount_of_trainings = 2
# Anzahl der zur Validierung zu verwendenden Daten in %
validation_percent = 30
# Stapelgrösse der zu trainierenden Daten pro Durchgang
batch_size = 64
# Bildgrösse des Trainingsdatensets (nicht der eigentlichen Bilder) -> recognizeShape erstellt ein 30x30  Bild
training_image_size = (30, 30)
# Max error-score -> Sobald ein Parameter diese Anzahl an errors auslöst wird es ignoriert
ignore_error_score = 5
# Soll Accuracy, Loss oder beides priorisiert werden?
priorize = [
    "accuracy",
    "loss"
]

#  __  __  _____  ____  ____  __      ___  ____  ____  ____  ____  _  _  ___  ___ 
# (  \/  )(  _  )(  _ \( ___)(  )    / __)( ___)(_  _)(_  _)(_  _)( \( )/ __)/ __)
#  )    (  )(_)(  )(_) ))__)  )(__   \__ \ )__)   )(    )(   _)(_  )  (( (_-.\__ \
# (_/\/\_)(_____)(____/(____)(____)  (___/(____) (__)  (__) (____)(_)\_)\___/(___/
# Mögliche Density Layers
# dense_layers = [0,1,2,3]
dense_layers = [2,3]
# Mögliche Layergrössen
# layer_sizes = [8,16,32,64,128]
layer_sizes = [32,64,128]
# Mögliche Convolution layers
# conv_layers = [0,1,2]
conv_layers = [1,2]
# Mögliche Loss-Algorithmen
# https://www.tensorflow.org/api_docs/python/tf/keras/losses
# loss_algorithms = ["sparse_categorical_crossentropy", "categorical_crossentropy", "binary_crossentropy", "categorical_hinge"]
loss_algorithms = ["sparse_categorical_crossentropy", "categorical_crossentropy"]
# Mögliche Optimizer
# https://www.tensorflow.org/api_docs/python/tf/keras/optimizers
# optimizers = ["adam", "SGD", "Adadelta", "RMSprop"]
optimizers = ["adam"]
# Mögliche Conv2D Aktivatoren
# https://www.tensorflow.org/api_docs/python/tf/keras/activations
# activators = ["relu", "selu", "sigmoid", "swish", "softmax", "softplus", "softsign"]
activators = ["relu", "selu", "swish"]
# Dense Aktivatoren
# https://www.tensorflow.org/api_docs/python/tf/keras/activations
# decision_activators = ["softmax", "softplus", "softsign", "sigmoid", "relu", "selu", "swish"]
dense_activators = ["softplus", "softsign", "softmax"]
# Entscheidungsaktivatoren
# https://www.tensorflow.org/api_docs/python/tf/keras/activations
decision_activators = ["sigmoid"] # ist immer eine Sigmoid-Funktion um Werte zwischen 0 und 1 zu erzeugen
# Matrixgrösse - Wie gross soll der Bildausschnitt sein der jeweils analysiert werden soll?
# kernel_sizes = [2,3,4,5]
kernel_sizes = [2,4,6]

#  ____   __   ____  _   _  ___ 
# (  _ \ /__\ (_  _)( )_( )/ __)
#  )___//(__)\  )(   ) _ ( \__ \
# (__) (__)(__)(__) (_) (_)(___/
# Change working directory into script directory
os.chdir(os.path.dirname(__file__))

# Pfaddefinitionen
log_dir = os.path.join(".", "logs")
model_dir = os.path.join(".", "modelData")
analyzer_exec = os.path.join(".", "analyzeRuns.py")
x_file_dir = os.path.join(model_dir, "X.pickle")
y_file_dir = os.path.join(model_dir, "y.pickle")
x_test_file_dir = os.path.join(model_dir, "X_test.pickle")
y_test_file_dir = os.path.join(model_dir, "y_test.pickle")
final_acc_dir = os.path.join(model_dir, f"mostAccModel-{model_name}")
final_loss_dir = os.path.join(model_dir, f"lestLossModel-{model_name}")


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
    
def createTrainingModel(X_data, optimizer, kernel_size, decision_activator, loss_algorithm, activator, dense_layer, dense_activator, layer_size, conv_layer, categories):
    
    # Funktion zum erstellen eines Trainingmodels
    # Inputs:
    #   X_data = numpy array        |  Trainingsdaten
    #   optimizer = string          |  Verwendeter Optimizer 
    #   loss_algorithm = string     | Algorithmus für die Verlustrate
    #   activator = string          | Aktivierungsalgorithmus
    #   dense_layer = integer       | Anzahl Denselayer
    #   layer_size = integer        | Grösse des Layers
    #   conv_layer = integer        | Anzahl Convolutionlayer
    #   categories = integer        | Anzahl möglicher Antworten/Kategorien
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
    print(f"OP={optimizer}-K={kernel_size}-DA={decision_activator}-LI={loss_initials}-A={activator}-CL={conv_layer}-LS={layer_size}-DL={dense_layer}-DnA={dense_activator}...", end="")
    desc = f"OP={optimizer}-K={kernel_size}-DA={decision_activator}-LI={loss_initials}-A={activator}-CL={conv_layer}-LS={layer_size}-DL={dense_layer}-DnA={dense_activator}"

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


def printMessageBox(strings):
    print("█" * 80)
    for string in strings:
        print("█" + string.center(80-2) + "█")
    print("█" * 80)

def printParameters(parameter_list):
    print("╔" + "═" * 78 + "╗")
    for key in parameter_list.keys():
        output = f"║ {key}".ljust(25) + ": " + f"{parameter_list[key]}".ljust(52) + "║"
        print(output)
    print("╚" + "═" * 78 + "╝")


def jobListGenerator(dense_layers, layer_sizes, conv_layers, loss_algorithms, optimizers, activators, decision_activators, dense_activators, kernel_sizes, returnArray=[]):
    for optimizer in optimizers:
        for kernel_size in kernel_sizes:
            for decision_activator in decision_activators:
                for loss_algorithm in loss_algorithms:
                    for activator in activators:
                        for dense_layer in dense_layers:
                            for dense_activator in dense_activators:
                                for layer_size in layer_sizes:
                                    for conv_layer in conv_layers:
                                        parameters = {
                                            "Optimizer" : optimizer,
                                            "Loss algorithm": loss_algorithm,
                                            "Kernel size": f"{kernel_size}x{kernel_size}",
                                            "Conv Layer Size": conv_layer,
                                            "Conv Activator": activator,
                                            "Dense Layer Size": layer_size,
                                            "Dense Activator": dense_activator,
                                            "Decision Activator": decision_activator
                                        }
                                        printParameters(parameters)
                                        yield [optimizer, kernel_size, decision_activator, loss_algorithm, activator, dense_layer, dense_activator, layer_size, conv_layer]

def runJob(job, model_name, training_amount, save_model=False, model_path=""):
    timenow = time()
    model, desc = createTrainingModel(X_train, job[0], job[1], job[2], job[3], job[4], job[5], job[6], job[7], job[8], amount_of_categories)
    logfile_dir = os.path.join(log_dir, f"{model_name}-{desc}-{int(timenow)}")
    # Tensorboard initialisieren
    tensorboard = TensorBoard(log_dir=logfile_dir)
    # Trainiere modell
    model.fit(X_train, y_train, batch_size=batch_size, epochs=amount_of_trainings, validation_split=validation_percent, callbacks=[tensorboard])
    # Evaluiere mit Testdaten falls verfügbar
    if test_data:
        loss, acc = model.evaluate(X_test, y_test)
    else:
        loss, acc = model.evaluate(X_train, y_train)

    # Speichere Trainingsmodell ab falls gewünscht
    if save_model and model_path != "":
        print(f"Saving model {model_name}...", end="")
        try:
            model.save(model_path)
            print("Done")
        except Exception:
            print("Failed!")
    elif save_model and model_path == "":
        raise Exception("MISSING MODEL PATH")

    return acc, loss

def updateScoreList(best_runs, acc, loss, job, top_list_size):
    # Prüfe ob Accuracy und loss einen bestimmten Wert erreichen
    if acc > (1 / amount_of_categories) * 1.1 and loss < 1:
        for priority in best_runs.keys():
            # Wenn liste noch zu kurz ist, füge eintrag ohne weitere Prüfung hinzu
            if len(best_runs[priority].keys()) < top_list_size:
                if priority == "accuracy":
                    best_runs[priority].setdefault(acc, job)
                elif priority == "loss":
                    best_runs[priority].setdefault(loss, job)
            else:
                # Füge neuen Wert hinzu und Werfe schlechtesten Wert pro Eigenschaft raus
                sorted_list = []
                if priority == "accuracy":
                    best_runs[priority].setdefault(acc, job)
                    for acc in best_runs[priority].keys():
                        sorted_list += [acc]
                    # Sortiere key-Liste
                    sorted_list.sort()
                    # Lösche schlechtesten Eintrag
                    del best_runs[priority][sorted_list[0]]

                elif priority == "loss":
                    best_runs[priority].setdefault(loss, job)
                    for loss in best_runs[priority].keys():
                        sorted_list += [loss]
                    # Sortiere key-Liste
                    sorted_list.sort(reverse=True)
                    # Lösche schlechtesten Eintrag
                    del best_runs[priority][sorted_list[0]]
    
    return best_runs
            

# _________ _       ___________________________ _______  _       _________ _______  _______ 
# \__   __/( (    /|\__   __/\__   __/\__   __/(  ___  )( \      \__   __// ___   )(  ____ \
#    ) (   |  \  ( |   ) (      ) (      ) (   | (   ) || (         ) (   \/   )  || (    \/
#    | |   |   \ | |   | |      | |      | |   | (___) || |         | |       /   )| (__    
#    | |   | (\ \) |   | |      | |      | |   |  ___  || |         | |      /   / |  __)   
#    | |   | | \   |   | |      | |      | |   | (   ) || |         | |     /   /  | (      
# ___) (___| )  \  |___) (___   | |   ___) (___| )   ( || (____/\___) (___ /   (_/\| (____/\
# \_______/|/    )_)\_______/   )_(   \_______/|/     \|(_______/\_______/(_______/(_______/

# Erstelle log-pfad
if not os.path.exists(log_dir):
    os.mkdir(log_dir)

# validation_split berechnen
validation_percent /= 100
# Iterationscounter setzen
round_counter = 1
isFirstRun = True



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

# Prüfe ob Testdaten geladen werden können
if os.path.exists(x_test_file_dir) and os.path.exists(y_test_file_dir):
    try:
        # Test daten laden
        print("Loading Test Data...", end="")
        X_test = loadPickleData(x_test_file_dir)
        y_test = loadPickleData(y_test_file_dir)
        print("Done")
        test_data = True
    except Exception:
        print("Failed")
        test_data = False
else:
    test_data = False


while True:
    # Zeitmessungstabelle erstellen
    time_table = {
        "timeStamp": time(), 
        "deltaTotal": 0, 
        "medTime": 0, 
        "minutesLeft": "N/A"}

    # Iterationscounter zurücksetzen
    iteration_counter = 0

    # Pfad zu den Logfiles und deren Namen definieren
    used_model_name = f"{round_counter}_{model_name}"
    error_log_path = os.path.join(log_dir, f"error-brute-{round_counter}_{model_name}.log")
    success_log_path = os.path.join(log_dir, f"success-brute-{round_counter}_{model_name}.log")
    best_log_path = os.path.join(log_dir, f"best-brute-{round_counter}_{model_name}.log")
    best_last_log_path = os.path.join(log_dir, f"best-brute-{round_counter-1}_{model_name}.log")

    # Tabelle für beste Läufe erstellen
    best_runs = {}
    for key in priorize:
        best_runs.setdefault(key, {})

    percent_now = 0
    if isFirstRun:
        # Alles durchprobieren mit kleiner anzahl durchläufen
        job_list = jobListGenerator(dense_layers, layer_sizes, conv_layers, loss_algorithms, optimizers, activators, decision_activators, dense_activators, kernel_sizes)
        # Berechne mögliche Anzahl iterationen des initialen Laufs
        amount_of_iterations = len(dense_layers) * len(layer_sizes) * len(conv_layers) * len(loss_algorithms) * len(optimizers) * len(activators) * len(decision_activators) * len(dense_activators) * len(kernel_sizes)
    else:
        amount_of_iterations = len(job_list)

    # Anzahl der Top-Läufe bei der Auswertung
    top_list_size = int(amount_of_iterations * 0.1)
    if top_list_size < 10:
        top_list_size = 10
    if amount_of_trainings < 100 and amount_of_iterations == 10:
        amount_of_trainings = 100
    
    while amount_of_iterations > iteration_counter:
        # Arbeite Jobliste ab
        iteration_counter += 1
        # Lade nächste Parameterkombination laden
        if isFirstRun:
            current_settings = next(job_list)
        else:
            current_settings = job_list.pop()
        
        # Berechne Fortschritt
        percent_now = round(100 / amount_of_iterations * iteration_counter, 2)
        # Gebe Fortschritt aus
        printMessageBox([f"{percent_now}%", f"{iteration_counter} of {amount_of_iterations}",f"Minutes left: ~{time_table['minutesLeft']}", f"Round {round_counter}"])
        try:
            # Starte job
            acc, loss = runJob(current_settings, used_model_name, amount_of_trainings)
            # Update die Bestenliste/n
            best_runs = updateScoreList(best_runs, acc, loss, current_settings, top_list_size)
        except Exception as error_message:
            print("Cancelled")

        # Berechne benötigte Restzeit anhand durchschnittlicher Dauer
        time_table["deltaTotal"] += time() - time_table["timeStamp"]
        time_table["medTime"] = time_table["deltaTotal"] / iteration_counter
        time_table["minutesLeft"] = int(((amount_of_iterations - iteration_counter) * time_table["medTime"]) / 60)
        time_table["timeStamp"] = time()

    # Neue Jobliste anhand der Bestenliste generieren
    job_list = []
    for priority in best_runs.keys():
        for run in best_runs[priority].keys():
            if not best_runs[priority][run] in job_list:
                job_list += [best_runs[priority][run]]

    # Trainingslänge für den nächsten Lauf verlängern
    amount_of_trainings *= 4
    round_counter += 1
    isFirstRun = False

    if top_list_size == 10:
        # Tabelle für beste Läufe erstellen
        final_runs = {}
        for key in priorize:
            final_runs.setdefault(key, {})

        # Suche beste Runs aus den top 10 Runs heraus
        for priority in best_runs.keys():
            for run in best_runs[priority].keys():
                if priority == "accuracy":
                    final_runs = updateScoreList(final_runs, run, 0.99999, best_runs[priority][run], 1)
                elif priority == "loss":
                    final_runs = updateScoreList(final_runs, (1 / amount_of_categories) * 1.11, run, best_runs[priority][run], 1)
        
        # Führe letzte Runs aus
        for priority in final_runs.keys():
            printMessageBox([f"Strarting final run for {priority}"])
            for run in final_runs[priority].keys():
                if amount_of_trainings < 300:
                    amount_of_trainings = 300
                if priority == "accuracy":
                    acc, loss = runJob(final_runs[priority][run], f"final_ACC_{model_name}", amount_of_trainings, save_model=True, model_path=final_acc_dir)
                elif priority == "loss":
                    acc, loss = runJob(final_runs[priority][run], f"final_LOSS_{model_name}", amount_of_trainings, save_model=True, model_path=final_loss_dir)
        
        # Speichere die finalen Werte ab
        pickle_out = open(os.path.join(model_dir, f"final_result-{used_model_name}.param"), "wb") 
        pickle.dump(job_list, pickle_out)
        pickle_out.close()
        break



print("Bruteforcing done")