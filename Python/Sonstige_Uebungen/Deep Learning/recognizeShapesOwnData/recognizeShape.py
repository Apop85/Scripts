#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: recognizeShape.py
# Project: recognizeShapesOwnData
#-----
# Created Date: Saturday 16.01.2021, 23:03
# Author: Raffael Baldinger
#-----
# Last Modified: Saturday 16.01.2021, 23:03
#-----
# Copyright (c) 2021 Raffael Baldinger
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Neurales Netzwerk zum erkennen von einfachen Formen mit eigenem Datensatz
# Quelle: https://www.youtube.com/watch?v=j-3vuBynnOE
####

import os
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3' 
from random import randint as rng
from random import shuffle
from PIL import Image, ImageDraw
import math
import tensorflow as tf
import numpy as np
import matplotlib.pyplot as plt
import cv2
import pickle

example_file_path = os.path.join(".", "trainingData")
model_data_path = os.path.join(".", "modelData")
test_data_path = os.path.join(".", "testData")
training_model_path = os.path.join(model_data_path, "trainingModel")
pickle_X_path = os.path.join(model_data_path, "X.pickle")
pickle_y_path = os.path.join(model_data_path, "y.pickle")
categories = ["square", "triangle", "circle"]
image_size = (64,64)
training_data_amount = 3500
test_images_amount = 3
target_image_size = 30
number_of_trainings = 200
debug = True


os.chdir(os.path.dirname(__file__))

def createTrainingData(path, image_size, amount):
    print(f"Create {amount*3} Training Images...", end="")
    # Vierecke erstellen
    for i in range(amount):
        random_background_color = (rng(0,255), rng(0,255), rng(0,255))
        # random_background_color = (255,255,255)
        image = Image.new('RGB', image_size, random_background_color)
        draw = ImageDraw.Draw(image)

        square_center = (image_size[0] // 2, image_size[1] // 2)
        square_length = rng(10,45)

        square_vertices = (
            (square_center[0] + square_length / 2, square_center[1] + square_length / 2),
            (square_center[0] + square_length / 2, square_center[1] - square_length / 2),
            (square_center[0] - square_length / 2, square_center[1] - square_length / 2),
            (square_center[0] - square_length / 2, square_center[1] + square_length / 2)
        )

        square_vertices = [rotated_about(x,y, square_center[0], square_center[1], math.radians(rng(0,90))) for x,y in square_vertices]
        # square_vertices = [rotated_about(x,y, square_center[0], square_center[1], math.radians(45)) for x,y in square_vertices]

        random_square_color = random_background_color
        color_difference = 0
        while color_difference < 155:
            random_square_color = (rng(0,255),rng(0,255),rng(0,255))
            color_difference = random_background_color[0] + random_square_color[1] + random_square_color[2] - random_background_color[0] - random_background_color[1] - random_background_color[2]
            if color_difference < 0:
                color_difference *= -1
        draw.polygon(square_vertices, fill=random_square_color)
        image.save(path + "\\square_{}.png".format(i))
    # Dreiecke erstellen
    for i in range(amount):
        random_background_color = (rng(0,255), rng(0,255), rng(0,255))
        # random_background_color = (255,255,255)
        image = Image.new('RGB', image_size, random_background_color)
        draw = ImageDraw.Draw(image)

        triangle_center = (image_size[0] // 2, image_size[1] // 2)
        triangle_length = rng(10,45)

        triangle_vertices = (
            (triangle_center[0] + triangle_length / 2, triangle_center[1] + triangle_length / 2),
            (triangle_center[0] + triangle_length / 2, triangle_center[1] - triangle_length / 2),
            (triangle_center[0] - triangle_length / 2, triangle_center[1] - triangle_length / 2)
        )

        triangle_vertices = [rotated_about(x,y, triangle_center[0], triangle_center[1], math.radians(rng(0,90))) for x,y in triangle_vertices]
        # triangle_vertices = [rotated_about(x,y, triangle_center[0], triangle_center[1], math.radians(45)) for x,y in triangle_vertices]

        random_triangle_color = random_background_color
        color_difference = 0
        while color_difference < 155:
            random_triangle_color = (rng(0,255),rng(0,255),rng(0,255))
            color_difference = random_background_color[0] + random_triangle_color[1] + random_triangle_color[2] - random_background_color[0] - random_background_color[1] - random_background_color[2]
            if color_difference < 0:
                color_difference *= -1
        draw.polygon(triangle_vertices, fill=random_triangle_color)
        image.save(path + "\\triangle_{}.png".format(i))
    # Kreise erstellen
    for i in range(amount):
        # random_background_color = (255,255,255)
        random_background_color = (rng(0,255), rng(0,255), rng(0,255))
        image = Image.new('RGB', image_size, random_background_color)
        draw = ImageDraw.Draw(image)

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
        
        random_circle_color = random_background_color
        color_difference = 0
        while color_difference < 155:
            random_circle_color = (rng(0,255),rng(0,255),rng(0,255))
            color_difference = random_background_color[0] + random_circle_color[1] + random_circle_color[2] - random_background_color[0] - random_background_color[1] - random_background_color[2]
            if color_difference < 0:
                color_difference *= -1
        draw.ellipse((circle_top_left, circle_bottom_right), fill=random_circle_color)
        # draw.ellipse(((rad*2, rad*2), (rad*2, rad*2)), fill=random_circle_color)
        image.save(path + "\\circle_{}.png".format(i))
    print("Done")


#finds the straight-line distance between two points
def distance(ax, ay, bx, by):
    return math.sqrt((by - ay)**2 + (bx - ax)**2)

#rotates point `A` about point `B` by `angle` radians clockwise.
def rotated_about(ax, ay, bx, by, angle):
    radius = distance(ax,ay,bx,by)
    angle += math.atan2(ay-by, ax-bx)
    return (
        round(bx + radius * math.cos(angle)),
        round(by + radius * math.sin(angle))
    )

def createDataStructure(path, training_data=[], train=True):
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
            # if train:
            training_data.append([new_array, class_num])
            # else:
            #     training_data.append(new_array)
        except Exception as error_message:
            print(f"Can't open image {image} --> {error_message}")
    return training_data

if not os.path.exists(example_file_path):
    os.mkdir(example_file_path)
    createTrainingData(example_file_path, image_size, training_data_amount)
elif debug:
    createTrainingData(example_file_path, image_size, training_data_amount)

if not os.path.exists(model_data_path):
    os.mkdir(model_data_path)

if not os.path.exists(pickle_X_path) or not os.path.exists(pickle_y_path) or debug:
    print("Create Training Data...", end="")
    training_data = createDataStructure(example_file_path)
    shuffle(training_data)


    X_train = []
    y_train = []

    for features,label in training_data:
        X_train.append(features)
        y_train.append(label)

    X_train = tf.keras.utils.normalize(X_train, axis=1)
    X_train = np.array(X_train).reshape(-1, target_image_size, target_image_size, 1)
    X_train = np.asarray(X_train)
    y_train = np.asarray(y_train)
    print("Done")
    print(f"Training data: {len(training_data)}")

    print("Save Training Data...", end="")
    pickle_out = open(pickle_X_path, "wb")
    pickle.dump(X_train, pickle_out)
    pickle_out.close()
    pickle_out = open(pickle_y_path, "wb")
    pickle.dump(y_train, pickle_out)
    pickle_out.close()
    print("Done")
else:
    print("Load Training Data...", end="")
    pickle_in = open(pickle_X_path, "rb")
    X_train = pickle.load(pickle_in)
    pickle_in.close()
    pickle_in = open(pickle_y_path, "rb")
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

    model.compile(optimizer='adam',                         # adam = Defaultoptimizer
                loss='sparse_categorical_crossentropy',   # Fehlerhafte Vorhersagen / (sparse_)categorical_entropy = default / binary_categorical_entropy = default für 2 Klassifikationen
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
print("Verlust    : {}".format(val_loss))
print("Genauigkeit: {}".format(val_acc))

# Testen der Formerkennung
if not os.path.exists(test_data_path):
    os.mkdir(test_data_path)
    createTrainingData(test_data_path, image_size, test_images_amount)

X_test = createDataStructure(test_data_path, train=False)
shuffle(X_test)
X_new = []
y_test = []
for image, class_num in X_test:
    X_new.append(image)
    y_test.append(class_num)
X_test = X_new
X_test = np.array(X_test).reshape(-1, target_image_size, target_image_size, 1)
X_test = np.asarray(X_test)

predictions = model.predict([X_test])

for i in range(len(X_test)):
    # Wahrscheinlichster Wert auslesen
    highest_weight_value = np.argmax(predictions[i])
    # Ausgabe der wahrscheinlichsten Zahl
    print(f"{categories[highest_weight_value]} -> ", end="")
    if highest_weight_value == y_test[i]:
        print(True)
    else:
        print(False)
    # Zeige Bild an
    plt.imshow(X_test[i])
    plt.show()